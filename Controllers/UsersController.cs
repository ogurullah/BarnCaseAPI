using BarnCaseAPI.Models;
using BarnCaseAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BarnCaseAPI.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using BarnCaseAPI.Security;

namespace BarnCaseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UsersController> _log;
        private readonly UserService _users;

        public UsersController(UserService users, ILogger<UsersController> log)
        {
            _users = users;
            _log = log;
        }

        private static UserResponse ToResponse(User user) => new()
        {
            Id = user.Id,
            Name = user.Name,
            Role = user.Role,
            Balance = user.Balance
        };

        // POST: api/user/register (public)
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)] //success
        [ProducesResponseType(StatusCodes.Status400BadRequest)]                    //wrong input
        [ProducesResponseType(StatusCodes.Status409Conflict)]                      //user already exists
        public async Task<ActionResult<UserResponse>> Register([FromBody] RegisterRequest request)
        {
            using var _ = _log.BeginScope("Register {Name}", request.Name);

            try
            {
                var user = await _users.RegisterAsync(request, Role: "User");
                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, ToResponse(user));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (DbUpdateException ex)
            {
                var root = ex.GetBaseException()?.Message ?? ex.Message;
                _log.LogError(ex, "Register failed {Root}", root);
                return Problem(title: "Failed to register user.", detail: root, statusCode: 500);
            }
        }

        // GET: api/users?skip=0&take=50
        [HttpGet]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers([FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            if (skip < 0) skip = 0;
            take = Math.Clamp(take, 1, 200);

            var items = await _users.GetUsers(skip, take);
            return Ok(items.Select(ToResponse));
        }

        // GET: api/users/5
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserResponse>> GetUser(int id)
        {
            var user = await _users.GetUser(id);
            return user is null ? NotFound() : Ok(ToResponse(user));
        }

        // PUT: api/users/5
        [Authorize]
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest update)
        {
            try
            {
                var callerId = User.UserId();
                bool isAdmin = User.IsInRole("Admin");
                bool isOwner = callerId == id;
                if (!(isAdmin || isOwner))
                {
                    return Forbid("Only admins or account owners can modify user account data.");
                }
                await _users.UpdateUser(id, update, isAdmin, isOwner);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid("Only admins or account owners can modify user account data.");
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict("The user was modified by another process. Retry your request.");
            }
        }

        // DELETE: api/users/5
        [Authorize]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // error conditions should be revisited
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var callerId = User.UserId();
                bool isAdmin = User.IsInRole("Admin");
                bool isOwner = callerId == id;
                if (isAdmin || isOwner)
                {
                    await _users.DeleteUser(id, isAdmin, isOwner);
                    return NoContent();
                }
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (DbUpdateException ex)
            {
                return Problem(title: "Failed to delete user.", detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
            return Forbid("Only admins or account owners can delete user accounts.");
        }
    }
}
