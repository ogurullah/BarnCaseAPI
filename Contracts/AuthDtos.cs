namespace BarnCaseAPI.Contracts;

public record RegisterRequest(string Name, string Password, string? Role);
public record LoginRequest(string Name, string Password);
public record TokenResponse(string AccessToken, DateTime ExpiresAtUtc, string RefreshToken);
public record RefreshRequest(string RefreshToken);
