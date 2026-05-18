using FluentAssertions;
using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Services;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Modules;
using Tabsan.EduSphere.Domain.Settings;

namespace Tabsan.EduSphere.UnitTests;

// Final-Touches Phase 30 Stage 30.3 — feature flag reliability and rollback tests.
public class Phase30Stage3Tests
{
    [Fact]
    public async Task GetAsync_ShouldReturnDefaultEnabled_ForTenantOperationsWrite()
    {
        var sut = new FeatureFlagService(new FeatureFlagRepoStub());

        var flag = await sut.GetAsync("tenant-operations.write");

        flag.IsEnabled.Should().BeTrue();
        flag.Key.Should().Be("tenant-operations.write");
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistValueAndDescription()
    {
        var repo = new FeatureFlagRepoStub();
        var sut = new FeatureFlagService(repo);

        await sut.SaveAsync(new SaveFeatureFlagCommand("tenant-operations.write", false, "Disable writes during incident"));

        var flag = await sut.GetAsync("tenant-operations.write");
        flag.IsEnabled.Should().BeFalse();
        flag.Description.Should().Be("Disable writes during incident");
    }

    [Fact]
    public async Task RollbackAsync_ShouldDisableSpecifiedFlags()
    {
        var repo = new FeatureFlagRepoStub();
        var sut = new FeatureFlagService(repo);

        await sut.SaveAsync(new SaveFeatureFlagCommand("integration-gateway.enabled", true));
        await sut.RollbackAsync(new RollbackFeatureFlagsCommand(["integration-gateway.enabled"], "Emergency rollback"));

        var flag = await sut.GetAsync("integration-gateway.enabled");
        flag.IsEnabled.Should().BeFalse();
    }
}

file sealed class FeatureFlagRepoStub : ISettingsRepository
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
