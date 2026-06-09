using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>EF Core implementation of ICourseRepository.</summary>
public class CourseRepository : ICourseRepository
{
    private readonly ApplicationDbContext _db;
    public CourseRepository(ApplicationDbContext db) => _db = db;

    /// <summary>Returns all courses, optionally filtered by department and/or HasSemesters, ordered by code.</summary>
    public async Task<IReadOnlyList<Course>> GetAllAsync(Guid? departmentId = null, bool? hasSemesters = null, CancellationToken ct = default)
    {
        var query = _db.Courses.Include(c => c.Department).AsQueryable();
        if (departmentId.HasValue)
            query = query.Where(c => c.DepartmentId == departmentId.Value);
        if (hasSemesters.HasValue)
            query = query.Where(c => c.HasSemesters == hasSemesters.Value);
        return await query.OrderBy(c => c.Code).ToListAsync(ct);
    }

    /// <summary>Returns the course with the given ID, or null.</summary>
    public Task<Course?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <summary>Returns true when the code+department combination already exists.</summary>
    public Task<bool> CodeExistsAsync(string code, Guid departmentId, CancellationToken ct = default)
        => _db.Courses.AnyAsync(c => c.Code == code.ToUpperInvariant() && c.DepartmentId == departmentId, ct);

    /// <summary>Queues the course for insertion.</summary>
    public async Task AddAsync(Course course, CancellationToken ct = default)
        => await _db.Courses.AddAsync(course, ct);

    /// <summary>Marks the course as modified.</summary>
    public void Update(Course course) => _db.Courses.Update(course);

    /// <summary>Returns all course offerings with Course and Semester navigation loaded, ordered by course code.</summary>
    // Final-Touches Phase 8 Stage 8.1 — all-offerings query for enrollment dropdown
    public async Task<IReadOnlyList<CourseOffering>> GetAllOfferingsAsync(CancellationToken ct = default)
        => await _db.CourseOfferings
                    .Include(o => o.Course)
                    .Include(o => o.Semester)
                    .OrderBy(o => o.Course.Code)
                    .ToListAsync(ct);

    /// <summary>Returns all offerings for a semester with Course, Semester navigation loaded, ordered by course code.</summary>
    public async Task<IReadOnlyList<CourseOffering>> GetOfferingsBySemesterAsync(Guid semesterId, CancellationToken ct = default)
        => await _db.CourseOfferings
                    .Include(o => o.Course)
                    .Include(o => o.Semester)
                    .Where(o => o.SemesterId == semesterId)
                    .OrderBy(o => o.Course.Code)
                    .ToListAsync(ct);

    /// <summary>Returns all offerings for a department (filtered by course.departmentId) with related entities loaded, ordered by course code.</summary>
    public async Task<IReadOnlyList<CourseOffering>> GetOfferingsByDepartmentAsync(Guid departmentId, CancellationToken ct = default)
        => await _db.CourseOfferings
                    .Include(o => o.Course)
                    .Include(o => o.Semester)
                    .Where(o => o.Course.DepartmentId == departmentId)
                    .OrderBy(o => o.Course.Code)
                    .ToListAsync(ct);

    /// <summary>Returns all offerings assigned to the given faculty user.</summary>
    public async Task<IReadOnlyList<CourseOffering>> GetOfferingsByFacultyAsync(Guid facultyUserId, CancellationToken ct = default)
        => await _db.CourseOfferings
                    .Include(o => o.Course)
                    .Include(o => o.Semester)
                    .Where(o => o.FacultyUserId == facultyUserId)
                    .ToListAsync(ct);

    /// <summary>Returns the offering with the given ID, with Course and Semester loaded, or null.</summary>
    public Task<CourseOffering?> GetOfferingByIdAsync(Guid offeringId, CancellationToken ct = default)
        => _db.CourseOfferings
              .Include(o => o.Course)
              .Include(o => o.Semester)
              .FirstOrDefaultAsync(o => o.Id == offeringId, ct);

    /// <summary>Returns the count of active (non-dropped) enrollments for the given offering.</summary>
    public Task<int> GetEnrollmentCountAsync(Guid offeringId, CancellationToken ct = default)
        => _db.Enrollments.CountAsync(e => e.CourseOfferingId == offeringId && e.Status == EnrollmentStatus.Active, ct);

    /// <summary>Queues the offering for insertion.</summary>
    public async Task AddOfferingAsync(CourseOffering offering, CancellationToken ct = default)
        => await _db.CourseOfferings.AddAsync(offering, ct);

    /// <summary>Marks the offering as modified.</summary>
    public void UpdateOffering(CourseOffering offering) => _db.CourseOfferings.Update(offering);

    /// <summary>Commits pending changes.</summary>
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
