namespace Tabsan.EduSphere.Application.DTOs.Quizzes;

// ── Authoring requests ─────────────────────────────────────────────────────

/// <summary>Request to create a new quiz for a course offering.</summary>
public record CreateQuizRequest(
    Guid CourseOfferingId,
    string Title,
    string? Instructions,
    int? TimeLimitMinutes,
    int MaxAttempts,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil);

/// <summary>Request to update the metadata of an existing quiz.</summary>
public record UpdateQuizRequest(
    string Title,
    string? Instructions,
    int? TimeLimitMinutes,
    int MaxAttempts,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil);

/// <summary>Request to add a question to a quiz.</summary>
public record AddQuestionRequest(
    Guid QuizId,
    string Text,
    string Type,
    decimal Marks,
    int OrderIndex,
    IReadOnlyList<AddOptionRequest>? Options);

/// <summary>A single answer option supplied when adding a question.</summary>
public record AddOptionRequest(
    string Text,
    bool IsCorrect,
    int OrderIndex);

/// <summary>Request to update a question's text, marks, or ordering.</summary>
public record UpdateQuestionRequest(
    string Text,
    decimal Marks,
    int OrderIndex);

// ── Attempt requests ───────────────────────────────────────────────────────

/// <summary>A student's answer for a single question within an attempt.</summary>
public record SubmitAnswerEntry(
    Guid QuizQuestionId,
    Guid? SelectedOptionId,
    string? TextResponse);

/// <summary>Request to submit a completed attempt with all answers.</summary>
public record SubmitAttemptRequest(
    Guid AttemptId,
    IReadOnlyList<SubmitAnswerEntry> Answers);

/// <summary>Faculty request to award marks for a ShortAnswer response.</summary>
public record GradeAnswerRequest(
    Guid AnswerId,
    decimal MarksAwarded);

// ── Response DTOs ──────────────────────────────────────────────────────────

/// <summary>Summary of a quiz shown in listing views.</summary>
public record QuizSummaryResponse(
    Guid QuizId,
    Guid CourseOfferingId,
    string Title,
    int? TimeLimitMinutes,
    int MaxAttempts,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    bool IsPublished,
    bool IsActive);

/// <summary>Full quiz detail with questions and options (faculty view).</summary>
public record QuizDetailResponse(
    Guid QuizId,
    Guid CourseOfferingId,
    string Title,
    string? Instructions,
    int? TimeLimitMinutes,
    int MaxAttempts,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    bool IsPublished,
    IReadOnlyList<QuestionResponse> Questions);

/// <summary>A question with its options as returned to students and faculty.</summary>
public record QuestionResponse(
    Guid QuestionId,
    string Text,
    string Type,
    decimal Marks,
    int OrderIndex,
    IReadOnlyList<OptionResponse> Options);

/// <summary>An answer option as returned to the student or faculty.</summary>
public record OptionResponse(
    Guid OptionId,
    string Text,
    int OrderIndex,
    bool? IsCorrect);   // null when returned to students during a live attempt

/// <summary>Attempt status returned after starting or submitting an attempt.</summary>
public record AttemptResponse(
    Guid AttemptId,
    Guid QuizId,
    Guid StudentProfileId,
    string Status,
    DateTime StartedAt,
    DateTime? FinishedAt,
    decimal? TotalScore);

/// <summary>Full attempt detail with answer breakdown (faculty/student review).</summary>
public record AttemptDetailResponse(
    Guid AttemptId,
    Guid QuizId,
    Guid StudentProfileId,
    string Status,
    DateTime StartedAt,
    DateTime? FinishedAt,
    decimal? TotalScore,
    IReadOnlyList<AnswerResponse> Answers);

/// <summary>A single answer as included in an attempt review.</summary>
public record AnswerResponse(
    Guid AnswerId,
    Guid QuizQuestionId,
    Guid? SelectedOptionId,
    string? TextResponse,
    decimal? MarksAwarded);
