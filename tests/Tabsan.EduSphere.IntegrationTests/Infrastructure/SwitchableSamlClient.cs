using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Infrastructure.Auth;

namespace Tabsan.EduSphere.IntegrationTests.Infrastructure;

/// <summary>
/// Plan M1.3: test-only <see cref="ISamlClient"/> forwarding to a stub a test installs.
/// SP metadata generation needs no IdP, so it always uses the real implementation.
/// </summary>
public sealed class SwitchableSamlClient : ISamlClient
{
    private static readonly SamlClient Real = new(new HttpClient());

    public ISamlClient? Inner { get; set; }

    private ISamlClient Current => Inner ?? throw new SsoProtocolException("No SAML identity provider stub installed for this test.");

    public Task<SamlAuthnRequest> BuildAuthnRequestAsync(string idpMetadataUrl, SamlServiceProvider sp, string relayState, CancellationToken ct = default)
        => Current.BuildAuthnRequestAsync(idpMetadataUrl, sp, relayState, ct);

    public Task<ExternalIdentity> ValidateResponseAsync(string idpMetadataUrl, SamlServiceProvider sp, string samlResponse, string expectedRequestId, CancellationToken ct = default)
        => Current.ValidateResponseAsync(idpMetadataUrl, sp, samlResponse, expectedRequestId, ct);

    public string BuildServiceProviderMetadata(SamlServiceProvider sp) => Real.BuildServiceProviderMetadata(sp);
}
