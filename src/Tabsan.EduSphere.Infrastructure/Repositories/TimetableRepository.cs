using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of ITimetableRepository.
/// Timetables are soft-deleted via the AuditableEntity pattern;
/// the query filter on TimetableConfiguration excludes them automatically.
/// </summary>
public class TimetableRepository : ITimetableRepository
{
    private readonly ApplicationDbContext _db;

    public TimetableRepository(ApplicationDbContext db) => _db = db;

    public async Task<IList<Timetable>> GetByDepartmentAsync(Guid departmentId, CancellationToken ct = default)
        => await _db.Timetables
              .Where(t => t.DepartmentId == departmentId)
              .Include(t => t.Department)
              .Include(t => t.AcademicProgram)
              .Include(t => t.Semester)
              .OrderByDescending(t => t.CreatedAt)
              .ToListAsync(ct);

    public async Task<IList<Timetable>> GetPublishedByDepartmentAsync(Guid departmentId, CancellationToken ct = default)
        => await _db.Timetables
              .Where(t => t.DepartmentId == departmentId && t.IsPublished)
              .Include(t => t.Entries)
              .Include(t => t.Department)
              .Include(t => t.AcademicProgram)
              .Include(t => t.Semester)
              .OrderByDescending(t => t.PublishedAt)
              .ToListAsync(ct);

    public Task<Timetable?> GetByIdWithEntriesAsync(Guid timetableId, CancellationToken ct = default)
        => _db.Timetables
              .Include(t => t.Department)
              .Include(t => t.AcademicProgram)
              .Include(t => t.Semester)
              .Include(t => t.Entries.OrderBy(e => e.DayOfWeek).ThenBy(e => e.StartTime))
                  .ThenInclude(e => e.Room)
                      .ThenInclude(r => r!.Building)
              .Include(t => t.Entries)
                  .ThenInclude(e => e.Building)
              .FirstOrDefaultAsync(t => t.Id == timetableId, ct);

    public Task<Timetable?> GetByIdAsync(Guid timetableId, CancellationToken ct = default)
        => _db.Timetables.FirstOrDefaultAsync(t => t.Id == timetableId, ct);

    public async Task AddAsync(Timetable timetable, CancellationToken ct = default)
        => await _db.Timetables.AddAsync(timetable, ct);

    public async Task AddEntryAsync(TimetableEntry entry, CancellationToken ct = default)
        => await _db.TimetableEntries.AddAsync(entry, ct);

    public void Update(Timetable timetable) => _db.Timetables.Update(timetable);

    public void UpdateEntry(TimetableEntry entry) => _db.TimetableEntries.Update(entry);

    public void RemoveEntry(TimetableEntry entry) => _db.TimetableEntries.Remove(entry);

    public Task<TimetableEntry?> GetEntryByIdAsync(Guid entryId, CancellationToken ct = default)
        => _db.TimetableEntries.FirstOrDefaultAsync(e => e.Id == entryId, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    public async Task<IList<TimetableEntry>> GetTeacherEntriesAsync(
        Guid facultyUserId,
        Guid? tenantId,
        Guid? campusId,
        bool includeInactive,
        CancellationToken ct = default)
    {
        var query = _db.TimetableEntries
            .Include(e => e.Timetable)
                .ThenInclude(t => t.AcademicProgram)
            .Include(e => e.Timetable)
                .ThenInclude(t => t.Semester)
            .Include(e => e.Building)
            .Include(e => e.Room)
                .ThenInclude(r => r!.Building)
            .Where(e => e.FacultyUserId == facultyUserId);

        if (!includeInactive)
            query = query.Where(e => e.Timetable.IsPublished);

        if (tenantId.HasValue)
            query = query.Where(e => e.Timetable.Department.TenantId == tenantId.Value);

        if (campusId.HasValue)
            query = query.Where(e => e.Timetable.Department.CampusId == campusId.Value);

        return await query
            .OrderBy(e => e.DayOfWeek)
            .ThenBy(e => e.StartTime)
            .ToListAsync(ct);
    }

    // Final-Touches Phase 15 Stage 15.2 — GetEntriesByCourseOfferingAsync: timetable clash detection
    /// <summary>
    /// Returns published timetable entries whose CourseId matches <paramref name="courseId"/>
    /// within the specified semester. Used to detect schedule conflicts before enrollment.
    /// </summary>
    public async Task<IList<TimetableEntry>> GetEntriesByCourseOfferingAsync(Guid courseId, Guid semesterId, CancellationToken ct = default)
        => await _db.TimetableEntries
              .Where(e => e.CourseId == courseId
                       && e.Timetable.SemesterId == semesterId
                       && e.Timetable.IsPublished)
              .OrderBy(e => e.DayOfWeek)
              .ThenBy(e => e.StartTime)
              .ToListAsync(ct);
}
