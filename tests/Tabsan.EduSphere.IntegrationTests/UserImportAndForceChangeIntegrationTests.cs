using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

[Collection(EduSphereCollection.Name)]
public class UserImportAndForceChangeIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public UserImportAndForceChangeIntegrationTests(EduSphereWebFactory factory)
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

    private static async Task SetInstitutionPolicyAsync(HttpClient client, bool university, bool school, bool college)
    {
        var response = await client.PutAsJsonAsync("api/v1/institution-policy", new
        {
            includeSchool = school,
            includeCollege = college,
            includeUniversity = university
        });
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task UserImportCsv_StudentRole_ReturnsForbidden()
    {
        using var client = CreateClient("Student");

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("Username,Email,FullName,Role\n")), "file", "users.csv");

        var response = await client.PostAsync("api/v1/user-import/csv", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserImportCsv_Then_ForceChangePassword_WorksEndToEnd()
    {
        var importedUsername = $"import_admin_{Guid.NewGuid():N}";
        var importedEmail = $"{importedUsername}@tabsan.local";

        using (var adminClient = CreateClient("Admin"))
        {
            using var superAdminClient = CreateClient("SuperAdmin");
            await SetInstitutionPolicyAsync(superAdminClient, university: true, school: false, college: false);

            var csv = string.Join('\n',
            [
                "Username,Email,FullName,Role,DepartmentId,InstitutionType",
                $"{importedUsername},{importedEmail},Imported Admin,Admin,,University"
            ]);

            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", "import-users.csv");

            var importResponse = await adminClient.PostAsync("api/v1/user-import/csv", content);
            Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

            var importJson = await importResponse.Content.ReadAsStringAsync();
            using var importDoc = JsonDocument.Parse(importJson);
            var imported = ReadInt(importDoc.RootElement, "imported");
            Assert.True(imported >= 1, "Expected at least one imported user.");
        }

        var loginResponse = await _factory.CreateClient().PostAsJsonAsync("api/v1/auth/login", new
        {
            username = importedUsername,
            password = importedUsername
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        using var loginDoc = JsonDocument.Parse(loginBody);

        var accessToken = ReadString(loginDoc.RootElement, "accessToken");
        var mustChangePassword = ReadBool(loginDoc.RootElement, "mustChangePassword");

        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.True(mustChangePassword);

        using (var importedUserClient = _factory.CreateClient())
        {
            importedUserClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var forceChangeResponse = await importedUserClient.PostAsJsonAsync("api/v1/auth/force-change-password", new
            {
                newPassword = "TempPass@12345"
            });

            Assert.Equal(HttpStatusCode.NoContent, forceChangeResponse.StatusCode);
        }

        var oldPasswordLoginResponse = await _factory.CreateClient().PostAsJsonAsync("api/v1/auth/login", new
        {
            username = importedUsername,
            password = importedUsername
        });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLoginResponse.StatusCode);

        var newPasswordLoginResponse = await _factory.CreateClient().PostAsJsonAsync("api/v1/auth/login", new
        {
            username = importedUsername,
            password = "TempPass@12345"
        });
        Assert.Equal(HttpStatusCode.OK, newPasswordLoginResponse.StatusCode);

        var reloginBody = await newPasswordLoginResponse.Content.ReadAsStringAsync();
        using var reloginDoc = JsonDocument.Parse(reloginBody);
        var mustChangePasswordAfterReset = ReadBool(reloginDoc.RootElement, "mustChangePassword");
        Assert.False(mustChangePasswordAfterReset);
    }

    [Fact]
    public async Task UserImportCsv_StrictMode_WithInvalidRow_RollsBackAllRows()
    {
        using var adminClient = CreateClient("Admin");
        using var superAdminClient = CreateClient("SuperAdmin");
        await SetInstitutionPolicyAsync(superAdminClient, university: true, school: false, college: false);

        var validUsername = $"import_strict_valid_{Guid.NewGuid():N}";
        var invalidUsername = $"import_strict_invalid_{Guid.NewGuid():N}";
        var csv = string.Join('\n',
        [
            "Username,Email,FullName,Role,DepartmentId,InstitutionType",
            $"{validUsername},{validUsername}@tabsan.local,Strict Valid,Admin,,University",
            $"{invalidUsername},{invalidUsername}@tabsan.local,Strict Invalid,Admin,,School"
        ]);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", "strict-import-users.csv");

        var response = await adminClient.PostAsync("api/v1/user-import/csv?strictMode=true", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, ReadInt(body.RootElement, "imported"));
        Assert.True(ReadInt(body.RootElement, "errors") >= 1);
        Assert.True(ReadBool(body.RootElement, "strictMode"));

        var errorDetails = body.RootElement.GetProperty("errorDetails").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();
        Assert.Contains(errorDetails, detail => detail.Contains("Strict mode rollback", StringComparison.OrdinalIgnoreCase));

        var loginResponse = await _factory.CreateClient().PostAsJsonAsync("api/v1/auth/login", new
        {
            username = validUsername,
            password = validUsername
        });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task UserImportCsv_WithDisabledInstitutionType_ReturnsValidationError()
    {
        using var adminClient = CreateClient("Admin");
        using var superAdminClient = CreateClient("SuperAdmin");
        await SetInstitutionPolicyAsync(superAdminClient, university: true, school: false, college: false);

        var username = $"import_disabled_{Guid.NewGuid():N}";
        var csv = string.Join('\n',
        [
            "Username,Email,FullName,Role,DepartmentId,InstitutionType",
            $"{username},{username}@tabsan.local,Import Disabled,Admin,,School"
        ]);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", "import-users-disabled-institution.csv");

        var response = await adminClient.PostAsync("api/v1/user-import/csv", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, ReadInt(body.RootElement, "imported"));
        Assert.True(ReadInt(body.RootElement, "errors") >= 1);

        var errorDetails = body.RootElement.GetProperty("errorDetails").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();
        Assert.Contains(errorDetails, detail => detail.Contains("InstitutionType 'School' is not enabled", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UserImportCsv_WithMobileNumberAndCampusAssignments_ImportsSuccessfully()
    {
        using var adminClient = CreateClient("Admin");
        using var superAdminClient = CreateClient("SuperAdmin");
        await SetInstitutionPolicyAsync(superAdminClient, university: true, school: true, college: true);

        var username = $"import_mobile_{Guid.NewGuid():N}";
        var csv = string.Join('\n',
        [
            "Username,Email,FullName,Role,DepartmentId,InstitutionType,MobileNumber,CampusAssignments",
            $"{username},{username}@tabsan.local,Import Mobile,Finance,,University,+61412345678,22222222-2222-2222-2222-222222222221|22222222-2222-2222-2222-222222222222"
        ]);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", "import-users-mobile-campus.csv");

        var response = await adminClient.PostAsync("api/v1/user-import/csv", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, ReadInt(body.RootElement, "imported"));
        Assert.Equal(0, ReadInt(body.RootElement, "errors"));
    }

    [Fact]
    public async Task UserImportCsv_BackwardCompatibleWithoutNewColumns_ImportsSuccessfully()
    {
        using var adminClient = CreateClient("Admin");
        using var superAdminClient = CreateClient("SuperAdmin");
        await SetInstitutionPolicyAsync(superAdminClient, university: true, school: false, college: false);

        var username = $"import_legacy_{Guid.NewGuid():N}";
        var csv = string.Join('\n',
        [
            "Username,Email,FullName,Role,DepartmentId,InstitutionType",
            $"{username},{username}@tabsan.local,Import Legacy,Admin,,University"
        ]);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", "import-users-legacy-template.csv");

        var response = await adminClient.PostAsync("api/v1/user-import/csv", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, ReadInt(body.RootElement, "imported"));
        Assert.Equal(0, ReadInt(body.RootElement, "errors"));
    }

    private static int ReadInt(JsonElement root, string propertyName)
    {
        if (TryGetProperty(root, propertyName, out var value) && value.ValueKind == JsonValueKind.Number)
            return value.GetInt32();

        return 0;
    }

    private static bool ReadBool(JsonElement root, string propertyName)
    {
        if (TryGetProperty(root, propertyName, out var value) && value.ValueKind == JsonValueKind.True)
            return true;

        if (TryGetProperty(root, propertyName, out value) && value.ValueKind == JsonValueKind.False)
            return false;

        return false;
    }

    private static string ReadString(JsonElement root, string propertyName)
    {
        if (TryGetProperty(root, propertyName, out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? string.Empty;

        return string.Empty;
    }

    private static bool TryGetProperty(JsonElement root, string propertyName, out JsonElement value)
    {
        if (root.TryGetProperty(propertyName, out value))
            return true;

        var pascalName = char.ToUpperInvariant(propertyName[0]) + propertyName[1..];
        return root.TryGetProperty(pascalName, out value);
    }
}
