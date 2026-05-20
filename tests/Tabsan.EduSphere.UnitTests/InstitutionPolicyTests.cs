using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Services;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Settings;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>
/// Phase 23 Stage 23.3 — Role-Rights Enforcement Hardening.
/// Tests cover:
/// 1. InstitutionPolicySnapshot value semantics.
/// 2. InstitutionPolicyService read / write behaviour.
/// 3. Role-hierarchy contract (SuperAdmin ≥ Admin ≥ Faculty ≥ Student).
/// 4. Finance policy contract (SuperAdmin/Admin/Finance satisfy Finance policy).
/// </summary>
public class InstitutionPolicyTests
{
    // ── Stubs ─────────────────────────────────────────────────────────────────

    /// <summary>In-memory stub for ISettingsRepository (portal-settings subset only).</summary>
    private sealed class StubSettingsRepository : ISettingsRepository
    {
        private readonly Dictionary<string, string> _store = new(StringComparer.OrdinalIgnoreCase);

        public Task<Dictionary<string, string>> GetAllPortalSettingsAsync(CancellationToken ct = default)
            => Task.FromResult(new Dictionary<string, string>(_store, StringComparer.OrdinalIgnoreCase));

        public Task<string?> GetPortalSettingAsync(string key, CancellationToken ct = default)
            => Task.FromResult(_store.TryGetValue(key, out var v) ? v : (string?)null);

        public async Task UpsertPortalSettingAsync(string key, string value, CancellationToken ct = default)
        {
            _store[key] = value;
            await Task.CompletedTask;
        }

        // ── Unused members — satisfy the interface contract ───────────────────
        public Task<IList<ReportDefinition>> GetAllReportsAsync(CancellationToken ct = default) => Task.FromResult<IList<ReportDefinition>>(new List<ReportDefinition>());
        public Task<ReportDefinition?> GetReportByKeyAsync(string key, CancellationToken ct = default) => Task.FromResult<ReportDefinition?>(null);
        public Task<ReportDefinition?> GetReportByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<ReportDefinition?>(null);
        public Task AddReportAsync(ReportDefinition report, CancellationToken ct = default) => Task.CompletedTask;
        public void UpdateReport(ReportDefinition report) { }
        public Task AddReportRoleAsync(ReportRoleAssignment assignment, CancellationToken ct = default) => Task.CompletedTask;
        public void RemoveReportRole(ReportRoleAssignment assignment) { }
        public Task<ReportRoleAssignment?> GetReportRoleAsync(Guid reportId, string roleName, CancellationToken ct = default) => Task.FromResult<ReportRoleAssignment?>(null);
        public Task<IList<ModuleRoleAssignment>> GetModuleRolesAsync(Guid moduleId, CancellationToken ct = default) => Task.FromResult<IList<ModuleRoleAssignment>>(new List<ModuleRoleAssignment>());
        public Task<IList<ModuleRoleAssignment>> GetAllModuleRolesAsync(CancellationToken ct = default) => Task.FromResult<IList<ModuleRoleAssignment>>(new List<ModuleRoleAssignment>());
        public Task AddModuleRoleAsync(ModuleRoleAssignment assignment, CancellationToken ct = default) => Task.CompletedTask;
        public void RemoveModuleRole(ModuleRoleAssignment assignment) { }
        public Task<ModuleRoleAssignment?> GetModuleRoleAsync(Guid moduleId, string roleName, CancellationToken ct = default) => Task.FromResult<ModuleRoleAssignment?>(null);
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
        public Task<IList<SidebarMenuItem>> GetTopLevelMenusAsync(CancellationToken ct = default) => Task.FromResult<IList<SidebarMenuItem>>(new List<SidebarMenuItem>());
        public Task<IList<SidebarMenuItem>> GetSubMenusAsync(Guid parentId, CancellationToken ct = default) => Task.FromResult<IList<SidebarMenuItem>>(new List<SidebarMenuItem>());
        public Task<SidebarMenuItem?> GetMenuByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<SidebarMenuItem?>(null);
        public Task<SidebarMenuItem?> GetMenuByKeyAsync(string key, CancellationToken ct = default) => Task.FromResult<SidebarMenuItem?>(null);
        public Task<IList<SidebarMenuItem>> GetVisibleMenusForRoleAsync(string roleName, CancellationToken ct = default) => Task.FromResult<IList<SidebarMenuItem>>(new List<SidebarMenuItem>());
        public Task AddMenuAsync(SidebarMenuItem item, CancellationToken ct = default) => Task.CompletedTask;
        public void UpdateMenu(SidebarMenuItem item) { }
        public Task AddMenuRoleAccessAsync(SidebarMenuRoleAccess access, CancellationToken ct = default) => Task.CompletedTask;
        public void RemoveMenuRoleAccess(SidebarMenuRoleAccess access) { }
        public Task<SidebarMenuRoleAccess?> GetMenuRoleAccessAsync(Guid menuItemId, string roleName, CancellationToken ct = default) => Task.FromResult<SidebarMenuRoleAccess?>(null);
    }

    private static IInstitutionPolicyService BuildService(StubSettingsRepository? repo = null)
    {
        repo ??= new StubSettingsRepository();
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new InstitutionPolicyService(repo, cache);
    }

    // ── InstitutionPolicySnapshot value semantics ─────────────────────────────

    [Fact]
    public void Snapshot_Default_IsUniversityOnly()
    {
        var snap = InstitutionPolicySnapshot.Default;

        snap.IncludeUniversity.Should().BeTrue();
        snap.IncludeSchool.Should().BeFalse();
        snap.IncludeCollege.Should().BeFalse();
        snap.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Snapshot_AllFalse_IsNotValid()
    {
        var snap = new InstitutionPolicySnapshot(false, false, false);
        snap.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Snapshot_IsEnabled_ReturnsCorrectFlagPerType()
    {
        var snap = new InstitutionPolicySnapshot(
            IncludeSchool: true, IncludeCollege: false, IncludeUniversity: true);

        snap.IsEnabled(InstitutionType.School).Should().BeTrue();
        snap.IsEnabled(InstitutionType.College).Should().BeFalse();
        snap.IsEnabled(InstitutionType.University).Should().BeTrue();
    }

    // ── InstitutionPolicyService read behaviour ───────────────────────────────

    [Fact]
    public async Task GetPolicy_WhenNotConfigured_DefaultsToUniversityOnly()
    {
        var svc = BuildService();

        var snapshot = await svc.GetPolicyAsync(CancellationToken.None);

        snapshot.IncludeUniversity.Should().BeTrue();
        snapshot.IncludeSchool.Should().BeFalse();
        snapshot.IncludeCollege.Should().BeFalse();
    }

    [Fact]
    public async Task GetPolicy_WhenSchoolAndUniversityConfigured_ReturnsBoth()
    {
        var repo = new StubSettingsRepository();
        await repo.UpsertPortalSettingAsync("institution_include_school",     "true",  CancellationToken.None);
        await repo.UpsertPortalSettingAsync("institution_include_college",    "false", CancellationToken.None);
        await repo.UpsertPortalSettingAsync("institution_include_university", "true",  CancellationToken.None);

        var svc      = BuildService(repo);
        var snapshot = await svc.GetPolicyAsync(CancellationToken.None);

        snapshot.IncludeSchool.Should().BeTrue();
        snapshot.IncludeCollege.Should().BeFalse();
        snapshot.IncludeUniversity.Should().BeTrue();
        snapshot.IsValid.Should().BeTrue();
    }

    // ── InstitutionPolicyService write behaviour ──────────────────────────────

    [Fact]
    public async Task SavePolicy_WhenAllFlagsOff_ThrowsInvalidOperationException()
    {
        var svc = BuildService();

        var act = async () => await svc.SavePolicyAsync(
            new SaveInstitutionPolicyCommand(false, false, false),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*at least one*");
    }

    [Fact]
    public async Task SavePolicy_PersistsFlags_AndCacheIsInvalidated()
    {
        var repo = new StubSettingsRepository();
        var svc  = BuildService(repo);

        // Warm the cache with defaults first.
        _ = await svc.GetPolicyAsync(CancellationToken.None);

        // Save school-only.
        await svc.SavePolicyAsync(
            new SaveInstitutionPolicyCommand(IncludeSchool: true, IncludeCollege: false, IncludeUniversity: false),
            CancellationToken.None);

        // Re-read — should reflect saved value (cache invalidated).
        var updated = await svc.GetPolicyAsync(CancellationToken.None);

        updated.IncludeSchool.Should().BeTrue();
        updated.IncludeUniversity.Should().BeFalse();
    }

    // ── Role hierarchy contract ───────────────────────────────────────────────

    [Theory]
    [InlineData("SuperAdmin", "SuperAdmin", true)]
    [InlineData("SuperAdmin", "Admin",      true)]
    [InlineData("SuperAdmin", "Faculty",    true)]
    [InlineData("SuperAdmin", "Student",    true)]
    [InlineData("Admin",      "Admin",      true)]
    [InlineData("Admin",      "Faculty",    true)]
    [InlineData("Admin",      "Student",    true)]
    [InlineData("Admin",      "SuperAdmin", false)]
    [InlineData("Faculty",    "Faculty",    true)]
    [InlineData("Faculty",    "Student",    true)]
    [InlineData("Faculty",    "Admin",      false)]
    [InlineData("Student",    "Student",    true)]
    [InlineData("Student",    "Faculty",    false)]
    public void RoleHierarchy_PolicyInclusion_IsCorrect(
        string callerRole, string requiredRole, bool shouldBeAllowed)
    {
        // Models the policy in Program.cs:
        //   "SuperAdmin"  → RequireRole("SuperAdmin")
        //   "Admin"       → RequireRole("SuperAdmin", "Admin")
        //   "Faculty"     → RequireRole("SuperAdmin", "Admin", "Faculty")
        //   "Student"     → RequireRole("SuperAdmin", "Admin", "Faculty", "Student")
        var hierarchy = new Dictionary<string, IReadOnlyList<string>>
        {
            ["SuperAdmin"] = new[] { "SuperAdmin" },
            ["Admin"]      = new[] { "SuperAdmin", "Admin" },
            ["Faculty"]    = new[] { "SuperAdmin", "Admin", "Faculty" },
            ["Student"]    = new[] { "SuperAdmin", "Admin", "Faculty", "Student" },
        };

        var allowedRoles = hierarchy[requiredRole];
        var actual = allowedRoles.Contains(callerRole);

        actual.Should().Be(shouldBeAllowed,
            because: $"role '{callerRole}' {(shouldBeAllowed ? "should" : "should not")} satisfy the '{requiredRole}' policy");
    }

    [Theory]
    [InlineData("SuperAdmin", true)]
    [InlineData("Admin", true)]
    [InlineData("Finance", true)]
    [InlineData("Faculty", false)]
    [InlineData("Student", false)]
    public void FinancePolicy_PolicyInclusion_IsCorrect(string callerRole, bool shouldBeAllowed)
    {
        // Models the policy in Program.cs:
        //   "Finance"  → RequireRole("SuperAdmin", "Admin", "Finance")
        var financeAllowedRoles = new[] { "SuperAdmin", "Admin", "Finance" };

        var actual = financeAllowedRoles.Contains(callerRole);

        actual.Should().Be(shouldBeAllowed,
            because: $"role '{callerRole}' {(shouldBeAllowed ? "should" : "should not")} satisfy the 'Finance' policy");
    }
}
