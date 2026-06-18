using Tabsan.EduSphere.Domain.Academic;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>
/// Repository contract for Timetable aggregate operations.
/// Timetables are created by Admins and published to department members.
/// </summary>
public interface ITimetableRepository
{
    /// <summary>Returns all timetables for a department (both draft and published).</summary>
    Task<IList<Timetable>> GetByDepartmentAsync(Guid departmentId, CancellationToken ct = default);

    /// <summary>Returns all published timetables for a department. Used by students and faculty.</summary>
    Task<IList<Timetable>> GetPublishedByDepartmentAsync(Guid departmentId, CancellationToken ct = default);

    /// <summary>Returns a timetable by ID with all entries loaded.</summary>
    Task<Timetable?> GetByIdWithEntriesAsync(Guid timetableId, CancellationToken ct = default);

    /// <summary>Returns a timetable by ID (no entries loaded — for header-only operations).</summary>
    Task<Timetable?> GetByIdAsync(Guid timetableId, CancellationToken ct = default);

    /// <summary>Creates a new timetable.</summary>
    Task AddAsync(Timetable timetable, CancellationToken ct = default);

    /// <summary>Adds a single entry to a timetable.</summary>
    Task AddEntryAsync(TimetableEntry entry, CancellationToken ct = default);

    /// <summary>Marks the timetable entity as modified.</summary>
    void Update(Timetable timetable);

    /// <summary>Marks a timetable entry as modified.</summary>
    void UpdateEntry(TimetableEntry entry);

    /// <summary>Removes a timetable entry.</summary>
    void RemoveEntry(TimetableEntry entry);

    /// <summary>Returns a specific timetable entry by ID.</summary>
    Task<TimetableEntry?> GetEntryByIdAsync(Guid entryId, CancellationToken ct = default);

    /// <summary>Commits all pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    /// <summary>
    /// Returns all published timetable entries where the assigned faculty user matches <paramref name="facultyUserId"/>.
    /// Loads Timetable (with AcademicProgram, Semester), Room, and Building navigations.
    /// Used by the teacher dashboard view.
    /// </summary>
    Task<IList<TimetableEntry>> GetTeacherEntriesAsync(
        Guid facultyUserId,
        Guid? tenantId,
        Guid? campusId,
        bool includeInactive,
        CancellationToken ct = default);

    // Final-Touches Phase 15 Stage 15.2 — GetEntriesByCourseOfferingAsync: timetable clash detection
    /// <summary>
    /// Returns all published timetable entries that belong to the given course within the given semester.
    /// Used to detect timetable clashes before enrollment.
    /// </summary>
    Task<IList<TimetableEntry>> GetEntriesByCourseOfferingAsync(Guid courseId, Guid semesterId, CancellationToken ct = default);

    /// <summary>
    /// Returns all published timetables for a department at a specific semester number.
    /// Used to deactivate old timetables when a student is promoted.
    /// </summary>
    Task<IList<Timetable>> GetPublishedByDepartmentAndSemesterAsync(Guid departmentId, int semesterNumber, CancellationToken ct = default);
}
