using Microsoft.EntityFrameworkCore;
using BarnCaseAPI.Models;

namespace BarnCaseAPI.Data; // <- change to your project’s root namespace if different

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    // Example DbSets — replace with your real entities
    public DbSet<User> Users { get; set; } = default!;
    // public DbSet<Something> Somethings { get; set; } = default!;
}
