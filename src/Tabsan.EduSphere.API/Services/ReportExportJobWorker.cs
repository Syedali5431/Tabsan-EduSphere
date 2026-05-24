using Tabsan.EduSphere.Application.DTOs.Reports;
using Tabsan.EduSphere.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Tabsan.EduSphere.API.Services;

public sealed class ReportExportJobWorker : BackgroundService
{
    private readonly ReportExportJobQueue _queue;
    private readonly ReportExportJobStore _store;
    private readonly IServiceProvider _services;
    private readonly ILogger<ReportExportJobWorker> _logger;
    private readonly BackgroundJobReliabilityOptions _reliability;
    private readonly BackgroundJobHealthTracker _healthTracker;

    public ReportExportJobWorker(
        ReportExportJobQueue queue,
        ReportExportJobStore store,
        IServiceProvider services,
        IOptions<BackgroundJobReliabilityOptions> reliability,
        BackgroundJobHealthTracker healthTracker,
        ILogger<ReportExportJobWorker> logger)
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
                await _store.SetStateAsync(new ReportExportJobState
                {
                    JobId = request.JobId,
                    RequestedByUserId = request.RequestedByUserId,
                    Status = "running"
                }, stoppingToken);

                byte[] bytes = [];
                string contentType = "application/octet-stream";
                string extension = "bin";

                var maxAttempts = Math.Max(1, _reliability.MaxRetryAttempts);
                for (var attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        using var scope = _services.CreateScope();
                        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
                        var reportRequest = new ResultSummaryRequest(request.SemesterId, request.DepartmentId, request.CourseOfferingId, request.StudentProfileId, request.InstitutionType, null, null);

                        switch (request.Format)
                        {
                            case ReportExportFormat.Csv:
                                bytes = await reportService.ExportResultSummaryCsvAsync(reportRequest, stoppingToken);
                                contentType = "text/csv";
                                extension = "csv";
                                break;
                            case ReportExportFormat.Pdf:
                                bytes = await reportService.ExportResultSummaryPdfAsync(reportRequest, stoppingToken);
                                contentType = "application/pdf";
                                extension = "pdf";
                                break;
                            default:
                                bytes = await reportService.ExportResultSummaryExcelAsync(reportRequest, stoppingToken);
                                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                                extension = "xlsx";
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
                        _healthTracker.RecordReportExportRetry();
                        var delayMs = Math.Max(25, _reliability.BaseDelayMilliseconds * attempt);
                        _logger.LogWarning(ex, "Report export job {JobId} attempt {Attempt}/{MaxAttempts} failed, retrying in {DelayMs}ms.", request.JobId, attempt, maxAttempts, delayMs);
                        await Task.Delay(TimeSpan.FromMilliseconds(delayMs), stoppingToken);
                    }
                }

                var fileName = $"result-summary-{request.JobId:N}.{extension}";
                await _store.SetPayloadAsync(request.JobId, bytes, stoppingToken);
                await _store.SetStateAsync(new ReportExportJobState
                {
                    JobId = request.JobId,
                    RequestedByUserId = request.RequestedByUserId,
                    Status = "completed",
                    ContentType = contentType,
                    FileName = fileName,
                    CompletedAt = DateTimeOffset.UtcNow
                }, stoppingToken);
                _healthTracker.RecordReportExportSuccess();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Report export job {JobId} failed.", request.JobId);
                _healthTracker.RecordReportExportFailure();
                var consecutiveFailures = _healthTracker.GetReportExportConsecutiveFailures();
                if (consecutiveFailures >= Math.Max(1, _reliability.AlertConsecutiveFailureThreshold))
                {
                    _logger.LogWarning("Report export worker consecutive failures reached {ConsecutiveFailures}.", consecutiveFailures);
                }
                await _store.SetStateAsync(new ReportExportJobState
                {
                    JobId = request.JobId,
                    RequestedByUserId = request.RequestedByUserId,
                    Status = "failed",
                    Error = ex.Message,
                    CompletedAt = DateTimeOffset.UtcNow
                }, stoppingToken);
            }
        }
    }
}