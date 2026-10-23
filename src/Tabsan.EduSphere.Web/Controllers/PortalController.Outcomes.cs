using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Outcomes;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// Plan M6 — learning outcomes, the Learning Mastery Gradebook, program attainment and the student's own mastery.
/// The API enforces roles and scope; these actions render pages, relay the alignment widget and stream exports.
/// </summary>
public partial class PortalController
{
    private static readonly JsonSerializerOptions OutcomeJson = new(JsonSerializerDefaults.Web);

    private IActionResult? RequireOutcomeStaff()
        => CanManageQuizzes(_api.GetSessionIdentity()) ? null : AccessDenied("Learning outcomes are managed by Faculty, Admin and Super Admin.");

    private static bool IsOutcomeModuleOff(ApiRaw raw) => raw.Status == 403 && (raw.Message ?? "").Contains("'outcomes'", StringComparison.OrdinalIgnoreCase);

    private static T? ReadJson<T>(ApiRaw raw) => raw.Ok && !string.IsNullOrWhiteSpace(raw.Body) ? JsonSerializer.Deserialize<T>(raw.Body, OutcomeJson) : default;

    // ═════════════════════════════════════════════════════════════════════════
    // Outcomes
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Outcomes(string? level, Guid? programId, Guid? courseId, bool includeInactive, Guid? edit, CancellationToken ct)
    {
        ViewData["Title"] = "Learning Outcomes";
        if (RequireOutcomeStaff() is { } denied) return denied;
        var identity = _api.GetSessionIdentity();
        var model = new OutcomesPageModel
        {
            IsConnected = _api.IsConnected(), IsAdmin = identity?.IsAdmin == true || identity?.IsSuperAdmin == true,
            Message = TempData["PortalMessage"]?.ToString(), Level = level, ProgramId = programId, CourseId = courseId, IncludeInactive = includeInactive
        };
        if (TempData["OutcomeImport"] is string importJson) model.Import = JsonSerializer.Deserialize<OutcomeImportResult>(importJson, OutcomeJson);
        if (!model.IsConnected) return View(model);
        try
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(level)) query.Add("level=" + Uri.EscapeDataString(level));
            if (programId is { } p) query.Add("programId=" + p);
            if (courseId is { } c) query.Add("courseId=" + c);
            if (includeInactive) query.Add("includeInactive=true");
            var list = await _api.OutcomeSendAsync(HttpMethod.Get, query.Count == 0 ? "" : "?" + string.Join("&", query), null, ct);
            if (IsOutcomeModuleOff(list)) { model.ModuleOff = true; return View(model); }
            if (!list.Ok) { model.Message = list.Message ?? "Outcomes could not be loaded."; return View(model); }
            model.Outcomes = ReadJson<List<OutcomeResponse>>(list) ?? [];
            model.ProgramOutcomes = ReadJson<List<OutcomeResponse>>(await _api.OutcomeSendAsync(HttpMethod.Get, "?level=Program", null, ct)) ?? [];
            model.Programs = ReadJson<List<OutcomeProgramInfo>>(await _api.OutcomeSendAsync(HttpMethod.Get, "programs", null, ct)) ?? [];
            if (edit is { } id) model.Editing = ReadJson<OutcomeResponse>(await _api.OutcomeSendAsync(HttpMethod.Get, id.ToString(), null, ct));
            model.Courses = await _api.GetCoursesAsync(null, ct);
            model.Offerings = await OutcomeOfferingsAsync(ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveOutcome(OutcomeFormModel form, CancellationToken ct)
    {
        if (RequireOutcomeStaff() is { } denied) return denied;
        var ratings = form.RatingDescription.Zip(form.RatingPoints)
            .Where(r => !string.IsNullOrWhiteSpace(r.First) && r.Second is not null)
            .Select(r => new OutcomeRatingDto(r.First!.Trim(), r.Second!.Value)).ToList();
        var request = new SaveOutcomeRequest(form.Level, form.ProgramId, form.CourseId, form.ParentId, form.Code ?? "", form.Title ?? "", form.Description,
            ratings.Count == 0 ? null : ratings, form.MasteryPoints, form.CalculationMethod, form.CalculationParameter);
        var json = JsonSerializer.Serialize(request, OutcomeJson);
        var result = form.Id is { } id
            ? await _api.OutcomeSendAsync(HttpMethod.Put, id.ToString(), json, ct)
            : await _api.OutcomeSendAsync(HttpMethod.Post, "", json, ct);
        if (!result.Ok)
        {
            TempData["PortalMessage"] = result.Message ?? "The outcome could not be saved.";
            return RedirectToAction(nameof(Outcomes), new { edit = form.Id });
        }
        var saved = ReadJson<OutcomeResponse>(result)!;
        TempData["PortalMessage"] = $"{saved.Code} saved.";
        return RedirectToAction(nameof(Outcomes), new { edit = saved.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetOutcomeActive(Guid outcomeId, bool active, CancellationToken ct)
    {
        if (RequireOutcomeStaff() is { } denied) return denied;
        var result = await _api.OutcomeSendAsync(HttpMethod.Post, $"{outcomeId}/{(active ? "activate" : "deactivate")}", null, ct);
        TempData["PortalMessage"] = result.Ok ? (active ? "Outcome turned on." : "Outcome turned off; its evidence is kept.") : result.Message ?? "The outcome could not be changed.";
        return RedirectToAction(nameof(Outcomes), new { includeInactive = true });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveOutcomeMappings(Guid outcomeId, List<Guid>? programOutcomeIds, CancellationToken ct)
    {
        if (RequireOutcomeStaff() is { } denied) return denied;
        var result = await _api.OutcomeSendAsync(HttpMethod.Put, $"{outcomeId}/mappings",
            JsonSerializer.Serialize(new SetOutcomeMappingsRequest(programOutcomeIds ?? []), OutcomeJson), ct);
        TempData["PortalMessage"] = result.Ok ? "Program outcome mapping saved." : result.Message ?? "The mapping could not be saved.";
        return RedirectToAction(nameof(Outcomes), new { edit = outcomeId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> ImportOutcomes(IFormFile? file, CancellationToken ct)
    {
        if (RequireOutcomeStaff() is { } denied) return denied;
        if (file is null || file.Length == 0) { TempData["PortalMessage"] = "Choose the Excel file to import."; return RedirectToAction(nameof(Outcomes)); }
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        var result = await _api.OutcomeImportAsync(new UploadPart(Path.GetFileName(file.FileName), file.ContentType, buffer.ToArray()), ct);
        if (result.Ok) TempData["OutcomeImport"] = result.Body;
        else TempData["PortalMessage"] = result.Message ?? "The file could not be imported.";
        return RedirectToAction(nameof(Outcomes));
    }

    [HttpGet]
    public async Task<IActionResult> OutcomeTemplate(CancellationToken ct)
    {
        if (RequireOutcomeStaff() is { } denied) return denied;
        var (file, error) = await _api.OutcomeDownloadAsync("import-template", ct);
        if (file is null) { TempData["PortalMessage"] = error?.Message ?? "The template could not be downloaded."; return RedirectToAction(nameof(Outcomes)); }
        return SendFile(file);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Alignment widget (rubric editor, quiz builder, question banks, assignments)
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> OutcomeAlignment(string targetType, Guid targetId, CancellationToken ct)
    {
        if (!CanManageQuizzes(_api.GetSessionIdentity())) return Forbid();
        return Relay(await _api.OutcomeSendAsync(HttpMethod.Get, $"alignments?targetType={Uri.EscapeDataString(targetType ?? "")}&targetId={targetId}", null, ct));
    }

    [HttpPost, ValidateAntiForgeryToken, ActionName("OutcomeAlignment")]
    public async Task<IActionResult> SaveOutcomeAlignment(CancellationToken ct)
    {
        if (!CanManageQuizzes(_api.GetSessionIdentity())) return Forbid();
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        SetAlignmentsRequest? request;
        try { request = JsonSerializer.Deserialize<SetAlignmentsRequest>(body, OutcomeJson); }
        catch (JsonException) { request = null; }
        if (request is null) return BadRequest(new { message = "The request was not valid JSON." });
        return Relay(await _api.OutcomeSendAsync(HttpMethod.Put, "alignments", JsonSerializer.Serialize(request, OutcomeJson), ct));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Mastery gradebook, attainment, student view
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> OutcomeMastery(Guid? offeringId, CancellationToken ct)
    {
        ViewData["Title"] = "Learning Mastery Gradebook";
        if (RequireOutcomeStaff() is { } denied) return denied;
        var model = new OutcomeMasteryPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        try
        {
            model.Offerings = await OutcomeOfferingsAsync(ct);
            model.OfferingId = offeringId ?? model.Offerings.FirstOrDefault()?.Id ?? Guid.Empty;
            if (model.OfferingId == Guid.Empty) return View(model);
            var raw = await _api.OutcomeSendAsync(HttpMethod.Get, $"mastery/offerings/{model.OfferingId}", null, ct);
            if (IsOutcomeModuleOff(raw)) model.ModuleOff = true;
            else if (!raw.Ok) model.Message ??= raw.Message ?? "The gradebook could not be loaded.";
            else model.Gradebook = ReadJson<MasteryGradebookResponse>(raw);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> OutcomeMasteryExport(Guid offeringId, CancellationToken ct)
    {
        if (RequireOutcomeStaff() is { } denied) return denied;
        var (file, error) = await _api.OutcomeDownloadAsync($"mastery/offerings/{offeringId}/export", ct);
        if (file is null) { TempData["PortalMessage"] = error?.Message ?? "The export failed."; return RedirectToAction(nameof(OutcomeMastery), new { offeringId }); }
        return SendFile(file);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RecalculateOutcomes(Guid offeringId, CancellationToken ct)
    {
        if (RequireOutcomeStaff() is { } denied) return denied;
        var result = await _api.OutcomeSendAsync(HttpMethod.Post, $"mastery/offerings/{offeringId}/recalculate", null, ct);
        TempData["PortalMessage"] = result.Ok ? "Mastery recalculated from the current grades." : result.Message ?? "Recalculation failed.";
        return RedirectToAction(nameof(OutcomeMastery), new { offeringId });
    }

    [HttpGet]
    public async Task<IActionResult> OutcomeAttainment(Guid? programId, decimal? target, CancellationToken ct)
    {
        ViewData["Title"] = "Program Outcome Attainment";
        if (RequireOutcomeStaff() is { } denied) return denied;
        var model = new OutcomeAttainmentPageModel { IsConnected = _api.IsConnected(), Target = Math.Clamp(target ?? 70, 1, 100) };
        if (!model.IsConnected) return View(model);
        try
        {
            var programs = await _api.OutcomeSendAsync(HttpMethod.Get, "programs", null, ct);
            if (IsOutcomeModuleOff(programs)) { model.ModuleOff = true; return View(model); }
            model.Programs = ReadJson<List<OutcomeProgramInfo>>(programs) ?? [];
            model.ProgramId = programId ?? Guid.Empty;
            if (model.ProgramId != Guid.Empty)
            {
                var raw = await _api.OutcomeSendAsync(HttpMethod.Get, $"attainment/programs/{model.ProgramId}?target={model.Target.ToString(System.Globalization.CultureInfo.InvariantCulture)}", null, ct);
                if (raw.Ok) model.Report = ReadJson<ProgramAttainmentReport>(raw);
                else model.Message = raw.Message ?? "The report could not be built.";
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> OutcomeAttainmentExport(Guid programId, string? format, decimal? target, CancellationToken ct)
    {
        if (RequireOutcomeStaff() is { } denied) return denied;
        var t = Math.Clamp(target ?? 70, 1, 100).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var (file, error) = await _api.OutcomeDownloadAsync($"attainment/programs/{programId}/export?format={(format == "pdf" ? "pdf" : "xlsx")}&target={t}", ct);
        if (file is null) { TempData["PortalMessage"] = error?.Message ?? "The export failed."; return RedirectToAction(nameof(OutcomeAttainment), new { programId, target }); }
        return SendFile(file);
    }

    [HttpGet]
    public async Task<IActionResult> MyOutcomes(CancellationToken ct)
    {
        ViewData["Title"] = "My Outcomes";
        if (RequireStudent() is { } denied) return denied;
        var model = new MyOutcomesPageModel { IsConnected = _api.IsConnected() };
        if (!model.IsConnected) return View(model);
        try
        {
            var raw = await _api.OutcomeSendAsync(HttpMethod.Get, "mastery/me", null, ct);
            if (IsOutcomeModuleOff(raw)) model.ModuleOff = true;
            else if (!raw.Ok) model.Message = raw.Message ?? "Your outcomes could not be loaded.";
            else model.Courses = ReadJson<List<StudentMasteryCourse>>(raw) ?? [];
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    /// <summary>Teachers see their own offerings; administrators every active offering in scope.</summary>
    private async Task<IReadOnlyList<LookupItem>> OutcomeOfferingsAsync(CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity?.IsAdmin == true || identity?.IsSuperAdmin == true)
            return (await _api.GetCourseOfferingsAsync(null, identity.TenantId, identity.CampusId, null, ct))
                .Where(o => o.IsActive).OrderBy(o => o.CourseCode).ThenBy(o => o.SemesterName)
                .Select(o => new LookupItem { Id = o.Id, Name = $"{o.CourseCode} {o.CourseTitle} ({o.SemesterName})" }).ToList();
        return await _api.GetMyOfferingsAsync(ct);
    }
}
