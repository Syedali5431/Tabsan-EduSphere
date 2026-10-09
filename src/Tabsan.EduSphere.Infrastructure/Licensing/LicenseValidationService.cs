// ╔══════════════════════════════════════════════════════════════════╗
// ║  REPLACED IN PHASE 7 — now handles binary .tablic format        ║
// ╚══════════════════════════════════════════════════════════════════╝
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Modules;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Licensing;
using Tabsan.EduSphere.Infrastructure.Modules;

namespace Tabsan.EduSphere.Infrastructure.Licensing;

/// <summary>
/// Validates binary .tablic license files and manages the stored <see cref="LicenseState"/>.
///
/// .tablic binary layout
/// ─────────────────────
/// Offset   0 –   6 : Magic "TABLIC\x01" (7 bytes)
/// Offset   7 – 262 : RSA-2048 PKCS#1 v1.5 signature of SHA-256(IV + ciphertext) (256 bytes)
/// Offset 263 – 278 : AES-256-CBC IV (16 bytes)
/// Offset 279+      : AES-256-CBC encrypted JSON payload
///
/// Activation rules
/// ────────────────
/// 1. Magic header must match.
/// 2. RSA signature over SHA-256(IV + ciphertext) must verify with the embedded public key.
/// 3. AES-256-CBC decryption must succeed.
/// 4. verificationKeyHash must not already exist in the ConsumedVerificationKeys table.
/// </summary>
public class LicenseValidationService
{
    private readonly ILicenseRepository _licenseRepo;
    private readonly IModuleRepository _moduleRepo;
    private readonly ModuleEntitlementResolver _moduleEntitlementResolver;
    private readonly IInstitutionPolicyService _institutionPolicy;
    private readonly ILogger<LicenseValidationService> _logger;
    private readonly LicenseStatusCache _statusCache;

    private static readonly byte[] _magic = "TABLIC\x01"u8.ToArray();
    private const int SignatureOffset  = 7;
    private const int SignatureLength  = 256;
    private const int IvOffset         = SignatureOffset + SignatureLength; // 263
    private const int IvLength         = 16;
    private const int CiphertextOffset = IvOffset + IvLength;              // 279
    private const int MinFileLength    = CiphertextOffset + 16;

    public LicenseValidationService(
        ILicenseRepository licenseRepo,
        IModuleRepository moduleRepo,
        ModuleEntitlementResolver moduleEntitlementResolver,
        IInstitutionPolicyService institutionPolicy,
        ILogger<LicenseValidationService> logger,
        LicenseStatusCache statusCache)
    {
        _licenseRepo      = licenseRepo;
        _moduleRepo       = moduleRepo;
        _moduleEntitlementResolver = moduleEntitlementResolver;
        _institutionPolicy = institutionPolicy;
        _logger           = logger;
        _statusCache      = statusCache;
    }

    /// <summary>
    /// Reads the .tablic file at <paramref name="licenseFilePath"/>, verifies the RSA
    /// signature, decrypts the AES payload, checks the VerificationKey has not been
    /// consumed, and then creates or replaces the <see cref="LicenseState"/> record.
    /// <para>
    /// P2-S3-02 / P2-S3-03: When <paramref name="requestDomain"/> is supplied the method
    /// additionally enforces domain binding:
    /// <list type="bullet">
    ///   <item>If the payload contains <c>AllowedDomain</c>, it must match <paramref name="requestDomain"/>.</item>
    ///   <item>An existing LicenseState whose <c>ActivatedDomain</c> is already set must originate from the same domain.</item>
    /// </list>
    /// </para>
    /// Returns true on success; false on any verification or format failure.
    /// </summary>
    public async Task<bool> ActivateFromFileAsync(string licenseFilePath,
                                                   string? requestDomain = null,
                                                   CancellationToken ct = default)
    {
        try
        {
            var fileBytes = await File.ReadAllBytesAsync(licenseFilePath, ct);
            return await ActivateFromBytesAsync(fileBytes, requestDomain, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during license activation.");
            return false;
        }
    }

    /// <summary>
    /// Validates and activates a license from in-memory bytes.
    /// </summary>
    public async Task<bool> ActivateFromBytesAsync(byte[] fileBytes,
                                                    string? requestDomain = null,
                                                    CancellationToken ct = default)
    {
        try
        {
            // Final-Touches Phase 28 Stage 28.3 — allow provider-backed upload flows to avoid temp-path coupling.
            var payload = TryReadSignedPayload(fileBytes);
            if (payload is null)
                return false;

            // ── P2-S3-03: License-embedded domain restriction ──────────────────
            // If the license issuer locked the license to a specific domain, enforce it.
            if (!string.IsNullOrWhiteSpace(payload.AllowedDomain) &&
                !string.IsNullOrWhiteSpace(requestDomain) &&
                !string.Equals(payload.AllowedDomain, requestDomain, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "License AllowedDomain '{Allowed}' does not match request domain '{Request}'. Activation rejected.",
                    payload.AllowedDomain, requestDomain);
                return false;
            }

            var fileHash = ComputeFileHash(fileBytes);
            var existing = await _licenseRepo.GetCurrentAsync(ct);

            // 5. VerificationKey replay guard.
            // Re-uploading the exact file that is already active is allowed so an installation can
            // re-bind its stored signed copy (e.g. after upgrading from a build that did not keep it).
            var isSameActiveFile = existing is not null &&
                                   string.Equals(existing.LicenseHash, fileHash, StringComparison.OrdinalIgnoreCase);
            if (!isSameActiveFile &&
                await _licenseRepo.IsVerificationKeyConsumedAsync(payload.VerificationKeyHash, ct))
            {
                _logger.LogWarning(
                    "VerificationKey '{Hash}' already consumed. Activation rejected.",
                    payload.VerificationKeyHash);
                return false;
            }

            // 6. Apply license state
            var licenseType = Enum.Parse<LicenseType>(payload.LicenseType, ignoreCase: true);

            // ── P2-S3-02: Capture activation domain ────────────────────────────
            // Use the request domain if available, fall back to the payload-embedded domain.
            var activatedDomain = requestDomain ?? payload.AllowedDomain;

            // The expiry is a fixed date carried inside the signed payload — it never depends on
            // when or where the file is activated, so the same file yields the same expiry everywhere.
            var effectiveExpiry = ResolveEffectiveExpiry(payload);
            LicenseState state;
            if (existing is null)
            {
                state = new LicenseState(fileHash, licenseType, effectiveExpiry,
                                         payload.MaxUsers, activatedDomain, fileBytes);
                await _licenseRepo.AddAsync(state, ct);
            }
            else
            {
                state = existing;
                state.Replace(fileHash, licenseType, effectiveExpiry,
                              payload.MaxUsers, activatedDomain, fileBytes);
                _licenseRepo.Update(state);
            }

            state.RefreshStatus(state.LastValidatedAt ?? DateTime.UtcNow);
            state.ApplySeal(ComputeSeal(state));

            if (!isSameActiveFile)
            {
                await _licenseRepo.AddConsumedKeyAsync(
                    new ConsumedVerificationKey(payload.VerificationKeyHash), ct);
            }
            await _licenseRepo.SaveChangesAsync(ct);
            _statusCache.Set(state.Status, state.ExpiresAt);

            await _institutionPolicy.SavePolicyAsync(new SaveInstitutionPolicyCommand(
                payload.IncludeSchool,
                payload.IncludeCollege,
                payload.IncludeUniversity), ct);

            // Keep module statuses aligned with what the activated license allows.
            // Optional modules are seeded inactive and must be explicitly activated by license scope.
            await SyncModuleStatusesForPolicyAsync(new InstitutionPolicySnapshot(
                payload.IncludeSchool,
                payload.IncludeCollege,
                payload.IncludeUniversity), ct);

            _logger.LogInformation(
                "License activated. Type={Type}, ExpiresAt={Expiry}, MaxUsers={MaxUsers}, Domain={Domain}",
                licenseType, effectiveExpiry?.ToString("O") ?? "never", payload.MaxUsers, activatedDomain ?? "any");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during license activation.");
            return false;
        }
    }

    /// <summary>
    /// Checks the stored <see cref="LicenseState"/> and refreshes its status.
    /// Called on startup, Super Admin login, and daily by the background job.
    /// <para>
    /// The stored signed license file is re-verified every time and the expiry, type and user
    /// limit are re-derived from it, so editing those columns directly in the database is undone.
    /// The integrity seal detects edits to the remaining columns, and the observed-time
    /// high-water mark prevents reviving an expired license by moving the system clock back.
    /// </para>
    /// </summary>
    public async Task<LicenseStatus> ValidateCurrentAsync(CancellationToken ct = default)
    {
        var state = await _licenseRepo.GetCurrentAsync(ct);

        if (state is null)
        {
            _logger.LogWarning("No license found. Portal will operate in read-only mode.");
            _statusCache.Set(LicenseStatus.Invalid, null);
            return LicenseStatus.Invalid;
        }

        var tampered = false;
        var payload = state.LicenseBlob is { Length: > 0 } blob ? TryReadSignedPayload(blob) : null;

        if (payload is null)
        {
            _logger.LogWarning(state.LicenseBlob is null
                ? "Stored license has no signed license file attached. Re-upload the .tablic file to re-activate."
                : "Stored signed license file failed verification. License marked invalid.");
            tampered = true;
        }
        else if (!string.Equals(ComputeFileHash(state.LicenseBlob!), state.LicenseHash, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Stored license hash does not match the stored signed license file. License marked invalid.");
            tampered = true;
        }
        else if (state.IntegritySeal is null || !SealMatches(state.IntegritySeal, ComputeSeal(state)))
        {
            // Edits limited to the values that come from the signed file (expiry, type, user limit) are
            // simply reverted below. Any other edit (domain, activation / validation timestamps, or a
            // removed seal) cannot be reconciled and invalidates the license.
            var sealWithSignedValues = ComputeSealFor(state.Id, state.LicenseHash,
                Enum.Parse<LicenseType>(payload.LicenseType, ignoreCase: true), ResolveEffectiveExpiry(payload),
                payload.MaxUsers, state.ActivatedDomain, state.ActivatedAt, state.LastValidatedAt);

            tampered = state.IntegritySeal is null || !SealMatches(state.IntegritySeal, sealWithSignedValues);
            _logger.LogWarning(tampered
                ? "License record integrity seal mismatch — the license row was modified outside the application. License marked invalid."
                : "License record values were modified outside the application. Restoring values from the signed license file.");
        }

        if (tampered)
        {
            state.MarkInvalid();
        }
        else
        {
            var now = DateTime.UtcNow;
            var signedExpiry = ResolveEffectiveExpiry(payload!);
            var signedType   = Enum.Parse<LicenseType>(payload!.LicenseType, ignoreCase: true);
            state.RestoreFromSignedPayload(signedType, signedExpiry, payload.MaxUsers);

            // Clock-rollback guard: evaluate against the latest time this installation has ever seen.
            if (state.LastValidatedAt.HasValue && now < state.LastValidatedAt.Value - ClockRollbackTolerance)
            {
                _logger.LogWarning(
                    "System clock ({Now:O}) is behind the last validated time ({Last:O}). Evaluating expiry against the last validated time.",
                    now, state.LastValidatedAt.Value);
            }
            state.RecordValidatedAt(now);
            state.RefreshStatus(state.LastValidatedAt ?? now);
        }

        state.ApplySeal(ComputeSeal(state));
        _licenseRepo.Update(state);
        await _licenseRepo.SaveChangesAsync(ct);
        _statusCache.Set(state.Status, state.ExpiresAt);

        if (state.Status == LicenseStatus.Active)
        {
            var policy = await _institutionPolicy.GetPolicyAsync(ct);
            await SyncModuleStatusesForPolicyAsync(policy, ct);
        }

        _logger.LogInformation("License validation complete. Status={Status}", state.Status);
        return state.Status;
    }

    private static readonly TimeSpan ClockRollbackTolerance = TimeSpan.FromHours(24);

    /// <summary>
    /// Verifies the magic header, RSA signature, AES decryption and vendor key fingerprint of a
    /// .tablic file and returns its payload, or null when any check fails.
    /// </summary>
    private TablicPayload? TryReadSignedPayload(byte[] fileBytes)
    {
        // 1. Magic header
        if (fileBytes.Length < MinFileLength || !fileBytes[..7].SequenceEqual(_magic))
        {
            _logger.LogWarning("License file has invalid magic header or is too short.");
            return null;
        }

        // 2. Extract components
        var signature  = fileBytes[SignatureOffset..IvOffset];
        var iv         = fileBytes[IvOffset..CiphertextOffset];
        var ciphertext = fileBytes[CiphertextOffset..];

        // 3. Verify RSA signature over SHA-256(IV + ciphertext)
        var signedData = new byte[iv.Length + ciphertext.Length];
        iv.CopyTo(signedData, 0);
        ciphertext.CopyTo(signedData, iv.Length);

        if (!VerifyRsaSignature(signedData, signature))
        {
            _logger.LogWarning("License file RSA signature verification failed. File may be tampered.");
            return null;
        }

        // 4. Decrypt payload
        byte[] plaintext;
        try { plaintext = DecryptAes(ciphertext, iv); }
        catch (CryptographicException)
        {
            _logger.LogWarning("License file AES decryption failed.");
            return null;
        }

        TablicPayload? payload;
        try { payload = JsonSerializer.Deserialize<TablicPayload>(Encoding.UTF8.GetString(plaintext), _jsonOptions); }
        catch (JsonException)
        {
            _logger.LogWarning("License payload is not valid JSON.");
            return null;
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.LicenseType) ||
            string.IsNullOrWhiteSpace(payload.VerificationKeyHash) ||
            !Enum.TryParse<LicenseType>(payload.LicenseType, ignoreCase: true, out _))
        {
            _logger.LogWarning("License payload is missing required fields.");
            return null;
        }

        // Optional verification-key fingerprint binding (Medyx-style hardening):
        // if the field exists in payload, it must match the app's embedded public key fingerprint.
        if (!string.IsNullOrWhiteSpace(payload.VerificationKey))
        {
            var expectedVerificationKey = ComputeVerificationKeyFromPublicKey();
            if (!string.Equals(payload.VerificationKey, expectedVerificationKey, StringComparison.Ordinal))
            {
                _logger.LogWarning("License VerificationKey mismatch. Expected={Expected}, Received={Received}",
                    expectedVerificationKey, payload.VerificationKey);
                return null;
            }
        }

        return payload;
    }

    private static bool SealMatches(string stored, string expected)
        => CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(stored), Encoding.ASCII.GetBytes(expected));

    private static string ComputeSeal(LicenseState s)
        => ComputeSealFor(s.Id, s.LicenseHash, s.LicenseType, s.ExpiresAt, s.MaxUsers,
                          s.ActivatedDomain, s.ActivatedAt, s.LastValidatedAt);

    private static string ComputeSealFor(Guid id, string licenseHash, LicenseType type, DateTime? expiresAt,
                                         int maxUsers, string? domain, DateTime activatedAt, DateTime? lastValidatedAt)
    {
        var material = string.Join('|',
            id.ToString("N"),
            licenseHash.ToLowerInvariant(),
            type.ToString(),
            expiresAt?.Ticks.ToString() ?? "-",
            maxUsers.ToString(),
            domain?.ToLowerInvariant() ?? "-",
            activatedAt.Ticks.ToString(),
            lastValidatedAt?.Ticks.ToString() ?? "-");

        return Convert.ToHexString(HMACSHA256.HashData(_sealKey.Value, Encoding.UTF8.GetBytes(material)))
                      .ToLowerInvariant();
    }

    private static readonly Lazy<byte[]> _sealKey = new(() =>
        SHA256.HashData(Encoding.UTF8.GetBytes("tabsan-license-state-seal:" + EmbeddedKeys.AesKeyBase64)));

    private async Task SyncModuleStatusesForPolicyAsync(InstitutionPolicySnapshot policy, CancellationToken ct)
    {
        var changed = false;

        foreach (var descriptor in ModuleRegistry.All())
        {
            var status = await _moduleRepo.GetStatusByKeyAsync(descriptor.Key, ct);
            if (status is null)
            {
                continue;
            }

            var isAllowedForAnyLicensedType = descriptor.AllowedTypes is null || descriptor.AllowedTypes.Any(policy.IsEnabled);
            var shouldBeActive = status.Module.IsMandatory || isAllowedForAnyLicensedType;

            if (!shouldBeActive || status.IsActive)
            {
                continue;
            }

            status.Activate(Guid.Empty, source: "license");
            _moduleRepo.UpdateStatus(status);
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        await _moduleRepo.SaveChangesAsync(ct);
        _moduleEntitlementResolver.InvalidateAll();
    }

    /// <summary>
    /// Returns the raw <see cref="LicenseState"/> without refreshing it.
    /// Used by the Super Admin / Admin license-detail view.
    /// </summary>
    public Task<LicenseState?> GetCurrentStateAsync(CancellationToken ct = default)
        => _licenseRepo.GetCurrentAsync(ct);

    // ── Crypto helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies the RSA-PKCS#1 v1.5 SHA-256 signature over <paramref name="data"/>
    /// using the compile-time embedded public key from <see cref="EmbeddedKeys"/>.
    /// </summary>
    private static bool VerifyRsaSignature(byte[] data, byte[] signature)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(EmbeddedKeys.RsaPublicKeyPem);
            var hash = SHA256.HashData(data);
            return rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch { return false; }
    }

    /// <summary>
    /// Decrypts AES-256-CBC <paramref name="ciphertext"/> using the embedded AES key
    /// and the supplied <paramref name="iv"/>.
    /// </summary>
    private static byte[] DecryptAes(byte[] ciphertext, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key     = Convert.FromBase64String(EmbeddedKeys.AesKeyBase64);
        aes.IV      = iv;
        aes.Mode    = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using var dec = aes.CreateDecryptor();
        return dec.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }

    /// <summary>Computes a SHA-256 hex hash of the raw file bytes for tamper detection.</summary>
    private static string ComputeFileHash(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static string ComputeVerificationKeyFromPublicKey()
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(EmbeddedKeys.RsaPublicKeyPem);
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

    private static DateTime? ResolveEffectiveExpiry(TablicPayload payload)
    {
        var expectedExpiry = ComputeExpectedExpiryFromIssuedAt(payload.ExpiryType, payload.IssuedAt);
        if (!expectedExpiry.HasValue)
        {
            return payload.ExpiresAt;
        }

        if (!payload.ExpiresAt.HasValue)
        {
            return expectedExpiry;
        }

        var payloadExpiryDate = ToInclusiveEndOfDay(payload.ExpiresAt.Value.Date);

        return payloadExpiryDate <= expectedExpiry.Value
            ? payloadExpiryDate
            : expectedExpiry;
    }

    private static DateTime? ComputeExpectedExpiryFromIssuedAt(string? expiryTypeRaw, DateTime issuedAt)
    {
        if (string.IsNullOrWhiteSpace(expiryTypeRaw))
        {
            return null;
        }

        if (!Enum.TryParse<TablicExpiryType>(expiryTypeRaw, ignoreCase: true, out var expiryType))
        {
            return null;
        }

        var issuedDate = issuedAt.Date;

        return expiryType switch
        {
            TablicExpiryType.OneMonth => ToInclusiveEndOfDay(issuedDate.AddMonths(1)),
            TablicExpiryType.OneYear => ToInclusiveEndOfDay(issuedDate.AddYears(1)),
            TablicExpiryType.TwoYears => ToInclusiveEndOfDay(issuedDate.AddYears(2)),
            TablicExpiryType.ThreeYears => ToInclusiveEndOfDay(issuedDate.AddYears(3)),
            TablicExpiryType.Permanent => null,
            _ => null
        };
    }

    private static DateTime ToInclusiveEndOfDay(DateTime date)
        => DateTime.SpecifyKind(date.Date.AddDays(1).AddTicks(-1), DateTimeKind.Unspecified);

    // ── Internal DTO ──────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Maps directly to the decrypted JSON payload inside a .tablic file.</summary>
    private sealed class TablicPayload
    {
        public string LicenseType { get; set; } = default!;
        public string? ExpiryType { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string VerificationKeyHash { get; set; } = default!;
        public Guid LicenseId { get; set; }
        public string? Nonce { get; set; }
        public string? VerificationKey { get; set; }
        public bool IncludeSchool { get; set; }
        public bool IncludeCollege { get; set; }
        public bool IncludeUniversity { get; set; } = true;

        // ── P2-S1-01 / P2-S2-01: Concurrent user limit ──────────────────────
        /// <summary>
        /// Maximum concurrent active sessions allowed. 0 = unlimited (All Users mode).
        /// Serialised as "MaxUsers" in the JSON payload by the KeyGen tool.
        /// </summary>
        public int MaxUsers { get; set; }

        // ── P2-S3-03: Domain binding embedded in the license ─────────────────
        /// <summary>
        /// Optional domain restriction set by the license issuer.
        /// When present, the activation request host MUST match this value.
        /// Null means the license is not domain-locked at issuance time.
        /// </summary>
        public string? AllowedDomain { get; set; }
    }

    private enum TablicExpiryType
    {
        OneMonth = 1,
        OneYear = 2,
        TwoYears = 3,
        ThreeYears = 4,
        Permanent = 5,
        SpecificDate = 6
    }
}
