using BarnCaseAPI.Data;
using BarnCaseAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BarnCaseAPI.Services;

public class FarmService
{
    private readonly AppDbContext _Database;
    private readonly ILogger<FarmService> _log;
    public FarmService(AppDbContext Database, ILogger<FarmService> log)
    {
        _Database = Database;
        _log = log;
    }
    public async Task<Farm> CreateFarmAsync(int ownerId, string name)
    {
        using var _ = _log.BeginScope(new { ownerId, name });
        _log.LogInformation("Creating farm named {name} for user with id {ownerId}.", name, ownerId);

        var owner = await _Database.Users.FindAsync(ownerId);
        if (owner == null) {
            _log.LogWarning("User with ID {ownerId} not found. Cannot create farm.", ownerId);
            throw new InvalidOperationException("User not found");
        }

        var farm = new Farm { Name = name, OwnerId = ownerId };
        _Database.Farms.Add(farm);
        await _Database.SaveChangesAsync();

        _log.LogInformation("Farm created successfully with FarmId {FarmId}.", farm.Id);

        return farm;
    }

    public Task<Farm?> GetFarmAsync(int farmId) =>
        _Database.Farms.Include(f => f.Animals).Include(f => f.Products)
            .FirstOrDefaultAsync(f => f.Id == farmId);
}