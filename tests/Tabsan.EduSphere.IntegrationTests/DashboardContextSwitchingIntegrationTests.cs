using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using System.Net;

using Tabsan.EduSphere.IntegrationTests.Infrastructure;
using Xunit;

namespace Tabsan.EduSphere.IntegrationTests;

[Collection(EduSphereCollection.Name)]
public class DashboardContextSwitchingIntegrationTests : IAsyncLifetime
{
    private readonly EduSphereWebFactory _factory;
    private (bool School, bool College, bool University)? _originalPolicy;

    public DashboardContextSwitchingIntegrationTests(EduSphereWebFactory factory)
    {
        _factory = factory;
    }

    // These tests switch the shared institution policy; restore it afterwards so later tests in the
    // collection (e.g. sidebar menu visibility, which hides University-only menus) are unaffected.
    public async Task InitializeAsync()
    {
        using var client = CreateClient("SuperAdmin");
        var response = await client.GetAsync("api/v1/institution-policy");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        _originalPolicy = (
            doc.RootElement.GetProperty("includeSchool").GetBoolean(),
            doc.RootElement.GetProperty("includeCollege").GetBoolean(),
            doc.RootElement.GetProperty("includeUniversity").GetBoolean());
    }

    public async Task DisposeAsync()
    {
        if (_originalPolicy is not { } original)
            return;

        using var client = CreateClient("SuperAdmin");
        await EnableInstitutionTypesAsync(client, original.University, original.School, original.College);
    }


    private HttpClient CreateClient(string role = "SuperAdmin", string userId = "00000000-0000-0000-0000-000000000001")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(role, userId));
        return client;
    }

    private async Task EnableInstitutionTypesAsync(HttpClient client, bool university = true, bool school = false, bool college = false)
    {
        var response = await client.PutAsJsonAsync("api/v1/institution-policy", new
        {
            includeSchool = school,
            includeCollege = college,
            includeUniversity = university
        });
        response.EnsureSuccessStatusCode();
    }

    [Theory]
    [InlineData("SuperAdmin", true, false, false, new[] { "system_health", "enrollment_stats", "academic_overview", "helpdesk_queue", "ai_assistant" })]
    [InlineData("Admin", true, false, false, new[] { "enrollment_stats", "academic_overview", "helpdesk_queue", "ai_assistant" })]
    [InlineData("Faculty", true, false, false, new[] { "faculty_assignments", "pending_results", "fyp_panel", "attendance_summary", "ai_assistant" })]
    [InlineData("Student", true, false, false, new[] { "fyp_panel", "my_courses", "attendance_summary", "ai_assistant" })]
    [InlineData("Admin", false, true, false, new[] { "enrollment_stats", "academic_overview", "helpdesk_queue", "ai_assistant" })]
    [InlineData("Faculty", false, true, false, new[] { "faculty_assignments", "attendance_summary", "ai_assistant" })]
    [InlineData("Student", false, true, false, new[] { "my_courses", "attendance_summary", "ai_assistant" })]
    [InlineData("Admin", false, false, true, new[] { "enrollment_stats", "academic_overview", "helpdesk_queue", "ai_assistant" })]
    [InlineData("Faculty", false, false, true, new[] { "faculty_assignments", "attendance_summary", "ai_assistant" })]
    [InlineData("Student", false, false, true, new[] { "my_courses", "attendance_summary", "ai_assistant" })]
    public async Task DashboardContext_Widgets_AreCorrectForInstitutionTypeAndRole(string role, bool university, bool school, bool college, string[] expectedWidgetKeys)
    {
        // Always use SuperAdmin for policy change, then switch to test role for dashboard context
        using var adminClient = CreateClient("SuperAdmin");
        await EnableInstitutionTypesAsync(adminClient, university, school, college);

        using var client = CreateClient(role);
        var response = await client.GetAsync("api/v1/dashboard/context");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var context = doc.RootElement;
        var widgets = context.GetProperty("widgets").EnumerateArray().Select(w => w.GetProperty("key").GetString()).ToArray();

        widgets.Should().ContainInOrder(expectedWidgetKeys);
    }

    [Theory]
    [InlineData(true, false, false, "Semester")]
    [InlineData(false, true, false, "Grade")]
    [InlineData(false, false, true, "Year")]
    public async Task DashboardContext_Vocabulary_AdaptsToInstitutionType(bool university, bool school, bool college, string expectedPeriodLabel)
    {
        using var adminClient = CreateClient("SuperAdmin");
        await EnableInstitutionTypesAsync(adminClient, university, school, college);

        using var client = CreateClient("Admin");
        var response = await client.GetAsync("api/v1/dashboard/context");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var context = doc.RootElement;
        var vocab = context.GetProperty("vocabulary");
        vocab.GetProperty("periodLabel").GetString().Should().Be(expectedPeriodLabel);
    }
}
