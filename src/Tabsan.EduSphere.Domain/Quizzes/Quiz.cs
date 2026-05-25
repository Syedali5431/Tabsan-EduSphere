using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Quizzes;

/// <summary>
/// Defines how quiz questions are structured.
/// Stored as a string in the database for readability.
/// </summary>
public enum QuestionType
{
    /// <summary>Multiple-choice with one or more selectable options.</summary>
    MultipleChoice = 1,

    /// <summary>True/False question with exactly two options.</summary>
    TrueFalse = 2,

    /// <summary>Free-text response graded manually by faculty.</summary>
    ShortAnswer = 3
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

    /// <summary>Navigation collection of questions belonging to this quiz.</summary>
    public IReadOnlyCollection<QuizQuestion> Questions { get; private set; } = new List<QuizQuestion>();

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
}

/// <summary>
/// A single question within a <see cref="Quiz"/>.
/// Questions are ordered and carry a marks value used for auto-scoring MCQ/TrueFalse types.
/// </summary>
public class QuizQuestion : BaseEntity
{
    /// <summary>FK to the parent quiz.</summary>
    public Guid QuizId { get; private set; }

    /// <summary>Navigation property to the parent quiz.</summary>
    public Quiz Quiz { get; private set; } = default!;

    /// <summary>The question text shown to the student.</summary>
    public string Text { get; private set; } = default!;

    /// <summary>Type of question — controls how options and answers are handled.</summary>
    public QuestionType Type { get; private set; }

    /// <summary>Marks awarded for a fully correct answer.</summary>
    public decimal Marks { get; private set; }

    /// <summary>Display position within the quiz (1-based).</summary>
    public int OrderIndex { get; private set; }

    /// <summary>Navigation collection of answer options (populated for MCQ and TrueFalse).</summary>
    public IReadOnlyCollection<QuizOption> Options { get; private set; } = new List<QuizOption>();

    // ── EF constructor ─────────────────────────────────────────────────────────
    private QuizQuestion() { }

    /// <summary>
    /// Creates a question for a quiz.
    /// </summary>
    /// <param name="quizId">FK to the parent quiz.</param>
    /// <param name="text">Question prompt text.</param>
    /// <param name="type">Question type (MCQ, TrueFalse, ShortAnswer).</param>
    /// <param name="marks">Full marks value for this question.</param>
    /// <param name="orderIndex">Display position within the quiz.</param>
    public QuizQuestion(Guid quizId, string text, QuestionType type, decimal marks, int orderIndex)
    {
        QuizId     = quizId;
        Text       = text;
        Type       = type;
        Marks      = marks;
        OrderIndex = orderIndex;
    }

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
}

/// <summary>
/// An answer option for a <see cref="QuizQuestion"/> of type MCQ or TrueFalse.
/// Exactly one option per question should have <see cref="IsCorrect"/> = true for auto-grading.
/// Multiple correct options are allowed for multi-select MCQ variants.
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
}
