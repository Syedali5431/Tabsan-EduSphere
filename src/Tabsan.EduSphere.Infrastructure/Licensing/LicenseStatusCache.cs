using Tabsan.EduSphere.Domain.Licensing;

namespace Tabsan.EduSphere.Infrastructure.Licensing;

/// <summary>
/// Process-wide snapshot of the last license validation result, used by request-path
/// enforcement so it does not have to re-verify the signed license on every request.
/// The expiry is evaluated against the clock on every read, so a license that passes its
/// expiry date is treated as expired immediately, without waiting for the next validation.
/// </summary>
public sealed class LicenseStatusCache
{
    /// <summary>How long a snapshot is trusted before the signed license is re-verified.</summary>
    public static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

    private readonly object _gate = new();
    private LicenseStatus _status = LicenseStatus.Invalid;
    private DateTime? _expiresAt;
    private DateTime _capturedAtUtc = DateTime.MinValue;

    public void Set(LicenseStatus status, DateTime? expiresAt)
    {
        lock (_gate)
        {
            _status = status;
            _expiresAt = expiresAt;
            _capturedAtUtc = DateTime.UtcNow;
        }
    }

    /// <summary>Forces the next read to re-validate.</summary>
    public void Invalidate()
    {
        lock (_gate) _capturedAtUtc = DateTime.MinValue;
    }

    /// <summary>
    /// Returns the cached status, or null when the snapshot is missing/stale and the caller
    /// should run a fresh validation.
    /// </summary>
    public LicenseStatus? TryGet()
    {
        lock (_gate)
        {
            if (DateTime.UtcNow - _capturedAtUtc > RefreshInterval)
                return null;

            if (_status == LicenseStatus.Active && _expiresAt.HasValue && DateTime.UtcNow > _expiresAt.Value)
                return LicenseStatus.Expired;

            return _status;
        }
    }
}
