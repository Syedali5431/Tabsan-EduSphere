using System.ComponentModel.DataAnnotations;

namespace Tabsan.EduSphere.Application.DTOs.Auth;

/// <summary>Request body sent by a client to the POST /api/v1/auth/login endpoint.</summary>
public sealed record LoginRequest(
    [property: Required]
    [property: StringLength(100, MinimumLength = 3)]
    [property: RegularExpression("^[a-zA-Z0-9._-]+$")]
    string Username,

    [property: Required]
    [property: StringLength(256, MinimumLength = 8)]
    string Password,

    [property: StringLength(64)]
    string? MfaCode = null,

    [property: StringLength(512)]
    string? DeviceInfo = null);

/// <summary>
/// Returned on a successful login.
/// The access token is short-lived (default 15 min).
/// The refresh token is long-lived (default 7 days) and stored as an HttpOnly cookie.
/// MustChangePassword is true for accounts imported via CSV (P4-S2-02) — the client
/// must redirect the user to the forced password change page before any other action.
/// </summary>
public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    string Role,
    Guid UserId,
    string Username,
    bool MustChangePassword = false,
    bool MfaEnabled = false,
    bool SsoEnabled = false,
    string? SsoProvider = null,
    string SessionRiskLevel = "low");

/// <summary>Request body sent to POST /api/v1/auth/refresh.</summary>
public sealed record RefreshRequest([property: Required] string RefreshToken);

/// <summary>Request body for POST /api/v1/auth/change-password.</summary>
public sealed record ChangePasswordRequest(
    [property: Required]
    [property: StringLength(256, MinimumLength = 8)]
    string CurrentPassword,

    [property: Required]
    [property: StringLength(16, MinimumLength = 12)]
    string NewPassword);

/// <summary>Request body for POST /api/v1/auth/force-change-password (P4-S2-02).</summary>
public sealed record ForceChangePasswordRequest(
    [property: Required]
    [property: StringLength(256, MinimumLength = 1)]
    string CurrentPassword,

    [property: Required]
    [property: StringLength(16, MinimumLength = 12)]
    string NewPassword);

/// <summary>Request body for completing MFA enrollment by proving current authenticator ownership.</summary>
public sealed record EnableMfaRequest(
    [property: Required]
    [property: StringLength(16, MinimumLength = 6)]
    string Code);

/// <summary>Response for MFA setup bootstrap, including secret and one-time recovery codes.</summary>
public sealed record MfaSetupResponse(
    bool Enabled,
    string Secret,
    string ProvisioningUri,
    IReadOnlyList<string> RecoveryCodes);

/// <summary>Response payload containing newly generated recovery codes.</summary>
public sealed record MfaRecoveryCodesResponse(
    IReadOnlyList<string> RecoveryCodes);

// ── P2-S1-01: Login result with failure reason for concurrency enforcement ──

/// <summary>
/// Reason why a login attempt was rejected.
/// Allows the controller to return different HTTP responses per scenario.
/// </summary>
public enum LoginFailureReason
{
    /// <summary>Credentials were invalid or the account is inactive/locked.</summary>
    InvalidCredentials,

    /// <summary>Login was blocked because the active session count reached the license limit (P2-S1-01).</summary>
    ConcurrencyLimitReached,

    /// <summary>MFA is required by deployment policy and no valid code was supplied.</summary>
    MfaRequired,

    /// <summary>Session risk control blocked this sign-in attempt.</summary>
    SessionRiskBlocked
}

/// <summary>
/// Security profile exposed to clients so login UX can adapt based on deployment policy.
/// </summary>
public sealed record AuthSecurityProfileResponse(
    bool MfaEnabled,
    bool RequireMfaForPasswordLogin,
    bool RequireMfaForPrivilegedRolesOnly,
    string[] PrivilegedMfaRoles,
    bool SsoEnabled,
    string? SsoProvider,
    string? SsoLoginUrl,
    bool SessionRiskEnabled,
    bool BlockHighRiskLogin);

/// <summary>
/// Wrapper returned by IAuthService.LoginAsync.
/// On success, Response is populated. On failure, FailureReason indicates why.
/// </summary>
public sealed class LoginResult
{
    public bool IsSuccess { get; private init; }
    public LoginResponse? Response { get; private init; }
    public LoginFailureReason? FailureReason { get; private init; }

    public static LoginResult Ok(LoginResponse response) =>
        new() { IsSuccess = true, Response = response };

    public static LoginResult Fail(LoginFailureReason reason) =>
        new() { IsSuccess = false, FailureReason = reason };
}
