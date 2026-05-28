namespace Tabsan.EduSphere.Application.DTOs.Assignments;

// ── Assignment DTOs ───────────────────────────────────────────────────────────

/// <summary>Request body for creating a new assignment (starts unpublished).</summary>
public sealed record CreateAssignmentRequest(
    Guid CourseOfferingId,
    string Title,
    string? Description,
    DateTime DueDate,
    decimal MaxMarks);

/// <summary>Request body for updating a draft (unpublished) assignment.</summary>
public sealed record UpdateAssignmentRequest(
    string Title,
    string? Description,
    DateTime DueDate,
    decimal MaxMarks);

/// <summary>Read-model returned to faculty and students.</summary>
public sealed record AssignmentResponse(
    Guid Id,
    Guid CourseOfferingId,
    Guid? TenantId,
    Guid? CampusId,
    string Title,
    string? Description,
    DateTime DueDate,
    decimal MaxMarks,
    bool IsActive,
    bool IsPublished,
    DateTime? PublishedAt,
    int SubmissionCount);

// ── Submission DTOs ───────────────────────────────────────────────────────────

/// <summary>Request body for a student submitting to an assignment.</summary>
public sealed record SubmitAssignmentRequest(
    Guid AssignmentId,
    string? FileUrl,
    string? TextContent);

/// <summary>Request body for faculty grading a submission.</summary>
public sealed record GradeSubmissionRequest(
    Guid AssignmentId,
    Guid StudentProfileId,
    decimal MarksAwarded,
    string? Feedback);

/// <summary>Read-model returned for a submission.</summary>
public sealed record SubmissionResponse(
    Guid SubmissionId,
    Guid AssignmentId,
    string AssignmentTitle,
    Guid StudentProfileId,
    string? FileUrl,
    string? TextContent,
    DateTime SubmittedAt,
    string Status,
    decimal? MarksAwarded,
    string? Feedback,
    DateTime? GradedAt);

// ── Result DTOs ───────────────────────────────────────────────────────────────

/// <summary>Request body for entering a single result (draft).</summary>
public sealed record CreateResultRequest(
    Guid StudentProfileId,
    Guid CourseOfferingId,
    string ResultType,
    decimal MarksObtained,
    decimal MaxMarks);

/// <summary>Request body for bulk-entering results for an entire class.</summary>
public sealed record BulkCreateResultsRequest(IReadOnlyList<CreateResultRequest> Results);

/// <summary>Request body for Admin correction of a published result.</summary>
public sealed record CorrectResultRequest(
    decimal NewMarksObtained,
    decimal NewMaxMarks,
    string? Reason = null);

/// <summary>Read-model returned for a result entry.</summary>
public sealed record ResultResponse(
    Guid ResultId,
    Guid StudentProfileId,
    Guid CourseOfferingId,
    string ResultType,
    decimal MarksObtained,
    decimal MaxMarks,
    decimal Percentage,
    decimal? GradePoint,
    bool IsPublished,
    DateTime? PublishedAt);

// ── Transcript DTOs ───────────────────────────────────────────────────────────

/// <summary>Request body for requesting a transcript export.</summary>
public sealed record TranscriptExportRequest(
    Guid StudentProfileId,
    string Format = "PDF");   // "PDF" | "CSV"

/// <summary>Summary of a past transcript export.</summary>
public sealed record TranscriptExportLogResponse(
    Guid LogId,
    DateTime ExportedAt,
    string Format,
    string? DocumentUrl);
