using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.API.Services.TwoFactor;
using Tabsan.EduSphere.Application.DTOs.TwoFactor;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Add-on 2FA controller for the Plan L boundary.
/// This surface is intentionally separate from the legacy auth controller.
/// </summary>
[ApiController]
[Route("api/v1/2fa")]
public sealed class TwoFactorController : ControllerBase
{
    private readonly TwoFactorSetupService _twoFactor;

    public TwoFactorController(TwoFactorSetupService twoFactor)
    {
        _twoFactor = twoFactor;
    }

    /// <summary>Starts 2FA enrollment for the current signed-in user.</summary>
    [HttpGet("setup")]
    [Authorize]
    public async Task<IActionResult> Setup(CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Forbid();

        var response = await _twoFactor.BeginSetupAsync(userId, ct);
        return response is null ? NotFound(new { message = "User not found or inactive." }) : Ok(response);
    }

    /// <summary>Confirms the initial TOTP code after setup has been displayed to the user.</summary>
    [HttpPost("verify")]
    [Authorize]
    public async Task<IActionResult> Verify([FromBody] TwoFactorVerifyRequest request, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Forbid();

        var ok = await _twoFactor.VerifySetupAsync(userId, request.Code, ct);
        return ok
            ? Ok(new TwoFactorVerificationResponse(true, true, "2FA setup verified and enabled."))
            : BadRequest(new { message = "Invalid 2FA setup code or pending setup state." });
    }

    /// <summary>Disables 2FA after the user confirms ownership with a valid current TOTP code.</summary>
    [HttpPost("disable")]
    [Authorize]
    public async Task<IActionResult> Disable([FromBody] TwoFactorDisableRequest request, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Forbid();

        var ok = await _twoFactor.DisableAsync(userId, request.Code, ct);
        return ok
            ? Ok(new TwoFactorVerificationResponse(true, false, "2FA disabled."))
            : BadRequest(new { message = "Invalid 2FA disable code or 2FA is not enabled." });
    }

    /// <summary>
    /// Verifies the TOTP challenge for a pending login hand-off.
    /// This endpoint is the plug-in insertion point for the existing password-first login flow.
    /// </summary>
    [HttpPost("login-verify")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginVerify([FromBody] TwoFactorLoginVerifyRequest request, CancellationToken ct)
    {
        var ok = await _twoFactor.VerifyLoginAsync(request.UserId, request.Code, ct);
        return ok ? Ok(new TwoFactorVerificationResponse(true, true, "2FA code accepted."))
                  : Unauthorized(new TwoFactorVerificationResponse(false, false, "Invalid or expired 2FA code."));
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdStr, out userId);
    }
}