using FluentAssertions;
using Tabsan.EduSphere.Web.Helpers;

namespace Tabsan.EduSphere.UnitTests;

public class AcademicLevelRangeHelperTests
{
    [Fact]
    public void ResolveUniversityLevelRange_UsesProgramTotalSemesters_WhenAvailable()
    {
        var result = AcademicLevelRangeHelper.ResolveUniversityLevelRange(
            configuredLevels: ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16"],
            totalSemesters: 6);

        result.Min.Should().Be(1);
        result.Max.Should().Be(6);
    }

    [Fact]
    public void ResolveUniversityLevelRange_FallsBackToConfiguredLevels_WhenProgramSemestersMissing()
    {
        var result = AcademicLevelRangeHelper.ResolveUniversityLevelRange(
            configuredLevels: ["1", "2", "3", "4", "5", "6", "7", "8"],
            totalSemesters: 0);

        result.Min.Should().Be(1);
        result.Max.Should().Be(8);
    }
}
