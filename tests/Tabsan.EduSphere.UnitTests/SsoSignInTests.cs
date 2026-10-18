using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Tabsan.EduSphere.Application.Auth;
using Tabsan.EduSphere.Application.DTOs.Auth;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Auth;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>Plan M1 Stage M1.2: OIDC client against a fake IdP, and SSO sign-in orchestration rules.</summary>
public class SsoSignInTests
{
    private const string Callback = "https://portal.uni.edu/Login/SsoCallback";

    // ═════════════════════════════════════════════════════════════════════════
    // OidcClient — real protocol handling against an in-memory identity provider
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task OidcClient_BuildsPkceAuthorizationUrl()
    {
        var idp = new FakeIdp();
        var client = new OidcClient(new HttpClient(idp));

        var url = await client.BuildAuthorizationUrlAsync(idp.Config, Callback, "st", "no", "chal");

        var query = QueryHelpers.ParseQuery(new Uri(url).Query);
        url.Should().StartWith(idp.Authority + "/authorize");
        query["client_id"].ToString().Should().Be(FakeIdp.ClientId);
        query["response_type"].ToString().Should().Be("code");
        query["redirect_uri"].ToString().Should().Be(Callback);
        query["state"].ToString().Should().Be("st");
        query["nonce"].ToString().Should().Be("no");
        query["code_challenge"].ToString().Should().Be("chal");
        query["code_challenge_method"].ToString().Should().Be("S256");
    }

    [Fact]
    public async Task OidcClient_RedeemsCodeAndValidatesIdToken()
    {
        var idp = new FakeIdp();
        idp.IdToken = idp.Sign(nonce: "n-1", extra: new Dictionary<string, object>
        {
            ["oid"] = "object-id-7",
            ["email"] = "Ali@Uni.edu",
            ["email_verified"] = true,
            ["name"] = "Ali Khan",
            ["amr"] = new[] { "pwd", "mfa" }
        });
        var client = new OidcClient(new HttpClient(idp));

        var identity = await client.RedeemCodeAsync(idp.Config, "the-code", Callback, "verifier-1", "n-1");

        identity.Subject.Should().Be("object-id-7", "Entra 'oid' is preferred over pairwise 'sub'");
        identity.Email.Should().Be("Ali@Uni.edu");
        identity.EmailVerified.Should().BeTrue();
        identity.MfaAsserted.Should().BeTrue();
        idp.LastTokenRequest.Should().NotBeNull();
        idp.LastTokenRequest!["code"].Should().Be("the-code");
        idp.LastTokenRequest["code_verifier"].Should().Be("verifier-1");
        idp.LastTokenRequest["redirect_uri"].Should().Be(Callback);
        idp.LastTokenRequest["grant_type"].Should().Be("authorization_code");
    }

    [Fact]
    public async Task OidcClient_RejectsNonceMismatch()
    {
        var idp = new FakeIdp();
        idp.IdToken = idp.Sign(nonce: "other");
        var client = new OidcClient(new HttpClient(idp));

        var act = () => client.RedeemCodeAsync(idp.Config, "c", Callback, "v", "expected");

        await act.Should().ThrowAsync<SsoProtocolException>().WithMessage("*nonce*");
    }

    [Fact]
    public async Task OidcClient_RejectsWrongAudience()
    {
        var idp = new FakeIdp();
        idp.IdToken = idp.Sign(nonce: "n", audience: "someone-else");
        var client = new OidcClient(new HttpClient(idp));

        var act = () => client.RedeemCodeAsync(idp.Config, "c", Callback, "v", "n");

        await act.Should().ThrowAsync<SsoProtocolException>();
    }

    [Fact]
    public async Task OidcClient_RejectsTokenSignedByUnknownKey()
    {
        var idp = new FakeIdp();
        using var attackerKey = RSA.Create(2048);
        idp.IdToken = idp.Sign(nonce: "n", signingKey: new RsaSecurityKey(attackerKey) { KeyId = idp.KeyId });
        var client = new OidcClient(new HttpClient(idp));

        var act = () => client.RedeemCodeAsync(idp.Config, "c", Callback, "v", "n");

        await act.Should().ThrowAsync<SsoProtocolException>();
    }

    [Fact]
    public async Task OidcClient_RejectsExpiredToken()
    {
        var idp = new FakeIdp();
        idp.IdToken = idp.Sign(nonce: "n", expires: DateTime.UtcNow.AddMinutes(-10));
        var client = new OidcClient(new HttpClient(idp));

        var act = () => client.RedeemCodeAsync(idp.Config, "c", Callback, "v", "n");

        await act.Should().ThrowAsync<SsoProtocolException>();
    }

    [Fact]
    public async Task OidcClient_SurfacesTokenEndpointErrors()
    {
        var idp = new FakeIdp { TokenStatus = HttpStatusCode.BadRequest };
        var client = new OidcClient(new HttpClient(idp));

        var act = () => client.RedeemCodeAsync(idp.Config, "c", Callback, "v", "n");

        await act.Should().ThrowAsync<SsoProtocolException>().WithMessage("*HTTP 400*");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SsoSignInService — state, redirect, linking and MFA rules
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Providers_ListsOnlyEnabledCompleteOidcProviders()
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.Microsoft, enabled: true);
        h.AddSetting(SsoProvider.Google, enabled: false);

        var providers = await h.Service.GetProvidersAsync(null);

        providers.Select(p => p.Provider).Should().Equal("Microsoft");
    }

    [Theory]
    [InlineData("https://evil.example.com/steal")]
    [InlineData("http://portal.uni.edu/Login/SsoCallback")]          // plain http off-loopback
    [InlineData("https://portal.uni.edu/Login/SsoCallback?x=1")]     // query not allowed
    [InlineData("/Login/SsoCallback")]                               // relative
    public async Task Start_RejectsDisallowedRedirectUris(string redirectUri)
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.Microsoft, enabled: true);

        var (response, failure) = await h.Service.StartAsync(new SsoStartRequest("Microsoft", redirectUri));

        response.Should().BeNull();
        failure.Should().Be(SsoFailureReason.InvalidRedirectUri);
    }

    [Fact]
    public async Task Start_AllowsLoopbackHttpForDevelopment()
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.Microsoft, enabled: true);

        var (response, _) = await h.Service.StartAsync(new SsoStartRequest("microsoft", "http://localhost:5063/Login/SsoCallback"));

        response.Should().NotBeNull();
    }

    [Fact]
    public async Task Start_RefusesDisabledOrUnknownProvider()
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.Google, enabled: false);

        (await h.Service.StartAsync(new SsoStartRequest("Google", Callback))).Failure.Should().Be(SsoFailureReason.ProviderUnavailable);
        (await h.Service.StartAsync(new SsoStartRequest("Saml", Acs))).Failure.Should().Be(SsoFailureReason.ProviderUnavailable);
        (await h.Service.StartAsync(new SsoStartRequest("Nope", Callback))).Failure.Should().Be(SsoFailureReason.ProviderUnavailable);
        (await h.Service.StartAsync(new SsoStartRequest("99", Callback))).Failure.Should().Be(SsoFailureReason.ProviderUnavailable);
    }

    [Fact]
    public async Task Start_SendsS256ChallengeOfStoredVerifier()
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.Microsoft, enabled: true);
        var user = h.AddUser("ali@uni.edu");
        h.Link(SsoProvider.Microsoft, "sub-1", user);

        var (start, _) = await h.Service.StartAsync(new SsoStartRequest("Microsoft", Callback));
        await h.Service.CompleteAsync(new SsoCompleteRequest(start!.State, "code"), "1.1.1.1");

        var expected = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(h.Oidc.LastVerifier!)));
        h.Oidc.LastChallenge.Should().Be(expected);
        h.Oidc.LastNonce.Should().Be(h.Oidc.LastExpectedNonce);
    }

    [Fact]
    public async Task Complete_WithExistingLink_SignsIn()
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.Microsoft, enabled: true);
        var user = h.AddUser("ali@uni.edu");
        h.Link(SsoProvider.Microsoft, "sub-1", user);

        var result = await h.RoundTripAsync(SsoProvider.Microsoft, new ExternalIdentity("sub-1", "ali@uni.edu", true, "Ali", false));

        result.IsSuccess.Should().BeTrue();
        h.Auth.Calls.Should().ContainSingle().Which.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task Complete_StateIsSingleUse()
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.Microsoft, enabled: true);
        var user = h.AddUser("ali@uni.edu");
        h.Link(SsoProvider.Microsoft, "sub-1", user);
        h.Oidc.Identity = new ExternalIdentity("sub-1", null, false, null, false);

        var (start, _) = await h.Service.StartAsync(new SsoStartRequest("Microsoft", Callback));
        (await h.Service.CompleteAsync(new SsoCompleteRequest(start!.State, "c"), null)).IsSuccess.Should().BeTrue();

        var replay = await h.Service.CompleteAsync(new SsoCompleteRequest(start.State, "c"), null);

        replay.FailureReason.Should().Be(SsoFailureReason.InvalidState);
    }

    [Fact]
    public async Task Complete_UnknownState_Fails()
    {
        var h = new Harness();

        var result = await h.Service.CompleteAsync(new SsoCompleteRequest("forged", "c"), null);

        result.FailureReason.Should().Be(SsoFailureReason.InvalidState);
    }

    [Fact]
    public async Task Complete_TokenValidationFailure_Fails()
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.Microsoft, enabled: true);
        h.Oidc.Throw = true;

        var result = await h.RoundTripAsync(SsoProvider.Microsoft, identity: null);

        result.FailureReason.Should().Be(SsoFailureReason.TokenValidationFailed);
        h.Audit.Entries.Should().Contain(a => a.Action == "SsoLoginFailed");
    }

    [Fact]
    public async Task Complete_WithoutLinkAndAutoLinkOff_IsRefused()
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.Google, enabled: true);
        h.AddUser("ali@uni.edu");

        var result = await h.RoundTripAsync(SsoProvider.Google, new ExternalIdentity("g-1", "ali@uni.edu", true, null, false));

        result.FailureReason.Should().Be(SsoFailureReason.AccountNotLinked);
        h.Repo.Links.Should().BeEmpty();
    }

    [Fact]
    public async Task Complete_AutoLinksByVerifiedEmail()
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.Google, enabled: true, autoLink: true);
        var user = h.AddUser("ali@uni.edu");

        var result = await h.RoundTripAsync(SsoProvider.Google, new ExternalIdentity("g-1", "ali@uni.edu", true, null, false));

        result.IsSuccess.Should().BeTrue();
        h.Repo.Links.Should().ContainSingle(l => l.UserId == user.Id && l.ExternalSubjectId == "g-1");
        h.Audit.Entries.Should().Contain(a => a.Action == "SsoAccountLinked");
    }

    [Fact]
    public async Task Complete_DoesNotAutoLinkUnverifiedEmail()
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.GenericOidc, enabled: true, autoLink: true);
        h.AddUser("ali@uni.edu");

        var result = await h.RoundTripAsync(SsoProvider.GenericOidc, new ExternalIdentity("x-1", "ali@uni.edu", false, null, false));

        result.FailureReason.Should().Be(SsoFailureReason.AccountNotLinked);
    }

    [Fact]
    public async Task Complete_DoesNotAutoLinkAcrossTenants()
    {
        var h = new Harness(tenantId: Guid.NewGuid());
        h.AddSetting(SsoProvider.Google, enabled: true, autoLink: true);
        h.AddUser("ali@uni.edu", tenantId: Guid.NewGuid());

        var result = await h.RoundTripAsync(SsoProvider.Google, new ExternalIdentity("g-1", "ali@uni.edu", true, null, false));

        result.FailureReason.Should().Be(SsoFailureReason.AccountNotLinked);
    }

    [Fact]
    public async Task Complete_NeverRelinksUserAlreadyLinkedToAnotherIdentity()
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.Google, enabled: true, autoLink: true);
        var user = h.AddUser("ali@uni.edu");
        h.Link(SsoProvider.Google, "original-subject", user);

        var result = await h.RoundTripAsync(SsoProvider.Google, new ExternalIdentity("attacker-subject", "ali@uni.edu", true, null, false));

        result.FailureReason.Should().Be(SsoFailureReason.AccountNotLinked);
        h.Repo.Links.Should().ContainSingle();
    }

    [Fact]
    public async Task Complete_EnforcesEmailDomainAllowList()
    {
        var h = new Harness();
        var setting = h.AddSetting(SsoProvider.Google, enabled: true);
        setting.SetPolicy(false, false, false, ["uni.edu"]);
        var user = h.AddUser("ali@gmail.com");
        h.Link(SsoProvider.Google, "g-1", user);

        var result = await h.RoundTripAsync(SsoProvider.Google, new ExternalIdentity("g-1", "ali@gmail.com", true, null, false));

        result.FailureReason.Should().Be(SsoFailureReason.EmailDomainNotAllowed);
        h.Auth.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Complete_TrustsIdpMfaOnlyWhenPolicyAllows()
    {
        var h = new Harness();
        var setting = h.AddSetting(SsoProvider.Microsoft, enabled: true);
        var user = h.AddUser("ali@uni.edu");
        h.Link(SsoProvider.Microsoft, "sub-1", user);

        await h.RoundTripAsync(SsoProvider.Microsoft, new ExternalIdentity("sub-1", null, false, null, MfaAsserted: true));
        h.Auth.Calls.Last().MfaSatisfied.Should().BeFalse("TrustIdpMfa is off");

        setting.SetPolicy(false, false, trustIdpMfa: true, null);
        await h.RoundTripAsync(SsoProvider.Microsoft, new ExternalIdentity("sub-1", null, false, null, MfaAsserted: true));
        h.Auth.Calls.Last().MfaSatisfied.Should().BeTrue();
    }

    [Fact]
    public async Task Complete_WhenTotpRequired_IssuesTicketAndCompletesWithCode()
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.Microsoft, enabled: true);
        var user = h.AddUser("ali@uni.edu");
        h.Link(SsoProvider.Microsoft, "sub-1", user);
        h.Auth.RequireTotp = "123456";

        var first = await h.RoundTripAsync(SsoProvider.Microsoft, new ExternalIdentity("sub-1", null, false, null, false));
        first.FailureReason.Should().Be(SsoFailureReason.MfaCodeRequired);
        first.MfaTicket.Should().NotBeNullOrWhiteSpace();

        var wrong = await h.Service.CompleteMfaAsync(new SsoMfaRequest(first.MfaTicket!, "000000"), null);
        wrong.FailureReason.Should().Be(SsoFailureReason.InvalidMfaCode);
        wrong.MfaTicket.Should().NotBe(first.MfaTicket, "a fresh ticket is issued after each failure");

        (await h.Service.CompleteMfaAsync(new SsoMfaRequest(first.MfaTicket!, "123456"), null))
            .FailureReason.Should().Be(SsoFailureReason.InvalidState, "the old ticket is single-use");

        var ok = await h.Service.CompleteMfaAsync(new SsoMfaRequest(wrong.MfaTicket!, "123456"), null);
        ok.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteMfa_LocksTicketAfterRepeatedFailures()
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.Microsoft, enabled: true);
        var user = h.AddUser("ali@uni.edu");
        h.Link(SsoProvider.Microsoft, "sub-1", user);
        h.Auth.RequireTotp = "123456";

        var result = await h.RoundTripAsync(SsoProvider.Microsoft, new ExternalIdentity("sub-1", null, false, null, false));
        for (var i = 0; i < 10 && result.MfaTicket is not null; i++)
            result = await h.Service.CompleteMfaAsync(new SsoMfaRequest(result.MfaTicket, "000000"), null);

        result.FailureReason.Should().Be(SsoFailureReason.InvalidState);
        h.Auth.Calls.Count(c => c.MfaCode == "000000").Should().Be(5);
    }

    [Fact]
    public async Task Complete_PropagatesLoginGateRejection()
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.Microsoft, enabled: true);
        var user = h.AddUser("ali@uni.edu");
        h.Link(SsoProvider.Microsoft, "sub-1", user);
        h.Auth.FailWith = LoginFailureReason.ConcurrencyLimitReached;

        var result = await h.RoundTripAsync(SsoProvider.Microsoft, new ExternalIdentity("sub-1", null, false, null, false));

        result.FailureReason.Should().Be(SsoFailureReason.LoginRejected);
        result.LoginFailure.Should().Be(LoginFailureReason.ConcurrencyLimitReached);
    }

    [Fact]
    public void MicrosoftSetting_RejectsMultiTenantAuthorities()
    {
        var setting = new SsoProviderSetting(null, SsoProvider.Microsoft, "Microsoft");

        foreach (var bad in new[] { "https://login.microsoftonline.com/common/v2.0", "https://login.microsoftonline.com/organizations/v2.0", "https://login.microsoftonline.com/consumers/v2.0", "https://login.microsoftonline.com/" })
            setting.Invoking(s => s.ConfigureOidc("c", bad)).Should().Throw<ArgumentException>(bad);

        setting.ConfigureOidc("c", "https://login.microsoftonline.com/11111111-2222-3333-4444-555555555555/v2.0");
        setting.Authority.Should().Contain("11111111");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SsoSignInService — SAML 2.0 (Stage M1.3)
    // ═════════════════════════════════════════════════════════════════════════

    private const string Acs = "https://portal.uni.edu/Login/SamlAcs";

    [Fact]
    public async Task Saml_ProvidersIncludeCompleteSamlSetting()
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.Saml, enabled: true);

        (await h.Service.GetProvidersAsync(null)).Select(p => p.Provider).Should().Equal("Saml");
    }

    [Fact]
    public async Task Saml_StartRequiresAcsPath_AndOidcRejectsAcsPath()
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.Saml, enabled: true);
        h.AddSetting(SsoProvider.Google, enabled: true);

        (await h.Service.StartAsync(new SsoStartRequest("Saml", Callback))).Failure.Should().Be(SsoFailureReason.InvalidRedirectUri);
        (await h.Service.StartAsync(new SsoStartRequest("Google", Acs))).Failure.Should().Be(SsoFailureReason.InvalidRedirectUri);
        (await h.Service.StartAsync(new SsoStartRequest("saml", Acs))).Response.Should().NotBeNull();
    }

    [Fact]
    public async Task Saml_EntityIdIsDerivedFromAcsAndTenant()
    {
        var h = new Harness(tenantId: Guid.NewGuid());
        h.AddSetting(SsoProvider.Saml, enabled: true);

        var (start, _) = await h.Service.StartAsync(new SsoStartRequest("Saml", Acs, "UNI"));

        start.Should().NotBeNull();
        start!.AuthorizationUrl.Should().StartWith("https://idp.uni.edu/sso");
        h.Saml.LastSp.Should().Be(new SamlServiceProvider("https://portal.uni.edu/Login/SamlMetadata?tenant=UNI", Acs));
        h.Saml.LastRelayState.Should().Be(start.State);
    }

    [Fact]
    public async Task Saml_RoundTrip_ValidatesAgainstStoredRequest_AndAutoLinks()
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.Saml, enabled: true, autoLink: true);
        var user = h.AddUser("ali@uni.edu");
        h.Saml.Identity = new ExternalIdentity("nameid-1", "ali@uni.edu", true, "Ali", false);

        var (start, _) = await h.Service.StartAsync(new SsoStartRequest("Saml", Acs));
        var result = await h.Service.CompleteSamlAsync(new SsoSamlCompleteRequest("PHNhbWw+", start!.State), "10.0.0.1");

        result.IsSuccess.Should().BeTrue();
        h.Saml.LastExpectedRequestId.Should().Be(FakeSaml.RequestId);
        h.Saml.LastValidatedSp!.AcsUrl.Should().Be(Acs);
        h.Repo.Links.Should().ContainSingle(l => l.UserId == user.Id && l.Provider == SsoProvider.Saml && l.ExternalSubjectId == "nameid-1");
    }

    [Fact]
    public async Task Saml_InvalidAssertion_Fails()
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.Saml, enabled: true);

        var (start, _) = await h.Service.StartAsync(new SsoStartRequest("Saml", Acs));
        var result = await h.Service.CompleteSamlAsync(new SsoSamlCompleteRequest("PHNhbWw+", start!.State), null);

        result.FailureReason.Should().Be(SsoFailureReason.TokenValidationFailed);
    }

    [Fact]
    public async Task StateCannotCrossProtocols()
    {
        var h = new Harness();
        h.AddSetting(SsoProvider.Saml, enabled: true);
        h.AddSetting(SsoProvider.Google, enabled: true);

        var (oidc, _) = await h.Service.StartAsync(new SsoStartRequest("Google", Callback));
        (await h.Service.CompleteSamlAsync(new SsoSamlCompleteRequest("x", oidc!.State), null))
            .FailureReason.Should().Be(SsoFailureReason.InvalidState);

        var (saml, _) = await h.Service.StartAsync(new SsoStartRequest("Saml", Acs));
        (await h.Service.CompleteAsync(new SsoCompleteRequest(saml!.State, "code"), null))
            .FailureReason.Should().Be(SsoFailureReason.InvalidState);
    }

    [Fact]
    public async Task SamlMetadata_OnlyForAllowedAcs()
    {
        var h = new Harness();

        (await h.Service.GetSamlMetadataAsync("https://evil.example.com/steal", null)).Should().BeNull();
        (await h.Service.GetSamlMetadataAsync(Callback, null)).Should().BeNull();
        (await h.Service.GetSamlMetadataAsync(Acs, null)).Should().Be("<md for https://portal.uni.edu/Login/SamlMetadata>");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Test doubles
    // ═════════════════════════════════════════════════════════════════════════

    private static string Base64Url(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>In-memory OpenID provider: discovery, JWKS and token endpoint.</summary>
    private sealed class FakeIdp : HttpMessageHandler
    {
        public const string ClientId = "client-abc";
        public readonly string Authority = $"https://idp-{Guid.NewGuid():N}.test";
        public readonly string KeyId = "key-1";
        private readonly RSA _rsa = RSA.Create(2048);

        public string? IdToken { get; set; }
        public HttpStatusCode TokenStatus { get; set; } = HttpStatusCode.OK;
        public Dictionary<string, string>? LastTokenRequest { get; private set; }

        public OidcProviderConfig Config => new(Authority, ClientId, "secret");

        public string Sign(string nonce, string? audience = null, DateTime? expires = null,
            SecurityKey? signingKey = null, Dictionary<string, object>? extra = null)
        {
            var claims = new Dictionary<string, object> { ["sub"] = "pairwise-sub", ["nonce"] = nonce };
            foreach (var kv in extra ?? []) claims[kv.Key] = kv.Value;

            var exp = expires ?? DateTime.UtcNow.AddMinutes(5);
            return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = Authority,
                Audience = audience ?? ClientId,
                Claims = claims,
                NotBefore = exp.AddMinutes(-10),
                IssuedAt = exp.AddMinutes(-10),
                Expires = exp,
                SigningCredentials = new SigningCredentials(
                    signingKey ?? new RsaSecurityKey(_rsa) { KeyId = KeyId }, SecurityAlgorithms.RsaSha256)
            });
        }

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
                return Json(new { keys = new[] { new { kty = "RSA", use = "sig", alg = "RS256", kid = KeyId, n = Base64Url(p.Modulus!), e = Base64Url(p.Exponent!) } } });
            }

            if (path.EndsWith("/token"))
            {
                var form = QueryHelpers.ParseQuery(await request.Content!.ReadAsStringAsync(ct));
                LastTokenRequest = form.ToDictionary(k => k.Key, v => v.Value.ToString());
                return TokenStatus == HttpStatusCode.OK
                    ? Json(new { access_token = "at", token_type = "Bearer", id_token = IdToken })
                    : new HttpResponseMessage(TokenStatus) { Content = new StringContent("{\"error\":\"invalid_grant\"}") };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage Json(object body) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
    }

    private sealed class Harness
    {
        public readonly Guid? TenantId;
        public readonly FakeSsoRepo Repo = new();
        public readonly FakeOidc Oidc = new();
        public readonly FakeSaml Saml = new();
        public readonly FakeAuth Auth = new();
        public readonly FakeAudit Audit = new();
        private readonly List<User> _users = new();
        public readonly SsoSignInService Service;

        public Harness(Guid? tenantId = null)
        {
            TenantId = tenantId;
            Repo.TenantForCodeUni = tenantId;
            var users = Proxy<IUserRepository>.Create(new()
            {
                ["GetByEmailAsync"] = a => Task.FromResult(_users.FirstOrDefault(u => string.Equals(u.Email, (string)a[0]!, StringComparison.OrdinalIgnoreCase)))
            });
            Service = new SsoSignInService(Repo, Oidc, Saml, new PlainEncryption(), users, Auth, Audit.Service,
                new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
                Options.Create(new AuthSecurityOptions()));
        }

        public SsoProviderSetting AddSetting(SsoProvider provider, bool enabled, bool autoLink = false)
        {
            var s = new SsoProviderSetting(TenantId, provider, provider + " login");
            if (provider == SsoProvider.Saml)
            {
                s.ConfigureSaml("https://idp.uni.edu/metadata.xml");
            }
            else
            {
                s.ConfigureOidc("client", provider == SsoProvider.Microsoft
                    ? "https://login.microsoftonline.com/contoso.edu/v2.0"
                    : "https://idp.uni.edu");
                s.SetEncryptedClientSecret("enc:secret");
            }
            s.SetPolicy(autoLink, false, false, null);
            if (enabled) s.Enable();
            Repo.Settings.Add(s);
            return s;
        }

        public User AddUser(string email, Guid? tenantId = null)
        {
            tenantId ??= TenantId;
            var user = new User("u" + _users.Count, "hash", 1, email,
                tenantId: tenantId, campusId: tenantId is null ? null : Guid.NewGuid());
            _users.Add(user);
            return user;
        }

        public void Link(SsoProvider provider, string subject, User user)
            => Repo.Links.Add(new ExternalLogin(TenantId, user.Id, provider, subject, user.Email));

        public async Task<SsoResult> RoundTripAsync(SsoProvider provider, ExternalIdentity? identity)
        {
            Oidc.Identity = identity;
            var (start, failure) = await Service.StartAsync(new SsoStartRequest(provider.ToString(), Callback, TenantId is null ? null : "UNI"));
            failure.Should().BeNull();
            return await Service.CompleteAsync(new SsoCompleteRequest(start!.State, "code"), "10.0.0.1");
        }
    }

    private sealed class FakeOidc : IOidcClient
    {
        public ExternalIdentity? Identity { get; set; }
        public bool Throw { get; set; }
        public string? LastChallenge, LastNonce, LastVerifier, LastExpectedNonce;

        public Task<string> BuildAuthorizationUrlAsync(OidcProviderConfig provider, string redirectUri, string state, string nonce, string codeChallenge, CancellationToken ct = default)
        {
            provider.ClientSecret.Should().Be("secret", "the stored secret is decrypted before use");
            LastChallenge = codeChallenge;
            LastNonce = nonce;
            return Task.FromResult("https://idp.test/authorize?state=" + state);
        }

        public Task<ExternalIdentity> RedeemCodeAsync(OidcProviderConfig provider, string code, string redirectUri, string codeVerifier, string expectedNonce, CancellationToken ct = default)
        {
            LastVerifier = codeVerifier;
            LastExpectedNonce = expectedNonce;
            redirectUri.Should().Be(Callback);
            if (Throw || Identity is null) throw new SsoProtocolException("bad token");
            return Task.FromResult(Identity);
        }
    }

    private sealed class FakeSaml : ISamlClient
    {
        public const string RequestId = "_req-1";
        public ExternalIdentity? Identity { get; set; }
        public SamlServiceProvider? LastSp, LastValidatedSp;
        public string? LastRelayState, LastExpectedRequestId;

        public Task<SamlAuthnRequest> BuildAuthnRequestAsync(string idpMetadataUrl, SamlServiceProvider sp, string relayState, CancellationToken ct = default)
        {
            LastSp = sp;
            LastRelayState = relayState;
            return Task.FromResult(new SamlAuthnRequest("https://idp.uni.edu/sso?SAMLRequest=x&RelayState=" + relayState, RequestId));
        }

        public Task<ExternalIdentity> ValidateResponseAsync(string idpMetadataUrl, SamlServiceProvider sp, string samlResponse, string expectedRequestId, CancellationToken ct = default)
        {
            LastValidatedSp = sp;
            LastExpectedRequestId = expectedRequestId;
            return Identity is null ? throw new SsoProtocolException("bad assertion") : Task.FromResult(Identity);
        }

        public string BuildServiceProviderMetadata(SamlServiceProvider sp) => $"<md for {sp.EntityId}>";
    }
    private sealed class FakeSsoRepo : ISsoRepository
    {
        public readonly List<SsoProviderSetting> Settings = new();
        public readonly List<ExternalLogin> Links = new();

        public Guid? TenantForCodeUni { get; set; }
        public Task<Guid?> GetTenantIdByCodeAsync(string tenantCode, CancellationToken ct = default)
            => Task.FromResult(tenantCode == "UNI" ? TenantForCodeUni : null);
        public Task<SsoProviderSetting?> GetSettingAsync(Guid? tenantId, SsoProvider provider, CancellationToken ct = default)
            => Task.FromResult(Settings.FirstOrDefault(s => s.TenantId == tenantId && s.Provider == provider));
        public Task<IReadOnlyList<SsoProviderSetting>> GetEnabledSettingsAsync(Guid? tenantId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SsoProviderSetting>>(Settings.Where(s => s.TenantId == tenantId && s.IsEnabled).ToList());
        public Task<ExternalLogin?> FindLinkAsync(Guid? tenantId, SsoProvider provider, string externalSubjectId, CancellationToken ct = default)
            => Task.FromResult(Links.FirstOrDefault(l => l.TenantId == tenantId && l.Provider == provider && l.ExternalSubjectId == externalSubjectId));
        public Task<bool> UserHasLinkAsync(Guid userId, SsoProvider provider, CancellationToken ct = default)
            => Task.FromResult(Links.Any(l => l.UserId == userId && l.Provider == provider));
        public Task AddLinkAsync(ExternalLogin link, CancellationToken ct = default) { Links.Add(link); return Task.CompletedTask; }
        public Task<IReadOnlyList<SsoProviderSetting>> GetEnforcingSettingsAsync(Guid? tenantId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SsoProviderSetting>>(Settings.Where(s => s.IsEnabled && s.EnforceSsoOnly && (s.TenantId == null || s.TenantId == tenantId)).ToList());
        public Task<IReadOnlyList<ExternalLogin>> GetLinksForUserAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ExternalLogin>>(Links.Where(l => l.UserId == userId).ToList());
        public void RemoveLink(ExternalLogin link) => Links.Remove(link);
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
    }

    private sealed class FakeAuth : IAuthService
    {
        public readonly List<(Guid UserId, string? MfaCode, bool MfaSatisfied)> Calls = new();
        public string? RequireTotp { get; set; }
        public LoginFailureReason? FailWith { get; set; }

        public Task<LoginResult> ExternalLoginAsync(Guid userId, string provider, string? mfaCode, bool mfaSatisfiedExternally, string? ipAddress, string? deviceInfo, CancellationToken ct = default)
        {
            Calls.Add((userId, mfaCode, mfaSatisfiedExternally));
            if (FailWith is not null) return Task.FromResult(LoginResult.Fail(FailWith.Value));
            if (RequireTotp is not null && !mfaSatisfiedExternally)
            {
                if (mfaCode is null) return Task.FromResult(LoginResult.Fail(LoginFailureReason.MfaCodeRequired));
                if (mfaCode != RequireTotp) return Task.FromResult(LoginResult.Fail(LoginFailureReason.InvalidMfaCode));
            }
            return Task.FromResult(LoginResult.Ok(new LoginResponse("jwt", "refresh", DateTime.UtcNow.AddMinutes(15), "Student", userId, "u")));
        }

        public Task<LoginResult> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<AuthSecurityProfileResponse> GetSecurityProfileAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<LoginResponse?> RefreshAsync(string rawRefreshToken, string? ipAddress, CancellationToken ct = default) => throw new NotSupportedException();
        public Task LogoutAsync(string rawRefreshToken, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> ForceChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<MfaSetupResponse?> BeginMfaSetupAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> EnableMfaAsync(Guid userId, EnableMfaRequest request, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<MfaRecoveryCodesResponse?> RegenerateRecoveryCodesAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FakeAudit
    {
        public readonly List<AuditLog> Entries = new();
        public IAuditService Service => Proxy<IAuditService>.Create(new()
        {
            ["LogAsync"] = a => { Entries.Add((AuditLog)a[0]!); return Task.CompletedTask; }
        });
    }

    private sealed class PlainEncryption : IEncryptionService
    {
        public string Encrypt(string plaintext) => "enc:" + plaintext;
        public string Decrypt(string ciphertext) => ciphertext.StartsWith("enc:") ? ciphertext[4..] : throw new CryptographicException();
    }

    /// <summary>Interface fake routing calls by method name; unrouted calls return default values.</summary>
    public class Proxy<T> : DispatchProxy where T : class
    {
        private Dictionary<string, Func<object?[], object?>> _handlers = new();

        public static T Create(Dictionary<string, Func<object?[], object?>> handlers)
        {
            var proxy = DispatchProxy.Create<T, Proxy<T>>();
            ((Proxy<T>)(object)proxy)._handlers = handlers;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (_handlers.TryGetValue(method!.Name, out var handler))
                return handler(args ?? []);

            var rt = method.ReturnType;
            if (rt == typeof(Task)) return Task.CompletedTask;
            if (rt.IsGenericType && rt.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var inner = rt.GetGenericArguments()[0];
                var value = inner.IsValueType ? Activator.CreateInstance(inner) : null;
                return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(inner).Invoke(null, [value]);
            }
            return rt.IsValueType && rt != typeof(void) ? Activator.CreateInstance(rt) : null;
        }
    }
}
