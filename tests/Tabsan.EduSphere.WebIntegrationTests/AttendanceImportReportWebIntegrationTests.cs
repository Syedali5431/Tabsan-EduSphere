using System.Collections;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Tabsan.EduSphere.Web;
using Tabsan.EduSphere.Web.Controllers;

namespace Tabsan.EduSphere.WebIntegrationTests;

public class AttendanceImportReportWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private readonly WebApplicationFactory<WebEntryPointMarker> _factory;

    private static readonly FieldInfo ReportStoreField =
        typeof(PortalController).GetField("AttendanceImportReports", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("AttendanceImportReports field was not found on PortalController.");

    private static readonly Type ReportPayloadType =
        typeof(PortalController).GetNestedType("AttendanceImportReportPayload", BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("AttendanceImportReportPayload nested type was not found on PortalController.");

    public AttendanceImportReportWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task DownloadAttendanceImportReport_WithValidToken_ReturnsCsv()
    {
        ClearReportStore();
        SeedReportToken("valid-token", DateTime.UtcNow, "RowNumber,StudentId,StudentName,Date,Present,Outcome,Reason\n2,abc,John,2026-05-28,true,Imported,");

        using var client = CreateClient();
        var response = await client.GetAsync("/Portal/DownloadAttendanceImportReport?reportToken=valid-token&entryPoint=EnterAttendance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("RowNumber,StudentId,StudentName,Date,Present,Outcome,Reason", body);
    }

    [Fact]
    public async Task DownloadAttendanceImportReport_TokenIsOneTimeUse_SecondCallRedirects()
    {
        ClearReportStore();
        SeedReportToken("one-time-token", DateTime.UtcNow, "RowNumber,StudentId,StudentName,Date,Present,Outcome,Reason\n2,abc,John,2026-05-28,true,Imported,");

        using var client = CreateClient();

        var first = await client.GetAsync("/Portal/DownloadAttendanceImportReport?reportToken=one-time-token&entryPoint=EnterAttendance");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.GetAsync("/Portal/DownloadAttendanceImportReport?reportToken=one-time-token&entryPoint=EnterAttendance");
        Assert.Equal(HttpStatusCode.Redirect, second.StatusCode);
        Assert.Contains("/Portal/EnterAttendance", second.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DownloadAttendanceImportReport_WithExpiredToken_Redirects()
    {
        ClearReportStore();
        SeedReportToken("expired-token", DateTime.UtcNow.AddHours(-3), "RowNumber,StudentId,StudentName,Date,Present,Outcome,Reason\n2,abc,John,2026-05-28,true,Imported,");

        using var client = CreateClient();
        var response = await client.GetAsync("/Portal/DownloadAttendanceImportReport?reportToken=expired-token&entryPoint=EnterAttendance");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Portal/EnterAttendance", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private HttpClient CreateClient()
        => _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

    private static object GetReportStore()
        => ReportStoreField.GetValue(null) ?? throw new InvalidOperationException("Attendance import report store is null.");

    private static void ClearReportStore()
    {
        var store = GetReportStore();
        var asEnumerable = (IEnumerable)store;
        var keys = new List<string>();

        foreach (var item in asEnumerable)
        {
            var keyProp = item.GetType().GetProperty("Key");
            if (keyProp?.GetValue(item) is string key)
                keys.Add(key);
        }

        var tryRemove = store.GetType().GetMethod("TryRemove", [typeof(string), ReportPayloadType.MakeByRefType()])
                        ?? throw new InvalidOperationException("TryRemove method not found on report store.");

        foreach (var key in keys)
        {
            var args = new object?[] { key, null };
            tryRemove.Invoke(store, args);
        }
    }

    private static void SeedReportToken(string token, DateTime createdAtUtc, string csvContent)
    {
        var payload = Activator.CreateInstance(
            ReportPayloadType,
            "attendance-import-report-test.csv",
            csvContent,
            createdAtUtc);

        if (payload is null)
            throw new InvalidOperationException("Could not create attendance import report payload instance.");

        var store = GetReportStore();
        var tryAdd = store.GetType().GetMethod("TryAdd", [typeof(string), ReportPayloadType])
                     ?? throw new InvalidOperationException("TryAdd method not found on report store.");

        var added = (bool)(tryAdd.Invoke(store, [token, payload]) ?? false);
        if (!added)
            throw new InvalidOperationException("Could not add seeded report token to report store.");
    }
}
