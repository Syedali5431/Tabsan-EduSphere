using Tabsan.EduSphere.Domain.Academic;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>Repository interface for Enrollment operations.</summary>
public interface IEnrollmentRepository
{
    /// <summary>Returns all enrollments for the given student profile.</summary>
    Task<IReadOnlyList<Enrollment>> GetByStudentAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns all active enrollments for the given course offering.</summary>
    Task<IReadOnlyList<Enrollment>> GetByOfferingAsync(Guid courseOfferingId, CancellationToken ct = default);

    /// <summary>Returns all waitlisted enrollments for the given course offering, ordered by queue time.</summary>
    Task<IReadOnlyList<Enrollment>> GetWaitlistedByOfferingAsync(Guid courseOfferingId, CancellationToken ct = default);

    // Final-Touches Phase 8 Stage 8.2 — admin drop by enrollment ID
    /// <summary>Returns the enrollment with the given ID, or null.</summary>
    Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns the enrollment for the given student + offering pair, or null.</summary>
    Task<Enrollment?> GetAsync(Guid studentProfileId, Guid courseOfferingId, CancellationToken ct = default);

    /// <summary>Returns true when the student already has an active enrollment in the offering.</summary>
    Task<bool> IsEnrolledAsync(Guid studentProfileId, Guid courseOfferingId, CancellationToken ct = default);

    /// <summary>Queues the enrollment for insertion.</summary>
    Task AddAsync(Enrollment enrollment, CancellationToken ct = default);

    /// <summary>Marks the enrollment as modified (status change).</summary>
    void Update(Enrollment enrollment);

    /// <summary>Commits changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
