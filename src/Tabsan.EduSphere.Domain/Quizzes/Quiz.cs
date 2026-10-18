using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Quizzes;

/// <summary>
/// Defines how quiz questions are structured.
/// Stored as a string in the database for readability.
/// Type-specific settings (pairs, blanks, tolerances, formulas …) live in <see cref="QuizQuestion.ConfigJson"/>.
/// </summary>
public enum QuestionType
{
    /// <summary>Multiple choice with exactly one correct option (radio buttons).</summary>
    MultipleChoice = 1,

    /// <summary>True/False question with exactly two options.</summary>
    TrueFalse = 2,

    /// <summary>Short free-text response graded manually by faculty.</summary>
    ShortAnswer = 3,

    /// <summary>Plan M2: several correct options (checkboxes); partial credit optional.</summary>
    MultipleAnswers = 4,

    /// <summary>Plan M2: match each prompt to an answer (with optional distractors).</summary>
    Matching = 5,

    /// <summary>Plan M2: put items in the correct order.</summary>
    Ordering = 6,

    /// <summary>Plan M2: one or more blanks in the text, each with accepted answers.</summary>
    FillInBlank = 7,

    /// <summary>Plan M2: numeric answer, exact with tolerance or within a range.</summary>
    Numeric = 8,

    /// <summary>Plan M2: numeric answer computed from a formula with per-attempt generated variables.</summary>
    Formula = 9,

    /// <summary>Plan M2: long rich-text answer graded manually.</summary>
    Essay = 10,

    /// <summary>Plan M2: the student uploads a file; graded manually.</summary>
    FileUpload = 11
}

/// <summary>Plan M2.6: what a student may see after submitting an attempt.</summary>
public enum QuizReviewVisibility
{
    /// <summary>Nothing beyond "submitted".</summary>
    None = 0,

    /// <summary>Only the total score.</summary>
    ScoreOnly = 1,

    /// <summary>Score plus the student's own responses and per-question marks.</summary>
    ResponsesAndScore = 2,

    /// <summary>Everything, including the correct answers.</summary>
    CorrectAnswers = 3
}

/// <summary>
/// A quiz authored by faculty and linked to a <see cref="CourseOffering"/>.
/// Business rules:
///   - Once published a quiz cannot be deleted, only deactivated.
///   - Students may not attempt an unpublished quiz.
///   - Attempts are capped at <see cref="MaxAttempts"/> per student.
/// </summary>
public class Quiz : BaseEntity
{
    /// <summary>FK to the course offering this quiz belongs to.</summary>
    public Guid CourseOfferingId { get; private set; }

    /// <summary>Descriptive title shown to students in the quiz list.</summary>
    public string Title { get; private set; } = default!;

    /// <summary>Optional instructions displayed before the student begins.</summary>
    public string? Instructions { get; private set; }

    /// <summary>
    /// Allowed time for each attempt in minutes.
    /// Null means no time limit.
    /// </summary>
    public int? TimeLimitMinutes { get; private set; }

    /// <summary>
    /// Maximum number of attempts a student is allowed.
    /// Defaults to 1.  0 means unlimited.
    /// </summary>
    public int MaxAttempts { get; private set; } = 1;

    /// <summary>UTC date-time from which the quiz is accessible to students.</summary>
    public DateTime? AvailableFrom { get; private set; }

    /// <summary>UTC date-time after which no new attempts are accepted.</summary>
    public DateTime? AvailableUntil { get; private set; }

    /// <summary>Whether the quiz is visible and open to students.</summary>
    public bool IsPublished { get; private set; }

    /// <summary>Whether the quiz is active (false means soft-deleted).</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>FK to the faculty user who created this quiz.</summary>
    public Guid CreatedByUserId { get; private set; }

    // ── Plan M2.4 / M2.6 settings ───────────────────────────────────────────────

    /// <summary>Present questions in a different random order to each attempt.</summary>
    public bool ShuffleQuestions { get; private set; }

    /// <summary>Present answer options (and matching / ordering items) in random order.</summary>
    public bool ShuffleOptions { get; private set; }

    /// <summary>Optional code students must enter to start an attempt (e.g. given out in the exam room).</summary>
    public string? AccessCode { get; private set; }

    /// <summary>Optional allow-list of client addresses: comma-separated IPs or CIDR ranges.</summary>
    public string? AllowedIpRanges { get; private set; }

    /// <summary>Show one question at a time.</summary>
    public bool OneQuestionAtATime { get; private set; }

    /// <summary>Once a question's answer is saved it can no longer be changed.</summary>
    public bool LockAnsweredQuestions { get; private set; }

    /// <summary>What students may see after submitting.</summary>
    public QuizReviewVisibility ReviewVisibility { get; private set; } = QuizReviewVisibility.ScoreOnly;

    /// <summary>When true, the review (beyond score) is only shown after <see cref="AvailableUntil"/> has passed.</summary>
    public bool ReviewAfterCloseOnly { get; private set; }

    /// <summary>Navigation collection of questions belonging to this quiz.</summary>
    public IReadOnlyCollection<QuizQuestion> Questions { get; private set; } = new List<QuizQuestion>();

    /// <summary>Plan M2.2: "pick N random questions from bank X" groups.</summary>
    public IReadOnlyCollection<QuizQuestionGroup> QuestionGroups { get; private set; } = new List<QuizQuestionGroup>();

    // ── EF constructor ─────────────────────────────────────────────────────────
    private Quiz() { }

    /// <summary>
    /// Creates a new unpublished quiz for a course offering.
    /// </summary>
    /// <param name="courseOfferingId">The offering this quiz belongs to.</param>
    /// <param name="title">Short descriptive title (max 300 characters).</param>
    /// <param name="createdByUserId">FK to the faculty user creating the quiz.</param>
    /// <param name="instructions">Optional instructions shown before the attempt starts.</param>
    /// <param name="timeLimitMinutes">Per-attempt time cap in minutes; null means unlimited.</param>
    /// <param name="maxAttempts">Attempt cap per student; 1 by default.</param>
    /// <param name="availableFrom">UTC open date; null means immediately when published.</param>
    /// <param name="availableUntil">UTC close date; null means no expiry.</param>
    public Quiz(
        Guid courseOfferingId,
        string title,
        Guid createdByUserId,
        string? instructions = null,
        int? timeLimitMinutes = null,
        int maxAttempts = 1,
        DateTime? availableFrom = null,
        DateTime? availableUntil = null)
    {
        CourseOfferingId  = courseOfferingId;
        Title             = title;
        CreatedByUserId   = createdByUserId;
        Instructions      = instructions;
        TimeLimitMinutes  = timeLimitMinutes;
        MaxAttempts       = maxAttempts < 0 ? 0 : maxAttempts;
        AvailableFrom     = availableFrom;
        AvailableUntil    = availableUntil;
    }

    /// <summary>
    /// Publishes the quiz so it becomes visible and accessible to students.
    /// Has no effect if already published.
    /// </summary>
    public void Publish()
    {
        if (IsPublished) return;
        IsPublished = true;
        Touch();
    }

    /// <summary>
    /// Unpublishes the quiz, hiding it from students without deleting it.
    /// Running attempts are not affected.
    /// </summary>
    public void Unpublish()
    {
        if (!IsPublished) return;
        IsPublished = false;
        Touch();
    }

    /// <summary>
    /// Soft-deletes the quiz. Can only be applied to unpublished quizzes.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    /// <summary>
    /// Reactivates a previously deactivated quiz.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    /// <summary>
    /// Updates the editable fields of an unpublished quiz.
    /// </summary>
    /// <param name="title">New title.</param>
    /// <param name="instructions">New instructions text.</param>
    /// <param name="timeLimitMinutes">New time limit in minutes.</param>
    /// <param name="maxAttempts">New attempt cap.</param>
    /// <param name="availableFrom">New open date.</param>
    /// <param name="availableUntil">New close date.</param>
    public void Update(
        string title,
        string? instructions,
        int? timeLimitMinutes,
        int maxAttempts,
        DateTime? availableFrom,
        DateTime? availableUntil)
    {
        Title            = title;
        Instructions     = instructions;
        TimeLimitMinutes = timeLimitMinutes;
        MaxAttempts      = maxAttempts < 0 ? 0 : maxAttempts;
        AvailableFrom    = availableFrom;
        AvailableUntil   = availableUntil;
        Touch();
    }

    /// <summary>Plan M2: updates presentation, security and review settings.</summary>
    public void ConfigureDelivery(
        bool shuffleQuestions,
        bool shuffleOptions,
        bool oneQuestionAtATime,
        bool lockAnsweredQuestions,
        string? accessCode,
        string? allowedIpRanges,
        QuizReviewVisibility reviewVisibility,
        bool reviewAfterCloseOnly)
    {
        ShuffleQuestions      = shuffleQuestions;
        ShuffleOptions        = shuffleOptions;
        OneQuestionAtATime    = oneQuestionAtATime;
        LockAnsweredQuestions = lockAnsweredQuestions;
        AccessCode            = string.IsNullOrWhiteSpace(accessCode) ? null : accessCode.Trim();
        AllowedIpRanges       = string.IsNullOrWhiteSpace(allowedIpRanges) ? null : allowedIpRanges.Trim();
        ReviewVisibility      = reviewVisibility;
        ReviewAfterCloseOnly  = reviewAfterCloseOnly;
        Touch();
    }
}

/// <summary>
/// A question. It belongs either to a <see cref="Quiz"/> (<see cref="QuizId"/>) or to a
/// <see cref="QuestionBank"/> (<see cref="QuestionBankId"/>) — exactly one of the two.
/// Bank questions are drawn into attempts by <see cref="QuizQuestionGroup"/>s and answered like
/// quiz questions, so options, graders, answers and statistics share one model.
/// </summary>
public class QuizQuestion : BaseEntity
{
    /// <summary>FK to the parent quiz. Null for bank questions.</summary>
    public Guid? QuizId { get; private set; }

    /// <summary>Navigation property to the parent quiz (null for bank questions).</summary>
    public Quiz? Quiz { get; private set; }

    /// <summary>Plan M2.2: FK to the owning question bank. Null for quiz questions.</summary>
    public Guid? QuestionBankId { get; private set; }

    /// <summary>Navigation to the owning bank (null for quiz questions).</summary>
    public QuestionBank? QuestionBank { get; private set; }

    /// <summary>The question text shown to the student (FillInBlank uses [[id]] placeholders, Formula uses [name]).</summary>
    public string Text { get; private set; } = default!;

    /// <summary>Type of question — controls how options and answers are handled.</summary>
    public QuestionType Type { get; private set; }

    /// <summary>Marks awarded for a fully correct answer.</summary>
    public decimal Marks { get; private set; }

    /// <summary>Display position within the quiz or bank (1-based).</summary>
    public int OrderIndex { get; private set; }

    /// <summary>Plan M2.3: type-specific settings as JSON (see the question graders for each schema).</summary>
    public string? ConfigJson { get; private set; }

    /// <summary>Plan M2.2: comma-separated tags (bank questions; used by draw filters and reports).</summary>
    public string? Tags { get; private set; }

    /// <summary>Plan M2.2: optional difficulty 1 (easy) – 5 (hard).</summary>
    public int? Difficulty { get; private set; }

    /// <summary>
    /// Plan M2.2: an archived bank question is no longer drawn but is kept because attempts reference it.
    /// </summary>
    public bool IsArchived { get; private set; }

    /// <summary>Navigation collection of answer options (MultipleChoice, MultipleAnswers, TrueFalse).</summary>
    public IReadOnlyCollection<QuizOption> Options { get; private set; } = new List<QuizOption>();

    // ── EF constructor ─────────────────────────────────────────────────────────
    private QuizQuestion() { }

    /// <summary>
    /// Creates a question for a quiz.
    /// </summary>
    /// <param name="quizId">FK to the parent quiz.</param>
    /// <param name="text">Question prompt text.</param>
    /// <param name="type">Question type.</param>
    /// <param name="marks">Full marks value for this question.</param>
    /// <param name="orderIndex">Display position within the quiz.</param>
    /// <param name="configJson">Type-specific settings (Plan M2.3).</param>
    public QuizQuestion(Guid quizId, string text, QuestionType type, decimal marks, int orderIndex, string? configJson = null)
    {
        QuizId     = quizId;
        Text       = text;
        Type       = type;
        Marks      = marks;
        OrderIndex = orderIndex;
        ConfigJson = configJson;
    }

    /// <summary>Plan M2.2: creates a question owned by a question bank.</summary>
    public static QuizQuestion ForBank(
        Guid questionBankId, string text, QuestionType type, decimal marks, int orderIndex,
        string? configJson = null, string? tags = null, int? difficulty = null)
        => new()
        {
            QuestionBankId = questionBankId,
            Text = text,
            Type = type,
            Marks = marks,
            OrderIndex = orderIndex,
            ConfigJson = configJson,
            Tags = NormalizeTags(tags),
            Difficulty = ClampDifficulty(difficulty)
        };

    /// <summary>True when the question belongs to a bank rather than a quiz.</summary>
    public bool IsBankQuestion => QuestionBankId.HasValue;

    /// <summary>
    /// Updates the question text, marks, and ordering.
    /// </summary>
    /// <param name="text">Updated question prompt.</param>
    /// <param name="marks">Updated marks value.</param>
    /// <param name="orderIndex">Updated display position.</param>
    public void Update(string text, decimal marks, int orderIndex)
    {
        Text       = text;
        Marks      = marks;
        OrderIndex = orderIndex;
        Touch();
    }

    /// <summary>Plan M2.3: replaces the type-specific settings.</summary>
    public void SetConfig(string? configJson)
    {
        ConfigJson = string.IsNullOrWhiteSpace(configJson) ? null : configJson;
        Touch();
    }

    /// <summary>Plan M2.2: updates bank metadata.</summary>
    public void SetBankMetadata(string? tags, int? difficulty)
    {
        Tags = NormalizeTags(tags);
        Difficulty = ClampDifficulty(difficulty);
        Touch();
    }

    /// <summary>Plan M2.2: stop drawing this bank question while keeping attempt history intact.</summary>
    public void Archive()
    {
        IsArchived = true;
        Touch();
    }

    /// <summary>Plan M2.2: copies this question (bank → quiz). Options are copied by the caller.</summary>
    public QuizQuestion CloneToQuiz(Guid quizId, int orderIndex, decimal? marks = null)
        => new(quizId, Text, Type, marks ?? Marks, orderIndex, ConfigJson);

    /// <summary>Returns the tags as a list.</summary>
    public IReadOnlyList<string> GetTags()
        => string.IsNullOrWhiteSpace(Tags) ? [] : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string? NormalizeTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags)) return null;
        var list = tags.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .ToList();
        return list.Count == 0 ? null : string.Join(',', list);
    }

    private static int? ClampDifficulty(int? difficulty)
        => difficulty is null ? null : Math.Clamp(difficulty.Value, 1, 5);
}

/// <summary>
/// An answer option for a <see cref="QuizQuestion"/> of type MultipleChoice, MultipleAnswers or TrueFalse.
/// MultipleChoice / TrueFalse have exactly one correct option; MultipleAnswers may have several.
/// </summary>
public class QuizOption : BaseEntity
{
    /// <summary>FK to the parent question.</summary>
    public Guid QuizQuestionId { get; private set; }

    /// <summary>Navigation property to the parent question.</summary>
    public QuizQuestion Question { get; private set; } = default!;

    /// <summary>Display text of the option shown to the student.</summary>
    public string Text { get; private set; } = default!;

    /// <summary>Whether selecting this option contributes to a correct answer.</summary>
    public bool IsCorrect { get; private set; }

    /// <summary>Display order within the question's option list.</summary>
    public int OrderIndex { get; private set; }

    // ── EF constructor ─────────────────────────────────────────────────────────
    private QuizOption() { }

    /// <summary>
    /// Creates an answer option for a question.
    /// </summary>
    /// <param name="quizQuestionId">FK to the parent question.</param>
    /// <param name="text">Option display text.</param>
    /// <param name="isCorrect">True if this is a correct answer choice.</param>
    /// <param name="orderIndex">Display position among sibling options.</param>
    public QuizOption(Guid quizQuestionId, string text, bool isCorrect, int orderIndex)
    {
        QuizQuestionId = quizQuestionId;
        Text           = text;
        IsCorrect      = isCorrect;
        OrderIndex     = orderIndex;
    }

    /// <summary>Plan M2: edits the option in place (used when answers already reference it).</summary>
    public void Update(string text, bool isCorrect, int orderIndex)
    {
        Text       = text;
        IsCorrect  = isCorrect;
        OrderIndex = orderIndex;
        Touch();
    }
}
