using System.Collections.Generic;

namespace BarnCaseAPI.Models;

public class Farm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int OwnerId { get; set; }
    public User Owner { get; set; } = default!; // Navigation property to User

    public List<Animal> Animals { get; set; } = new();
    public List<Product> Products { get; set; } = new();
}