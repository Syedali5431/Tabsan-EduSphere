using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Saml2;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Tenancy;
using Tabsan.EduSphere.Infrastructure.Auth;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Plan M1 Stage M1.6 — full protocol round trips through the real API pipeline with the
/// <b>real</b> OidcClient / SamlClient talking to in-memory identity providers:
/// discovery, PKCE (the fake IdP verifies the S256 challenge), signed id_tokens and genuinely
/// signed SAML assertions, tenant-scoped settings and SP entity IDs.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class SsoProtocolIntegrationTests
{
    private const string TenantCode = "PROTO";
    private const string Callback = "https://portal.test/Login/SsoCallback";
    private const string Acs = "https://portal.test/Login/SamlAcs";
    private readonly EduSphereWebFactory _factory;

    public SsoProtocolIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Oidc_RealClient_WithPkce_SignsInLinkedUser()
    {
        var idp = new OidcIdp();
        var tenantId = await EnsureTenantAsync();
        await UpsertSettingAsync(tenantId, SsoProvider.GenericOidc, s =>
        {
            s.ConfigureOidc(OidcIdp.ClientId, idp.Authority);
            s.SetEncryptedClientSecret(_factory.Services.CreateScope().ServiceProvider.GetRequiredService<IEncryptionService>().Encrypt("proto-secret"));
        });
        var userId = await SeedLinkedUserAsync(tenantId, SsoProvider.GenericOidc, "oidc-subject-1");
        _factory.Services.GetRequiredService<SwitchableOidcClient>().Inner = new OidcClient(new HttpClient(idp));
        using var client = _factory.CreateClient();

        var start = await client.PostAsJsonAsync("api/v1/sso/start", new { provider = "GenericOidc", redirectUri = Callback, tenantCode = TenantCode });
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var startBody = await start.Content.ReadFromJsonAsync<JsonElement>();
        var authorize = new Uri(startBody.GetProperty("authorizationUrl").GetString()!);
        var query = QueryHelpers.ParseQuery(authorize.Query);
        Assert.Equal(idp.Authority + "/authorize", authorize.GetLeftPart(UriPartial.Path));
        Assert.Equal("S256", query["code_challenge_method"].ToString());

        // The "user" authenticates at the IdP, which issues a code bound to this nonce + challenge.
        idp.Prepare(subject: "oidc-subject-1", nonce: query["nonce"].ToString(), codeChallenge: query["code_challenge"].ToString());

        var complete = await client.PostAsJsonAsync("api/v1/sso/complete", new { state = query["state"].ToString(), code = OidcIdp.Code });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        Assert.Equal(userId, (await complete.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid());
        Assert.True(idp.VerifierMatched, "the API must send the PKCE verifier matching its challenge");
    }

    [Fact]
    public async Task Saml_RealClient_SignedAssertion_SignsInLinkedUser_AndForeignAudienceIsRejected()
    {
        var idp = new SamlIdp();
        var tenantId = await EnsureTenantAsync();
        await UpsertSettingAsync(tenantId, SsoProvider.Saml, s => s.ConfigureSaml(idp.MetadataUrl));
        var userId = await SeedLinkedUserAsync(tenantId, SsoProvider.Saml, "saml-subject-1");
        var realClient = new SamlClient(new HttpClient(idp));
        _factory.Services.GetRequiredService<SwitchableSamlClient>().Inner = realClient;
        using var client = _factory.CreateClient();

        // 1) Happy path.
        var (relayState, requestId, entityId) = await StartSamlAsync(client);
        Assert.Equal($"https://portal.test/Login/SamlMetadata?tenant={TenantCode}", entityId);
        var ok = await client.PostAsJsonAsync("api/v1/sso/saml/complete",
            new { samlResponse = idp.Respond("saml-subject-1", requestId, audience: entityId), relayState });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(userId, (await ok.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid());

        // 2) An assertion issued for another service provider (installation-wide entity ID) is refused.
        var (relayState2, requestId2, _) = await StartSamlAsync(client);
        var foreign = await client.PostAsJsonAsync("api/v1/sso/saml/complete",
            new { samlResponse = idp.Respond("saml-subject-1", requestId2, audience: "https://portal.test/Login/SamlMetadata"), relayState = relayState2 });
        Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
        Assert.Contains("TokenValidationFailed", await foreign.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Saml_SignedRequest_EncryptedAssertion_LinksByInstitutionId_AndChecksTheRole()
    {
        const string employeeNumber = "urn:oid:2.16.840.1.113730.3.1.3";
        const string affiliation = "urn:oid:1.3.6.1.4.1.5923.1.1.1.1";
        var idp = new SamlIdp();
        var tenantId = await EnsureTenantAsync();
        await UpsertSettingAsync(tenantId, SsoProvider.Saml, s =>
        {
            s.ConfigureSaml(idp.MetadataUrl);
            s.SetSamlSecurity(signAuthnRequests: true, requireEncryptedAssertions: true);
            s.SetAttributeMapping(employeeNumber, linkById: true, affiliation, "student=Student", requireRoleMatch: true);
        });
        var institutionId = "EMP-" + Guid.NewGuid().ToString("N")[..6];
        var userId = await SeedUserAsync(tenantId, institutionId, "Student");
        var spCertificate = _factory.Services.GetRequiredService<SamlServiceProviderCertificate>();
        _factory.Services.GetRequiredService<SwitchableSamlClient>().Inner = new SamlClient(new HttpClient(idp), spCertificate);
        using var client = _factory.CreateClient();

        // The SP metadata publishes the certificate and says requests are signed.
        var metadata = await client.GetStringAsync($"api/v1/sso/saml/metadata?acs={Uri.EscapeDataString(Acs)}&tenant={TenantCode}");
        Assert.Contains("AuthnRequestsSigned=\"true\"", metadata);
        Assert.Contains(Convert.ToBase64String(spCertificate.PublicCertificate.RawData), metadata);

        // 1) Signed request; encrypted assertion with the employee number links the account and signs in.
        var (relayState, requestId, entityId, redirect) = await StartSignedSamlAsync(client);
        Assert.Contains("Signature=", redirect);
        var attributes = new Dictionary<string, string> { [employeeNumber] = institutionId, [affiliation] = "student" };
        var ok = await client.PostAsJsonAsync("api/v1/sso/saml/complete", new
        {
            samlResponse = idp.Respond("enc-subject-" + institutionId, requestId, entityId, attributes, spCertificate.PublicCertificate),
            relayState
        });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(userId, (await ok.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid());

        // 2) The same assertion unencrypted is refused (this provider requires encryption).
        var (relayState2, requestId2, _, _) = await StartSignedSamlAsync(client);
        var plain = await client.PostAsJsonAsync("api/v1/sso/saml/complete",
            new { samlResponse = idp.Respond("enc-subject-" + institutionId, requestId2, entityId, attributes), relayState = relayState2 });
        Assert.Equal(HttpStatusCode.BadRequest, plain.StatusCode);
        Assert.Contains("TokenValidationFailed", await plain.Content.ReadAsStringAsync());

        // 3) The linked account asserting another affiliation is refused.
        var (relayState3, requestId3, _, _) = await StartSignedSamlAsync(client);
        var wrongRole = await client.PostAsJsonAsync("api/v1/sso/saml/complete", new
        {
            samlResponse = idp.Respond("enc-subject-" + institutionId, requestId3, entityId,
                new Dictionary<string, string> { [employeeNumber] = institutionId, [affiliation] = "faculty" }, spCertificate.PublicCertificate),
            relayState = relayState3
        });
        Assert.Equal(HttpStatusCode.Forbidden, wrongRole.StatusCode);
        Assert.Contains("RoleNotAllowed", await wrongRole.Content.ReadAsStringAsync());
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static async Task<(string RelayState, string RequestId, string EntityId, string Redirect)> StartSignedSamlAsync(HttpClient client)
    {
        var start = await client.PostAsJsonAsync("api/v1/sso/start", new { provider = "Saml", redirectUri = Acs, tenantCode = TenantCode });
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var body = await start.Content.ReadFromJsonAsync<JsonElement>();
        var redirect = body.GetProperty("authorizationUrl").GetString()!;
        var xml = Inflate(QueryHelpers.ParseQuery(new Uri(redirect).Query)["SAMLRequest"].ToString());
        var doc = System.Xml.Linq.XDocument.Parse(xml);
        return (body.GetProperty("state").GetString()!, doc.Root!.Attribute("ID")!.Value,
            doc.Root.Elements().First(e => e.Name.LocalName == "Issuer").Value, redirect);
    }

    private async Task<Guid> SeedUserAsync(Guid tenantId, string username, string roleName)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var role = await db.Roles.SingleAsync(r => r.Name == roleName);
        var campus = await db.Campuses.FirstOrDefaultAsync(c => c.TenantId == tenantId);
        if (campus is null)
        {
            campus = new Campus(tenantId, "PROTO-MAIN", "Protocol Test Campus");
            db.Campuses.Add(campus);
        }
        var user = new User(username, "integration-hash", role.Id, email: $"{username.ToLowerInvariant()}@proto.test",
            tenantId: tenantId, campusId: campus.Id);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private static async Task<(string RelayState, string RequestId, string EntityId)> StartSamlAsync(HttpClient client)
    {
        var start = await client.PostAsJsonAsync("api/v1/sso/start", new { provider = "Saml", redirectUri = Acs, tenantCode = TenantCode });
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var body = await start.Content.ReadFromJsonAsync<JsonElement>();
        var redirect = new Uri(body.GetProperty("authorizationUrl").GetString()!);
        var xml = Inflate(QueryHelpers.ParseQuery(redirect.Query)["SAMLRequest"].ToString());
        var doc = System.Xml.Linq.XDocument.Parse(xml);
        var id = doc.Root!.Attribute("ID")!.Value;
        var issuer = doc.Root.Elements().First(e => e.Name.LocalName == "Issuer").Value;
        return (body.GetProperty("state").GetString()!, id, issuer);
    }

    private static string Inflate(string base64)
    {
        using var input = new MemoryStream(Convert.FromBase64String(base64));
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(deflate, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private async Task<Guid> EnsureTenantAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Code == TenantCode);
        if (tenant is not null)
            return tenant.Id;

        tenant = new Tenant(TenantCode, "Protocol Test University");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant.Id;
    }

    private async Task UpsertSettingAsync(Guid tenantId, SsoProvider provider, Action<SsoProviderSetting> configure)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var setting = await db.SsoProviderSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Provider == provider);
        if (setting is null)
        {
            setting = new SsoProviderSetting(tenantId, provider, provider + " (protocol test)");
            db.SsoProviderSettings.Add(setting);
        }
        setting.SetSamlSecurity(false, false);
        setting.SetAttributeMapping(null, false, null, null, false);
        configure(setting);
        setting.SetPolicy(false, false, false, null);
        setting.Enable();
        await db.SaveChangesAsync();
    }

    private async Task<Guid> SeedLinkedUserAsync(Guid tenantId, SsoProvider provider, string subject)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var existing = await db.ExternalLogins.FirstOrDefaultAsync(l => l.TenantId == tenantId && l.Provider == provider && l.ExternalSubjectId == subject);
        if (existing is not null)
            return existing.UserId;

        var role = await db.Roles.SingleAsync(r => r.Name == "Student");
        var username = $"proto_{Guid.NewGuid().ToString("N")[..8]}";
        var user = new User(username, "integration-hash", role.Id, email: $"{username}@proto.test");
        db.Users.Add(user);
        db.ExternalLogins.Add(new ExternalLogin(tenantId, user.Id, provider, subject, user.Email));
        await db.SaveChangesAsync();
        return user.Id;
    }

    /// <summary>In-memory OpenID provider that checks PKCE before issuing a signed id_token.</summary>
    private sealed class OidcIdp : HttpMessageHandler
    {
        public const string ClientId = "proto-client";
        public const string Code = "proto-auth-code";
        public readonly string Authority = $"https://oidc-{Guid.NewGuid():N}.test";
        private readonly RSA _rsa = RSA.Create(2048);
        private string? _subject, _nonce, _challenge;
        public bool VerifierMatched { get; private set; }

        public void Prepare(string subject, string nonce, string codeChallenge)
            => (_subject, _nonce, _challenge) = (subject, nonce, codeChallenge);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/.well-known/openid-configuration"))
                return Json(new
                {
                    issuer = Authority,
                    authorization_endpoint = Authority + "/authorize",
                    token_endpoint = Authority + "/token",
                    jwks_uri = Authority + "/jwks",
                    response_types_supported = new[] { "code" },
                    subject_types_supported = new[] { "public" },
                    id_token_signing_alg_values_supported = new[] { "RS256" }
                });

            if (path.EndsWith("/jwks"))
            {
                var p = _rsa.ExportParameters(false);
                return Json(new { keys = new[] { new { kty = "RSA", use = "sig", alg = "RS256", kid = "k1", n = B64(p.Modulus!), e = B64(p.Exponent!) } } });
            }

            if (path.EndsWith("/token"))
            {
                var form = QueryHelpers.ParseQuery(await request.Content!.ReadAsStringAsync(ct));
                var verifier = form["code_verifier"].ToString();
                VerifierMatched = B64(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))) == _challenge;
                if (!VerifierMatched || form["code"] != Code || form["client_id"] != ClientId)
                    return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("{\"error\":\"invalid_grant\"}") };

                var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
                {
                    Issuer = Authority,
                    Audience = ClientId,
                    Claims = new Dictionary<string, object> { ["sub"] = _subject!, ["nonce"] = _nonce! },
                    Expires = DateTime.UtcNow.AddMinutes(5),
                    SigningCredentials = new SigningCredentials(new RsaSecurityKey(_rsa) { KeyId = "k1" }, SecurityAlgorithms.RsaSha256)
                });
                return Json(new { access_token = "at", token_type = "Bearer", id_token = token });
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static string B64(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static HttpResponseMessage Json(object body) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
    }

    /// <summary>In-memory SAML IdP: metadata over "https" and signed responses.</summary>
    private sealed class SamlIdp : HttpMessageHandler
    {
        private readonly string _host = $"https://saml-{Guid.NewGuid():N}.test";
        private readonly X509Certificate2 _cert;

        public SamlIdp()
        {
            using var rsa = RSA.Create(2048);
            using var raw = new CertificateRequest("CN=Proto SAML IdP", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
                .CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
            _cert = new X509Certificate2(raw.Export(X509ContentType.Pfx), (string?)null, X509KeyStorageFlags.Exportable);
        }

        private string EntityId => _host + "/entity";
        public string MetadataUrl => _host + "/metadata";

        public string Respond(string subject, string inResponseTo, string audience,
            Dictionary<string, string>? attributes = null, X509Certificate2? encryptFor = null)
        {
            var now = DateTime.UtcNow;
            var config = new Saml2Configuration
            {
                Issuer = EntityId, SigningCertificate = _cert, AuthnResponseSignType = Saml2AuthnResponseSignTypes.SignAssertionAndResponse,
                EncryptionCertificate = encryptFor
            };
            var claims = new List<Claim> { new("urn:oid:0.9.2342.19200300.100.1.1", subject) };
            claims.AddRange((attributes ?? new()).Select(a => new Claim(a.Key, a.Value)));
            var response = new Saml2AuthnResponse(config)
            {
                InResponseTo = new Saml2Id(inResponseTo),
                Status = Saml2StatusCodes.Success,
                Destination = new Uri(Acs),
                NameId = new Saml2NameIdentifier(subject, new Uri("urn:oasis:names:tc:SAML:2.0:nameid-format:persistent")),
                ClaimsIdentity = new ClaimsIdentity(claims)
            };
            response.CreateSecurityToken(
                new SecurityTokenDescriptor { Subject = new ClaimsIdentity(claims), Audience = audience, Issuer = EntityId, IssuedAt = now, NotBefore = now.AddMinutes(-1), Expires = now.AddMinutes(5) },
                new Saml2AuthenticationStatement(new Saml2AuthenticationContext(new Uri("urn:oasis:names:tc:SAML:2.0:ac:classes:PasswordProtectedTransport")), now),
                new Saml2SubjectConfirmation(new Uri("urn:oasis:names:tc:SAML:2.0:cm:bearer"),
                    new Saml2SubjectConfirmationData { InResponseTo = new Saml2Id(inResponseTo), Recipient = new Uri(Acs), NotOnOrAfter = now.AddMinutes(5) }));

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
                    SigningCertificates = [new X509Certificate2(_cert.Export(X509ContentType.Cert))],
                    SingleSignOnServices = [new SingleSignOnService { Binding = ProtocolBindings.HttpRedirect, Location = new Uri(_host + "/sso") }]
                }
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(descriptor.ToXmlDocument().OuterXml, Encoding.UTF8, "application/samlmetadata+xml")
            });
        }
    }
}
