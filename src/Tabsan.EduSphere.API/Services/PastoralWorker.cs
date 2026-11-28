using Tabsan.EduSphere.Application.Pastoral;

namespace Tabsan.EduSphere.API.Services;

/// <summary>
/// Every minute: reminds the owners of support plans whose review date has come, and the people following up pastoral
/// notes whose date has passed (once each).
/// </summary>
public sealed class PastoralWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<PastoralWorker> _logger;

    public PastoralWorker(IServiceScopeFactory scopes, ILogger<PastoralWorker> logger)
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
                var reminded = await scope.ServiceProvider.GetRequiredService<PastoralService>().RemindDueAsync(DateTime.UtcNow, stoppingToken);
                if (reminded > 0) _logger.LogInformation("Pastoral care: {Count} plan review(s) or follow-up(s) reminded.", reminded);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogWarning(ex, "Pastoral reminders run failed."); }

            try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
