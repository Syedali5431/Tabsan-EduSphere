using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.API.Services;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Exposes analytics reports and PDF/Excel export endpoints.
/// All read endpoints require Admin or Faculty. Export endpoints require Admin or above.
/// </summary>
[ApiController]
[Route("api/analytics")]
[Authorize(Policy = "Faculty")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analytics;
    private readonly IDepartmentRepository _departments;
    private readonly AnalyticsExportJobQueue _exportQueue;
    private readonly AnalyticsExportJobStore _exportStore;

    /// <summary>Initialises the controller with the analytics service.</summary>
    public AnalyticsController(
        IAnalyticsService analytics,
        IDepartmentRepository departments,
        AnalyticsExportJobQueue exportQueue,
        AnalyticsExportJobStore exportStore)
    {
        _analytics = analytics;
        _departments = departments;
        _exportQueue = exportQueue;
        _exportStore = exportStore;
    }

    // ── Report endpoints ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns the performance report for a specific department or all departments.
    /// Admins see all; Faculty see their own department only.
    /// </summary>
    [HttpGet("performance")]
    public async Task<IActionResult> GetPerformance(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        [FromQuery] Guid? courseId,
        [FromQuery] Guid? semesterId,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, institutionType, ct);
        if (scope.Error is not null) return scope.Error;

        var result = await _analytics.GetPerformanceReportAsync(scope.DepartmentId, scope.InstitutionType, ct, courseId, semesterId);
        return result is null ? NotFound("No data found.") : Ok(result);
    }

    /// <summary>Returns the attendance summary for a department or all departments.</summary>
    [HttpGet("attendance")]
    public async Task<IActionResult> GetAttendance(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        [FromQuery] Guid? courseId,
        [FromQuery] Guid? semesterId,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, institutionType, ct);
        if (scope.Error is not null) return scope.Error;

        var result = await _analytics.GetAttendanceReportAsync(scope.DepartmentId, scope.InstitutionType, ct, courseId, semesterId);
        return result is null ? NotFound("No data found.") : Ok(result);
    }

    /// <summary>Returns assignment statistics for a department or all departments.</summary>
    [HttpGet("assignments")]
    public async Task<IActionResult> GetAssignmentStats(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        [FromQuery] Guid? courseId,
        [FromQuery] Guid? semesterId,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, institutionType, ct);
        if (scope.Error is not null) return scope.Error;

        var result = await _analytics.GetAssignmentStatsAsync(scope.DepartmentId, scope.InstitutionType, ct, courseId, semesterId);
        return result is null ? NotFound("No data found.") : Ok(result);
    }

    /// <summary>Returns quiz statistics for a department or all departments.</summary>
    [HttpGet("quizzes")]
    public async Task<IActionResult> GetQuizStats(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, institutionType, ct);
        if (scope.Error is not null) return scope.Error;

        var result = await _analytics.GetQuizStatsAsync(scope.DepartmentId, scope.InstitutionType, ct);
        return result is null ? NotFound("No data found.") : Ok(result);
    }

    /// <summary>Returns ranked top performers for a department or scoped institution context.</summary>
    [HttpGet("top-performers")]
    public async Task<IActionResult> GetTopPerformers(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        [FromQuery] int take = 10,
        CancellationToken ct = default)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, institutionType, ct);
        if (scope.Error is not null) return scope.Error;

        var result = await _analytics.GetTopPerformersAsync(scope.DepartmentId, scope.InstitutionType, take, ct);
        return result is null ? NotFound("No data found.") : Ok(result);
    }

    /// <summary>Returns daily performance trends for a department or scoped institution context.</summary>
    [HttpGet("performance-trends")]
    public async Task<IActionResult> GetPerformanceTrends(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        [FromQuery] int windowDays = 30,
        CancellationToken ct = default)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, institutionType, ct);
        if (scope.Error is not null) return scope.Error;

        var result = await _analytics.GetPerformanceTrendsAsync(scope.DepartmentId, scope.InstitutionType, windowDays, ct);
        return result is null ? NotFound("No data found.") : Ok(result);
    }

    /// <summary>Returns comparative summary metrics across scoped departments.</summary>
    [HttpGet("comparative-summary")]
    public async Task<IActionResult> GetComparativeSummary(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        CancellationToken ct = default)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, institutionType, ct);
        if (scope.Error is not null) return scope.Error;

        var result = await _analytics.GetComparativeSummaryAsync(scope.DepartmentId, scope.InstitutionType, ct);
        return result is null ? NotFound("No data found.") : Ok(result);
    }

    // ── Export endpoints ──────────────────────────────────────────────────────

    /// <summary>Downloads the performance report as a PDF file.</summary>
    [HttpGet("performance/export/pdf")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ExportPerformancePdf(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, institutionType, ct);
        if (scope.Error is not null) return scope.Error;

        var bytes = await _analytics.ExportPerformancePdfAsync(scope.DepartmentId, scope.InstitutionType, ct);
        return ReturnAnalyticsFile(bytes, AnalyticsExportReportType.Performance, AnalyticsExportFormat.Pdf);
    }

    /// <summary>Downloads the performance report as an Excel file.</summary>
    [HttpGet("performance/export/excel")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ExportPerformanceExcel(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, institutionType, ct);
        if (scope.Error is not null) return scope.Error;

        var bytes = await _analytics.ExportPerformanceExcelAsync(scope.DepartmentId, scope.InstitutionType, ct);
        return ReturnAnalyticsFile(bytes, AnalyticsExportReportType.Performance, AnalyticsExportFormat.Excel);
    }

    /// <summary>Downloads the attendance report as a PDF file.</summary>
    [HttpGet("attendance/export/pdf")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ExportAttendancePdf(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, institutionType, ct);
        if (scope.Error is not null) return scope.Error;

        var bytes = await _analytics.ExportAttendancePdfAsync(scope.DepartmentId, scope.InstitutionType, ct);
        return ReturnAnalyticsFile(bytes, AnalyticsExportReportType.Attendance, AnalyticsExportFormat.Pdf);
    }

    /// <summary>Downloads the attendance report as an Excel file.</summary>
    [HttpGet("attendance/export/excel")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ExportAttendanceExcelAsync(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, institutionType, ct);
        if (scope.Error is not null) return scope.Error;

        var bytes = await _analytics.ExportAttendanceExcelAsync(scope.DepartmentId, scope.InstitutionType, ct);
        return ReturnAnalyticsFile(bytes, AnalyticsExportReportType.Attendance, AnalyticsExportFormat.Excel);
    }

    /// <summary>Downloads top performers report as a PDF file.</summary>
    [HttpGet("top-performers/export/pdf")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ExportTopPerformersPdf(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        [FromQuery] int take = 10,
        CancellationToken ct = default)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, institutionType, ct);
        if (scope.Error is not null) return scope.Error;

        var bytes = await _analytics.ExportTopPerformersPdfAsync(scope.DepartmentId, scope.InstitutionType, take, ct);
        return ReturnAnalyticsFile(bytes, AnalyticsExportReportType.TopPerformers, AnalyticsExportFormat.Pdf);
    }

    /// <summary>Downloads top performers report as an Excel file.</summary>
    [HttpGet("top-performers/export/excel")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ExportTopPerformersExcel(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        [FromQuery] int take = 10,
        CancellationToken ct = default)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, institutionType, ct);
        if (scope.Error is not null) return scope.Error;

        var bytes = await _analytics.ExportTopPerformersExcelAsync(scope.DepartmentId, scope.InstitutionType, take, ct);
        return ReturnAnalyticsFile(bytes, AnalyticsExportReportType.TopPerformers, AnalyticsExportFormat.Excel);
    }

    /// <summary>Downloads performance trends report as a PDF file.</summary>
    [HttpGet("performance-trends/export/pdf")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ExportPerformanceTrendsPdf(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        [FromQuery] int windowDays = 30,
        CancellationToken ct = default)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, institutionType, ct);
        if (scope.Error is not null) return scope.Error;

        var bytes = await _analytics.ExportPerformanceTrendsPdfAsync(scope.DepartmentId, scope.InstitutionType, windowDays, ct);
        return ReturnAnalyticsFile(bytes, AnalyticsExportReportType.PerformanceTrends, AnalyticsExportFormat.Pdf);
    }

    /// <summary>Downloads performance trends report as an Excel file.</summary>
    [HttpGet("performance-trends/export/excel")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ExportPerformanceTrendsExcel(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        [FromQuery] int windowDays = 30,
        CancellationToken ct = default)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, institutionType, ct);
        if (scope.Error is not null) return scope.Error;

        var bytes = await _analytics.ExportPerformanceTrendsExcelAsync(scope.DepartmentId, scope.InstitutionType, windowDays, ct);
        return ReturnAnalyticsFile(bytes, AnalyticsExportReportType.PerformanceTrends, AnalyticsExportFormat.Excel);
    }

    /// <summary>Downloads comparative summary report as a PDF file.</summary>
    [HttpGet("comparative-summary/export/pdf")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ExportComparativeSummaryPdf(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        CancellationToken ct = default)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, institutionType, ct);
        if (scope.Error is not null) return scope.Error;

        var bytes = await _analytics.ExportComparativeSummaryPdfAsync(scope.DepartmentId, scope.InstitutionType, ct);
        return ReturnAnalyticsFile(bytes, AnalyticsExportReportType.ComparativeSummary, AnalyticsExportFormat.Pdf);
    }

    /// <summary>Downloads comparative summary report as an Excel file.</summary>
    [HttpGet("comparative-summary/export/excel")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ExportComparativeSummaryExcel(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        CancellationToken ct = default)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, institutionType, ct);
        if (scope.Error is not null) return scope.Error;

        var bytes = await _analytics.ExportComparativeSummaryExcelAsync(scope.DepartmentId, scope.InstitutionType, ct);
        return ReturnAnalyticsFile(bytes, AnalyticsExportReportType.ComparativeSummary, AnalyticsExportFormat.Excel);
    }

    /// <summary>
    /// Queues analytics export generation for background processing.
    /// Supported reportType: performance, attendance, top-performers, performance-trends, comparative-summary.
    /// Supported format: pdf, excel.
    /// </summary>
    [HttpPost("export-jobs")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> QueueExportJob(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        [FromQuery] string reportType = "performance",
        [FromQuery] string format = "pdf",
        CancellationToken ct = default)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, institutionType, ct);
        if (scope.Error is not null) return scope.Error;

        if (!TryParseReportType(reportType, out var parsedReportType))
            return BadRequest("reportType must be one of: performance, attendance, top-performers, performance-trends, comparative-summary.");

        if (!TryParseExportFormat(format, out var parsedFormat))
            return BadRequest("format must be one of: pdf, excel.");

        var requestedByUserId = GetCurrentUserId();
        if (requestedByUserId == Guid.Empty) return Unauthorized();

        var requestedByTenantId = GetCurrentTenantId();
        var requestedByCampusId = GetCurrentCampusId();

        var jobId = Guid.NewGuid();
        await _exportStore.SetStateAsync(new AnalyticsExportJobState
        {
            JobId = jobId,
            RequestedByUserId = requestedByUserId,
            RequestedByTenantId = requestedByTenantId,
            RequestedByCampusId = requestedByCampusId,
            ReportType = parsedReportType,
            Format = parsedFormat,
            Status = "queued"
        }, ct);

        _exportQueue.Enqueue(new AnalyticsExportJobRequest(
            jobId,
            requestedByUserId,
            requestedByTenantId,
            requestedByCampusId,
            scope.DepartmentId,
            scope.InstitutionType,
            parsedReportType,
            parsedFormat));

        return Accepted(new
        {
            jobId,
            status = "queued",
            statusUrl = $"/api/analytics/export-jobs/{jobId}",
            downloadUrl = $"/api/analytics/export-jobs/{jobId}/download"
        });
    }

    /// <summary>Returns status for a queued analytics export job.</summary>
    [HttpGet("export-jobs/{jobId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetExportJob(Guid jobId, CancellationToken ct)
    {
        var requestedByUserId = GetCurrentUserId();
        if (requestedByUserId == Guid.Empty) return Unauthorized();

        var state = await _exportStore.GetStateAsync(jobId, ct);
        if (state is null) return NotFound();

        if (!CanAccessExportJob(state, requestedByUserId))
            return Forbid();

        return Ok(state);
    }

    /// <summary>Downloads the completed payload for a queued analytics export job.</summary>
    [HttpGet("export-jobs/{jobId:guid}/download")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> DownloadExportJob(Guid jobId, CancellationToken ct)
    {
        var requestedByUserId = GetCurrentUserId();
        if (requestedByUserId == Guid.Empty) return Unauthorized();

        var state = await _exportStore.GetStateAsync(jobId, ct);
        if (state is null) return NotFound();

        if (!CanAccessExportJob(state, requestedByUserId))
            return Forbid();

        if (!string.Equals(state.Status, "completed", StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = $"Job status is '{state.Status}'." });

        var payload = await _exportStore.GetPayloadAsync(jobId, ct);
        if (payload is null) return NotFound("Export payload not found or expired.");

        return File(payload, state.ContentType ?? "application/octet-stream", state.FileName ?? $"analytics-{jobId:N}.bin");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private int? GetCurrentInstitutionType()
    {
        var raw = User.FindFirst("institutionType")?.Value;
        return int.TryParse(raw, out var value) ? value : null;
    }

    private Guid GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var userId) ? userId : Guid.Empty;
    }

    private Guid? GetCurrentTenantId()
    {
        var raw = User.FindFirstValue("tenant_id")
                  ?? User.FindFirstValue("tenantId")
                  ?? User.FindFirstValue("tid");
        return Guid.TryParse(raw, out var tenantId) ? tenantId : null;
    }

    private Guid? GetCurrentCampusId()
    {
        var raw = User.FindFirstValue("campus_id")
                  ?? User.FindFirstValue("campusId")
                  ?? User.FindFirstValue("cid");
        return Guid.TryParse(raw, out var campusId) ? campusId : null;
    }

    private bool CanAccessExportJob(AnalyticsExportJobState state, Guid requestedByUserId)
    {
        if (User.IsInRole("SuperAdmin"))
            return true;

        if (state.RequestedByUserId != requestedByUserId)
            return false;

        var currentTenantId = GetCurrentTenantId();
        var currentCampusId = GetCurrentCampusId();

        if (state.RequestedByTenantId.HasValue && state.RequestedByTenantId != currentTenantId)
            return false;

        if (state.RequestedByCampusId.HasValue && state.RequestedByCampusId != currentCampusId)
            return false;

        return true;
    }

    private static bool TryParseReportType(string value, out AnalyticsExportReportType reportType)
    {
        switch ((value ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "performance":
                reportType = AnalyticsExportReportType.Performance;
                return true;
            case "attendance":
                reportType = AnalyticsExportReportType.Attendance;
                return true;
            case "top-performers":
            case "topperformers":
                reportType = AnalyticsExportReportType.TopPerformers;
                return true;
            case "performance-trends":
            case "performancetrends":
                reportType = AnalyticsExportReportType.PerformanceTrends;
                return true;
            case "comparative-summary":
            case "comparativesummary":
                reportType = AnalyticsExportReportType.ComparativeSummary;
                return true;
            default:
                reportType = default;
                return false;
        }
    }

    private IActionResult ReturnAnalyticsFile(byte[] payload, AnalyticsExportReportType reportType, AnalyticsExportFormat format)
    {
        var fileName = AnalyticsExportConventions.CreateFileName(reportType, format);
        var contentType = AnalyticsExportConventions.GetContentType(format);
        return File(payload, contentType, fileName);
    }

    private static bool TryParseExportFormat(string value, out AnalyticsExportFormat format)
    {
        switch ((value ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "pdf":
                format = AnalyticsExportFormat.Pdf;
                return true;
            case "excel":
                format = AnalyticsExportFormat.Excel;
                return true;
            default:
                format = default;
                return false;
        }
    }

    /// <summary>
    /// Faculty users are scoped to their own department. Constrained roles are auto-scoped
    /// to their institution claim when present; explicit mismatches are forbidden.
    /// </summary>
    private async Task<(Guid? DepartmentId, int? InstitutionType, IActionResult? Error)> ResolveEffectiveScopeAsync(
        Guid? requestedDepartmentId,
        int? requestedInstitutionType,
        CancellationToken ct)
    {
        Guid? effectiveDepartmentId = requestedDepartmentId;

        var isAdminOrAbove = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
        if (!isAdminOrAbove)
        {
            var claim = User.FindFirstValue("departmentId");
            if (Guid.TryParse(claim, out var facultyDepartmentId))
                effectiveDepartmentId = facultyDepartmentId;
        }

        var callerInstitutionType = GetCurrentInstitutionType();
        var effectiveInstitutionType = requestedInstitutionType;

        if (!User.IsInRole("SuperAdmin") && callerInstitutionType.HasValue)
        {
            if (requestedInstitutionType.HasValue && requestedInstitutionType.Value != callerInstitutionType.Value)
                return (null, null, Forbid());

            effectiveInstitutionType = callerInstitutionType.Value;
        }

        if (effectiveDepartmentId.HasValue)
        {
            var department = await _departments.GetByIdAsync(effectiveDepartmentId.Value, ct);
            if (department is null)
                return (null, null, NotFound("Department not found."));

            if (effectiveInstitutionType.HasValue && (int)department.InstitutionType != effectiveInstitutionType.Value)
                return (null, null, Forbid());

            if (!effectiveInstitutionType.HasValue)
                effectiveInstitutionType = (int)department.InstitutionType;
        }

        return (effectiveDepartmentId, effectiveInstitutionType, null);
    }
}
