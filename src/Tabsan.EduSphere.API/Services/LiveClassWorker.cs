using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.LiveClasses;
using Tabsan.EduSphere.Infrastructure.Modules;

namespace Tabsan.EduSphere.API.Services;

/// <summary>
/// Plan M10: every minute — reminders shortly before live classes, automatic attendance after them, and recording links
/// from the provider. Does nothing while the "live_classes" module is not licensed.
/// </summary>
public sealed class LiveClassWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IOptions<LiveClassOptions> _options;
    private readonly ILogger<LiveClassWorker> _logger;

    public LiveClassWorker(IServiceScopeFactory scopes, IOptions<LiveClassOptions> options, ILogger<LiveClassWorker> logger)
    {
        _scopes = scopes;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var modules = scope.ServiceProvider.GetRequiredService<Tabsan.EduSphere.Application.Interfaces.IModuleEntitlementResolver>();
                if (await modules.IsActiveAsync(KnownModuleKeys.LiveClasses, stoppingToken))
                {
                    var (reminded, attendance, recordings) = await scope.ServiceProvider.GetRequiredService<LiveClassService>().RunAsync(DateTime.UtcNow, stoppingToken);
                    if (reminded + attendance + recordings > 0)
                        _logger.LogInformation("Live classes: {Reminded} reminders, {Attendance} attendance runs, {Recordings} recordings.", reminded, attendance, recordings);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogWarning(ex, "Live class run failed."); }

            try { await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(_options.Value.WorkerIntervalSeconds, 10, 3600)), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
