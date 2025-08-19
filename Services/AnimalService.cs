using BarnCaseAPI.Data;
using BarnCaseAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BarnCaseAPI.Services;

public class AnimalService
{
    private readonly AppDbContext _Database;
    private readonly ILogger<AnimalService> _log;
    public AnimalService(AppDbContext Database, ILogger<AnimalService> log)
    {
        _Database = Database;
        _log = log;
    }

    private (decimal price, int lifeDays, int intervalMin, ProductType ptype) Specs(AnimalSpecies s) =>
        s switch
        {
            AnimalSpecies.Cow => (price: 500m, lifeDays: 90, intervalMin: 1, ptype: ProductType.Milk), // set to 1 for testing, default 60
            AnimalSpecies.Chicken => (price: 50m, lifeDays: 45, intervalMin: 120, ptype: ProductType.Eggs),
            AnimalSpecies.Sheep => (price: 200m, lifeDays: 60, intervalMin: 180, ptype: ProductType.Wool),
            _ => throw new ArgumentOutOfRangeException(nameof(s))
        };

    public async Task<Animal> BuyAsync(int userID, int farmId, AnimalSpecies species)
    {
        using var _ = _log.BeginScope(new { userID, farmId, species });
        _log.LogInformation("Buying {species} for farm with ID {farmId} for user with ID {userID}.", species, farmId, userID);

        var user = await _Database.Users.Include(u => u.Ledger).FirstAsync(u => u.Id == userID);
        var farm = await _Database.Farms.FirstAsync(f => f.Id == farmId && f.OwnerId == userID);

        var spec = Specs(species);
        if (user.Balance < spec.price)
            throw new InvalidOperationException("Insufficient balance");

        user.Balance -= spec.price;
        _Database.Ledger.Add(new Ledger
        {
            UserId = userID,
            Type = LedgerType.PurchaseAnimal,
            Amount = spec.price,
            Reference = $"Bought {species} for farm {farm.Name}"
        });

        var animal = new Animal
        {
            FarmID = farmId,
            Species = species,
            PurchasePrice = spec.price,
            LifeSpanInDays = spec.lifeDays,
            RemainingLifeDays = spec.lifeDays, // initially full lifespan
            PurchasedAt = DateTime.UtcNow,
            ProductionIntervalInMinutes = spec.intervalMin
        };

        _Database.Animals.Add(animal);
        await _Database.SaveChangesAsync();

        _log.LogInformation("Animal {species} purchased successfully with ID {AnimalId} for farm with ID {farmId} for user with ID {userId}", species, animal.Id, farmId, userID);

        return animal;
    }


    public async Task<decimal> SellAsync(int userId, int animalId)
    {

        var animal = await _Database.Animals.Include(a => a.Farm)
            .ThenInclude(f => f.Owner)
            .FirstAsync(a => a.Id == animalId && a.Farm.OwnerId == userId);

        var sellPrice = animal.isAlive ? Math.Round(animal.PurchasePrice * 0.7m, 2) : 0m;  //zero if dead

        var user = animal.Farm.Owner;
        user.Balance += sellPrice;

        _Database.Ledger.Add(new Ledger
        {
            UserId = userId,
            Type = LedgerType.SellAnimal,
            Amount = sellPrice,
            Reference = $"Sold {animal.Species} for farm {animal.Farm.Name}"
        });

        _Database.Animals.Remove(animal);
        await _Database.SaveChangesAsync();
        return sellPrice;
    }
    public async Task<IEnumerable<Animal>> ViewAnimalsAsync(int farmId)
    {
        return await _Database.Animals
            .Where(a => a.FarmID == farmId)
            .ToListAsync();
    }
}