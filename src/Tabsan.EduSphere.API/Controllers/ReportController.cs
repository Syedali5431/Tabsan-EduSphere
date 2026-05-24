using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.API.Services;
using Tabsan.EduSphere.Application.DTOs.Reports;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Settings;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Role-gated report data and Excel export endpoints for Phase 12 Reporting.
/// Catalog is accessible to all authenticated roles.
/// Data and export endpoints require Admin or Faculty.
/// </summary>
[ApiController]
[Route("api/v1/reports")]
[Authorize]
public sealed class ReportController : ControllerBase
{
    private readonly IReportService _reports;
    private readonly ICourseRepository _courses;
    private readonly IDepartmentRepository _departments;
    private readonly IAdminAssignmentRepository _adminAssignments;
    private readonly IFacultyAssignmentRepository _facultyAssignments;
    private readonly IAccessScopeResolver _accessScope;
    private readonly ApplicationDbContext _db;
    private readonly ReportExportJobQueue _exportQueue;
    private readonly ReportExportJobStore _exportStore;

    public ReportController(
        IReportService reports,
        ICourseRepository courses,
        IDepartmentRepository departments,
        IAdminAssignmentRepository adminAssignments,
        IFacultyAssignmentRepository facultyAssignments,
        IAccessScopeResolver accessScope,
        ApplicationDbContext db,
        ReportExportJobQueue exportQueue,
        ReportExportJobStore exportStore)
    {
        _reports = reports;
        _courses = courses;
        _departments = departments;
        _adminAssignments = adminAssignments;
        _facultyAssignments = facultyAssignments;
        _accessScope = accessScope;
        _db = db;
        _exportQueue = exportQueue;
        _exportStore = exportStore;
    }

    [HttpGet("status")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetReportsStatus([FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scoped = ResolveRequestedScope(tenantId, campusId);
        if (scoped.ErrorResult is not null) return scoped.ErrorResult;

        var isActive = await IsReportsScopeActiveAsync(scoped.TenantId, scoped.CampusId, ct);
        return Ok(new { isActive, tenantId = scoped.TenantId, campusId = scoped.CampusId });
    }

    [HttpPost("activate")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ActivateReports([FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scoped = ResolveRequestedScope(tenantId, campusId);
        if (scoped.ErrorResult is not null) return scoped.ErrorResult;

        await SetReportsScopeActiveAsync(scoped.TenantId, scoped.CampusId, true, ct);
        return NoContent();
    }

    [HttpPost("deactivate")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeactivateReports([FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scoped = ResolveRequestedScope(tenantId, campusId);
        if (scoped.ErrorResult is not null) return scoped.ErrorResult;

        await SetReportsScopeActiveAsync(scoped.TenantId, scoped.CampusId, false, ct);
        return NoContent();
    }

    // ── Catalog ────────────────────────────────────────────────────────────────

    /// <summary>Returns all active reports the caller's role is permitted to view.</summary>
    [HttpGet]
    public async Task<IActionResult> GetCatalog(CancellationToken ct)
    {
        var role = GetCurrentUserRole();
        if (role is null) return Unauthorized();
        var result = await _reports.GetCatalogAsync(role, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns institution-specific report sections for the caller.
    /// SuperAdmin may override the institution context via query.
    /// </summary>
    [HttpGet("sections")]
    public async Task<IActionResult> GetInstitutionReportSections(
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var role = GetCurrentUserRole();
        if (role is null) return Unauthorized();

        var scope = await ResolveEffectiveReportScopeAsync(institutionType, null, null, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var effectiveInstitutionType = scope.InstitutionType ?? (int)InstitutionType.University;
        var catalog = await _reports.GetCatalogAsync(role, ct);
        var response = BuildInstitutionSections(catalog, effectiveInstitutionType);
        return Ok(response);
    }

    // ── Attendance Summary ─────────────────────────────────────────────────────

    /// <summary>Returns attendance summary data with optional filters.</summary>
    [HttpGet("attendance-summary")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetAttendanceSummary(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseOfferingId,
        [FromQuery] Guid? studentProfileId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, courseOfferingId, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, courseOfferingId, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyOfferingScopeAsync(courseOfferingId, ct);
        if (scoped is not null) return scoped;

        var request = new AttendanceSummaryRequest(semesterId, scope.DepartmentId, courseOfferingId, studentProfileId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var result = await _reports.GetAttendanceSummaryAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Downloads attendance summary as an Excel (.xlsx) file.</summary>
    [HttpGet("attendance-summary/export")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> ExportAttendanceSummary(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseOfferingId,
        [FromQuery] Guid? studentProfileId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, courseOfferingId, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, courseOfferingId, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyOfferingScopeAsync(courseOfferingId, ct);
        if (scoped is not null) return scoped;

        var request = new AttendanceSummaryRequest(semesterId, scope.DepartmentId, courseOfferingId, studentProfileId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var bytes = await _reports.ExportAttendanceSummaryExcelAsync(request, ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "attendance-summary.xlsx");
    }

    /// <summary>Downloads attendance summary as CSV.</summary>
    [HttpGet("attendance-summary/export/csv")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> ExportAttendanceSummaryCsv(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseOfferingId,
        [FromQuery] Guid? studentProfileId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, courseOfferingId, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, courseOfferingId, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyOfferingScopeAsync(courseOfferingId, ct);
        if (scoped is not null) return scoped;

        var request = new AttendanceSummaryRequest(semesterId, scope.DepartmentId, courseOfferingId, studentProfileId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var bytes = await _reports.ExportAttendanceSummaryCsvAsync(request, ct);
        return File(bytes, "text/csv", "attendance-summary.csv");
    }

    /// <summary>Downloads attendance summary as PDF.</summary>
    [HttpGet("attendance-summary/export/pdf")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> ExportAttendanceSummaryPdf(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseOfferingId,
        [FromQuery] Guid? studentProfileId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, courseOfferingId, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, courseOfferingId, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyOfferingScopeAsync(courseOfferingId, ct);
        if (scoped is not null) return scoped;

        var request = new AttendanceSummaryRequest(semesterId, scope.DepartmentId, courseOfferingId, studentProfileId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var bytes = await _reports.ExportAttendanceSummaryPdfAsync(request, ct);
        return File(bytes, "application/pdf", "attendance-summary.pdf");
    }

    // ── Result Summary ─────────────────────────────────────────────────────────

    /// <summary>Returns published result data with optional filters.</summary>
    [HttpGet("result-summary")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetResultSummary(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseOfferingId,
        [FromQuery] Guid? studentProfileId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, courseOfferingId, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, courseOfferingId, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyOfferingScopeAsync(courseOfferingId, ct);
        if (scoped is not null) return scoped;

        var request = new ResultSummaryRequest(semesterId, scope.DepartmentId, courseOfferingId, studentProfileId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var result = await _reports.GetResultSummaryAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Downloads result summary as an Excel file.</summary>
    [HttpGet("result-summary/export")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> ExportResultSummary(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseOfferingId,
        [FromQuery] Guid? studentProfileId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, courseOfferingId, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, courseOfferingId, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyOfferingScopeAsync(courseOfferingId, ct);
        if (scoped is not null) return scoped;

        var request = new ResultSummaryRequest(semesterId, scope.DepartmentId, courseOfferingId, studentProfileId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var bytes = await _reports.ExportResultSummaryExcelAsync(request, ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "result-summary.xlsx");
    }

    /// <summary>Downloads result summary as CSV.</summary>
    [HttpGet("result-summary/export/csv")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> ExportResultSummaryCsv(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseOfferingId,
        [FromQuery] Guid? studentProfileId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, courseOfferingId, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, courseOfferingId, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyOfferingScopeAsync(courseOfferingId, ct);
        if (scoped is not null) return scoped;

        var request = new ResultSummaryRequest(semesterId, scope.DepartmentId, courseOfferingId, studentProfileId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var bytes = await _reports.ExportResultSummaryCsvAsync(request, ct);
        return File(bytes, "text/csv", "result-summary.csv");
    }

    /// <summary>Downloads result summary as PDF.</summary>
    [HttpGet("result-summary/export/pdf")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> ExportResultSummaryPdf(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseOfferingId,
        [FromQuery] Guid? studentProfileId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, courseOfferingId, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, courseOfferingId, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyOfferingScopeAsync(courseOfferingId, ct);
        if (scoped is not null) return scoped;

        var request = new ResultSummaryRequest(semesterId, scope.DepartmentId, courseOfferingId, studentProfileId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var bytes = await _reports.ExportResultSummaryPdfAsync(request, ct);
        return File(bytes, "application/pdf", "result-summary.pdf");
    }

    /// <summary>
    /// Queues result summary export generation for background processing.
    /// Supported formats: excel, csv, pdf.
    /// </summary>
    [HttpPost("result-summary/export-jobs")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> QueueResultSummaryExport(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseOfferingId,
        [FromQuery] Guid? studentProfileId,
        [FromQuery] int? institutionType,
        [FromQuery] string format = "excel",
        CancellationToken ct = default)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, courseOfferingId, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, courseOfferingId, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyOfferingScopeAsync(courseOfferingId, ct);
        if (scoped is not null) return scoped;

        var requestedByUserId = GetCurrentUserId();
        if (requestedByUserId == Guid.Empty) return Unauthorized();

        if (!TryParseFormat(format, out var exportFormat))
            return BadRequest("format must be one of: excel, csv, pdf.");

        var jobId = Guid.NewGuid();
        await _exportStore.SetStateAsync(new ReportExportJobState
        {
            JobId = jobId,
            RequestedByUserId = requestedByUserId,
            Status = "queued"
        }, ct);

        _exportQueue.Enqueue(new ResultSummaryExportJobRequest(
            jobId,
            requestedByUserId,
            semesterId,
            scope.DepartmentId,
            courseOfferingId,
            studentProfileId,
            scope.InstitutionType,
            exportFormat));

        return Accepted(new
        {
            jobId,
            status = "queued",
            statusUrl = $"/api/v1/reports/export-jobs/{jobId}",
            downloadUrl = $"/api/v1/reports/export-jobs/{jobId}/download"
        });
    }

    /// <summary>Returns status for a queued report export job.</summary>
    [HttpGet("export-jobs/{jobId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetExportJob(Guid jobId, CancellationToken ct)
    {
        var requestedByUserId = GetCurrentUserId();
        if (requestedByUserId == Guid.Empty) return Unauthorized();

        var state = await _exportStore.GetStateAsync(jobId, ct);
        if (state is null) return NotFound();

        if (state.RequestedByUserId != requestedByUserId && !User.IsInRole("SuperAdmin") && !User.IsInRole("Admin"))
            return Forbid();

        return Ok(state);
    }

    /// <summary>Downloads the completed payload for a queued report export job.</summary>
    [HttpGet("export-jobs/{jobId:guid}/download")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> DownloadExportJob(Guid jobId, CancellationToken ct)
    {
        var requestedByUserId = GetCurrentUserId();
        if (requestedByUserId == Guid.Empty) return Unauthorized();

        var state = await _exportStore.GetStateAsync(jobId, ct);
        if (state is null) return NotFound();

        if (state.RequestedByUserId != requestedByUserId && !User.IsInRole("SuperAdmin") && !User.IsInRole("Admin"))
            return Forbid();

        if (!string.Equals(state.Status, "completed", StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = $"Job status is '{state.Status}'." });

        var payload = await _exportStore.GetPayloadAsync(jobId, ct);
        if (payload is null) return NotFound("Export payload not found or expired.");

        return File(payload, state.ContentType ?? "application/octet-stream", state.FileName ?? $"report-{jobId:N}.bin");
    }

    // ── Assignment Summary ───────────────────────────────────────────────────

    /// <summary>Returns assignment submission data with optional filters.</summary>
    [HttpGet("assignment-summary")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetAssignmentSummary(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseOfferingId,
        [FromQuery] Guid? studentProfileId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, courseOfferingId, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, courseOfferingId, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyOfferingScopeAsync(courseOfferingId, ct);
        if (scoped is not null) return scoped;

        var request = new AssignmentSummaryRequest(semesterId, scope.DepartmentId, courseOfferingId, studentProfileId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var result = await _reports.GetAssignmentSummaryAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Downloads assignment summary as an Excel file.</summary>
    [HttpGet("assignment-summary/export")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> ExportAssignmentSummary(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseOfferingId,
        [FromQuery] Guid? studentProfileId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, courseOfferingId, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, courseOfferingId, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyOfferingScopeAsync(courseOfferingId, ct);
        if (scoped is not null) return scoped;

        var request = new AssignmentSummaryRequest(semesterId, scope.DepartmentId, courseOfferingId, studentProfileId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var bytes = await _reports.ExportAssignmentSummaryExcelAsync(request, ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "assignment-summary.xlsx");
    }

    /// <summary>Downloads assignment summary as CSV.</summary>
    [HttpGet("assignment-summary/export/csv")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> ExportAssignmentSummaryCsv(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseOfferingId,
        [FromQuery] Guid? studentProfileId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, courseOfferingId, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, courseOfferingId, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyOfferingScopeAsync(courseOfferingId, ct);
        if (scoped is not null) return scoped;

        var request = new AssignmentSummaryRequest(semesterId, scope.DepartmentId, courseOfferingId, studentProfileId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var bytes = await _reports.ExportAssignmentSummaryCsvAsync(request, ct);
        return File(bytes, "text/csv", "assignment-summary.csv");
    }

    /// <summary>Downloads assignment summary as PDF.</summary>
    [HttpGet("assignment-summary/export/pdf")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> ExportAssignmentSummaryPdf(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseOfferingId,
        [FromQuery] Guid? studentProfileId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, courseOfferingId, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, courseOfferingId, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyOfferingScopeAsync(courseOfferingId, ct);
        if (scoped is not null) return scoped;

        var request = new AssignmentSummaryRequest(semesterId, scope.DepartmentId, courseOfferingId, studentProfileId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var bytes = await _reports.ExportAssignmentSummaryPdfAsync(request, ct);
        return File(bytes, "application/pdf", "assignment-summary.pdf");
    }

    // ── Quiz Summary ─────────────────────────────────────────────────────────

    /// <summary>Returns quiz attempt data with optional filters.</summary>
    [HttpGet("quiz-summary")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetQuizSummary(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseOfferingId,
        [FromQuery] Guid? studentProfileId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, courseOfferingId, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, courseOfferingId, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyOfferingScopeAsync(courseOfferingId, ct);
        if (scoped is not null) return scoped;

        var request = new QuizSummaryRequest(semesterId, scope.DepartmentId, courseOfferingId, studentProfileId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var result = await _reports.GetQuizSummaryAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Downloads quiz summary as an Excel file.</summary>
    [HttpGet("quiz-summary/export")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> ExportQuizSummary(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseOfferingId,
        [FromQuery] Guid? studentProfileId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, courseOfferingId, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, courseOfferingId, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyOfferingScopeAsync(courseOfferingId, ct);
        if (scoped is not null) return scoped;

        var request = new QuizSummaryRequest(semesterId, scope.DepartmentId, courseOfferingId, studentProfileId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var bytes = await _reports.ExportQuizSummaryExcelAsync(request, ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "quiz-summary.xlsx");
    }

    /// <summary>Downloads quiz summary as CSV.</summary>
    [HttpGet("quiz-summary/export/csv")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> ExportQuizSummaryCsv(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseOfferingId,
        [FromQuery] Guid? studentProfileId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, courseOfferingId, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, courseOfferingId, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyOfferingScopeAsync(courseOfferingId, ct);
        if (scoped is not null) return scoped;

        var request = new QuizSummaryRequest(semesterId, scope.DepartmentId, courseOfferingId, studentProfileId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var bytes = await _reports.ExportQuizSummaryCsvAsync(request, ct);
        return File(bytes, "text/csv", "quiz-summary.csv");
    }

    /// <summary>Downloads quiz summary as PDF.</summary>
    [HttpGet("quiz-summary/export/pdf")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> ExportQuizSummaryPdf(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseOfferingId,
        [FromQuery] Guid? studentProfileId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, courseOfferingId, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, courseOfferingId, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyOfferingScopeAsync(courseOfferingId, ct);
        if (scoped is not null) return scoped;

        var request = new QuizSummaryRequest(semesterId, scope.DepartmentId, courseOfferingId, studentProfileId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var bytes = await _reports.ExportQuizSummaryPdfAsync(request, ct);
        return File(bytes, "application/pdf", "quiz-summary.pdf");
    }

    // ── GPA Report ─────────────────────────────────────────────────────────────

    /// <summary>Returns per-student GPA and CGPA data.</summary>
    [HttpGet("gpa-report")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetGpaReport(
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? programId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, null, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, null, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyDepartmentScopeAsync(departmentId, ct);
        if (scoped is not null) return scoped;

        var request = new GpaReportRequest(scope.DepartmentId, programId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var result = await _reports.GetGpaReportAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Downloads GPA report as an Excel file.</summary>
    [HttpGet("gpa-report/export")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> ExportGpaReport(
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? programId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, null, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, null, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyDepartmentScopeAsync(departmentId, ct);
        if (scoped is not null) return scoped;

        var request = new GpaReportRequest(scope.DepartmentId, programId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var bytes = await _reports.ExportGpaReportExcelAsync(request, ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "gpa-report.xlsx");
    }

    // ── Enrollment Summary ─────────────────────────────────────────────────────

    /// <summary>Returns course offering enrollment utilisation data.</summary>
    [HttpGet("enrollment-summary")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetEnrollmentSummary(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, null, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, null, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyDepartmentScopeAsync(departmentId, ct);
        if (scoped is not null) return scoped;

        var request = new EnrollmentSummaryRequest(semesterId, scope.DepartmentId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var result = await _reports.GetEnrollmentSummaryAsync(request, ct);
        return Ok(result);
    }

    // ── Semester Results ───────────────────────────────────────────────────────

    /// <summary>Returns all published results for a specific semester.</summary>
    [HttpGet("semester-results")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetSemesterResults(
        [FromQuery] Guid semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        if (semesterId == Guid.Empty)
            return BadRequest("semesterId is required.");

        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, null, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, null, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyDepartmentScopeAsync(departmentId, ct);
        if (scoped is not null) return scoped;

        var request = new SemesterResultsRequest(semesterId, scope.DepartmentId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var result = await _reports.GetSemesterResultsAsync(request, ct);
        return Ok(result);
    }

    // ── Stage 4.2: Student Transcript ─────────────────────────────────────────

    /// <summary>Returns all published results for a single student (transcript).</summary>
    [HttpGet("student-transcript")]
    [Authorize]
    public async Task<IActionResult> GetStudentTranscript(
        [FromQuery] Guid studentProfileId,
        CancellationToken ct)
    {
        if (studentProfileId == Guid.Empty)
            return BadRequest("studentProfileId is required.");

        var request = new TranscriptRequest(studentProfileId);
        var result = await _reports.GetStudentTranscriptAsync(request, ct);
        if (result is null) return NotFound("Student not found.");
        return Ok(result);
    }

    /// <summary>Downloads student transcript as an Excel file.</summary>
    [HttpGet("student-transcript/export")]
    [Authorize]
    public async Task<IActionResult> ExportStudentTranscript(
        [FromQuery] Guid studentProfileId,
        CancellationToken ct)
    {
        if (studentProfileId == Guid.Empty)
            return BadRequest("studentProfileId is required.");

        var request = new TranscriptRequest(studentProfileId);
        var bytes = await _reports.ExportTranscriptExcelAsync(request, ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "student-transcript.xlsx");
    }

    // ── Stage 4.2: Low Attendance Warning ─────────────────────────────────────

    /// <summary>Returns students whose attendance is below the given threshold.</summary>
    [HttpGet("low-attendance")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetLowAttendanceWarning(
        [FromQuery] decimal threshold = 75m,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? courseOfferingId = null,
        [FromQuery] int? institutionType = null,
        CancellationToken ct = default)
    {
        threshold = 75m;

        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, courseOfferingId, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, courseOfferingId, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyDepartmentOrOfferingScopeAsync(departmentId, courseOfferingId, ct);
        if (scoped is not null) return scoped;

        var request = new LowAttendanceRequest(threshold, scope.DepartmentId, courseOfferingId, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var result = await _reports.GetLowAttendanceWarningAsync(request, ct);
        return Ok(result);
    }

    // ── Stage 4.2: FYP Status Report ──────────────────────────────────────────

    /// <summary>Returns all FYP project rows, optionally filtered by department and status.</summary>
    [HttpGet("fyp-status")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetFypStatusReport(
        [FromQuery] Guid? departmentId,
        [FromQuery] string? status,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, null, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, null, ct);
        if (scoped is not null) return scoped;

        scoped = await EnforceFacultyDepartmentScopeAsync(departmentId, ct);
        if (scoped is not null) return scoped;

        var request = new FypStatusRequest(scope.DepartmentId, status, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var result = await _reports.GetFypStatusReportAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Returns finance payment receipts with optional calendar and academic filters.</summary>
    [HttpGet("payment-summary")]
    [Authorize(Roles = "SuperAdmin,Admin,Finance")]
    public async Task<IActionResult> GetPaymentSummary(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseId,
        [FromQuery] int? levelNumber,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        if (month is < 1 or > 12)
            return BadRequest("month must be between 1 and 12.");

        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, null, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, null, ct);
        if (scoped is not null) return scoped;

        var request = new PaymentSummaryRequest(
            year,
            month,
            semesterId,
            scope.DepartmentId,
            courseId,
            levelNumber,
            scope.InstitutionType,
            GetCurrentTenantId(),
            GetCurrentCampusId());

        var result = await _reports.GetPaymentSummaryAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("payment-summary/export")]
    [Authorize(Roles = "SuperAdmin,Admin,Finance")]
    public async Task<IActionResult> ExportPaymentSummary(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseId,
        [FromQuery] int? levelNumber,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        if (month is < 1 or > 12)
            return BadRequest("month must be between 1 and 12.");

        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, null, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, null, ct);
        if (scoped is not null) return scoped;

        var request = new PaymentSummaryRequest(year, month, semesterId, scope.DepartmentId, courseId, levelNumber, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var bytes = await _reports.ExportPaymentSummaryExcelAsync(request, ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "payment-summary.xlsx");
    }

    [HttpGet("payment-summary/export/csv")]
    [Authorize(Roles = "SuperAdmin,Admin,Finance")]
    public async Task<IActionResult> ExportPaymentSummaryCsv(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseId,
        [FromQuery] int? levelNumber,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        if (month is < 1 or > 12)
            return BadRequest("month must be between 1 and 12.");

        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, null, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, null, ct);
        if (scoped is not null) return scoped;

        var request = new PaymentSummaryRequest(year, month, semesterId, scope.DepartmentId, courseId, levelNumber, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var bytes = await _reports.ExportPaymentSummaryCsvAsync(request, ct);
        return File(bytes, "text/csv", "payment-summary.csv");
    }

    [HttpGet("payment-summary/export/pdf")]
    [Authorize(Roles = "SuperAdmin,Admin,Finance")]
    public async Task<IActionResult> ExportPaymentSummaryPdf(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseId,
        [FromQuery] int? levelNumber,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        if (month is < 1 or > 12)
            return BadRequest("month must be between 1 and 12.");

        var scope = await ResolveEffectiveReportScopeAsync(institutionType, departmentId, null, ct);
        if (scope.ErrorResult is not null) return scope.ErrorResult;

        var scoped = await EnforceAdminDepartmentScopeAsync(departmentId, null, ct);
        if (scoped is not null) return scoped;

        var request = new PaymentSummaryRequest(year, month, semesterId, scope.DepartmentId, courseId, levelNumber, scope.InstitutionType, GetCurrentTenantId(), GetCurrentCampusId());
        var bytes = await _reports.ExportPaymentSummaryPdfAsync(request, ct);
        return File(bytes, "application/pdf", "payment-summary.pdf");
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private string? GetCurrentUserRole() =>
        User.FindFirstValue(ClaimTypes.Role);

    private Guid GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    private int? GetCurrentInstitutionType()
    {
        var raw = User.FindFirst("institutionType")?.Value;
        return int.TryParse(raw, out var value) ? value : null;
    }

    private Guid? GetCurrentTenantId()
    {
        var raw = User.FindFirstValue("tenant_id")
                  ?? User.FindFirstValue("tenantId")
                  ?? User.FindFirstValue("tid");
        return Guid.TryParse(raw, out var value) ? value : null;
    }

    private Guid? GetCurrentCampusId()
    {
        var raw = User.FindFirstValue("campus_id")
                  ?? User.FindFirstValue("campusId")
                  ?? User.FindFirstValue("cid");
        return Guid.TryParse(raw, out var value) ? value : null;
    }

    private async Task<IActionResult?> EnforceInstitutionTypeDepartmentScopeAsync(Guid departmentId, CancellationToken ct)
    {
        if (User.IsInRole("SuperAdmin"))
            return null;

        var callerInstitutionType = GetCurrentInstitutionType();
        if (!callerInstitutionType.HasValue)
            return null;

        var department = await _departments.GetByIdAsync(departmentId, ct);
        if (department is null)
            return NotFound("Department not found.");

        return (int)department.InstitutionType == callerInstitutionType.Value ? null : Forbid();
    }

    private async Task<(Guid? DepartmentId, int? InstitutionType, IActionResult? ErrorResult)> ResolveEffectiveReportScopeAsync(
        int? requestedInstitutionType,
        Guid? requestedDepartmentId,
        Guid? requestedCourseOfferingId,
        CancellationToken ct)
    {
        var effectiveInstitutionType = requestedInstitutionType;

        if (!User.IsInRole("SuperAdmin"))
        {
            var callerInstitutionType = GetCurrentInstitutionType();
            if (callerInstitutionType.HasValue)
            {
                if (requestedInstitutionType.HasValue && requestedInstitutionType.Value != callerInstitutionType.Value)
                    return (requestedDepartmentId, null, Forbid());

                effectiveInstitutionType = callerInstitutionType.Value;
            }
        }

        if (requestedDepartmentId.HasValue)
        {
            var department = await _departments.GetByIdAsync(requestedDepartmentId.Value, ct);
            if (department is null)
                return (requestedDepartmentId, effectiveInstitutionType, NotFound("Department not found."));

            if (effectiveInstitutionType.HasValue && (int)department.InstitutionType != effectiveInstitutionType.Value)
                return (requestedDepartmentId, effectiveInstitutionType, Forbid());
        }

        if (requestedCourseOfferingId.HasValue && requestedCourseOfferingId.Value != Guid.Empty)
        {
            var offering = await _courses.GetOfferingByIdAsync(requestedCourseOfferingId.Value, ct);
            if (offering is null)
                return (requestedDepartmentId, effectiveInstitutionType, NotFound("Course offering not found."));

            if (requestedDepartmentId.HasValue && requestedDepartmentId.Value != offering.Course.DepartmentId)
                return (requestedDepartmentId, effectiveInstitutionType, BadRequest("departmentId does not match the selected course offering."));

            if (effectiveInstitutionType.HasValue)
            {
                var offeringDepartment = await _departments.GetByIdAsync(offering.Course.DepartmentId, ct);
                if (offeringDepartment is null)
                    return (requestedDepartmentId, effectiveInstitutionType, NotFound("Department not found."));

                if ((int)offeringDepartment.InstitutionType != effectiveInstitutionType.Value)
                    return (requestedDepartmentId, effectiveInstitutionType, Forbid());
            }
        }

        var activeCheck = await EnsureReportsScopeIsActiveAsync(GetCurrentTenantId(), GetCurrentCampusId(), ct);
        if (activeCheck is not null)
            return (requestedDepartmentId, effectiveInstitutionType, activeCheck);

        return (requestedDepartmentId, effectiveInstitutionType, null);
    }

    private (Guid? TenantId, Guid? CampusId, IActionResult? ErrorResult) ResolveRequestedScope(Guid? requestedTenantId, Guid? requestedCampusId)
    {
        var tenantId = requestedTenantId;
        var campusId = requestedCampusId;

        if (!_accessScope.IsSuperAdmin())
        {
            var callerTenantId = _accessScope.GetTenantId();
            var callerCampusId = _accessScope.GetCampusId();

            if (callerTenantId.HasValue)
            {
                if (tenantId.HasValue && tenantId.Value != callerTenantId.Value)
                    return (null, null, Forbid());

                tenantId = callerTenantId.Value;
            }

            if (callerCampusId.HasValue)
            {
                if (campusId.HasValue && campusId.Value != callerCampusId.Value)
                    return (null, null, Forbid());

                campusId = callerCampusId.Value;
            }
        }

        if (tenantId.HasValue != campusId.HasValue)
            return (null, null, BadRequest(new { message = "TenantId and CampusId must be provided together." }));

        return (tenantId, campusId, null);
    }

    private static string GetReportsScopeSettingKey(Guid? tenantId, Guid? campusId)
        => tenantId.HasValue && campusId.HasValue
            ? $"reports.active.{tenantId.Value:N}.{campusId.Value:N}"
            : "reports.active.global";

    private async Task<bool> IsReportsScopeActiveAsync(Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        var key = GetReportsScopeSettingKey(tenantId, campusId);
        var setting = await _db.PortalSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key, ct);
        if (setting is null)
            return true;

        return bool.TryParse(setting.Value, out var isActive) ? isActive : true;
    }

    private async Task SetReportsScopeActiveAsync(Guid? tenantId, Guid? campusId, bool isActive, CancellationToken ct)
    {
        var key = GetReportsScopeSettingKey(tenantId, campusId);
        var setting = await _db.PortalSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (setting is null)
        {
            setting = new PortalSetting(key, isActive.ToString().ToLowerInvariant());
            await _db.PortalSettings.AddAsync(setting, ct);
        }
        else
        {
            setting.SetValue(isActive.ToString().ToLowerInvariant());
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<IActionResult?> EnsureReportsScopeIsActiveAsync(Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        var isActive = await IsReportsScopeActiveAsync(tenantId, campusId, ct);
        if (isActive)
            return null;

        return StatusCode(StatusCodes.Status423Locked, new { message = "Reports are deactivated for the selected tenant/campus scope." });
    }

    private static bool TryParseFormat(string? format, out ReportExportFormat exportFormat)
    {
        switch (format?.Trim().ToLowerInvariant())
        {
            case "csv":
                exportFormat = ReportExportFormat.Csv;
                return true;
            case "pdf":
                exportFormat = ReportExportFormat.Pdf;
                return true;
            case "excel":
            case "xlsx":
            case null:
            case "":
                exportFormat = ReportExportFormat.Excel;
                return true;
            default:
                exportFormat = ReportExportFormat.Excel;
                return false;
        }
    }

    private async Task<IActionResult?> EnforceFacultyOfferingScopeAsync(Guid? courseOfferingId, CancellationToken ct)
    {
        if (!User.IsInRole("Faculty") || User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
            return null;

        if (!courseOfferingId.HasValue || courseOfferingId.Value == Guid.Empty)
            return BadRequest("Faculty must select a course offering for report generation.");

        var offering = await _courses.GetOfferingByIdAsync(courseOfferingId.Value, ct);
        if (offering is null) return NotFound("Course offering not found.");

        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Forbid();

        var allowedDepartmentIds = await _facultyAssignments.GetDepartmentIdsForFacultyAsync(userId, ct);
        if (allowedDepartmentIds.Count == 0)
            return Forbid();

        if (!allowedDepartmentIds.Contains(offering.Course.DepartmentId))
            return Forbid();

        var instituteScope = await EnforceInstitutionTypeDepartmentScopeAsync(offering.Course.DepartmentId, ct);
        if (instituteScope is not null)
            return instituteScope;

        return null;
    }

    private async Task<IActionResult?> EnforceAdminDepartmentScopeAsync(Guid? departmentId, Guid? courseOfferingId, CancellationToken ct)
    {
        if (!User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
            return null;

        var adminUserId = GetCurrentUserId();
        if (adminUserId == Guid.Empty)
            return Forbid();

        var allowedDepartmentIds = await _adminAssignments.GetDepartmentIdsForAdminAsync(adminUserId, ct);
        if (allowedDepartmentIds.Count == 0)
            return Forbid();

        if (departmentId.HasValue && !allowedDepartmentIds.Contains(departmentId.Value))
            return Forbid();

        if (departmentId.HasValue)
        {
            var instituteScope = await EnforceInstitutionTypeDepartmentScopeAsync(departmentId.Value, ct);
            if (instituteScope is not null)
                return instituteScope;
        }

        if (courseOfferingId.HasValue && courseOfferingId.Value != Guid.Empty)
        {
            var offering = await _courses.GetOfferingByIdAsync(courseOfferingId.Value, ct);
            if (offering is null)
                return NotFound("Course offering not found.");

            if (!allowedDepartmentIds.Contains(offering.Course.DepartmentId))
                return Forbid();

            var instituteScope = await EnforceInstitutionTypeDepartmentScopeAsync(offering.Course.DepartmentId, ct);
            if (instituteScope is not null)
                return instituteScope;
        }

        // Multi-department admin scope requires at least one explicit filter to avoid cross-dept aggregate leakage.
        if (!departmentId.HasValue && !courseOfferingId.HasValue)
            return BadRequest("Admin must select a department or course offering for report generation.");

        return null;
    }

    private async Task<IActionResult?> EnforceFacultyDepartmentScopeAsync(Guid? departmentId, CancellationToken ct)
    {
        if (!User.IsInRole("Faculty") || User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
            return null;

        if (!departmentId.HasValue)
            return BadRequest("Faculty must select a department for report generation.");

        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Forbid();

        var allowedDepartmentIds = await _facultyAssignments.GetDepartmentIdsForFacultyAsync(userId, ct);
        if (allowedDepartmentIds.Count == 0)
            return Forbid();

        if (!allowedDepartmentIds.Contains(departmentId.Value))
            return Forbid();

        var instituteScope = await EnforceInstitutionTypeDepartmentScopeAsync(departmentId.Value, ct);
        if (instituteScope is not null)
            return instituteScope;

        return null;
    }

    private async Task<IActionResult?> EnforceFacultyDepartmentOrOfferingScopeAsync(Guid? departmentId, Guid? courseOfferingId, CancellationToken ct)
    {
        if (!User.IsInRole("Faculty") || User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
            return null;

        if (!departmentId.HasValue && !courseOfferingId.HasValue)
            return BadRequest("Faculty must select a department or course offering for report generation.");

        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Forbid();

        var allowedDepartmentIds = await _facultyAssignments.GetDepartmentIdsForFacultyAsync(userId, ct);
        if (allowedDepartmentIds.Count == 0)
            return Forbid();

        if (departmentId.HasValue)
        {
            if (!allowedDepartmentIds.Contains(departmentId.Value))
                return Forbid();

            var instituteScope = await EnforceInstitutionTypeDepartmentScopeAsync(departmentId.Value, ct);
            if (instituteScope is not null)
                return instituteScope;
        }

        if (courseOfferingId.HasValue && courseOfferingId.Value != Guid.Empty)
        {
            var offering = await _courses.GetOfferingByIdAsync(courseOfferingId.Value, ct);
            if (offering is null)
                return NotFound("Course offering not found.");

            if (!allowedDepartmentIds.Contains(offering.Course.DepartmentId))
                return Forbid();

            if (departmentId.HasValue && departmentId.Value != offering.Course.DepartmentId)
                return BadRequest("departmentId does not match the selected course offering.");

            var instituteScope = await EnforceInstitutionTypeDepartmentScopeAsync(offering.Course.DepartmentId, ct);
            if (instituteScope is not null)
                return instituteScope;
        }

        return null;
    }

    private static InstitutionReportSectionsResponse BuildInstitutionSections(
        ReportCatalogResponse catalog,
        int effectiveInstitutionType)
    {
        var reportLookup = catalog.Reports.ToDictionary(r => r.Key, StringComparer.OrdinalIgnoreCase);

        static ReportSectionResponse BuildSection(
            string sectionKey,
            string sectionName,
            IReadOnlyCollection<string> reportKeys,
            IDictionary<string, ReportCatalogItemResponse> lookup)
        {
            var reports = reportKeys
                .Where(lookup.ContainsKey)
                .Select(k => lookup[k])
                .Select(r => new ReportSectionItemResponse(r.Key, r.Name, r.Purpose))
                .ToList();

            return new ReportSectionResponse(sectionKey, sectionName, reports);
        }

        var operationalKeys = new[]
        {
            ReportKeys.AttendanceSummary,
            ReportKeys.ResultSummary,
            "assignment_summary",
            "quiz_summary",
            ReportKeys.EnrollmentSummary
        };

        var complianceKeys = new[]
        {
            ReportKeys.LowAttendanceWarning,
            ReportKeys.SemesterResults
        };

        var financeKeys = new[]
        {
            ReportKeys.PaymentSummary
        };

        var institutionSpecific = (InstitutionType)effectiveInstitutionType switch
        {
            InstitutionType.School => BuildSection(
                "school_outcomes",
                "School Outcomes",
                new[]
                {
                    ReportKeys.ResultSummary,
                    ReportKeys.LowAttendanceWarning,
                    ReportKeys.StudentTranscript
                },
                reportLookup),

            InstitutionType.College => BuildSection(
                "college_progression",
                "College Progression",
                new[]
                {
                    ReportKeys.ResultSummary,
                    ReportKeys.SemesterResults,
                    ReportKeys.StudentTranscript
                },
                reportLookup),

            _ => BuildSection(
                "university_academics",
                "University Academics",
                new[]
                {
                    ReportKeys.GpaReport,
                    ReportKeys.SemesterResults,
                    ReportKeys.FypStatus,
                    ReportKeys.StudentTranscript
                },
                reportLookup)
        };

        var sections = new List<ReportSectionResponse>
        {
            BuildSection("operational", "Operational Reports", operationalKeys, reportLookup),
            BuildSection("compliance", "Compliance Reports", complianceKeys, reportLookup),
            institutionSpecific
        };

        var financeSection = BuildSection("finance", "Finance Reports", financeKeys, reportLookup);
        if (financeSection.Reports.Count > 0)
            sections.Add(financeSection);

        return new InstitutionReportSectionsResponse(
            effectiveInstitutionType,
            Enum.GetName(typeof(InstitutionType), (InstitutionType)effectiveInstitutionType) ?? InstitutionType.University.ToString(),
            sections);
    }
}
