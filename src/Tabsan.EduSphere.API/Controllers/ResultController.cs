using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.API.Services;
using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Manages result entry, publication, Admin corrections, and transcript exports.
/// Faculty: enter and publish results for their own offerings.
/// Admins: bulk-enter, publish all, and correct results.
/// Students: view own published results and request transcript exports.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ResultController : ControllerBase
{
    private readonly IResultService _service;
    private readonly ResultPublishJobQueue _publishQueue;
    private readonly ResultPublishJobStore _publishStore;
    private readonly IAccessScopeResolver _accessScope;
    private readonly ApplicationDbContext _db;

    public ResultController(
        IResultService service,
        ResultPublishJobQueue publishQueue,
        ResultPublishJobStore publishStore,
        IAccessScopeResolver accessScope,
        ApplicationDbContext db)
    {
        _service = service;
        _publishQueue = publishQueue;
        _publishStore = publishStore;
        _accessScope = accessScope;
        _db = db;
    }

    // ── Result entry ──────────────────────────────────────────────────────────

    /// <summary>Creates a single draft result entry (Faculty/Admin).</summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Create(
        [FromBody] CreateResultRequest request,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        try
        {
            var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, request.CourseOfferingId, request.StudentProfileId, ct);
            if (scope.Error is not null)
                return scope.Error;

            var result = await _service.CreateAsync(request, ct);
            return Ok(result);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    /// <summary>Bulk-creates draft result entries for an entire class in one call (Faculty/Admin).</summary>
    [HttpPost("bulk")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> BulkCreate(
        [FromBody] BulkCreateResultsRequest request,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        foreach (var item in request.Results)
        {
            var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, item.CourseOfferingId, item.StudentProfileId, ct);
            if (scope.Error is not null)
                return scope.Error;
        }

        var count = await _service.BulkCreateAsync(request, ct);
        return Ok(new { inserted = count });
    }

    // ── Publication ───────────────────────────────────────────────────────────

    /// <summary>Publishes a single result, making it visible to the student (Faculty/Admin).</summary>
    [HttpPost("publish")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Publish(
        [FromQuery] Guid studentProfileId,
        [FromQuery] Guid courseOfferingId,
        [FromQuery] string resultType,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, courseOfferingId, studentProfileId, ct);
        if (scope.Error is not null)
            return scope.Error;

        var ok = await _service.PublishAsync(studentProfileId, courseOfferingId, resultType, userId, ct);
        return ok ? NoContent() : BadRequest("Result not found or already published.");
    }

    /// <summary>Publishes all draft results for a course offering in one batch (Faculty/Admin).</summary>
    [HttpPost("publish-all")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> PublishAll(
        [FromQuery] Guid courseOfferingId,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, courseOfferingId, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        var count = await _service.PublishAllForOfferingAsync(courseOfferingId, userId, ct);
        return Ok(new { published = count });
    }

    /// <summary>
    /// Queues a publish-all operation for asynchronous execution.
    /// Use the returned job ID to poll status.
    /// </summary>
    [HttpPost("publish-all/jobs")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> QueuePublishAll([FromQuery] Guid courseOfferingId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();
        if (courseOfferingId == Guid.Empty) return BadRequest("courseOfferingId is required.");

        var scope = await ResolveEffectiveScopeAsync(null, null, courseOfferingId, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        var jobId = Guid.NewGuid();
        await _publishStore.SetAsync(new ResultPublishJobState
        {
            JobId = jobId,
            CourseOfferingId = courseOfferingId,
            RequestedByUserId = userId,
            Status = "queued"
        }, ct);

        _publishQueue.Enqueue(new ResultPublishJobWorkItem(jobId, courseOfferingId, userId));
        return Accepted(new
        {
            jobId,
            status = "queued",
            statusUrl = $"/api/v1/result/publish-all/jobs/{jobId}"
        });
    }

    /// <summary>Returns status for an asynchronous publish-all job.</summary>
    [HttpGet("publish-all/jobs/{jobId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetPublishAllJob(Guid jobId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var state = await _publishStore.GetAsync(jobId, ct);
        if (state is null) return NotFound();
        if (state.RequestedByUserId != userId && !User.IsInRole("SuperAdmin") && !User.IsInRole("Admin"))
            return Forbid();

        return Ok(state);
    }

    // ── Admin correction ──────────────────────────────────────────────────────

    /// <summary>
    /// Corrects an already-published result (Admin only).
    /// Creates an audit trail before applying changes.
    /// </summary>
    [HttpPut("correct")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Correct(
        [FromQuery] Guid studentProfileId,
        [FromQuery] Guid courseOfferingId,
        [FromQuery] string resultType,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromBody] CorrectResultRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, courseOfferingId, studentProfileId, ct);
        if (scope.Error is not null)
            return scope.Error;

        try
        {
            var ok = await _service.CorrectAsync(studentProfileId, courseOfferingId, resultType, request, userId, ct);
            return ok ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Returns all published results for the current student (Student view).</summary>
    [HttpGet("my-results")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyResults(
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        var studentProfileId = GetCurrentStudentProfileId();
        if (studentProfileId == Guid.Empty) return Unauthorized();

        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, studentProfileId, ct);
        if (scope.Error is not null)
            return scope.Error;

        var results = await _service.GetPublishedByStudentAsync(studentProfileId, ct);
        results = await FilterResultsByScopeAsync(results, scope.TenantId, scope.CampusId, ct);
        return Ok(results);
    }

    /// <summary>Returns all results (draft + published) for a student (Faculty/Admin view).</summary>
    [HttpGet("by-student/{studentProfileId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetByStudent(
        Guid studentProfileId,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, studentProfileId, ct);
        if (scope.Error is not null)
            return scope.Error;

        var results = await _service.GetByStudentAsync(studentProfileId, ct);
        results = await FilterResultsByScopeAsync(results, scope.TenantId, scope.CampusId, ct);
        return Ok(results);
    }

    /// <summary>Returns all results for a course offering (Faculty/Admin view).</summary>
    [HttpGet("by-offering/{courseOfferingId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetByOffering(
        Guid courseOfferingId,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, courseOfferingId, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        var results = await _service.GetByOfferingAsync(courseOfferingId, ct);
        results = await FilterResultsByScopeAsync(results, scope.TenantId, scope.CampusId, ct);
        return Ok(results);
    }

    // ── Transcript ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all published results for a student as a transcript payload.
    /// Logs the export in TranscriptExportLog and AuditLog.
    /// </summary>
    [HttpGet("transcript/{studentProfileId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty,Student")]
    public async Task<IActionResult> GetTranscript(Guid studentProfileId, [FromQuery] string format = "PDF", CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var request = new TranscriptExportRequest(studentProfileId, format.ToUpperInvariant());
        var (results, logId) = await _service.ExportTranscriptAsync(request, userId, ip, ct);
        return Ok(new { logId, results });
    }

    /// <summary>Returns a student's transcript export history (Admin/Faculty).</summary>
    [HttpGet("transcript/{studentProfileId:guid}/history")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetTranscriptHistory(Guid studentProfileId, CancellationToken ct)
    {
        var logs = await _service.GetExportHistoryAsync(studentProfileId, ct);
        return Ok(logs);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Extracts the authenticated user's ID from JWT claims.</summary>
    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// Extracts the student profile ID from the "studentProfileId" JWT claim.
    /// Students must have this claim populated during login.
    /// </summary>
    private Guid GetCurrentStudentProfileId()
    {
        var claim = User.FindFirst("studentProfileId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private async Task<(Guid? TenantId, Guid? CampusId, IActionResult? Error)> ResolveEffectiveScopeAsync(
        Guid? requestedTenantId,
        Guid? requestedCampusId,
        Guid? requestedCourseOfferingId,
        Guid? requestedStudentProfileId,
        CancellationToken ct)
    {
        Guid? effectiveTenantId = requestedTenantId;
        Guid? effectiveCampusId = requestedCampusId;

        if (!_accessScope.IsSuperAdmin())
        {
            var callerTenantId = _accessScope.GetTenantId();
            var callerCampusId = _accessScope.GetCampusId();

            if (callerTenantId.HasValue)
            {
                if (requestedTenantId.HasValue && requestedTenantId.Value != callerTenantId.Value)
                    return (null, null, Forbid());

                effectiveTenantId = callerTenantId;
            }

            if (callerCampusId.HasValue)
            {
                if (requestedCampusId.HasValue && requestedCampusId.Value != callerCampusId.Value)
                    return (null, null, Forbid());

                effectiveCampusId = callerCampusId;
            }
        }

        if (effectiveTenantId.HasValue != effectiveCampusId.HasValue)
        {
            return (null, null, BadRequest(new { message = "TenantId and CampusId must be provided together." }));
        }

        if (requestedCourseOfferingId.HasValue)
        {
            var offeringScope = await _db.CourseOfferings
                .AsNoTracking()
                .Where(o => o.Id == requestedCourseOfferingId.Value)
                .Select(o => new { o.TenantId, o.CampusId })
                .FirstOrDefaultAsync(ct);

            if (offeringScope is null)
                return (null, null, NotFound(new { message = $"Course offering {requestedCourseOfferingId.Value} not found." }));

            if (effectiveTenantId.HasValue && offeringScope.TenantId != effectiveTenantId.Value)
                return (null, null, Forbid());

            if (effectiveCampusId.HasValue && offeringScope.CampusId != effectiveCampusId.Value)
                return (null, null, Forbid());

            effectiveTenantId ??= offeringScope.TenantId;
            effectiveCampusId ??= offeringScope.CampusId;
        }

        if (requestedStudentProfileId.HasValue)
        {
            var studentScope = await _db.StudentProfiles
                .AsNoTracking()
                .Where(s => s.Id == requestedStudentProfileId.Value)
                .Select(s => new { s.Department.TenantId, s.Department.CampusId })
                .FirstOrDefaultAsync(ct);

            if (studentScope is null)
                return (null, null, NotFound(new { message = $"Student profile {requestedStudentProfileId.Value} not found." }));

            if (effectiveTenantId.HasValue && studentScope.TenantId != effectiveTenantId.Value)
                return (null, null, Forbid());

            if (effectiveCampusId.HasValue && studentScope.CampusId != effectiveCampusId.Value)
                return (null, null, Forbid());

            effectiveTenantId ??= studentScope.TenantId;
            effectiveCampusId ??= studentScope.CampusId;
        }

        return (effectiveTenantId, effectiveCampusId, null);
    }

    private async Task<IReadOnlyList<ResultResponse>> FilterResultsByScopeAsync(
        IReadOnlyList<ResultResponse> results,
        Guid? tenantId,
        Guid? campusId,
        CancellationToken ct)
    {
        if (!tenantId.HasValue || !campusId.HasValue || results.Count == 0)
            return results;

        var offeringIds = results.Select(r => r.CourseOfferingId).Distinct().ToList();

        var allowedOfferingIds = await _db.CourseOfferings
            .AsNoTracking()
            .Where(o => offeringIds.Contains(o.Id) && o.TenantId == tenantId.Value && o.CampusId == campusId.Value)
            .Select(o => o.Id)
            .ToListAsync(ct);

        var allowed = allowedOfferingIds.ToHashSet();
        return results.Where(r => allowed.Contains(r.CourseOfferingId)).ToList();
    }
}
