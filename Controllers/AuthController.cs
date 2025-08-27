using BarnCaseAPI.Data;
using BarnCaseAPI.Models;
using BarnCaseAPI.Options;
using BarnCaseAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BarnCaseAPI.Contracts;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _Database;
    private readonly IPasswordService _Password;
    private readonly ITokenService _Tokens;
    private readonly JwtOptions _Options;

    public AuthController(AppDbContext database, IPasswordService password, ITokenService tokens, IOptions<JwtOptions> options)
    {
        _Database = database;
        _Password = password;
        _Tokens = tokens;
        _Options = options.Value;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await _Database.Users.AnyAsync(u => u.Name == request.Name))
            return Conflict("Username already taken.");

        var (hash, salt) = _Password.Hash(request.Password);
        var user = new User
        {
            Name = request.Name,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = string.IsNullOrWhiteSpace(request.Role) ? "User" : request.Role
        };
        _Database.Users.Add(user);
        await _Database.SaveChangesAsync();

        var (jwt, exp) = _Tokens.CreateAccessToken(user);
        var rt = new RefreshToken
        {
            Token = _Tokens.CreateRefreshToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_Options.RefreshTokenDays),
            UserId = user.Id
        };
        _Database.RefreshTokens.Add(rt);
        await _Database.SaveChangesAsync();

        return Ok(new TokenResponse(jwt, exp, rt.Token));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _Database.Users.FirstOrDefaultAsync(u => u.Name == request.Name);
        if (user == null || !_Password.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
            return Unauthorized("Invalid credentials.");

        var (jwt, exp) = _Tokens.CreateAccessToken(user);
        var rt = new RefreshToken
        {
            Token = _Tokens.CreateRefreshToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_Options.RefreshTokenDays),
            UserId = user.Id
        };
        _Database.RefreshTokens.Add(rt);
        await _Database.SaveChangesAsync();

        return Ok(new TokenResponse(jwt, exp, rt.Token));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var token = await _Database.RefreshTokens.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (token is null || !token.IsActive) return Unauthorized("Invalid refresh token.");

        token.RevokedAt = DateTime.UtcNow;
        var newRt = new RefreshToken
        {
            Token = _Tokens.CreateRefreshToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_Options.RefreshTokenDays),
            UserId = token.UserId
        };
        _Database.RefreshTokens.Add(newRt);

        var (jwt, exp) = _Tokens.CreateAccessToken(token.User);
        await _Database.SaveChangesAsync();

        return Ok(new TokenResponse(jwt, exp, newRt.Token));
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RefreshRequest request)
    {
        var token = await _Database.RefreshTokens.FirstOrDefaultAsync(t => t.Token == request.RefreshToken);
        if (token is null) return NotFound();
        token.RevokedAt = DateTime.UtcNow;
        await _Database.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("whoami")]
    public IActionResult WhoAmI()
    {
        return Ok(new
        {
            name = User.FindFirstValue(ClaimTypes.Name),
            id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            isAuthenticated = User.Identity?.IsAuthenticated ?? false,
            roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray(),
            claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }
}