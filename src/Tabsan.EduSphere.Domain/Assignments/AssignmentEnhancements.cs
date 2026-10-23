using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Assignments;

// Plan M8 — late & missing policy, due-date overrides, student groups and peer review.

public enum LateInterval
{
    Hour = 0,
    Day = 1
}

/// <summary>Plan M8.1: a course offering's late and missing-submission rules.</summary>
public class LatePolicy : BaseEntity
{
    public Guid CourseOfferingId { get; private set; }
    public bool LateDeductionEnabled { get; private set; }
    /// <summary>Percentage of the assignment's points deducted per started interval of lateness.</summary>
    public decimal DeductionPercent { get; private set; }
    public LateInterval Interval { get; private set; }
    /// <summary>Deductions never take a grade below this percentage of the points.</summary>
    public decimal LowestGradePercent { get; private set; }
    public bool MissingEnabled { get; private set; }
    /// <summary>Grade (percentage of the points) given automatically for work never handed in.</summary>
    public decimal MissingGradePercent { get; private set; }

    private LatePolicy() { }

    public LatePolicy(Guid courseOfferingId) => CourseOfferingId = courseOfferingId;

    public void Configure(bool lateDeductionEnabled, decimal deductionPercent, LateInterval interval, decimal lowestGradePercent,
        bool missingEnabled, decimal missingGradePercent)
    {
        if (lateDeductionEnabled && deductionPercent is <= 0 or > 100) throw new ArgumentException("The deduction must be between 0 and 100% per interval.", nameof(deductionPercent));
        if (lowestGradePercent is < 0 or > 100) throw new ArgumentException("The lowest grade must be between 0 and 100%.", nameof(lowestGradePercent));
        if (missingGradePercent is < 0 or > 100) throw new ArgumentException("The missing grade must be between 0 and 100%.", nameof(missingGradePercent));
        LateDeductionEnabled = lateDeductionEnabled;
        DeductionPercent = lateDeductionEnabled ? deductionPercent : 0;
        Interval = interval;
        LowestGradePercent = lowestGradePercent;
        MissingEnabled = missingEnabled;
        MissingGradePercent = missingGradePercent;
        Touch();
    }

    /// <summary>
    /// Marks deducted from <paramref name="rawMarks"/> for work handed in at <paramref name="submittedAt"/> against
    /// <paramref name="dueAt"/>: every started interval costs <see cref="DeductionPercent"/>% of the points, but the result
    /// never drops below <see cref="LowestGradePercent"/>% of the points (a grade already below that floor is not reduced).
    /// </summary>
    public decimal PenaltyFor(DateTime dueAt, DateTime submittedAt, decimal maxMarks, decimal rawMarks)
    {
        if (!LateDeductionEnabled || submittedAt <= dueAt || maxMarks <= 0 || rawMarks <= 0) return 0;
        var late = submittedAt - dueAt;
        var length = Interval == LateInterval.Day ? TimeSpan.FromDays(1) : TimeSpan.FromHours(1);
        var intervals = (decimal)Math.Ceiling(late.Ticks / (double)length.Ticks);
        var deduction = Math.Round(maxMarks * Math.Min(100, intervals * DeductionPercent) / 100, 2, MidpointRounding.AwayFromZero);
        var floor = Math.Round(maxMarks * LowestGradePercent / 100, 2, MidpointRounding.AwayFromZero);
        var adjusted = Math.Max(rawMarks - deduction, Math.Min(rawMarks, floor));
        return Math.Max(0, rawMarks - adjusted);
    }

    public decimal MissingMarks(decimal maxMarks) => Math.Round(maxMarks * MissingGradePercent / 100, 2, MidpointRounding.AwayFromZero);
}

/// <summary>Plan M8.2: different dates for one student or one group ("Assign to").</summary>
public class AssignmentOverride : BaseEntity
{
    public Guid AssignmentId { get; private set; }
    public Guid? StudentProfileId { get; private set; }
    public Guid? GroupId { get; private set; }
    public DateTime DueDate { get; private set; }
    public DateTime? AvailableFrom { get; private set; }
    public DateTime? AvailableUntil { get; private set; }
    public string? Note { get; private set; }

    private AssignmentOverride() { }

    public AssignmentOverride(Guid assignmentId, Guid? studentProfileId, Guid? groupId, DateTime dueDate, DateTime? availableFrom, DateTime? availableUntil, string? note)
    {
        if ((studentProfileId is null) == (groupId is null)) throw new ArgumentException("An override is for one student or one group.");
        if (availableFrom is { } from && from > dueDate) throw new ArgumentException("Submissions must open before the due date.", nameof(availableFrom));
        if (availableUntil is { } until && until < dueDate) throw new ArgumentException("The late cut-off cannot be before the due date.", nameof(availableUntil));
        AssignmentId = assignmentId;
        StudentProfileId = studentProfileId;
        GroupId = groupId;
        DueDate = dueDate;
        AvailableFrom = availableFrom;
        AvailableUntil = availableUntil;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()[..Math.Min(note.Trim().Length, 300)];
    }
}

/// <summary>Plan M8.3: a way of splitting a course into groups (e.g. "Project teams").</summary>
public class GroupSet : BaseEntity
{
    public Guid CourseOfferingId { get; private set; }
    public string Name { get; private set; } = default!;
    public bool SelfSignup { get; private set; }
    /// <summary>Largest group size (null = no limit).</summary>
    public int? MaxSize { get; private set; }

    private GroupSet() { }

    public GroupSet(Guid courseOfferingId, string name, bool selfSignup, int? maxSize)
    {
        CourseOfferingId = courseOfferingId;
        Update(name, selfSignup, maxSize);
    }

    public void Update(string name, bool selfSignup, int? maxSize)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 150) throw new ArgumentException("Enter a name (up to 150 characters).", nameof(name));
        if (maxSize is < 1 or > 500) throw new ArgumentException("A group holds 1 to 500 students.", nameof(maxSize));
        Name = name.Trim();
        SelfSignup = selfSignup;
        MaxSize = maxSize;
        Touch();
    }
}

public class StudentGroup : BaseEntity
{
    public Guid GroupSetId { get; private set; }
    public string Name { get; private set; } = default!;

    private StudentGroup() { }

    public StudentGroup(Guid groupSetId, string name)
    {
        GroupSetId = groupSetId;
        Rename(name);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 150) throw new ArgumentException("Enter a group name (up to 150 characters).", nameof(name));
        Name = name.Trim();
        Touch();
    }
}

/// <summary>A student in a group. A student is in at most one group of a set.</summary>
public class StudentGroupMember : BaseEntity
{
    public Guid GroupId { get; private set; }
    public Guid GroupSetId { get; private set; }
    public Guid StudentProfileId { get; private set; }

    private StudentGroupMember() { }

    public StudentGroupMember(Guid groupId, Guid groupSetId, Guid studentProfileId)
    {
        GroupId = groupId;
        GroupSetId = groupSetId;
        StudentProfileId = studentProfileId;
    }
}

public enum PeerReviewStatus
{
    Assigned = 0,
    Completed = 1
}

/// <summary>Plan M8.4: one student reviewing another student's (or group's) submission.</summary>
public class PeerReview : BaseEntity
{
    public Guid AssignmentId { get; private set; }
    public Guid SubmissionId { get; private set; }
    public Guid ReviewerStudentProfileId { get; private set; }
    public PeerReviewStatus Status { get; private set; }
    /// <summary>Points on the assignment's scale (from the rubric when there is one).</summary>
    public decimal? Score { get; private set; }
    public string? Comment { get; private set; }
    /// <summary>Chosen rubric levels as JSON [{criterionId, levelId}].</summary>
    public string? RubricJson { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private PeerReview() { }

    public PeerReview(Guid assignmentId, Guid submissionId, Guid reviewerStudentProfileId)
    {
        AssignmentId = assignmentId;
        SubmissionId = submissionId;
        ReviewerStudentProfileId = reviewerStudentProfileId;
        Status = PeerReviewStatus.Assigned;
    }

    public void Complete(decimal? score, string? comment, string? rubricJson, DateTime now)
    {
        if (score is < 0) throw new ArgumentException("The score cannot be negative.", nameof(score));
        if (string.IsNullOrWhiteSpace(comment) && score is null && rubricJson is null) throw new ArgumentException("Add a comment, a score or rubric choices.");
        if (comment is { Length: > 4000 }) throw new ArgumentException("Comments are limited to 4000 characters.", nameof(comment));
        Score = score;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        RubricJson = rubricJson;
        Status = PeerReviewStatus.Completed;
        CompletedAt ??= now;
        Touch();
    }
}
