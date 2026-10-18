using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Quizzes;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Plan M2.2 — question banks (faculty/admin). Banks are tenant-scoped; staff of the tenant can read
/// shared banks and the banks of the course/department they ask for; the creator, Admins and
/// SuperAdmins can change a bank (shared banks are editable by any staff member of the tenant).
/// Question edits and removals use the quiz endpoints (<c>PUT/DELETE /api/v1/quiz/question/{id}</c>).
/// Gated by the "quizzes" module (route prefix /api/v1/quiz).
/// </summary>
[ApiController]
[Route("api/v1/quiz/banks")]
[Authorize(Policy = "Faculty")]
public sealed class QuestionBankController : ControllerBase
{
    private readonly IQuestionBankService _banks;
    private readonly IAccessScopeResolver _accessScope;
    private readonly ApplicationDbContext _db;

    public QuestionBankController(IQuestionBankService banks, IAccessScopeResolver accessScope, ApplicationDbContext db)
    {
        _banks = banks;
        _accessScope = accessScope;
        _db = db;
    }

    /// <summary>GET /api/v1/quiz/banks?courseOfferingId=&amp;departmentId= — banks visible to the caller.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? courseOfferingId, [FromQuery] Guid? departmentId, CancellationToken ct)
        => Ok(await _banks.ListAsync(CallerTenant(), courseOfferingId, departmentId, CurrentUserId(), ct));

    [HttpGet("{bankId:guid}")]
    public async Task<IActionResult> Get(Guid bankId, CancellationToken ct)
    {
        var denied = await AuthorizeAsync(bankId, write: false, ct);
        if (denied is not null) return denied;
        var bank = await _banks.GetAsync(bankId, ct);
        return bank is null ? NotFound() : Ok(bank);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveBankRequest request, CancellationToken ct)
    {
        if (request.CourseOfferingId.HasValue)
        {
            var offeringTenant = await _db.CourseOfferings.AsNoTracking().Where(o => o.Id == request.CourseOfferingId)
                .Select(o => new { o.TenantId }).FirstOrDefaultAsync(ct);
            if (offeringTenant is null) return NotFound(new { message = "Course offering not found." });
            if (!_accessScope.IsSuperAdmin() && CallerTenant().HasValue && offeringTenant.TenantId != CallerTenant()) return Forbid();
        }

        try
        {
            var id = await _banks.CreateAsync(request, CallerTenant(), CurrentUserId(), ct);
            return Ok(new { bankId = id });
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{bankId:guid}")]
    public async Task<IActionResult> Update(Guid bankId, [FromBody] SaveBankRequest request, CancellationToken ct)
    {
        var denied = await AuthorizeAsync(bankId, write: true, ct);
        if (denied is not null) return denied;
        var error = await _banks.UpdateAsync(bankId, request, ct);
        return error is null ? NoContent() : BadRequest(new { message = error });
    }

    [HttpDelete("{bankId:guid}")]
    public async Task<IActionResult> Delete(Guid bankId, CancellationToken ct)
    {
        var denied = await AuthorizeAsync(bankId, write: true, ct);
        if (denied is not null) return denied;
        var (found, error) = await _banks.DeleteAsync(bankId, ct);
        if (!found) return NotFound();
        return error is null ? NoContent() : Conflict(new { message = error });
    }

    [HttpPost("{bankId:guid}/questions")]
    public async Task<IActionResult> AddQuestion(Guid bankId, [FromBody] AddBankQuestionRequest request, CancellationToken ct)
    {
        var denied = await AuthorizeAsync(bankId, write: true, ct);
        if (denied is not null) return denied;
        var result = await _banks.AddQuestionAsync(bankId, request, ct);
        return result.Error is null ? Ok(new { questionId = result.QuestionId }) : BadRequest(new { message = result.Error });
    }

    /// <summary>Downloads the bank as an Excel workbook (with a Help sheet describing the format).</summary>
    [HttpGet("{bankId:guid}/export")]
    public async Task<IActionResult> Export(Guid bankId, CancellationToken ct)
    {
        var denied = await AuthorizeAsync(bankId, write: false, ct);
        if (denied is not null) return denied;
        var file = await _banks.ExportAsync(bankId, ct);
        return file is null
            ? NotFound()
            : File(file.Value.Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.Value.FileName);
    }

    /// <summary>Imports questions from an Excel workbook; invalid rows are reported and skipped.</summary>
    [HttpPost("{bankId:guid}/import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Import(Guid bankId, IFormFile file, CancellationToken ct)
    {
        var denied = await AuthorizeAsync(bankId, write: true, ct);
        if (denied is not null) return denied;
        if (file is null || file.Length == 0) return BadRequest(new { message = "Choose an .xlsx file to import." });
        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only .xlsx workbooks can be imported." });

        await using var stream = file.OpenReadStream();
        var result = await _banks.ImportAsync(bankId, stream, ct);
        return Ok(new { imported = result.Imported, errors = result.Errors });
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private Guid? CallerTenant() => _accessScope.IsSuperAdmin() ? null : _accessScope.GetTenantId();

    private Guid CurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    private async Task<IActionResult?> AuthorizeAsync(Guid bankId, bool write, CancellationToken ct)
    {
        var bank = await _db.QuestionBanks.AsNoTracking().Where(b => b.Id == bankId)
            .Select(b => new { b.TenantId, b.CreatedByUserId, b.IsShared }).FirstOrDefaultAsync(ct);
        if (bank is null) return NotFound();
        if (_accessScope.IsSuperAdmin()) return null;

        var tenant = _accessScope.GetTenantId();
        if (tenant.HasValue && bank.TenantId != tenant) return Forbid();
        if (!write || User.IsInRole("Admin")) return null;
        return bank.CreatedByUserId == CurrentUserId() || bank.IsShared ? null : Forbid();
    }
}
