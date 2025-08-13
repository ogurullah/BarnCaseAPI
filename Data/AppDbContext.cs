using Microsoft.EntityFrameworkCore;
using BarnCaseAPI.Models;

namespace BarnCaseAPI.Data; // <- change to your project’s root namespace if different

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<Animal> Animals => Set<Animal>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Ledger> Ledger => Set<Ledger>();

    // Example DbSets — replace with your real entities
    //public DbSet<User> Users { get; set; } = default!;
    // public DbSet<Something> Somethings { get; set; } = default!;

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
            .HasForeignKey(a => a.FarmID);

        b.Entity<Product>()
            .HasOne(p => p.Farm)
            .WithMany(f => f.Products)
            .HasForeignKey(p => p.FarmId);

        b.Entity<User>().Property(u => u.Balance).HasPrecision(18, 2);
        b.Entity<Product>().Property(p => p.UnitPrice).HasPrecision(18, 2);
        b.Entity<Product>().Property(p => p.SoldTotal).HasPrecision(18, 2);
        b.Entity<Animal>().Property(a => a.PurchasePrice).HasPrecision(18, 2);
        b.Entity<Ledger>().Property(l => l.Amount).HasPrecision(18, 2);
    }
}
