using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PdfSharp.Pdf;
using Tabsan.EduSphere.Application.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Plan M3 — submission history, files, comments, annotations, the grading workspace, posting policy,
/// anonymous and moderated grading, rubric scoring, the conversion queue and the gradebook sync,
/// through the real API pipeline and database.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class GradingWorkspaceIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public GradingWorkspaceIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    // ═════════════════════════════════════════════════════════════════════════
    // Attempts, files, anonymous grading, manual posting, comments, annotations
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Lifecycle_Attempts_Anonymous_ManualPosting_Comments_Annotations()
    {
        var seed = await SeedAsync(students: 3, enrolled: 2);
        var faculty = StaffClient("Faculty");
        var assignmentId = await CreateAssignmentAsync(faculty, seed.OfferingId, maxMarks: 20);

        var settings = await faculty.PutAsJsonAsync($"api/v1/assignment/{assignmentId}/settings", new
        {
            allowedAttempts = 2, allowedFileTypes = new[] { "pdf", ".docx", ".png" }, maxFileSizeMb = 5,
            anonymousGrading = true, postingPolicy = "Manual", moderatedGrading = false
        });
        Assert.Equal(HttpStatusCode.OK, settings.StatusCode);
        var unsafeType = await faculty.PutAsJsonAsync($"api/v1/assignment/{assignmentId}/settings", new
        {
            allowedAttempts = 1, allowedFileTypes = new[] { ".html" }, maxFileSizeMb = 5, anonymousGrading = false, postingPolicy = "Automatic", moderatedGrading = false
        });
        Assert.Equal(HttpStatusCode.BadRequest, unsafeType.StatusCode);

        var a = StudentClient(seed.Students[0]);
        var b = StudentClient(seed.Students[1]);
        var outsider = StudentClient(seed.Students[2]);

        // Not enrolled → refused. Wrong type → refused.
        Assert.Equal(HttpStatusCode.Forbidden, (await SubmitAsync(outsider, assignmentId, "hi")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SubmitAsync(a, assignmentId, null, ("run.exe", new byte[] { 1, 2 }))).StatusCode);

        // Attempt 1: text + PDF + DOCX.
        var pdf = MinimalPdf();
        var first = await SubmitAsync(a, assignmentId, "My essay", ("essay.pdf", pdf), ("notes.docx", new byte[] { 80, 75, 3, 4, 1, 2, 3 }));
        var firstBody = await first.Content.ReadAsStringAsync();
        Assert.True(first.IsSuccessStatusCode, firstBody);
        var detail = JsonDocument.Parse(firstBody).RootElement;
        var submissionA = detail.GetProperty("submissionId").GetGuid();
        Assert.Equal(1, detail.GetProperty("currentAttemptNumber").GetInt32());
        var files = detail.GetProperty("attempts")[0].GetProperty("files").EnumerateArray().ToList();
        Assert.Equal(2, files.Count);
        var pdfFile = files.Single(f => f.GetProperty("fileName").GetString() == "essay.pdf");
        var docxFile = files.Single(f => f.GetProperty("fileName").GetString() == "notes.docx");
        Assert.Equal("pdf", pdfFile.GetProperty("viewerKind").GetString());
        Assert.Equal("Pending", docxFile.GetProperty("conversionStatus").GetString());
        Assert.True(detail.GetProperty("canResubmit").GetBoolean());

        // Attempt 2 is allowed; attempt 3 is not. Every attempt is kept.
        Assert.True((await SubmitAsync(a, assignmentId, "Second try", ("essay-v2.pdf", pdf))).IsSuccessStatusCode);
        var third = await SubmitAsync(a, assignmentId, "Third");
        Assert.Equal(HttpStatusCode.Conflict, third.StatusCode);
        Assert.Contains("all 2 attempts", await third.Content.ReadAsStringAsync());
        var afterTwo = await a.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{submissionA}");
        Assert.Equal(2, afterTwo.GetProperty("attempts").GetArrayLength());
        Assert.False(afterTwo.GetProperty("canResubmit").GetBoolean());

        var bSubmit = await SubmitAsync(b, assignmentId, "B's answer");
        var submissionB = JsonDocument.Parse(await bSubmit.Content.ReadAsStringAsync()).RootElement.GetProperty("submissionId").GetGuid();

        // Students cannot reach each other's work or the workspace.
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"api/v1/assignment/submissions/{submissionA}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await b.GetAsync($"api/v1/assignment/{assignmentId}/workspace")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"api/v1/assignment/submission-files/{pdfFile.GetProperty("fileId").GetGuid()}")).StatusCode);

        // Anonymous workspace: labels only, no ids or names.
        var workspace = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/assignment/{assignmentId}/workspace");
        var items = workspace.GetProperty("submissions").EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        Assert.All(items, i => Assert.StartsWith("Student ", i.GetProperty("studentLabel").GetString()));
        Assert.All(items, i => Assert.True(IsMissing(i, "studentProfileId")));
        var facultyView = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{submissionA}");
        Assert.True(IsMissing(facultyView, "studentProfileId"));
        Assert.DoesNotContain(seed.Names[0], facultyView.GetRawText());

        // Files: original download works; the DOCX rendition is not ready.
        var pdfId = pdfFile.GetProperty("fileId").GetGuid();
        var downloaded = await faculty.GetAsync($"api/v1/assignment/submission-files/{pdfId}");
        Assert.Equal(pdf, await downloaded.Content.ReadAsByteArrayAsync());
        Assert.Equal("nosniff", downloaded.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(HttpStatusCode.Conflict, (await faculty.GetAsync($"api/v1/assignment/submission-files/{docxFile.GetProperty("fileId").GetGuid()}?pdf=true")).StatusCode);

        // Annotations (bad geometry refused) and comments (public + grader-only).
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PostAsJsonAsync($"api/v1/assignment/submission-files/{pdfId}/annotations",
            new { pageNumber = 1, type = "Highlight", geometryJson = "{\"x\":0.9,\"y\":0.1,\"w\":0.5,\"h\":0.1}" })).StatusCode);
        var highlight = await faculty.PostAsJsonAsync($"api/v1/assignment/submission-files/{pdfId}/annotations",
            new { pageNumber = 1, type = "Highlight", geometryJson = "{\"x\":0.1,\"y\":0.1,\"w\":0.5,\"h\":0.05}", comment = "Good thesis", color = "#FFCC00" });
        Assert.True(highlight.IsSuccessStatusCode, await highlight.Content.ReadAsStringAsync());
        Assert.True((await faculty.PostAsJsonAsync($"api/v1/assignment/submission-files/{pdfId}/annotations",
            new { pageNumber = 1, type = "Freehand", geometryJson = "{\"points\":[[0.2,0.5],[0.3,0.55],[0.4,0.5]]}" })).IsSuccessStatusCode);
        Assert.True((await CommentAsync(faculty, submissionA, "Well argued", isPrivate: false)).IsSuccessStatusCode);
        Assert.True((await CommentAsync(faculty, submissionA, "Check for plagiarism", isPrivate: true)).IsSuccessStatusCode);
        Assert.True((await CommentAsync(a, submissionA, "Thanks!", isPrivate: true)).IsSuccessStatusCode); // students cannot make private comments

        // Grade under manual posting: saved, not visible to the student.
        var grade = await faculty.PutAsJsonAsync($"api/v1/assignment/submissions/{submissionA}/grade", new { marksAwarded = 15, feedback = "Strong work" });
        var gradeBody = await grade.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("saved", gradeBody.GetProperty("outcome").GetString());
        Assert.False(gradeBody.GetProperty("isPosted").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PutAsJsonAsync($"api/v1/assignment/submissions/{submissionA}/grade", new { marksAwarded = 25 })).StatusCode);

        var hidden = await a.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{submissionA}");
        Assert.True(IsMissing(hidden, "marksAwarded"));
        Assert.True(IsMissing(hidden, "feedback"));
        var hiddenComments = hidden.GetProperty("comments").EnumerateArray().ToList();
        Assert.Single(hiddenComments); // only the student's own comment (shown even though "private" was asked)
        Assert.False(hiddenComments[0].GetProperty("isPrivateToGraders").GetBoolean());
        Assert.Empty((await a.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submission-files/{pdfId}/annotations")).EnumerateArray());
        Assert.Equal(HttpStatusCode.Conflict, (await a.GetAsync($"api/v1/assignment/submission-files/{pdfId}/annotated")).StatusCode);
        var mySubmissions = await a.GetFromJsonAsync<JsonElement>("api/v1/assignment/my-submissions");
        Assert.True(IsMissing(mySubmissions.EnumerateArray().Single(s => s.GetProperty("assignmentId").GetGuid() == assignmentId), "marksAwarded"));

        // Posting while B is ungraded keeps names hidden.
        var post = await faculty.PostAsJsonAsync($"api/v1/assignment/{assignmentId}/post-grades", new { submissionIds = (Guid[]?)null });
        var postBody = await post.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, postBody.GetProperty("posted").GetInt32());
        Assert.False(postBody.GetProperty("namesRevealed").GetBoolean());

        // The student now sees marks, feedback, the public teacher comment (not the private one) and annotations.
        var visible = await a.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{submissionA}");
        Assert.Equal(15m, visible.GetProperty("marksAwarded").GetDecimal());
        Assert.Equal("Strong work", visible.GetProperty("feedback").GetString());
        var visibleComments = visible.GetProperty("comments").EnumerateArray().Select(c => c.GetProperty("body").GetString()).ToList();
        Assert.Contains("Well argued", visibleComments);
        Assert.DoesNotContain("Check for plagiarism", visibleComments);
        Assert.Equal(2, (await a.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submission-files/{pdfId}/annotations")).GetArrayLength());
        var annotated = await a.GetAsync($"api/v1/assignment/submission-files/{pdfId}/annotated");
        Assert.True(annotated.IsSuccessStatusCode, await annotated.Content.ReadAsStringAsync());
        var annotatedBytes = await annotated.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(annotatedBytes, 0, 4));
        Assert.True(annotatedBytes.Length > pdf.Length);

        // Media comment from B; staff can play it; only the author deletes a comment.
        var media = await CommentAsync(b, submissionB, null, false, ("voice.webm", new byte[] { 26, 69, 223, 163, 1, 2, 3 }));
        Assert.True(media.IsSuccessStatusCode, await media.Content.ReadAsStringAsync());
        var mediaComment = (await media.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("commentId").GetGuid();
        Assert.Equal(7, (await (await faculty.GetAsync($"api/v1/assignment/submission-comments/{mediaComment}/media")).Content.ReadAsByteArrayAsync()).Length);
        Assert.Equal(HttpStatusCode.Forbidden, (await faculty.DeleteAsync($"api/v1/assignment/submission-comments/{mediaComment}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await b.DeleteAsync($"api/v1/assignment/submission-comments/{mediaComment}")).StatusCode);

        // Grading and posting B lifts anonymity.
        await faculty.PutAsJsonAsync($"api/v1/assignment/submissions/{submissionB}/grade", new { marksAwarded = 12 });
        var postAll = await (await faculty.PostAsJsonAsync($"api/v1/assignment/{assignmentId}/post-grades", new { })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(postAll.GetProperty("namesRevealed").GetBoolean());
        var named = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/assignment/{assignmentId}/workspace");
        Assert.Contains(named.GetProperty("submissions").EnumerateArray(), i => i.GetProperty("studentLabel").GetString() == seed.Names[0]);

        // Hide again: the student loses sight of the grade.
        await faculty.PostAsJsonAsync($"api/v1/assignment/{assignmentId}/hide-grades", new { submissionIds = new[] { submissionA } });
        Assert.True(IsMissing(await a.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{submissionA}"), "marksAwarded"));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Moderated grading
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Moderated_ProvisionalGrades_ModeratorChoosesFinal()
    {
        var seed = await SeedAsync(students: 1, enrolled: 1);
        var moderatorId = Guid.NewGuid();
        var moderator = StaffClient("Faculty", moderatorId);
        var graderOne = StaffClient("Faculty");
        var graderTwo = StaffClient("Faculty");
        var assignmentId = await CreateAssignmentAsync(moderator, seed.OfferingId, maxMarks: 10);
        Assert.Equal(HttpStatusCode.OK, (await moderator.PutAsJsonAsync($"api/v1/assignment/{assignmentId}/settings", new
        {
            allowedAttempts = 1, allowedFileTypes = (string[]?)null, maxFileSizeMb = 10, anonymousGrading = false, postingPolicy = "Automatic", moderatedGrading = true
        })).StatusCode);

        var student = StudentClient(seed.Students[0]);
        var submitted = await SubmitAsync(student, assignmentId, "Answer");
        var submissionId = (await submitted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("submissionId").GetGuid();

        var p1 = await (await graderOne.PutAsJsonAsync($"api/v1/assignment/submissions/{submissionId}/grade", new { marksAwarded = 6, feedback = "ok" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(p1.GetProperty("provisional").GetBoolean());
        await graderTwo.PutAsJsonAsync($"api/v1/assignment/submissions/{submissionId}/grade", new { marksAwarded = 8, feedback = "good" });
        await graderTwo.PutAsJsonAsync($"api/v1/assignment/submissions/{submissionId}/grade", new { marksAwarded = 9, feedback = "very good" }); // updates, not duplicates

        // The student sees nothing yet; a grader sees only their own provisional grade; graders cannot post.
        Assert.True(IsMissing(await student.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{submissionId}"), "marksAwarded"));
        Assert.Equal(1, (await graderOne.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{submissionId}")).GetProperty("provisionalGrades").GetArrayLength());
        Assert.Equal(HttpStatusCode.Forbidden, (await graderOne.PostAsJsonAsync($"api/v1/assignment/{assignmentId}/post-grades", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await graderOne.PostAsJsonAsync($"api/v1/assignment/submissions/{submissionId}/moderate", new { marksAwarded = 10 })).StatusCode);

        var moderatorView = await moderator.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{submissionId}");
        Assert.True(moderatorView.GetProperty("canModerate").GetBoolean());
        var provisional = moderatorView.GetProperty("provisionalGrades").EnumerateArray().ToList();
        Assert.Equal(2, provisional.Count);
        var nine = provisional.Single(p => p.GetProperty("marks").GetDecimal() == 9m).GetProperty("provisionalGradeId").GetGuid();

        var moderated = await moderator.PostAsJsonAsync($"api/v1/assignment/submissions/{submissionId}/moderate", new { provisionalGradeId = nine });
        Assert.True(moderated.IsSuccessStatusCode, await moderated.Content.ReadAsStringAsync());
        var final = await student.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{submissionId}");
        Assert.Equal(9m, final.GetProperty("marksAwarded").GetDecimal()); // automatic posting
        Assert.Equal("very good", final.GetProperty("feedback").GetString());
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Rubric, legacy endpoints, gradebook sync
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Rubric_LegacyEndpoints_AndGradebookSync()
    {
        var seed = await SeedAsync(students: 1, enrolled: 1);
        var faculty = StaffClient("Faculty");
        var assignmentId = await CreateAssignmentAsync(faculty, seed.OfferingId, maxMarks: 20);
        await faculty.PutAsJsonAsync($"api/v1/assignment/{assignmentId}/settings", new
        {
            allowedAttempts = 1, allowedFileTypes = (string[]?)null, maxFileSizeMb = 10, anonymousGrading = false, postingPolicy = "Manual", moderatedGrading = false
        });

        var rubric = await faculty.PostAsJsonAsync("api/v1/rubric", new
        {
            assignmentId, title = "Essay rubric",
            criteria = new[]
            {
                new { name = "Argument", maxPoints = 6m, displayOrder = 1, levels = new[] { new { label = "Weak", pointsAwarded = 2m, displayOrder = 1 }, new { label = "Strong", pointsAwarded = 6m, displayOrder = 2 } } },
                new { name = "Style", maxPoints = 4m, displayOrder = 2, levels = new[] { new { label = "Poor", pointsAwarded = 1m, displayOrder = 1 }, new { label = "Clear", pointsAwarded = 4m, displayOrder = 2 } } }
            }
        });
        Assert.True(rubric.IsSuccessStatusCode, await rubric.Content.ReadAsStringAsync());

        // Legacy JSON submit still works and becomes attempt 1.
        var student = StudentClient(seed.Students[0]);
        var legacy = await student.PostAsJsonAsync("api/v1/assignment/submit", new { assignmentId, fileUrl = "/uploads/assignment-submissions/old.pdf", textContent = (string?)null });
        Assert.True(legacy.IsSuccessStatusCode, await legacy.Content.ReadAsStringAsync());
        var submissionId = (await legacy.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("submissionId").GetGuid();
        var detail = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{submissionId}");
        var legacyFile = detail.GetProperty("attempts")[0].GetProperty("files")[0];
        Assert.True(legacyFile.GetProperty("isLegacy").GetBoolean());
        Assert.Equal("old.pdf", legacyFile.GetProperty("fileName").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await student.PostAsJsonAsync("api/v1/assignment/submit", new { assignmentId, textContent = "again" })).StatusCode);

        // Rubric: levels must belong to their criterion; the total (6+1 of 10) is scaled to 20 marks = 14.
        var criteria = detail.GetProperty("rubric").GetProperty("criteria").EnumerateArray().ToList();
        var argument = criteria.Single(c => c.GetProperty("name").GetString() == "Argument");
        var style = criteria.Single(c => c.GetProperty("name").GetString() == "Style");
        Guid Level(JsonElement c, string label) => c.GetProperty("levels").EnumerateArray().Single(l => l.GetProperty("label").GetString() == label).GetProperty("levelId").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PostAsJsonAsync($"api/v1/assignment/submissions/{submissionId}/rubric", new
        {
            grades = new[] { new { criterionId = argument.GetProperty("criterionId").GetGuid(), levelId = Level(style, "Clear") } }, applyTotalToMarks = true
        })).StatusCode);

        var restore = await UseCollegeComponentsAsync(("Assignments", 30m), ("Final", 70m));
        try
        {
            var scored = await faculty.PostAsJsonAsync($"api/v1/assignment/submissions/{submissionId}/rubric", new
            {
                grades = new[]
                {
                    new { criterionId = argument.GetProperty("criterionId").GetGuid(), levelId = Level(argument, "Strong") },
                    new { criterionId = style.GetProperty("criterionId").GetGuid(), levelId = Level(style, "Poor") }
                },
                applyTotalToMarks = true
            });
            var scoredBody = await scored.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(7m, scoredBody.GetProperty("rubric").GetProperty("totalPoints").GetDecimal());
            Assert.Equal(14m, scoredBody.GetProperty("marksAwarded").GetDecimal());

            // Manual posting: nothing in the gradebook yet; the legacy grade endpoint follows the same policy.
            Assert.Null(await ResultAsync(seed.Students[0].ProfileId, seed.OfferingId, "Assignments"));
            Assert.Equal(HttpStatusCode.NoContent, (await faculty.PutAsJsonAsync("api/v1/assignment/submissions/grade", new
            {
                assignmentId, studentProfileId = seed.Students[0].ProfileId, marksAwarded = 16m, feedback = "Revised"
            })).StatusCode);
            Assert.True(IsMissing(await student.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{submissionId}"), "marksAwarded"));

            await faculty.PostAsJsonAsync($"api/v1/assignment/{assignmentId}/post-grades", new { });
            var result = await ResultAsync(seed.Students[0].ProfileId, seed.OfferingId, "Assignments");
            Assert.NotNull(result);
            Assert.Equal(24m, result!.Value.Marks); // 16/20 of a 30-point component
            Assert.Equal(30m, result.Value.Max);
            Assert.Equal(ResultSource.AssignmentSync, result.Value.Source);
        }
        finally { await restore(); }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Conversion queue (real repository and storage, fake converter)
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConversionQueue_ConvertsRetriesAndRequeues()
    {
        var seed = await SeedAsync(students: 1, enrolled: 1);
        var faculty = StaffClient("Faculty");
        var assignmentId = await CreateAssignmentAsync(faculty, seed.OfferingId, maxMarks: 10);
        var student = StudentClient(seed.Students[0]);
        var submitted = await SubmitAsync(student, assignmentId, null, ("report.docx", new byte[] { 80, 75, 3, 4, 9 }), ("data.xlsx", new byte[] { 80, 75, 3, 4, 8 }));
        var body = await submitted.Content.ReadFromJsonAsync<JsonElement>();
        var files = body.GetProperty("attempts")[0].GetProperty("files").EnumerateArray().ToDictionary(f => f.GetProperty("fileName").GetString()!, f => f.GetProperty("fileId").GetGuid());

        var converter = new FakeConverter(name => name.EndsWith(".xlsx") ? throw new InvalidOperationException("boom") : MinimalPdf());
        for (var round = 0; round < 3; round++)
        {
            using var scope = _factory.Services.CreateScope();
            var processor = new SubmissionConversionProcessor(scope.ServiceProvider.GetRequiredService<ISubmissionWorkspaceRepository>(),
                scope.ServiceProvider.GetRequiredService<IMediaStorageService>(), converter);
            await processor.ProcessBatchAsync(50);
        }

        var after = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{body.GetProperty("submissionId").GetGuid()}");
        var states = after.GetProperty("attempts")[0].GetProperty("files").EnumerateArray().ToDictionary(f => f.GetProperty("fileName").GetString()!);
        Assert.Equal("Ready", states["report.docx"].GetProperty("conversionStatus").GetString());
        Assert.Equal("pdf", states["report.docx"].GetProperty("viewerKind").GetString());
        Assert.Equal("Failed", states["data.xlsx"].GetProperty("conversionStatus").GetString());
        Assert.Contains("boom", states["data.xlsx"].GetProperty("conversionError").GetString());

        var rendition = await faculty.GetAsync($"api/v1/assignment/submission-files/{files["report.docx"]}?pdf=true");
        Assert.Equal("application/pdf", rendition.Content.Headers.ContentType!.MediaType);
        Assert.Equal(HttpStatusCode.NoContent, (await faculty.PostAsync($"api/v1/assignment/submission-files/{files["data.xlsx"]}/reconvert", null)).StatusCode);
        var requeued = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{body.GetProperty("submissionId").GetGuid()}");
        Assert.Equal("Pending", requeued.GetProperty("attempts")[0].GetProperty("files").EnumerateArray()
            .Single(f => f.GetProperty("fileName").GetString() == "data.xlsx").GetProperty("conversionStatus").GetString());
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═════════════════════════════════════════════════════════════════════════

    private sealed class FakeConverter(Func<string, byte[]> convert) : IDocumentConverter
    {
        public bool IsEnabled => true;
        public Task<byte[]> ConvertToPdfAsync(byte[] content, string fileName, CancellationToken ct = default) => Task.FromResult(convert(fileName));
    }

    private sealed record SeedStudent(Guid ProfileId, Guid UserId);

    private sealed record Seed(Guid OfferingId, List<SeedStudent> Students, List<string> Names);

    private static byte[] MinimalPdf()
    {
        using var document = new PdfDocument();
        document.AddPage();
        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static bool IsMissing(JsonElement e, string name)
        => !e.TryGetProperty(name, out var v) || v.ValueKind == JsonValueKind.Null;

    private HttpClient StaffClient(string role, Guid? userId = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            JwtTestHelper.GenerateToken(role, (userId ?? Guid.NewGuid()).ToString()));
        return client;
    }

    private HttpClient StudentClient(SeedStudent student)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            JwtTestHelper.GenerateToken("Student", student.UserId.ToString(), studentProfileId: student.ProfileId.ToString()));
        return client;
    }

    /// <summary>A College offering with <paramref name="students"/> students, the first <paramref name="enrolled"/> enrolled.</summary>
    private async Task<Seed> SeedAsync(int students, int enrolled)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var studentRole = db.Roles.First(r => r.Name == "Student");

        var department = new Department($"M3 Dept {suffix}", $"M3D{suffix}", InstitutionType.College);
        var program = new AcademicProgram($"M3 Program {suffix}", $"M3P{suffix}", department.Id, 4);
        var semester = new Semester($"M3 Sem {suffix}", DateTime.UtcNow.Date.AddDays(-10), DateTime.UtcNow.Date.AddDays(80));
        var course = new Course($"M3 Course {suffix}", $"M3C{suffix}", 3, department.Id);
        var offering = new CourseOffering(course.Id, semester.Id, 40);
        offering.SetInstitutionScope(InstitutionType.College, null, null);
        db.AddRange(department, program, semester, course, offering);

        var seeded = new List<SeedStudent>();
        var names = new List<string>();
        for (var i = 0; i < students; i++)
        {
            var name = $"M3 Student {i} {suffix}";
            var user = new User($"m3_{i}_{suffix}", "integration-hash", studentRole.Id, $"m3_{i}_{suffix}@tabsan.local");
            user.UpdateFullName(name);
            var profile = new StudentProfile(user.Id, $"M3-{i}-{suffix}", program.Id, department.Id, DateTime.UtcNow.Date);
            db.AddRange(user, profile);
            if (i < enrolled) db.Add(new Enrollment(profile.Id, offering.Id));
            seeded.Add(new SeedStudent(profile.Id, user.Id));
            names.Add(name);
        }
        await db.SaveChangesAsync();
        return new Seed(offering.Id, seeded, names);
    }

    private static async Task<Guid> CreateAssignmentAsync(HttpClient faculty, Guid offeringId, decimal maxMarks)
    {
        var created = await faculty.PostAsJsonAsync("api/v1/assignment", new
        {
            courseOfferingId = offeringId, title = "M3 essay " + Guid.NewGuid().ToString("N")[..5], description = "Write it",
            dueDate = DateTime.UtcNow.AddDays(7), maxMarks
        });
        var body = await created.Content.ReadAsStringAsync();
        Assert.True(created.IsSuccessStatusCode, body);
        var id = JsonDocument.Parse(body).RootElement.GetProperty("id").GetGuid();
        Assert.True((await faculty.PostAsync($"api/v1/assignment/{id}/publish", null)).IsSuccessStatusCode);
        return id;
    }

    private static Task<HttpResponseMessage> SubmitAsync(HttpClient student, Guid assignmentId, string? text, params (string Name, byte[] Bytes)[] files)
    {
        var form = new MultipartFormDataContent();
        if (text is not null) form.Add(new StringContent(text), "textContent");
        foreach (var (name, bytes) in files)
        {
            var part = new ByteArrayContent(bytes);
            part.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(part, "files", name);
        }
        return student.PostAsync($"api/v1/assignment/{assignmentId}/submissions", form);
    }

    private static Task<HttpResponseMessage> CommentAsync(HttpClient client, Guid submissionId, string? body, bool isPrivate, (string Name, byte[] Bytes)? media = null)
    {
        var form = new MultipartFormDataContent();
        if (body is not null) form.Add(new StringContent(body), "body");
        form.Add(new StringContent(isPrivate ? "true" : "false"), "isPrivate");
        if (media is { } m)
        {
            var part = new ByteArrayContent(m.Bytes);
            part.Headers.ContentType = new MediaTypeHeaderValue("audio/webm");
            form.Add(part, "media", m.Name);
        }
        return client.PostAsync($"api/v1/assignment/submissions/{submissionId}/comments", form);
    }

    private async Task<(decimal Marks, decimal Max, ResultSource Source)?> ResultAsync(Guid studentProfileId, Guid offeringId, string type)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var row = await db.Results.AsNoTracking().FirstOrDefaultAsync(r => r.StudentProfileId == studentProfileId && r.CourseOfferingId == offeringId && r.ResultType == type);
        return row is null ? null : (row.MarksObtained, row.MaxMarks, row.Source);
    }

    /// <summary>Result components are global per institution type: swap in a College set and restore it afterwards.</summary>
    private async Task<Func<Task>> UseCollegeComponentsAsync(params (string Name, decimal Weight)[] components)
    {
        List<(string Name, decimal Weight, int Order, bool Active)> previous;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var existing = await db.ResultComponentRules.Where(r => r.InstitutionType == InstitutionType.College).ToListAsync();
            previous = existing.Select(r => (r.Name, r.Weightage, r.DisplayOrder, r.IsActive)).ToList();
            db.ResultComponentRules.RemoveRange(existing);
            db.ResultComponentRules.AddRange(components.Select((c, i) => new ResultComponentRule(c.Name, c.Weight, i + 1, true, InstitutionType.College)));
            await db.SaveChangesAsync();
        }
        return async () =>
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.ResultComponentRules.RemoveRange(await db.ResultComponentRules.Where(r => r.InstitutionType == InstitutionType.College).ToListAsync());
            db.ResultComponentRules.AddRange(previous.Select(p => new ResultComponentRule(p.Name, p.Weight, p.Order, p.Active, InstitutionType.College)));
            await db.SaveChangesAsync();
        };
    }
}
