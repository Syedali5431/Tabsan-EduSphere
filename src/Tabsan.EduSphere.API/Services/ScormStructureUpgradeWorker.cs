using Tabsan.EduSphere.Application.Lms.Scorm;

namespace Tabsan.EduSphere.API.Services;

/// <summary>
/// Once after start-up: reads again the manifest of SCORM packages uploaded under older rules, so packages in parts get
/// their contents list, SCORM 2004 packages their sequencing and SCORM 1.2 packages their prerequisites without being
/// uploaded again.
/// </summary>
public sealed class ScormStructureUpgradeWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ScormStructureUpgradeWorker> _logger;

    public ScormStructureUpgradeWorker(IServiceScopeFactory scopes, ILogger<ScormStructureUpgradeWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            using var scope = _scopes.CreateScope();
            var upgraded = await scope.ServiceProvider.GetRequiredService<ScormService>().UpgradeStructuresAsync(stoppingToken);
            if (upgraded > 0) _logger.LogInformation("Read {Count} SCORM package(s) again under the current structure rules.", upgraded);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex) { _logger.LogWarning(ex, "SCORM package structure upgrade failed."); }
    }
}
