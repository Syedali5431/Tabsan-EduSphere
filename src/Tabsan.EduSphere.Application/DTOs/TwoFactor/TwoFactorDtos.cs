using System.ComponentModel.DataAnnotations;

namespace Tabsan.EduSphere.Application.DTOs.TwoFactor;

/// <summary>Returned by the add-on 2FA setup endpoint.</summary>
public sealed record TwoFactorSetupResponse(
    bool TwoFactorEnabled,
    string Issuer,
    string AccountName,
    string ManualKey,
    string ProvisioningUri,
    string QrCodeDataUrl);

/// <summary>Request body for verifying a freshly enrolled 2FA secret.</summary>
public sealed record TwoFactorVerifyRequest(
    [property: Required]
    [property: StringLength(16, MinimumLength = 6)]
    string Code);

/// <summary>Request body for disabling 2FA with a valid TOTP challenge.</summary>
public sealed record TwoFactorDisableRequest(
    [property: Required]
    [property: StringLength(16, MinimumLength = 6)]
    string Code);

/// <summary>Request body for the login hand-off verification endpoint.</summary>
public sealed record TwoFactorLoginVerifyRequest(
    [property: Required]
    Guid UserId,

    [property: Required]
    [property: StringLength(16, MinimumLength = 6)]
    string Code);

/// <summary>Simple response payload used by the add-on verification endpoints.</summary>
public sealed record TwoFactorVerificationResponse(
    bool Success,
    bool TwoFactorEnabled,
    string Message);