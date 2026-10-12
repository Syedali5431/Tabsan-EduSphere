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
            // The portal compares against names ("Active"); a numeric enum broke every student course list.
            Status      = e.Status.ToString(),
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
        var names = await StudentLabelsAsync(roster.Select(e => e.StudentProfileId).Distinct().ToList(), ct);
        return Ok(roster.Select(e => new
        {
            Id                 = e.Id,
            StudentProfileId   = e.StudentProfileId,
            StudentName        = names.TryGetValue(e.StudentProfileId, out var st) ? st.FullName : e.StudentProfile.RegistrationNumber,
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
    // Read-only flag; Faculty and Students use it to show whether enrollment is open.
    [Authorize(Roles = "SuperAdmin,Admin,Faculty,Student")]
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

    // ── GET /api/v1/enrollment/workspace ──────────────────────────────────────

    /// <summary>
    /// Everything the staff enrollment screen needs for one department and (optionally) one class /
    /// semester: the course offerings (with seats, open state and teacher) and the department's students
    /// (roll number, name, program, level) with the offerings each is already enrolled or waitlisted in.
    /// Faculty only see the offerings they teach.
    /// </summary>
    [HttpGet("workspace")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetWorkspace([FromQuery] Guid departmentId, [FromQuery] Guid? semesterId, CancellationToken ct)
    {
        var department = await _db.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == departmentId, ct);
        if (department is null) return NotFound("Department not found.");
        if (!await CanAccessDepartmentAsync(department.Id, department.TenantId, ct)) return Forbid();

        var userId = GetUserId();
        var offeringsQuery = _db.CourseOfferings.AsNoTracking().Where(o => o.Course.DepartmentId == departmentId);
        if (semesterId.HasValue) offeringsQuery = offeringsQuery.Where(o => o.SemesterId == semesterId.Value);
        if (IsFacultyOnly()) offeringsQuery = offeringsQuery.Where(o => o.FacultyUserId == userId);

        var offerings = await offeringsQuery
            .Select(o => new
            {
                o.Id,
                CourseCode = o.Course.Code,
                CourseTitle = o.Course.Title,
                o.SemesterId,
                SemesterName = o.Semester.Name,
                SemesterClosed = o.Semester.IsClosed,
                o.IsOpen,
                o.MaxEnrollment,
                o.FacultyUserId
            })
            .ToListAsync(ct);
        var offeringIds = offerings.Select(o => o.Id).ToList();

        var memberships = await _db.Enrollments.AsNoTracking()
            .Where(e => offeringIds.Contains(e.CourseOfferingId)
                        && (e.Status == Domain.Academic.EnrollmentStatus.Active || e.Status == Domain.Academic.EnrollmentStatus.Waitlisted))
            .Select(e => new { e.Id, e.StudentProfileId, e.CourseOfferingId, e.Status, e.EnrolledAt })
            .ToListAsync(ct);

        var facultyIds = offerings.Where(o => o.FacultyUserId.HasValue).Select(o => o.FacultyUserId!.Value).Distinct().ToList();
        var facultyNames = await _db.Users.AsNoTracking()
            .Where(u => facultyIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FullName ?? u.Username })
            .ToDictionaryAsync(u => u.Id, u => u.Name, ct);

        var students = await _db.StudentProfiles.AsNoTracking()
            .Where(s => s.DepartmentId == departmentId)
            .Join(_db.Users.AsNoTracking(), s => s.UserId, u => u.Id, (s, u) => new
            {
                s.Id,
                s.RegistrationNumber,
                FullName = u.FullName ?? u.Username,
                s.ProgramId,
                ProgramName = s.Program.Name,
                Level = s.CurrentSemesterNumber,
                s.Status,
                AccountActive = u.IsActive
            })
            .OrderBy(s => s.RegistrationNumber)
            .ToListAsync(ct);

        return Ok(new
        {
            DepartmentId = department.Id,
            DepartmentName = department.Name,
            InstitutionType = (int)department.InstitutionType,
            Offerings = offerings
                .OrderBy(o => o.SemesterName).ThenBy(o => o.CourseCode)
                .Select(o => new
                {
                    o.Id, o.CourseCode, o.CourseTitle, o.SemesterId, o.SemesterName, o.SemesterClosed, o.IsOpen, o.MaxEnrollment,
                    FacultyName = o.FacultyUserId.HasValue && facultyNames.TryGetValue(o.FacultyUserId.Value, out var fn) ? fn : "",
                    EnrolledCount = memberships.Count(m => m.CourseOfferingId == o.Id && m.Status == Domain.Academic.EnrollmentStatus.Active),
                    WaitlistCount = memberships.Count(m => m.CourseOfferingId == o.Id && m.Status == Domain.Academic.EnrollmentStatus.Waitlisted)
                }),
            Students = students.Select(s => new
            {
                s.Id, s.RegistrationNumber, s.FullName, s.ProgramId, s.ProgramName, s.Level,
                Status = s.Status.ToString(), s.AccountActive
            }),
            Enrollments = memberships.Select(m => new
            {
                m.Id, m.StudentProfileId, m.CourseOfferingId, Status = m.Status.ToString(), m.EnrolledAt
            })
        });
    }

    // ── POST /api/v1/enrollment/bulk ───────────────────────────────────────────

    /// <summary>
    /// Enrolls several students into one or more offerings in one go. Every student/offering pair is
    /// checked with the normal rules (open offering and semester, duplicates, prerequisites, timetable
    /// clashes, seats → waitlist) and reported back individually.
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> BulkEnroll([FromBody] BulkEnrollRequest request, CancellationToken ct)
    {
        var offeringIds = (request.CourseOfferingIds ?? Array.Empty<Guid>()).Distinct().ToList();
        var studentIds = (request.StudentProfileIds ?? Array.Empty<Guid>()).Distinct().ToList();
        if (offeringIds.Count == 0 || studentIds.Count == 0)
            return BadRequest(new { message = "Select at least one course and one student." });
        if (offeringIds.Count * studentIds.Count > 2000)
            return BadRequest(new { message = "Too many enrollments in one request (maximum 2000 student/course pairs)." });

        // Only administrators may override timetable clashes (same rule as the single-student admin endpoint).
        var overrideClash = request.OverrideClash && !IsFacultyOnly();
        var students = await StudentLabelsAsync(studentIds, ct);
        var results = new List<BulkEnrollmentOutcome>();

        foreach (var offeringId in offeringIds)
        {
            var offering = await _courses.GetOfferingByIdAsync(offeringId, ct);
            var courseLabel = offering is null ? offeringId.ToString() : $"{offering.Course.Code} — {offering.Course.Title}";
            var denied = offering is null ? "Course offering not found." : await CannotManageOfferingAsync(offering, ct);
            if (denied is null)
            {
                var activeCheck = await EnsureEnrollmentScopeIsActiveAsync(offering!.TenantId, offering.CampusId, ct);
                if (activeCheck is not null) denied = "Enrollment is deactivated for this campus.";
            }

            foreach (var studentId in studentIds)
            {
                students.TryGetValue(studentId, out var st);
                var outcome = new BulkEnrollmentOutcome(studentId, st?.RegistrationNumber ?? "", st?.FullName ?? "", offeringId, courseLabel, "Skipped", denied ?? "");
                if (denied is not null) { results.Add(outcome); continue; }
                if (st is null) { results.Add(outcome with { Message = "Student not found." }); continue; }
                if (st.InstitutionType != (int)offering!.InstitutionType)
                {
                    results.Add(outcome with { Message = "Student belongs to a different institute." });
                    continue;
                }

                var attempt = await _enrollmentService.TryEnrollAsync(studentId, offeringId, overrideClash, overrideClash ? request.OverrideReason : null, ct);
                if (attempt.IsSuccess)
                {
                    var waitlisted = string.Equals(attempt.Enrollment?.Status, "Waitlisted", StringComparison.OrdinalIgnoreCase);
                    results.Add(outcome with
                    {
                        Outcome = waitlisted ? "Waitlisted" : "Enrolled",
                        Message = waitlisted ? "Course is full — added to the waitlist." : ""
                    });
                }
                else
                {
                    var detail = attempt.UnmetPrerequisites?.Count > 0 ? $" ({string.Join(", ", attempt.UnmetPrerequisites)})"
                               : attempt.ClashDetails?.Count > 0 ? $" ({string.Join("; ", attempt.ClashDetails)})" : "";
                    var alreadyIn = attempt.RejectionReason?.StartsWith("Already enrolled", StringComparison.OrdinalIgnoreCase) == true;
                    results.Add(outcome with { Outcome = alreadyIn ? "AlreadyEnrolled" : "Skipped", Message = (attempt.RejectionReason ?? "Not enrolled.") + detail });
                }
            }
        }

        return Ok(new
        {
            Enrolled = results.Count(r => r.Outcome == "Enrolled"),
            Waitlisted = results.Count(r => r.Outcome == "Waitlisted"),
            AlreadyEnrolled = results.Count(r => r.Outcome == "AlreadyEnrolled"),
            Skipped = results.Count(r => r.Outcome == "Skipped"),
            Results = results
        });
    }

    // ── POST /api/v1/enrollment/bulk-drop ──────────────────────────────────────

    /// <summary>Drops (or removes from the waitlist) several students from one offering.</summary>
    [HttpPost("bulk-drop")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> BulkDrop([FromBody] BulkDropRequest request, CancellationToken ct)
    {
        var studentIds = (request.StudentProfileIds ?? Array.Empty<Guid>()).Distinct().ToList();
        if (studentIds.Count == 0) return BadRequest(new { message = "Select at least one student." });

        var offering = await _courses.GetOfferingByIdAsync(request.CourseOfferingId, ct);
        if (offering is null) return NotFound(new { message = "Course offering not found." });
        var denied = await CannotManageOfferingAsync(offering, ct);
        if (denied is not null) return StatusCode(StatusCodes.Status403Forbidden, new { message = denied });
        var activeCheck = await EnsureEnrollmentScopeIsActiveAsync(offering.TenantId, offering.CampusId, ct);
        if (activeCheck is not null) return activeCheck;

        var enrollments = await _db.Enrollments
            .Where(e => e.CourseOfferingId == offering.Id && studentIds.Contains(e.StudentProfileId)
                        && (e.Status == Domain.Academic.EnrollmentStatus.Active || e.Status == Domain.Academic.EnrollmentStatus.Waitlisted))
            .ToListAsync(ct);

        int dropped = 0, removedFromWaitlist = 0;
        foreach (var e in enrollments.Where(e => e.Status == Domain.Academic.EnrollmentStatus.Waitlisted))
        {
            e.Cancel();
            removedFromWaitlist++;
        }
        if (removedFromWaitlist > 0) await _db.SaveChangesAsync(ct);

        // Dropping an active seat promotes the next waitlisted student, so go through the service.
        foreach (var e in enrollments.Where(e => e.Status == Domain.Academic.EnrollmentStatus.Active).ToList())
        {
            if (await _enrollmentService.AdminDropByIdAsync(e.Id, ct)) dropped++;
        }

        return Ok(new { Dropped = dropped, RemovedFromWaitlist = removedFromWaitlist, NotEnrolled = studentIds.Count - dropped - removedFromWaitlist });
    }

    public sealed record BulkEnrollRequest(IReadOnlyList<Guid>? CourseOfferingIds, IReadOnlyList<Guid>? StudentProfileIds, bool OverrideClash = false, string? OverrideReason = null);
    public sealed record BulkDropRequest(Guid CourseOfferingId, IReadOnlyList<Guid>? StudentProfileIds);
    public sealed record BulkEnrollmentOutcome(Guid StudentProfileId, string RegistrationNumber, string StudentName, Guid CourseOfferingId, string Course, string Outcome, string Message);
    private sealed record StudentLabel(string RegistrationNumber, string FullName, int InstitutionType);

    private async Task<Dictionary<Guid, StudentLabel>> StudentLabelsAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken ct)
        => await _db.StudentProfiles.AsNoTracking()
            .Where(s => studentIds.Contains(s.Id))
            .Join(_db.Users.AsNoTracking(), s => s.UserId, u => u.Id,
                (s, u) => new { s.Id, s.RegistrationNumber, FullName = u.FullName ?? u.Username, InstitutionType = (int)s.Department.InstitutionType })
            .ToDictionaryAsync(x => x.Id, x => new StudentLabel(x.RegistrationNumber, x.FullName, x.InstitutionType), ct);

    private bool IsFacultyOnly() => User.IsInRole("Faculty") && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin");

    private async Task<bool> CanAccessDepartmentAsync(Guid departmentId, Guid? departmentTenantId, CancellationToken ct)
    {
        if (_accessScope.IsSuperAdmin()) return true;
        var callerTenantId = _accessScope.GetTenantId();
        if (callerTenantId.HasValue && departmentTenantId != callerTenantId) return false;

        var userId = GetUserId();
        if (User.IsInRole("Admin"))
            return await _db.AdminDepartmentAssignments.AnyAsync(a => a.AdminUserId == userId && a.DepartmentId == departmentId && a.RemovedAt == null, ct);

        return await _db.FacultyDepartmentAssignments.AnyAsync(a => a.FacultyUserId == userId && a.DepartmentId == departmentId && a.RemovedAt == null, ct)
            || await _db.CourseOfferings.AnyAsync(o => o.FacultyUserId == userId && o.Course.DepartmentId == departmentId, ct);
    }

    /// <summary>Null when the caller may enroll/drop students in the offering, otherwise the reason.</summary>
    private async Task<string?> CannotManageOfferingAsync(Domain.Academic.CourseOffering offering, CancellationToken ct)
    {
        if (_accessScope.IsSuperAdmin()) return null;
        var callerTenantId = _accessScope.GetTenantId();
        if (callerTenantId.HasValue && offering.TenantId != callerTenantId) return "This course belongs to another institute.";

        var userId = GetUserId();
        if (User.IsInRole("Admin"))
            return await _db.AdminDepartmentAssignments.AnyAsync(a => a.AdminUserId == userId && a.DepartmentId == offering.Course.DepartmentId && a.RemovedAt == null, ct)
                ? null : "You do not manage this course's department.";

        return offering.FacultyUserId == userId ? null : "Only the course's teacher can enroll students in it.";
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
