using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Calendar;
using Tabsan.EduSphere.Web.Models.Portal;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// The calendar: a month of the person's homework due dates, quizzes, live classes and key dates (the page lays them out in
/// the browser's time zone), and the private link that puts them in Google Calendar, Outlook or a phone's calendar.
/// </summary>
public partial class PortalController
{
    private static readonly JsonSerializerOptions CalendarJson = new(JsonSerializerDefaults.Web);

    /// <summary>?month=2026-11 (default: this month).</summary>
    [HttpGet]
    public async Task<IActionResult> AcademicCalendar(string? month, CancellationToken ct)
    {
        ViewData["Title"] = "Calendar";
        var today = DateTime.UtcNow;
        var first = DateOnly.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var m)
            ? new DateOnly(m.Year, m.Month, 1)
            : new DateOnly(today.Year, today.Month, 1);
        var identity = _api.GetSessionIdentity();
        var model = new AcademicCalendarPageModel
        {
            IsConnected = _api.IsConnected(),
            Month = first,
            CanManageKeyDates = identity is { IsAdmin: true } or { IsSuperAdmin: true },
            Message = TempData["PortalMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            // The grid shows up to six weeks around the month, and time zones move entries a day either way.
            var from = first.AddDays(-8).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var to = first.AddMonths(1).AddDays(15).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var raw = await _api.CalendarSendAsync(HttpMethod.Get,
                $"events?from={from:yyyy-MM-ddTHH:mm:ssZ}&to={to:yyyy-MM-ddTHH:mm:ssZ}", null, ct);
            if (raw.Ok && !string.IsNullOrWhiteSpace(raw.Body))
                model.Events = JsonSerializer.Deserialize<CalendarEventsResponse>(raw.Body, CalendarJson)?.Events.ToList() ?? [];
            else if (!raw.Ok) model.Message ??= raw.Message ?? "The calendar could not be loaded.";

            var feed = await _api.CalendarSendAsync(HttpMethod.Get, "feed", null, ct);
            if (feed.Ok && !string.IsNullOrWhiteSpace(feed.Body) && JsonSerializer.Deserialize<CalendarFeedDto>(feed.Body, CalendarJson) is { } link)
            {
                model.FeedUrl = FeedUrl(link.Token);
                model.FeedCreatedAt = link.CreatedAt;
                model.FeedLastUsedAt = link.LastUsedAt;
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    /// <summary>Creates the calendar link, or replaces it (the old one stops working).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetCalendarFeed(string? month, bool replace, CancellationToken ct)
    {
        var raw = await _api.CalendarSendAsync(HttpMethod.Post, "feed", null, ct);
        TempData["PortalMessage"] = raw.Ok
            ? replace ? "A new calendar link is ready. The old link no longer works: subscribe again with the new one." : "Your calendar link is ready."
            : raw.Message ?? "The calendar link could not be created.";
        return RedirectToAction(nameof(AcademicCalendar), new { month });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveCalendarFeed(string? month, CancellationToken ct)
    {
        var raw = await _api.CalendarSendAsync(HttpMethod.Delete, "feed", null, ct);
        TempData["PortalMessage"] = raw.Ok || raw.Status == 404
            ? "Your calendar link is turned off. Calendars subscribed to it stop updating."
            : raw.Message ?? "The calendar link could not be turned off.";
        return RedirectToAction(nameof(AcademicCalendar), new { month });
    }

    /// <summary>The link at the portal's public address (behind a proxy, from the forwarded host).</summary>
    private string FeedUrl(string token) => $"{Request.Scheme}://{Request.Host}{Request.PathBase}/calendar/{token}.ics";
}
