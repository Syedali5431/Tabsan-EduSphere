using Tabsan.EduSphere.Application.Assignments;

namespace Tabsan.EduSphere.API.Services;

/// <summary>
/// Plan M8: every hour gives the missing grade to work never handed in (where the course's policy asks for it) and
/// allocates peer reviews for assignments that have passed their due date.
/// </summary>
public sealed class AssignmentPolicyWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<AssignmentPolicyWorker> _logger;

    public AssignmentPolicyWorker(IServiceScopeFactory scopes, ILogger<AssignmentPolicyWorker> logger)
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
                var now = DateTime.UtcNow;
                var missing = await scope.ServiceProvider.GetRequiredService<AssignmentPolicyService>().ApplyMissingEverywhereAsync(now, stoppingToken);
                var allocated = await scope.ServiceProvider.GetRequiredService<PeerReviewService>().AllocateDueAsync(now, stoppingToken);
                if (missing > 0 || allocated > 0)
                    _logger.LogInformation("Assignment policies: {Missing} missing grades given, {Allocated} peer reviews allocated.", missing, allocated);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogWarning(ex, "Assignment policy run failed."); }

            try { await Task.Delay(TimeSpan.FromHours(1), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
