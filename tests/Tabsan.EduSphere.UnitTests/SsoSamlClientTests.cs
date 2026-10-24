using System.IO.Compression;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using FluentAssertions;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Saml2;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Infrastructure.Auth;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>
/// Plan M1 Stage M1.3: SamlClient against an in-memory identity provider that serves real
/// metadata and mints genuinely signed SAML responses.
/// </summary>
public class SsoSamlClientTests
{
    private const string Acs = "https://portal.uni.edu/Login/SamlAcs";
    private static readonly SamlServiceProvider Sp = new("https://portal.uni.edu/Login/SamlMetadata", Acs);
    private const string Persistent = "urn:oasis:names:tc:SAML:2.0:nameid-format:persistent";
    private const string Transient = "urn:oasis:names:tc:SAML:2.0:nameid-format:transient";

    [Fact]
    public async Task BuildsRedirectAuthnRequestForOurAcsAndEntityId()
    {
        var idp = new FakeSamlIdp();
        var client = new SamlClient(new HttpClient(idp));

        var request = await client.BuildAuthnRequestAsync(idp.MetadataUrl, Sp, "relay-1");

        var uri = new Uri(request.RedirectUrl);
        uri.GetLeftPart(UriPartial.Path).Should().Be(idp.SsoUrl);
        var query = QueryHelpers.ParseQuery(uri.Query);
        query["RelayState"].ToString().Should().Be("relay-1");

        var xml = Inflate(query["SAMLRequest"].ToString());
        xml.Should().Contain($"ID=\"{request.RequestId}\"");
        xml.Should().Contain($"AssertionConsumerServiceURL=\"{Acs}\"");
        xml.Should().Contain(Sp.EntityId);
        xml.Should().Contain("urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST");
    }

    [Fact]
    public async Task ValidSignedResponse_YieldsIdentity()
    {
        var idp = new FakeSamlIdp();
        var client = new SamlClient(new HttpClient(idp));
        var response = idp.Respond("_req-1", attributes: new()
        {
            ["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] = "Ali@Uni.edu",
            ["urn:oid:2.16.840.1.113730.3.1.241"] = "Ali Khan"
        }, authnContext: "https://refeds.org/profile/mfa");

        var identity = await client.ValidateResponseAsync(idp.MetadataUrl, Sp, response, "_req-1");

        identity.Subject.Should().Be("persistent-123");
        identity.Email.Should().Be("Ali@Uni.edu");
        identity.EmailVerified.Should().BeTrue();
        identity.Name.Should().Be("Ali Khan");
        identity.MfaAsserted.Should().BeTrue();
    }

    [Fact]
    public async Task PrefersEntraObjectIdOverNameId()
    {
        var idp = new FakeSamlIdp();
        var response = idp.Respond("_r", attributes: new() { ["http://schemas.microsoft.com/identity/claims/objectidentifier"] = "oid-9" });

        var identity = await new SamlClient(new HttpClient(idp)).ValidateResponseAsync(idp.MetadataUrl, Sp, response, "_r");

        identity.Subject.Should().Be("oid-9");
        identity.MfaAsserted.Should().BeFalse();
    }

    [Fact]
    public async Task RejectsWrongRequestId()
    {
        var idp = new FakeSamlIdp();
        var response = idp.Respond("_other-request");

        await Rejects(idp, response, "_req-1");
    }

    [Fact]
    public async Task RejectsAssertionForAnotherServiceProvider()
    {
        var idp = new FakeSamlIdp();
        var response = idp.Respond("_r", audience: "https://some-other-app.example.com/saml");

        await Rejects(idp, response, "_r");
    }

    [Fact]
    public async Task RejectsWrongRecipientOrDestination()
    {
        var idp = new FakeSamlIdp();

        await Rejects(idp, idp.Respond("_r", recipient: "https://evil.example.com/Login/SamlAcs"), "_r");
        await Rejects(idp, idp.Respond("_r2", destination: "https://evil.example.com/Login/SamlAcs"), "_r2");
    }

    [Fact]
    public async Task RejectsTamperedAssertion()
    {
        var idp = new FakeSamlIdp();
        var response = idp.Respond("_r", attributes: new() { ["email"] = "student@uni.edu" });
        var tampered = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            Encoding.UTF8.GetString(Convert.FromBase64String(response)).Replace("student@uni.edu", "dean@uni.edu")));

        await Rejects(idp, tampered, "_r");
    }

    [Fact]
    public async Task RejectsUnsignedResponse()
    {
        var idp = new FakeSamlIdp();
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(Encoding.UTF8.GetString(Convert.FromBase64String(idp.Respond("_r"))));
        foreach (var sig in doc.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#").Cast<XmlNode>().ToList())
            sig.ParentNode!.RemoveChild(sig);

        await Rejects(idp, Convert.ToBase64String(Encoding.UTF8.GetBytes(doc.OuterXml)), "_r");
    }

    [Fact]
    public async Task RejectsResponseSignedByUnknownCertificate()
    {
        var idp = new FakeSamlIdp();
        using var attacker = FakeSamlIdp.CreateCertificate("CN=Attacker");

        await Rejects(idp, idp.Respond("_r", signingCertificate: attacker), "_r");
    }

    [Fact]
    public async Task RejectsExpiredAssertion()
    {
        var idp = new FakeSamlIdp();

        await Rejects(idp, idp.Respond("_r", issuedAt: DateTime.UtcNow.AddHours(-2)), "_r");
    }

    [Fact]
    public async Task RejectsTransientNameIdWithoutStableAttribute()
    {
        var idp = new FakeSamlIdp();

        await Rejects(idp, idp.Respond("_r", nameIdFormat: Transient), "_r");
    }

    [Fact]
    public async Task RejectsIdpErrorStatus()
    {
        var idp = new FakeSamlIdp();

        await Rejects(idp, idp.Respond("_r", status: Saml2StatusCodes.Responder), "_r");
    }

    [Fact]
    public async Task RejectsNonHttpsMetadata()
    {
        var idp = new FakeSamlIdp();
        var act = () => new SamlClient(new HttpClient(idp)).BuildAuthnRequestAsync("http://idp.test/metadata", Sp, "r");

        await act.Should().ThrowAsync<SsoProtocolException>();
    }

    // ── Signed requests and encrypted assertions (SP certificate) ──

    private static SamlServiceProviderCertificate SpCertificate()
        => new(Path.Combine(Path.GetTempPath(), "edusphere-saml-tests", Guid.NewGuid().ToString("N") + ".json"));

    [Fact]
    public async Task SignedAuthnRequest_CarriesASignatureTheSpCertificateVerifies()
    {
        var idp = new FakeSamlIdp();
        var sp = SpCertificate();
        var request = await new SamlClient(new HttpClient(idp), sp).BuildAuthnRequestAsync(idp.MetadataUrl, Sp, "relay-1", signRequest: true);

        var query = new Uri(request.RedirectUrl).Query.TrimStart('?');
        var signatureAt = query.IndexOf("&Signature=", StringComparison.Ordinal);
        signatureAt.Should().BePositive();
        var parsed = QueryHelpers.ParseQuery(query);
        parsed["SigAlg"].ToString().Should().Be("http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
        using var key = sp.PublicCertificate.GetRSAPublicKey()!;
        key.VerifyData(Encoding.UTF8.GetBytes(query[..signatureAt]), Convert.FromBase64String(parsed["Signature"].ToString()),
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).Should().BeTrue();

        // Unsigned by default.
        var plain = await new SamlClient(new HttpClient(idp), sp).BuildAuthnRequestAsync(idp.MetadataUrl, Sp, "relay-2");
        plain.RedirectUrl.Should().NotContain("Signature=");
    }

    [Fact]
    public async Task SigningWithoutAnSpCertificate_Fails()
    {
        var idp = new FakeSamlIdp();
        var act = () => new SamlClient(new HttpClient(idp)).BuildAuthnRequestAsync(idp.MetadataUrl, Sp, "r", signRequest: true);
        await act.Should().ThrowAsync<SsoProtocolException>();
    }

    [Fact]
    public async Task EncryptedAssertion_IsDecrypted_AndCanBeRequired()
    {
        var idp = new FakeSamlIdp();
        var sp = SpCertificate();
        var client = new SamlClient(new HttpClient(idp), sp);
        var encrypted = idp.Respond("_r", attributes: new() { ["email"] = "ali@uni.edu" }, encryptFor: sp.PublicCertificate);
        Encoding.UTF8.GetString(Convert.FromBase64String(encrypted)).Should().Contain("EncryptedAssertion").And.NotContain("ali@uni.edu");

        var identity = await client.ValidateResponseAsync(idp.MetadataUrl, Sp, encrypted, "_r", requireEncryptedAssertion: true);
        identity.Email.Should().Be("ali@uni.edu");
        identity.Subject.Should().Be("persistent-123");

        // A plain assertion is accepted unless encryption is required.
        (await client.ValidateResponseAsync(idp.MetadataUrl, Sp, idp.Respond("_p"), "_p")).Subject.Should().Be("persistent-123");
        var act = () => client.ValidateResponseAsync(idp.MetadataUrl, Sp, idp.Respond("_q"), "_q", requireEncryptedAssertion: true);
        await act.Should().ThrowAsync<SsoProtocolException>();
    }

    [Fact]
    public async Task AssertionEncryptedForAnotherCertificate_IsRejected()
    {
        var idp = new FakeSamlIdp();
        using var other = FakeSamlIdp.CreateCertificate("CN=Someone else");
        var response = idp.Respond("_r", encryptFor: new X509Certificate2(other.Export(X509ContentType.Cert)));

        var act = () => new SamlClient(new HttpClient(idp), SpCertificate()).ValidateResponseAsync(idp.MetadataUrl, Sp, response, "_r");
        await act.Should().ThrowAsync<SsoProtocolException>();
    }

    [Fact]
    public async Task Attributes_KeepEveryValue_ForIdAndRoleMapping()
    {
        var idp = new FakeSamlIdp();
        var response = idp.Respond("_r", attributes: new() { ["urn:oid:2.16.840.1.113730.3.1.3"] = "EMP-77" },
            multiValued: [("urn:oid:1.3.6.1.4.1.5923.1.1.1.1", "member"), ("urn:oid:1.3.6.1.4.1.5923.1.1.1.1", "faculty")]);

        var identity = await new SamlClient(new HttpClient(idp)).ValidateResponseAsync(idp.MetadataUrl, Sp, response, "_r");

        identity.Values("urn:oid:2.16.840.1.113730.3.1.3").Should().Equal("EMP-77");
        identity.Values("URN:OID:1.3.6.1.4.1.5923.1.1.1.1").Should().BeEquivalentTo("member", "faculty");
        identity.Values("missing").Should().BeEmpty();
    }

    [Fact]
    public void ServiceProviderMetadata_PublishesTheSpCertificate_ForSigningAndEncryption()
    {
        var sp = SpCertificate();
        var xml = new SamlClient(new HttpClient(), sp).BuildServiceProviderMetadata(Sp, authnRequestsSigned: true);
        var certificate = Convert.ToBase64String(sp.PublicCertificate.RawData);

        xml.Should().Contain("AuthnRequestsSigned=\"true\"");
        xml.Should().Contain("use=\"signing\"").And.Contain("use=\"encryption\"");
        xml.Should().Contain(certificate);
        new SamlClient(new HttpClient(), sp).BuildServiceProviderMetadata(Sp).Should().Contain("AuthnRequestsSigned=\"false\"");
    }

    [Fact]
    public void SpCertificate_IsCreatedOnce_AndReusedFromItsFile()
    {
        var file = Path.Combine(Path.GetTempPath(), "edusphere-saml-tests", Guid.NewGuid().ToString("N") + ".json");
        var first = new SamlServiceProviderCertificate(file).Certificate;
        first.HasPrivateKey.Should().BeTrue();
        File.Exists(file).Should().BeTrue();
        new SamlServiceProviderCertificate(file).Certificate.Thumbprint.Should().Be(first.Thumbprint);
    }

    [Fact]
    public void ServiceProviderMetadata_DescribesAcsAndRequiresSignedAssertions()
    {
        var xml = new SamlClient(new HttpClient()).BuildServiceProviderMetadata(Sp);

        xml.Should().Contain($"entityID=\"{Sp.EntityId}\"");
        xml.Should().Contain($"Location=\"{Acs}\"");
        xml.Should().Contain("WantAssertionsSigned=\"true\"");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static async Task Rejects(FakeSamlIdp idp, string samlResponse, string expectedRequestId)
    {
        var act = () => new SamlClient(new HttpClient(idp)).ValidateResponseAsync(idp.MetadataUrl, Sp, samlResponse, expectedRequestId);
        await act.Should().ThrowAsync<SsoProtocolException>();
    }

    private static string Inflate(string base64)
    {
        using var input = new MemoryStream(Convert.FromBase64String(base64));
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(deflate, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>In-memory SAML IdP: serves metadata over "https" and mints signed responses.</summary>
    private sealed class FakeSamlIdp : HttpMessageHandler
    {
        private readonly string _host = $"https://idp-{Guid.NewGuid():N}.test";
        private readonly X509Certificate2 _certificate = CreateCertificate("CN=Fake SAML IdP");

        public string EntityId => _host + "/entity";
        public string MetadataUrl => _host + "/metadata";
        public string SsoUrl => _host + "/sso";

        public static X509Certificate2 CreateCertificate(string subject)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
            // Round-trip through PFX so the private key is usable by SignedXml on every platform.
            return new X509Certificate2(cert.Export(X509ContentType.Pfx), (string?)null, X509KeyStorageFlags.Exportable);
        }

        public string Respond(
            string inResponseTo,
            Dictionary<string, string>? attributes = null,
            string? audience = null,
            string? recipient = null,
            string? destination = null,
            string nameIdFormat = Persistent,
            string? authnContext = null,
            DateTime? issuedAt = null,
            Saml2StatusCodes status = Saml2StatusCodes.Success,
            X509Certificate2? signingCertificate = null,
            X509Certificate2? encryptFor = null,
            (string Type, string Value)[]? multiValued = null)
        {
            var now = issuedAt ?? DateTime.UtcNow;
            var config = new Saml2Configuration
            {
                Issuer = EntityId,
                SigningCertificate = signingCertificate ?? _certificate,
                AuthnResponseSignType = Saml2AuthnResponseSignTypes.SignAssertionAndResponse,
                EncryptionCertificate = encryptFor
            };

            var response = new Saml2AuthnResponse(config)
            {
                InResponseTo = new Saml2Id(inResponseTo),
                Status = status,
                Destination = new Uri(destination ?? Acs),
                NameId = new Saml2NameIdentifier("persistent-123", new Uri(nameIdFormat))
            };

            // An AttributeStatement must contain at least one attribute; real IdPs always send some.
            var claims = (attributes ?? new()).Select(kv => new Claim(kv.Key, kv.Value))
                .Concat((multiValued ?? []).Select(m => new Claim(m.Type, m.Value)))
                .Append(new Claim("urn:oid:0.9.2342.19200300.100.1.1", "ali")).ToList();
            response.ClaimsIdentity = new ClaimsIdentity(claims);

            if (status == Saml2StatusCodes.Success)
            {
                var descriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Audience = audience ?? Sp.EntityId,
                    Issuer = EntityId,
                    IssuedAt = now,
                    NotBefore = now.AddMinutes(-1),
                    Expires = now.AddMinutes(5)
                };
                var authn = new Saml2AuthenticationStatement(
                    new Saml2AuthenticationContext(new Uri(authnContext ?? "urn:oasis:names:tc:SAML:2.0:ac:classes:PasswordProtectedTransport")),
                    now);
                var confirmation = new Saml2SubjectConfirmation(
                    new Uri("urn:oasis:names:tc:SAML:2.0:cm:bearer"),
                    new Saml2SubjectConfirmationData
                    {
                        InResponseTo = new Saml2Id(inResponseTo),
                        Recipient = new Uri(recipient ?? Acs),
                        NotOnOrAfter = now.AddMinutes(5)
                    });
                response.CreateSecurityToken(descriptor, authn, confirmation);
            }

            var binding = new Saml2PostBinding();
            binding.Bind(response);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(binding.XmlDocument.OuterXml));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.RequestUri!.ToString() != MetadataUrl)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

            var descriptor = new EntityDescriptor(new Saml2Configuration { Issuer = EntityId }, signMetadata: false)
            {
                IdPSsoDescriptor = new IdPSsoDescriptor
                {
                    SigningCertificates = [new X509Certificate2(_certificate.Export(X509ContentType.Cert))],
                    SingleSignOnServices =
                    [
                        new SingleSignOnService { Binding = ProtocolBindings.HttpRedirect, Location = new Uri(SsoUrl) }
                    ]
                }
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(descriptor.ToXmlDocument().OuterXml, Encoding.UTF8, "application/samlmetadata+xml")
            });
        }
    }
}
