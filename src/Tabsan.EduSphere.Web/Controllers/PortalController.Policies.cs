using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Assignments;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// Plan M8 — late &amp; missing policy, student groups, assignment options ("Assign to" dates, group work, peer review)
/// and the student's groups and peer reviews. The API enforces every rule (scope, enrolment, membership, reviewer);
/// these actions render the pages and pass form posts on.
/// </summary>
public partial class PortalController
{
    private static DateTime? UtcInput(string? value)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d) ? d : null;

    private void Report(ApiRaw result, string success)
        => TempData["PortalMessage"] = result.Ok ? success : result.Message ?? "That did not work.";

    // ═════════════════════════════════════════════════════════════════════════
    // Staff: late policy and groups (per course offering)
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> AssignmentPolicies(Guid? offeringId, CancellationToken ct)
    {
        ViewData["Title"] = "Late policy & groups";
        if (RequireGradingStaff() is { } denied) return denied;
        var model = new AssignmentPoliciesPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        try
        {
            model.Offerings = (await OutcomeOfferingsAsync(ct)).Select(o => (o.Id, o.Name)).ToList();
            model.OfferingId = offeringId ?? model.Offerings.FirstOrDefault().Id;
            if (model.OfferingId != Guid.Empty)
            {
                if (!model.Offerings.Any(o => o.Id == model.OfferingId)) model.Offerings.Insert(0, (model.OfferingId, "Selected course"));
                model.LatePolicy = await _api.GetLatePolicyAsync(model.OfferingId, ct);
                model.GroupSets = await _api.GetGroupSetsAsync(model.OfferingId, ct);
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLatePolicy(Guid offeringId, bool lateDeductionEnabled, decimal deductionPercent, string? interval,
        decimal lowestGradePercent, bool missingEnabled, decimal missingGradePercent, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        Report(await _api.AssignmentPolicySendAsync(HttpMethod.Put, $"offerings/{offeringId}/late-policy",
            new LatePolicyDto(lateDeductionEnabled, deductionPercent, interval ?? "Day", lowestGradePercent, missingEnabled, missingGradePercent), ct),
            "Late and missing policy saved.");
        return RedirectToAction(nameof(AssignmentPolicies), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyMissingGrades(Guid offeringId, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        var result = await _api.AssignmentPolicySendAsync(HttpMethod.Post, $"offerings/{offeringId}/apply-missing", null, ct);
        var graded = result.Ok ? ReadJson<MissingRunResult>(result)?.Graded ?? 0 : 0;
        Report(result, graded == 0 ? "No missing work needed a grade." : $"{graded} missing submission(s) graded.");
        return RedirectToAction(nameof(AssignmentPolicies), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroupSet(Guid offeringId, string? name, bool selfSignup, int? maxSize, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        Report(await _api.AssignmentPolicySendAsync(HttpMethod.Post, $"offerings/{offeringId}/group-sets", new SaveGroupSetRequest(name ?? string.Empty, selfSignup, maxSize), ct),
            $"Group set “{name}” created.");
        return RedirectToAction(nameof(AssignmentPolicies), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateGroupSet(Guid setId, Guid offeringId, string? name, bool selfSignup, int? maxSize, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        Report(await _api.AssignmentPolicySendAsync(HttpMethod.Put, $"group-sets/{setId}", new SaveGroupSetRequest(name ?? string.Empty, selfSignup, maxSize), ct), "Group set saved.");
        return RedirectToAction(nameof(AssignmentPolicies), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGroupSet(Guid setId, Guid offeringId, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        Report(await _api.AssignmentPolicySendAsync(HttpMethod.Delete, $"group-sets/{setId}", null, ct), "Group set deleted.");
        return RedirectToAction(nameof(AssignmentPolicies), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStudentGroup(Guid setId, Guid offeringId, string? name, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        Report(await _api.AssignmentPolicySendAsync(HttpMethod.Post, $"group-sets/{setId}/groups", new CreateGroupRequest(name ?? string.Empty), ct), $"Group “{name}” created.");
        return RedirectToAction(nameof(AssignmentPolicies), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStudentGroup(Guid groupId, Guid offeringId, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        Report(await _api.AssignmentPolicySendAsync(HttpMethod.Delete, $"groups/{groupId}", null, ct), "Group deleted.");
        return RedirectToAction(nameof(AssignmentPolicies), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SplitGroupSet(Guid setId, Guid offeringId, string? splitBy, int? splitValue, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        var request = splitBy == "size" ? new RandomSplitRequest(null, splitValue) : new RandomSplitRequest(splitValue, null);
        Report(await _api.AssignmentPolicySendAsync(HttpMethod.Post, $"group-sets/{setId}/random-split", request, ct), "Students split into groups at random.");
        return RedirectToAction(nameof(AssignmentPolicies), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddGroupMember(Guid groupId, Guid offeringId, Guid studentProfileId, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        Report(await _api.AssignmentPolicySendAsync(HttpMethod.Post, $"groups/{groupId}/members", new AddMemberRequest(studentProfileId), ct), "Student added to the group.");
        return RedirectToAction(nameof(AssignmentPolicies), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveGroupMember(Guid groupId, Guid offeringId, Guid studentProfileId, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        Report(await _api.AssignmentPolicySendAsync(HttpMethod.Delete, $"groups/{groupId}/members/{studentProfileId}", null, ct), "Student removed from the group.");
        return RedirectToAction(nameof(AssignmentPolicies), new { offeringId });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Staff: assignment options, "Assign to" dates, peer review
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> AssignmentOptions(Guid assignmentId, CancellationToken ct)
    {
        ViewData["Title"] = "Assignment options";
        if (RequireGradingStaff() is { } denied) return denied;
        var model = new AssignmentOptionsPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString(), AssignmentId = assignmentId };
        if (!model.IsConnected) return View(model);
        try
        {
            var workspace = await _api.GetGradingWorkspaceAsync(assignmentId, null, ct);
            model.Options = await _api.GetAssignmentOptionsAsync(assignmentId, ct);
            if (workspace is null || model.Options is null) { TempData["PortalMessage"] = "This assignment was not found."; return RedirectToAction(nameof(Assignments)); }
            model.Title = workspace.Title;
            model.OfferingId = workspace.CourseOfferingId;
            model.MaxMarks = workspace.MaxMarks;
            ViewData["Title"] = $"Options — {workspace.Title}";
            model.Overrides = await _api.GetAssignmentOverridesAsync(assignmentId, ct);
            model.GroupSets = await _api.GetGroupSetsAsync(model.OfferingId, ct);
            model.Roster = await _api.GetCourseRosterAsync(model.OfferingId, ct);
            if (model.Options.PeerReviewCount > 0) model.PeerReviews = await _api.GetPeerReviewOverviewAsync(assignmentId, ct);
            model.Similarity = await AssignmentSimilarityAsync(assignmentId, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAssignmentOptions(Guid assignmentId, string? availableFrom, string? availableUntil, Guid? groupSetId,
        bool gradeGroupIndividually, int peerReviewCount, string? peerReviewDueDate, bool peerReviewAnonymous, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        Report(await _api.AssignmentPolicySendAsync(HttpMethod.Put, $"{assignmentId}/options", new SaveAssignmentOptionsRequest(
            UtcInput(availableFrom), UtcInput(availableUntil), groupSetId == Guid.Empty ? null : groupSetId, gradeGroupIndividually,
            peerReviewCount, UtcInput(peerReviewDueDate), peerReviewAnonymous), ct), "Assignment options saved.");
        return RedirectToAction(nameof(AssignmentOptions), new { assignmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAssignmentOverride(Guid assignmentId, string? target, string? dueDate, string? availableFrom, string? availableUntil,
        string? note, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        Guid? student = null, group = null;
        if (target is { Length: > 2 } && Guid.TryParse(target[2..], out var targetId))
        {
            if (target.StartsWith("s:")) student = targetId;
            else if (target.StartsWith("g:")) group = targetId;
        }
        if ((student is null && group is null) || UtcInput(dueDate) is not { } due)
            TempData["PortalMessage"] = "Choose a student or group and a due date.";
        else
            Report(await _api.AssignmentPolicySendAsync(HttpMethod.Post, $"{assignmentId}/overrides",
                new AddOverrideRequest(student, group, due, UtcInput(availableFrom), UtcInput(availableUntil), note), ct), "Dates added.");
        return RedirectToAction(nameof(AssignmentOptions), new { assignmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAssignmentOverride(Guid assignmentId, Guid overrideId, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        Report(await _api.AssignmentPolicySendAsync(HttpMethod.Delete, $"overrides/{overrideId}", null, ct), "Dates removed.");
        return RedirectToAction(nameof(AssignmentOptions), new { assignmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AllocatePeerReviews(Guid assignmentId, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        var result = await _api.AssignmentPolicySendAsync(HttpMethod.Post, $"{assignmentId}/peer-reviews/allocate", null, ct);
        var count = 0;
        if (result.Ok)
        {
            using var doc = System.Text.Json.JsonDocument.Parse(result.Body);
            count = doc.RootElement.GetProperty("allocated").GetInt32();
        }
        Report(result, $"{count} peer review(s) assigned.");
        return RedirectToAction(nameof(AssignmentOptions), new { assignmentId });
    }

    /// <summary>Workspace script: waive or restore a late deduction.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> WorkspaceLatePenalty(Guid submissionId, CancellationToken ct)
    {
        if (RequireGradingStaff() is not null) return Forbid();
        return await RelayJsonAsync(HttpMethod.Put, $"submissions/{submissionId}/late-penalty", ct);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Students: groups and peer reviews
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> MyGroups(Guid? offeringId, CancellationToken ct)
    {
        ViewData["Title"] = "My groups";
        if (RequireStudent() is { } denied) return denied;
        var model = new MyGroupsPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        try
        {
            model.Offerings = (await _api.GetMyOfferingsAsync(ct)).Select(o => (o.Id, o.Name)).ToList();
            model.OfferingId = offeringId ?? model.Offerings.FirstOrDefault().Id;
            if (model.OfferingId != Guid.Empty) model.Groups = await _api.GetMyGroupsAsync(model.OfferingId, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> JoinGroup(Guid groupId, Guid offeringId, CancellationToken ct)
    {
        if (RequireStudent() is { } denied) return denied;
        Report(await _api.AssignmentPolicySendAsync(HttpMethod.Post, $"groups/{groupId}/join", null, ct), "You joined the group.");
        return RedirectToAction(nameof(MyGroups), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LeaveGroup(Guid groupId, Guid offeringId, CancellationToken ct)
    {
        if (RequireStudent() is { } denied) return denied;
        Report(await _api.AssignmentPolicySendAsync(HttpMethod.Delete, $"groups/{groupId}/join", null, ct), "You left the group.");
        return RedirectToAction(nameof(MyGroups), new { offeringId });
    }

    [HttpGet]
    public async Task<IActionResult> PeerReview(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Peer review";
        if (RequireStudent() is { } denied) return denied;
        var model = new PeerReviewPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        try
        {
            model.Task = await _api.GetPeerReviewTaskAsync(id, ct);
            if (model.Task is null) { TempData["PortalMessage"] = "This review is not available."; return RedirectToAction(nameof(Assignments)); }
            ViewData["Title"] = $"Peer review — {model.Task.AssignmentTitle}";
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitPeerReview(Guid id, Guid assignmentId, string? score, string? comment, Dictionary<Guid, Guid>? rubric, CancellationToken ct)
    {
        if (RequireStudent() is { } denied) return denied;
        decimal? points = decimal.TryParse(score, NumberStyles.Number, CultureInfo.InvariantCulture, out var p) ? p : null;
        var choices = rubric?.Where(kv => kv.Value != Guid.Empty).Select(kv => new PeerRubricChoice(kv.Key, kv.Value)).ToList();
        var result = await _api.AssignmentPolicySendAsync(HttpMethod.Post, $"peer-reviews/{id}",
            new SubmitPeerReviewRequest(choices is { Count: > 0 } ? null : points, comment, choices), ct);
        Report(result, "Your review has been saved. Thank you!");
        return result.Ok ? RedirectToAction(nameof(AssignmentSubmission), new { assignmentId }) : RedirectToAction(nameof(PeerReview), new { id });
    }
}
