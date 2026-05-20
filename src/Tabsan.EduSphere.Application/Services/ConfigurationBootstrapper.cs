using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Hosting;

namespace Tabsan.EduSphere.Application.Services;

public static class ConfigurationBootstrapper
{
    private const string DefaultEnvironmentVariablePrefix = "EDUSPHERE_";
    private const string BaseSettingsFile = "appsettings.json";
    private const string DeploymentSettingsFile = "appsettings.Deployment.json";
    private const string ExternalSettingsFile = "appsettings.External.json";
    private const string LocalSettingsFile = "appsettings.Local.json";
    private const string EnvironmentProfilesFile = "environments.json";

    public static IConfigurationBuilder AddEduSphereConfigurationHierarchy(
        this IConfigurationBuilder configurationBuilder,
        IHostEnvironment environment,
        string? environmentVariablePrefix = null)
    {
        var prefix = string.IsNullOrWhiteSpace(environmentVariablePrefix)
            ? DefaultEnvironmentVariablePrefix
            : environmentVariablePrefix.Trim();
        var tenantConfigPath = Environment.GetEnvironmentVariable("EDUSPHERE_TENANT_CONFIG_PATH")?.Trim();
        var environmentProfilesPath = Environment.GetEnvironmentVariable("EDUSPHERE_ENVIRONMENTS_FILE")?.Trim();
        var environmentSettingsFile = $"appsettings.{environment.EnvironmentName}.json";
        var insertIndex = GetInsertionIndex(configurationBuilder);
        var contentRootParent = Directory.GetParent(environment.ContentRootPath)?.FullName;

        configurationBuilder.SetBasePath(environment.ContentRootPath);

        insertIndex = InsertJsonSourceIfMissing(configurationBuilder, insertIndex, BaseSettingsFile, optional: false, reloadOnChange: environment.IsDevelopment());
        insertIndex = InsertJsonSourceIfMissing(configurationBuilder, insertIndex, environmentSettingsFile, optional: true, reloadOnChange: environment.IsDevelopment());
        insertIndex = InsertJsonSourceIfMissing(configurationBuilder, insertIndex, DeploymentSettingsFile, optional: true, reloadOnChange: false);
        insertIndex = InsertJsonSourceIfMissing(configurationBuilder, insertIndex, ExternalSettingsFile, optional: true, reloadOnChange: false);
        insertIndex = InsertJsonSourceIfMissing(configurationBuilder, insertIndex, LocalSettingsFile, optional: true, reloadOnChange: environment.IsDevelopment());
        insertIndex = InsertJsonSourceIfMissing(configurationBuilder, insertIndex, EnvironmentProfilesFile, optional: true, reloadOnChange: environment.IsDevelopment());

        if (!string.IsNullOrWhiteSpace(contentRootParent))
        {
            var parentEnvironmentProfilesPath = Path.Combine(contentRootParent, EnvironmentProfilesFile);
            insertIndex = InsertJsonSourceIfMissing(configurationBuilder, insertIndex, parentEnvironmentProfilesPath, optional: true, reloadOnChange: false);
        }

        if (!string.IsNullOrWhiteSpace(tenantConfigPath))
        {
            insertIndex = InsertJsonSourceIfMissing(configurationBuilder, insertIndex, tenantConfigPath, optional: true, reloadOnChange: false);
        }

        if (!string.IsNullOrWhiteSpace(environmentProfilesPath))
        {
            insertIndex = InsertJsonSourceIfMissing(configurationBuilder, insertIndex, environmentProfilesPath, optional: true, reloadOnChange: false);
        }

        insertIndex = InsertEnvironmentVariablesSourceIfMissing(configurationBuilder, insertIndex, prefix);
        InsertEnvironmentVariablesSourceIfMissing(configurationBuilder, insertIndex, prefix: null);

        return configurationBuilder;
    }

    private static int InsertJsonSourceIfMissing(IConfigurationBuilder configurationBuilder, int insertIndex, string path, bool optional, bool reloadOnChange)
    {
        if (HasJsonSource(configurationBuilder, path))
        {
            return insertIndex;
        }

        var source = new JsonConfigurationSource
        {
            Path = path,
            Optional = optional,
            ReloadOnChange = reloadOnChange
        };
        source.EnsureDefaults(configurationBuilder);

        configurationBuilder.Sources.Insert(insertIndex, source);

        return insertIndex + 1;
    }

    private static int InsertEnvironmentVariablesSourceIfMissing(IConfigurationBuilder configurationBuilder, int insertIndex, string? prefix)
    {
        if (configurationBuilder.Sources.OfType<EnvironmentVariablesConfigurationSource>()
            .Any(source => string.Equals(source.Prefix ?? string.Empty, prefix ?? string.Empty, StringComparison.OrdinalIgnoreCase)))
        {
            return insertIndex;
        }

        configurationBuilder.Sources.Insert(insertIndex, new EnvironmentVariablesConfigurationSource
        {
            Prefix = prefix
        });

        return insertIndex + 1;
    }

    private static bool HasJsonSource(IConfigurationBuilder configurationBuilder, string path)
    {
        return configurationBuilder.Sources.OfType<JsonConfigurationSource>()
            .Any(source => string.Equals(NormalizePath(source.Path), NormalizePath(path), StringComparison.OrdinalIgnoreCase));
    }

    private static int GetInsertionIndex(IConfigurationBuilder configurationBuilder)
    {
        for (var index = 0; index < configurationBuilder.Sources.Count; index++)
        {
            var source = configurationBuilder.Sources[index];
            if (source is EnvironmentVariablesConfigurationSource || source.GetType().Name.Contains("CommandLineConfigurationSource", StringComparison.Ordinal))
            {
                return index;
            }
        }

        return configurationBuilder.Sources.Count;
    }

    private static string NormalizePath(string? path)
    {
        return (path ?? string.Empty)
            .Trim()
            .Replace('\\', '/');
    }
}
