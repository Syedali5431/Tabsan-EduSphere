using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Tabsan.EduSphere.Application.Lms.Scorm;

namespace Tabsan.EduSphere.API.Services;

/// <summary>
/// Plan M13 — SCORM play links: the attempt, package and learner sealed with ASP.NET data protection and an expiry. The
/// result is base64url, so it can be a path segment (relative links inside the package keep working under it).
/// </summary>
public sealed class ScormPlayTokens : IScormPlayTokens
{
    private readonly ITimeLimitedDataProtector _protector;

    public ScormPlayTokens(IDataProtectionProvider provider)
        => _protector = provider.CreateProtector("Tabsan.EduSphere.Scorm.Play.v1").ToTimeLimitedDataProtector();

    public string Issue(ScormPlayClaims claims, TimeSpan lifetime)
        => _protector.Protect($"{claims.AttemptId:N}|{claims.PackageId:N}|{claims.StudentProfileId:N}|{(claims.Review ? 1 : 0)}", lifetime);

    public ScormPlayClaims? Read(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 2048) return null;
        try
        {
            var parts = _protector.Unprotect(token).Split('|');
            return parts.Length == 4 && Guid.TryParse(parts[0], out var attempt) && Guid.TryParse(parts[1], out var package) && Guid.TryParse(parts[2], out var student)
                ? new ScormPlayClaims(attempt, package, student, parts[3] == "1")
                : null;
        }
        catch (CryptographicException) { return null; }
        catch (FormatException) { return null; }
    }
}
