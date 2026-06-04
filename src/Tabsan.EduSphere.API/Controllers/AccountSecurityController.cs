using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Provides admin-level account security operations: unlock accounts and reset passwords.
/// Only Admin and SuperAdmin can access these endpoints.
/// Routes: /api/v1/account-security
/// </summary>
[ApiController]
[Route("api/v1/account-security")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class AccountSecurityController : ControllerBase
{
    private readonly IAccountSecurityService _securityService;

    public AccountSecurityController(IAccountSecurityService securityService)
    {
        _securityService = securityService;
    }

    private Guid GetUserId()
    {
        var val = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(val, out var id) ? id : Guid.Empty;
    }

    // ── GET /api/v1/account-security/locked ──────────────────────────────────

    /// <summary>Returns a list of all currently locked-out non-admin accounts.</summary>
    [HttpGet("locked")]
    public async Task<IActionResult> GetLockedAccounts(CancellationToken ct)
    {
        var accounts = await _securityService.GetLockedAccountsAsync(ct);
        return Ok(accounts);
    }

    // ── GET /api/v1/account-security/{userId}/status ─────────────────────────

    /// <summary>Returns the lockout status for a specific user account.</summary>
    [HttpGet("{userId:guid}/status")]
    public async Task<IActionResult> GetStatus(Guid userId, CancellationToken ct)
    {
        var status = await _securityService.GetLockoutStatusAsync(userId, ct);
        if (status is null) return NotFound();
        return Ok(status);
    }

    // ── POST /api/v1/account-security/{userId}/unlock ────────────────────────

    /// <summary>Unlocks a locked-out account and resets the failed attempt counter.</summary>
    [HttpPost("{userId:guid}/unlock")]
    public async Task<IActionResult> Unlock(Guid userId, CancellationToken ct)
    {
        var adminId = GetUserId();
        if (adminId == Guid.Empty) return Forbid();

        try
        {
            await _securityService.UnlockAccountAsync(userId, adminId, ct);
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

    // ── POST /api/v1/account-security/{userId}/reset-password ────────────────

    /// <summary>Admin resets the password for a non-admin account. Also unlocks the account if locked.</summary>
    [HttpPost("{userId:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        Guid userId,
        [FromBody] string newPassword,
        CancellationToken ct)
    {
        var adminId = GetUserId();
        if (adminId == Guid.Empty) return Forbid();

        try
        {
            await _securityService.ResetPasswordAsync(
                new AdminResetPasswordRequest(userId, newPassword),
                adminId,
                ct);
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
        catch (ArgumentException e)
        {
            return BadRequest(new { message = e.Message });
        }
    }

    // ── Phase 2 - ISO Security: Session management ─────────────────────────

    /// <summary>Returns all currently active user sessions across the system. Admin only.</summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> GetActiveSessions(CancellationToken ct)
    {
        var sessions = await _securityService.GetActiveSessionsAsync(ct);
        return Ok(sessions);
    }

    /// <summary>Force-revokes a specific active session by session ID. Admin only.</summary>
    [HttpPost("sessions/{sessionId:guid}/revoke")]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken ct)
    {
        var adminId = GetUserId();
        if (adminId == Guid.Empty) return Forbid();

        var success = await _securityService.RevokeSessionAsync(sessionId, adminId, ct);
        if (!success)
            return NotFound(new { message = "Session not found or already revoked." });

        return NoContent();
    }

    /// <summary>Force-revokes all active sessions for a specific user. Admin only.</summary>
    [HttpPost("users/{userId:guid}/revoke-sessions")]
    public async Task<IActionResult> RevokeAllUserSessions(Guid userId, CancellationToken ct)
    {
        var adminId = GetUserId();
        if (adminId == Guid.Empty) return Forbid();

        var count = await _securityService.RevokeAllSessionsForUserAsync(userId, adminId, ct);
        return Ok(new { revokedCount = count });
    }
}
