using BarnCaseAPI.Data;
using BarnCaseAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BarnCaseAPI.Services;

public class ProductionService
{
    private readonly AppDbContext _Database;
    public ProductionService(AppDbContext Database) => _Database = Database;

    private decimal UnitPrice(ProductType type) => type switch
    {
        ProductType.Milk => 2.5m,
        ProductType.Eggs => 0.4m,
        ProductType.Wool => 5m,
        _ => 1m // Default price for unknown types
    };

    private ProductType Map(AnimalSpecies species) => species switch
    {
        AnimalSpecies.Cow => ProductType.Milk,
        AnimalSpecies.Chicken => ProductType.Eggs,
        AnimalSpecies.Sheep => ProductType.Wool,
        _ => ProductType.Milk // Default to Milk if unknown
    };

    public async Task<int> TickAsync(int farmId, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        var animals = await _Database.Animals.Where(a => a.FarmID == farmId && a.isAlive)
            .ToListAsync();

        int created = 0;
        foreach (var animal in animals)
        {
            if ((now - animal.PurchasedAt).TotalDays >= animal.LifeSpanInDays)
            {
                animal.isAlive = false;
                continue;
            }

            var due = animal.LastProductionAt == null ||
                (now - animal.LastProductionAt.Value).TotalMinutes >= animal.ProductionIntervalInMinutes;

            if (!due) continue;

            var productType = Map(animal.Species);
            var unitPrice = UnitPrice(productType);
            var quantity = productType switch
            {
                ProductType.Milk => 5, // 5 liters
                ProductType.Eggs => 3, // 3 eggs
                ProductType.Wool => 1, // 1 wool
                _ => 1 // Default to 1 unit for unknown types
            };

            _Database.Products.Add(new Product
            {
                FarmId = animal.FarmID,
                AnimalId = animal.Id,
                Type = productType,
                Quantity = quantity,
                UnitPrice = unitPrice,
                CreatedAt = now
            });

            animal.LastProductionAt = now;
            created++;
        }
        await _Database.SaveChangesAsync();
        return created;
    }
}