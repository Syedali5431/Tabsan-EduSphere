using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tabsan.EduSphere.API.Services.Setup;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;
using Xunit;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// The API's Database Setup page (<c>/setup</c>) needs no sign-in, so a new server without a database can use it. It is
/// open only while the API has no working database; once the database works every <c>/setup</c> address answers 404, so
/// nobody who can reach the API (students on the school Wi-Fi) can change the database or make the API connect elsewhere.
/// </summary>
/// <remarks>
/// A new server is a second test host whose runtime state and EF both get an unusable connection string. A database
/// that stops working is simulated on the shared host by giving its runtime state a placeholder connection string for
/// one test (EF keeps the tests' own database), then restoring it.
/// Save and import are only ever sent input that fails validation, so they can never write App_Data/database-setup.json.
/// </remarks>
[Collection(EduSphereCollection.Name)]
public sealed class DatabaseSetupPageIntegrationTests
{
    private const string PlaceholderConnection = "Server=REPLACE_WITH_SQL_SERVER;Database=REPLACE_WITH_DATABASE;";

    /// <summary>
    /// A new server's unusable connection string: a placeholder for the setup check, and for EF a port nothing listens
    /// on, so a database read fails within seconds instead of waiting for a name lookup.
    /// </summary>
    private const string NoDatabaseConnection =
        "Server=tcp:127.0.0.1,1;Database=REPLACE_WITH_DATABASE;Connect Timeout=2;TrustServerCertificate=True;";

    private readonly EduSphereWebFactory _factory;

    public DatabaseSetupPageIntegrationTests(EduSphereWebFactory factory)
    {
        _factory = factory;
    }

    // ── Working database: closed ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("setup")]
    [InlineData("setup/")]
    [InlineData("SETUP")]
    [InlineData("setup/export")]
    [InlineData("setup/docker-env")]
    public async Task WorkingDatabase_SetupPages_Return404(string path)
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WorkingDatabase_SetupPage_Returns404_EvenForSuperAdmin()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", JwtTestHelper.GenerateToken("SuperAdmin", userId: Guid.NewGuid().ToString()));

        using var response = await client.GetAsync("setup");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WorkingDatabase_TestConnection_IsRefused_AndConnectsNowhere()
    {
        using var client = CreateClient();
        var token = await GetFormTokenWhileSetupRequiredAsync(client);
        var connectionBefore = RuntimeState().Snapshot().ConnectionString;
        var probeFile = NewProbeFilePath();

        try
        {
            using var response = await client.PostAsync("setup/test-connection", Form(token, SqliteFields(probeFile)));

            // A valid anti-forgery token was sent, so 404 is the setup gate and not the token check (400).
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            File.Exists(probeFile).Should().BeFalse("the API must not open a connection to what an anonymous caller typed");
            RuntimeState().Snapshot().ConnectionString.Should().Be(connectionBefore);
        }
        finally
        {
            DeleteProbeFile(probeFile);
        }
    }

    [Fact]
    public async Task WorkingDatabase_Save_IsRefused()
    {
        using var client = CreateClient();
        var token = await GetFormTokenWhileSetupRequiredAsync(client);
        var connectionBefore = RuntimeState().Snapshot().ConnectionString;

        using var response = await client.PostAsync("setup/save", Form(token, UnsaveableSqlServerFields()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        RuntimeState().Snapshot().ConnectionString.Should().Be(connectionBefore);
        RuntimeState().Snapshot().RequiresSetup.Should().BeFalse();
    }

    [Fact]
    public async Task WorkingDatabase_Import_IsRefused()
    {
        using var client = CreateClient();
        var token = await GetFormTokenWhileSetupRequiredAsync(client);
        var connectionBefore = RuntimeState().Snapshot().ConnectionString;

        using var content = ImportForm(token);
        using var response = await client.PostAsync("setup/import", content);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        RuntimeState().Snapshot().ConnectionString.Should().Be(connectionBefore);
    }

    // ── Two-factor set-up addresses end in "setup" and are not part of the page ────────────

    [Theory]
    [InlineData("GET", "api/v1/2fa/setup")]
    [InlineData("POST", "api/v1/auth/mfa/setup")]
    public async Task TwoFactorSetupRoutes_AreNotClosedByTheSetupGate(string method, string path)
    {
        using var client = CreateClient();

        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── No working database (a new server): open ────────────────────────────────────────

    [Fact]
    public async Task NoDatabase_PageOpens_AndItsActionsWork()
    {
        // A new server: the connection string is unusable for the setup check and for EF alike, so anything on the
        // way to the page that reads the database (licence domain, institution context) would fail it with 500.
        await using var noDatabase = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
        {
            var state = new DatabaseSetupRuntimeState();
            state.Set(RuntimeDatabaseProvider.SqlServer, NoDatabaseConnection, requiresSetup: true,
                lastError: "Placeholder connection string detected.");
            services.Replace(ServiceDescriptor.Singleton(state));
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(NoDatabaseConnection));
        }));
        using var client = noDatabase.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var probeFile = NewProbeFilePath();

        try
        {
            using var page = await client.GetAsync("setup");
            page.StatusCode.Should().Be(HttpStatusCode.OK, await page.Content.ReadAsStringAsync());
            var html = await page.Content.ReadAsStringAsync();
            html.Should().Contain("Database Setup").And.Contain("action=\"/setup/save\"");
            var token = ReadFormToken(html);

            // Test Connection really connects (SQLite: a file, no server needed).
            using var test = await client.PostAsync("setup/test-connection", Form(token, SqliteFields(probeFile)));
            test.StatusCode.Should().Be(HttpStatusCode.OK);
            using (var json = JsonDocument.Parse(await test.Content.ReadAsStringAsync()))
            {
                json.RootElement.GetProperty("success").GetBoolean().Should().BeTrue(
                    json.RootElement.GetProperty("message").GetString());
            }
            File.Exists(probeFile).Should().BeTrue();

            // Save & Continue and Import reach the page (their validation answers; nothing is saved).
            using var save = await client.PostAsync("setup/save", Form(token, UnsaveableSqlServerFields()));
            save.StatusCode.Should().Be(HttpStatusCode.OK);
            (await save.Content.ReadAsStringAsync()).Should().Contain("Database Name is required.");

            using var importContent = ImportForm(token);
            using var import = await client.PostAsync("setup/import", importContent);
            import.StatusCode.Should().Be(HttpStatusCode.OK);
            (await import.Content.ReadAsStringAsync()).Should().Contain("Database Name is required.");

            using var export = await client.GetAsync("setup/export");
            export.StatusCode.Should().Be(HttpStatusCode.OK);

            // The rest of the API waits for the database.
            using var apiRequest = new HttpRequestMessage(HttpMethod.Get, "api/v1/mobile/config");
            apiRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var api = await client.SendAsync(apiRequest);
            api.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            (await api.Content.ReadAsStringAsync()).Should().Contain("database_setup_required");
        }
        finally
        {
            DeleteProbeFile(probeFile);
        }
    }

    [Fact]
    public async Task DatabaseUnreachable_PageOpens_ThenClosesOnceTheDatabaseWorks()
    {
        using var client = CreateClient();
        string token;

        using (new SetupRequiredScope(RuntimeState()))
        {
            using var page = await client.GetAsync("setup");
            page.StatusCode.Should().Be(HttpStatusCode.OK);
            token = ReadFormToken(await page.Content.ReadAsStringAsync());

            using var test = await client.PostAsync("setup/test-connection", Form(token, UnsaveableSqlServerFields()));
            test.StatusCode.Should().Be(HttpStatusCode.OK);
            (await test.Content.ReadAsStringAsync()).Should().Contain("Database Name is required.");
        }

        using var closedPage = await client.GetAsync("setup");
        closedPage.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var closedTest = await client.PostAsync("setup/test-connection", Form(token, UnsaveableSqlServerFields()));
        closedTest.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────────────

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private DatabaseSetupRuntimeState RuntimeState() => _factory.Services.GetRequiredService<DatabaseSetupRuntimeState>();

    /// <summary>
    /// Opens the page while setup is required, so the client holds a valid anti-forgery cookie and form token, then
    /// restores the working database.
    /// </summary>
    private async Task<string> GetFormTokenWhileSetupRequiredAsync(HttpClient client)
    {
        using (new SetupRequiredScope(RuntimeState()))
        {
            using var page = await client.GetAsync("setup");
            page.StatusCode.Should().Be(HttpStatusCode.OK);
            return ReadFormToken(await page.Content.ReadAsStringAsync());
        }
    }

    private static string ReadFormToken(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        match.Success.Should().BeTrue("the setup page renders an anti-forgery token");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static FormUrlEncodedContent Form(string token, IEnumerable<KeyValuePair<string, string>> fields) =>
        new(fields.Append(new KeyValuePair<string, string>("__RequestVerificationToken", token)));

    private static IEnumerable<KeyValuePair<string, string>> SqliteFields(string sqliteFile) =>
    [
        new("DatabaseType", "Local"),
        new("LocalProvider", "Sqlite"),
        new("SqliteFilePath", sqliteFile)
    ];

    /// <summary>SQL Server without a database name: fails validation before any connection, so it is never saved.</summary>
    private static IEnumerable<KeyValuePair<string, string>> UnsaveableSqlServerFields() =>
    [
        new("DatabaseType", "Local"),
        new("LocalProvider", "SqlServerExpress"),
        new("ServerHost", "attacker.example.test"),
        new("TrustedConnection", "true")
    ];

    private static MultipartFormDataContent ImportForm(string token)
    {
        var profile = JsonSerializer.Serialize(new
        {
            DatabaseType = "Local",
            LocalProvider = "SqlServerExpress",
            ServerHost = "attacker.example.test",
            TrustedConnection = true
        });

        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(profile));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        return new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { file, "profileFile", "profile.json" }
        };
    }

    private static string NewProbeFilePath() =>
        Path.Combine(Path.GetTempPath(), "edusphere-integration-tests", $"setup-probe-{Guid.NewGuid():N}.db");

    private static void DeleteProbeFile(string path)
    {
        SqliteConnection.ClearAllPools();
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
            // A temp file; leaving it behind does not affect other tests.
        }
    }

    /// <summary>Makes the API behave as a server without a working database until disposed.</summary>
    private sealed class SetupRequiredScope : IDisposable
    {
        private readonly DatabaseSetupRuntimeState _state;
        private readonly (RuntimeDatabaseProvider Provider, string ConnectionString, bool RequiresSetup, string? LastError, DateTimeOffset LastValidationUtc) _original;

        public SetupRequiredScope(DatabaseSetupRuntimeState state)
        {
            _state = state;
            _original = state.Snapshot();
            // A placeholder fails validation at once, also when the page re-checks the connection (no network needed).
            state.Set(RuntimeDatabaseProvider.SqlServer, PlaceholderConnection, requiresSetup: true,
                lastError: "Placeholder connection string detected.");
        }

        public void Dispose() =>
            _state.Set(_original.Provider, _original.ConnectionString, _original.RequiresSetup, _original.LastError);
    }
}
