using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Manages assignments within a course offering.
/// Faculty: create, edit, publish, retract, grade, reject submissions.
/// Students: view published assignments, submit work, view own submissions.
/// Admins: read-only access to all assignments and submissions.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AssignmentController : ControllerBase
{
    private readonly IAssignmentService _service;
    private readonly IAccessScopeResolver _accessScope;
    private readonly ApplicationDbContext _db;

    public AssignmentController(IAssignmentService service, IAccessScopeResolver accessScope, ApplicationDbContext db)
    {
        _service = service;
        _accessScope = accessScope;
        _db = db;
    }

    // ── Assignment CRUD ───────────────────────────────────────────────────────

    /// <summary>Creates a new unpublished assignment for a course offering (Faculty/Admin).</summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Create([FromBody] CreateAssignmentRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var offeringScopeResult = await EnsureOfferingIsInScopeAsync(request.CourseOfferingId, scope.TenantId, scope.CampusId, ct);
        if (offeringScopeResult is not null)
            return offeringScopeResult;

        var result = await _service.CreateAsync(request, userId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Updates a draft (unpublished) assignment (Faculty/Admin).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAssignmentRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var assignmentScopeResult = await EnsureAssignmentIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
        if (assignmentScopeResult is not null)
            return assignmentScopeResult;

        var updated = await _service.UpdateAsync(id, request, ct);
        return updated ? NoContent() : BadRequest("Assignment not found or already published.");
    }

    /// <summary>Publishes an assignment so students can see and submit it (Faculty/Admin).</summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Publish(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var assignmentScopeResult = await EnsureAssignmentIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
        if (assignmentScopeResult is not null)
            return assignmentScopeResult;

        var ok = await _service.PublishAsync(id, ct);
        return ok ? NoContent() : BadRequest("Assignment not found or already published.");
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Activate(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var assignmentScopeResult = await EnsureAssignmentIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
        if (assignmentScopeResult is not null)
            return assignmentScopeResult;

        var ok = await _service.ActivateAsync(id, ct);
        return ok ? NoContent() : BadRequest("Assignment not found or already active.");
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Deactivate(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var assignmentScopeResult = await EnsureAssignmentIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
        if (assignmentScopeResult is not null)
            return assignmentScopeResult;

        var ok = await _service.DeactivateAsync(id, ct);
        return ok ? NoContent() : BadRequest("Assignment not found or already inactive.");
    }

    /// <summary>
    /// Retracts a published assignment (Faculty/Admin).
    /// Fails when submissions already exist.
    /// </summary>
    [HttpPost("{id:guid}/retract")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Retract(Guid id, CancellationToken ct)
    {
        var ok = await _service.RetractAsync(id, ct);
        return ok ? NoContent() : BadRequest("Cannot retract — assignment has submissions or is not published.");
    }

    /// <summary>Soft-deletes an assignment (Admin only, only when no submissions exist).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var assignmentScopeResult = await EnsureAssignmentIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
        if (assignmentScopeResult is not null)
            return assignmentScopeResult;

        var ok = await _service.DeactivateAsync(id, ct);
        return ok ? NoContent() : BadRequest("Assignment not found or already inactive.");
    }

    /// <summary>Returns all assignments for a course offering.</summary>
    [HttpGet("by-offering/{courseOfferingId:guid}")]
    public async Task<IActionResult> GetByOffering(Guid courseOfferingId, [FromQuery] bool includeInactive = false, [FromQuery] Guid? tenantId = null, [FromQuery] Guid? campusId = null, CancellationToken ct = default)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var offeringScopeResult = await EnsureOfferingIsInScopeAsync(courseOfferingId, scope.TenantId, scope.CampusId, ct);
        if (offeringScopeResult is not null)
            return offeringScopeResult;

        var list = await _service.GetByOfferingAsync(courseOfferingId, includeInactive, ct);
        return Ok(list);
    }

    /// <summary>Returns a single assignment by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        var assignmentScopeResult = await EnsureAssignmentIsInScopeAsync(id, scope.TenantId, scope.CampusId, ct);
        if (assignmentScopeResult is not null)
            return assignmentScopeResult;

        var item = await _service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    // ── Student submissions ───────────────────────────────────────────────────

    /// <summary>Submits a student's work for a published assignment (Student).</summary>
    [HttpPost("submit")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Submit([FromBody] SubmitAssignmentRequest request, CancellationToken ct)
    {
        var studentProfileId = GetCurrentStudentProfileId();
        if (studentProfileId == Guid.Empty) return Unauthorized();

        var result = await _service.SubmitAsync(studentProfileId, request, ct);
        return result is null
            ? BadRequest("Submission rejected: assignment not published, past due date, or already submitted.")
            : Ok(result);
    }

    /// <summary>Returns the current student's own submissions (Student).</summary>
    [HttpGet("my-submissions")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMySubmissions(CancellationToken ct)
    {
        var studentProfileId = GetCurrentStudentProfileId();
        if (studentProfileId == Guid.Empty) return Unauthorized();

        var submissions = await _service.GetMySubmissionsAsync(studentProfileId, ct);
        return Ok(submissions);
    }

    // ── Faculty grading ───────────────────────────────────────────────────────

    /// <summary>Returns all submissions for an assignment (Faculty/Admin grading view).</summary>
    [HttpGet("{id:guid}/submissions")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetSubmissions(Guid id, CancellationToken ct)
    {
        var submissions = await _service.GetSubmissionsByAssignmentAsync(id, ct);
        return Ok(submissions);
    }

    /// <summary>Grades a student's submission (Faculty/Admin).</summary>
    [HttpPut("submissions/grade")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Grade([FromBody] GradeSubmissionRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var ok = await _service.GradeSubmissionAsync(request, userId, ct);
        return ok ? NoContent() : BadRequest("Submission not found or marks out of range.");
    }

    /// <summary>Rejects a submission (Faculty/Admin, e.g. plagiarism).</summary>
    [HttpPost("{assignmentId:guid}/submissions/{studentProfileId:guid}/reject")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Reject(Guid assignmentId, Guid studentProfileId, CancellationToken ct)
    {
        var ok = await _service.RejectSubmissionAsync(assignmentId, studentProfileId, ct);
        return ok ? NoContent() : NotFound();
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

    private (Guid? TenantId, Guid? CampusId, IActionResult? Error) ResolveEffectiveScope(Guid? requestedTenantId, Guid? requestedCampusId)
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

                effectiveTenantId = callerTenantId.Value;
            }

            if (callerCampusId.HasValue)
            {
                if (requestedCampusId.HasValue && requestedCampusId.Value != callerCampusId.Value)
                    return (null, null, Forbid());

                effectiveCampusId = callerCampusId.Value;
            }
        }

        if (effectiveTenantId.HasValue != effectiveCampusId.HasValue)
            return (null, null, BadRequest(new { message = "TenantId and CampusId must be provided together." }));

        return (effectiveTenantId, effectiveCampusId, null);
    }

    private async Task<IActionResult?> EnsureOfferingIsInScopeAsync(Guid offeringId, Guid? effectiveTenantId, Guid? effectiveCampusId, CancellationToken ct)
    {
        if (!effectiveTenantId.HasValue && !effectiveCampusId.HasValue)
            return null;

        var scope = await _db.CourseOfferings
            .AsNoTracking()
            .Where(o => o.Id == offeringId)
            .Select(o => new { o.TenantId, o.CampusId })
            .FirstOrDefaultAsync(ct);

        if (scope is null)
            return null;

        if (effectiveTenantId.HasValue && scope.TenantId != effectiveTenantId.Value)
            return Forbid();

        if (effectiveCampusId.HasValue && scope.CampusId != effectiveCampusId.Value)
            return Forbid();

        return null;
    }

    private async Task<IActionResult?> EnsureAssignmentIsInScopeAsync(Guid assignmentId, Guid? effectiveTenantId, Guid? effectiveCampusId, CancellationToken ct)
    {
        if (!effectiveTenantId.HasValue && !effectiveCampusId.HasValue)
            return null;

        var scope = await _db.Assignments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => a.Id == assignmentId)
            .Join(
                _db.CourseOfferings.AsNoTracking(),
                a => a.CourseOfferingId,
                o => o.Id,
                (_, o) => new { o.TenantId, o.CampusId })
            .FirstOrDefaultAsync(ct);

        if (scope is null)
            return null;

        if (effectiveTenantId.HasValue && scope.TenantId != effectiveTenantId.Value)
            return Forbid();

        if (effectiveCampusId.HasValue && scope.CampusId != effectiveCampusId.Value)
            return Forbid();

        return null;
    }
}
