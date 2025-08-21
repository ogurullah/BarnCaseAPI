namespace BarnCaseAPI.Contracts;

public record CreateUserRequest
{
    public string Name { get; init; } = "";
    public decimal Balance { get; init; } = 0m;
}