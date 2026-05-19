using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Auth;

/// <summary>
/// Settings injected from appsettings.json → JwtSettings section.
/// </summary>
public class JwtSettings
{
    /// <summary>Symmetric signing key. Must be at least 32 characters in production.</summary>
    public string SecretKey { get; set; } = default!;

    /// <summary>Token issuer — should match the portal's base URL.</summary>
    public string Issuer { get; set; } = default!;

    /// <summary>Intended audience for issued tokens.</summary>
    public string Audience { get; set; } = default!;

    /// <summary>Access token lifetime in minutes. Default 15 minutes.</summary>
    public int AccessTokenExpiryMinutes { get; set; } = 15;

    /// <summary>Refresh token lifetime in days. Default 7 days.</summary>
    public int RefreshTokenExpiryDays { get; set; } = 7;
}

/// <summary>
/// Generates and validates JWT access tokens and opaque refresh tokens.
/// Refresh tokens are not JWTs — they are cryptographically random bytes
/// stored as a hash in the database to prevent plain-text exposure.
/// </summary>
public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly ApplicationDbContext _db;

    public TokenService(IOptions<JwtSettings> options, ApplicationDbContext db)
    {
        _settings = options.Value;
        _db = db;
    }

    /// <summary>
    /// Builds and signs a JWT access token for the given user.
    /// Embeds the user ID, username, role name, and department ID as claims
    /// so downstream authorization policies can read them without a database hit.
    /// </summary>
    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(ClaimTypes.Role, user.Role?.Name ?? string.Empty),
            new("role_id", user.RoleId.ToString()),
        };

        // Embed department scope into the token so API policies can filter without DB.
        if (user.DepartmentId.HasValue)
            claims.Add(new Claim("department_id", user.DepartmentId.Value.ToString()));

        if (user.InstitutionType.HasValue)
            claims.Add(new Claim("institutionType", ((int)user.InstitutionType.Value).ToString()));

        if (user.TenantId.HasValue)
            claims.Add(new Claim("tenant_id", user.TenantId.Value.ToString()));

        if (user.CampusId.HasValue)
            claims.Add(new Claim("campus_id", user.CampusId.Value.ToString()));

        // Student-only endpoints expect this claim. Resolve it at login so runtime
        // authorization does not depend on ad-hoc claim injection.
        var studentProfileId = _db.StudentProfiles
            .AsNoTracking()
            .Where(sp => sp.UserId == user.Id)
            .Select(sp => (Guid?)sp.Id)
            .FirstOrDefault();

        if (studentProfileId.HasValue)
            claims.Add(new Claim("studentProfileId", studentProfileId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a cryptographically random 64-byte refresh token (as Base64).
    /// The raw token is returned to the caller once; only the SHA-256 hash is stored.
    /// </summary>
    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Computes the SHA-256 hash of a raw refresh token string.
    /// Used both when storing a new session and when looking up a presented token.
    /// </summary>
    public string HashRefreshToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Returns the UTC expiry DateTime for a new refresh token session
    /// based on the configured RefreshTokenExpiryDays setting.
    /// </summary>
    public DateTime GetRefreshTokenExpiry()
        => DateTime.UtcNow.AddDays(_settings.RefreshTokenExpiryDays);
}
