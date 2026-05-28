using System.Text.Json;
using Tabsan.Lic.Crypto;
using Tabsan.Lic.Models;
using System.Security.Cryptography;
using System.Text;

namespace Tabsan.Lic.Services;

/// <summary>
/// Builds binary .tablic license files from an existing <see cref="IssuedKey"/> record.
/// The file is AES-256-CBC encrypted and RSA-2048 signed.
/// </summary>
public class LicenseBuilder
{
    private readonly KeyService _keyService;

    public LicenseBuilder(KeyService keyService) => _keyService = keyService;

    /// <summary>
    /// Generates a .tablic binary file for the given <paramref name="key"/> and writes it
    /// to <paramref name="outputPath"/>.
    /// </summary>
    /// <param name="key">The IssuedKey record to embed in the license payload.</param>
    /// <param name="outputPath">Destination file path (should end in .tablic).</param>
    public async Task BuildAsync(IssuedKey key, string outputPath)
    {
        var verificationKey = ComputeVerificationKey();
        var payload = new TablicPayload
        {
            LicenseType         = key.ExpiryType == ExpiryType.Permanent ? "Permanent" : "Yearly",
            ExpiryType          = key.ExpiryType.ToString(),
            IssuedAt            = key.IssuedAt,
            ExpiresAt           = key.ExpiresAt,
            VerificationKeyHash = key.VerificationKeyHash,
            LicenseId           = Guid.NewGuid(),
            Nonce               = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant(),
            VerificationKey     = verificationKey,
            IncludeSchool       = key.IncludeSchool,
            IncludeCollege      = key.IncludeCollege,
            IncludeUniversity    = key.IncludeUniversity,
            // P3-S1-01: Embed Phase 2 constraint fields in the payload
            MaxUsers            = key.MaxUsers,
            AllowedDomain       = key.AllowedDomain
        };

        var json      = JsonSerializer.Serialize(payload, TablicJsonOptions.Default);
        var fileBytes = LicCrypto.BuildTablicFile(json);

        await File.WriteAllBytesAsync(outputPath, fileBytes);
        await _keyService.MarkLicenseGeneratedAsync(key);
    }

    internal static string ComputeVerificationKey()
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(EmbeddedKeys.RsaPrivateKeyPem);
        var p = rsa.ExportParameters(false);

        var modulusHex = Convert.ToHexString(p.Modulus ?? []);
        var exponentHex = Convert.ToHexString(p.Exponent ?? []);
        var material = $"{modulusHex}:{exponentHex}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));

        return Convert.ToBase64String(hash)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    // ── Internal payload DTO ──────────────────────────────────────────────────

    private sealed class TablicPayload
    {
        public string LicenseType { get; init; } = default!;
        public string ExpiryType { get; init; } = default!;
        public DateTime IssuedAt { get; init; }
        public DateTime? ExpiresAt { get; init; }
        public string VerificationKeyHash { get; init; } = default!;
        public Guid LicenseId { get; init; }
        public string Nonce { get; init; } = default!;
        public string VerificationKey { get; init; } = default!;
        public bool IncludeSchool { get; init; }
        public bool IncludeCollege { get; init; }
        public bool IncludeUniversity { get; init; } = true;
        // P3-S1-01: Phase 2 constraint fields
        public int MaxUsers { get; init; }
        public string? AllowedDomain { get; init; }
    }

    private static class TablicJsonOptions
    {
        internal static readonly JsonSerializerOptions Default = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented        = false
        };
    }
}
