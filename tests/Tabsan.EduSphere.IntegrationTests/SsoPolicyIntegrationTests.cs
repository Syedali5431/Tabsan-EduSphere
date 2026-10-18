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
/// Plan M1 Stage M1.4 — SSO-only enforcement on the real password login endpoint,
/// login-activity auth method, and linked-account management with audit.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class SsoPolicyIntegrationTests
{
    private const string Password = "SsoPolicy#2026";
    private readonly EduSphereWebFactory _factory;

    public SsoPolicyIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    [Fact]
    public async Task EnforceSsoOnly_BlocksPasswordLogin_ExceptSuperAdmin_AndOnlyWhileModuleActive()
    {
        var student = await SeedPasswordUserAsync("Student");
        var superAdmin = await SeedPasswordUserAsync("SuperAdmin");

        // Baseline: password login works.
        Assert.Equal(HttpStatusCode.OK, (await PasswordLoginAsync(student)).StatusCode);

        var settingId = await AddEnforcingProviderAsync();
        try
        {
            var blocked = await PasswordLoginAsync(student);
            Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
            Assert.Contains("SSO_REQUIRED", await blocked.Content.ReadAsStringAsync());

            // Break-glass: SuperAdmin keeps password login.
            Assert.Equal(HttpStatusCode.OK, (await PasswordLoginAsync(superAdmin)).StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var activity = await db.LoginActivityLogs.AsNoTracking()
                    .Where(a => a.Username == student).OrderByDescending(a => a.AttemptedAt).FirstAsync();
                Assert.False(activity.IsSuccess);
                Assert.Equal("SsoRequired", activity.FailureReason);
                Assert.Equal("password", activity.AuthMethod);
                Assert.True(await db.AuditLogs.AnyAsync(a => a.Action == "LoginBlockedSsoOnly"));
            }

            // A lapsed SSO licence must not lock users out.
            using var admin = AdminClient();
            (await admin.PostAsync("api/v1/module/sso/deactivate", null)).EnsureSuccessStatusCode();
            try
            {
                Assert.Equal(HttpStatusCode.OK, (await PasswordLoginAsync(student)).StatusCode);
            }
            finally
            {
                (await admin.PostAsync("api/v1/module/sso/activate", null)).EnsureSuccessStatusCode();
            }
        }
        finally
        {
            await RemoveProviderAsync(settingId);
        }

        Assert.Equal(HttpStatusCode.OK, (await PasswordLoginAsync(student)).StatusCode);
    }

    [Fact]
    public async Task PasswordLogin_RecordsPasswordAuthMethod()
    {
        var student = await SeedPasswordUserAsync("Student");

        Assert.Equal(HttpStatusCode.OK, (await PasswordLoginAsync(student)).StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var activity = await db.LoginActivityLogs.AsNoTracking()
            .Where(a => a.Username == student && a.IsSuccess).FirstAsync();
        Assert.Equal("password", activity.AuthMethod);
    }

    [Fact]
    public async Task LinkedAccounts_ListAndUnlink_AreAudited_AndAdminRoutesNeedAdminRole()
    {
        var (userId, linkSubject) = await SeedLinkedUserAsync();

        using var self = TokenClient("Student", userId);
        var links = await self.GetFromJsonAsync<JsonElement>("api/v1/sso/links");
        Assert.Contains(links.EnumerateArray(), l => l.GetProperty("provider").GetString() == "Google");

        // Students cannot use the admin routes.
        Assert.Equal(HttpStatusCode.Forbidden, (await self.GetAsync($"api/v1/sso/links/users/{userId}")).StatusCode);

        var unlink = await self.DeleteAsync("api/v1/sso/links/google");
        Assert.Equal(HttpStatusCode.NoContent, unlink.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await self.DeleteAsync("api/v1/sso/links/google")).StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.ExternalLogins.AnyAsync(l => l.ExternalSubjectId == linkSubject));
        var audit = await db.AuditLogs.AsNoTracking()
            .Where(a => a.Action == "SsoAccountUnlinked" && a.EntityId == userId.ToString()).SingleAsync();
        Assert.Equal(userId, audit.ActorUserId);
    }

    [Fact]
    public async Task Admin_CanUnlinkAnotherUsersAccount()
    {
        var (userId, _) = await SeedLinkedUserAsync();
        using var admin = AdminClient();

        var view = await admin.GetFromJsonAsync<JsonElement>($"api/v1/sso/links/users/{userId}");
        Assert.Equal(1, view.GetArrayLength());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"api/v1/sso/links/users/{userId}/Google")).StatusCode);
    }

    [Fact]
    public async Task SsoLoginActivity_RecordsSsoMethod_AndFailures()
    {
        await EnsureGoogleProviderAsync();
        var (userId, subject) = await SeedLinkedUserAsync();
        var email = await UserEmailAsync(userId);
        var stub = new StubOidc(new ExternalIdentity(subject, email, true, null, false));
        _factory.Services.GetRequiredService<SwitchableOidcClient>().Inner = stub;
        using var client = _factory.CreateClient();

        var ok = await RoundTripAsync(client);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        stub.Identity = new ExternalIdentity("unknown-" + Guid.NewGuid().ToString("N"), "stranger@tabsan.local", false, null, false);
        Assert.Equal(HttpStatusCode.Forbidden, (await RoundTripAsync(client)).StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await db.LoginActivityLogs.AnyAsync(a => a.UserId == userId && a.IsSuccess && a.AuthMethod == "sso:Google"));
        Assert.True(await db.LoginActivityLogs.AnyAsync(a =>
            !a.IsSuccess && a.AuthMethod == "sso:Google" && a.FailureReason == "SsoAccountNotLinked" && a.Username == "stranger@tabsan.local"));
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<HttpResponseMessage> RoundTripAsync(HttpClient client)
    {
        var start = await client.PostAsJsonAsync("api/v1/sso/start", new { provider = "Google", redirectUri = "https://portal.test/Login/SsoCallback" });
        var state = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("state").GetString();
        return await client.PostAsJsonAsync("api/v1/sso/complete", new { state, code = "c" });
    }

    private Task<HttpResponseMessage> PasswordLoginAsync(string username)
        => _factory.CreateClient().PostAsJsonAsync("api/v1/auth/login", new { username, password = Password });

    private async Task<string> SeedPasswordUserAsync(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var roleEntity = await db.Roles.SingleAsync(r => r.Name == role);
        var username = $"ssopol.{role.ToLowerInvariant()}.{Guid.NewGuid().ToString("N")[..6]}";
        db.Users.Add(new User(username, hasher.Hash(Password), roleEntity.Id, email: $"{username}@integration.local"));
        await db.SaveChangesAsync();
        return username;
    }

    private async Task<(Guid UserId, string Subject)> SeedLinkedUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var role = await db.Roles.SingleAsync(r => r.Name == "Student");
        var username = $"ssolink_{Guid.NewGuid().ToString("N")[..8]}";
        var user = new User(username, "integration-hash", role.Id, email: $"{username}@tabsan.local");
        db.Users.Add(user);
        var subject = "linked-" + Guid.NewGuid().ToString("N");
        db.ExternalLogins.Add(new ExternalLogin(null, user.Id, SsoProvider.Google, subject, user.Email));
        await db.SaveChangesAsync();
        return (user.Id, subject);
    }

private async Task<string> UserEmailAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return (await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId)).Email!;
    }

    private async Task<Guid> AddEnforcingProviderAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var encryption = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
        var setting = new SsoProviderSetting(null, SsoProvider.Microsoft, "Microsoft (SSO only)");
        setting.ConfigureOidc("ms-client", "https://login.microsoftonline.com/integration.test/v2.0");
        setting.SetEncryptedClientSecret(encryption.Encrypt("ms-secret"));
        setting.SetPolicy(false, enforceSsoOnly: true, false, null);
        setting.Enable();
        db.SsoProviderSettings.Add(setting);
        await db.SaveChangesAsync();
        return setting.Id;
    }

    private async Task RemoveProviderAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var setting = await db.SsoProviderSettings.SingleAsync(s => s.Id == id);
        db.SsoProviderSettings.Remove(setting);
        await db.SaveChangesAsync();
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

    private HttpClient AdminClient() => TokenClient("SuperAdmin", Guid.Parse("00000000-0000-0000-0000-000000000001"));

    private HttpClient TokenClient(string role, Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(role, userId.ToString()));
        return client;
    }

    private sealed class StubOidc(ExternalIdentity identity) : IOidcClient
    {
        public ExternalIdentity Identity { get; set; } = identity;

        public Task<string> BuildAuthorizationUrlAsync(OidcProviderConfig provider, string redirectUri, string state, string nonce, string codeChallenge, CancellationToken ct = default)
            => Task.FromResult("https://idp.stub/authorize?state=" + state);

        public Task<ExternalIdentity> RedeemCodeAsync(OidcProviderConfig provider, string code, string redirectUri, string codeVerifier, string expectedNonce, CancellationToken ct = default)
            => Task.FromResult(Identity);
    }
}
