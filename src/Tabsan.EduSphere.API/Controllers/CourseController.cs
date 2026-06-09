using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tabsan.EduSphere.Application.Academic;
using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Manages the course catalogue and course offerings.
/// Admin+ manages courses and offerings; Faculty can read their own offerings; Students can browse.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CourseController : ControllerBase
{
    private readonly ICourseRepository _repo;
    private readonly IFacultyAssignmentRepository _facultyAssignments;
    private readonly IAdminAssignmentRepository _adminAssignments;
    private readonly IEnrollmentRepository _enrollments;
    private readonly IStudentProfileRepository _studentProfiles;
    private readonly ISchoolStreamRepository _schoolStreams;
    private readonly IDepartmentRepository _departments;
    private readonly ISemesterRepository _semesters;
    private readonly Tabsan.EduSphere.Application.Interfaces.ICourseService _courseService;

    public CourseController(
        ICourseRepository repo,
        IFacultyAssignmentRepository facultyAssignments,
        IAdminAssignmentRepository adminAssignments,
        IEnrollmentRepository enrollments,
        IStudentProfileRepository studentProfiles,
        ISchoolStreamRepository schoolStreams,
        IDepartmentRepository departments,
        ISemesterRepository semesters,
        Tabsan.EduSphere.Application.Interfaces.ICourseService courseService)
    {
        _repo = repo;
        _facultyAssignments = facultyAssignments;
        _adminAssignments = adminAssignments;
        _enrollments = enrollments;
        _studentProfiles = studentProfiles;
        _schoolStreams = schoolStreams;
        _departments = departments;
        _semesters = semesters;
        _courseService = courseService;
    }

    // ────────────────────── COURSES ────────────────────────────────────────────

    // ── GET /api/v1/course ─────────────────────────────────────────────────────

    /// <summary>Returns the course catalogue, optionally filtered by department and institution scope.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? departmentId,
        [FromQuery] bool? hasSemesters,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, tenantId, campusId, institutionType, ct);
        if (scope.Error is not null)
            return scope.Error;

        var courses = await _repo.GetAllAsync(departmentId, hasSemesters, ct);
        courses = courses
            .Where(c => ScopeMatches(c.DepartmentId, c.TenantId, c.CampusId, (int)c.InstitutionType, scope))
            .ToList();

        // Issue-Fix Phase 3 Stage 3.1 — Replace Forbid with empty result; faculty sees only their assigned-dept courses.
        if (User.IsInRole("Faculty") && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
        {
            var allowedDepartmentIds = await _facultyAssignments.GetDepartmentIdsForFacultyAsync(GetUserId(), ct);
            if (departmentId.HasValue && !allowedDepartmentIds.Contains(departmentId.Value))
                return Ok(Array.Empty<CourseResponse>()); // Not in assigned depts — return empty instead of 403.

            courses = courses.Where(c => allowedDepartmentIds.Contains(c.DepartmentId)).ToList();
        }

        if (User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
        {
            var allowedDepartmentIds = await _adminAssignments.GetDepartmentIdsForAdminAsync(GetUserId(), ct);
            if (departmentId.HasValue && !allowedDepartmentIds.Contains(departmentId.Value))
                return Forbid();

            courses = courses.Where(c => allowedDepartmentIds.Contains(c.DepartmentId)).ToList();
        }

        return Ok(courses.Select(MapCourseResponse));
    }

    // ── GET /api/v1/course/{id} ────────────────────────────────────────────────

    /// <summary>Returns a single course definition by its GUID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(id, ct);
        if (c is null)
            return NotFound();

        var scope = await ResolveEffectiveScopeAsync(c.DepartmentId, tenantId, campusId, institutionType, ct);
        if (scope.Error is not null)
            return scope.Error;

        if (!ScopeMatches(c.DepartmentId, c.TenantId, c.CampusId, (int)c.InstitutionType, scope))
            return Forbid();

        return Ok(MapCourseResponse(c));
    }

    // ── POST /api/v1/course ────────────────────────────────────────────────────

    /// <summary>Adds a new course to the catalogue. Admin and SuperAdmin only.</summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateCourseRequest request,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var department = await _departments.GetByIdAsync(request.DepartmentId, ct);
        if (department is null)
            return BadRequest("Department not found.");

        var scope = await ResolveEffectiveScopeAsync(request.DepartmentId, tenantId, campusId, institutionType, ct);
        if (scope.Error is not null)
            return scope.Error;

        if (await _repo.CodeExistsAsync(request.Code, request.DepartmentId, ct))
            return Conflict($"Course code '{request.Code}' already exists in this department.");

        var course = new Course(request.Title, request.Code, request.CreditHours, request.DepartmentId);
        course.SetInstitutionScope(department.InstitutionType, department.TenantId, department.CampusId);

        // Final-Touches Phase 19 Stage 19.1/19.2 — apply semester/duration/grading configuration
        if (request.HasSemesters)
            course.SetSemesterBased(request.TotalSemesters ?? 0, request.GradingType ?? "GPA");
        else
            course.SetNonSemesterBased(request.DurationValue ?? 0, request.DurationUnit ?? "Months", request.GradingType ?? "GPA");

        await _repo.AddAsync(course, ct);
        await _repo.SaveChangesAsync(ct);

        // Final-Touches Phase 19 Stage 19.1 — auto-create semester rows for semester-based courses
        if (request.HasSemesters && (request.TotalSemesters ?? 0) > 0)
            await _courseService.AutoCreateSemestersAsync(course.Id, ct);

        return CreatedAtAction(nameof(GetById), new { id = course.Id }, new { course.Id });
    }

    // ── PUT /api/v1/course/{id}/title ──────────────────────────────────────────

    /// <summary>Updates the course title. Admin and SuperAdmin only.</summary>
    [HttpPut("{id:guid}/title")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateTitle(
        Guid id,
        [FromBody] UpdateCourseTitleRequest request,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var course = await _repo.GetByIdAsync(id, ct);
        if (course is null) return NotFound();

        var scope = await ResolveEffectiveScopeAsync(course.DepartmentId, tenantId, campusId, institutionType, ct);
        if (scope.Error is not null)
            return scope.Error;

        if (!ScopeMatches(course.DepartmentId, course.TenantId, course.CampusId, (int)course.InstitutionType, scope))
            return Forbid();

        course.UpdateTitle(request.NewTitle);
        _repo.Update(course);
        await _repo.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── DELETE /api/v1/course/{id} ─────────────────────────────────────────────

    /// <summary>Soft-deactivates a course. Admin and SuperAdmin only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Deactivate(
        Guid id,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var course = await _repo.GetByIdAsync(id, ct);
        if (course is null) return NotFound();

        var scope = await ResolveEffectiveScopeAsync(course.DepartmentId, tenantId, campusId, institutionType, ct);
        if (scope.Error is not null)
            return scope.Error;

        if (!ScopeMatches(course.DepartmentId, course.TenantId, course.CampusId, (int)course.InstitutionType, scope))
            return Forbid();

        course.Deactivate();
        _repo.Update(course);
        await _repo.SaveChangesAsync(ct);
        return NoContent();
    }

    // ────────────────────── OFFERINGS ──────────────────────────────────────────

    // ── GET /api/v1/course/offerings?semesterId={id} or ?departmentId={id} ──────

    // Final-Touches Phase 8 Stage 8.1 — return all offerings when no filter; fix field names to match EduApiClient OfferingApiDto
    /// <summary>Returns all offerings for the given semester or department. Returns all when no filter is provided.</summary>
    [HttpGet("offerings")]
    public async Task<IActionResult> GetOfferings(
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(departmentId, tenantId, campusId, institutionType, ct);
        if (scope.Error is not null)
            return scope.Error;

        IReadOnlyList<CourseOffering> offerings;
        
        if (departmentId.HasValue)
            offerings = await _repo.GetOfferingsByDepartmentAsync(departmentId.Value, ct);
        else if (semesterId.HasValue)
            offerings = await _repo.GetOfferingsBySemesterAsync(semesterId.Value, ct);
        else
            offerings = await _repo.GetAllOfferingsAsync(ct);

        offerings = offerings
            .Where(o => ScopeMatches(o.Course.DepartmentId, o.TenantId, o.CampusId, (int)o.InstitutionType, scope))
            .ToList();

        // Issue-Fix Phase 3 Stage 3.1 — Replace Forbid with empty result; faculty sees all offerings in their depts.
        if (User.IsInRole("Faculty") && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
        {
            var userId = GetUserId();
            var allowedDepartmentIds = await _facultyAssignments.GetDepartmentIdsForFacultyAsync(userId, ct);

            if (departmentId.HasValue && !allowedDepartmentIds.Contains(departmentId.Value))
                return Ok(Array.Empty<CourseOfferingResponse>()); // Not in assigned depts — return empty instead of 403.

            offerings = offerings
                .Where(o => allowedDepartmentIds.Contains(o.Course.DepartmentId))
                .ToList();
        }

        if (User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
        {
            var allowedDepartmentIds = await _adminAssignments.GetDepartmentIdsForAdminAsync(GetUserId(), ct);
            if (departmentId.HasValue && !allowedDepartmentIds.Contains(departmentId.Value))
                return Forbid();

            offerings = offerings
                .Where(o => allowedDepartmentIds.Contains(o.Course.DepartmentId))
                .ToList();
        }

        offerings = await ApplyStudentStreamFilteringAsync(offerings, ct);

        return Ok(offerings
            .OrderBy(o => o.Course.Code)
            .ThenBy(o => o.Semester.StartDate)
            .Select(MapCourseOfferingResponse));
    }

    // ── GET /api/v1/course/offerings/my ───────────────────────────────────────

    /// <summary>
    /// Returns offerings for the current user role:
    /// - SuperAdmin/Admin: all offerings
    /// - Faculty: assigned offerings (department constrained)
    /// - Student: enrolled offerings (fallback to all offerings if claim is missing)
    /// </summary>
    [HttpGet("offerings/my")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty,Student")]
    public async Task<IActionResult> GetMyOfferings(CancellationToken ct)
    {
        // Final-Touches Phase 1 Stage 1.1 — keep "my offerings" available for all roles including SuperAdmin.
        var userId = GetUserId();
        if (userId == Guid.Empty) return Forbid();
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        if (role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)
            || role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            var all = await _repo.GetAllOfferingsAsync(ct);

            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var allowedDepartmentIds = await _adminAssignments.GetDepartmentIdsForAdminAsync(userId, ct);
                all = all.Where(o => allowedDepartmentIds.Contains(o.Course.DepartmentId)).ToList();
            }

            return Ok(all.Select(MapMyOfferingResponse));
        }

        // Issue-Fix Phase 3 Stage 3.2 — Return ALL active offerings in faculty's assigned depts (not just FacultyUserId-matched).
        // This ensures the dropdown on Assignments, Attendance, Results, and Quizzes pages is populated.
        if (role.Equals("Faculty", StringComparison.OrdinalIgnoreCase))
        {
            var allowedDepts = await _facultyAssignments.GetDepartmentIdsForFacultyAsync(userId, ct);
            var allOfferings = await _repo.GetAllOfferingsAsync(ct);

            var filtered = allOfferings.Where(o => allowedDepts.Contains(o.Course.DepartmentId));
            return Ok(filtered.Select(MapMyOfferingResponse));
        }

        if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
        {
            var studentProfileId = GetStudentProfileId();
            if (studentProfileId != Guid.Empty)
            {
                var enrollments = await _enrollments.GetByStudentAsync(studentProfileId, ct);
                IReadOnlyList<CourseOffering> offerings = enrollments
                    .Where(e => e.CourseOffering is not null)
                    .Select(e => e.CourseOffering)
                    .GroupBy(o => o!.Id)
                    .Select(g => g.First()!)
                    .ToList();

                offerings = await ApplyStudentStreamFilteringAsync(offerings, ct);

                return Ok(offerings.Select(MapMyOfferingResponse));
            }

            // Keep portal usable if legacy student tokens do not carry studentProfileId.
            var fallback = await _repo.GetAllOfferingsAsync(ct);
            fallback = await ApplyStudentStreamFilteringAsync(fallback, ct);
            return Ok(fallback.Select(MapMyOfferingResponse));
        }

        return Forbid();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────
    private Guid GetUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    private static CourseResponse MapCourseResponse(Course c)
        => new(
            c.Id,
            c.Title,
            c.Code,
            c.CreditHours,
            c.DepartmentId,
            c.TenantId,
            c.CampusId,
            (int)c.InstitutionType,
            c.Department?.Name ?? string.Empty,
            c.IsActive,
            c.HasSemesters,
            c.TotalSemesters,
            c.DurationValue,
            c.DurationUnit,
            c.GradingType,
            (int)c.CourseType);

    private static CourseOfferingResponse MapCourseOfferingResponse(CourseOffering o)
        => new(
            o.Id,
            o.CourseId,
            o.Course.Code,
            o.Course.Title,
            o.Course.DepartmentId,
            o.TenantId,
            o.CampusId,
            (int)o.InstitutionType,
            o.SemesterId,
            o.Semester.Name,
            o.FacultyUserId,
            o.MaxEnrollment,
            o.IsOpen);

    private static MyOfferingResponse MapMyOfferingResponse(CourseOffering o)
        => new(
            o.Id,
            o.Course.Title,
            o.Semester.Name,
            o.MaxEnrollment,
            o.IsOpen);

    private Guid GetStudentProfileId()
    {
        var raw = User.FindFirst("studentProfileId")?.Value;
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    private int? GetCurrentInstitutionType()
    {
        var raw = User.FindFirst("institutionType")?.Value;
        return int.TryParse(raw, out var value) ? value : null;
    }

    private Guid? GetCurrentTenantId()
    {
        var raw = User.FindFirstValue("tenantId") ?? User.FindFirstValue("tenant_id") ?? User.FindFirstValue("tid");
        return Guid.TryParse(raw, out var tenantId) ? tenantId : null;
    }

    private Guid? GetCurrentCampusId()
    {
        var raw = User.FindFirstValue("campusId") ?? User.FindFirstValue("campus_id") ?? User.FindFirstValue("cid");
        return Guid.TryParse(raw, out var campusId) ? campusId : null;
    }

    private async Task<(Guid? DepartmentId, Guid? TenantId, Guid? CampusId, int? InstitutionType, IActionResult? Error)> ResolveEffectiveScopeAsync(
        Guid? requestedDepartmentId,
        Guid? requestedTenantId,
        Guid? requestedCampusId,
        int? requestedInstitutionType,
        CancellationToken ct)
    {
        Guid? effectiveDepartmentId = requestedDepartmentId;
        Guid? effectiveTenantId = requestedTenantId;
        Guid? effectiveCampusId = requestedCampusId;
        int? effectiveInstitutionType = requestedInstitutionType;

        var callerTenantId = GetCurrentTenantId();
        var callerCampusId = GetCurrentCampusId();
        var callerInstitutionType = GetCurrentInstitutionType();

        if (!User.IsInRole("SuperAdmin"))
        {
            if (callerTenantId.HasValue)
            {
                if (requestedTenantId.HasValue && requestedTenantId.Value != callerTenantId.Value)
                    return (null, null, null, null, Forbid());
                effectiveTenantId = callerTenantId.Value;
            }

            if (callerCampusId.HasValue)
            {
                if (requestedCampusId.HasValue && requestedCampusId.Value != callerCampusId.Value)
                    return (null, null, null, null, Forbid());
                effectiveCampusId = callerCampusId.Value;
            }

            if (callerInstitutionType.HasValue)
            {
                if (requestedInstitutionType.HasValue && requestedInstitutionType.Value != callerInstitutionType.Value)
                    return (null, null, null, null, Forbid());
                effectiveInstitutionType = callerInstitutionType.Value;
            }
        }

        if (effectiveDepartmentId.HasValue)
        {
            var department = await _departments.GetByIdAsync(effectiveDepartmentId.Value, ct);
            if (department is null)
                return (null, null, null, null, NotFound(new { message = $"Department {effectiveDepartmentId} not found." }));

            if (effectiveTenantId.HasValue && department.TenantId != effectiveTenantId.Value)
                return (null, null, null, null, BadRequest(new { message = "Department tenant scope does not match the requested tenant." }));

            if (effectiveCampusId.HasValue && department.CampusId != effectiveCampusId.Value)
                return (null, null, null, null, BadRequest(new { message = "Department campus scope does not match the requested campus." }));

            if (effectiveInstitutionType.HasValue && (int)department.InstitutionType != effectiveInstitutionType.Value)
                return (null, null, null, null, BadRequest(new { message = "Department institution scope does not match the requested institution type." }));

            effectiveTenantId ??= department.TenantId;
            effectiveCampusId ??= department.CampusId;
            effectiveInstitutionType ??= (int)department.InstitutionType;
        }

        return (effectiveDepartmentId, effectiveTenantId, effectiveCampusId, effectiveInstitutionType, null);
    }

    private static bool ScopeMatches(
        Guid departmentId,
        Guid? tenantId,
        Guid? campusId,
        int institutionType,
        (Guid? DepartmentId, Guid? TenantId, Guid? CampusId, int? InstitutionType, IActionResult? Error) scope)
    {
        if (scope.DepartmentId.HasValue && departmentId != scope.DepartmentId.Value)
            return false;
        if (scope.TenantId.HasValue && tenantId != scope.TenantId.Value)
            return false;
        if (scope.CampusId.HasValue && campusId != scope.CampusId.Value)
            return false;
        if (scope.InstitutionType.HasValue && institutionType != scope.InstitutionType.Value)
            return false;
        return true;
    }

    private async Task<StudentProfile?> ResolveCurrentStudentProfileAsync(CancellationToken ct)
    {
        var studentProfileId = GetStudentProfileId();
        if (studentProfileId != Guid.Empty)
            return await _studentProfiles.GetByIdAsync(studentProfileId, ct);

        var userId = GetUserId();
        if (userId == Guid.Empty)
            return null;

        return await _studentProfiles.GetByUserIdAsync(userId, ct);
    }

    private async Task<IReadOnlyList<CourseOffering>> ApplyStudentStreamFilteringAsync(
        IReadOnlyList<CourseOffering> offerings,
        CancellationToken ct)
    {
        if (!User.IsInRole("Student") || offerings.Count == 0)
            return offerings;

        var student = await ResolveCurrentStudentProfileAsync(ct);
        if (student?.Department is null)
            return offerings;

        if (student.Department.InstitutionType != InstitutionType.School)
            return offerings;

        if (student.CurrentSemesterNumber < 9 || student.CurrentSemesterNumber > 12)
            return offerings;

        var assignment = await _schoolStreams.GetStudentAssignmentAsync(student.Id, ct);
        if (assignment is null)
            return Array.Empty<CourseOffering>();

        var stream = await _schoolStreams.GetStreamByIdAsync(assignment.SchoolStreamId, ct);
        if (stream is null)
            return Array.Empty<CourseOffering>();

        return SchoolStreamSubjectFilter.FilterOfferingsByStream(offerings, stream.Name);
    }

    // ── DELETE /api/v1/course/offerings/{id} ───────────────────────────────────

    /// <summary>Soft-deletes a course offering. Admin and SuperAdmin only.</summary>
    [HttpDelete("offerings/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteOffering(
        Guid id,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var offering = await _repo.GetOfferingByIdAsync(id, ct);
        if (offering is null) return NotFound();

        var scope = await ResolveEffectiveScopeAsync(offering.Course.DepartmentId, tenantId, campusId, institutionType, ct);
        if (scope.Error is not null)
            return scope.Error;

        if (!ScopeMatches(offering.Course.DepartmentId, offering.TenantId, offering.CampusId, (int)offering.InstitutionType, scope))
            return Forbid();

        offering.SoftDelete();
        _repo.UpdateOffering(offering);
        await _repo.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── POST /api/v1/course/offerings ─────────────────────────────────────────

    /// <summary>Creates a course offering for a semester. Admin and SuperAdmin only.</summary>
    [HttpPost("offerings")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> CreateOffering(
        [FromBody] CreateOfferingRequest request,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var course = await _repo.GetByIdAsync(request.CourseId, ct);
        if (course is null)
            return BadRequest("Course not found.");

        var scope = await ResolveEffectiveScopeAsync(course.DepartmentId, tenantId, campusId, institutionType, ct);
        if (scope.Error is not null)
            return scope.Error;

        if (!ScopeMatches(course.DepartmentId, course.TenantId, course.CampusId, (int)course.InstitutionType, scope))
            return Forbid();

        if (await _semesters.GetByIdAsync(request.SemesterId, ct) is null)
            return BadRequest("Semester not found.");

        if (request.FacultyUserId.HasValue)
        {
            var assignment = await _facultyAssignments.GetAsync(request.FacultyUserId.Value, course.DepartmentId, ct);
            if (assignment is null)
                return BadRequest("Faculty user is not actively assigned to the course department.");
        }

        var offering = new CourseOffering(request.CourseId, request.SemesterId, request.MaxEnrollment, request.FacultyUserId);
    offering.SetInstitutionScope(course.InstitutionType, course.TenantId, course.CampusId);
        await _repo.AddOfferingAsync(offering, ct);
        await _repo.SaveChangesAsync(ct);
        return Created($"/api/v1/course/offerings/{offering.Id}", new { offering.Id });
    }

    // ── PUT /api/v1/course/offerings/{id}/faculty ──────────────────────────────

    /// <summary>Assigns or re-assigns faculty to a course offering. Admin and SuperAdmin only.</summary>
    [HttpPut("offerings/{id:guid}/faculty")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AssignFaculty(
        Guid id,
        [FromBody] AssignFacultyRequest request,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var offering = await _repo.GetOfferingByIdAsync(id, ct);
        if (offering is null) return NotFound();

        var scope = await ResolveEffectiveScopeAsync(offering.Course.DepartmentId, tenantId, campusId, institutionType, ct);
        if (scope.Error is not null)
            return scope.Error;

        if (!ScopeMatches(offering.Course.DepartmentId, offering.TenantId, offering.CampusId, (int)offering.InstitutionType, scope))
            return Forbid();

        offering.AssignFaculty(request.FacultyUserId);
        _repo.UpdateOffering(offering);
        await _repo.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── PUT /api/v1/course/offerings/{id}/maxenrollment ────────────────────────

    /// <summary>Updates the maximum enrollment for a course offering. Admin and SuperAdmin only.</summary>
    [HttpPut("offerings/{id:guid}/maxenrollment")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateMaxEnrollment(
        Guid id,
        [FromBody] UpdateMaxEnrollmentRequest request,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var offering = await _repo.GetOfferingByIdAsync(id, ct);
        if (offering is null) return NotFound();

        var scope = await ResolveEffectiveScopeAsync(offering.Course.DepartmentId, tenantId, campusId, institutionType, ct);
        if (scope.Error is not null)
            return scope.Error;

        if (!ScopeMatches(offering.Course.DepartmentId, offering.TenantId, offering.CampusId, (int)offering.InstitutionType, scope))
            return Forbid();

        try
        {
            offering.UpdateMaxEnrollment(request.NewMaxEnrollment);
            _repo.UpdateOffering(offering);
            await _repo.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // ── PUT /api/v1/course/offerings/{id}/close ────────────────────────────────

    /// <summary>Closes enrollment for a course offering. Admin and SuperAdmin only.</summary>
    [HttpPut("offerings/{id:guid}/close")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> CloseOffering(
        Guid id,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var offering = await _repo.GetOfferingByIdAsync(id, ct);
        if (offering is null) return NotFound();

        var scope = await ResolveEffectiveScopeAsync(offering.Course.DepartmentId, tenantId, campusId, institutionType, ct);
        if (scope.Error is not null)
            return scope.Error;

        if (!ScopeMatches(offering.Course.DepartmentId, offering.TenantId, offering.CampusId, (int)offering.InstitutionType, scope))
            return Forbid();

        offering.Close();
        _repo.UpdateOffering(offering);
        await _repo.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── PUT /api/v1/course/offerings/{id}/reopen ───────────────────────────────

    /// <summary>Re-opens enrollment for a course offering. Admin and SuperAdmin only.</summary>
    [HttpPut("offerings/{id:guid}/reopen")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ReopenOffering(
        Guid id,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var offering = await _repo.GetOfferingByIdAsync(id, ct);
        if (offering is null) return NotFound();

        var scope = await ResolveEffectiveScopeAsync(offering.Course.DepartmentId, tenantId, campusId, institutionType, ct);
        if (scope.Error is not null)
            return scope.Error;

        if (!ScopeMatches(offering.Course.DepartmentId, offering.TenantId, offering.CampusId, (int)offering.InstitutionType, scope))
            return Forbid();

        offering.Reopen();
        _repo.UpdateOffering(offering);
        await _repo.SaveChangesAsync(ct);
        return NoContent();
    }

}
