using Tabsan.EduSphere.Domain.Notifications;

namespace Tabsan.EduSphere.Application.Interfaces;

// Plan M4.3 — Web Push notifications.

/// <summary>Settings (section "WebPush").</summary>
public sealed class WebPushOptions
{
    public const string SectionName = "WebPush";

    public bool Enabled { get; set; } = true;

    /// <summary>VAPID "sub": a mailto: or https: contact for push services. Required by Apple.</summary>
    public string Subject { get; set; } = "mailto:support@tabsan.local";

    /// <summary>Where the VAPID key pair is kept (generated on first use). Default: the OS user's app-data folder.</summary>
    public string? KeyFile { get; set; }

    /// <summary>Optional: VAPID keys from configuration (base64url), overriding the key file.</summary>
    public string? PublicKey { get; set; }
    public string? PrivateKey { get; set; }

    /// <summary>Push services a subscription endpoint may point to (host or ".suffix"). Protects against server-side request forgery.</summary>
    public List<string> AllowedHosts { get; set; } =
    [
        "fcm.googleapis.com", "android.googleapis.com", ".push.services.mozilla.com", ".notify.windows.com", ".push.apple.com"
    ];

    public int TtlSeconds { get; set; } = 86400;

    /// <summary>Consecutive delivery failures after which a subscription is switched off.</summary>
    public int MaxConsecutiveFailures { get; set; } = 5;
}

public enum WebPushSendResult
{
    Delivered,
    /// <summary>The push service no longer knows the subscription (404/410).</summary>
    Gone,
    Failed
}

public sealed record PushTarget(string Endpoint, string P256dh, string Auth);

/// <summary>Encrypts (RFC 8291) and sends (RFC 8292 VAPID) one push message.</summary>
public interface IWebPushSender
{
    bool IsConfigured { get; }

    /// <summary>The application server public key browsers subscribe with (base64url, 65 bytes uncompressed P-256).</summary>
    string? PublicKey { get; }

    Task<WebPushSendResult> SendAsync(PushTarget target, byte[] payload, int ttlSeconds, CancellationToken ct = default);
}

public sealed record PushSubscribeRequest(string Endpoint, string P256dh, string Auth, string? DeviceLabel);

public sealed record PushDeviceResponse(Guid Id, string? DeviceLabel, string EndpointHost, bool IsActive, DateTime CreatedAt, DateTime? LastSuccessAt);

public sealed record PushPreferenceItem(string Type, string Label, bool PushEnabled);

public sealed record PushPreferencesRequest(IReadOnlyList<PushPreferenceItem> Preferences);

public sealed record PushSummaryResponse(bool Enabled, string? PublicKey, IReadOnlyList<PushDeviceResponse> Devices, IReadOnlyList<PushPreferenceItem> Preferences);

public interface IPushNotificationService
{
    Task<PushSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Registers (or moves to this user) a browser subscription. Returns an error message when refused.</summary>
    Task<string?> SubscribeAsync(Guid userId, PushSubscribeRequest request, CancellationToken ct = default);

    Task<bool> UnsubscribeAsync(Guid userId, string endpoint, CancellationToken ct = default);
    Task SavePreferencesAsync(Guid userId, PushPreferencesRequest request, CancellationToken ct = default);

    /// <summary>Sends a test message to the user's own devices. Returns how many were delivered.</summary>
    Task<int> SendTestAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Sends a notification to the users' devices (respecting their per-type choices). Returns how many were delivered.</summary>
    Task<int> SendToUsersAsync(NotificationType type, string title, string body, string? url, IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);
}

/// <summary>Hands notifications to push delivery (queued in the API, direct elsewhere). Never throws.</summary>
public interface IPushNotificationDispatcher
{
    Task DispatchAsync(NotificationType type, string title, string body, IReadOnlyList<Guid> userIds, CancellationToken ct = default);
}
