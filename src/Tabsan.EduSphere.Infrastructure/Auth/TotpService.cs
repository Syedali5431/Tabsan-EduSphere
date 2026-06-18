using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using OtpNet;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.Infrastructure.Auth;

/// <summary>
/// Production-ready TOTP service using Otp.NET for RFC 6238 compliance.
/// Compatible with Google Authenticator, Microsoft Authenticator, and Authy.
/// </summary>
public sealed class TotpService : ITotpService
{
    private readonly ILogger<TotpService> _logger;

    public TotpService(ILogger<TotpService> logger)
    {
        _logger = logger;
    }

    /// <summary>Generates a cryptographically secure Base32-encoded TOTP secret.</summary>
    public string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        return Base32Encoding.ToString(bytes);
    }

    /// <summary>
    /// Builds an otpauth:// URI for authenticator app enrollment.
    /// Format: otpauth://totp/{issuer}:{account}?secret={secret}&issuer={issuer}&digits={digits}&period={stepSeconds}
    /// </summary>
    public string BuildProvisioningUri(string issuer, string accountName, string secret, int digits, int stepSeconds)
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        // Most authenticator apps don't decode %40 back to @, so preserve it literally.
        var encodedAccount = Uri.EscapeDataString(accountName).Replace("%40", "@");
        return $"otpauth://totp/{encodedIssuer}:{encodedAccount}?secret={secret}&issuer={encodedIssuer}&digits={digits}&period={stepSeconds}";
    }

    /// <summary>
    /// Validates a TOTP code using Otp.NET's RFC 6238 implementation.
    /// Handles clock drift via VerificationWindow (allowedDriftWindows on each side).
    /// </summary>
    public bool ValidateCode(string secret, string code, DateTime utcNow, int digits, int stepSeconds, int allowedDriftWindows)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogWarning("TOTP validation skipped: secret is null or empty.");
            return false;
        }

        var normalized = code?.Trim() ?? string.Empty;
        if (normalized.Length != digits || !normalized.All(char.IsDigit))
        {
            _logger.LogDebug("TOTP code rejected: invalid format (length={Length}, expected {Digits})", normalized.Length, digits);
            return false;
        }

        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes, step: stepSeconds, totpSize: digits);

            // VerifyTotp returns true if the provided code matches
            // within the verification window (allowedDrift on each side).
            var isValid = totp.VerifyTotp(
                normalized,
                out long timeStepMatched,
                new VerificationWindow(allowedDriftWindows, allowedDriftWindows));

            _logger.LogDebug(
                "TOTP validation result: {Result}, timeStepMatched={TimeStep}, drift={Drift}",
                isValid, timeStepMatched, allowedDriftWindows);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TOTP validation failed with exception.");
            return false;
        }
    }
}
