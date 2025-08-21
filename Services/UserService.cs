using BarnCaseAPI.Data;
using BarnCaseAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using BarnCaseAPI.Contracts;

namespace BarnCaseAPI.Services;

public class UserService
{
    private readonly AppDbContext _Database;
    private readonly ILogger<UserService> _log;

    public UserService(AppDbContext Database, ILogger<UserService> log)
    {
        _Database = Database;
        _log = log;
    }

    public async Task<User?> GetUser(int id)
    {
        using var _ = _log.BeginScope(new { id });
        var user = await _Database.Users.FindAsync(id);
        if (user is null)
        {
            _log.LogWarning("User with ID {id} not found.", id);
            return null;
        }
        return user;
    }

    public async Task<IEnumerable<User>> GetUsers(int skip = 0, int take = 50)
    {
        var items = await _Database.Users
                            .AsNoTracking()
                            .OrderBy(u => u.Id)
                            .Skip(Math.Max(0, skip))
                            .Take(Math.Clamp(take, 1, 200))
                            .ToListAsync();
        return items;
    }
//    public record CreateUserRequest(string Name, decimal Balance = 0);
    public async Task<User> CreateUser(CreateUserRequest Request)
    {
        using var _ = _log.BeginScope(new { Request.Name, Request.Balance });
        var user = new User { Name = Request.Name, Balance = Request.Balance };
        _Database.Users.Add(user);
        _log.LogInformation("User {Name} created with ID {UserId} and balance {Balance}.", user.Name, user.Id, user.Balance);
        await _Database.SaveChangesAsync();
        return user;
    }

    public async Task UpdateUser(int id, User incoming)
    {
        using var _ = _log.BeginScope(new { id, incoming.Name, incoming.Balance });
        if (id != incoming.Id)
        {
            _log.LogWarning("User ID in URL and body did not match during update. URL ID: {id}, Body ID: {incomingId}", id, incoming.Id);
            throw new ArgumentException("Id in URL and body must match.");
        }
        var user = await _Database.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            _log.LogWarning("User with ID {id} not found during update.", id);
            throw new KeyNotFoundException($"User with ID {id} not found.");
        }

        user.Name = incoming.Name;
        user.Balance = incoming.Balance;
        _log.LogInformation("User with ID {id} updated. New Name: {Name}, New Balance: {Balance}.", id, user.Name, user.Balance);

        await _Database.SaveChangesAsync();
    }


    public async Task DeleteUser(int id)
    {
        using var _ = _log.BeginScope(new { id });
        var user = await _Database.Users.FindAsync(id);
        if (user is null)
        {
            _log.LogWarning("User with ID {id} not found during deletion.", id);
            throw new KeyNotFoundException($"User with ID {id} not found.");
        }
        _Database.Users.Remove(user);
        _log.LogInformation("User with ID {id} deleted successfully.", id);
        await _Database.SaveChangesAsync();
        return;
    }
}