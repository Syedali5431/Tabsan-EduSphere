using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Notifications;

namespace Tabsan.EduSphere.Application.Notifications;

/// <summary>
/// Plan M4.3: Web Push subscriptions, per-type preferences and delivery.
/// <list type="bullet">
///   <item>Subscription endpoints must be https on an allowed push service host (no server-side request forgery).</item>
///   <item>Browser keys are checked (65-byte uncompressed P-256 point, 16-byte auth secret) before they are stored.</item>
///   <item>Push is on for every type until the user switches a type off; a 404/410 from the push service removes the device.</item>
///   <item>Payloads carry only the title, a short body and a portal link — never grades or other record data.</item>
/// </list>
/// </summary>
public sealed class PushNotificationService : IPushNotificationService
{
    private const int MaxBodyChars = 240;

    private static readonly (NotificationType Type, string Label, string Url)[] Types =
    [
        (NotificationType.General, "General messages", "/Portal/Notifications"),
        (NotificationType.Assignment, "Assignments", "/Portal/Assignments"),
        (NotificationType.Result, "Results", "/Portal/Notifications"),
        (NotificationType.AttendanceAlert, "Attendance alerts", "/Portal/Notifications"),
        (NotificationType.System, "System notices", "/Portal/Notifications"),
        (NotificationType.Announcement, "Announcements", "/Portal/Announcements"),
        (NotificationType.LiveClass, "Live classes", "/Portal/LiveClasses")
    ];

    private readonly IPushSubscriptionRepository _repo;
    private readonly IWebPushSender _sender;
    private readonly WebPushOptions _options;
    private readonly ILogger<PushNotificationService>? _logger;

    public PushNotificationService(IPushSubscriptionRepository repo, IWebPushSender sender, IOptions<WebPushOptions> options,
        ILogger<PushNotificationService>? logger = null)
    {
        _repo = repo;
        _sender = sender;
        _options = options.Value;
        _logger = logger;
    }

    private bool Enabled => _options.Enabled && _sender.IsConfigured;

    public async Task<PushSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken ct = default)
    {
        var devices = await _repo.GetForUserAsync(userId, ct);
        var prefs = (await _repo.GetPreferencesAsync(userId, ct)).ToDictionary(p => p.Type);
        return new PushSummaryResponse(
            Enabled,
            Enabled ? _sender.PublicKey : null,
            devices.OrderByDescending(d => d.CreatedAt)
                .Select(d => new PushDeviceResponse(d.Id, d.DeviceLabel, Uri.TryCreate(d.Endpoint, UriKind.Absolute, out var u) ? u.Host : "", d.IsActive, d.CreatedAt, d.LastSuccessAt))
                .ToList(),
            Types.Select(t => new PushPreferenceItem(t.Type.ToString(), t.Label, !prefs.TryGetValue(t.Type, out var p) || p.PushEnabled)).ToList());
    }

    public async Task<string?> SubscribeAsync(Guid userId, PushSubscribeRequest request, CancellationToken ct = default)
    {
        if (!Enabled) return "Push notifications are not enabled on this server.";
        var error = ValidateEndpoint(request.Endpoint, _options.AllowedHosts) ?? ValidateKeys(request.P256dh, request.Auth);
        if (error is not null) return error;

        var existing = await _repo.GetByEndpointAsync(request.Endpoint.Trim(), ct);
        if (existing is null) _repo.Add(new PushSubscription(userId, request.Endpoint, request.P256dh, request.Auth, request.DeviceLabel));
        else existing.Assign(userId, request.P256dh, request.Auth, request.DeviceLabel);
        await _repo.SaveChangesAsync(ct);
        return null;
    }

    public async Task<bool> UnsubscribeAsync(Guid userId, string endpoint, CancellationToken ct = default)
    {
        var existing = await _repo.GetByEndpointAsync((endpoint ?? string.Empty).Trim(), ct);
        if (existing is null || existing.UserId != userId) return false;
        _repo.Remove(existing);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    public async Task SavePreferencesAsync(Guid userId, PushPreferencesRequest request, CancellationToken ct = default)
    {
        var current = (await _repo.GetPreferencesAsync(userId, ct)).ToDictionary(p => p.Type);
        foreach (var item in request.Preferences)
        {
            if (!Enum.TryParse<NotificationType>(item.Type, true, out var type) || !Enum.IsDefined(type)) continue;
            if (current.TryGetValue(type, out var pref)) pref.SetPush(item.PushEnabled);
            else _repo.Add(new NotificationPreference(userId, type, item.PushEnabled));
        }
        await _repo.SaveChangesAsync(ct);
    }

    public async Task<int> SendTestAsync(Guid userId, CancellationToken ct = default)
    {
        if (!Enabled) return 0;
        var devices = (await _repo.GetForUserAsync(userId, ct)).Where(d => d.IsActive).ToList();
        return await DeliverAsync(devices, BuildPayload(NotificationType.General, "Notifications are working",
            "This device will now show EduSphere notifications.", "/Portal/Notifications"), ct);
    }

    public async Task<int> SendToUsersAsync(NotificationType type, string title, string body, string? url, IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
    {
        if (!Enabled || userIds.Count == 0) return 0;
        var devices = await _repo.GetDeliverableAsync(userIds, type, ct);
        if (devices.Count == 0) return 0;
        var link = url ?? Types.FirstOrDefault(t => t.Type == type).Url ?? "/Portal/Notifications";
        return await DeliverAsync(devices, BuildPayload(type, title, body, link), ct);
    }

    private async Task<int> DeliverAsync(IReadOnlyList<PushSubscription> devices, byte[] payload, CancellationToken ct)
    {
        var delivered = 0;
        var now = DateTime.UtcNow;
        foreach (var device in devices)
        {
            WebPushSendResult result;
            try { result = await _sender.SendAsync(new PushTarget(device.Endpoint, device.P256dh, device.Auth), payload, _options.TtlSeconds, ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogWarning(ex, "Push delivery to subscription {SubscriptionId} failed.", device.Id);
                result = WebPushSendResult.Failed;
            }

            switch (result)
            {
                case WebPushSendResult.Delivered: device.RecordSuccess(now); delivered++; break;
                case WebPushSendResult.Gone: _repo.Remove(device); break;
                default: device.RecordFailure(now, Math.Max(1, _options.MaxConsecutiveFailures)); break;
            }
        }
        await _repo.SaveChangesAsync(ct);
        return delivered;
    }

    /// <summary>The JSON the service worker turns into a notification (kept well under the 4 KB push limit).</summary>
    public static byte[] BuildPayload(NotificationType type, string title, string body, string url)
    {
        var text = (body ?? string.Empty).Trim();
        if (text.Length > MaxBodyChars) text = text[..(MaxBodyChars - 1)] + "…";
        var json = JsonSerializer.Serialize(new
        {
            title = (title ?? "EduSphere").Trim() is { Length: > 120 } t ? t[..119] + "…" : (title ?? "EduSphere").Trim(),
            body = text,
            url = url.StartsWith('/') && !url.StartsWith("//") ? url : "/Portal/Notifications",
            type = type.ToString(),
            tag = "edusphere-" + type.ToString().ToLowerInvariant()
        });
        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>https only, on an allowed push service host (exact host or ".suffix").</summary>
    public static string? ValidateEndpoint(string? endpoint, IReadOnlyCollection<string> allowedHosts)
    {
        if (string.IsNullOrWhiteSpace(endpoint) || endpoint.Length > 1000
            || !Uri.TryCreate(endpoint.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(uri.UserInfo) || uri.HostNameType != UriHostNameType.Dns || !uri.IsDefaultPort)
            return "The push endpoint is not a valid https address.";
        var host = uri.IdnHost.ToLowerInvariant();
        var allowed = allowedHosts.Any(a =>
        {
            var rule = a.Trim().ToLowerInvariant();
            return rule.StartsWith('.') ? host.EndsWith(rule, StringComparison.Ordinal) && host.Length > rule.Length : host == rule;
        });
        return allowed ? null : $"Push service \"{host}\" is not allowed on this server.";
    }

    /// <summary>The browser's keys must be a valid P-256 point and a 16-byte secret.</summary>
    public static string? ValidateKeys(string? p256dh, string? auth)
    {
        try
        {
            var key = Base64Url.Decode(p256dh ?? string.Empty);
            var secret = Base64Url.Decode(auth ?? string.Empty);
            if (key.Length != 65 || key[0] != 0x04 || secret.Length != 16) return "The browser's push keys are not valid.";
            using var ecdh = ECDiffieHellman.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint { X = key[1..33], Y = key[33..65] }
            });
            return null;
        }
        // Windows CNG reports a point that is not on the curve as PlatformNotSupportedException.
        catch (Exception ex) when (ex is FormatException or CryptographicException or PlatformNotSupportedException or ArgumentException)
        {
            return "The browser's push keys are not valid.";
        }
    }
}

/// <summary>base64url without padding (RFC 4648 §5).</summary>
public static class Base64Url
{
    public static string Encode(ReadOnlySpan<byte> data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static byte[] Decode(string value)
    {
        var s = value.Trim().Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
    }
}
