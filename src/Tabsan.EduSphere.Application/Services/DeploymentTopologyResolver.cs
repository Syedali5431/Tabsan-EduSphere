using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Tabsan.EduSphere.Application.Services;

public static class DeploymentTopologyResolver
{
    public static DeploymentTopologyResolution Resolve(IConfiguration configuration, IHostEnvironment environment)
    {
        var mode = ReadString(configuration, "EDUSPHERE_DEPLOYMENT_MODE", "Deployment:Mode", "SingleInstance");
        var customerCode = ReadString(configuration, "EDUSPHERE_CUSTOMER_CODE", "Deployment:CustomerCode", environment.EnvironmentName);
        var customerName = ReadString(configuration, "EDUSPHERE_CUSTOMER_NAME", "Deployment:CustomerName", null);
        var customerDomain = ReadString(configuration, "EDUSPHERE_DEPLOYMENT_DOMAIN", "Deployment:CustomerDomain", ResolveDomainFallback(configuration));
        var customerDatabaseName = ReadString(configuration, "EDUSPHERE_DEPLOYMENT_DATABASE_NAME", "Deployment:Database:Name", ResolveDatabaseFallback(configuration, customerCode));

        var scalingEnabled = ReadBool(configuration, "EDUSPHERE_DEPLOYMENT_SCALING_ENABLED", "Deployment:Scaling:Enabled", configuration.GetValue("InfrastructureTuning:AutoScaling:Enabled", true));
        var minReplicas = ReadInt(configuration, "EDUSPHERE_DEPLOYMENT_MIN_REPLICAS", "Deployment:Scaling:MinReplicas", configuration.GetValue("InfrastructureTuning:AutoScaling:MinReplicas", 1));
        var maxReplicas = ReadInt(configuration, "EDUSPHERE_DEPLOYMENT_MAX_REPLICAS", "Deployment:Scaling:MaxReplicas", configuration.GetValue("InfrastructureTuning:AutoScaling:MaxReplicas", 1));

        if (maxReplicas < minReplicas)
        {
            maxReplicas = minReplicas;
        }

        return new DeploymentTopologyResolution(
            Mode: mode,
            CustomerCode: customerCode,
            CustomerName: customerName,
            CustomerDomain: customerDomain,
            CustomerDatabaseName: customerDatabaseName,
            ScalingEnabled: scalingEnabled,
            MinReplicas: minReplicas,
            MaxReplicas: maxReplicas,
            Source: BuildSourceSummary(configuration));
    }

    private static string ReadString(IConfiguration configuration, string environmentVariableName, string configurationKey, string? fallback)
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

        return fallback?.Trim() ?? string.Empty;
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

    private static int ReadInt(IConfiguration configuration, string environmentVariableName, string configurationKey, int fallback)
    {
        var environmentValue = Environment.GetEnvironmentVariable(environmentVariableName);
        if (int.TryParse(environmentValue, out var parsedEnvironmentValue))
        {
            return parsedEnvironmentValue;
        }

        return configuration.GetValue(configurationKey, fallback);
    }

    private static string ResolveDomainFallback(IConfiguration configuration)
    {
        var webBaseUrl = configuration["AppSettings:WebBaseUrl"];
        if (Uri.TryCreate(webBaseUrl, UriKind.Absolute, out var webUri) && !string.IsNullOrWhiteSpace(webUri.Host))
        {
            return webUri.Host;
        }

        var apiBaseUrl = configuration["AppSettings:ApiBaseUrl"];
        if (Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri) && !string.IsNullOrWhiteSpace(apiUri.Host))
        {
            return apiUri.Host;
        }

        var allowedHosts = configuration["AllowedHosts"];
        if (!string.IsNullOrWhiteSpace(allowedHosts))
        {
            return allowedHosts;
        }

        return string.Empty;
    }

    private static string ResolveDatabaseFallback(IConfiguration configuration, string customerCode)
    {
        var configuredDatabaseName = configuration["Deployment:Database:Name"];
        if (!string.IsNullOrWhiteSpace(configuredDatabaseName))
        {
            return configuredDatabaseName.Trim();
        }

        var configuredConnectionString = configuration["ConnectionStrings:DefaultConnection"];
        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            var databaseName = ExtractConnectionStringValue(configuredConnectionString, "Database")
                ?? ExtractConnectionStringValue(configuredConnectionString, "Initial Catalog");

            if (!string.IsNullOrWhiteSpace(databaseName))
            {
                return databaseName;
            }
        }

        return customerCode;
    }

    private static string? ExtractConnectionStringValue(string connectionString, string keyName)
    {
        var segments = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            var parts = segment.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && parts[0].Equals(keyName, StringComparison.OrdinalIgnoreCase))
            {
                return parts[1];
            }
        }

        return null;
    }

    private static string BuildSourceSummary(IConfiguration configuration)
    {
        return string.Join(
            "; ",
            new[]
            {
                configuration["Deployment:Mode"] is not null ? "Deployment:Mode" : null,
                configuration["Deployment:CustomerCode"] is not null ? "Deployment:CustomerCode" : null,
                configuration["Deployment:CustomerDomain"] is not null ? "Deployment:CustomerDomain" : null,
                configuration["Deployment:Database:Name"] is not null ? "Deployment:Database:Name" : null,
                configuration["Deployment:Scaling:MinReplicas"] is not null || configuration["Deployment:Scaling:MaxReplicas"] is not null ? "Deployment:Scaling" : null
            }.Where(value => !string.IsNullOrWhiteSpace(value))!);
    }
}

public sealed record DeploymentTopologyResolution(
    string Mode,
    string CustomerCode,
    string CustomerName,
    string CustomerDomain,
    string CustomerDatabaseName,
    bool ScalingEnabled,
    int MinReplicas,
    int MaxReplicas,
    string Source);
