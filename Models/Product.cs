using System;

namespace BarnCaseAPI.Models;

public class Product {

    public int Id { get; set; }
    public int FarmId { get; set; }
    public Farm Farm { get; set; } = default!; // Navigation property to Farm

    public int AnimalId { get; set; }
    public Animal Animal { get; set; } = default!; // Navigation property to Animal

    public ProductType Type { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; //saved on initialization

    public bool isSold { get; set; }
    public DateTime? SoldAt { get; set; }
    public decimal? SoldTotal { get; set; }

}