using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Authorization regression tests that codify the expected HTTP status codes
/// for Attendance, Assignment, Quiz, and Result endpoints across all roles.
/// These tests ensure that authorization fixes remain stable and 403/401 regressions
/// are caught immediately.
///
/// Convention:
///   - Unauthenticated → 401
///   - Wrong role      → 403
///   - Correct role    → NOT 401 or 403 (the business layer may return 400/404 for test data)
/// </summary>
[Collection(EduSphereCollection.Name)]
public class AuthorizationRegressionTests
{
    private readonly EduSphereWebFactory _factory;

    public AuthorizationRegressionTests(EduSphereWebFactory factory)
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

    private HttpClient CreateScopedClient(
        string role,
        string userId = "00000000-0000-0000-0000-000000000001",
        string tenantId = "00000000-0000-0000-0000-000000000111",
        string campusId = "00000000-0000-0000-0000-000000000222")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                JwtTestHelper.GenerateToken(
                    role,
                    userId,
                    tenantId: tenantId,
                    campusId: campusId));
        return client;
    }

    private HttpClient CreateUnauthenticatedClient() => _factory.CreateClient();

    // ─────────────────────────────────────────────────────────────────────────
    // ATTENDANCE
    // ─────────────────────────────────────────────────────────────────────────

    // GET /api/v1/attendance/by-offering/{id}  — requires SuperAdmin, Admin, Faculty
    [Fact]
    public async Task Attendance_GetByOffering_Unauthenticated_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.GetAsync($"api/v1/attendance/by-offering/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Attendance_GetByOffering_Student_Returns403()
    {
        using var client = CreateClient("Student");
        var response = await client.GetAsync($"api/v1/attendance/by-offering/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Attendance_GetByOffering_Finance_Returns403()
    {
        using var client = CreateClient("Finance");
        var response = await client.GetAsync($"api/v1/attendance/by-offering/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Attendance_GetByOffering_Faculty_ReturnsNotForbiddenOrUnauthorized()
    {
        using var client = CreateClient("Faculty");
        var response = await client.GetAsync($"api/v1/attendance/by-offering/{Guid.NewGuid()}");
        Assert.NotEqual(HttpStatusCode.Forbidden,     response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized,  response.StatusCode);
    }

    [Fact]
    public async Task Attendance_GetByOffering_Admin_ReturnsNotForbiddenOrUnauthorized()
    {
        using var client = CreateClient("Admin");
        var response = await client.GetAsync($"api/v1/attendance/by-offering/{Guid.NewGuid()}");
        Assert.NotEqual(HttpStatusCode.Forbidden,     response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized,  response.StatusCode);
    }

    [Fact]
    public async Task Attendance_GetByOffering_SuperAdmin_ReturnsNotForbiddenOrUnauthorized()
    {
        using var client = CreateClient("SuperAdmin");
        var response = await client.GetAsync($"api/v1/attendance/by-offering/{Guid.NewGuid()}");
        Assert.NotEqual(HttpStatusCode.Forbidden,     response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized,  response.StatusCode);
    }

    // GET /api/v1/attendance/my-attendance  — requires Student only
    [Fact]
    public async Task Attendance_MyAttendance_Unauthenticated_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.GetAsync("api/v1/attendance/my-attendance");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Attendance_MyAttendance_Faculty_Returns403()
    {
        using var client = CreateClient("Faculty");
        var response = await client.GetAsync("api/v1/attendance/my-attendance");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Attendance_MyAttendance_Admin_Returns403()
    {
        using var client = CreateClient("Admin");
        var response = await client.GetAsync("api/v1/attendance/my-attendance");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // GET /api/v1/attendance/below-threshold  — requires SuperAdmin, Admin only
    [Fact]
    public async Task Attendance_BelowThreshold_Unauthenticated_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.GetAsync("api/v1/attendance/below-threshold");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Attendance_BelowThreshold_Faculty_Returns403()
    {
        using var client = CreateClient("Faculty");
        var response = await client.GetAsync("api/v1/attendance/below-threshold");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Attendance_BelowThreshold_Student_Returns403()
    {
        using var client = CreateClient("Student");
        var response = await client.GetAsync("api/v1/attendance/below-threshold");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Attendance_BelowThreshold_Admin_ReturnsNotForbiddenOrUnauthorized()
    {
        using var client = CreateClient("Admin");
        var response = await client.GetAsync("api/v1/attendance/below-threshold");
        Assert.NotEqual(HttpStatusCode.Forbidden,    response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Attendance_BelowThreshold_SuperAdmin_ReturnsNotForbiddenOrUnauthorized()
    {
        using var client = CreateClient("SuperAdmin");
        var response = await client.GetAsync("api/v1/attendance/below-threshold");
        Assert.NotEqual(HttpStatusCode.Forbidden,    response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ASSIGNMENT
    // ─────────────────────────────────────────────────────────────────────────

    // DELETE /api/v1/assignment/{id}  — requires SuperAdmin, Admin only
    [Fact]
    public async Task Assignment_Delete_Unauthenticated_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.DeleteAsync($"api/v1/assignment/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Assignment_Delete_Faculty_Returns403()
    {
        using var client = CreateClient("Faculty");
        var response = await client.DeleteAsync($"api/v1/assignment/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Assignment_Delete_Student_Returns403()
    {
        using var client = CreateClient("Student");
        var response = await client.DeleteAsync($"api/v1/assignment/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Assignment_Delete_Finance_Returns403()
    {
        using var client = CreateClient("Finance");
        var response = await client.DeleteAsync($"api/v1/assignment/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Assignment_Delete_Admin_ReturnsNotForbiddenOrUnauthorized()
    {
        using var client = CreateClient("Admin");
        var response = await client.DeleteAsync($"api/v1/assignment/{Guid.NewGuid()}");
        Assert.NotEqual(HttpStatusCode.Forbidden,    response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // GET /api/v1/assignment/my-submissions  — requires Student only
    [Fact]
    public async Task Assignment_MySubmissions_Unauthenticated_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.GetAsync("api/v1/assignment/my-submissions");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Assignment_MySubmissions_Faculty_Returns403()
    {
        using var client = CreateClient("Faculty");
        var response = await client.GetAsync("api/v1/assignment/my-submissions");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Assignment_MySubmissions_Admin_Returns403()
    {
        using var client = CreateClient("Admin");
        var response = await client.GetAsync("api/v1/assignment/my-submissions");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // POST /api/v1/assignment  — requires SuperAdmin, Admin, Faculty
    [Fact]
    public async Task Assignment_Create_Unauthenticated_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.PostAsync("api/v1/assignment", JsonContent.Create(new { }));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Assignment_Create_Student_Returns403()
    {
        using var client = CreateClient("Student");
        var response = await client.PostAsync("api/v1/assignment", JsonContent.Create(new { }));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Assignment_Create_Finance_Returns403()
    {
        using var client = CreateClient("Finance");
        var response = await client.PostAsync("api/v1/assignment", JsonContent.Create(new { }));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Payments_Delete_Finance_ReturnsMethodNotAllowed()
    {
        using var client = CreateClient("Finance");
        var response = await client.DeleteAsync($"api/v1/payments/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task Assignment_Create_Faculty_ReturnsNotForbiddenOrUnauthorized()
    {
        using var client = CreateClient("Faculty");
        var response = await client.PostAsync("api/v1/assignment", JsonContent.Create(new { }));
        Assert.NotEqual(HttpStatusCode.Forbidden,    response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Assignment_Create_Admin_ReturnsNotForbiddenOrUnauthorized()
    {
        using var client = CreateClient("Admin");
        var response = await client.PostAsync("api/v1/assignment", JsonContent.Create(new { }));
        Assert.NotEqual(HttpStatusCode.Forbidden,    response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // QUIZ
    // ─────────────────────────────────────────────────────────────────────────

    // POST /api/v1/quiz  — requires Policy "Faculty" (SuperAdmin, Admin, Faculty)
    [Fact]
    public async Task Quiz_Create_Unauthenticated_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.PostAsync("api/v1/quiz", JsonContent.Create(new { }));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Quiz_Create_Student_Returns403()
    {
        using var client = CreateClient("Student");
        var response = await client.PostAsync("api/v1/quiz", JsonContent.Create(new { }));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Quiz_Create_Faculty_ReturnsNotForbiddenOrUnauthorized()
    {
        using var client = CreateClient("Faculty");
        var response = await client.PostAsync("api/v1/quiz", JsonContent.Create(new { }));
        Assert.NotEqual(HttpStatusCode.Forbidden,    response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Quiz_Create_Admin_ReturnsNotForbiddenOrUnauthorized()
    {
        using var client = CreateClient("Admin");
        var response = await client.PostAsync("api/v1/quiz", JsonContent.Create(new { }));
        Assert.NotEqual(HttpStatusCode.Forbidden,    response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Quiz_Create_SuperAdmin_ReturnsNotForbiddenOrUnauthorized()
    {
        using var client = CreateClient("SuperAdmin");
        var response = await client.PostAsync("api/v1/quiz", JsonContent.Create(new { }));
        Assert.NotEqual(HttpStatusCode.Forbidden,    response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // DELETE /api/v1/quiz/{id}  — requires Policy "Admin" (SuperAdmin, Admin)
    [Fact]
    public async Task Quiz_Delete_Unauthenticated_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.DeleteAsync($"api/v1/quiz/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Quiz_Delete_Faculty_Returns403()
    {
        using var client = CreateClient("Faculty");
        var response = await client.DeleteAsync($"api/v1/quiz/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Quiz_Delete_Student_Returns403()
    {
        using var client = CreateClient("Student");
        var response = await client.DeleteAsync($"api/v1/quiz/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Quiz_Delete_Admin_ReturnsNotForbiddenOrUnauthorized()
    {
        using var client = CreateClient("Admin");
        var response = await client.DeleteAsync($"api/v1/quiz/{Guid.NewGuid()}");
        Assert.NotEqual(HttpStatusCode.Forbidden,    response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // POST /api/v1/quiz/{id}/start  — requires Policy "Student" (all roles pass)
    [Fact]
    public async Task Quiz_Start_Unauthenticated_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.PostAsync($"api/v1/quiz/{Guid.NewGuid()}/start", JsonContent.Create(new { }));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Quiz_Start_Student_ReturnsNotForbiddenOrUnauthorized()
    {
        using var client = CreateClient("Student");
        var response = await client.PostAsync($"api/v1/quiz/{Guid.NewGuid()}/start", JsonContent.Create(new { }));
        Assert.NotEqual(HttpStatusCode.Forbidden,    response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RESULT
    // ─────────────────────────────────────────────────────────────────────────

    // GET /api/v1/result/my-results  — requires Student only
    [Fact]
    public async Task Result_MyResults_Unauthenticated_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.GetAsync("api/v1/result/my-results");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Result_MyResults_Faculty_Returns403()
    {
        using var client = CreateClient("Faculty");
        var response = await client.GetAsync("api/v1/result/my-results");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Result_MyResults_Admin_Returns403()
    {
        using var client = CreateClient("Admin");
        var response = await client.GetAsync("api/v1/result/my-results");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // GET /api/v1/result/by-offering/{id}  — requires SuperAdmin, Admin, Faculty
    [Fact]
    public async Task Result_GetByOffering_Unauthenticated_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.GetAsync($"api/v1/result/by-offering/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Result_GetByOffering_Student_Returns403()
    {
        using var client = CreateClient("Student");
        var response = await client.GetAsync($"api/v1/result/by-offering/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Result_GetByOffering_Faculty_ReturnsNotForbiddenOrUnauthorized()
    {
        using var client = CreateClient("Faculty");
        var response = await client.GetAsync($"api/v1/result/by-offering/{Guid.NewGuid()}");
        Assert.NotEqual(HttpStatusCode.Forbidden,    response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Result_GetByOffering_Admin_ReturnsNotForbiddenOrUnauthorized()
    {
        using var client = CreateClient("Admin");
        var response = await client.GetAsync($"api/v1/result/by-offering/{Guid.NewGuid()}");
        Assert.NotEqual(HttpStatusCode.Forbidden,    response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Result_GetByOffering_SuperAdmin_ReturnsNotForbiddenOrUnauthorized()
    {
        using var client = CreateClient("SuperAdmin");
        var response = await client.GetAsync($"api/v1/result/by-offering/{Guid.NewGuid()}");
        Assert.NotEqual(HttpStatusCode.Forbidden,    response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // PUT /api/v1/result/correct  — requires SuperAdmin, Admin only
    [Fact]
    public async Task Result_Correct_Faculty_Returns403()
    {
        using var client = CreateClient("Faculty");
        var response = await client.PutAsync(
            $"api/v1/result/correct?studentProfileId={Guid.NewGuid()}&courseOfferingId={Guid.NewGuid()}&resultType=Final",
            JsonContent.Create(new { marksObtained = 80, maxMarks = 100 }));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Result_Correct_Student_Returns403()
    {
        using var client = CreateClient("Student");
        var response = await client.PutAsync(
            $"api/v1/result/correct?studentProfileId={Guid.NewGuid()}&courseOfferingId={Guid.NewGuid()}&resultType=Final",
            JsonContent.Create(new { marksObtained = 80, maxMarks = 100 }));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // COURSE MATERIAL
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CourseMaterial_DownloadFile_Unauthenticated_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.GetAsync($"api/v1/course-materials/{Guid.NewGuid()}/file");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CourseMaterial_DownloadFile_Student_ReturnsNotForbiddenOrUnauthorized()
    {
        using var client = CreateClient("Student");
        var response = await client.GetAsync($"api/v1/course-materials/{Guid.NewGuid()}/file");
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CourseMaterial_Upload_Unauthenticated_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        using var content = BuildCourseMaterialUploadContent();

        var response = await client.PostAsync("api/v1/course-materials/upload", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CourseMaterial_Upload_Student_Returns403()
    {
        using var client = CreateClient("Student");
        using var content = BuildCourseMaterialUploadContent();

        var response = await client.PostAsync("api/v1/course-materials/upload", content);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CourseMaterial_Upload_Faculty_ReturnsNotForbiddenOrUnauthorized()
    {
        using var client = CreateScopedClient("Faculty");
        using var content = BuildCourseMaterialUploadContent();

        var response = await client.PostAsync("api/v1/course-materials/upload", content);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ANALYTICS (PAYMENT STATUS)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Analytics_PaymentStatus_Unauthenticated_Returns401()
    {
        using var client = CreateUnauthenticatedClient();

        var response = await client.GetAsync("api/analytics/payment-status");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Analytics_PaymentStatus_Student_Returns403()
    {
        using var client = CreateClient("Student");

        var response = await client.GetAsync("api/analytics/payment-status");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Analytics_PaymentStatus_Finance_ReturnsNotForbiddenOrUnauthorized()
    {
        using var client = CreateClient("Finance");

        var response = await client.GetAsync("api/analytics/payment-status");
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Analytics_Performance_Finance_Returns403()
    {
        using var client = CreateClient("Finance");

        var response = await client.GetAsync("api/analytics/performance");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Analytics_Attendance_Finance_Returns403()
    {
        using var client = CreateClient("Finance");

        var response = await client.GetAsync("api/analytics/attendance");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Analytics_Assignments_Finance_Returns403()
    {
        using var client = CreateClient("Finance");

        var response = await client.GetAsync("api/analytics/assignments");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static MultipartFormDataContent BuildCourseMaterialUploadContent()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("%PDF-1.4 test file");
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var content = new MultipartFormDataContent();
        content.Add(fileContent, "file", "sample.pdf");
        return content;
    }
}
