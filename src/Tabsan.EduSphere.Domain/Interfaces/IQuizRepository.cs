using Tabsan.EduSphere.Domain.Quizzes;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>
/// Repository contract for quiz authoring and attempt management.
/// </summary>
public interface IQuizRepository
{
    // ── Plan M2 ───────────────────────────────────────────────────────────────

    /// <summary>Quiz with questions, options, groups (and their banks).</summary>
    Task<Quiz?> GetForAuthoringAsync(Guid quizId, CancellationToken ct = default);

    /// <summary>True when any answer references the question (it then cannot be deleted / its options re-created).</summary>
    Task<bool> QuestionHasAnswersAsync(Guid questionId, CancellationToken ct = default);

    Task<QuestionBank?> GetBankAsync(Guid bankId, bool includeQuestions, CancellationToken ct = default);
    Task<IReadOnlyList<QuestionBank>> GetBanksAsync(Guid? tenantId, Guid? courseOfferingId, Guid? departmentId, Guid? userId, CancellationToken ct = default);
    Task AddBankAsync(QuestionBank bank, CancellationToken ct = default);
    /// <summary>True when any quiz draws from the bank.</summary>
    Task<bool> IsBankUsedAsync(Guid bankId, CancellationToken ct = default);
    Task<int> CountDrawableAsync(Guid bankId, string? tagFilter, CancellationToken ct = default);
    Task<IReadOnlyList<QuizQuestion>> GetDrawableQuestionsAsync(Guid bankId, string? tagFilter, CancellationToken ct = default);
    Task<IReadOnlyList<QuizQuestion>> GetQuestionsWithOptionsAsync(IReadOnlyCollection<Guid> questionIds, CancellationToken ct = default);

    Task<QuizQuestionGroup?> GetGroupAsync(Guid groupId, CancellationToken ct = default);
    Task AddGroupAsync(QuizQuestionGroup group, CancellationToken ct = default);
    void RemoveGroup(QuizQuestionGroup group);

    /// <summary>Attempt with quiz, items (+question+options) and answers (+selections).</summary>
    Task<QuizAttempt?> GetAttemptForDeliveryAsync(Guid attemptId, CancellationToken ct = default);
    Task AddAttemptItemsAsync(IEnumerable<QuizAttemptItem> items, CancellationToken ct = default);

    /// <summary>Answer with its question (+options) and attempt (+items, answers).</summary>
    Task<QuizAnswer?> GetAnswerForGradingAsync(Guid answerId, CancellationToken ct = default);

    /// <summary>Finished attempts on a quiz, with items and answers (+selections).</summary>
    Task<IReadOnlyList<QuizAttempt>> GetFinishedAttemptsAsync(Guid quizId, CancellationToken ct = default);

    /// <summary>
    /// Plan M2 gradebook sync: the student's finished attempts on every active quiz of the offering,
    /// with items and the quiz's questions (to size legacy attempts that have no item snapshot).
    /// </summary>
    Task<IReadOnlyList<QuizAttempt>> GetFinishedAttemptsForGradebookAsync(Guid courseOfferingId, Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Answers on finished attempts whose question type is graded manually and that have no marks yet.</summary>
    Task<IReadOnlyList<QuizAnswer>> GetManualGradingQueueAsync(Guid quizId, CancellationToken ct = default);

    Task<IReadOnlyList<QuizAccommodation>> GetAccommodationsAsync(Guid courseOfferingId, CancellationToken ct = default);
    Task<QuizAccommodation?> GetAccommodationAsync(Guid id, CancellationToken ct = default);
    /// <summary>Quiz-specific accommodation for the student, else the offering-wide one.</summary>
    Task<QuizAccommodation?> GetEffectiveAccommodationAsync(Guid courseOfferingId, Guid quizId, Guid studentProfileId, CancellationToken ct = default);
    Task AddAccommodationAsync(QuizAccommodation accommodation, CancellationToken ct = default);
    void RemoveAccommodation(QuizAccommodation accommodation);

    Task<IReadOnlyList<QuizAvailabilityOverride>> GetOverridesAsync(Guid quizId, CancellationToken ct = default);
    Task<QuizAvailabilityOverride?> GetOverrideAsync(Guid id, CancellationToken ct = default);
    Task<QuizAvailabilityOverride?> GetOverrideForStudentAsync(Guid quizId, Guid studentProfileId, CancellationToken ct = default);
    Task AddOverrideAsync(QuizAvailabilityOverride availabilityOverride, CancellationToken ct = default);
    void RemoveOverride(QuizAvailabilityOverride availabilityOverride);

    /// <summary>Display names for student profiles (best effort).</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetStudentNamesAsync(IReadOnlyCollection<Guid> studentProfileIds, CancellationToken ct = default);

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
