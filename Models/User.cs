namespace BarnCaseAPI.Models;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class User
{
    [SwaggerSchema(ReadOnly = true)]
    public int Id { get; set; }

    public string Name { get; set; } = "";
    public string? Role { get; set; } = "User"; // Default role is User, can be User or Admin

    [JsonIgnore]
    public byte[] PasswordHash { get; set; } = default!;
    [JsonIgnore]
    public byte[] PasswordSalt { get; set; } = default!;

    public decimal Balance { get; set; }

    public List<Farm> Farms { get; set; } = new();
    public List<Ledger> Ledger { get; set; } = new();
    public List<RefreshToken> RefreshTokens { get; set; } = new();   
    
}
