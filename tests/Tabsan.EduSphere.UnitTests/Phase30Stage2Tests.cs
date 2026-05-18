using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Services;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Modules;
using Tabsan.EduSphere.Domain.Settings;

namespace Tabsan.EduSphere.UnitTests;

// Final-Touches Phase 30 Stage 30.2 — tenant operations settings tests.
public class Phase30Stage2Tests
{
    [Fact]
    public async Task GetSubscriptionPlanAsync_ReturnsDefaults_WhenSettingsAreMissing()
    {
        var repo = new InMemorySettingsRepository();
        var sut = new TenantOperationsService(repo, CreateDistributedCache());

        var plan = await sut.GetSubscriptionPlanAsync();

        plan.PlanKey.Should().Be("standard");
        plan.MaxUsers.Should().Be(1000);
        plan.EnableIntegrations.Should().BeTrue();
        plan.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SaveSubscriptionPlanAsync_PersistsValues_InPortalSettingsStore()
    {
        var repo = new InMemorySettingsRepository();
        var sut = new TenantOperationsService(repo, CreateDistributedCache());

        await sut.SaveSubscriptionPlanAsync(new SaveTenantSubscriptionPlanCommand(
            PlanKey: "enterprise",
            PlanName: "Enterprise",
            MonthlyPrice: 499.99m,
            MaxUsers: 8000,
            EnableLms: true,
            EnablePayments: true,
            EnableIntegrations: true,
            IsActive: true));

        var plan = await sut.GetSubscriptionPlanAsync();
        plan.PlanKey.Should().Be("enterprise");
        plan.PlanName.Should().Be("Enterprise");
        plan.MonthlyPrice.Should().Be(499.99m);
        plan.MaxUsers.Should().Be(8000);
    }

    [Fact]
    public async Task SaveAndGetTenantProfileAsync_RoundTripsValues()
    {
        var repo = new InMemorySettingsRepository();
        var sut = new TenantOperationsService(repo, CreateDistributedCache());

        await sut.SaveTenantProfileAsync(new SaveTenantProfileSettingsCommand(
            TenantCode: "north-campus",
            TenantDisplayName: "North Campus",
            SupportEmail: "support@north-campus.edu",
            SupportPhone: "+1-555-0101",
            TimeZone: "Asia/Karachi",
            Locale: "en-PK",
            CurrencyCode: "PKR",
            BrandingTheme: "north-theme"));

        var profile = await sut.GetTenantProfileAsync();
        profile.TenantCode.Should().Be("north-campus");
        profile.TenantDisplayName.Should().Be("North Campus");
        profile.CurrencyCode.Should().Be("PKR");
        profile.BrandingTheme.Should().Be("north-theme");
    }

    private static IDistributedCache CreateDistributedCache()
        => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}

file sealed class InMemorySettingsRepository : ISettingsRepository
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
