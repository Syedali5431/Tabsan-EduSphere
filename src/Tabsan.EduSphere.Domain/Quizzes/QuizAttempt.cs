using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Quizzes;

/// <summary>
/// Lifecycle states of a student's quiz attempt.
/// </summary>
public enum AttemptStatus
{
    /// <summary>Attempt has started but not yet been submitted.</summary>
    InProgress = 1,

    /// <summary>Student explicitly submitted the attempt before time expired.</summary>
    Submitted = 2,

    /// <summary>Attempt was auto-submitted when the time limit expired.</summary>
    TimedOut = 3,

    /// <summary>Attempt was abandoned (browser closed, no submission) — scored as zero.</summary>
    Abandoned = 4
}

/// <summary>
/// Records a single attempt by a student on a <see cref="Quiz"/>.
/// Business rules:
///   - Total attempts per student must not exceed <see cref="Quiz.MaxAttempts"/> (0 = unlimited).
///   - A new attempt cannot be started while a previous InProgress attempt exists.
///   - Once submitted or timed-out, answers are immutable (except faculty grading / regrade).
/// </summary>
public class QuizAttempt : BaseEntity
{
    /// <summary>FK to the quiz being attempted.</summary>
    public Guid QuizId { get; private set; }

    /// <summary>Navigation property to the parent quiz.</summary>
    public Quiz Quiz { get; private set; } = default!;

    /// <summary>FK to the student profile taking the quiz.</summary>
    public Guid StudentProfileId { get; private set; }

    /// <summary>UTC date-time when the student opened the quiz.</summary>
    public DateTime StartedAt { get; private set; }

    /// <summary>UTC date-time when the attempt was finalised (submitted or timed out). Null while in progress.</summary>
    public DateTime? FinishedAt { get; private set; }

    /// <summary>Current lifecycle state of this attempt.</summary>
    public AttemptStatus Status { get; private set; }

    /// <summary>
    /// Aggregate score. Null until every answer is graded (manual question types are graded by faculty).
    /// </summary>
    public decimal? TotalScore { get; private set; }

    /// <summary>
    /// Plan M2.5: server-side deadline (start + effective time limit incl. accommodations, capped by the
    /// availability window). Null = no time limit. Answers arriving after it are refused and the attempt times out.
    /// </summary>
    public DateTime? DeadlineAt { get; private set; }

    /// <summary>Client address that started the attempt (audit).</summary>
    public string? StartedFromIp { get; private set; }

    /// <summary>Navigation collection of per-question answers for this attempt.</summary>
    public IReadOnlyCollection<QuizAnswer> Answers { get; private set; } = new List<QuizAnswer>();

    /// <summary>
    /// Plan M2.4: the questions this attempt was given, in presentation order. Empty for attempts
    /// started before Plan M2 (those fall back to the quiz's own questions).
    /// </summary>
    public IReadOnlyCollection<QuizAttemptItem> Items { get; private set; } = new List<QuizAttemptItem>();

    // ── EF constructor ─────────────────────────────────────────────────────────
    private QuizAttempt() { }

    /// <summary>
    /// Opens a new in-progress attempt for a student.
    /// </summary>
    /// <param name="quizId">FK to the quiz.</param>
    /// <param name="studentProfileId">FK to the student starting the attempt.</param>
    /// <param name="deadlineAt">Plan M2.5: server-enforced deadline, or null for untimed.</param>
    /// <param name="startedFromIp">Client address (audit).</param>
    public QuizAttempt(Guid quizId, Guid studentProfileId, DateTime? deadlineAt = null, string? startedFromIp = null)
    {
        QuizId           = quizId;
        StudentProfileId = studentProfileId;
        StartedAt        = DateTime.UtcNow;
        Status           = AttemptStatus.InProgress;
        DeadlineAt       = deadlineAt;
        StartedFromIp    = startedFromIp;
    }

    /// <summary>True while the attempt can still take answers at <paramref name="utcNow"/> (with a small network grace).</summary>
    public bool IsOpenAt(DateTime utcNow, TimeSpan? grace = null)
        => Status == AttemptStatus.InProgress
           && (DeadlineAt is null || utcNow <= DeadlineAt.Value + (grace ?? TimeSpan.FromSeconds(30)));

    /// <summary>
    /// Marks the attempt as submitted by the student and records the finish time.
    /// Has no effect if the attempt is already finalised.
    /// </summary>
    public void Submit()
    {
        if (Status != AttemptStatus.InProgress) return;
        Status     = AttemptStatus.Submitted;
        FinishedAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    /// Marks the attempt as timed-out (auto-submitted by the server when time expires).
    /// Has no effect if the attempt is already finalised.
    /// </summary>
    public void TimeOut()
    {
        if (Status != AttemptStatus.InProgress) return;
        Status     = AttemptStatus.TimedOut;
        FinishedAt = DeadlineAt ?? DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    /// Marks the attempt as abandoned (e.g., connection lost with no submission).
    /// Has no effect if the attempt is already finalised.
    /// </summary>
    public void Abandon()
    {
        if (Status != AttemptStatus.InProgress) return;
        Status     = AttemptStatus.Abandoned;
        FinishedAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    /// Records the total score once every answer is graded.
    /// </summary>
    /// <param name="score">Sum of awarded marks.</param>
    public void RecordScore(decimal score)
    {
        TotalScore = score;
        Touch();
    }

    /// <summary>Clears the total while manual grading is pending again (e.g. after a regrade).</summary>
    public void ClearScore()
    {
        TotalScore = null;
        Touch();
    }
}

/// <summary>
/// Plan M2.4: one question as presented in one attempt — order, effective marks (bank groups override
/// the question's own marks), shuffled option order and generated formula values. Review and regrade
/// always use this snapshot, so a student sees exactly what they answered.
/// </summary>
public class QuizAttemptItem : BaseEntity
{
    public Guid QuizAttemptId { get; private set; }
    public QuizAttempt Attempt { get; private set; } = default!;

    public Guid QuizQuestionId { get; private set; }
    public QuizQuestion Question { get; private set; } = default!;

    /// <summary>1-based position in this attempt.</summary>
    public int OrderIndex { get; private set; }

    /// <summary>Marks this question is worth in this attempt.</summary>
    public decimal Marks { get; private set; }

    /// <summary>Drawn by this group (null for the quiz's own questions).</summary>
    public Guid? QuizQuestionGroupId { get; private set; }

    /// <summary>JSON array of option ids (or item ids for Matching / Ordering) in presentation order.</summary>
    public string? PresentationJson { get; private set; }

    /// <summary>Formula questions: JSON object of generated variable values.</summary>
    public string? GeneratedValuesJson { get; private set; }

    private QuizAttemptItem() { }

    public QuizAttemptItem(Guid quizAttemptId, Guid quizQuestionId, int orderIndex, decimal marks,
        Guid? quizQuestionGroupId, string? presentationJson, string? generatedValuesJson)
    {
        QuizAttemptId = quizAttemptId;
        QuizQuestionId = quizQuestionId;
        OrderIndex = orderIndex;
        Marks = marks;
        QuizQuestionGroupId = quizQuestionGroupId;
        PresentationJson = presentationJson;
        GeneratedValuesJson = generatedValuesJson;
    }
}

/// <summary>
/// A student's response to a single <see cref="QuizQuestion"/> within a <see cref="QuizAttempt"/>.
/// MultipleChoice / TrueFalse: <see cref="SelectedOptionId"/> (+ one <see cref="Selections"/> row).
/// MultipleAnswers: <see cref="Selections"/>. ShortAnswer / Essay: <see cref="TextResponse"/>.
/// Matching, Ordering, FillInBlank, Numeric, Formula, FileUpload: <see cref="ResponseJson"/>.
/// MarksAwarded is null until graded.
/// </summary>
public class QuizAnswer : BaseEntity
{
    /// <summary>FK to the parent attempt.</summary>
    public Guid QuizAttemptId { get; private set; }

    /// <summary>Navigation property to the parent attempt.</summary>
    public QuizAttempt Attempt { get; private set; } = default!;

    /// <summary>FK to the question being answered.</summary>
    public Guid QuizQuestionId { get; private set; }

    /// <summary>Navigation property to the question.</summary>
    public QuizQuestion Question { get; private set; } = default!;

    /// <summary>
    /// FK to the option selected for MultipleChoice/TrueFalse questions (kept for compatibility;
    /// <see cref="Selections"/> is the source of truth for every option-based type).
    /// </summary>
    public Guid? SelectedOptionId { get; private set; }

    /// <summary>Free-text response for ShortAnswer and Essay questions.</summary>
    public string? TextResponse { get; private set; }

    /// <summary>Plan M2.1: structured response for the newer question types (JSON).</summary>
    public string? ResponseJson { get; private set; }

    /// <summary>Plan M2.1: every selected option (multi-select safe).</summary>
    public IReadOnlyCollection<QuizAnswerSelection> Selections => _selections;
    private readonly List<QuizAnswerSelection> _selections = new();

    /// <summary>
    /// Marks awarded for this answer.
    /// Populated automatically for auto-graded types; set manually by faculty for the others.
    /// </summary>
    public decimal? MarksAwarded { get; private set; }

    /// <summary>Plan M2.6: grader feedback shown to the student when review allows it.</summary>
    public string? Feedback { get; private set; }

    /// <summary>Plan M2.6: who graded it manually (null for auto-grading).</summary>
    public Guid? GradedByUserId { get; private set; }

    public DateTime? GradedAt { get; private set; }

    /// <summary>Plan M2.6: last time the student saved this answer.</summary>
    public DateTime? SavedAt { get; private set; }

    // ── EF constructor ─────────────────────────────────────────────────────────
    private QuizAnswer() { }

    /// <summary>Plan M2: an empty answer to be filled with <see cref="SetResponse"/>.</summary>
    public QuizAnswer(Guid quizAttemptId, Guid quizQuestionId)
    {
        QuizAttemptId  = quizAttemptId;
        QuizQuestionId = quizQuestionId;
    }

    /// <summary>
    /// Records a student's MCQ or TrueFalse answer by option selection.
    /// </summary>
    /// <param name="quizAttemptId">FK to the parent attempt.</param>
    /// <param name="quizQuestionId">FK to the question being answered.</param>
    /// <param name="selectedOptionId">FK to the chosen option.</param>
    public QuizAnswer(Guid quizAttemptId, Guid quizQuestionId, Guid selectedOptionId)
        : this(quizAttemptId, quizQuestionId)
    {
        SetResponse([selectedOptionId], null, null, singleSelection: true);
    }

    /// <summary>
    /// Records a student's ShortAnswer response.
    /// </summary>
    /// <param name="quizAttemptId">FK to the parent attempt.</param>
    /// <param name="quizQuestionId">FK to the question being answered.</param>
    /// <param name="textResponse">Free-text answer provided by the student.</param>
    public QuizAnswer(Guid quizAttemptId, Guid quizQuestionId, string textResponse)
        : this(quizAttemptId, quizQuestionId)
    {
        SetResponse(null, textResponse, null, singleSelection: false);
    }

    /// <summary>
    /// Plan M2.1: replaces the student's response. Selected options become <see cref="Selections"/>;
    /// <see cref="SelectedOptionId"/> mirrors the choice for single-select types.
    /// Clears any previous mark so the answer is graded again.
    /// </summary>
    public void SetResponse(IEnumerable<Guid>? selectedOptionIds, string? textResponse, string? responseJson, bool singleSelection)
    {
        var ids = (selectedOptionIds ?? []).Where(id => id != Guid.Empty).Distinct().ToList();
        if (singleSelection && ids.Count > 1)
            ids = ids.Take(1).ToList();

        _selections.RemoveAll(s => !ids.Contains(s.QuizOptionId));
        foreach (var id in ids.Where(id => _selections.All(s => s.QuizOptionId != id)))
            _selections.Add(new QuizAnswerSelection(Id, id));

        SelectedOptionId = singleSelection && ids.Count > 0 ? ids[0] : null;
        TextResponse = textResponse;
        ResponseJson = string.IsNullOrWhiteSpace(responseJson) ? null : responseJson;
        MarksAwarded = null;
        GradedByUserId = null;
        GradedAt = null;
        SavedAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>The selected option ids (multi-select safe; falls back to the legacy column).</summary>
    public IReadOnlyList<Guid> SelectedOptionIds
        => Selections.Count > 0
            ? Selections.Select(s => s.QuizOptionId).ToList()
            : SelectedOptionId.HasValue ? [SelectedOptionId.Value] : [];

    /// <summary>
    /// Awards marks for this answer (auto-grader or regrade).
    /// </summary>
    /// <param name="marks">Marks to award (must be &gt;= 0 and &lt;= question total marks).</param>
    public void AwardMarks(decimal marks)
    {
        MarksAwarded = marks;
        Touch();
    }

    /// <summary>Plan M2.6: manual grading by faculty, with optional feedback.</summary>
    public void Grade(decimal marks, string? feedback, Guid gradedByUserId)
    {
        if (marks < 0)
            throw new ArgumentException("Marks cannot be negative.", nameof(marks));

        MarksAwarded = marks;
        Feedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();
        GradedByUserId = gradedByUserId == Guid.Empty ? null : gradedByUserId;
        GradedAt = DateTime.UtcNow;
        Touch();
    }
}

/// <summary>Plan M2.1: one selected option of a <see cref="QuizAnswer"/>.</summary>
public class QuizAnswerSelection
{
    public Guid QuizAnswerId { get; private set; }
    public Guid QuizOptionId { get; private set; }

    private QuizAnswerSelection() { }

    public QuizAnswerSelection(Guid quizAnswerId, Guid quizOptionId)
    {
        QuizAnswerId = quizAnswerId;
        QuizOptionId = quizOptionId;
    }
}
