using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Lms.Scorm;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// Plan M13 — SCORM packages (teachers upload and manage them on the course content page; students open them from the
/// course or its modules) and the xAPI Learning Record Store credentials. The API checks scope, enrolment and locks.
/// </summary>
public partial class PortalController
{
    private const long ScormUploadLimit = 1_100_000_000;
    private const string ScormTokenKey = "ScormPlayToken";
    private static readonly JsonSerializerOptions ScormJson = new(JsonSerializerDefaults.Web);

    private IActionResult? RequireScormStaff()
        => CanManageQuizzes(_api.GetSessionIdentity()) ? null : AccessDenied("SCORM packages are managed by Faculty, Admin and Super Admin.");

    private IActionResult BackToScormPackages(Guid offeringId, ApiRaw result, string success)
    {
        if (result.Ok) TempData["SuccessMessage"] = success;
        else TempData["ErrorMessage"] = result.Message ?? "The change could not be saved.";
        return Redirect(Url.Action(nameof(LmsManage), new { offeringId }) + "#scorm-packages");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Teacher
    // ═════════════════════════════════════════════════════════════════════════

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(ScormUploadLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = ScormUploadLimit)]
    public async Task<IActionResult> UploadScormPackage(Guid offeringId, IFormFile? file, string? title, bool graded, decimal? maxMarks, int maxAttempts,
        bool publish, CancellationToken ct)
    {
        if (RequireScormStaff() is { } denied) return denied;
        if (file is null || file.Length == 0)
            return BackToScormPackages(offeringId, new ApiRaw(400, "{\"message\":\"Choose the package's zip file.\"}"), "");
        var result = await _api.ScormUploadAsync(offeringId, file, title, graded, graded ? maxMarks : null, maxAttempts, publish, ct);
        return BackToScormPackages(offeringId, result, "Package uploaded.");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateScormPackage(Guid packageId, Guid offeringId, string? title, int maxAttempts, bool isPublished, bool graded,
        decimal? maxMarks, CancellationToken ct)
    {
        if (RequireScormStaff() is { } denied) return denied;
        var request = new ScormUpdateRequest(title ?? "", maxAttempts, isPublished, graded, maxMarks);
        var result = await _api.ScormSendAsync(HttpMethod.Put, $"packages/{packageId}", JsonSerializer.Serialize(request, ScormJson), ct);
        return BackToScormPackages(offeringId, result, "Package saved.");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveScormPackage(Guid packageId, Guid offeringId, CancellationToken ct)
    {
        if (RequireScormStaff() is { } denied) return denied;
        var result = await _api.ScormSendAsync(HttpMethod.Delete, $"packages/{packageId}", null, ct);
        return BackToScormPackages(offeringId, result, "Package removed.");
    }

    /// <summary>Opens the package for the teacher in review mode (nothing is recorded).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PreviewScorm(Guid packageId, Guid offeringId, CancellationToken ct)
    {
        if (RequireScormStaff() is { } denied) return denied;
        var result = await _api.ScormSendAsync(HttpMethod.Post, $"packages/{packageId}/preview", null, ct);
        if (!result.Ok) return BackToScormPackages(offeringId, result, "");
        TempData[ScormTokenKey] = result.Body;
        return RedirectToAction(nameof(ScormPlay), new { id = packageId, offeringId });
    }

    [HttpGet]
    public async Task<IActionResult> ScormReport(Guid packageId, CancellationToken ct)
    {
        ViewData["Title"] = "SCORM Package Report";
        if (RequireScormStaff() is { } denied) return denied;
        var model = new ScormReportPageModel();
        var raw = await _api.ScormSendAsync(HttpMethod.Get, $"packages/{packageId}/report", null, ct);
        if (raw.Ok) model.Report = JsonSerializer.Deserialize<ScormReportResponse>(raw.Body, ScormJson);
        else model.Message = raw.Message ?? "The report could not be loaded.";
        return View(model);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Student
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet("/Portal/Scorm/{id:guid}")]
    public async Task<IActionResult> Scorm(Guid id, Guid? moduleItem, CancellationToken ct)
    {
        ViewData["Title"] = "SCORM Package";
        if (!_api.IsConnected()) return RedirectToAction("Index", "Login", new { returnUrl = Request.Path + Request.QueryString });
        var model = new ScormPageModel { ModuleItemId = moduleItem, Message = TempData["ScormMessage"]?.ToString() };
        if (_api.GetSessionIdentity()?.IsStudent != true)
        {
            model.Message = "Students open packages here. Teachers preview them from the course content page.";
            return View(model);
        }
        var raw = await _api.ScormSendAsync(HttpMethod.Get, $"packages/{id}/mine", null, ct);
        if (raw.Ok)
        {
            model.Package = JsonSerializer.Deserialize<ScormPackageResponse>(raw.Body, ScormJson);
            ViewData["Title"] = model.Package?.Title ?? "SCORM Package";
        }
        else model.Message = raw.Status is 403 or 404 ? "This package is not available." : raw.Message ?? "The package could not be loaded.";
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LaunchScorm(Guid packageId, bool newAttempt, Guid? moduleItem, CancellationToken ct)
    {
        if (_api.GetSessionIdentity()?.IsStudent != true) return AccessDenied("Only students take SCORM packages; teachers use Preview.");
        var result = await _api.ScormSendAsync(HttpMethod.Post, $"packages/{packageId}/launch",
            JsonSerializer.Serialize(new ScormLaunchRequest(newAttempt), ScormJson), ct);
        if (!result.Ok)
        {
            TempData["ScormMessage"] = result.Message ?? "The package could not be opened.";
            return Redirect($"/Portal/Scorm/{packageId}" + (moduleItem is { } m ? $"?moduleItem={m}" : ""));
        }
        TempData[ScormTokenKey] = result.Body;
        return RedirectToAction(nameof(ScormPlay), new { id = packageId, moduleItem });
    }

    /// <summary>The page around the player. The play link comes once from the launch (a refresh goes back to the package page).</summary>
    [HttpGet("/Portal/ScormPlay/{id:guid}")]
    public IActionResult ScormPlay(Guid id, Guid? offeringId, Guid? moduleItem)
    {
        ViewData["Title"] = "SCORM Package";
        var isStaff = CanManageQuizzes(_api.GetSessionIdentity());
        if (TempData[ScormTokenKey] is not string json || JsonSerializer.Deserialize<ScormLaunchResponse>(json, ScormJson) is not { } launch)
            return isStaff && offeringId is { } o ? Redirect(Url.Action(nameof(LmsManage), new { offeringId = o }) + "#scorm-packages")
                : Redirect($"/Portal/Scorm/{id}" + (moduleItem is { } m ? $"?moduleItem={m}" : ""));
        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var origin = (config["Scorm:ContentOrigin"] ?? "").TrimEnd('/');
        ViewData["Title"] = launch.Title;
        return View(new ScormPlayerPageModel
        {
            PackageId = id,
            OfferingId = offeringId ?? Guid.Empty,
            Title = launch.Title,
            Mode = launch.Mode,
            AttemptNumber = launch.AttemptNumber,
            PlayerUrl = $"{origin}/scorm/play/{launch.Token}/",
            IsStaff = isStaff,
            ModuleItemId = moduleItem
        });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Administrator: xAPI Learning Record Store
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> XapiSettings(CancellationToken ct)
    {
        ViewData["Title"] = "Learning Record Store (xAPI)";
        if (!IsAdminOrSuperAdminUser()) return AccessDenied("The Learning Record Store is managed by Admin and Super Admin.");
        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var model = new XapiSettingsPageModel
        {
            IsConnected = _api.IsConnected(),
            Endpoint = (config["Scorm:XapiEndpoint"] ?? $"{(config["EduApi:BaseUrl"] ?? "").TrimEnd('/')}/api/v1/xapi/"),
            Message = TempData["ErrorMessage"]?.ToString()
        };
        if (TempData["XapiCreated"] is string created) model.Created = JsonSerializer.Deserialize<XapiCredentialCreated>(created, ScormJson);
        if (!model.IsConnected) return View(model);
        var credentials = await _api.XapiSendAsync(HttpMethod.Get, "credentials", null, ct);
        if (credentials.Ok) model.Credentials = JsonSerializer.Deserialize<List<XapiCredentialResponse>>(credentials.Body, ScormJson) ?? [];
        else model.Message ??= credentials.Message ?? "Credentials could not be loaded.";
        var recent = await _api.XapiSendAsync(HttpMethod.Get, "recent?take=50", null, ct);
        if (recent.Ok) model.Recent = JsonSerializer.Deserialize<List<XapiStatementSummary>>(recent.Body, ScormJson) ?? [];
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateXapiCredential(string? name, CancellationToken ct)
    {
        if (!IsAdminOrSuperAdminUser()) return AccessDenied("The Learning Record Store is managed by Admin and Super Admin.");
        var result = await _api.XapiSendAsync(HttpMethod.Post, "credentials", JsonSerializer.Serialize(new XapiCredentialRequest(name ?? "", null), ScormJson), ct);
        if (result.Ok) TempData["XapiCreated"] = result.Body;
        else TempData["ErrorMessage"] = result.Message ?? "The credentials could not be created.";
        return RedirectToAction(nameof(XapiSettings));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateXapiCredential(Guid id, CancellationToken ct)
    {
        if (!IsAdminOrSuperAdminUser()) return AccessDenied("The Learning Record Store is managed by Admin and Super Admin.");
        var result = await _api.XapiSendAsync(HttpMethod.Delete, $"credentials/{id}", null, ct);
        TempData[result.Ok ? "SuccessMessage" : "ErrorMessage"] = result.Ok ? "Credentials switched off." : result.Message ?? "The credentials could not be switched off.";
        return RedirectToAction(nameof(XapiSettings));
    }

    private bool IsAdminOrSuperAdminUser() => _api.GetSessionIdentity() is { } i && (i.IsAdmin || i.IsSuperAdmin);
}
