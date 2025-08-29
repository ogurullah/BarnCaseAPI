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

    public async Task<decimal> SellAsync(int userId, ProductType type, int quantity)
    {
        var items = await _Database.Products
            .Where(p => p.Farm.OwnerId == userId && p.Type == type)
            .OrderBy(p => p.Id)
            .Take(quantity)
            .ToListAsync();

        if (items.Count < quantity)
            throw new InvalidOperationException("Insufficient quantity.");

        var (ok, price) = await GetSellQuoteAsync(userId, type, quantity);
        if (!ok) throw new InvalidOperationException("Insufficient quantity.");

        _Database.Products.RemoveRange(items);
        var user = await _Database.Users.FindAsync(userId);
        if (user != null) user.Balance += price;

        await _Database.SaveChangesAsync();
        return price;
    }

    public async Task<(bool ok, decimal price)> GetSellQuoteAsync(int userId, ProductType type, int quantity)
    {
        var available = await _Database.Products
            .Where(p => p.Farm.OwnerId == userId && p.Type == type)
            .CountAsync();

        if (available < quantity) return (false, 0m);

        decimal unitPrice = type switch
        {
            ProductType.Milk => 2.5m,
            ProductType.Eggs => 0.4m,
            ProductType.Wool => 5m,
            _ => 1m
        };
        return (true, unitPrice * quantity);
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