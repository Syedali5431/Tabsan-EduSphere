using System.Threading.Channels;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Notifications;
using Tabsan.EduSphere.Domain.Notifications;

namespace Tabsan.EduSphere.API.Services;

/// <summary>
/// Plan M4.3: push delivery runs off the request path. The notification is already saved in the inbox, so a
/// full queue (or a restart) only loses the device alert, never the notification itself.
/// </summary>
public sealed class PushDispatchQueue : IPushNotificationDispatcher
{
    private readonly Channel<PushWorkItem> _channel = Channel.CreateBounded<PushWorkItem>(new BoundedChannelOptions(1000)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true
    });

    public ChannelReader<PushWorkItem> Reader => _channel.Reader;

    public Task DispatchAsync(NotificationType type, string title, string body, IReadOnlyList<Guid> userIds, CancellationToken ct = default)
    {
        if (userIds.Count > 0) _channel.Writer.TryWrite(new PushWorkItem(type, title, body, userIds));
        return Task.CompletedTask;
    }
}

public sealed class PushDispatchWorker : BackgroundService
{
    private readonly PushDispatchQueue _queue;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<PushDispatchWorker> _logger;

    public PushDispatchWorker(PushDispatchQueue queue, IServiceScopeFactory scopes, ILogger<PushDispatchWorker> logger)
    {
        _queue = queue;
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var item in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var push = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
                    await push.SendToUsersAsync(item.Type, item.Title, item.Body, null, item.UserIds, stoppingToken);
                    // Plan M12 — the same notification to the users' phones (mobile app, FCM).
                    var mobile = scope.ServiceProvider.GetService<Tabsan.EduSphere.Application.Mobile.IMobileService>();
                    if (mobile is not null) await mobile.SendPushAsync(item.Type, item.Title, item.Body, item.UserIds, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Push delivery for '{Title}' failed.", item.Title);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
