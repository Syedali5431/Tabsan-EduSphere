using Tabsan.EduSphere.Domain.Community;

namespace Tabsan.EduSphere.Application.Community;

/// <summary>A child as their parent's audience: news, events and forms reach a parent through their children's classes.</summary>
public sealed record CommunityChild(Guid StudentProfileId, Guid UserId, string Name, IReadOnlySet<Guid> Campuses, IReadOnlySet<Guid> Semesters,
    IReadOnlySet<Guid> Offerings);

/// <summary>
/// The person asking, with where they belong: the campuses, year levels and classes they study in (students), their
/// children's (parents) or teach (teachers). SuperAdmins see and manage every school; admins their school (or only some of
/// its campuses: <see cref="AdminCampuses"/>).
/// </summary>
/// <param name="TenantId">The person's school; null for SuperAdmins (every school).</param>
/// <param name="AdminCampuses">The campuses an admin looks after (from their departments, else their own campus); null = the whole school.</param>
public sealed record CommunityMember(
    Guid UserId,
    string Role,
    string Name,
    Guid? TenantId,
    IReadOnlySet<Guid>? AdminCampuses,
    IReadOnlySet<Guid> Campuses,
    IReadOnlySet<Guid> Semesters,
    IReadOnlySet<Guid> Offerings,
    IReadOnlyList<CommunityChild> Children,
    IReadOnlySet<Guid> TeachingOfferings)
{
    public bool IsSuperAdmin => Is("SuperAdmin");
    public bool IsAdmin => Is("Admin");
    public bool IsManager => IsSuperAdmin || IsAdmin;
    public bool IsFaculty => Is("Faculty");
    public bool IsStudent => Is("Student");
    public bool IsParent => Is("Parent");

    /// <summary>Which audience group the person is in.</summary>
    public AudienceRoles Group => IsStudent ? AudienceRoles.Students : IsParent ? AudienceRoles.Parents : AudienceRoles.Staff;

    private bool Is(string role) => string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Who an item is for (as sent by the editor). The school is the author's.</summary>
/// <param name="Roles">"Students", "Parents", "Staff" (any combination; none = everyone).</param>
public sealed record AudienceRequest(Guid? CampusId, Guid? SemesterId, Guid? CourseOfferingId, IReadOnlyList<string>? Roles);

/// <summary>An item's audience in words, for lists and the editor.</summary>
public sealed record AudienceDto(Guid? CampusId, string? CampusName, Guid? SemesterId, string? SemesterName, Guid? CourseOfferingId, string? CourseName,
    IReadOnlyList<string> Roles);

/// <summary>What an author may choose from.</summary>
public sealed record AudienceOptionsDto(bool WholeSchool, IReadOnlyList<OptionDto> Campuses, IReadOnlyList<OptionDto> Semesters, IReadOnlyList<OptionDto> Classes);

public sealed record OptionDto(Guid Id, string Name, Guid? CampusId = null);

/// <summary>The names behind an audience's ids.</summary>
public sealed record AudienceNames(IReadOnlyDictionary<Guid, string> Campuses, IReadOnlyDictionary<Guid, string> Semesters, IReadOnlyDictionary<Guid, string> Classes);

/// <summary>Where a chosen campus, year level or class belongs (to check an author may use it).</summary>
public sealed record AudienceTarget(Guid Id, Guid? TenantId, Guid? CampusId, Guid? SemesterId);

/// <summary>The people an item reaches, for notifications and for following up a form.</summary>
public sealed class AudiencePeople
{
    public List<AudienceStudent> Students { get; } = [];
    /// <summary>Parents with the child (in <see cref="Students"/>) that brings them in.</summary>
    public List<(Guid ParentUserId, string Name, Guid StudentProfileId)> Parents { get; } = [];
    public List<(Guid UserId, string Name, string Role)> Staff { get; } = [];

    public IReadOnlyList<Guid> UserIds(AudienceRoles roles)
        => (roles.HasFlag(AudienceRoles.Students) ? Students.Select(s => s.UserId) : [])
            .Concat(roles.HasFlag(AudienceRoles.Parents) ? Parents.Select(p => p.ParentUserId) : [])
            .Concat(roles.HasFlag(AudienceRoles.Staff) ? Staff.Select(s => s.UserId) : [])
            .Distinct().ToList();
}

public sealed record AudienceStudent(Guid StudentProfileId, Guid UserId, string Name, string? ClassName);

public interface ICommunityStore
{
    /// <summary>Null when the user does not exist.</summary>
    Task<CommunityMember?> MemberAsync(Guid userId, string role, CancellationToken ct = default);
    Task<AudienceOptionsDto> AudienceOptionsAsync(CommunityMember member, CancellationToken ct = default);
    Task<AudienceNames> AudienceNamesAsync(IEnumerable<CommunityItem> items, CancellationToken ct = default);
    Task<AudienceTarget?> CampusAsync(Guid id, CancellationToken ct = default);
    Task<AudienceTarget?> SemesterAsync(Guid id, CancellationToken ct = default);
    Task<AudienceTarget?> OfferingAsync(Guid id, CancellationToken ct = default);
    Task<AudiencePeople> PeopleAsync(CommunityItem item, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);

    /// <summary>Items of a school (and every-school items); null tenant = every item (SuperAdmins).</summary>
    Task<IReadOnlyList<NewsPost>> NewsAsync(Guid? tenantId, CancellationToken ct = default);
    Task<NewsPost?> NewsPostAsync(Guid id, CancellationToken ct = default);
    void Add(NewsPost post);
    void Remove(NewsPost post);
    Task<IReadOnlySet<Guid>> ReadPostsAsync(Guid userId, IReadOnlyCollection<Guid> postIds, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, int>> SeenCountsAsync(IReadOnlyCollection<Guid> postIds, CancellationToken ct = default);
    void Add(NewsRead read);
    /// <summary>Published posts whose time has come and whose audience has not been told yet.</summary>
    Task<IReadOnlyList<NewsPost>> NewsToAnnounceAsync(DateTime now, CancellationToken ct = default);

    Task<IReadOnlyList<SchoolEvent>> EventsAsync(Guid? tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<SchoolEvent?> EventAsync(Guid id, CancellationToken ct = default);
    void Add(SchoolEvent schoolEvent);
    /// <summary>The (not cancelled) event a form belongs to.</summary>
    Task<Guid?> EventOfFormAsync(Guid formId, CancellationToken ct = default);
    Task<IReadOnlyList<EventReply>> RepliesAsync(IReadOnlyCollection<Guid> eventIds, CancellationToken ct = default);
    void Add(EventReply reply);

    Task<IReadOnlyList<SchoolForm>> FormsAsync(Guid? tenantId, CancellationToken ct = default);
    Task<SchoolForm?> FormAsync(Guid id, CancellationToken ct = default);
    void Add(SchoolForm form);
    void Remove(SchoolForm form);
    Task<IReadOnlyList<FormField>> FieldsAsync(Guid formId, CancellationToken ct = default);
    void ReplaceFields(IReadOnlyList<FormField> old, IReadOnlyList<FormField> fields);
    Task<IReadOnlyList<FormResponse>> ResponsesAsync(IReadOnlyCollection<Guid> formIds, CancellationToken ct = default);
    void Add(FormResponse response);

    Task SaveChangesAsync(CancellationToken ct = default);
}

public sealed record CommunityResult<T>(bool Ok, int Status, T? Value, string? Error)
{
    public static CommunityResult<T> Success(T value, int status = 200) => new(true, status, value, null);
    public static CommunityResult<T> Invalid(string error) => new(false, 400, default, error);
    public static CommunityResult<T> NotFound(string error) => new(false, 404, default, error);
    public static CommunityResult<T> Forbidden(string error) => new(false, 403, default, error);
    public static CommunityResult<T> Conflict(string error) => new(false, 409, default, error);
}

// ── News ─────────────────────────────────────────────────────────────────────

public sealed record NewsSummaryDto(Guid Id, string Title, string? Summary, string? ImageUrl, DateTime PublishAt, bool IsPinned, bool IsNew,
    string AuthorName, AudienceDto Audience);

public sealed record NewsPostDto(Guid Id, string Title, string? Summary, string Body, string? ImageUrl, DateTime PublishAt, DateTime? ExpiresAt,
    bool IsPinned, string Status, string AuthorName, AudienceDto Audience, bool CanManage, int? SeenBy, int? AudienceSize);

public sealed record NewsManageDto(Guid Id, string Title, string Status, DateTime PublishAt, DateTime? ExpiresAt, bool IsPinned, bool Live,
    string AuthorName, AudienceDto Audience, int SeenBy);

/// <param name="PublishAt">When it appears (null = now).</param>
/// <param name="Publish">False keeps it as a draft.</param>
public sealed record SaveNewsRequest(string? Title, string? Summary, string? Body, string? ImageUrl, AudienceRequest? Audience, DateTime? PublishAt,
    DateTime? ExpiresAt, bool Pinned, bool Publish);

// ── Events ───────────────────────────────────────────────────────────────────

public sealed record EventReplyCounts(int Going, int NotGoing, int Maybe, int Places);

public sealed record EventDto(Guid Id, string Title, string? Description, string? Location, DateTime StartsAt, DateTime EndsAt, bool AllDay,
    string Category, string Status, string? CancelReason, bool RepliesOpen, DateTime? RepliesCloseAt, int? Capacity, int MaxGuests, Guid? FormId,
    string? MyAnswer, int MyGuests, string? MyNote, EventReplyCounts? Counts, bool CanReply, bool CanManage, AudienceDto Audience, string AuthorName);

public sealed record SaveEventRequest(string? Title, string? Description, string? Location, string? Category, DateTime StartsAt, DateTime EndsAt, bool AllDay,
    bool RepliesOpen, DateTime? ReplyBy, int? Capacity, int MaxGuests, Guid? FormId, AudienceRequest? Audience, bool Publish);

public sealed record EventReplyRequest(string? Answer, int Guests, string? Note);

public sealed record EventReplyRowDto(Guid UserId, string Name, string Role, string Answer, int Guests, string? Note, DateTime AnsweredAt);

public sealed record EventRepliesDto(EventDto Event, IReadOnlyList<EventReplyRowDto> Replies, int AudienceSize);

// ── Forms ────────────────────────────────────────────────────────────────────

public sealed record FormFieldDto(Guid Id, string Type, string Label, string? Help, bool Required, IReadOnlyList<string> Options);

public sealed record FormFieldRequest(string? Type, string? Label, string? Help, bool Required, IReadOnlyList<string>? Options);

public sealed record SaveFormRequest(string? Title, string? Description, DateTime? DueAt, AudienceRequest? Audience, IReadOnlyList<FormFieldRequest>? Fields,
    bool Open);

/// <summary>A form waiting for the person (for a parent: one row per child it concerns).</summary>
public sealed record FormToFillDto(Guid FormId, string Title, DateTime? DueAt, Guid? StudentProfileId, string? StudentName, bool Answered,
    DateTime? AnsweredAt, string? AnsweredBy, bool Open);

public sealed record FormDto(Guid Id, string Title, string? Description, DateTime? DueAt, string Status, bool TakesAnswers, AudienceDto Audience,
    IReadOnlyList<FormFieldDto> Fields, Guid? StudentProfileId, string? StudentName, IReadOnlyDictionary<Guid, string> Answers, DateTime? AnsweredAt,
    string? AnsweredBy, bool CanManage, Guid? EventId);

public sealed record SubmitFormRequest(Guid? StudentProfileId, IReadOnlyDictionary<Guid, string?>? Answers);

public sealed record FormManageDto(Guid Id, string Title, string Status, DateTime? DueAt, AudienceDto Audience, int Answered, string AuthorName);

/// <summary>One person (or child, for parent forms) the form is waiting for, and whether it has been answered.</summary>
/// <param name="Kind">"Parent" (a child whose parent should answer), "Student" or "Staff".</param>
public sealed record FormTrackingRowDto(string Kind, string Name, string? ClassName, string? Parents, bool Answered, string? AnsweredBy, DateTime? AnsweredAt,
    bool NoParentAccount);

public sealed record FieldSummaryDto(Guid FieldId, string Label, string Type, IReadOnlyDictionary<string, int> Counts);

public sealed record FormTrackingDto(FormDto Form, int Expected, int Answered, IReadOnlyList<FormTrackingRowDto> Rows, IReadOnlyList<FieldSummaryDto> Summary,
    DateTime? RemindedAt);
