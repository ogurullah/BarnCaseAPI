namespace BarnCaseAPI.Contracts;

public record CreateUserRequest
{
    public string Name { get; init; } = "";
    public decimal Balance { get; init; } = 1000m; //all users start with 1000 money
}