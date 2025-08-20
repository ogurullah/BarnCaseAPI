using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace BarnCaseAPI.Models;

[Index(nameof(Token), IsUnique = true)]
public class RefreshToken
{
    public int Id { get; set; }

    [JsonIgnore]         // tokens should not be serialized
    public string Token { get; set; } = default!;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public int UserId { get; set; }

    [JsonIgnore]         // navigation properties should not be serialized
    public User User { get; set; } = default!;

    [JsonIgnore]
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}