using BarnCaseAPI.Data;
using BarnCaseAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BarnCaseAPI.Services;

public class FarmService
{
    private readonly AppDbContext _Database;
    public FarmService(AppDbContext Database) => _Database = Database;

    public async Task<Farm> CreateFarmAsync(int ownerId, string name)
    {
        var owner = await _Database.Users.FindAsync(ownerId)
            ?? throw new InvalidOperationException("Owner not found");

        var farm = new Farm { Name = name, OwnerId = ownerId };
        _Database.Farms.Add(farm);
        await _Database.SaveChangesAsync();
        return farm;
    }

    public Task<Farm?> GetFarmAsync(int farmId) =>
        _Database.Farms.Include(f => f.Animals).Include(f => f.Products)
            .FirstOrDefaultAsync(f => f.Id == farmId);
}