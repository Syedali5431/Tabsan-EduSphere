using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Assignments;

/// <summary>
/// Orchestrates the full assignment lifecycle:
///   - Faculty creates, edits, publishes, and retracts assignments.
///   - Students submit their work (one submission per student per assignment).
///   - Faculty grade and reject submissions.
/// Business invariants enforced here:
///   - Assignments can only be edited while unpublished.
///   - Submissions require the assignment to be published and within the due date.
///   - Duplicate submissions are rejected.
///   - Marks awarded must not exceed the assignment's MaxMarks.
/// </summary>
public class AssignmentService : IAssignmentService
{
    private readonly IAssignmentRepository _repo;
    private readonly IAuditService _audit;

    public AssignmentService(IAssignmentRepository repo, IAuditService audit)
    {
        _repo = repo;
        _audit = audit;
    }

    // ── Assignment management ─────────────────────────────────────────────────

    /// <summary>
    /// Creates a new unpublished assignment for the given course offering.
    /// Returns the created assignment as a response DTO.
    /// </summary>
    public async Task<AssignmentResponse> CreateAsync(CreateAssignmentRequest request, Guid createdByUserId, CancellationToken ct = default)
    {
        var assignment = new Assignment(
            request.CourseOfferingId,
            request.Title,
            request.Description,
            request.DueDate,
            request.MaxMarks);

        await _repo.AddAsync(assignment, ct);
        await _repo.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditLog("CreateAssignment", "Assignment", assignment.Id.ToString(),
            actorUserId: createdByUserId), ct);

        var scope = await _repo.GetOfferingScopeAsync(assignment.CourseOfferingId, ct);
        return ToResponse(assignment, submissionCount: 0, scope?.TenantId, scope?.CampusId);
    }

    /// <summary>
    /// Updates editable fields on a draft assignment.
    /// Returns false when the assignment is not found or is already published.
    /// </summary>
    public async Task<bool> UpdateAsync(Guid assignmentId, UpdateAssignmentRequest request, CancellationToken ct = default)
    {
        var assignment = await _repo.GetByIdAsync(assignmentId, ct);
        if (assignment is null) return false;

        try
        {
            assignment.Update(request.Title, request.Description, request.DueDate, request.MaxMarks);
        }
        catch (InvalidOperationException)
        {
            // Already published — cannot edit.
            return false;
        }

        _repo.Update(assignment);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Publishes an assignment so enrolled students can view and submit.
    /// Returns false when the assignment is not found or already published.
    /// </summary>
    public async Task<bool> PublishAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var assignment = await _repo.GetByIdAsync(assignmentId, ct);
        if (assignment is null) return false;

        try { assignment.Publish(); }
        catch (InvalidOperationException) { return false; }

        _repo.Update(assignment);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ActivateAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var assignment = await _repo.GetByIdIncludingInactiveAsync(assignmentId, ct);
        if (assignment is null || !assignment.IsDeleted) return false;

        assignment.Restore();
        _repo.Update(assignment);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Retracts a published assignment.
    /// Returns false when submissions already exist (cannot retract after students have submitted).
    /// </summary>
    public async Task<bool> RetractAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var assignment = await _repo.GetByIdAsync(assignmentId, ct);
        if (assignment is null) return false;

        // Prevent retraction when submissions exist.
        var count = await _repo.GetSubmissionCountAsync(assignmentId, ct);
        if (count > 0) return false;

        try { assignment.Retract(); }
        catch (InvalidOperationException) { return false; }

        _repo.Update(assignment);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Soft-deletes an assignment.
    /// Returns false when submissions exist — cannot delete an assignment with submissions.
    /// </summary>
    public async Task<bool> DeleteAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var assignment = await _repo.GetByIdAsync(assignmentId, ct);
        if (assignment is null) return false;

        var count = await _repo.GetSubmissionCountAsync(assignmentId, ct);
        if (count > 0) return false;

        assignment.SoftDelete();
        _repo.Update(assignment);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeactivateAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var assignment = await _repo.GetByIdAsync(assignmentId, ct);
        if (assignment is null || assignment.IsDeleted) return false;

        assignment.SoftDelete();
        _repo.Update(assignment);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Returns all assignments for a course offering, including submission counts.</summary>
    public async Task<IReadOnlyList<AssignmentResponse>> GetByOfferingAsync(Guid courseOfferingId, bool includeInactive = false, CancellationToken ct = default)
    {
        var assignments = await _repo.GetByOfferingAsync(courseOfferingId, includeInactive, ct);
        var scope = await _repo.GetOfferingScopeAsync(courseOfferingId, ct);
        var responses = new List<AssignmentResponse>(assignments.Count);
        foreach (var a in assignments)
        {
            var count = await _repo.GetSubmissionCountAsync(a.Id, ct);
            responses.Add(ToResponse(a, count, scope?.TenantId, scope?.CampusId));
        }
        return responses;
    }

    /// <summary>Returns a single assignment by ID with submission count, or null.</summary>
    public async Task<AssignmentResponse?> GetByIdAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var assignment = await _repo.GetByIdAsync(assignmentId, ct);
        if (assignment is null) return null;
        var count = await _repo.GetSubmissionCountAsync(assignmentId, ct);
        var scope = await _repo.GetOfferingScopeAsync(assignment.CourseOfferingId, ct);
        return ToResponse(assignment, count, scope?.TenantId, scope?.CampusId);
    }

    // ── Student submission ────────────────────────────────────────────────────

    /// <summary>
    /// Records a student's submission for a published assignment.
    /// Enforces: assignment must be published, current time must be before due date,
    /// and student must not have already submitted.
    /// Returns null on any rejection.
    /// </summary>
    public async Task<SubmissionResponse?> SubmitAsync(Guid studentProfileId, SubmitAssignmentRequest request, CancellationToken ct = default)
    {
        var assignment = await _repo.GetByIdAsync(request.AssignmentId, ct);

        // Guard: assignment must exist and be published.
        if (assignment is null || !assignment.IsPublished) return null;

        // Guard: reject late submissions.
        if (DateTime.UtcNow > assignment.DueDate) return null;

        // Guard: no duplicate submissions.
        if (await _repo.HasSubmittedAsync(request.AssignmentId, studentProfileId, ct)) return null;

        var submission = new AssignmentSubmission(request.AssignmentId, studentProfileId, request.FileUrl, request.TextContent);
        await _repo.AddSubmissionAsync(submission, ct);
        await _repo.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditLog("Submit", "AssignmentSubmission", submission.Id.ToString(),
            actorUserId: studentProfileId), ct);

        return ToSubmissionResponse(submission, assignment.Title);
    }

    /// <summary>Returns all submissions made by the student, including assignment title.</summary>
    public async Task<IReadOnlyList<SubmissionResponse>> GetMySubmissionsAsync(Guid studentProfileId, CancellationToken ct = default)
    {
        var submissions = await _repo.GetSubmissionsByStudentAsync(studentProfileId, ct);
        return submissions.Select(s => ToSubmissionResponse(s, s.Assignment.Title)).ToList();
    }

    // ── Faculty grading ───────────────────────────────────────────────────────

    /// <summary>Returns all submissions for an assignment in the faculty grading view.</summary>
    public async Task<IReadOnlyList<SubmissionResponse>> GetSubmissionsByAssignmentAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var assignment = await _repo.GetByIdAsync(assignmentId, ct);
        var title = assignment?.Title ?? string.Empty;
        var submissions = await _repo.GetSubmissionsByAssignmentAsync(assignmentId, ct);
        return submissions.Select(s => ToSubmissionResponse(s, title)).ToList();
    }

    /// <summary>
    /// Grades a student's submission.
    /// Validates that marks awarded do not exceed the assignment's MaxMarks.
    /// Returns false when the submission is not found or marks are out of range.
    /// </summary>
    public async Task<bool> GradeSubmissionAsync(GradeSubmissionRequest request, Guid gradedByUserId, CancellationToken ct = default)
    {
        var submission = await _repo.GetSubmissionAsync(request.AssignmentId, request.StudentProfileId, ct);
        if (submission is null) return false;

        var assignment = await _repo.GetByIdAsync(request.AssignmentId, ct);
        if (assignment is null) return false;

        // Validate marks range against the assignment's maximum.
        if (request.MarksAwarded < 0 || request.MarksAwarded > assignment.MaxMarks) return false;

        submission.Grade(request.MarksAwarded, request.Feedback, gradedByUserId);
        _repo.UpdateSubmission(submission);
        await _repo.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditLog("GradeSubmission", "AssignmentSubmission", submission.Id.ToString(),
            actorUserId: gradedByUserId), ct);

        return true;
    }

    /// <summary>Rejects a submission. Returns false when not found.</summary>
    public async Task<bool> RejectSubmissionAsync(Guid assignmentId, Guid studentProfileId, CancellationToken ct = default)
    {
        var submission = await _repo.GetSubmissionAsync(assignmentId, studentProfileId, ct);
        if (submission is null) return false;

        submission.Reject();
        _repo.UpdateSubmission(submission);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    /// <summary>Maps a domain Assignment to an AssignmentResponse DTO.</summary>
    private static AssignmentResponse ToResponse(Assignment a, int submissionCount, Guid? tenantId, Guid? campusId) =>
        new(a.Id, a.CourseOfferingId, tenantId, campusId, a.Title, a.Description,
            a.DueDate, a.MaxMarks, !a.IsDeleted, a.IsPublished, a.PublishedAt, submissionCount);

    /// <summary>Maps a domain AssignmentSubmission to a SubmissionResponse DTO.</summary>
    private static SubmissionResponse ToSubmissionResponse(AssignmentSubmission s, string assignmentTitle) =>
        new(s.Id, s.AssignmentId, assignmentTitle, s.StudentProfileId,
            s.FileUrl, s.TextContent, s.SubmittedAt, s.Status.ToString(),
            s.MarksAwarded, s.Feedback, s.GradedAt);
}
