using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Tabsan.EduSphere.Application.Services;

public static class StartupConfigurationFailSafeValidator
{
    public static void ValidateCommonStartupConfiguration(
        IConfiguration configuration,
        IHostEnvironment environment,
        string applicationName,
        DeploymentTopologyResolution deploymentTopology,
        TenantIsolationResolution tenantIsolation)
    {
        ValidateDeploymentScaling(applicationName, deploymentTopology);
        ValidateReverseProxy(configuration, environment, applicationName);
        ValidateTenantIsolation(environment, applicationName, tenantIsolation);
    }

    public static void ValidateCommonStartupConfiguration(
        IConfiguration configuration,
        IHostEnvironment environment,
        string applicationName,
        DatabaseConnectionResolution databaseConnection,
        DeploymentTopologyResolution deploymentTopology,
        TenantIsolationResolution tenantIsolation)
    {
        ValidateCommonStartupConfiguration(configuration, environment, applicationName, deploymentTopology, tenantIsolation);
        ValidateDatabaseConnection(environment, applicationName, databaseConnection);
    }

    public static void ValidateRequiredSetting(
        IHostEnvironment environment,
        string applicationName,
        string settingPath,
        string? value,
        int minLength = 1)
    {
        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            return;
        }

        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length < minLength || SecureConfigurationValidator.IsUnsafePlaceholderValue(trimmed))
        {
            throw new InvalidOperationException($"{applicationName} startup requires '{settingPath}' to be set to a non-placeholder value outside Development/Testing.");
        }
    }

    private static void ValidateDatabaseConnection(IHostEnvironment environment, string applicationName, DatabaseConnectionResolution databaseConnection)
    {
        var connectionString = databaseConnection.ConnectionString?.Trim() ?? string.Empty;
        var isNonDevelopmentEnvironment = !environment.IsDevelopment() && !environment.IsEnvironment("Testing");
        if (string.IsNullOrWhiteSpace(connectionString)
            || connectionString.Contains("NOT_SET", StringComparison.OrdinalIgnoreCase)
            || (isNonDevelopmentEnvironment && SecureConfigurationValidator.IsUnsafePlaceholderValue(connectionString)))
        {
            throw new InvalidOperationException($"{applicationName} startup requires a valid database connection string. Resolved source: {databaseConnection.Source}. Configure EDUSPHERE_DB_CONNECTION, Deployment:Database:ConnectionString, or ConnectionStrings:DefaultConnection.");
        }

        if (isNonDevelopmentEnvironment && IsLoopbackSqlServerConnection(connectionString))
        {
            throw new InvalidOperationException($"{applicationName} startup blocked a localhost/localdb SQL connection string in non-development mode. Resolved source: {databaseConnection.Source}. Configure EDUSPHERE_DB_CONNECTION (or Deployment:Database:ConnectionString) to a reachable external database endpoint.");
        }
    }

    private static bool IsLoopbackSqlServerConnection(string connectionString)
    {
        var normalized = connectionString.Trim();
        if (normalized.Length == 0)
        {
            return false;
        }

        var lowered = normalized.ToLowerInvariant();
        if (lowered.Contains("(localdb)"))
        {
            return true;
        }

        var parts = normalized.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            var separatorIndex = part.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = part[..separatorIndex].Trim();
            var value = part[(separatorIndex + 1)..].Trim();
            if (value.Length == 0)
            {
                continue;
            }

            if (!key.Equals("Server", StringComparison.OrdinalIgnoreCase)
                && !key.Equals("Data Source", StringComparison.OrdinalIgnoreCase)
                && !key.Equals("Addr", StringComparison.OrdinalIgnoreCase)
                && !key.Equals("Address", StringComparison.OrdinalIgnoreCase)
                && !key.Equals("Network Address", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var hostToken = value;
            var tcpPrefix = "tcp:";
            if (hostToken.StartsWith(tcpPrefix, StringComparison.OrdinalIgnoreCase))
            {
                hostToken = hostToken[tcpPrefix.Length..];
            }

            var commaIndex = hostToken.IndexOf(',');
            if (commaIndex >= 0)
            {
                hostToken = hostToken[..commaIndex];
            }

            var slashIndex = hostToken.IndexOf('/');
            if (slashIndex >= 0)
            {
                hostToken = hostToken[..slashIndex];
            }

            var backslashIndex = hostToken.IndexOf('\\');
            if (backslashIndex >= 0)
            {
                hostToken = hostToken[..backslashIndex];
            }

            var colonIndex = hostToken.IndexOf(':');
            if (colonIndex >= 0)
            {
                hostToken = hostToken[..colonIndex];
            }

            hostToken = hostToken.Trim();
            if (hostToken.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || hostToken.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || hostToken.Equals("::1", StringComparison.OrdinalIgnoreCase)
                || hostToken.Equals(".", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidateDeploymentScaling(string applicationName, DeploymentTopologyResolution deploymentTopology)
    {
        if (!deploymentTopology.ScalingEnabled)
        {
            return;
        }

        if (deploymentTopology.MinReplicas > deploymentTopology.MaxReplicas)
        {
            throw new InvalidOperationException($"{applicationName} startup found invalid deployment scaling settings: Deployment:Scaling:MinReplicas cannot exceed Deployment:Scaling:MaxReplicas.");
        }
    }

    private static void ValidateReverseProxy(IConfiguration configuration, IHostEnvironment environment, string applicationName)
    {
        var useForwardedHeaders = configuration.GetValue<bool>("ReverseProxy:Enabled");
        var configuredKnownProxies = configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [];
        if (useForwardedHeaders && !environment.IsDevelopment() && !environment.IsEnvironment("Testing") && configuredKnownProxies.Length == 0)
        {
            throw new InvalidOperationException($"{applicationName} startup has ReverseProxy enabled but ReverseProxy:KnownProxies is empty. Configure at least one trusted proxy IP before non-development startup.");
        }
    }

    private static void ValidateTenantIsolation(IHostEnvironment environment, string applicationName, TenantIsolationResolution tenantIsolation)
    {
        if (!tenantIsolation.Enabled)
        {
            if (!string.IsNullOrWhiteSpace(tenantIsolation.TenantConfigPath) && !File.Exists(tenantIsolation.TenantConfigPath))
            {
                throw new InvalidOperationException($"{applicationName} startup could not find the tenant configuration overlay file '{tenantIsolation.TenantConfigPath}'.");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(tenantIsolation.TenantCode))
        {
            throw new InvalidOperationException($"{applicationName} startup requires Deployment:TenantIsolation:TenantCode when tenant isolation is enabled.");
        }

        if (!string.IsNullOrWhiteSpace(tenantIsolation.TenantConfigPath) && !File.Exists(tenantIsolation.TenantConfigPath))
        {
            throw new InvalidOperationException($"{applicationName} startup could not find the tenant configuration overlay file '{tenantIsolation.TenantConfigPath}'.");
        }

        if (tenantIsolation.IsolationStrategy.Equals("FileOverlay", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(tenantIsolation.TenantConfigPath))
        {
            throw new InvalidOperationException($"{applicationName} startup requires Deployment:TenantIsolation:ConfigPath when TenantIsolation:IsolationStrategy is FileOverlay.");
        }
    }
}