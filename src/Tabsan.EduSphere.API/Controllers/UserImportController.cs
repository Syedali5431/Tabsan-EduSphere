using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Allows SuperAdmin and Admin to import user accounts in bulk via a CSV file (P4-S1-01).
/// Routes: /api/v1/user-import
/// </summary>
[ApiController]
[Route("api/v1/user-import")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class UserImportController : ControllerBase
{
    private readonly IUserImportService _importService;

    public UserImportController(IUserImportService importService)
    {
        _importService = importService;
    }

    // ── POST /api/v1/user-import/csv ─────────────────────────────────────────

    /// <summary>
    /// Imports user accounts from a CSV file.
    /// Expected CSV header: Username,Email,FullName,Role[,DepartmentId]
    /// On import: initial password = Username; MustChangePassword is set to true (P4-S2-01/02).
    /// Returns a summary: total rows, imported, duplicates, errors.
    /// </summary>
    [HttpPost("csv")]
    public async Task<IActionResult> ImportCsv(IFormFile file, [FromQuery] bool strictMode = false, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No CSV file provided." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".csv")
            return BadRequest(new { message = "Only .csv files are accepted." });

        await using var stream = file.OpenReadStream();
        var result = await _importService.ImportFromCsvAsync(stream, strictMode, ct);
        return Ok(result);
    }
}
