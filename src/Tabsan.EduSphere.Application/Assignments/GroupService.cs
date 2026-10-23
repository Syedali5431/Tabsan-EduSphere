using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Assignments;

/// <summary>
/// Plan M8.3 — group sets and groups in a course: teachers create groups by hand, split students at random or let them
/// sign up; a student belongs to at most one group per set. Groups are used by group assignments, group dates and group
/// discussions.
/// </summary>
public sealed class GroupService
{
    private readonly IAssignmentPolicyRepository _repo;
    private readonly ISubmissionWorkspaceRepository _students;
    private readonly IAuditService _audit;

    public GroupService(IAssignmentPolicyRepository repo, ISubmissionWorkspaceRepository students, IAuditService audit)
    {
        _repo = repo;
        _students = students;
        _audit = audit;
    }

    public async Task<IReadOnlyList<GroupSetDto>> ListAsync(Guid offeringId, CancellationToken ct = default)
    {
        var sets = await _repo.GetGroupSetsAsync(offeringId, ct);
        if (sets.Count == 0) return [];
        var ids = sets.Select(s => s.Id).ToList();
        var groups = await _repo.GetGroupsAsync(ids, ct);
        var members = await _repo.GetMembersAsync(ids, ct);
        var enrolled = await _repo.GetEnrolledStudentIdsAsync(offeringId, ct);
        var names = await _students.GetStudentNamesAsync(enrolled.Concat(members.Select(m => m.StudentProfileId)).Distinct().ToList(), ct);
        GroupMemberDto Member(Guid id) => new(id, names.TryGetValue(id, out var n) ? n.Name : "Student", names.TryGetValue(id, out var n2) ? n2.RegistrationNumber : null);
        return sets.OrderBy(s => s.Name).Select(s =>
        {
            var setMembers = members.Where(m => m.GroupSetId == s.Id).ToList();
            var inGroups = setMembers.Select(m => m.StudentProfileId).ToHashSet();
            return new GroupSetDto(s.Id, s.Name, s.SelfSignup, s.MaxSize,
                groups.Where(g => g.GroupSetId == s.Id).OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase).Select(g =>
                {
                    var list = setMembers.Where(m => m.GroupId == g.Id).Select(m => Member(m.StudentProfileId)).OrderBy(m => m.Name).ToList();
                    return new GroupDto(g.Id, g.Name, list.Count, list);
                }).ToList(),
                enrolled.Where(id => !inGroups.Contains(id)).Select(Member).OrderBy(m => m.Name).ToList());
        }).ToList();
    }

    public async Task<PolicyResult<GroupSetDto>> CreateSetAsync(Guid offeringId, SaveGroupSetRequest r, Guid actor, CancellationToken ct = default)
    {
        GroupSet set;
        try { set = new GroupSet(offeringId, r.Name, r.SelfSignup, r.MaxSize); }
        catch (ArgumentException ex) { return PolicyResult<GroupSetDto>.Fail(400, ex.Message); }
        if ((await _repo.GetGroupSetsAsync(offeringId, ct)).Any(s => string.Equals(s.Name, set.Name, StringComparison.OrdinalIgnoreCase)))
            return PolicyResult<GroupSetDto>.Fail(400, "A group set with this name already exists.");
        _repo.Add(set);
        await _repo.SaveChangesAsync(ct);
        await Audit("GroupSetCreated", set.Id, actor, ct);
        return PolicyResult<GroupSetDto>.Success((await ListAsync(offeringId, ct)).First(s => s.Id == set.Id), 201);
    }

    public async Task<PolicyResult<GroupSetDto>> UpdateSetAsync(Guid setId, SaveGroupSetRequest r, Guid actor, CancellationToken ct = default)
    {
        var set = await _repo.GetGroupSetAsync(setId, ct);
        if (set is null) return PolicyResult<GroupSetDto>.Fail(404, "Group set not found.");
        try { set.Update(r.Name, r.SelfSignup, r.MaxSize); }
        catch (ArgumentException ex) { return PolicyResult<GroupSetDto>.Fail(400, ex.Message); }
        await _repo.SaveChangesAsync(ct);
        await Audit("GroupSetUpdated", set.Id, actor, ct);
        return PolicyResult<GroupSetDto>.Success((await ListAsync(set.CourseOfferingId, ct)).First(s => s.Id == set.Id));
    }

    public async Task<PolicyResult<bool>> DeleteSetAsync(Guid setId, Guid actor, CancellationToken ct = default)
    {
        var set = await _repo.GetGroupSetAsync(setId, ct);
        if (set is null) return PolicyResult<bool>.Fail(404, "Group set not found.");
        foreach (var g in await _repo.GetGroupsAsync([setId], ct))
            if (await _repo.GroupHasSubmissionsAsync(g.Id, ct)) return PolicyResult<bool>.Fail(409, "A group in this set has handed in work, so the set is kept.");
        if ((await _repo.GetGradableAssignmentsAsync(set.CourseOfferingId, ct)).Any(a => a.GroupSetId == setId))
            return PolicyResult<bool>.Fail(409, "A group assignment uses this set.");
        foreach (var m in await _repo.GetMembersAsync([setId], ct)) _repo.Remove(m);
        foreach (var g in await _repo.GetGroupsAsync([setId], ct)) _repo.Remove(g);
        _repo.Remove(set);
        await _repo.SaveChangesAsync(ct);
        await Audit("GroupSetDeleted", setId, actor, ct);
        return PolicyResult<bool>.Success(true);
    }

    public async Task<PolicyResult<GroupSetDto>> CreateGroupAsync(Guid setId, CreateGroupRequest r, Guid actor, CancellationToken ct = default)
    {
        var set = await _repo.GetGroupSetAsync(setId, ct);
        if (set is null) return PolicyResult<GroupSetDto>.Fail(404, "Group set not found.");
        StudentGroup group;
        try { group = new StudentGroup(setId, r.Name); }
        catch (ArgumentException ex) { return PolicyResult<GroupSetDto>.Fail(400, ex.Message); }
        if ((await _repo.GetGroupsAsync([setId], ct)).Any(g => string.Equals(g.Name, group.Name, StringComparison.OrdinalIgnoreCase)))
            return PolicyResult<GroupSetDto>.Fail(400, "A group with this name already exists in the set.");
        _repo.Add(group);
        await _repo.SaveChangesAsync(ct);
        await Audit("GroupCreated", group.Id, actor, ct);
        return PolicyResult<GroupSetDto>.Success((await ListAsync(set.CourseOfferingId, ct)).First(s => s.Id == setId), 201);
    }

    public async Task<PolicyResult<bool>> DeleteGroupAsync(Guid groupId, Guid actor, CancellationToken ct = default)
    {
        var group = await _repo.GetGroupAsync(groupId, ct);
        if (group is null) return PolicyResult<bool>.Fail(404, "Group not found.");
        if (await _repo.GroupHasSubmissionsAsync(groupId, ct)) return PolicyResult<bool>.Fail(409, "This group has handed in work, so it is kept.");
        foreach (var m in (await _repo.GetMembersAsync([group.GroupSetId], ct)).Where(m => m.GroupId == groupId)) _repo.Remove(m);
        _repo.Remove(group);
        await _repo.SaveChangesAsync(ct);
        await Audit("GroupDeleted", groupId, actor, ct);
        return PolicyResult<bool>.Success(true);
    }

    /// <summary>Puts every student who is not yet in a group of the set into new groups (by count or by size).</summary>
    public async Task<PolicyResult<GroupSetDto>> RandomSplitAsync(Guid setId, RandomSplitRequest r, Guid actor, Random? random = null, CancellationToken ct = default)
    {
        var set = await _repo.GetGroupSetAsync(setId, ct);
        if (set is null) return PolicyResult<GroupSetDto>.Fail(404, "Group set not found.");
        var members = await _repo.GetMembersAsync([setId], ct);
        var placed = members.Select(m => m.StudentProfileId).ToHashSet();
        var students = (await _repo.GetEnrolledStudentIdsAsync(set.CourseOfferingId, ct)).Where(s => !placed.Contains(s)).ToList();
        if (students.Count == 0) return PolicyResult<GroupSetDto>.Fail(409, "Every student is already in a group of this set.");
        int count;
        if (r.GroupCount is { } c) count = c;
        else if (r.GroupSize is { } size and > 0) count = (int)Math.Ceiling(students.Count / (double)size);
        else return PolicyResult<GroupSetDto>.Fail(400, "Give the number of groups or the group size.");
        if (count < 1 || count > students.Count) return PolicyResult<GroupSetDto>.Fail(400, $"Make between 1 and {students.Count} groups.");

        var rng = random ?? Random.Shared;
        var shuffled = students.OrderBy(_ => rng.Next()).ToList();
        var existing = (await _repo.GetGroupsAsync([setId], ct)).Select(g => g.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var next = existing.Count + 1;
        var groups = new List<StudentGroup>();
        for (var i = 0; i < count; i++)
        {
            while (existing.Contains($"Group {next}")) next++;
            var g = new StudentGroup(setId, $"Group {next++}");
            groups.Add(g);
            _repo.Add(g);
        }
        for (var i = 0; i < shuffled.Count; i++) _repo.Add(new StudentGroupMember(groups[i % count].Id, setId, shuffled[i]));
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("GroupsRandomSplit", "GroupSet", setId.ToString(), actorUserId: actor,
            newValuesJson: $"{{\"groups\":{count},\"students\":{shuffled.Count}}}"), ct);
        return PolicyResult<GroupSetDto>.Success((await ListAsync(set.CourseOfferingId, ct)).First(s => s.Id == setId));
    }

    /// <summary>Teacher: puts a student in a group (moving them out of their current group of the same set).</summary>
    public async Task<PolicyResult<bool>> AddMemberAsync(Guid groupId, Guid studentProfileId, Guid actor, CancellationToken ct = default)
    {
        var group = await _repo.GetGroupAsync(groupId, ct);
        var set = group is null ? null : await _repo.GetGroupSetAsync(group.GroupSetId, ct);
        if (group is null || set is null) return PolicyResult<bool>.Fail(404, "Group not found.");
        if (!await _students.IsEnrolledAsync(set.CourseOfferingId, studentProfileId, ct)) return PolicyResult<bool>.Fail(400, "The student is not enrolled in this course.");
        var result = await MoveAsync(set, group, studentProfileId, enforceSize: false, ct);
        if (result.Ok) await Audit("GroupMemberAdded", groupId, actor, ct);
        return result;
    }

    public async Task<PolicyResult<bool>> RemoveMemberAsync(Guid groupId, Guid studentProfileId, Guid actor, CancellationToken ct = default)
    {
        var group = await _repo.GetGroupAsync(groupId, ct);
        if (group is null) return PolicyResult<bool>.Fail(404, "Group not found.");
        var member = (await _repo.GetMembersAsync([group.GroupSetId], ct)).FirstOrDefault(m => m.GroupId == groupId && m.StudentProfileId == studentProfileId);
        if (member is null) return PolicyResult<bool>.Fail(404, "The student is not in this group.");
        _repo.Remove(member);
        await _repo.SaveChangesAsync(ct);
        await Audit("GroupMemberRemoved", groupId, actor, ct);
        return PolicyResult<bool>.Success(true);
    }

    /// <summary>The course's enrolled students (for choosing members and "Assign to" targets).</summary>
    public async Task<IReadOnlyList<GroupMemberDto>> RosterAsync(Guid offeringId, CancellationToken ct = default)
    {
        var ids = await _repo.GetEnrolledStudentIdsAsync(offeringId, ct);
        var names = await _students.GetStudentNamesAsync(ids, ct);
        return ids.Select(id => names.TryGetValue(id, out var n) ? new GroupMemberDto(id, n.Name, n.RegistrationNumber) : new GroupMemberDto(id, "Student", null))
            .OrderBy(m => m.Name).ToList();
    }

    // ── students ──

    public async Task<IReadOnlyList<MyGroupDto>> MyGroupsAsync(Guid offeringId, Guid studentProfileId, CancellationToken ct = default)
    {
        var sets = await ListAsync(offeringId, ct);
        return sets.Select(s =>
        {
            var mine = s.Groups.FirstOrDefault(g => g.Members.Any(m => m.StudentProfileId == studentProfileId));
            var joinable = mine is null && s.SelfSignup ? s.Groups.Where(g => s.MaxSize is null || g.MemberCount < s.MaxSize).ToList() : [];
            return new MyGroupDto(s.Id, s.Name, s.SelfSignup, s.MaxSize, mine, joinable);
        }).ToList();
    }

    public async Task<PolicyResult<bool>> JoinAsync(Guid groupId, Guid studentProfileId, CancellationToken ct = default)
    {
        var group = await _repo.GetGroupAsync(groupId, ct);
        var set = group is null ? null : await _repo.GetGroupSetAsync(group.GroupSetId, ct);
        if (group is null || set is null) return PolicyResult<bool>.Fail(404, "Group not found.");
        if (!set.SelfSignup) return PolicyResult<bool>.Fail(403, "Your teacher assigns these groups.");
        if (!await _students.IsEnrolledAsync(set.CourseOfferingId, studentProfileId, ct)) return PolicyResult<bool>.Fail(403, "You are not enrolled in this course.");
        if (await CurrentMembershipAsync(set.Id, studentProfileId, ct) is { } current && await _repo.GroupHasSubmissionsAsync(current.GroupId, ct))
            return PolicyResult<bool>.Fail(409, "Your group has handed in work, so you cannot change groups.");
        return await MoveAsync(set, group, studentProfileId, enforceSize: true, ct);
    }

    public async Task<PolicyResult<bool>> LeaveAsync(Guid groupId, Guid studentProfileId, CancellationToken ct = default)
    {
        var group = await _repo.GetGroupAsync(groupId, ct);
        var set = group is null ? null : await _repo.GetGroupSetAsync(group.GroupSetId, ct);
        if (group is null || set is null) return PolicyResult<bool>.Fail(404, "Group not found.");
        if (!set.SelfSignup) return PolicyResult<bool>.Fail(403, "Your teacher assigns these groups.");
        if (await _repo.GroupHasSubmissionsAsync(groupId, ct)) return PolicyResult<bool>.Fail(409, "Your group has handed in work, so you cannot leave it.");
        var member = (await _repo.GetMembersAsync([set.Id], ct)).FirstOrDefault(m => m.GroupId == groupId && m.StudentProfileId == studentProfileId);
        if (member is null) return PolicyResult<bool>.Fail(404, "You are not in this group.");
        _repo.Remove(member);
        await _repo.SaveChangesAsync(ct);
        return PolicyResult<bool>.Success(true);
    }

    public async Task<Guid?> OfferingOfSetAsync(Guid setId, CancellationToken ct = default) => (await _repo.GetGroupSetAsync(setId, ct))?.CourseOfferingId;

    public async Task<Guid?> OfferingOfGroupAsync(Guid groupId, CancellationToken ct = default)
        => await _repo.GetGroupAsync(groupId, ct) is { } g ? (await _repo.GetGroupSetAsync(g.GroupSetId, ct))?.CourseOfferingId : null;

    private async Task<StudentGroupMember?> CurrentMembershipAsync(Guid setId, Guid studentProfileId, CancellationToken ct)
        => (await _repo.GetMembersAsync([setId], ct)).FirstOrDefault(m => m.StudentProfileId == studentProfileId);

    private async Task<PolicyResult<bool>> MoveAsync(GroupSet set, StudentGroup group, Guid studentProfileId, bool enforceSize, CancellationToken ct)
    {
        var members = await _repo.GetMembersAsync([set.Id], ct);
        if (members.Any(m => m.GroupId == group.Id && m.StudentProfileId == studentProfileId)) return PolicyResult<bool>.Success(true);
        if (enforceSize && set.MaxSize is { } max && members.Count(m => m.GroupId == group.Id) >= max) return PolicyResult<bool>.Fail(409, "This group is full.");
        foreach (var old in members.Where(m => m.StudentProfileId == studentProfileId)) _repo.Remove(old);
        _repo.Add(new StudentGroupMember(group.Id, set.Id, studentProfileId));
        await _repo.SaveChangesAsync(ct);
        return PolicyResult<bool>.Success(true);
    }

    private Task Audit(string action, Guid id, Guid actor, CancellationToken ct) => _audit.LogAsync(new AuditLog(action, "StudentGroup", id.ToString(), actorUserId: actor), ct);
}
