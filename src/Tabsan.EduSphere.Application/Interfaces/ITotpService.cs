namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Provides TOTP primitives for MFA setup and verification.
/// </summary>
public interface ITotpService
{
    /// <summary>Generates a new Base32-encoded TOTP secret.</summary>
    string GenerateSecret();

    /// <summary>Builds an otpauth:// URI compatible with authenticator apps.</summary>
    string BuildProvisioningUri(string issuer, string accountName, string secret, int digits, int stepSeconds);

    /// <summary>Validates an RFC 6238 TOTP code.</summary>
    bool ValidateCode(string secret, string code, DateTime utcNow, int digits, int stepSeconds, int allowedDriftWindows);
}
