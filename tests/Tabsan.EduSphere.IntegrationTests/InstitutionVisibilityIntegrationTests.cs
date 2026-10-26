using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Tenancy;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.Infrastructure.Repositories;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;
using Xunit;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// A School-only installation: with School departments only, the portal's menus, capability matrix (filters, reports,
/// certificates), labels and modules leave College and University out, while the institution policy and its validation
/// are unchanged; adding a College department brings College back.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class InstitutionVisibilityIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public InstitutionVisibilityIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    /// <summary>The real repository, answering "which types have departments" from a list the test controls.</summary>
    private sealed class TypesInUse(IDepartmentRepository inner) : IDepartmentRepository
    {
        public static List<InstitutionType> Types { get; } = [];
        public Task<IReadOnlyList<InstitutionType>> GetInstitutionTypesInUseAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<InstitutionType>>(Types.ToList());
        public Task<IReadOnlyList<Department>> GetAllAsync(Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default) => inner.GetAllAsync(tenantId, campusId, ct);
        public Task<Department?> GetByIdAsync(Guid id, CancellationToken ct = default) => inner.GetByIdAsync(id, ct);
        public Task<bool> CodeExistsAsync(string code, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default) => inner.CodeExistsAsync(code, tenantId, campusId, ct);
        public Task AddAsync(Department department, CancellationToken ct = default) => inner.AddAsync(department, ct);
        public void Update(Department department) => inner.Update(department);
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => inner.SaveChangesAsync(ct);
    }

    [Fact]
    public async Task SchoolOnlyData_HidesCollegeAndUniversity_InWhatThePortalShows_AndCollegeReturnsWithItsFirstDepartment()
    {
        TypesInUse.Types.Clear();
        TypesInUse.Types.Add(InstitutionType.School);
        // A second host runs the start-up seeder on the test database; with a SuperAdmin present it has nothing to create.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var suffix = Guid.NewGuid().ToString("N")[..8];
            db.Add(new User($"vis_{suffix}", "integration-hash", db.Roles.First(r => r.Name == "SuperAdmin").Id, $"vis_{suffix}@tabsan.local"));
            await db.SaveChangesAsync();
        }
        await using var factory = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            s.AddScoped<IDepartmentRepository>(sp => new TypesInUse(ActivatorUtilities.CreateInstance<DepartmentRepository>(sp)))));
        using var admin = factory.CreateClient();
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken("SuperAdmin", "00000000-0000-0000-0000-000000000001"));
        (await admin.PutAsJsonAsync("api/v1/institution-policy", new { includeSchool = true, includeCollege = true, includeUniversity = true })).EnsureSuccessStatusCode();

        static (bool School, bool College, bool University) Types(JsonElement e)
            => (e.GetProperty("includeSchool").GetBoolean(), e.GetProperty("includeCollege").GetBoolean(), e.GetProperty("includeUniversity").GetBoolean());
        static IEnumerable<string> Keys(JsonElement menus)
            => menus.EnumerateArray().SelectMany(m => new[] { m.GetProperty("key").GetString()! }.Concat(Keys(m.GetProperty("subMenus"))));

        // The capability matrix, which builds the portal's Institute choices, reports and certificates: School only.
        Assert.Equal((true, false, false), Types(await admin.GetFromJsonAsync<JsonElement>("api/v1/portal-capabilities/matrix")));
        // University menus are hidden; the others are not.
        var menus = Keys(await admin.GetFromJsonAsync<JsonElement>("api/v1/sidebar-menu/my-visible")).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("degree_audit", menus);
        Assert.DoesNotContain("graduation_applications", menus);
        Assert.DoesNotContain("fyp", menus);
        Assert.DoesNotContain("study_plan", menus);
        Assert.Contains("departments", menus);
        // School vocabulary, and the University-only FYP module is not offered.
        Assert.Equal("Grade", (await admin.GetFromJsonAsync<JsonElement>("api/v1/labels")).GetProperty("periodLabel").GetString());
        var modules = (await admin.GetFromJsonAsync<JsonElement>("api/v1/module-registry/visible")).EnumerateArray()
            .ToDictionary(m => m.GetProperty("key").GetString()!, m => m.GetProperty("isAccessible").GetBoolean(), StringComparer.OrdinalIgnoreCase);
        Assert.False(modules.GetValueOrDefault("fyp"));
        // The policy itself is unchanged.
        Assert.Equal((true, true, true), Types(await admin.GetFromJsonAsync<JsonElement>("api/v1/institution-policy")));

        // Adding the first College department in the portal: allowed by the policy, and College is shown at once.
        Guid tenantId, campusId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tenant = new Tenant($"VIS-{Guid.NewGuid():N}"[..20], "Visibility test school");
            db.Tenants.Add(tenant);
            var campus = new Campus(tenant.Id, "VIS-MAIN", "Main campus");
            db.Campuses.Add(campus);
            await db.SaveChangesAsync();
            (tenantId, campusId) = (tenant.Id, campus.Id);
        }
        TypesInUse.Types.Add(InstitutionType.College);
        var created = await admin.PostAsJsonAsync($"api/v1/department?tenantId={tenantId}&campusId={campusId}",
            new { name = "Pre-Medical", code = $"V{Guid.NewGuid():N}"[..10], institutionType = (int)InstitutionType.College });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal((true, true, false), Types(await admin.GetFromJsonAsync<JsonElement>("api/v1/portal-capabilities/matrix")));
        menus = Keys(await admin.GetFromJsonAsync<JsonElement>("api/v1/sidebar-menu/my-visible")).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("degree_audit", menus); // University still has no departments
    }
}
