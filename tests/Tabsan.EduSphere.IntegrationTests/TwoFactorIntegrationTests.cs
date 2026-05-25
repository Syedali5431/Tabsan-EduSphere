using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Infrastructure.Auth;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

[Collection(EduSphereCollection.Name)]
public class TwoFactorIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public TwoFactorIntegrationTests(EduSphereWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Setup_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("api/v1/2fa/setup");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Setup_Authenticated_ReturnsProvisioningPayload()
    {
        var seed = await SeedTwoFactorUserAsync();
        using var client = CreateClient(seed.UserId);

        var response = await client.GetAsync("api/v1/2fa/setup");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var setup = await response.Content.ReadFromJsonAsync<TwoFactorSetupPayload>();
        Assert.NotNull(setup);
        Assert.False(setup!.TwoFactorEnabled);
        Assert.Equal(seed.Email, setup.AccountName);
        Assert.Equal("Tabsan EduSphere", setup.Issuer);
        Assert.StartsWith("data:image/png;base64,", setup.QrCodeDataUrl, StringComparison.Ordinal);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == seed.UserId);

        Assert.False(user.MfaIsEnabled);
        Assert.NotNull(user.MfaTotpSecret);
        Assert.NotEqual(setup.ManualKey, user.MfaTotpSecret);
    }

    [Fact]
    public async Task VerifySetup_WithGeneratedCode_EnablesTwoFactor()
    {
        var seed = await SeedTwoFactorUserAsync();
        using var client = CreateClient(seed.UserId);

        var setupResponse = await client.GetAsync("api/v1/2fa/setup");
        setupResponse.EnsureSuccessStatusCode();
        var setup = await setupResponse.Content.ReadFromJsonAsync<TwoFactorSetupPayload>();
        Assert.NotNull(setup);

        var code = GenerateTotpCode(setup!.ManualKey, DateTime.UtcNow);
        var verifyResponse = await client.PostAsJsonAsync("api/v1/2fa/verify", new { code });

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        var verification = await verifyResponse.Content.ReadFromJsonAsync<TwoFactorOperationPayload>();
        Assert.NotNull(verification);
        Assert.True(verification!.Success);
        Assert.True(verification.TwoFactorEnabled);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == seed.UserId);

        Assert.True(user.MfaIsEnabled);
        Assert.NotNull(user.MfaTotpSecret);
    }

    [Fact]
    public async Task Disable_WithGeneratedCode_ClearsSecretAndDisablesTwoFactor()
    {
        var seed = await SeedTwoFactorUserAsync();
        using var client = CreateClient(seed.UserId);

        var setupResponse = await client.GetAsync("api/v1/2fa/setup");
        setupResponse.EnsureSuccessStatusCode();
        var setup = await setupResponse.Content.ReadFromJsonAsync<TwoFactorSetupPayload>();
        Assert.NotNull(setup);

        var enableCode = GenerateTotpCode(setup!.ManualKey, DateTime.UtcNow);
        var verifyResponse = await client.PostAsJsonAsync("api/v1/2fa/verify", new { code = enableCode });
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        var disableCode = GenerateTotpCode(setup.ManualKey, DateTime.UtcNow);
        var disableResponse = await client.PostAsJsonAsync("api/v1/2fa/disable", new { code = disableCode });

        Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);

        var result = await disableResponse.Content.ReadFromJsonAsync<TwoFactorOperationPayload>();
        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.False(result.TwoFactorEnabled);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == seed.UserId);

        Assert.False(user.MfaIsEnabled);
        Assert.Null(user.MfaTotpSecret);
    }

    [Fact]
    public async Task LoginVerify_WithGeneratedCode_ReturnsOk()
    {
        var seed = await SeedTwoFactorUserAsync();
        using var client = _factory.CreateClient();

        string code;

        using (var setupClient = CreateClient(seed.UserId))
        {
            var setupResponse = await setupClient.GetAsync("api/v1/2fa/setup");
            setupResponse.EnsureSuccessStatusCode();
            var setup = await setupResponse.Content.ReadFromJsonAsync<TwoFactorSetupPayload>();
            Assert.NotNull(setup);

            code = GenerateTotpCode(setup!.ManualKey, DateTime.UtcNow);

            var verifyResponse = await setupClient.PostAsJsonAsync("api/v1/2fa/verify", new { code });
            Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        }

        var loginResponse = await client.PostAsJsonAsync("api/v1/2fa/login-verify", new
        {
            userId = seed.UserId,
            code
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var result = await loginResponse.Content.ReadFromJsonAsync<TwoFactorOperationPayload>();
        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.True(result.TwoFactorEnabled);
    }

    [Fact]
    public async Task LoginVerify_WithInvalidCode_Returns401()
    {
        var seed = await SeedTwoFactorUserAsync();
        using var client = CreateClient(seed.UserId);

        var setupResponse = await client.GetAsync("api/v1/2fa/setup");
        setupResponse.EnsureSuccessStatusCode();
        var setup = await setupResponse.Content.ReadFromJsonAsync<TwoFactorSetupPayload>();
        Assert.NotNull(setup);

        var verifyResponse = await client.PostAsJsonAsync("api/v1/2fa/verify", new { code = GenerateTotpCode(setup!.ManualKey, DateTime.UtcNow) });
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("api/v1/2fa/login-verify", new
        {
            userId = seed.UserId,
            code = "000000"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    private HttpClient CreateClient(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.GenerateToken("Admin", userId.ToString(), email: "twofactor.integration@tabsan.local"));
        return client;
    }

    private async Task<(Guid UserId, string Email)> SeedTwoFactorUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var role = await db.Roles.SingleAsync(r => r.Name == "Admin");
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"twofactor_{suffix}";
        var email = $"{username}@tabsan.local";

        var user = new User(username, "integration-hash", role.Id, email: email);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return (user.Id, email);
    }

    private static string GenerateTotpCode(string secret, DateTime utcNow, int digits = 6, int stepSeconds = 30)
    {
        var key = Base32Decode(secret);
        var counter = (long)Math.Floor((utcNow - DateTime.UnixEpoch).TotalSeconds / stepSeconds);

        Span<byte> counterBytes = stackalloc byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xff);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0f;
        var binaryCode = ((hash[offset] & 0x7f) << 24)
                         | (hash[offset + 1] << 16)
                         | (hash[offset + 2] << 8)
                         | hash[offset + 3];
        var mod = (int)Math.Pow(10, digits);
        return (binaryCode % mod).ToString(new string('0', digits));
    }

    private static byte[] Base32Decode(string input)
    {
        var clean = input.Trim().TrimEnd('=').ToUpperInvariant();
        var bytes = new List<byte>(clean.Length * 5 / 8);

        var bitBuffer = 0;
        var bitsInBuffer = 0;
        foreach (var ch in clean)
        {
            var val = ch switch
            {
                >= 'A' and <= 'Z' => ch - 'A',
                >= '2' and <= '7' => ch - '2' + 26,
                _ => -1
            };

            if (val < 0)
                continue;

            bitBuffer = (bitBuffer << 5) | val;
            bitsInBuffer += 5;

            if (bitsInBuffer >= 8)
            {
                bytes.Add((byte)((bitBuffer >> (bitsInBuffer - 8)) & 0xff));
                bitsInBuffer -= 8;
            }
        }

        return bytes.ToArray();
    }

    private sealed record TwoFactorSetupPayload(
        bool TwoFactorEnabled,
        string Issuer,
        string AccountName,
        string ManualKey,
        string ProvisioningUri,
        string QrCodeDataUrl);

    private sealed record TwoFactorOperationPayload(
        bool Success,
        bool TwoFactorEnabled,
        string Message);
}