namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Lightweight 2FA persistence boundary used by the two-factor authentication add-on.
/// It stores the protected TOTP secret alongside the user's current 2FA state.
/// </summary>
public sealed record TwoFactorStateSnapshot(
    Guid UserId,
    string Username,
    string? Email,
    bool IsActive,
    bool TwoFactorEnabled,
    string? SecretKey);

public interface ITwoFactorStateStore
{
    /// <summary>Returns the current 2FA snapshot for the specified user, or null when the user is missing.</summary>
    Task<TwoFactorStateSnapshot?> GetAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Saves a new protected secret for an in-progress 2FA setup.</summary>
    Task<bool> SaveSetupAsync(Guid userId, string secretKey, CancellationToken ct = default);

    /// <summary>Marks 2FA as enabled after the confirmation code has been validated.</summary>
    Task<bool> EnableAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Sets MfaIsEnabled=false while preserving the stored secret for later re-enable.</summary>
    Task<bool> DisableAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Fully clears the stored 2FA secret, recovery codes, and disables 2FA.</summary>
    Task<bool> HardDeleteAsync(Guid userId, CancellationToken ct = default);
}