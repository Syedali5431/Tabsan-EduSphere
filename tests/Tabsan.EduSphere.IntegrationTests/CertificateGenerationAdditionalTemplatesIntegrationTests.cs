using System.Net;
using System.Net.Http.Headers;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

[Collection(EduSphereCollection.Name)]
public class CertificateGenerationAdditionalTemplatesIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public CertificateGenerationAdditionalTemplatesIntegrationTests(EduSphereWebFactory factory)
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
    public async Task DownloadDefaultTemplate_Completion_Admin_ReturnsDocx()
    {
        using var client = CreateClient("Admin");

        var response = await client.GetAsync("api/v1/certificate-generation/templates/completion/default");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 4);
        Assert.Equal(0x50, bytes[0]);
        Assert.Equal(0x4B, bytes[1]);
    }

    [Fact]
    public async Task DownloadDefaultTemplate_ReportCard_Admin_ReturnsDocx()
    {
        using var client = CreateClient("Admin");

        var response = await client.GetAsync("api/v1/certificate-generation/templates/reportcard/default");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task DownloadDefaultTemplate_UnsupportedType_ReturnsBadRequest()
    {
        using var client = CreateClient("Admin");

        var response = await client.GetAsync("api/v1/certificate-generation/templates/degree/default");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DownloadDefaultTemplate_StudentRole_ReturnsForbidden()
    {
        using var client = CreateClient("Student");

        var response = await client.GetAsync("api/v1/certificate-generation/templates/completion/default");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadAdditionalTemplate_InvalidExtension_ReturnsBadRequest()
    {
        using var client = CreateClient("Admin");
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([0x50, 0x4B, 0x03, 0x04, 0x14]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        content.Add(fileContent, "file", "completion-template.txt");

        var response = await client.PostAsync("api/v1/certificate-generation/templates/completion/upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadAdditionalTemplate_ValidDocx_ReturnsOk()
    {
        using var client = CreateClient("Admin");
        using var content = new MultipartFormDataContent();

        var fileBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00, 0x08, 0x00 };
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        content.Add(fileContent, "file", "completion-template.docx");

        var response = await client.PostAsync("api/v1/certificate-generation/templates/completion/upload", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GenerateAdditionalCertificate_UnsupportedType_ReturnsBadRequest()
    {
        using var client = CreateClient("Admin");

        var response = await client.PostAsync($"api/v1/certificate-generation/students/{Guid.NewGuid()}/additional-certificates/generate?documentType=degree", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
