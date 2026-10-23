using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Notifications;

/// <summary>
/// Plan M4.3: a browser's Web Push subscription for one user on one device.
/// The endpoint is unique: when another user signs in on the same browser and subscribes,
/// the subscription moves to that user.
/// </summary>
public class PushSubscription : BaseEntity
{
    public Guid UserId { get; private set; }

    /// <summary>The push service URL the browser handed out (https only).</summary>
    public string Endpoint { get; private set; } = default!;

    /// <summary>The browser's P-256 public key (base64url, 65 bytes uncompressed).</summary>
    public string P256dh { get; private set; } = default!;

    /// <summary>The browser's authentication secret (base64url, 16 bytes).</summary>
    public string Auth { get; private set; } = default!;

    public string? DeviceLabel { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? LastSuccessAt { get; private set; }
    public DateTime? LastFailureAt { get; private set; }
    public int ConsecutiveFailures { get; private set; }

    private PushSubscription() { }

    public PushSubscription(Guid userId, string endpoint, string p256dh, string auth, string? deviceLabel)
    {
        Endpoint = endpoint.Trim();
        Assign(userId, p256dh, auth, deviceLabel);
    }

    /// <summary>Refreshes the keys (and owner) when the same browser subscribes again.</summary>
    public void Assign(Guid userId, string p256dh, string auth, string? deviceLabel)
    {
        UserId = userId;
        P256dh = p256dh.Trim();
        Auth = auth.Trim();
        DeviceLabel = string.IsNullOrWhiteSpace(deviceLabel) ? null : deviceLabel.Trim()[..Math.Min(deviceLabel.Trim().Length, 120)];
        IsActive = true;
        ConsecutiveFailures = 0;
        Touch();
    }

    public void RecordSuccess(DateTime now)
    {
        LastSuccessAt = now;
        ConsecutiveFailures = 0;
        Touch();
    }

    /// <summary>Counts a failure; after <paramref name="maxFailures"/> in a row the subscription is switched off.</summary>
    public void RecordFailure(DateTime now, int maxFailures)
    {
        LastFailureAt = now;
        ConsecutiveFailures++;
        if (ConsecutiveFailures >= maxFailures) IsActive = false;
        Touch();
    }

    /// <summary>The push service says the subscription no longer exists (404/410).</summary>
    public void Expire()
    {
        IsActive = false;
        Touch();
    }
}

/// <summary>Plan M4.3: a user's choice to receive (or not) push notifications for one notification type.</summary>
public class NotificationPreference : BaseEntity
{
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public bool PushEnabled { get; private set; }

    private NotificationPreference() { }

    public NotificationPreference(Guid userId, NotificationType type, bool pushEnabled)
    {
        UserId = userId;
        Type = type;
        PushEnabled = pushEnabled;
    }

    public void SetPush(bool enabled)
    {
        PushEnabled = enabled;
        Touch();
    }
}
