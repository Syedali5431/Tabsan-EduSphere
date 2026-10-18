using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>Plan M1 Stage M1.5 — SuperAdmin SSO settings API (create/update/list/test/delete, audit, secrecy).</summary>
[Collection(EduSphereCollection.Name)]
public class SsoSettingsIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public SsoSettingsIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    [Fact]
    public async Task NonSuperAdmins_CannotReadOrChangeSettings()
    {
        foreach (var role in new[] { "Admin", "Faculty", "Student" })
        {
            using var client = TokenClient(role);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/v1/sso/settings")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("api/v1/sso/settings/GenericOidc", Body())).StatusCode);
        }
    }

    [Fact]
    public async Task GenericOidc_CreateUpdateListTestDelete_NeverReturnsSecret_AndAudits()
    {
        using var admin = TokenClient("SuperAdmin");

        // Create (disabled first, then enable) — the secret is write-only.
        var create = await admin.PutAsJsonAsync("api/v1/sso/settings/GenericOidc", Body(secret: "s3cr3t-value", enabled: false));
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var createdJson = await create.Content.ReadAsStringAsync();
        Assert.DoesNotContain("s3cr3t-value", createdJson);
        var created = JsonDocument.Parse(createdJson).RootElement;
        Assert.True(created.GetProperty("hasClientSecret").GetBoolean());
        Assert.False(created.GetProperty("isEnabled").GetBoolean());

        // Update without a secret keeps the stored one; enabling now succeeds.
        var update = await admin.PutAsJsonAsync("api/v1/sso/settings/GenericOidc", Body(secret: null, enabled: true, label: "Keycloak"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(updated.GetProperty("isEnabled").GetBoolean());
        Assert.Equal("Keycloak", updated.GetProperty("displayName").GetString());

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var encryption = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
            var row = await db.SsoProviderSettings.AsNoTracking().SingleAsync(s => s.TenantId == null && s.Provider == SsoProvider.GenericOidc);
            Assert.NotEqual("s3cr3t-value", row.ClientSecretEncrypted);
            Assert.Equal("s3cr3t-value", encryption.Decrypt(row.ClientSecretEncrypted!));
            var audits = await db.AuditLogs.AsNoTracking().Where(a => a.EntityId == row.Id.ToString()).ToListAsync();
            Assert.Contains(audits, a => a.Action == "SsoSettingsCreated");
            Assert.Contains(audits, a => a.Action == "SsoSettingsUpdated");
            Assert.DoesNotContain(audits, a => (a.NewValuesJson ?? "").Contains("s3cr3t-value"));
        }

        var list = await admin.GetFromJsonAsync<JsonElement>("api/v1/sso/settings");
        Assert.Contains(list.EnumerateArray(), s => s.GetProperty("provider").GetString() == "GenericOidc");
        Assert.DoesNotContain("s3cr3t-value", list.GetRawText());

        // Test connection goes through the (stubbed) OIDC client.
        _factory.Services.GetRequiredService<SwitchableOidcClient>().Inner = new ThrowingOidc();
        var test = await (await admin.PostAsync("api/v1/sso/settings/GenericOidc/test", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(test.GetProperty("success").GetBoolean());
        Assert.Contains("unreachable", test.GetProperty("message").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync("api/v1/sso/settings/GenericOidc")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync("api/v1/sso/settings/GenericOidc")).StatusCode);
    }

    [Fact]
    public async Task InvalidConfiguration_ReturnsFriendlyMessage()
    {
        using var admin = TokenClient("SuperAdmin");

        var multiTenant = await admin.PutAsJsonAsync("api/v1/sso/settings/Microsoft",
            Body(authority: "https://login.microsoftonline.com/common/v2.0"));
        Assert.Equal(HttpStatusCode.BadRequest, multiTenant.StatusCode);
        Assert.Contains("Authority", (await multiTenant.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());

        var badMetadata = await admin.PutAsJsonAsync("api/v1/sso/settings/Saml?tenant=",
            new { displayName = "Uni", metadataUrl = "http://not-https.test/md", enabled = true, allowedEmailDomains = Array.Empty<string>() });
        Assert.Equal(HttpStatusCode.BadRequest, badMetadata.StatusCode);
        Assert.Contains("Metadata URL", (await badMetadata.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());

        var unknownTenant = await admin.PutAsJsonAsync("api/v1/sso/settings/Google?tenant=NOPE", Body());
        Assert.Equal(HttpStatusCode.BadRequest, unknownTenant.StatusCode);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static object Body(string? secret = "secret", bool enabled = false, string label = "Institution login",
        string authority = "https://idp.integration.test")
        => new
        {
            displayName = label,
            clientId = "client-1",
            clientSecret = secret,
            authority,
            allowedEmailDomains = new[] { "tabsan.local" },
            autoLinkByEmail = false,
            enforceSsoOnly = false,
            trustIdpMfa = false,
            enabled
        };

    private HttpClient TokenClient(string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            JwtTestHelper.GenerateToken(role, role == "SuperAdmin" ? "00000000-0000-0000-0000-000000000001" : Guid.NewGuid().ToString()));
        return client;
    }

    private sealed class ThrowingOidc : IOidcClient
    {
        public Task<string> BuildAuthorizationUrlAsync(OidcProviderConfig provider, string redirectUri, string state, string nonce, string codeChallenge, CancellationToken ct = default)
            => throw new SsoProtocolException("Token endpoint unreachable.");

        public Task<ExternalIdentity> RedeemCodeAsync(OidcProviderConfig provider, string code, string redirectUri, string codeVerifier, string expectedNonce, CancellationToken ct = default)
            => throw new SsoProtocolException("unreachable");
    }
}
