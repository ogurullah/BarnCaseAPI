namespace BarnCaseAPI.Contracts;

public sealed class UserResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Role { get; init; } = "User";
    public decimal Balance { get; init; }
}