using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Tabsan.EduSphere.IntegrationTests.Infrastructure;

/// <summary>
/// Generates signed JWT tokens for the four system roles using the same secret
/// and issuer/audience that the API validates against.
/// </summary>
public static class JwtTestHelper
{
    // Must match appsettings.json JwtSettings values used by the API.
    public const string SecretKey = "CHANGE_THIS_TO_A_LONG_RANDOM_KEY_IN_PRODUCTION_MIN_32_CHARS";
    public const string Issuer    = "https://localhost:7001";
    public const string Audience  = "tabsan-edusphere-clients";

    public static string GenerateToken(
        string role,
        string userId = "00000000-0000-0000-0000-000000000001",
        string email  = "test@tabsan.local",
        string? studentProfileId = null,
        int? institutionType = null,
        string? tenantId = null,
        string? campusId = null)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email,          email),
            new Claim(ClaimTypes.Role,           role),
        };

        if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(
                "studentProfileId",
                string.IsNullOrWhiteSpace(studentProfileId)
                    ? "00000000-0000-0000-0000-000000000123"
                    : studentProfileId));
        }

        if (institutionType.HasValue)
            claims.Add(new Claim("institutionType", institutionType.Value.ToString()));

        if (!string.IsNullOrWhiteSpace(tenantId))
            claims.Add(new Claim("tenant_id", tenantId));

        if (!string.IsNullOrWhiteSpace(campusId))
            claims.Add(new Claim("campus_id", campusId));

        var token = new JwtSecurityToken(
            issuer:             Issuer,
            audience:           Audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
