using Tabsan.EduSphere.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Tabsan.EduSphere.API.Services;

public sealed class AnalyticsExportJobWorker : BackgroundService
{
    private readonly AnalyticsExportJobQueue _queue;
    private readonly AnalyticsExportJobStore _store;
    private readonly IServiceProvider _services;
    private readonly ILogger<AnalyticsExportJobWorker> _logger;
    private readonly BackgroundJobReliabilityOptions _reliability;
    private readonly BackgroundJobHealthTracker _healthTracker;

    public AnalyticsExportJobWorker(
        AnalyticsExportJobQueue queue,
        AnalyticsExportJobStore store,
        IServiceProvider services,
        IOptions<BackgroundJobReliabilityOptions> reliability,
        BackgroundJobHealthTracker healthTracker,
        ILogger<AnalyticsExportJobWorker> logger)
    {
        _queue = queue;
        _store = store;
        _services = services;
        _reliability = reliability.Value;
        _healthTracker = healthTracker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await _store.SetStateAsync(new AnalyticsExportJobState
                {
                    JobId = request.JobId,
                    RequestedByUserId = request.RequestedByUserId,
                    RequestedByTenantId = request.RequestedByTenantId,
                    RequestedByCampusId = request.RequestedByCampusId,
                    ReportType = request.ReportType,
                    Format = request.Format,
                    Status = "running"
                }, stoppingToken);

                byte[] bytes = [];
                var contentType = AnalyticsExportConventions.GetContentType(request.Format);

                var maxAttempts = Math.Max(1, _reliability.MaxRetryAttempts);
                for (var attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        using var scope = _services.CreateScope();
                        var analytics = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();

                        switch (request.ReportType)
                        {
                            case AnalyticsExportReportType.Attendance when request.Format == AnalyticsExportFormat.Excel:
                                bytes = await analytics.ExportAttendanceExcelAsync(request.DepartmentId, request.InstitutionType, stoppingToken, request.TenantId, request.CampusId);
                                break;
                            case AnalyticsExportReportType.Attendance:
                                bytes = await analytics.ExportAttendancePdfAsync(request.DepartmentId, request.InstitutionType, stoppingToken, request.TenantId, request.CampusId);
                                break;
                            case AnalyticsExportReportType.Performance when request.Format == AnalyticsExportFormat.Excel:
                                bytes = await analytics.ExportPerformanceExcelAsync(request.DepartmentId, request.InstitutionType, stoppingToken, request.TenantId, request.CampusId);
                                break;
                            case AnalyticsExportReportType.TopPerformers when request.Format == AnalyticsExportFormat.Excel:
                                bytes = await analytics.ExportTopPerformersExcelAsync(request.DepartmentId, request.InstitutionType, 10, stoppingToken, request.TenantId, request.CampusId);
                                break;
                            case AnalyticsExportReportType.TopPerformers:
                                bytes = await analytics.ExportTopPerformersPdfAsync(request.DepartmentId, request.InstitutionType, 10, stoppingToken, request.TenantId, request.CampusId);
                                break;
                            case AnalyticsExportReportType.PerformanceTrends when request.Format == AnalyticsExportFormat.Excel:
                                bytes = await analytics.ExportPerformanceTrendsExcelAsync(request.DepartmentId, request.InstitutionType, 30, stoppingToken, request.TenantId, request.CampusId);
                                break;
                            case AnalyticsExportReportType.PerformanceTrends:
                                bytes = await analytics.ExportPerformanceTrendsPdfAsync(request.DepartmentId, request.InstitutionType, 30, stoppingToken, request.TenantId, request.CampusId);
                                break;
                            case AnalyticsExportReportType.ComparativeSummary when request.Format == AnalyticsExportFormat.Excel:
                                bytes = await analytics.ExportComparativeSummaryExcelAsync(request.DepartmentId, request.InstitutionType, stoppingToken, request.TenantId, request.CampusId);
                                break;
                            case AnalyticsExportReportType.ComparativeSummary:
                                bytes = await analytics.ExportComparativeSummaryPdfAsync(request.DepartmentId, request.InstitutionType, stoppingToken, request.TenantId, request.CampusId);
                                break;
                            default:
                                bytes = await analytics.ExportPerformancePdfAsync(request.DepartmentId, request.InstitutionType, stoppingToken, request.TenantId, request.CampusId);
                                break;
                        }

                        break;
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex) when (attempt < maxAttempts)
                    {
                        _healthTracker.RecordAnalyticsExportRetry();
                        var delayMs = Math.Max(25, _reliability.BaseDelayMilliseconds * attempt);
                        _logger.LogWarning(ex, "Analytics export job {JobId} attempt {Attempt}/{MaxAttempts} failed, retrying in {DelayMs}ms.", request.JobId, attempt, maxAttempts, delayMs);
                        await Task.Delay(TimeSpan.FromMilliseconds(delayMs), stoppingToken);
                    }
                }

                var fileName = AnalyticsExportConventions.CreateFileName(request.ReportType, request.Format);
                await _store.SetPayloadAsync(request.JobId, bytes, stoppingToken);
                await _store.SetStateAsync(new AnalyticsExportJobState
                {
                    JobId = request.JobId,
                    RequestedByUserId = request.RequestedByUserId,
                    RequestedByTenantId = request.RequestedByTenantId,
                    RequestedByCampusId = request.RequestedByCampusId,
                    ReportType = request.ReportType,
                    Format = request.Format,
                    Status = "completed",
                    ContentType = contentType,
                    FileName = fileName,
                    CompletedAt = DateTimeOffset.UtcNow
                }, stoppingToken);
                _healthTracker.RecordAnalyticsExportSuccess();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analytics export job {JobId} failed.", request.JobId);
                _healthTracker.RecordAnalyticsExportFailure();
                var consecutiveFailures = _healthTracker.GetAnalyticsExportConsecutiveFailures();
                if (consecutiveFailures >= Math.Max(1, _reliability.AlertConsecutiveFailureThreshold))
                {
                    _logger.LogWarning("Analytics export worker consecutive failures reached {ConsecutiveFailures}.", consecutiveFailures);
                }
                await _store.SetStateAsync(new AnalyticsExportJobState
                {
                    JobId = request.JobId,
                    RequestedByUserId = request.RequestedByUserId,
                    RequestedByTenantId = request.RequestedByTenantId,
                    RequestedByCampusId = request.RequestedByCampusId,
                    ReportType = request.ReportType,
                    Format = request.Format,
                    Status = "failed",
                    Error = ex.Message,
                    CompletedAt = DateTimeOffset.UtcNow
                }, stoppingToken);
            }
        }
    }
}
