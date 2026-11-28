using Tabsan.EduSphere.Application.Community;
using Tabsan.EduSphere.Domain.Pastoral;

namespace Tabsan.EduSphere.Application.Pastoral;

/// <summary>A place on a school's wellbeing team: the whole school, a campus and/or a year level.</summary>
public sealed record TeamScope(Guid MemberId, Guid TenantId, Guid? CampusId, Guid? SemesterId, bool SeesConfidential, string Title);

/// <summary>The person asking: who they are in the school (see <see cref="CommunityMember"/>), their own student profile
/// (students) and their places on the wellbeing team.</summary>
public sealed record PastoralViewer(CommunityMember Member, Guid? StudentProfileId, IReadOnlyList<TeamScope> Team)
{
    public Guid UserId => Member.UserId;
    public Guid? TenantId => Member.TenantId;
    public bool IsSuperAdmin => Member.IsSuperAdmin;
    public bool IsAdmin => Member.IsAdmin;
    public bool IsManager => Member.IsManager;
    public bool IsFaculty => Member.IsFaculty;
    public bool IsStudent => Member.IsStudent;
    public bool IsParent => Member.IsParent;
    public bool IsStaff => !IsStudent && !IsParent;
}

/// <summary>The student a note is about: where they study, who teaches them and who their parents are.</summary>
/// <param name="CampusId">Their own campus (else the campus of one of their classes).</param>
/// <param name="ClassName">Their year level ("Class 9").</param>
public sealed record PastoralStudent(
    Guid StudentProfileId,
    Guid UserId,
    string Name,
    string? RegistrationNumber,
    Guid? TenantId,
    Guid? CampusId,
    IReadOnlySet<Guid> Campuses,
    IReadOnlySet<Guid> Semesters,
    IReadOnlySet<Guid> Offerings,
    string? ClassName,
    IReadOnlySet<Guid> TeacherUserIds,
    IReadOnlyList<Guid> ParentUserIds);

/// <summary>What the person asking may do for one student.</summary>
public sealed record PastoralAccess(bool IsSelf, bool IsParent, bool IsTeacher, bool IsTeam, bool SeesConfidential, bool IsAdmin, bool IsSuperAdmin)
{
    public static readonly PastoralAccess None = new(false, false, false, false, false, false, false);

    /// <summary>Staff who work with the student: their teachers, the wellbeing team, admins.</summary>
    public bool IsStaff => IsTeacher || IsTeam || IsAdmin || IsSuperAdmin;
    /// <summary>The wellbeing team and admins: support plans, check-ins, and other people's notes.</summary>
    public bool Manages => IsTeam || IsAdmin || IsSuperAdmin;
    public bool Any => IsStaff || IsSelf || IsParent;
}

/// <summary>A member of the wellbeing team with their name and place, for alerts and the team page.</summary>
public sealed record TeamMemberRow(PastoralTeamMember Member, string Name);

public interface IPastoralStore
{
    /// <summary>Null when the user does not exist.</summary>
    Task<PastoralViewer?> ViewerAsync(Guid userId, string role, CancellationToken ct = default);
    Task<IReadOnlyList<PastoralStudent>> StudentsAsync(IReadOnlyCollection<Guid> studentProfileIds, CancellationToken ct = default);
    /// <summary>Students of a school (null = every school) in some classes (null = any) whose name or number contains the
    /// search (null = anyone).</summary>
    Task<IReadOnlyList<PastoralStudent>> FindStudentsAsync(Guid? tenantId, IReadOnlyCollection<Guid>? offerings, string? search, int take, CancellationToken ct = default);
    /// <summary>The campuses, year levels and classes of a school (null = every school), limited to some campuses or classes.</summary>
    Task<FilterOptionsDto> FilterOptionsAsync(Guid? tenantId, IReadOnlySet<Guid>? campuses, IReadOnlySet<Guid>? offerings, CancellationToken ct = default);
    Task<IReadOnlyList<OptionDto>> SchoolsAsync(CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, string>> PlaceNamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    /// <summary>Active admins of a school who look after any of the campuses (everyone when no campus is known).</summary>
    Task<IReadOnlyList<Guid>> AdminsAsync(Guid? tenantId, IReadOnlySet<Guid> campuses, CancellationToken ct = default);
    /// <summary>Active staff of a school (teachers and admins), for the wellbeing team.</summary>
    Task<IReadOnlyList<PersonOptionDto>> StaffAsync(Guid tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<PastoralRecord>> RecordsOfAsync(Guid studentProfileId, CancellationToken ct = default);
    /// <summary>Notes (not removed) of a school that happened in a period; null tenant = every school.</summary>
    Task<IReadOnlyList<PastoralRecord>> RecordsAsync(Guid? tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<PastoralRecord>> OpenFollowUpsAsync(Guid? tenantId, CancellationToken ct = default);
    Task<PastoralRecord?> RecordAsync(Guid id, CancellationToken ct = default);
    /// <summary>Behaviour and wellbeing concerns (not removed) about a student since a time.</summary>
    Task<int> ConcernCountAsync(Guid studentProfileId, DateTime since, CancellationToken ct = default);
    void Add(PastoralRecord record);
    Task<IReadOnlyList<PastoralNote>> NotesAsync(IReadOnlyCollection<Guid> recordIds, CancellationToken ct = default);
    void Add(PastoralNote note);
    /// <summary>Open follow-ups whose date has passed and nobody has been reminded of.</summary>
    Task<IReadOnlyList<PastoralRecord>> FollowUpsDueAsync(DateTime now, CancellationToken ct = default);

    Task<IReadOnlyList<SupportPlan>> PlansOfAsync(Guid studentProfileId, CancellationToken ct = default);
    Task<IReadOnlyList<SupportPlan>> ActivePlansAsync(Guid? tenantId, CancellationToken ct = default);
    Task<SupportPlan?> PlanAsync(Guid id, CancellationToken ct = default);
    void Add(SupportPlan plan);
    /// <summary>Active plans whose review date has come and whose owner has not been reminded.</summary>
    Task<IReadOnlyList<SupportPlan>> PlansToReviewAsync(DateTime now, CancellationToken ct = default);

    Task<IReadOnlyList<WellbeingCheckIn>> CheckInsOfAsync(Guid studentProfileId, int take, CancellationToken ct = default);
    Task<IReadOnlyList<WellbeingCheckIn>> CheckInsAsync(Guid? tenantId, DateTime since, CancellationToken ct = default);
    /// <summary>Check-ins still waiting for someone on the wellbeing team, however old.</summary>
    Task<IReadOnlyList<WellbeingCheckIn>> CheckInsNeedingAttentionAsync(Guid? tenantId, CancellationToken ct = default);
    Task<WellbeingCheckIn?> CheckInAsync(Guid id, CancellationToken ct = default);
    void Add(WellbeingCheckIn checkIn);

    Task<IReadOnlyList<TeamMemberRow>> TeamAsync(Guid? tenantId, CancellationToken ct = default);
    Task<PastoralTeamMember?> TeamMemberAsync(Guid id, CancellationToken ct = default);
    void Add(PastoralTeamMember member);
    void Remove(PastoralTeamMember member);

    Task<IReadOnlyList<PastoralCategory>> CategoriesAsync(Guid? tenantId, CancellationToken ct = default);
    Task<PastoralCategory?> CategoryAsync(Guid id, CancellationToken ct = default);
    void Add(PastoralCategory category);

    Task<PastoralSettings?> SettingsAsync(Guid tenantId, CancellationToken ct = default);
    void Add(PastoralSettings settings);

    Task SaveChangesAsync(CancellationToken ct = default);
}

public sealed record PastoralResult<T>(bool Ok, int Status, T? Value, string? Error)
{
    public static PastoralResult<T> Success(T value, int status = 200) => new(true, status, value, null);
    public static PastoralResult<T> Invalid(string error) => new(false, 400, default, error);
    public static PastoralResult<T> NotFound(string error) => new(false, 404, default, error);
    public static PastoralResult<T> Forbidden(string error) => new(false, 403, default, error);
}

// ── Shared ───────────────────────────────────────────────────────────────────

/// <summary>What the person may do on the pastoral pages.</summary>
/// <param name="IsTeam">On the wellbeing team (any place).</param>
/// <param name="CanSetUp">Admins and SuperAdmins: the team, categories and settings.</param>
public sealed record PastoralMeDto(string Role, bool IsStaff, bool IsTeam, bool SeesConfidential, bool SeesCheckIns, bool CanSetUp, bool CheckInsEnabled);

public sealed record StudentOptionDto(Guid Id, string Name, string? RegistrationNumber, string? ClassName);

/// <param name="Role">Their role on the team ("School counsellor") or in the school ("Teacher", "Admin").</param>
public sealed record PersonOptionDto(Guid UserId, string Name, string? Role, bool SeesConfidential = false);

public sealed record FilterOptionsDto(IReadOnlyList<OptionDto> Campuses, IReadOnlyList<OptionDto> Semesters, IReadOnlyList<OptionDto> Classes);

public sealed record CategoryDto(Guid Id, string Kind, string Tone, string Name, bool IsActive);

// ── Notes ────────────────────────────────────────────────────────────────────

public sealed record PastoralNoteDto(Guid Id, string AuthorName, string Text, DateTime At);

/// <summary>A note as staff see it.</summary>
public sealed record PastoralRecordDto(
    Guid Id,
    Guid StudentProfileId,
    string StudentName,
    string Kind,
    string Tone,
    string Severity,
    string? Category,
    string Summary,
    string? Details,
    string? ActionTaken,
    DateTime OccurredAt,
    string? ClassName,
    string Visibility,
    bool SharedWithStudent,
    bool SharedWithParents,
    string AuthorName,
    DateTime RecordedAt,
    string FollowUp,
    Guid? FollowUpUserId,
    string? FollowUpName,
    DateTime? FollowUpBy,
    DateTime? FollowUpDoneAt,
    bool IsRemoved,
    string? RemovedReason,
    bool CanManage,
    bool CanFollowUp,
    IReadOnlyList<PastoralNoteDto> Notes);

/// <summary>A note shared with the student or their family.</summary>
public sealed record SharedRecordDto(Guid Id, string Kind, string Tone, string? Category, string Summary, string? Details, string? ActionTaken,
    DateTime OccurredAt, string AuthorName);

/// <param name="StudentProfileIds">One or more students (up to 60): the same note is written for each.</param>
/// <param name="Kind">Behaviour, Wellbeing, LearningSupport, FamilyContact or General.</param>
/// <param name="Tone">Positive, Neutral or Concern.</param>
/// <param name="Severity">Low, Medium or High (concerns only).</param>
/// <param name="OccurredAt">When it happened (null = now).</param>
public sealed record SaveRecordRequest(
    IReadOnlyList<Guid>? StudentProfileIds,
    Guid? CourseOfferingId,
    string? Kind,
    string? Tone,
    string? Severity,
    string? Category,
    string? Summary,
    string? Details,
    string? ActionTaken,
    DateTime? OccurredAt,
    bool Confidential,
    bool ShareWithStudent,
    bool ShareWithParents,
    Guid? FollowUpUserId,
    DateTime? FollowUpBy);

/// <param name="Skipped">Students the note could not be written for, with the reason.</param>
public sealed record SavedRecordsDto(int Saved, IReadOnlyList<Guid> RecordIds, IReadOnlyList<string> Skipped);

public sealed record PastoralTextRequest(string? Text);

// ── Plans ────────────────────────────────────────────────────────────────────

public sealed record SupportPlanDto(Guid Id, Guid StudentProfileId, string Kind, string Title, string? Needs, string Strategies, DateTime StartsOn,
    DateTime? ReviewOn, string Status, string Visibility, bool SharedWithStudent, bool SharedWithParents, string OwnerName, bool CanManage, DateTime? EndedAt);

public sealed record SharedPlanDto(Guid Id, string Kind, string Title, string Strategies, DateTime StartsOn, DateTime? ReviewOn, string OwnerName);

/// <param name="Kind">LearningAdjustments, Behaviour, Wellbeing or Health.</param>
public sealed record SavePlanRequest(Guid StudentProfileId, string? Kind, string? Title, string? Needs, string? Strategies, DateTime? StartsOn,
    DateTime? ReviewOn, bool Confidential, bool ShareWithStudent, bool ShareWithParents);

// ── Check-ins ────────────────────────────────────────────────────────────────

public sealed record CheckInDto(Guid Id, Guid StudentProfileId, string StudentName, string? ClassName, int Mood, string? Note, bool WantsToTalk,
    DateTime CheckedInAt, bool NeedsAttention, DateTime? AcknowledgedAt, string? AcknowledgedBy, string? AcknowledgeNote);

public sealed record CheckInRequest(int Mood, string? Note, bool WantsToTalk);

// ── Pages ────────────────────────────────────────────────────────────────────

public sealed record PastoralCountsDto(int Positive, int Concerns, int Wellbeing, int Other, int OpenFollowUps, int ActivePlans);

/// <summary>One student's pastoral page: what the person asking may see of it.</summary>
/// <param name="Counts">The last 90 days (follow-ups and plans: now).</param>
/// <param name="CheckIns">Null when the person does not see check-ins.</param>
/// <param name="FollowUpOptions">Who a note can be passed to (those who see confidential notes are marked).</param>
public sealed record StudentPastoralDto(
    StudentOptionDto Student,
    string? CampusName,
    PastoralCountsDto Counts,
    IReadOnlyList<PastoralRecordDto> Records,
    IReadOnlyList<SupportPlanDto> Plans,
    IReadOnlyList<CheckInDto>? CheckIns,
    bool CanWrite,
    bool CanPlan,
    bool SeesConfidential,
    IReadOnlyList<PersonOptionDto> FollowUpOptions,
    IReadOnlyList<CategoryDto> Categories);

/// <summary>A student's own page: notes and plans shared with them, and their check-ins.</summary>
public sealed record MyWellbeingDto(bool CheckInsEnabled, CheckInDto? Latest, IReadOnlyList<CheckInDto> History, IReadOnlyList<SharedRecordDto> Records,
    IReadOnlyList<SharedPlanDto> Plans);

public sealed record FamilyChildDto(Guid StudentProfileId, string Name, string? ClassName, IReadOnlyList<SharedRecordDto> Records, IReadOnlyList<SharedPlanDto> Plans);

public sealed record OverviewQuery(Guid? CampusId, Guid? SemesterId, Guid? CourseOfferingId, DateTime From, DateTime To);

public sealed record OverviewRowDto(StudentOptionDto Student, int Positive, int Concerns, int Wellbeing, int Other, int OpenFollowUps, int ActivePlans,
    int? LastMood, bool NeedsAttention, DateTime? LastNoteAt);

public sealed record CategoryCountDto(string Kind, string Tone, string Category, int Count);

/// <summary>The pastoral overview: totals for the period, the students with notes, check-ins waiting and the person's follow-ups.</summary>
public sealed record OverviewDto(
    DateTime From,
    DateTime To,
    PastoralCountsDto Totals,
    IReadOnlyList<CategoryCountDto> Categories,
    IReadOnlyList<OverviewRowDto> Students,
    IReadOnlyList<CheckInDto> NeedsAttention,
    IReadOnlyList<PastoralRecordDto> MyFollowUps,
    FilterOptionsDto Filters,
    PastoralMeDto Me);

// ── Set-up ───────────────────────────────────────────────────────────────────

public sealed record TeamMemberDto(Guid Id, Guid UserId, string Name, string Title, Guid? CampusId, string? CampusName, Guid? SemesterId, string? SemesterName,
    bool SeesConfidential);

/// <param name="TenantId">The school (SuperAdmins of every school choose one).</param>
public sealed record AddTeamMemberRequest(Guid? TenantId, Guid UserId, string? Title, Guid? CampusId, Guid? SemesterId, bool SeesConfidential);

public sealed record PastoralSettingsDto(bool CheckInsEnabled, int ConcernAlertCount, int ConcernAlertDays);

public sealed record SaveSettingsRequest(Guid? TenantId, bool CheckInsEnabled, int ConcernAlertCount, int ConcernAlertDays);

public sealed record SaveCategoryRequest(Guid? TenantId, string? Kind, string? Tone, string? Name, bool IsActive);

/// <param name="TenantId">The school being set up (null: a SuperAdmin still has to choose one of <paramref name="Schools"/>).</param>
public sealed record PastoralSetupDto(Guid? TenantId, IReadOnlyList<OptionDto> Schools, IReadOnlyList<TeamMemberDto> Team, IReadOnlyList<PersonOptionDto> Staff,
    PastoralSettingsDto Settings, IReadOnlyList<CategoryDto> Categories, FilterOptionsDto Places);
