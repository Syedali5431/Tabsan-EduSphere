using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Pastoral;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// Pastoral care and wellbeing. Staff see the overview, a student's pastoral page, and write notes and support plans;
/// admins set up the wellbeing team; students check in and see what the school shared with them. The API decides who sees
/// and changes what.
/// </summary>
public partial class PortalController
{
    private static readonly JsonSerializerOptions PastoralJson = new(JsonSerializerDefaults.Web);

    private static T? ReadPastoral<T>(ApiRaw raw) => raw.Ok && !string.IsNullOrWhiteSpace(raw.Body) ? JsonSerializer.Deserialize<T>(raw.Body, PastoralJson) : default;

    private bool IsStudentSession() => _api.GetSessionIdentity()?.Roles.Any(r => r == "Student") == true;

    private async Task<StudentPastoralDto?> PastoralStudentAsync(Guid id, CancellationToken ct)
        => ReadPastoral<StudentPastoralDto>(await _api.PastoralSendAsync(HttpMethod.Get, $"students/{id}", null, ct));

    // ═════════════════════════════════════════════════════════════════════════
    // Overview and a student's page
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Pastoral(Guid? campusId, Guid? semesterId, Guid? classId, int? days, string? q, CancellationToken ct)
    {
        if (IsStudentSession()) return RedirectToAction(nameof(Wellbeing));
        ViewData["Title"] = "Pastoral care";
        var model = new PastoralPageModel
        {
            IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString(), CampusId = campusId, SemesterId = semesterId,
            CourseOfferingId = classId, Days = days is { } d && PastoralPageModel.DayChoices.Contains(d) ? d : 30, Search = q?.Trim()
        };
        if (!model.IsConnected) return View(model);
        var to = DateTime.UtcNow;
        var query = $"overview?from={Uri.EscapeDataString(CommunityFormat.Iso(to.AddDays(-model.Days)))}&to={Uri.EscapeDataString(CommunityFormat.Iso(to.AddMinutes(1)))}"
                    + (campusId is { } c ? $"&campusId={c}" : "") + (semesterId is { } s ? $"&semesterId={s}" : "") + (classId is { } o ? $"&courseOfferingId={o}" : "");
        var raw = await _api.PastoralSendAsync(HttpMethod.Get, query, null, ct);
        model.Overview = ReadPastoral<OverviewDto>(raw);
        if (!raw.Ok) model.Message ??= raw.Message ?? "The pastoral overview could not be loaded.";
        if (model.Search is { Length: >= 2 })
            model.Found = ReadPastoral<List<StudentOptionDto>>(await _api.PastoralSendAsync(HttpMethod.Get, $"students?q={Uri.EscapeDataString(model.Search)}", null, ct)) ?? [];
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> PastoralStudent(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Pastoral care";
        var model = new PastoralStudentPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        var raw = await _api.PastoralSendAsync(HttpMethod.Get, $"students/{id}", null, ct);
        if (raw.Status is 404 or 403) { TempData["PortalMessage"] = raw.Message ?? "Student not found."; return RedirectToAction(nameof(Pastoral)); }
        model.Student = ReadPastoral<StudentPastoralDto>(raw);
        if (!raw.Ok) model.Message ??= raw.Message ?? "The student's page could not be loaded.";
        return View(model);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Notes
    // ═════════════════════════════════════════════════════════════════════════

    private async Task<PastoralNotePageModel> NotePageAsync(Guid? id, Guid? studentId, Guid? classId, string? q, PastoralNoteInput? input, CancellationToken ct)
    {
        var model = new PastoralNotePageModel { IsConnected = _api.IsConnected(), Id = id, ClassId = classId, Search = q?.Trim(), Input = input ?? new PastoralNoteInput() };
        if (!model.IsConnected) return model;
        if (studentId is { } one)
        {
            model.Student = await PastoralStudentAsync(one, ct);
            model.Categories = model.Student?.Categories ?? [];
            if (input is null) model.Input.StudentProfileIds = [one];
            return model;
        }
        model.Filters = ReadPastoral<FilterOptionsDto>(await _api.PastoralSendAsync(HttpMethod.Get, "filters", null, ct));
        model.Categories = ReadPastoral<List<CategoryDto>>(await _api.PastoralSendAsync(HttpMethod.Get, "categories", null, ct)) ?? [];
        if (classId is { } c)
        {
            model.Choices = ReadPastoral<List<StudentOptionDto>>(await _api.PastoralSendAsync(HttpMethod.Get, $"students?courseOfferingId={c}", null, ct)) ?? [];
            if (input is null) model.Input.CourseOfferingId = c;
        }
        else if (model.Search is { Length: >= 2 })
            model.Choices = ReadPastoral<List<StudentOptionDto>>(await _api.PastoralSendAsync(HttpMethod.Get, $"students?q={Uri.EscapeDataString(model.Search)}", null, ct)) ?? [];
        return model;
    }

    [HttpGet]
    public async Task<IActionResult> PastoralNote(Guid? id, Guid? studentId, Guid? classId, string? q, CancellationToken ct)
    {
        ViewData["Title"] = id is null ? "Write a note" : "Change a note";
        var model = await NotePageAsync(id, studentId, classId, q, null, ct);
        if (id is { } existing)
        {
            var record = model.Student?.Records.FirstOrDefault(r => r.Id == existing);
            if (record is null || !record.CanManage) { TempData["PortalMessage"] = "Note not found."; return RedirectToAction(nameof(Pastoral)); }
            model.Input = PastoralNoteInput.From(record);
        }
        else if (studentId is not null && model.Student is null && model.IsConnected)
        {
            TempData["PortalMessage"] = "Student not found.";
            return RedirectToAction(nameof(Pastoral));
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePastoralNote(Guid? id, Guid? studentId, Guid? classId, string? q, PastoralNoteInput input, CancellationToken ct)
    {
        if (studentId is { } fixedStudent) input.StudentProfileIds = [fixedStudent];
        var raw = id is { } existing
            ? await _api.PastoralSendAsync(HttpMethod.Put, $"records/{existing}", input.ToRequest(), ct)
            : await _api.PastoralSendAsync(HttpMethod.Post, "records", input.ToRequest(), ct);
        if (raw.Ok)
        {
            var saved = id is null ? ReadPastoral<SavedRecordsDto>(raw) : null;
            TempData["PortalMessage"] = saved is { Skipped.Count: > 0 }
                ? "The note is saved. Some students were left out: you do not teach or look after them."
                : input.StudentProfileIds.Count > 1 ? "The note is saved for each student you chose." : "The note is saved.";
            return input.StudentProfileIds.Count == 1
                ? RedirectToAction(nameof(PastoralStudent), new { id = input.StudentProfileIds[0] })
                : RedirectToAction(nameof(Pastoral));
        }
        ViewData["Title"] = id is null ? "Write a note" : "Change a note";
        var model = await NotePageAsync(id, studentId, classId, q, input, ct);
        model.Message = raw.Message ?? "The note could not be saved.";
        return View(nameof(PastoralNote), model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPastoralFollowUpNote(Guid id, Guid studentId, string? text, CancellationToken ct)
    {
        var raw = await _api.PastoralSendAsync(HttpMethod.Post, $"records/{id}/notes", new PastoralTextRequest(text), ct);
        TempData["PortalMessage"] = raw.Ok ? "Your follow-up note is added." : raw.Message ?? "The follow-up note could not be added.";
        return RedirectToAction(nameof(PastoralStudent), new { id = studentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PastoralFollowUp(Guid id, Guid studentId, bool done, CancellationToken ct)
    {
        var raw = await _api.PastoralSendAsync(HttpMethod.Post, $"records/{id}/follow-up/{(done ? "done" : "reopen")}", null, ct);
        TempData["PortalMessage"] = raw.Ok ? done ? "The follow-up is done." : "The follow-up is open again." : raw.Message ?? "The note could not be changed.";
        return RedirectToAction(nameof(PastoralStudent), new { id = studentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemovePastoralNote(Guid id, Guid studentId, string? reason, CancellationToken ct)
    {
        var raw = await _api.PastoralSendAsync(HttpMethod.Post, $"records/{id}/remove", new PastoralTextRequest(reason), ct);
        TempData["PortalMessage"] = raw.Ok ? "The note is removed. The wellbeing team can still see it, with your reason." : raw.Message ?? "The note could not be removed.";
        return RedirectToAction(nameof(PastoralStudent), new { id = studentId });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Support plans
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> PastoralPlan(Guid? id, Guid studentId, CancellationToken ct)
    {
        ViewData["Title"] = id is null ? "New support plan" : "Change support plan";
        var model = new PastoralPlanPageModel { IsConnected = _api.IsConnected(), Id = id, Input = new PastoralPlanInput { StudentProfileId = studentId } };
        if (!model.IsConnected) return View(model);
        model.Student = await PastoralStudentAsync(studentId, ct);
        if (model.Student is null || (!model.Student.CanPlan && id is null)) { TempData["PortalMessage"] = "Support plans are written by the wellbeing team and admins."; return RedirectToAction(nameof(Pastoral)); }
        if (id is { } existing)
        {
            var plan = model.Student.Plans.FirstOrDefault(p => p.Id == existing);
            if (plan is null || !plan.CanManage) { TempData["PortalMessage"] = "Support plan not found."; return RedirectToAction(nameof(PastoralStudent), new { id = studentId }); }
            model.Input = PastoralPlanInput.From(plan);
        }
        else model.Input.StartsOn = PastoralFormat.Date(DateTime.UtcNow);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePastoralPlan(Guid? id, PastoralPlanInput input, CancellationToken ct)
    {
        var raw = id is { } existing
            ? await _api.PastoralSendAsync(HttpMethod.Put, $"plans/{existing}", input.ToRequest(), ct)
            : await _api.PastoralSendAsync(HttpMethod.Post, "plans", input.ToRequest(), ct);
        if (raw.Ok)
        {
            TempData["PortalMessage"] = id is null && !input.Confidential ? "The support plan is saved. The student's teachers have been told." : "The support plan is saved.";
            return RedirectToAction(nameof(PastoralStudent), new { id = input.StudentProfileId });
        }
        ViewData["Title"] = id is null ? "New support plan" : "Change support plan";
        return View(nameof(PastoralPlan), new PastoralPlanPageModel
        {
            IsConnected = true, Id = id, Input = input, Student = await PastoralStudentAsync(input.StudentProfileId, ct), Message = raw.Message ?? "The support plan could not be saved."
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EndPastoralPlan(Guid id, Guid studentId, bool restart, CancellationToken ct)
    {
        var raw = await _api.PastoralSendAsync(HttpMethod.Post, $"plans/{id}/{(restart ? "restart" : "end")}", null, ct);
        TempData["PortalMessage"] = raw.Ok ? restart ? "The support plan is running again." : "The support plan has ended." : raw.Message ?? "The support plan could not be changed.";
        return RedirectToAction(nameof(PastoralStudent), new { id = studentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PastoralCheckInSeen(Guid id, string? note, Guid? studentId, CancellationToken ct)
    {
        var raw = await _api.PastoralSendAsync(HttpMethod.Post, $"check-ins/{id}/seen", new PastoralTextRequest(note), ct);
        TempData["PortalMessage"] = raw.Ok ? "Marked as seen to." : raw.Message ?? "The check-in could not be changed.";
        return studentId is { } s ? RedirectToAction(nameof(PastoralStudent), new { id = s }) : RedirectToAction(nameof(Pastoral));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Set-up
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> PastoralSetup(Guid? tenantId, CancellationToken ct)
    {
        ViewData["Title"] = "Set up pastoral care";
        var model = new PastoralSetupPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        var raw = await _api.PastoralSendAsync(HttpMethod.Get, "setup" + (tenantId is { } t ? $"?tenantId={t}" : ""), null, ct);
        if (raw.Status == 403) { TempData["PortalMessage"] = raw.Message ?? "Only admins set up pastoral care."; return RedirectToAction(nameof(Pastoral)); }
        model.Setup = ReadPastoral<PastoralSetupDto>(raw);
        if (!raw.Ok) model.Message ??= raw.Message ?? "The set-up could not be loaded.";
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPastoralTeamMember(Guid? tenantId, Guid userId, string? title, Guid? campusId, Guid? semesterId, bool seesConfidential, CancellationToken ct)
    {
        var raw = await _api.PastoralSendAsync(HttpMethod.Post, "team", new AddTeamMemberRequest(tenantId, userId, title, campusId, semesterId, seesConfidential), ct);
        TempData["PortalMessage"] = raw.Ok ? "They are on the wellbeing team, and have been told." : raw.Message ?? "They could not be added to the team.";
        return RedirectToAction(nameof(PastoralSetup), new { tenantId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemovePastoralTeamMember(Guid id, Guid? tenantId, CancellationToken ct)
    {
        var raw = await _api.PastoralSendAsync(HttpMethod.Delete, $"team/{id}", null, ct);
        TempData["PortalMessage"] = raw.Ok ? "They are no longer on the wellbeing team." : raw.Message ?? "The team could not be changed.";
        return RedirectToAction(nameof(PastoralSetup), new { tenantId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePastoralSettings(Guid? tenantId, bool checkInsEnabled, int concernAlertCount, int concernAlertDays, CancellationToken ct)
    {
        var raw = await _api.PastoralSendAsync(HttpMethod.Put, "settings", new SaveSettingsRequest(tenantId, checkInsEnabled, concernAlertCount, concernAlertDays), ct);
        TempData["PortalMessage"] = raw.Ok ? "The settings are saved." : raw.Message ?? "The settings could not be saved.";
        return RedirectToAction(nameof(PastoralSetup), new { tenantId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePastoralCategory(Guid? id, Guid? tenantId, string? kind, string? tone, string? name, bool isActive, CancellationToken ct)
    {
        var request = new SaveCategoryRequest(tenantId, kind, tone, name, isActive);
        var raw = id is { } existing && existing != Guid.Empty
            ? await _api.PastoralSendAsync(HttpMethod.Put, $"categories/{existing}", request, ct)
            : await _api.PastoralSendAsync(HttpMethod.Post, "categories", request, ct);
        TempData["PortalMessage"] = raw.Ok ? "The categories are saved." : raw.Message ?? "The category could not be saved.";
        return RedirectToAction(nameof(PastoralSetup), new { tenantId });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Students
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Wellbeing(CancellationToken ct)
    {
        ViewData["Title"] = "Wellbeing";
        var model = new WellbeingPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        var raw = await _api.PastoralSendAsync(HttpMethod.Get, "mine", null, ct);
        model.Mine = ReadPastoral<MyWellbeingDto>(raw);
        if (!raw.Ok) model.Message ??= raw.Message ?? "Your wellbeing page could not be loaded.";
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> WellbeingCheckIn(int mood, string? note, bool wantsToTalk, CancellationToken ct)
    {
        var raw = await _api.PastoralSendAsync(HttpMethod.Post, "check-ins", new CheckInRequest(mood, note, wantsToTalk), ct);
        TempData["PortalMessage"] = !raw.Ok ? raw.Message ?? "Your check-in could not be saved."
            : wantsToTalk ? "Thank you. Someone from the wellbeing team will find you soon." : "Thank you for checking in.";
        return RedirectToAction(nameof(Wellbeing));
    }
}
