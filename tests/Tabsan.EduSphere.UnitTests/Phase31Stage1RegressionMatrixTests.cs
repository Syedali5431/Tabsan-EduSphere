using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Modules;
using Tabsan.EduSphere.Application.Services;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Modules;
using Tabsan.EduSphere.Domain.Settings;

namespace Tabsan.EduSphere.UnitTests;

// Final-Touches Phase 31 Stage 31.1 — regression matrix coverage baseline.
public class Phase31Stage1RegressionMatrixTests
{
    [Theory]
    [InlineData("SuperAdmin", "University", true)]
    [InlineData("SuperAdmin", "University", false)]
    [InlineData("SuperAdmin", "School", true)]
    [InlineData("SuperAdmin", "School", false)]
    [InlineData("SuperAdmin", "College", true)]
    [InlineData("SuperAdmin", "College", false)]
    [InlineData("Admin", "University", true)]
    [InlineData("Admin", "University", false)]
    [InlineData("Admin", "School", true)]
    [InlineData("Admin", "School", false)]
    [InlineData("Admin", "College", true)]
    [InlineData("Admin", "College", false)]
    [InlineData("Faculty", "University", true)]
    [InlineData("Faculty", "University", false)]
    [InlineData("Faculty", "School", true)]
    [InlineData("Faculty", "School", false)]
    [InlineData("Faculty", "College", true)]
    [InlineData("Faculty", "College", false)]
    [InlineData("Student", "University", true)]
    [InlineData("Student", "University", false)]
    [InlineData("Student", "School", true)]
    [InlineData("Student", "School", false)]
    [InlineData("Student", "College", true)]
    [InlineData("Student", "College", false)]
    public async Task Matrix_RoleModeLicense_Combinations_AreConsistent(
        string role,
        string institutionMode,
        bool aiChatLicensed)
    {
        var policy = institutionMode switch
        {
            "University" => new InstitutionPolicySnapshot(false, false, true),
            "School" => new InstitutionPolicySnapshot(true, false, false),
            "College" => new InstitutionPolicySnapshot(false, true, false),
            _ => InstitutionPolicySnapshot.Default
        };

        var entitlement = new MatrixEntitlementResolver(aiChatLicensed);
        var service = new ModuleRegistryService(entitlement, new MatrixModuleService());

        var modules = await service.GetVisibleModulesAsync(role, policy);

        modules.Should().NotBeEmpty();

        var advancedAudit = modules.Single(m => m.Key == "advanced_audit");
        advancedAudit.IsAccessible.Should().Be(role == "SuperAdmin");

        var fyp = modules.Single(m => m.Key == "fyp");
        var shouldFypBeAccessible = institutionMode == "University";
        fyp.IsAccessible.Should().Be(shouldFypBeAccessible);

        var aiChat = modules.Single(m => m.Key == "ai_chat");
        aiChat.IsActive.Should().Be(aiChatLicensed);
    }

    [Fact]
    public async Task TenantIsolation_Baseline_SeparateStores_DoNotLeakData()
    {
        var tenantARepo = new MatrixSettingsRepository();
        var tenantBRepo = new MatrixSettingsRepository();

        var tenantA = new TenantOperationsService(tenantARepo, CreateDistributedCache());
        var tenantB = new TenantOperationsService(tenantBRepo, CreateDistributedCache());

        await tenantA.SaveTenantProfileAsync(new SaveTenantProfileSettingsCommand(
            TenantCode: "tenant-a",
            TenantDisplayName: "Tenant A",
            SupportEmail: "a@tenant.test",
            SupportPhone: "+1-111-1111",
            TimeZone: "UTC",
            Locale: "en-US",
            CurrencyCode: "USD",
            BrandingTheme: "a-theme"));

        await tenantB.SaveTenantProfileAsync(new SaveTenantProfileSettingsCommand(
            TenantCode: "tenant-b",
            TenantDisplayName: "Tenant B",
            SupportEmail: "b@tenant.test",
            SupportPhone: "+1-222-2222",
            TimeZone: "UTC",
            Locale: "en-US",
            CurrencyCode: "USD",
            BrandingTheme: "b-theme"));

        var profileA = await tenantA.GetTenantProfileAsync();
        var profileB = await tenantB.GetTenantProfileAsync();

        profileA.TenantCode.Should().Be("tenant-a");
        profileB.TenantCode.Should().Be("tenant-b");
        profileA.TenantCode.Should().NotBe(profileB.TenantCode);
    }

    [Fact]
    public async Task TenantIsolation_SameStore_DifferentTenantScopes_DoNotLeakData()
    {
        var sharedRepo = new MatrixSettingsRepository();

        var tenantA = new TenantOperationsService(sharedRepo, CreateDistributedCache(), new MatrixTenantScopeResolver("tenant-a"));
        var tenantB = new TenantOperationsService(sharedRepo, CreateDistributedCache(), new MatrixTenantScopeResolver("tenant-b"));

        await tenantA.SaveTenantProfileAsync(new SaveTenantProfileSettingsCommand(
            TenantCode: "tenant-a",
            TenantDisplayName: "Tenant A",
            SupportEmail: "a@tenant.test",
            SupportPhone: "+1-111-1111",
            TimeZone: "UTC",
            Locale: "en-US",
            CurrencyCode: "USD",
            BrandingTheme: "a-theme"));

        await tenantB.SaveTenantProfileAsync(new SaveTenantProfileSettingsCommand(
            TenantCode: "tenant-b",
            TenantDisplayName: "Tenant B",
            SupportEmail: "b@tenant.test",
            SupportPhone: "+1-222-2222",
            TimeZone: "UTC",
            Locale: "en-US",
            CurrencyCode: "USD",
            BrandingTheme: "b-theme"));

        var profileA = await tenantA.GetTenantProfileAsync();
        var profileB = await tenantB.GetTenantProfileAsync();

        profileA.TenantCode.Should().Be("tenant-a");
        profileB.TenantCode.Should().Be("tenant-b");
        profileA.TenantCode.Should().NotBe(profileB.TenantCode);
    }

    private static IDistributedCache CreateDistributedCache()
        => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}

file sealed class MatrixEntitlementResolver : IModuleEntitlementResolver
{
    private readonly bool _aiChatLicensed;

    public MatrixEntitlementResolver(bool aiChatLicensed)
    {
        _aiChatLicensed = aiChatLicensed;
    }

    public Task<bool> IsActiveAsync(string moduleKey, CancellationToken ct = default)
    {
        if (string.Equals(moduleKey, "ai_chat", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(_aiChatLicensed);

        return Task.FromResult(true);
    }

    public void InvalidateCache(string moduleKey)
    {
    }
}

file sealed class MatrixModuleService : IModuleService
{
    public Task<IReadOnlyList<Module>> GetAllAsync(CancellationToken ct = default)
    {
        var modules = ModuleRegistry.All()
            .Select(d => new Module(d.Key, d.Key, isMandatory: false))
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<Module>>(modules);
    }

    public Task ActivateAsync(string moduleKey, Guid changedByUserId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task DeactivateAsync(string moduleKey, Guid changedByUserId, CancellationToken ct = default)
        => Task.CompletedTask;
}

file sealed class MatrixSettingsRepository : ISettingsRepository
{
    private readonly Dictionary<string, string> _settings = new(StringComparer.OrdinalIgnoreCase);

    public Task<Dictionary<string, string>> GetAllPortalSettingsAsync(CancellationToken ct = default)
        => Task.FromResult(new Dictionary<string, string>(_settings, StringComparer.OrdinalIgnoreCase));

    public Task<string?> GetPortalSettingAsync(string key, CancellationToken ct = default)
    {
        _settings.TryGetValue(key, out var value);
        return Task.FromResult<string?>(value);
    }

    public Task UpsertPortalSettingAsync(string key, string value, CancellationToken ct = default)
    {
        _settings[key] = value;
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);

    public Task<IList<ReportDefinition>> GetAllReportsAsync(CancellationToken ct = default) => Task.FromResult<IList<ReportDefinition>>(Array.Empty<ReportDefinition>());
    public Task<ReportDefinition?> GetReportByKeyAsync(string key, CancellationToken ct = default) => Task.FromResult<ReportDefinition?>(null);
    public Task<ReportDefinition?> GetReportByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<ReportDefinition?>(null);
    public Task AddReportAsync(ReportDefinition report, CancellationToken ct = default) => Task.CompletedTask;
    public void UpdateReport(ReportDefinition report) { }
    public Task AddReportRoleAsync(ReportRoleAssignment assignment, CancellationToken ct = default) => Task.CompletedTask;
    public void RemoveReportRole(ReportRoleAssignment assignment) { }
    public Task<ReportRoleAssignment?> GetReportRoleAsync(Guid reportId, string roleName, CancellationToken ct = default) => Task.FromResult<ReportRoleAssignment?>(null);
    public Task<IList<ModuleRoleAssignment>> GetModuleRolesAsync(Guid moduleId, CancellationToken ct = default) => Task.FromResult<IList<ModuleRoleAssignment>>(Array.Empty<ModuleRoleAssignment>());
    public Task<IList<ModuleRoleAssignment>> GetAllModuleRolesAsync(CancellationToken ct = default) => Task.FromResult<IList<ModuleRoleAssignment>>(Array.Empty<ModuleRoleAssignment>());
    public Task AddModuleRoleAsync(ModuleRoleAssignment assignment, CancellationToken ct = default) => Task.CompletedTask;
    public void RemoveModuleRole(ModuleRoleAssignment assignment) { }
    public Task<ModuleRoleAssignment?> GetModuleRoleAsync(Guid moduleId, string roleName, CancellationToken ct = default) => Task.FromResult<ModuleRoleAssignment?>(null);
    public Task<IList<SidebarMenuItem>> GetTopLevelMenusAsync(CancellationToken ct = default) => Task.FromResult<IList<SidebarMenuItem>>(Array.Empty<SidebarMenuItem>());
    public Task<IList<SidebarMenuItem>> GetSubMenusAsync(Guid parentId, CancellationToken ct = default) => Task.FromResult<IList<SidebarMenuItem>>(Array.Empty<SidebarMenuItem>());
    public Task<SidebarMenuItem?> GetMenuByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<SidebarMenuItem?>(null);
    public Task<SidebarMenuItem?> GetMenuByKeyAsync(string key, CancellationToken ct = default) => Task.FromResult<SidebarMenuItem?>(null);
    public Task<IList<SidebarMenuItem>> GetVisibleMenusForRoleAsync(string roleName, CancellationToken ct = default) => Task.FromResult<IList<SidebarMenuItem>>(Array.Empty<SidebarMenuItem>());
    public Task AddMenuAsync(SidebarMenuItem item, CancellationToken ct = default) => Task.CompletedTask;
    public void UpdateMenu(SidebarMenuItem item) { }
    public Task AddMenuRoleAccessAsync(SidebarMenuRoleAccess access, CancellationToken ct = default) => Task.CompletedTask;
    public void RemoveMenuRoleAccess(SidebarMenuRoleAccess access) { }
    public Task<SidebarMenuRoleAccess?> GetMenuRoleAccessAsync(Guid menuItemId, string roleName, CancellationToken ct = default) => Task.FromResult<SidebarMenuRoleAccess?>(null);
}

file sealed class MatrixTenantScopeResolver : ITenantScopeResolver
{
    private readonly string _scope;

    public MatrixTenantScopeResolver(string scope)
    {
        _scope = scope;
    }

    public string? GetTenantScopeKey() => _scope;
}
