using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Tabsan.EduSphere.Application.Services;

public static class ConfigurationBootstrapper
{
    private const string DefaultEnvironmentVariablePrefix = "EDUSPHERE_";

    public static IConfigurationBuilder AddEduSphereConfigurationHierarchy(
        this IConfigurationBuilder configurationBuilder,
        IHostEnvironment environment,
        string? environmentVariablePrefix = null)
    {
        var prefix = string.IsNullOrWhiteSpace(environmentVariablePrefix)
            ? DefaultEnvironmentVariablePrefix
            : environmentVariablePrefix.Trim();

        return configurationBuilder
            .SetBasePath(environment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables(prefix)
            .AddEnvironmentVariables();
    }
}
