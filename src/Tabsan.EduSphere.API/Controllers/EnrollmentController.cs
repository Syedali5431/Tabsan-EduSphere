using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Handles student enrollment and drop operations.
/// Students can enroll and drop their own courses.
/// Faculty can view the roster for their assigned offerings.
/// Admins can view any offering's roster.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class EnrollmentController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly ICourseRepository _courses;
    private readonly IStudentProfileRepository _studentRepo;
    private readonly IFacultyAssignmentRepository _facultyAssignments;
    private readonly IAccessScopeResolver _accessScope;
    private readonly ApplicationDbContext _db;

    public EnrollmentController(
        IEnrollmentService enrollmentService,
        IEnrollmentRepository enrollmentRepo,
        ICourseRepository courses,
        IStudentProfileRepository studentRepo,
        IFacultyAssignmentRepository facultyAssignments,
        IAccessScopeResolver accessScope,
        ApplicationDbContext db)
    {
        _enrollmentService = enrollmentService;
        _enrollmentRepo = enrollmentRepo;
        _courses = courses;
        _studentRepo = studentRepo;
        _facultyAssignments = facultyAssignments;
        _accessScope = accessScope;
        _db = db;
    }

    // ── POST /api/v1/enrollment ────────────────────────────────────────────────

    /// <summary>
    /// Enrolls the calling student into a course offering.
    /// Validates seat availability, semester state, and duplicate enrollment.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Enroll([FromBody] EnrollRequest request, CancellationToken ct)
    {
        var scope = await ResolveOfferingScopeAsync(request.CourseOfferingId, null, null, ct);
        if (scope.Error is not null) return scope.Error;
        var activeCheck = await EnsureEnrollmentScopeIsActiveAsync(scope.TenantId, scope.CampusId, ct);
        if (activeCheck is not null) return activeCheck;

        var userId = GetUserId();
        var profile = await _studentRepo.GetByUserIdAsync(userId, ct);
        if (profile is null) return BadRequest("Student profile not found.");

        var result = await _enrollmentService.TryEnrollAsync(profile.Id, request.CourseOfferingId, ct: ct);
        if (!result.IsSuccess)
        {
            var body = new
            {
                message              = result.RejectionReason,
                unmetPrerequisites   = result.UnmetPrerequisites,
                clashDetails         = result.ClashDetails
            };
            return Conflict(body);
        }
        return Ok(result.Enrollment);
    }

    // ── DELETE /api/v1/enrollment/{offeringId} ────────────────────────────────

    /// <summary>Drops the calling student's active enrollment in the given offering.</summary>
    [HttpDelete("{offeringId:guid}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Drop(Guid offeringId, CancellationToken ct)
    {
        var scope = await ResolveOfferingScopeAsync(offeringId, null, null, ct);
        if (scope.Error is not null) return scope.Error;
        var activeCheck = await EnsureEnrollmentScopeIsActiveAsync(scope.TenantId, scope.CampusId, ct);
        if (activeCheck is not null) return activeCheck;

        var userId = GetUserId();
        var profile = await _studentRepo.GetByUserIdAsync(userId, ct);
        if (profile is null) return BadRequest("Student profile not found.");

        var ok = await _enrollmentService.DropAsync(profile.Id, offeringId, ct);
        return ok ? NoContent() : NotFound("Active enrollment not found.");
    }

    // ── GET /api/v1/enrollment/my-courses ─────────────────────────────────────

    // Final-Touches Phase 8 Stage 8.1 — added CourseOfferingId to response so student can drop using offeringId
    /// <summary>Returns the calling student's enrollment history (all statuses).</summary>
    [HttpGet("my-courses")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyCourses(CancellationToken ct)
    {
        var requestedScope = ResolveRequestedScope(null, null);
        if (requestedScope.Error is not null) return requestedScope.Error;
        var activeCheck = await EnsureEnrollmentScopeIsActiveAsync(requestedScope.TenantId, requestedScope.CampusId, ct);
        if (activeCheck is not null) return activeCheck;

        var userId = GetUserId();
        var profile = await _studentRepo.GetByUserIdAsync(userId, ct);
        if (profile is null) return NotFound("Student profile not found.");

        var enrollments = await _enrollmentService.GetForStudentAsync(profile.Id, ct);
        return Ok(enrollments.Select(e => new
        {
            e.Id,
            CourseOfferingId = e.CourseOfferingId,
            CourseTitle = e.CourseOffering.Course.Title,
            CourseCode  = e.CourseOffering.Course.Code,
            Semester    = e.CourseOffering.Semester.Name,
            e.Status,
            e.EnrolledAt,
            e.DroppedAt
        }));
    }

    // ── GET /api/v1/enrollment/roster/{offeringId} ────────────────────────────

    // Final-Touches Phase 8 Stage 8.1 — fixed response fields to match RosterApiDto: Id, StudentName, RegistrationNumber, ProgramName, SemesterNumber
    /// <summary>Returns the active enrollment roster for a course offering.</summary>
    [HttpGet("roster/{offeringId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetRoster(Guid offeringId, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveOfferingScopeAsync(offeringId, tenantId, campusId, ct);
        if (scope.Error is not null) return scope.Error;
        var activeCheck = await EnsureEnrollmentScopeIsActiveAsync(scope.TenantId, scope.CampusId, ct);
        if (activeCheck is not null) return activeCheck;

        var roster = await _enrollmentService.GetForOfferingAsync(offeringId, ct);
        return Ok(roster.Select(e => new
        {
            Id                 = e.Id,
            StudentProfileId   = e.StudentProfileId,
            StudentName        = e.StudentProfile.RegistrationNumber,
            RegistrationNumber = e.StudentProfile.RegistrationNumber,
            ProgramName        = e.StudentProfile.Program?.Name ?? "",
            SemesterNumber     = e.StudentProfile.CurrentSemesterNumber
        }));
    }

    // ── GET /api/v1/enrollment/waitlist/{offeringId} ─────────────────────────

    /// <summary>Returns the current waitlist queue for a course offering.</summary>
    [HttpGet("waitlist/{offeringId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetWaitlist(Guid offeringId, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveOfferingScopeAsync(offeringId, tenantId, campusId, ct);
        if (scope.Error is not null) return scope.Error;
        var activeCheck = await EnsureEnrollmentScopeIsActiveAsync(scope.TenantId, scope.CampusId, ct);
        if (activeCheck is not null) return activeCheck;

        var waitlist = await _enrollmentService.GetWaitlistedForOfferingAsync(offeringId, ct);
        return Ok(waitlist.Select((e, index) => new
        {
            QueuePosition      = index + 1,
            Id                 = e.Id,
            StudentName        = e.StudentProfile.RegistrationNumber,
            RegistrationNumber = e.StudentProfile.RegistrationNumber,
            ProgramName        = e.StudentProfile.Program?.Name ?? string.Empty,
            SemesterNumber     = e.StudentProfile.CurrentSemesterNumber,
            e.Status,
            e.EnrolledAt
        }));
    }

    // ── POST /api/v1/enrollment/admin ──────────────────────────────────────────

    // Final-Touches Phase 8 Stage 8.2 — admin enrolls any student into any offering
    // Final-Touches Phase 15 Stage 15.2 — admin can override timetable clash with reason
    /// <summary>Admin enrolls a student into a course offering. Supports clash override (Phase 15).</summary>
    [HttpPost("admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AdminEnroll([FromBody] AdminEnrollRequest request, CancellationToken ct)
    {
        var scope = await ResolveOfferingScopeAsync(request.CourseOfferingId, null, null, ct);
        if (scope.Error is not null) return scope.Error;
        var activeCheck = await EnsureEnrollmentScopeIsActiveAsync(scope.TenantId, scope.CampusId, ct);
        if (activeCheck is not null) return activeCheck;

        var result = await _enrollmentService.TryEnrollAsync(
            request.StudentProfileId, request.CourseOfferingId,
            request.OverrideClash, request.OverrideReason, ct);
        if (!result.IsSuccess)
        {
            var body = new
            {
                message            = result.RejectionReason,
                unmetPrerequisites = result.UnmetPrerequisites,
                clashDetails       = result.ClashDetails
            };
            return Conflict(body);
        }
        return Ok(result.Enrollment);
    }

    // ── DELETE /api/v1/enrollment/admin/{enrollmentId} ─────────────────────────

    // Final-Touches Phase 8 Stage 8.2 — admin drops any active enrollment by its ID
    /// <summary>Admin drops an active enrollment identified by enrollment ID.</summary>
    [HttpDelete("admin/{enrollmentId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AdminDrop(Guid enrollmentId, CancellationToken ct)
    {
        var enrollment = await _enrollmentRepo.GetByIdAsync(enrollmentId, ct);
        if (enrollment is null) return NotFound("Active enrollment not found.");

        var scope = await ResolveOfferingScopeAsync(enrollment.CourseOfferingId, null, null, ct);
        if (scope.Error is not null) return scope.Error;
        var activeCheck = await EnsureEnrollmentScopeIsActiveAsync(scope.TenantId, scope.CampusId, ct);
        if (activeCheck is not null) return activeCheck;

        var ok = await _enrollmentService.AdminDropByIdAsync(enrollmentId, ct);
        return ok ? NoContent() : NotFound("Active enrollment not found.");
    }

    [HttpGet("status")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetEnrollmentStatus([FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = ResolveRequestedScope(tenantId, campusId);
        if (scope.Error is not null) return scope.Error;

        var isActive = await IsEnrollmentScopeActiveAsync(scope.TenantId, scope.CampusId, ct);
        return Ok(new { isActive, tenantId = scope.TenantId, campusId = scope.CampusId });
    }

    [HttpPost("activate")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ActivateEnrollment([FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = ResolveRequestedScope(tenantId, campusId);
        if (scope.Error is not null) return scope.Error;

        await SetEnrollmentScopeActiveAsync(scope.TenantId, scope.CampusId, true, ct);
        return NoContent();
    }

    [HttpPost("deactivate")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeactivateEnrollment([FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = ResolveRequestedScope(tenantId, campusId);
        if (scope.Error is not null) return scope.Error;

        await SetEnrollmentScopeActiveAsync(scope.TenantId, scope.CampusId, false, ct);
        return NoContent();
    }

    // ── Helper ─────────────────────────────────────────────────────────────────
    private Guid GetUserId()
    {
        var raw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    private async Task<(Guid? TenantId, Guid? CampusId, IActionResult? Error)> ResolveOfferingScopeAsync(
        Guid offeringId,
        Guid? requestedTenantId,
        Guid? requestedCampusId,
        CancellationToken ct)
    {
        var requestedScope = ResolveRequestedScope(requestedTenantId, requestedCampusId);
        if (requestedScope.Error is not null)
            return requestedScope;

        var offering = await _courses.GetOfferingByIdAsync(offeringId, ct);
        if (offering is null)
            return (null, null, NotFound("Course offering not found."));

        if (requestedScope.TenantId.HasValue && offering.TenantId != requestedScope.TenantId)
            return (null, null, Forbid());

        if (requestedScope.CampusId.HasValue && offering.CampusId != requestedScope.CampusId)
            return (null, null, Forbid());

        if (!_accessScope.IsSuperAdmin())
        {
            var callerTenantId = _accessScope.GetTenantId();
            var callerCampusId = _accessScope.GetCampusId();

            if (callerTenantId.HasValue && offering.TenantId != callerTenantId)
                return (null, null, Forbid());

            if (callerCampusId.HasValue && offering.CampusId != callerCampusId)
                return (null, null, Forbid());
        }

        return (offering.TenantId, offering.CampusId, null);
    }

    private (Guid? TenantId, Guid? CampusId, IActionResult? Error) ResolveRequestedScope(Guid? requestedTenantId, Guid? requestedCampusId)
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

    private static string GetEnrollmentScopeSettingKey(Guid? tenantId, Guid? campusId)
        => tenantId.HasValue && campusId.HasValue
            ? $"enrollment.active.{tenantId.Value:N}.{campusId.Value:N}"
            : "enrollment.active.global";

    private async Task<bool> IsEnrollmentScopeActiveAsync(Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        var key = GetEnrollmentScopeSettingKey(tenantId, campusId);
        var setting = await _db.PortalSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key, ct);
        if (setting is null)
            return true;

        return bool.TryParse(setting.Value, out var isActive) ? isActive : true;
    }

    private async Task SetEnrollmentScopeActiveAsync(Guid? tenantId, Guid? campusId, bool isActive, CancellationToken ct)
    {
        var key = GetEnrollmentScopeSettingKey(tenantId, campusId);
        var setting = await _db.PortalSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (setting is null)
        {
            setting = new Domain.Settings.PortalSetting(key, isActive.ToString().ToLowerInvariant());
            await _db.PortalSettings.AddAsync(setting, ct);
        }
        else
        {
            setting.SetValue(isActive.ToString().ToLowerInvariant());
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<IActionResult?> EnsureEnrollmentScopeIsActiveAsync(Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        var isActive = await IsEnrollmentScopeActiveAsync(tenantId, campusId, ct);
        if (isActive)
            return null;

        return StatusCode(StatusCodes.Status423Locked, new { message = "Enrollment is deactivated for the selected tenant/campus scope." });
    }
}
