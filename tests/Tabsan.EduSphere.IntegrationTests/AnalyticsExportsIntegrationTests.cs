using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

[Collection(EduSphereCollection.Name)]
public class AnalyticsExportsIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public AnalyticsExportsIntegrationTests(EduSphereWebFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient(
        string role,
        string userId = "00000000-0000-0000-0000-000000000001",
        string? tenantId = null,
        string? campusId = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(role, userId, tenantId: tenantId, campusId: campusId));
        return client;
    }

    private async Task EnsureReportsModuleActiveAsync(CancellationToken ct = default)
    {
        using var superClient = CreateClient("SuperAdmin", "00000000-0000-0000-0000-000000000992");

        var statusResponse = await superClient.GetAsync("api/v1/module/reports/status", ct);
        statusResponse.EnsureSuccessStatusCode();

        using var statusDoc = JsonDocument.Parse(await statusResponse.Content.ReadAsStringAsync(ct));
        if (statusDoc.RootElement.GetProperty("isActive").GetBoolean())
            return;

        var activateResponse = await superClient.PostAsync("api/v1/module/reports/activate", content: null, ct);
        activateResponse.EnsureSuccessStatusCode();
    }

    [Theory]
    [InlineData("api/analytics/performance/export/pdf", "performance", "application/pdf", ".pdf")]
    [InlineData("api/analytics/performance/export/excel", "performance", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx")]
    [InlineData("api/analytics/attendance/export/pdf", "attendance", "application/pdf", ".pdf")]
    [InlineData("api/analytics/attendance/export/excel", "attendance", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx")]
    [InlineData("api/analytics/top-performers/export/pdf", "top-performers", "application/pdf", ".pdf")]
    [InlineData("api/analytics/top-performers/export/excel", "top-performers", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx")]
    [InlineData("api/analytics/performance-trends/export/pdf", "performance-trends", "application/pdf", ".pdf")]
    [InlineData("api/analytics/performance-trends/export/excel", "performance-trends", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx")]
    [InlineData("api/analytics/comparative-summary/export/pdf", "comparative-summary", "application/pdf", ".pdf")]
    [InlineData("api/analytics/comparative-summary/export/excel", "comparative-summary", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx")]
    public async Task AnalyticsExports_WithSuperAdmin_ReturnStandardizedFileMetadata(
        string route,
        string expectedReportKey,
        string expectedContentType,
        string expectedExtension)
    {
        await EnsureReportsModuleActiveAsync();

        using var client = CreateClient("SuperAdmin", "00000000-0000-0000-0000-000000000777");

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedContentType, response.Content.Headers.ContentType?.MediaType);

        var disposition = response.Content.Headers.ContentDisposition?.ToString() ?? string.Empty;
        Assert.Contains("analytics-", disposition, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(expectedReportKey, disposition, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(expectedExtension, disposition, StringComparison.OrdinalIgnoreCase);

        var payload = await response.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(payload);
    }

    [Fact]
    public async Task AnalyticsExportJobStatus_WithDifferentAdminUser_ReturnsForbidden()
    {
        await EnsureReportsModuleActiveAsync();

        const string ownerUserId = "00000000-0000-0000-0000-000000000751";
        const string otherUserId = "00000000-0000-0000-0000-000000000752";

        using var ownerClient = CreateClient("Admin", ownerUserId);
        var queueResponse = await ownerClient.PostAsync("api/analytics/export-jobs?reportType=performance&format=pdf", content: null);
        Assert.Equal(HttpStatusCode.Accepted, queueResponse.StatusCode);

        using var queueDoc = JsonDocument.Parse(await queueResponse.Content.ReadAsStringAsync());
        var jobId = queueDoc.RootElement.GetProperty("jobId").GetGuid();

        using var otherClient = CreateClient("Admin", otherUserId);
        var statusResponse = await otherClient.GetAsync($"api/analytics/export-jobs/{jobId}");

        Assert.Equal(HttpStatusCode.Forbidden, statusResponse.StatusCode);
    }

    [Fact]
    public async Task AnalyticsExportJobStatus_WithTenantCampusMismatchForSameUser_ReturnsForbidden()
    {
        await EnsureReportsModuleActiveAsync();

        const string userId = "00000000-0000-0000-0000-000000000761";
        var tenantA = Guid.NewGuid().ToString();
        var campusA = Guid.NewGuid().ToString();
        var tenantB = Guid.NewGuid().ToString();
        var campusB = Guid.NewGuid().ToString();

        using var scopedClient = CreateClient("Admin", userId, tenantA, campusA);
        var queueResponse = await scopedClient.PostAsync("api/analytics/export-jobs?reportType=attendance&format=excel", content: null);
        Assert.Equal(HttpStatusCode.Accepted, queueResponse.StatusCode);

        using var queueDoc = JsonDocument.Parse(await queueResponse.Content.ReadAsStringAsync());
        var jobId = queueDoc.RootElement.GetProperty("jobId").GetGuid();

        using var mismatchedScopeClient = CreateClient("Admin", userId, tenantB, campusB);
        var statusResponse = await mismatchedScopeClient.GetAsync($"api/analytics/export-jobs/{jobId}");

        Assert.Equal(HttpStatusCode.Forbidden, statusResponse.StatusCode);
    }
}
