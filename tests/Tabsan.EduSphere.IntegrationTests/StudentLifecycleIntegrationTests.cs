using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.StudentLifecycle;
using Tabsan.EduSphere.Domain.Tenancy;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

[Collection(EduSphereCollection.Name)]
public class StudentLifecycleIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public StudentLifecycleIntegrationTests(EduSphereWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GraduationCandidates_WithAdminInstitutionMismatch_ReturnsForbidden()
    {
        var seeded = await SeedLifecycleScopeDataAsync(InstitutionType.University, InstitutionType.College);

        using var client = CreateAdminClient(seeded.AdminUserId, seeded.AdminInstitutionType);

        var response = await client.GetAsync($"api/v1/student-lifecycle/graduation-candidates/{seeded.DepartmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GraduationCandidates_WithAdminMatchingInstitution_ReturnsOk()
    {
        var seeded = await SeedLifecycleScopeDataAsync(InstitutionType.University, InstitutionType.University);

        using var client = CreateAdminClient(seeded.AdminUserId, seeded.AdminInstitutionType);

        var response = await client.GetAsync($"api/v1/student-lifecycle/graduation-candidates/{seeded.DepartmentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PromoteStudent_WithAdminInstitutionMismatch_ReturnsForbidden()
    {
        var seeded = await SeedLifecycleScopeDataAsync(InstitutionType.College, InstitutionType.University);

        using var client = CreateAdminClient(seeded.AdminUserId, seeded.AdminInstitutionType);

        var response = await client.PostAsync($"api/v1/student-lifecycle/{seeded.StudentProfileId}/promote", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AcademicLevelStudents_WithAdminMatchingInstitution_ReturnsOk()
    {
        var seeded = await SeedLifecycleScopeDataAsync(InstitutionType.School, InstitutionType.School);

        using var client = CreateAdminClient(seeded.AdminUserId, seeded.AdminInstitutionType);

        var response = await client.GetAsync($"api/v1/student-lifecycle/academic-level-students/{seeded.DepartmentId}/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PromoteStudent_SchoolWithoutPassingScore_ReturnsBadRequest()
    {
        var seeded = await SeedLifecycleScopeDataAsync(InstitutionType.School, InstitutionType.School);

        using var client = CreateAdminClient(seeded.AdminUserId, seeded.AdminInstitutionType);

        var response = await client.PostAsync($"api/v1/student-lifecycle/{seeded.StudentProfileId}/promote", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AcademicLevelStudents_CollegeYearOne_IncludesSemesterOneAndTwoStudents()
    {
        var seeded = await SeedLifecycleScopeDataAsync(
            InstitutionType.College,
            InstitutionType.College,
            includeSemesterTwoStudent: true,
            primaryStudentPassingStanding: true);

        using var client = CreateAdminClient(seeded.AdminUserId, seeded.AdminInstitutionType);

        var response = await client.GetAsync($"api/v1/student-lifecycle/academic-level-students/{seeded.DepartmentId}/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var students = await response.Content.ReadFromJsonAsync<List<SemesterPromotionSummary>>();
        Assert.NotNull(students);
        Assert.True(students!.Count >= 2);
    }

    [Fact]
    public async Task PromoteStudent_CollegePassingStudent_AdvancesByTwoSemesters()
    {
        var seeded = await SeedLifecycleScopeDataAsync(
            InstitutionType.College,
            InstitutionType.College,
            includeSemesterTwoStudent: false,
            primaryStudentPassingStanding: true);

        using var client = CreateAdminClient(seeded.AdminUserId, seeded.AdminInstitutionType);

        var response = await client.PostAsync($"api/v1/student-lifecycle/{seeded.StudentProfileId}/promote", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var student = await db.StudentProfiles.FindAsync(seeded.StudentProfileId);
        Assert.NotNull(student);
        Assert.Equal(3, student!.CurrentSemesterNumber);
    }

    [Fact]
    public async Task Payments_GetAll_WithMatchingTenantCampusScope_ReturnsScopedReceipt()
    {
        var seeded = await SeedPaymentScopeDataAsync();

        using var client = CreateFinanceClient(
            seeded.FinanceUserId,
            seeded.TenantId,
            seeded.CampusId);

        var response = await client.GetAsync("api/v1/payments?page=1&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PaymentReceiptPage>();
        Assert.NotNull(page);
        Assert.Contains(page!.Items, i => i.Id == seeded.ReceiptId);
    }

    [Fact]
    public async Task Payments_GetAll_WithMismatchedCampusScope_HidesOutOfScopeReceipt()
    {
        var seeded = await SeedPaymentScopeDataAsync();

        using var client = CreateFinanceClient(
            seeded.FinanceUserId,
            seeded.TenantId,
            seeded.OtherCampusId);

        var response = await client.GetAsync("api/v1/payments?page=1&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PaymentReceiptPage>();
        Assert.NotNull(page);
        Assert.DoesNotContain(page!.Items, i => i.Id == seeded.ReceiptId);
    }

    private HttpClient CreateAdminClient(Guid adminUserId, int institutionType)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.GenerateToken(
                role: "Admin",
                userId: adminUserId.ToString(),
                institutionType: institutionType));
        return client;
    }

    private HttpClient CreateFinanceClient(Guid financeUserId, Guid tenantId, Guid campusId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.GenerateToken(
                role: "Finance",
                userId: financeUserId.ToString(),
                tenantId: tenantId.ToString(),
                campusId: campusId.ToString()));
        return client;
    }

    private async Task<(Guid DepartmentId, Guid StudentProfileId, Guid AdminUserId, int AdminInstitutionType)> SeedLifecycleScopeDataAsync(
        InstitutionType departmentInstitutionType,
        InstitutionType adminInstitutionType,
        bool includeSemesterTwoStudent = false,
        bool primaryStudentPassingStanding = false)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var adminRole = db.Roles.First(r => r.Name == "Admin");
        var studentRole = db.Roles.First(r => r.Name == "Student");

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var department = new Department($"Lifecycle Dept {suffix}", $"LFD{suffix}", departmentInstitutionType);
        db.Departments.Add(department);

        var program = new AcademicProgram($"Lifecycle Program {suffix}", $"LFP{suffix}", department.Id, 8);
        db.AcademicPrograms.Add(program);

        var admin = new User(
            username: $"lifecycle_admin_{suffix}",
            passwordHash: "integration-hash",
            roleId: adminRole.Id,
            email: $"lifecycle_admin_{suffix}@tabsan.local",
            institutionType: adminInstitutionType);
        db.Users.Add(admin);

        var studentUser = new User(
            username: $"lifecycle_student_{suffix}",
            passwordHash: "integration-hash",
            roleId: studentRole.Id,
            email: $"lifecycle_student_{suffix}@tabsan.local");
        db.Users.Add(studentUser);

        var studentProfile = new StudentProfile(
            studentUser.Id,
            $"LIFE-{suffix}",
            program.Id,
            department.Id,
            DateTime.UtcNow.Date);

        if (primaryStudentPassingStanding)
            studentProfile.UpdateAcademicStanding(2.0m, 2.0m);

        db.StudentProfiles.Add(studentProfile);

        if (includeSemesterTwoStudent)
        {
            var studentUser2 = new User(
                username: $"lifecycle_student2_{suffix}",
                passwordHash: "integration-hash",
                roleId: studentRole.Id,
                email: $"lifecycle_student2_{suffix}@tabsan.local");
            db.Users.Add(studentUser2);

            var studentProfile2 = new StudentProfile(
                studentUser2.Id,
                $"LIFE2-{suffix}",
                program.Id,
                department.Id,
                DateTime.UtcNow.Date);
            studentProfile2.AdvanceSemester();
            studentProfile2.UpdateAcademicStanding(2.0m, 2.0m);
            db.StudentProfiles.Add(studentProfile2);
        }

        db.AdminDepartmentAssignments.Add(new AdminDepartmentAssignment(admin.Id, department.Id));

        await db.SaveChangesAsync();

        return (department.Id, studentProfile.Id, admin.Id, (int)adminInstitutionType);
    }

    private async Task<(Guid ReceiptId, Guid FinanceUserId, Guid TenantId, Guid CampusId, Guid OtherCampusId)> SeedPaymentScopeDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var tenantSuffix = Guid.NewGuid().ToString("N")[..6];
        var tenant = new Tenant($"TP{tenantSuffix}", $"Tenant {tenantSuffix}");
        db.Tenants.Add(tenant);

        var campus = new Campus(tenant.Id, $"CP{tenantSuffix}", $"Campus {tenantSuffix}");
        var otherCampus = new Campus(tenant.Id, $"CX{tenantSuffix}", $"Campus X {tenantSuffix}");
        db.Campuses.AddRange(campus, otherCampus);

        var financeRole = db.Roles.First(r => r.Name == "Finance");
        var studentRole = db.Roles.First(r => r.Name == "Student");

        var department = new Department($"Payments Dept {tenantSuffix}", $"PD{tenantSuffix}", InstitutionType.University);
        department.SetTenantCampus(tenant.Id, campus.Id);
        db.Departments.Add(department);

        var program = new AcademicProgram($"Payments Program {tenantSuffix}", $"PP{tenantSuffix}", department.Id, 8);
        db.AcademicPrograms.Add(program);

        var finance = new User(
            username: $"finance_scope_{tenantSuffix}",
            passwordHash: "integration-hash",
            roleId: financeRole.Id,
            email: $"finance_scope_{tenantSuffix}@tabsan.local",
            tenantId: tenant.Id,
            campusId: campus.Id);
        db.Users.Add(finance);

        var studentUser = new User(
            username: $"payment_student_{tenantSuffix}",
            passwordHash: "integration-hash",
            roleId: studentRole.Id,
            email: $"payment_student_{tenantSuffix}@tabsan.local",
            tenantId: tenant.Id,
            campusId: campus.Id);
        db.Users.Add(studentUser);

        var studentProfile = new StudentProfile(
            studentUser.Id,
            $"PAY-{tenantSuffix}",
            program.Id,
            department.Id,
            DateTime.UtcNow.Date);
        db.StudentProfiles.Add(studentProfile);

        var receipt = new PaymentReceipt(
            studentProfile.Id,
            finance.Id,
            1000m,
            $"INT-{tenantSuffix}-001",
            $"Scoped receipt {tenantSuffix}",
            DateTime.UtcNow.Date.AddDays(14));
        db.PaymentReceipts.Add(receipt);

        await db.SaveChangesAsync();

        return (receipt.Id, finance.Id, tenant.Id, campus.Id, otherCampus.Id);
    }

    private sealed record SemesterPromotionSummary(
        Guid StudentProfileId,
        string RegistrationNumber,
        string ProgramName,
        int CurrentSemesterNumber);

    private sealed record PaymentReceiptPage(
        IReadOnlyList<PaymentReceiptItem> Items,
        int Page,
        int PageSize,
        int TotalCount);

    private sealed record PaymentReceiptItem(
        Guid Id,
        Guid StudentProfileId,
        string StudentName,
        decimal Amount,
        string Description,
        DateTime DueDate,
        string Status);
}
