using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.DTOs.Backup;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>Phase 8: Backup Verification — integrity checks and restore tests. Admin/SuperAdmin only.</summary>
[ApiController]
[Route("api/v1/backup-verifications")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class BackupVerificationController : ControllerBase
{
    private readonly IBackupVerificationService _service;
    public BackupVerificationController(IBackupVerificationService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await _service.GetAllAsync(ct));

    [HttpGet("by-backup/{backupLogId:guid}")]
    public async Task<IActionResult> GetByBackup(Guid backupLogId, CancellationToken ct) => Ok(await _service.GetByBackupLogIdAsync(backupLogId, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVerificationRequest r, CancellationToken ct)
    {
        var result = await _service.CreateAsync(r, ct);
        return CreatedAtAction(nameof(GetAll), null, result);
    }
}
