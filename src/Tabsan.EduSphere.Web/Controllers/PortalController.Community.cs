using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Community;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// School news, events and online forms. Everyone reads what is for them, replies to events and fills in forms; teachers
/// write for their classes and admins for the school. Parents use the app. The API decides who sees and changes what.
/// </summary>
public partial class PortalController
{
    private static readonly JsonSerializerOptions CommunityJson = new(JsonSerializerDefaults.Web);

    private static T? ReadCommunity<T>(ApiRaw raw) => raw.Ok && !string.IsNullOrWhiteSpace(raw.Body) ? JsonSerializer.Deserialize<T>(raw.Body, CommunityJson) : default;

    private bool CanWriteForSchool() => _api.GetSessionIdentity()?.Roles.Any(r => r is "Admin" or "SuperAdmin" or "Faculty") == true;

    private async Task<AudienceOptionsDto?> AudienceOptionsAsync(CancellationToken ct)
        => ReadCommunity<AudienceOptionsDto>(await _api.CommunitySendAsync(HttpMethod.Get, "audiences", null, ct));

    // ═════════════════════════════════════════════════════════════════════════
    // News
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> News(string? tab, CancellationToken ct)
    {
        ViewData["Title"] = "School news";
        var model = new NewsPageModel { IsConnected = _api.IsConnected(), CanWrite = CanWriteForSchool(), Message = TempData["PortalMessage"]?.ToString() };
        model.Tab = model.CanWrite && tab == "manage" ? "manage" : "feed";
        if (!model.IsConnected) return View(model);
        try
        {
            if (model.Tab == "manage")
            {
                var raw = await _api.CommunitySendAsync(HttpMethod.Get, "news/manage", null, ct);
                if (raw.Ok) model.Managed = ReadCommunity<List<NewsManageDto>>(raw) ?? [];
                else model.Message ??= raw.Message ?? "The news could not be loaded.";
            }
            else
            {
                var raw = await _api.CommunitySendAsync(HttpMethod.Get, "news", null, ct);
                if (raw.Ok) model.Feed = ReadCommunity<List<NewsSummaryDto>>(raw) ?? [];
                else model.Message ??= raw.Message ?? "The news could not be loaded.";
            }
        }
        catch (Exception ex) { model.Message ??= ex.Message; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> NewsPost(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "School news";
        var model = new NewsPostPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        var raw = await _api.CommunitySendAsync(HttpMethod.Get, $"news/{id}", null, ct);
        if (raw.Status == 404) { TempData["PortalMessage"] = "News post not found."; return RedirectToAction(nameof(News)); }
        model.Post = ReadCommunity<NewsPostDto>(raw);
        if (!raw.Ok) model.Message ??= raw.Message ?? "The news could not be loaded.";
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditNews(Guid? id, CancellationToken ct)
    {
        ViewData["Title"] = id is null ? "Write news" : "Change news";
        var model = new EditNewsPageModel { IsConnected = _api.IsConnected(), Id = id };
        if (!model.IsConnected) return View(model);
        model.Options = await AudienceOptionsAsync(ct);
        if (model.Options is null) { TempData["PortalMessage"] = "Only teachers and admins write for the school."; return RedirectToAction(nameof(News)); }
        if (id is { } existing)
        {
            var post = ReadCommunity<NewsPostDto>(await _api.CommunitySendAsync(HttpMethod.Get, $"news/{existing}", null, ct));
            if (post is null || !post.CanManage) { TempData["PortalMessage"] = "News post not found."; return RedirectToAction(nameof(News)); }
            model.Input = new NewsInput
            {
                Title = post.Title, Summary = post.Summary, Body = post.Body, ImageUrl = post.ImageUrl, Pinned = post.IsPinned,
                PublishAt = CommunityFormat.Iso(post.PublishAt), ExpiresAt = CommunityFormat.Iso(post.ExpiresAt), Audience = AudienceInput.From(post.Audience)
            };
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveNews(Guid? id, NewsInput input, string? submit, CancellationToken ct)
    {
        var request = new SaveNewsRequest(input.Title, input.Summary, input.Body, input.ImageUrl, input.Audience.ToRequest(),
            CommunityFormat.Parse(input.PublishAt), CommunityFormat.Parse(input.ExpiresAt), input.Pinned, submit != "draft");
        var raw = id is { } existing
            ? await _api.CommunitySendAsync(HttpMethod.Put, $"news/{existing}", request, ct)
            : await _api.CommunitySendAsync(HttpMethod.Post, "news", request, ct);
        if (raw.Ok && ReadCommunity<NewsPostDto>(raw) is { } saved)
        {
            TempData["PortalMessage"] = submit == "draft" ? "Saved as a draft." : saved.PublishAt > DateTime.UtcNow ? "The post is scheduled." : "The post is published.";
            return RedirectToAction(nameof(NewsPost), new { id = saved.Id });
        }
        ViewData["Title"] = id is null ? "Write news" : "Change news";
        return View(nameof(EditNews), new EditNewsPageModel
        {
            IsConnected = true, Id = id, Input = input, Options = await AudienceOptionsAsync(ct), Message = raw.Message ?? "The post could not be saved."
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> WithdrawNews(Guid id, CancellationToken ct)
    {
        var raw = await _api.CommunitySendAsync(HttpMethod.Post, $"news/{id}/withdraw", null, ct);
        TempData["PortalMessage"] = raw.Ok ? "The post is taken down." : raw.Message ?? "The post could not be changed.";
        return RedirectToAction(nameof(News), new { tab = "manage" });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Events
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> SchoolEvents(string? tab, CancellationToken ct)
    {
        ViewData["Title"] = "Events";
        var model = new EventsPageModel { IsConnected = _api.IsConnected(), CanWrite = CanWriteForSchool(), Message = TempData["PortalMessage"]?.ToString() };
        model.Tab = model.CanWrite && tab == "manage" ? "manage" : "upcoming";
        if (!model.IsConnected) return View(model);
        try
        {
            var raw = await _api.CommunitySendAsync(HttpMethod.Get, model.Tab == "manage" ? "events/manage" : "events", null, ct);
            if (raw.Ok) model.Events = ReadCommunity<List<EventDto>>(raw) ?? [];
            else model.Message ??= raw.Message ?? "The events could not be loaded.";
        }
        catch (Exception ex) { model.Message ??= ex.Message; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(Guid? id, CancellationToken ct)
    {
        ViewData["Title"] = id is null ? "New event" : "Change event";
        var model = new EditEventPageModel { IsConnected = _api.IsConnected(), Id = id };
        if (!model.IsConnected) return View(model);
        model.Options = await AudienceOptionsAsync(ct);
        if (model.Options is null) { TempData["PortalMessage"] = "Only teachers and admins write for the school."; return RedirectToAction(nameof(SchoolEvents)); }
        model.Forms = ReadCommunity<List<FormManageDto>>(await _api.CommunitySendAsync(HttpMethod.Get, "forms/manage", null, ct)) ?? [];
        if (id is { } existing)
        {
            var e = ReadCommunity<EventDto>(await _api.CommunitySendAsync(HttpMethod.Get, $"events/{existing}", null, ct));
            if (e is null || !e.CanManage) { TempData["PortalMessage"] = "Event not found."; return RedirectToAction(nameof(SchoolEvents)); }
            model.Input = new EventInput
            {
                Title = e.Title, Description = e.Description, Location = e.Location, Category = e.Category, StartsAt = CommunityFormat.Iso(e.StartsAt),
                EndsAt = CommunityFormat.Iso(e.EndsAt), AllDay = e.AllDay, RepliesOpen = e.RepliesOpen,
                ReplyBy = e.RepliesOpen && e.RepliesCloseAt != e.StartsAt ? CommunityFormat.Iso(e.RepliesCloseAt) : null, Capacity = e.Capacity,
                MaxGuests = e.MaxGuests, FormId = e.FormId, Audience = AudienceInput.From(e.Audience)
            };
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEvent(Guid? id, EventInput input, string? submit, CancellationToken ct)
    {
        string? error = null;
        var starts = CommunityFormat.Parse(input.StartsAt);
        var ends = CommunityFormat.Parse(input.EndsAt) ?? starts;
        if (starts is null) error = "Choose when the event starts.";
        if (error is null)
        {
            var request = new SaveEventRequest(input.Title, input.Description, input.Location, input.Category, starts!.Value, ends!.Value, input.AllDay,
                input.RepliesOpen, CommunityFormat.Parse(input.ReplyBy), input.Capacity, input.MaxGuests, input.FormId, input.Audience.ToRequest(), submit != "draft");
            var raw = id is { } existing
                ? await _api.CommunitySendAsync(HttpMethod.Put, $"events/{existing}", request, ct)
                : await _api.CommunitySendAsync(HttpMethod.Post, "events", request, ct);
            if (raw.Ok)
            {
                TempData["PortalMessage"] = submit == "draft" ? "Saved as a draft." : "The event is published.";
                return RedirectToAction(nameof(SchoolEvents), new { tab = "manage" });
            }
            error = raw.Message ?? "The event could not be saved.";
        }
        ViewData["Title"] = id is null ? "New event" : "Change event";
        return View(nameof(EditEvent), new EditEventPageModel
        {
            IsConnected = true, Id = id, Input = input, Options = await AudienceOptionsAsync(ct), Message = error,
            Forms = ReadCommunity<List<FormManageDto>>(await _api.CommunitySendAsync(HttpMethod.Get, "forms/manage", null, ct)) ?? []
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyEvent(Guid id, string? answer, int guests, string? note, CancellationToken ct)
    {
        var raw = await _api.CommunitySendAsync(HttpMethod.Post, $"events/{id}/reply", new EventReplyRequest(answer, guests, note), ct);
        TempData["PortalMessage"] = raw.Ok ? "Your reply is saved." : raw.Message ?? "Your reply could not be saved.";
        return Redirect(Url.Action(nameof(SchoolEvents)) + "#event-" + id);
    }

    [HttpGet]
    public async Task<IActionResult> EventReplies(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Event replies";
        var model = new EventRepliesPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        var raw = await _api.CommunitySendAsync(HttpMethod.Get, $"events/{id}/replies", null, ct);
        if (!raw.Ok) { TempData["PortalMessage"] = raw.Message ?? "Event not found."; return RedirectToAction(nameof(SchoolEvents), new { tab = "manage" }); }
        model.Replies = ReadCommunity<EventRepliesDto>(raw);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelEvent(Guid id, string? reason, CancellationToken ct)
    {
        var raw = await _api.CommunitySendAsync(HttpMethod.Post, $"events/{id}/cancel", new { reason }, ct);
        TempData["PortalMessage"] = raw.Ok ? "The event is cancelled. Everyone it was for has been told." : raw.Message ?? "The event could not be cancelled.";
        return RedirectToAction(nameof(SchoolEvents), new { tab = "manage" });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Forms
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> SchoolForms(string? tab, CancellationToken ct)
    {
        ViewData["Title"] = "Forms";
        var model = new FormsPageModel { IsConnected = _api.IsConnected(), CanWrite = CanWriteForSchool(), Message = TempData["PortalMessage"]?.ToString() };
        model.Tab = model.CanWrite && tab == "manage" ? "manage" : "fill";
        if (!model.IsConnected) return View(model);
        try
        {
            if (model.Tab == "manage")
            {
                var raw = await _api.CommunitySendAsync(HttpMethod.Get, "forms/manage", null, ct);
                if (raw.Ok) model.Managed = ReadCommunity<List<FormManageDto>>(raw) ?? [];
                else model.Message ??= raw.Message ?? "The forms could not be loaded.";
            }
            else
            {
                var raw = await _api.CommunitySendAsync(HttpMethod.Get, "forms", null, ct);
                if (raw.Ok) model.ToFill = ReadCommunity<List<FormToFillDto>>(raw) ?? [];
                else model.Message ??= raw.Message ?? "The forms could not be loaded.";
            }
        }
        catch (Exception ex) { model.Message ??= ex.Message; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> FillForm(Guid id, Guid? studentProfileId, CancellationToken ct)
    {
        ViewData["Title"] = "Forms";
        var model = new FillFormPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        var raw = await _api.CommunitySendAsync(HttpMethod.Get, $"forms/{id}" + (studentProfileId is { } s ? $"?studentProfileId={s}" : ""), null, ct);
        if (!raw.Ok) { TempData["PortalMessage"] = raw.Message ?? "Form not found."; return RedirectToAction(nameof(SchoolForms)); }
        model.Form = ReadCommunity<FormDto>(raw);
        return View(model);
    }

    /// <summary>Answers arrive as q_{field id}; a question with several choices sends one value per ticked choice.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitForm(Guid id, Guid? studentProfileId, CancellationToken ct)
    {
        var answers = new Dictionary<Guid, string?>();
        foreach (var (key, values) in Request.Form)
            if (key.StartsWith("q_", StringComparison.Ordinal) && Guid.TryParse(key[2..], out var field))
                answers[field] = string.Join("\n", values.Where(v => !string.IsNullOrWhiteSpace(v)));
        var raw = await _api.CommunitySendAsync(HttpMethod.Post, $"forms/{id}/responses", new SubmitFormRequest(studentProfileId, answers), ct);
        if (raw.Ok)
        {
            TempData["PortalMessage"] = "Thank you. Your answers are saved.";
            return RedirectToAction(nameof(SchoolForms));
        }
        ViewData["Title"] = "Forms";
        var form = ReadCommunity<FormDto>(await _api.CommunitySendAsync(HttpMethod.Get, $"forms/{id}" + (studentProfileId is { } s ? $"?studentProfileId={s}" : ""), null, ct));
        if (form is null) { TempData["PortalMessage"] = raw.Message ?? "Form not found."; return RedirectToAction(nameof(SchoolForms)); }
        return View(nameof(FillForm), new FillFormPageModel
        {
            IsConnected = true, Form = form, Message = raw.Message ?? "Your answers could not be saved.",
            Typed = answers.Where(a => a.Value is not null).ToDictionary(a => a.Key, a => a.Value!)
        });
    }

    [HttpGet]
    public async Task<IActionResult> EditForm(Guid? id, CancellationToken ct)
    {
        ViewData["Title"] = id is null ? "New form" : "Change form";
        var model = new EditFormPageModel { IsConnected = _api.IsConnected(), Id = id };
        if (!model.IsConnected) return View(model);
        model.Options = await AudienceOptionsAsync(ct);
        if (model.Options is null) { TempData["PortalMessage"] = "Only teachers and admins write for the school."; return RedirectToAction(nameof(SchoolForms)); }
        if (id is { } existing)
        {
            var form = ReadCommunity<FormDto>(await _api.CommunitySendAsync(HttpMethod.Get, $"forms/{existing}", null, ct));
            if (form is null || !form.CanManage) { TempData["PortalMessage"] = "Form not found."; return RedirectToAction(nameof(SchoolForms)); }
            model.Input = new FormInput
            {
                Title = form.Title, Description = form.Description, DueAt = CommunityFormat.Iso(form.DueAt), Audience = AudienceInput.From(form.Audience),
                Fields = form.Fields.Select(FieldInput.From).ToList()
            };
            var managed = ReadCommunity<List<FormManageDto>>(await _api.CommunitySendAsync(HttpMethod.Get, "forms/manage", null, ct)) ?? [];
            model.Answered = managed.FirstOrDefault(f => f.Id == existing)?.Answered > 0;
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveForm(Guid? id, FormInput input, string? submit, CancellationToken ct)
    {
        var fields = input.Fields.Where(f => !f.IsEmpty).Select(f => f.ToRequest()).ToList();
        var request = new SaveFormRequest(input.Title, input.Description, CommunityFormat.Parse(input.DueAt), input.Audience.ToRequest(), fields, submit == "open");
        var raw = id is { } existing
            ? await _api.CommunitySendAsync(HttpMethod.Put, $"forms/{existing}", request, ct)
            : await _api.CommunitySendAsync(HttpMethod.Post, "forms", request, ct);
        if (raw.Ok && ReadCommunity<FormDto>(raw) is { } saved)
        {
            TempData["PortalMessage"] = saved.Status == "Open" ? "The form is open for answers." : "Saved as a draft.";
            return RedirectToAction(nameof(FormResponses), new { id = saved.Id });
        }
        ViewData["Title"] = id is null ? "New form" : "Change form";
        return View(nameof(EditForm), new EditFormPageModel
        {
            IsConnected = true, Id = id, Input = input, Options = await AudienceOptionsAsync(ct), Message = raw.Message ?? "The form could not be saved.",
            Answered = raw.Status == 409
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OpenForm(Guid id, bool open, CancellationToken ct)
    {
        var raw = await _api.CommunitySendAsync(HttpMethod.Post, $"forms/{id}/{(open ? "open" : "close")}", null, ct);
        TempData["PortalMessage"] = raw.Ok ? open ? "The form is open for answers." : "The form is closed." : raw.Message ?? "The form could not be changed.";
        return RedirectToAction(nameof(FormResponses), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> FormResponses(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Form answers";
        var model = new FormResponsesPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        var raw = await _api.CommunitySendAsync(HttpMethod.Get, $"forms/{id}/tracking", null, ct);
        if (!raw.Ok) { TempData["PortalMessage"] = raw.Message ?? "Form not found."; return RedirectToAction(nameof(SchoolForms), new { tab = "manage" }); }
        model.Tracking = ReadCommunity<FormTrackingDto>(raw);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemindForm(Guid id, CancellationToken ct)
    {
        var raw = await _api.CommunitySendAsync(HttpMethod.Post, $"forms/{id}/remind", null, ct);
        var count = raw.Ok && !string.IsNullOrWhiteSpace(raw.Body) ? JsonDocument.Parse(raw.Body).RootElement.GetProperty("reminded").GetInt32() : 0;
        TempData["PortalMessage"] = raw.Ok
            ? count == 0 ? "Everyone has answered; nobody needed a reminder." : $"Reminder sent to {count} people."
            : raw.Message ?? "The reminder could not be sent.";
        return RedirectToAction(nameof(FormResponses), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> ExportForm(Guid id, CancellationToken ct)
    {
        var (status, content, type, name) = await _api.CommunityFileAsync($"forms/{id}/export", ct);
        if (status is < 200 or >= 300) { TempData["PortalMessage"] = "The answers could not be downloaded."; return RedirectToAction(nameof(FormResponses), new { id }); }
        return File(content, type, name ?? "form-answers.csv");
    }
}
