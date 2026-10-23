using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.LiveClasses;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Plan M10 — live classes under /api/v1/live, gated by the licensed "live_classes" module. Settings: Admin/SuperAdmin
/// (SuperAdmin also the installation default). Scheduling, attendance: staff of the course's tenant/campus. Students:
/// classes of courses they are enrolled in.
/// </summary>
[ApiController]
[Route("api/v1/live")]
[Authorize]
public sealed class LiveClassesController : ControllerBase
{
    private const string Staff = "SuperAdmin,Admin,Faculty";

    private readonly LiveClassService _service;
    private readonly ILiveClassRepository _repo;
    private readonly IAccessScopeResolver _scope;
    private readonly ApplicationDbContext _db;

    public LiveClassesController(LiveClassService service, ILiveClassRepository repo, IAccessScopeResolver scope, ApplicationDbContext db)
    {
        _service = service;
        _repo = repo;
        _scope = scope;
        _db = db;
    }

    // ── settings ──

    [HttpGet("settings")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetSettings([FromQuery] bool installationDefault, CancellationToken ct)
    {
        var (tenant, denied) = TenantFor(installationDefault);
        if (denied is not null) return denied;
        return Ok(await _service.GetSettingsAsync(tenant, ct));
    }

    [HttpPut("settings")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> SaveSettings([FromBody] SaveConferenceSettingsRequest request, [FromQuery] bool installationDefault, CancellationToken ct)
    {
        var (tenant, denied) = TenantFor(installationDefault);
        if (denied is not null) return denied;
        return ToResult(await _service.SaveSettingsAsync(tenant, request, UserId(), ct));
    }

    [HttpPost("settings/test")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Test([FromQuery] bool installationDefault, CancellationToken ct)
    {
        var (tenant, denied) = TenantFor(installationDefault);
        if (denied is not null) return denied;
        var result = await _service.TestAsync(tenant, ct);
        return result.Ok ? Ok(new { message = result.Value }) : StatusCode(result.Status, new { message = result.Error });
    }

    private (Guid? Tenant, IActionResult? Denied) TenantFor(bool installationDefault)
    {
        var tenant = _scope.GetTenantId();
        if (_scope.IsSuperAdmin()) return (installationDefault ? null : tenant, null);
        return installationDefault ? (null, Forbid()) : (tenant, null);
    }

    // ── sessions ──

    [HttpGet("upcoming")]
    public async Task<IActionResult> Upcoming(CancellationToken ct)
    {
        var student = User.IsInRole("Student") ? await StudentAsync(ct) : null;
        return Ok(await _service.UpcomingAsync(UserId(), student, DateTime.UtcNow, ct));
    }

    [HttpGet("offerings/{offeringId:guid}/sessions")]
    public async Task<IActionResult> Sessions(Guid offeringId, CancellationToken ct)
    {
        if (await ParticipantDeniedAsync(offeringId, ct) is { } denied) return denied;
        return Ok(await _service.SessionsAsync(offeringId, ct));
    }

    [HttpGet("offerings/{offeringId:guid}/timetable")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Timetable(Guid offeringId, CancellationToken ct)
    {
        if (await OfferingDeniedAsync(offeringId, ct) is { } denied) return denied;
        return Ok(await _service.TimetableSlotsAsync(offeringId, ct));
    }

    [HttpPost("offerings/{offeringId:guid}/sessions")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Schedule(Guid offeringId, [FromBody] ScheduleLiveSessionRequest request, CancellationToken ct)
    {
        if (await OfferingDeniedAsync(offeringId, ct) is { } denied) return denied;
        return ToResult(await _service.ScheduleAsync(offeringId, request, UserId(), ct));
    }

    [HttpPost("offerings/{offeringId:guid}/sessions/from-timetable")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> ScheduleFromTimetable(Guid offeringId, [FromBody] ScheduleFromTimetableRequest request, CancellationToken ct)
    {
        if (await OfferingDeniedAsync(offeringId, ct) is { } denied) return denied;
        return ToResult(await _service.ScheduleFromTimetableAsync(offeringId, request, UserId(), DateTime.UtcNow, ct));
    }

    [HttpPut("sessions/{sessionId:guid}")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Update(Guid sessionId, [FromBody] ScheduleLiveSessionRequest request, CancellationToken ct)
    {
        if (await SessionDeniedAsync(sessionId, ct) is { } denied) return denied;
        return ToResult(await _service.UpdateAsync(sessionId, request, UserId(), ct));
    }

    [HttpPost("sessions/{sessionId:guid}/cancel")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Cancel(Guid sessionId, CancellationToken ct)
    {
        if (await SessionDeniedAsync(sessionId, ct) is { } denied) return denied;
        var result = await _service.CancelAsync(sessionId, UserId(), ct);
        return result.Ok ? NoContent() : StatusCode(result.Status, new { message = result.Error });
    }

    [HttpPost("sessions/{sessionId:guid}/join")]
    public async Task<IActionResult> Join(Guid sessionId, CancellationToken ct)
    {
        if (await _service.OfferingOfSessionAsync(sessionId, ct) is not { } offeringId) return NotFound();
        if (await ParticipantDeniedAsync(offeringId, ct) is { } denied) return denied;
        var userId = UserId();
        var name = await _db.Users.IgnoreQueryFilters().AsNoTracking().Where(u => u.Id == userId).Select(u => u.FullName ?? u.Username).FirstOrDefaultAsync(ct)
            ?? User.FindFirstValue(ClaimTypes.Name) ?? "Participant";
        var student = IsStaff() ? null : await StudentAsync(ct);
        return ToResult(await _service.JoinAsync(sessionId, userId, student, name, IsStaff(), DateTime.UtcNow, ct));
    }

    [HttpGet("sessions/{sessionId:guid}/calendar")]
    public async Task<IActionResult> Calendar(Guid sessionId, CancellationToken ct)
    {
        if (await _service.OfferingOfSessionAsync(sessionId, ct) is not { } offeringId) return NotFound();
        if (await ParticipantDeniedAsync(offeringId, ct) is { } denied) return denied;
        var result = await _service.CalendarAsync(sessionId, ct);
        return result.Ok ? File(Encoding.UTF8.GetBytes(result.Value!), "text/calendar", $"live-class-{sessionId:N}.ics") : NotFound();
    }

    [HttpGet("sessions/{sessionId:guid}/attendance")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Attendance(Guid sessionId, CancellationToken ct)
    {
        if (await SessionDeniedAsync(sessionId, ct) is { } denied) return denied;
        return await _service.AttendanceReportAsync(sessionId, ct) is { } report ? Ok(report) : NotFound();
    }

    [HttpPost("sessions/{sessionId:guid}/attendance")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> TakeAttendance(Guid sessionId, CancellationToken ct)
    {
        if (await SessionDeniedAsync(sessionId, ct) is { } denied) return denied;
        var result = await _service.TakeAttendanceAsync(sessionId, UserId(), DateTime.UtcNow, ct);
        return result.Ok ? Ok(new { marked = result.Value }) : StatusCode(result.Status, new { message = result.Error });
    }

    // ── helpers ──

    private bool IsStaff() => User.IsInRole("Faculty") || User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    private async Task<Guid?> StudentAsync(CancellationToken ct)
    {
        if (Guid.TryParse(User.FindFirstValue("studentProfileId"), out var id)) return id;
        var userId = UserId();
        return await _db.StudentProfiles.AsNoTracking().Where(s => s.UserId == userId).Select(s => (Guid?)s.Id).FirstOrDefaultAsync(ct);
    }

    private async Task<IActionResult?> SessionDeniedAsync(Guid sessionId, CancellationToken ct)
        => await _service.OfferingOfSessionAsync(sessionId, ct) is { } offeringId ? await OfferingDeniedAsync(offeringId, ct) : NotFound();

    /// <summary>Staff within the tenant/campus, or students enrolled in the course.</summary>
    private async Task<IActionResult?> ParticipantDeniedAsync(Guid offeringId, CancellationToken ct)
    {
        if (IsStaff()) return await OfferingDeniedAsync(offeringId, ct);
        if (await _repo.GetOfferingAsync(offeringId, ct) is null) return NotFound();
        return await StudentAsync(ct) is { } sp && await _repo.IsEnrolledAsync(offeringId, sp, ct) ? null : Forbid();
    }

    private async Task<IActionResult?> OfferingDeniedAsync(Guid offeringId, CancellationToken ct)
    {
        var o = await _repo.GetOfferingAsync(offeringId, ct);
        if (o is null) return NotFound();
        if (_scope.IsSuperAdmin()) return null;
        var tenant = _scope.GetTenantId();
        var campus = _scope.GetCampusId();
        if (tenant.HasValue && o.TenantId != tenant) return Forbid();
        if (campus.HasValue && o.CampusId != campus) return Forbid();
        return null;
    }

    private IActionResult ToResult<T>(LiveResult<T> result)
        => result.Ok ? StatusCode(result.Status, result.Value) : StatusCode(result.Status, new { message = result.Error });
}
