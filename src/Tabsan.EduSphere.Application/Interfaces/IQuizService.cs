using Tabsan.EduSphere.Application.DTOs.Quizzes;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Quiz authoring, delivery, grading and analysis (Plan M2: question types, banks, randomisation,
/// accommodations, security, review options, manual grading, regrade, item analysis).
/// </summary>
public interface IQuizService
{
    // ── Authoring ─────────────────────────────────────────────────────────────
    Task<Guid> CreateAsync(CreateQuizRequest request, Guid createdByUserId, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guid quizId, UpdateQuizRequest request, CancellationToken ct = default);
    Task<bool> PublishAsync(Guid quizId, CancellationToken ct = default);
    Task<bool> UnpublishAsync(Guid quizId, CancellationToken ct = default);
    Task<bool> DeactivateAsync(Guid quizId, CancellationToken ct = default);
    Task<bool> ActivateAsync(Guid quizId, CancellationToken ct = default);
    Task<string?> ConfigureDeliveryAsync(Guid quizId, QuizDeliverySettingsRequest request, CancellationToken ct = default);

    Task<QuestionSaveResult> AddQuestionAsync(AddQuestionRequest request, CancellationToken ct = default);
    Task<bool> UpdateQuestionAsync(Guid questionId, UpdateQuestionRequest request, CancellationToken ct = default);
    Task<QuestionSaveResult> SaveQuestionAsync(Guid questionId, SaveQuestionRequest request, CancellationToken ct = default);
    /// <summary>Null on success; an error message when the question cannot be removed.</summary>
    Task<(bool Found, string? Error)> RemoveQuestionAsync(Guid questionId, CancellationToken ct = default);

    Task<QuestionSaveResult> AddGroupAsync(Guid quizId, SaveQuestionGroupRequest request, CancellationToken ct = default);
    Task<QuestionSaveResult> UpdateGroupAsync(Guid groupId, SaveQuestionGroupRequest request, CancellationToken ct = default);
    Task<bool> RemoveGroupAsync(Guid groupId, CancellationToken ct = default);
    Task<(int Copied, string? Error)> CopyFromBankAsync(Guid quizId, CopyBankQuestionsRequest request, CancellationToken ct = default);

    // ── Queries ───────────────────────────────────────────────────────────────
    Task<IReadOnlyList<QuizSummaryResponse>> GetByOfferingAsync(Guid courseOfferingId, bool includeInactive = false, CancellationToken ct = default);
    /// <summary>Faculty view: questions, options with correctness, settings, groups.</summary>
    Task<QuizDetailResponse?> GetDetailAsync(Guid quizId, CancellationToken ct = default);
    /// <summary>Student view before starting: no questions, no answers.</summary>
    Task<StudentQuizInfoResponse?> GetStudentInfoAsync(Guid quizId, CancellationToken ct = default);
    Task<Guid?> GetCourseOfferingIdAsync(Guid quizId, CancellationToken ct = default);

    // ── Attempts (student) ────────────────────────────────────────────────────
    Task<StartAttemptResult> StartAttemptAsync(Guid quizId, Guid studentProfileId, string? accessCode, string? clientIp, CancellationToken ct = default);
    Task<(StudentAttemptPaper? Paper, string? Error)> GetPaperAsync(Guid attemptId, Guid studentProfileId, CancellationToken ct = default);
    Task<(int Saved, int LockedSkipped, string? Error)> SaveAnswersAsync(Guid attemptId, Guid studentProfileId, SaveAnswersRequest request, CancellationToken ct = default);
    Task<(int Saved, string? Error)> SaveFileAnswerAsync(Guid attemptId, Guid studentProfileId, Guid questionId, string fileName, string? contentType, long length, Stream content, CancellationToken ct = default);
    Task<(AttemptDetailResponse? Review, string? Error)> SubmitAttemptAsync(SubmitAttemptRequest request, Guid studentProfileId, CancellationToken ct = default);
    Task<(AttemptDetailResponse? Review, string? Error)> GetStudentReviewAsync(Guid attemptId, Guid studentProfileId, CancellationToken ct = default);
    Task<IReadOnlyList<AttemptResponse>> GetStudentAttemptsAsync(Guid quizId, Guid studentProfileId, CancellationToken ct = default);
    Task<IReadOnlyList<AttemptResponse>> GetAllMyAttemptsAsync(Guid studentProfileId, CancellationToken ct = default);

    // ── Grading & analysis (faculty) ──────────────────────────────────────────
    Task<AttemptDetailResponse?> GetAttemptDetailAsync(Guid attemptId, CancellationToken ct = default);
    Task<Guid?> GetAttemptOwnerAsync(Guid attemptId, CancellationToken ct = default);
    Task<IReadOnlyList<GradingQueueItem>> GetGradingQueueAsync(Guid quizId, CancellationToken ct = default);
    Task<(bool Found, string? Error)> GradeAnswerAsync(GradeAnswerRequest request, Guid gradedByUserId, CancellationToken ct = default);
    Task<RegradeResult> RegradeQuestionAsync(Guid quizId, Guid questionId, RegradeQuestionRequest request, CancellationToken ct = default);
    Task<QuizStatisticsResponse?> GetStatisticsAsync(Guid quizId, CancellationToken ct = default);
    Task<(byte[] Content, string FileName, string ContentType)?> GetAnswerFileAsync(Guid answerId, Guid? studentProfileId, CancellationToken ct = default);

    // ── Accommodations & overrides ────────────────────────────────────────────
    Task<IReadOnlyList<AccommodationResponse>> GetAccommodationsAsync(Guid courseOfferingId, CancellationToken ct = default);
    Task<(Guid? Id, string? Error)> SaveAccommodationAsync(SaveAccommodationRequest request, CancellationToken ct = default);
    Task<bool> RemoveAccommodationAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AvailabilityOverrideResponse>> GetOverridesAsync(Guid quizId, CancellationToken ct = default);
    Task<(Guid? Id, string? Error)> SaveOverrideAsync(Guid quizId, SaveAvailabilityOverrideRequest request, CancellationToken ct = default);
    Task<bool> RemoveOverrideAsync(Guid id, CancellationToken ct = default);
}
