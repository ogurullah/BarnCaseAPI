using BarnCaseAPI.Data;
using BarnCaseAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BarnCaseAPI.Services;

public class ProductService
{
    private readonly AppDbContext _Database;
    public ProductService(AppDbContext Database) => _Database = Database;

    public async Task<decimal> SellAsync(int userId, int farmId, IEnumerable<int> productIds)
    {
        var farm = await _Database.Farms.Include(f => f.Owner)
            .FirstAsync(f => f.Id == farmId && f.OwnerId == userId);

        var items = await _Database.Products
            .Where(p => p.FarmId == farmId && productIds.Contains(p.Id) && !p.isSold)
            .ToListAsync();


        if (items.Count == 0) return 0m;
        decimal total = 0m;

        foreach (var product in items)
        {
            var TotalPrice = product.Quantity * product.UnitPrice;
            product.isSold = true;
            product.SoldAt = DateTime.UtcNow;
            product.SoldTotal = TotalPrice;
            total += TotalPrice;
        }

        farm.Owner.Balance += total;

        _Database.Ledger.Add(new Ledger
        {
            UserId = farm.OwnerId,
            Type = LedgerType.SellProduct,
            Amount = total,
            Reference = $"Sold {items.Count} products from farm {farm.Name}"
        });

        await _Database.SaveChangesAsync();
        return Math.Round(total, 2);
    }
}