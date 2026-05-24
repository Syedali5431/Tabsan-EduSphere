using Tabsan.EduSphere.Domain.Assignments;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>Repository interface for Assignment and AssignmentSubmission operations.</summary>
public interface IAssignmentRepository
{
    // ── Assignments ───────────────────────────────────────────────────────────

    /// <summary>Returns all assignments for the given course offering, ordered by due date.</summary>
    Task<IReadOnlyList<Assignment>> GetByOfferingAsync(Guid courseOfferingId, bool includeInactive = false, CancellationToken ct = default);

    /// <summary>Returns a single assignment by ID, or null.</summary>
    Task<Assignment?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns a single assignment by ID including inactive (soft-deleted) rows.</summary>
    Task<Assignment?> GetByIdIncludingInactiveAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns tenant/campus scope for a course offering.</summary>
    Task<(Guid? TenantId, Guid? CampusId)?> GetOfferingScopeAsync(Guid courseOfferingId, CancellationToken ct = default);

    /// <summary>Returns tenant/campus scope for a specific assignment via its course offering.</summary>
    Task<(Guid? TenantId, Guid? CampusId)?> GetAssignmentScopeAsync(Guid assignmentId, CancellationToken ct = default);

    /// <summary>Returns true when the offering already has an assignment with the given title.</summary>
    Task<bool> TitleExistsAsync(Guid courseOfferingId, string title, CancellationToken ct = default);

    /// <summary>Queues the assignment for insertion.</summary>
    Task AddAsync(Assignment assignment, CancellationToken ct = default);

    /// <summary>Marks the assignment as modified.</summary>
    void Update(Assignment assignment);

    // ── Submissions ───────────────────────────────────────────────────────────

    /// <summary>Returns the submission for the given student and assignment pair, or null.</summary>
    Task<AssignmentSubmission?> GetSubmissionAsync(Guid assignmentId, Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns all submissions for the given assignment (faculty grading view).</summary>
    Task<IReadOnlyList<AssignmentSubmission>> GetSubmissionsByAssignmentAsync(Guid assignmentId, CancellationToken ct = default);

    /// <summary>Returns all submissions made by a student across all assignments.</summary>
    Task<IReadOnlyList<AssignmentSubmission>> GetSubmissionsByStudentAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns true when the student has already submitted for this assignment.</summary>
    Task<bool> HasSubmittedAsync(Guid assignmentId, Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns the total number of submissions received for an assignment.</summary>
    Task<int> GetSubmissionCountAsync(Guid assignmentId, CancellationToken ct = default);

    /// <summary>Queues the submission for insertion.</summary>
    Task AddSubmissionAsync(AssignmentSubmission submission, CancellationToken ct = default);

    /// <summary>Marks the submission as modified (graded / rejected).</summary>
    void UpdateSubmission(AssignmentSubmission submission);

    /// <summary>Commits pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
