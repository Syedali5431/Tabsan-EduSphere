using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Tabsan.EduSphere.Application.Lms.Scorm;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lms;
using Tabsan.EduSphere.Web.Controllers;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>
/// Plan M13 — SCORM packages (zip checks, manifest reading for 1.2 and 2004), the run-time data model on the server (commit
/// values, mastery, time, the CMI a launch starts from), attempts, and the xAPI statement store (credentials, validation,
/// voiding, conflicts, queries).
/// </summary>
public class ScormTests
{
    // ── packages ───────────────────────────────────────────────────────────────────────────────

    internal static string Manifest12(string href = "index.html", string? parameters = null, string? mastery = null, string title = "Fire safety")
        => $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <manifest identifier="com.example.fire" version="1"
                      xmlns="http://www.imsproject.org/xsd/imscp_rootv1p1p2" xmlns:adlcp="http://www.adlnet.org/xsd/adlcp_rootv1p2">
              <metadata><schema>ADL SCORM</schema><schemaversion>1.2</schemaversion></metadata>
              <organizations default="org1">
                <organization identifier="org1"><title>{title}</title>
                  <item identifier="i1" identifierref="r1"{(parameters is null ? "" : $" parameters=\"{parameters}\"")}><title>Lesson</title>{(mastery is null ? "" : $"<adlcp:masteryscore>{mastery}</adlcp:masteryscore>")}</item>
                </organization>
              </organizations>
              <resources>
                <resource identifier="r1" type="webcontent" adlcp:scormtype="sco" href="{href}"><file href="{href}"/></resource>
              </resources>
            </manifest>
            """;

    internal static string Manifest2004(string href = "content/start.html", string? passing = "0.7")
        => $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <manifest identifier="com.example.math" version="1" xmlns="http://www.imsglobal.org/xsd/imscp_v1p1"
                      xmlns:adlcp="http://www.adlnet.org/xsd/adlcp_v1p3" xmlns:imsss="http://www.imsglobal.org/xsd/imsss">
              <metadata><schema>ADL SCORM</schema><schemaversion>2004 4th Edition</schemaversion></metadata>
              <organizations default="o">
                <organization identifier="o"><title>Fractions</title>
                  <item identifier="intro"><title>Intro</title>
                    <item identifier="a" identifierref="asset"><title>Notes</title></item>
                    <item identifier="s" identifierref="sco"><title>Quiz</title>
                      {(passing is null ? "" : $"<imsss:sequencing><imsss:objectives><imsss:primaryObjective satisfiedByMeasure=\"true\"><imsss:minNormalizedMeasure>{passing}</imsss:minNormalizedMeasure></imsss:primaryObjective></imsss:objectives></imsss:sequencing>")}
                    </item>
                  </item>
                </organization>
              </organizations>
              <resources>
                <resource identifier="asset" type="webcontent" adlcp:scormType="asset" href="notes.html"/>
                <resource identifier="sco" type="webcontent" adlcp:scormType="sco" xml:base="units/" href="{href}"/>
              </resources>
            </manifest>
            """;

    /// <summary>A zip with the given files (name → text).</summary>
    internal static MemoryStream Zip(params (string Name, string Content)[] files)
    {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            foreach (var (name, content) in files)
            {
                using var w = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
                w.Write(content);
            }
        stream.Position = 0;
        return stream;
    }

    private static ScormPackageContents Read(MemoryStream zip, ScormOptions? options = null)
    {
        using var archive = new ZipArchive(zip, ZipArchiveMode.Read);
        return ScormPackageReader.Read(archive, options ?? new ScormOptions());
    }

    [Fact]
    public void Scorm12Package_IsRead_WithTitleLaunchParametersAndMasteryScore()
    {
        var contents = Read(Zip(("imsmanifest.xml", Manifest12("index.html?lang=en", "?mode=lms", "75")), ("index.html", "<html></html>"), ("img/logo.png", "x")));
        Assert.Equal(ScormVersion.Scorm12, contents.Manifest.Version);
        Assert.Equal("Fire safety", contents.Manifest.Title);
        Assert.Equal("com.example.fire", contents.Manifest.Identifier);
        Assert.Equal("index.html?lang=en&mode=lms", contents.Manifest.LaunchPath);
        Assert.Equal(75m, contents.Manifest.MasteryScore);
        Assert.Equal(3, contents.Files.Count);
    }

    [Fact]
    public void Scorm2004Package_InAFolder_LaunchesTheFirstSco_WithXmlBase_AndPassingMeasure()
    {
        var contents = Read(Zip(("course/imsmanifest.xml", Manifest2004()), ("course/units/content/start.html", "<html></html>"), ("course/notes.html", "n")));
        Assert.Equal(ScormVersion.Scorm2004, contents.Manifest.Version);
        Assert.Equal("units/content/start.html", contents.Manifest.LaunchPath);
        Assert.Equal(70m, contents.Manifest.MasteryScore);
        Assert.Contains(contents.Files, f => f.Path == "units/content/start.html"); // the folder is the package root
        Assert.DoesNotContain(contents.Files, f => f.Path.StartsWith("course/"));
    }

    [Theory]
    [InlineData("../evil.html", "outside its folder")]
    [InlineData("/etc/passwd", "invalid file name")]
    [InlineData("C:/windows/x.dll", "invalid file name")]
    [InlineData("a/./b.html", "outside its folder")]
    public void UnsafePaths_AreRejected(string name, string message)
    {
        var ex = Assert.Throws<ScormPackageException>(() => Read(Zip(("imsmanifest.xml", Manifest12()), ("index.html", "x"), (name, "x"))));
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void BadPackages_AreExplained()
    {
        Assert.Contains("imsmanifest.xml is missing", Assert.Throws<ScormPackageException>(() => Read(Zip(("index.html", "x")))).Message);
        Assert.Contains("not in the package", Assert.Throws<ScormPackageException>(() => Read(Zip(("imsmanifest.xml", Manifest12("missing.html"))))).Message);
        Assert.Contains("another website", Assert.Throws<ScormPackageException>(() => Read(Zip(("imsmanifest.xml", Manifest12("https://cdn.example.com/sco.html"))))).Message);
        Assert.Contains("cannot be read", Assert.Throws<ScormPackageException>(() => Read(Zip(("imsmanifest.xml", "<manifest><unclosed>")))).Message);
        // External entities / DTDs are refused (XXE).
        var xxe = "<?xml version=\"1.0\"?><!DOCTYPE m [<!ENTITY x SYSTEM \"file:///c:/windows/win.ini\">]><manifest>&x;</manifest>";
        Assert.Throws<ScormPackageException>(() => Read(Zip(("imsmanifest.xml", xxe))));
        Assert.Contains("more than 2 files", Assert.Throws<ScormPackageException>(() =>
            Read(Zip(("imsmanifest.xml", Manifest12()), ("index.html", "x"), ("b.html", "x")), new ScormOptions { MaxFiles = 2 })).Message);
        Assert.Contains("twice", Assert.Throws<ScormPackageException>(() => Read(Zip(("imsmanifest.xml", Manifest12()), ("index.html", "x"), ("INDEX.html", "y")))).Message);
    }

    [Fact]
    public void ZipBombs_AreRejected()
    {
        // 30 MB of zeros compresses to a few KB: far beyond a believable ratio.
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var w = new StreamWriter(zip.CreateEntry("imsmanifest.xml").Open())) w.Write(Manifest12());
            using (var w = new StreamWriter(zip.CreateEntry("index.html").Open())) w.Write("x");
            using var big = zip.CreateEntry("video.bin", CompressionLevel.Optimal).Open();
            var zeros = new byte[1024 * 1024];
            for (var i = 0; i < 30; i++) big.Write(zeros);
        }
        stream.Position = 0;
        Assert.Contains("compressed suspiciously well", Assert.Throws<ScormPackageException>(() => Read(stream)).Message);

        // Too much data after extraction, even when it does not compress.
        var large = new MemoryStream();
        using (var zip = new ZipArchive(large, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var w = new StreamWriter(zip.CreateEntry("imsmanifest.xml").Open())) w.Write(Manifest12());
            using (var w = new StreamWriter(zip.CreateEntry("index.html").Open())) w.Write("x");
            using var noise = zip.CreateEntry("noise.bin").Open();
            noise.Write(System.Security.Cryptography.RandomNumberGenerator.GetBytes(2 * 1024 * 1024));
        }
        large.Position = 0;
        Assert.Contains("more than 1 MB", Assert.Throws<ScormPackageException>(() => Read(large, new ScormOptions { MaxUncompressedMb = 1 })).Message);
    }

    [Theory]
    [InlineData("index.html", "?a=1", "index.html?a=1")]
    [InlineData("index.html?x=2", "a=1", "index.html?x=2&a=1")]
    [InlineData("index.html#top", "?a=1", "index.html?a=1#top")]
    [InlineData("index.html", "#page3", "index.html#page3")]
    public void ItemParameters_AreJoinedAsTheSpecSays(string href, string parameters, string expected)
        => Assert.Equal(expected, ScormPackageReader.AppendParameters(href, parameters));

    [Fact]
    public void LaunchUrls_EncodeEachSegmentOnce()
    {
        Assert.Equal("my%20course/start%20here.html?x=1", ScormPlayController.LaunchUrl("my course/start here.html?x=1"));
        Assert.Equal("my%20course/a.html", ScormPlayController.LaunchUrl("my%20course/a.html"));
    }

    // ── run-time data ──────────────────────────────────────────────────────────────────────────

    private static JsonObject Cmi(string json) => ScormCmi.CmiOf(json)!;

    [Theory]
    [InlineData("passed", "completed", "passed")]
    [InlineData("failed", "completed", "failed")]
    [InlineData("completed", "completed", "unknown")]
    [InlineData("incomplete", "incomplete", "unknown")]
    [InlineData("browsed", "incomplete", "unknown")]
    [InlineData("not attempted", "not attempted", "unknown")]
    public void Scorm12Statuses_MapToCompletionAndSuccess(string lessonStatus, string completion, string success)
    {
        var body = JsonSerializer.Serialize(new { cmi = new { core = new { lesson_status = lessonStatus } } });
        var v = ScormCmi.Read(ScormVersion.Scorm12, Cmi(body), null);
        Assert.Equal((completion, success), (v.CompletionStatus, v.SuccessStatus));
    }

    [Fact]
    public void Scorm12Commit_ReadsScoreLocationSuspendDataAndTime_AndAppliesTheMasteryScore()
    {
        var body = """{"cmi":{"core":{"lesson_status":"completed","lesson_location":"page-7","exit":"suspend","session_time":"0001:02:03.50","score":{"raw":"72","min":"0","max":"80"}},"suspend_data":"s=1;p=7"}}""";
        var v = ScormCmi.Read(ScormVersion.Scorm12, Cmi(body), masteryScore: 80);
        Assert.Equal(("completed", "passed"), (v.CompletionStatus, v.SuccessStatus)); // 72/80 = 90% ≥ 80
        Assert.Equal((72m, 0m, 80m), (v.ScoreRaw!.Value, v.ScoreMin!.Value, v.ScoreMax!.Value));
        Assert.Equal("page-7", v.Location);
        Assert.Equal("s=1;p=7", v.SuspendData);
        Assert.Equal("suspend", v.Exit);
        Assert.Equal(3723, v.SessionSeconds);
        Assert.Equal(90m, ScormAttempt.Percent(v));
        Assert.Equal("failed", ScormCmi.Read(ScormVersion.Scorm12, Cmi(body), masteryScore: 95).SuccessStatus);
    }

    [Fact]
    public void Scorm2004Commit_UsesTheScaledScore_AndThePassingScore()
    {
        var body = """{"completion_status":"completed","success_status":"unknown","score":{"scaled":0.65,"raw":13,"max":20},"location":"q4","session_time":"PT1H30M5.5S"}""";
        var v = ScormCmi.Read(ScormVersion.Scorm2004, Cmi(body), masteryScore: 70);
        Assert.Equal(("completed", "failed"), (v.CompletionStatus, v.SuccessStatus));
        Assert.Equal(65m, ScormAttempt.Percent(v));
        Assert.Equal(5405, v.SessionSeconds);
        Assert.Equal("unknown", ScormCmi.Read(ScormVersion.Scorm2004, Cmi("""{"completion_status":"weird"}"""), null).CompletionStatus);
        Assert.Null(ScormCmi.CmiOf("not json"));
        Assert.Null(ScormCmi.CmiOf("""{"hello":1}"""));
    }

    [Theory]
    [InlineData("0000:00:30", 30)]
    [InlineData("12:5:7.25", 43507)]
    [InlineData("bad", 0)]
    public void Scorm12Times_AreParsed(string value, int seconds) => Assert.Equal(seconds, ScormCmi.Seconds12(value));

    [Theory]
    [InlineData("PT45S", 45)]
    [InlineData("P1DT1M", 86460)]
    [InlineData("PT0.5S", 0)]
    [InlineData("1 hour", 0)]
    public void Scorm2004Durations_AreParsed(string value, int seconds) => Assert.Equal(seconds, ScormCmi.Seconds2004(value));

    [Fact]
    public void Attempts_RecordCommits_FinishOnce_AndAddSessionTimeOnTheFinalCommit()
    {
        var now = new DateTime(2026, 10, 23, 9, 0, 0, DateTimeKind.Utc);
        var a = new ScormAttempt(Guid.NewGuid(), Guid.NewGuid(), 1, now);
        var progress = new ScormCommitValues("incomplete", "unknown", null, null, null, null, "p2", new string('x', 70_000), "suspend", 600);
        a.Apply(progress, """{"cmi":{}}""", finalCommit: false, now);
        Assert.False(a.IsFinished);
        Assert.Equal(0, a.TotalSeconds);
        Assert.Equal(ScormAttempt.MaxSuspendDataLength, a.SuspendData!.Length);

        a.Apply(progress with { CompletionStatus = "completed", SuccessStatus = "passed", ScoreScaled = 0.9m }, null, finalCommit: true, now.AddMinutes(10));
        Assert.True(a.IsFinished);
        Assert.Equal(now.AddMinutes(10), a.FinishedAt);
        Assert.Equal(600, a.TotalSeconds);
        Assert.Equal(90m, a.ScorePercent);
        Assert.Equal("""{"cmi":{}}""", a.CmiJson); // a commit without CMI keeps the last one

        a.Apply(progress with { CompletionStatus = "completed" }, null, finalCommit: false, now.AddMinutes(20));
        Assert.Equal(now.AddMinutes(10), a.FinishedAt); // the first finish counts
    }

    [Fact]
    public void Launches_StartFresh_ResumeWithTheStoredData_AndReviewWithoutCredit()
    {
        var a = new ScormAttempt(Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow);
        var fresh = JsonNode.Parse(ScormCmi.Initial(ScormVersion.Scorm12, a, "S-1", "Ali Khan", 80, review: false))!;
        Assert.Equal("ab-initio", fresh["core"]!["entry"]!.GetValue<string>());
        Assert.Equal("S-1", fresh["core"]!["student_id"]!.GetValue<string>());
        Assert.Equal("credit", fresh["core"]!["credit"]!.GetValue<string>());
        Assert.Equal("80", fresh["student_data"]!["mastery_score"]!.GetValue<string>());

        var stored = """{"cmi":{"core":{"lesson_status":"incomplete","lesson_location":"p3","exit":"suspend","session_time":"00:10:00","_children":"x"},"suspend_data":"abc","interactions":[{"id":"q1","_count":1}]}}""";
        a.Apply(ScormCmi.Read(ScormVersion.Scorm12, Cmi(stored), null), stored, finalCommit: true, DateTime.UtcNow);
        var resume = JsonNode.Parse(ScormCmi.Initial(ScormVersion.Scorm12, a, "S-1", "Ali Khan", null, review: false))!;
        Assert.Equal("resume", resume["core"]!["entry"]!.GetValue<string>());
        Assert.Equal("abc", resume["suspend_data"]!.GetValue<string>());
        Assert.Equal("p3", resume["core"]!["lesson_location"]!.GetValue<string>());
        Assert.Null(resume["core"]!["session_time"]);
        Assert.Null(resume["core"]!["exit"]);
        Assert.Null(resume["core"]!["_children"]);
        Assert.Null(resume["interactions"]![0]!["_count"]);
        Assert.Equal("0000:10:00.00", resume["core"]!["total_time"]!.GetValue<string>());

        var review = JsonNode.Parse(ScormCmi.Initial(ScormVersion.Scorm2004, a, "S-1", "Ali Khan", 70, review: true))!;
        Assert.Equal(("no-credit", "review"), (review["credit"]!.GetValue<string>(), review["mode"]!.GetValue<string>()));
        Assert.Equal("0.7", review["scaled_passing_score"]!.GetValue<string>());
        Assert.Equal("PT0H10M0S", review["total_time"]!.GetValue<string>());
    }

    [Fact]
    public void Packages_CountAttempts_AndKeepTheirSettingsInRange()
    {
        var p = new ScormPackage(Guid.NewGuid(), "  Safety  ", ScormVersion.Scorm12, "id", "index.html", 3, 1000, 120, Guid.NewGuid(), scoCount: 0);
        Assert.Equal("Safety", p.Title);
        Assert.Null(p.MasteryScore); // out of range
        Assert.Equal(1, p.ScoCount);
        Assert.True(p.AllowsAnotherAttempt(50)); // unlimited
        p.SetMaxAttempts(2);
        Assert.True(p.AllowsAnotherAttempt(1));
        Assert.False(p.AllowsAnotherAttempt(2));
        Assert.Throws<ArgumentException>(() => p.SetMaxAttempts(101));
        Assert.Throws<ArgumentException>(() => p.AttachAssignment(Guid.NewGuid(), 0));
        p.Remove();
        Assert.True(p.IsRemoved);
        Assert.False(p.IsPublished);
    }

    // ── packages in parts ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void MultiPartPackages_ListEveryLaunchableItem_InOrder_WithTheirOwnLaunchAndPassingScore()
    {
        // Like the Golf Explained "one file per SCO" sample: groups of items, quiz items with parameters and a mastery score.
        const string manifest = """
            <?xml version="1.0" encoding="UTF-8"?>
            <manifest identifier="golf" version="1" xmlns="http://www.imsproject.org/xsd/imscp_rootv1p1p2" xmlns:adlcp="http://www.adlnet.org/xsd/adlcp_rootv1p2">
              <organizations default="o"><organization identifier="o"><title>Golf</title>
                <item identifier="playing_item"><title>Playing the Game</title>
                  <item identifier="playing_par_item" identifierref="par"><title>Par</title></item>
                  <item identifier="playing_quiz_item" identifierref="quiz" parameters="?questions=Playing"><title>Playing Golf Quiz</title><adlcp:masteryscore>70</adlcp:masteryscore></item>
                </item>
                <item identifier="etiquette_item"><title>Etiquette</title>
                  <item identifier="etiquette_quiz_item" identifierref="quiz" parameters="?questions=Etiquette"><title></title></item>
                </item>
              </organization></organizations>
              <resources>
                <resource identifier="par" type="webcontent" adlcp:scormtype="sco" href="Playing/Par.html"/>
                <resource identifier="quiz" type="webcontent" adlcp:scormtype="sco" href="shared/assessmenttemplate.html"/>
              </resources>
            </manifest>
            """;
        var m = Read(Zip(("imsmanifest.xml", manifest), ("Playing/Par.html", "p"), ("shared/assessmenttemplate.html", "q"))).Manifest;
        Assert.Equal(3, m.Parts!.Count);
        Assert.Equal(["playing_par_item", "playing_quiz_item", "etiquette_quiz_item"], m.Parts.Select(p => p.Id));
        Assert.Equal(["Par", "Playing Golf Quiz", "Part 3"], m.Parts.Select(p => p.Title));
        Assert.Equal("shared/assessmenttemplate.html?questions=Etiquette", m.Parts[2].LaunchPath);
        Assert.Equal((null, 70m), (m.Parts[0].MasteryScore, m.Parts[1].MasteryScore));
        Assert.All(m.Parts, p => Assert.True(p.IsSco));
        Assert.Equal("Playing/Par.html", m.LaunchPath); // the first SCO, as before

        // SCORM 2004 with an asset: both are parts; the asset is not a SCO.
        var m2004 = Read(Zip(("imsmanifest.xml", Manifest2004()), ("units/content/start.html", "s"), ("notes.html", "n"))).Manifest;
        Assert.Equal([("a", false), ("s", true)], m2004.Parts!.Select(p => (p.Id, p.IsSco)));

        // Every part's file has to be in the package.
        Assert.Contains("not in the package", Assert.Throws<ScormPackageException>(() => Read(Zip(("imsmanifest.xml", manifest), ("Playing/Par.html", "p")))).Message);
    }

    [Fact]
    public void Packages_StoreTheirParts_OnlyWhenThereIsMoreThanOne()
    {
        var p = new ScormPackage(Guid.NewGuid(), "Golf", ScormVersion.Scorm12, "id", "a.html", 3, 1000, 80, Guid.NewGuid());
        Assert.False(p.IsMultiPart);
        var only = Assert.Single(p.Parts);
        Assert.Equal((ScormPackage.SinglePartId, "a.html", 80m), (only.Id, only.LaunchPath, only.MasteryScore));

        p.SetParts([new ScormPart("x", "X", "a.html", true, null)]);
        Assert.Null(p.PartsJson);
        p.SetParts([new ScormPart("x", "X", "a.html", true, null), new ScormPart("y", "Y", "b.html", false, null)]);
        Assert.True(p.IsMultiPart);
        Assert.Equal(["x", "y"], p.Parts.Select(x => x.Id));
        Assert.Throws<ArgumentException>(() => p.SetParts(Enumerable.Range(0, ScormPackage.MaxParts + 1).Select(i => new ScormPart($"p{i}", "P", "a.html", true, null)).ToList()));
    }

    [Fact]
    public void PartCommits_RollUp_IntoTheAttempt()
    {
        var parts = new List<ScormPart> { new("lesson", "Lesson", "l.html", true, null), new("notes", "Notes", "n.html", false, null), new("quiz", "Quiz", "q.html", true, 80) };
        var now = new DateTime(2026, 10, 25, 9, 0, 0, DateTimeKind.Utc);
        var a = new ScormAttempt(Guid.NewGuid(), Guid.NewGuid(), 1, now);
        var values = new ScormCommitValues("incomplete", "unknown", null, null, null, null, "p2", "s", "suspend", 300);

        a.ApplyPart(parts, "lesson", values, """{"cmi":{"suspend_data":"s"}}""", finalCommit: true, now);
        Assert.Equal(("incomplete", "unknown", 300), (a.CompletionStatus, a.SuccessStatus, a.TotalSeconds));
        Assert.Equal("""{"cmi":{"suspend_data":"s"}}""", a.PartStates()["lesson"].CmiJson);
        Assert.Null(a.CmiJson); // each part keeps its own data

        a.ApplyPart(parts, "lesson", values with { CompletionStatus = "completed", SuccessStatus = "passed", ScoreRaw = 90, SessionSeconds = 60 }, null, finalCommit: true, now);
        a.MarkPartViewed(parts, "notes", now);
        Assert.False(a.IsFinished);
        a.ApplyPart(parts, "quiz", values with { CompletionStatus = "completed", SuccessStatus = "failed", ScoreRaw = 70, SessionSeconds = 240 }, null, finalCommit: true, now.AddMinutes(5));
        Assert.Equal(("completed", "failed", 80m, 600), (a.CompletionStatus, a.SuccessStatus, a.ScorePercent!.Value, a.TotalSeconds));
        Assert.Equal(now.AddMinutes(5), a.FinishedAt);
        Assert.Equal(0.8m, a.ScoreScaled);

        // Passing the quiz later passes the attempt (every SCO passed; the asset only needs opening).
        a.ApplyPart(parts, "quiz", values with { CompletionStatus = "completed", SuccessStatus = "passed", ScoreRaw = 100, SessionSeconds = 0 }, null, finalCommit: false, now.AddMinutes(9));
        Assert.Equal(("passed", 95m), (a.SuccessStatus, a.ScorePercent!.Value));
        Assert.Equal(now.AddMinutes(5), a.FinishedAt); // the first finish counts

        // Pages only (like the Golf "one file per SCO" sample, whose resources are all assets): completed, nothing passed.
        var pages = new List<ScormPart> { new("p1", "One", "1.html", false, null), new("p2", "Two", "2.html", false, null) };
        var b = new ScormAttempt(Guid.NewGuid(), Guid.NewGuid(), 1, now);
        b.MarkPartViewed(pages, "p1", now);
        Assert.Equal(("incomplete", "unknown"), (b.CompletionStatus, b.SuccessStatus));
        b.MarkPartViewed(pages, "p2", now);
        Assert.Equal(("completed", "unknown", true), (b.CompletionStatus, b.SuccessStatus, b.IsFinished));
        Assert.Null(b.ScorePercent);
    }

    [Fact]
    public void APartsLaunch_StartsFromThatPartsOwnData()
    {
        var fresh = JsonNode.Parse(ScormCmi.Initial(ScormVersion.Scorm12, null, 0, null, 0, "S-1", "Ali Khan", 80, review: false))!;
        Assert.Equal("ab-initio", fresh["core"]!["entry"]!.GetValue<string>());
        Assert.Equal("80", fresh["student_data"]!["mastery_score"]!.GetValue<string>());

        var saved = """{"cmi":{"core":{"lesson_status":"incomplete","lesson_location":"p3"},"suspend_data":"abc"}}""";
        var resume = JsonNode.Parse(ScormCmi.Initial(ScormVersion.Scorm12, saved, 2, "suspend", 600, "S-1", "Ali Khan", null, review: false))!;
        Assert.Equal(("resume", "abc", "0000:10:00.00"), (resume["core"]!["entry"]!.GetValue<string>(), resume["suspend_data"]!.GetValue<string>(),
            resume["core"]!["total_time"]!.GetValue<string>()));
    }

    // ── xAPI ───────────────────────────────────────────────────────────────────────────────────

    private sealed class Repo : IScormRepository
    {
        public readonly List<XapiCredential> Credentials = [];
        public readonly List<XapiStatement> Statements = [];
        public Task<ScormPackage?> GetPackageAsync(Guid packageId, CancellationToken ct = default) => Task.FromResult<ScormPackage?>(null);
        public Task<IReadOnlyList<ScormPackage>> GetPackagesAsync(Guid offeringId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ScormPackage>>([]);
        public Task<IReadOnlyList<ScormAttempt>> GetAttemptsAsync(Guid packageId, Guid? studentProfileId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ScormAttempt>>([]);
        public Task<IReadOnlyList<ScormAttempt>> GetAttemptsForStudentAsync(IReadOnlyCollection<Guid> packageIds, Guid studentProfileId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ScormAttempt>>([]);
        public Task<ScormAttempt?> GetAttemptAsync(Guid attemptId, CancellationToken ct = default) => Task.FromResult<ScormAttempt?>(null);
        public Task<bool> IsEnrolledAsync(Guid offeringId, Guid studentProfileId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<ScormLearnerInfo?> GetLearnerAsync(Guid studentProfileId, CancellationToken ct = default) => Task.FromResult<ScormLearnerInfo?>(null);
        public Task<Guid?> GetStudentProfileIdAsync(Guid userId, CancellationToken ct = default) => Task.FromResult<Guid?>(null);
        public Task<IReadOnlyList<ScormLearnerInfo>> GetEnrolledLearnersAsync(Guid offeringId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ScormLearnerInfo>>([]);
        public Task<(Guid? TenantId, Guid? CampusId)?> GetOfferingScopeAsync(Guid offeringId, CancellationToken ct = default) => Task.FromResult<(Guid?, Guid?)?>(null);
        public Task<XapiCredential?> GetCredentialByKeyAsync(string key, CancellationToken ct = default) => Task.FromResult(Credentials.FirstOrDefault(c => c.Key == key));
        public Task<XapiCredential?> GetCredentialAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Credentials.FirstOrDefault(c => c.Id == id));
        public Task<IReadOnlyList<XapiCredential>> GetCredentialsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<XapiCredential>>(Credentials);
        public Task<XapiStatement?> GetStatementAsync(Guid statementId, CancellationToken ct = default) => Task.FromResult(Statements.FirstOrDefault(s => s.StatementId == statementId));
        public Task<IReadOnlyList<XapiStatement>> QueryStatementsAsync(XapiQuery q, CancellationToken ct = default)
        {
            var rows = Statements.Where(s => (q.IncludeVoided || !s.IsVoided) && (q.VerbId == null || s.VerbId == q.VerbId) && (q.ActorKey == null || s.ActorKey == q.ActorKey)
                                             && (q.CredentialId == null || s.CredentialId == q.CredentialId) && (q.Until == null || s.Stored <= q.Until));
            rows = q.Ascending ? rows.OrderBy(s => s.Stored) : rows.OrderByDescending(s => s.Stored);
            return Task.FromResult<IReadOnlyList<XapiStatement>>(rows.Take(q.Limit).ToList());
        }
        public void Add(ScormPackage package) { }
        public void Add(ScormAttempt attempt) { }
        public void Add(XapiCredential credential) => Credentials.Add(credential);
        public void Add(XapiStatement statement) => Statements.Add(statement);
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
    }

    private sealed class Audit : IAuditService
    {
        public readonly List<AuditLog> Logs = [];
        public Task LogAsync(AuditLog entry, CancellationToken ct = default) { Logs.Add(entry); return Task.CompletedTask; }
        public Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(string? query = null, Guid? actorUserId = null, string? action = null,
            string? entityName = null, DateTime? fromUtc = null, DateTime? toUtc = null, int page = 1, int pageSize = 50, string? actorRole = null,
            string? severity = null, string? eventCategory = null, string? correlationId = null, CancellationToken ct = default)
            => Task.FromResult<(IReadOnlyList<AuditLog>, int)>((Logs, Logs.Count));
    }

    private static string Basic(string key, string secret) => "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{key}:{secret}"));

    private static string Statement(Guid? id = null, string verb = "http://adlnet.gov/expapi/verbs/completed", string mbox = "mailto:Ali@Example.edu", double? scaled = 0.8)
        => JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["id"] = id?.ToString(),
            ["actor"] = new { mbox, name = "Ali" },
            ["verb"] = new { id = verb },
            ["object"] = new { id = "https://edusphere.example/activities/fire-safety" },
            ["result"] = scaled is null ? null : new { score = new { scaled }, success = true, completion = true }
        }.Where(p => p.Value is not null).ToDictionary(p => p.Key, p => p.Value));

    [Fact]
    public async Task XapiCredentials_AreShownOnce_StoredHashed_AndCheckedWithBasicAuth()
    {
        var repo = new Repo();
        var audit = new Audit();
        var service = new XapiService(repo, audit);
        var created = (await service.CreateCredentialAsync(new XapiCredentialRequest("Articulate", null), Guid.NewGuid())).Value!;
        Assert.StartsWith("xapi_", created.Credential.Key);
        Assert.DoesNotContain(created.Secret, repo.Credentials[0].SecretHash, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(64, repo.Credentials[0].SecretHash.Length);
        Assert.Single(audit.Logs);

        Assert.NotNull(await service.AuthenticateAsync(Basic(created.Credential.Key, created.Secret)));
        Assert.Null(await service.AuthenticateAsync(Basic(created.Credential.Key, created.Secret + "x")));
        Assert.Null(await service.AuthenticateAsync(Basic("xapi_unknown", created.Secret)));
        Assert.Null(await service.AuthenticateAsync("Bearer abc"));
        Assert.Null(await service.AuthenticateAsync("Basic !!!"));
        Assert.Equal(400, (await service.CreateCredentialAsync(new XapiCredentialRequest(" ", null), Guid.NewGuid())).Status);

        Assert.True(await service.DeactivateAsync(created.Credential.Id, Guid.NewGuid()));
        Assert.Null(await service.AuthenticateAsync(Basic(created.Credential.Key, created.Secret)));
    }

    [Fact]
    public async Task Statements_AreValidated_Stored_Voided_AndQueried()
    {
        var repo = new Repo();
        var now = new DateTime(2026, 10, 23, 10, 0, 0, DateTimeKind.Utc);
        var service = new XapiService(repo, new Audit(), () => now);
        var credential = new XapiCredential("Tool", "xapi_k", new string('0', 64), null, Guid.NewGuid());
        repo.Add(credential);

        var stored = await service.StoreAsync(credential, Statement(), null, "https://lms.example/xapi");
        Assert.True(stored.Ok);
        var id = stored.Value!.Single();
        var row = repo.Statements.Single();
        Assert.Equal("mailto:ali@example.edu", row.ActorKey); // e-mail addresses compare case-insensitively
        Assert.Equal(0.8m, row.ScoreScaled);
        var json = JsonNode.Parse(row.Json)!;
        Assert.Equal(id.ToString(), json["id"]!.GetValue<string>());
        Assert.Equal("xapi_k", json["authority"]!["account"]!["name"]!.GetValue<string>());
        Assert.NotNull(json["stored"]);

        // The same statement again is accepted; different content under the same id is a conflict.
        Assert.True((await service.StoreAsync(credential, Statement(id), null, "h")).Ok);
        Assert.Equal(409, (await service.StoreAsync(credential, Statement(id, scaled: 0.1), null, "h")).Status);
        Assert.Single(repo.Statements);

        // PUT: the path id must match.
        Assert.Equal(400, (await service.StoreAsync(credential, Statement(Guid.NewGuid()), Guid.NewGuid(), "h")).Status);

        // Invalid statements.
        Assert.Equal(400, (await service.StoreAsync(credential, Statement(mbox: "ali@example.edu"), null, "h")).Status);
        Assert.Equal(400, (await service.StoreAsync(credential, Statement(verb: "completed"), null, "h")).Status);
        Assert.Equal(400, (await service.StoreAsync(credential, "{\"actor\":{\"mbox\":\"mailto:a@b.c\",\"openid\":\"https://x\"},\"verb\":{\"id\":\"http://v\"},\"object\":{\"id\":\"http://o\"}}", null, "h")).Status);
        Assert.Equal(400, (await service.StoreAsync(credential, "[]", null, "h")).Status);
        Assert.Equal(400, (await service.StoreAsync(credential, "nonsense", null, "h")).Status);
        Assert.Equal(400, (await service.StoreAsync(credential, Statement().Replace("0.8", "1.5"), null, "h")).Status);

        // A batch, then voiding the first statement.
        var batch = "[" + Statement(verb: "http://adlnet.gov/expapi/verbs/attempted", scaled: null) + "," + Statement(verb: "http://adlnet.gov/expapi/verbs/attempted", scaled: null) + "]";
        Assert.Equal(2, (await service.StoreAsync(credential, batch, null, "h")).Value!.Count);
        var voiding = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["actor"] = new { mbox = "mailto:admin@example.edu" },
            ["verb"] = new { id = XapiService.VoidedVerb },
            ["object"] = new { objectType = "StatementRef", id = id.ToString() }
        });
        Assert.True((await service.StoreAsync(credential, voiding, null, "h")).Ok);
        Assert.True(repo.Statements.Single(s => s.StatementId == id).IsVoided);
        Assert.Null(await service.GetAsync(id, voided: false));
        Assert.NotNull(await service.GetAsync(id, voided: true));

        var (attempted, _) = await service.QueryAsync(new XapiQuery(null, "http://adlnet.gov/expapi/verbs/attempted", null, null, null, null, 10, false, null));
        Assert.Equal(2, attempted.Count);
        var (page, next) = await service.QueryAsync(new XapiQuery(null, null, null, null, null, null, 1, false, credential.Id));
        Assert.Single(page);
        Assert.NotNull(next); // more statements to read
        Assert.Equal("account:https://lms|ali", XapiService.ActorKey(JsonNode.Parse("""{"account":{"homePage":"https://lms","name":"ali"}}""")));
        Assert.Null(XapiService.ActorKey(JsonNode.Parse("""{"mbox_sha1sum":"zz"}""")));
    }
}
