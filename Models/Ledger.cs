using System;

namespace BarnCaseAPI.Models;

public class Ledger
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = default!; // Navigation property to User

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // saved on initialization
    public LedgerType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; } // Optional reference for the transaction
}