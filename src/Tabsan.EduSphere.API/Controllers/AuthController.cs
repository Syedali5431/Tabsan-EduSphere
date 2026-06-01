using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.DTOs.Auth;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Handles all authentication operations: login, logout, token refresh, and password change.
/// Refresh tokens are exchanged as JSON body values (callers should store them securely).
/// All endpoints are under /api/v1/auth.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    // ── POST /api/v1/auth/login ────────────────────────────────────────────────

    /// <summary>
    /// Authenticates the user and returns a JWT access token plus a refresh token.
    /// Returns 401 Unauthorized when credentials are invalid or the account is inactive.
    /// Returns 403 Forbidden when the license concurrent-user limit has been reached (P2-S1-01).
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        // Capture the client IP for audit logging.
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _auth.LoginAsync(request, ip, ct);

        if (!result.IsSuccess)
        {
            return result.FailureReason switch
            {
                // P2-S1-01: Distinguish limit-reached from bad credentials.
                LoginFailureReason.ConcurrencyLimitReached
                    => StatusCode(403, new { message = "Login limit reached. The maximum number of concurrent users allowed by the current license has been reached. Please contact your administrator." }),

                LoginFailureReason.MfaRequired
                    => StatusCode(StatusCodes.Status428PreconditionRequired, new { message = "Multi-factor authentication is required for this deployment. Provide a valid MFA code and retry." }),

                LoginFailureReason.SessionRiskBlocked
                    => StatusCode(StatusCodes.Status423Locked, new { message = "This sign-in was blocked by session risk controls. Please retry from a trusted network or contact an administrator." }),

                _ => Unauthorized(new { message = "Invalid credentials or account inactive." })
            };
        }

        return Ok(result.Response);
    }

    // ── GET /api/v1/auth/security-profile ───────────────────────────────────

    /// <summary>
    /// Returns deployment-auth security capabilities so clients can adapt UX
    /// (MFA prompt, SSO button, risk-policy messaging) without hardcoding behavior.
    /// </summary>
    [HttpGet("security-profile")]
    [AllowAnonymous]
    public async Task<IActionResult> SecurityProfile(CancellationToken ct)
    {
        var profile = await _auth.GetSecurityProfileAsync(ct);
        return Ok(profile);
    }

    // ── POST /api/v1/auth/refresh ──────────────────────────────────────────────

    /// <summary>
    /// Rotates the presented refresh token and returns a new access + refresh pair.
    /// Returns 401 when the token is invalid, expired, or already revoked.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _auth.RefreshAsync(request.RefreshToken, ip, ct);

        if (result is null)
            return Unauthorized(new { message = "Invalid or expired refresh token." });

        return Ok(result);
    }

    // ── POST /api/v1/auth/logout ───────────────────────────────────────────────

    /// <summary>
    /// Revokes the current session associated with the presented refresh token.
    /// Always returns 204 No Content — even when the token does not exist —
    /// to avoid revealing session state to callers.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken ct)
    {
        await _auth.LogoutAsync(request.RefreshToken, ct);
        return NoContent();
    }

    // ── PUT /api/v1/auth/change-password ──────────────────────────────────────

    /// <summary>
    /// Changes the authenticated user's password.
    /// Returns 400 Bad Request when the current password is wrong.
    /// </summary>
    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        // Read the user ID from the JWT sub claim.
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdStr, out var userId))
            return Forbid();

        var success = await _auth.ChangePasswordAsync(userId, request, ct);

        if (!success)
            return BadRequest(new { message = "Current password is incorrect." });

        return NoContent();
    }

    // ── POST /api/v1/auth/force-change-password ───────────────────────────────

    /// <summary>
    /// Allows a user flagged with MustChangePassword (imported via CSV) to set a new password (P4-S2-02).
    /// Requires old password verification. Returns 204 on success, 400 on failure.
    /// </summary>
    [HttpPost("force-change-password")]
    [Authorize]
    public async Task<IActionResult> ForceChangePassword(
        [FromBody] ForceChangePasswordRequest request,
        CancellationToken ct)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdStr, out var userId))
            return Forbid();

        var success = await _auth.ForceChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, ct);

        if (!success)
            return BadRequest(new { message = "Failed to change password. Verify old password and ensure the new password follows policy: 12-16 characters with uppercase, lowercase, number, symbol (! @ # $ % ^ & *), and no simple patterns." });

        return NoContent();
    }

    // ── POST /api/v1/auth/mfa/setup ──────────────────────────────────────────

    /// <summary>
    /// Starts MFA enrollment by issuing a TOTP secret and one-time recovery codes for the current user.
    /// </summary>
    [HttpPost("mfa/setup")]
    [Authorize]
    public async Task<IActionResult> BeginMfaSetup(CancellationToken ct)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdStr, out var userId))
            return Forbid();

        var result = await _auth.BeginMfaSetupAsync(userId, ct);
        if (result is null)
            return NotFound(new { message = "User not found or inactive." });

        return Ok(result);
    }

    // ── POST /api/v1/auth/mfa/enable ─────────────────────────────────────────

    /// <summary>
    /// Completes MFA enrollment by validating a TOTP code from the authenticator app.
    /// </summary>
    [HttpPost("mfa/enable")]
    [Authorize]
    public async Task<IActionResult> EnableMfa([FromBody] EnableMfaRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdStr, out var userId))
            return Forbid();

        var ok = await _auth.EnableMfaAsync(userId, request, ct);
        if (!ok)
            return BadRequest(new { message = "Invalid MFA setup state or verification code." });

        return NoContent();
    }

    // ── POST /api/v1/auth/mfa/recovery-codes/regenerate ──────────────────────

    /// <summary>
    /// Regenerates one-time MFA recovery codes for the current user.
    /// </summary>
    [HttpPost("mfa/recovery-codes/regenerate")]
    [Authorize]
    public async Task<IActionResult> RegenerateMfaRecoveryCodes(CancellationToken ct)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdStr, out var userId))
            return Forbid();

        var response = await _auth.RegenerateRecoveryCodesAsync(userId, ct);
        if (response is null)
            return BadRequest(new { message = "MFA must be enabled before regenerating recovery codes." });

        return Ok(response);
    }
}
