using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.API.Services;
using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Manages student payment receipts and fee collection.
/// Finance creates receipts; students upload proof; finance confirms payment.
/// Until fees are Paid, student account is in read-only mode.
/// All payment records are permanent (no delete).
/// Routes: /api/v1/payments
/// </summary>
[ApiController]
[Route("api/v1/payments")]
[Authorize]
public class PaymentReceiptController : ControllerBase
{
    private readonly IStudentLifecycleService _service;
    private readonly IMediaStorageService _mediaStorage;
    private readonly IAccessScopeResolver _accessScope;
    private readonly ApplicationDbContext _db;

    public PaymentReceiptController(
        IStudentLifecycleService service,
        IMediaStorageService mediaStorage,
        IAccessScopeResolver accessScope,
        ApplicationDbContext db)
    {
        _service = service;
        _mediaStorage = mediaStorage;
        _accessScope = accessScope;
        _db = db;
    }

    private Guid GetUserId()
    {
        var val = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(val, out var id) ? id : Guid.Empty;
    }

    // ── POST /api/v1/payments ─────────────────────────────────────────────────

    /// <summary>Finance creates a new payment receipt for a student.</summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,Finance")]
    public async Task<IActionResult> Create([FromBody] CreatePaymentReceiptCommand cmd, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Forbid();

        var scope = await EnforceStudentScopeAsync(cmd.StudentProfileId, tenantId, campusId, ct);
        if (scope is not null)
            return scope;

        try
        {
            var receipt = await _service.CreatePaymentReceiptAsync(userId, cmd, ct);
            return CreatedAtAction(nameof(GetById), new { id = receipt.Id }, receipt);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { message = e.Message });
        }
    }

    /// <summary>Finance/Admin bulk-creates payment receipts in one request.</summary>
    [HttpPost("bulk")]
    [Authorize(Roles = "SuperAdmin,Admin,Finance")]
    public async Task<IActionResult> CreateBulk([FromBody] List<CreatePaymentReceiptCommand> commands, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        if (commands is null || commands.Count == 0)
            return BadRequest(new { message = "At least one payment row is required." });

        var userId = GetUserId();
        if (userId == Guid.Empty) return Forbid();

        var created = new List<PaymentReceiptDto>(commands.Count);

        foreach (var cmd in commands)
        {
            var scope = await EnforceStudentScopeAsync(cmd.StudentProfileId, tenantId, campusId, ct);
            if (scope is not null)
                return scope;

            try
            {
                var receipt = await _service.CreatePaymentReceiptAsync(userId, cmd, ct);
                created.Add(receipt);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        return Ok(new { createdCount = created.Count });
    }

    // ── PUT /api/v1/payments/{id} ───────────────────────────────────────────

    /// <summary>Finance edits an actionable receipt before it is finalized.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Finance")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePaymentReceiptCommand cmd, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Forbid();

        var scope = await EnforceReceiptScopeAsync(id, tenantId, campusId, ct);
        if (scope is not null)
            return scope;

        try
        {
            var receipt = await _service.UpdatePaymentReceiptAsync(id, userId, cmd, ct);
            return Ok(receipt);
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new { message = e.Message });
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { message = e.Message });
        }
    }

    // ── DELETE /api/v1/payments/{id} ─────────────────────────────────────────

    /// <summary>Deletion is intentionally disabled. Payment receipts are permanent audit records.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Finance")]
    public IActionResult Delete(Guid id)
    {
        Response.Headers.Allow = "GET,POST,PUT";
        return StatusCode(StatusCodes.Status405MethodNotAllowed,
            new { message = "Payment receipt deletion is not allowed. Use cancellation to retain audit history." });
    }

    // ── GET /api/v1/payments ──────────────────────────────────────────────

    // Final-Touches Phase 7 Stage 7.2 — all receipts for admin view
    /// <summary>Returns a paged set of payment receipts across all students. Admin / Finance only.</summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin,Finance")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? tenantId = null, [FromQuery] Guid? campusId = null, [FromQuery] int? institutionType = null, CancellationToken ct = default)
    {
        var scopeValidation = ValidateRequestedScopePair(tenantId, campusId);
        if (scopeValidation is not null)
            return scopeValidation;

        var effectiveTenantId = _accessScope.IsSuperAdmin() ? tenantId : _accessScope.GetTenantId();
        var effectiveCampusId = _accessScope.IsSuperAdmin() ? campusId : _accessScope.GetCampusId();

        var scopedPage = await GetScopedReceiptsPageAsync(page, pageSize, effectiveTenantId, effectiveCampusId, studentProfileId: null, onlyActive: false, institutionType, ct);
        return Ok(scopedPage);
    }

    // ── GET /api/v1/payments/mine ─────────────────────────────────────────

    // Final-Touches Phase 7 Stage 7.3 — student views their own receipts via JWT
    /// <summary>Student views their own payment receipts (resolved from JWT user ID).</summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Forbid();
        var receipts = await _service.GetReceiptsByUserAsync(userId, page, pageSize, ct);
        return Ok(receipts);
    }
    // ── GET /api/v1/payments/student/{studentProfileId} ──────────────────────

    /// <summary>Returns all active (non-cancelled) receipts for a student. Admin or Finance only.</summary>
    [HttpGet("student/{studentProfileId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Finance")]
    public async Task<IActionResult> GetByStudent(Guid studentProfileId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? tenantId = null, [FromQuery] Guid? campusId = null, [FromQuery] int? institutionType = null, CancellationToken ct = default)
    {
        var scope = await EnforceStudentScopeAsync(studentProfileId, tenantId, campusId, ct);
        if (scope is not null)
            return scope;

        var effectiveTenantId = _accessScope.IsSuperAdmin() ? tenantId : _accessScope.GetTenantId();
        var effectiveCampusId = _accessScope.IsSuperAdmin() ? campusId : _accessScope.GetCampusId();

        var scopedPage = await GetScopedReceiptsPageAsync(page, pageSize, effectiveTenantId, effectiveCampusId, studentProfileId, onlyActive: true, institutionType, ct);
        return Ok(scopedPage);
    }

    // ── GET /api/v1/payments/my ───────────────────────────────────────────────

    /// <summary>Student views their own active payment receipts.</summary>
    [HttpGet("my/{studentProfileId:guid}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMine(Guid studentProfileId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Forbid();
        if (!await IsStudentOwnerOfProfileAsync(studentProfileId, userId, ct)) return Forbid();

        var receipts = await _service.GetActiveReceiptsByStudentAsync(studentProfileId, page, pageSize, ct);
        return Ok(receipts);
    }

    // ── GET /api/v1/payments/student/{studentProfileId}/fee-status ───────────

    /// <summary>Returns full fee status for a student (paid + unpaid summary).</summary>
    [HttpGet("student/{studentProfileId:guid}/fee-status")]
    public async Task<IActionResult> GetFeeStatus(Guid studentProfileId, [FromQuery] Guid? tenantId = null, [FromQuery] Guid? campusId = null, CancellationToken ct = default)
    {
        if (User.IsInRole("Student"))
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Forbid();
            if (!await IsStudentOwnerOfProfileAsync(studentProfileId, userId, ct)) return Forbid();
        }

        var scope = await EnforceStudentScopeAsync(studentProfileId, tenantId, campusId, ct);
        if (scope is not null)
            return scope;

        var status = await _service.GetStudentFeeStatusAsync(studentProfileId, ct);
        return Ok(status);
    }

    // ── GET /api/v1/payments/{id} ─────────────────────────────────────────────

    /// <summary>Returns a specific payment receipt by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid? tenantId = null, [FromQuery] Guid? campusId = null, CancellationToken ct = default)
    {
        if (User.IsInRole("Student"))
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Forbid();
            if (!await IsStudentOwnerOfReceiptAsync(id, userId, ct)) return Forbid();
        }

        var scope = await EnforceReceiptScopeAsync(id, tenantId, campusId, ct);
        if (scope is not null)
            return scope;

        var receipt = await _service.GetPaymentReceiptByIdAsync(id, ct);
        if (receipt is null) return NotFound();
        return Ok(receipt);
    }

    // ── POST /api/v1/payments/{id}/mark-submitted ──────────────────────────

    // Final-Touches Phase 7 Stage 7.3 — student marks receipt as submitted with a text note as proof reference
    /// <summary>Student marks a receipt as Submitted, providing a text reference as proof (e.g., transaction ID).</summary>
    [HttpPost("{id:guid}/mark-submitted")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MarkSubmitted(Guid id, [FromBody] string proofNote, [FromQuery] Guid? tenantId = null, [FromQuery] Guid? campusId = null, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Forbid();
        if (!await IsStudentOwnerOfReceiptAsync(id, userId, ct)) return Forbid();

        var scope = await EnforceReceiptScopeAsync(id, tenantId, campusId, ct);
        if (scope is not null)
            return scope;

        if (string.IsNullOrWhiteSpace(proofNote))
            return BadRequest(new { message = "Proof note cannot be empty." });

        try
        {
            await _service.SubmitPaymentProofAsync(id, proofNote, ct);
            return NoContent();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new { message = e.Message });
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { message = e.Message });
        }
    }

    // ── POST /api/v1/payments/{id}/submit-proof ───────────────────────────────

    /// <summary>
    /// Student uploads proof of payment. Accepts a multipart file upload.
    /// Sets receipt status to Submitted.
    /// </summary>
    [HttpPost("{id:guid}/submit-proof")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> SubmitProof(Guid id, IFormFile file, [FromQuery] Guid? tenantId = null, [FromQuery] Guid? campusId = null, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Forbid();
        if (!await IsStudentOwnerOfReceiptAsync(id, userId, ct)) return Forbid();

        var scope = await EnforceReceiptScopeAsync(id, tenantId, campusId, ct);
        if (scope is not null)
            return scope;

        // Final-Touches Phase 28 Stage 28.3 — route payment proof uploads through the storage abstraction.
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        // Keep this endpoint intentionally strict to payment-proof formats.
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        if (!allowed.Contains(ext))
            return BadRequest(new { message = "Only PDF, JPG, or PNG files are accepted as payment proof." });

        // Final-Touches Phase 28 Stage 28.3 — retain strict extension gate + deep upload validation.
        var uploadError = await FileUploadValidator.ValidateAsync(file);
        if (!string.IsNullOrWhiteSpace(uploadError))
            return BadRequest(new { message = uploadError });

        await using var stream = file.OpenReadStream();
        var stored = await _mediaStorage.SaveAsync(stream, "payment-proofs", ext, file.ContentType, file.FileName, ct);

        try
        {
            await _service.SubmitPaymentProofAsync(id, stored.StorageKey, ct);
            return NoContent();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new { message = e.Message });
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { message = e.Message });
        }
    }

    // ── POST /api/v1/payments/{id}/confirm ───────────────────────────────────

    /// <summary>Finance confirms payment received. Sets receipt status to Paid (final state).</summary>
    [HttpPost("{id:guid}/confirm")]
    [Authorize(Roles = "SuperAdmin,Admin,Finance")]
    public async Task<IActionResult> ConfirmPayment(Guid id, [FromBody] string? notes, [FromQuery] Guid? tenantId = null, [FromQuery] Guid? campusId = null, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Forbid();

        var scope = await EnforceReceiptScopeAsync(id, tenantId, campusId, ct);
        if (scope is not null)
            return scope;

        try
        {
            await _service.ConfirmPaymentAsync(id, userId, notes, ct);
            return NoContent();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new { message = e.Message });
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { message = e.Message });
        }
    }

    // ── POST /api/v1/payments/{id}/cancel ────────────────────────────────────

    /// <summary>Finance cancels a receipt (e.g., issued in error). Record is permanently preserved.</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "SuperAdmin,Admin,Finance")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] string? reason, [FromQuery] Guid? tenantId = null, [FromQuery] Guid? campusId = null, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Forbid();

        var scope = await EnforceReceiptScopeAsync(id, tenantId, campusId, ct);
        if (scope is not null)
            return scope;

        try
        {
            await _service.CancelReceiptAsync(id, userId, reason, ct);
            return NoContent();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new { message = e.Message });
        }
    }

    private IActionResult? ValidateRequestedScopePair(Guid? requestedTenantId, Guid? requestedCampusId)
    {
        if (requestedTenantId.HasValue != requestedCampusId.HasValue)
            return BadRequest(new { message = "TenantId and CampusId must be provided together." });

        return null;
    }

    private async Task<IActionResult?> EnforceStudentScopeAsync(Guid studentProfileId, Guid? requestedTenantId, Guid? requestedCampusId, CancellationToken ct)
    {
        var pairValidation = ValidateRequestedScopePair(requestedTenantId, requestedCampusId);
        if (pairValidation is not null)
            return pairValidation;

        var studentScope = await _db.StudentProfiles
            .AsNoTracking()
            .Where(s => s.Id == studentProfileId)
            .Join(
                _db.Departments.AsNoTracking(),
                s => s.DepartmentId,
                d => d.Id,
                (s, d) => new { d.TenantId, d.CampusId })
            .FirstOrDefaultAsync(ct);

        if (studentScope is null)
            return NotFound(new { message = $"Student profile {studentProfileId} not found." });

        return EnforceResolvedScope(studentScope.TenantId, studentScope.CampusId, requestedTenantId, requestedCampusId);
    }

    private async Task<IActionResult?> EnforceReceiptScopeAsync(Guid receiptId, Guid? requestedTenantId, Guid? requestedCampusId, CancellationToken ct)
    {
        var pairValidation = ValidateRequestedScopePair(requestedTenantId, requestedCampusId);
        if (pairValidation is not null)
            return pairValidation;

        var receiptScope = await _db.PaymentReceipts
            .AsNoTracking()
            .Where(r => r.Id == receiptId)
            .Join(
                _db.StudentProfiles.AsNoTracking(),
                r => r.StudentProfileId,
                s => s.Id,
                (r, s) => new { s.DepartmentId })
            .Join(
                _db.Departments.AsNoTracking(),
                rs => rs.DepartmentId,
                d => d.Id,
                (rs, d) => new { d.TenantId, d.CampusId })
            .FirstOrDefaultAsync(ct);

        if (receiptScope is null)
            return NotFound(new { message = $"Receipt {receiptId} not found." });

        return EnforceResolvedScope(receiptScope.TenantId, receiptScope.CampusId, requestedTenantId, requestedCampusId);
    }

    private IActionResult? EnforceResolvedScope(Guid? entityTenantId, Guid? entityCampusId, Guid? requestedTenantId, Guid? requestedCampusId)
    {
        if (_accessScope.IsSuperAdmin())
        {
            if (requestedTenantId.HasValue && entityTenantId != requestedTenantId.Value)
                return Forbid();
            if (requestedCampusId.HasValue && entityCampusId != requestedCampusId.Value)
                return Forbid();
            return null;
        }

        var callerTenantId = _accessScope.GetTenantId();
        var callerCampusId = _accessScope.GetCampusId();

        if (callerTenantId.HasValue && entityTenantId != callerTenantId.Value)
            return Forbid();
        if (callerCampusId.HasValue && entityCampusId != callerCampusId.Value)
            return Forbid();

        if (requestedTenantId.HasValue && entityTenantId != requestedTenantId.Value)
            return Forbid();
        if (requestedCampusId.HasValue && entityCampusId != requestedCampusId.Value)
            return Forbid();

        return null;
    }

    private async Task<PaymentReceiptPageDto> GetScopedReceiptsPageAsync(
        int page,
        int pageSize,
        Guid? tenantId,
        Guid? campusId,
        Guid? studentProfileId,
        bool onlyActive,
        int? institutionType,
        CancellationToken ct)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = Math.Clamp(pageSize, 10, 100);
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var query =
            from r in _db.PaymentReceipts.AsNoTracking()
            join s in _db.StudentProfiles.AsNoTracking() on r.StudentProfileId equals s.Id
            join d in _db.Departments.AsNoTracking() on s.DepartmentId equals d.Id
            where (!tenantId.HasValue || d.TenantId == tenantId.Value)
                  && (!campusId.HasValue || d.CampusId == campusId.Value)
                && (!institutionType.HasValue || (int)d.InstitutionType == institutionType.Value)
                  && (!studentProfileId.HasValue || r.StudentProfileId == studentProfileId.Value)
            select r;

        if (onlyActive)
            query = query.Where(r => r.Status != PaymentReceiptStatus.Cancelled);

        var totalCount = await query.CountAsync(ct);
        var ids = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(r => r.Id)
            .ToListAsync(ct);

        var items = new List<PaymentReceiptDto>(ids.Count);
        foreach (var id in ids)
        {
            var receipt = await _service.GetPaymentReceiptByIdAsync(id, ct);
            if (receipt is not null)
                items.Add(receipt);
        }

        return new PaymentReceiptPageDto(items, normalizedPage, normalizedPageSize, totalCount);
    }

    private Task<bool> IsStudentOwnerOfProfileAsync(Guid studentProfileId, Guid userId, CancellationToken ct)
    {
        return _db.StudentProfiles
            .AsNoTracking()
            .AnyAsync(s => s.Id == studentProfileId && s.UserId == userId, ct);
    }

    private Task<bool> IsStudentOwnerOfReceiptAsync(Guid receiptId, Guid userId, CancellationToken ct)
    {
        return (from r in _db.PaymentReceipts.AsNoTracking()
                join s in _db.StudentProfiles.AsNoTracking() on r.StudentProfileId equals s.Id
                where r.Id == receiptId && s.UserId == userId
                select r.Id)
            .AnyAsync(ct);
    }
}
