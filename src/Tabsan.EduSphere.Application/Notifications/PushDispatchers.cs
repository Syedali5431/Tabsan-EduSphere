using Microsoft.Extensions.Logging;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Notifications;

namespace Tabsan.EduSphere.Application.Notifications;

/// <summary>Plan M4.3: sends push messages straight away (used by the BackgroundJobs host).</summary>
public sealed class DirectPushNotificationDispatcher : IPushNotificationDispatcher
{
    private readonly IPushNotificationService _push;
    private readonly ILogger<DirectPushNotificationDispatcher>? _logger;

    public DirectPushNotificationDispatcher(IPushNotificationService push, ILogger<DirectPushNotificationDispatcher>? logger = null)
    {
        _push = push;
        _logger = logger;
    }

    public async Task DispatchAsync(NotificationType type, string title, string body, IReadOnlyList<Guid> userIds, CancellationToken ct = default)
    {
        try { await _push.SendToUsersAsync(type, title, body, null, userIds, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogWarning(ex, "Push delivery for '{Title}' failed.", title);
        }
    }
}

/// <summary>Plan M4.3: one queued push delivery.</summary>
public sealed record PushWorkItem(NotificationType Type, string Title, string Body, IReadOnlyList<Guid> UserIds);
