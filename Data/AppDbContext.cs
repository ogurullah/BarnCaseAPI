using Microsoft.EntityFrameworkCore;
using BarnCaseAPI.Models;
using Microsoft.AspNetCore.Routing.Template;

namespace BarnCaseAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<Animal> Animals => Set<Animal>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Ledger> Ledger => Set<Ledger>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>()
            .HasIndex(u => u.Name)
            .IsUnique();

        b.Entity<Farm>()
            .HasOne(f => f.Owner)
            .WithMany(u => u.Farms)
            .HasForeignKey(f => f.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Animal>()
            .HasOne(a => a.Farm)
            .WithMany(f => f.Animals)
            .HasForeignKey(a => a.FarmID)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Product>()
            .HasOne(p => p.Farm)
            .WithMany(f => f.Products)
            .HasForeignKey(p => p.FarmId)
            .OnDelete(DeleteBehavior.NoAction);

        b.Entity<RefreshToken>()
            .Property(rt => rt.Token)
            .HasMaxLength(512);
        
        b.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<User>().Property(u => u.Balance).HasPrecision(18, 2);
        b.Entity<Product>().Property(p => p.UnitPrice).HasPrecision(18, 2);
        b.Entity<Product>().Property(p => p.SoldTotal).HasPrecision(18, 2);
        b.Entity<Animal>().Property(a => a.PurchasePrice).HasPrecision(18, 2);
        b.Entity<Ledger>().Property(l => l.Amount).HasPrecision(18, 2);
    }
}
