using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Auth;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Services.TwoFactor;

/// <summary>
/// Core TOTP helper for the Plan L add-on.
/// It wraps the shared ITotpService with deployment defaults from AuthSecurityOptions.
/// </summary>
public sealed class TwoFactorService
{
    private readonly ITotpService _totp;
    private readonly AuthSecurityOptions _security;

    public TwoFactorService(ITotpService totp, IOptions<AuthSecurityOptions> security)
    {
        _totp = totp;
        _security = security.Value;
    }

    public string Issuer => _security.Mfa.TotpIssuer;

    public int Digits => _security.Mfa.TotpDigits;

    public int StepSeconds => _security.Mfa.TotpStepSeconds;

    public int AllowedDriftWindows => _security.Mfa.TotpAllowedDriftWindows;

    public string GenerateSecret() => _totp.GenerateSecret();

    public string BuildProvisioningUri(string accountName, string secret)
        => _totp.BuildProvisioningUri(Issuer, accountName, secret, Digits, StepSeconds);

    public bool ValidateCode(string secret, string code, DateTime? utcNow = null)
        => _totp.ValidateCode(secret, code, utcNow ?? DateTime.UtcNow, Digits, StepSeconds, AllowedDriftWindows);
}