using FluentAssertions;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
using Tabsan.EduSphere.Application.Auth;
using Tabsan.EduSphere.Application.DTOs.Auth;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Licensing;
using Xunit;

namespace Tabsan.EduSphere.UnitTests;

// Phase 27.2 — Authentication and Security UX unit tests

public class AuthSecurityUxTests
{
    [Fact]
    public async Task GetSecurityProfileAsync_ReturnsConfiguredFlags()
    {
        var sut = CreateSut(new AuthSecurityOptions
        {
            Mfa = new MfaSettings { Enabled = true, RequireForPasswordLogin = true, TotpIssuer = "Tabsan EduSphere" },
            Sso = new SsoSettings { Enabled = true, Provider = "AzureAD", LoginUrl = "https://sso.contoso.edu/login" },
            SessionRisk = new SessionRiskSettings { Enabled = true, BlockHighRiskLogin = true }
        });

        var profile = await sut.GetSecurityProfileAsync();

        profile.MfaEnabled.Should().BeTrue();
        profile.RequireMfaForPasswordLogin.Should().BeTrue();
        profile.RequireMfaForPrivilegedRolesOnly.Should().BeTrue();
        profile.PrivilegedMfaRoles.Should().Contain(new[] { "SuperAdmin", "Admin" });
        profile.SsoEnabled.Should().BeTrue();
        profile.SsoProvider.Should().Be("AzureAD");
        profile.SsoLoginUrl.Should().Be("https://sso.contoso.edu/login");
        profile.BlockHighRiskLogin.Should().BeTrue();
    }

    [Fact]
    public async Task LoginAsync_WhenMfaIsRequiredAndMissing_ReturnsMfaRequired()
    {
        var user = new User("student1", "HASH", roleId: 1);
        user.SetMfaEnrollment("KNOWNSECRET", "[]");
        user.EnableMfa();

        var sut = CreateSut(
            new AuthSecurityOptions
            {
                Mfa = new MfaSettings { Enabled = true, RequireForPasswordLogin = true, RequireForPrivilegedRolesOnly = false },
                SessionRisk = new SessionRiskSettings { Enabled = false }
            },
            user: user);

        var result = await sut.LoginAsync(new LoginRequest("student1", "pass"), "10.0.0.5");

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be(LoginFailureReason.MfaRequired);
    }

    [Fact]
    public async Task LoginAsync_WhenPrivilegedOnlyMfaEnabled_StudentWithoutCode_CanLogin()
    {
        var user = new User("student1", "HASH", roleId: 1);
        SetRole(user, "Student");

        var sut = CreateSut(
            new AuthSecurityOptions
            {
                Mfa = new MfaSettings
                {
                    Enabled = true,
                    RequireForPasswordLogin = true,
                    RequireForPrivilegedRolesOnly = true,
                    PrivilegedRoles = ["SuperAdmin", "Admin"]
                },
                SessionRisk = new SessionRiskSettings { Enabled = false }
            },
            user: user);

        var result = await sut.LoginAsync(new LoginRequest("student1", "pass"), "10.0.0.5");

        result.IsSuccess.Should().BeTrue();
        result.Response.Should().NotBeNull();
        result.Response!.MfaEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_WhenPrivilegedOnlyMfaEnabled_AdminWithoutCode_ReturnsMfaRequired()
    {
        var user = new User("admin1", "HASH", roleId: 2);
        SetRole(user, "Admin");
        user.SetMfaEnrollment("KNOWNSECRET", "[]");
        user.EnableMfa();

        var sut = CreateSut(
            new AuthSecurityOptions
            {
                Mfa = new MfaSettings
                {
                    Enabled = true,
                    RequireForPasswordLogin = true,
                    RequireForPrivilegedRolesOnly = true,
                    PrivilegedRoles = ["SuperAdmin", "Admin"]
                },
                SessionRisk = new SessionRiskSettings { Enabled = false }
            },
            user: user);

        var result = await sut.LoginAsync(new LoginRequest("admin1", "pass"), "10.0.0.5");

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be(LoginFailureReason.MfaRequired);
    }

    [Fact]
    public async Task LoginAsync_WhenHighRiskBlocked_ReturnsSessionRiskBlocked()
    {
        var user = new User("student1", "HASH", roleId: 1);

        var sessions = new StubSessionRepository
        {
            MostRecent = new UserSession(user.Id, "old-hash", DateTime.UtcNow.AddDays(1), ipAddress: "10.0.0.1")
        };

        var sut = CreateSut(
            new AuthSecurityOptions
            {
                Mfa = new MfaSettings { Enabled = false, RequireForPasswordLogin = false },
                SessionRisk = new SessionRiskSettings { Enabled = true, BlockHighRiskLogin = true }
            },
            user: user,
            sessionRepo: sessions);

        var result = await sut.LoginAsync(new LoginRequest("student1", "pass"), "10.0.0.99");

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be(LoginFailureReason.SessionRiskBlocked);
    }

    [Fact]
    public async Task LoginAsync_WhenMfaIsRequiredAndTotpIsValid_ReturnsSuccess()
    {
        var user = new User("student1", "HASH", roleId: 1);
        user.SetMfaEnrollment("KNOWNSECRET", "[]");
        user.EnableMfa();

        var sut = CreateSut(
            new AuthSecurityOptions
            {
                Mfa = new MfaSettings { Enabled = true, RequireForPasswordLogin = true, RequireForPrivilegedRolesOnly = false },
                SessionRisk = new SessionRiskSettings { Enabled = false }
            },
            user: user,
            totpService: new StubTotpService(validCodes: ["111111"]));

        var result = await sut.LoginAsync(new LoginRequest("student1", "pass", MfaCode: "111111"), "10.0.0.5");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task LoginAsync_WhenTotpInvalidAndRecoveryCodeValid_ConsumesRecoveryCodeAndReturnsSuccess()
    {
        var validRecoveryCode = "abcde-12345";
        var validRecoveryHash = HashRecoveryCode(validRecoveryCode);
        var user = new User("student1", "HASH", roleId: 1);
        user.SetMfaEnrollment("KNOWNSECRET", $"[\"{validRecoveryHash}\"]");
        user.EnableMfa();

        var sut = CreateSut(
            new AuthSecurityOptions
            {
                Mfa = new MfaSettings { Enabled = true, RequireForPasswordLogin = true, RequireForPrivilegedRolesOnly = false },
                SessionRisk = new SessionRiskSettings { Enabled = false }
            },
            user: user,
            totpService: new StubTotpService(validCodes: []));

        var result = await sut.LoginAsync(new LoginRequest("student1", "pass", MfaCode: validRecoveryCode), "10.0.0.5");

        result.IsSuccess.Should().BeTrue();
        user.MfaRecoveryCodesHashJson.Should().Be("[]");
    }

    [Fact]
    public async Task RefreshAsync_WhenSessionIsPastIdleTimeout_ReturnsNull()
    {
        var user = new User("student1", "HASH", roleId: 1);
        var expiredSession = new UserSession(user.Id, "old-hash", DateTime.UtcNow.AddDays(1), ipAddress: "10.0.0.1");

        var lastActivityProperty = typeof(UserSession).GetProperty("LastActivityAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        lastActivityProperty.Should().NotBeNull();
        lastActivityProperty!.SetValue(expiredSession, DateTime.UtcNow.AddMinutes(-10));

        var sessionRepo = new StubSessionRepository
        {
            ActiveSession = expiredSession
        };

        var sut = CreateSut(
            new AuthSecurityOptions
            {
                SessionTimeout = new SessionTimeoutSettings { Enabled = true, IdleTimeoutMinutes = 5 },
                SessionRisk = new SessionRiskSettings { Enabled = false }
            },
            user: user,
            sessionRepo: sessionRepo);

        var result = await sut.RefreshAsync("refresh-token", "10.0.0.5");

        result.Should().BeNull();
    }

    private static string HashRecoveryCode(string code)
    {
        var normalized = code.Replace("-", string.Empty, StringComparison.Ordinal).Trim().ToUpperInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }

    private static AuthService CreateSut(
        AuthSecurityOptions options,
        User? user = null,
        IUserSessionRepository? sessionRepo = null,
        ITotpService? totpService = null)
    {
        user ??= new User("student1", "HASH", roleId: 1);
        sessionRepo ??= new StubSessionRepository();
        totpService ??= new StubTotpService(validCodes: []);

        return new AuthService(
            new StubUserRepository(user),
            sessionRepo,
            new StubTokenService(),
            new StubPasswordHasher(),
            new StubAuditService(),
            new StubPasswordHistoryRepository(),
            new StubLicenseRepository(),
            totpService,
            new StubTwoFactorStateStore(null),
            Options.Create(options));
    }

    private static void SetRole(User user, string roleName)
    {
        var roleProperty = typeof(User).GetProperty("Role", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        roleProperty.Should().NotBeNull();
        roleProperty!.SetValue(user, new Role(roleName));
    }
}

file sealed class StubUserRepository : IUserRepository
{
    private readonly User _user;

    public StubUserRepository(User user) => _user = user;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult<User?>(_user.Id == id ? _user : null);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => Task.FromResult<User?>(string.Equals(_user.Username, username, StringComparison.OrdinalIgnoreCase) ? _user : null);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult<User?>(null);

    public Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
        => Task.FromResult(string.Equals(_user.Username, username, StringComparison.OrdinalIgnoreCase));

    public Task<IList<User>> GetLockedAccountsAsync(CancellationToken ct = default)
        => Task.FromResult<IList<User>>([]);

    public Task<IList<User>> GetFacultyUsersAsync(CancellationToken ct = default)
        => Task.FromResult<IList<User>>([]);

    public Task<IList<User>> GetActiveUsersByRolesAsync(IReadOnlyList<string> roleNames, CancellationToken ct = default)
        => Task.FromResult<IList<User>>([]);

    public Task<IList<User>> GetUsersByRolesAsync(IReadOnlyList<string> roleNames, bool includeInactive = false, CancellationToken ct = default)
        => Task.FromResult<IList<User>>([]);

    public Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken ct = default)
        => Task.FromResult<Role?>(null);

    public Task AddAsync(User user, CancellationToken ct = default) => Task.CompletedTask;

    public Task AddRangeAsync(IEnumerable<User> users, CancellationToken ct = default) => Task.CompletedTask;

    public void Update(User user)
    {
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => Task.FromResult(1);
}

file sealed class StubSessionRepository : IUserSessionRepository
{
    public UserSession? MostRecent { get; set; }
    public UserSession? ActiveSession { get; set; }

    public Task<UserSession?> GetActiveByHashAsync(string tokenHash, CancellationToken ct = default)
        => Task.FromResult<UserSession?>(ActiveSession ?? MostRecent);

    public Task<UserSession?> GetMostRecentByUserIdAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(MostRecent);

    public Task AddAsync(UserSession session, CancellationToken ct = default)
    {
        MostRecent = session;
        return Task.CompletedTask;
    }

    public void Update(UserSession session)
    {
        MostRecent = session;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => Task.FromResult(1);

    public Task<int> CountActiveSessionsAsync(CancellationToken ct = default)
        => Task.FromResult(0);

    public Task<UserSession?> GetByIdAsync(Guid sessionId, CancellationToken ct = default)
        => Task.FromResult<UserSession?>(null);

    public Task<IList<UserSession>> GetActiveSessionsAsync(CancellationToken ct = default)
        => Task.FromResult<IList<UserSession>>([]);

    public Task<IList<UserSession>> GetActiveSessionsByUserIdAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult<IList<UserSession>>([]);

    public Task<IList<UserSession>> GetIdleSessionsAsync(int idleTimeoutMinutes, CancellationToken ct = default)
        => Task.FromResult<IList<UserSession>>([]);
}

file sealed class StubTokenService : ITokenService
{
    public string GenerateAccessToken(User user) => "ACCESS";

    public string GenerateRefreshToken() => "REFRESH";

    public string HashRefreshToken(string rawToken) => "HASHED";

    public DateTime GetRefreshTokenExpiry() => DateTime.UtcNow.AddDays(7);
}

file sealed class StubPasswordHasher : IPasswordHasher
{
    public string Hash(string input) => "HASH";

    public bool Verify(string hash, string input) => hash == "HASH" && input == "pass";
}

file sealed class StubAuditService : IAuditService
{
    public Task LogAsync(AuditLog entry, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(
        string? query = null,
        Guid? actorUserId = null,
        string? action = null,
        string? entityName = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int page = 1,
        int pageSize = 50,
        string? actorRole = null,
        string? severity = null,
        string? eventCategory = null,
        string? correlationId = null,
        CancellationToken ct = default)
        => Task.FromResult(((IReadOnlyList<AuditLog>)[], 0));
}

file sealed class StubPasswordHistoryRepository : IPasswordHistoryRepository
{
    public Task<IList<PasswordHistoryEntry>> GetRecentAsync(Guid userId, int count, CancellationToken ct = default)
        => Task.FromResult<IList<PasswordHistoryEntry>>([]);

    public Task AddAsync(PasswordHistoryEntry entry, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SaveChangesAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}

file sealed class StubLicenseRepository : ILicenseRepository
{
    public Task<LicenseState?> GetCurrentAsync(CancellationToken ct = default)
        => Task.FromResult<LicenseState?>(null);

    public Task AddAsync(LicenseState state, CancellationToken ct = default)
        => Task.CompletedTask;

    public void Update(LicenseState state)
    {
    }

    public Task<bool> IsVerificationKeyConsumedAsync(string keyHash, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task AddConsumedKeyAsync(ConsumedVerificationKey key, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => Task.FromResult(1);
}

file sealed class StubTotpService : ITotpService
{
    private readonly HashSet<string> _validCodes;

    public StubTotpService(IEnumerable<string> validCodes)
    {
        _validCodes = validCodes.ToHashSet(StringComparer.Ordinal);
    }

    public string GenerateSecret() => "KNOWNSECRET";

    public string BuildProvisioningUri(string issuer, string accountName, string secret, int digits, int stepSeconds)
        => $"otpauth://totp/{issuer}:{accountName}?secret={secret}&issuer={issuer}&digits={digits}&period={stepSeconds}";

    public bool ValidateCode(string secret, string code, DateTime utcNow, int digits, int stepSeconds, int allowedDriftWindows)
        => _validCodes.Contains(code);
}

file sealed class StubTwoFactorStateStore : ITwoFactorStateStore
{
    private TwoFactorStateSnapshot? _snapshot;

    public StubTwoFactorStateStore(TwoFactorStateSnapshot? snapshot) => _snapshot = snapshot;

    public Task<TwoFactorStateSnapshot?> GetAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(_snapshot is not null && _snapshot.UserId == userId ? _snapshot : null);

    public Task<bool> SaveSetupAsync(Guid userId, string secretKey, CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<bool> EnableAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<bool> DisableAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<bool> HardDeleteAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(true);
}
