using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Tabsan.EduSphere.Application.Services;

public static class DatabaseConnectionResolver
{
    public static DatabaseConnectionResolution ResolveDefaultConnection(IConfiguration configuration, IHostEnvironment environment)
    {
        var profileResolution = EnvironmentConfigurationResolver.Resolve(configuration, environment);

        var explicitEnvironmentCandidates = new[]
        {
            (Name: "EDUSPHERE_DB_CONNECTION", Value: Environment.GetEnvironmentVariable("EDUSPHERE_DB_CONNECTION")),
            (Name: "EDUSPHERE_DEFAULT_CONNECTION", Value: Environment.GetEnvironmentVariable("EDUSPHERE_DEFAULT_CONNECTION")),
            (Name: "DB_CONNECTION", Value: Environment.GetEnvironmentVariable("DB_CONNECTION")),
            (Name: "DATABASE_URL", Value: Environment.GetEnvironmentVariable("DATABASE_URL"))
        };

        foreach (var candidate in explicitEnvironmentCandidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate.Value))
            {
                return new DatabaseConnectionResolution(candidate.Value.Trim(), $"Environment variable: {candidate.Name}");
            }
        }

        if (profileResolution.ShouldPreferProfile && !string.IsNullOrWhiteSpace(profileResolution.DatabaseConnectionString))
        {
            return new DatabaseConnectionResolution(
                profileResolution.DatabaseConnectionString.Trim(),
                $"Environment profile: {profileResolution.EnvironmentName} ({profileResolution.DetectionSource})");
        }

        var orderedConfigurationCandidates = new[]
        {
            (Key: "Deployment:Database:ConnectionString", Source: "Deployment settings"),
            (Key: "Database:ConnectionString", Source: "Database settings"),
            (Key: "Database:DefaultConnection", Source: "Database settings"),
            (Key: "ConnectionStrings:DefaultConnection", Source: "ConnectionStrings")
        };

        foreach (var candidate in orderedConfigurationCandidates)
        {
            var value = configuration[candidate.Key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return new DatabaseConnectionResolution(value.Trim(), $"Configuration key: {candidate.Key} ({candidate.Source})");
            }
        }

        if (!string.IsNullOrWhiteSpace(profileResolution.DatabaseConnectionString))
        {
            return new DatabaseConnectionResolution(
                profileResolution.DatabaseConnectionString.Trim(),
                $"Environment profile fallback: {profileResolution.EnvironmentName} ({profileResolution.DetectionSource})");
        }

        throw new InvalidOperationException($"Default database connection string is missing for environment '{environment.EnvironmentName}'. Configure EDUSPHERE_DB_CONNECTION, Deployment:Database:ConnectionString, Database:ConnectionString, ConnectionStrings:DefaultConnection, or Environments:<name>:DatabaseConnectionString in environments.json.");
    }
}

public sealed record DatabaseConnectionResolution(string ConnectionString, string Source);
