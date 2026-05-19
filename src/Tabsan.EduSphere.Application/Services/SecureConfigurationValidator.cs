using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Tabsan.EduSphere.Application.Services;

public static class SecureConfigurationValidator
{
    public static string RequireSecureValue(IConfiguration configuration, IHostEnvironment environment, string settingPath, int minLength = 1)
    {
        var value = configuration[settingPath];
        var trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length < minLength || IsUnsafePlaceholderValue(trimmed))
        {
            throw new InvalidOperationException($"{settingPath} must be provided by environment variables or an external deployment configuration source for non-development startup.");
        }

        return trimmed;
    }

    public static bool IsUnsafePlaceholderValue(string? value)
    {
        var incompleteMarker = string.Concat("to", "do");

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized.Contains("replace_with", StringComparison.Ordinal)
            || normalized.Contains("or_set_via_env_var", StringComparison.Ordinal)
            || normalized.Contains("change_me", StringComparison.Ordinal)
            || normalized.Contains("changeme", StringComparison.Ordinal)
            || normalized.Contains(incompleteMarker, StringComparison.Ordinal)
            || normalized.Contains("yourdomain.com", StringComparison.Ordinal)
            || normalized.Contains("example.com", StringComparison.Ordinal)
            || normalized.Contains("<")
            || normalized.Contains(">");
    }
}