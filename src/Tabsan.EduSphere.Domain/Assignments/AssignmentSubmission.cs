using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Assignments;

/// <summary>
/// Current lifecycle state of a student's assignment submission.
/// </summary>
public enum SubmissionStatus
{
    /// <summary>Submitted by the student and awaiting faculty review.</summary>
    Submitted = 1,

    /// <summary>Marks and optional feedback have been entered by the faculty member.</summary>
    Graded = 2,

    /// <summary>Submission was rejected (e.g. submitted after deadline by override).</summary>
    Rejected = 3
}

/// <summary>
/// A student's submission for a specific <see cref="Assignment"/>.
/// There is at most one submission per student per assignment (enforced by unique index).
/// Submission rows are NEVER deleted — they form permanent academic evidence.
/// </summary>
public class AssignmentSubmission : BaseEntity
{
    /// <summary>FK to the assignment being submitted.</summary>
    public Guid AssignmentId { get; private set; }

    /// <summary>Navigation to the parent assignment.</summary>
    public Assignment Assignment { get; private set; } = default!;

    /// <summary>FK to the student profile who made the submission.</summary>
    public Guid StudentProfileId { get; private set; }

    /// <summary>
    /// Optional URL or path to the uploaded file stored in blob/object storage.
    /// Null when the submission is text-only.
    /// </summary>
    public string? FileUrl { get; private set; }

    /// <summary>
    /// Optional free-text body of the submission.
    /// Null when the submission is file-only.
    /// </summary>
    public string? TextContent { get; private set; }

    /// <summary>UTC timestamp of when the student pressed Submit.</summary>
    public DateTime SubmittedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Marks awarded by the faculty member.
    /// Null until the submission is graded.
    /// Must be between 0 and the parent Assignment's MaxMarks.
    /// </summary>
    public decimal? MarksAwarded { get; private set; }

    /// <summary>Optional written feedback from the faculty member.</summary>
    public string? Feedback { get; private set; }

    /// <summary>UTC timestamp of when marks were awarded.</summary>
    public DateTime? GradedAt { get; private set; }

    /// <summary>FK to the faculty user who graded this submission.</summary>
    public Guid? GradedByUserId { get; private set; }

    /// <summary>Current lifecycle state of this submission.</summary>
    public SubmissionStatus Status { get; private set; } = SubmissionStatus.Submitted;

    // ── Plan M3 ───────────────────────────────────────────────────────────────

    /// <summary>Number of the latest attempt (1-based). <see cref="FileUrl"/>/<see cref="TextContent"/>/<see cref="SubmittedAt"/> mirror it.</summary>
    public int CurrentAttemptNumber { get; private set; } = 1;

    /// <summary>True when the latest attempt was late.</summary>
    public bool IsLate { get; private set; }

    /// <summary>When the grade became visible to the student (null = hidden / not graded).</summary>
    public DateTime? PostedAt { get; private set; }

    // ── Plan M8 ───────────────────────────────────────────────────────────────

    /// <summary>The grade as entered, before any late deduction (<see cref="MarksAwarded"/> is the final grade).</summary>
    public decimal? RawMarks { get; private set; }

    /// <summary>Marks deducted by the course's late policy.</summary>
    public decimal LatePenalty { get; private set; }

    /// <summary>The teacher waived the late deduction for this submission.</summary>
    public bool LatePenaltyWaived { get; private set; }

    /// <summary>Graded automatically by the missing-submission policy (no work was handed in).</summary>
    public bool IsMissing { get; private set; }

    /// <summary>The group that handed this in together (group assignments).</summary>
    public Guid? GroupId { get; private set; }

    private readonly List<SubmissionAttempt> _attempts = new();
    public IReadOnlyCollection<SubmissionAttempt> Attempts => _attempts;

    /// <summary>True when the student may see marks, feedback, grader comments and annotations.</summary>
    public bool IsGradeVisibleToStudent => PostedAt.HasValue && MarksAwarded.HasValue;

    private AssignmentSubmission() { }

    /// <summary>
    /// Creates a new submission. At least one of fileUrl or textContent must be provided.
    /// </summary>
    public AssignmentSubmission(Guid assignmentId, Guid studentProfileId, string? fileUrl, string? textContent)
    {
        if (string.IsNullOrWhiteSpace(fileUrl) && string.IsNullOrWhiteSpace(textContent))
            throw new ArgumentException("A submission must have either a file URL or text content.");

        AssignmentId = assignmentId;
        StudentProfileId = studentProfileId;
        FileUrl = fileUrl?.Trim();
        TextContent = textContent?.Trim();
    }

    /// <summary>Plan M3: a submission whose content arrives as its first attempt (files and/or text).</summary>
    public static AssignmentSubmission Start(Guid assignmentId, Guid studentProfileId)
        => new() { AssignmentId = assignmentId, StudentProfileId = studentProfileId, CurrentAttemptNumber = 0 };

    /// <summary>
    /// Plan M3: records a new attempt. The submission's summary fields follow the latest attempt, and a
    /// graded submission goes back to "Submitted" (needs grading) — the earlier grade stays visible until regraded.
    /// The caller adds the returned attempt (and its files) to the unit of work.
    /// </summary>
    public SubmissionAttempt AddAttempt(string? textContent, string? legacyFileUrl, DateTime submittedAt, bool isLate)
    {
        // CurrentAttemptNumber is persisted, so numbering works without loading the attempts.
        var number = CurrentAttemptNumber + 1;
        var attempt = new SubmissionAttempt(Id, number, textContent, submittedAt, isLate);
        if (!string.IsNullOrWhiteSpace(legacyFileUrl)) attempt.AddLegacyFile(legacyFileUrl);
        _attempts.Add(attempt);

        CurrentAttemptNumber = number;
        TextContent = attempt.TextContent;
        FileUrl = string.IsNullOrWhiteSpace(legacyFileUrl) ? null : legacyFileUrl.Trim();
        SubmittedAt = submittedAt;
        IsLate = isLate;
        if (Status != SubmissionStatus.Submitted) Status = SubmissionStatus.Submitted;
        if (IsMissing)
        {
            // Plan M8: real work replaces the automatic "missing" grade.
            IsMissing = false;
            MarksAwarded = null;
            RawMarks = null;
            LatePenalty = 0;
            GradedAt = null;
            GradedByUserId = null;
            PostedAt = null;
        }
        return attempt;
    }

    /// <summary>Plan M8.3: marks the submission as handed in by a group.</summary>
    public void SetGroup(Guid groupId) => GroupId = groupId;

    /// <summary>Plan M8.1: a grade for work never handed in. Only possible while there is no attempt.</summary>
    public bool MarkMissing(decimal marks, DateTime now, bool postNow)
    {
        if (CurrentAttemptNumber > 0 || Status == SubmissionStatus.Rejected) return false;
        if (IsMissing && MarksAwarded == marks) return false;
        IsMissing = true;
        RawMarks = marks;
        LatePenalty = 0;
        MarksAwarded = marks;
        Feedback = null;
        GradedAt = now;
        GradedByUserId = null;
        Status = SubmissionStatus.Graded;
        if (postNow) PostedAt ??= now;
        return true;
    }

    /// <summary>Plan M8.1: waives (or re-applies) the late deduction; <paramref name="penalty"/> is the deduction the policy gives.</summary>
    public void SetLatePenaltyWaived(bool waived, decimal penalty)
    {
        LatePenaltyWaived = waived;
        if (RawMarks is not { } raw) return;
        LatePenalty = waived ? 0 : Math.Clamp(penalty, 0, raw);
        MarksAwarded = raw - LatePenalty;
    }

    /// <summary>
    /// Records marks and optional feedback for this submission.
    /// Marks must be between 0 and the assignment's max marks — validated at the service layer.
    /// The grade is posted at once (the behaviour before Plan M3 posting policies).
    /// </summary>
    public void Grade(decimal marksAwarded, string? feedback, Guid gradedByUserId)
        => Grade(marksAwarded, feedback, gradedByUserId, postNow: true);

    /// <summary>Plan M3.5: grades, posting immediately only when the assignment's policy is automatic.</summary>
    public void Grade(decimal marksAwarded, string? feedback, Guid gradedByUserId, bool postNow)
        => Grade(marksAwarded, feedback, gradedByUserId, postNow, latePenalty: 0);

    /// <summary>
    /// Plan M8.1: grades with the late policy's deduction. <paramref name="marksAwarded"/> is the grade as entered;
    /// the final grade (<see cref="MarksAwarded"/>) is that minus the deduction, unless the teacher waived it.
    /// </summary>
    public void Grade(decimal marksAwarded, string? feedback, Guid gradedByUserId, bool postNow, decimal latePenalty)
    {
        if (Status == SubmissionStatus.Rejected)
            throw new InvalidOperationException("Cannot grade a rejected submission.");

        RawMarks = marksAwarded;
        LatePenalty = LatePenaltyWaived ? 0 : Math.Clamp(latePenalty, 0, marksAwarded);
        IsMissing = false;
        marksAwarded -= LatePenalty;
        MarksAwarded = marksAwarded;
        Feedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();
        GradedAt = DateTime.UtcNow;
        GradedByUserId = gradedByUserId;
        Status = SubmissionStatus.Graded;
        if (postNow) PostedAt ??= GradedAt;
    }

    /// <summary>Plan M3.5: makes the grade visible to the student. Returns false when there is nothing to post.</summary>
    public bool Post(DateTime now)
    {
        if (!MarksAwarded.HasValue || PostedAt.HasValue) return false;
        PostedAt = now;
        return true;
    }

    /// <summary>Plan M3.5: hides a posted grade again.</summary>
    public bool Hide()
    {
        if (!PostedAt.HasValue) return false;
        PostedAt = null;
        return true;
    }

    /// <summary>
    /// Rejects the submission (e.g. plagiarism or late override).
    /// Sets status to Rejected; previously awarded marks are cleared.
    /// </summary>
    public void Reject()
    {
        Status = SubmissionStatus.Rejected;
        MarksAwarded = null;
        RawMarks = null;
        LatePenalty = 0;
        IsMissing = false;
        GradedAt = null;
        PostedAt = null;
    }
}
