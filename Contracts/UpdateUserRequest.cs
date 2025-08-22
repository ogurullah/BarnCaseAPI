namespace BarnCaseAPI.Contracts;

public sealed class UpdateUserRequest
{
    public string? Name { get; set; }
    public decimal? Balance { get; set; }
    public string? Role { get; set; }
}