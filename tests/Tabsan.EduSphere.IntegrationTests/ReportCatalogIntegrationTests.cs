using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tabsan.EduSphere.Domain.Settings;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

[Collection(EduSphereCollection.Name)]
public class ReportCatalogIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    private static readonly Dictionary<string, string> ReportDataRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        [ReportKeys.AttendanceSummary] = "api/v1/reports/attendance-summary",
        [ReportKeys.ResultSummary] = "api/v1/reports/result-summary",
        [ReportKeys.GpaReport] = "api/v1/reports/gpa-report",
        [ReportKeys.EnrollmentSummary] = "api/v1/reports/enrollment-summary",
        [ReportKeys.SemesterResults] = "api/v1/reports/semester-results",
        [ReportKeys.StudentTranscript] = "api/v1/reports/student-transcript",
        [ReportKeys.LowAttendanceWarning] = "api/v1/reports/low-attendance",
        [ReportKeys.FypStatus] = "api/v1/reports/fyp-status",
        [ReportKeys.PaymentSummary] = "api/v1/reports/payment-summary"
    };

    public ReportCatalogIntegrationTests(EduSphereWebFactory factory)
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

    [Fact]
    public async Task ReportCatalog_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("api/v1/reports");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("SuperAdmin")]
    [InlineData("Admin")]
    [InlineData("Faculty")]
    public async Task ReportCatalog_PrivilegedRoles_ReturnsExpectedSeededKeys(string role)
    {
        using var client = CreateClient(role);

        var response = await client.GetAsync("api/v1/reports");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ReportCatalogResponse>();
        Assert.NotNull(payload);
        Assert.NotNull(payload!.Reports);

        var keys = payload.Reports.Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(ReportKeys.AttendanceSummary, keys);
        Assert.Contains(ReportKeys.ResultSummary, keys);
        Assert.Contains(ReportKeys.GpaReport, keys);
        Assert.Contains(ReportKeys.EnrollmentSummary, keys);
        Assert.Contains(ReportKeys.SemesterResults, keys);
        Assert.Contains(ReportKeys.StudentTranscript, keys);
        Assert.Contains(ReportKeys.LowAttendanceWarning, keys);
        Assert.Contains(ReportKeys.FypStatus, keys);
    }

    [Fact]
    public async Task ReportCatalog_Student_ReturnsOnlyStudentAllowedReports()
    {
        using var client = CreateClient("Student");

        var response = await client.GetAsync("api/v1/reports");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ReportCatalogResponse>();
        Assert.NotNull(payload);

        var keys = payload!.Reports.Select(x => x.Key).ToList();

        Assert.Contains(ReportKeys.StudentTranscript, keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(ReportKeys.AttendanceSummary, keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(ReportKeys.ResultSummary, keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(ReportKeys.GpaReport, keys, StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("SuperAdmin")]
    [InlineData("Admin")]
    [InlineData("Faculty")]
    public async Task ReportCatalog_PrivilegedRoles_AllCatalogKeysMapToLiveReportDataRoutes(string role)
    {
        using var client = CreateClient(role);

        var response = await client.GetAsync("api/v1/reports");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ReportCatalogResponse>();
        Assert.NotNull(payload);

        foreach (var item in payload!.Reports)
        {
            Assert.True(ReportDataRoutes.ContainsKey(item.Key), $"Catalog key '{item.Key}' has no route mapping.");

            var dataRoute = ReportDataRoutes[item.Key];
            var routeResponse = await client.GetAsync(dataRoute);

            Assert.NotEqual(HttpStatusCode.NotFound, routeResponse.StatusCode);
            Assert.NotEqual(HttpStatusCode.Unauthorized, routeResponse.StatusCode);
        }
    }

    private sealed class ReportCatalogResponse
    {
        public List<ReportCatalogItem> Reports { get; set; } = new();
    }

    private sealed class ReportCatalogItem
    {
        public string Key { get; set; } = string.Empty;
    }
}
