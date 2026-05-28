using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Tabsan.Lic.Data;
using Tabsan.Lic.Models;

namespace Tabsan.Lic.Services;

/// <summary>
/// Manages the lifecycle of VerificationKeys: generation, listing, and CSV export.
/// All data is persisted in the local SQLite database (tabsan_lic.db).
/// </summary>
public class KeyService
{
    private readonly LicDb _db;

    public KeyService(LicDb db) => _db = db;

    /// <summary>
    /// Generates a new random VerificationKey, hashes it with SHA-256, stores the hash
    /// in the database, and returns the raw token (shown to operator once).
    /// </summary>
    /// <param name="expiry">Expiry category for the new key.</param>
    /// <param name="label">Optional human-readable note (customer name, etc.).</param>
    /// <returns>
    /// A tuple of the newly created <see cref="IssuedKey"/> record and the raw
    /// VerificationKey token string that should be presented to the operator.
    /// </returns>
    public async Task<(IssuedKey Record, string RawToken)> GenerateAsync(
        ExpiryType expiry, string? label = null)
    {
        var rawToken = GenerateRawToken();
        var hash     = HashToken(rawToken);
        // Use local calendar date (no timezone component) so validity matches operator expectations.
        var issuedAt = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);

        DateTime? expiresAt = expiry switch
        {
            ExpiryType.OneMonth   => ToInclusiveEndOfDay(issuedAt.AddMonths(1)),
            ExpiryType.OneYear    => ToInclusiveEndOfDay(issuedAt.AddYears(1)),
            ExpiryType.TwoYears   => ToInclusiveEndOfDay(issuedAt.AddYears(2)),
            ExpiryType.ThreeYears => ToInclusiveEndOfDay(issuedAt.AddYears(3)),
            ExpiryType.Permanent  => null,
            _                     => throw new ArgumentOutOfRangeException(nameof(expiry))
        };

        var record = new IssuedKey
        {
            KeyId               = Guid.NewGuid(),
            VerificationKeyHash = hash,
            ExpiryType          = expiry,
            IssuedAt            = issuedAt,
            ExpiresAt           = expiresAt,
            IsLicenseGenerated  = false,
            Label               = label
        };

        _db.IssuedKeys.Add(record);
        await _db.SaveChangesAsync();
        return (record, rawToken);
    }

    /// <summary>
    /// Generates <paramref name="count"/> VerificationKeys at once with the same expiry.
    /// Returns a list of (record, rawToken) tuples.
    /// </summary>
    public async Task<IReadOnlyList<(IssuedKey Record, string RawToken)>> GenerateBulkAsync(
        int count, ExpiryType expiry, string? labelPrefix = null)
    {
        var results = new List<(IssuedKey, string)>(count);
        for (var i = 1; i <= count; i++)
        {
            var label = labelPrefix is not null ? $"{labelPrefix}-{i}" : null;
            results.Add(await GenerateAsync(expiry, label));
        }
        return results;
    }

    /// <summary>Returns all issued keys ordered by IssuedAt descending.</summary>
    public Task<List<IssuedKey>> ListAllAsync()
        => _db.IssuedKeys.OrderByDescending(k => k.IssuedAt).ToListAsync();

    /// <summary>Returns the IssuedKey by its auto-increment Id, or null if not found.</summary>
    public Task<IssuedKey?> GetByIdAsync(int id)
        => _db.IssuedKeys.FirstOrDefaultAsync(k => k.Id == id);

    /// <summary>Marks the key record as having a .tablic file generated and saves.</summary>
    public async Task MarkLicenseGeneratedAsync(IssuedKey key)
    {
        key.IsLicenseGenerated  = true;
        key.LicenseGeneratedAt  = DateTime.UtcNow;
        _db.IssuedKeys.Update(key);
        await _db.SaveChangesAsync();
    }

    // P3-S1-01: Persist Phase 2 constraint values (MaxUsers, AllowedDomain) back to DB
    /// <summary>
    /// Persists updated institution scope, MaxUsers, and AllowedDomain values for the given key.
    /// Call this before generating the .tablic so the DB record matches the payload.
    /// </summary>
    public async Task UpdateConstraintsAsync(IssuedKey key)
    {
        _db.IssuedKeys.Update(key);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Exports all issued keys to a CSV string suitable for writing to a file.
    /// Columns: Id, KeyId, ExpiryType, IssuedAt, ExpiresAt, IncludeSchool, IncludeCollege, IncludeUniversity, IsLicenseGenerated, MaxUsers, AllowedDomain, Label.
    /// </summary>
    public async Task<string> ExportCsvAsync()
    {
        var keys = await ListAllAsync();
        var sb   = new System.Text.StringBuilder();
        // P3-S1-01: Include institution scope, MaxUsers, and AllowedDomain in export
        sb.AppendLine("Id,KeyId,ExpiryType,IssuedAt,ExpiresAt,IncludeSchool,IncludeCollege,IncludeUniversity,IsLicenseGenerated,MaxUsers,AllowedDomain,Label");
        foreach (var k in keys)
        {
            sb.AppendLine(
                $"{k.Id},{k.KeyId},{k.ExpiryType},{k.IssuedAt:O}," +
                $"{k.ExpiresAt?.ToString("O") ?? ""}," +
                $"{k.IncludeSchool}," +
                $"{k.IncludeCollege}," +
                $"{k.IncludeUniversity}," +
                $"{k.IsLicenseGenerated}," +
                $"{k.MaxUsers},{EscapeCsv(k.AllowedDomain)},{EscapeCsv(k.Label)}");
        }
        return sb.ToString();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a cryptographically random token string (base64url, 32 bytes of entropy).
    /// </summary>
    private static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
                      .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    /// <summary>Returns the lowercase hex SHA-256 hash of the token string.</summary>
    internal static string HashToken(string token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static DateTime ToInclusiveEndOfDay(DateTime date)
        => DateTime.SpecifyKind(date.Date.AddDays(1).AddTicks(-1), DateTimeKind.Unspecified);

    private static string EscapeCsv(string? value)
        => value is null ? "" : $"\"{value.Replace("\"", "\"\"")}\"";
}
