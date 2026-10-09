using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Licensing;

/// <summary>
/// Stores the currently activated license state on the application side.
/// This table holds the validated result of reading the signed license file plus the
/// signed file itself (<see cref="LicenseBlob"/>), so every later validation re-derives
/// the expiry from the RSA-signed payload instead of trusting the editable columns.
///
/// There is always exactly ONE row in this table (the active license).
/// </summary>
public class LicenseState : BaseEntity
{
    /// <summary>
    /// SHA-256 hash of the license file content at the time of last successful validation.
    /// Used on subsequent startups to detect if the file has been swapped or tampered with.
    /// </summary>
    public string LicenseHash { get; private set; } = default!;

    /// <summary>The decoded license type extracted from the verified payload.</summary>
    public LicenseType LicenseType { get; private set; }

    /// <summary>Current computed status, updated by the LicenseValidationService on each check.</summary>
    public LicenseStatus Status { get; private set; }

    /// <summary>UTC timestamp when the license was first activated on this installation.</summary>
    public DateTime ActivatedAt { get; private set; }

    /// <summary>
    /// Expiry extracted from the signed license payload. This is a fixed calendar date set
    /// when the license was issued; activating the same file on another system yields the
    /// same value. Null for Permanent licenses — they never expire.
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    // ── P2-S1-01 / P2-S2-01: Concurrency limit ──────────────────────────────

    /// <summary>
    /// Maximum number of concurrent active sessions allowed.
    /// A value of 0 means unlimited (All Users mode — P2-S2-01).
    /// SuperAdmin is always exempt regardless of this value (P2-S1-02).
    /// </summary>
    public int MaxUsers { get; private set; }

    // ── P2-S3-01 / P2-S3-02: Domain binding ─────────────────────────────────

    /// <summary>
    /// The HTTP host (domain) on which this license was first activated.
    /// Null until first activation. On subsequent activations, the incoming
    /// request host must match this value to prevent reuse across deployments.
    /// </summary>
    public string? ActivatedDomain { get; private set; }

    // ── Tamper protection ───────────────────────────────────────────────────

    /// <summary>
    /// The original encrypted + RSA-signed .tablic file. Re-verified on every validation;
    /// expiry, type and user limit are always re-read from it, so editing the columns above
    /// in the database has no lasting effect.
    /// </summary>
    public byte[]? LicenseBlob { get; private set; }

    /// <summary>HMAC over the stored state; a mismatch means the row was edited outside the app.</summary>
    public string? IntegritySeal { get; private set; }

    /// <summary>
    /// Highest UTC time observed by a validation. Expiry is evaluated against
    /// max(now, LastValidatedAt) so winding the system clock back cannot revive a license.
    /// </summary>
    public DateTime? LastValidatedAt { get; private set; }

    private LicenseState() { }

    /// <summary>Creates the initial license state record after a successful upload and validation.</summary>
    public LicenseState(string licenseHash, LicenseType licenseType, DateTime? expiresAt,
                        int maxUsers = 0, string? activatedDomain = null, byte[]? licenseBlob = null)
    {
        LicenseHash = licenseHash;
        LicenseType = licenseType;
        Status = LicenseStatus.Active;
        ActivatedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
        MaxUsers = maxUsers;
        ActivatedDomain = activatedDomain;
        LicenseBlob = licenseBlob;
        LastValidatedAt = ActivatedAt;
    }

    /// <summary>
    /// Re-evaluates the status based on the current UTC time.
    /// Called during startup validation, daily background checks, and Super Admin login.
    /// </summary>
    public void RefreshStatus() => RefreshStatus(DateTime.UtcNow);

    /// <summary>Re-evaluates the status against the supplied (clock-rollback-safe) point in time.</summary>
    public void RefreshStatus(DateTime effectiveUtcNow)
    {
        if (ExpiresAt.HasValue && effectiveUtcNow > ExpiresAt.Value)
            Status = LicenseStatus.Expired;
        else
            Status = LicenseStatus.Active;

        Touch();
    }

    /// <summary>Forces the status to Invalid when the signature check fails.</summary>
    public void MarkInvalid()
    {
        Status = LicenseStatus.Invalid;
        Touch();
    }

    /// <summary>
    /// Restores the license-derived values from the verified signed payload, discarding any
    /// out-of-band edits to the corresponding columns.
    /// </summary>
    public void RestoreFromSignedPayload(LicenseType licenseType, DateTime? expiresAt, int maxUsers)
    {
        LicenseType = licenseType;
        ExpiresAt = expiresAt;
        MaxUsers = maxUsers;
    }

    /// <summary>Advances the observed-time high-water mark (never moves it backwards).</summary>
    public void RecordValidatedAt(DateTime utcNow)
    {
        if (!LastValidatedAt.HasValue || utcNow > LastValidatedAt.Value)
            LastValidatedAt = utcNow;
    }

    /// <summary>Stores the integrity seal computed by the infrastructure layer.</summary>
    public void ApplySeal(string seal) => IntegritySeal = seal;

    /// <summary>
    /// Replaces the current license record with a newly uploaded and validated license.
    /// Used when a Super Admin uploads a renewal or upgraded license file.
    /// </summary>
    public void Replace(string newHash, LicenseType newType, DateTime? newExpiry,
                        int maxUsers = 0, string? activatedDomain = null, byte[]? licenseBlob = null)
    {
        LicenseHash = newHash;
        LicenseType = newType;
        ExpiresAt = newExpiry;
        Status = LicenseStatus.Active;
        ActivatedAt = DateTime.UtcNow;
        MaxUsers = maxUsers;
        ActivatedDomain = activatedDomain;
        LicenseBlob = licenseBlob;
        if (!LastValidatedAt.HasValue || ActivatedAt > LastValidatedAt.Value)
            LastValidatedAt = ActivatedAt;
        Touch();
    }
}
