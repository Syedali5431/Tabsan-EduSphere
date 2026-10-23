using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Plan M8 — late &amp; missing policy, availability and per-student/group dates, student groups and peer review.
/// Lives under /api/v1/assignment so the "assignments" module licence gates it. Staff work within their tenant/campus;
/// students only in offerings they are enrolled in, and only on their own groups and reviews.
/// </summary>
[ApiController]
[Route("api/v1/assignment")]
[Authorize]
public sealed class AssignmentPolicyController : ControllerBase
{
    private const string Staff = "SuperAdmin,Admin,Faculty";

    private readonly AssignmentPolicyService _policies;
    private readonly GroupService _groups;
    private readonly PeerReviewService _peer;
    private readonly ISubmissionWorkspaceRepository _submissions;
    private readonly IAccessScopeResolver _scope;
    private readonly ApplicationDbContext _db;

    public AssignmentPolicyController(AssignmentPolicyService policies, GroupService groups, PeerReviewService peer,
        ISubmissionWorkspaceRepository submissions, IAccessScopeResolver scope, ApplicationDbContext db)
    {
        _policies = policies;
        _groups = groups;
        _peer = peer;
        _submissions = submissions;
        _scope = scope;
        _db = db;
    }

    // ── M8.1 late & missing policy (per course offering) ──

    [HttpGet("offerings/{offeringId:guid}/late-policy")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> GetLatePolicy(Guid offeringId, CancellationToken ct)
    {
        if (await DeniedAsync(offeringId, ct) is { } denied) return denied;
        return Ok(await _policies.GetLatePolicyAsync(offeringId, ct));
    }

    [HttpPut("offerings/{offeringId:guid}/late-policy")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> SaveLatePolicy(Guid offeringId, [FromBody] LatePolicyDto request, CancellationToken ct)
    {
        if (await DeniedAsync(offeringId, ct) is { } denied) return denied;
        return ToResult(await _policies.SaveLatePolicyAsync(offeringId, request, UserId(), ct));
    }

    /// <summary>Gives the missing grade now (the background worker also does this every hour).</summary>
    [HttpPost("offerings/{offeringId:guid}/apply-missing")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> ApplyMissing(Guid offeringId, [FromQuery] Guid? assignmentId, CancellationToken ct)
    {
        if (await DeniedAsync(offeringId, ct) is { } denied) return denied;
        return Ok(await _policies.ApplyMissingAsync(offeringId, assignmentId, DateTime.UtcNow, ct));
    }

    [HttpPut("submissions/{submissionId:guid}/late-penalty")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> WaiveLatePenalty(Guid submissionId, [FromBody] WaiveLatePenaltyRequest request, CancellationToken ct)
    {
        var submission = await _submissions.GetSubmissionAsync(submissionId, false, ct);
        if (submission is null) return NotFound();
        if (await DeniedAsync(submission.Assignment.CourseOfferingId, ct) is { } denied) return denied;
        return ToResult(await _policies.WaiveLatePenaltyAsync(submissionId, request.Waived, UserId(), ct));
    }

    // ── M8.2 availability, groups and peer-review options; "Assign to" overrides ──

    [HttpGet("{assignmentId:guid}/options")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> GetOptions(Guid assignmentId, CancellationToken ct)
    {
        if (await AssignmentDeniedAsync(assignmentId, ct) is { } denied) return denied;
        return await _policies.GetOptionsAsync(assignmentId, ct) is { } options ? Ok(options) : NotFound();
    }

    [HttpPut("{assignmentId:guid}/options")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> SaveOptions(Guid assignmentId, [FromBody] SaveAssignmentOptionsRequest request, CancellationToken ct)
    {
        if (await AssignmentDeniedAsync(assignmentId, ct) is { } denied) return denied;
        return ToResult(await _policies.SaveOptionsAsync(assignmentId, request, UserId(), ct));
    }

    [HttpGet("{assignmentId:guid}/overrides")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> ListOverrides(Guid assignmentId, CancellationToken ct)
    {
        if (await AssignmentDeniedAsync(assignmentId, ct) is { } denied) return denied;
        return Ok(await _policies.ListOverridesAsync(assignmentId, ct));
    }

    [HttpPost("{assignmentId:guid}/overrides")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> AddOverride(Guid assignmentId, [FromBody] AddOverrideRequest request, CancellationToken ct)
    {
        if (await AssignmentDeniedAsync(assignmentId, ct) is { } denied) return denied;
        return ToResult(await _policies.AddOverrideAsync(assignmentId, request, UserId(), ct));
    }

    [HttpDelete("overrides/{overrideId:guid}")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> RemoveOverride(Guid overrideId, CancellationToken ct)
    {
        if (await _policies.AssignmentOfOverrideAsync(overrideId, ct) is not { } assignmentId) return NotFound();
        if (await AssignmentDeniedAsync(assignmentId, ct) is { } denied) return denied;
        var result = await _policies.RemoveOverrideAsync(overrideId, UserId(), ct);
        return result.Ok ? NoContent() : Error(result.Status, result.Error);
    }

    /// <summary>The student's dates, late policy, group and peer reviews for an assignment.</summary>
    [HttpGet("{assignmentId:guid}/my-extras")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyExtras(Guid assignmentId, CancellationToken ct)
    {
        if (await StudentAsync(ct) is not { } studentId) return Forbid();
        var assignment = await _submissions.GetAssignmentAsync(assignmentId, ct);
        if (assignment is null || !assignment.IsPublished) return NotFound();
        if (!await _submissions.IsEnrolledAsync(assignment.CourseOfferingId, studentId, ct)) return Forbid();
        return await _policies.StudentExtrasAsync(assignmentId, studentId, ct) is { } extras ? Ok(extras) : NotFound();
    }

    // ── M8.3 student groups ──

    [HttpGet("offerings/{offeringId:guid}/group-sets")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> ListGroupSets(Guid offeringId, CancellationToken ct)
    {
        if (await DeniedAsync(offeringId, ct) is { } denied) return denied;
        return Ok(await _groups.ListAsync(offeringId, ct));
    }

    [HttpPost("offerings/{offeringId:guid}/group-sets")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> CreateGroupSet(Guid offeringId, [FromBody] SaveGroupSetRequest request, CancellationToken ct)
    {
        if (await DeniedAsync(offeringId, ct) is { } denied) return denied;
        return ToResult(await _groups.CreateSetAsync(offeringId, request, UserId(), ct));
    }

    [HttpPut("group-sets/{setId:guid}")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> UpdateGroupSet(Guid setId, [FromBody] SaveGroupSetRequest request, CancellationToken ct)
    {
        if (await SetDeniedAsync(setId, ct) is { } denied) return denied;
        return ToResult(await _groups.UpdateSetAsync(setId, request, UserId(), ct));
    }

    [HttpDelete("group-sets/{setId:guid}")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> DeleteGroupSet(Guid setId, CancellationToken ct)
    {
        if (await SetDeniedAsync(setId, ct) is { } denied) return denied;
        var result = await _groups.DeleteSetAsync(setId, UserId(), ct);
        return result.Ok ? NoContent() : Error(result.Status, result.Error);
    }

    [HttpPost("group-sets/{setId:guid}/groups")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> CreateGroup(Guid setId, [FromBody] CreateGroupRequest request, CancellationToken ct)
    {
        if (await SetDeniedAsync(setId, ct) is { } denied) return denied;
        return ToResult(await _groups.CreateGroupAsync(setId, request, UserId(), ct));
    }

    [HttpPost("group-sets/{setId:guid}/random-split")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> RandomSplit(Guid setId, [FromBody] RandomSplitRequest request, CancellationToken ct)
    {
        if (await SetDeniedAsync(setId, ct) is { } denied) return denied;
        return ToResult(await _groups.RandomSplitAsync(setId, request, UserId(), null, ct));
    }

    [HttpDelete("groups/{groupId:guid}")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> DeleteGroup(Guid groupId, CancellationToken ct)
    {
        if (await GroupDeniedAsync(groupId, ct) is { } denied) return denied;
        var result = await _groups.DeleteGroupAsync(groupId, UserId(), ct);
        return result.Ok ? NoContent() : Error(result.Status, result.Error);
    }

    [HttpPost("groups/{groupId:guid}/members")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> AddMember(Guid groupId, [FromBody] AddMemberRequest request, CancellationToken ct)
    {
        if (await GroupDeniedAsync(groupId, ct) is { } denied) return denied;
        var result = await _groups.AddMemberAsync(groupId, request.StudentProfileId, UserId(), ct);
        return result.Ok ? NoContent() : Error(result.Status, result.Error);
    }

    [HttpDelete("groups/{groupId:guid}/members/{studentProfileId:guid}")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> RemoveMember(Guid groupId, Guid studentProfileId, CancellationToken ct)
    {
        if (await GroupDeniedAsync(groupId, ct) is { } denied) return denied;
        var result = await _groups.RemoveMemberAsync(groupId, studentProfileId, UserId(), ct);
        return result.Ok ? NoContent() : Error(result.Status, result.Error);
    }

    [HttpGet("offerings/{offeringId:guid}/roster")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Roster(Guid offeringId, CancellationToken ct)
    {
        if (await DeniedAsync(offeringId, ct) is { } denied) return denied;
        return Ok(await _groups.RosterAsync(offeringId, ct));
    }

    [HttpGet("offerings/{offeringId:guid}/my-groups")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyGroups(Guid offeringId, CancellationToken ct)
    {
        if (await StudentAsync(ct) is not { } studentId || !await _submissions.IsEnrolledAsync(offeringId, studentId, ct)) return Forbid();
        return Ok(await _groups.MyGroupsAsync(offeringId, studentId, ct));
    }

    [HttpPost("groups/{groupId:guid}/join")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Join(Guid groupId, CancellationToken ct)
    {
        if (await StudentAsync(ct) is not { } studentId) return Forbid();
        var result = await _groups.JoinAsync(groupId, studentId, ct);
        return result.Ok ? NoContent() : Error(result.Status, result.Error);
    }

    [HttpDelete("groups/{groupId:guid}/join")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Leave(Guid groupId, CancellationToken ct)
    {
        if (await StudentAsync(ct) is not { } studentId) return Forbid();
        if (await _groups.OfferingOfGroupAsync(groupId, ct) is not { } offeringId || !await _submissions.IsEnrolledAsync(offeringId, studentId, ct)) return NotFound();
        var result = await _groups.LeaveAsync(groupId, studentId, ct);
        return result.Ok ? NoContent() : Error(result.Status, result.Error);
    }

    // ── M8.4 peer review ──

    [HttpPost("{assignmentId:guid}/peer-reviews/allocate")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Allocate(Guid assignmentId, CancellationToken ct)
    {
        if (await AssignmentDeniedAsync(assignmentId, ct) is { } denied) return denied;
        var result = await _peer.AllocateAsync(assignmentId, UserId(), null, ct);
        return result.Ok ? Ok(new { allocated = result.Value }) : Error(result.Status, result.Error);
    }

    [HttpGet("{assignmentId:guid}/peer-reviews")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> PeerOverview(Guid assignmentId, CancellationToken ct)
    {
        if (await AssignmentDeniedAsync(assignmentId, ct) is { } denied) return denied;
        return await _peer.OverviewAsync(assignmentId, ct) is { } overview ? Ok(overview) : NotFound();
    }

    [HttpGet("submissions/{submissionId:guid}/peer-reviews")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> SubmissionPeerReviews(Guid submissionId, CancellationToken ct)
    {
        var submission = await _submissions.GetSubmissionAsync(submissionId, false, ct);
        if (submission is null) return NotFound();
        if (await DeniedAsync(submission.Assignment.CourseOfferingId, ct) is { } denied) return denied;
        var (average, reviews) = await _peer.ForSubmissionAsync(submissionId, ct);
        return Ok(new { average, reviews });
    }

    [HttpGet("peer-reviews/{peerReviewId:guid}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> PeerTask(Guid peerReviewId, CancellationToken ct)
    {
        if (await StudentAsync(ct) is not { } studentId) return Forbid();
        return await _peer.TaskAsync(peerReviewId, studentId, ct) is { } task ? Ok(task) : NotFound();
    }

    [HttpPost("peer-reviews/{peerReviewId:guid}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> SubmitPeerReview(Guid peerReviewId, [FromBody] SubmitPeerReviewRequest request, CancellationToken ct)
    {
        if (await StudentAsync(ct) is not { } studentId) return Forbid();
        if (await _peer.OfferingOfReviewAsync(peerReviewId, ct) is not { } offeringId || !await _submissions.IsEnrolledAsync(offeringId, studentId, ct)) return NotFound();
        return ToResult(await _peer.SubmitAsync(peerReviewId, studentId, request, ct));
    }

    // ── helpers ──

    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    private async Task<Guid?> StudentAsync(CancellationToken ct)
    {
        if (Guid.TryParse(User.FindFirstValue("studentProfileId"), out var id)) return id;
        var userId = UserId();
        return await _db.StudentProfiles.AsNoTracking().Where(s => s.UserId == userId).Select(s => (Guid?)s.Id).FirstOrDefaultAsync(ct);
    }

    private async Task<IActionResult?> AssignmentDeniedAsync(Guid assignmentId, CancellationToken ct)
        => await _submissions.GetAssignmentAsync(assignmentId, ct) is { } a ? await DeniedAsync(a.CourseOfferingId, ct) : NotFound();

    private async Task<IActionResult?> SetDeniedAsync(Guid setId, CancellationToken ct)
        => await _groups.OfferingOfSetAsync(setId, ct) is { } offeringId ? await DeniedAsync(offeringId, ct) : NotFound();

    private async Task<IActionResult?> GroupDeniedAsync(Guid groupId, CancellationToken ct)
        => await _groups.OfferingOfGroupAsync(groupId, ct) is { } offeringId ? await DeniedAsync(offeringId, ct) : NotFound();

    /// <summary>Staff may only act inside their tenant/campus.</summary>
    private async Task<IActionResult?> DeniedAsync(Guid offeringId, CancellationToken ct)
    {
        var offering = await _db.CourseOfferings.IgnoreQueryFilters().AsNoTracking().Where(o => o.Id == offeringId)
            .Select(o => new { o.TenantId, o.CampusId }).FirstOrDefaultAsync(ct);
        if (offering is null) return NotFound();
        if (_scope.IsSuperAdmin()) return null;
        var tenant = _scope.GetTenantId();
        var campus = _scope.GetCampusId();
        if (tenant.HasValue && offering.TenantId != tenant) return Forbid();
        if (campus.HasValue && offering.CampusId != campus) return Forbid();
        return null;
    }

    private IActionResult Error(int status, string? message) => StatusCode(status, new { message });

    private IActionResult ToResult<T>(PolicyResult<T> result) => result.Ok ? StatusCode(result.Status, result.Value) : Error(result.Status, result.Error);
}
