using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Tabsan.EduSphere.API.Controllers;
using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.IntegrationTests;

// Final-Touches Phase 31 Stage 31.2 — authorization/data-exposure/audit hardening tests.
public class Phase31Stage2SecurityHardeningTests
{
    [Fact]
    public void All_Api_Endpoints_Are_Explicitly_Authorized_Or_Anonymous()
    {
        var endpointMethods = typeof(AuthController).Assembly
            .GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.DeclaringType == type)
                .Where(method => method.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any())
                .Select(method => new { Type = type, Method = method }))
            .ToList();

        var unguarded = endpointMethods
            .Where(x =>
                !x.Type.GetCustomAttributes(inherit: true).Any(a => a is AuthorizeAttribute || a is AllowAnonymousAttribute)
                && !x.Method.GetCustomAttributes(inherit: true).Any(a => a is AuthorizeAttribute || a is AllowAnonymousAttribute))
            .Select(x => $"{x.Type.Name}.{x.Method.Name}")
            .ToList();

        Assert.True(unguarded.Count == 0, "Unguarded endpoints: " + string.Join(", ", unguarded));
    }

    [Fact]
    public void AllowAnonymous_Surface_Is_Whitelisted()
    {
        var allowAnonymousEndpoints = typeof(AuthController).Assembly
            .GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.DeclaringType == type)
                .Where(method => method.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any())
                .Where(method => method.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any())
                .Select(method => $"{type.Name}.{method.Name}"))
            .OrderBy(x => x)
            .ToList();

        var expected = new[]
        {
            "AnalyticsController.GetPaymentStatus",
            "AuthController.Login",
            "AuthController.Refresh",
            "AuthController.SecurityProfile",
            "PortalSettingsController.GetLogoFile",
            "TwoFactorController.LoginVerify",
            "StudentController.SelfRegister"
        };

        Assert.Equal(expected.OrderBy(x => x), allowAnonymousEndpoints);
    }

    [Fact]
    public async Task Sensitive_ControlPlane_Mutations_Write_Audit_Logs()
    {
        var audit = new AuditCollector();

        var featureFlags = new FeatureFlagsController(new FeatureFlagServiceStub(), audit);
        SetAuthenticatedSuperAdmin(featureFlags);
        await featureFlags.Save("tenant-operations.write", new SaveFeatureFlagCommand("placeholder", false, "maintenance"), CancellationToken.None);
        await featureFlags.Rollback(new RollbackFeatureFlagsCommand(["tenant-operations.write"], "emergency"), CancellationToken.None);

        var tenantOps = new TenantOperationsController(
            new TenantOperationsServiceStub(),
            new FeatureFlagServiceStub(isTenantWriteEnabled: true),
            audit);
        SetAuthenticatedSuperAdmin(tenantOps);

        await tenantOps.SaveOnboardingTemplate(new SaveTenantOnboardingTemplateCommand(
            "Default",
            "University",
            "Admin",
            "Welcome",
            "authentication,courses"), CancellationToken.None);

        await tenantOps.SaveSubscriptionPlan(new SaveTenantSubscriptionPlanCommand(
            "pro",
            "Pro",
            99,
            500,
            true,
            true,
            true,
            true), CancellationToken.None);

        await tenantOps.SaveTenantProfile(new SaveTenantProfileSettingsCommand(
            "tenant-x",
            "Tenant X",
            "support@tenantx.test",
            "+1-555-0101",
            "UTC",
            "en-US",
            "USD",
            "default"), CancellationToken.None);

        var institutionPolicy = new InstitutionPolicyController(new InstitutionPolicyServiceStub(), audit);
        SetAuthenticatedSuperAdmin(institutionPolicy);
        await institutionPolicy.Save(new SaveInstitutionPolicyRequest(true, false, true), CancellationToken.None);

        Assert.Contains(audit.Entries, e => e.Action == "FeatureFlagSave");
        Assert.Contains(audit.Entries, e => e.Action == "FeatureFlagRollback");
        Assert.Contains(audit.Entries, e => e.Action == "TenantOnboardingTemplateSave");
        Assert.Contains(audit.Entries, e => e.Action == "TenantSubscriptionPlanSave");
        Assert.Contains(audit.Entries, e => e.Action == "TenantProfileSave");
        Assert.Contains(audit.Entries, e => e.Action == "InstitutionPolicySave");
    }

    [Fact]
    public async Task Blocked_Tenant_Write_Is_Audited()
    {
        var audit = new AuditCollector();
        var tenantOps = new TenantOperationsController(
            new TenantOperationsServiceStub(),
            new FeatureFlagServiceStub(isTenantWriteEnabled: false),
            audit);

        SetAuthenticatedSuperAdmin(tenantOps);

        var result = await tenantOps.SaveTenantProfile(new SaveTenantProfileSettingsCommand(
            "tenant-x",
            "Tenant X",
            "support@tenantx.test",
            "+1-555-0101",
            "UTC",
            "en-US",
            "USD",
            "default"), CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status423Locked, status.StatusCode);
        Assert.Contains(audit.Entries, e => e.Action == "TenantOperationsWriteBlocked" && e.EntityName == "TenantProfile");
    }

    private static void SetAuthenticatedSuperAdmin(ControllerBase controller)
    {
        var userId = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, "SuperAdmin")
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };
    }
}

file sealed class AuditCollector : IAuditService
{
    public List<AuditLog> Entries { get; } = [];

    public Task LogAsync(AuditLog entry, CancellationToken ct = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }

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
        => Task.FromResult(((IReadOnlyList<AuditLog>)Entries, Entries.Count));
}

file sealed class FeatureFlagServiceStub : IFeatureFlagService
{
    private readonly bool _isTenantWriteEnabled;

    public FeatureFlagServiceStub(bool isTenantWriteEnabled = true)
    {
        _isTenantWriteEnabled = isTenantWriteEnabled;
    }

    public Task<IList<FeatureFlagDto>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IList<FeatureFlagDto>>([]);

    public Task<FeatureFlagDto> GetAsync(string key, CancellationToken ct = default)
    {
        var enabled = !string.Equals(key, "tenant-operations.write", StringComparison.OrdinalIgnoreCase) || _isTenantWriteEnabled;
        return Task.FromResult(new FeatureFlagDto(key, enabled, null, DateTime.UtcNow));
    }

    public Task SaveAsync(SaveFeatureFlagCommand command, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task RollbackAsync(RollbackFeatureFlagsCommand command, CancellationToken ct = default)
        => Task.CompletedTask;
}

file sealed class TenantOperationsServiceStub : ITenantOperationsService
{
    public Task<TenantOnboardingTemplateDto> GetOnboardingTemplateAsync(CancellationToken ct = default)
        => Task.FromResult(new TenantOnboardingTemplateDto("Default", "University", "Admin", "Welcome", "authentication"));

    public Task SaveOnboardingTemplateAsync(SaveTenantOnboardingTemplateCommand cmd, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<TenantSubscriptionPlanDto> GetSubscriptionPlanAsync(CancellationToken ct = default)
        => Task.FromResult(new TenantSubscriptionPlanDto("pro", "Pro", 99, 500, true, true, true, true));

    public Task SaveSubscriptionPlanAsync(SaveTenantSubscriptionPlanCommand cmd, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<TenantProfileSettingsDto> GetTenantProfileAsync(CancellationToken ct = default)
        => Task.FromResult(new TenantProfileSettingsDto("tenant", "Tenant", "support@test", "+1", "UTC", "en-US", "USD", "default"));

    public Task SaveTenantProfileAsync(SaveTenantProfileSettingsCommand cmd, CancellationToken ct = default)
        => Task.CompletedTask;
}

file sealed class InstitutionPolicyServiceStub : IInstitutionPolicyService
{
    public Task<InstitutionPolicySnapshot> GetPolicyAsync(CancellationToken ct = default)
        => Task.FromResult(InstitutionPolicySnapshot.Default);

    public Task SavePolicyAsync(SaveInstitutionPolicyCommand cmd, CancellationToken ct = default)
    {
        if (!cmd.IncludeSchool && !cmd.IncludeCollege && !cmd.IncludeUniversity)
            throw new InvalidOperationException("At least one mode must stay enabled.");

        return Task.CompletedTask;
    }

    public void InvalidateCache()
    {
    }
}
