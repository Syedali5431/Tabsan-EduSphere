namespace Tabsan.EduSphere.Application.DTOs.Assignments;

// Plan M3 — submission history, comments, annotations and the grading workspace.

/// <summary>Submission settings and grading policies for one assignment.</summary>
public sealed record AssignmentSettingsRequest(
    int AllowedAttempts,
    IReadOnlyList<string>? AllowedFileTypes,
    int MaxFileSizeMb,
    bool AnonymousGrading,
    string PostingPolicy,
    bool ModeratedGrading);

public sealed record AssignmentSettingsResponse(
    Guid AssignmentId,
    int AllowedAttempts,
    IReadOnlyList<string> AllowedFileTypes,
    bool FileTypesRestricted,
    int MaxFileSizeMb,
    bool AnonymousGrading,
    bool NamesHidden,
    string PostingPolicy,
    bool ModeratedGrading,
    Guid? ModeratorUserId,
    string? ModeratorName);

/// <summary>A file arriving with a submission or comment (the stream belongs to the caller).</summary>
public sealed record IncomingFile(string FileName, string? ContentType, long Length, Stream Content);

public sealed record SubmissionFileResponse(
    Guid FileId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string ViewerKind,
    string ConversionStatus,
    string? ConversionError,
    bool IsLegacy,
    string? LegacyUrl,
    int AnnotationCount);

public sealed record SubmissionAttemptResponse(
    int AttemptNumber,
    DateTime SubmittedAt,
    bool IsLate,
    string? TextContent,
    IReadOnlyList<SubmissionFileResponse> Files);

public sealed record SubmissionCommentResponse(
    Guid CommentId,
    int? AttemptNumber,
    string AuthorName,
    bool AuthorIsStaff,
    bool IsMine,
    string? Body,
    bool HasMedia,
    string? MediaContentType,
    string? MediaFileName,
    bool IsPrivateToGraders,
    DateTime CreatedAt);

public sealed record ProvisionalGradeResponse(
    Guid ProvisionalGradeId,
    Guid GraderUserId,
    string GraderName,
    decimal Marks,
    string? Feedback,
    bool IsSelected,
    DateTime UpdatedAt);

public sealed record RubricLevelChoice(Guid LevelId, string Label, decimal Points);

public sealed record RubricCriterionView(
    Guid CriterionId,
    string Name,
    decimal MaxPoints,
    IReadOnlyList<RubricLevelChoice> Levels,
    Guid? SelectedLevelId,
    decimal? Points);

public sealed record RubricGradeView(
    Guid RubricId,
    string Title,
    decimal TotalPoints,
    decimal MaxPoints,
    IReadOnlyList<RubricCriterionView> Criteria);

/// <summary>One submission with everything the viewer may see (what is included depends on who asks).</summary>
public sealed record SubmissionDetailResponse(
    Guid SubmissionId,
    Guid AssignmentId,
    string AssignmentTitle,
    Guid CourseOfferingId,
    decimal MaxMarks,
    DateTime DueDate,
    Guid? StudentProfileId,
    string StudentLabel,
    string? RegistrationNumber,
    string Status,
    int CurrentAttemptNumber,
    bool IsLate,
    DateTime SubmittedAt,
    decimal? MarksAwarded,
    string? Feedback,
    DateTime? GradedAt,
    bool IsPosted,
    DateTime? PostedAt,
    bool GradeHiddenFromStudent,
    IReadOnlyList<SubmissionAttemptResponse> Attempts,
    IReadOnlyList<SubmissionCommentResponse> Comments,
    IReadOnlyList<ProvisionalGradeResponse>? ProvisionalGrades,
    RubricGradeView? Rubric,
    AssignmentSettingsResponse Settings,
    bool CanResubmit,
    int? AttemptsRemaining,
    bool CanModerate,
    Guid? PreviousSubmissionId = null,
    Guid? NextSubmissionId = null,
    // Plan M8: the grade as entered, the late deduction, the automatic missing grade and group work.
    decimal? RawMarks = null,
    decimal LatePenalty = 0,
    bool LatePenaltyWaived = false,
    bool IsMissing = false,
    Guid? GroupId = null,
    string? GroupName = null,
    decimal? PeerReviewAverage = null,
    int PeerReviewsCompleted = 0);

/// <summary>What a student sees before submitting for the first time.</summary>
public sealed record StudentAssignmentView(
    Guid AssignmentId,
    string Title,
    string? Description,
    DateTime DueDate,
    decimal MaxMarks,
    bool IsOpen,
    AssignmentSettingsResponse Settings,
    SubmissionDetailResponse? Submission);

public sealed record WorkspaceSubmissionItem(
    Guid SubmissionId,
    string StudentLabel,
    string? RegistrationNumber,
    Guid? StudentProfileId,
    string Status,
    bool IsLate,
    int AttemptNumber,
    DateTime SubmittedAt,
    decimal? MarksAwarded,
    bool IsPosted,
    bool NeedsGrading,
    int ProvisionalGradeCount,
    bool IsMissing = false,
    decimal LatePenalty = 0,
    Guid? GroupId = null);

public sealed record GradingWorkspaceResponse(
    Guid AssignmentId,
    string Title,
    Guid CourseOfferingId,
    decimal MaxMarks,
    DateTime DueDate,
    AssignmentSettingsResponse Settings,
    IReadOnlyList<WorkspaceSubmissionItem> Submissions,
    int GradedCount,
    int PostedCount,
    int GradedNotPostedCount,
    bool HasRubric,
    bool ConversionEnabled,
    bool CanModerate);

/// <summary>Autosave from the workspace. Null marks clears nothing — it just saves the feedback.</summary>
public sealed record SaveGradeRequest(decimal? MarksAwarded, string? Feedback);

public sealed record SaveGradeResponse(
    string Outcome,
    decimal? MarksAwarded,
    bool IsPosted,
    bool Provisional,
    // Plan M8: the late deduction taken from the entered mark, and how many group mates got the same grade.
    decimal LatePenalty = 0,
    int GroupMembersGraded = 0);

public sealed record RubricScoreRequest(IReadOnlyList<RubricCriterionGradeDto> Grades, bool ApplyTotalToMarks);

public sealed record SaveAnnotationRequest(int PageNumber, string Type, string GeometryJson, string? Comment, string? Color);

public sealed record AnnotationResponse(
    Guid AnnotationId,
    Guid FileId,
    int PageNumber,
    string Type,
    string GeometryJson,
    string? Comment,
    string Color,
    string AuthorName,
    bool IsMine,
    DateTime CreatedAt);

public sealed record PostGradesRequest(IReadOnlyList<Guid>? SubmissionIds);

public sealed record PostGradesResponse(int Posted, int Hidden, bool NamesRevealed);

public sealed record ModerateRequest(Guid? ProvisionalGradeId, decimal? MarksAwarded, string? Feedback);

public sealed record FileDownload(byte[] Content, string ContentType, string FileName);
