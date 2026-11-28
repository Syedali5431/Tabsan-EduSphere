using Tabsan.EduSphere.Application.Community;

namespace Tabsan.EduSphere.API.Services;

/// <summary>
/// Every minute: tells the audience of scheduled school news when its time comes, and reminds everyone who has not answered
/// a form a day before it is due (once a day at most).
/// </summary>
public sealed class CommunityWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<CommunityWorker> _logger;

    public CommunityWorker(IServiceScopeFactory scopes, ILogger<CommunityWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the application finish starting (and migrations run) first.
        try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var handled = await scope.ServiceProvider.GetRequiredService<CommunityService>().NotifyDueAsync(DateTime.UtcNow, stoppingToken);
                if (handled > 0) _logger.LogInformation("School news and forms: {Count} item(s) announced or reminded.", handled);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogWarning(ex, "School news and forms run failed."); }

            try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
