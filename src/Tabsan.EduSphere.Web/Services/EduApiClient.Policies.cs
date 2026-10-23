using System.Text.Json;
using Tabsan.EduSphere.Application.Assignments;

namespace Tabsan.EduSphere.Web.Services;

// Plan M8 — late & missing policy, assignment options and overrides, student groups and peer review.

public partial interface IEduApiClient
{
    Task<LatePolicyDto?> GetLatePolicyAsync(Guid offeringId, CancellationToken ct);
    Task<List<GroupSetDto>> GetGroupSetsAsync(Guid offeringId, CancellationToken ct);
    Task<List<GroupMemberDto>> GetCourseRosterAsync(Guid offeringId, CancellationToken ct);
    Task<AssignmentOptionsDto?> GetAssignmentOptionsAsync(Guid assignmentId, CancellationToken ct);
    Task<List<AssignmentOverrideDto>> GetAssignmentOverridesAsync(Guid assignmentId, CancellationToken ct);
    Task<PeerReviewOverviewDto?> GetPeerReviewOverviewAsync(Guid assignmentId, CancellationToken ct);
    Task<StudentAssignmentExtrasDto?> GetMyAssignmentExtrasAsync(Guid assignmentId, CancellationToken ct);
    Task<List<MyGroupDto>> GetMyGroupsAsync(Guid offeringId, CancellationToken ct);
    Task<PeerReviewTaskDto?> GetPeerReviewTaskAsync(Guid peerReviewId, CancellationToken ct);

    /// <summary>JSON call under api/v1/assignment/ with the body serialised for the API (null = no body).</summary>
    Task<ApiRaw> AssignmentPolicySendAsync(HttpMethod method, string path, object? body, CancellationToken ct);
}

public partial class EduApiClient
{
    public Task<LatePolicyDto?> GetLatePolicyAsync(Guid offeringId, CancellationToken ct)
        => TryGetAsync<LatePolicyDto>($"{WorkspaceBase}offerings/{offeringId}/late-policy", ct);

    public async Task<List<GroupSetDto>> GetGroupSetsAsync(Guid offeringId, CancellationToken ct)
        => await TryGetAsync<List<GroupSetDto>>($"{WorkspaceBase}offerings/{offeringId}/group-sets", ct) ?? [];

    public async Task<List<GroupMemberDto>> GetCourseRosterAsync(Guid offeringId, CancellationToken ct)
        => await TryGetAsync<List<GroupMemberDto>>($"{WorkspaceBase}offerings/{offeringId}/roster", ct) ?? [];

    public Task<AssignmentOptionsDto?> GetAssignmentOptionsAsync(Guid assignmentId, CancellationToken ct)
        => TryGetAsync<AssignmentOptionsDto>($"{WorkspaceBase}{assignmentId}/options", ct);

    public async Task<List<AssignmentOverrideDto>> GetAssignmentOverridesAsync(Guid assignmentId, CancellationToken ct)
        => await TryGetAsync<List<AssignmentOverrideDto>>($"{WorkspaceBase}{assignmentId}/overrides", ct) ?? [];

    public Task<PeerReviewOverviewDto?> GetPeerReviewOverviewAsync(Guid assignmentId, CancellationToken ct)
        => TryGetAsync<PeerReviewOverviewDto>($"{WorkspaceBase}{assignmentId}/peer-reviews", ct);

    public Task<StudentAssignmentExtrasDto?> GetMyAssignmentExtrasAsync(Guid assignmentId, CancellationToken ct)
        => TryGetAsync<StudentAssignmentExtrasDto>($"{WorkspaceBase}{assignmentId}/my-extras", ct);

    public async Task<List<MyGroupDto>> GetMyGroupsAsync(Guid offeringId, CancellationToken ct)
        => await TryGetAsync<List<MyGroupDto>>($"{WorkspaceBase}offerings/{offeringId}/my-groups", ct) ?? [];

    public Task<PeerReviewTaskDto?> GetPeerReviewTaskAsync(Guid peerReviewId, CancellationToken ct)
        => TryGetAsync<PeerReviewTaskDto>($"{WorkspaceBase}peer-reviews/{peerReviewId}", ct);

    public Task<ApiRaw> AssignmentPolicySendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
        => AssignmentWorkspaceSendAsync(method, path, body is null ? null : JsonSerializer.Serialize(body, CamelCase), ct);

    private static readonly JsonSerializerOptions CamelCase = new(JsonSerializerDefaults.Web);
}
