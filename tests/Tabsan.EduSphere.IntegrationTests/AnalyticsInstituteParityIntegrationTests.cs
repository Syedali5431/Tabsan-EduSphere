using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.StudentLifecycle;
using Tabsan.EduSphere.Domain.Tenancy;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

[Collection(EduSphereCollection.Name)]
public class AnalyticsInstituteParityIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public AnalyticsInstituteParityIntegrationTests(EduSphereWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnalyticsAssignments_WithMismatchedInstitutionQueryForAdminClaim_ReturnsForbidden()
    {
        using var client = CreateAdminClient(institutionType: (int)InstitutionType.College);

        var response = await client.GetAsync("api/analytics/assignments?institutionType=0");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AnalyticsAssignments_WithoutFilters_AutoScopesToAdminInstitutionClaim()
    {
        var seeded = await SeedAssignmentAnalyticsAcrossInstitutesAsync();

        using var client = CreateAdminClient(institutionType: (int)InstitutionType.College);

        var response = await client.GetAsync("api/analytics/assignments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var assignments = doc.RootElement
            .GetProperty("assignments")
            .EnumerateArray()
            .Select(x => x.GetProperty("title").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        Assert.Contains(seeded.CollegeAssignmentTitle, assignments);
        Assert.DoesNotContain(seeded.UniversityAssignmentTitle, assignments);
    }

    [Fact]
    public async Task AnalyticsAssignments_WithTenantCampusClaims_ReturnsOnlyCallerScopeData()
    {
        var seeded = await SeedAssignmentAnalyticsAcrossTenantCampusAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.GenerateToken(
                role: "Admin",
                userId: Guid.NewGuid().ToString(),
                institutionType: (int)InstitutionType.College,
                tenantId: seeded.CallerTenantId.ToString(),
                campusId: seeded.CallerCampusId.ToString()));

        var response = await client.GetAsync("api/analytics/assignments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var assignments = doc.RootElement
            .GetProperty("assignments")
            .EnumerateArray()
            .Select(x => x.GetProperty("title").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        Assert.Contains(seeded.CallerAssignmentTitle, assignments);
        Assert.DoesNotContain(seeded.OtherTenantAssignmentTitle, assignments);
    }

    [Fact]
    public async Task AnalyticsTopPerformers_WithoutFilters_AutoScopesToAdminInstitutionClaim()
    {
        var seeded = await SeedResultAnalyticsAcrossInstitutesAsync();

        using var client = CreateAdminClient(institutionType: (int)InstitutionType.College);
        var response = await client.GetAsync("api/analytics/top-performers?take=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)InstitutionType.College, doc.RootElement.GetProperty("effectiveInstitutionType").GetInt32());

        var rows = doc.RootElement.GetProperty("rows").EnumerateArray().ToList();
        Assert.NotEmpty(rows);

        var registrations = rows
            .Select(x => x.GetProperty("registrationNumber").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        Assert.Contains(seeded.CollegeRegistration, registrations);
        Assert.DoesNotContain(seeded.UniversityRegistration, registrations);
    }

    [Fact]
    public async Task AnalyticsPerformanceTrends_WithoutFilters_AutoScopesToAdminInstitutionClaim()
    {
        var seeded = await SeedResultAnalyticsAcrossInstitutesAsync();

        using var client = CreateAdminClient(institutionType: (int)InstitutionType.College);
        var response = await client.GetAsync("api/analytics/performance-trends?windowDays=30");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)InstitutionType.College, doc.RootElement.GetProperty("effectiveInstitutionType").GetInt32());

        var points = doc.RootElement.GetProperty("points").EnumerateArray().ToList();
        Assert.NotEmpty(points);
        Assert.All(points, p => Assert.True(p.GetProperty("resultCount").GetInt32() >= 1));

        var averages = points.Select(p => p.GetProperty("averagePercentage").GetDecimal()).ToList();
        Assert.Contains(averages, x => x >= seeded.ExpectedCollegePercentage - 0.01m);
    }

    [Fact]
    public async Task AnalyticsComparativeSummary_WithoutFilters_AutoScopesToAdminInstitutionClaim()
    {
        var seeded = await SeedResultAnalyticsAcrossInstitutesAsync();

        using var client = CreateAdminClient(institutionType: (int)InstitutionType.College);
        var response = await client.GetAsync("api/analytics/comparative-summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)InstitutionType.College, doc.RootElement.GetProperty("effectiveInstitutionType").GetInt32());

        var rows = doc.RootElement.GetProperty("rows").EnumerateArray().ToList();
        Assert.NotEmpty(rows);

        var departmentNames = rows
            .Select(r => r.GetProperty("departmentName").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        Assert.Contains(seeded.CollegeDepartmentName, departmentNames);
        Assert.DoesNotContain(seeded.UniversityDepartmentName, departmentNames);
    }

    [Fact]
    public async Task AnalyticsPaymentStatus_WithMismatchedInstitutionQueryForAdminClaim_ReturnsForbidden()
    {
        using var client = CreateAdminClient(institutionType: (int)InstitutionType.College);

        var response = await client.GetAsync("api/analytics/payment-status?institutionType=0");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AnalyticsPaymentStatus_WithTenantCampusClaims_ReturnsOnlyCallerScopeData()
    {
        var seeded = await SeedPaymentAnalyticsAcrossTenantCampusAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.GenerateToken(
                role: "Admin",
                userId: Guid.NewGuid().ToString(),
                institutionType: (int)InstitutionType.College,
                tenantId: seeded.CallerTenantId.ToString(),
                campusId: seeded.CallerCampusId.ToString()));

        var response = await client.GetAsync("api/analytics/payment-status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(seeded.CallerPaidCount, doc.RootElement.GetProperty("paidCount").GetInt32());
        Assert.Equal(seeded.CallerUnpaidCount, doc.RootElement.GetProperty("unpaidCount").GetInt32());

        var statuses = doc.RootElement.GetProperty("slices")
            .EnumerateArray()
            .Select(x => x.GetProperty("status").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        Assert.Contains("Paid", statuses);
        Assert.Contains("Unpaid", statuses);
    }

    [Fact]
    public async Task AnalyticsPaymentStatus_WithCourseAndSemesterFilters_ReturnsMatchingEnrollmentScope()
    {
        var seeded = await SeedPaymentAnalyticsWithCourseSemesterScopeAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.GenerateToken(
                role: "Admin",
                userId: Guid.NewGuid().ToString(),
                institutionType: (int)InstitutionType.College,
                tenantId: seeded.CallerTenantId.ToString(),
                campusId: seeded.CallerCampusId.ToString()));

        var response = await client.GetAsync($"api/analytics/payment-status?courseId={seeded.FilterCourseId}&semesterId={seeded.FilterSemesterId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, doc.RootElement.GetProperty("paidCount").GetInt32());
        Assert.Equal(0, doc.RootElement.GetProperty("unpaidCount").GetInt32());
    }

    private HttpClient CreateAdminClient(int institutionType)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.GenerateToken(
                role: "Admin",
                userId: Guid.NewGuid().ToString(),
                institutionType: institutionType));
        return client;
    }

    private async Task EnsureReportsModuleActiveAsync(CancellationToken ct = default)
    {
        using var superClient = _factory.CreateClient();
        superClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken("SuperAdmin", Guid.NewGuid().ToString()));

        var statusResponse = await superClient.GetAsync("api/v1/module/reports/status", ct);
        statusResponse.EnsureSuccessStatusCode();

        using var statusDoc = JsonDocument.Parse(await statusResponse.Content.ReadAsStringAsync(ct));
        if (statusDoc.RootElement.GetProperty("isActive").GetBoolean())
            return;

        var activateResponse = await superClient.PostAsync("api/v1/module/reports/activate", content: null, ct);
        activateResponse.EnsureSuccessStatusCode();
    }

    private async Task<(string CollegeAssignmentTitle, string UniversityAssignmentTitle)> SeedAssignmentAnalyticsAcrossInstitutesAsync()
    {
        await EnsureReportsModuleActiveAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var suffix = Guid.NewGuid().ToString("N")[..6];

        var collegeDepartment = new Department($"Analytics College {suffix}", $"AC{suffix}", InstitutionType.College);
        var universityDepartment = new Department($"Analytics University {suffix}", $"AU{suffix}", InstitutionType.University);
        db.Departments.AddRange(collegeDepartment, universityDepartment);

        var semester = new Semester($"Analytics Sem {suffix}", DateTime.UtcNow.Date.AddDays(-15), DateTime.UtcNow.Date.AddDays(75));
        db.Semesters.Add(semester);

        var collegeCourse = new Course($"Analytics College Course {suffix}", $"ACC{suffix}", 3, collegeDepartment.Id);
        var universityCourse = new Course($"Analytics University Course {suffix}", $"AUC{suffix}", 3, universityDepartment.Id);
        db.Courses.AddRange(collegeCourse, universityCourse);

        var collegeOffering = new CourseOffering(collegeCourse.Id, semester.Id, 50);
        var universityOffering = new CourseOffering(universityCourse.Id, semester.Id, 50);
        db.CourseOfferings.AddRange(collegeOffering, universityOffering);

        var collegeAssignmentTitle = $"College Assignment {suffix}";
        var universityAssignmentTitle = $"University Assignment {suffix}";
        db.Assignments.Add(new Assignment(collegeOffering.Id, collegeAssignmentTitle, "seed", DateTime.UtcNow.Date.AddDays(10), 100));
        db.Assignments.Add(new Assignment(universityOffering.Id, universityAssignmentTitle, "seed", DateTime.UtcNow.Date.AddDays(10), 100));

        await db.SaveChangesAsync();

        return (collegeAssignmentTitle, universityAssignmentTitle);
    }

    private async Task<(
        Guid CallerTenantId,
        Guid CallerCampusId,
        string CallerAssignmentTitle,
        string OtherTenantAssignmentTitle)> SeedAssignmentAnalyticsAcrossTenantCampusAsync()
    {
        await EnsureReportsModuleActiveAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var callerTenant = new Tenant($"ATC{suffix}", $"Analytics Tenant Caller {suffix}");
        var otherTenant = new Tenant($"ATO{suffix}", $"Analytics Tenant Other {suffix}");
        db.Tenants.AddRange(callerTenant, otherTenant);

        var callerCampus = new Campus(callerTenant.Id, $"ATCC{suffix}", $"Analytics Caller Campus {suffix}");
        var otherCampus = new Campus(otherTenant.Id, $"ATOC{suffix}", $"Analytics Other Campus {suffix}");
        db.Campuses.AddRange(callerCampus, otherCampus);

        var callerDepartment = new Department($"Analytics Tenant Caller {suffix}", $"ATC{suffix}", InstitutionType.College);
        callerDepartment.SetTenantCampus(callerTenant.Id, callerCampus.Id);

        var otherDepartment = new Department($"Analytics Tenant Other {suffix}", $"ATO{suffix}", InstitutionType.College);
        otherDepartment.SetTenantCampus(otherTenant.Id, otherCampus.Id);

        db.Departments.AddRange(callerDepartment, otherDepartment);

        var semester = new Semester($"Analytics Tenant Sem {suffix}", DateTime.UtcNow.Date.AddDays(-10), DateTime.UtcNow.Date.AddDays(70));
        db.Semesters.Add(semester);

        var callerCourse = new Course($"Analytics Tenant Caller Course {suffix}", $"ATCC{suffix}", 3, callerDepartment.Id);
        var otherCourse = new Course($"Analytics Tenant Other Course {suffix}", $"ATOC{suffix}", 3, otherDepartment.Id);
        db.Courses.AddRange(callerCourse, otherCourse);

        var callerOffering = new CourseOffering(callerCourse.Id, semester.Id, 40);
        var otherOffering = new CourseOffering(otherCourse.Id, semester.Id, 40);
        db.CourseOfferings.AddRange(callerOffering, otherOffering);

        var callerAssignmentTitle = $"Tenant Caller Assignment {suffix}";
        var otherTenantAssignmentTitle = $"Tenant Other Assignment {suffix}";
        db.Assignments.Add(new Assignment(callerOffering.Id, callerAssignmentTitle, "seed", DateTime.UtcNow.Date.AddDays(10), 100));
        db.Assignments.Add(new Assignment(otherOffering.Id, otherTenantAssignmentTitle, "seed", DateTime.UtcNow.Date.AddDays(10), 100));

        await db.SaveChangesAsync();

        return (callerTenant.Id, callerCampus.Id, callerAssignmentTitle, otherTenantAssignmentTitle);
    }

    private async Task<(
        string CollegeRegistration,
        string UniversityRegistration,
        decimal ExpectedCollegePercentage,
        string CollegeDepartmentName,
        string UniversityDepartmentName)> SeedResultAnalyticsAcrossInstitutesAsync()
    {
        await EnsureReportsModuleActiveAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var studentRole = db.Roles.First(r => r.Name == "Student");
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var collegeDepartment = new Department($"Analytics Result College {suffix}", $"ARC{suffix}", InstitutionType.College);
        var universityDepartment = new Department($"Analytics Result University {suffix}", $"ARU{suffix}", InstitutionType.University);
        db.Departments.AddRange(collegeDepartment, universityDepartment);

        var collegeProgram = new AcademicProgram($"Analytics College Program {suffix}", $"ACP{suffix}", collegeDepartment.Id, 4);
        var universityProgram = new AcademicProgram($"Analytics University Program {suffix}", $"AUP{suffix}", universityDepartment.Id, 8);
        db.AcademicPrograms.AddRange(collegeProgram, universityProgram);

        var semester = new Semester($"Analytics Result Sem {suffix}", DateTime.UtcNow.Date.AddDays(-20), DateTime.UtcNow.Date.AddDays(40));
        db.Semesters.Add(semester);

        var collegeCourse = new Course($"Analytics Result College Course {suffix}", $"ARCC{suffix}", 3, collegeDepartment.Id);
        var universityCourse = new Course($"Analytics Result University Course {suffix}", $"ARUC{suffix}", 3, universityDepartment.Id);
        db.Courses.AddRange(collegeCourse, universityCourse);

        var collegeOffering = new CourseOffering(collegeCourse.Id, semester.Id, 30);
        var universityOffering = new CourseOffering(universityCourse.Id, semester.Id, 30);
        db.CourseOfferings.AddRange(collegeOffering, universityOffering);

        var collegeStudentUser = new User($"analytics_col_student_{suffix}", "integration-hash", studentRole.Id, $"analytics_col_student_{suffix}@tabsan.local");
        var universityStudentUser = new User($"analytics_uni_student_{suffix}", "integration-hash", studentRole.Id, $"analytics_uni_student_{suffix}@tabsan.local");
        db.Users.AddRange(collegeStudentUser, universityStudentUser);

        var collegeRegistration = $"ARC-ST-{suffix}";
        var universityRegistration = $"ARU-ST-{suffix}";
        var collegeProfile = new StudentProfile(collegeStudentUser.Id, collegeRegistration, collegeProgram.Id, collegeDepartment.Id, DateTime.UtcNow.Date);
        var universityProfile = new StudentProfile(universityStudentUser.Id, universityRegistration, universityProgram.Id, universityDepartment.Id, DateTime.UtcNow.Date);
        db.StudentProfiles.AddRange(collegeProfile, universityProfile);

        var collegeResult = new Result(collegeProfile.Id, collegeOffering.Id, "Total", 88m, 100m);
        collegeResult.Publish(Guid.NewGuid());
        var universityResult = new Result(universityProfile.Id, universityOffering.Id, "Total", 54m, 100m);
        universityResult.Publish(Guid.NewGuid());
        db.Results.AddRange(collegeResult, universityResult);

        await db.SaveChangesAsync();

        return (
            collegeRegistration,
            universityRegistration,
            88m,
            collegeDepartment.Name,
            universityDepartment.Name);
    }

    private async Task<(
        Guid CallerTenantId,
        Guid CallerCampusId,
        int CallerPaidCount,
        int CallerUnpaidCount)> SeedPaymentAnalyticsAcrossTenantCampusAsync()
    {
        await EnsureReportsModuleActiveAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var studentRole = db.Roles.First(r => r.Name == "Student");
        var financeRole = db.Roles.First(r => r.Name == "Finance");
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var callerTenant = new Tenant($"PTC{suffix}", $"Payment Tenant Caller {suffix}");
        var otherTenant = new Tenant($"PTO{suffix}", $"Payment Tenant Other {suffix}");
        db.Tenants.AddRange(callerTenant, otherTenant);

        var callerCampus = new Campus(callerTenant.Id, $"PTCC{suffix}", $"Payment Caller Campus {suffix}");
        var otherCampus = new Campus(otherTenant.Id, $"PTOC{suffix}", $"Payment Other Campus {suffix}");
        db.Campuses.AddRange(callerCampus, otherCampus);

        var callerDepartment = new Department($"Payment Caller Department {suffix}", $"PCD{suffix}", InstitutionType.College);
        callerDepartment.SetTenantCampus(callerTenant.Id, callerCampus.Id);
        var otherDepartment = new Department($"Payment Other Department {suffix}", $"POD{suffix}", InstitutionType.College);
        otherDepartment.SetTenantCampus(otherTenant.Id, otherCampus.Id);
        db.Departments.AddRange(callerDepartment, otherDepartment);

        var callerProgram = new AcademicProgram($"Payment Caller Program {suffix}", $"PCP{suffix}", callerDepartment.Id, 4);
        var otherProgram = new AcademicProgram($"Payment Other Program {suffix}", $"POP{suffix}", otherDepartment.Id, 4);
        db.AcademicPrograms.AddRange(callerProgram, otherProgram);

        var callerStudentUser = new User($"pay_caller_student_{suffix}", "integration-hash", studentRole.Id, $"pay_caller_student_{suffix}@tabsan.local");
        var otherStudentUser = new User($"pay_other_student_{suffix}", "integration-hash", studentRole.Id, $"pay_other_student_{suffix}@tabsan.local");
        var financeUser = new User($"pay_finance_{suffix}", "integration-hash", financeRole.Id, $"pay_finance_{suffix}@tabsan.local");
        db.Users.AddRange(callerStudentUser, otherStudentUser, financeUser);

        var callerProfile = new StudentProfile(callerStudentUser.Id, $"PAY-CALL-{suffix}", callerProgram.Id, callerDepartment.Id, DateTime.UtcNow.Date);
        var otherProfile = new StudentProfile(otherStudentUser.Id, $"PAY-OTH-{suffix}", otherProgram.Id, otherDepartment.Id, DateTime.UtcNow.Date);
        db.StudentProfiles.AddRange(callerProfile, otherProfile);

        var callerPaid = new PaymentReceipt(callerProfile.Id, financeUser.Id, 100m, "Caller paid", DateTime.UtcNow.Date.AddDays(7));
        callerPaid.ConfirmPayment(financeUser.Id, "paid");
        var callerUnpaid = new PaymentReceipt(callerProfile.Id, financeUser.Id, 75m, "Caller unpaid", DateTime.UtcNow.Date.AddDays(7));

        var otherPaid = new PaymentReceipt(otherProfile.Id, financeUser.Id, 120m, "Other paid", DateTime.UtcNow.Date.AddDays(7));
        otherPaid.ConfirmPayment(financeUser.Id, "paid");
        var otherUnpaid = new PaymentReceipt(otherProfile.Id, financeUser.Id, 90m, "Other unpaid", DateTime.UtcNow.Date.AddDays(7));

        db.PaymentReceipts.AddRange(callerPaid, callerUnpaid, otherPaid, otherUnpaid);
        await db.SaveChangesAsync();

        return (callerTenant.Id, callerCampus.Id, CallerPaidCount: 1, CallerUnpaidCount: 1);
    }

    private async Task<(Guid CallerTenantId, Guid CallerCampusId, Guid FilterCourseId, Guid FilterSemesterId)>
        SeedPaymentAnalyticsWithCourseSemesterScopeAsync()
    {
        await EnsureReportsModuleActiveAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var studentRole = db.Roles.First(r => r.Name == "Student");
        var financeRole = db.Roles.First(r => r.Name == "Finance");
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var tenant = new Tenant($"PTS{suffix}", $"Payment Tenant Scoped {suffix}");
        db.Tenants.Add(tenant);

        var campus = new Campus(tenant.Id, $"PTSC{suffix}", $"Payment Scoped Campus {suffix}");
        db.Campuses.Add(campus);

        var department = new Department($"Payment Scoped Department {suffix}", $"PSD{suffix}", InstitutionType.College);
        department.SetTenantCampus(tenant.Id, campus.Id);
        db.Departments.Add(department);

        var program = new AcademicProgram($"Payment Scoped Program {suffix}", $"PSP{suffix}", department.Id, 4);
        db.AcademicPrograms.Add(program);

        var semesterFilter = new Semester($"Payment Scoped Sem A {suffix}", DateTime.UtcNow.Date.AddDays(-10), DateTime.UtcNow.Date.AddDays(80));
        var semesterOther = new Semester($"Payment Scoped Sem B {suffix}", DateTime.UtcNow.Date.AddDays(-10), DateTime.UtcNow.Date.AddDays(80));
        db.Semesters.AddRange(semesterFilter, semesterOther);

        var courseFilter = new Course($"Payment Scoped Course A {suffix}", $"PSCA{suffix}", 3, department.Id);
        var courseOther = new Course($"Payment Scoped Course B {suffix}", $"PSCB{suffix}", 3, department.Id);
        db.Courses.AddRange(courseFilter, courseOther);

        var filterOffering = new CourseOffering(courseFilter.Id, semesterFilter.Id, 30);
        var otherOffering = new CourseOffering(courseOther.Id, semesterOther.Id, 30);
        db.CourseOfferings.AddRange(filterOffering, otherOffering);

        var matchedStudentUser = new User($"pay_scope_match_{suffix}", "integration-hash", studentRole.Id, $"pay_scope_match_{suffix}@tabsan.local");
        var otherStudentUser = new User($"pay_scope_other_{suffix}", "integration-hash", studentRole.Id, $"pay_scope_other_{suffix}@tabsan.local");
        var financeUser = new User($"pay_scope_finance_{suffix}", "integration-hash", financeRole.Id, $"pay_scope_finance_{suffix}@tabsan.local");
        db.Users.AddRange(matchedStudentUser, otherStudentUser, financeUser);

        var matchedProfile = new StudentProfile(matchedStudentUser.Id, $"PAY-SCOPE-M-{suffix}", program.Id, department.Id, DateTime.UtcNow.Date);
        var otherProfile = new StudentProfile(otherStudentUser.Id, $"PAY-SCOPE-O-{suffix}", program.Id, department.Id, DateTime.UtcNow.Date);
        db.StudentProfiles.AddRange(matchedProfile, otherProfile);

        db.Enrollments.Add(new Enrollment(matchedProfile.Id, filterOffering.Id));
        db.Enrollments.Add(new Enrollment(otherProfile.Id, otherOffering.Id));

        var matchedReceipt = new PaymentReceipt(matchedProfile.Id, financeUser.Id, 110m, "Scoped matched", DateTime.UtcNow.Date.AddDays(7));
        matchedReceipt.ConfirmPayment(financeUser.Id, "paid");
        var otherReceipt = new PaymentReceipt(otherProfile.Id, financeUser.Id, 95m, "Scoped other", DateTime.UtcNow.Date.AddDays(7));

        db.PaymentReceipts.AddRange(matchedReceipt, otherReceipt);
        await db.SaveChangesAsync();

        return (tenant.Id, campus.Id, courseFilter.Id, semesterFilter.Id);
    }
}