using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Infrastructure.Licensing;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Allows Super Admin users to upload a new license file and query the current license status.
/// Admin users may read license status (read-only). Upload requires SuperAdmin.
/// Endpoints are under /api/v1/license.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class LicenseController : ControllerBase
{
    private readonly LicenseValidationService _licenseService;
    private readonly IMediaStorageService _mediaStorage;

    public LicenseController(
        LicenseValidationService licenseService,
        IMediaStorageService mediaStorage)
    {
        _licenseService = licenseService;
        _mediaStorage = mediaStorage;
    }

    // ── POST /api/v1/license/upload ───────────────────────────────────────────

    /// <summary>
    /// Accepts a license file upload, saves it to a temp location,
    /// validates the RSA signature, and activates the license on success.
    /// Returns 400 Bad Request when the file fails validation.
    /// File size is capped at 64 KB to prevent oversized upload abuse.
    /// </summary>
    [HttpPost("upload")]
    [Authorize(Roles = "SuperAdmin")]
    [RequestSizeLimit(65_536)] // 64 KB max license file
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        // Final-Touches Phase 28 Stage 28.3 — move license temp persistence onto storage abstraction.
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        if (!file.FileName.EndsWith(".tablic", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "License file must be a .tablic file." });

        string? tempStorageKey = null;

        try
        {
            await using var uploadStream = file.OpenReadStream();
            var stored = await _mediaStorage.SaveAsync(uploadStream, "license-temp", ".tablic", file.ContentType, file.FileName, ct);
            tempStorageKey = stored.StorageKey;
        }
        catch
        {
            return StatusCode(500, new { message = "Failed to save the uploaded file." });
        }

        // P2-S3-02 / P2-S3-03: Pass the HTTP host so the service can enforce domain binding.
        var requestDomain = Request.Host.Host;
        bool success;
        try
        {
            var fileBytes = await _mediaStorage.ReadAsBytesAsync(tempStorageKey!, ct);
            if (fileBytes is null || fileBytes.Length == 0)
                return StatusCode(500, new { message = "Failed to read the uploaded license file." });

            success = await _licenseService.ActivateFromBytesAsync(fileBytes, requestDomain, ct);
        }
        finally
        {
            // Clean up temp file regardless of outcome.
            if (!string.IsNullOrWhiteSpace(tempStorageKey))
                await _mediaStorage.DeleteAsync(tempStorageKey, ct);
        }

        if (!success)
            return BadRequest(new { message = "License validation failed. The file may be invalid or tampered." });

        return Ok(new { message = "License activated successfully." });
    }

    // ── GET /api/v1/license/status ────────────────────────────────────────────

    /// <summary>
    /// Runs an on-demand license check and returns the current status.
    /// Useful for health checks and the Super Admin dashboard.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var status = await _licenseService.ValidateCurrentAsync(ct);
        return Ok(new { status = status.ToString() });
    }

    // ── GET /api/v1/license/details ───────────────────────────────────────────

    /// <summary>
    /// Returns full license detail: status, type, activation date, expiry, remaining days.
    /// Available to both Super Admin (read + upload) and Admin (read-only).
    /// </summary>
    [HttpGet("details")]
    public async Task<IActionResult> Details(CancellationToken ct)
    {
        // Re-verify first so the figures shown always come from the signed license file,
        // never from values edited directly in the database.
        await _licenseService.ValidateCurrentAsync(ct);
        var state = await _licenseService.GetCurrentStateAsync(ct);
        if (state is null)
            return Ok(new { status = "None", licenseType = (string?)null, activatedAt = (DateTime?)null, expiresAt = (DateTime?)null, remainingDays = (int?)null, updatedAt = (DateTime?)null });

        int? remainingDays = state.ExpiresAt.HasValue
            ? (int)Math.Max(0, Math.Ceiling((state.ExpiresAt.Value - DateTime.UtcNow).TotalDays))
            : null;

        return Ok(new
        {
            status       = state.Status.ToString(),
            licenseType  = state.LicenseType.ToString(),
            activatedAt  = state.ActivatedAt,
            expiresAt    = state.ExpiresAt,
            updatedAt    = state.UpdatedAt,
            remainingDays
        });
    }
}
