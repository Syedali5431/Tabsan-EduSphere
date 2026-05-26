using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tabsan.EduSphere.Domain.Modules;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.IntegrationTests.Infrastructure;

/// <summary>
/// Shared WebApplicationFactory that targets a dedicated LocalDB integration-test
/// database (<c>TabsanEduSphere_IntegrationTests</c>).
/// The database is dropped and recreated via EF migrations on first use, ensuring
/// a clean, reproducible baseline. All background hosted services are removed to
/// prevent interference with the isolated test database.
/// </summary>
public sealed class EduSphereWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// Dedicated database name — separate from the development database so tests
    /// never touch real data.
    /// </summary>
    private const string TestDbName = "TabsanEduSphere_IntegrationTests";

    private const string TestConnectionString =
        $@"Server=(localdb)\mssqllocaldb;Database={TestDbName};Trusted_Connection=True;MultipleActiveResultSets=true";

    public EduSphereWebFactory()
    {
        var sharedEnvironmentsFile = TryResolveSharedEnvironmentsFile();
        if (!string.IsNullOrWhiteSpace(sharedEnvironmentsFile))
        {
            Environment.SetEnvironmentVariable("EDUSPHERE_ENVIRONMENTS_FILE", sharedEnvironmentsFile);
        }
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        // Use a named system Mutex to prevent parallel test worker processes from
        // racing to drop/create the same LocalDB database simultaneously.
        // Run on a dedicated thread so WaitOne and ReleaseMutex stay on the same thread.
        await Task.Factory.StartNew(async () =>
        {
            using var mutex = new Mutex(false, "Global\\TabsanEduSphereTestDb");
            mutex.WaitOne(TimeSpan.FromSeconds(60));
            try
            {
                ForceDropDatabaseSync();
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();

        // Most integration suites exercise the app as a fully licensed deployment.
        // Activate every module in the shared baseline so auth/role checks are not
        // masked by module-license middleware unless a test opts out explicitly.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var moduleStatuses = await db.ModuleStatuses.ToListAsync();
        foreach (var status in moduleStatuses.Where(status => !status.IsActive))
        {
            status.Activate(Guid.Empty, "manual");
        }

        await db.SaveChangesAsync();
    }

    public new async Task DisposeAsync()
    {
        // Drop the test database after the full test run so nothing persists.
        await ForceDropDatabaseAsync();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Drops the integration-test database unconditionally, closing all existing
    /// connections first (SET SINGLE_USER WITH ROLLBACK IMMEDIATE).
    /// Uses the master database connection so the target DB need not exist.
    /// </summary>
    private static async Task ForceDropDatabaseAsync()
    {
        const string masterConn =
            @"Server=(localdb)\mssqllocaldb;Database=master;Trusted_Connection=True;";

        await using var conn = new SqlConnection(masterConn);
        await conn.OpenAsync();

        var sql = $"""
            IF DB_ID('{TestDbName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{TestDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{TestDbName}];
            END
            """;

        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private static void ForceDropDatabaseSync()
    {
        const string masterConn =
            @"Server=(localdb)\mssqllocaldb;Database=master;Trusted_Connection=True;";

        using var conn = new SqlConnection(masterConn);
        conn.Open();

        var sql = $"""
            IF DB_ID('{TestDbName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{TestDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{TestDbName}];
            END
            """;

        using var cmd = new SqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    // ── WebApplicationFactory override ─────────────────────────────────────────

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var sharedEnvironmentsFile = TryResolveSharedEnvironmentsFile();
            if (!string.IsNullOrWhiteSpace(sharedEnvironmentsFile))
            {
                Environment.SetEnvironmentVariable("EDUSPHERE_ENVIRONMENTS_FILE", sharedEnvironmentsFile);
                config.AddJsonFile(sharedEnvironmentsFile, optional: true, reloadOnChange: false);
            }
        });

        builder.ConfigureServices(services =>
        {
            // Swap out the production DbContext options for the test database.
            var dbOpts = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbOpts is not null)
                services.Remove(dbOpts);

            services.AddDbContext<ApplicationDbContext>(opts =>
                opts.UseSqlServer(
                    TestConnectionString,
                    sql => sql.MigrationsAssembly("Tabsan.EduSphere.Infrastructure")));

            // Remove all background hosted services (LicenseCheckWorker, AttendanceAlertJob, etc.)
            // so they do not interfere with the isolated test database.
            var hosted = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();
            foreach (var d in hosted)
                services.Remove(d);
        });
    }

    private static string? TryResolveSharedEnvironmentsFile()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "src", "environments.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }
}
