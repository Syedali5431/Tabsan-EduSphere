using System.Text;

namespace Tabsan.EduSphere.Application.Services;

public static class StartupVisibilityReporter
{
    public static string DescribeDatabaseType(string? connectionString)
    {
        var value = connectionString?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unknown";
        }

        if (value.Contains("(localdb)", StringComparison.OrdinalIgnoreCase)
            || value.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || value.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            return "Local SQL Server";
        }

        if (value.Contains("Server=", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Trusted_Connection=", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Integrated Security=", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
        {
            return "SQL Server";
        }

        return "External database";
    }

    public static string DescribeConfigurationSources(params string?[] sources)
    {
        var builder = new StringBuilder();
        foreach (var source in sources)
        {
            var trimmed = source?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append("; ");
            }

            builder.Append(trimmed);
        }

        return builder.Length == 0 ? "Unavailable" : builder.ToString();
    }
}