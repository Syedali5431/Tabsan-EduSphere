using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// The private calendar link at the portal's own address (/calendar/&lt;token&gt;.ics), which Google Calendar, Outlook and
/// phones subscribe to. The link alone authorises it (no session or cookies); the API builds the calendar.
/// </summary>
[Route("calendar")]
public sealed partial class CalendarFeedController : Controller
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public CalendarFeedController(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    [HttpGet("{token}.ics")]
    public async Task<IActionResult> Feed(string token, CancellationToken ct)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex";
        if (!TokenPattern().IsMatch(token)) return NotFound();
        using var response = await _http.CreateClient("EduApi")
            .GetAsync($"{(_config["EduApi:BaseUrl"] ?? "").TrimEnd('/')}/api/v1/calendar/feed/{token}.ics", ct);
        if (!response.IsSuccessStatusCode) return response.StatusCode == System.Net.HttpStatusCode.NotFound ? NotFound() : StatusCode(502);
        Response.Headers.CacheControl = "private, max-age=900";
        return File(await response.Content.ReadAsByteArrayAsync(ct), "text/calendar; charset=utf-8");
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{43}$")]
    private static partial Regex TokenPattern();
}
