using System.Globalization;
using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Pastoral;

// Pastoral care and wellbeing: notes about a student (behaviour, wellbeing, learning support, contact with the family),
// support plans that tell staff what a student needs, students' own wellbeing check-ins, and the wellbeing team that
// follows them up. Confidential notes are seen only by their author and the members of the wellbeing team allowed to
// see confidential notes.

public enum PastoralKind
{
    Behaviour = 0,
    Wellbeing = 1,
    LearningSupport = 2,
    FamilyContact = 3,
    General = 4
}

public enum PastoralTone
{
    Neutral = 0,
    /// <summary>Praise: good work, effort, kindness, leadership.</summary>
    Positive = 1,
    /// <summary>Something to watch or act on.</summary>
    Concern = 2
}

public enum PastoralSeverity
{
    Low = 0,
    Medium = 1,
    /// <summary>The wellbeing team is told at once.</summary>
    High = 2
}

public enum PastoralVisibility
{
    /// <summary>The student's teachers, the wellbeing team and the school's admins.</summary>
    Staff = 0,
    /// <summary>Only the author and the members of the wellbeing team who see confidential notes.</summary>
    Confidential = 1
}

public enum FollowUpStatus
{
    None = 0,
    Open = 1,
    Done = 2
}

internal static class PastoralText
{
    public static string? Clean(string? text, int max, string tooLong)
    {
        var t = string.IsNullOrWhiteSpace(text) ? null : text.Trim().Replace("\r\n", "\n");
        if (t is { Length: var n } && n > max) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, tooLong, max));
        return t;
    }

    public static string Required(string? text, int max, string missing, string tooLong)
        => Clean(text, max, tooLong) ?? throw new ArgumentException(missing);
}

/// <summary>A note about one student: what happened, what was done, who follows it up and who may read it.</summary>
public class PastoralRecord : BaseEntity
{
    public const int MaxSummaryLength = 200;
    public const int MaxDetailsLength = 4000;
    public const int MaxActionLength = 1000;
    public const int MaxCategoryLength = 80;
    public const int MaxReasonLength = 500;

    public Guid? TenantId { get; private set; }
    /// <summary>The student's campus when the note was written (for campus admins).</summary>
    public Guid? CampusId { get; private set; }
    public Guid StudentProfileId { get; private set; }
    /// <summary>The class it happened in (optional).</summary>
    public Guid? CourseOfferingId { get; private set; }
    public Guid AuthorUserId { get; private set; }

    public PastoralKind Kind { get; private set; }
    public PastoralTone Tone { get; private set; }
    public PastoralSeverity Severity { get; private set; }
    /// <summary>The school's own word for it ("Excellent effort", "Late to class").</summary>
    public string? Category { get; private set; }
    public string Summary { get; private set; } = default!;
    public string? Details { get; private set; }
    public string? ActionTaken { get; private set; }
    public DateTime OccurredAt { get; private set; }

    public PastoralVisibility Visibility { get; private set; }
    public bool SharedWithStudent { get; private set; }
    public bool SharedWithParents { get; private set; }
    /// <summary>When the student and parents were told about it (once).</summary>
    public DateTime? SharedNotifiedAt { get; private set; }

    public FollowUpStatus FollowUp { get; private set; }
    public Guid? FollowUpUserId { get; private set; }
    public DateTime? FollowUpBy { get; private set; }
    public DateTime? FollowUpRemindedAt { get; private set; }
    public DateTime? FollowUpDoneAt { get; private set; }
    public Guid? FollowUpDoneByUserId { get; private set; }

    /// <summary>Notes are never deleted: a note written in error is removed with a reason and kept for the wellbeing team.</summary>
    public DateTime? RemovedAt { get; private set; }
    public Guid? RemovedByUserId { get; private set; }
    public string? RemovedReason { get; private set; }

    public bool IsRemoved => RemovedAt is not null;

    private PastoralRecord() { }

    public PastoralRecord(Guid? tenantId, Guid? campusId, Guid studentProfileId, Guid? courseOfferingId, Guid authorUserId)
    {
        TenantId = tenantId;
        CampusId = campusId;
        StudentProfileId = studentProfileId;
        CourseOfferingId = courseOfferingId;
        AuthorUserId = authorUserId;
    }

    public void Describe(PastoralKind kind, PastoralTone tone, PastoralSeverity severity, string? category, string? summary, string? details,
        string? actionTaken, DateTime occurredAt, DateTime now)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentException("Choose what kind of note it is.");
        if (!Enum.IsDefined(tone)) throw new ArgumentException("Choose whether it is praise, a concern or neither.");
        if (!Enum.IsDefined(severity)) throw new ArgumentException("Choose how serious it is.");
        if (occurredAt > now.AddMinutes(5)) throw new ArgumentException("A note is about something that has already happened.");
        Kind = kind;
        Tone = tone;
        Severity = tone == PastoralTone.Concern ? severity : PastoralSeverity.Low;
        Category = PastoralText.Clean(category, MaxCategoryLength, "A category has at most {0:N0} characters.");
        Summary = PastoralText.Required(summary, MaxSummaryLength, "Write what happened in a few words.", "The summary has at most {0:N0} characters.");
        Details = PastoralText.Clean(details, MaxDetailsLength, "The details have at most {0:N0} characters.");
        ActionTaken = PastoralText.Clean(actionTaken, MaxActionLength, "What was done has at most {0:N0} characters.");
        OccurredAt = occurredAt;
        Touch();
    }

    /// <summary>Confidential notes are never shared with the student or the family.</summary>
    public void SetSharing(PastoralVisibility visibility, bool withStudent, bool withParents)
    {
        if (!Enum.IsDefined(visibility)) throw new ArgumentException("Choose who may read the note.");
        if (visibility == PastoralVisibility.Confidential && (withStudent || withParents))
            throw new ArgumentException("A confidential note cannot be shared with the student or the family.");
        Visibility = visibility;
        SharedWithStudent = withStudent;
        SharedWithParents = withParents;
        Touch();
    }

    public bool IsShared => !IsRemoved && Visibility == PastoralVisibility.Staff && (SharedWithStudent || SharedWithParents);

    public void MarkSharedNotified(DateTime now) => SharedNotifiedAt = now;

    /// <summary>Asks someone to follow the note up (by a date); null takes the request away unless it is already done.</summary>
    public void SetFollowUp(Guid? userId, DateTime? by)
    {
        if (userId is null)
        {
            if (FollowUp == FollowUpStatus.Open) FollowUp = FollowUpStatus.None;
            FollowUpUserId = null;
            FollowUpBy = null;
        }
        else
        {
            if (userId != FollowUpUserId || by != FollowUpBy) FollowUpRemindedAt = null;
            FollowUpUserId = userId;
            FollowUpBy = by;
            if (FollowUp == FollowUpStatus.None) FollowUp = FollowUpStatus.Open;
        }
        Touch();
    }

    public void CompleteFollowUp(Guid byUserId, DateTime now)
    {
        if (FollowUp != FollowUpStatus.Open) throw new InvalidOperationException("There is no follow-up waiting on this note.");
        FollowUp = FollowUpStatus.Done;
        FollowUpDoneAt = now;
        FollowUpDoneByUserId = byUserId;
        Touch();
    }

    public void ReopenFollowUp()
    {
        if (FollowUp != FollowUpStatus.Done) throw new InvalidOperationException("The follow-up is not finished.");
        FollowUp = FollowUpStatus.Open;
        FollowUpDoneAt = null;
        FollowUpDoneByUserId = null;
        Touch();
    }

    public void MarkFollowUpReminded(DateTime now) => FollowUpRemindedAt = now;

    public void Remove(Guid byUserId, string? reason, DateTime now)
    {
        if (IsRemoved) throw new InvalidOperationException("The note is already removed.");
        RemovedReason = PastoralText.Required(reason, MaxReasonLength, "Say why the note is removed.", "The reason has at most {0:N0} characters.");
        RemovedAt = now;
        RemovedByUserId = byUserId;
        if (FollowUp == FollowUpStatus.Open) FollowUp = FollowUpStatus.None;
        Touch();
    }
}

/// <summary>A follow-up note on a record (staff only).</summary>
public class PastoralNote : BaseEntity
{
    public const int MaxTextLength = 2000;

    public Guid RecordId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string Text { get; private set; } = default!;

    private PastoralNote() { }

    public PastoralNote(Guid recordId, Guid authorUserId, string? text, DateTime now)
    {
        RecordId = recordId;
        AuthorUserId = authorUserId;
        Text = PastoralText.Required(text, MaxTextLength, "Write the note.", "A follow-up note has at most {0:N0} characters.");
        CreatedAt = now;
    }
}

public enum SupportPlanKind
{
    /// <summary>Learning adjustments: extra time, seating, reading support…</summary>
    LearningAdjustments = 0,
    Behaviour = 1,
    Wellbeing = 2,
    Health = 3
}

public enum SupportPlanStatus
{
    Active = 0,
    Ended = 1
}

/// <summary>What a student needs and what staff should do, with a date to review it.</summary>
public class SupportPlan : BaseEntity
{
    public const int MaxTitleLength = 200;
    public const int MaxTextLength = 4000;

    public Guid? TenantId { get; private set; }
    public Guid? CampusId { get; private set; }
    public Guid StudentProfileId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public SupportPlanKind Kind { get; private set; }
    public string Title { get; private set; } = default!;
    /// <summary>Background: what the student needs and why.</summary>
    public string? Needs { get; private set; }
    /// <summary>What staff should do (shown to the student's teachers).</summary>
    public string Strategies { get; private set; } = default!;
    public DateTime StartsOn { get; private set; }
    public DateTime? ReviewOn { get; private set; }
    public DateTime? ReviewRemindedAt { get; private set; }
    public SupportPlanStatus Status { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public PastoralVisibility Visibility { get; private set; }
    public bool SharedWithStudent { get; private set; }
    public bool SharedWithParents { get; private set; }
    public DateTime? SharedNotifiedAt { get; private set; }

    private SupportPlan() { }

    public SupportPlan(Guid? tenantId, Guid? campusId, Guid studentProfileId, Guid ownerUserId)
    {
        TenantId = tenantId;
        CampusId = campusId;
        StudentProfileId = studentProfileId;
        OwnerUserId = ownerUserId;
    }

    public void Edit(SupportPlanKind kind, string? title, string? needs, string? strategies, DateTime startsOn, DateTime? reviewOn)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentException("Choose what kind of plan it is.");
        var start = startsOn.Date;
        var review = reviewOn?.Date;
        if (review is { } r && r < start) throw new ArgumentException("The review date is after the start date.");
        if (review != ReviewOn) ReviewRemindedAt = null;
        Kind = kind;
        Title = PastoralText.Required(title, MaxTitleLength, "Give the plan a title.", "A title has at most {0:N0} characters.");
        Needs = PastoralText.Clean(needs, MaxTextLength, "The background has at most {0:N0} characters.");
        Strategies = PastoralText.Required(strategies, MaxTextLength, "Write what staff should do.", "What staff should do has at most {0:N0} characters.");
        StartsOn = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        ReviewOn = review is { } d ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : null;
        Touch();
    }

    public void SetSharing(PastoralVisibility visibility, bool withStudent, bool withParents)
    {
        if (!Enum.IsDefined(visibility)) throw new ArgumentException("Choose who may read the plan.");
        if (visibility == PastoralVisibility.Confidential && (withStudent || withParents))
            throw new ArgumentException("A confidential plan cannot be shared with the student or the family.");
        Visibility = visibility;
        SharedWithStudent = withStudent;
        SharedWithParents = withParents;
        Touch();
    }

    public bool IsShared => Status == SupportPlanStatus.Active && Visibility == PastoralVisibility.Staff && (SharedWithStudent || SharedWithParents);

    public void MarkSharedNotified(DateTime now) => SharedNotifiedAt = now;

    public void MarkReviewReminded(DateTime now) => ReviewRemindedAt = now;

    public void End(DateTime now)
    {
        if (Status == SupportPlanStatus.Ended) throw new InvalidOperationException("The plan has already ended.");
        Status = SupportPlanStatus.Ended;
        EndedAt = now;
        Touch();
    }

    public void Restart()
    {
        if (Status == SupportPlanStatus.Active) throw new InvalidOperationException("The plan is still running.");
        Status = SupportPlanStatus.Active;
        EndedAt = null;
        Touch();
    }
}

/// <summary>A student's own answer to "How are you feeling?" (1 = very low … 5 = great), seen by the wellbeing team.</summary>
public class WellbeingCheckIn : BaseEntity
{
    public const int MaxNoteLength = 500;
    /// <summary>A second check-in within this time changes the first one.</summary>
    public static readonly TimeSpan SameCheckIn = TimeSpan.FromHours(6);

    public Guid? TenantId { get; private set; }
    public Guid? CampusId { get; private set; }
    public Guid StudentProfileId { get; private set; }
    public Guid StudentUserId { get; private set; }
    public int Mood { get; private set; }
    public string? Note { get; private set; }
    public bool WantsToTalk { get; private set; }
    public DateTime CheckedInAt { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }
    public Guid? AcknowledgedByUserId { get; private set; }
    public string? AcknowledgeNote { get; private set; }
    public DateTime? AlertedAt { get; private set; }

    private WellbeingCheckIn() { }

    public WellbeingCheckIn(Guid? tenantId, Guid? campusId, Guid studentProfileId, Guid studentUserId)
    {
        TenantId = tenantId;
        CampusId = campusId;
        StudentProfileId = studentProfileId;
        StudentUserId = studentUserId;
    }

    /// <summary>Low moods (1–2) and asking to talk wait for someone on the wellbeing team.</summary>
    public bool NeedsAttention => AcknowledgedAt is null && (WantsToTalk || Mood <= 2);

    /// <summary>Very low (1) or asking to talk: the wellbeing team is told at once.</summary>
    public bool Urgent => WantsToTalk || Mood <= 1;

    public void Answer(int mood, string? note, bool wantsToTalk, DateTime now)
    {
        if (mood is < 1 or > 5) throw new ArgumentException("Choose how you are feeling, from 1 to 5.");
        var wasUrgent = AlertedAt is not null && Urgent;
        Mood = mood;
        Note = PastoralText.Clean(note, MaxNoteLength, "A note has at most {0:N0} characters.");
        WantsToTalk = wantsToTalk;
        CheckedInAt = now;
        // A changed answer that needs someone again waits for them again.
        if (NeedsAttentionIgnoringAcknowledgement())
        {
            AcknowledgedAt = null;
            AcknowledgedByUserId = null;
            AcknowledgeNote = null;
        }
        if (!wasUrgent || !Urgent) AlertedAt = null;
        Touch();
    }

    private bool NeedsAttentionIgnoringAcknowledgement() => WantsToTalk || Mood <= 2;

    public void MarkAlerted(DateTime now) => AlertedAt = now;

    public void Acknowledge(Guid byUserId, string? note, DateTime now)
    {
        AcknowledgeNote = PastoralText.Clean(note, MaxNoteLength, "A note has at most {0:N0} characters.");
        AcknowledgedAt = now;
        AcknowledgedByUserId = byUserId;
        Touch();
    }
}

/// <summary>A member of a school's wellbeing team (a counsellor, a head of year, a learning-support teacher), for the whole
/// school, a campus and/or a year level.</summary>
public class PastoralTeamMember : BaseEntity
{
    public const int MaxTitleLength = 100;

    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? CampusId { get; private set; }
    /// <summary>A year level ("Class 9"); null = every year level.</summary>
    public Guid? SemesterId { get; private set; }
    public string Title { get; private set; } = default!;
    public bool SeesConfidential { get; private set; }
    public Guid AddedByUserId { get; private set; }

    private PastoralTeamMember() { }

    public PastoralTeamMember(Guid tenantId, Guid userId, Guid? campusId, Guid? semesterId, string? title, bool seesConfidential, Guid addedByUserId)
    {
        TenantId = tenantId;
        UserId = userId;
        CampusId = campusId;
        SemesterId = semesterId;
        Title = PastoralText.Required(title, MaxTitleLength, "Give their role, for example \"School counsellor\".", "A role has at most {0:N0} characters.");
        SeesConfidential = seesConfidential;
        AddedByUserId = addedByUserId;
    }
}

/// <summary>A school's own words for notes ("Excellent effort", "Late to class"), offered when a note is written.</summary>
public class PastoralCategory : BaseEntity
{
    public Guid? TenantId { get; private set; }
    public PastoralKind Kind { get; private set; }
    public PastoralTone Tone { get; private set; }
    public string Name { get; private set; } = default!;
    public int Position { get; private set; }
    public bool IsActive { get; private set; } = true;

    private PastoralCategory() { }

    public PastoralCategory(Guid? tenantId, PastoralKind kind, PastoralTone tone, string? name, int position)
    {
        TenantId = tenantId;
        Position = position;
        Edit(kind, tone, name, true);
    }

    public void Edit(PastoralKind kind, PastoralTone tone, string? name, bool isActive)
    {
        if (!Enum.IsDefined(kind) || !Enum.IsDefined(tone)) throw new ArgumentException("Choose the kind of note and whether it is praise or a concern.");
        Kind = kind;
        Tone = tone;
        Name = PastoralText.Required(name, PastoralRecord.MaxCategoryLength, "Give the category a name.", "A category has at most {0:N0} characters.");
        IsActive = isActive;
        Touch();
    }

    /// <summary>The categories a school starts with.</summary>
    public static IReadOnlyList<(PastoralKind Kind, PastoralTone Tone, string Name)> Defaults { get; } =
    [
        (PastoralKind.Behaviour, PastoralTone.Positive, "Excellent effort"),
        (PastoralKind.Behaviour, PastoralTone.Positive, "Outstanding work"),
        (PastoralKind.Behaviour, PastoralTone.Positive, "Helping others"),
        (PastoralKind.Behaviour, PastoralTone.Positive, "Leadership"),
        (PastoralKind.Behaviour, PastoralTone.Concern, "Disrupting the class"),
        (PastoralKind.Behaviour, PastoralTone.Concern, "Late to class"),
        (PastoralKind.Behaviour, PastoralTone.Concern, "Homework not done"),
        (PastoralKind.Behaviour, PastoralTone.Concern, "Uniform"),
        (PastoralKind.Behaviour, PastoralTone.Concern, "Unkind to others"),
        (PastoralKind.Behaviour, PastoralTone.Concern, "Bullying"),
        (PastoralKind.Wellbeing, PastoralTone.Concern, "Seems upset or withdrawn"),
        (PastoralKind.Wellbeing, PastoralTone.Concern, "Friendship problems"),
        (PastoralKind.Wellbeing, PastoralTone.Concern, "Family circumstances"),
        (PastoralKind.Wellbeing, PastoralTone.Neutral, "Health"),
        (PastoralKind.LearningSupport, PastoralTone.Neutral, "Needs extra help"),
        (PastoralKind.LearningSupport, PastoralTone.Neutral, "Adjustment agreed"),
        (PastoralKind.FamilyContact, PastoralTone.Neutral, "Phone call"),
        (PastoralKind.FamilyContact, PastoralTone.Neutral, "Meeting"),
        (PastoralKind.FamilyContact, PastoralTone.Neutral, "Message")
    ];
}

/// <summary>A school's pastoral settings: whether students check in, and when a run of concerns alerts the team.</summary>
public class PastoralSettings : BaseEntity
{
    public const int DefaultConcernCount = 3;
    public const int DefaultConcernDays = 14;

    public Guid TenantId { get; private set; }
    public bool CheckInsEnabled { get; private set; }
    /// <summary>This many behaviour or wellbeing concerns about a student …</summary>
    public int ConcernAlertCount { get; private set; } = DefaultConcernCount;
    /// <summary>… within this many days tell the wellbeing team.</summary>
    public int ConcernAlertDays { get; private set; } = DefaultConcernDays;

    private PastoralSettings() { }

    public PastoralSettings(Guid tenantId) => TenantId = tenantId;

    public void Set(bool checkInsEnabled, int concernAlertCount, int concernAlertDays)
    {
        if (concernAlertCount is < 2 or > 20) throw new ArgumentException("Choose between 2 and 20 concerns.");
        if (concernAlertDays is < 1 or > 90) throw new ArgumentException("Choose between 1 and 90 days.");
        CheckInsEnabled = checkInsEnabled;
        ConcernAlertCount = concernAlertCount;
        ConcernAlertDays = concernAlertDays;
        Touch();
    }
}
