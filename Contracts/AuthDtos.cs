public record RegisterRequest(string Username, string Password, string? Role);
public record LoginRequest(string Username, string Password);
public record TokenResponse(string AccessToken, DateTime ExpiresAtUtc, string RefreshToken);
public record RefreshRequest(string RefreshToken);
