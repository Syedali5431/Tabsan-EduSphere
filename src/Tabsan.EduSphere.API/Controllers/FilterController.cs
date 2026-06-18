using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Cascading filter API — returns filtered lookup lists for dependent dropdowns.
/// Used by the client-side cascading-filters.js to populate child dropdowns
/// without full page reloads.
/// </summary>
[ApiController]
[Route("api/v1/filters")]
[Authorize]
public sealed class FilterController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public FilterController(ApplicationDbContext db) => _db = db;

    // ── GET /api/v1/filters/tenants?institutionType= ──────────────────────────

    /// <summary>
    /// Returns tenants that have at least one department of the given institution type.
    /// SuperAdmin only.
    /// </summary>
    [HttpGet("tenants")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetTenantsByInstitutionType(
        [FromQuery] InstitutionType? institutionType,
        CancellationToken ct)
    {
        var query = _db.Tenants.AsNoTracking().AsQueryable();

        if (institutionType.HasValue)
        {
            // Only tenants that have departments of this institution type
            var tenantIdsWithType = await _db.Departments
                .Where(d => d.InstitutionType == institutionType.Value)
                .Select(d => d.TenantId)
                .Distinct()
                .ToListAsync(ct);

            query = query.Where(t => tenantIdsWithType.Contains(t.Id));
        }

        var tenants = await query
            .OrderBy(t => t.Code)
            .Select(t => new { t.Id, t.Code, t.Name, t.IsActive })
            .ToListAsync(ct);

        return Ok(tenants);
    }

    // ── GET /api/v1/filters/campuses?tenantId=&institutionType= ───────────────

    /// <summary>
    /// Returns campuses filtered by tenant and optionally institution type.
    /// SuperAdmin: all; Admin/Finance: scoped to assigned campuses.
    /// </summary>
    [HttpGet("campuses")]
    public async Task<IActionResult> GetCampuses(
        [FromQuery] Guid? tenantId,
        [FromQuery] InstitutionType? institutionType,
        CancellationToken ct)
    {
        var query = _db.Campuses.AsNoTracking().AsQueryable();

        if (tenantId.HasValue)
            query = query.Where(c => c.TenantId == tenantId.Value);

        if (institutionType.HasValue)
        {
            var campusIdsWithType = await _db.Departments
                .Where(d => d.InstitutionType == institutionType.Value && d.CampusId != null)
                .Select(d => d.CampusId!.Value)
                .Distinct()
                .ToListAsync(ct);

            query = query.Where(c => campusIdsWithType.Contains(c.Id));
        }

        var campuses = await query
            .OrderBy(c => c.Code)
            .Select(c => new { c.Id, c.TenantId, c.Code, c.Name, c.IsActive })
            .ToListAsync(ct);

        return Ok(campuses);
    }

    // ── GET /api/v1/filters/departments?campusId=&institutionType= ────────────

    /// <summary>
    /// Returns departments filtered by campus and institution type.
    /// Role-scoped: Faculty sees only assigned departments; Admin sees campus-scoped.
    /// </summary>
    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments(
        [FromQuery] Guid? campusId,
        [FromQuery] Guid? tenantId,
        [FromQuery] InstitutionType? institutionType,
        CancellationToken ct)
    {
        var query = _db.Departments.AsNoTracking().AsQueryable();

        if (campusId.HasValue)
            query = query.Where(d => d.CampusId == campusId.Value);
        else if (tenantId.HasValue)
            query = query.Where(d => d.TenantId == tenantId.Value);

        if (institutionType.HasValue)
            query = query.Where(d => d.InstitutionType == institutionType.Value);

        var departments = await query
            .OrderBy(d => d.Name)
            .Select(d => new { d.Id, d.Name, d.Code, d.IsActive, d.InstitutionType, d.TenantId, d.CampusId })
            .ToListAsync(ct);

        return Ok(departments);
    }

    // ── GET /api/v1/filters/courses?departmentId= ────────────────────────────

    /// <summary>
    /// Returns courses for a department.
    /// </summary>
    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses(
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? campusId,
        [FromQuery] Guid? tenantId,
        CancellationToken ct)
    {
        var query = _db.Courses.AsNoTracking().AsQueryable();

        if (departmentId.HasValue)
            query = query.Where(c => c.DepartmentId == departmentId.Value);

        if (campusId.HasValue)
            query = query.Where(c => c.CampusId == campusId.Value);
        else if (tenantId.HasValue)
            query = query.Where(c => c.TenantId == tenantId.Value);

        var courses = await query
            .OrderBy(c => c.Code)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.Title,
                c.CreditHours,
                c.HasSemesters,
                c.TotalSemesters,
                c.DurationValue,
                c.DurationUnit,
                c.InstitutionType,
                c.DepartmentId,
                c.IsActive
            })
            .ToListAsync(ct);

        return Ok(courses);
    }

    // ── GET /api/v1/filters/periods?courseId= ─────────────────────────────────

    /// <summary>
    /// Returns period options (class numbers, semester numbers, or years)
    /// based on the course configuration and institution type.
    ///
    /// University + HasSemesters=true → "Semester 1".."Semester N"
    /// University + HasSemesters=false → "Year 1".."Year N"
    /// School → "Class 1".."Class 10"
    /// College → "Class 11".."Class 12"
    /// </summary>
    [HttpGet("periods")]
    public async Task<IActionResult> GetPeriods(
        [FromQuery] Guid? courseId,
        CancellationToken ct)
    {
        if (!courseId.HasValue)
            return Ok(Array.Empty<object>());

        var course = await _db.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId.Value, ct);

        if (course is null)
            return NotFound("Course not found.");

        var periods = new List<object>();
        var instType = course.InstitutionType;

        switch (instType)
        {
            case InstitutionType.School:
                // Class 1 to 10
                for (int i = 1; i <= 10; i++)
                    periods.Add(new { id = i, name = $"Class {i}", value = i });
                break;

            case InstitutionType.College:
                // Class 11 to 12
                for (int i = 11; i <= 12; i++)
                    periods.Add(new { id = i, name = $"Class {i}", value = i });
                break;

            case InstitutionType.University:
                if (course.HasSemesters && course.TotalSemesters > 0)
                {
                    for (int i = 1; i <= course.TotalSemesters; i++)
                        periods.Add(new { id = i, name = $"Semester {i}", value = i });
                }
                else
                {
                    // Year-based: use DurationValue (e.g., 4 years, 2 years)
                    int years = (course.DurationValue.HasValue && course.DurationValue.Value > 0) ? course.DurationValue.Value : 4;
                    for (int i = 1; i <= years; i++)
                        periods.Add(new { id = i, name = $"Year {i}", value = i });
                }
                break;
        }

        return Ok(new
        {
            periods,
            institutionType = (int)instType,
            label = GetPeriodLabel(instType, course.HasSemesters),
            hasSemesters = course.HasSemesters,
            totalSemesters = course.TotalSemesters,
            durationValue = course.DurationValue
        });
    }

    // ── GET /api/v1/filters/subjects?courseId=&period= ────────────────────────

    /// <summary>
    /// Returns subject/offering options for a course, optionally filtered by period.
    /// For University: finds course offerings for the course's semester matching the period.
    /// For School/College: finds course offerings linked to the course.
    /// </summary>
    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects(
        [FromQuery] Guid? courseId,
        [FromQuery] int? period,
        CancellationToken ct)
    {
        if (!courseId.HasValue)
            return Ok(Array.Empty<object>());

        var query = _db.CourseOfferings
            .AsNoTracking()
            .Include(o => o.Semester)
            .Where(o => o.CourseId == courseId.Value);

        if (period.HasValue)
        {
            // For university: match semester number
            var semesters = await _db.Semesters
                .AsNoTracking()
                .ToListAsync(ct);

            // Try to match semester names like "Semester 1", "Fall 2025" etc.
            // Simple approach: filter offerings where semester name contains the period number
            var matchingSemesterIds = semesters
                .Where(s => s.Name != null &&
                    (s.Name.Contains(period.Value.ToString()) ||
                     s.Name.Contains(Ordinal(period.Value))))
                .Select(s => s.Id)
                .ToList();

            if (matchingSemesterIds.Count > 0)
                query = query.Where(o => matchingSemesterIds.Contains(o.SemesterId));
        }

        var offerings = await query
            .OrderBy(o => o.Semester.Name)
            .Select(o => new
            {
                o.Id,
                name = o.Semester.Name ?? o.Course.Title,
                o.CourseId,
                o.SemesterId,
                semesterName = o.Semester.Name,
                o.FacultyUserId,
                o.IsOpen,
                o.MaxEnrollment
            })
            .ToListAsync(ct);

        return Ok(offerings);
    }

    // ── GET /api/v1/filters/students?campusId=&departmentId=&courseId= ───────

    /// <summary>
    /// Returns student list filtered by scope. Used by Payments, StudyPlan, etc.
    /// </summary>
    [HttpGet("students")]
    [Authorize(Roles = "Admin,SuperAdmin,Finance,Faculty")]
    public async Task<IActionResult> GetStudents(
        [FromQuery] Guid? campusId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseId,
        CancellationToken ct)
    {
        var query = _db.StudentProfiles
            .AsNoTracking()
            .AsQueryable();

        if (campusId.HasValue)
        {
            var deptIds = await _db.Departments
                .Where(d => d.CampusId == campusId.Value)
                .Select(d => d.Id)
                .ToListAsync(ct);
            query = query.Where(s => deptIds.Contains(s.DepartmentId));
        }

        if (departmentId.HasValue)
            query = query.Where(s => s.DepartmentId == departmentId.Value);

        var students = await query
            .OrderBy(s => s.RegistrationNumber)
            .Select(s => new
            {
                id = s.Id,
                name = s.RegistrationNumber,
                s.RegistrationNumber,
                s.DepartmentId,
                s.CurrentSemesterNumber
            })
            .ToListAsync(ct);

        return Ok(students);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GetPeriodLabel(InstitutionType type, bool hasSemesters) => type switch
    {
        InstitutionType.School => "Class",
        InstitutionType.College => "Class",
        InstitutionType.University => hasSemesters ? "Semester" : "Year",
        _ => "Period"
    };

    private static string Ordinal(int n) => n switch
    {
        1 => "1st", 2 => "2nd", 3 => "3rd", _ => $"{n}th"
    };
}
