using BarnCaseAPI.Data;
using BarnCaseAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BarnCaseAPI.Services;

public class ProductService
{
    private readonly AppDbContext _Database;
    private readonly ILogger<ProductService> _log;
    public ProductService(AppDbContext Database, ILogger<ProductService> log)
    {
        _Database = Database;
        _log = log;
    }

    public async Task<decimal> SellAsync(int userId, int farmId, IEnumerable<int> productIds)
    {
        using var _ = _log.BeginScope(new { userId, farmId, productIds });
        _log.LogInformation("Selling products for farm with ID {farmId} for user with ID {userId}. Products: {productIds}", farmId, userId, productIds);

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
        _log.LogInformation("Total profit is {Total} from selling products {productIds} for farm with ID {farmId}.", total, productIds, farmId);

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

    public async Task<IReadOnlyList<Product>> GetProductsByFarmAsync(int farmId)
    {
        var rows = await _Database.Products
            .Where(p => p.FarmId == farmId)
            .AsNoTracking()
            .ToListAsync();

        return rows;
    }

    public async Task<IReadOnlyList<(string Name, int Count)>> GetProductCountsForUserAsync(int userId)
    {
        var rows = await _Database.Products
            .Where(p => p.Farm.OwnerId == userId)
            .GroupBy(p => p.Type)
            .Select(g => new { Name = g.Key.ToString(), Count = g.Count() })
            .AsNoTracking()
            .ToListAsync();

        return rows.Select(r => (r.Name, r.Count)).ToList();
    }
}