using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Domain.Assignments;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Contract for assignment lifecycle and submission grading operations.
/// Faculty can create, publish, retract, and grade assignments.
/// Students can submit and view their own submissions.
/// All operations enforce department/offering access boundaries.
/// </summary>
public interface IAssignmentService
{
    // ── Assignment management (faculty) ───────────────────────────────────────

    /// <summary>Creates an unpublished assignment for the given course offering.</summary>
    Task<AssignmentResponse> CreateAsync(CreateAssignmentRequest request, Guid createdByUserId, CancellationToken ct = default);

    /// <summary>Updates editable fields of a draft (unpublished) assignment.</summary>
    Task<bool> UpdateAsync(Guid assignmentId, UpdateAssignmentRequest request, CancellationToken ct = default);

    /// <summary>Publishes an assignment, making it visible to enrolled students.</summary>
    Task<bool> PublishAsync(Guid assignmentId, CancellationToken ct = default);

    /// <summary>Re-activates an inactive assignment.</summary>
    Task<bool> ActivateAsync(Guid assignmentId, CancellationToken ct = default);

    /// <summary>
    /// Retracts a published assignment.
    /// Fails when submissions already exist (returns false).
    /// </summary>
    Task<bool> RetractAsync(Guid assignmentId, CancellationToken ct = default);

    /// <summary>Soft-deletes an assignment (only allowed when no submissions exist).</summary>
    Task<bool> DeleteAsync(Guid assignmentId, CancellationToken ct = default);

    /// <summary>Deactivates an assignment without permanently deleting data.</summary>
    Task<bool> DeactivateAsync(Guid assignmentId, CancellationToken ct = default);

    /// <summary>Returns all assignments for a course offering.</summary>
    Task<IReadOnlyList<AssignmentResponse>> GetByOfferingAsync(Guid courseOfferingId, bool includeInactive = false, CancellationToken ct = default);

    /// <summary>Returns a single assignment by ID, or null.</summary>
    Task<AssignmentResponse?> GetByIdAsync(Guid assignmentId, CancellationToken ct = default);

    // ── Submission operations (students) ──────────────────────────────────────

    /// <summary>
    /// Submits a student's work for an assignment.
    /// Enforces: assignment published, not past due, no duplicate submission.
    /// Returns null on rejection.
    /// </summary>
    Task<SubmissionResponse?> SubmitAsync(Guid studentProfileId, SubmitAssignmentRequest request, CancellationToken ct = default);

    /// <summary>Returns all submissions made by the student, with assignment titles.</summary>
    Task<IReadOnlyList<SubmissionResponse>> GetMySubmissionsAsync(Guid studentProfileId, CancellationToken ct = default);

    // ── Grading operations (faculty) ──────────────────────────────────────────

    /// <summary>Returns all submissions for an assignment (faculty grading view).</summary>
    Task<IReadOnlyList<SubmissionResponse>> GetSubmissionsByAssignmentAsync(Guid assignmentId, CancellationToken ct = default);

    /// <summary>Grades a student's submission, recording marks and feedback.</summary>
    Task<bool> GradeSubmissionAsync(GradeSubmissionRequest request, Guid gradedByUserId, CancellationToken ct = default);

    /// <summary>Rejects a submission (e.g. plagiarism or policy violation).</summary>
    Task<bool> RejectSubmissionAsync(Guid assignmentId, Guid studentProfileId, CancellationToken ct = default);
}
