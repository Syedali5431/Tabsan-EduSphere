using Tabsan.EduSphere.Application.Dtos;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Service for account security operations: lockout status, admin unlock, and admin password reset.
/// Lockout policy: locked after configurable consecutive failed attempts (default 5), 15 min lockout.
/// Only Admin and SuperAdmin can unlock non-admin accounts and reset passwords.
/// </summary>
public interface IAccountSecurityService
{
    /// <summary>Gets lockout status for a specific user. Admin only.</summary>
    Task<AccountLockoutStatusDto?> GetLockoutStatusAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Admin unlocks a non-admin account and resets failed login counter.</summary>
    Task UnlockAccountAsync(Guid targetUserId, Guid adminUserId, CancellationToken ct = default);

    /// <summary>Admin resets the password for a non-admin account (e.g., after lockout).</summary>
    Task ResetPasswordAsync(AdminResetPasswordRequest request, Guid adminUserId, CancellationToken ct = default);

    /// <summary>Admin or super admin resets the password for any allowed account.</summary>
    Task ResetPasswordAsync(Guid targetUserId, string newPassword, Guid adminUserId, bool canResetAdminAccounts, bool requirePasswordChange, CancellationToken ct = default);

    /// <summary>Gets all currently locked-out non-admin accounts. Admin only.</summary>
    Task<IList<AccountLockoutStatusDto>> GetLockedAccountsAsync(CancellationToken ct = default);
}
