using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.DTOs.Attendance;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Manages attendance marking, correction, and reporting.
/// Faculty: mark and bulk-mark daily attendance for their offerings.
/// Admin: correct records and view any student's attendance.
/// Students: view their own attendance records and percentages.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _service;
    public AttendanceController(IAttendanceService service) => _service = service;

    // ── Marking ───────────────────────────────────────────────────────────────

    /// <summary>Records attendance for a single student for one session (Faculty/Admin).</summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Mark([FromBody] MarkAttendanceRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var ok = await _service.MarkAsync(request, userId, tenantId ?? GetCurrentTenantId(), campusId ?? GetCurrentCampusId(), ct);
        return ok ? Ok() : Conflict("Attendance already recorded for this student / offering / date.");
    }

    /// <summary>Bulk-marks attendance for a full class for one session (Faculty/Admin).</summary>
    [HttpPost("bulk")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> BulkMark([FromBody] BulkMarkAttendanceRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var inserted = await _service.BulkMarkAsync(request, userId, tenantId ?? GetCurrentTenantId(), campusId ?? GetCurrentCampusId(), ct);
        return Ok(new { inserted });
    }

    /// <summary>Corrects an existing attendance record (Faculty/Admin).</summary>
    [HttpPut("correct")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Correct([FromBody] CorrectAttendanceRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var ok = await _service.CorrectAsync(request, userId, tenantId ?? GetCurrentTenantId(), campusId ?? GetCurrentCampusId(), ct);
        return ok ? NoContent() : NotFound("Attendance record not found.");
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns attendance records for a course offering (Faculty/Admin).
    /// Supports optional date range filtering via ?from=&amp;to= query parameters.
    /// </summary>
    [HttpGet("by-offering/{courseOfferingId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetByOffering(
        Guid courseOfferingId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        var records = await _service.GetByOfferingAsync(courseOfferingId, from, to, tenantId ?? GetCurrentTenantId(), campusId ?? GetCurrentCampusId(), ct);
        return Ok(records);
    }

    /// <summary>
    /// Returns attendance records for a specific student (Admin/Faculty).
    /// Optionally scoped to a single course offering via ?courseOfferingId=.
    /// </summary>
    [HttpGet("by-student/{studentProfileId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetByStudent(
        Guid studentProfileId,
        [FromQuery] Guid? courseOfferingId,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        CancellationToken ct)
    {
        var records = await _service.GetByStudentAsync(studentProfileId, courseOfferingId, tenantId ?? GetCurrentTenantId(), campusId ?? GetCurrentCampusId(), ct);
        return Ok(records);
    }

    /// <summary>Returns the current student's own attendance records (Student).</summary>
    [HttpGet("my-attendance")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyAttendance([FromQuery] Guid? courseOfferingId, CancellationToken ct)
    {
        var studentProfileId = GetCurrentStudentProfileId();
        if (studentProfileId == Guid.Empty) return Unauthorized();

        var records = await _service.GetByStudentAsync(studentProfileId, courseOfferingId, GetCurrentTenantId(), GetCurrentCampusId(), ct);
        return Ok(records);
    }

    /// <summary>Returns the attendance percentage summary for a student in one offering (All roles).</summary>
    [HttpGet("summary/{studentProfileId:guid}/{courseOfferingId:guid}")]
    public async Task<IActionResult> GetSummary(Guid studentProfileId, Guid courseOfferingId, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var summary = await _service.GetSummaryAsync(studentProfileId, courseOfferingId, tenantId ?? GetCurrentTenantId(), campusId ?? GetCurrentCampusId(), ct);
        return Ok(summary);
    }

    /// <summary>Returns all students below the given attendance threshold percentage (Admin only).</summary>
    [HttpGet("below-threshold")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetBelowThreshold([FromQuery] double threshold = 75.0, [FromQuery] Guid? tenantId = null, [FromQuery] Guid? campusId = null, CancellationToken ct = default)
    {
        const double fixedThreshold = 75.0;
        var results = await _service.GetBelowThresholdAsync(fixedThreshold, tenantId ?? GetCurrentTenantId(), campusId ?? GetCurrentCampusId(), ct);
        return Ok(results.Select(x => new
        {
            StudentProfileId    = x.StudentProfileId,
            CourseOfferingId    = x.CourseOfferingId,
            AttendancePercent   = x.AttendancePercent
        }));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Extracts the authenticated user's ID from JWT claims.</summary>
    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    /// <summary>Extracts the student profile ID from the "studentProfileId" JWT claim.</summary>
    private Guid GetCurrentStudentProfileId()
    {
        var claim = User.FindFirst("studentProfileId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private Guid? GetCurrentTenantId()
    {
        var claim = User.FindFirst("tenant_id")?.Value ?? User.FindFirst("tenantId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private Guid? GetCurrentCampusId()
    {
        var claim = User.FindFirst("campus_id")?.Value ?? User.FindFirst("campusId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
