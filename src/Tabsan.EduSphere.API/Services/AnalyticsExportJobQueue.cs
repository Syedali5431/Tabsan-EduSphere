using System.Threading.Channels;

namespace Tabsan.EduSphere.API.Services;

public enum AnalyticsExportReportType
{
    Performance,
    Attendance,
    TopPerformers,
    PerformanceTrends,
    ComparativeSummary
}

public enum AnalyticsExportFormat
{
    Pdf,
    Excel
}

public sealed record AnalyticsExportJobRequest(
    Guid JobId,
    Guid RequestedByUserId,
    Guid? RequestedByTenantId,
    Guid? RequestedByCampusId,
    Guid? DepartmentId,
    int? InstitutionType,
    AnalyticsExportReportType ReportType,
    AnalyticsExportFormat Format);

public sealed class AnalyticsExportJobQueue
{
    private readonly Channel<AnalyticsExportJobRequest> _channel = Channel.CreateUnbounded<AnalyticsExportJobRequest>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public void Enqueue(AnalyticsExportJobRequest workItem)
    {
        if (!_channel.Writer.TryWrite(workItem))
            throw new InvalidOperationException("Unable to queue analytics export job.");
    }

    public IAsyncEnumerable<AnalyticsExportJobRequest> DequeueAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}

public static class AnalyticsExportConventions
{
    public static string GetExtension(AnalyticsExportFormat format)
        => format == AnalyticsExportFormat.Excel ? "xlsx" : "pdf";

    public static string GetContentType(AnalyticsExportFormat format)
        => format == AnalyticsExportFormat.Excel
            ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            : "application/pdf";

    public static string GetReportKey(AnalyticsExportReportType reportType)
        => reportType switch
        {
            AnalyticsExportReportType.Performance => "performance",
            AnalyticsExportReportType.Attendance => "attendance",
            AnalyticsExportReportType.TopPerformers => "top-performers",
            AnalyticsExportReportType.PerformanceTrends => "performance-trends",
            AnalyticsExportReportType.ComparativeSummary => "comparative-summary",
            _ => "report"
        };

    public static string CreateFileName(AnalyticsExportReportType reportType, AnalyticsExportFormat format, DateTimeOffset? timestamp = null)
    {
        var utc = (timestamp ?? DateTimeOffset.UtcNow).UtcDateTime;
        return $"analytics-{GetReportKey(reportType)}-{utc:yyyyMMddHHmmss}.{GetExtension(format)}";
    }
}

public sealed class AnalyticsExportJobState
{
    public Guid JobId { get; init; }
    public Guid RequestedByUserId { get; init; }
    public Guid? RequestedByTenantId { get; init; }
    public Guid? RequestedByCampusId { get; init; }
    public AnalyticsExportReportType ReportType { get; init; }
    public AnalyticsExportFormat Format { get; init; }
    public string Status { get; init; } = "queued";
    public string? Error { get; init; }
    public string? ContentType { get; init; }
    public string? FileName { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; init; }
}
