using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Lms;
using Tabsan.EduSphere.Domain.Outcomes;
using Tabsan.EduSphere.Domain.Quizzes;
using Tabsan.EduSphere.Domain.Tenancy;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Course copy through the real API and database: a teacher copies last term's course into this term's (modules, pages,
/// videos, assignments with rubrics, a quiz drawing from the course's bank, a file, the late policy, outcome links), with
/// the dates moved and everything hidden; SCORM and discussions are reported; the source is untouched; the copy is in the
/// history. Teachers of another department cannot copy it until an admin shares it as a school-wide template.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class CourseCopyIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public CourseCopyIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    private sealed record Seed(Guid Tenant, Guid TeacherId, Guid OtherTeacherId, Guid AdminId, Guid SourceId, Guid TargetId, Guid ScienceId, Guid PoemId,
        Guid OwnOutcomeId, DateTime OldStart, DateTime EssayDue, DateTime Unlock, string TeacherName, string MaterialPath);

    private HttpClient Client(string role, Guid userId, Guid? tenant)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            JwtTestHelper.GenerateToken(role, userId.ToString(), tenantId: tenant?.ToString()));
        return client;
    }

    private async Task<Seed> SeedAsync()
    {
        var super = Client("SuperAdmin", Guid.NewGuid(), null);
        foreach (var key in new[] { "courses", "assignments", "quizzes" })
        {
            var response = await super.PostAsync($"api/v1/module/{key}/activate", null);
            Assert.True(response.IsSuccessStatusCode, $"{key}: {await response.Content.ReadAsStringAsync()}");
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var s = Guid.NewGuid().ToString("N")[..6];
        var role = (string name) => db.Roles.First(r => r.Name == name).Id;
        var tenant = new Tenant($"CCT{s}", $"Copy Tenant {s}");
        var campus = new Campus(tenant.Id, $"CCK{s}", $"Copy Campus {s}");
        var english = new Department($"Copy English {s}", $"CCE{s}", InstitutionType.School);
        english.SetTenantCampus(tenant.Id, campus.Id);
        var science = new Department($"Copy Science {s}", $"CCS{s}", InstitutionType.School);
        science.SetTenantCampus(tenant.Id, campus.Id);
        var program = new AcademicProgram($"Copy Program {s}", $"CCP{s}", english.Id, 10);
        var today = DateTime.UtcNow.Date;
        var oldStart = DateTime.SpecifyKind(today.AddDays(-182), DateTimeKind.Utc);
        var oldTerm = new Semester($"Copy Term 1 {s}", oldStart, oldStart.AddDays(120));
        var newTerm = new Semester($"Copy Term 2 {s}", today, today.AddDays(120));
        var course = new Course($"Copy English 7 {s}", $"CE{s}".ToUpperInvariant(), 3, english.Id);
        var scienceCourse = new Course($"Copy Science 7 {s}", $"CS{s}".ToUpperInvariant(), 3, science.Id);

        var teacherName = $"Ms Khan {s}";
        var teacher = new User($"cc_t_{s}", "integration-hash", role("Faculty"), $"cc_t_{s}@tabsan.local");
        teacher.UpdateFullName(teacherName);
        var otherTeacher = new User($"cc_o_{s}", "integration-hash", role("Faculty"), $"cc_o_{s}@tabsan.local");
        var admin = new User($"cc_a_{s}", "integration-hash", role("Admin"), $"cc_a_{s}@tabsan.local");

        CourseOffering Offer(Course c, Semester term, User by)
        {
            var o = new CourseOffering(c.Id, term.Id, 40, by.Id);
            o.SetInstitutionScope(InstitutionType.School, tenant.Id, campus.Id);
            return o;
        }
        var source = Offer(course, oldTerm, teacher);
        var target = Offer(course, newTerm, teacher);
        var scienceOffering = Offer(scienceCourse, newTerm, otherTeacher);

        // ── last term's content ──
        var week1 = new CourseContentModule(source.Id, "Week 1 — Rivers", 1, "Read chapter 1.");
        var unlock = oldStart.AddDays(7);
        week1.ConfigureRelease(unlock, true, ModuleCompletionMode.All);
        week1.Publish();
        var week2 = new CourseContentModule(source.Id, "Week 2 — Mountains", 2);
        var video = new ContentVideo(week1.Id, "River video", null, "https://video.example/rivers");
        var essayDue = oldStart.AddDays(14);
        var essay = new Assignment(source.Id, "Essay on rivers", "Two pages.", essayDue, 20);
        essay.Publish();
        var poem = new Assignment(source.Id, "Poem", null, oldStart.AddDays(30), 10);
        var rubric = Rubric.Create(essay.Id, "Essay rubric", teacher.Id);
        var ideas = rubric.AddCriterion("Ideas", 10, 1);
        var good = ideas.AddLevel("Good", 10, 1);
        var weak = ideas.AddLevel("Weak", 4, 2);
        var bank = new QuestionBank(tenant.Id, source.Id, english.Id, "Rivers bank", null, false, teacher.Id);
        var bankQuestion = QuizQuestion.ForBank(bank.Id, "Longest river?", QuestionType.MultipleChoice, 1, 1);
        var quiz = new Quiz(source.Id, "Rivers quiz", teacher.Id, null, 20, 1, oldStart.AddDays(8), oldStart.AddDays(9));
        quiz.Publish();
        var written = new QuizQuestion(quiz.Id, "Name a river.", QuestionType.MultipleChoice, 2, 1);
        var materialPath = $"materials/handout-{s}.pdf";
        var handout = new CourseMaterial(tenant.Id, campus.Id, english.Id, program.Id, oldTerm.Id, course.Id, "Handout", teacher.Id,
            CourseMaterialType.File, filePath: materialPath);
        var policy = new LatePolicy(source.Id);
        policy.Configure(true, 10, LateInterval.Day, 50, false, 0);
        var scorm = new ScormPackage(source.Id, "River tour", ScormVersion.Scorm12, "tour", "index.html", 3, 1000, null, teacher.Id);
        var thread = new DiscussionThread(source.Id, teacher.Id, "Your river", "FAQ", null, $"CC-{s}");
        var ownOutcome = new Outcome(tenant.Id, OutcomeLevel.Course, null, course.Id, $"CLO{s}", "Writes clearly", admin.Id);
        var otherOutcome = new Outcome(tenant.Id, OutcomeLevel.Course, null, scienceCourse.Id, $"SLO{s}", "Explains experiments", admin.Id);
        var position = 0;
        ModuleItem Item(ModuleItemType type, Guid? to, string title, string? body = null)
            => new(week1.Id, source.Id, type, to, title, null, body, ++position, teacher.Id);

        db.AddRange(tenant, campus, english, science, program, oldTerm, newTerm, course, scienceCourse, teacher, otherTeacher, admin,
            new FacultyDepartmentAssignment(teacher.Id, english.Id), new FacultyDepartmentAssignment(otherTeacher.Id, science.Id),
            new AdminDepartmentAssignment(admin.Id, english.Id), source, target, scienceOffering,
            week1, week2, new ModulePrerequisite(week2.Id, week1.Id), video, essay, poem, rubric, ideas, good, weak,
            bank, bankQuestion, new QuizOption(bankQuestion.Id, "Nile", true, 1), new QuizOption(bankQuestion.Id, "Ravi", false, 2),
            quiz, written, new QuizOption(written.Id, "Indus", true, 1), new QuizQuestionGroup(quiz.Id, bank.Id, "From the bank", 1, 1, 2),
            handout, policy, scorm, thread, ownOutcome, otherOutcome,
            new OutcomeAlignment(ownOutcome.Id, OutcomeTargetType.Assignment, essay.Id, source.Id, teacher.Id),
            new OutcomeAlignment(otherOutcome.Id, OutcomeTargetType.Assignment, essay.Id, source.Id, teacher.Id),
            Item(ModuleItemType.Page, null, "Welcome", "Hello class"), Item(ModuleItemType.Assignment, essay.Id, "Essay on rivers"),
            Item(ModuleItemType.Quiz, quiz.Id, "Rivers quiz"), Item(ModuleItemType.Material, handout.Id, "Handout"),
            Item(ModuleItemType.Video, video.Id, "River video"), Item(ModuleItemType.Discussion, thread.Id, "Your river"));
        await db.SaveChangesAsync();
        return new Seed(tenant.Id, teacher.Id, otherTeacher.Id, admin.Id, source.Id, target.Id, scienceOffering.Id, poem.Id, ownOutcome.Id,
            oldStart, essayDue, unlock, teacherName, materialPath);
    }

    private static async Task<JsonElement> OkJson(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"{(int)response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    private static int PartCount(JsonElement preview, string part)
        => preview.GetProperty("parts").EnumerateArray().Single(p => p.GetProperty("part").GetString() == part).GetProperty("count").GetInt32();

    [Fact]
    public async Task Teacher_CopiesLastTerm_IntoThisTerm_WithDatesMovedAndEverythingHidden()
    {
        var seed = await SeedAsync();
        var teacher = Client("Faculty", seed.TeacherId, seed.Tenant);

        var sources = await OkJson(await teacher.GetAsync($"api/v1/course-copy/offerings/{seed.TargetId}/sources"));
        Assert.Equal(seed.TargetId, sources.GetProperty("target").GetProperty("offeringId").GetGuid());
        var ids = sources.GetProperty("sources").EnumerateArray().Select(x => x.GetProperty("offeringId").GetGuid()).ToList();
        Assert.Contains(seed.SourceId, ids);
        Assert.DoesNotContain(seed.ScienceId, ids);
        Assert.DoesNotContain(seed.TargetId, ids);

        var preview = await OkJson(await teacher.GetAsync($"api/v1/course-copy/offerings/{seed.TargetId}/preview?sourceId={seed.SourceId}"));
        Assert.Equal(182, preview.GetProperty("suggestedShiftDays").GetInt32());
        Assert.False(preview.TryGetProperty("previouslyCopiedAt", out var before) && before.ValueKind == JsonValueKind.String); // nulls are left out
        Assert.Equal((2, 2, 1, 1, 1, 1), (PartCount(preview, "modules"), PartCount(preview, "assignments"), PartCount(preview, "quizzes"),
            PartCount(preview, "banks"), PartCount(preview, "files"), PartCount(preview, "settings")));
        var notes = preview.GetProperty("notCopied").EnumerateArray().Select(n => (n.GetProperty("reason").GetString(), n.GetProperty("title").GetString())).ToList();
        Assert.Contains(("scorm", "River tour"), notes);
        Assert.Contains(("discussion", "Your river"), notes);

        var copied = await OkJson(await teacher.PostAsJsonAsync($"api/v1/course-copy/offerings/{seed.TargetId}/copy",
            new { sourceOfferingId = seed.SourceId, shiftDays = 182, keepPublished = false }), HttpStatusCode.Created);
        var counts = copied.GetProperty("copied");
        Assert.Equal((2, 5, 1, 2, 1, 1, 2, 1, 1, 1, 1), (counts.GetProperty("modules").GetInt32(), counts.GetProperty("items").GetInt32(),
            counts.GetProperty("videos").GetInt32(), counts.GetProperty("assignments").GetInt32(), counts.GetProperty("rubrics").GetInt32(),
            counts.GetProperty("quizzes").GetInt32(), counts.GetProperty("questions").GetInt32(), counts.GetProperty("banks").GetInt32(),
            counts.GetProperty("files").GetInt32(), counts.GetProperty("settings").GetInt32(), counts.GetProperty("outcomes").GetInt32()));
        Assert.Contains(copied.GetProperty("notCopied").EnumerateArray(), n => n.GetProperty("reason").GetString() == "outcomes" && n.GetProperty("count").GetInt32() == 1);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var target = await db.CourseOfferings.Where(o => o.Id == seed.TargetId).Select(o => new { o.SemesterId, o.CourseId }).SingleAsync();
            var modules = await db.CourseContentModules.Where(m => m.OfferingId == seed.TargetId).OrderBy(m => m.WeekNumber).ToListAsync();
            Assert.Equal(["Week 1 — Rivers", "Week 2 — Mountains"], modules.Select(m => m.Title));
            Assert.All(modules, m => Assert.False(m.IsPublished));
            Assert.Equal(seed.Unlock.AddDays(182), modules[0].UnlockAt);
            Assert.True(await db.ModulePrerequisites.AnyAsync(p => p.ModuleId == modules[1].Id && p.PrerequisiteModuleId == modules[0].Id));

            var assignments = await db.Assignments.Where(a => a.CourseOfferingId == seed.TargetId).ToListAsync();
            var essay = assignments.Single(a => a.Title == "Essay on rivers");
            Assert.Equal((seed.EssayDue.AddDays(182), false), (essay.DueDate, essay.IsPublished));
            var rubric = await db.Rubrics.SingleAsync(r => r.AssignmentId == essay.Id);
            var criterion = await db.RubricCriteria.SingleAsync(c => c.RubricId == rubric.Id);
            Assert.Equal(["Good", "Weak"], await db.RubricLevels.Where(l => l.CriterionId == criterion.Id).OrderBy(l => l.DisplayOrder).Select(l => l.Label).ToListAsync());

            var quiz = await db.Quizzes.SingleAsync(q => q.CourseOfferingId == seed.TargetId);
            Assert.Equal((seed.OldStart.AddDays(190), false), (quiz.AvailableFrom!.Value, quiz.IsPublished));
            var newBank = await db.QuestionBanks.SingleAsync(b => b.CourseOfferingId == seed.TargetId);
            Assert.Equal(newBank.Id, (await db.QuizQuestionGroups.SingleAsync(g => g.QuizId == quiz.Id)).QuestionBankId);
            Assert.Equal(2, await db.QuizOptions.CountAsync(o => db.QuizQuestions.Any(q => q.Id == o.QuizQuestionId && q.QuestionBankId == newBank.Id)));
            Assert.Equal("Name a river.", (await db.QuizQuestions.SingleAsync(q => q.QuizId == quiz.Id)).Text);

            var material = await db.CourseMaterials.SingleAsync(m => m.SemesterId == target.SemesterId && m.CourseId == target.CourseId);
            Assert.Equal(seed.MaterialPath, material.FilePath);
            Assert.Equal(10, (await db.LatePolicies.SingleAsync(p => p.CourseOfferingId == seed.TargetId)).DeductionPercent);
            var link = await db.OutcomeAlignments.SingleAsync(a => a.CourseOfferingId == seed.TargetId);
            Assert.Equal((seed.OwnOutcomeId, essay.Id), (link.OutcomeId, link.TargetId));

            // Every item opens the copy, never last term's content.
            var items = await db.ModuleItems.Where(i => i.OfferingId == seed.TargetId).ToListAsync();
            Assert.Equal(5, items.Count);
            Assert.Equal(essay.Id, items.Single(i => i.ItemType == ModuleItemType.Assignment).TargetId);
            Assert.Equal(quiz.Id, items.Single(i => i.ItemType == ModuleItemType.Quiz).TargetId);
            Assert.Equal(material.Id, items.Single(i => i.ItemType == ModuleItemType.Material).TargetId);
            var videoId = items.Single(i => i.ItemType == ModuleItemType.Video).TargetId;
            Assert.Equal(modules[0].Id, (await db.ContentVideos.SingleAsync(v => v.Id == videoId)).ModuleId);

            // Last term is untouched.
            Assert.Equal(2, await db.CourseContentModules.CountAsync(m => m.OfferingId == seed.SourceId));
            Assert.Equal(6, await db.ModuleItems.CountAsync(i => i.OfferingId == seed.SourceId));
            Assert.True(await db.Assignments.AnyAsync(a => a.CourseOfferingId == seed.SourceId && a.Title == "Essay on rivers" && a.IsPublished));
        }

        var history = await OkJson(await teacher.GetAsync($"api/v1/course-copy/offerings/{seed.TargetId}/history"));
        var run = Assert.Single(history.EnumerateArray());
        Assert.Equal((seed.SourceId, seed.TeacherName, 182), (run.GetProperty("sourceOfferingId").GetGuid(), run.GetProperty("copiedBy").GetString(),
            run.GetProperty("shiftDays").GetInt32()));
        var again = await OkJson(await teacher.GetAsync($"api/v1/course-copy/offerings/{seed.TargetId}/preview?sourceId={seed.SourceId}"));
        Assert.Equal(JsonValueKind.String, again.GetProperty("previouslyCopiedAt").ValueKind);

        var unknown = await teacher.PostAsJsonAsync($"api/v1/course-copy/offerings/{seed.TargetId}/copy", new { sourceOfferingId = seed.SourceId, parts = new[] { "homework" } });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
    }

    [Fact]
    public async Task OtherDepartments_CopyOnlyWhatAnAdminSharesWithTheWholeSchool()
    {
        var seed = await SeedAsync();
        var teacher = Client("Faculty", seed.TeacherId, seed.Tenant);
        var scienceTeacher = Client("Faculty", seed.OtherTeacherId, seed.Tenant);
        var admin = Client("Admin", seed.AdminId, seed.Tenant);

        Assert.Equal(HttpStatusCode.Forbidden, (await scienceTeacher.GetAsync($"api/v1/course-copy/offerings/{seed.TargetId}/sources")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await scienceTeacher.GetAsync($"api/v1/course-copy/offerings/{seed.ScienceId}/preview?sourceId={seed.SourceId}")).StatusCode);
        var template = new { name = "English starter", description = "Weeks 1 and 2", scope = "Institution" };
        Assert.Equal(HttpStatusCode.Forbidden, (await teacher.PutAsJsonAsync($"api/v1/course-copy/offerings/{seed.SourceId}/template", template)).StatusCode);

        var shared = await OkJson(await admin.PutAsJsonAsync($"api/v1/course-copy/offerings/{seed.SourceId}/template", template), HttpStatusCode.Created);
        Assert.Equal(("English starter", "Institution"), (shared.GetProperty("name").GetString(), shared.GetProperty("scope").GetString()));
        var mine = await OkJson(await admin.GetAsync($"api/v1/course-copy/offerings/{seed.SourceId}/template"));
        Assert.Equal("Weeks 1 and 2", mine.GetProperty("description").GetString());

        var sources = await OkJson(await scienceTeacher.GetAsync($"api/v1/course-copy/offerings/{seed.ScienceId}/sources"));
        var listed = sources.GetProperty("sources").EnumerateArray().Single(x => x.GetProperty("offeringId").GetGuid() == seed.SourceId);
        Assert.True(listed.GetProperty("isTemplate").GetBoolean());

        await OkJson(await scienceTeacher.PostAsJsonAsync($"api/v1/course-copy/offerings/{seed.ScienceId}/copy",
            new { sourceOfferingId = seed.SourceId, parts = new[] { "assignments" }, itemIds = new[] { seed.PoemId } }), HttpStatusCode.Created);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(["Poem"], await db.Assignments.Where(a => a.CourseOfferingId == seed.ScienceId).Select(a => a.Title).ToListAsync());
            Assert.False(await db.CourseContentModules.AnyAsync(m => m.OfferingId == seed.ScienceId));
        }

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"api/v1/course-copy/offerings/{seed.SourceId}/template")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.GetAsync($"api/v1/course-copy/offerings/{seed.SourceId}/template")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await scienceTeacher.GetAsync($"api/v1/course-copy/offerings/{seed.ScienceId}/preview?sourceId={seed.SourceId}")).StatusCode);
    }
}
