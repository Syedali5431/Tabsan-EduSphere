using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Mobile;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Plan M12 — endpoints made for the EduSphere mobile app: configuration before sign-in (white-label branding, versions,
/// languages, single sign-on providers), the sign-in hand-off from the portal (PKCE), the home screen, the timetable,
/// a parent's children, device registration for native push, and crash reports. Everything else the app shows comes
/// from the regular endpoints (courses, materials, assignments, results, attendance, notifications, parent portal).
/// </summary>
[ApiController]
[Route("api/v1/mobile")]
[Authorize]
public sealed class MobileController : ControllerBase
{
    private readonly IMobileService _mobile;
    private readonly IMobileQueries _queries;

    public MobileController(IMobileService mobile, IMobileQueries queries)
    {
        _mobile = mobile;
        _queries = queries;
    }

    /// <summary>Branding, versions, languages and sign-in options — the app reads this before anyone signs in.</summary>
    [HttpGet("config")]
    [AllowAnonymous]
    public async Task<IActionResult> Config([FromQuery] string? tenant, CancellationToken ct)
        => Ok(await _mobile.GetConfigAsync(tenant, ct));

    /// <summary>The portal, right after a single sign-on started from the app, asks for a one-time code for the app.</summary>
    [HttpPost("handoff")]
    public async Task<IActionResult> CreateHandoff([FromBody] HandoffRequest request, CancellationToken ct)
    {
        try { return Ok(await _mobile.CreateHandoffAsync(UserId(), request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>The app exchanges the one-time code (with its PKCE verifier) for its own tokens.</summary>
    [HttpPost("handoff/redeem")]
    [AllowAnonymous]
    public async Task<IActionResult> RedeemHandoff([FromBody] HandoffRedeemRequest request, CancellationToken ct)
    {
        var result = await _mobile.RedeemHandoffAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        if (result is null) return BadRequest(new { error = "INVALID_HANDOFF", message = "The sign-in code is invalid or has expired. Please sign in again." });
        return result.IsSuccess
            ? Ok(result.Response)
            : StatusCode(StatusCodes.Status403Forbidden, new { error = result.FailureReason?.ToString(), message = "Your EduSphere account is inactive or locked. Contact your administrator." });
    }

    /// <summary>
    /// The home screen for the signed-in role (today's classes, what is due, announcements, summaries).
    /// <paramref name="day"/>: today on the phone, 0 Sunday … 6 Saturday (without it, today in UTC).
    /// </summary>
    [HttpGet("home")]
    public async Task<IActionResult> Home([FromHeader(Name = "X-App-Version")] string? appVersion, [FromQuery] int? day, CancellationToken ct)
    {
        if (appVersion is not null && !_mobile.IsSupportedVersion(appVersion))
            return StatusCode(StatusCodes.Status426UpgradeRequired, new { error = "UPDATE_REQUIRED", message = "Please update the EduSphere app to continue." });
        if (day is < 0 or > 6) return BadRequest(new { message = "day must be 0 (Sunday) to 6 (Saturday)." });
        return Ok(await _queries.HomeAsync(UserId(), Role(), day, ct));
    }

    /// <summary>The week's classes: a student's enrolled courses, or the classes a teacher teaches.</summary>
    [HttpGet("timetable")]
    public async Task<IActionResult> Timetable(CancellationToken ct) => Ok(await _queries.TimetableAsync(UserId(), Role(), ct));

    /// <summary>The courses the user studies (student) or teaches (teacher).</summary>
    [HttpGet("courses")]
    public async Task<IActionResult> Courses(CancellationToken ct) => Ok(await _queries.CoursesAsync(UserId(), Role(), ct));

    /// <summary>One course: materials, assignments (with the student's own state), quizzes and announcements.</summary>
    [HttpGet("courses/{offeringId:guid}")]
    public async Task<IActionResult> Course(Guid offeringId, CancellationToken ct)
        => await _queries.CourseAsync(UserId(), Role(), offeringId, ct) is { } course ? Ok(course) : NotFound(new { message = "Course offering not found." });

    /// <summary>Published results with course names (students: own; parents: ?studentProfileId= of a linked child).</summary>
    [HttpGet("results")]
    public async Task<IActionResult> Results([FromQuery] Guid? studentProfileId, CancellationToken ct)
        => await _queries.ResultsAsync(UserId(), Role(), studentProfileId, ct) is { } rows ? Ok(rows) : StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot see this student's results." });

    /// <summary>Attendance per course (students: own; parents: ?studentProfileId= of a linked child).</summary>
    [HttpGet("attendance")]
    public async Task<IActionResult> Attendance([FromQuery] Guid? studentProfileId, CancellationToken ct)
        => await _queries.AttendanceAsync(UserId(), Role(), studentProfileId, ct) is { } rows ? Ok(rows) : StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot see this student's attendance." });

    /// <summary>Report cards, newest first (students: own; parents: ?studentProfileId= of a linked child).</summary>
    [HttpGet("report-cards")]
    public async Task<IActionResult> ReportCards([FromQuery] Guid? studentProfileId, CancellationToken ct)
        => await _queries.ReportCardsAsync(UserId(), Role(), studentProfileId, ct) is { } rows ? Ok(rows) : StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot see this student's report cards." });

    /// <summary>Announcements of the student's courses, newest first (students: own; parents: ?studentProfileId= of a linked child).</summary>
    [HttpGet("announcements")]
    public async Task<IActionResult> Announcements([FromQuery] Guid? studentProfileId, CancellationToken ct)
        => await _queries.StudentAnnouncementsAsync(UserId(), Role(), studentProfileId, ct) is { } rows ? Ok(rows) : StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot see this student's announcements." });

    /// <summary>A parent's linked children with attendance, CGPA (University) or average marks, fees and results at a glance.</summary>
    [HttpGet("children")]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> Children(CancellationToken ct) => Ok(await _queries.ChildrenAsync(UserId(), ct));

    [HttpGet("devices")]
    public async Task<IActionResult> Devices(CancellationToken ct) => Ok(await _mobile.GetDevicesAsync(UserId(), ct));

    /// <summary>Registers this phone for push notifications (or moves the token to the user now signed in).</summary>
    [HttpPost("devices")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request, CancellationToken ct)
    {
        try { return Ok(await _mobile.RegisterDeviceAsync(UserId(), request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Stops push to this phone (sign-out).</summary>
    [HttpPost("devices/unregister")]
    public async Task<IActionResult> UnregisterDevice([FromBody] RegisterDeviceRequest request, CancellationToken ct)
        => await _mobile.UnregisterDeviceAsync(UserId(), request.Token, ct) ? NoContent() : NotFound();

    /// <summary>An error the app could not handle (self-hosted crash reporting).</summary>
    [HttpPost("crash-reports")]
    [RequestSizeLimit(64 * 1024)]
    public async Task<IActionResult> CrashReport([FromBody] CrashReportRequest request, CancellationToken ct)
    {
        try { return Ok(new { id = await _mobile.ReportCrashAsync(UserId(), request, ct) }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    private Guid UserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    private string Role() => User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;
}
