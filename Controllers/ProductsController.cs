


//revisit this file before making any adjustments



using BarnCaseAPI.Data;
using BarnCaseAPI.Models; // <- assumes you have a User entity in Models
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarnCaseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController(AppDbContext db) : ControllerBase
    {
        // GET: api/users?skip=0&take=50
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers([FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            var items = await db.Users
                                .AsNoTracking()
                                .Skip(Math.Max(0, skip))
                                .Take(Math.Clamp(take, 1, 200))
                                .ToListAsync();
            return Ok(items);
        }

        // GET: api/users/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await db.Users.FindAsync(id);
            return user is null ? NotFound() : Ok(user);
        }

        // POST: api/users
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser([FromBody] User user)
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
            // returns 201 + Location header -> GET api/users/{id}
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        // PUT: api/users/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User update)
        {
            if (id != update.Id) return BadRequest("Id in URL and body must match.");

            var exists = await db.Users.AnyAsync(u => u.Id == id);
            if (!exists) return NotFound();

            db.Entry(update).State = EntityState.Modified;
            await db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/users/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await db.Users.FindAsync(id);
            if (user is null) return NotFound();

            db.Users.Remove(user);
            await db.SaveChangesAsync();
            return NoContent();
        }
    }
}
