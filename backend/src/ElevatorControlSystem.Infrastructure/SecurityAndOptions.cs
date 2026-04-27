using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ElevatorControlSystem.Application;
using ElevatorControlSystem.Domain;

namespace ElevatorControlSystem.Infrastructure;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 480;
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public sealed class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<ApplicationUser> _passwordHasher = new();

    public string HashPassword(ApplicationUser user, string password)
    {
        return _passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(ApplicationUser user, string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        return result == PasswordVerificationResult.Success
            || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly IClock _clock;

    public JwtTokenService(
        IOptions<JwtOptions> jwtOptions,
        IClock clock)
    {
        _jwtOptions = jwtOptions.Value;
        _clock = clock;
    }

    public GeneratedToken GenerateToken(ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(_jwtOptions.Key))
        {
            throw new InvalidOperationException("JWT secret key is not configured.");
        }

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.Key));

        var signingCredentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var expiresAtUtc = _clock.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.GivenName, user.DisplayName),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

        return new GeneratedToken
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc
        };
    }
}
