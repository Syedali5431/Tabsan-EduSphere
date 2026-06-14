using FluentAssertions;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.API.Services.TwoFactor;
using Tabsan.EduSphere.Application.Auth;
using Tabsan.EduSphere.Application.DTOs.TwoFactor;
using Tabsan.EduSphere.Application.Interfaces;
using Xunit;

namespace Tabsan.EduSphere.UnitTests;

public class TwoFactorSetupServiceTests
{
    [Fact]
    public async Task BeginSetupAsync_ReturnsQrPayload_AndStoresSecret()
    {
        var userId = Guid.NewGuid();
        var stateStore = new StubTwoFactorStateStore(
            new TwoFactorStateSnapshot(userId, "student1", "student1@tabsan.edu", true, false, null));
        var totp = new StubTotpService(secretToGenerate: "ABCDEF123456");
        var sut = CreateSut(stateStore, totp);

        var result = await sut.BeginSetupAsync(userId);

        result.Should().NotBeNull();
        result!.TwoFactorEnabled.Should().BeFalse();
        result.Issuer.Should().Be("Tabsan EduSphere");
        result.AccountName.Should().Be("student1@tabsan.edu");
        result.ManualKey.Should().Be("ABCDEF123456");
        result.ProvisioningUri.Should().Be("otpauth://totp/Tabsan EduSphere:student1@tabsan.edu?secret=ABCDEF123456&issuer=Tabsan EduSphere&digits=6&period=30");
        result.QrCodeDataUrl.Should().StartWith("data:image/png;base64,");
        stateStore.SavedSecret.Should().Be("ABCDEF123456");
        totp.LastBuildProvisioningUriArgs.Should().BeEquivalentTo(new
        {
            Issuer = "Tabsan EduSphere",
            AccountName = "student1@tabsan.edu",
            Secret = "ABCDEF123456",
            Digits = 6,
            StepSeconds = 30
        });
    }

    [Fact]
    public async Task BeginSetupAsync_ReturnsNull_WhenUserIsMissingOrInactive()
    {
        var stateStore = new StubTwoFactorStateStore(snapshot: null);
        var sut = CreateSut(stateStore);

        var result = await sut.BeginSetupAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task VerifySetupAsync_WhenCodeMatches_EnablesTwoFactor()
    {
        var userId = Guid.NewGuid();
        var stateStore = new StubTwoFactorStateStore(
            new TwoFactorStateSnapshot(userId, "student1", null, true, false, "SECRET"));
        var totp = new StubTotpService(validCodes: ["111111"]);
        var sut = CreateSut(stateStore, totp);

        var result = await sut.VerifySetupAsync(userId, "111111");

        result.Should().BeTrue();
        stateStore.EnableCalled.Should().BeTrue();
        stateStore.CurrentSnapshot!.TwoFactorEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task VerifySetupAsync_WhenCodeDoesNotMatch_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var stateStore = new StubTwoFactorStateStore(
            new TwoFactorStateSnapshot(userId, "student1", null, true, false, "SECRET"));
        var totp = new StubTotpService(validCodes: ["111111"]);
        var sut = CreateSut(stateStore, totp);

        var result = await sut.VerifySetupAsync(userId, "222222");

        result.Should().BeFalse();
        stateStore.EnableCalled.Should().BeFalse();
        stateStore.CurrentSnapshot!.TwoFactorEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task DisableAsync_WhenCodeMatches_ClearsSecretAndDisablesTwoFactor()
    {
        var userId = Guid.NewGuid();
        var stateStore = new StubTwoFactorStateStore(
            new TwoFactorStateSnapshot(userId, "student1", null, true, true, "SECRET"));
        var totp = new StubTotpService(validCodes: ["111111"]);
        var sut = CreateSut(stateStore, totp);

        var result = await sut.DisableAsync(userId, "111111");

        result.Should().BeTrue();
        stateStore.DisableCalled.Should().BeTrue();
        stateStore.CurrentSnapshot!.TwoFactorEnabled.Should().BeFalse();
        stateStore.CurrentSnapshot.SecretKey.Should().BeNull();
    }

    [Fact]
    public async Task VerifyLoginAsync_WhenCodeMatches_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var stateStore = new StubTwoFactorStateStore(
            new TwoFactorStateSnapshot(userId, "student1", null, true, true, "SECRET"));
        var totp = new StubTotpService(validCodes: ["111111"]);
        var sut = CreateSut(stateStore, totp);

        var result = await sut.VerifyLoginAsync(userId, "111111");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyLoginAsync_WhenTwoFactorIsDisabled_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var stateStore = new StubTwoFactorStateStore(
            new TwoFactorStateSnapshot(userId, "student1", null, true, false, "SECRET"));
        var totp = new StubTotpService(validCodes: ["111111"]);
        var sut = CreateSut(stateStore, totp);

        var result = await sut.VerifyLoginAsync(userId, "111111");

        result.Should().BeFalse();
    }

    private static TwoFactorSetupService CreateSut(ITwoFactorStateStore stateStore, ITotpService? totp = null)
    {
        totp ??= new StubTotpService(secretToGenerate: "ABCDEF123456");

        var twoFactorTotp = (ITotpService)totp;

        return new TwoFactorSetupService(
            stateStore,
            new TwoFactorService(twoFactorTotp, Options.Create(new AuthSecurityOptions
            {
                Mfa = new MfaSettings
                {
                    TotpIssuer = "Tabsan EduSphere",
                    TotpDigits = 6,
                    TotpStepSeconds = 30,
                    TotpAllowedDriftWindows = 1
                }
            })),
            new QRCodeService());
    }
}

file sealed class StubTwoFactorStateStore : ITwoFactorStateStore
{
    private TwoFactorStateSnapshot? _snapshot;

    public StubTwoFactorStateStore(TwoFactorStateSnapshot? snapshot)
    {
        _snapshot = snapshot;
    }

    public string? SavedSecret { get; private set; }
    public bool EnableCalled { get; private set; }
    public bool DisableCalled { get; private set; }
    public TwoFactorStateSnapshot? CurrentSnapshot => _snapshot;

    public Task<TwoFactorStateSnapshot?> GetAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(_snapshot is not null && _snapshot.UserId == userId ? _snapshot : null);

    public Task<bool> SaveSetupAsync(Guid userId, string secretKey, CancellationToken ct = default)
    {
        if (_snapshot is null || _snapshot.UserId != userId || string.IsNullOrWhiteSpace(secretKey))
            return Task.FromResult(false);

        SavedSecret = secretKey;
        _snapshot = _snapshot with { TwoFactorEnabled = false, SecretKey = secretKey };
        return Task.FromResult(true);
    }

    public Task<bool> EnableAsync(Guid userId, CancellationToken ct = default)
    {
        if (_snapshot is null || _snapshot.UserId != userId || string.IsNullOrWhiteSpace(_snapshot.SecretKey))
            return Task.FromResult(false);

        EnableCalled = true;
        _snapshot = _snapshot with { TwoFactorEnabled = true };
        return Task.FromResult(true);
    }

    public Task<bool> DisableAsync(Guid userId, CancellationToken ct = default)
    {
        if (_snapshot is null || _snapshot.UserId != userId)
            return Task.FromResult(false);

        DisableCalled = true;
        _snapshot = _snapshot with { TwoFactorEnabled = false, SecretKey = null };
        return Task.FromResult(true);
    }

    public Task<bool> HardDeleteAsync(Guid userId, CancellationToken ct = default)
    {
        if (_snapshot is null || _snapshot.UserId != userId)
            return Task.FromResult(false);

        _snapshot = null;
        return Task.FromResult(true);
    }
}

file sealed class StubTotpService : ITotpService
{
    private readonly HashSet<string> _validCodes;
    private readonly string _secretToGenerate;

    public StubTotpService(IEnumerable<string>? validCodes = null, string secretToGenerate = "ABCDEF123456")
    {
        _validCodes = new HashSet<string>(validCodes ?? [], StringComparer.Ordinal);
        _secretToGenerate = secretToGenerate;
    }

    public object? LastBuildProvisioningUriArgs { get; private set; }

    public string GenerateSecret() => _secretToGenerate;

    public string BuildProvisioningUri(string issuer, string accountName, string secret, int digits, int stepSeconds)
    {
        LastBuildProvisioningUriArgs = new { Issuer = issuer, AccountName = accountName, Secret = secret, Digits = digits, StepSeconds = stepSeconds };
        return $"otpauth://totp/{issuer}:{accountName}?secret={secret}&issuer={issuer}&digits={digits}&period={stepSeconds}";
    }

    public bool ValidateCode(string secret, string code, DateTime utcNow, int digits, int stepSeconds, int allowedDriftWindows)
        => _validCodes.Contains(code);
}
