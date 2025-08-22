// Services/TokenService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using BarnCaseAPI.Models;
using BarnCaseAPI.Options;

namespace BarnCaseAPI.Services;

public interface ITokenService
{
    (string jwt, DateTime expiresUtc) CreateAccessToken(User user);
    string CreateRefreshToken();
    SymmetricSecurityKey GetSigningKey();
}

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _opt;
    private readonly SymmetricSecurityKey _signingKey;

    public TokenService(IOptions<JwtOptions> opt)
    {
        _opt = opt.Value ?? throw new InvalidOperationException("Jwt options missing.");
        if (string.IsNullOrWhiteSpace(_opt.SigningKeyB64))
            throw new InvalidOperationException("Jwt:SigningKeyB64 missing.");
        _signingKey = new SymmetricSecurityKey(Convert.FromBase64String(_opt.SigningKeyB64));
    }

    public SymmetricSecurityKey GetSigningKey() => _signingKey;

    public (string jwt, DateTime expiresUtc) CreateAccessToken(User user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_opt.AccessTokenMinutes);

        var role = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role.Trim();

        var claims = new List<Claim>
        {
            // identity
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),

            // authorization
            new Claim(ClaimTypes.Role, role),
        };

        var creds = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
