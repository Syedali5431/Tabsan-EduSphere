using Tabsan.EduSphere.Domain.Quizzes;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>
/// Repository contract for quiz authoring and attempt management.
/// </summary>
public interface IQuizRepository
{
    // ── Quiz ──────────────────────────────────────────────────────────────────

    /// <summary>Returns a quiz by its ID (active only), or null if not found.</summary>
    Task<Quiz?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns a quiz by ID including inactive rows, or null if not found.</summary>
    Task<Quiz?> GetByIdIncludingInactiveAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns a quiz including its questions and options, or null.</summary>
    Task<Quiz?> GetWithQuestionsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns quizzes for a course offering, optionally including inactive rows.</summary>
    Task<IReadOnlyList<Quiz>> GetByOfferingAsync(Guid courseOfferingId, bool includeInactive = false, CancellationToken ct = default);

    /// <summary>Queues a new quiz for insertion.</summary>
    Task AddAsync(Quiz quiz, CancellationToken ct = default);

    /// <summary>Marks a quiz entity as modified.</summary>
    void Update(Quiz quiz);

    // ── Questions and Options ─────────────────────────────────────────────────

    /// <summary>Returns a question by its ID, or null if not found.</summary>
    Task<QuizQuestion?> GetQuestionByIdAsync(Guid questionId, CancellationToken ct = default);

    /// <summary>Queues a question for insertion.</summary>
    Task AddQuestionAsync(QuizQuestion question, CancellationToken ct = default);

    /// <summary>Marks a question entity as modified.</summary>
    void UpdateQuestion(QuizQuestion question);

    /// <summary>Removes a question and its options from the context.</summary>
    void RemoveQuestion(QuizQuestion question);

    /// <summary>Queues multiple options for bulk insertion.</summary>
    Task AddOptionsAsync(IEnumerable<QuizOption> options, CancellationToken ct = default);

    /// <summary>Removes a collection of options from the context.</summary>
    void RemoveOptions(IEnumerable<QuizOption> options);

    // ── Attempts ──────────────────────────────────────────────────────────────

    /// <summary>Returns an attempt by its ID including its answers, or null.</summary>
    Task<QuizAttempt?> GetAttemptByIdAsync(Guid attemptId, CancellationToken ct = default);

    /// <summary>Returns all attempts a student has made on a specific quiz.</summary>
    Task<IReadOnlyList<QuizAttempt>> GetAttemptsAsync(Guid quizId, Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns all attempts across all quizzes for a student (for portal summary view).</summary>
    Task<IReadOnlyList<QuizAttempt>> GetAllAttemptsForStudentAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns the count of attempts the student has made on the quiz.</summary>
    Task<int> GetAttemptCountAsync(Guid quizId, Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Returns the in-progress attempt for a student on a quiz, or null.</summary>
    Task<QuizAttempt?> GetInProgressAttemptAsync(Guid quizId, Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Queues a new attempt for insertion.</summary>
    Task AddAttemptAsync(QuizAttempt attempt, CancellationToken ct = default);

    /// <summary>Marks an attempt entity as modified.</summary>
    void UpdateAttempt(QuizAttempt attempt);

    /// <summary>Queues multiple answers for bulk insertion.</summary>
    Task AddAnswersAsync(IEnumerable<QuizAnswer> answers, CancellationToken ct = default);

    /// <summary>Returns a single answer by its ID, or null.</summary>
    Task<QuizAnswer?> GetAnswerByIdAsync(Guid answerId, CancellationToken ct = default);

    /// <summary>Marks an answer entity as modified (for manual short-answer grading).</summary>
    void UpdateAnswer(QuizAnswer answer);

    /// <summary>Commits all pending changes.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
