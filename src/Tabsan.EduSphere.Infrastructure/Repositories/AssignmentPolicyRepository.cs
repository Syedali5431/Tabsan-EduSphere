using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>Plan M8 — late policies, due-date overrides, student groups and peer reviews.</summary>
public sealed class AssignmentPolicyRepository : IAssignmentPolicyRepository
{
    private readonly ApplicationDbContext _db;

    public AssignmentPolicyRepository(ApplicationDbContext db) => _db = db;

    // ── late policy ──

    public Task<LatePolicy?> GetLatePolicyAsync(Guid offeringId, CancellationToken ct = default)
        => _db.LatePolicies.FirstOrDefaultAsync(p => p.CourseOfferingId == offeringId, ct);

    public async Task<IReadOnlyList<LatePolicy>> GetMissingPoliciesAsync(CancellationToken ct = default)
        => await _db.LatePolicies.Where(p => p.MissingEnabled).ToListAsync(ct);

    public void Add(LatePolicy policy) => _db.LatePolicies.Add(policy);

    // ── assignments ──

    public async Task<IReadOnlyList<Assignment>> GetGradableAssignmentsAsync(Guid offeringId, CancellationToken ct = default)
        => await _db.Assignments.Where(a => a.CourseOfferingId == offeringId && a.IsPublished && a.ExternalToolId == null).ToListAsync(ct);

    public async Task<IReadOnlyList<Assignment>> GetAssignmentsAwaitingPeerAllocationAsync(DateTime now, CancellationToken ct = default)
        => await _db.Assignments
            .Where(a => a.IsPublished && a.PeerReviewCount > 0 && a.DueDate <= now && !_db.PeerReviews.Any(r => r.AssignmentId == a.Id))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetEnrolledStudentIdsAsync(Guid offeringId, CancellationToken ct = default)
        => await _db.Enrollments.Where(e => e.CourseOfferingId == offeringId && e.Status == EnrollmentStatus.Active)
            .Select(e => e.StudentProfileId).Distinct().ToListAsync(ct);

    // ── overrides ──

    public async Task<IReadOnlyList<AssignmentOverride>> GetOverridesAsync(Guid assignmentId, CancellationToken ct = default)
        => await _db.AssignmentOverrides.Where(o => o.AssignmentId == assignmentId).OrderBy(o => o.CreatedAt).ToListAsync(ct);

    public Task<AssignmentOverride?> GetOverrideAsync(Guid overrideId, CancellationToken ct = default)
        => _db.AssignmentOverrides.FirstOrDefaultAsync(o => o.Id == overrideId, ct);

    public void Add(AssignmentOverride item) => _db.AssignmentOverrides.Add(item);

    public void Remove(AssignmentOverride item) => _db.AssignmentOverrides.Remove(item);

    // ── groups ──

    public async Task<IReadOnlyList<GroupSet>> GetGroupSetsAsync(Guid offeringId, CancellationToken ct = default)
        => await _db.GroupSets.Where(s => s.CourseOfferingId == offeringId).OrderBy(s => s.CreatedAt).ToListAsync(ct);

    public Task<GroupSet?> GetGroupSetAsync(Guid groupSetId, CancellationToken ct = default)
        => _db.GroupSets.FirstOrDefaultAsync(s => s.Id == groupSetId, ct);

    public async Task<IReadOnlyList<StudentGroup>> GetGroupsAsync(IReadOnlyCollection<Guid> groupSetIds, CancellationToken ct = default)
        => groupSetIds.Count == 0 ? [] : await _db.StudentGroups.Where(g => groupSetIds.Contains(g.GroupSetId)).OrderBy(g => g.Name).ToListAsync(ct);

    public Task<StudentGroup?> GetGroupAsync(Guid groupId, CancellationToken ct = default)
        => _db.StudentGroups.FirstOrDefaultAsync(g => g.Id == groupId, ct);

    public async Task<IReadOnlyList<StudentGroupMember>> GetMembersAsync(IReadOnlyCollection<Guid> groupSetIds, CancellationToken ct = default)
        => groupSetIds.Count == 0 ? [] : await _db.StudentGroupMembers.Where(m => groupSetIds.Contains(m.GroupSetId)).ToListAsync(ct);

    public async Task<IReadOnlyList<StudentGroupMember>> GetMembershipsAsync(Guid studentProfileId, Guid offeringId, CancellationToken ct = default)
        => await (from m in _db.StudentGroupMembers
                  join s in _db.GroupSets on m.GroupSetId equals s.Id
                  where m.StudentProfileId == studentProfileId && s.CourseOfferingId == offeringId
                  select m).ToListAsync(ct);

    public Task<bool> GroupHasSubmissionsAsync(Guid groupId, CancellationToken ct = default)
        => _db.AssignmentSubmissions.AnyAsync(s => s.GroupId == groupId && s.CurrentAttemptNumber > 0, ct);

    public void Add(GroupSet set) => _db.GroupSets.Add(set);
    public void Add(StudentGroup group) => _db.StudentGroups.Add(group);
    public void Add(StudentGroupMember member) => _db.StudentGroupMembers.Add(member);
    public void Remove(GroupSet set) => _db.GroupSets.Remove(set);
    public void Remove(StudentGroup group) => _db.StudentGroups.Remove(group);
    public void Remove(StudentGroupMember member) => _db.StudentGroupMembers.Remove(member);

    // ── peer reviews ──

    public async Task<IReadOnlyList<PeerReview>> GetPeerReviewsAsync(Guid assignmentId, CancellationToken ct = default)
        => await _db.PeerReviews.Where(r => r.AssignmentId == assignmentId).OrderBy(r => r.CreatedAt).ToListAsync(ct);

    public Task<PeerReview?> GetPeerReviewAsync(Guid peerReviewId, CancellationToken ct = default)
        => _db.PeerReviews.FirstOrDefaultAsync(r => r.Id == peerReviewId, ct);

    public Task<bool> IsReviewerOfAsync(Guid submissionId, Guid reviewerStudentProfileId, CancellationToken ct = default)
        => _db.PeerReviews.AnyAsync(r => r.SubmissionId == submissionId && r.ReviewerStudentProfileId == reviewerStudentProfileId, ct);

    public void Add(PeerReview review) => _db.PeerReviews.Add(review);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
