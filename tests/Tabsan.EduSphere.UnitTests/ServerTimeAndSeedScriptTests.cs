using System.Text.RegularExpressions;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>
/// Guards for two faults found on 2026-11-10: portal scripts showed UTC server times as if they were local time, and the
/// student-journey demo script deleted assignments without their rubrics (rubrics have no foreign key to assignments).
/// </summary>
public class ServerTimeAndSeedScriptTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Tabsan.EduSphere.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([RepoRoot, .. parts]));

    // The API sends times as "2026-11-09T13:20:06" (UTC, no zone). A browser reads a zone-less time as local time, so
    // each script that shows one must add the "Z" first.
    [Theory]
    [InlineData("grading-workspace.js")]
    [InlineData("site.js")]
    [InlineData("pwa.js")]
    public void PortalScripts_ReadServerTimesWithoutAZone_AsUtc(string script)
    {
        var js = Read("src", "Tabsan.EduSphere.Web", "wwwroot", "js", script);

        Assert.Contains(@"/T\d{2}:\d{2}(:\d{2}(\.\d+)?)?$/", js, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"\+=?\s*'Z'"), js);
    }

    [Fact]
    public void GradingWorkspace_FormatsEveryTimeThroughTheUtcRule()
    {
        var js = Read("src", "Tabsan.EduSphere.Web", "wwwroot", "js", "grading-workspace.js");

        // Every server time (hand-ins, attempts, comments) goes through fmtDate, and fmtDate through parseServerTime.
        Assert.Contains("var d = parseServerTime(iso);", js, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(js, @"new Date\("));
    }

    [Fact]
    public void JourneyScript_CleansOnlyItsOwnAssignments_AndTheirRubricsFirst()
    {
        var sql = Read("Scripts", "student-journey-class1-10.sql");

        // It used to delete every assignment in every course the student was enrolled in, leaving their rubrics behind.
        Assert.DoesNotMatch(new Regex(@"DELETE FROM assignments WHERE CourseOfferingId IN \(SELECT[^;]*enrollments", RegexOptions.IgnoreCase), sql);
        Assert.Contains("WHERE se.Name LIKE 'IT-COL Class [0-9]%';", sql, StringComparison.Ordinal);

        var rubrics = sql.IndexOf("DELETE FROM rubrics WHERE", StringComparison.Ordinal);
        var criteria = sql.IndexOf("DELETE FROM rubric_criteria WHERE", StringComparison.Ordinal);
        var assignments = sql.IndexOf("DELETE FROM assignments WHERE Id IN (SELECT Id FROM @JourneyAssignments);", StringComparison.Ordinal);
        Assert.True(criteria > 0 && rubrics > criteria && assignments > rubrics, "rubric criteria, then rubrics, then assignments");

        // Hand-in children with NO ACTION foreign keys must go before the hand-ins themselves.
        var submissions = sql.IndexOf("DELETE FROM assignment_submissions WHERE Id IN", StringComparison.Ordinal);
        foreach (var child in new[] { "submission_attempts", "submission_files", "submission_comments", "submission_provisional_grades", "peer_reviews" })
        {
            var at = sql.IndexOf($"DELETE FROM {child} WHERE", StringComparison.Ordinal);
            Assert.True(at > 0 && at < submissions, $"{child} is deleted before assignment_submissions");
        }
    }
}
