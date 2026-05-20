using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Tabsan.EduSphere.Application.Services;

public static class EnvironmentConfigurationResolver
{
    private static readonly string[] ExplicitEnvironmentVariableCandidates =
    [
        "EDUSPHERE_ENVIRONMENT",
        "ASPNETCORE_ENVIRONMENT",
        "DOTNET_ENVIRONMENT",
        "NODE_ENV"
    ];

    private static readonly string[] CiSignals =
    [
        "GITHUB_ACTIONS",
        "CI",
        "TF_BUILD",
        "GITLAB_CI",
        "BUILD_BUILDID"
    ];

    public static EnvironmentConfigurationResolution Resolve(IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        var warnings = new List<string>();
        var environmentsSection = configuration.GetSection("Environments");
        var profiles = environmentsSection.GetChildren().ToList();
        if (profiles.Count == 0)
        {
            warnings.Add("No 'Environments' section was found in environments.json or merged configuration sources.");
        }

        string detectedEnvironment;
        string detectionSource;
        var shouldPreferProfile = false;

        if (TryResolveFromExplicitEnvironmentVariables(configuration, profiles, out detectedEnvironment, out detectionSource))
        {
            shouldPreferProfile = true;
        }
        else if (IsRunningInsideContainer() && TryResolveByName(profiles, "Docker", out detectedEnvironment))
        {
            detectionSource = "DockerDetection";
            shouldPreferProfile = true;
        }
        else if (IsCiEnvironment() && TryResolveByName(profiles, "CI/CD", out detectedEnvironment))
        {
            detectionSource = "CICDDetection";
            shouldPreferProfile = true;
        }
        else if (TryResolveFromHostName(configuration, profiles, out detectedEnvironment, out detectionSource))
        {
            shouldPreferProfile = true;
        }
        else
        {
            var defaultEnvironment = configuration["DefaultEnvironment"]?.Trim();
            if (!string.IsNullOrWhiteSpace(defaultEnvironment) && TryResolveByName(profiles, defaultEnvironment, out detectedEnvironment))
            {
                detectionSource = "DefaultEnvironmentFallback";
            }
            else if (TryResolveByName(profiles, hostEnvironment.EnvironmentName, out detectedEnvironment))
            {
                detectionSource = "HostEnvironmentFallback";
            }
            else if (profiles.Count > 0)
            {
                detectedEnvironment = profiles[0].Key;
                detectionSource = "FirstAvailableProfileFallback";
                warnings.Add("DefaultEnvironment value was not found; using the first available environment profile.");
            }
            else
            {
                detectedEnvironment = hostEnvironment.EnvironmentName;
                detectionSource = "NoProfilesAvailable";
                warnings.Add("Environment profile fallback could not be resolved because no profiles were available.");
            }
        }

        var selectedProfile = FindProfile(profiles, detectedEnvironment);
        var appConnectionString = selectedProfile?["AppConnectionString"]?.Trim();
        var databaseConnectionString = selectedProfile?["DatabaseConnectionString"]?.Trim();

        var appOverride = FirstNonEmptyEnvironmentValue(
            "EDUSPHERE_APP_CONNECTION",
            "EDUSPHERE_APP_BASE_URL",
            "APP_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(appOverride))
        {
            appConnectionString = appOverride;
            warnings.Add("App connection string was overridden by environment variable.");
            shouldPreferProfile = true;
        }

        var databaseOverride = FirstNonEmptyEnvironmentValue(
            "EDUSPHERE_DB_CONNECTION",
            "EDUSPHERE_DEFAULT_CONNECTION",
            "DB_CONNECTION",
            "DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(databaseOverride))
        {
            databaseConnectionString = databaseOverride;
            warnings.Add("Database connection string was overridden by environment variable.");
            shouldPreferProfile = true;
        }

        if (selectedProfile is null)
        {
            warnings.Add($"Detected environment '{detectedEnvironment}' does not have a matching profile entry.");
        }

        return new EnvironmentConfigurationResolution(
            detectedEnvironment,
            detectionSource,
            appConnectionString,
            databaseConnectionString,
            shouldPreferProfile,
            warnings);
    }

    private static bool TryResolveFromExplicitEnvironmentVariables(
        IConfiguration configuration,
        IReadOnlyCollection<IConfigurationSection> profiles,
        out string environment,
        out string source)
    {
        foreach (var variableName in ExplicitEnvironmentVariableCandidates)
        {
            var rawValue = Environment.GetEnvironmentVariable(variableName)?.Trim();
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                continue;
            }

            if (TryResolveByName(profiles, rawValue, out environment))
            {
                source = $"EnvironmentVariable:{variableName}";
                return true;
            }

            var normalizedAlias = NormalizeAlias(rawValue);
            if (TryResolveByName(profiles, normalizedAlias, out environment))
            {
                source = $"EnvironmentVariable:{variableName}";
                return true;
            }
        }

        environment = string.Empty;
        source = string.Empty;
        return false;
    }

    private static bool TryResolveFromHostName(
        IConfiguration configuration,
        IReadOnlyCollection<IConfigurationSection> profiles,
        out string environment,
        out string source)
    {
        var hostName = Environment.MachineName.Trim();
        var configuredMap = configuration.GetSection("EnvironmentHostMap").GetChildren();

        foreach (var entry in configuredMap)
        {
            var configuredHost = entry.Key.Trim();
            var mappedEnvironment = entry.Value?.Trim();
            if (string.IsNullOrWhiteSpace(configuredHost) || string.IsNullOrWhiteSpace(mappedEnvironment))
            {
                continue;
            }

            if (!string.Equals(configuredHost, hostName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TryResolveByName(profiles, mappedEnvironment, out environment))
            {
                source = "HostNameMapping";
                return true;
            }
        }

        environment = string.Empty;
        source = string.Empty;
        return false;
    }

    private static IConfigurationSection? FindProfile(IEnumerable<IConfigurationSection> profiles, string environment)
        => profiles.FirstOrDefault(p => IsNameMatch(p.Key, environment));

    private static bool TryResolveByName(IReadOnlyCollection<IConfigurationSection> profiles, string candidate, out string environment)
    {
        foreach (var profile in profiles)
        {
            if (!IsNameMatch(profile.Key, candidate))
            {
                continue;
            }

            environment = profile.Key;
            return true;
        }

        environment = string.Empty;
        return false;
    }

    private static bool IsNameMatch(string configuredName, string candidate)
    {
        if (string.Equals(configuredName, candidate, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(
            NormalizeToken(configuredName),
            NormalizeToken(candidate),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeAlias(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "local" or "localhost" or "development" or "dev" => "LocalHost",
            "production" or "prod" or "cloud" => "Cloud",
            "stage" or "staging" => "Staging",
            "docker" or "container" => "Docker",
            "ci" or "cicd" or "ci/cd" => "CI/CD",
            "vps" => "VPS",
            "test" or "testing" => "Testing",
            _ => value.Trim()
        };
    }

    private static string NormalizeToken(string value)
    {
        var chars = value.Where(char.IsLetterOrDigit).ToArray();
        return new string(chars).ToLowerInvariant();
    }

    private static string? FirstNonEmptyEnvironmentValue(params string[] variableNames)
    {
        foreach (var variableName in variableNames)
        {
            var value = Environment.GetEnvironmentVariable(variableName)?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static bool IsRunningInsideContainer()
    {
        var dotnetContainerSignal = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        if (string.Equals(dotnetContainerSignal, "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(Environment.GetEnvironmentVariable("CONTAINER"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            return File.Exists("/.dockerenv");
        }
        catch
        {
            return false;
        }
    }

    private static bool IsCiEnvironment()
    {
        foreach (var variable in CiSignals)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrWhiteSpace(value) && !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) && value != "0")
            {
                return true;
            }
        }

        return false;
    }
}

public sealed record EnvironmentConfigurationResolution(
    string EnvironmentName,
    string DetectionSource,
    string? AppConnectionString,
    string? DatabaseConnectionString,
    bool ShouldPreferProfile,
    IReadOnlyList<string> Warnings);
