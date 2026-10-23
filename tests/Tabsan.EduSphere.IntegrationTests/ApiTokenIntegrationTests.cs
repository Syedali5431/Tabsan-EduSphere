using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Application.Auth;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Plan M14.2/M14.3 — personal access tokens for the public API (created after signing in, shown once, act as their owner,
/// read-only tokens only read, revoked/expired tokens refused, tokens cannot manage tokens) and the request limits
/// (per user, per address, 429 with Retry-After).
/// </summary>
[Collection(EduSphereCollection.Name)]
public class ApiTokenIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public ApiTokenIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    private async Task<Guid> SeedUserAsync(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = new User($"tok_{suffix}", "integration-hash", db.Roles.First(r => r.Name == role).Id, $"tok_{suffix}@tabsan.local");
        db.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private HttpClient Jwt(string role, Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(role, userId.ToString()));
        return client;
    }

    private HttpClient Token(string value, WebApplicationFactory<Program>? factory = null)
    {
        var client = (factory ?? _factory).CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", value);
        return client;
    }

    [Fact]
    public async Task Tokens_ActAsTheirOwner_RespectReadOnly_AndStopWhenRevoked()
    {
        var adminId = await SeedUserAsync("Admin");
        var admin = Jwt("Admin", adminId);

        var created = await admin.PostAsJsonAsync("api/v1/api-tokens", new { name = "Nightly SIS sync", expiresInDays = 30, readOnly = false });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        var value = body.GetProperty("value").GetString()!;
        Assert.StartsWith(ApiToken.Prefix, value);
        Assert.Equal(value[..12] + "…", body.GetProperty("token").GetProperty("hint").GetString());
        using (var scope = _factory.Services.CreateScope())
        {
            var stored = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().ApiTokens.Single(t => t.Name == "Nightly SIS sync" && t.UserId == adminId);
            Assert.Equal(ApiTokenService.Hash(value), stored.TokenHash); // only the hash is kept
        }

        // The token works like the owner's sign-in (an Admin endpoint), but cannot manage tokens.
        var integration = Token(value);
        Assert.Equal(HttpStatusCode.OK, (await integration.GetAsync("api/v1/xapi/credentials")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await integration.PostAsJsonAsync("api/v1/xapi/credentials", new { name = "Created with a token" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await integration.GetAsync("api/v1/api-tokens")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await integration.PostAsJsonAsync("api/v1/api-tokens", new { name = "x", expiresInDays = 1, readOnly = false })).StatusCode);

        // A read-only token reads but does not write.
        var readOnly = (await (await admin.PostAsJsonAsync("api/v1/api-tokens", new { name = "Reporting", expiresInDays = 7, readOnly = true }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("value").GetString()!;
        var reporting = Token(readOnly);
        Assert.Equal(HttpStatusCode.OK, (await reporting.GetAsync("api/v1/xapi/credentials")).StatusCode);
        var write = await reporting.PostAsJsonAsync("api/v1/xapi/credentials", new { name = "nope" });
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
        Assert.Contains("read-only", await write.Content.ReadAsStringAsync());

        // Listing and revoking.
        var mine = await admin.GetFromJsonAsync<JsonElement>("api/v1/api-tokens");
        Assert.Equal(2, mine.GetArrayLength());
        var id = mine.EnumerateArray().Single(t => t.GetProperty("name").GetString() == "Nightly SIS sync").GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"api/v1/api-tokens/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Token(value).GetAsync("api/v1/xapi/credentials")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Token(ApiToken.Prefix + "made-up").GetAsync("api/v1/xapi/credentials")).StatusCode);
        Assert.Contains((await admin.GetFromJsonAsync<JsonElement>("api/v1/api-tokens/all")).EnumerateArray(), t => t.GetProperty("id").GetGuid() == id && !t.GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task Tokens_AreLimitedByRole_Lifetime_AndExpiry()
    {
        var studentId = await SeedUserAsync("Student");
        Assert.Equal(HttpStatusCode.Forbidden, (await Jwt("Student", studentId).PostAsJsonAsync("api/v1/api-tokens", new { name = "x", expiresInDays = 5, readOnly = true })).StatusCode);
        var adminId = await SeedUserAsync("Admin");
        var admin = Jwt("Admin", adminId);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("api/v1/api-tokens", new { name = "forever", expiresInDays = (int?)null, readOnly = false })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("api/v1/api-tokens", new { name = "too long", expiresInDays = 400, readOnly = false })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("api/v1/api-tokens", new { name = " ", expiresInDays = 5, readOnly = false })).StatusCode);

        // An expired token is refused.
        const string expired = ApiToken.Prefix + "expired-token-for-the-test-0000000000000";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Add(new ApiToken(adminId, "old", expired[..12], ApiTokenService.Hash(expired), false, DateTime.UtcNow.AddMinutes(-1)));
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await Token(expired).GetAsync("api/v1/xapi/credentials")).StatusCode);
        // Someone else cannot revoke your token.
        var other = Jwt("Faculty", await SeedUserAsync("Faculty"));
        var mine = await admin.GetFromJsonAsync<JsonElement>("api/v1/api-tokens");
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync($"api/v1/api-tokens/{mine.EnumerateArray().First().GetProperty("id").GetGuid()}")).StatusCode);
    }

    [Fact]
    public async Task RequestLimits_ApplyPerUserAndPerAddress_WithRetryAfter()
    {
        // A second host runs the start-up seeder on the existing test database; with a SuperAdmin present it has nothing to create.
        await SeedUserAsync("SuperAdmin");
        await using var limited = _factory.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RateLimiting:Enabled"] = "true",
            ["RateLimiting:PerUserPerMinute"] = "5",
            ["RateLimiting:AnonymousPerIpPerMinute"] = "3",
            ["RateLimiting:ExemptIps:0"] = "203.0.113.250"
        })));
        var userId = Guid.NewGuid();
        var user = limited.CreateClient();
        user.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken("Admin", userId.ToString()));
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 6; i++) statuses.Add((await user.GetAsync("api/v1/xapi/credentials")).StatusCode);
        Assert.Equal(5, statuses.Count(s => s == HttpStatusCode.OK));
        var throttled = await user.GetAsync("api/v1/xapi/credentials");
        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        Assert.NotNull(throttled.Headers.RetryAfter);
        Assert.Contains("Too many requests", await throttled.Content.ReadAsStringAsync());

        // Another user has their own allowance.
        var other = limited.CreateClient();
        other.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken("Admin", Guid.NewGuid().ToString()));
        Assert.Equal(HttpStatusCode.OK, (await other.GetAsync("api/v1/xapi/credentials")).StatusCode);

        // Without a sign-in the address is limited.
        var anonymous = limited.CreateClient();
        var codes = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++) codes.Add((await anonymous.GetAsync("api/v1/xapi/about")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, codes.Last());
    }

    [Fact]
    public void OpenApiDocument_GeneratesForEveryEndpoint()
    {
        // One bad action (e.g. [FromForm] on an IFormFile parameter) makes the whole /swagger document fail with 500.
        var document = _factory.Services.GetRequiredService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>().GetSwagger("v1");
        Assert.Contains("/api/v1/api-tokens", document.Paths.Keys);
        Assert.Contains("/api/v1/xapi/statements", document.Paths.Keys);
        Assert.True(document.Paths.Count > 500);
        Assert.Contains("esk_", document.Components.SecuritySchemes["Bearer"].Description);
        Assert.Contains("/api/v1/", document.Info.Description);
    }
}
