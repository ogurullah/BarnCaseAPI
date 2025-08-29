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
        if (owner == null)
        {
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

    public async Task<IReadOnlyList<Farm>> GetAllFarmsForUserAsync(int userId)
    {
        return await _Database.Farms
            .Include(f => f.Animals)
            .Include(f => f.Products)
            .Where(f => f.OwnerId == userId)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task<bool> DeleteFarmAsync(int userId, int farmId)
    {
        using var _ = _log.BeginScope(new { userId, farmId });
        _log.LogInformation("Deleting farm with ID {farm} from user with ID {userId}.", farmId, userId);
        var farm = await _Database.Farms
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.Id == farmId);

        if (farm is null)
        {
            _log.LogInformation("Farm with ID {farm} does not exist so it can not be deleted.", farmId);
            return false;
        }    
        if (farm.OwnerId != userId)
                throw new UnauthorizedAccessException();

        // if you have related Animals/Products with FK constraints, either
        // configure cascade delete in your model, or remove them explicitly:
        // _Database.Animals.RemoveRange(_Database.Animals.Where(a => a.FarmId == farmId));
        // _Database.Products.RemoveRange(_Database.Products.Where(p => p.FarmId == farmId));

        _Database.Farms.Remove(farm);
        await _Database.SaveChangesAsync();
        return true;
    }

}