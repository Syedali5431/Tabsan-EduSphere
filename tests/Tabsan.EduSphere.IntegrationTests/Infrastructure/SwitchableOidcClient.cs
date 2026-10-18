using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.IntegrationTests.Infrastructure;

/// <summary>
/// Plan M1: test-only <see cref="IOidcClient"/> that forwards to whatever stub a test installs,
/// so SSO flows can run without a real identity provider or a second test host.
/// </summary>
public sealed class SwitchableOidcClient : IOidcClient
{
    public IOidcClient? Inner { get; set; }

    private IOidcClient Current => Inner ?? throw new OidcException("No identity provider stub installed for this test.");

    public Task<string> BuildAuthorizationUrlAsync(OidcProviderConfig provider, string redirectUri, string state, string nonce, string codeChallenge, CancellationToken ct = default)
        => Current.BuildAuthorizationUrlAsync(provider, redirectUri, state, nonce, codeChallenge, ct);

    public Task<OidcIdentity> RedeemCodeAsync(OidcProviderConfig provider, string code, string redirectUri, string codeVerifier, string expectedNonce, CancellationToken ct = default)
        => Current.RedeemCodeAsync(provider, code, redirectUri, codeVerifier, expectedNonce, ct);
}
