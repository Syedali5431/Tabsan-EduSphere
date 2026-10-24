using Tabsan.EduSphere.Application.DTOs.Auth;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;

namespace Tabsan.EduSphere.Application.Interfaces;

// Plan M1 Stages M1.2 (OIDC) and M1.3 (SAML 2.0) — SSO contracts.

/// <summary>Connection details for one OpenID Connect provider (client secret already decrypted).</summary>
public sealed record OidcProviderConfig(string Authority, string ClientId, string ClientSecret);

/// <summary>
/// Identity asserted by a validated id_token (OIDC) or signed assertion (SAML).
/// <see cref="Subject"/> is the provider's stable identifier for the person; <see cref="Attributes"/> holds every
/// claim/attribute (name → values) for the provider's institution-ID and role mapping.
/// </summary>
public sealed record ExternalIdentity(
    string Subject,
    string? Email,
    bool EmailVerified,
    string? Name,
    bool MfaAsserted,
    IReadOnlyDictionary<string, string[]>? Attributes = null)
{
    /// <summary>Values of one claim/attribute (case-insensitive name); empty when absent.</summary>
    public IReadOnlyList<string> Values(string? name)
        => string.IsNullOrWhiteSpace(name) || Attributes is null
            ? []
            : Attributes.FirstOrDefault(a => string.Equals(a.Key, name.Trim(), StringComparison.OrdinalIgnoreCase)).Value ?? [];
}

/// <summary>Raised when an SSO protocol exchange or token/assertion validation fails. The message is safe to log, not to show.</summary>
public sealed class SsoProtocolException : Exception
{
    public SsoProtocolException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>
/// Low-level OpenID Connect authorization-code + PKCE client
/// (discovery, authorize URL, code redemption, id_token validation).
/// </summary>
public interface IOidcClient
{
    /// <summary>Builds the provider's authorize URL for the code flow with PKCE (S256).</summary>
    Task<string> BuildAuthorizationUrlAsync(
        OidcProviderConfig provider, string redirectUri, string state, string nonce, string codeChallenge,
        CancellationToken ct = default);

    /// <summary>
    /// Redeems the authorization code and validates the returned id_token
    /// (signature, issuer, audience, lifetime, nonce). Throws <see cref="SsoProtocolException"/> on failure.
    /// </summary>
    Task<ExternalIdentity> RedeemCodeAsync(
        OidcProviderConfig provider, string code, string redirectUri, string codeVerifier, string expectedNonce,
        CancellationToken ct = default);
}

/// <summary>
/// SAML 2.0 service-provider endpoints for one portal origin (and tenant).
/// The entity ID is always derived from the ACS URL — never taken from a caller —
/// so an assertion issued to a different service provider can never match our audience.
/// </summary>
public sealed record SamlServiceProvider(string EntityId, string AcsUrl);

/// <summary>A SAML AuthnRequest ready to send: browser redirect URL plus the request ID to expect in InResponseTo.</summary>
public sealed record SamlAuthnRequest(string RedirectUrl, string RequestId);

/// <summary>SAML 2.0 service-provider protocol operations (HTTP-Redirect requests, HTTP-POST responses).</summary>
public interface ISamlClient
{
    /// <summary>
    /// Builds an AuthnRequest for the IdP described by <paramref name="idpMetadataUrl"/>, signed with the
    /// service-provider certificate when <paramref name="signRequest"/> is set.
    /// </summary>
    Task<SamlAuthnRequest> BuildAuthnRequestAsync(
        string idpMetadataUrl, SamlServiceProvider sp, string relayState, bool signRequest = false, CancellationToken ct = default);

    /// <summary>
    /// Validates a base64 SAMLResponse: IdP signature (certificates from metadata), issuer,
    /// audience, validity window, destination/recipient, success status and InResponseTo.
    /// Encrypted assertions are decrypted with the service-provider certificate; with
    /// <paramref name="requireEncryptedAssertion"/> an unencrypted assertion is refused.
    /// Throws <see cref="SsoProtocolException"/> on any failure.
    /// </summary>
    Task<ExternalIdentity> ValidateResponseAsync(
        string idpMetadataUrl, SamlServiceProvider sp, string samlResponse, string expectedRequestId,
        bool requireEncryptedAssertion = false, CancellationToken ct = default);

    /// <summary>
    /// Service-provider metadata XML to register at the IdP (with the SP certificate for signing and encryption).
    /// </summary>
    string BuildServiceProviderMetadata(SamlServiceProvider sp, bool authnRequestsSigned = false);
}

/// <summary>Persistence for SSO provider settings and external login links.</summary>
public interface ISsoRepository
{
    Task<Guid?> GetTenantIdByCodeAsync(string tenantCode, CancellationToken ct = default);
    Task<SsoProviderSetting?> GetSettingAsync(Guid? tenantId, SsoProvider provider, CancellationToken ct = default);
    Task<IReadOnlyList<SsoProviderSetting>> GetEnabledSettingsAsync(Guid? tenantId, CancellationToken ct = default);
    Task<ExternalLogin?> FindLinkAsync(Guid? tenantId, SsoProvider provider, string externalSubjectId, CancellationToken ct = default);
    Task<bool> UserHasLinkAsync(Guid userId, SsoProvider provider, CancellationToken ct = default);
    Task AddLinkAsync(ExternalLogin link, CancellationToken ct = default);

    /// <summary>
    /// Users of <paramref name="tenantId"/> (null = any) whose institution ID is <paramref name="institutionId"/>:
    /// students by registration number, everyone by username. Callers link only when exactly one matches.
    /// </summary>
    Task<IReadOnlyList<Guid>> FindUserIdsByInstitutionIdAsync(Guid? tenantId, string institutionId, CancellationToken ct = default);

    /// <summary>Plan M1.4: enabled settings with EnforceSsoOnly that apply to a user of <paramref name="tenantId"/> (its own + installation-wide).</summary>
    Task<IReadOnlyList<SsoProviderSetting>> GetEnforcingSettingsAsync(Guid? tenantId, CancellationToken ct = default);

    /// <summary>Plan M1.4: all external links of one user.</summary>
    Task<IReadOnlyList<ExternalLogin>> GetLinksForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Plan M1.4: removes a link (the audit log keeps the history).</summary>
    void RemoveLink(ExternalLogin link);

    /// <summary>Plan M1.5: every live provider setting with its tenant code (null = installation-wide).</summary>
    Task<IReadOnlyList<(SsoProviderSetting Setting, string? TenantCode)>> GetAllSettingsAsync(CancellationToken ct = default);

    /// <summary>Plan M1.5: stages a new provider setting.</summary>
    Task AddSettingAsync(SsoProviderSetting setting, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Plan M1.4: decides whether a user must sign in through SSO instead of a password.</summary>
public interface ISsoLoginPolicy
{
    /// <summary>
    /// True when an enabled, complete provider covering the user's tenant (or the whole installation)
    /// has EnforceSsoOnly set, the "sso" module is active, and the user is not a SuperAdmin (break-glass).
    /// </summary>
    Task<bool> IsPasswordLoginBlockedAsync(User user, CancellationToken ct = default);
}

/// <summary>A user's linked external account, as shown in user settings / admin screens.</summary>
public sealed record SsoLinkSummary(string Provider, string? Email, DateTime LinkedAt, DateTime? LastLoginAt);

/// <summary>Why an unlink request was refused.</summary>
public enum SsoUnlinkResult { Unlinked, NotFound, WouldLockOut }

/// <summary>Plan M1.4: viewing and removing a user's external account links (with audit).</summary>
public interface ISsoAccountService
{
    Task<IReadOnlyList<SsoLinkSummary>> GetLinksAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Removes the user's link to <paramref name="provider"/>. A self-service unlink is refused when it would
    /// leave the user with no way to sign in (password login blocked by SSO-only and no other link).
    /// </summary>
    Task<SsoUnlinkResult> UnlinkAsync(Guid userId, string provider, Guid actorUserId, bool isSelfService, string? ipAddress, CancellationToken ct = default);
}

/// <summary>Plan M1.5: SuperAdmin management of SSO provider settings (audited; secrets never returned).</summary>
public interface ISsoSettingsService
{
    Task<IReadOnlyList<SsoProviderSettingDto>> ListAsync(CancellationToken ct = default);

    /// <summary>Creates or updates one provider. Returns the saved setting, or an error message for the admin.</summary>
    Task<(SsoProviderSettingDto? Saved, string? Error)> SaveAsync(
        string provider, string? tenantCode, SaveSsoProviderSettingRequest request, Guid actorUserId, string? ipAddress, CancellationToken ct = default);

    /// <summary>Removes one provider (soft delete). False when it does not exist.</summary>
    Task<bool> DeleteAsync(string provider, string? tenantCode, Guid actorUserId, string? ipAddress, CancellationToken ct = default);

    /// <summary>Checks that the saved configuration can reach the provider (discovery / metadata, secret decryptable).</summary>
    Task<SsoConnectionTestResult> TestAsync(string provider, string? tenantCode, CancellationToken ct = default);
}

/// <summary>Orchestrates the browser SSO round trip on behalf of the web portal.</summary>
public interface ISsoSignInService
{
    /// <summary>Enabled providers for the tenant (null/empty code = installation-wide providers).</summary>
    Task<IReadOnlyList<SsoProviderSummary>> GetProvidersAsync(string? tenantCode, CancellationToken ct = default);

    /// <summary>Starts a sign-in; returns null with a failure reason when it cannot start.</summary>
    Task<(SsoStartResponse? Response, SsoFailureReason? Failure)> StartAsync(SsoStartRequest request, CancellationToken ct = default);

    /// <summary>Completes the IdP callback: redeem code, resolve/link user, issue EduSphere tokens.</summary>
    Task<SsoResult> CompleteAsync(SsoCompleteRequest request, string? ipAddress, CancellationToken ct = default);

    /// <summary>Second step for users with TOTP enabled.</summary>
    Task<SsoResult> CompleteMfaAsync(SsoMfaRequest request, string? ipAddress, CancellationToken ct = default);

    /// <summary>Completes a SAML sign-in from the SAMLResponse the IdP posted to the portal's ACS.</summary>
    Task<SsoResult> CompleteSamlAsync(SsoSamlCompleteRequest request, string? ipAddress, CancellationToken ct = default);

    /// <summary>Service-provider metadata for the given ACS URL / tenant, or null when the ACS URL or tenant is not allowed.</summary>
    Task<string?> GetSamlMetadataAsync(string acsUrl, string? tenantCode, CancellationToken ct = default);
}
