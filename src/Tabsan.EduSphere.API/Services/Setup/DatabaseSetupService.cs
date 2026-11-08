using System.Data.Common;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Tabsan.EduSphere.API.Models.Setup;

namespace Tabsan.EduSphere.API.Services.Setup;

public enum RuntimeDatabaseProvider
{
    SqlServer = 0,
    Sqlite = 1
}

public sealed class DatabaseSetupRuntimeState
{
    private readonly object _gate = new();

    public RuntimeDatabaseProvider Provider { get; private set; } = RuntimeDatabaseProvider.SqlServer;
    public string ConnectionString { get; private set; } = string.Empty;
    public bool RequiresSetup { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset LastValidationUtc { get; private set; } = DateTimeOffset.MinValue;

    public void Set(RuntimeDatabaseProvider provider, string connectionString, bool requiresSetup, string? lastError)
    {
        lock (_gate)
        {
            Provider = provider;
            ConnectionString = connectionString;
            RequiresSetup = requiresSetup;
            LastError = lastError;
            LastValidationUtc = DateTimeOffset.UtcNow;
        }
    }

    public (RuntimeDatabaseProvider Provider, string ConnectionString, bool RequiresSetup, string? LastError, DateTimeOffset LastValidationUtc) Snapshot()
    {
        lock (_gate)
        {
            return (Provider, ConnectionString, RequiresSetup, LastError, LastValidationUtc);
        }
    }
}

public sealed record SetupConnectionTestResult(bool Success, string Message);
internal sealed record BuildConnectionResult(bool Success, RuntimeDatabaseProvider Provider, string? ConnectionString, string Message);

public interface IDatabaseSetupService
{
    DatabaseSetupViewModel BuildViewModel(string? returnUrl = null);
    Task<SetupConnectionTestResult> TestConnectionAsync(DatabaseSetupViewModel input, CancellationToken ct);
    Task<SetupConnectionTestResult> SaveAsync(DatabaseSetupViewModel input, CancellationToken ct);
    /// <param name="allowMissingDatabase">
    /// Start-up only: a reachable SQL Server whose database does not exist yet counts as healthy, because the
    /// EF migrations that run next create it (a fresh install or a dropped test database).
    /// </param>
    Task<SetupConnectionTestResult> ValidateCurrentConnectionAsync(bool force, CancellationToken ct, bool allowMissingDatabase = false);
    string ExportProfileJson();
    Task<SetupConnectionTestResult> ImportProfileAsync(DatabaseSetupProfileModel profile, CancellationToken ct);
    string BuildDockerEnvTemplate();
}

public sealed class DatabaseSetupService : IDatabaseSetupService
{
    private const string SetupFileName = "database-setup.json";
    private const string ProtectorPurpose = "Tabsan.EduSphere.API.DatabaseSetup.v1";

    private readonly IHostEnvironment _environment;
    private readonly IDataProtector _protector;
    private readonly DatabaseSetupRuntimeState _state;
    private readonly ILogger<DatabaseSetupService> _logger;

    public DatabaseSetupService(
        IHostEnvironment environment,
        IDataProtectionProvider dataProtectionProvider,
        DatabaseSetupRuntimeState state,
        ILogger<DatabaseSetupService> logger)
    {
        _environment = environment;
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        _state = state;
        _logger = logger;
    }

    public DatabaseSetupViewModel BuildViewModel(string? returnUrl = null)
    {
        var snapshot = _state.Snapshot();
        var stored = TryReadStoredConfiguration();

        return new DatabaseSetupViewModel
        {
            DatabaseType = stored?.DatabaseType ?? "Local",
            LocalProvider = stored?.LocalProvider ?? (snapshot.Provider == RuntimeDatabaseProvider.Sqlite ? "Sqlite" : "SqlServerExpress"),
            ServerHost = stored?.ServerHost,
            DatabaseName = stored?.DatabaseName,
            Username = stored?.Username,
            Port = stored?.Port,
            UseSsl = stored?.UseSsl ?? true,
            TrustServerCertificate = stored?.TrustServerCertificate ?? true,
            TrustedConnection = stored?.TrustedConnection ?? false,
            SqliteFilePath = stored?.SqliteFilePath,
            ReturnUrl = returnUrl,
            RequiresSetup = snapshot.RequiresSetup,
            CurrentProvider = snapshot.Provider.ToString(),
            StatusMessage = snapshot.LastError
        };
    }

    public async Task<SetupConnectionTestResult> TestConnectionAsync(DatabaseSetupViewModel input, CancellationToken ct)
    {
        var build = BuildConnection(input);
        if (!build.Success)
            return new SetupConnectionTestResult(false, build.Message);

        return await ProbeConnectionAsync(build.ConnectionString!, build.Provider, ct);
    }

    public async Task<SetupConnectionTestResult> SaveAsync(DatabaseSetupViewModel input, CancellationToken ct)
    {
        var build = BuildConnection(input);
        if (!build.Success)
            return new SetupConnectionTestResult(false, build.Message);

        var probe = await ProbeConnectionAsync(build.ConnectionString!, build.Provider, ct);
        if (!probe.Success)
            return probe;

        try
        {
            PersistEncryptedConfiguration(input, build.Provider, build.ConnectionString!);

            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", build.ConnectionString, EnvironmentVariableTarget.Process);
            _state.Set(build.Provider, build.ConnectionString!, requiresSetup: false, lastError: null);

            return new SetupConnectionTestResult(true, "Database configuration saved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist database setup configuration.");
            return new SetupConnectionTestResult(false, $"Failed to save setup: {ex.Message}");
        }
    }

    public async Task<SetupConnectionTestResult> ValidateCurrentConnectionAsync(bool force, CancellationToken ct, bool allowMissingDatabase = false)
    {
        var snapshot = _state.Snapshot();
        if (!force && DateTimeOffset.UtcNow - snapshot.LastValidationUtc < TimeSpan.FromSeconds(30))
        {
            return snapshot.RequiresSetup
                ? new SetupConnectionTestResult(false, snapshot.LastError ?? "Database setup is required.")
                : new SetupConnectionTestResult(true, "Database connection is healthy.");
        }

        if (string.IsNullOrWhiteSpace(snapshot.ConnectionString))
        {
            _state.Set(snapshot.Provider, snapshot.ConnectionString, requiresSetup: true, lastError: "Connection string is missing.");
            return new SetupConnectionTestResult(false, "Connection string is missing.");
        }

        if (snapshot.ConnectionString.Contains("REPLACE_WITH", StringComparison.OrdinalIgnoreCase))
        {
            _state.Set(snapshot.Provider, snapshot.ConnectionString, requiresSetup: true, lastError: "Placeholder connection string detected.");
            return new SetupConnectionTestResult(false, "Placeholder connection string detected.");
        }

        var probe = await ProbeConnectionAsync(snapshot.ConnectionString, snapshot.Provider, ct, allowMissingDatabase);
        _state.Set(snapshot.Provider, snapshot.ConnectionString, requiresSetup: !probe.Success, lastError: probe.Success ? null : probe.Message);
        return probe;
    }

    public string ExportProfileJson()
    {
        var stored = TryReadStoredConfiguration();
        if (stored is null)
        {
            var empty = new DatabaseSetupProfileModel();
            return JsonSerializer.Serialize(empty, new JsonSerializerOptions { WriteIndented = true });
        }

        var profile = new DatabaseSetupProfileModel
        {
            Version = stored.Version,
            DatabaseType = stored.DatabaseType ?? "Local",
            LocalProvider = stored.LocalProvider ?? "SqlServerExpress",
            ServerHost = stored.ServerHost,
            DatabaseName = stored.DatabaseName,
            Username = stored.Username,
            Port = stored.Port,
            UseSsl = stored.UseSsl,
            TrustServerCertificate = stored.TrustServerCertificate,
            TrustedConnection = stored.TrustedConnection,
            SqliteFilePath = stored.SqliteFilePath,
            ExportedUtc = DateTimeOffset.UtcNow
        };

        return JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
    }

    public Task<SetupConnectionTestResult> ImportProfileAsync(DatabaseSetupProfileModel profile, CancellationToken ct)
    {
        var input = new DatabaseSetupViewModel
        {
            DatabaseType = profile.DatabaseType,
            LocalProvider = profile.LocalProvider,
            ServerHost = profile.ServerHost,
            DatabaseName = profile.DatabaseName,
            Username = profile.Username,
            Password = profile.Password,
            Port = profile.Port,
            UseSsl = profile.UseSsl,
            TrustServerCertificate = profile.TrustServerCertificate,
            TrustedConnection = profile.TrustedConnection,
            SqliteFilePath = profile.SqliteFilePath
        };

        return SaveAsync(input, ct);
    }

    public string BuildDockerEnvTemplate()
    {
        var snapshot = _state.Snapshot();
        var lines = new List<string>
        {
            "# EduSphere runtime database setup",
            "ASPNETCORE_ENVIRONMENT=Production"
        };

        // The setup page asks for no sign-in, so this download never shows the password.
        var passwordHidden = false;
        string? shown = null;
        if (!string.IsNullOrWhiteSpace(snapshot.ConnectionString))
            shown = HidePassword(snapshot.ConnectionString, snapshot.Provider, out passwordHidden);

        if (shown is not null)
        {
            if (passwordHidden)
                lines.Add($"# The password is not shown: replace {PasswordPlaceholder} with it.");
            var escaped = shown.Replace("\"", "\\\"");
            lines.Add($"ConnectionStrings__DefaultConnection=\"{escaped}\"");
        }
        else
        {
            lines.Add("ConnectionStrings__DefaultConnection=\"<set-your-connection-string>\"");
        }

        lines.Add($"EDUSPHERE_RUNTIME_DB_PROVIDER={snapshot.Provider}");
        lines.Add("# Optional: mount persistent setup store");
        lines.Add("# -v /host/path/edusphere-app-data:/app/App_Data");

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Stands in for the password in connection strings shown on the setup page. It contains "REPLACE_WITH", so a
    /// template copied without the real password counts as unconfigured instead of failing to sign in.
    /// </summary>
    public const string PasswordPlaceholder = "REPLACE_WITH_PASSWORD";

    /// <summary>
    /// The connection string with its password (<c>Password</c> or <c>Pwd</c>, quoted or not) replaced by
    /// <see cref="PasswordPlaceholder"/>. Without a password it is returned unchanged; when it cannot be read it is
    /// not shown at all (<c>null</c>).
    /// </summary>
    public static string? HidePassword(string connectionString, RuntimeDatabaseProvider provider, out bool passwordHidden)
    {
        passwordHidden = false;
        try
        {
            if (provider == RuntimeDatabaseProvider.Sqlite)
            {
                var sqlite = new SqliteConnectionStringBuilder(connectionString);
                if (string.IsNullOrEmpty(sqlite.Password))
                    return connectionString;
                sqlite.Password = PasswordPlaceholder;
                passwordHidden = true;
                return sqlite.ConnectionString;
            }

            var sql = new SqlConnectionStringBuilder(connectionString);
            if (string.IsNullOrEmpty(sql.Password))
                return connectionString;
            sql.Password = PasswordPlaceholder;
            passwordHidden = true;
            return sql.ConnectionString;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }

    private BuildConnectionResult BuildConnection(DatabaseSetupViewModel input)
    {
        var provider = ResolveProvider(input);

        if (provider == RuntimeDatabaseProvider.Sqlite)
        {
            if (string.IsNullOrWhiteSpace(input.SqliteFilePath))
                return new BuildConnectionResult(false, provider, null, "SQLite file path is required.");

            var sqlitePath = input.SqliteFilePath.Trim();
            var directory = Path.GetDirectoryName(sqlitePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            return new BuildConnectionResult(true, provider, $"Data Source={sqlitePath}", "Connection string built.");
        }

        if (string.IsNullOrWhiteSpace(input.ServerHost))
            return new BuildConnectionResult(false, provider, null, "Server/Host is required.");

        if (string.IsNullOrWhiteSpace(input.DatabaseName))
            return new BuildConnectionResult(false, provider, null, "Database Name is required.");

        var dataSource = input.ServerHost.Trim();
        if (input.Port is > 0)
            dataSource = $"{dataSource},{input.Port.Value}";

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = dataSource,
            InitialCatalog = input.DatabaseName.Trim(),
            Encrypt = input.UseSsl,
            TrustServerCertificate = input.TrustServerCertificate,
            MultipleActiveResultSets = true,
            ConnectTimeout = 30
        };

        if (input.TrustedConnection)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(input.Username))
                return new BuildConnectionResult(false, provider, null, "Username is required when Trusted Connection is disabled.");
            if (string.IsNullOrWhiteSpace(input.Password))
                return new BuildConnectionResult(false, provider, null, "Password is required when Trusted Connection is disabled.");

            builder.UserID = input.Username.Trim();
            builder.Password = input.Password;
        }

        return new BuildConnectionResult(true, provider, builder.ConnectionString, "Connection string built.");
    }

    private static RuntimeDatabaseProvider ResolveProvider(DatabaseSetupViewModel input)
    {
        if (string.Equals(input.LocalProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            return RuntimeDatabaseProvider.Sqlite;

        return RuntimeDatabaseProvider.SqlServer;
    }

    private async Task<SetupConnectionTestResult> ProbeConnectionAsync(string connectionString, RuntimeDatabaseProvider provider, CancellationToken ct, bool allowMissingDatabase = false)
    {
        try
        {
            await using DbConnection connection = provider switch
            {
                RuntimeDatabaseProvider.Sqlite => new SqliteConnection(connectionString),
                _ => new SqlConnection(connectionString)
            };

            await connection.OpenAsync(ct);
            await connection.CloseAsync();
            return new SetupConnectionTestResult(true, "Connection test succeeded.");
        }
        catch (SqlException ex) when (allowMissingDatabase && provider == RuntimeDatabaseProvider.SqlServer
                                      && ex.Number == CannotOpenDatabaseError)
        {
            if (await DatabaseIsMissingAsync(connectionString, ct))
            {
                var name = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
                _logger.LogInformation("Database {Database} does not exist yet; the start-up migrations will create it.", name);
                return new SetupConnectionTestResult(true, $"Server reachable; database '{name}' will be created by the migrations.");
            }

            _logger.LogWarning(ex, "Database connection test failed for provider {Provider}", provider);
            return new SetupConnectionTestResult(false, $"Connection test failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database connection test failed for provider {Provider}", provider);
            return new SetupConnectionTestResult(false, $"Connection test failed: {ex.Message}");
        }
    }

    /// <summary>SQL Server error 4060: "Cannot open database … requested by the login".</summary>
    private const int CannotOpenDatabaseError = 4060;

    /// <summary>
    /// True when the server answers on <c>master</c> and the target database is not there. Any other cause of error 4060
    /// (the database exists but the login may not open it) stays a failure.
    /// </summary>
    private async Task<bool> DatabaseIsMissingAsync(string connectionString, CancellationToken ct)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var database = builder.InitialCatalog;
            if (string.IsNullOrWhiteSpace(database))
                return false;

            builder.InitialCatalog = "master";
            await using var master = new SqlConnection(builder.ConnectionString);
            await master.OpenAsync(ct);
            await using var command = new SqlCommand("SELECT DB_ID(@name)", master);
            command.Parameters.AddWithValue("@name", database);
            return await command.ExecuteScalarAsync(ct) is null or DBNull;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check on master whether the configured database exists.");
            return false;
        }
    }

    private void PersistEncryptedConfiguration(DatabaseSetupViewModel input, RuntimeDatabaseProvider provider, string connectionString)
    {
        var appDataPath = Path.Combine(_environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(appDataPath);

        var payload = new StoredDatabaseSetup
        {
            Version = 1,
            Provider = provider.ToString(),
            DatabaseType = input.DatabaseType,
            LocalProvider = input.LocalProvider,
            ServerHost = input.ServerHost,
            DatabaseName = input.DatabaseName,
            Username = input.Username,
            Port = input.Port,
            UseSsl = input.UseSsl,
            TrustServerCertificate = input.TrustServerCertificate,
            TrustedConnection = input.TrustedConnection,
            SqliteFilePath = input.SqliteFilePath,
            EncryptedConnectionString = _protector.Protect(connectionString),
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        var filePath = Path.Combine(appDataPath, SetupFileName);
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    private StoredDatabaseSetup? TryReadStoredConfiguration()
    {
        var appDataPath = Path.Combine(_environment.ContentRootPath, "App_Data");
        var filePath = Path.Combine(appDataPath, SetupFileName);
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<StoredDatabaseSetup>(json);
        }
        catch
        {
            return null;
        }
    }

    private sealed class StoredDatabaseSetup
    {
        public int Version { get; set; }
        public string Provider { get; set; } = RuntimeDatabaseProvider.SqlServer.ToString();
        public string? DatabaseType { get; set; }
        public string? LocalProvider { get; set; }
        public string? ServerHost { get; set; }
        public string? DatabaseName { get; set; }
        public string? Username { get; set; }
        public int? Port { get; set; }
        public bool UseSsl { get; set; }
        public bool TrustServerCertificate { get; set; }
        public bool TrustedConnection { get; set; }
        public string? SqliteFilePath { get; set; }
        public string EncryptedConnectionString { get; set; } = string.Empty;
        public DateTimeOffset UpdatedUtc { get; set; }
    }
}

public static class DatabaseSetupBootstrapper
{
    private const string SetupFileName = "database-setup.json";
    private const string ProtectorPurpose = "Tabsan.EduSphere.API.DatabaseSetup.v1";

    public static (RuntimeDatabaseProvider Provider, string ConnectionString, bool HasRuntimeSetup, string Source) Resolve(string contentRootPath, string configuredConnectionString)
    {
        var appDataPath = Path.Combine(contentRootPath, "App_Data");
        var filePath = Path.Combine(appDataPath, SetupFileName);

        if (!File.Exists(filePath))
            return (RuntimeDatabaseProvider.SqlServer, configuredConnectionString, false, "appsettings/environment");

        try
        {
            var json = File.ReadAllText(filePath);
            var doc = JsonDocument.Parse(json);

            var providerRaw = doc.RootElement.TryGetProperty("Provider", out var providerEl)
                ? providerEl.GetString()
                : RuntimeDatabaseProvider.SqlServer.ToString();

            var encrypted = doc.RootElement.TryGetProperty("EncryptedConnectionString", out var connEl)
                ? connEl.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(encrypted))
                return (RuntimeDatabaseProvider.SqlServer, configuredConnectionString, false, "appsettings/environment");

            var dataProtection = DataProtectionProvider.Create(new DirectoryInfo(appDataPath));
            var protector = dataProtection.CreateProtector(ProtectorPurpose);
            var connectionString = protector.Unprotect(encrypted);

            var provider = Enum.TryParse<RuntimeDatabaseProvider>(providerRaw, ignoreCase: true, out var parsed)
                ? parsed
                : RuntimeDatabaseProvider.SqlServer;

            return (provider, connectionString, true, "runtime encrypted setup");
        }
        catch
        {
            return (RuntimeDatabaseProvider.SqlServer, configuredConnectionString, false, "appsettings/environment");
        }
    }
}
