using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Similarity;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// Plan M9 — similarity checking: provider settings, the report page, the assignment option, the workspace's scores and
/// "Check again", and the student's own score. The API enforces licensing, roles and scope.
/// </summary>
public partial class PortalController
{
    private static readonly JsonSerializerOptions SimilarityJson = new(JsonSerializerDefaults.Web);

    private static bool IsSimilarityModuleOff(ApiRaw raw) => raw.Status == 403 && (raw.Message ?? "").Contains("'similarity'", StringComparison.OrdinalIgnoreCase);

    private static T? ReadSimilarity<T>(ApiRaw raw) => raw.Ok && !string.IsNullOrWhiteSpace(raw.Body) ? JsonSerializer.Deserialize<T>(raw.Body, SimilarityJson) : default;

    // ═════════════════════════════════════════════════════════════════════════
    // Settings (Admin / SuperAdmin)
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> SimilaritySettings(bool installationDefault, CancellationToken ct)
    {
        ViewData["Title"] = "Similarity Checking";
        var identity = _api.GetSessionIdentity();
        if (identity is null || !(identity.IsAdmin || identity.IsSuperAdmin)) return AccessDenied("Similarity checking is set up by Admin and SuperAdmin.");
        var model = new SimilaritySettingsPageModel
        {
            IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString(),
            IsSuperAdmin = identity.IsSuperAdmin, InstallationDefault = installationDefault && identity.IsSuperAdmin
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var raw = await _api.SimilaritySendAsync(HttpMethod.Get, $"settings?installationDefault={(model.InstallationDefault ? "true" : "false")}", null, ct);
            if (IsSimilarityModuleOff(raw)) model.ModuleOff = true;
            else if (!raw.Ok) model.Message ??= raw.Message ?? "The settings could not be loaded.";
            else model.Settings = ReadSimilarity<SimilaritySettingsDto>(raw);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSimilaritySettings(string? provider, bool isEnabled, string? scope, string? accountEmail, string? apiKey, bool sandbox,
        bool installationDefault, CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity is null || !(identity.IsAdmin || identity.IsSuperAdmin)) return AccessDenied("Similarity checking is set up by Admin and SuperAdmin.");
        var raw = await _api.SimilaritySendAsync(HttpMethod.Put, $"settings?installationDefault={(installationDefault ? "true" : "false")}",
            new SaveSimilaritySettingsRequest(provider ?? "Internal", isEnabled, scope ?? "Course", accountEmail, apiKey, sandbox), ct);
        Report(raw, "Similarity settings saved.");
        return RedirectToAction(nameof(SimilaritySettings), new { installationDefault });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TestSimilaritySettings(bool installationDefault, CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity is null || !(identity.IsAdmin || identity.IsSuperAdmin)) return AccessDenied("Similarity checking is set up by Admin and SuperAdmin.");
        var raw = await _api.SimilaritySendAsync(HttpMethod.Post, $"settings/test?installationDefault={(installationDefault ? "true" : "false")}", null, ct);
        TempData["PortalMessage"] = raw.Message ?? (raw.Ok ? "Connected." : "The test failed.");
        return RedirectToAction(nameof(SimilaritySettings), new { installationDefault });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Report and assignment option (staff)
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> SimilarityReport(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Similarity report";
        if (RequireGradingStaff() is { } denied) return denied;
        var model = new SimilarityReportPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        try
        {
            var raw = await _api.SimilaritySendAsync(HttpMethod.Get, $"checks/{id}", null, ct);
            if (!raw.Ok) model.Message = raw.Status == 404 ? "This report was not found." : raw.Message ?? "The report could not be loaded.";
            else model.Report = ReadSimilarity<SimilarityReportDto>(raw);
            if (model.Report is not null) ViewData["Title"] = $"Similarity — {model.Report.AssignmentTitle}";
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAssignmentSimilarity(Guid assignmentId, bool enabled, bool visibleToStudents, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        Report(await _api.SimilaritySendAsync(HttpMethod.Put, $"assignments/{assignmentId}/settings", new SaveAssignmentSimilarityRequest(enabled, visibleToStudents), ct),
            enabled ? "Similarity checking is on for this assignment." : "Similarity checking is off for this assignment.");
        return RedirectToAction(nameof(AssignmentOptions), new { assignmentId });
    }

    /// <summary>Workspace script: check the current attempt again.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> WorkspaceSimilarityRecheck(Guid submissionId, CancellationToken ct)
    {
        if (RequireGradingStaff() is not null) return Forbid();
        return Relay(await _api.SimilaritySendAsync(HttpMethod.Post, $"submissions/{submissionId}/recheck", null, ct));
    }

    /// <summary>The assignment's similarity option for the options page (null when the module is not licensed).</summary>
    private async Task<AssignmentSimilarityDto?> AssignmentSimilarityAsync(Guid assignmentId, CancellationToken ct)
    {
        try { return ReadSimilarity<AssignmentSimilarityDto>(await _api.SimilaritySendAsync(HttpMethod.Get, $"assignments/{assignmentId}/settings", null, ct)); }
        catch (Exception) { return null; }
    }

    /// <summary>Scores per submission for the workspace (empty when the module is off or checking is not used).</summary>
    private async Task<Dictionary<Guid, SimilarityScoreDto>> SimilarityScoresAsync(Guid assignmentId, CancellationToken ct)
    {
        try
        {
            return (ReadSimilarity<List<SimilarityScoreDto>>(await _api.SimilaritySendAsync(HttpMethod.Get, $"assignments/{assignmentId}/scores", null, ct)) ?? [])
                .ToDictionary(s => s.SubmissionId);
        }
        catch (Exception) { return []; }
    }

    private async Task<StudentSimilarityDto?> MySimilarityAsync(Guid submissionId, CancellationToken ct)
    {
        try { return ReadSimilarity<StudentSimilarityDto>(await _api.SimilaritySendAsync(HttpMethod.Get, $"my/submissions/{submissionId}", null, ct)); }
        catch (Exception) { return null; }
    }
}
