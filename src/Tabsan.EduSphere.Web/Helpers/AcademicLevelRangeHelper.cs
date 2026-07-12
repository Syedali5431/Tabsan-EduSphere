using System.Globalization;

namespace Tabsan.EduSphere.Web.Helpers;

public static class AcademicLevelRangeHelper
{
    public static (int Min, int Max) ResolveUniversityLevelRange(IEnumerable<string?> configuredLevels, int totalSemesters)
    {
        var levels = configuredLevels
            .Where(level => !string.IsNullOrWhiteSpace(level))
            .Select(level => ExtractLevel(level!))
            .Where(level => level.HasValue)
            .Select(level => level!.Value)
            .Distinct()
            .OrderBy(level => level)
            .ToList();

        if (levels.Count == 0)
            return (1, 1);

        if (totalSemesters > 0)
        {
            var maxAllowed = levels.Where(level => level <= totalSemesters).ToList();
            if (maxAllowed.Count > 0)
                return (levels[0], maxAllowed[^1]);
        }

        return (levels[0], levels[^1]);
    }

    private static int? ExtractLevel(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
            ? parsed
            : null;
    }
}