using Tabsan.EduSphere.Application.Assignments;
using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Domain.Assignments;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>Plan M3 — pure rules behind the grading workspace.</summary>
public class GradingWorkspaceTests
{
    // ── Annotation geometry ─────────────────────────────────────────────────

    [Theory]
    [InlineData("Highlight", "{\"x\":0.1,\"y\":0.2,\"w\":0.3,\"h\":0.05}", true)]
    [InlineData("Box", "{\"x\":0.8,\"y\":0.2,\"w\":0.3,\"h\":0.05}", false)]       // runs off the page
    [InlineData("Strikeout", "{\"x\":0.1,\"y\":0.2,\"w\":0,\"h\":0.05}", false)]   // zero width
    [InlineData("Pin", "{\"x\":0.5,\"y\":0.5}", true)]
    [InlineData("Pin", "{\"x\":1.5,\"y\":0.5}", false)]
    [InlineData("Pin", "{\"x\":\"a\",\"y\":0.5}", false)]
    [InlineData("Freehand", "{\"points\":[[0.1,0.1],[0.2,0.2]]}", true)]
    [InlineData("Freehand", "{\"points\":[[0.1,0.1]]}", false)]
    [InlineData("Freehand", "{\"points\":[[0.1,\"x\"],[0.2,0.2]]}", false)]
    [InlineData("Freehand", "{\"points\":[[0.1,0.1],[0.2,0.2]],\"width\":0.5}", false)]
    [InlineData("Highlight", "not json", false)]
    [InlineData("Sticker", "{}", false)]
    public void Geometry_IsValidatedPerType(string type, string geometry, bool valid)
    {
        var (_, json, _, error) = AnnotationGeometry.Validate(new SaveAnnotationRequest(1, type, geometry, "note", null));
        Assert.Equal(valid, error is null);
        Assert.Equal(valid, json is not null);
    }

    [Fact]
    public void Geometry_IsReserialised_NotEchoed()
    {
        var (_, json, color, error) = AnnotationGeometry.Validate(new SaveAnnotationRequest(2, "highlight",
            "{\"x\":0.1234567,\"y\":0.2,\"w\":0.3,\"h\":0.05,\"onclick\":\"alert(1)\"}", null, "#ABCDEF"));
        Assert.Null(error);
        Assert.Equal("{\"x\":0.12346,\"y\":0.2,\"w\":0.3,\"h\":0.05}", json);
        Assert.Equal("#abcdef", color);
    }

    [Theory]
    [InlineData(0, "#fff", "{\"x\":0.1,\"y\":0.1}", "Pin")]
    [InlineData(1, "red", "{\"x\":0.1,\"y\":0.1}", "Pin")]
    [InlineData(1, null, "{\"x\":0.1,\"y\":0.1}", "Text")] // text annotations need text (comment is null below)
    public void Geometry_RejectsBadPageColourOrMissingText(int page, string? color, string geometry, string type)
    {
        var (_, _, _, error) = AnnotationGeometry.Validate(new SaveAnnotationRequest(page, type, geometry, type == "Text" ? null : "c", color));
        Assert.NotNull(error);
    }

    // ── Submissions, attempts, posting ──────────────────────────────────────

    [Fact]
    public void Attempts_AreNumberedFromThePersistedCounter()
    {
        var submission = AssignmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid());
        var first = submission.AddAttempt("v1", null, DateTime.UtcNow, false);
        var second = submission.AddAttempt(null, "/uploads/x.pdf", DateTime.UtcNow, true);
        Assert.Equal(1, first.AttemptNumber);
        Assert.Equal(2, second.AttemptNumber);
        Assert.Equal(2, submission.CurrentAttemptNumber);
        Assert.True(submission.IsLate);
        Assert.Null(submission.TextContent);
        Assert.Equal("/uploads/x.pdf", submission.FileUrl);
        Assert.Equal("x.pdf", Assert.Single(second.Files).FileName);

        // A pre-M3 submission (attempt #1 implied) continues at 2.
        var legacy = new AssignmentSubmission(Guid.NewGuid(), Guid.NewGuid(), null, "old");
        Assert.Equal(2, legacy.AddAttempt("new", null, DateTime.UtcNow, false).AttemptNumber);
    }

    [Fact]
    public void Resubmitting_AGradedSubmission_NeedsGradingAgain_KeepingTheGrade()
    {
        var submission = AssignmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid());
        submission.AddAttempt("v1", null, DateTime.UtcNow, false);
        submission.Grade(7, "ok", Guid.NewGuid(), postNow: true);
        submission.AddAttempt("v2", null, DateTime.UtcNow, false);
        Assert.Equal(SubmissionStatus.Submitted, submission.Status);
        Assert.Equal(7, submission.MarksAwarded);
        Assert.True(submission.IsGradeVisibleToStudent);
    }

    [Fact]
    public void ManualPosting_HidesUntilPosted_AndHideWorks()
    {
        var submission = AssignmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid());
        submission.AddAttempt("v1", null, DateTime.UtcNow, false);
        Assert.False(submission.Post(DateTime.UtcNow)); // nothing to post yet
        submission.Grade(5, null, Guid.NewGuid(), postNow: false);
        Assert.False(submission.IsGradeVisibleToStudent);
        Assert.True(submission.Post(DateTime.UtcNow));
        Assert.True(submission.IsGradeVisibleToStudent);
        Assert.True(submission.Hide());
        Assert.False(submission.IsGradeVisibleToStudent);

        // The pre-M3 Grade overload posts at once (old behaviour).
        var old = new AssignmentSubmission(Guid.NewGuid(), Guid.NewGuid(), null, "t");
        old.Grade(3, null, Guid.NewGuid());
        Assert.True(old.IsGradeVisibleToStudent);
        old.Reject();
        Assert.False(old.IsGradeVisibleToStudent);
    }

    // ── Assignment settings ─────────────────────────────────────────────────

    [Fact]
    public void FileTypes_AreNormalised_AndUnsafeTypesRefused()
    {
        var assignment = new Assignment(Guid.NewGuid(), "A", null, DateTime.UtcNow.AddDays(1), 10);
        Assert.True(assignment.AcceptsFileType("x.docx")); // default list
        Assert.False(assignment.AcceptsFileType("x.html"));
        Assert.False(assignment.AcceptsFileType("x.svg"));

        assignment.ConfigureSubmissions(3, ["PDF", ".png", " docx "], 20);
        Assert.Equal(".pdf,.png,.docx", assignment.AllowedFileTypes);
        Assert.False(assignment.AcceptsFileType("x.xlsx"));
        Assert.True(assignment.AllowsAttempt(3));
        Assert.False(assignment.AllowsAttempt(4));

        Assert.Throws<ArgumentException>(() => assignment.ConfigureSubmissions(1, [".exe"], 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => assignment.ConfigureSubmissions(1, null, Assignment.MaxFileSizeLimitMb + 1));
        assignment.ConfigureSubmissions(0, null, 10);
        Assert.True(assignment.AllowsAttempt(500)); // 0 = unlimited
    }

    [Fact]
    public void Anonymity_RevealsOnce_AndModerationNeedsAModerator()
    {
        var assignment = new Assignment(Guid.NewGuid(), "A", null, DateTime.UtcNow.AddDays(1), 10);
        assignment.ConfigureGrading(true, GradePostingPolicy.Manual, false, null);
        Assert.True(assignment.NamesHidden);
        assignment.RevealNames(DateTime.UtcNow);
        Assert.False(assignment.NamesHidden);
        assignment.ConfigureGrading(false, GradePostingPolicy.Manual, false, null);
        assignment.ConfigureGrading(true, GradePostingPolicy.Manual, false, null); // turning it on again hides names again
        Assert.True(assignment.NamesHidden);
        Assert.Throws<ArgumentException>(() => assignment.ConfigureGrading(false, GradePostingPolicy.Automatic, true, null));
    }

    // ── Conversion state machine ────────────────────────────────────────────

    [Fact]
    public void Conversion_ClaimRetryFailRequeue()
    {
        var submission = AssignmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid());
        var attempt = submission.AddAttempt(null, null, DateTime.UtcNow, false);
        var docx = attempt.AddFile("Report.DOCX", "application/vnd", 10, "key", null);
        var pdf = attempt.AddFile("scan.pdf", "application/pdf", 10, "key2", null);
        var png = attempt.AddFile("photo.png", "image/png", 10, "key3", null);
        Assert.Equal(FileConversionStatus.Pending, docx.ConversionStatus);
        Assert.Equal("download", docx.ViewerKind);
        Assert.Equal("pdf", pdf.ViewerKind);
        Assert.Equal("image", png.ViewerKind);
        Assert.False(pdf.TryClaimForConversion(DateTime.UtcNow, TimeSpan.FromMinutes(10)));

        var now = DateTime.UtcNow;
        Assert.True(docx.TryClaimForConversion(now, TimeSpan.FromMinutes(10)));
        Assert.False(docx.TryClaimForConversion(now.AddMinutes(1), TimeSpan.FromMinutes(10)));  // fresh claim
        Assert.True(docx.TryClaimForConversion(now.AddMinutes(11), TimeSpan.FromMinutes(10))); // stale claim
        docx.MarkConversionFailed("boom", maxAttempts: 3);
        Assert.Equal(FileConversionStatus.Pending, docx.ConversionStatus); // 2 of 3
        Assert.True(docx.TryClaimForConversion(now.AddMinutes(12), TimeSpan.FromMinutes(10)));
        docx.MarkConversionFailed("boom", maxAttempts: 3);
        Assert.Equal(FileConversionStatus.Failed, docx.ConversionStatus);

        Assert.True(docx.RequeueConversion());
        Assert.Equal(FileConversionStatus.Pending, docx.ConversionStatus);
        Assert.True(docx.TryClaimForConversion(now.AddMinutes(13), TimeSpan.FromMinutes(10)));
        docx.MarkConverted("pdf-key", now);
        Assert.Equal("pdf", docx.ViewerKind);
        Assert.False(png.RequeueConversion());
    }

    [Fact]
    public void FileNames_AreCleaned()
    {
        var submission = AssignmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid());
        var attempt = submission.AddAttempt(null, "https://host/a/b/report%20v2.pdf?x=1", DateTime.UtcNow, false);
        Assert.Equal("report%20v2.pdf", attempt.Files.Single().FileName);
        var stored = attempt.AddFile(@"..\..\evil<>.pdf", "application/pdf", 1, "k", null);
        Assert.DoesNotContain("..", stored.FileName.Replace("evil", string.Empty).TrimStart('.'));
        Assert.DoesNotContain("<", stored.FileName);
    }

    // ── Gradebook ───────────────────────────────────────────────────────────

    [Fact]
    public void Gradebook_CombinesPostedGradesByPoints_ScaledToTheComponent()
    {
        var score = AssignmentGradebookCalculator.Calculate([(8m, 10m), (15m, 20m), (5m, 0m)], 30m);
        Assert.Equal((23m, 30m), score); // (8+15)/(10+20) of 30; zero-mark assignment ignored
        Assert.Null(AssignmentGradebookCalculator.Calculate([], 30m));
        Assert.Null(AssignmentGradebookCalculator.Calculate([(5m, 10m)], 0m));
        Assert.Equal((10m, 10m), AssignmentGradebookCalculator.Calculate([(12m, 10m)], 10m)); // clamped
        var rules = new[] { new ResultComponentRule("Final", 70m, 1, true), new ResultComponentRule("assignments", 30m, 2, true) };
        Assert.Equal("assignments", AssignmentGradebookCalculator.FindComponent(rules)!.Name);
    }

    // ── Rubric fix ──────────────────────────────────────────────────────────

    [Fact]
    public void Rubric_AttachesCriteriaAndLevels()
    {
        var rubric = Rubric.Create(Guid.NewGuid(), "R", Guid.NewGuid());
        var criterion = rubric.AddCriterion("Argument", 6, 1);
        criterion.AddLevel("Weak", 2, 1);
        criterion.AddLevel("Strong", 6, 2);
        Assert.Single(rubric.Criteria);
        Assert.Equal(2, rubric.Criteria[0].Levels.Count);
        Assert.Equal(rubric.Id, criterion.RubricId);
        Assert.Throws<ArgumentException>(() => criterion.AddLevel("Too much", 7, 3));
    }
}
