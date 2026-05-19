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
        var tenantConfigPath = Environment.GetEnvironmentVariable("EDUSPHERE_TENANT_CONFIG_PATH");

        var builder = configurationBuilder
            .SetBasePath(environment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Deployment.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.External.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

        if (!string.IsNullOrWhiteSpace(tenantConfigPath))
        {
            builder.AddJsonFile(tenantConfigPath.Trim(), optional: true, reloadOnChange: true);
        }

        return builder
            .AddEnvironmentVariables(prefix)
            .AddEnvironmentVariables();
    }
}
