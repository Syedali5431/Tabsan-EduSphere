using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.DTOs.Backup;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Phase 4: Backup & DR — record, query, and monitor database backup operations.
/// ISO 27001 A.17.1.2 — documented backup procedures with verifiable logs.
/// Admin and SuperAdmin only.
/// </summary>
[ApiController]
[Route("api/v1/backup")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class BackupController : ControllerBase
{
    private readonly IBackupService _backupService;

    public BackupController(IBackupService backupService) => _backupService = backupService;

    /// <summary>GET /api/v1/backup/logs — paged backup history with filters.</summary>
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] BackupLogFilterDto filter, CancellationToken ct)
    {
        if (filter.ToUtc.HasValue && filter.FromUtc.HasValue && filter.ToUtc.Value < filter.FromUtc.Value)
            return BadRequest(new { message = "toUtc must be >= fromUtc." });

        var result = await _backupService.GetLogsAsync(filter, ct);
        return Ok(result);
    }

    /// <summary>POST /api/v1/backup/logs — record the start of a backup operation.</summary>
    [HttpPost("logs")]
    public async Task<IActionResult> RecordBackup([FromBody] RecordBackupRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.BackupType) || string.IsNullOrWhiteSpace(request.FileName))
            return BadRequest(new { message = "BackupType and FileName are required." });

        var result = await _backupService.RecordBackupStartAsync(request, ct);
        return CreatedAtAction(nameof(GetLogs), new { id = result.Id }, result);
    }

    /// <summary>PUT /api/v1/backup/logs/{id} — update backup status (Completed/Failed/Verified).</summary>
    [HttpPut("logs/{id:guid}")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateBackupStatusRequest request, CancellationToken ct)
    {
        var result = await _backupService.UpdateBackupStatusAsync(id, request, ct);
        if (result is null) return NotFound(new { message = "Backup record not found or invalid status." });
        return Ok(result);
    }

    /// <summary>GET /api/v1/backup/status — latest backup summary for monitoring dashboard.</summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var summary = await _backupService.GetStatusSummaryAsync(ct);
        return Ok(summary);
    }
}
