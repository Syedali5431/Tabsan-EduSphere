using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Similarity;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Modules;

namespace Tabsan.EduSphere.API.Services;

/// <summary>
/// Plan M9.2: the similarity queue. Every <see cref="SimilarityOptions.WorkerIntervalSeconds"/> it queues a check for each
/// new attempt of an assignment with checking turned on and works through the queued checks (built-in comparison, or
/// submission to Copyleaks with retries). Does nothing while the "similarity" module is not licensed.
/// </summary>
public sealed class SimilarityWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IOptions<SimilarityOptions> _options;
    private readonly ILogger<SimilarityWorker> _logger;

    public SimilarityWorker(IServiceScopeFactory scopes, IOptions<SimilarityOptions> options, ILogger<SimilarityWorker> logger)
    {
        _scopes = scopes;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var modules = scope.ServiceProvider.GetRequiredService<Tabsan.EduSphere.Application.Interfaces.IModuleEntitlementResolver>();
                if (await modules.IsActiveAsync(KnownModuleKeys.Similarity, stoppingToken))
                {
                    var run = await scope.ServiceProvider.GetRequiredService<SimilarityService>().RunAsync(DateTime.UtcNow, stoppingToken);
                    if (run.Queued > 0 || run.Processed > 0)
                        _logger.LogInformation("Similarity: {Queued} queued, {Processed} processed.", run.Queued, run.Processed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogWarning(ex, "Similarity run failed."); }

            try { await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(_options.Value.WorkerIntervalSeconds, 5, 3600)), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
