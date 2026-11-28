using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Notifications;
using Tabsan.EduSphere.Domain.Pastoral;
using Tabsan.EduSphere.Domain.Tenancy;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Pastoral care and wellbeing through the real API and database: a teacher limited to the students they teach, the
/// wellbeing team set up by an admin, confidential notes kept from teachers, notes shared with parents, alerts and
/// follow-ups, a removed note; then check-ins with an alert and "seen to", a support plan shown to teachers and parents,
/// the overview and the set-up page.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class PastoralIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public PastoralIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    private sealed record Seed(Guid Tenant, Guid CampusA, Guid CampusB, Guid AdminId, Guid TeacherId, Guid CounsellorId, Guid AliId, Guid AliProfile,
        Guid ZoyaProfile, Guid MumId, Guid DadId);

    private HttpClient Client(string role, Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(role, userId.ToString()));
        return client;
    }

    private async Task<Seed> SeedAsync()
    {
        var super = Client("SuperAdmin", Guid.NewGuid());
        foreach (var module in new[] { "notifications" }) // pastoral care itself is part of the core "sis" module
        {
            var activated = await super.PostAsync($"api/v1/module/{module}/activate", null);
            Assert.True(activated.IsSuccessStatusCode, await activated.Content.ReadAsStringAsync());
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var s = Guid.NewGuid().ToString("N")[..6];
        var role = (string name) => db.Roles.First(r => r.Name == name).Id;
        var tenant = new Tenant($"PCT{s}", $"Pastoral School {s}");
        var campusA = new Campus(tenant.Id, $"PCA{s}", $"Gulberg {s}");
        var campusB = new Campus(tenant.Id, $"PCB{s}", $"DHA {s}");
        var deptA = new Department($"Gulberg Classes {s}", $"PCDA{s}", InstitutionType.School);
        deptA.SetTenantCampus(tenant.Id, campusA.Id);
        var deptB = new Department($"DHA Classes {s}", $"PCDB{s}", InstitutionType.School);
        deptB.SetTenantCampus(tenant.Id, campusB.Id);
        var program = new AcademicProgram($"Pastoral Program {s}", $"PCP{s}", deptA.Id, 10);
        var class7 = new Semester($"Class 7 {s}", DateTime.UtcNow.Date.AddDays(-30), DateTime.UtcNow.Date.AddDays(200));
        var maths = new Course($"Mathematics 7 {s}", $"PM7{s}".ToUpperInvariant(), 3, deptA.Id);
        var mathsB = new Course($"Mathematics 7B {s}", $"PB7{s}".ToUpperInvariant(), 3, deptB.Id);

        User Person(string tag, string roleName, string name, Guid? campus)
        {
            var u = new User($"pc_{tag}_{s}", "integration-hash", role(roleName), $"pc_{tag}_{s}@tabsan.local", fullName: name);
            u.SetTenantCampus(tenant.Id, campus);
            return u;
        }
        var admin = Person("a", "Admin", "Mrs Admin", campusA.Id);
        var teacher = Person("t", "Faculty", "Mr Siddiqui", campusA.Id);
        var counsellor = Person("c", "Faculty", "Mrs Counsellor", campusA.Id);
        var ali = Person("ali", "Student", "Ali Raza", campusA.Id);
        var zoya = Person("zoya", "Student", "Zoya Raza", campusB.Id);
        var mum = Person("mum", "Parent", "Nadia Raza", campusA.Id);
        var dad = Person("dad", "Parent", "Kamran Raza", campusA.Id);
        var aliProfile = new StudentProfile(ali.Id, $"PC-1-{s}", program.Id, deptA.Id, DateTime.UtcNow.Date);
        var zoyaProfile = new StudentProfile(zoya.Id, $"PC-2-{s}", program.Id, deptB.Id, DateTime.UtcNow.Date);
        var offeringA = new CourseOffering(maths.Id, class7.Id, 40, teacher.Id);
        offeringA.SetInstitutionScope(InstitutionType.School, tenant.Id, campusA.Id);
        var offeringB = new CourseOffering(mathsB.Id, class7.Id, 40);
        offeringB.SetInstitutionScope(InstitutionType.School, tenant.Id, campusB.Id);

        db.AddRange(tenant, campusA, campusB, deptA, deptB, program, class7, maths, mathsB, admin, teacher, counsellor, ali, zoya, mum, dad, aliProfile, zoyaProfile,
            offeringA, offeringB, new Enrollment(aliProfile.Id, offeringA.Id), new Enrollment(zoyaProfile.Id, offeringB.Id),
            new ParentStudentLink(mum.Id, aliProfile.Id, "Mother"), new ParentStudentLink(mum.Id, zoyaProfile.Id, "Mother"),
            new ParentStudentLink(dad.Id, aliProfile.Id, "Father"), new AdminDepartmentAssignment(admin.Id, deptA.Id), new AdminDepartmentAssignment(admin.Id, deptB.Id));
        await db.SaveChangesAsync();
        return new Seed(tenant.Id, campusA.Id, campusB.Id, admin.Id, teacher.Id, counsellor.Id, ali.Id, aliProfile.Id, zoyaProfile.Id, mum.Id, dad.Id);
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"{(int)response.StatusCode}: {text}");
        return string.IsNullOrWhiteSpace(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
    }

    private async Task<List<Guid>> NotifiedAsync(string title)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await (from n in db.Notifications
                      join r in db.NotificationRecipients on n.Id equals r.NotificationId
                      where n.Title == title && n.Type == NotificationType.Pastoral
                      select r.RecipientUserId).ToListAsync();
    }

    private static object NoteFor(Guid student, string tone, string summary, string kind = "Behaviour", string severity = "Low", bool confidential = false,
        bool parents = false, string? category = null)
        => new { studentProfileIds = new[] { student }, kind, tone, severity, category, summary, confidential, shareWithParents = parents };

    private static int Records(JsonElement list, Guid student)
        => list.EnumerateArray().Single(c => c.GetProperty("studentProfileId").GetGuid() == student).GetProperty("records").GetArrayLength();

    [Fact]
    public async Task Notes_ReachTheRightPeople_ConfidentialNotesStayWithTheTeam()
    {
        var seed = await SeedAsync();
        var admin = Client("Admin", seed.AdminId);
        var teacher = Client("Faculty", seed.TeacherId);
        var counsellor = Client("Faculty", seed.CounsellorId);
        var mum = Client("Parent", seed.MumId);

        await Json(await admin.PostAsJsonAsync("api/v1/pastoral/team", new { userId = seed.CounsellorId, title = "School counsellor", seesConfidential = true }),
            HttpStatusCode.Created);
        Assert.Contains(seed.CounsellorId, await NotifiedAsync("You are on the wellbeing team"));

        // A teacher finds and writes about the students they teach only.
        var mine = await Json(await teacher.GetAsync("api/v1/pastoral/students"));
        Assert.Equal([seed.AliProfile], mine.EnumerateArray().Select(x => x.GetProperty("id").GetGuid()));
        var praise = await Json(await teacher.PostAsJsonAsync("api/v1/pastoral/records",
            NoteFor(seed.AliProfile, "Positive", "Helped a new classmate settle in", parents: true, category: "Helping others")), HttpStatusCode.Created);
        var praiseId = praise.GetProperty("recordIds")[0].GetGuid();
        var refused = await Json(await teacher.PostAsJsonAsync("api/v1/pastoral/records", NoteFor(seed.ZoyaProfile, "Positive", "Well done")), HttpStatusCode.Forbidden);
        Assert.Equal("You do not teach or look after this student.", refused.GetProperty("message").GetString());

        // A serious concern tells the wellbeing team; the parents heard the good news.
        var concern = await Json(await teacher.PostAsJsonAsync("api/v1/pastoral/records",
            NoteFor(seed.AliProfile, "Concern", "Crying at break", "Wellbeing", "High")), HttpStatusCode.Created);
        var concernId = concern.GetProperty("recordIds")[0].GetGuid();
        Assert.Equal([seed.CounsellorId], await NotifiedAsync("Serious concern"));
        Assert.Equal(new[] { seed.MumId, seed.DadId }.Order(), (await NotifiedAsync("Good news from school")).Order());

        await Json(await counsellor.PostAsJsonAsync("api/v1/pastoral/records",
            NoteFor(seed.AliProfile, "Concern", "Spoke about difficulties at home", "Wellbeing", confidential: true)), HttpStatusCode.Created);
        var forTeacher = await Json(await teacher.GetAsync($"api/v1/pastoral/students/{seed.AliProfile}"));
        Assert.Equal(2, forTeacher.GetProperty("records").GetArrayLength());
        Assert.False(forTeacher.GetProperty("canPlan").GetBoolean());
        Assert.False(forTeacher.TryGetProperty("checkIns", out var checkIns) && checkIns.ValueKind != JsonValueKind.Null); // the API leaves out empty fields
        var forCounsellor = await Json(await counsellor.GetAsync($"api/v1/pastoral/students/{seed.AliProfile}"));
        Assert.Equal(3, forCounsellor.GetProperty("records").GetArrayLength());
        Assert.Equal(2, (await Json(await admin.GetAsync($"api/v1/pastoral/students/{seed.AliProfile}"))).GetProperty("records").GetArrayLength());

        // Families see what was shared with them.
        var family = await Json(await mum.GetAsync("api/v1/pastoral/family"));
        Assert.Equal(1, Records(family, seed.AliProfile));
        Assert.Equal(0, Records(family, seed.ZoyaProfile));
        Assert.Equal(0, (await Json(await Client("Student", seed.AliId).GetAsync("api/v1/pastoral/mine"))).GetProperty("records").GetArrayLength());
        await Json(await mum.GetAsync($"api/v1/pastoral/students/{seed.AliProfile}"), HttpStatusCode.Forbidden);

        // Passing the concern to the counsellor to follow up, who adds a note and finishes it.
        var changed = await Json(await teacher.PutAsJsonAsync($"api/v1/pastoral/records/{concernId}", new
        {
            kind = "Wellbeing", tone = "Concern", severity = "High", summary = "Crying at break", followUpUserId = seed.CounsellorId,
            followUpBy = DateTime.UtcNow.Date.AddDays(2)
        }));
        Assert.Equal("Open", changed.GetProperty("followUp").GetString());
        Assert.Equal([seed.CounsellorId], await NotifiedAsync("A note for you to follow up"));
        Assert.Single(await NotifiedAsync("Serious concern")); // still serious: not told again
        await Json(await counsellor.PostAsJsonAsync($"api/v1/pastoral/records/{concernId}/notes", new { text = "Met Ali and his mother." }));
        Assert.Equal("Done", (await Json(await counsellor.PostAsync($"api/v1/pastoral/records/{concernId}/follow-up/done", null))).GetProperty("followUp").GetString());

        // A note written in error: removed with a reason, gone for the family, kept for the team.
        await Json(await teacher.PostAsJsonAsync($"api/v1/pastoral/records/{praiseId}/remove", new { text = "" }), HttpStatusCode.BadRequest);
        await Json(await teacher.PostAsJsonAsync($"api/v1/pastoral/records/{praiseId}/remove", new { text = "Wrong student" }));
        Assert.Equal(0, Records(await Json(await mum.GetAsync("api/v1/pastoral/family")), seed.AliProfile));
        var kept = (await Json(await counsellor.GetAsync($"api/v1/pastoral/students/{seed.AliProfile}"))).GetProperty("records").EnumerateArray()
            .Single(r => r.GetProperty("id").GetGuid() == praiseId);
        Assert.True(kept.GetProperty("isRemoved").GetBoolean());
        Assert.Equal("Wrong student", kept.GetProperty("removedReason").GetString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await db.PastoralNotes.AnyAsync(n => n.RecordId == concernId));
        Assert.Equal(PastoralVisibility.Confidential, (await db.PastoralRecords.SingleAsync(r => r.StudentProfileId == seed.AliProfile && r.AuthorUserId == seed.CounsellorId)).Visibility);
    }

    [Fact]
    public async Task CheckInsPlansOverviewAndSetup_WorkTogether()
    {
        var seed = await SeedAsync();
        var admin = Client("Admin", seed.AdminId);
        var teacher = Client("Faculty", seed.TeacherId);
        var counsellor = Client("Faculty", seed.CounsellorId);
        var ali = Client("Student", seed.AliId);

        await Json(await admin.PostAsJsonAsync("api/v1/pastoral/team", new { userId = seed.CounsellorId, title = "School counsellor", campusId = seed.CampusA, seesConfidential = true }),
            HttpStatusCode.Created);
        await Json(await teacher.GetAsync("api/v1/pastoral/setup"), HttpStatusCode.Forbidden);
        var setup = await Json(await admin.GetAsync("api/v1/pastoral/setup"));
        Assert.Equal(1, setup.GetProperty("team").GetArrayLength());
        Assert.Equal(PastoralCategory.Defaults.Count, setup.GetProperty("categories").GetArrayLength());
        Assert.Equal(2, setup.GetProperty("places").GetProperty("campuses").GetArrayLength());

        // Check-ins: off until the school switches them on; asking to talk tells the team.
        await Json(await ali.PostAsJsonAsync("api/v1/pastoral/check-ins", new { mood = 4 }), HttpStatusCode.Forbidden);
        await Json(await admin.PutAsJsonAsync("api/v1/pastoral/settings", new { checkInsEnabled = true, concernAlertCount = 3, concernAlertDays = 14 }));
        var checkIn = await Json(await ali.PostAsJsonAsync("api/v1/pastoral/check-ins", new { mood = 2, note = "Worried about the exams", wantsToTalk = true }),
            HttpStatusCode.Created);
        Assert.Equal([seed.CounsellorId], await NotifiedAsync("A student would like to talk"));
        var overview = await Json(await counsellor.GetAsync("api/v1/pastoral/overview"));
        Assert.Equal(checkIn.GetProperty("id").GetGuid(), overview.GetProperty("needsAttention")[0].GetProperty("id").GetGuid());
        Assert.False((await Json(await teacher.GetAsync("api/v1/pastoral/overview"))).GetProperty("me").GetProperty("seesCheckIns").GetBoolean());
        await Json(await teacher.PostAsJsonAsync($"api/v1/pastoral/check-ins/{checkIn.GetProperty("id").GetGuid()}/seen", new { text = "x" }), HttpStatusCode.NotFound);
        await Json(await counsellor.PostAsJsonAsync($"api/v1/pastoral/check-ins/{checkIn.GetProperty("id").GetGuid()}/seen", new { text = "Met at lunch." }));
        Assert.Equal(0, (await Json(await counsellor.GetAsync("api/v1/pastoral/overview"))).GetProperty("needsAttention").GetArrayLength());
        var mine = await Json(await ali.GetAsync("api/v1/pastoral/mine"));
        Assert.True(mine.GetProperty("checkInsEnabled").GetBoolean());
        Assert.Equal("Mrs Counsellor", mine.GetProperty("latest").GetProperty("acknowledgedBy").GetString());

        // A support plan from the team: the teacher is told and sees what to do; the parents see it too.
        await Json(await teacher.PostAsJsonAsync("api/v1/pastoral/plans", new { studentProfileId = seed.AliProfile, title = "x", strategies = "y" }), HttpStatusCode.Forbidden);
        await Json(await counsellor.PostAsJsonAsync("api/v1/pastoral/plans", new
        {
            studentProfileId = seed.AliProfile, kind = "LearningAdjustments", title = "Extra time in tests", strategies = "Give 25% extra time; seat him near the front.",
            startsOn = DateTime.UtcNow.Date, reviewOn = DateTime.UtcNow.Date.AddDays(60), shareWithParents = true
        }), HttpStatusCode.Created);
        Assert.Equal([seed.TeacherId], await NotifiedAsync("New support plan"));
        var page = await Json(await teacher.GetAsync($"api/v1/pastoral/students/{seed.AliProfile}"));
        Assert.Equal("Give 25% extra time; seat him near the front.", page.GetProperty("plans")[0].GetProperty("strategies").GetString());
        var family = await Json(await Client("Parent", seed.DadId).GetAsync("api/v1/pastoral/family"));
        Assert.Equal("Extra time in tests", family[0].GetProperty("plans")[0].GetProperty("title").GetString());

        // The overview counts the period's notes by class and campus.
        await Json(await teacher.PostAsJsonAsync("api/v1/pastoral/records", NoteFor(seed.AliProfile, "Positive", "Excellent work", category: "Excellent effort")),
            HttpStatusCode.Created);
        var counted = await Json(await counsellor.GetAsync($"api/v1/pastoral/overview?campusId={seed.CampusA}"));
        Assert.Equal(1, counted.GetProperty("totals").GetProperty("positive").GetInt32());
        Assert.Equal(1, counted.GetProperty("totals").GetProperty("activePlans").GetInt32());
        Assert.Equal(seed.AliProfile, counted.GetProperty("students")[0].GetProperty("student").GetProperty("id").GetGuid());
        Assert.Equal(0, (await Json(await counsellor.GetAsync($"api/v1/pastoral/overview?campusId={seed.CampusB}"))).GetProperty("students").GetArrayLength());
    }
}
