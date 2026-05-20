using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

[Collection(EduSphereCollection.Name)]
public class ReportExportsIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public ReportExportsIntegrationTests(EduSphereWebFactory factory)
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

    private async Task EnsureReportsModuleActiveAsync(CancellationToken ct = default)
    {
        using var superClient = CreateClient("SuperAdmin", "00000000-0000-0000-0000-000000000991");

        var statusResponse = await superClient.GetAsync("api/v1/module/reports/status", ct);
        statusResponse.EnsureSuccessStatusCode();

        using var statusDoc = JsonDocument.Parse(await statusResponse.Content.ReadAsStringAsync(ct));
        if (statusDoc.RootElement.GetProperty("isActive").GetBoolean())
            return;

        var activateResponse = await superClient.PostAsync("api/v1/module/reports/activate", content: null, ct);
        activateResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AttendanceSummary_Export_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("api/v1/reports/attendance-summary/export");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("api/v1/reports/attendance-summary/export", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "attendance-summary.xlsx")]
    [InlineData("api/v1/reports/attendance-summary/export/csv", "text/csv", "attendance-summary.csv")]
    [InlineData("api/v1/reports/attendance-summary/export/pdf", "application/pdf", "attendance-summary.pdf")]
    [InlineData("api/v1/reports/result-summary/export", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "result-summary.xlsx")]
    [InlineData("api/v1/reports/result-summary/export/csv", "text/csv", "result-summary.csv")]
    [InlineData("api/v1/reports/result-summary/export/pdf", "application/pdf", "result-summary.pdf")]
    [InlineData("api/v1/reports/assignment-summary/export", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "assignment-summary.xlsx")]
    [InlineData("api/v1/reports/assignment-summary/export/csv", "text/csv", "assignment-summary.csv")]
    [InlineData("api/v1/reports/assignment-summary/export/pdf", "application/pdf", "assignment-summary.pdf")]
    [InlineData("api/v1/reports/quiz-summary/export", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "quiz-summary.xlsx")]
    [InlineData("api/v1/reports/quiz-summary/export/csv", "text/csv", "quiz-summary.csv")]
    [InlineData("api/v1/reports/quiz-summary/export/pdf", "application/pdf", "quiz-summary.pdf")]
    [InlineData("api/v1/reports/payment-summary/export", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "payment-summary.xlsx")]
    [InlineData("api/v1/reports/payment-summary/export/pdf", "application/pdf", "payment-summary.pdf")]
    public async Task ReportExports_WithSuperAdmin_ReturnExpectedFileMetadata(string route, string expectedContentType, string expectedFileName)
    {
        // Final-Touches Phase 32 Stage 32.2 — export endpoint guardrails for content-type and filename contracts.
        using var client = CreateClient("SuperAdmin", "00000000-0000-0000-0000-000000000099");

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedContentType, response.Content.Headers.ContentType?.MediaType);

        var disposition = response.Content.Headers.ContentDisposition?.ToString() ?? string.Empty;
        Assert.Contains(expectedFileName, disposition, StringComparison.OrdinalIgnoreCase);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task EnrollmentSummary_WithInstitutionFilter_ReturnsScopedInstituteRows()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        const int universityType = (int)InstitutionType.University;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var universityDepartment = new Department($"Report Uni {suffix}", $"RU{suffix}", InstitutionType.University);
            var collegeDepartment = new Department($"Report Col {suffix}", $"RC{suffix}", InstitutionType.College);
            db.Departments.AddRange(universityDepartment, collegeDepartment);

            var semester = new Semester($"Report Sem {suffix}", DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(45));
            db.Semesters.Add(semester);

            var uniCourse = new Course($"Report Uni Course {suffix}", $"RUC{suffix}", 3, universityDepartment.Id);
            var colCourse = new Course($"Report Col Course {suffix}", $"RCC{suffix}", 3, collegeDepartment.Id);
            db.Courses.AddRange(uniCourse, colCourse);

            db.CourseOfferings.Add(new CourseOffering(uniCourse.Id, semester.Id, 30));
            db.CourseOfferings.Add(new CourseOffering(colCourse.Id, semester.Id, 30));

            await db.SaveChangesAsync();
        }

        using var client = CreateClient("SuperAdmin", "00000000-0000-0000-0000-000000000199");
        var response = await client.GetAsync($"api/v1/reports/enrollment-summary?institutionType={universityType}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var rows = doc.RootElement.GetProperty("rows").EnumerateArray().ToList();

        Assert.Contains(rows, r => (r.GetProperty("courseCode").GetString() ?? string.Empty).StartsWith("RUC", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(rows, r => (r.GetProperty("courseCode").GetString() ?? string.Empty).StartsWith("RCC", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ReportSections_WithSuperAdminSchoolOverride_ReturnsSchoolSectionsWithoutUniversitySpecificReports()
    {
        await EnsureReportsModuleActiveAsync();
        using var client = CreateClient("SuperAdmin", "00000000-0000-0000-0000-000000000299");

        var response = await client.GetAsync($"api/v1/reports/sections?institutionType={(int)InstitutionType.School}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)InstitutionType.School, doc.RootElement.GetProperty("effectiveInstitutionType").GetInt32());
        Assert.Equal(nameof(InstitutionType.School), doc.RootElement.GetProperty("institutionModel").GetString());

        var sections = doc.RootElement.GetProperty("sections").EnumerateArray().ToList();
        Assert.Contains(sections, s => s.GetProperty("sectionKey").GetString() == "school_outcomes");

        var allReportKeys = sections
            .SelectMany(s => s.GetProperty("reports").EnumerateArray())
            .Select(r => r.GetProperty("key").GetString())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("gpa_report", allReportKeys);
        Assert.DoesNotContain("fyp_status", allReportKeys);
    }

    [Fact]
    public async Task ReportSections_WithAdminInstitutionClaim_UsesClaimScopeWithoutQueryOverride()
    {
        await EnsureReportsModuleActiveAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(
                role: "Admin",
                userId: "00000000-0000-0000-0000-000000000399",
                institutionType: (int)InstitutionType.College));

        var response = await client.GetAsync("api/v1/reports/sections");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)InstitutionType.College, doc.RootElement.GetProperty("effectiveInstitutionType").GetInt32());
        Assert.Equal(nameof(InstitutionType.College), doc.RootElement.GetProperty("institutionModel").GetString());

        var sectionKeys = doc.RootElement
            .GetProperty("sections")
            .EnumerateArray()
            .Select(s => s.GetProperty("sectionKey").GetString())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("college_progression", sectionKeys);
        Assert.DoesNotContain("university_academics", sectionKeys);
    }

    [Fact]
    public async Task EnrollmentSummary_WithAdminClaimAndMismatchedInstitutionQuery_ReturnsForbidden()
    {
        Guid adminUserId;
        Guid departmentId;
        int adminInstitutionType;
        int mismatchedInstitutionType;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var adminRole = db.Roles.First(r => r.Name == "Admin");
            var department = db.Departments.FirstOrDefault(d => d.InstitutionType == InstitutionType.College);
            if (department is null)
            {
                var suffix = Guid.NewGuid().ToString("N")[..6];
                department = new Department($"Report Scope College {suffix}", $"RSC{suffix}", InstitutionType.College);
                db.Departments.Add(department);
                await db.SaveChangesAsync();
            }

            departmentId = department.Id;
            adminInstitutionType = (int)department.InstitutionType;
            mismatchedInstitutionType = adminInstitutionType == (int)InstitutionType.University
                ? (int)InstitutionType.College
                : (int)InstitutionType.University;

            var admin = new User(
                username: $"report_admin_mismatch_{Guid.NewGuid():N}",
                passwordHash: "integration-hash",
                roleId: adminRole.Id,
                email: $"report_admin_mismatch_{Guid.NewGuid():N}@tabsan.local",
                departmentId: null,
                mustChangePassword: false,
                institutionType: (InstitutionType)adminInstitutionType);

            db.Users.Add(admin);
            await db.SaveChangesAsync();

            db.AdminDepartmentAssignments.Add(new AdminDepartmentAssignment(admin.Id, departmentId));
            await db.SaveChangesAsync();

            adminUserId = admin.Id;
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(
                role: "Admin",
                userId: adminUserId.ToString(),
                institutionType: adminInstitutionType));

        var response = await client.GetAsync($"api/v1/reports/enrollment-summary?departmentId={departmentId}&institutionType={mismatchedInstitutionType}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EnrollmentSummary_WithAdminClaimAndMatchingInstitutionQuery_ReturnsOk()
    {
        Guid adminUserId;
        Guid departmentId;
        int adminInstitutionType;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var adminRole = db.Roles.First(r => r.Name == "Admin");
            var department = db.Departments.FirstOrDefault(d => d.InstitutionType == InstitutionType.College);
            if (department is null)
            {
                var suffix = Guid.NewGuid().ToString("N")[..6];
                department = new Department($"Report Scope Match College {suffix}", $"RMC{suffix}", InstitutionType.College);
                db.Departments.Add(department);
                await db.SaveChangesAsync();
            }

            departmentId = department.Id;
            adminInstitutionType = (int)department.InstitutionType;

            var admin = new User(
                username: $"report_admin_match_{Guid.NewGuid():N}",
                passwordHash: "integration-hash",
                roleId: adminRole.Id,
                email: $"report_admin_match_{Guid.NewGuid():N}@tabsan.local",
                departmentId: null,
                mustChangePassword: false,
                institutionType: (InstitutionType)adminInstitutionType);

            db.Users.Add(admin);
            await db.SaveChangesAsync();

            db.AdminDepartmentAssignments.Add(new AdminDepartmentAssignment(admin.Id, departmentId));
            await db.SaveChangesAsync();

            adminUserId = admin.Id;
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(
                role: "Admin",
                userId: adminUserId.ToString(),
                institutionType: adminInstitutionType));

        var response = await client.GetAsync($"api/v1/reports/enrollment-summary?departmentId={departmentId}&institutionType={adminInstitutionType}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EnrollmentSummary_WithAdminInstitutionMismatch_ReturnsForbidden()
    {
        Guid adminUserId;
        Guid departmentId;
        int mismatchedInstitutionType;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var adminRole = db.Roles.First(r => r.Name == "Admin");

            var department = db.Departments.FirstOrDefault();
            if (department is null)
            {
                var suffix = Guid.NewGuid().ToString("N")[..6];
                department = new Department($"Report Scope Dept {suffix}", $"RSD{suffix}", InstitutionType.University);
                db.Departments.Add(department);
                await db.SaveChangesAsync();
            }

            departmentId = department.Id;
            mismatchedInstitutionType = (((int)department.InstitutionType) + 1) % 3;

            var admin = new User(
                username: $"report_admin_{Guid.NewGuid():N}",
                passwordHash: "integration-hash",
                roleId: adminRole.Id,
                email: $"report_admin_{Guid.NewGuid():N}@tabsan.local",
                departmentId: null,
                mustChangePassword: false,
                institutionType: (InstitutionType)mismatchedInstitutionType);

            db.Users.Add(admin);
            await db.SaveChangesAsync();

            db.AdminDepartmentAssignments.Add(new AdminDepartmentAssignment(admin.Id, departmentId));
            await db.SaveChangesAsync();

            adminUserId = admin.Id;
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(
                role: "Admin",
                userId: adminUserId.ToString(),
                institutionType: mismatchedInstitutionType));

        var response = await client.GetAsync($"api/v1/reports/enrollment-summary?departmentId={departmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GpaReport_WithFacultyAndNoDepartment_ReturnsBadRequest()
    {
        var (facultyUserId, institutionType, _, _, _) = await SeedFacultyScopeFixtureAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(
                role: "Faculty",
                userId: facultyUserId.ToString(),
                institutionType: institutionType));

        var response = await client.GetAsync("api/v1/reports/gpa-report");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EnrollmentSummary_WithFacultyUnassignedDepartment_ReturnsForbidden()
    {
        var (facultyUserId, institutionType, _, deniedDepartmentId, _) = await SeedFacultyScopeFixtureAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(
                role: "Faculty",
                userId: facultyUserId.ToString(),
                institutionType: institutionType));

        var response = await client.GetAsync($"api/v1/reports/enrollment-summary?departmentId={deniedDepartmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task LowAttendance_WithFacultyAndNoFilters_ReturnsBadRequest()
    {
        var (facultyUserId, institutionType, _, _, _) = await SeedFacultyScopeFixtureAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(
                role: "Faculty",
                userId: facultyUserId.ToString(),
                institutionType: institutionType));

        var response = await client.GetAsync("api/v1/reports/low-attendance");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SemesterResults_WithFacultyUnassignedDepartment_ReturnsForbidden()
    {
        var (facultyUserId, institutionType, _, deniedDepartmentId, semesterId) = await SeedFacultyScopeFixtureAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(
                role: "Faculty",
                userId: facultyUserId.ToString(),
                institutionType: institutionType));

        var response = await client.GetAsync($"api/v1/reports/semester-results?semesterId={semesterId}&departmentId={deniedDepartmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task FypStatus_WithFacultyUnassignedDepartment_ReturnsForbidden()
    {
        var (facultyUserId, institutionType, _, deniedDepartmentId, _) = await SeedFacultyScopeFixtureAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(
                role: "Faculty",
                userId: facultyUserId.ToString(),
                institutionType: institutionType));

        var response = await client.GetAsync($"api/v1/reports/fyp-status?departmentId={deniedDepartmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<(Guid FacultyUserId, int InstitutionType, Guid AllowedDepartmentId, Guid DeniedDepartmentId, Guid SemesterId)> SeedFacultyScopeFixtureAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var role = db.Roles.First(r => r.Name == "Faculty");
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var allowedDepartment = new Department($"Report Faculty Allow {suffix}", $"RFA{suffix}", InstitutionType.University);
        var deniedDepartment = new Department($"Report Faculty Deny {suffix}", $"RFD{suffix}", InstitutionType.College);
        var semester = new Semester($"Report Faculty Scope Sem {suffix}", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(30));

        db.Departments.AddRange(allowedDepartment, deniedDepartment);
        db.Semesters.Add(semester);

        var faculty = new User(
            username: $"report_faculty_scope_{suffix}",
            passwordHash: "integration-hash",
            roleId: role.Id,
            email: $"report_faculty_scope_{suffix}@tabsan.local",
            departmentId: null,
            mustChangePassword: false,
            institutionType: allowedDepartment.InstitutionType);

        db.Users.Add(faculty);
        await db.SaveChangesAsync();

        db.FacultyDepartmentAssignments.Add(new FacultyDepartmentAssignment(faculty.Id, allowedDepartment.Id));
        await db.SaveChangesAsync();

        return (faculty.Id, (int)allowedDepartment.InstitutionType, allowedDepartment.Id, deniedDepartment.Id, semester.Id);
    }
}
