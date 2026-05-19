using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Tabsan.EduSphere.Application.Services;

public static class TenantIsolationResolver
{
    public static TenantIsolationResolution Resolve(IConfiguration configuration, IHostEnvironment environment, DeploymentTopologyResolution? deploymentTopology = null)
    {
        var enabled = ReadBool(configuration, "EDUSPHERE_TENANT_ISOLATION_ENABLED", "Deployment:TenantIsolation:Enabled", false);
        var mode = ReadString(configuration, "EDUSPHERE_TENANT_ISOLATION_MODE", "Deployment:TenantIsolation:Mode", enabled ? "Isolated" : "Shared");
        var tenantCode = ReadString(configuration, "EDUSPHERE_TENANT_CODE", "Deployment:TenantIsolation:TenantCode", deploymentTopology?.CustomerCode ?? environment.EnvironmentName);
        var tenantName = ReadString(configuration, "EDUSPHERE_TENANT_NAME", "Deployment:TenantIsolation:TenantName", deploymentTopology?.CustomerName ?? string.Empty);
        var tenantDomain = ReadString(configuration, "EDUSPHERE_TENANT_DOMAIN", "Deployment:TenantIsolation:TenantDomain", deploymentTopology?.CustomerDomain ?? string.Empty);
        var tenantDatabaseName = ReadString(configuration, "EDUSPHERE_TENANT_DATABASE_NAME", "Deployment:TenantIsolation:TenantDatabaseName", deploymentTopology?.CustomerDatabaseName ?? string.Empty);
        var tenantConfigPath = ReadString(configuration, "EDUSPHERE_TENANT_CONFIG_PATH", "Deployment:TenantIsolation:ConfigPath", string.Empty);
        var isolationStrategy = ReadString(configuration, "EDUSPHERE_TENANT_ISOLATION_STRATEGY", "Deployment:TenantIsolation:IsolationStrategy", string.IsNullOrWhiteSpace(tenantConfigPath) ? "SharedConfig" : "FileOverlay");

        return new TenantIsolationResolution(
            Enabled: enabled,
            Mode: mode,
            TenantCode: tenantCode,
            TenantName: tenantName,
            TenantDomain: tenantDomain,
            TenantDatabaseName: tenantDatabaseName,
            TenantConfigPath: tenantConfigPath,
            IsolationStrategy: isolationStrategy,
            Source: BuildSourceSummary(configuration));
    }

    private static string ReadString(IConfiguration configuration, string environmentVariableName, string configurationKey, string fallback)
    {
        var environmentValue = Environment.GetEnvironmentVariable(environmentVariableName);
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return environmentValue.Trim();
        }

        var configuredValue = configuration[configurationKey];
        if (!string.IsNullOrWhiteSpace(configuredValue))
        {
            return configuredValue.Trim();
        }

        return fallback.Trim();
    }

    private static bool ReadBool(IConfiguration configuration, string environmentVariableName, string configurationKey, bool fallback)
    {
        var environmentValue = Environment.GetEnvironmentVariable(environmentVariableName);
        if (bool.TryParse(environmentValue, out var parsedEnvironmentValue))
        {
            return parsedEnvironmentValue;
        }

        return configuration.GetValue(configurationKey, fallback);
    }

    private static string BuildSourceSummary(IConfiguration configuration)
    {
        return string.Join(
            "; ",
            new[]
            {
                configuration["Deployment:TenantIsolation:Enabled"] is not null ? "Deployment:TenantIsolation:Enabled" : null,
                configuration["Deployment:TenantIsolation:Mode"] is not null ? "Deployment:TenantIsolation:Mode" : null,
                configuration["Deployment:TenantIsolation:TenantCode"] is not null ? "Deployment:TenantIsolation:TenantCode" : null,
                configuration["Deployment:TenantIsolation:TenantDomain"] is not null ? "Deployment:TenantIsolation:TenantDomain" : null,
                configuration["Deployment:TenantIsolation:TenantDatabaseName"] is not null ? "Deployment:TenantIsolation:TenantDatabaseName" : null,
                configuration["Deployment:TenantIsolation:ConfigPath"] is not null ? "Deployment:TenantIsolation:ConfigPath" : null
            }.Where(value => !string.IsNullOrWhiteSpace(value))!);
    }
}

public sealed record TenantIsolationResolution(
    bool Enabled,
    string Mode,
    string TenantCode,
    string TenantName,
    string TenantDomain,
    string TenantDatabaseName,
    string TenantConfigPath,
    string IsolationStrategy,
    string Source);
