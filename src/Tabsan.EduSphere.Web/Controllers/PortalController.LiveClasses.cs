using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.LiveClasses;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// Plan M10 — live classes: the course's classes (schedule, from the timetable, edit, cancel, join, calendar file),
/// attendance from who joined, and the conferencing settings. The API enforces licensing, roles, scope and enrolment.
/// </summary>
public partial class PortalController
{
    private static readonly JsonSerializerOptions LiveJson = new(JsonSerializerDefaults.Web);

    private static bool IsLiveModuleOff(ApiRaw raw) => raw.Status == 403 && (raw.Message ?? "").Contains("'live_classes'", StringComparison.OrdinalIgnoreCase);

    private static T? ReadLive<T>(ApiRaw raw) => raw.Ok && !string.IsNullOrWhiteSpace(raw.Body) ? JsonSerializer.Deserialize<T>(raw.Body, LiveJson) : default;

    private bool IsStaffUser() => CanManageQuizzes(_api.GetSessionIdentity());

    // ═════════════════════════════════════════════════════════════════════════
    // Course classes
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> LiveClasses(Guid? offeringId, Guid? edit, CancellationToken ct)
    {
        ViewData["Title"] = "Live Classes";
        var model = new LiveClassesPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString(), IsStaff = IsStaffUser(), EditingId = edit };
        if (!model.IsConnected) return View(model);
        try
        {
            var offerings = model.IsStaff ? await OutcomeOfferingsAsync(ct) : await _api.GetMyOfferingsAsync(ct);
            model.Offerings = offerings.Select(o => (o.Id, o.Name)).ToList();
            model.OfferingId = offeringId ?? model.Offerings.FirstOrDefault().Id;
            if (model.OfferingId != Guid.Empty)
            {
                if (!model.Offerings.Any(o => o.Id == model.OfferingId)) model.Offerings.Insert(0, (model.OfferingId, "Selected course"));
                var raw = await _api.LiveSendAsync(HttpMethod.Get, $"offerings/{model.OfferingId}/sessions", null, ct);
                if (IsLiveModuleOff(raw)) model.ModuleOff = true;
                else if (!raw.Ok) model.Message ??= raw.Message ?? "The live classes could not be loaded.";
                else model.Sessions = ReadLive<List<LiveSessionDto>>(raw) ?? [];
                if (model.IsStaff && !model.ModuleOff)
                    model.Slots = ReadLive<List<LiveTimetableSlotDto>>(await _api.LiveSendAsync(HttpMethod.Get, $"offerings/{model.OfferingId}/timetable", null, ct)) ?? [];
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ScheduleLiveSession(Guid offeringId, string? title, string? description, string? startsAt, int durationMinutes, bool autoAttendance,
        int minimumMinutes, string? meetingUrl, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        if (UtcInput(startsAt) is not { } start) { TempData["PortalMessage"] = "Choose the start date and time."; return RedirectToAction(nameof(LiveClasses), new { offeringId }); }
        Report(await _api.LiveSendAsync(HttpMethod.Post, $"offerings/{offeringId}/sessions",
            new ScheduleLiveSessionRequest(title ?? string.Empty, description, start, durationMinutes, autoAttendance, minimumMinutes, meetingUrl), ct),
            "Live class scheduled. Students have been notified.");
        return RedirectToAction(nameof(LiveClasses), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ScheduleLiveFromTimetable(Guid offeringId, Guid entryId, int weeks, bool autoAttendance, int minimumMinutes, string? meetingUrl, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        var raw = await _api.LiveSendAsync(HttpMethod.Post, $"offerings/{offeringId}/sessions/from-timetable",
            new ScheduleFromTimetableRequest(entryId, weeks, autoAttendance, minimumMinutes, meetingUrl), ct);
        var count = raw.Ok ? ReadLive<List<LiveSessionDto>>(raw)?.Count ?? 0 : 0;
        Report(raw, count == 0 ? "Those weeks are already scheduled." : $"{count} live class(es) scheduled from the timetable.");
        return RedirectToAction(nameof(LiveClasses), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLiveSession(Guid sessionId, Guid offeringId, string? title, string? description, string? startsAt, int durationMinutes,
        bool autoAttendance, int minimumMinutes, string? meetingUrl, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        if (UtcInput(startsAt) is not { } start) { TempData["PortalMessage"] = "Choose the start date and time."; return RedirectToAction(nameof(LiveClasses), new { offeringId, edit = sessionId }); }
        Report(await _api.LiveSendAsync(HttpMethod.Put, $"sessions/{sessionId}",
            new ScheduleLiveSessionRequest(title ?? string.Empty, description, start, durationMinutes, autoAttendance, minimumMinutes, meetingUrl), ct), "Live class saved.");
        return RedirectToAction(nameof(LiveClasses), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelLiveSession(Guid sessionId, Guid offeringId, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        Report(await _api.LiveSendAsync(HttpMethod.Post, $"sessions/{sessionId}/cancel", null, ct), "Live class cancelled. Students have been told.");
        return RedirectToAction(nameof(LiveClasses), new { offeringId });
    }

    /// <summary>Records the join and opens the meeting (a POST, because joining is recorded for attendance).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> JoinLiveSession(Guid sessionId, string? returnUrl, CancellationToken ct)
    {
        var raw = await _api.LiveSendAsync(HttpMethod.Post, $"sessions/{sessionId}/join", null, ct);
        var url = ReadLive<LiveJoinDto>(raw)?.Url;
        if (raw.Ok && Uri.TryCreate(url, UriKind.Absolute, out var target) && (target.Scheme == Uri.UriSchemeHttps || target.Scheme == Uri.UriSchemeHttp))
            return Redirect(target.ToString());
        TempData["PortalMessage"] = raw.Message ?? "The class could not be opened.";
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction(nameof(LiveClasses));
    }

    [HttpGet]
    public async Task<IActionResult> LiveSessionCalendar(Guid sessionId, CancellationToken ct)
    {
        var (file, error) = await _api.LiveDownloadAsync($"sessions/{sessionId}/calendar", ct);
        if (file is not null) return SendFile(file);
        TempData["PortalMessage"] = error?.Message ?? "The calendar file is not available.";
        return RedirectToAction(nameof(LiveClasses));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Attendance
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> LiveSessionAttendance(Guid id, Guid offeringId, CancellationToken ct)
    {
        ViewData["Title"] = "Live class attendance";
        if (RequireGradingStaff() is { } denied) return denied;
        var model = new LiveSessionAttendancePageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString(), OfferingId = offeringId };
        if (!model.IsConnected) return View(model);
        try
        {
            var raw = await _api.LiveSendAsync(HttpMethod.Get, $"sessions/{id}/attendance", null, ct);
            if (!raw.Ok) model.Message ??= raw.Message ?? "The attendance could not be loaded.";
            else model.Report = ReadLive<LiveAttendanceReportDto>(raw);
            if (model.Report is not null) ViewData["Title"] = $"Attendance — {model.Report.Title}";
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TakeLiveAttendance(Guid sessionId, Guid offeringId, CancellationToken ct)
    {
        if (RequireGradingStaff() is { } denied) return denied;
        var raw = await _api.LiveSendAsync(HttpMethod.Post, $"sessions/{sessionId}/attendance", null, ct);
        var marked = 0;
        if (raw.Ok) { using var doc = JsonDocument.Parse(raw.Body); marked = doc.RootElement.GetProperty("marked").GetInt32(); }
        Report(raw, $"Attendance marked for {marked} student(s). Existing marks for that day were kept.");
        return RedirectToAction(nameof(LiveSessionAttendance), new { id = sessionId, offeringId });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Settings (Admin / SuperAdmin)
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> LiveClassSettings(bool installationDefault, CancellationToken ct)
    {
        ViewData["Title"] = "Live Class Settings";
        var identity = _api.GetSessionIdentity();
        if (identity is null || !(identity.IsAdmin || identity.IsSuperAdmin)) return AccessDenied("Live classes are set up by Admin and SuperAdmin.");
        var model = new LiveClassSettingsPageModel
        {
            IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString(),
            IsSuperAdmin = identity.IsSuperAdmin, InstallationDefault = installationDefault && identity.IsSuperAdmin
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var raw = await _api.LiveSendAsync(HttpMethod.Get, $"settings?installationDefault={(model.InstallationDefault ? "true" : "false")}", null, ct);
            if (IsLiveModuleOff(raw)) model.ModuleOff = true;
            else if (!raw.Ok) model.Message ??= raw.Message ?? "The settings could not be loaded.";
            else model.Settings = ReadLive<ConferenceSettingsDto>(raw);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLiveClassSettings(string? provider, bool isEnabled, string? baseUrl, string? secret, string? accountId, string? clientId,
        string? organizerUserId, int joinEarlyMinutes, bool installationDefault, CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity is null || !(identity.IsAdmin || identity.IsSuperAdmin)) return AccessDenied("Live classes are set up by Admin and SuperAdmin.");
        Report(await _api.LiveSendAsync(HttpMethod.Put, $"settings?installationDefault={(installationDefault ? "true" : "false")}",
            new SaveConferenceSettingsRequest(provider ?? "ExternalLink", isEnabled, baseUrl, secret, accountId, clientId, organizerUserId, joinEarlyMinutes), ct),
            "Live class settings saved.");
        return RedirectToAction(nameof(LiveClassSettings), new { installationDefault });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TestLiveClassSettings(bool installationDefault, CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity is null || !(identity.IsAdmin || identity.IsSuperAdmin)) return AccessDenied("Live classes are set up by Admin and SuperAdmin.");
        var raw = await _api.LiveSendAsync(HttpMethod.Post, $"settings/test?installationDefault={(installationDefault ? "true" : "false")}", null, ct);
        TempData["PortalMessage"] = raw.Message ?? (raw.Ok ? "Connected." : "The test failed.");
        return RedirectToAction(nameof(LiveClassSettings), new { installationDefault });
    }
}
