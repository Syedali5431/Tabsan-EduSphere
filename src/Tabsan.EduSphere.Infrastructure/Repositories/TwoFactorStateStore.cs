using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>
/// EF-backed 2FA state store for the two-factor authentication add-on.
/// It keeps the implementation isolated from the legacy auth services.
/// </summary>
public sealed class TwoFactorStateStore : ITwoFactorStateStore
{
    private const string ProtectorPurpose = "Tabsan.EduSphere.TwoFactorSecret.v1";

    private readonly ApplicationDbContext _db;
    private readonly IDataProtector _protector;

    public TwoFactorStateStore(ApplicationDbContext db, IDataProtectionProvider dataProtectionProvider)
    {
        _db = db;
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
    }

    public async Task<TwoFactorStateSnapshot?> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return null;

        return new TwoFactorStateSnapshot(
            user.Id,
            user.Username,
            user.Email,
            user.IsActive,
            user.MfaIsEnabled,
            TryUnprotect(user.MfaTotpSecret));
    }

    public async Task<bool> SaveSetupAsync(Guid userId, string secretKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
            return false;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return false;

        _db.Entry(user).Property(nameof(User.MfaTotpSecret)).CurrentValue = _protector.Protect(secretKey);
        _db.Entry(user).Property(nameof(User.MfaRecoveryCodesHashJson)).CurrentValue = null;
        _db.Entry(user).Property(nameof(User.MfaIsEnabled)).CurrentValue = false;
        user.Touch();

        return await _db.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> EnableAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return false;

        if (string.IsNullOrWhiteSpace(user.MfaTotpSecret))
            return false;

        _db.Entry(user).Property(nameof(User.MfaIsEnabled)).CurrentValue = true;
        user.Touch();

        return await _db.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> DisableAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return false;

        _db.Entry(user).Property(nameof(User.MfaTotpSecret)).CurrentValue = null;
        _db.Entry(user).Property(nameof(User.MfaRecoveryCodesHashJson)).CurrentValue = null;
        _db.Entry(user).Property(nameof(User.MfaIsEnabled)).CurrentValue = false;
        user.Touch();

        return await _db.SaveChangesAsync(ct) > 0;
    }

    private string? TryUnprotect(string? protectedSecret)
    {
        if (string.IsNullOrWhiteSpace(protectedSecret))
            return null;

        try
        {
            return _protector.Unprotect(protectedSecret);
        }
        catch
        {
            return null;
        }
    }
}