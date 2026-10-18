using System.ComponentModel.DataAnnotations;

namespace Tabsan.EduSphere.Application.DTOs.Auth;

// Plan M1 Stage M1.2 — SSO sign-in contracts (API ⇄ Web portal).

/// <summary>An enabled SSO provider shown as a button on the login page.</summary>
public sealed record SsoProviderSummary(string Provider, string DisplayName);

/// <summary>Body for POST /api/v1/sso/start.</summary>
public sealed record SsoStartRequest(
    [property: Required, StringLength(32)] string Provider,
    [property: Required, StringLength(500)] string RedirectUri,
    [property: StringLength(64)] string? TenantCode = null);

/// <summary>Where the browser must be sent, plus the state value the caller binds to the browser.</summary>
public sealed record SsoStartResponse(string AuthorizationUrl, string State);

/// <summary>Body for POST /api/v1/sso/complete (the IdP callback relayed by the portal).</summary>
public sealed record SsoCompleteRequest(
    [property: Required, StringLength(200)] string State,
    [property: Required, StringLength(4096)] string Code,
    [property: StringLength(512)] string? DeviceInfo = null);

/// <summary>Body for POST /api/v1/sso/saml/complete (the SAMLResponse posted to the portal ACS).</summary>
public sealed record SsoSamlCompleteRequest(
    [property: Required, StringLength(256000)] string SamlResponse,
    [property: Required, StringLength(200)] string RelayState,
    [property: StringLength(512)] string? DeviceInfo = null);

/// <summary>Body for POST /api/v1/sso/complete-mfa (second step when the user has TOTP enabled).</summary>
public sealed record SsoMfaRequest(
    [property: Required, StringLength(200)] string Ticket,
    [property: Required, StringLength(64)] string MfaCode,
    [property: StringLength(512)] string? DeviceInfo = null);

/// <summary>Why an SSO sign-in did not produce a session.</summary>
public enum SsoFailureReason
{
    /// <summary>Provider unknown, disabled or incompletely configured.</summary>
    ProviderUnavailable,

    /// <summary>The redirect URI is not an allowed portal callback.</summary>
    InvalidRedirectUri,

    /// <summary>State missing, expired or already used.</summary>
    InvalidState,

    /// <summary>Code exchange or id_token validation failed.</summary>
    TokenValidationFailed,

    /// <summary>The email domain is not on the provider's allow-list.</summary>
    EmailDomainNotAllowed,

    /// <summary>No EduSphere account is linked to this external identity.</summary>
    AccountNotLinked,

    /// <summary>The user has TOTP enabled; resend with the ticket and an authenticator code.</summary>
    MfaCodeRequired,

    /// <summary>The authenticator code was wrong.</summary>
    InvalidMfaCode,

    /// <summary>The standard login gates refused the sign-in (inactive, locked, risk, license limit).</summary>
    LoginRejected
}

/// <summary>Outcome of an SSO step. On success <see cref="Response"/> is the normal login payload.</summary>
public sealed class SsoResult
{
    public bool IsSuccess { get; private init; }
    public LoginResponse? Response { get; private init; }
    public SsoFailureReason? FailureReason { get; private init; }

    /// <summary>Underlying login gate failure when <see cref="FailureReason"/> is LoginRejected.</summary>
    public LoginFailureReason? LoginFailure { get; private init; }

    /// <summary>Short-lived one-time ticket returned with MfaCodeRequired / InvalidMfaCode.</summary>
    public string? MfaTicket { get; private init; }

    public static SsoResult Ok(LoginResponse response) => new() { IsSuccess = true, Response = response };

    public static SsoResult Fail(SsoFailureReason reason, string? mfaTicket = null) =>
        new() { FailureReason = reason, MfaTicket = mfaTicket };

    public static SsoResult Rejected(LoginFailureReason loginFailure) =>
        new() { FailureReason = SsoFailureReason.LoginRejected, LoginFailure = loginFailure };
}
