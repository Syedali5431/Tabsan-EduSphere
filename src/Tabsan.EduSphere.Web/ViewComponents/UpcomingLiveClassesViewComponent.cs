using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.LiveClasses;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.ViewComponents;

/// <summary>
/// Plan M10.2 — the user's live classes. As a card (dashboard, academic calendar) it lists the next two weeks with Join and
/// "Add to calendar"; as a banner (every page, from the layout) it appears only while one of the user's classes is open for
/// joining. The list is kept in the session for a minute so the banner does not call the API on every page.
/// Renders nothing when there are none, the module is off or nobody is signed in.
/// </summary>
public sealed class UpcomingLiveClassesViewComponent : ViewComponent
{
    private const string CacheKey = "live-classes:upcoming";
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly IEduApiClient _api;

    public UpcomingLiveClassesViewComponent(IEduApiClient api) => _api = api;

    public async Task<IViewComponentResult> InvokeAsync(bool banner = false)
    {
        if (!_api.IsConnected() || _api.GetSessionIdentity() is null) return Content(string.Empty);
        try
        {
            var sessions = await SessionsAsync();
            if (banner) sessions = sessions.Where(s => s.CanJoin).ToList();
            if (sessions.Count == 0) return Content(string.Empty);
            var model = new UpcomingLiveClassesModel { Sessions = sessions, ReturnUrl = HttpContext.Request.Path + HttpContext.Request.QueryString };
            return banner ? View("Banner", model) : View(model);
        }
        catch (Exception)
        {
            return Content(string.Empty);
        }
    }

    private async Task<List<LiveSessionDto>> SessionsAsync()
    {
        var session = HttpContext.Session;
        var cached = session.GetString(CacheKey);
        if (cached is not null && cached.IndexOf('|') is var bar and > 0
            && long.TryParse(cached[..bar], out var ticks) && DateTime.UtcNow.Ticks - ticks < TimeSpan.FromMinutes(1).Ticks)
            return JsonSerializer.Deserialize<List<LiveSessionDto>>(cached[(bar + 1)..], Web) ?? [];

        var raw = await _api.LiveSendAsync(HttpMethod.Get, "upcoming", null, HttpContext.RequestAborted);
        var body = raw.Ok ? raw.Body : "[]";
        session.SetString(CacheKey, $"{DateTime.UtcNow.Ticks}|{body}");
        return JsonSerializer.Deserialize<List<LiveSessionDto>>(body, Web) ?? [];
    }
}
