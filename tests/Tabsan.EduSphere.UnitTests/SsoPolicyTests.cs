using System.Reflection;
using FluentAssertions;
using Tabsan.EduSphere.Application.Auth;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Activity;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>Plan M1 Stage M1.4: SSO-only policy, linked-account management and audit.</summary>
public class SsoPolicyTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();

    // ── SsoLoginPolicy ───────────────────────────────────────────────────────

    [Fact]
    public async Task BlocksPasswordLogin_WhenEnforcingProviderCoversUsersTenant()
    {
        var repo = new Repo();
        repo.Settings.Add(Enforcing(TenantA));

        (await Policy(repo).IsPasswordLoginBlockedAsync(UserIn(TenantA, "Student"))).Should().BeTrue();
        (await Policy(repo).IsPasswordLoginBlockedAsync(UserIn(TenantB, "Student"))).Should().BeFalse("another tenant's policy does not apply");
    }

    [Fact]
    public async Task InstallationWideEnforcement_AppliesToEveryTenant()
    {
        var repo = new Repo();
        repo.Settings.Add(Enforcing(null));

        (await Policy(repo).IsPasswordLoginBlockedAsync(UserIn(TenantB, "Faculty"))).Should().BeTrue();
        (await Policy(repo).IsPasswordLoginBlockedAsync(UserIn(null, "Admin"))).Should().BeTrue();
    }

    [Fact]
    public async Task SuperAdmin_IsAlwaysExempt()
    {
        var repo = new Repo();
        repo.Settings.Add(Enforcing(null));

        (await Policy(repo).IsPasswordLoginBlockedAsync(UserIn(null, "SuperAdmin"))).Should().BeFalse();
    }

    [Fact]
    public async Task NotEnforced_WhenSsoModuleInactive()
    {
        var repo = new Repo();
        repo.Settings.Add(Enforcing(null));

        (await Policy(repo, moduleActive: false).IsPasswordLoginBlockedAsync(UserIn(null, "Student"))).Should().BeFalse(
            "a lapsed SSO licence must not lock everyone out");
    }

    [Fact]
    public async Task NotEnforced_WhenProviderDisabledOrNotFlagged()
    {
        var repo = new Repo();
        var disabled = Enforcing(TenantA);
        disabled.Disable();
        repo.Settings.Add(disabled);
        var notFlagged = new SsoProviderSetting(TenantA, SsoProvider.Saml, "SAML");
        notFlagged.ConfigureSaml("https://idp.uni.edu/md");
        notFlagged.Enable();
        repo.Settings.Add(notFlagged);

        (await Policy(repo).IsPasswordLoginBlockedAsync(UserIn(TenantA, "Student"))).Should().BeFalse();
    }

    // ── SsoAccountService ────────────────────────────────────────────────────

    [Fact]
    public async Task Unlink_RemovesLinkAndAudits()
    {
        var (service, repo, audit, user) = AccountHarness(enforcing: false);
        repo.Links.Add(new ExternalLogin(null, user.Id, SsoProvider.Google, "g-1", "ali@uni.edu"));
        var admin = Guid.NewGuid();

        var result = await service.UnlinkAsync(user.Id, "google", admin, isSelfService: false, "10.0.0.1");

        result.Should().Be(SsoUnlinkResult.Unlinked);
        repo.Links.Should().BeEmpty();
        var entry = audit.Should().ContainSingle(a => a.Action == "SsoAccountUnlinked").Which;
        entry.ActorUserId.Should().Be(admin);
        entry.EntityId.Should().Be(user.Id.ToString());
        entry.OldValuesJson.Should().Contain("Google");
    }

    [Fact]
    public async Task SelfUnlink_OfOnlyLinkUnderSsoOnly_IsRefused_ButAdminMayUnlink()
    {
        var (service, repo, audit, user) = AccountHarness(enforcing: true);
        repo.Links.Add(new ExternalLogin(null, user.Id, SsoProvider.Microsoft, "m-1", "ali@uni.edu"));

        (await service.UnlinkAsync(user.Id, "Microsoft", user.Id, isSelfService: true, null)).Should().Be(SsoUnlinkResult.WouldLockOut);
        repo.Links.Should().ContainSingle();
        audit.Should().BeEmpty();

        (await service.UnlinkAsync(user.Id, "Microsoft", Guid.NewGuid(), isSelfService: false, null)).Should().Be(SsoUnlinkResult.Unlinked);
    }

    [Fact]
    public async Task SelfUnlink_AllowedWhenAnotherLinkRemains()
    {
        var (service, repo, _, user) = AccountHarness(enforcing: true);
        repo.Links.Add(new ExternalLogin(null, user.Id, SsoProvider.Microsoft, "m-1", null));
        repo.Links.Add(new ExternalLogin(null, user.Id, SsoProvider.Saml, "s-1", null));

        (await service.UnlinkAsync(user.Id, "Microsoft", user.Id, isSelfService: true, null)).Should().Be(SsoUnlinkResult.Unlinked);
    }

    [Fact]
    public async Task Unlink_UnknownProviderOrUserOrLink_IsNotFound()
    {
        var (service, _, _, user) = AccountHarness(enforcing: false);

        (await service.UnlinkAsync(user.Id, "Nope", user.Id, true, null)).Should().Be(SsoUnlinkResult.NotFound);
        (await service.UnlinkAsync(user.Id, "Google", user.Id, true, null)).Should().Be(SsoUnlinkResult.NotFound);
        (await service.UnlinkAsync(Guid.NewGuid(), "Google", user.Id, false, null)).Should().Be(SsoUnlinkResult.NotFound, "users outside the caller's scope are invisible");
    }

    [Fact]
    public async Task GetLinks_ReturnsSummaries()
    {
        var (service, repo, _, user) = AccountHarness(enforcing: false);
        repo.Links.Add(new ExternalLogin(null, user.Id, SsoProvider.Google, "g-1", "ali@uni.edu"));

        var links = await service.GetLinksAsync(user.Id);

        links.Should().ContainSingle(l => l.Provider == "Google" && l.Email == "ali@uni.edu");
    }

    [Fact]
    public void LoginActivity_RecordsAuthMethod()
    {
        var entry = new LoginActivityLog(null, "ali", DateTime.UtcNow, null, null, null, true, null, "low", false, "sso:Google");

        entry.AuthMethod.Should().Be("sso:Google");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static SsoProviderSetting Enforcing(Guid? tenantId)
    {
        var s = new SsoProviderSetting(tenantId, SsoProvider.Microsoft, "Microsoft");
        s.ConfigureOidc("client", "https://login.microsoftonline.com/contoso.edu/v2.0");
        s.SetEncryptedClientSecret("enc");
        s.SetPolicy(false, enforceSsoOnly: true, false, null);
        s.Enable();
        return s;
    }

    private static User UserIn(Guid? tenantId, string role)
    {
        var user = new User("u" + Guid.NewGuid().ToString("N")[..6], "hash", 1, "u@uni.edu",
            tenantId: tenantId, campusId: tenantId is null ? null : Guid.NewGuid());
        typeof(User).GetProperty(nameof(User.Role))!.SetValue(user, new Role(role));
        return user;
    }

    private static SsoLoginPolicy Policy(Repo repo, bool moduleActive = true)
        => new(repo, SsoSignInTests.Proxy<IModuleEntitlementResolver>.Create(new()
        {
            ["IsActiveAsync"] = a => Task.FromResult((string)a[0]! == "sso" && moduleActive)
        }));

    private static (SsoAccountService Service, Repo Repo, List<AuditLog> Audit, User User) AccountHarness(bool enforcing)
    {
        var repo = new Repo();
        if (enforcing) repo.Settings.Add(Enforcing(null));
        var user = UserIn(null, "Student");
        var audit = new List<AuditLog>();
        var users = SsoSignInTests.Proxy<IUserRepository>.Create(new()
        {
            ["GetByIdAsync"] = a => Task.FromResult((Guid)a[0]! == user.Id ? user : null)
        });
        var auditService = SsoSignInTests.Proxy<IAuditService>.Create(new()
        {
            ["LogAsync"] = a => { audit.Add((AuditLog)a[0]!); return Task.CompletedTask; }
        });
        return (new SsoAccountService(repo, users, Policy(repo), auditService), repo, audit, user);
    }

    private sealed class Repo : ISsoRepository
    {
        public readonly List<SsoProviderSetting> Settings = new();
        public readonly List<ExternalLogin> Links = new();

        public Task<Guid?> GetTenantIdByCodeAsync(string tenantCode, CancellationToken ct = default) => Task.FromResult<Guid?>(null);
        public Task<SsoProviderSetting?> GetSettingAsync(Guid? tenantId, SsoProvider provider, CancellationToken ct = default)
            => Task.FromResult(Settings.FirstOrDefault(s => s.TenantId == tenantId && s.Provider == provider));
        public Task<IReadOnlyList<SsoProviderSetting>> GetEnabledSettingsAsync(Guid? tenantId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SsoProviderSetting>>(Settings.Where(s => s.TenantId == tenantId && s.IsEnabled).ToList());
        public Task<ExternalLogin?> FindLinkAsync(Guid? tenantId, SsoProvider provider, string externalSubjectId, CancellationToken ct = default)
            => Task.FromResult(Links.FirstOrDefault(l => l.Provider == provider && l.ExternalSubjectId == externalSubjectId));
        public Task<bool> UserHasLinkAsync(Guid userId, SsoProvider provider, CancellationToken ct = default)
            => Task.FromResult(Links.Any(l => l.UserId == userId && l.Provider == provider));
        public Task AddLinkAsync(ExternalLogin link, CancellationToken ct = default) { Links.Add(link); return Task.CompletedTask; }
        public Task<IReadOnlyList<Guid>> FindUserIdsByInstitutionIdAsync(Guid? tenantId, string institutionId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<IReadOnlyList<SsoProviderSetting>> GetEnforcingSettingsAsync(Guid? tenantId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SsoProviderSetting>>(Settings.Where(s => s.IsEnabled && s.EnforceSsoOnly && (s.TenantId == null || s.TenantId == tenantId)).ToList());
        public Task<IReadOnlyList<ExternalLogin>> GetLinksForUserAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ExternalLogin>>(Links.Where(l => l.UserId == userId).ToList());
        public void RemoveLink(ExternalLogin link) => Links.Remove(link);
        public Task<IReadOnlyList<(SsoProviderSetting Setting, string? TenantCode)>> GetAllSettingsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<(SsoProviderSetting, string?)>>(Settings.Select(s => (s, (string?)null)).ToList());
        public Task AddSettingAsync(SsoProviderSetting setting, CancellationToken ct = default) { Settings.Add(setting); return Task.CompletedTask; }
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
    }
}
