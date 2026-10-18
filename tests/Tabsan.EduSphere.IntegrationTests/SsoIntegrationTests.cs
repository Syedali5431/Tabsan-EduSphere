using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Plan M1 Stage M1.2 — SSO endpoints through the real API pipeline (routing, module gate,
/// DI, EF persistence, AuthService token issuance). The identity provider is stubbed;
/// OIDC protocol handling is covered by SsoSignInTests in the unit suite.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class SsoIntegrationTests
{
    private const string Callback = "https://portal.test/Login/SsoCallback";
    private readonly EduSphereWebFactory _factory;

    public SsoIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    [Fact]
    public async Task SsoRoutes_AreBlockedWhenModuleInactive()
    {
        using var admin = AdminClient();
        await SetModuleAsync(admin, active: false);
        try
        {
            using var client = _factory.CreateClient();
            var response = await client.GetAsync("api/v1/sso/providers");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            await SetModuleAsync(admin, active: true);
        }
    }

    [Fact]
    public async Task Start_RejectsForeignRedirectUri()
    {
        await EnsureGoogleProviderAsync();
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("api/v1/sso/start",
            new { provider = "Google", redirectUri = "https://evil.example.com/Login/SsoCallback?x=1" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("InvalidRedirectUri", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task FullRoundTrip_AutoLinksVerifiedEmail_IssuesTokens_AndStateIsSingleUse()
    {
        await EnsureGoogleProviderAsync();
        var (userId, email) = await SeedUserAsync();
        var stub = new StubOidc { Identity = new OidcIdentity("google-sub-" + userId.ToString("N"), email, true, "SSO User", false) };
        using var client = ClientWithIdp(stub);

        var providers = await client.GetFromJsonAsync<JsonElement>("api/v1/sso/providers");
        Assert.Contains(providers.EnumerateArray(), p => p.GetProperty("provider").GetString() == "Google");

        var start = await client.PostAsJsonAsync("api/v1/sso/start", new { provider = "google", redirectUri = Callback });
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var startBody = await start.Content.ReadFromJsonAsync<JsonElement>();
        var state = startBody.GetProperty("state").GetString()!;
        Assert.StartsWith("https://idp.stub/authorize", startBody.GetProperty("authorizationUrl").GetString());

        var complete = await client.PostAsJsonAsync("api/v1/sso/complete", new { state, code = "auth-code" });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        var login = await complete.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(login.GetProperty("accessToken").GetString()));
        Assert.Equal(userId, login.GetProperty("userId").GetGuid());
        Assert.False(login.GetProperty("mustChangePassword").GetBoolean());

        // The issued access token works against an authenticated endpoint.
        using var authed = _factory.CreateClient();
        authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("accessToken").GetString());
        var me = await authed.GetAsync("api/v1/notification");
        Assert.NotEqual(HttpStatusCode.Unauthorized, me.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var link = await db.ExternalLogins.AsNoTracking().SingleAsync(l => l.UserId == userId);
            Assert.Equal(SsoProvider.Google, link.Provider);
            Assert.NotNull(link.LastLoginAt);
            Assert.True(await db.UserSessions.AnyAsync(s => s.UserId == userId));
        }

        var replay = await client.PostAsJsonAsync("api/v1/sso/complete", new { state, code = "auth-code" });
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        Assert.Contains("InvalidState", await replay.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Complete_UnlinkedUnverifiedIdentity_IsForbidden()
    {
        await EnsureGoogleProviderAsync();
        var stub = new StubOidc { Identity = new OidcIdentity("stranger-" + Guid.NewGuid().ToString("N"), "nobody@tabsan.local", false, null, false) };
        using var client = ClientWithIdp(stub);

        var start = await client.PostAsJsonAsync("api/v1/sso/start", new { provider = "Google", redirectUri = Callback });
        var state = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("state").GetString();
        var complete = await client.PostAsJsonAsync("api/v1/sso/complete", new { state, code = "c" });

        Assert.Equal(HttpStatusCode.Forbidden, complete.StatusCode);
        Assert.Contains("AccountNotLinked", await complete.Content.ReadAsStringAsync());
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private HttpClient AdminClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken("SuperAdmin", "00000000-0000-0000-0000-000000000001"));
        return client;
    }

    private static async Task SetModuleAsync(HttpClient admin, bool active)
    {
        var response = await admin.PostAsync($"api/v1/module/sso/{(active ? "activate" : "deactivate")}", null);
        response.EnsureSuccessStatusCode();
    }

    private HttpClient ClientWithIdp(IOidcClient stub)
    {
        _factory.Services.GetRequiredService<SwitchableOidcClient>().Inner = stub;
        return _factory.CreateClient();
    }

    private async Task EnsureGoogleProviderAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (await db.SsoProviderSettings.AnyAsync(s => s.TenantId == null && s.Provider == SsoProvider.Google))
            return;

        var encryption = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
        var setting = new SsoProviderSetting(null, SsoProvider.Google, "Sign in with Google");
        setting.ConfigureOidc("google-client-id", null);
        setting.SetEncryptedClientSecret(encryption.Encrypt("google-client-secret"));
        setting.SetPolicy(autoLinkByEmail: true, enforceSsoOnly: false, trustIdpMfa: false, ["tabsan.local"]);
        setting.Enable();
        db.SsoProviderSettings.Add(setting);
        await db.SaveChangesAsync();
    }

    private async Task<(Guid UserId, string Email)> SeedUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var role = await db.Roles.SingleAsync(r => r.Name == "Admin");
        var username = $"sso_{Guid.NewGuid().ToString("N")[..8]}";
        var email = $"{username}@tabsan.local";
        var user = new User(username, "integration-hash", role.Id, email: email, mustChangePassword: true);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (user.Id, email);
    }

    private sealed class StubOidc : IOidcClient
    {
        public OidcIdentity? Identity { get; init; }

        public Task<string> BuildAuthorizationUrlAsync(OidcProviderConfig provider, string redirectUri, string state, string nonce, string codeChallenge, CancellationToken ct = default)
        {
            Assert.Equal("google-client-secret", provider.ClientSecret);
            return Task.FromResult($"https://idp.stub/authorize?state={state}&code_challenge={codeChallenge}");
        }

        public Task<OidcIdentity> RedeemCodeAsync(OidcProviderConfig provider, string code, string redirectUri, string codeVerifier, string expectedNonce, CancellationToken ct = default)
            => Identity is null ? throw new OidcException("no identity") : Task.FromResult(Identity);
    }
}
