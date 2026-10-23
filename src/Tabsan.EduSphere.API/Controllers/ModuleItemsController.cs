using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Lms;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Plan M7 — module items, release conditions and progress. Staff work within their tenant/campus; students only in
/// offerings they are enrolled in, and only see published modules and items.
/// </summary>
[ApiController]
[Route("api/v1/lms")]
[Authorize]
public sealed class ModuleItemsController : ControllerBase
{
    private readonly ModuleItemService _service;
    private readonly IModuleItemRepository _repo;
    private readonly IAccessScopeResolver _scope;
    private readonly ApplicationDbContext _db;

    public ModuleItemsController(ModuleItemService service, IModuleItemRepository repo, IAccessScopeResolver scope, ApplicationDbContext db)
    {
        _service = service;
        _repo = repo;
        _scope = scope;
        _db = db;
    }

    // ── teacher ──

    [HttpGet("offerings/{offeringId:guid}/structure")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Structure(Guid offeringId, CancellationToken ct)
    {
        if (await DeniedAsync(offeringId, ct) is { } denied) return denied;
        return await _service.StructureAsync(offeringId, ct) is { } structure ? Ok(structure) : NotFound();
    }

    [HttpGet("offerings/{offeringId:guid}/linkable")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Linkable(Guid offeringId, CancellationToken ct)
    {
        if (await DeniedAsync(offeringId, ct) is { } denied) return denied;
        return Ok(await _service.LinkablesAsync(offeringId, ct));
    }

    [HttpPost("modules/{moduleId:guid}/items")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> AddItem(Guid moduleId, [FromBody] AddModuleItemRequest request, CancellationToken ct)
    {
        if (await ModuleDeniedAsync(moduleId, ct) is { } denied) return denied;
        return ToResult(await _service.AddItemAsync(moduleId, request, UserId(), ct));
    }

    [HttpPut("module-items/{itemId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> UpdateItem(Guid itemId, [FromBody] UpdateModuleItemRequest request, CancellationToken ct)
    {
        if (await ItemDeniedAsync(itemId, ct) is { } denied) return denied;
        return ToResult(await _service.UpdateItemAsync(itemId, request, UserId(), ct));
    }

    [HttpDelete("module-items/{itemId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> RemoveItem(Guid itemId, CancellationToken ct)
    {
        if (await ItemDeniedAsync(itemId, ct) is { } denied) return denied;
        var result = await _service.RemoveItemAsync(itemId, UserId(), ct);
        return result.Ok ? NoContent() : StatusCode(result.Status, new { message = result.Error });
    }

    [HttpPost("modules/{moduleId:guid}/items/order")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Reorder(Guid moduleId, [FromBody] ReorderModuleItemsRequest request, CancellationToken ct)
    {
        if (await ModuleDeniedAsync(moduleId, ct) is { } denied) return denied;
        var result = await _service.ReorderAsync(moduleId, request, ct);
        return result.Ok ? NoContent() : StatusCode(result.Status, new { message = result.Error });
    }

    [HttpPut("modules/{moduleId:guid}/release")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Release(Guid moduleId, [FromBody] ModuleReleaseRequest request, CancellationToken ct)
    {
        if (await ModuleDeniedAsync(moduleId, ct) is { } denied) return denied;
        var result = await _service.SetReleaseAsync(moduleId, request, UserId(), ct);
        return result.Ok ? NoContent() : StatusCode(result.Status, new { message = result.Error });
    }

    [HttpGet("offerings/{offeringId:guid}/progress")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> Progress(Guid offeringId, CancellationToken ct)
    {
        if (await DeniedAsync(offeringId, ct) is { } denied) return denied;
        return await _service.ProgressReportAsync(offeringId, ct) is { } report ? Ok(report) : NotFound();
    }

    [HttpGet("offerings/{offeringId:guid}/progress/{studentProfileId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> StudentProgress(Guid offeringId, Guid studentProfileId, CancellationToken ct)
    {
        if (await DeniedAsync(offeringId, ct) is { } denied) return denied;
        if (!await _repo.IsEnrolledAsync(offeringId, studentProfileId, ct)) return NotFound();
        return await _service.StudentViewAsync(offeringId, studentProfileId, ct) is { } view ? Ok(view) : NotFound();
    }

    // ── student (staff get a preview where it makes sense) ──

    [HttpGet("offerings/{offeringId:guid}/my-modules")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyModules(Guid offeringId, CancellationToken ct)
    {
        if (await StudentAsync(ct) is not { } studentId) return Forbid();
        if (!await _repo.IsEnrolledAsync(offeringId, studentId, ct)) return Forbid();
        return await _service.StudentViewAsync(offeringId, studentId, ct) is { } view ? Ok(view) : NotFound();
    }

    [HttpPost("module-items/{itemId:guid}/open")]
    public async Task<IActionResult> Open(Guid itemId, CancellationToken ct)
    {
        var (studentId, denied) = await ParticipantAsync(itemId, ct);
        if (denied is not null) return denied;
        return ToResult(await _service.OpenAsync(itemId, studentId, ct));
    }

    [HttpPost("module-items/{itemId:guid}/done")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MarkDone(Guid itemId, CancellationToken ct) => await SetDoneAsync(itemId, true, ct);

    [HttpDelete("module-items/{itemId:guid}/done")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> UndoDone(Guid itemId, CancellationToken ct) => await SetDoneAsync(itemId, false, ct);

    [HttpGet("module-items/{itemId:guid}/nav")]
    public async Task<IActionResult> Nav(Guid itemId, CancellationToken ct)
    {
        var (studentId, denied) = await ParticipantAsync(itemId, ct);
        if (denied is not null) return denied;
        return ToResult(await _service.NavAsync(itemId, studentId, ct));
    }

    private async Task<IActionResult> SetDoneAsync(Guid itemId, bool done, CancellationToken ct)
    {
        var (studentId, denied) = await ParticipantAsync(itemId, ct);
        if (denied is not null) return denied;
        return ToResult(await _service.SetDoneAsync(itemId, studentId!.Value, done, ct));
    }

    // ── helpers ──

    private bool IsStaff() => User.IsInRole("Faculty") || User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    private async Task<Guid?> StudentAsync(CancellationToken ct)
    {
        if (Guid.TryParse(User.FindFirstValue("studentProfileId"), out var id)) return id;
        var userId = UserId();
        var profile = await _db.StudentProfiles.AsNoTracking().Where(s => s.UserId == userId).Select(s => (Guid?)s.Id).FirstOrDefaultAsync(ct);
        return profile;
    }

    /// <summary>Students must be enrolled (their id is returned); staff must be in scope (null id = preview).</summary>
    private async Task<(Guid? StudentId, IActionResult? Denied)> ParticipantAsync(Guid itemId, CancellationToken ct)
    {
        var offeringId = await _service.OfferingOfItemAsync(itemId, ct);
        if (offeringId is null) return (null, NotFound());
        if (IsStaff()) return (null, await DeniedAsync(offeringId.Value, ct));
        if (await StudentAsync(ct) is not { } studentId || !await _repo.IsEnrolledAsync(offeringId.Value, studentId, ct)) return (null, Forbid());
        return (studentId, null);
    }

    private async Task<IActionResult?> ModuleDeniedAsync(Guid moduleId, CancellationToken ct)
        => await _service.OfferingOfModuleAsync(moduleId, ct) is { } offeringId ? await DeniedAsync(offeringId, ct) : NotFound();

    private async Task<IActionResult?> ItemDeniedAsync(Guid itemId, CancellationToken ct)
        => await _service.OfferingOfItemAsync(itemId, ct) is { } offeringId ? await DeniedAsync(offeringId, ct) : NotFound();

    private async Task<IActionResult?> DeniedAsync(Guid offeringId, CancellationToken ct)
    {
        var offering = await _repo.GetOfferingAsync(offeringId, ct);
        if (offering is null) return NotFound();
        if (_scope.IsSuperAdmin()) return null;
        var tenant = _scope.GetTenantId();
        var campus = _scope.GetCampusId();
        if (tenant.HasValue && offering.TenantId != tenant) return Forbid();
        if (campus.HasValue && offering.CampusId != campus) return Forbid();
        return null;
    }

    private IActionResult ToResult<T>(ModuleResult<T> result)
        => result.Ok ? StatusCode(result.Status, result.Value) : StatusCode(result.Status, new { message = result.Error });
}
