using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Notifications;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Infrastructure.Notifications;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Plan M4.3 — Web Push through the real API and database: subscription rules (allowed push services, valid keys,
/// own devices only), preferences, the real VAPID key store, and notification fan-out to devices through the
/// queued worker (with a recording sender instead of the network).
/// </summary>
[Collection(EduSphereCollection.Name)]
public class WebPushIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public WebPushIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Subscriptions_AreValidated_AndOwnedByTheCaller()
    {
        var (userId, _) = await SeedUserAsync();
        var me = Client("Student", userId);

        var summary = await me.GetFromJsonAsync<JsonElement>("api/v1/push");
        Assert.True(summary.GetProperty("enabled").GetBoolean());
        var publicKey = Base64Url.Decode(summary.GetProperty("publicKey").GetString()!);
        Assert.Equal(65, publicKey.Length); // the real key store generated a P-256 key

        var (p256dh, auth) = BrowserKeys();
        var endpoint = "https://fcm.googleapis.com/fcm/send/" + Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.BadRequest, (await me.PostAsJsonAsync("api/v1/push/subscriptions",
            new { endpoint = "https://attacker.example.com/collect", p256dh, auth })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await me.PostAsJsonAsync("api/v1/push/subscriptions",
            new { endpoint, p256dh = "AAAA", auth })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await me.PostAsJsonAsync("api/v1/push/subscriptions",
            new { endpoint, p256dh, auth, deviceLabel = "Chrome on Android" })).StatusCode);

        var devices = (await me.GetFromJsonAsync<JsonElement>("api/v1/push")).GetProperty("devices").EnumerateArray().ToList();
        Assert.Single(devices);
        Assert.Equal("fcm.googleapis.com", devices[0].GetProperty("endpointHost").GetString());
        Assert.DoesNotContain(endpoint, (await me.GetStringAsync("api/v1/push"))); // endpoints are never echoed back

        // Another user cannot remove it; the owner can.
        var (otherId, _) = await SeedUserAsync();
        var other = Client("Student", otherId);
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync("api/v1/push/subscriptions?endpoint=" + Uri.EscapeDataString(endpoint))).StatusCode);

        // The same browser used by another user moves to that user.
        Assert.Equal(HttpStatusCode.NoContent, (await other.PostAsJsonAsync("api/v1/push/subscriptions", new { endpoint, p256dh, auth })).StatusCode);
        Assert.Empty((await me.GetFromJsonAsync<JsonElement>("api/v1/push")).GetProperty("devices").EnumerateArray());
        Assert.Equal(HttpStatusCode.NoContent, (await other.DeleteAsync("api/v1/push/subscriptions?endpoint=" + Uri.EscapeDataString(endpoint))).StatusCode);

        // Preferences: everything on by default; a type can be switched off.
        Assert.Equal(HttpStatusCode.NoContent, (await me.PutAsJsonAsync("api/v1/push/preferences",
            new { preferences = new[] { new { type = "Announcement", label = "x", pushEnabled = false } } })).StatusCode);
        var prefs = (await me.GetFromJsonAsync<JsonElement>("api/v1/push")).GetProperty("preferences").EnumerateArray().ToList();
        Assert.False(prefs.Single(p => p.GetProperty("type").GetString() == "Announcement").GetProperty("pushEnabled").GetBoolean());
        Assert.True(prefs.Single(p => p.GetProperty("type").GetString() == "Assignment").GetProperty("pushEnabled").GetBoolean());
    }

    [Fact]
    public async Task Notifications_ReachSubscribedDevices_RespectingPreferences_AndDropGoneDevices()
    {
        var recorder = new RecordingSender();
        var switchable = _factory.Services.GetRequiredService<SwitchableWebPushSender>();
        switchable.Inner = recorder;
        // The test host removes hosted services, so the real queue worker is run here for the scenario.
        var worker = new Tabsan.EduSphere.API.Services.PushDispatchWorker(
            _factory.Services.GetRequiredService<Tabsan.EduSphere.API.Services.PushDispatchQueue>(),
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Tabsan.EduSphere.API.Services.PushDispatchWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        try
        {
            await DeliveryScenarioAsync(recorder);
        }
        finally
        {
            switchable.Inner = null;
            await worker.StopAsync(CancellationToken.None);
        }
    }

    private async Task DeliveryScenarioAsync(RecordingSender recorder)
    {
        HttpClient ClientFor(string role, Guid user) => Client(role, user);

        var (alice, _) = await SeedUserAsync();
        var (bob, _) = await SeedUserAsync();
        var aliceClient = ClientFor("Student", alice);
        var bobClient = ClientFor("Student", bob);
        var (k1, a1) = BrowserKeys();
        var (k2, a2) = BrowserKeys();
        var aliceEndpoint = "https://fcm.googleapis.com/fcm/send/alice-" + Guid.NewGuid().ToString("N");
        var bobEndpoint = "https://updates.push.services.mozilla.com/wpush/v2/bob-" + Guid.NewGuid().ToString("N");
        await aliceClient.PostAsJsonAsync("api/v1/push/subscriptions", new { endpoint = aliceEndpoint, p256dh = k1, auth = a1 });
        await bobClient.PostAsJsonAsync("api/v1/push/subscriptions", new { endpoint = bobEndpoint, p256dh = k2, auth = a2 });
        await bobClient.PutAsJsonAsync("api/v1/push/preferences", new { preferences = new[] { new { type = "Announcement", label = "x", pushEnabled = false } } });

        var admin = ClientFor("Admin", Guid.NewGuid());
        var sent = await admin.PostAsJsonAsync("api/v1/notification", new { title = "Exam moved", body = "Room 4 instead of 2", type = 6, recipientUserIds = new[] { alice, bob } });
        Assert.True(sent.IsSuccessStatusCode, await sent.Content.ReadAsStringAsync());

        var delivered = await recorder.WaitForAsync(aliceEndpoint);
        using (var payload = JsonDocument.Parse(delivered))
        {
            Assert.Equal("Exam moved", payload.RootElement.GetProperty("title").GetString());
            Assert.Equal("/Portal/Announcements", payload.RootElement.GetProperty("url").GetString());
        }
        await Task.Delay(300);
        Assert.False(recorder.Sent.ContainsKey(bobEndpoint)); // Bob switched announcements off

        // Bob's assignment notification arrives; then his push service says the subscription is gone → removed.
        recorder.GoneEndpoints.Add(bobEndpoint);
        await admin.PostAsJsonAsync("api/v1/notification", new { title = "New assignment", body = "Essay due Friday", type = 2, recipientUserIds = new[] { bob } });
        await recorder.WaitForAsync(bobEndpoint);
        await WaitUntilAsync(async () => !(await bobClient.GetFromJsonAsync<JsonElement>("api/v1/push")).GetProperty("devices").EnumerateArray().Any());
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private sealed class RecordingSender : IWebPushSender
    {
        private static readonly VapidKeys Keys = VapidKeys.Generate();
        public ConcurrentDictionary<string, string> Sent { get; } = new();
        public ConcurrentBag<string> GoneEndpoints { get; } = new();
        public bool IsConfigured => true;
        public string? PublicKey => Base64Url.Encode(Keys.PublicKey);

        public Task<WebPushSendResult> SendAsync(PushTarget target, byte[] payload, int ttlSeconds, CancellationToken ct = default)
        {
            Sent[target.Endpoint] = System.Text.Encoding.UTF8.GetString(payload);
            return Task.FromResult(GoneEndpoints.Contains(target.Endpoint) ? WebPushSendResult.Gone : WebPushSendResult.Delivered);
        }

        public async Task<string> WaitForAsync(string endpoint)
        {
            for (var i = 0; i < 100; i++)
            {
                if (Sent.TryGetValue(endpoint, out var payload)) return payload;
                await Task.Delay(100);
            }
            throw new TimeoutException("No push message was sent to " + endpoint);
        }
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        for (var i = 0; i < 50; i++)
        {
            if (await condition()) return;
            await Task.Delay(100);
        }
        Assert.Fail("The condition was not met in time.");
    }

    private static (string P256dh, string Auth) BrowserKeys()
    {
        using var browser = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        return (Base64Url.Encode(WebPushEncryption.ExportPublic(browser)), Base64Url.Encode(RandomNumberGenerator.GetBytes(16)));
    }

    private HttpClient Client(string role, Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(role, userId.ToString()));
        return client;
    }

    private async Task<(Guid UserId, string Username)> SeedUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var role = db.Roles.First(r => r.Name == "Student");
        var user = new User($"push_{suffix}", "integration-hash", role.Id, $"push_{suffix}@tabsan.local");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (user.Id, user.Username);
    }
}
