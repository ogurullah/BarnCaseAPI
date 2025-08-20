using BarnCaseAPI.Models;
using BarnCaseAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarnCaseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _users;

        public UsersController(UserService users)
        {
            _users = users;
        }

        // GET: api/users?skip=0&take=50
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<User>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers([FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            var items = await _users.GetUsers(skip, take);
            return Ok(items);
        }

        // GET: api/users/5
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            try
            {
                var user = await _users.GetUser(id);
                return Ok(user);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        // POST: api/users
        public record CreateUserRequest(string Name, decimal Balance = 0);

        [HttpPost]
        [ProducesResponseType(typeof(User), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Name is required.");

            try
            {
                var user = await _users.CreateUser(new UserService.CreateUserRequest(request.Name, request.Balance));
                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
            }
            catch (DbUpdateException ex)
            {
                // If you log, do it here
                return Problem(title: "Failed to create user.", detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // PUT: api/users/5
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User update)
        {
            try
            {
                await _users.UpdateUser(id, update);
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
            catch (DbUpdateConcurrencyException)
            {
                // If using rowversion/concurrency tokens, map to 409
                return Conflict("The user was modified by another process. Retry your request.");
            }
        }

        // DELETE: api/users/5
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                await _users.DeleteUser(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (DbUpdateException ex)
            {
                return Problem(title: "Failed to delete user.", detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
