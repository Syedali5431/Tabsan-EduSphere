using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Lti;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// Plan M5 — External tools (LTI 1.3): the administrator's tool registry and the course card's form posts.
/// The API enforces every rule (roles, course scope, module licence); these actions render and redirect.
/// </summary>
public partial class PortalController
{
    private static readonly JsonSerializerOptions LtiJson = new(JsonSerializerDefaults.Web);

    private IActionResult? RequireLtiAdmin()
    {
        var identity = _api.GetSessionIdentity();
        return identity is not null && (identity.IsAdmin || identity.IsSuperAdmin)
            ? null : AccessDenied("External tools are managed by Admins and Super Admins.");
    }

    [HttpGet]
    public async Task<IActionResult> LtiTools(Guid? edit, CancellationToken ct)
    {
        ViewData["Title"] = "External Tools (LTI 1.3)";
        if (RequireLtiAdmin() is { } denied) return denied;
        var model = new LtiToolsPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        try
        {
            var platform = await _api.LtiSendAsync(HttpMethod.Get, "platform", null, ct);
            if (platform.Status == 403 && (platform.Message ?? "").Contains("'lti'", StringComparison.OrdinalIgnoreCase))
            {
                model.ModuleOff = true;
                return View(model);
            }
            if (!platform.Ok) { model.Message = platform.Message ?? "The tool registry could not be loaded."; return View(model); }
            model.Platform = JsonSerializer.Deserialize<LtiPlatformInfo>(platform.Body, LtiJson);

            var tools = await _api.LtiSendAsync(HttpMethod.Get, "tools", null, ct);
            if (tools.Ok) model.Tools = JsonSerializer.Deserialize<List<LtiToolResponse>>(tools.Body, LtiJson) ?? [];
            model.Editing = edit is { } id ? model.Tools.FirstOrDefault(t => t.Id == id) : null;
        }
        catch (Exception ex) { model.Message = ex.Message; }

        // Only the deployment pickers need these; the page works without them.
        if (model.Editing is not null)
        {
            try
            {
                model.Departments = await _api.GetDepartmentsAsync(ct);
                model.Offerings = (await _api.GetCourseOfferingsAsync(null, null, null, null, ct))
                    .Where(o => o.IsActive).OrderBy(o => o.CourseCode).ThenBy(o => o.SemesterName).ToList();
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Departments or course offerings could not be loaded for the LTI deployment pickers."); }
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLtiTool(LtiToolFormModel form, CancellationToken ct)
    {
        if (RequireLtiAdmin() is { } denied) return denied;
        var request = new SaveLtiToolRequest(form.Name ?? "", form.Description, form.OidcInitiationUrl ?? "", form.TargetLinkUri ?? "",
            form.DeepLinkingUrl, (form.RedirectUris ?? "").Split(['\r', '\n', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            form.JwksUrl, form.PublicKeyPem, form.CustomParameters, form.PrivacyLevel ?? "Anonymous", form.AllowGradeService, form.AllowRosterService);
        var json = JsonSerializer.Serialize(request, LtiJson);
        var result = form.Id is { } id
            ? await _api.LtiSendAsync(HttpMethod.Put, $"tools/{id}", json, ct)
            : await _api.LtiSendAsync(HttpMethod.Post, "tools", json, ct);
        if (!result.Ok)
        {
            TempData["PortalMessage"] = result.Message ?? "The tool could not be saved.";
            return RedirectToAction(nameof(LtiTools), new { edit = form.Id });
        }
        var saved = JsonSerializer.Deserialize<LtiToolResponse>(result.Body, LtiJson)!;
        TempData["PortalMessage"] = form.Id is null
            ? $"\"{saved.Name}\" registered. Give the tool its client ID {saved.ClientId} and deployment ID {saved.Deployments.FirstOrDefault()?.DeploymentId}."
            : $"\"{saved.Name}\" saved.";
        return RedirectToAction(nameof(LtiTools), new { edit = saved.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLtiToolActive(Guid toolId, bool active, CancellationToken ct)
    {
        if (RequireLtiAdmin() is { } denied) return denied;
        var result = await _api.LtiSendAsync(HttpMethod.Post, $"tools/{toolId}/{(active ? "enable" : "disable")}", null, ct);
        TempData["PortalMessage"] = result.Ok ? (active ? "Tool enabled." : "Tool disabled. Its course items stop launching.") : result.Message ?? "The tool could not be changed.";
        return RedirectToAction(nameof(LtiTools), new { edit = toolId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLtiDeployment(Guid toolId, string scope, Guid? departmentId, Guid? offeringId, CancellationToken ct)
    {
        if (RequireLtiAdmin() is { } denied) return denied;
        Guid? scopeId = scope switch { "Department" => departmentId, "CourseOffering" => offeringId, _ => null };
        var result = await _api.LtiSendAsync(HttpMethod.Post, $"tools/{toolId}/deployments",
            JsonSerializer.Serialize(new AddLtiDeploymentRequest(scope, scopeId), LtiJson), ct);
        TempData["PortalMessage"] = result.Ok ? "Deployment added." : result.Message ?? "The deployment could not be added.";
        return RedirectToAction(nameof(LtiTools), new { edit = toolId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLtiDeploymentActive(Guid toolId, Guid deploymentId, bool active, CancellationToken ct)
    {
        if (RequireLtiAdmin() is { } denied) return denied;
        var result = await _api.LtiSendAsync(HttpMethod.Post, $"tools/{toolId}/deployments/{deploymentId}/{(active ? "enable" : "disable")}", null, ct);
        TempData["PortalMessage"] = result.Ok ? (active ? "Deployment enabled." : "Deployment disabled.") : result.Message ?? "The deployment could not be changed.";
        return RedirectToAction(nameof(LtiTools), new { edit = toolId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RotateLtiKeys(CancellationToken ct)
    {
        if (_api.GetSessionIdentity()?.IsSuperAdmin != true) return AccessDenied("Only Super Admins can rotate the platform key.");
        var result = await _api.LtiSendAsync(HttpMethod.Post, "platform/rotate-keys", null, ct);
        TempData["PortalMessage"] = result.Ok
            ? "A new signing key is active. The previous key stays published for a while so tools that cached it keep working."
            : result.Message ?? "The key could not be rotated.";
        return RedirectToAction(nameof(LtiTools));
    }

    // ── Course card (staff) ─────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLtiLink(Guid offeringId, Guid toolId, string? title, string? description, string? presentationTarget,
        decimal? gradeMaximum, string? returnUrl, CancellationToken ct)
    {
        var request = new CreateLtiLinkRequest(toolId, null, title ?? "", description, null, null,
            presentationTarget == "iframe" ? "iframe" : "window", gradeMaximum is > 0 ? gradeMaximum : null);
        var result = await _api.LtiSendAsync(HttpMethod.Post, $"offerings/{offeringId}/links", JsonSerializer.Serialize(request, LtiJson), ct);
        // LmsManage shows SuccessMessage/ErrorMessage.
        if (result.Ok) TempData["SuccessMessage"] = gradeMaximum is > 0 ? "Tool added. Its scores arrive as a graded assignment." : "Tool added.";
        else TempData["ErrorMessage"] = result.Message ?? "The tool could not be added.";
        return LocalRedirectOrCourse(returnUrl, offeringId);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLtiLinkVisible(Guid linkId, bool visible, string? returnUrl, CancellationToken ct)
    {
        var result = await _api.LtiSendAsync(HttpMethod.Post, $"links/{linkId}/{(visible ? "show" : "hide")}", null, ct);
        if (result.Ok) TempData["SuccessMessage"] = visible ? "Students can see this tool again." : "Hidden from students.";
        else TempData["ErrorMessage"] = result.Message ?? "The item could not be changed.";
        return LocalRedirectOrCourse(returnUrl, null);
    }

    private IActionResult LocalRedirectOrCourse(string? returnUrl, Guid? offeringId)
        => Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!)
            : offeringId is { } id ? RedirectToAction(nameof(LmsManage), new { offeringId = id }) : RedirectToAction(nameof(Dashboard));
}
