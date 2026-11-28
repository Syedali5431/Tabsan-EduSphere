using System.Text.Json;
using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Community;

// School community: news, events with replies, and online forms (consent forms, permission slips, surveys), each for an
// audience — the whole school or a campus, a year level or one class, and students, parents and/or staff.

[Flags]
public enum AudienceRoles
{
    None = 0,
    Students = 1,
    Parents = 2,
    Staff = 4,
    Everyone = Students | Parents | Staff
}

/// <summary>Who a news post, event or form is for, and who wrote it. Every narrowing is optional: no campus = the whole
/// school, no year level or class = everyone in the campus or school.</summary>
public abstract class CommunityItem : BaseEntity
{
    public const int MaxTitleLength = 200;

    /// <summary>The school (null = every school of the installation; SuperAdmins only).</summary>
    public Guid? TenantId { get; protected set; }
    public Guid? CampusId { get; protected set; }
    /// <summary>A year level or term (a "semester": in schools "Class 7"); students and parents of its classes.</summary>
    public Guid? SemesterId { get; protected set; }
    /// <summary>One class (course offering): its students, their parents and its teacher.</summary>
    public Guid? CourseOfferingId { get; protected set; }
    public AudienceRoles Roles { get; protected set; } = AudienceRoles.Everyone;
    public Guid AuthorUserId { get; protected set; }
    public string Title { get; protected set; } = default!;

    public void SetAudience(Guid? tenantId, Guid? campusId, Guid? semesterId, Guid? courseOfferingId, AudienceRoles roles)
    {
        if ((roles & AudienceRoles.Everyone) == AudienceRoles.None) throw new ArgumentException("Choose who it is for: students, parents or staff.", nameof(roles));
        TenantId = tenantId;
        CampusId = campusId;
        SemesterId = semesterId;
        CourseOfferingId = courseOfferingId;
        Roles = roles & AudienceRoles.Everyone;
        Touch();
    }

    protected static string CleanTitle(string? title)
    {
        var t = (title ?? "").Trim();
        if (t.Length == 0) throw new ArgumentException("Enter a title.", nameof(title));
        if (t.Length > MaxTitleLength) throw new ArgumentException($"A title has at most {MaxTitleLength} characters.", nameof(title));
        return t;
    }

    /// <param name="tooLong">The message when the text is too long, with {0} for the limit.</param>
    protected static string? CleanText(string? text, int max, string tooLong)
    {
        var t = string.IsNullOrWhiteSpace(text) ? null : text.Trim().Replace("\r\n", "\n");
        if (t is { Length: var n } && n > max) throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture, tooLong, max));
        return t;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// News
// ═════════════════════════════════════════════════════════════════════════════

public enum NewsStatus
{
    Draft = 0,
    /// <summary>Visible from <see cref="NewsPost.PublishAt"/> until <see cref="NewsPost.ExpiresAt"/>.</summary>
    Published = 1,
    /// <summary>Taken down; kept for the record.</summary>
    Withdrawn = 2
}

public class NewsPost : CommunityItem
{
    public const int MaxSummaryLength = 400;
    public const int MaxBodyLength = 20_000;

    /// <summary>One or two sentences for the news list.</summary>
    public string? Summary { get; private set; }
    public string Body { get; private set; } = "";
    /// <summary>A picture for the post (an https link).</summary>
    public string? ImageUrl { get; private set; }
    public DateTime PublishAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IsPinned { get; private set; }
    public NewsStatus Status { get; private set; }
    /// <summary>When the audience was told (once, when it first went live).</summary>
    public DateTime? NotifiedAt { get; private set; }

    private NewsPost() { }

    public NewsPost(Guid authorUserId, string title, string? summary, string? body, string? imageUrl, DateTime now)
    {
        AuthorUserId = authorUserId;
        PublishAt = now;
        Edit(title, summary, body, imageUrl);
    }

    public void Edit(string title, string? summary, string? body, string? imageUrl)
    {
        Title = CleanTitle(title);
        Summary = CleanText(summary, MaxSummaryLength, "A summary has at most {0:N0} characters.");
        Body = CleanText(body, MaxBodyLength, "A post has at most {0:N0} characters.") ?? throw new ArgumentException("Write the post.", nameof(body));
        ImageUrl = CleanUrl(imageUrl);
        Touch();
    }

    public void Schedule(DateTime publishAt, DateTime? expiresAt, bool pinned)
    {
        if (expiresAt is { } e && e <= publishAt) throw new ArgumentException("A post must stay up for a while after it is published.", nameof(expiresAt));
        PublishAt = publishAt;
        ExpiresAt = expiresAt;
        IsPinned = pinned;
        Touch();
    }

    public void Publish() { Status = NewsStatus.Published; Touch(); }

    public void Unpublish() { Status = NewsStatus.Draft; Touch(); }

    public void Withdraw() { Status = NewsStatus.Withdrawn; Touch(); }

    public void MarkNotified(DateTime now) { NotifiedAt ??= now; Touch(); }

    public bool IsLive(DateTime now) => Status == NewsStatus.Published && PublishAt <= now && (ExpiresAt is null || ExpiresAt > now);

    internal static string? CleanUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var u) || u.Scheme != Uri.UriSchemeHttps || url.Trim().Length > 1000)
            throw new ArgumentException("A picture link must start with https://.", nameof(url));
        return u.ToString();
    }
}

/// <summary>Someone opened a post (for the "new" mark and the "seen by" count).</summary>
public class NewsRead : BaseEntity
{
    public Guid PostId { get; private set; }
    public Guid UserId { get; private set; }

    private NewsRead() { }

    public NewsRead(Guid postId, Guid userId, DateTime now)
    {
        PostId = postId;
        UserId = userId;
        CreatedAt = now;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// Events
// ═════════════════════════════════════════════════════════════════════════════

public enum EventCategory
{
    General = 0,
    Sports = 1,
    Excursion = 2,
    Meeting = 3,
    Performance = 4,
    Holiday = 5,
    Exam = 6
}

public enum EventStatus
{
    Draft = 0,
    Published = 1,
    Cancelled = 2
}

public enum RsvpAnswer
{
    Going = 1,
    NotGoing = 2,
    Maybe = 3
}

public class SchoolEvent : CommunityItem
{
    public const int MaxDescriptionLength = 5000;
    public const int MaxLocationLength = 300;
    public const int MaxGuestsLimit = 10;

    public string? Description { get; private set; }
    public string? Location { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }
    public bool AllDay { get; private set; }
    public EventCategory Category { get; private set; }
    public EventStatus Status { get; private set; }
    /// <summary>People in the audience can say whether they are coming.</summary>
    public bool RepliesOpen { get; private set; }
    /// <summary>Replies close at this time (null = when the event starts).</summary>
    public DateTime? ReplyBy { get; private set; }
    /// <summary>Places (people coming, guests included); null = no limit.</summary>
    public int? Capacity { get; private set; }
    /// <summary>Guests each reply may bring (0 = none).</summary>
    public int MaxGuests { get; private set; }
    /// <summary>A form to fill in for the event, such as a consent form for an excursion.</summary>
    public Guid? FormId { get; private set; }
    public string? CancelReason { get; private set; }
    public DateTime? NotifiedAt { get; private set; }

    private SchoolEvent() { }

    public SchoolEvent(Guid authorUserId, string title, DateTime startsAt, DateTime endsAt, bool allDay)
    {
        AuthorUserId = authorUserId;
        Title = CleanTitle(title);
        SetTimes(startsAt, endsAt, allDay);
    }

    public void Edit(string title, string? description, string? location, EventCategory category)
    {
        Title = CleanTitle(title);
        Description = CleanText(description, MaxDescriptionLength, "A description has at most {0:N0} characters.");
        Location = CleanText(location, MaxLocationLength, "A place has at most {0:N0} characters.");
        Category = Enum.IsDefined(category) ? category : throw new ArgumentException("Choose a kind of event.", nameof(category));
        Touch();
    }

    public void SetTimes(DateTime startsAt, DateTime endsAt, bool allDay)
    {
        if (allDay) { startsAt = startsAt.Date; endsAt = endsAt.Date; }
        if (endsAt < startsAt || (!allDay && endsAt == startsAt)) throw new ArgumentException("An event ends after it starts.", nameof(endsAt));
        if ((endsAt - startsAt).TotalDays > 31) throw new ArgumentException("An event lasts at most a month.", nameof(endsAt));
        StartsAt = startsAt;
        EndsAt = endsAt;
        AllDay = allDay;
        Touch();
    }

    public void ConfigureReplies(bool open, DateTime? replyBy, int? capacity, int maxGuests, Guid? formId)
    {
        if (capacity is < 1 or > 100_000) throw new ArgumentException("Places must be between 1 and 100,000.", nameof(capacity));
        if (maxGuests is < 0 or > MaxGuestsLimit) throw new ArgumentException($"Each reply may bring 0 to {MaxGuestsLimit} guests.", nameof(maxGuests));
        if (replyBy is { } by && by > StartsAt) throw new ArgumentException("Replies close before the event starts.", nameof(replyBy));
        RepliesOpen = open;
        ReplyBy = open ? replyBy : null;
        Capacity = open ? capacity : null;
        MaxGuests = open ? maxGuests : 0;
        FormId = formId;
        Touch();
    }

    public void Publish() { if (Status == EventStatus.Cancelled) throw new InvalidOperationException("A cancelled event cannot be published again."); Status = EventStatus.Published; Touch(); }

    public void Cancel(string? reason)
    {
        Status = EventStatus.Cancelled;
        CancelReason = CleanText(reason, 500, "A reason has at most {0:N0} characters.");
        Touch();
    }

    public void MarkNotified(DateTime now) { NotifiedAt ??= now; Touch(); }

    public DateTime RepliesCloseAt => ReplyBy ?? StartsAt;

    public bool TakesReplies(DateTime now) => Status == EventStatus.Published && RepliesOpen && now < RepliesCloseAt;
}

/// <summary>One person's reply to an event (changed as often as they like until replies close).</summary>
public class EventReply : BaseEntity
{
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public RsvpAnswer Answer { get; private set; }
    public int Guests { get; private set; }
    public string? Note { get; private set; }

    private EventReply() { }

    public EventReply(Guid eventId, Guid userId)
    {
        EventId = eventId;
        UserId = userId;
    }

    public void Set(RsvpAnswer answer, int guests, string? note)
    {
        if (!Enum.IsDefined(answer)) throw new ArgumentException("Choose going, not going or maybe.", nameof(answer));
        Answer = answer;
        Guests = answer == RsvpAnswer.NotGoing ? 0 : Math.Max(0, guests);
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()[..Math.Min(note.Trim().Length, 500)];
        Touch();
    }

    /// <summary>Places taken: the person and their guests when going.</summary>
    public int Places => Answer == RsvpAnswer.Going ? 1 + Guests : 0;
}

// ═════════════════════════════════════════════════════════════════════════════
// Forms
// ═════════════════════════════════════════════════════════════════════════════

public enum FormStatus
{
    Draft = 0,
    /// <summary>Taking answers.</summary>
    Open = 1,
    Closed = 2
}

public enum FormFieldType
{
    ShortText = 1,
    LongText = 2,
    /// <summary>One of the options.</summary>
    SingleChoice = 3,
    /// <summary>Any of the options.</summary>
    MultipleChoice = 4,
    /// <summary>Yes or no (a consent question).</summary>
    YesNo = 5,
    Date = 6,
    Number = 7,
    /// <summary>The person types their full name to sign.</summary>
    Signature = 8,
    /// <summary>Text shown on the form; nothing to answer.</summary>
    Information = 9
}

/// <summary>A form to fill in online. Parents answer once for each child it concerns; students and staff once each.</summary>
public class SchoolForm : CommunityItem
{
    public const int MaxDescriptionLength = 5000;
    public const int MaxFields = 60;

    public string? Description { get; private set; }
    public DateTime? DueAt { get; private set; }
    public FormStatus Status { get; private set; }
    public DateTime? OpenedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    /// <summary>When those who had not answered were last reminded.</summary>
    public DateTime? RemindedAt { get; private set; }

    private SchoolForm() { }

    public SchoolForm(Guid authorUserId, string title, string? description, DateTime? dueAt)
    {
        AuthorUserId = authorUserId;
        Edit(title, description, dueAt);
    }

    public void Edit(string title, string? description, DateTime? dueAt)
    {
        Title = CleanTitle(title);
        Description = CleanText(description, MaxDescriptionLength, "Instructions have at most {0:N0} characters.");
        DueAt = dueAt;
        Touch();
    }

    public void Open(DateTime now)
    {
        Status = FormStatus.Open;
        OpenedAt ??= now;
        ClosedAt = null;
        Touch();
    }

    public void Close(DateTime now)
    {
        if (Status == FormStatus.Draft) throw new InvalidOperationException("A draft form is not open.");
        Status = FormStatus.Closed;
        ClosedAt = now;
        Touch();
    }

    public void MarkReminded(DateTime now) { RemindedAt = now; Touch(); }

    /// <summary>Answers are taken while the form is open (and, with a due date, until it passes).</summary>
    public bool TakesAnswers(DateTime now) => Status == FormStatus.Open && (DueAt is null || now <= DueAt);
}

public class FormField : BaseEntity
{
    public const int MaxLabelLength = 300;
    public const int MaxHelpLength = 1000;
    public const int MaxOptions = 30;

    public Guid FormId { get; private set; }
    public int Position { get; private set; }
    public FormFieldType Type { get; private set; }
    public string Label { get; private set; } = default!;
    public string? Help { get; private set; }
    public bool Required { get; private set; }
    /// <summary>The choices of a single or multiple choice field (JSON list).</summary>
    public string? OptionsJson { get; private set; }

    private FormField() { }

    public FormField(Guid formId, int position, FormFieldType type, string label, string? help, bool required, IEnumerable<string>? options)
    {
        if (!Enum.IsDefined(type)) throw new ArgumentException("Choose a kind of question.", nameof(type));
        FormId = formId;
        Position = position;
        Type = type;
        var l = (label ?? "").Trim();
        if (l.Length == 0) throw new ArgumentException("Every question needs its text.", nameof(label));
        if (l.Length > MaxLabelLength) throw new ArgumentException($"A question has at most {MaxLabelLength} characters.", nameof(label));
        Label = l;
        var h = string.IsNullOrWhiteSpace(help) ? null : help.Trim();
        if (h is { Length: > MaxHelpLength }) throw new ArgumentException($"Help text has at most {MaxHelpLength} characters.", nameof(help));
        Help = h;
        Required = type != FormFieldType.Information && required;
        if (type is FormFieldType.SingleChoice or FormFieldType.MultipleChoice)
        {
            var list = (options ?? []).Select(o => (o ?? "").Trim()).Where(o => o.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (list.Count < 2) throw new ArgumentException($"\"{l}\" needs at least two choices.", nameof(options));
            if (list.Count > MaxOptions || list.Any(o => o.Length > 200)) throw new ArgumentException($"\"{l}\" has at most {MaxOptions} choices of up to 200 characters.", nameof(options));
            OptionsJson = JsonSerializer.Serialize(list);
        }
    }

    public IReadOnlyList<string> Options => OptionsJson is null ? [] : JsonSerializer.Deserialize<List<string>>(OptionsJson) ?? [];
}

/// <summary>One filled-in form: by a student or staff member for themself, or by a parent for one child (either parent may
/// answer; the latest answer counts).</summary>
public class FormResponse : BaseEntity
{
    public const int MaxAnswerLength = 4000;

    public Guid FormId { get; private set; }
    public Guid RespondentUserId { get; private set; }
    /// <summary>The child a parent answered for (null for students and staff answering for themselves).</summary>
    public Guid? StudentProfileId { get; private set; }
    /// <summary>Field id → answer (JSON object of strings; several choices are joined with a line break).</summary>
    public string AnswersJson { get; private set; } = "{}";
    public DateTime SubmittedAt { get; private set; }

    private FormResponse() { }

    public FormResponse(Guid formId, Guid respondentUserId, Guid? studentProfileId)
    {
        FormId = formId;
        RespondentUserId = respondentUserId;
        StudentProfileId = studentProfileId;
    }

    public void Submit(Guid respondentUserId, IReadOnlyDictionary<Guid, string> answers, DateTime now)
    {
        RespondentUserId = respondentUserId;
        AnswersJson = JsonSerializer.Serialize(answers.ToDictionary(a => a.Key.ToString(), a => a.Value));
        SubmittedAt = now;
        Touch();
    }

    public IReadOnlyDictionary<Guid, string> Answers
        => (JsonSerializer.Deserialize<Dictionary<string, string>>(AnswersJson) ?? [])
            .Where(a => Guid.TryParse(a.Key, out _)).ToDictionary(a => Guid.Parse(a.Key), a => a.Value);
}

/// <summary>Checks a set of answers against a form's questions, returning the cleaned answers or the first problem.</summary>
public static class FormAnswers
{
    public const string Yes = "Yes";
    public const string No = "No";

    public static (IReadOnlyDictionary<Guid, string>? Answers, string? Error) Check(IReadOnlyList<FormField> fields, IReadOnlyDictionary<Guid, string?> given)
    {
        var clean = new Dictionary<Guid, string>();
        foreach (var f in fields.Where(f => f.Type != FormFieldType.Information).OrderBy(f => f.Position))
        {
            var raw = given.TryGetValue(f.Id, out var v) ? (v ?? "").Trim() : "";
            if (raw.Length == 0)
            {
                if (f.Required) return (null, $"Answer \"{f.Label}\".");
                continue;
            }
            if (raw.Length > FormResponse.MaxAnswerLength) return (null, $"The answer to \"{f.Label}\" is too long.");
            switch (f.Type)
            {
                case FormFieldType.SingleChoice:
                    if (!f.Options.Contains(raw)) return (null, $"Choose one of the answers to \"{f.Label}\".");
                    break;
                case FormFieldType.MultipleChoice:
                    var picked = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToList();
                    if (picked.Any(p => !f.Options.Contains(p))) return (null, $"Choose from the answers to \"{f.Label}\".");
                    if (picked.Count == 0 && f.Required) return (null, $"Answer \"{f.Label}\".");
                    raw = string.Join("\n", f.Options.Where(picked.Contains));
                    break;
                case FormFieldType.YesNo:
                    raw = raw.Equals(Yes, StringComparison.OrdinalIgnoreCase) || raw.Equals("true", StringComparison.OrdinalIgnoreCase) ? Yes
                        : raw.Equals(No, StringComparison.OrdinalIgnoreCase) || raw.Equals("false", StringComparison.OrdinalIgnoreCase) ? No
                        : "";
                    if (raw.Length == 0) return (null, $"Answer \"{f.Label}\" with yes or no.");
                    break;
                case FormFieldType.Date:
                    if (!DateOnly.TryParseExact(raw, "yyyy-MM-dd", out var date)) return (null, $"Enter a date for \"{f.Label}\".");
                    raw = date.ToString("yyyy-MM-dd");
                    break;
                case FormFieldType.Number:
                    if (!decimal.TryParse(raw, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var number))
                        return (null, $"Enter a number for \"{f.Label}\".");
                    raw = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case FormFieldType.ShortText or FormFieldType.Signature when raw.Length > 300:
                    return (null, $"The answer to \"{f.Label}\" is too long.");
            }
            clean[f.Id] = raw;
        }
        return (clean, null);
    }
}
