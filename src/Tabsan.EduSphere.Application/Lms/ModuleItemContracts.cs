namespace Tabsan.EduSphere.Application.Lms;

// Plan M7 — module items, release conditions and progress contracts.

public sealed record ModuleItemResponse(Guid Id, Guid ModuleId, string ItemType, Guid? TargetId, string Title, string? Url, string? Body, int Position,
    int IndentLevel, bool IsPublished, string RequirementType, decimal? MinScorePercent, bool TargetMissing,
    IReadOnlyList<string> AllowedRequirements);

public sealed record ModuleStructureModule(Guid Id, string Title, int WeekNumber, string? Body, bool IsPublished, DateTime? UnlockAt,
    bool RequireSequentialProgress, string CompletionMode, IReadOnlyList<Guid> PrerequisiteModuleIds, IReadOnlyList<ModuleItemResponse> Items);

public sealed record ModuleStructureResponse(Guid CourseOfferingId, string CourseCode, string CourseTitle, IReadOnlyList<ModuleStructureModule> Modules);

public sealed record LinkableResponse(string Type, Guid Id, string Title, string? Detail);

public sealed record AddModuleItemRequest(string ItemType, Guid? TargetId, string? Title, string? Url, string? Body, int IndentLevel,
    string? RequirementType, decimal? MinScorePercent);

public sealed record UpdateModuleItemRequest(string Title, string? Url, string? Body, int IndentLevel, string? RequirementType, decimal? MinScorePercent, bool IsPublished);

public sealed record ReorderModuleItemsRequest(IReadOnlyList<Guid> ItemIds);

public sealed record ModuleReleaseRequest(DateTime? UnlockAt, bool RequireSequentialProgress, string? CompletionMode, IReadOnlyList<Guid>? PrerequisiteModuleIds);

// ── student view ──

/// <summary>Status: "done", "todo", "locked" or "none" (no requirement).</summary>
public sealed record StudentModuleItem(Guid Id, string ItemType, string Title, int IndentLevel, string RequirementType, decimal? MinScorePercent,
    string Status, string? LockReason, bool CanMarkDone);

public sealed record StudentModule(Guid Id, string Title, int WeekNumber, string? Body, bool Locked, string? LockReason, bool Completed,
    int Required, int Done, int Percent, IReadOnlyList<StudentModuleItem> Items);

public sealed record StudentModulesResponse(Guid CourseOfferingId, string CourseCode, string CourseTitle, int CoursePercent, IReadOnlyList<StudentModule> Modules);

/// <summary>What opening an item leads to.</summary>
public sealed record ModuleItemOpenResponse(Guid ItemId, string ItemType, Guid? TargetId, string Title, Guid CourseOfferingId, bool Locked, string? LockReason,
    string? Url, string? Body, string? EmbedUrl, string? StorageUrl, bool MaterialHasFile, string RequirementType, string Status, bool CanMarkDone);

public sealed record ModuleNavLink(Guid Id, string Title, bool Locked);

public sealed record ModuleItemNavResponse(Guid ItemId, string ModuleTitle, ModuleNavLink? Previous, ModuleNavLink? Next, string Status, bool CanMarkDone, Guid CourseOfferingId);

// ── teacher report ──

public sealed record ProgressModuleColumn(Guid ModuleId, string Title, int Required);

public sealed record ProgressCell(Guid ModuleId, int Done, int Required, int Percent, bool Completed, bool Locked);

public sealed record ProgressStudentRow(Guid StudentProfileId, string Name, string? RegistrationNumber, int CoursePercent, IReadOnlyList<ProgressCell> Modules);

public sealed record ModuleProgressReport(Guid CourseOfferingId, string CourseCode, string CourseTitle, IReadOnlyList<ProgressModuleColumn> Modules,
    IReadOnlyList<ProgressStudentRow> Students);

public sealed record ModuleResult<T>(T? Value, int Status, string? Error)
{
    public bool Ok => Status is >= 200 and < 300;
    public static ModuleResult<T> Success(T value, int status = 200) => new(value, status, null);
    public static ModuleResult<T> Fail(int status, string error) => new(default, status, error);
}
