using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Application.Assignments;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Tenancy;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Plan M8 — late &amp; missing policy, availability and "Assign to" dates, student groups (group work, group grading,
/// group discussions) and peer review, through the real API.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class AssignmentEnhancementsIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    // Discussions (and staff scope checks) are tenant/campus scoped, so each test runs inside its own tenant and campus.
    private Guid _tenantId;
    private Guid _campusId;

    public AssignmentEnhancementsIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    [Fact]
    public async Task LatePolicy_Overrides_Deduction_Waiver_AndMissingGrades()
    {
        var seed = await SeedAsync(2);
        var (annId, benId) = (seed.Students[0], seed.Students[1]);
        var faculty = Client("Faculty");
        var ann = Client("Student", annId);
        var ben = Client("Student", benId);

        // Policy: 10% a started day, missing work gets 0.
        var initial = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/assignment/offerings/{seed.OfferingId}/late-policy");
        Assert.False(initial.GetProperty("lateDeductionEnabled").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PutAsJsonAsync($"api/v1/assignment/offerings/{seed.OfferingId}/late-policy",
            new { lateDeductionEnabled = true, deductionPercent = 10m, interval = "Week", lowestGradePercent = 0m, missingEnabled = false, missingGradePercent = 0m })).StatusCode);
        var saved = await faculty.PutAsJsonAsync($"api/v1/assignment/offerings/{seed.OfferingId}/late-policy",
            new { lateDeductionEnabled = true, deductionPercent = 10m, interval = "Day", lowestGradePercent = 0m, missingEnabled = true, missingGradePercent = 0m });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ann.GetAsync($"api/v1/assignment/offerings/{seed.OfferingId}/late-policy")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client("Faculty", tenantId: Guid.NewGuid()).GetAsync($"api/v1/assignment/offerings/{seed.OfferingId}/late-policy")).StatusCode);

        var due = DateTime.UtcNow.AddDays(7);
        var assignmentId = await CreateAssignmentAsync(faculty, seed.OfferingId, "M8 essay", due);
        await SaveOptionsAsync(faculty, assignmentId, availableUntil: due.AddDays(3));
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PutAsJsonAsync($"api/v1/assignment/{assignmentId}/options", Options(availableUntil: due.AddDays(-1)))).StatusCode);

        // Ann's own dates: due 36 hours ago, late work accepted until tomorrow.
        var annOverride = await faculty.PostAsJsonAsync($"api/v1/assignment/{assignmentId}/overrides",
            new { studentProfileId = annId, dueDate = DateTime.UtcNow.AddHours(-36), availableUntil = DateTime.UtcNow.AddDays(1), note = "Extension test" });
        Assert.Equal(HttpStatusCode.Created, annOverride.StatusCode);
        var annOverrideId = (await annOverride.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PostAsJsonAsync($"api/v1/assignment/{assignmentId}/overrides",
            new { studentProfileId = seed.Outsider, dueDate = due })).StatusCode);

        var extras = await ann.GetFromJsonAsync<JsonElement>($"api/v1/assignment/{assignmentId}/my-extras");
        Assert.True(extras.GetProperty("datesOverridden").GetBoolean());
        Assert.True(extras.GetProperty("dueDate").GetDateTime() < DateTime.UtcNow);
        Assert.Equal(10m, extras.GetProperty("latePolicy").GetProperty("deductionPercent").GetDecimal());
        Assert.Equal(HttpStatusCode.Forbidden, (await Client("Student", seed.Outsider).GetAsync($"api/v1/assignment/{assignmentId}/my-extras")).StatusCode);
        // The assignment list shows Ann her own due date, and everyone else the assignment's.
        DateTime ListedDue(JsonElement list) => list.EnumerateArray().Single(a => a.GetProperty("id").GetGuid() == assignmentId).GetProperty("dueDate").GetDateTime();
        Assert.True(ListedDue(await ann.GetFromJsonAsync<JsonElement>($"api/v1/assignment/by-offering/{seed.OfferingId}")) < DateTime.UtcNow);
        Assert.True(ListedDue(await ben.GetFromJsonAsync<JsonElement>($"api/v1/assignment/by-offering/{seed.OfferingId}")) > DateTime.UtcNow.AddDays(6));

        // Ann hands in late (2 started days) and is graded 15/20 → 4 marks deducted.
        var submissionId = await SubmitAsync(ann, assignmentId, "Late essay");
        Assert.Equal(HttpStatusCode.OK, (await faculty.PutAsJsonAsync($"api/v1/assignment/submissions/{submissionId}/grade", new { marksAwarded = 15m, feedback = "Good" })).StatusCode);
        var detail = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{submissionId}");
        Assert.True(detail.GetProperty("isLate").GetBoolean());
        Assert.Equal(15m, detail.GetProperty("rawMarks").GetDecimal());
        Assert.Equal(4m, detail.GetProperty("latePenalty").GetDecimal());
        Assert.Equal(11m, detail.GetProperty("marksAwarded").GetDecimal());
        extras = await ann.GetFromJsonAsync<JsonElement>($"api/v1/assignment/{assignmentId}/my-extras");
        Assert.Equal(4m, extras.GetProperty("latePenalty").GetDecimal());

        // Waiving the deduction gives the raw mark; restoring it applies it again.
        Assert.Equal(HttpStatusCode.OK, (await faculty.PutAsJsonAsync($"api/v1/assignment/submissions/{submissionId}/late-penalty", new { waived = true })).StatusCode);
        detail = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{submissionId}");
        Assert.Equal(15m, detail.GetProperty("marksAwarded").GetDecimal());
        Assert.True(detail.GetProperty("latePenaltyWaived").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await faculty.PutAsJsonAsync($"api/v1/assignment/submissions/{submissionId}/late-penalty", new { waived = false })).StatusCode);
        Assert.Equal(11m, (await faculty.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{submissionId}")).GetProperty("marksAwarded").GetDecimal());

        // Ben: due an hour ago with no late window → the missing grade; he cannot hand in any more.
        Assert.Equal(HttpStatusCode.Created, (await faculty.PostAsJsonAsync($"api/v1/assignment/{assignmentId}/overrides",
            new { studentProfileId = benId, dueDate = DateTime.UtcNow.AddHours(-1) })).StatusCode);
        var run = await (await faculty.PostAsync($"api/v1/assignment/offerings/{seed.OfferingId}/apply-missing", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, run.GetProperty("graded").GetInt32());
        Assert.Equal(0, (await (await faculty.PostAsync($"api/v1/assignment/offerings/{seed.OfferingId}/apply-missing", null)).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("graded").GetInt32());
        var benRow = WorkspaceRow(await WorkspaceAsync(faculty, assignmentId), benId);
        Assert.True(benRow.GetProperty("isMissing").GetBoolean());
        Assert.Equal(0m, benRow.GetProperty("marksAwarded").GetDecimal());
        var refused = await ben.PostAsJsonAsync("api/v1/assignment/submit", new { assignmentId, textContent = "Too late" });
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Contains("due date has passed", await refused.Content.ReadAsStringAsync());

        // A new override (replacing the old one) opens a late window; real work replaces the missing grade.
        Assert.Equal(HttpStatusCode.Created, (await faculty.PostAsJsonAsync($"api/v1/assignment/{assignmentId}/overrides",
            new { studentProfileId = benId, dueDate = DateTime.UtcNow.AddHours(-1), availableUntil = DateTime.UtcNow.AddDays(1) })).StatusCode);
        var overrides = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/assignment/{assignmentId}/overrides");
        Assert.Equal(2, overrides.GetArrayLength());
        Assert.Contains(overrides.EnumerateArray(), o => o.GetProperty("note").GetString() == "Extension test" && o.GetProperty("targetName").GetString()!.StartsWith("M8 Student 0"));
        await SubmitAsync(ben, assignmentId, "Finally");
        benRow = WorkspaceRow(await WorkspaceAsync(faculty, assignmentId), benId);
        Assert.False(benRow.GetProperty("isMissing").GetBoolean());
        Assert.True(!benRow.TryGetProperty("marksAwarded", out var benMarks) || benMarks.ValueKind == JsonValueKind.Null);

        // Removing Ann's override brings back the assignment's dates.
        Assert.Equal(HttpStatusCode.NoContent, (await faculty.DeleteAsync($"api/v1/assignment/overrides/{annOverrideId}")).StatusCode);
        Assert.False((await ann.GetFromJsonAsync<JsonElement>($"api/v1/assignment/{assignmentId}/my-extras")).GetProperty("datesOverridden").GetBoolean());
    }

    [Fact]
    public async Task Groups_GroupWork_GroupGrading_SelfSignup_AndGroupDiscussions()
    {
        var seed = await SeedAsync(4);
        var faculty = Client("Faculty");

        var created = await faculty.PostAsJsonAsync($"api/v1/assignment/offerings/{seed.OfferingId}/group-sets", new { name = "Project teams", selfSignup = false, maxSize = (int?)null });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var setId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var split = await faculty.PostAsJsonAsync($"api/v1/assignment/group-sets/{setId}/random-split", new { groupCount = 2, groupSize = (int?)null });
        Assert.Equal(HttpStatusCode.OK, split.StatusCode);
        var set = await split.Content.ReadFromJsonAsync<JsonElement>();
        var groups = set.GetProperty("groups").EnumerateArray().ToList();
        Assert.Equal(2, groups.Count);
        Assert.All(groups, g => Assert.Equal(2, g.GetProperty("memberCount").GetInt32()));
        Assert.Equal(0, set.GetProperty("unassigned").GetArrayLength());
        var (g1, g2) = (groups[0].GetProperty("id").GetGuid(), groups[1].GetProperty("id").GetGuid());
        Guid Member(JsonElement g, int i) => g.GetProperty("members")[i].GetProperty("studentProfileId").GetGuid();
        var (s1, s2, s3, s4) = (Member(groups[0], 0), Member(groups[0], 1), Member(groups[1], 0), Member(groups[1], 1));
        var roster = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/assignment/offerings/{seed.OfferingId}/roster");
        Assert.Equal(4, roster.GetArrayLength());
        Assert.DoesNotContain(roster.EnumerateArray(), r => r.GetProperty("studentProfileId").GetGuid() == seed.Outsider);

        // A group assignment: one hand-in for the group, one grade for every member.
        var assignmentId = await CreateAssignmentAsync(faculty, seed.OfferingId, "M8 group project", DateTime.UtcNow.AddDays(7));
        await SaveOptionsAsync(faculty, assignmentId, groupSetId: setId);
        var submissionId = await SubmitAsync(Client("Student", s1), assignmentId, "Our project");
        var mateRefused = await Client("Student", s2).PostAsJsonAsync("api/v1/assignment/submit", new { assignmentId, textContent = "Mine too" });
        Assert.Equal(HttpStatusCode.BadRequest, mateRefused.StatusCode); // the group already handed in (1 attempt)
        var workspace = await WorkspaceAsync(faculty, assignmentId);
        Assert.Equal(g1, WorkspaceRow(workspace, s2).GetProperty("groupId").GetGuid());
        Assert.Equal(HttpStatusCode.OK, (await faculty.PutAsJsonAsync($"api/v1/assignment/submissions/{submissionId}/grade", new { marksAwarded = 18m, feedback = "Team effort" })).StatusCode);
        var mateSubmission = WorkspaceRow(await WorkspaceAsync(faculty, assignmentId), s2).GetProperty("submissionId").GetGuid();
        var mateDetail = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{mateSubmission}");
        Assert.Equal(18m, mateDetail.GetProperty("marksAwarded").GetDecimal());
        Assert.Equal(groups[0].GetProperty("name").GetString(), mateDetail.GetProperty("groupName").GetString());
        var change = await faculty.PutAsJsonAsync($"api/v1/assignment/{assignmentId}/options", Options());
        Assert.Equal(HttpStatusCode.Conflict, change.StatusCode);

        // A student taken out of their group cannot hand in group work.
        Assert.Equal(HttpStatusCode.NoContent, (await faculty.DeleteAsync($"api/v1/assignment/groups/{g2}/members/{s4}")).StatusCode);
        var alone = await Client("Student", s4).PostAsJsonAsync("api/v1/assignment/submit", new { assignmentId, textContent = "Solo" });
        Assert.Equal(HttpStatusCode.BadRequest, alone.StatusCode);
        Assert.Contains("Join a group", await alone.Content.ReadAsStringAsync());
        Assert.True((await Client("Student", s4).GetFromJsonAsync<JsonElement>($"api/v1/assignment/{assignmentId}/my-extras")).GetProperty("needsGroup").GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent, (await faculty.PostAsJsonAsync($"api/v1/assignment/groups/{g2}/members", new { studentProfileId = s4 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PostAsJsonAsync($"api/v1/assignment/groups/{g2}/members", new { studentProfileId = seed.Outsider })).StatusCode);

        // Self sign-up: groups of one; full groups refuse; teacher-assigned groups cannot be joined.
        var buddies = await (await faculty.PostAsJsonAsync($"api/v1/assignment/offerings/{seed.OfferingId}/group-sets", new { name = "Study buddies", selfSignup = true, maxSize = 1 }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var buddiesId = buddies.GetProperty("id").GetGuid();
        var pairA = (await (await faculty.PostAsJsonAsync($"api/v1/assignment/group-sets/{buddiesId}/groups", new { name = "Pair A" })).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("groups").EnumerateArray().Single(g => g.GetProperty("name").GetString() == "Pair A").GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await Client("Student", s1).PostAsync($"api/v1/assignment/groups/{pairA}/join", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Client("Student", s2).PostAsync($"api/v1/assignment/groups/{pairA}/join", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client("Student", s1).PostAsync($"api/v1/assignment/groups/{g2}/join", null)).StatusCode);
        var mine = await Client("Student", s1).GetFromJsonAsync<JsonElement>($"api/v1/assignment/offerings/{seed.OfferingId}/my-groups");
        Assert.Equal(2, mine.GetArrayLength());
        Assert.All(mine.EnumerateArray(), m => Assert.NotEqual(JsonValueKind.Null, m.GetProperty("group").ValueKind));
        Assert.Equal(HttpStatusCode.NoContent, (await Client("Student", s1).DeleteAsync($"api/v1/assignment/groups/{pairA}/join")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Client("Student", s2).PostAsync($"api/v1/assignment/groups/{pairA}/join", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client("Student", seed.Outsider).GetAsync($"api/v1/assignment/offerings/{seed.OfferingId}/my-groups")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client("Student", s1).GetAsync($"api/v1/assignment/offerings/{seed.OfferingId}/group-sets")).StatusCode);

        // Group discussions: only the group (and staff) see them.
        var thread = await Client("Student", s1).PostAsJsonAsync("api/v1/discussion/thread",
            new { offeringId = seed.OfferingId, authorId = Guid.Empty, title = "Team planning", threadType = "Discussion", groupId = g1 });
        Assert.Equal(HttpStatusCode.Created, thread.StatusCode);
        var threadId = (await thread.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Forbidden, (await Client("Student", s3).PostAsJsonAsync("api/v1/discussion/thread",
            new { offeringId = seed.OfferingId, authorId = Guid.Empty, title = "Sneaky", threadType = "Discussion", groupId = g1 })).StatusCode);
        async Task<bool> SeesAsync(HttpClient c) => (await c.GetFromJsonAsync<JsonElement>($"api/v1/discussion/{seed.OfferingId}/threads"))
            .EnumerateArray().Any(t => t.GetProperty("id").GetGuid() == threadId);
        Assert.True(await SeesAsync(Client("Student", s2)));
        Assert.True(await SeesAsync(faculty));
        Assert.False(await SeesAsync(Client("Student", s3)));
        Assert.Equal(HttpStatusCode.NotFound, (await Client("Student", s3).GetAsync($"api/v1/discussion/thread/{threadId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client("Student", s3).PostAsJsonAsync("api/v1/discussion/reply", new { threadId, authorId = Guid.Empty, body = "hi" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client("Student", s2).GetAsync($"api/v1/discussion/thread/{threadId}")).StatusCode);
    }

    [Fact]
    public async Task PeerReview_Allocation_Rubric_Anonymity_AndAverages()
    {
        var seed = await SeedAsync(4);
        var faculty = Client("Faculty");
        var due = DateTime.UtcNow.AddDays(7);
        var assignmentId = await CreateAssignmentAsync(faculty, seed.OfferingId, "M8 peer essay", due);
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PutAsJsonAsync($"api/v1/assignment/{assignmentId}/options", Options(peerReviewCount: 6))).StatusCode);
        await SaveOptionsAsync(faculty, assignmentId, peerReviewCount: 2, peerReviewDueDate: due.AddDays(3));
        var rubric = await faculty.PostAsJsonAsync("api/v1/rubric", new
        {
            assignmentId, title = "Peer rubric",
            criteria = new[]
            {
                new { name = "Argument", maxPoints = 6m, displayOrder = 1, levels = new[] { new { label = "Weak", pointsAwarded = 2m, displayOrder = 1 }, new { label = "Strong", pointsAwarded = 6m, displayOrder = 2 } } },
                new { name = "Style", maxPoints = 4m, displayOrder = 2, levels = new[] { new { label = "Poor", pointsAwarded = 1m, displayOrder = 1 }, new { label = "Clear", pointsAwarded = 4m, displayOrder = 2 } } }
            }
        });
        Assert.True(rubric.IsSuccessStatusCode, await rubric.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Conflict, (await faculty.PostAsync($"api/v1/assignment/{assignmentId}/peer-reviews/allocate", null)).StatusCode); // nothing handed in
        var submissions = new Dictionary<Guid, Guid>();
        foreach (var s in seed.Students.Take(3)) submissions[s] = await SubmitAsync(Client("Student", s), assignmentId, $"Essay by {s:N}");

        // Three pieces of work, two reviews each, never their own.
        var allocated = await faculty.PostAsync($"api/v1/assignment/{assignmentId}/peer-reviews/allocate", null);
        Assert.Equal(HttpStatusCode.OK, allocated.StatusCode);
        Assert.Equal(6, (await allocated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("allocated").GetInt32());
        Assert.Equal(HttpStatusCode.Conflict, (await faculty.PostAsync($"api/v1/assignment/{assignmentId}/peer-reviews/allocate", null)).StatusCode);
        var overview = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/assignment/{assignmentId}/peer-reviews");
        Assert.Equal(6, overview.GetProperty("assigned").GetInt32());
        Assert.All(overview.GetProperty("reviews").EnumerateArray(), r => Assert.NotEqual(r.GetProperty("revieweeLabel").GetString(), r.GetProperty("reviewerLabel").GetString()));

        // The reviewer sees the work anonymously, with the rubric; others' reviews are hidden from them.
        var reviewerId = seed.Students[0];
        var reviewer = Client("Student", reviewerId);
        var toDo = (await reviewer.GetFromJsonAsync<JsonElement>($"api/v1/assignment/{assignmentId}/my-extras")).GetProperty("reviewsToDo").EnumerateArray().ToList();
        Assert.Equal(2, toDo.Count);
        var task = await reviewer.GetFromJsonAsync<JsonElement>($"api/v1/assignment/peer-reviews/{toDo[0].GetProperty("id").GetGuid()}");
        Assert.Equal("Peer submission", task.GetProperty("revieweeLabel").GetString());
        Assert.StartsWith("Essay by", task.GetProperty("text").GetString());
        Assert.Equal(2, task.GetProperty("rubric").GetArrayLength());
        var someoneElses = overview.GetProperty("reviews").EnumerateArray().First(r => toDo.All(t => t.GetProperty("id").GetGuid() != r.GetProperty("id").GetGuid())).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await reviewer.GetAsync($"api/v1/assignment/peer-reviews/{someoneElses}")).StatusCode);

        // Rubric review: Strong + Clear = 10/10 → 20 marks. Score-only review: 10.
        Guid Level(JsonElement c, string label) => c.GetProperty("levels").EnumerateArray().Single(l => l.GetProperty("label").GetString() == label).GetProperty("levelId").GetGuid();
        var criteria = task.GetProperty("rubric").EnumerateArray().ToList();
        var first = await reviewer.PostAsJsonAsync($"api/v1/assignment/peer-reviews/{toDo[0].GetProperty("id").GetGuid()}", new
        {
            score = (decimal?)null, comment = "Convincing",
            rubric = new[]
            {
                new { criterionId = criteria[0].GetProperty("criterionId").GetGuid(), levelId = Level(criteria[0], "Strong") },
                new { criterionId = criteria[1].GetProperty("criterionId").GetGuid(), levelId = Level(criteria[1], "Clear") }
            }
        });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(20m, (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("score").GetDecimal());
        var secondId = toDo[1].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await reviewer.PostAsJsonAsync($"api/v1/assignment/peer-reviews/{secondId}", new { score = 25m, comment = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await reviewer.PostAsJsonAsync($"api/v1/assignment/peer-reviews/{secondId}", new { score = (decimal?)null, comment = " " })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await reviewer.PostAsJsonAsync($"api/v1/assignment/peer-reviews/{secondId}", new { score = 10m, comment = "Needs sources" })).StatusCode);

        // The reviewee sees the anonymous review; the teacher sees the average in the workspace.
        var revieweeSubmission = toDo[0].GetProperty("submissionId").GetGuid();
        var reviewee = submissions.Single(kv => kv.Value == revieweeSubmission).Key;
        var received = (await Client("Student", reviewee).GetFromJsonAsync<JsonElement>($"api/v1/assignment/{assignmentId}/my-extras")).GetProperty("reviewsReceived");
        Assert.Contains(received.EnumerateArray(), r => r.GetProperty("reviewerLabel").GetString()!.StartsWith("Peer") && r.GetProperty("comment").GetString() == "Convincing");
        var detail = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{revieweeSubmission}");
        Assert.Equal(20m, detail.GetProperty("peerReviewAverage").GetDecimal());
        Assert.Equal(1, detail.GetProperty("peerReviewsCompleted").GetInt32());
        var perSubmission = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/assignment/submissions/{revieweeSubmission}/peer-reviews");
        Assert.Equal(20m, perSubmission.GetProperty("average").GetDecimal());
        Assert.Equal(2, (await faculty.GetFromJsonAsync<JsonElement>($"api/v1/assignment/{assignmentId}/peer-reviews")).GetProperty("completed").GetInt32());

        // The student who handed nothing in has nothing to review; staff endpoints refuse students.
        Assert.Equal(0, (await Client("Student", seed.Students[3]).GetFromJsonAsync<JsonElement>($"api/v1/assignment/{assignmentId}/my-extras"))
            .GetProperty("reviewsToDo").GetArrayLength());
        Assert.Equal(HttpStatusCode.Forbidden, (await reviewer.PostAsync($"api/v1/assignment/{assignmentId}/peer-reviews/allocate", null)).StatusCode);

        // Automatic allocation once the due date has passed.
        var second = await CreateAssignmentAsync(faculty, seed.OfferingId, "M8 auto peer", due);
        await SaveOptionsAsync(faculty, second, peerReviewCount: 1, peerReviewDueDate: due.AddDays(1));
        await SubmitAsync(Client("Student", seed.Students[0]), second, "One");
        await SubmitAsync(Client("Student", seed.Students[1]), second, "Two");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE assignments SET DueDate = {DateTime.UtcNow.AddHours(-1)}, PeerReviewDueDate = {DateTime.UtcNow.AddDays(2)} WHERE Id = {second}");
            Assert.True(await scope.ServiceProvider.GetRequiredService<PeerReviewService>().AllocateDueAsync(DateTime.UtcNow) >= 2);
        }
        Assert.Equal(2, (await faculty.GetFromJsonAsync<JsonElement>($"api/v1/assignment/{second}/peer-reviews")).GetProperty("assigned").GetInt32());
    }

    // ── helpers ──

    private sealed record Seed(Guid OfferingId, IReadOnlyList<Guid> Students, Guid Outsider);

    private async Task<Seed> SeedAsync(int enrolled)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var studentRole = db.Roles.First(r => r.Name == "Student");
        var tenant = new Tenant($"M8T{suffix}", $"M8 Tenant {suffix}");
        var campus = new Campus(tenant.Id, $"M8K{suffix}", $"M8 Campus {suffix}");
        db.AddRange(tenant, campus);
        (_tenantId, _campusId) = (tenant.Id, campus.Id);
        var department = new Department($"M8 Dept {suffix}", $"M8D{suffix}", InstitutionType.University);
        department.SetTenantCampus(tenant.Id, campus.Id);
        var program = new AcademicProgram($"M8 Program {suffix}", $"M8P{suffix}", department.Id, 8);
        var semester = new Semester($"M8 Sem {suffix}", DateTime.UtcNow.Date.AddDays(-10), DateTime.UtcNow.Date.AddDays(80));
        var course = new Course($"M8 Course {suffix}", $"M8C{suffix}", 3, department.Id);
        var offering = new CourseOffering(course.Id, semester.Id, 40);
        offering.SetInstitutionScope(InstitutionType.University, tenant.Id, campus.Id);
        db.AddRange(department, program, semester, course, offering);
        var ids = new List<Guid>();
        for (var i = 0; i <= enrolled; i++)
        {
            var user = new User($"m8_{i}_{suffix}", "integration-hash", studentRole.Id, $"m8_{i}_{suffix}@tabsan.local");
            user.UpdateFullName($"M8 Student {i} {suffix}");
            var profile = new StudentProfile(user.Id, $"M8-{i}-{suffix}", program.Id, department.Id, DateTime.UtcNow.Date);
            db.AddRange(user, profile);
            if (i < enrolled) db.Add(new Enrollment(profile.Id, offering.Id));
            ids.Add(profile.Id);
        }
        await db.SaveChangesAsync();
        return new Seed(offering.Id, ids.Take(enrolled).ToList(), ids[enrolled]);
    }

    private HttpClient Client(string role, Guid? studentProfileId = null, Guid? tenantId = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            JwtTestHelper.GenerateToken(role, Guid.NewGuid().ToString(), studentProfileId: studentProfileId?.ToString(),
                tenantId: (tenantId ?? _tenantId).ToString(), campusId: (tenantId is null ? _campusId : Guid.NewGuid()).ToString()));
        return client;
    }

    private static object Options(DateTime? availableUntil = null, Guid? groupSetId = null, int peerReviewCount = 0, DateTime? peerReviewDueDate = null)
        => new
        {
            availableFrom = (DateTime?)null, availableUntil, groupSetId, gradeGroupIndividually = false,
            peerReviewCount, peerReviewDueDate, peerReviewAnonymous = true
        };

    private static async Task SaveOptionsAsync(HttpClient faculty, Guid assignmentId, DateTime? availableUntil = null, Guid? groupSetId = null,
        int peerReviewCount = 0, DateTime? peerReviewDueDate = null)
    {
        var response = await faculty.PutAsJsonAsync($"api/v1/assignment/{assignmentId}/options", Options(availableUntil, groupSetId, peerReviewCount, peerReviewDueDate));
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private static async Task<Guid> CreateAssignmentAsync(HttpClient faculty, Guid offeringId, string title, DateTime due)
    {
        var created = await faculty.PostAsJsonAsync("api/v1/assignment", new { courseOfferingId = offeringId, title, description = "Write", dueDate = due, maxMarks = 20m });
        var body = await created.Content.ReadAsStringAsync();
        Assert.True(created.IsSuccessStatusCode, body);
        var id = JsonDocument.Parse(body).RootElement.GetProperty("id").GetGuid();
        Assert.True((await faculty.PostAsync($"api/v1/assignment/{id}/publish", null)).IsSuccessStatusCode);
        await faculty.PutAsJsonAsync($"api/v1/assignment/{id}/settings", new
        {
            allowedAttempts = 1, allowedFileTypes = (string[]?)null, maxFileSizeMb = 10, anonymousGrading = false, postingPolicy = "Automatic", moderatedGrading = false
        });
        return id;
    }

    private static async Task<Guid> SubmitAsync(HttpClient student, Guid assignmentId, string text)
    {
        var response = await student.PostAsJsonAsync("api/v1/assignment/submit", new { assignmentId, textContent = text });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonDocument.Parse(body).RootElement.GetProperty("submissionId").GetGuid();
    }

    private static async Task<JsonElement> WorkspaceAsync(HttpClient faculty, Guid assignmentId)
    {
        var response = await faculty.GetAsync($"api/v1/assignment/{assignmentId}/workspace");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonDocument.Parse(body).RootElement;
    }

    private static JsonElement WorkspaceRow(JsonElement workspace, Guid studentProfileId)
        => workspace.GetProperty("submissions").EnumerateArray().Single(s => s.GetProperty("studentProfileId").GetGuid() == studentProfileId);
}
