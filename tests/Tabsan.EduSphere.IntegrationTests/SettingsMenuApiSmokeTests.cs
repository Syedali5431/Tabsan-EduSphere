using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

[Collection(EduSphereCollection.Name)]
public class SettingsMenuApiSmokeTests
{
    private readonly EduSphereWebFactory _factory;

    public SettingsMenuApiSmokeTests(EduSphereWebFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateSuperAdminClient(string userId = "00000000-0000-0000-0000-000000000901")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken("SuperAdmin", userId));
        return client;
    }

    [Theory]
    [InlineData("api/v1/building")]
    [InlineData("api/v1/room")]
    [InlineData("api/v1/report-settings")]
    [InlineData("api/v1/modules/all-settings")]
    [InlineData("api/v1/sidebar-menu")]
    [InlineData("api/v1/theme")]
    [InlineData("api/v1/license/status")]
    [InlineData("api/v1/license/details")]
    [InlineData("api/v1/portal-settings")]
    [InlineData("api/v1/institution-policy")]
    [InlineData("api/v1/tenant")]
    [InlineData("api/v1/campus")]
    [InlineData("api/v1/library/config")]
    [InlineData("api/v1/accreditation")]
    [InlineData("api/v1/notification/inbox")]
    [InlineData("api/v1/analytics/status")]
    [InlineData("api/v1/helpdesk/tickets?page=1&pageSize=5")]
    [InlineData("api/v1/reports")]
    [InlineData("api/v1/2fa/setup")]
    [InlineData("api/v1/admin-user")]
    public async Task SettingsRelatedEndpoints_LoadWithoutAuthOrServerErrors(string route)
    {
        using var client = CreateSuperAdminClient();

        var response = await client.GetAsync(route);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task UserImportEndpoint_BoundaryRequest_DoesNotReturnServerError()
    {
        using var client = CreateSuperAdminClient();

        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("api/v1/registration-import/single", content);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
