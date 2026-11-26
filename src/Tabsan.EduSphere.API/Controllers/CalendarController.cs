using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Calendar;
using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// REST API for the calendar: key dates (Phase 12; GET for all signed-in users, writes for Admin/SuperAdmin), the person's
/// calendar of homework, quizzes, live classes and key dates (<c>events</c>), and their private calendar link (<c>feed</c>),
/// which Google Calendar, Outlook or a phone subscribes to without signing in.
/// </summary>
[ApiController]
[Route("api/v1/calendar")]
[Authorize]
public class CalendarController : ControllerBase
{
    private readonly IAcademicCalendarService _calendar;
    private readonly CalendarService _events;

    public CalendarController(IAcademicCalendarService calendar, CalendarService events)
    {
        _calendar = calendar;
        _events = events;
    }

    // ── The person's calendar ─────────────────────────────────────────────────

    /// <summary>
    /// Entries between ?from= and ?to= (UTC, at most 400 days; default: the next 30 days): a student's homework and quizzes
    /// (with their own dates and whether they are done), live classes and key dates; a teacher's courses; a parent's children
    /// (all, or ?studentProfileId= of one); key dates for admins.
    /// </summary>
    [HttpGet("events")]
    public async Task<IActionResult> Events([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? studentProfileId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var start = from ?? now.Date;
        var (response, error) = await _events.EventsAsync(UserId(), Role(), studentProfileId, start, to ?? start.AddDays(30), now, ct);
        if (error is not null) return BadRequest(new { message = error });
        return response is null ? StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot see this student's calendar." }) : Ok(response);
    }

    /// <summary>The person's private calendar link, or 204 when they have none.</summary>
    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed(CancellationToken ct)
        => await _events.GetFeedAsync(UserId(), ct) is { } feed ? Ok(feed) : NoContent();

    /// <summary>Creates the private calendar link, or replaces it (the old link stops working).</summary>
    [HttpPost("feed")]
    public async Task<IActionResult> ResetFeed(CancellationToken ct) => Ok(await _events.ResetFeedAsync(UserId(), ct));

    /// <summary>Turns the private calendar link off.</summary>
    [HttpDelete("feed")]
    public async Task<IActionResult> RemoveFeed(CancellationToken ct) => await _events.RemoveFeedAsync(UserId(), ct) ? NoContent() : NotFound();

    /// <summary>The calendar as iCalendar for subscribing apps: the link itself is the key (no sign-in).</summary>
    [HttpGet("feed/{token}.ics")]
    [AllowAnonymous]
    public async Task<IActionResult> Feed(string token, CancellationToken ct)
    {
        var ics = await _events.FeedAsync(token, DateTime.UtcNow, ct);
        if (ics is null) return NotFound();
        Response.Headers.CacheControl = "private, max-age=900";
        Response.Headers["X-Robots-Tag"] = "noindex";
        return File(Encoding.UTF8.GetBytes(ics), "text/calendar; charset=utf-8");
    }

    private Guid UserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    private string Role() => User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;

    // ── Read endpoints (all authenticated roles) ──────────────────────────────

    /// <summary>Returns all active deadlines across all semesters ordered by date.</summary>
    [HttpGet("deadlines")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _calendar.GetAllAsync(ct);
        return Ok(items);
    }

    /// <summary>Returns active deadlines for a specific semester.</summary>
    [HttpGet("deadlines/by-semester/{semesterId:guid}")]
    public async Task<IActionResult> GetBySemester(Guid semesterId, CancellationToken ct)
    {
        var items = await _calendar.GetBySemesterAsync(semesterId, ct);
        return Ok(items);
    }

    /// <summary>Returns the detail of a single deadline by ID.</summary>
    [HttpGet("deadlines/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await _calendar.GetByIdAsync(id, ct);
        if (item is null) return NotFound();
        return Ok(item);
    }

    // ── Write endpoints (Admin / SuperAdmin only) ─────────────────────────────

    /// <summary>Creates a new academic deadline.</summary>
    [HttpPost("deadlines")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateDeadlineRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title is required.");

        var created = await _calendar.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates an existing academic deadline.</summary>
    [HttpPut("deadlines/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDeadlineRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title is required.");

        var updated = await _calendar.UpdateAsync(id, request, ct);
        if (updated is null) return NotFound();
        return Ok(updated);
    }

    /// <summary>Soft-deletes an academic deadline.</summary>
    [HttpDelete("deadlines/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _calendar.DeleteAsync(id, ct);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
