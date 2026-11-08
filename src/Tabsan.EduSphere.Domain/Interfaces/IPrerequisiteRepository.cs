using Tabsan.EduSphere.Domain.Academic;

namespace Tabsan.EduSphere.Domain.Interfaces;

// Final-Touches Phase 15 Stage 15.1 — IPrerequisiteRepository: contract for prerequisite management
/// <summary>Repository contract for CoursePrerequisite operations (Phase 15).</summary>
public interface IPrerequisiteRepository
{
    /// <summary>Returns all prerequisites for the given course (with PrerequisiteCourse loaded).</summary>
    Task<IReadOnlyList<CoursePrerequisite>> GetByCourseIdAsync(Guid courseId, CancellationToken ct = default);

    /// <summary>
    /// Returns the prerequisites of every course in the department, in one query (Course and PrerequisiteCourse loaded):
    /// the portal's Prerequisites page asked course by course before, hundreds of calls for one page.
    /// </summary>
    Task<IReadOnlyList<CoursePrerequisite>> GetByDepartmentIdAsync(Guid departmentId, CancellationToken ct = default);

    /// <summary>Returns true when the prerequisite link already exists.</summary>
    Task<bool> ExistsAsync(Guid courseId, Guid prerequisiteCourseId, CancellationToken ct = default);

    /// <summary>Queues the prerequisite link for insertion.</summary>
    Task AddAsync(CoursePrerequisite prerequisite, CancellationToken ct = default);

    /// <summary>Removes the prerequisite link (if it exists).</summary>
    Task RemoveAsync(Guid courseId, Guid prerequisiteCourseId, CancellationToken ct = default);

    /// <summary>Commits pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
