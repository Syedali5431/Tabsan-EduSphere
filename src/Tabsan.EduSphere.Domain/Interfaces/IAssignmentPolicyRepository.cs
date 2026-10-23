using Tabsan.EduSphere.Domain.Assignments;

namespace Tabsan.EduSphere.Domain.Interfaces;

// Plan M8 — late policy, overrides, groups and peer reviews.

public interface IAssignmentPolicyRepository
{
    // ── late policy ──
    Task<LatePolicy?> GetLatePolicyAsync(Guid offeringId, CancellationToken ct = default);
    /// <summary>Offerings whose policy gives a grade to missing work.</summary>
    Task<IReadOnlyList<LatePolicy>> GetMissingPoliciesAsync(CancellationToken ct = default);
    void Add(LatePolicy policy);

    // ── assignments ──
    /// <summary>Published assignments of an offering that students hand in here (not external tools).</summary>
    Task<IReadOnlyList<Assignment>> GetGradableAssignmentsAsync(Guid offeringId, CancellationToken ct = default);
    /// <summary>Peer-review assignments past their due date that have no reviews allocated yet.</summary>
    Task<IReadOnlyList<Assignment>> GetAssignmentsAwaitingPeerAllocationAsync(DateTime now, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetEnrolledStudentIdsAsync(Guid offeringId, CancellationToken ct = default);

    // ── overrides ──
    Task<IReadOnlyList<AssignmentOverride>> GetOverridesAsync(Guid assignmentId, CancellationToken ct = default);
    Task<AssignmentOverride?> GetOverrideAsync(Guid overrideId, CancellationToken ct = default);
    void Add(AssignmentOverride item);
    void Remove(AssignmentOverride item);

    // ── groups ──
    Task<IReadOnlyList<GroupSet>> GetGroupSetsAsync(Guid offeringId, CancellationToken ct = default);
    Task<GroupSet?> GetGroupSetAsync(Guid groupSetId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentGroup>> GetGroupsAsync(IReadOnlyCollection<Guid> groupSetIds, CancellationToken ct = default);
    Task<StudentGroup?> GetGroupAsync(Guid groupId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentGroupMember>> GetMembersAsync(IReadOnlyCollection<Guid> groupSetIds, CancellationToken ct = default);
    /// <summary>The student's memberships in groups of an offering.</summary>
    Task<IReadOnlyList<StudentGroupMember>> GetMembershipsAsync(Guid studentProfileId, Guid offeringId, CancellationToken ct = default);
    Task<bool> GroupHasSubmissionsAsync(Guid groupId, CancellationToken ct = default);
    void Add(GroupSet set);
    void Add(StudentGroup group);
    void Add(StudentGroupMember member);
    void Remove(GroupSet set);
    void Remove(StudentGroup group);
    void Remove(StudentGroupMember member);

    // ── peer reviews ──
    Task<IReadOnlyList<PeerReview>> GetPeerReviewsAsync(Guid assignmentId, CancellationToken ct = default);
    Task<PeerReview?> GetPeerReviewAsync(Guid peerReviewId, CancellationToken ct = default);
    Task<bool> IsReviewerOfAsync(Guid submissionId, Guid reviewerStudentProfileId, CancellationToken ct = default);
    void Add(PeerReview review);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
