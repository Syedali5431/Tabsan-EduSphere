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
using Tabsan.EduSphere.Domain.Tenancy;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// School news, events and forms through the real API and database: who each post reaches (campus, class, parents through
/// each child, staff), the people counted and told, a teacher limited to their class, an event in the calendar with places
/// and replies, a parent consent form answered per child by either parent, tracking, reminders and the CSV download.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class CommunityIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public CommunityIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    private sealed record Seed(Guid Tenant, Guid CampusA, Guid CampusB, Guid Class7, Guid OfferingA, Guid AdminId, Guid TeacherId, Guid AliId, Guid AliProfile,
        Guid ZoyaId, Guid ZoyaProfile, Guid MumId, Guid DadId);

    private HttpClient Client(string role, Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(role, userId.ToString()));
        return client;
    }

    private async Task<Seed> SeedAsync()
    {
        var super = Client("SuperAdmin", Guid.NewGuid());
        var activated = await super.PostAsync("api/v1/module/notifications/activate", null);
        Assert.True(activated.IsSuccessStatusCode, await activated.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var s = Guid.NewGuid().ToString("N")[..6];
        var role = (string name) => db.Roles.First(r => r.Name == name).Id;
        var tenant = new Tenant($"CMT{s}", $"Community School {s}");
        var campusA = new Campus(tenant.Id, $"CMA{s}", $"Gulberg {s}");
        var campusB = new Campus(tenant.Id, $"CMB{s}", $"DHA {s}");
        var deptA = new Department($"Gulberg Classes {s}", $"CMDA{s}", InstitutionType.School);
        deptA.SetTenantCampus(tenant.Id, campusA.Id);
        var deptB = new Department($"DHA Classes {s}", $"CMDB{s}", InstitutionType.School);
        deptB.SetTenantCampus(tenant.Id, campusB.Id);
        var program = new AcademicProgram($"Community Program {s}", $"CMP{s}", deptA.Id, 10);
        var class7 = new Semester($"Class 7 {s}", DateTime.UtcNow.Date.AddDays(-30), DateTime.UtcNow.Date.AddDays(200));
        var maths = new Course($"Mathematics 7 {s}", $"CM7{s}".ToUpperInvariant(), 3, deptA.Id);
        var mathsB = new Course($"Mathematics 7B {s}", $"CB7{s}".ToUpperInvariant(), 3, deptB.Id);

        User Person(string tag, string roleName, string name, Guid? campus)
        {
            var u = new User($"cm_{tag}_{s}", "integration-hash", role(roleName), $"cm_{tag}_{s}@tabsan.local", fullName: name);
            u.SetTenantCampus(tenant.Id, campus);
            return u;
        }
        var admin = Person("a", "Admin", "Mrs Admin", campusA.Id); // filed under one campus, looks after both (its departments)
        var teacher = Person("t", "Faculty", "Mr Siddiqui", campusA.Id);
        var ali = Person("ali", "Student", "Ali Raza", campusA.Id);
        var zoya = Person("zoya", "Student", "Zoya Raza", campusB.Id);
        var mum = Person("mum", "Parent", "Nadia Raza", campusA.Id);
        var dad = Person("dad", "Parent", "Kamran Raza", campusA.Id);
        var aliProfile = new StudentProfile(ali.Id, $"CM-1-{s}", program.Id, deptA.Id, DateTime.UtcNow.Date);
        var zoyaProfile = new StudentProfile(zoya.Id, $"CM-2-{s}", program.Id, deptB.Id, DateTime.UtcNow.Date);
        var offeringA = new CourseOffering(maths.Id, class7.Id, 40, teacher.Id);
        offeringA.SetInstitutionScope(InstitutionType.School, tenant.Id, campusA.Id);
        var offeringB = new CourseOffering(mathsB.Id, class7.Id, 40);
        offeringB.SetInstitutionScope(InstitutionType.School, tenant.Id, campusB.Id);

        db.AddRange(tenant, campusA, campusB, deptA, deptB, program, class7, maths, mathsB, admin, teacher, ali, zoya, mum, dad, aliProfile, zoyaProfile,
            offeringA, offeringB, new Enrollment(aliProfile.Id, offeringA.Id), new Enrollment(zoyaProfile.Id, offeringB.Id),
            new ParentStudentLink(mum.Id, aliProfile.Id, "Mother"), new ParentStudentLink(mum.Id, zoyaProfile.Id, "Mother"),
            new ParentStudentLink(dad.Id, aliProfile.Id, "Father"), new AdminDepartmentAssignment(admin.Id, deptA.Id), new AdminDepartmentAssignment(admin.Id, deptB.Id));
        await db.SaveChangesAsync();
        return new Seed(tenant.Id, campusA.Id, campusB.Id, class7.Id, offeringA.Id, admin.Id, teacher.Id, ali.Id, aliProfile.Id, zoya.Id, zoyaProfile.Id, mum.Id, dad.Id);
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"{(int)response.StatusCode}: {text}");
        return string.IsNullOrWhiteSpace(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
    }

    private static List<string> Titles(JsonElement list) => list.EnumerateArray().Select(x => x.GetProperty("title").GetString()!).OrderBy(t => t).ToList();

    private async Task<List<Guid>> NotifiedAsync(string title)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await (from n in db.Notifications
                      join r in db.NotificationRecipients on n.Id equals r.NotificationId
                      where n.Title == title && n.Type == NotificationType.SchoolNews
                      select r.RecipientUserId).ToListAsync();
    }

    [Fact]
    public async Task News_ReachesCampusClassAndParents_TeachersWriteForTheirClass()
    {
        var seed = await SeedAsync();
        var admin = Client("Admin", seed.AdminId);
        var teacher = Client("Faculty", seed.TeacherId);

        var options = await Json(await admin.GetAsync("api/v1/community/audiences"));
        Assert.True(options.GetProperty("wholeSchool").GetBoolean());
        Assert.Equal(2, options.GetProperty("campuses").GetArrayLength());
        var teacherOptions = await Json(await teacher.GetAsync("api/v1/community/audiences"));
        Assert.False(teacherOptions.GetProperty("wholeSchool").GetBoolean());
        Assert.Equal([seed.OfferingA], teacherOptions.GetProperty("classes").EnumerateArray().Select(c => c.GetProperty("id").GetGuid()));

        var sports = await Json(await admin.PostAsJsonAsync("api/v1/community/news", new
        {
            title = "Sports day", body = "Friday on the big field.", audience = new { campusId = seed.CampusA }, pinned = true, publish = true
        }), HttpStatusCode.Created);
        await Json(await teacher.PostAsJsonAsync("api/v1/community/news", new
        {
            title = "Maths test on Friday", body = "Chapters 1 and 2.", audience = new { courseOfferingId = seed.OfferingA, roles = new[] { "Students", "Parents" } }, publish = true
        }), HttpStatusCode.Created);
        var refused = await Json(await teacher.PostAsJsonAsync("api/v1/community/news", new { title = "Hi", body = "x", audience = new { campusId = seed.CampusA }, publish = true }),
            HttpStatusCode.BadRequest);
        Assert.Equal("Teachers write to their own classes: choose one of your classes.", refused.GetProperty("message").GetString());

        Assert.Equal(["Maths test on Friday", "Sports day"], Titles(await Json(await Client("Student", seed.AliId).GetAsync("api/v1/community/news"))));
        Assert.Empty(Titles(await Json(await Client("Student", seed.ZoyaId).GetAsync("api/v1/community/news"))));
        Assert.Equal(["Maths test on Friday", "Sports day"], Titles(await Json(await Client("Parent", seed.MumId).GetAsync("api/v1/community/news"))));
        Assert.Equal(["Sports day"], Titles(await Json(await teacher.GetAsync("api/v1/community/news")))); // the class post is for students and parents

        // Everyone it reaches was told, the author was not.
        var told = await NotifiedAsync("Sports day");
        Assert.Equal(new[] { seed.AliId, seed.MumId, seed.DadId, seed.TeacherId }.OrderBy(x => x), told.OrderBy(x => x));

        var id = sports.GetProperty("id").GetGuid();
        await Json(await Client("Student", seed.AliId).GetAsync($"api/v1/community/news/{id}"));
        await Json(await Client("Parent", seed.DadId).GetAsync($"api/v1/community/news/{id}"));
        var managed = await Json(await admin.GetAsync($"api/v1/community/news/{id}"));
        Assert.Equal((3, 5), (managed.GetProperty("seenBy").GetInt32(), managed.GetProperty("audienceSize").GetInt32())); // read by Ali, Dad and the admin; for Ali, two parents, the teacher and the admin
        Assert.Equal(HttpStatusCode.NotFound, (await Client("Student", seed.ZoyaId).GetAsync($"api/v1/community/news/{id}")).StatusCode);
        var feed = await Json(await Client("Student", seed.AliId).GetAsync("api/v1/community/news"));
        Assert.False(feed.EnumerateArray().Single(n => n.GetProperty("id").GetGuid() == id).GetProperty("isNew").GetBoolean());
    }

    [Fact]
    public async Task EventInTheCalendar_RepliesWithPlaces_AndAParentConsentFormPerChild()
    {
        var seed = await SeedAsync();
        var admin = Client("Admin", seed.AdminId);
        var mum = Client("Parent", seed.MumId);
        var dad = Client("Parent", seed.DadId);

        // The consent form: parents of Class 7 on both campuses.
        var form = await Json(await admin.PostAsJsonAsync("api/v1/community/forms", new
        {
            title = "Zoo trip consent", description = "Friday's trip.", dueAt = DateTime.UtcNow.AddDays(5), audience = new { semesterId = seed.Class7, roles = new[] { "Parents" } },
            fields = new object[]
            {
                new { type = "YesNo", label = "May your child go?", required = true },
                new { type = "SingleChoice", label = "Lunch", required = true, options = new[] { "Packed lunch", "School lunch" } },
                new { type = "Signature", label = "Your full name", required = true }
            },
            open = true
        }), HttpStatusCode.Created);
        var formId = form.GetProperty("id").GetGuid();
        var fields = form.GetProperty("fields").EnumerateArray().ToDictionary(f => f.GetProperty("label").GetString()!, f => f.GetProperty("id").GetGuid());
        Assert.Equal(new[] { seed.MumId, seed.DadId }.OrderBy(x => x), (await NotifiedAsync("Please fill in: Zoo trip consent")).Distinct().OrderBy(x => x));

        // The trip, with 3 places and the form.
        var start = DateTime.UtcNow.Date.AddDays(6).AddHours(4);
        var trip = await Json(await admin.PostAsJsonAsync("api/v1/community/events", new
        {
            title = "Zoo trip", location = "Lahore Zoo", category = "Excursion", startsAt = start, endsAt = start.AddHours(5), allDay = false,
            repliesOpen = true, capacity = 3, maxGuests = 1, formId, audience = new { semesterId = seed.Class7 }, publish = true
        }), HttpStatusCode.Created);
        var tripId = trip.GetProperty("id").GetGuid();

        var calendar = await Json(await Client("Student", seed.AliId).GetAsync($"api/v1/calendar/events?from={DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}&to={DateTime.UtcNow.AddDays(14):yyyy-MM-ddTHH:mm:ssZ}"));
        var entry = calendar.GetProperty("events").EnumerateArray().Single(e => e.GetProperty("kind").GetString() == "Event");
        Assert.Equal(("Zoo trip", "Lahore Zoo"), (entry.GetProperty("title").GetString(), entry.GetProperty("note").GetString()));

        var mumReply = await Json(await mum.PostAsJsonAsync($"api/v1/community/events/{tripId}/reply", new { answer = "Going", guests = 1 }));
        Assert.Equal(2, mumReply.GetProperty("counts").GetProperty("places").GetInt32());
        var full = await Json(await dad.PostAsJsonAsync($"api/v1/community/events/{tripId}/reply", new { answer = "Going", guests = 1 }), HttpStatusCode.Conflict);
        Assert.Equal("Only 1 place is left.", full.GetProperty("message").GetString());
        await Json(await dad.PostAsJsonAsync($"api/v1/community/events/{tripId}/reply", new { answer = "Going", guests = 0 }));
        var replies = await Json(await admin.GetAsync($"api/v1/community/events/{tripId}/replies"));
        Assert.Equal(["Kamran Raza", "Nadia Raza"], replies.GetProperty("replies").EnumerateArray().Select(r => r.GetProperty("name").GetString()).OrderBy(n => n));

        // Mum answers for both children; Dad sees Ali's answer and changes it.
        var mumForms = await Json(await mum.GetAsync("api/v1/community/forms"));
        Assert.Equal(["Ali Raza", "Zoya Raza"], mumForms.EnumerateArray().Select(f => f.GetProperty("studentName").GetString()).OrderBy(n => n));
        object Answers(string go, string lunch) => new Dictionary<string, string>
        {
            [fields["May your child go?"].ToString()] = go, [fields["Lunch"].ToString()] = lunch, [fields["Your full name"].ToString()] = "Nadia Raza"
        };
        await Json(await mum.PostAsJsonAsync($"api/v1/community/forms/{formId}/responses", new { studentProfileId = seed.AliProfile, answers = Answers("Yes", "Packed lunch") }));
        var notYours = await Json(await dad.PostAsJsonAsync($"api/v1/community/forms/{formId}/responses", new { studentProfileId = seed.ZoyaProfile, answers = Answers("Yes", "Packed lunch") }),
            HttpStatusCode.BadRequest);
        Assert.Equal("Choose which child you are answering for.", notYours.GetProperty("message").GetString());
        var dadView = await Json(await dad.GetAsync($"api/v1/community/forms/{formId}"));
        Assert.Equal(("Ali Raza", "Nadia Raza"), (dadView.GetProperty("studentName").GetString(), dadView.GetProperty("answeredBy").GetString()));
        await Json(await dad.PostAsJsonAsync($"api/v1/community/forms/{formId}/responses", new { studentProfileId = seed.AliProfile, answers = Answers("No", "School lunch") }));

        var tracking = await Json(await admin.GetAsync($"api/v1/community/forms/{formId}/tracking"));
        Assert.Equal((2, 1), (tracking.GetProperty("expected").GetInt32(), tracking.GetProperty("answered").GetInt32()));
        var aliRow = tracking.GetProperty("rows").EnumerateArray().Single(r => r.GetProperty("name").GetString() == "Ali Raza");
        Assert.Equal("Kamran Raza", aliRow.GetProperty("answeredBy").GetString());
        var remind = await Json(await admin.PostAsync($"api/v1/community/forms/{formId}/remind", null));
        Assert.Equal(1, remind.GetProperty("reminded").GetInt32());              // Zoya's mother
        Assert.Equal([seed.MumId], await NotifiedAsync("Reminder: Zoo trip consent"));

        var csv = await admin.GetAsync($"api/v1/community/forms/{formId}/export");
        Assert.Equal("text/csv", csv.Content.Headers.ContentType!.MediaType);
        var text = (await csv.Content.ReadAsStringAsync()).TrimStart('﻿');
        Assert.Contains("Ali Raza,", text);
        Assert.Contains(",No,School lunch,Nadia Raza", text);

        // Questions are locked once answered.
        var locked = await admin.PutAsJsonAsync($"api/v1/community/forms/{formId}", new
        {
            title = "Zoo trip consent", audience = new { semesterId = seed.Class7, roles = new[] { "Parents" } }, fields = new object[] { new { type = "YesNo", label = "Different?", required = true } }, open = true
        });
        Assert.Equal(HttpStatusCode.Conflict, locked.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client("Student", seed.AliId).GetAsync($"api/v1/community/forms/{formId}/tracking")).StatusCode);
    }
}
