using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Lms;
using Tabsan.EduSphere.Domain.Lti;
using Tabsan.EduSphere.Domain.Outcomes;
using Tabsan.EduSphere.Domain.Quizzes;

namespace Tabsan.EduSphere.Application.Lms;

/// <summary>The parts of a course that can be copied (the values sent and returned by the API).</summary>
public static class CourseCopyParts
{
    public const string Modules = "modules";
    public const string Assignments = "assignments";
    public const string Quizzes = "quizzes";
    public const string QuestionBanks = "banks";
    public const string Files = "files";
    public const string ExternalTools = "tools";
    /// <summary>Course settings: the late and missing work policy.</summary>
    public const string Settings = "settings";

    public static readonly IReadOnlyList<string> All = [Modules, Assignments, Quizzes, QuestionBanks, Files, ExternalTools, Settings];
}

/// <summary>A course offering as course copy sees it.</summary>
public sealed record CourseCopyOffering(
    Guid OfferingId,
    Guid CourseId,
    string CourseCode,
    string CourseTitle,
    Guid DepartmentId,
    Guid? TenantId,
    Guid? CampusId,
    Guid SemesterId,
    string SemesterName,
    DateTime SemesterStart,
    Guid? TeacherUserId,
    string? TeacherName);

/// <summary>Everything in a course that a copy can take, read without tracking.</summary>
public sealed class CourseContent
{
    public List<CourseContentModule> Modules { get; } = [];
    public List<ContentVideo> Videos { get; } = [];
    public List<ModuleItem> Items { get; } = [];
    public List<ModulePrerequisite> Prerequisites { get; } = [];
    /// <summary>The course's active files and links (every term's, since modules may link to any of them).</summary>
    public List<CourseMaterial> Materials { get; } = [];
    public List<Assignment> Assignments { get; } = [];
    public List<Rubric> Rubrics { get; } = [];
    public List<RubricCriterion> Criteria { get; } = [];
    public List<RubricLevel> Levels { get; } = [];
    public List<GroupSet> GroupSets { get; } = [];
    public LatePolicy? LatePolicy { get; set; }
    public List<Quiz> Quizzes { get; } = [];
    /// <summary>Questions of the quizzes and of the course's own question banks.</summary>
    public List<QuizQuestion> Questions { get; } = [];
    public List<QuizOption> Options { get; } = [];
    public List<QuizQuestionGroup> QuestionGroups { get; } = [];
    /// <summary>Question banks that belong to the course (shared and department banks are used where they are).</summary>
    public List<QuestionBank> Banks { get; } = [];
    public List<LtiResourceLink> ToolLinks { get; } = [];
    public List<LtiLineItem> LineItems { get; } = [];
    public List<OutcomeAlignment> Alignments { get; } = [];
    /// <summary>For each aligned outcome: the course it belongs to (course outcomes) or null (program and school outcomes).</summary>
    public Dictionary<Guid, Guid?> OutcomeCourses { get; } = [];
    /// <summary>Packages are not copied (their files are not); listed so the copy can say so.</summary>
    public List<ScormPackage> ScormPackages { get; } = [];
    /// <summary>Discussion titles by id: discussions are not copied, so module items that open one are listed as left out.</summary>
    public Dictionary<Guid, string> DiscussionTitles { get; } = [];
}

/// <summary>What a copy is asked to do.</summary>
public sealed record CourseCopyPlan(
    CourseCopyOffering Source,
    CourseCopyOffering Target,
    IReadOnlySet<string> Parts,
    IReadOnlySet<Guid>? ItemIds,
    int ShiftDays,
    bool KeepPublished,
    Guid UserId);

public interface ICourseCopyStore
{
    Task<CourseCopyOffering?> OfferingAsync(Guid offeringId, CancellationToken ct = default);

    /// <summary>Offerings of a school (tenant; null = installations without schools), newest term first: those of
    /// <paramref name="departmentIds"/> (null = every department) and those <paramref name="teacherUserId"/> teaches.</summary>
    Task<IReadOnlyList<CourseCopyOffering>> OfferingsAsync(Guid? tenantId, IReadOnlyCollection<Guid>? departmentIds, Guid? teacherUserId, int limit,
        CancellationToken ct = default);

    /// <summary>The departments an admin or teacher works in.</summary>
    Task<IReadOnlyList<Guid>> DepartmentsOfAsync(Guid userId, string role, CancellationToken ct = default);

    Task<CourseTemplate?> TemplateAsync(Guid offeringId, CancellationToken ct = default);

    /// <summary>Templates whose offering belongs to the school, with their offerings.</summary>
    Task<IReadOnlyList<(CourseTemplate Template, CourseCopyOffering Offering)>> TemplatesAsync(Guid? tenantId, CancellationToken ct = default);

    void AddTemplate(CourseTemplate template);
    void RemoveTemplate(CourseTemplate template);

    Task<CourseContent> ReadAsync(CourseCopyOffering offering, CancellationToken ct = default);

    /// <summary>Adds the copy and its history row in one save (all or nothing).</summary>
    Task SaveCopyAsync(CourseContent copy, CourseCopyRun run, CancellationToken ct = default);

    /// <summary>Copies into an offering, newest first, with the copier's name.</summary>
    Task<IReadOnlyList<(CourseCopyRun Run, string CopiedBy)>> RunsAsync(Guid targetOfferingId, int limit, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}

// ── API shapes ──────────────────────────────────────────────────────────────

public sealed record CourseCopySourceDto(
    Guid OfferingId,
    string CourseCode,
    string CourseTitle,
    string SemesterName,
    DateTime SemesterStart,
    string? TeacherName,
    bool IsTemplate,
    string? TemplateName,
    string? TemplateDescription,
    string? TemplateScope,
    bool TaughtByYou = false);

public sealed record CourseCopySourcesDto(CourseCopySourceDto Target, IReadOnlyList<CourseCopySourceDto> Sources);

/// <param name="Week">Modules: the week number.</param>
/// <param name="DueDate">Assignments: the due date (UTC).</param>
/// <param name="Count">Modules: items; quizzes and banks: questions.</param>
/// <param name="Published">False when students cannot see it in the source.</param>
public sealed record CourseCopyItemDto(Guid Id, string Title, int? Week, DateTime? DueDate, int? Count, bool Published);

public sealed record CourseCopyPartDto(string Part, int Count, IReadOnlyList<CourseCopyItemDto> Items);

/// <summary>Something not copied: <see cref="CourseCopyNotes"/> says why.</summary>
/// <param name="Title">What it is (null for a count of outcome links).</param>
public sealed record CourseCopyNoteDto(string Reason, string? Title, int Count = 1);

public static class CourseCopyNotes
{
    /// <summary>SCORM packages (and the module items and assignments that use one): their files are not copied.</summary>
    public const string Scorm = "scorm";
    /// <summary>Module items that open a discussion: discussions belong to the class that held them.</summary>
    public const string Discussion = "discussion";
    /// <summary>Module items whose content no longer exists, or was not chosen and could not be brought along.</summary>
    public const string Missing = "missing";
    /// <summary>Outcome links to another course's own outcomes.</summary>
    public const string OtherCourseOutcomes = "outcomes";
    /// <summary>Content that failed a check (for example a date that no longer fits).</summary>
    public const string Invalid = "invalid";
}

/// <summary>The person asking: their role, and the school they are limited to (null = every school, for SuperAdmins).</summary>
public sealed record CourseCopyActor(Guid UserId, string Role, Guid? TenantId);

public sealed record CourseCopyPreviewDto(
    CourseCopySourceDto Source,
    CourseCopySourceDto Target,
    int SuggestedShiftDays,
    DateTime? PreviouslyCopiedAt,
    IReadOnlyList<CourseCopyPartDto> Parts,
    IReadOnlyList<CourseCopyNoteDto> NotCopied);

/// <param name="Parts">What to copy (<see cref="CourseCopyParts"/>); null or empty = everything.</param>
/// <param name="ItemIds">Only these modules, assignments, quizzes, banks, files and tools of the chosen parts; null = all of them.</param>
/// <param name="ShiftDays">Days added to every date (due dates, availability, module unlock dates).</param>
/// <param name="KeepPublished">Publish what was published in the source; otherwise everything arrives hidden from students.</param>
public sealed record CourseCopyRequest(Guid SourceOfferingId, IReadOnlyList<string>? Parts, IReadOnlyList<Guid>? ItemIds, int ShiftDays, bool KeepPublished);

public sealed record CourseCopyResultDto(
    Guid RunId,
    IReadOnlyDictionary<string, int> Copied,
    IReadOnlyList<CourseCopyNoteDto> NotCopied,
    int ShiftDays,
    bool KeptPublished);

public sealed record CourseCopyRunDto(
    Guid Id,
    Guid SourceOfferingId,
    string SourceTitle,
    string SourceSemester,
    string CopiedBy,
    DateTime CopiedAt,
    int ShiftDays,
    bool KeptPublished,
    IReadOnlyDictionary<string, int> Copied,
    int NotCopiedCount);

public sealed record CourseTemplateDto(Guid OfferingId, string Name, string? Description, string Scope, string CourseCode, string CourseTitle, string SemesterName);

/// <param name="Scope">"Department" or "Institution".</param>
public sealed record SaveCourseTemplateRequest(string? Name, string? Description, string? Scope);

public sealed record CourseCopyResult<T>(bool Ok, int Status, T? Value, string? Error)
{
    public static CourseCopyResult<T> Success(T value, int status = 200) => new(true, status, value, null);
    public static CourseCopyResult<T> Invalid(string error) => new(false, 400, default, error);
    public static CourseCopyResult<T> NotFound(string error = "Course not found.") => new(false, 404, default, error);
    public static CourseCopyResult<T> Forbidden(string error) => new(false, 403, default, error);
}
