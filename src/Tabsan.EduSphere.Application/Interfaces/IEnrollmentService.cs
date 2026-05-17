using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Domain.Academic;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Contract for enrollment workflow operations.
/// Enforces seat-limit checks, duplicate enrollment guards, and semester-closed guards.
/// </summary>
public interface IEnrollmentService
{
    /// <summary>
    /// Enrolls the student into the requested course offering.
    /// Returns null when enrollment is rejected (offering full, closed semester, duplicate, etc.).
    /// </summary>
    Task<EnrollmentResponse?> EnrollAsync(Guid studentProfileId, EnrollRequest request, CancellationToken ct = default);

    /// <summary>
    /// Drops an active enrollment for the student.
    /// Returns false when the enrollment does not exist or is not currently active.
    /// </summary>
    Task<bool> DropAsync(Guid studentProfileId, Guid courseOfferingId, CancellationToken ct = default);

    /// <summary>Returns all enrollment records for the given student (history + active).</summary>
    Task<IReadOnlyList<Enrollment>> GetForStudentAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns all active enrollments in the given course offering (faculty roster view).</summary>
    Task<IReadOnlyList<Enrollment>> GetForOfferingAsync(Guid courseOfferingId, CancellationToken ct = default);

    /// <summary>Returns the waitlisted enrollments in queue order for the given course offering.</summary>
    Task<IReadOnlyList<Enrollment>> GetWaitlistedForOfferingAsync(Guid courseOfferingId, CancellationToken ct = default);

    // Final-Touches Phase 8 Stage 8.2 — admin drop any enrollment by its ID
    /// <summary>Drops any active enrollment identified by its enrollment ID. Returns false when not found or not active.</summary>
    Task<bool> AdminDropByIdAsync(Guid enrollmentId, CancellationToken ct = default);

    // Final-Touches Phase 15 Stages 15.1 & 15.2 — TryEnrollAsync: rich enrollment attempt with rules
    /// <summary>
    /// Attempts enrollment with full Phase 15 rule checks (prerequisite validation + timetable clash detection).
    /// Returns a rich result describing success or the specific rejection reason.
    /// </summary>
    Task<EnrollmentAttemptResult> TryEnrollAsync(
        Guid studentProfileId,
        Guid courseOfferingId,
        bool overrideClash = false,
        string? overrideReason = null,
        CancellationToken ct = default);
}
