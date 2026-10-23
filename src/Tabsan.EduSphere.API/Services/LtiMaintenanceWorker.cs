using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.API.Services;

/// <summary>Plan M5: hourly removal of expired LTI launches, used nonces and access tokens.</summary>
public sealed class LtiMaintenanceWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<LtiMaintenanceWorker> _logger;

    public LtiMaintenanceWorker(IServiceScopeFactory scopes, ILogger<LtiMaintenanceWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<ILtiRepository>().PurgeExpiredAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogWarning(ex, "LTI clean-up failed."); }

            try { await Task.Delay(TimeSpan.FromHours(1), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
