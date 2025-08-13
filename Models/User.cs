namespace BarnCaseAPI.Models;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;

public class User
{
    [SwaggerSchema(ReadOnly = true)]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public decimal Balance { get; set; }

    public List<Farm> Farms { get; set; } = new();
    public List<Ledger> Ledger { get; set; } = new();
    
}
