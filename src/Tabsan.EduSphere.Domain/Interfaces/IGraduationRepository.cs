// Final-Touches Phase 18 Stage 18.1/18.2 — Graduation repository interface

using Tabsan.EduSphere.Domain.Academic;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>
/// Repository contract for GraduationApplication persistence.
/// </summary>
public interface IGraduationRepository
{
    // Final-Touches Phase 18 Stage 18.1 — application queries
    /// <summary>Returns all applications for a specific student profile.</summary>
    Task<IReadOnlyList<GraduationApplication>> GetByStudentAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns a paged application slice for a specific student profile.</summary>
    Task<IReadOnlyList<GraduationApplication>> GetByStudentPagedAsync(Guid studentProfileId, int skip, int take, CancellationToken ct = default);

    /// <summary>Returns the total number of applications for a specific student profile.</summary>
    Task<int> CountByStudentAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns the active (non-rejected, non-soft-deleted) application for a student, or null.</summary>
    Task<GraduationApplication?> GetActiveByStudentAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns a single application by ID with Approvals and StudentProfile loaded.</summary>
    Task<GraduationApplication?> GetByIdAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>Returns all applications with a given status (for admin/faculty dashboards).</summary>
    Task<IReadOnlyList<GraduationApplication>> GetByStatusAsync(GraduationApplicationStatus status, CancellationToken ct = default);

    /// <summary>Returns all applications visible to an Admin (scoped to their department).</summary>
    Task<IReadOnlyList<GraduationApplication>> GetByDepartmentAsync(Guid departmentId, GraduationApplicationStatus? status, CancellationToken ct = default);

    /// <summary>Returns a paged application slice visible to an Admin (scoped to their department).</summary>
    Task<IReadOnlyList<GraduationApplication>> GetByDepartmentPagedAsync(Guid departmentId, GraduationApplicationStatus? status, int skip, int take, CancellationToken ct = default);

    /// <summary>Returns total applications visible to an Admin (scoped to their department).</summary>
    Task<int> CountByDepartmentAsync(Guid departmentId, GraduationApplicationStatus? status, CancellationToken ct = default);

    /// <summary>Returns all applications (SuperAdmin full view), optionally filtered by status.</summary>
    Task<IReadOnlyList<GraduationApplication>> GetAllAsync(GraduationApplicationStatus? status, CancellationToken ct = default);

    /// <summary>Returns a paged application slice (SuperAdmin full view), optionally filtered by status.</summary>
    Task<IReadOnlyList<GraduationApplication>> GetAllPagedAsync(GraduationApplicationStatus? status, int skip, int take, CancellationToken ct = default);

    /// <summary>Returns total applications (SuperAdmin full view), optionally filtered by status.</summary>
    Task<int> CountAllAsync(GraduationApplicationStatus? status, CancellationToken ct = default);

    /// <summary>Queues a new application for insertion.</summary>
    Task AddAsync(GraduationApplication application, CancellationToken ct = default);

    /// <summary>Marks an application as modified.</summary>
    void Update(GraduationApplication application);

    // Final-Touches Phase 18 Stage 18.2 — helper query for certificate generation
    /// <summary>Returns the student's display name (full name from User) for the certificate.</summary>
    Task<string> GetStudentDisplayNameAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns the student's registration number for the certificate.</summary>
    Task<string> GetStudentRegistrationNumberAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns the program name for the student's profile for the certificate.</summary>
    Task<string> GetStudentProgramNameAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns true when the student has at least one completed FYP project.</summary>
    Task<bool> HasCompletedFypProjectAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns the User IDs of Faculty assigned to the student's department (for notifications).</summary>
    Task<IReadOnlyList<Guid>> GetFacultyUserIdsByDepartmentAsync(Guid departmentId, CancellationToken ct = default);

    /// <summary>Returns the User IDs of Admins assigned to the student's department (for notifications).</summary>
    Task<IReadOnlyList<Guid>> GetAdminUserIdsByDepartmentAsync(Guid departmentId, CancellationToken ct = default);

    /// <summary>Returns the User IDs of all SuperAdmin users (for notifications).</summary>
    Task<IReadOnlyList<Guid>> GetSuperAdminUserIdsAsync(CancellationToken ct = default);

    /// <summary>Commits all pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
