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

/// <summary>Plan M2: presentation, security and review settings.</summary>
public record QuizDeliverySettingsRequest(
    bool ShuffleQuestions,
    bool ShuffleOptions,
    bool OneQuestionAtATime,
    bool LockAnsweredQuestions,
    string? AccessCode,
    string? AllowedIpRanges,
    string ReviewVisibility,
    bool ReviewAfterCloseOnly);

/// <summary>Request to add a question to a quiz.</summary>
public record AddQuestionRequest(
    Guid QuizId,
    string Text,
    string Type,
    decimal Marks,
    int OrderIndex,
    IReadOnlyList<AddOptionRequest>? Options,
    string? ConfigJson = null);

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

/// <summary>Plan M2: full question edit (text, marks, order, settings, options).</summary>
public record SaveQuestionRequest(
    string Text,
    decimal Marks,
    int OrderIndex,
    IReadOnlyList<AddOptionRequest>? Options,
    string? ConfigJson,
    string? Tags = null,
    int? Difficulty = null);

/// <summary>Outcome of saving a question.</summary>
public record QuestionSaveResult(Guid? QuestionId, string? Error)
{
    public static QuestionSaveResult Ok(Guid id) => new(id, null);
    public static QuestionSaveResult Fail(string error) => new(null, error);
}

/// <summary>Plan M2.2: add or update a "pick N from bank" group.</summary>
public record SaveQuestionGroupRequest(
    Guid QuestionBankId,
    string Title,
    int PickCount,
    decimal MarksPerQuestion,
    int OrderIndex,
    string? TagFilter);

/// <summary>Plan M2.2: copy bank questions into the quiz.</summary>
public record CopyBankQuestionsRequest(IReadOnlyList<Guid> QuestionIds);

// ── Attempt requests ───────────────────────────────────────────────────────

/// <summary>A student's answer for a single question within an attempt.</summary>
public record SubmitAnswerEntry(
    Guid QuizQuestionId,
    Guid? SelectedOptionId,
    string? TextResponse,
    IReadOnlyList<Guid>? SelectedOptionIds = null,
    string? ResponseJson = null);

/// <summary>Request to submit a completed attempt with all answers.</summary>
public record SubmitAttemptRequest(
    Guid AttemptId,
    IReadOnlyList<SubmitAnswerEntry> Answers);

/// <summary>Plan M2: save answers without submitting (autosave / one-question-at-a-time).</summary>
public record SaveAnswersRequest(IReadOnlyList<SubmitAnswerEntry> Answers);

/// <summary>Plan M2.6: start an attempt (access code when the quiz requires one).</summary>
public record StartAttemptRequest(string? AccessCode);

/// <summary>Faculty request to award marks for a manually graded response.</summary>
public record GradeAnswerRequest(
    Guid AnswerId,
    decimal MarksAwarded,
    string? Feedback = null);

/// <summary>Plan M2.6: regrade one question across every finished attempt.</summary>
public record RegradeQuestionRequest(string Mode);

/// <summary>Plan M2.5: accommodation for one student.</summary>
public record SaveAccommodationRequest(
    Guid CourseOfferingId,
    Guid? QuizId,
    Guid StudentProfileId,
    int ExtraTimeMinutes,
    decimal TimeMultiplier,
    int ExtraAttempts,
    string? Note);

/// <summary>Plan M2.5: availability override for one student on one quiz.</summary>
public record SaveAvailabilityOverrideRequest(
    Guid StudentProfileId,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    string? Note);

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
    bool IsActive,
    int QuestionCount = 0);

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
    IReadOnlyList<QuestionResponse> Questions,
    QuizDeliverySettingsResponse? Settings = null,
    IReadOnlyList<QuestionGroupResponse>? Groups = null,
    decimal TotalMarks = 0);

/// <summary>Plan M2: delivery settings as stored.</summary>
public record QuizDeliverySettingsResponse(
    bool ShuffleQuestions,
    bool ShuffleOptions,
    bool OneQuestionAtATime,
    bool LockAnsweredQuestions,
    string? AccessCode,
    string? AllowedIpRanges,
    string ReviewVisibility,
    bool ReviewAfterCloseOnly);

/// <summary>Plan M2.2: a bank draw group.</summary>
public record QuestionGroupResponse(
    Guid GroupId,
    Guid QuestionBankId,
    string BankTitle,
    string Title,
    int PickCount,
    decimal MarksPerQuestion,
    int OrderIndex,
    string? TagFilter,
    int AvailableQuestions);

/// <summary>A question with its options as returned to faculty.</summary>
public record QuestionResponse(
    Guid QuestionId,
    string Text,
    string Type,
    decimal Marks,
    int OrderIndex,
    IReadOnlyList<OptionResponse> Options,
    string? ConfigJson = null,
    string? Tags = null,
    int? Difficulty = null,
    bool IsArchived = false);

/// <summary>An answer option as returned to faculty (students never receive IsCorrect).</summary>
public record OptionResponse(
    Guid OptionId,
    string Text,
    int OrderIndex,
    bool? IsCorrect);

/// <summary>Plan M2: quiz information a student may see before starting (no questions).</summary>
public record StudentQuizInfoResponse(
    Guid QuizId,
    string Title,
    string? Instructions,
    int? TimeLimitMinutes,
    int MaxAttempts,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    bool RequiresAccessCode,
    int QuestionCount);

/// <summary>Attempt status returned after starting or submitting an attempt.</summary>
public record AttemptResponse(
    Guid AttemptId,
    Guid QuizId,
    Guid StudentProfileId,
    string Status,
    DateTime StartedAt,
    DateTime? FinishedAt,
    decimal? TotalScore,
    DateTime? DeadlineAt = null);

/// <summary>Outcome of a start request.</summary>
public record StartAttemptResult(AttemptResponse? Attempt, string? ErrorCode, string? Message)
{
    public static StartAttemptResult Ok(AttemptResponse a) => new(a, null, null);
    public static StartAttemptResult Fail(string code, string message) => new(null, code, message);
}

/// <summary>Full attempt detail with answer breakdown (faculty view / student review).</summary>
public record AttemptDetailResponse(
    Guid AttemptId,
    Guid QuizId,
    Guid StudentProfileId,
    string Status,
    DateTime StartedAt,
    DateTime? FinishedAt,
    decimal? TotalScore,
    IReadOnlyList<AnswerResponse> Answers,
    decimal MaxScore = 0,
    IReadOnlyList<ReviewItemResponse>? Items = null,
    string? ReviewVisibility = null);

/// <summary>A single answer as included in an attempt review.</summary>
public record AnswerResponse(
    Guid AnswerId,
    Guid QuizQuestionId,
    Guid? SelectedOptionId,
    string? TextResponse,
    decimal? MarksAwarded,
    IReadOnlyList<Guid>? SelectedOptionIds = null,
    string? ResponseJson = null,
    string? Feedback = null);

/// <summary>Plan M2: one question of an attempt with the student's answer (and, when allowed, the correct answer).</summary>
public record ReviewItemResponse(
    int Order,
    StudentQuestionView Question,
    decimal Marks,
    decimal? MarksAwarded,
    string? Feedback,
    string? CorrectAnswer,
    bool PendingManualGrading);

// ── Plan M2: the student's paper ───────────────────────────────────────────

/// <summary>Everything the take-quiz page needs. Contains no correctness information.</summary>
public record StudentAttemptPaper(
    Guid AttemptId,
    Guid QuizId,
    string QuizTitle,
    string? Instructions,
    string Status,
    DateTime StartedAt,
    DateTime? DeadlineAt,
    DateTime ServerNow,
    bool OneQuestionAtATime,
    bool LockAnsweredQuestions,
    decimal TotalMarks,
    IReadOnlyList<StudentQuestionView> Questions);

/// <summary>One question as shown to a student.</summary>
public record StudentQuestionView(
    int Order,
    Guid QuestionId,
    string Type,
    string Text,
    decimal Marks,
    IReadOnlyList<StudentChoice>? Options = null,
    IReadOnlyList<StudentChoice>? MatchingPrompts = null,
    IReadOnlyList<string>? MatchingChoices = null,
    IReadOnlyList<StudentChoice>? OrderingItems = null,
    IReadOnlyList<string>? BlankIds = null,
    IReadOnlyList<string>? AllowedExtensions = null,
    int? MaxFileSizeMb = null,
    int? WordLimit = null,
    SavedAnswerView? Saved = null,
    bool Locked = false);

public record StudentChoice(string Id, string Text);

public record SavedAnswerView(IReadOnlyList<Guid> SelectedOptionIds, string? TextResponse, string? ResponseJson, string? FileName);

// ── Plan M2.5 / M2.6 / M2.7 ────────────────────────────────────────────────

public record AccommodationResponse(
    Guid Id, Guid CourseOfferingId, Guid? QuizId, Guid StudentProfileId, string? StudentName,
    int ExtraTimeMinutes, decimal TimeMultiplier, int ExtraAttempts, string? Note);

public record AvailabilityOverrideResponse(
    Guid Id, Guid QuizId, Guid StudentProfileId, string? StudentName,
    DateTime? AvailableFrom, DateTime? AvailableUntil, string? Note);

/// <summary>An answer waiting for manual grading.</summary>
public record GradingQueueItem(
    Guid AnswerId,
    Guid AttemptId,
    Guid StudentProfileId,
    string? StudentName,
    Guid QuestionId,
    string QuestionType,
    string QuestionText,
    decimal Marks,
    string? TextResponse,
    string? FileName,
    string? FileStorageKey,
    DateTime? SubmittedAt);

public record RegradeResult(int AttemptsUpdated, string? Error);

/// <summary>Item analysis for one quiz.</summary>
public record QuizStatisticsResponse(
    Guid QuizId,
    int CompletedAttempts,
    decimal? MeanScore,
    decimal? MedianScore,
    decimal? HighestScore,
    decimal? LowestScore,
    decimal? StandardDeviation,
    decimal MaxScore,
    IReadOnlyList<QuestionStatistics> Questions);

/// <summary>
/// Difficulty index = mean fraction of marks earned (1 = everyone right).
/// Discrimination = difficulty in the top 27% of attempts minus the bottom 27% (higher is better; negative is a warning sign).
/// </summary>
public record QuestionStatistics(
    Guid QuestionId,
    int Order,
    string Text,
    string Type,
    int Responses,
    decimal? DifficultyIndex,
    decimal? Discrimination,
    decimal? MeanMarks,
    decimal Marks,
    IReadOnlyList<OptionStatistics>? Options);

public record OptionStatistics(Guid OptionId, string Text, bool IsCorrect, int Count, decimal Share);
