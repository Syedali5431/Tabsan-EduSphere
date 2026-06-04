using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Identity;

/// <summary>
/// Tracks an active refresh-token family for a user session.
/// Storing the hashed refresh token (never the plain token) allows the server
/// to validate renewals without keeping secrets in plain text, and lets us
/// revoke specific sessions (e.g. logout from one device) or all sessions.
/// </summary>
public class UserSession : BaseEntity
{
    /// <summary>FK linking this session back to the owning user.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Navigation property.</summary>
    public User User { get; private set; } = default!;

    /// <summary>
    /// SHA-256 hash of the refresh token value issued to the client.
    /// The raw token is sent to the client only and is never stored here.
    /// </summary>
    public string RefreshTokenHash { get; private set; } = default!;

    /// <summary>Browser / OS hint captured at login for audit and anomaly detection.</summary>
    public string? DeviceInfo { get; private set; }

    /// <summary>Client IP address captured at login. Stored for audit purposes.</summary>
    public string? IpAddress { get; private set; }

    /// <summary>UTC expiry of this session. After this point the refresh token is rejected.</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>UTC timestamp when the session was explicitly revoked (logout or admin action). Null if still valid.</summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>Phase 2: UTC timestamp of the last authenticated request made with this session. Null until first activity.</summary>
    public DateTime? LastActivityAt { get; private set; }

    /// <summary>Returns true when the session can still be used to obtain a new access token.</summary>
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;

    /// <summary>
    /// Phase 2: Returns true when the session is active AND has not exceeded the idle timeout.
    /// Falls back to CreatedAt when LastActivityAt is null (legacy sessions).
    /// </summary>
    public bool IsActiveWithinIdleTimeout(int idleTimeoutMinutes)
    {
        if (!IsActive) return false;
        var lastActivity = LastActivityAt ?? CreatedAt;
        return lastActivity.AddMinutes(idleTimeoutMinutes) > DateTime.UtcNow;
    }

    private UserSession() { }

    public UserSession(Guid userId, string refreshTokenHash, DateTime expiresAt,
                       string? deviceInfo = null, string? ipAddress = null)
    {
        UserId = userId;
        RefreshTokenHash = refreshTokenHash;
        ExpiresAt = expiresAt;
        DeviceInfo = deviceInfo;
        IpAddress = ipAddress;
    }

    /// <summary>Invalidates this session immediately (e.g. on explicit logout or token rotation).</summary>
    public void Revoke() => RevokedAt = DateTime.UtcNow;

    /// <summary>Phase 2: Updates LastActivityAt to the current UTC time. Called on each authenticated action.</summary>
    public void TouchActivity() => LastActivityAt = DateTime.UtcNow;

    /// <summary>
    /// Replaces the stored refresh token hash after a successful token rotation.
    /// Old hash is discarded; the client receives the new raw token.
    /// </summary>
    public void Rotate(string newHash, DateTime newExpiry)
    {
        RefreshTokenHash = newHash;
        ExpiresAt = newExpiry;
    }
}
