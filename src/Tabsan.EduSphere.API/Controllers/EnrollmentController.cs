using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;

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
    private readonly IStudentProfileRepository _studentRepo;
    private readonly IFacultyAssignmentRepository _facultyAssignments;

    public EnrollmentController(
        IEnrollmentService enrollmentService,
        IStudentProfileRepository studentRepo,
        IFacultyAssignmentRepository facultyAssignments)
    {
        _enrollmentService = enrollmentService;
        _studentRepo = studentRepo;
        _facultyAssignments = facultyAssignments;
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
    public async Task<IActionResult> GetRoster(Guid offeringId, CancellationToken ct)
    {
        var roster = await _enrollmentService.GetForOfferingAsync(offeringId, ct);
        return Ok(roster.Select(e => new
        {
            Id                 = e.Id,
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
    public async Task<IActionResult> GetWaitlist(Guid offeringId, CancellationToken ct)
    {
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
        var ok = await _enrollmentService.AdminDropByIdAsync(enrollmentId, ct);
        return ok ? NoContent() : NotFound("Active enrollment not found.");
    }

    // ── Helper ─────────────────────────────────────────────────────────────────
    private Guid GetUserId()
    {
        var raw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}
