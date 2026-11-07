using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Application.Interfaces;
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
/// Parent Links screen (2026-10-30): administrators find Parent-role accounts and School students by name, then link,
/// pause, remove and link again; Admins only manage students of their assigned departments; the menu goes with School.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class ParentLinkAdminIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public ParentLinkAdminIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    private sealed record Seed(string Suffix, Guid ParentUserId, Guid OtherParentUserId, Guid AdminUserId, Guid SchoolStudentId,
        Guid OtherDepartmentStudentId, Guid UniversityStudentId);

    private HttpClient Client(string role, Guid? userId = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            JwtTestHelper.GenerateToken(role, (userId ?? Guid.NewGuid()).ToString()));
        return client;
    }

    /// <summary>
    /// Two School departments (the Admin is assigned to the first) and a University one, a student in each, two Parent
    /// accounts and an Admin account that shares the parent's surname (it must not be offered as a parent).
    /// </summary>
    private async Task<Seed> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        int Role(string name) => db.Roles.First(r => r.Name == name).Id;

        var school = new Department($"PL School {suffix}", $"PLS{suffix}", InstitutionType.School);
        var otherSchool = new Department($"PL Other School {suffix}", $"PLO{suffix}", InstitutionType.School);
        var university = new Department($"PL University {suffix}", $"PLU{suffix}", InstitutionType.University);
        db.Departments.AddRange(school, otherSchool, university);
        var programs = new[] { school, otherSchool, university }
            .Select(d => new AcademicProgram($"PL Program {d.Code}", $"P{d.Code}", d.Id, 10)).ToArray();
        db.AcademicPrograms.AddRange(programs);

        var parent = new User($"imran.{suffix}", "integration-hash", Role("Parent"), $"imran.{suffix}@tabsan.local",
            fullName: $"Imran Sheikh{suffix}", phoneNumber: $"0300{suffix}");
        var otherParent = new User($"ayesha.{suffix}", "integration-hash", Role("Parent"), fullName: $"Ayesha Khan{suffix}");
        var admin = new User($"pl_admin_{suffix}", "integration-hash", Role("Admin"), fullName: $"Admin Sheikh{suffix}");
        db.Users.AddRange(parent, otherParent, admin);
        db.AdminDepartmentAssignments.Add(new AdminDepartmentAssignment(admin.Id, school.Id));

        StudentProfile Student(string username, string fullName, string regNo, Department department, AcademicProgram program)
        {
            var user = new User(username, "integration-hash", Role("Student"), fullName: fullName);
            db.Users.Add(user);
            var profile = new StudentProfile(user.Id, regNo, program.Id, department.Id, DateTime.UtcNow.Date);
            db.StudentProfiles.Add(profile);
            return profile;
        }

        var fatima = Student($"fatima.{suffix}", $"Fatima Sheikh{suffix}", $"TMS-{suffix}-01", school, programs[0]);
        var hassan = Student($"hassan.{suffix}", $"Hassan Sheikh{suffix}", $"TMS-{suffix}-02", otherSchool, programs[1]);
        var uni = Student($"uni.{suffix}", $"Uni Sheikh{suffix}", $"UNI-{suffix}-01", university, programs[2]);
        await db.SaveChangesAsync();
        return new Seed(suffix, parent.Id, otherParent.Id, admin.Id, fatima.Id, hassan.Id, uni.Id);
    }

    private static async Task<JsonElement[]> ArrayAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToArray();
    }

    [Fact]
    public async Task Lookups_FindParentAccountsAndSchoolStudents_ByNameOrNumber()
    {
        var seed = await SeedAsync();
        using var superAdmin = Client("SuperAdmin");

        // Parents: by surname (the Admin with the same surname is not a parent), username, email and phone.
        var bySurname = await ArrayAsync(superAdmin, $"api/v1/parent-portal/parents?search=Sheikh{seed.Suffix}");
        var parent = Assert.Single(bySurname);
        Assert.Equal(seed.ParentUserId, parent.GetProperty("userId").GetGuid());
        Assert.Equal($"Imran Sheikh{seed.Suffix}", parent.GetProperty("fullName").GetString());
        Assert.Equal(0, parent.GetProperty("linkedStudents").GetInt32());
        Assert.True(parent.GetProperty("isActive").GetBoolean());
        foreach (var term in new[] { $"imran.{seed.Suffix}", $"imran.{seed.Suffix}@tabsan", $"0300{seed.Suffix}" })
            Assert.Equal(seed.ParentUserId, Assert.Single(await ArrayAsync(superAdmin, $"api/v1/parent-portal/parents?search={Uri.EscapeDataString(term)}")).GetProperty("userId").GetGuid());
        Assert.Equal(2, (await ArrayAsync(superAdmin, $"api/v1/parent-portal/parents?search={seed.Suffix}")).Length);
        Assert.True((await ArrayAsync(superAdmin, "api/v1/parent-portal/parents?take=1")).Length <= 1);

        // One parent by id; a non-parent id is not found.
        var one = await superAdmin.GetFromJsonAsync<JsonElement>($"api/v1/parent-portal/parents/{seed.ParentUserId}");
        Assert.Equal($"imran.{seed.Suffix}", one.GetProperty("username").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await superAdmin.GetAsync($"api/v1/parent-portal/parents/{seed.AdminUserId}")).StatusCode);

        // Students: School only (the University student with the same surname is left out), by name or registration number.
        var students = await ArrayAsync(superAdmin, $"api/v1/parent-portal/students?search=Sheikh{seed.Suffix}");
        Assert.Equal(new[] { seed.SchoolStudentId, seed.OtherDepartmentStudentId }, students.Select(s => s.GetProperty("studentProfileId").GetGuid()));
        Assert.Equal($"Fatima Sheikh{seed.Suffix}", students[0].GetProperty("name").GetString());
        Assert.Equal($"TMS-{seed.Suffix}-01", students[0].GetProperty("registrationNumber").GetString());
        Assert.Equal($"PL School {seed.Suffix}", students[0].GetProperty("departmentName").GetString());
        Assert.Equal(1, students[0].GetProperty("classNumber").GetInt32());
        Assert.Equal("Active", students[0].GetProperty("status").GetString());
        Assert.Equal(seed.OtherDepartmentStudentId, Assert.Single(await ArrayAsync(superAdmin, $"api/v1/parent-portal/students?search=TMS-{seed.Suffix}-02")).GetProperty("studentProfileId").GetGuid());
        Assert.Empty(await ArrayAsync(superAdmin, $"api/v1/parent-portal/students?search=UNI-{seed.Suffix}"));
    }

    [Fact]
    public async Task Link_Pause_Remove_AndLinkAgain_ThroughTheApi()
    {
        var seed = await SeedAsync();
        using var superAdmin = Client("SuperAdmin");
        using var parentApp = Client("Parent", seed.ParentUserId);
        async Task<JsonElement[]> Links() => await ArrayAsync(superAdmin, $"api/v1/parent-portal/parents/{seed.ParentUserId}/links");
        async Task<JsonElement[]> Children() => await ArrayAsync(parentApp, "api/v1/parent-portal/me/students");

        var linked = await superAdmin.PutAsJsonAsync("api/v1/parent-portal/links",
            new { parentUserId = seed.ParentUserId, studentProfileId = seed.SchoolStudentId, relationship = "Father" });
        Assert.Equal(HttpStatusCode.OK, linked.StatusCode);
        var link = Assert.Single(await Links());
        Assert.Equal($"Fatima Sheikh{seed.Suffix}", link.GetProperty("studentName").GetString());
        Assert.Equal($"TMS-{seed.Suffix}-01", link.GetProperty("registrationNumber").GetString());
        Assert.Equal("Father", link.GetProperty("relationship").GetString());
        Assert.True(link.GetProperty("isActive").GetBoolean());
        Assert.Equal(seed.SchoolStudentId, Assert.Single(await Children()).GetProperty("studentProfileId").GetGuid());
        Assert.Equal(1, Assert.Single(await ArrayAsync(superAdmin, $"api/v1/parent-portal/students?search=TMS-{seed.Suffix}-01")).GetProperty("linkedParents").GetInt32());

        // Pause (isActive=false): still listed for the administrator, hidden from the parent.
        (await superAdmin.PutAsJsonAsync("api/v1/parent-portal/links",
            new { parentUserId = seed.ParentUserId, studentProfileId = seed.SchoolStudentId, relationship = "Father", isActive = false })).EnsureSuccessStatusCode();
        Assert.False(Assert.Single(await Links()).GetProperty("isActive").GetBoolean());
        Assert.Empty(await Children());
        Assert.Equal(HttpStatusCode.Forbidden, (await parentApp.GetAsync($"api/v1/parent-portal/me/students/{seed.SchoolStudentId}/results")).StatusCode);

        // Remove: gone from every list; removing again is 404.
        Assert.Equal(HttpStatusCode.NoContent, (await superAdmin.DeleteAsync($"api/v1/parent-portal/links/{seed.ParentUserId}/{seed.SchoolStudentId}?remove=true")).StatusCode);
        Assert.Empty(await Links());
        Assert.Empty(await ArrayAsync(superAdmin, $"api/v1/parent-portal/links/{seed.ParentUserId}"));
        Assert.Equal(0, Assert.Single(await ArrayAsync(superAdmin, $"api/v1/parent-portal/parents?search=imran.{seed.Suffix}")).GetProperty("linkedStudents").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await superAdmin.DeleteAsync($"api/v1/parent-portal/links/{seed.ParentUserId}/{seed.SchoolStudentId}?remove=true")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await superAdmin.DeleteAsync($"api/v1/parent-portal/links/{seed.ParentUserId}/{seed.SchoolStudentId}")).StatusCode);

        // Linking again restores the same row (the parent/student pair is unique in the database).
        (await superAdmin.PutAsJsonAsync("api/v1/parent-portal/links",
            new { parentUserId = seed.ParentUserId, studentProfileId = seed.SchoolStudentId, relationship = "Guardian" })).EnsureSuccessStatusCode();
        Assert.Equal("Guardian", Assert.Single(await Links()).GetProperty("relationship").GetString());
        Assert.Single(await Children());
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var rows = await db.ParentStudentLinks.Where(l => l.ParentUserId == seed.ParentUserId).ToListAsync();
            Assert.False(Assert.Single(rows).IsDeleted);
        }

        // The plain DELETE still pauses, as before.
        Assert.Equal(HttpStatusCode.NoContent, (await superAdmin.DeleteAsync($"api/v1/parent-portal/links/{seed.ParentUserId}/{seed.SchoolStudentId}")).StatusCode);
        Assert.False(Assert.Single(await Links()).GetProperty("isActive").GetBoolean());

        // The rules stay: only Parent accounts, only School students, a relationship that fits the column.
        var notParent = await superAdmin.PutAsJsonAsync("api/v1/parent-portal/links", new { parentUserId = seed.AdminUserId, studentProfileId = seed.SchoolStudentId, relationship = "Father" });
        Assert.Equal(HttpStatusCode.BadRequest, notParent.StatusCode);
        var university = await superAdmin.PutAsJsonAsync("api/v1/parent-portal/links", new { parentUserId = seed.ParentUserId, studentProfileId = seed.UniversityStudentId, relationship = "Father" });
        Assert.Equal(HttpStatusCode.BadRequest, university.StatusCode);
        var tooLong = await superAdmin.PutAsJsonAsync("api/v1/parent-portal/links", new { parentUserId = seed.ParentUserId, studentProfileId = seed.SchoolStudentId, relationship = new string('x', 61) });
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
    }

    [Fact]
    public async Task Admin_SeesAndChangesOnlyTheStudentsOfTheirDepartments()
    {
        var seed = await SeedAsync();
        using var admin = Client("Admin", seed.AdminUserId);

        var students = await ArrayAsync(admin, $"api/v1/parent-portal/students?search=Sheikh{seed.Suffix}");
        Assert.Equal(seed.SchoolStudentId, Assert.Single(students).GetProperty("studentProfileId").GetGuid());
        Assert.Single(await ArrayAsync(admin, $"api/v1/parent-portal/parents?search=imran.{seed.Suffix}")); // parents without a tenant: every Admin

        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync("api/v1/parent-portal/links",
            new { parentUserId = seed.ParentUserId, studentProfileId = seed.SchoolStudentId, relationship = "Mother" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PutAsJsonAsync("api/v1/parent-portal/links",
            new { parentUserId = seed.ParentUserId, studentProfileId = seed.OtherDepartmentStudentId, relationship = "Mother" })).StatusCode);

        // A link made by a SuperAdmin to the other department's student is neither shown to nor changeable by this Admin.
        using var superAdmin = Client("SuperAdmin");
        (await superAdmin.PutAsJsonAsync("api/v1/parent-portal/links",
            new { parentUserId = seed.ParentUserId, studentProfileId = seed.OtherDepartmentStudentId, relationship = "Mother" })).EnsureSuccessStatusCode();
        Assert.Equal(seed.SchoolStudentId, Assert.Single(await ArrayAsync(admin, $"api/v1/parent-portal/parents/{seed.ParentUserId}/links")).GetProperty("studentProfileId").GetGuid());
        Assert.Equal(2, (await ArrayAsync(superAdmin, $"api/v1/parent-portal/parents/{seed.ParentUserId}/links")).Length);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.DeleteAsync($"api/v1/parent-portal/links/{seed.ParentUserId}/{seed.OtherDepartmentStudentId}?remove=true")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"api/v1/parent-portal/links/{seed.ParentUserId}/{seed.SchoolStudentId}?remove=true")).StatusCode);
    }

    [Fact]
    public async Task Admin_SeesTheParentsOfTheirSchool_AtAnyCampus_AndParentsWithoutASchool_NotOtherSchools()
    {
        var seed = await SeedAsync();
        Guid schoolId, campusId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var school = new Tenant($"PLT{seed.Suffix}", $"PL School Group {seed.Suffix}");
            var otherSchool = new Tenant($"PLX{seed.Suffix}", $"PL Other Group {seed.Suffix}");
            db.Tenants.AddRange(school, otherSchool);
            var campusA = new Campus(school.Id, $"PLA{seed.Suffix}", "Campus A");
            var campusB = new Campus(school.Id, $"PLB{seed.Suffix}", "Campus B");
            var otherCampus = new Campus(otherSchool.Id, $"PLC{seed.Suffix}", "Other campus");
            db.Campuses.AddRange(campusA, campusB, otherCampus);
            var parentRole = db.Roles.First(r => r.Name == "Parent").Id;
            db.Users.AddRange(
                new User($"sameschool.{seed.Suffix}", "integration-hash", parentRole, fullName: $"Zara Tenant{seed.Suffix}", tenantId: school.Id, campusId: campusB.Id),
                new User($"otherschool.{seed.Suffix}", "integration-hash", parentRole, fullName: $"Omar Tenant{seed.Suffix}", tenantId: otherSchool.Id, campusId: otherCampus.Id),
                new User($"noschool.{seed.Suffix}", "integration-hash", parentRole, fullName: $"Nadia Tenant{seed.Suffix}"));
            await db.SaveChangesAsync();
            (schoolId, campusId) = (school.Id, campusA.Id);
        }

        using var admin = _factory.CreateClient();
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            JwtTestHelper.GenerateToken("Admin", seed.AdminUserId.ToString(), tenantId: schoolId.ToString(), campusId: campusId.ToString()));
        static HashSet<string> Usernames(JsonElement[] parents) => parents.Select(p => p.GetProperty("username").GetString()!).ToHashSet();

        Assert.Equal(new HashSet<string> { $"sameschool.{seed.Suffix}", $"noschool.{seed.Suffix}" },
            Usernames(await ArrayAsync(admin, $"api/v1/parent-portal/parents?search=Tenant{seed.Suffix}")));
        using var superAdmin = Client("SuperAdmin");
        Assert.Equal(3, (await ArrayAsync(superAdmin, $"api/v1/parent-portal/parents?search=Tenant{seed.Suffix}")).Length);
    }

    [Theory]
    [InlineData("Student")]
    [InlineData("Faculty")]
    [InlineData("Parent")]
    [InlineData("Finance")]
    public async Task Lookups_AreForAdministratorsOnly(string role)
    {
        using var client = Client(role);
        foreach (var url in new[] { "api/v1/parent-portal/parents", "api/v1/parent-portal/students?search=a", $"api/v1/parent-portal/parents/{Guid.NewGuid()}/links" })
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"api/v1/parent-portal/links/{Guid.NewGuid()}/{Guid.NewGuid()}?remove=true")).StatusCode);

        using var anonymous = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("api/v1/parent-portal/parents")).StatusCode);
    }

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
    public async Task Menu_IsSeededForAdmins_AndShownOnlyWhileSchoolIsInUse()
    {
        var seed = await SeedAsync();
        // A second host runs the start-up seeder on the test database; with a SuperAdmin present it has nothing to create.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Add(new User($"pl_super_{seed.Suffix}", "integration-hash", db.Roles.First(r => r.Name == "SuperAdmin").Id, $"pl_super_{seed.Suffix}@tabsan.local"));
            await db.SaveChangesAsync();
        }
        await using var factory = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            s.AddScoped<IDepartmentRepository>(sp => new TypesInUse(ActivatorUtilities.CreateInstance<DepartmentRepository>(sp)))));
        using var superAdmin = factory.CreateClient();
        superAdmin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken("SuperAdmin", Guid.NewGuid().ToString()));
        using var admin = factory.CreateClient();
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken("Admin", seed.AdminUserId.ToString()));
        using var faculty = factory.CreateClient();
        faculty.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken("Faculty", Guid.NewGuid().ToString()));
        (await superAdmin.PutAsJsonAsync("api/v1/institution-policy", new { includeSchool = true, includeCollege = true, includeUniversity = true })).EnsureSuccessStatusCode();

        static IEnumerable<string> Keys(JsonElement menus)
            => menus.EnumerateArray().SelectMany(m => new[] { m.GetProperty("key").GetString()! }.Concat(Keys(m.GetProperty("subMenus"))));
        async Task<HashSet<string>> Visible(HttpClient client)
            => Keys(await client.GetFromJsonAsync<JsonElement>("api/v1/sidebar-menu/my-visible")).ToHashSet(StringComparer.OrdinalIgnoreCase);
        void UseTypes(params InstitutionType[] types)
        {
            TypesInUse.Types.Clear();
            TypesInUse.Types.AddRange(types);
            using var scope = factory.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<IInstitutionVisibilityService>().InvalidateCache();
        }

        try
        {
            UseTypes(InstitutionType.School, InstitutionType.University);
            Assert.Contains("parent_links", await Visible(superAdmin));
            Assert.Contains("parent_links", await Visible(admin));
            Assert.DoesNotContain("parent_links", await Visible(faculty));

            UseTypes(InstitutionType.University);
            Assert.DoesNotContain("parent_links", await Visible(superAdmin));
            Assert.DoesNotContain("parent_links", await Visible(admin));
        }
        finally
        {
            UseTypes();
        }
    }
}
