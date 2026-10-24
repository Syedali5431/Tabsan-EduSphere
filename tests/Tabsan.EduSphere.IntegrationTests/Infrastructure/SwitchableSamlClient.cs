using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Infrastructure.Auth;

namespace Tabsan.EduSphere.IntegrationTests.Infrastructure;

/// <summary>
/// Plan M1.3: test-only <see cref="ISamlClient"/> forwarding to a stub a test installs.
/// SP metadata generation needs no IdP, so it always uses the real implementation (with the API's SP certificate).
/// </summary>
public sealed class SwitchableSamlClient : ISamlClient
{
    private readonly SamlClient _real;

    public SwitchableSamlClient(SamlServiceProviderCertificate certificate) => _real = new SamlClient(new HttpClient(), certificate);

    public ISamlClient? Inner { get; set; }

    private ISamlClient Current => Inner ?? throw new SsoProtocolException("No SAML identity provider stub installed for this test.");

    public Task<SamlAuthnRequest> BuildAuthnRequestAsync(string idpMetadataUrl, SamlServiceProvider sp, string relayState, bool signRequest = false, CancellationToken ct = default)
        => Current.BuildAuthnRequestAsync(idpMetadataUrl, sp, relayState, signRequest, ct);

    public Task<ExternalIdentity> ValidateResponseAsync(string idpMetadataUrl, SamlServiceProvider sp, string samlResponse, string expectedRequestId,
        bool requireEncryptedAssertion = false, CancellationToken ct = default)
        => Current.ValidateResponseAsync(idpMetadataUrl, sp, samlResponse, expectedRequestId, requireEncryptedAssertion, ct);

    public string BuildServiceProviderMetadata(SamlServiceProvider sp, bool authnRequestsSigned = false) => _real.BuildServiceProviderMetadata(sp, authnRequestsSigned);
}
