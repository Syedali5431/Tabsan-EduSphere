using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Lms;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// Plan M7 — module items, release conditions, the student's item page and the teacher's progress report.
/// The API enforces scope, enrolment and locks; these actions render pages and relay form posts.
/// </summary>
public partial class PortalController
{
    private static readonly JsonSerializerOptions ModuleJson = new(JsonSerializerDefaults.Web);

    private IActionResult? RequireModuleStaff()
        => CanManageQuizzes(_api.GetSessionIdentity()) ? null : AccessDenied("Module content is managed by Faculty, Admin and Super Admin.");

    private IActionResult BackToLmsManage(Guid offeringId, ApiRaw result, string success)
    {
        if (result.Ok) TempData["SuccessMessage"] = success;
        else TempData["ErrorMessage"] = result.Message ?? "The change could not be saved.";
        return RedirectToAction(nameof(LmsManage), new { offeringId });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Teacher: items and release settings
    // ═════════════════════════════════════════════════════════════════════════

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddModuleItem(Guid moduleId, Guid offeringId, string? itemType, Guid? targetId, string? title, string? url, string? body,
        int indentLevel, string? requirementType, decimal? minScorePercent, CancellationToken ct)
    {
        if (RequireModuleStaff() is { } denied) return denied;
        var request = new AddModuleItemRequest(itemType ?? "", targetId, title, url, body, indentLevel, requirementType, minScorePercent);
        var result = await _api.ModuleSendAsync(HttpMethod.Post, $"modules/{moduleId}/items", JsonSerializer.Serialize(request, ModuleJson), ct);
        return BackToLmsManage(offeringId, result, "Item added.");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateModuleItem(Guid itemId, Guid offeringId, string? title, string? url, string? body, int indentLevel,
        string? requirementType, decimal? minScorePercent, bool isPublished, CancellationToken ct)
    {
        if (RequireModuleStaff() is { } denied) return denied;
        var request = new UpdateModuleItemRequest(title ?? "", url, body, indentLevel, requirementType, minScorePercent, isPublished);
        var result = await _api.ModuleSendAsync(HttpMethod.Put, $"module-items/{itemId}", JsonSerializer.Serialize(request, ModuleJson), ct);
        return BackToLmsManage(offeringId, result, "Item saved.");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveModuleItem(Guid itemId, Guid offeringId, CancellationToken ct)
    {
        if (RequireModuleStaff() is { } denied) return denied;
        return BackToLmsManage(offeringId, await _api.ModuleSendAsync(HttpMethod.Delete, $"module-items/{itemId}", null, ct), "Item removed from the module.");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveModuleItem(Guid moduleId, Guid itemId, int direction, Guid offeringId, CancellationToken ct)
    {
        if (RequireModuleStaff() is { } denied) return denied;
        var raw = await _api.ModuleSendAsync(HttpMethod.Get, $"offerings/{offeringId}/structure", null, ct);
        var module = raw.Ok ? JsonSerializer.Deserialize<ModuleStructureResponse>(raw.Body, ModuleJson)?.Modules.FirstOrDefault(m => m.Id == moduleId) : null;
        if (module is null) return BackToLmsManage(offeringId, raw.Ok ? new ApiRaw(404, "{\"message\":\"Module not found.\"}") : raw, "");
        var ids = module.Items.Select(i => i.Id).ToList();
        var index = ids.IndexOf(itemId);
        var target = index + Math.Sign(direction);
        if (index < 0 || target < 0 || target >= ids.Count) return RedirectToAction(nameof(LmsManage), new { offeringId });
        (ids[index], ids[target]) = (ids[target], ids[index]);
        var result = await _api.ModuleSendAsync(HttpMethod.Post, $"modules/{moduleId}/items/order",
            JsonSerializer.Serialize(new ReorderModuleItemsRequest(ids), ModuleJson), ct);
        return BackToLmsManage(offeringId, result, "Order saved.");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveModuleRelease(Guid moduleId, Guid offeringId, string? unlockAt, bool requireSequentialProgress, string? completionMode,
        List<Guid>? prerequisiteModuleIds, CancellationToken ct)
    {
        if (RequireModuleStaff() is { } denied) return denied;
        DateTime? unlock = DateTime.TryParse(unlockAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var u) ? u : null;
        var request = new ModuleReleaseRequest(unlock, requireSequentialProgress, completionMode, prerequisiteModuleIds ?? []);
        var result = await _api.ModuleSendAsync(HttpMethod.Put, $"modules/{moduleId}/release", JsonSerializer.Serialize(request, ModuleJson), ct);
        return BackToLmsManage(offeringId, result, "Release settings saved.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Student: items
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Opens a module item: in-portal items (page, video, link, file) render here; the rest go to their own page.</summary>
    [HttpGet("/Portal/ModuleItem/{id:guid}")]
    public async Task<IActionResult> ModuleItem(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Module item";
        if (!_api.IsConnected()) return RedirectToAction("Index", "Login", new { returnUrl = Request.Path.ToString() });
        var identity = _api.GetSessionIdentity();
        var model = new ModuleItemPageModel { IsStaff = CanManageQuizzes(identity) };
        var opened = await _api.ModuleSendAsync(HttpMethod.Post, $"module-items/{id}/open", null, ct);
        if (!opened.Ok)
        {
            model.Message = opened.Status is 403 or 404 ? "This item is not available." : opened.Message ?? "The item could not be opened.";
            return View("ModuleItem", model);
        }
        var item = JsonSerializer.Deserialize<ModuleItemOpenResponse>(opened.Body, ModuleJson)!;
        model.Item = item;
        ViewData["Title"] = item.Title;
        if (!item.Locked && item.TargetId is { } target)
        {
            switch (item.ItemType)
            {
                case "Assignment":
                    return Redirect($"/Portal/AssignmentSubmission?assignmentId={target}&moduleItem={id}");
                case "Quiz":
                    return Redirect($"/Portal/TakeQuiz?quizId={target}&moduleItem={id}");
                case "Discussion":
                    return Redirect($"/Portal/DiscussionThreadDetail?threadId={target}&offeringId={item.CourseOfferingId}&moduleItem={id}");
                case "ExternalTool":
                    return Redirect($"/lti/launch/{target}?returnUrl={Uri.EscapeDataString($"/Portal/CourseLms?offeringId={item.CourseOfferingId}")}");
                case "ScormPackage" when !model.IsStaff:
                    return Redirect($"/Portal/Scorm/{target}?moduleItem={id}");
            }
        }
        var nav = await _api.ModuleSendAsync(HttpMethod.Get, $"module-items/{id}/nav", null, ct);
        if (nav.Ok) model.Nav = JsonSerializer.Deserialize<ModuleItemNavResponse>(nav.Body, ModuleJson);
        return View("ModuleItem", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ModuleItemDone(Guid itemId, bool done, string? returnUrl, CancellationToken ct)
    {
        if (_api.GetSessionIdentity()?.IsStudent != true) return AccessDenied("Only students mark items as done.");
        var result = await _api.ModuleSendAsync(done ? HttpMethod.Post : HttpMethod.Delete, $"module-items/{itemId}/done", null, ct);
        if (!result.Ok) TempData["PortalMessage"] = result.Message ?? "The item could not be updated.";
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : Redirect($"/Portal/ModuleItem/{itemId}");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Teacher: progress report
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> ModuleProgress(Guid? offeringId, Guid? studentId, CancellationToken ct)
    {
        ViewData["Title"] = "Module Progress";
        if (RequireModuleStaff() is { } denied) return denied;
        var model = new ModuleProgressPageModel { IsConnected = _api.IsConnected(), StudentId = studentId };
        if (!model.IsConnected) return View(model);
        try
        {
            model.Offerings = await OutcomeOfferingsAsync(ct);
            model.OfferingId = offeringId ?? model.Offerings.FirstOrDefault()?.Id ?? Guid.Empty;
            if (model.OfferingId == Guid.Empty) return View(model);
            var raw = await _api.ModuleSendAsync(HttpMethod.Get, $"offerings/{model.OfferingId}/progress", null, ct);
            if (raw.Ok) model.Report = JsonSerializer.Deserialize<ModuleProgressReport>(raw.Body, ModuleJson);
            else model.Message = raw.Message ?? "Progress could not be loaded.";
            if (studentId is { } sid)
            {
                var detail = await _api.ModuleSendAsync(HttpMethod.Get, $"offerings/{model.OfferingId}/progress/{sid}", null, ct);
                if (detail.Ok) model.StudentDetail = JsonSerializer.Deserialize<StudentModulesResponse>(detail.Body, ModuleJson);
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }
}
