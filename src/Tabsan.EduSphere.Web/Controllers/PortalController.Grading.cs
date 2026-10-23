using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// Plan M3 — the student's submission page and the grading workspace. The API enforces every rule
/// (ownership, scope, posting policy, anonymity, moderation); these actions render pages and relay
/// the workspace script's requests, keeping the API token on the server.
/// </summary>
public partial class PortalController
{
    private const long MaxSubmissionUploadBytes = 105L * 1024 * 1024;

    private IActionResult? RequireGradingStaff()
        => CanManageQuizzes(_api.GetSessionIdentity()) ? null : AccessDenied("Grading is available to Faculty, Admin and SuperAdmin.");

    private IActionResult Relay(ApiRaw raw)
        => new ContentResult { StatusCode = raw.Status, Content = string.IsNullOrEmpty(raw.Body) ? "{}" : raw.Body, ContentType = "application/json" };

    /// <summary>Relays the request body (well-formed JSON only) to the API.</summary>
    private async Task<IActionResult> RelayJsonAsync(HttpMethod method, string path, CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        if (string.IsNullOrWhiteSpace(body)) body = "{}";
        try { using var _ = JsonDocument.Parse(body); }
        catch (JsonException) { return BadRequest(new { message = "The request was not valid JSON." }); }
        return Relay(await _api.AssignmentWorkspaceSendAsync(method, path, body, ct));
    }

    private IActionResult SendFile(ApiFile file)
    {
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(file.Content, file.ContentType, file.FileName);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Student: submission page
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> AssignmentSubmission(Guid assignmentId, CancellationToken ct)
    {
        ViewData["Title"] = "Assignment";
        if (RequireStudent() is { } denied) return denied;
        var model = new AssignmentSubmissionPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        try
        {
            model.View = await _api.GetStudentAssignmentViewAsync(assignmentId, ct);
            if (model.View is null) { TempData["PortalMessage"] = "This assignment is not available."; return RedirectToAction(nameof(Assignments)); }
            ViewData["Title"] = model.View.Title;
            // Plan M8: dates, late policy, group and peer reviews — the page still works without them.
            try { model.Extras = await _api.GetMyAssignmentExtrasAsync(assignmentId, ct); }
            catch (Exception) { model.Extras = null; }
            if (model.View.Submission is { } mine) model.Similarity = await MySimilarityAsync(mine.SubmissionId, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxSubmissionUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxSubmissionUploadBytes)]
    public async Task<IActionResult> SubmitAssignmentFiles(Guid assignmentId, string? textContent, List<IFormFile>? files, CancellationToken ct)
    {
        if (RequireStudent() is { } denied) return denied;
        try
        {
            var parts = new List<UploadPart>();
            foreach (var f in files ?? [])
            {
                if (f.Length == 0) continue;
                using var buffer = new MemoryStream();
                await f.CopyToAsync(buffer, ct);
                parts.Add(new UploadPart(Path.GetFileName(f.FileName), f.ContentType, buffer.ToArray()));
            }
            var result = await _api.SubmitAssignmentWithFilesAsync(assignmentId, textContent, parts, ct);
            TempData["PortalMessage"] = result.Ok ? "Your work has been submitted." : result.Message ?? "The submission was not accepted.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(AssignmentSubmission), new { assignmentId });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Staff: grading workspace page and settings
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> GradingWorkspace(Guid assignmentId, Guid? submissionId, string? filter, CancellationToken ct)
    {
        ViewData["Title"] = "Grading workspace";
        if (RequireGradingStaff() is { } denied) return denied;
        var model = new GradingWorkspacePageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString(), Filter = filter ?? "all" };
        if (!model.IsConnected) return View(model);
        try
        {
            model.Workspace = await _api.GetGradingWorkspaceAsync(assignmentId, filter, ct);
            if (model.Workspace is null) { TempData["PortalMessage"] = "This assignment was not found."; return RedirectToAction(nameof(Assignments)); }
            ViewData["Title"] = $"Grading — {model.Workspace.Title}";
            model.Similarity = await SimilarityScoresAsync(assignmentId, ct);
            model.SelectedSubmissionId = submissionId is { } id && model.Workspace.Submissions.Any(s => s.SubmissionId == id)
                ? id
                : model.Workspace.Submissions.FirstOrDefault(s => s.NeedsGrading)?.SubmissionId ?? model.Workspace.Submissions.FirstOrDefault()?.SubmissionId;
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAssignmentSettings(Guid assignmentId, int allowedAttempts, string? allowedFileTypes, int maxFileSizeMb,
        bool anonymousGrading, string postingPolicy, bool moderatedGrading, string? returnUrl, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        try
        {
            var types = (allowedFileTypes ?? string.Empty).Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries);
            var result = await _api.SaveAssignmentSettingsAsync(assignmentId,
                new AssignmentSettingsRequest(allowedAttempts, types, maxFileSizeMb, anonymousGrading, postingPolicy, moderatedGrading), ct);
            TempData["PortalMessage"] = result.Ok ? "Assignment settings saved." : result.Message ?? "The settings were not saved.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction(nameof(GradingWorkspace), new { assignmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeAssignmentGradePosting(Guid assignmentId, string operation, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        try
        {
            var path = operation switch { "post" => "post-grades", "hide" => "hide-grades", "reveal" => "reveal-names", _ => null };
            if (path is null) return BadRequest();
            var result = await _api.AssignmentWorkspaceSendAsync(HttpMethod.Post, $"{assignmentId}/{path}", operation == "reveal" ? null : "{}", ct);
            if (!result.Ok) TempData["PortalMessage"] = result.Message ?? "That did not work.";
            else if (operation == "reveal") TempData["PortalMessage"] = "Student names are now visible to graders.";
            else
            {
                using var doc = JsonDocument.Parse(result.Body);
                var count = doc.RootElement.GetProperty(operation == "post" ? "posted" : "hidden").GetInt32();
                var revealed = doc.RootElement.TryGetProperty("namesRevealed", out var r) && r.GetBoolean();
                TempData["PortalMessage"] = operation == "post"
                    ? $"{count} grade(s) posted to students.{(revealed ? " Student names are now visible." : string.Empty)}"
                    : $"{count} grade(s) hidden from students.";
            }
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(GradingWorkspace), new { assignmentId });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // JSON relays for the workspace / submission scripts (antiforgery header)
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> WorkspaceSubmission(Guid submissionId, CancellationToken ct)
        => Relay(await _api.AssignmentWorkspaceSendAsync(HttpMethod.Get, $"submissions/{submissionId}", null, ct));

    [HttpGet]
    public async Task<IActionResult> WorkspaceList(Guid assignmentId, string? filter, CancellationToken ct)
    {
        if (RequireGradingStaff() is not null) return Forbid();
        return Relay(await _api.AssignmentWorkspaceSendAsync(HttpMethod.Get,
            $"{assignmentId}/workspace" + (string.IsNullOrWhiteSpace(filter) ? string.Empty : $"?filter={Uri.EscapeDataString(filter)}"), null, ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> WorkspaceGrade(Guid submissionId, CancellationToken ct)
    {
        if (RequireGradingStaff() is not null) return Forbid();
        return await RelayJsonAsync(HttpMethod.Put, $"submissions/{submissionId}/grade", ct);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> WorkspaceRubric(Guid submissionId, CancellationToken ct)
    {
        if (RequireGradingStaff() is not null) return Forbid();
        return await RelayJsonAsync(HttpMethod.Post, $"submissions/{submissionId}/rubric", ct);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> WorkspaceModerate(Guid submissionId, CancellationToken ct)
    {
        if (RequireGradingStaff() is not null) return Forbid();
        return await RelayJsonAsync(HttpMethod.Post, $"submissions/{submissionId}/moderate", ct);
    }

    [HttpGet]
    public async Task<IActionResult> WorkspaceAnnotations(Guid fileId, CancellationToken ct)
        => Relay(await _api.AssignmentWorkspaceSendAsync(HttpMethod.Get, $"submission-files/{fileId}/annotations", null, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> WorkspaceAddAnnotation(Guid fileId, CancellationToken ct)
    {
        if (RequireGradingStaff() is not null) return Forbid();
        return await RelayJsonAsync(HttpMethod.Post, $"submission-files/{fileId}/annotations", ct);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> WorkspaceUpdateAnnotation(Guid annotationId, CancellationToken ct)
    {
        if (RequireGradingStaff() is not null) return Forbid();
        return await RelayJsonAsync(HttpMethod.Put, $"submission-annotations/{annotationId}", ct);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> WorkspaceDeleteAnnotation(Guid annotationId, CancellationToken ct)
    {
        if (RequireGradingStaff() is not null) return Forbid();
        return Relay(await _api.AssignmentWorkspaceSendAsync(HttpMethod.Delete, $"submission-annotations/{annotationId}", null, ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> WorkspaceReconvert(Guid fileId, CancellationToken ct)
    {
        if (RequireGradingStaff() is not null) return Forbid();
        return Relay(await _api.AssignmentWorkspaceSendAsync(HttpMethod.Post, $"submission-files/{fileId}/reconvert", null, ct));
    }

    /// <summary>Adds a comment (text and/or a recording). Used by both the workspace and the student page.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(30 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 30 * 1024 * 1024)]
    public async Task<IActionResult> WorkspaceComment(Guid submissionId, string? body, int? attemptNumber, bool isPrivate, IFormFile? media, CancellationToken ct)
    {
        UploadPart? part = null;
        if (media is { Length: > 0 })
        {
            using var buffer = new MemoryStream();
            await media.CopyToAsync(buffer, ct);
            part = new UploadPart(Path.GetFileName(media.FileName), media.ContentType, buffer.ToArray());
        }
        return Relay(await _api.AddSubmissionCommentAsync(submissionId, body, attemptNumber, isPrivate, part, ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> WorkspaceDeleteComment(Guid commentId, CancellationToken ct)
        => Relay(await _api.AssignmentWorkspaceSendAsync(HttpMethod.Delete, $"submission-comments/{commentId}", null, ct));

    // ═════════════════════════════════════════════════════════════════════════
    // File relays (attachments with nosniff; the viewer fetches them same-origin)
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> SubmissionFile(Guid fileId, bool pdf, CancellationToken ct)
    {
        var (file, error) = await _api.AssignmentWorkspaceDownloadAsync($"submission-files/{fileId}" + (pdf ? "?pdf=true" : string.Empty), ct);
        return file is null ? Relay(error!) : SendFile(file);
    }

    [HttpGet]
    public async Task<IActionResult> SubmissionFileAnnotated(Guid fileId, CancellationToken ct)
    {
        var (file, error) = await _api.AssignmentWorkspaceDownloadAsync($"submission-files/{fileId}/annotated", ct);
        if (file is not null) return SendFile(file);
        TempData["PortalMessage"] = error?.Message ?? "The annotated PDF is not available.";
        return Redirect(Request.Headers.Referer.FirstOrDefault() is { } back && Url.IsLocalUrl(new Uri(back).PathAndQuery) ? new Uri(back).PathAndQuery : Url.Action(nameof(Assignments))!);
    }

    [HttpGet]
    public async Task<IActionResult> SubmissionCommentMedia(Guid commentId, CancellationToken ct)
    {
        var (file, error) = await _api.AssignmentWorkspaceDownloadAsync($"submission-comments/{commentId}/media", ct);
        return file is null ? Relay(error!) : SendFile(file);
    }
}
