using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.DTOs.Fyp;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Manages FYP project proposals, panel assignments, and meeting scheduling.
/// </summary>
[ApiController]
[Route("api/v1/fyp")]
[Authorize]
public sealed class FypController : ControllerBase
{
    private readonly IFypService _fypService;
    private readonly IAccessScopeResolver _accessScope;
    private readonly ApplicationDbContext _db;

    /// <summary>Initialises the controller with the FYP service.</summary>
    public FypController(IFypService fypService, IAccessScopeResolver accessScope, ApplicationDbContext db)
    {
        _fypService = fypService;
        _accessScope = accessScope;
        _db = db;
    }

    // ── Projects ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Submits a new FYP project proposal. Student only.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "Student")]
    public async Task<IActionResult> Propose([FromBody] ProposeProjectRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var studentProfileId = GetStudentProfileId();
        if (studentProfileId == Guid.Empty) return Forbid();

        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, request.DepartmentId, null, studentProfileId, ct);
        if (scope.Error is not null)
            return scope.Error;

        var id = await _fypService.ProposeAsync(request, studentProfileId, ct);
        return CreatedAtAction(nameof(GetDetail), new { id }, new { projectId = id });
    }

    /// <summary>
    /// Creates a new FYP project directly for a student. Admin, SuperAdmin, and Faculty.
    /// </summary>
    // Issue-Fix Phase 3 Stage 3.8 — Allow Faculty to create FYP records for their students.
    [HttpPost("admin-create")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> CreateForStudent([FromBody] CreateProjectForStudentRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, request.DepartmentId, null, request.StudentProfileId, ct);
        if (scope.Error is not null)
            return scope.Error;

        var id = await _fypService.CreateForStudentAsync(request, ct);
        return CreatedAtAction(nameof(GetDetail), new { id }, new { projectId = id });
    }

    /// <summary>
    /// Updates the title and description of a project (student or admin).
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Student")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, id, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        var ok = await _fypService.UpdateAsync(id, request, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Approves a project proposal. Admin/Coordinator only.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveProjectRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, id, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        var ok = await _fypService.ApproveAsync(id, request, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Rejects a project proposal with remarks. Admin/Coordinator only.
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectProjectRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, id, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        var ok = await _fypService.RejectAsync(id, request, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Assigns a supervisor to a project. Admin/Coordinator only.
    /// </summary>
    [HttpPost("{id:guid}/assign-supervisor")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> AssignSupervisor(Guid id, [FromBody] AssignSupervisorRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, id, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        var ok = await _fypService.AssignSupervisorAsync(id, request, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Marks a project as completed. Admin/Coordinator only.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Complete(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, id, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        var ok = await _fypService.CompleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Enters or updates final result for a completed project. Faculty/Admin only.
    /// </summary>
    [HttpPost("{id:guid}/result")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> EnterResult(Guid id, [FromBody] EnterFypResultRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, id, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        var ok = await _fypService.EnterResultAsync(id, request, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Student requests completion review for their own in-progress project.
    /// </summary>
    [HttpPost("{id:guid}/request-completion")]
    [Authorize(Policy = "Student")]
    public async Task<IActionResult> RequestCompletion(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var studentProfileId = GetStudentProfileId();
        if (studentProfileId == Guid.Empty) return Forbid();

        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, id, studentProfileId, ct);
        if (scope.Error is not null)
            return scope.Error;

        var ok = await _fypService.RequestCompletionAsync(id, studentProfileId, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Assigned faculty approves student completion review. Project auto-completes once all approvals are collected.
    /// </summary>
    [HttpPost("{id:guid}/approve-completion")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> ApproveCompletion(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var facultyUserId = GetCurrentUserId();
        if (facultyUserId == Guid.Empty) return Forbid();

        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, id, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        var result = await _fypService.ApproveCompletionAsync(id, facultyUserId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all FYP projects for the current student.
    /// </summary>
    [HttpGet("my-projects")]
    [Authorize(Policy = "Student")]
    public async Task<IActionResult> GetMyProjects([FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var studentProfileId = GetStudentProfileId();
        if (studentProfileId == Guid.Empty) return Forbid();

        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, null, studentProfileId, ct);
        if (scope.Error is not null)
            return scope.Error;

        return Ok(await _fypService.GetByStudentAsync(studentProfileId, scope.TenantId, scope.CampusId, ct));
    }

    /// <summary>
    /// Returns all projects in a department, optionally filtered by status.
    /// Faculty and Admin only.
    /// </summary>
    [HttpGet("by-department/{departmentId:guid}")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> GetByDepartment(Guid departmentId, [FromQuery] string? status, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, departmentId, null, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        return Ok(await _fypService.GetByDepartmentAsync(departmentId, status, scope.TenantId, scope.CampusId, ct));
    }

    /// <summary>
    /// Returns all projects across all departments, optionally filtered by status.
    /// Admin and SuperAdmin only.
    /// </summary>
    [HttpGet("all")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, null, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        return Ok(await _fypService.GetAllAsync(status, scope.TenantId, scope.CampusId, ct));
    }

    /// <summary>
    /// Returns projects supervised by the current faculty user.
    /// </summary>
    [HttpGet("my-supervised")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> GetMySupervised([FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, null, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        return Ok(await _fypService.GetBySupervisorAsync(GetCurrentUserId(), scope.TenantId, scope.CampusId, ct));
    }

    /// <summary>
    /// Returns full project detail including panel members and meeting history.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, id, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        var result = await _fypService.GetDetailAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // ── Panel members ─────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a faculty user to a project panel. Admin/Coordinator only.
    /// </summary>
    [HttpPost("{id:guid}/panel")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> AddPanelMember(Guid id, [FromBody] AddPanelMemberRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, id, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        var ok = await _fypService.AddPanelMemberAsync(id, request, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Removes a user from the project panel. Admin/Coordinator only.
    /// </summary>
    [HttpDelete("{id:guid}/panel/{userId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> RemovePanelMember(Guid id, Guid userId, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, id, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        var ok = await _fypService.RemovePanelMemberAsync(id, userId, ct);
        return ok ? NoContent() : NotFound();
    }

    // ── Meetings ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Schedules a meeting for an FYP project. Faculty and Admin.
    /// </summary>
    [HttpPost("meeting")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> ScheduleMeeting([FromBody] ScheduleMeetingRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, request.FypProjectId, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        var id = await _fypService.ScheduleMeetingAsync(request, GetCurrentUserId(), ct);
        return Ok(new { meetingId = id });
    }

    /// <summary>
    /// Reschedules an existing meeting. Faculty and Admin.
    /// </summary>
    [HttpPut("meeting/{meetingId:guid}")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> RescheduleMeeting(Guid meetingId, [FromBody] RescheduleMeetingRequest request, CancellationToken ct)
    {
        var ok = await _fypService.RescheduleMeetingAsync(meetingId, request, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Marks a meeting as completed and records minutes. Faculty and Admin.
    /// </summary>
    [HttpPost("meeting/{meetingId:guid}/complete")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> CompleteMeeting(Guid meetingId, [FromBody] CompleteMeetingRequest request, CancellationToken ct)
    {
        var ok = await _fypService.CompleteMeetingAsync(meetingId, request, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Cancels a scheduled meeting. Faculty and Admin.
    /// </summary>
    [HttpPost("meeting/{meetingId:guid}/cancel")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> CancelMeeting(Guid meetingId, CancellationToken ct)
    {
        var ok = await _fypService.CancelMeetingAsync(meetingId, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Returns all meetings for a project.
    /// </summary>
    [HttpGet("{id:guid}/meetings")]
    public async Task<IActionResult> GetMeetings(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, id, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        return Ok(await _fypService.GetMeetingsByProjectAsync(id, ct));
    }

    /// <summary>
    /// Returns upcoming meetings for the current faculty supervisor.
    /// </summary>
    [HttpGet("meeting/upcoming")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> GetUpcomingMeetings([FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, null, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        return Ok(await _fypService.GetUpcomingMeetingsAsync(GetCurrentUserId(), ct));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Extracts the authenticated user ID from the JWT NameIdentifier claim.</summary>
    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    /// <summary>Extracts the student profile ID from the "studentProfileId" JWT claim.</summary>
    private Guid GetStudentProfileId()
    {
        var value = User.FindFirstValue("studentProfileId");
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    private async Task<(Guid? TenantId, Guid? CampusId, IActionResult? Error)> ResolveEffectiveScopeAsync(
        Guid? requestedTenantId,
        Guid? requestedCampusId,
        Guid? requestedDepartmentId,
        Guid? requestedProjectId,
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

        if (requestedDepartmentId.HasValue)
        {
            var departmentScope = await _db.Departments
                .AsNoTracking()
                .Where(d => d.Id == requestedDepartmentId.Value)
                .Select(d => new { d.TenantId, d.CampusId })
                .FirstOrDefaultAsync(ct);

            if (departmentScope is null)
                return (null, null, NotFound(new { message = $"Department {requestedDepartmentId.Value} not found." }));

            if (effectiveTenantId.HasValue && departmentScope.TenantId != effectiveTenantId.Value)
                return (null, null, Forbid());

            if (effectiveCampusId.HasValue && departmentScope.CampusId != effectiveCampusId.Value)
                return (null, null, Forbid());

            effectiveTenantId ??= departmentScope.TenantId;
            effectiveCampusId ??= departmentScope.CampusId;
        }

        if (requestedProjectId.HasValue)
        {
            var projectScope = await _db.FypProjects
                .AsNoTracking()
                .Where(p => p.Id == requestedProjectId.Value)
                .Join(
                    _db.Departments.AsNoTracking(),
                    p => p.DepartmentId,
                    d => d.Id,
                    (_, d) => new { d.TenantId, d.CampusId })
                .FirstOrDefaultAsync(ct);

            if (projectScope is null)
                return (null, null, NotFound(new { message = $"FYP project {requestedProjectId.Value} not found." }));

            if (effectiveTenantId.HasValue && projectScope.TenantId != effectiveTenantId.Value)
                return (null, null, Forbid());

            if (effectiveCampusId.HasValue && projectScope.CampusId != effectiveCampusId.Value)
                return (null, null, Forbid());

            effectiveTenantId ??= projectScope.TenantId;
            effectiveCampusId ??= projectScope.CampusId;
        }

        if (requestedStudentProfileId.HasValue)
        {
            var studentScope = await _db.StudentProfiles
                .AsNoTracking()
                .Where(sp => sp.Id == requestedStudentProfileId.Value)
                .Join(
                    _db.Departments.AsNoTracking(),
                    sp => sp.DepartmentId,
                    d => d.Id,
                    (_, d) => new { d.TenantId, d.CampusId })
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
}
