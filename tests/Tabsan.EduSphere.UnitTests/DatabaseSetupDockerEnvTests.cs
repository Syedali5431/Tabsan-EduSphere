using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Tabsan.EduSphere.API.Services.Setup;
using Xunit;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>
/// The setup page's "Download Docker .env Template" needs no sign-in (the page is open while the API has no working
/// database), so the template must never show the configured password.
/// </summary>
public sealed class DatabaseSetupDockerEnvTests
{
    private const string Secret = "S3cret!Pass";

    [Theory]
    [InlineData("Server=db01.school.local,1433;Database=EduSphere;User Id=edusphere;Password=S3cret!Pass;Encrypt=True")]
    [InlineData("Server=db01;Database=EduSphere;User Id=edusphere;pwd=S3cret!Pass")]
    [InlineData("Server=db01;Database=EduSphere;User Id=edusphere;PASSWORD = S3cret!Pass ;MultipleActiveResultSets=true")]
    public void SqlLogin_TemplateHidesThePassword(string connectionString)
    {
        var template = BuildTemplate(RuntimeDatabaseProvider.SqlServer, connectionString);

        template.Should().NotContain(Secret);
        template.Should().Contain($"Password={DatabaseSetupService.PasswordPlaceholder}");
        template.Should().Contain("# The password is not shown");
        template.Should().Contain("db01").And.Contain("EduSphere").And.Contain("edusphere");
    }

    [Fact]
    public void QuotedPasswordWithSeparators_IsHiddenWhole()
    {
        var template = BuildTemplate(RuntimeDatabaseProvider.SqlServer,
            "Server=db01;Database=EduSphere;User Id=edusphere;Password=\"S3cret;Pass=word\"");

        template.Should().NotContain("S3cret").And.NotContain("Pass=word");
        template.Should().Contain(DatabaseSetupService.PasswordPlaceholder);
    }

    [Fact]
    public void WindowsAuthentication_TemplateKeepsTheConnectionStringAsItIs()
    {
        const string connection = @"Server=localhost\SQLEXPRESS;Database=Tabsan-EduSphere;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

        var template = BuildTemplate(RuntimeDatabaseProvider.SqlServer, connection);

        template.Should().Contain($"ConnectionStrings__DefaultConnection=\"{connection}\"");
        template.Should().NotContain("# The password is not shown");
    }

    [Fact]
    public void SqliteWithPassword_TemplateHidesThePassword()
    {
        var template = BuildTemplate(RuntimeDatabaseProvider.Sqlite, "Data Source=/data/edusphere.db;Password=S3cret!Pass");

        template.Should().NotContain(Secret);
        template.Should().Contain(DatabaseSetupService.PasswordPlaceholder).And.Contain("/data/edusphere.db");
    }

    [Fact]
    public void UnreadableConnectionString_IsNotShown()
    {
        var template = BuildTemplate(RuntimeDatabaseProvider.SqlServer, "Server=db01;NotAKeyword=1;Password=S3cret!Pass");

        template.Should().NotContain(Secret).And.NotContain("db01");
        template.Should().Contain("ConnectionStrings__DefaultConnection=\"<set-your-connection-string>\"");
    }

    [Fact]
    public void HiddenPassword_StillReadsAsAPlaceholderConnectionString()
    {
        var hidden = DatabaseSetupService.HidePassword("Server=db01;Database=EduSphere;User Id=u;Password=S3cret!Pass",
            RuntimeDatabaseProvider.SqlServer, out var passwordHidden);

        passwordHidden.Should().BeTrue();
        // The API treats "REPLACE_WITH" as not configured, so a template used without the real password asks for set-up.
        hidden.Should().Contain("REPLACE_WITH");
    }

    private static string BuildTemplate(RuntimeDatabaseProvider provider, string connectionString)
    {
        var state = new DatabaseSetupRuntimeState();
        state.Set(provider, connectionString, requiresSetup: true, lastError: null);
        var service = new DatabaseSetupService(new TestHostEnvironment(), new EphemeralDataProtectionProvider(), state,
            NullLogger<DatabaseSetupService>.Instance);
        return service.BuildDockerEnvTemplate();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "Tabsan.EduSphere.API";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
