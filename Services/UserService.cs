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
    private readonly IPasswordService _Passwords;

    public UserService(AppDbContext Database, ILogger<UserService> log, IPasswordService Passwords)
    {
        _Database = Database;
        _log = log;
        _Passwords = Passwords;
    }

    public async Task<User> RegisterAsync(RegisterRequest request, string Role = "User")
    {
        using var _ = _log.BeginScope(new { request.Name });

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            _log.LogWarning("Name is empty.");
            throw new ArgumentException("Name is required.", nameof(request.Name));
        }
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            _log.LogWarning("Password can't be shorter than 8 characters.");
            throw new ArgumentException("Password can't be shorter than 8 characters.", nameof(request.Password));
        }

        bool exists = await _Database.Users.AnyAsync(u => u.Name == request.Name);
        if (exists)
        {
            _log.LogWarning("Username {request.Name} already exists.", request.Name);
            throw new InvalidOperationException("Username already exists.");
        }

        var (hash, salt) = _Passwords.Hash(request.Password);

        var user = new User
        {
            Name = request.Name,
            Role = string.IsNullOrWhiteSpace(request.Role) ? "User" : request.Role,
            PasswordHash = hash,
            PasswordSalt = salt,
            Balance = request.Balance
        };

        _Database.Users.Add(user);
        await _Database.SaveChangesAsync();

        _log.LogInformation("User {name} registered with ID {UserId}", user.Name, user.Id);
        return user;
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

    public async Task UpdateUser(int id, UpdateUserRequest incoming, bool isAdmin, bool isOwner)
    {
        using var _ = _log.BeginScope(new { id, incoming?.Name, isAdmin, isOwner});

        var user = await _Database.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            _log.LogWarning("User with ID {id} not found during update.", id);
            throw new KeyNotFoundException($"User with ID {id} not found.");
        }

        if (incoming is null)
        {
            throw new ArgumentException("Body is required.");
        }

        if (!(isAdmin || isOwner))
        {
            throw new UnauthorizedAccessException("Only accounts owners or admins can modify account data.");
        }
        if (incoming.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(incoming.Name))
            {
                throw new ArgumentException("Name can't be empty.");
            }
            else if (isAdmin || isOwner)
            {
                user.Name = incoming.Name.Trim();
            }
        }
        if (incoming.Balance is not null)
        {
            if (!isAdmin)
            {
                throw new UnauthorizedAccessException("Only admins can update balance.");
            }
            else
            {
                user.Balance = incoming.Balance.Value;
            }
        }
        if (incoming.Role is not null)
        {
            if (!isAdmin)
            {
                throw new UnauthorizedAccessException("Only admins can update roles.");
            }
            else
            {
                var role = incoming.Role.Trim();
                if (role is not ("User" or "Admin"))
                {
                    throw new ArgumentException("Role must be 'User' or 'Admin'.");
                }
                user.Role = role;
            }
        }

        await _Database.SaveChangesAsync();
        _log.LogInformation("User {id} updated.", id);
    }


    public async Task DeleteUser(int id, bool isAdmin, bool isOwner)
    {
        using var _ = _log.BeginScope(new { id });
        if (!(isAdmin || isOwner))
        {
            throw new UnauthorizedAccessException("Only admins or account owners can delete user accounts.");
        }
        
        var user = await _Database.Users.FindAsync(id);
        if (user is null)
        {
            _log.LogWarning("User with ID {id} not found during deletion.", id);
            throw new KeyNotFoundException($"User with ID {id} not found.");
        }
        _Database.Users.Remove(user);
        await _Database.SaveChangesAsync();
        _log.LogInformation("User with ID {id} deleted successfully.", id);
        return;
    }
}