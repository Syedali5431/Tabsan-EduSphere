using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.API.Services;
using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Interfaces;

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

    public PaymentReceiptController(IStudentLifecycleService service, IMediaStorageService mediaStorage)
    {
        _service = service;
        _mediaStorage = mediaStorage;
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
    public async Task<IActionResult> Create([FromBody] CreatePaymentReceiptCommand cmd, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Forbid();

        var receipt = await _service.CreatePaymentReceiptAsync(userId, cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = receipt.Id }, receipt);
    }

    // ── PUT /api/v1/payments/{id} ───────────────────────────────────────────

    /// <summary>Finance edits an actionable receipt before it is finalized.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Finance")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePaymentReceiptCommand cmd, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Forbid();

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
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var receipts = await _service.GetAllReceiptsAsync(page, pageSize, ct);
        return Ok(receipts);
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
    public async Task<IActionResult> GetByStudent(Guid studentProfileId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var receipts = await _service.GetActiveReceiptsByStudentAsync(studentProfileId, page, pageSize, ct);
        return Ok(receipts);
    }

    // ── GET /api/v1/payments/my ───────────────────────────────────────────────

    /// <summary>Student views their own active payment receipts.</summary>
    [HttpGet("my/{studentProfileId:guid}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMine(Guid studentProfileId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var receipts = await _service.GetActiveReceiptsByStudentAsync(studentProfileId, page, pageSize, ct);
        return Ok(receipts);
    }

    // ── GET /api/v1/payments/student/{studentProfileId}/fee-status ───────────

    /// <summary>Returns full fee status for a student (paid + unpaid summary).</summary>
    [HttpGet("student/{studentProfileId:guid}/fee-status")]
    public async Task<IActionResult> GetFeeStatus(Guid studentProfileId, CancellationToken ct)
    {
        var status = await _service.GetStudentFeeStatusAsync(studentProfileId, ct);
        return Ok(status);
    }

    // ── GET /api/v1/payments/{id} ─────────────────────────────────────────────

    /// <summary>Returns a specific payment receipt by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var receipt = await _service.GetPaymentReceiptByIdAsync(id, ct);
        if (receipt is null) return NotFound();
        return Ok(receipt);
    }

    // ── POST /api/v1/payments/{id}/mark-submitted ──────────────────────────

    // Final-Touches Phase 7 Stage 7.3 — student marks receipt as submitted with a text note as proof reference
    /// <summary>Student marks a receipt as Submitted, providing a text reference as proof (e.g., transaction ID).</summary>
    [HttpPost("{id:guid}/mark-submitted")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MarkSubmitted(Guid id, [FromBody] string proofNote, CancellationToken ct)
    {
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
    public async Task<IActionResult> SubmitProof(Guid id, IFormFile file, CancellationToken ct)
    {
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
    public async Task<IActionResult> ConfirmPayment(Guid id, [FromBody] string? notes, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Forbid();

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
    public async Task<IActionResult> Cancel(Guid id, [FromBody] string? reason, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Forbid();

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
}
