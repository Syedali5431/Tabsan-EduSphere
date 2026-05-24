using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;

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
    private readonly IAccessScopeResolver _accessScope;

    public UserImportController(IUserImportService importService, IAccessScopeResolver accessScope)
    {
        _importService = importService;
        _accessScope = accessScope;
    }

    // ── POST /api/v1/user-import/csv ─────────────────────────────────────────

    /// <summary>
    /// Imports user accounts from a CSV file.
    /// Expected CSV header: Username,Email,FullName,Role[,DepartmentId,InstitutionType,MobileNumber,CampusAssignments]
    /// On import: initial password = Username; MustChangePassword is set to true (P4-S2-01/02).
    /// Returns a summary: total rows, imported, duplicates, errors.
    /// </summary>
    [HttpPost("csv")]
    public async Task<IActionResult> ImportCsv(
        IFormFile file,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromQuery] bool strictMode = false,
        CancellationToken ct = default)
    {
        var scope = ResolveEffectiveScope(tenantId, campusId);
        if (scope.Error is not null)
            return scope.Error;

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No CSV file provided." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".csv")
            return BadRequest(new { message = "Only .csv files are accepted." });

        await using var stream = file.OpenReadStream();
        var result = await _importService.ImportFromCsvAsync(stream, scope.TenantId, scope.CampusId, strictMode, ct);
        return Ok(result);
    }

    private (Guid? TenantId, Guid? CampusId, IActionResult? Error) ResolveEffectiveScope(Guid? requestedTenantId, Guid? requestedCampusId)
    {
        Guid? effectiveTenantId = requestedTenantId;
        Guid? effectiveCampusId = requestedCampusId;

        if (!_accessScope.IsSuperAdmin())
        {
            var callerTenantId = _accessScope.GetTenantId();
            var callerCampusId = _accessScope.GetCampusId();

            if (callerTenantId.HasValue)
            {
                if (requestedTenantId.HasValue && requestedTenantId.Value != callerTenantId.Value)
                    return (null, null, Forbid());

                effectiveTenantId = callerTenantId.Value;
            }

            if (callerCampusId.HasValue)
            {
                if (requestedCampusId.HasValue && requestedCampusId.Value != callerCampusId.Value)
                    return (null, null, Forbid());

                effectiveCampusId = callerCampusId.Value;
            }
        }

        if (effectiveTenantId.HasValue != effectiveCampusId.HasValue)
            return (null, null, BadRequest(new { message = "TenantId and CampusId must be provided together." }));

        return (effectiveTenantId, effectiveCampusId, null);
    }
}
