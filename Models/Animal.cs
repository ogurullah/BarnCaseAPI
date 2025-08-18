using System;

namespace BarnCaseAPI.Models;

public class Animal {
    public int Id { get; set; }
    public int FarmID { get; set; }
    public Farm Farm { get; set; } = default!; // Navigation property to Farm

    public AnimalSpecies Species { get; set; }
    public DateTime PurchasedAt { get; set; }
    public decimal PurchasePrice { get; set; }

    public int LifeSpanInDays { get; set; }

    public int RemainingLifeDays { get; set; }
    public int ProductionIntervalInMinutes { get; set; }
    public DateTime? LastProductionAt { get; set; }

    public bool isAlive { get; set; } = true;


}