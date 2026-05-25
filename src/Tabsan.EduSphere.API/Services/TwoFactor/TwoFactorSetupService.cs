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

        var secret = _twoFactor.GenerateSecret();
        var saved = await _stateStore.SaveSetupAsync(userId, secret, ct);
        if (!saved)
            return null;

        var accountName = string.IsNullOrWhiteSpace(snapshot.Email) ? snapshot.Username : snapshot.Email;
        var provisioningUri = _twoFactor.BuildProvisioningUri(accountName, secret);
        var qrCodeDataUrl = _qrCode.GenerateDataUrl(provisioningUri);

        return new TwoFactorSetupResponse(
            TwoFactorEnabled: snapshot.TwoFactorEnabled,
            Issuer: _twoFactor.Issuer,
            AccountName: accountName,
            ManualKey: secret,
            ProvisioningUri: provisioningUri,
            QrCodeDataUrl: qrCodeDataUrl);
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

        return await _stateStore.DisableAsync(userId, ct);
    }

    public async Task<bool> VerifyLoginAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var snapshot = await _stateStore.GetAsync(userId, ct);
        if (snapshot is null || !snapshot.IsActive || !snapshot.TwoFactorEnabled || string.IsNullOrWhiteSpace(snapshot.SecretKey))
            return false;

        return _twoFactor.ValidateCode(snapshot.SecretKey, code);
    }
}