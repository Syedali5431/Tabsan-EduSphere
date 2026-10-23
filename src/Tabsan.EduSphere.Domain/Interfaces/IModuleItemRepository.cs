using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Domain.Interfaces;

// Plan M7 — module items, progress and the read models used to evaluate requirements.

public sealed record ModuleOfferingInfo(Guid OfferingId, Guid CourseId, string CourseCode, string CourseTitle, Guid? TenantId, Guid? CampusId);

public sealed record ModuleStudentInfo(Guid StudentProfileId, Guid UserId, string Name, string? RegistrationNumber);

/// <summary>Something in the course a module item can point to.</summary>
public sealed record ModuleLinkable(ModuleItemType Type, Guid Id, string Title, string? Detail);

/// <summary>A student's best evidence on an assignment or quiz.</summary>
public sealed record ModuleWorkEvidence(Guid TargetId, Guid StudentProfileId, bool Submitted, decimal? ScorePercent);

public interface IModuleItemRepository
{
    Task<ModuleOfferingInfo?> GetOfferingAsync(Guid offeringId, CancellationToken ct = default);
    Task<IReadOnlyList<CourseContentModule>> GetModulesAsync(Guid offeringId, CancellationToken ct = default);
    Task<CourseContentModule?> GetModuleAsync(Guid moduleId, CancellationToken ct = default);
    Task<IReadOnlyList<ModuleItem>> GetItemsAsync(Guid offeringId, CancellationToken ct = default);
    Task<ModuleItem?> GetItemAsync(Guid itemId, CancellationToken ct = default);
    /// <summary>Items pointing at a record, in any offering.</summary>
    Task<IReadOnlyList<ModuleItem>> GetItemsByTargetAsync(ModuleItemType type, Guid targetId, CancellationToken ct = default);
    Task<IReadOnlyList<ModulePrerequisite>> GetPrerequisitesAsync(IReadOnlyCollection<Guid> moduleIds, CancellationToken ct = default);
    Task<IReadOnlyList<ModuleItemProgress>> GetProgressAsync(Guid offeringId, IReadOnlyCollection<Guid>? studentIds, CancellationToken ct = default);
    Task<IReadOnlyList<ModuleItemProgress>> GetProgressForItemAsync(Guid itemId, CancellationToken ct = default);

    Task<IReadOnlyList<ModuleStudentInfo>> GetEnrolledStudentsAsync(Guid offeringId, CancellationToken ct = default);
    Task<ModuleStudentInfo?> GetStudentAsync(Guid studentProfileId, CancellationToken ct = default);
    Task<bool> IsEnrolledAsync(Guid offeringId, Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Course content that can be added to a module, with titles (also used to show item titles and spot removed targets).</summary>
    Task<IReadOnlyList<ModuleLinkable>> GetLinkablesAsync(Guid offeringId, CancellationToken ct = default);
    Task<IReadOnlyList<ModuleWorkEvidence>> GetAssignmentEvidenceAsync(IReadOnlyCollection<Guid> assignmentIds, IReadOnlyCollection<Guid> studentIds, CancellationToken ct = default);
    Task<IReadOnlyList<ModuleWorkEvidence>> GetQuizEvidenceAsync(IReadOnlyCollection<Guid> quizIds, IReadOnlyCollection<Guid> studentIds, CancellationToken ct = default);
    /// <summary>(thread, user) pairs where the user started the thread or replied in it.</summary>
    Task<IReadOnlyList<(Guid ThreadId, Guid UserId)>> GetDiscussionContributionsAsync(IReadOnlyCollection<Guid> threadIds, IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);
    /// <summary>Plan M13: per SCORM package and student — finished (completed, passed or failed) and the best finished score.</summary>
    Task<IReadOnlyList<ModuleWorkEvidence>> GetScormEvidenceAsync(IReadOnlyCollection<Guid> packageIds, IReadOnlyCollection<Guid> studentIds, CancellationToken ct = default);
    /// <summary>The graded assignment behind each LTI course item (when it has one).</summary>
    Task<IReadOnlyDictionary<Guid, Guid>> GetLtiAssignmentsAsync(IReadOnlyCollection<Guid> linkIds, CancellationToken ct = default);
    Task<(string? EmbedUrl, string? StorageUrl)?> GetVideoAsync(Guid videoId, CancellationToken ct = default);
    Task<(string? LinkUrl, bool HasFile)?> GetMaterialAsync(Guid materialId, CancellationToken ct = default);

    void Add(ModuleItem item);
    void Remove(ModuleItem item);
    void Add(ModulePrerequisite prerequisite);
    void Remove(ModulePrerequisite prerequisite);
    void Add(ModuleItemProgress progress);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
