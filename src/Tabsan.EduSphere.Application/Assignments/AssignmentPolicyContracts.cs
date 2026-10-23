using Tabsan.EduSphere.Domain.Assignments;

namespace Tabsan.EduSphere.Application.Assignments;

// Plan M8 — late & missing policy, availability, overrides, groups and peer review contracts.

/// <summary>A student's dates for an assignment after overrides.</summary>
public sealed record EffectiveDates(DateTime DueDate, DateTime? AvailableFrom, DateTime ClosesAt, string Source);

public sealed record LatePolicyDto(bool LateDeductionEnabled, decimal DeductionPercent, string Interval, decimal LowestGradePercent,
    bool MissingEnabled, decimal MissingGradePercent);

public sealed record AssignmentOptionsDto(Guid AssignmentId, DateTime DueDate, DateTime? AvailableFrom, DateTime? AvailableUntil, Guid? GroupSetId,
    bool GradeGroupIndividually, int PeerReviewCount, DateTime? PeerReviewDueDate, bool PeerReviewAnonymous, bool HasSubmissions);

public sealed record SaveAssignmentOptionsRequest(DateTime? AvailableFrom, DateTime? AvailableUntil, Guid? GroupSetId, bool GradeGroupIndividually,
    int PeerReviewCount, DateTime? PeerReviewDueDate, bool PeerReviewAnonymous);

public sealed record AssignmentOverrideDto(Guid Id, string Target, Guid? StudentProfileId, Guid? GroupId, string TargetName, DateTime DueDate,
    DateTime? AvailableFrom, DateTime? AvailableUntil, string? Note);

public sealed record AddOverrideRequest(Guid? StudentProfileId, Guid? GroupId, DateTime DueDate, DateTime? AvailableFrom, DateTime? AvailableUntil, string? Note);

public sealed record WaiveLatePenaltyRequest(bool Waived);

public sealed record MissingRunResult(int Graded, int Assignments);

// ── groups ──

public sealed record GroupMemberDto(Guid StudentProfileId, string Name, string? RegistrationNumber);

public sealed record GroupDto(Guid Id, string Name, int MemberCount, IReadOnlyList<GroupMemberDto> Members);

public sealed record GroupSetDto(Guid Id, string Name, bool SelfSignup, int? MaxSize, IReadOnlyList<GroupDto> Groups, IReadOnlyList<GroupMemberDto> Unassigned);

public sealed record SaveGroupSetRequest(string Name, bool SelfSignup, int? MaxSize);

public sealed record CreateGroupRequest(string Name);

public sealed record RandomSplitRequest(int? GroupCount, int? GroupSize);

public sealed record AddMemberRequest(Guid StudentProfileId);

/// <summary>A student's group in one group set (Group null when not in one yet).</summary>
public sealed record MyGroupDto(Guid GroupSetId, string GroupSetName, bool SelfSignup, int? MaxSize, GroupDto? Group, IReadOnlyList<GroupDto> Joinable);

// ── peer review ──

public sealed record PeerReviewSummaryDto(Guid Id, Guid SubmissionId, string RevieweeLabel, string ReviewerLabel, string Status, decimal? Score, DateTime? CompletedAt);

public sealed record PeerReviewOverviewDto(Guid AssignmentId, int PeerReviewCount, DateTime? PeerReviewDueDate, bool Anonymous, int Assigned, int Completed,
    IReadOnlyList<PeerReviewSummaryDto> Reviews);

public sealed record PeerRubricLevelDto(Guid LevelId, string Label, decimal Points);

public sealed record PeerRubricCriterionDto(Guid CriterionId, string Name, decimal MaxPoints, IReadOnlyList<PeerRubricLevelDto> Levels, Guid? ChosenLevelId);

public sealed record PeerFileDto(Guid FileId, string FileName, string ContentType, long SizeBytes);

/// <summary>What a reviewer sees: the work (latest attempt), the rubric and their own review so far.</summary>
public sealed record PeerReviewTaskDto(Guid Id, Guid AssignmentId, string AssignmentTitle, string RevieweeLabel, decimal MaxMarks, DateTime? DueDate,
    string Status, string? Text, IReadOnlyList<PeerFileDto> Files, IReadOnlyList<PeerRubricCriterionDto> Rubric, decimal? Score, string? Comment);

public sealed record PeerRubricChoice(Guid CriterionId, Guid LevelId);

public sealed record SubmitPeerReviewRequest(decimal? Score, string? Comment, IReadOnlyList<PeerRubricChoice>? Rubric);

/// <summary>Reviews a student received (completed ones only).</summary>
public sealed record ReceivedPeerReviewDto(string ReviewerLabel, decimal? Score, string? Comment, DateTime? CompletedAt);

/// <summary>The extra assignment information for a student: their dates, late policy, group and peer reviews.</summary>
public sealed record StudentAssignmentExtrasDto(Guid AssignmentId, Guid CourseOfferingId, DateTime DueDate, DateTime? AvailableFrom, DateTime ClosesAt, bool DatesOverridden,
    LatePolicyDto? LatePolicy, string? GroupName, IReadOnlyList<string> GroupMembers, bool NeedsGroup, int PeerReviewCount, DateTime? PeerReviewDueDate,
    IReadOnlyList<PeerReviewSummaryDto> ReviewsToDo, IReadOnlyList<ReceivedPeerReviewDto> ReviewsReceived, decimal? RawMarks, decimal LatePenalty, bool IsMissing);

public sealed record PolicyResult<T>(T? Value, int Status, string? Error)
{
    public bool Ok => Status is >= 200 and < 300;
    public static PolicyResult<T> Success(T value, int status = 200) => new(value, status, null);
    public static PolicyResult<T> Fail(int status, string error) => new(default, status, error);
}

/// <summary>Plan M8: dates and late deductions used by submission and grading paths.</summary>
public interface IAssignmentPolicyService
{
    Task<EffectiveDates> DatesForAsync(Assignment assignment, Guid studentProfileId, CancellationToken ct = default);
    /// <summary>Marks the late policy deducts from <paramref name="rawMarks"/> for this submission (0 when not late or no policy).</summary>
    Task<decimal> LatePenaltyAsync(Assignment assignment, AssignmentSubmission submission, decimal rawMarks, CancellationToken ct = default);
    /// <summary>The student's group in the assignment's group set (null for individual assignments or no group).</summary>
    Task<(Guid GroupId, IReadOnlyList<Guid> MemberIds)?> GroupForAsync(Assignment assignment, Guid studentProfileId, CancellationToken ct = default);
    /// <summary>Peer reviewers may open the work they review.</summary>
    Task<bool> IsPeerReviewerAsync(Guid submissionId, Guid studentProfileId, CancellationToken ct = default);
    Task<string?> GroupNameAsync(Guid groupId, CancellationToken ct = default);
    /// <summary>Average score and number of completed peer reviews of a piece of work (group work counts once).</summary>
    Task<(decimal? Average, int Completed)> PeerSummaryAsync(AssignmentSubmission submission, CancellationToken ct = default);
}
