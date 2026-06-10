using Tabsan.EduSphere.Application.DTOs.TwoFactor;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Services.TwoFactor;

/// <summary>
/// Orchestrates the add-on 2FA setup, verify, disable, and login-verification flows.
/// Existing login flow can hand off to these methods without refactoring the legacy auth service.
/// </summary>
public sealed class TwoFactorSetupService
{
    private readonly ITwoFactorStateStore _stateStore;
    private readonly TwoFactorService _twoFactor;
    private readonly QRCodeService _qrCode;

    public TwoFactorSetupService(
        ITwoFactorStateStore stateStore,
        TwoFactorService twoFactor,
        QRCodeService qrCode)
    {
        _stateStore = stateStore;
        _twoFactor = twoFactor;
        _qrCode = qrCode;
    }

    public async Task<TwoFactorSetupResponse?> BeginSetupAsync(Guid userId, CancellationToken ct = default)
    {
        var snapshot = await _stateStore.GetAsync(userId, ct);
        if (snapshot is null || !snapshot.IsActive)
            return null;

        // If a secret already exists (pending setup OR already enabled),
        // return the existing setup to avoid invalidating a previously shown QR/manual key.
        if (!string.IsNullOrWhiteSpace(snapshot.SecretKey))
        {
            var accountName = string.IsNullOrWhiteSpace(snapshot.Email) ? snapshot.Username : snapshot.Email;
            var provisioningUri = _twoFactor.BuildProvisioningUri(accountName, snapshot.SecretKey);
            var qrCodeDataUrl = _qrCode.GenerateDataUrl(provisioningUri);

            return new TwoFactorSetupResponse(
                TwoFactorEnabled: snapshot.TwoFactorEnabled,
                Issuer: _twoFactor.Issuer,
                AccountName: accountName,
                ManualKey: snapshot.SecretKey,
                ProvisioningUri: provisioningUri,
                QrCodeDataUrl: qrCodeDataUrl);
        }

        var secret = _twoFactor.GenerateSecret();
        var saved = await _stateStore.SaveSetupAsync(userId, secret, ct);
        if (!saved)
            return null;

        var newAccountName = string.IsNullOrWhiteSpace(snapshot.Email) ? snapshot.Username : snapshot.Email;
        var newProvisioningUri = _twoFactor.BuildProvisioningUri(newAccountName, secret);
        var newQrCodeDataUrl = _qrCode.GenerateDataUrl(newProvisioningUri);

        return new TwoFactorSetupResponse(
            TwoFactorEnabled: snapshot.TwoFactorEnabled,
            Issuer: _twoFactor.Issuer,
            AccountName: newAccountName,
            ManualKey: secret,
            ProvisioningUri: newProvisioningUri,
            QrCodeDataUrl: newQrCodeDataUrl);
    }

    /// <summary>Returns the current 2FA status without modifying any state.</summary>
    public async Task<TwoFactorStateSnapshot?> GetStatusAsync(Guid userId, CancellationToken ct = default)
    {
        return await _stateStore.GetAsync(userId, ct);
    }

    public async Task<bool> VerifySetupAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var snapshot = await _stateStore.GetAsync(userId, ct);
        if (snapshot is null || !snapshot.IsActive || string.IsNullOrWhiteSpace(snapshot.SecretKey))
            return false;

        if (!_twoFactor.ValidateCode(snapshot.SecretKey, code))
            return false;

        return await _stateStore.EnableAsync(userId, ct);
    }

    public async Task<bool> DisableAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var snapshot = await _stateStore.GetAsync(userId, ct);
        if (snapshot is null || !snapshot.IsActive || !snapshot.TwoFactorEnabled || string.IsNullOrWhiteSpace(snapshot.SecretKey))
            return false;

        if (!_twoFactor.ValidateCode(snapshot.SecretKey, code))
            return false;

        // Soft-disable: keeps the secret so user can re-enable later with a valid code.
        return await _stateStore.DisableAsync(userId, ct);
    }

    /// <summary>Re-enables 2FA using an existing stored secret and a valid TOTP code.</summary>
    public async Task<bool> EnableWithCodeAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var snapshot = await _stateStore.GetAsync(userId, ct);
        if (snapshot is null || !snapshot.IsActive || string.IsNullOrWhiteSpace(snapshot.SecretKey))
            return false;

        // Already enabled — nothing to do.
        if (snapshot.TwoFactorEnabled)
            return true;

        if (!_twoFactor.ValidateCode(snapshot.SecretKey, code))
            return false;

        return await _stateStore.EnableAsync(userId, ct);
    }

    /// <summary>Clears a pending or disabled 2FA setup so the user can start fresh.</summary>
    public async Task<bool> ResetSetupAsync(Guid userId, CancellationToken ct = default)
    {
        var snapshot = await _stateStore.GetAsync(userId, ct);
        if (snapshot is null || !snapshot.IsActive)
            return false;

        // Allow reset when 2FA is not enabled (pending or soft-disabled state).
        if (snapshot.TwoFactorEnabled)
            return false;

        return await _stateStore.HardDeleteAsync(userId, ct);
    }

    public async Task<bool> VerifyLoginAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var snapshot = await _stateStore.GetAsync(userId, ct);
        if (snapshot is null || !snapshot.IsActive || !snapshot.TwoFactorEnabled || string.IsNullOrWhiteSpace(snapshot.SecretKey))
            return false;

        return _twoFactor.ValidateCode(snapshot.SecretKey, code);
    }
}