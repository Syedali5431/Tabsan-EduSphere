using Tabsan.EduSphere.Application.DTOs.Quizzes;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Application service contract for quiz authoring, publishing, attempt management, and grading.
/// </summary>
public interface IQuizService
{
    // ── Authoring ─────────────────────────────────────────────────────────────

    /// <summary>Creates a new unpublished quiz and returns its ID.</summary>
    Task<Guid> CreateAsync(CreateQuizRequest request, Guid createdByUserId, CancellationToken ct = default);

    /// <summary>Updates the metadata of an unpublished quiz. Returns false if not found.</summary>
    Task<bool> UpdateAsync(Guid quizId, UpdateQuizRequest request, CancellationToken ct = default);

    /// <summary>Publishes a quiz so students can start attempts. Returns false if not found.</summary>
    Task<bool> PublishAsync(Guid quizId, CancellationToken ct = default);

    /// <summary>Unpublishes a quiz, hiding it from students. Returns false if not found.</summary>
    Task<bool> UnpublishAsync(Guid quizId, CancellationToken ct = default);

    /// <summary>Soft-deletes a quiz. Returns false if not found.</summary>
    Task<bool> DeactivateAsync(Guid quizId, CancellationToken ct = default);

    /// <summary>Reactivates a previously deactivated quiz. Returns false if not found.</summary>
    Task<bool> ActivateAsync(Guid quizId, CancellationToken ct = default);

    /// <summary>Adds a question (with options) to a quiz. Returns the new question ID.</summary>
    Task<Guid> AddQuestionAsync(AddQuestionRequest request, CancellationToken ct = default);

    /// <summary>Updates the text, marks, and ordering of an existing question. Returns false if not found.</summary>
    Task<bool> UpdateQuestionAsync(Guid questionId, UpdateQuestionRequest request, CancellationToken ct = default);

    /// <summary>Removes a question and its options. Returns false if not found.</summary>
    Task<bool> RemoveQuestionAsync(Guid questionId, CancellationToken ct = default);

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Returns quizzes for a course offering, optionally including inactive rows.</summary>
    Task<IReadOnlyList<QuizSummaryResponse>> GetByOfferingAsync(Guid courseOfferingId, bool includeInactive = false, CancellationToken ct = default);

    /// <summary>Returns full quiz detail with questions and options. Returns null if not found.</summary>
    Task<QuizDetailResponse?> GetDetailAsync(Guid quizId, CancellationToken ct = default);

    // ── Attempts ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Opens a new attempt for a student.
    /// Returns null when the quiz is not found, not published, outside its availability window,
    /// or the student has reached the attempt cap.
    /// </summary>
    Task<AttemptResponse?> StartAttemptAsync(Guid quizId, Guid studentProfileId, CancellationToken ct = default);

    /// <summary>
    /// Submits an in-progress attempt with all answers, auto-grades MCQ/TrueFalse responses, and returns the result.
    /// Returns null if the attempt is not found or is already finalised.
    /// </summary>
    Task<AttemptDetailResponse?> SubmitAttemptAsync(SubmitAttemptRequest request, CancellationToken ct = default);

    /// <summary>Returns all attempts a student has made on a quiz.</summary>
    Task<IReadOnlyList<AttemptResponse>> GetStudentAttemptsAsync(Guid quizId, Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns all attempts across all quizzes for a student (portal summary).</summary>
    Task<IReadOnlyList<AttemptResponse>> GetAllMyAttemptsAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns full attempt detail including answers. Returns null if not found.</summary>
    Task<AttemptDetailResponse?> GetAttemptDetailAsync(Guid attemptId, CancellationToken ct = default);

    /// <summary>
    /// Awards marks for a ShortAnswer response (faculty manual grading).
    /// Returns false if the answer is not found.
    /// </summary>
    Task<bool> GradeAnswerAsync(GradeAnswerRequest request, CancellationToken ct = default);
}
