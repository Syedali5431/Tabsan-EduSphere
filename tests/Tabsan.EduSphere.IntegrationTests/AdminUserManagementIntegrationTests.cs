using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Tenancy;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

[Collection(EduSphereCollection.Name)]
public class AdminUserManagementIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    private sealed record InstitutionPolicySnapshot(bool IncludeSchool, bool IncludeCollege, bool IncludeUniversity);

    public AdminUserManagementIntegrationTests(EduSphereWebFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient(string role, string userId = "00000000-0000-0000-0000-000000000001")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(role, userId));
        return client;
    }

    private async Task<(Guid TenantId, Guid CampusId)> GetDefaultTenantCampusAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var tenant = await db.Tenants.FirstAsync(t => t.Code == "DEFAULT");
        var campus = await db.Campuses.FirstAsync(c => c.TenantId == tenant.Id && c.Code == "MAIN");
        return (tenant.Id, campus.Id);
    }

    private static async Task<InstitutionPolicySnapshot> GetPolicySnapshotAsync(HttpClient client)
    {
        var response = await client.GetAsync("api/v1/institution-policy");
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return new InstitutionPolicySnapshot(
            doc.RootElement.GetProperty("includeSchool").GetBoolean(),
            doc.RootElement.GetProperty("includeCollege").GetBoolean(),
            doc.RootElement.GetProperty("includeUniversity").GetBoolean());
    }

    private static Task<HttpResponseMessage> SetPolicyAsync(HttpClient client, bool includeSchool, bool includeCollege, bool includeUniversity)
        => client.PutAsJsonAsync("api/v1/institution-policy", new
        {
            includeSchool,
            includeCollege,
            includeUniversity
        });

    private static async Task ExecuteWithPolicyAsync(
        HttpClient client,
        bool includeSchool,
        bool includeCollege,
        bool includeUniversity,
        Func<Task> action)
    {
        var originalPolicy = await GetPolicySnapshotAsync(client);
        try
        {
            var setResponse = await SetPolicyAsync(client, includeSchool, includeCollege, includeUniversity);
            Assert.Equal(HttpStatusCode.NoContent, setResponse.StatusCode);
            await action();
        }
        finally
        {
            var restoreResponse = await SetPolicyAsync(
                client,
                originalPolicy.IncludeSchool,
                originalPolicy.IncludeCollege,
                originalPolicy.IncludeUniversity);
            Assert.Equal(HttpStatusCode.NoContent, restoreResponse.StatusCode);
        }
    }

    [Fact]
    public async Task AdminUser_List_WithSuperAdminRole_ReturnsSuccess()
    {
        using var client = CreateClient("SuperAdmin");

        var response = await client.GetAsync("api/v1/admin-user");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminUser_List_WithAdminRole_ReturnsForbidden()
    {
        using var client = CreateClient("Admin");

        var response = await client.GetAsync("api/v1/admin-user");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminUser_CreateUpdate_AndDepartmentAssignmentRoundTrip_Works()
    {
        using var client = CreateClient("SuperAdmin", "00000000-0000-0000-0000-000000000010");
        var (tenantId, campusId) = await GetDefaultTenantCampusAsync();

        await ExecuteWithPolicyAsync(client, includeSchool: false, includeCollege: false, includeUniversity: true, async () =>
        {
            var username = $"admin_it_{Guid.NewGuid():N}";
            var createPayload = new
            {
                username,
                email = $"{username}@tabsan.local",
                password = "Pass@123",
                institutionType = 0
            };

            var createResponse = await client.PostAsJsonAsync("api/v1/admin-user", createPayload);
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

            using var createDoc = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
            var adminUserId = createDoc.RootElement.GetProperty("id").GetGuid();
            Assert.Equal(0, createDoc.RootElement.GetProperty("institutionType").GetInt32());

            var deptsResponse = await client.GetAsync("api/v1/department");
            Assert.Equal(HttpStatusCode.OK, deptsResponse.StatusCode);
            using var deptsDoc = JsonDocument.Parse(await deptsResponse.Content.ReadAsStringAsync());
            var departmentItems = deptsDoc.RootElement.EnumerateArray().ToList();
            Guid firstDepartmentId;
            var universityDepartment = departmentItems
                .FirstOrDefault(d => d.TryGetProperty("institutionType", out var t) && t.GetInt32() == 0);

            if (universityDepartment.ValueKind != JsonValueKind.Undefined)
            {
                firstDepartmentId = universityDepartment.GetProperty("id").GetGuid();
            }
            else
            {
                var suffix = Guid.NewGuid().ToString("N")[..6];
                var createDepartmentResponse = await client.PostAsJsonAsync($"api/v1/department?tenantId={tenantId}&campusId={campusId}", new
                {
                    name = $"Integration Dept {suffix}",
                    code = $"INT{suffix}",
                    institutionType = 0
                });
                Assert.Equal(HttpStatusCode.Created, createDepartmentResponse.StatusCode);

                using var createDeptDoc = JsonDocument.Parse(await createDepartmentResponse.Content.ReadAsStringAsync());
                firstDepartmentId = createDeptDoc.RootElement.GetProperty("id").GetGuid();
            }

            var assignResponse = await client.PostAsJsonAsync("api/v1/department/admin-assignment", new
            {
                adminUserId,
                departmentId = firstDepartmentId
            });
            Assert.Equal(HttpStatusCode.NoContent, assignResponse.StatusCode);

            var listAssignmentsResponse = await client.GetAsync($"api/v1/department/admin-assignment/{adminUserId}");
            Assert.Equal(HttpStatusCode.OK, listAssignmentsResponse.StatusCode);
            using var assignmentsDoc = JsonDocument.Parse(await listAssignmentsResponse.Content.ReadAsStringAsync());
            Assert.Contains(assignmentsDoc.RootElement.EnumerateArray(), x => x.GetProperty("departmentId").GetGuid() == firstDepartmentId);

            var updateResponse = await client.PutAsJsonAsync($"api/v1/admin-user/{adminUserId}", new
            {
                email = $"updated_{username}@tabsan.local",
                isActive = false,
                newPassword = "Pass@1234"
            });
            Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

            var removeRequest = new HttpRequestMessage(HttpMethod.Delete, "api/v1/department/admin-assignment")
            {
                Content = JsonContent.Create(new { adminUserId, departmentId = firstDepartmentId })
            };
            var removeResponse = await client.SendAsync(removeRequest);
            Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);
        });
    }

    [Fact]
    public async Task AdminUser_Create_WithDisabledInstitutionType_ReturnsBadRequest()
    {
        using var client = CreateClient("SuperAdmin", "00000000-0000-0000-0000-000000000011");

        await ExecuteWithPolicyAsync(client, includeSchool: false, includeCollege: false, includeUniversity: true, async () =>
        {
            var username = $"admin_disabled_{Guid.NewGuid():N}";
            var createPayload = new
            {
                username,
                email = $"{username}@tabsan.local",
                password = "Pass@123",
                institutionType = 1
            };

            var createResponse = await client.PostAsJsonAsync("api/v1/admin-user", createPayload);
            Assert.Equal(HttpStatusCode.BadRequest, createResponse.StatusCode);
        });
    }

    [Fact]
    public async Task Department_FacultyAssignmentRoundTrip_WithSuperAdmin_Works()
    {
        using var client = CreateClient("SuperAdmin", "00000000-0000-0000-0000-000000000012");
        var (tenantId, campusId) = await GetDefaultTenantCampusAsync();

        var deptsResponse = await client.GetAsync("api/v1/department");
        Assert.Equal(HttpStatusCode.OK, deptsResponse.StatusCode);
        using var deptsDoc = JsonDocument.Parse(await deptsResponse.Content.ReadAsStringAsync());
        var departmentItems = deptsDoc.RootElement.EnumerateArray().ToList();
        Guid targetDepartmentId;
        if (departmentItems.Count > 0)
        {
            targetDepartmentId = departmentItems[0].GetProperty("id").GetGuid();
        }
        else
        {
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var createDepartmentResponse = await client.PostAsJsonAsync($"api/v1/department?tenantId={tenantId}&campusId={campusId}", new
            {
                name = $"Faculty Integration Dept {suffix}",
                code = $"FID{suffix}",
                institutionType = 0
            });
            Assert.Equal(HttpStatusCode.Created, createDepartmentResponse.StatusCode);

            using var createDeptDoc = JsonDocument.Parse(await createDepartmentResponse.Content.ReadAsStringAsync());
            targetDepartmentId = createDeptDoc.RootElement.GetProperty("id").GetGuid();
        }

        Guid facultyUserId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var facultyRole = db.Roles.First(r => r.Name == "Faculty");
            var user = new User(
                username: $"faculty_it_{Guid.NewGuid():N}",
                passwordHash: "integration-hash",
                roleId: facultyRole.Id,
                email: $"faculty_it_{Guid.NewGuid():N}@tabsan.local",
                departmentId: null,
                mustChangePassword: false,
                institutionType: null);

            db.Users.Add(user);
            await db.SaveChangesAsync();
            facultyUserId = user.Id;
        }

        var assignResponse = await client.PostAsJsonAsync("api/v1/department/faculty-assignment", new
        {
            facultyUserId,
            departmentId = targetDepartmentId
        });
        Assert.Equal(HttpStatusCode.NoContent, assignResponse.StatusCode);

        var listAssignmentsResponse = await client.GetAsync($"api/v1/department/faculty-assignment/{facultyUserId}");
        Assert.Equal(HttpStatusCode.OK, listAssignmentsResponse.StatusCode);
        using var assignmentsDoc = JsonDocument.Parse(await listAssignmentsResponse.Content.ReadAsStringAsync());
        Assert.Contains(assignmentsDoc.RootElement.EnumerateArray(), x => x.GetProperty("departmentId").GetGuid() == targetDepartmentId);

        var removeRequest = new HttpRequestMessage(HttpMethod.Delete, "api/v1/department/faculty-assignment")
        {
            Content = JsonContent.Create(new { facultyUserId, departmentId = targetDepartmentId })
        };
        var removeResponse = await client.SendAsync(removeRequest);
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);
    }

    [Fact]
    public async Task Department_AdminAssignment_WithInstitutionMismatch_ReturnsBadRequest()
    {
        using var client = CreateClient("SuperAdmin", "00000000-0000-0000-0000-000000000013");
        var (tenantId, campusId) = await GetDefaultTenantCampusAsync();

        await ExecuteWithPolicyAsync(client, includeSchool: true, includeCollege: true, includeUniversity: true, async () =>
        {
            var username = $"admin_mismatch_{Guid.NewGuid():N}";
            var createPayload = new
            {
                username,
                email = $"{username}@tabsan.local",
                password = "Pass@123",
                institutionType = 0
            };

            var createResponse = await client.PostAsJsonAsync("api/v1/admin-user", createPayload);
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

            using var createDoc = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
            var adminUserId = createDoc.RootElement.GetProperty("id").GetGuid();

            var suffix = Guid.NewGuid().ToString("N")[..6];
            var createDepartmentResponse = await client.PostAsJsonAsync($"api/v1/department?tenantId={tenantId}&campusId={campusId}", new
            {
                name = $"College Dept {suffix}",
                code = $"COL{suffix}",
                institutionType = 1
            });

            Assert.Equal(HttpStatusCode.Created, createDepartmentResponse.StatusCode);
            using var deptDoc = JsonDocument.Parse(await createDepartmentResponse.Content.ReadAsStringAsync());
            var departmentId = deptDoc.RootElement.GetProperty("id").GetGuid();

            var assignResponse = await client.PostAsJsonAsync("api/v1/department/admin-assignment", new
            {
                adminUserId,
                departmentId
            });
            Assert.Equal(HttpStatusCode.BadRequest, assignResponse.StatusCode);
        });
    }

    [Fact]
    public async Task DepartmentAndCourse_Crud_WorksAcrossAllInstitutionTypes_WhenPolicyEnablesAll()
    {
        using var client = CreateClient("SuperAdmin", "00000000-0000-0000-0000-000000000014");
        var (tenantId, campusId) = await GetDefaultTenantCampusAsync();

        var originalPolicy = await GetPolicySnapshotAsync(client);

        try
        {
            var enableAllResponse = await SetPolicyAsync(client, includeSchool: true, includeCollege: true, includeUniversity: true);
            Assert.Equal(HttpStatusCode.NoContent, enableAllResponse.StatusCode);

            async Task<Guid> CreateDepartmentAsync(string prefix, int institutionType)
            {
                var suffix = Guid.NewGuid().ToString("N")[..6];
                var response = await client.PostAsJsonAsync($"api/v1/department?tenantId={tenantId}&campusId={campusId}", new
                {
                    name = $"{prefix} Dept {suffix}",
                    code = $"{prefix[..Math.Min(3, prefix.Length)].ToUpperInvariant()}{suffix}",
                    institutionType
                });
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                return doc.RootElement.GetProperty("id").GetGuid();
            }

            async Task<Guid> CreateCourseAsync(Guid departmentId, string prefix)
            {
                var suffix = Guid.NewGuid().ToString("N")[..6];
                var response = await client.PostAsJsonAsync($"api/v1/course?tenantId={tenantId}&campusId={campusId}", new
                {
                    title = $"{prefix} Course {suffix}",
                    code = $"{prefix[..Math.Min(3, prefix.Length)].ToUpperInvariant()}{suffix}",
                    creditHours = 3,
                    departmentId,
                    hasSemesters = true,
                    totalSemesters = 2,
                    durationValue = (int?)null,
                    durationUnit = (string?)null,
                    gradingType = "GPA"
                });
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                return doc.RootElement.GetProperty("id").GetGuid();
            }

            var schoolDepartmentId = await CreateDepartmentAsync("School", institutionType: 1);
            var collegeDepartmentId = await CreateDepartmentAsync("College", institutionType: 2);
            var universityDepartmentId = await CreateDepartmentAsync("University", institutionType: 0);

            var schoolCourseId = await CreateCourseAsync(schoolDepartmentId, "School");
            var collegeCourseId = await CreateCourseAsync(collegeDepartmentId, "College");
            var universityCourseId = await CreateCourseAsync(universityDepartmentId, "University");

            var schoolUpdateResponse = await client.PutAsJsonAsync($"api/v1/course/{schoolCourseId}/title", new { newTitle = "School Course Updated" });
            Assert.Equal(HttpStatusCode.NoContent, schoolUpdateResponse.StatusCode);

            var collegeDeactivateResponse = await client.DeleteAsync($"api/v1/course/{collegeCourseId}");
            Assert.Equal(HttpStatusCode.NoContent, collegeDeactivateResponse.StatusCode);

            var departmentUpdateResponse = await client.PutAsJsonAsync($"api/v1/department/{schoolDepartmentId}", new
            {
                newName = "School Department Updated",
                institutionType = 2
            });
            Assert.Equal(HttpStatusCode.NoContent, departmentUpdateResponse.StatusCode);

            var getDepartmentResponse = await client.GetAsync($"api/v1/department/{schoolDepartmentId}");
            Assert.Equal(HttpStatusCode.OK, getDepartmentResponse.StatusCode);
            using var getDepartmentDoc = JsonDocument.Parse(await getDepartmentResponse.Content.ReadAsStringAsync());
            Assert.Equal(2, getDepartmentDoc.RootElement.GetProperty("institutionType").GetInt32());

            // Keep one university course active to avoid impacting unrelated tests that expect baseline data.
            Assert.NotEqual(Guid.Empty, universityCourseId);
        }
        finally
        {
            var restoreResponse = await SetPolicyAsync(
                client,
                originalPolicy.IncludeSchool,
                originalPolicy.IncludeCollege,
                originalPolicy.IncludeUniversity);
            Assert.Equal(HttpStatusCode.NoContent, restoreResponse.StatusCode);
        }
    }
}
