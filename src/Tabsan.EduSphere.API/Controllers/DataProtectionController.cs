using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.DTOs.DataClassification;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Phase 5: Data Protection — encryption, data masking testing, and classification management.
/// ISO 27001 A.8.2.1 / A.18.1.4. Admin and SuperAdmin only.
/// </summary>
[ApiController]
[Route("api/v1/data-protection")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class DataProtectionController : ControllerBase
{
    private readonly IEncryptionService _encryption;
    private readonly IDataMaskingService _masking;
    private readonly IDataClassificationService _classification;

    public DataProtectionController(IEncryptionService encryption, IDataMaskingService masking, IDataClassificationService classification)
    {
        _encryption = encryption;
        _masking = masking;
        _classification = classification;
    }

    private Guid GetUserId()
    {
        var val = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(val, out var id) ? id : Guid.Empty;
    }

    // ── Encryption ─────────────────────────────────────────────────────────

    /// <summary>POST /api/v1/data-protection/encrypt — encrypts a plaintext value.</summary>
    [HttpPost("encrypt")]
    public IActionResult Encrypt([FromBody] CryptoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Value))
            return BadRequest(new { message = "Value is required." });
        return Ok(new CryptoResponse { Result = _encryption.Encrypt(request.Value) });
    }

    /// <summary>POST /api/v1/data-protection/decrypt — decrypts a ciphertext value.</summary>
    [HttpPost("decrypt")]
    public IActionResult Decrypt([FromBody] CryptoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Value))
            return BadRequest(new { message = "Value is required." });
        return Ok(new CryptoResponse { Result = _encryption.Decrypt(request.Value) });
    }

    // ── Data Masking ───────────────────────────────────────────────────────

    /// <summary>POST /api/v1/data-protection/mask — demonstrates data masking on provided values.</summary>
    [HttpPost("mask")]
    public IActionResult Mask([FromBody] MaskingDemoRequest request)
    {
        return Ok(new
        {
            email = _masking.MaskEmail(request.Email ?? string.Empty),
            phone = _masking.MaskPhone(request.Phone ?? string.Empty),
            name = _masking.MaskName(request.Name ?? string.Empty)
        });
    }

    // ── Classification ─────────────────────────────────────────────────────

    /// <summary>GET /api/v1/data-protection/classifications — lists all classification entries.</summary>
    [HttpGet("classifications")]
    public async Task<IActionResult> GetClassifications(CancellationToken ct)
    {
        var result = await _classification.GetAllAsync(ct);
        return Ok(result);
    }

    /// <summary>POST /api/v1/data-protection/classifications — creates a new classification entry.</summary>
    [HttpPost("classifications")]
    public async Task<IActionResult> CreateClassification([FromBody] CreateClassificationRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Forbid();

        if (string.IsNullOrWhiteSpace(request.EntityName) || string.IsNullOrWhiteSpace(request.ClassificationLevel))
            return BadRequest(new { message = "EntityName and ClassificationLevel are required." });

        var result = await _classification.CreateAsync(request, userId, ct);
        return CreatedAtAction(nameof(GetClassifications), null, result);
    }
}

/// <summary>Phase 5: Request to demonstrate data masking.</summary>
public sealed class MaskingDemoRequest
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Name { get; set; }
}
