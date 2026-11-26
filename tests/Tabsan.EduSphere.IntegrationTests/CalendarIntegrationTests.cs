using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Application.Calendar;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Calendar;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.LiveClasses;
using Tabsan.EduSphere.Domain.Notifications;
using Tabsan.EduSphere.Domain.Quizzes;
using Tabsan.EduSphere.Domain.Tenancy;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// The calendar through the real API and database: a student's entries (their own "Assign to" dates, work handed in, live
/// classes, key dates; no drafts), a teacher's (drafts, hand-ins to mark), a parent's (their children only), the private
/// calendar link read without signing in (replaced and turned off), and due-date reminders (only to students who have not
/// handed in, once per due date, again when the date moves).
/// </summary>
[Collection(EduSphereCollection.Name)]
public class CalendarIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public CalendarIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    private sealed record Seed(Guid TeacherUserId, Guid StudentUserId, Guid StudentProfileId, Guid OtherUserId, Guid OutsiderProfileId,
        Guid ParentUserId, Guid EssayId, Guid DraftId, Guid HandedInId, Guid QuizId, Guid OverrideId, DateTime EssayDue, DateTime OwnEssayDue,
        DateTime OwnQuizClose, DateTime ClassStart, string StudentName, string Code);

    private HttpClient Client(string role, Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(role, userId.ToString()));
        return client;
    }

    private async Task ActivateModulesAsync()
    {
        var admin = Client("SuperAdmin", Guid.NewGuid());
        foreach (var key in new[] { "assignments", "quizzes", "live_classes" })
        {
            var response = await admin.PostAsync($"api/v1/module/{key}/activate", null);
            Assert.True(response.IsSuccessStatusCode, $"{key}: {await response.Content.ReadAsStringAsync()}");
        }
    }

    private async Task<Seed> SeedAsync()
    {
        await ActivateModulesAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var role = (string name) => db.Roles.First(r => r.Name == name).Id;
        var tenant = new Tenant($"CLT{suffix}", $"Calendar Tenant {suffix}");
        var campus = new Campus(tenant.Id, $"CLK{suffix}", $"Calendar Campus {suffix}");
        var department = new Department($"Calendar Dept {suffix}", $"CLD{suffix}", InstitutionType.School);
        department.SetTenantCampus(tenant.Id, campus.Id);
        var program = new AcademicProgram($"Calendar Program {suffix}", $"CLP{suffix}", department.Id, 10);
        var semester = new Semester($"Calendar Term {suffix}", DateTime.UtcNow.Date.AddDays(-20), DateTime.UtcNow.Date.AddDays(100));
        var code = $"CL{suffix}".ToUpperInvariant();
        var course = new Course($"Calendar English {suffix}", code, 3, department.Id);
        var teacher = new User($"cl_t_{suffix}", "integration-hash", role("Faculty"), $"cl_t_{suffix}@tabsan.local");
        var offering = new CourseOffering(course.Id, semester.Id, 40, teacher.Id);
        offering.SetInstitutionScope(InstitutionType.School, tenant.Id, campus.Id);
        var studentName = $"Ayesha {suffix}";
        var student = new User($"cl_s_{suffix}", "integration-hash", role("Student"), $"cl_s_{suffix}@tabsan.local");
        student.UpdateFullName(studentName);
        var profile = new StudentProfile(student.Id, $"CL-1-{suffix}", program.Id, department.Id, DateTime.UtcNow.Date);
        var other = new User($"cl_o_{suffix}", "integration-hash", role("Student"), $"cl_o_{suffix}@tabsan.local");
        other.UpdateFullName($"Bilal {suffix}");
        var otherProfile = new StudentProfile(other.Id, $"CL-2-{suffix}", program.Id, department.Id, DateTime.UtcNow.Date);
        var outsider = new User($"cl_x_{suffix}", "integration-hash", role("Student"), $"cl_x_{suffix}@tabsan.local");
        var outsiderProfile = new StudentProfile(outsider.Id, $"CL-3-{suffix}", program.Id, department.Id, DateTime.UtcNow.Date);
        var parent = new User($"cl_p_{suffix}", "integration-hash", role("Parent"), $"cl_p_{suffix}@tabsan.local");

        var now = DateTime.UtcNow;
        var essayDue = Trim(now.AddHours(10));
        var ownEssayDue = Trim(now.AddHours(30)); // outside the 24-hour reminder window
        var essay = new Assignment(offering.Id, "Essay on rivers", "Two pages.", essayDue, 20);
        essay.Publish();
        var handedIn = new Assignment(offering.Id, "Map work", null, Trim(now.AddHours(5)), 10);
        handedIn.Publish();
        var draft = new Assignment(offering.Id, "Poem (draft)", null, Trim(now.AddDays(2)), 10);
        var ownQuizClose = Trim(now.AddHours(12));
        var quiz = new Quiz(offering.Id, "Week 3 quiz", teacher.Id, availableUntil: Trim(now.AddDays(3)));
        quiz.Publish();
        var classStart = Trim(now.AddDays(2));
        var session = new LiveSession(offering.Id, "Revision class", null, ConferenceProviderType.ExternalLink, classStart, 45, false, 0, null, teacher.Id, "m", "a");
        var keyDate = new AcademicDeadline(semester.Id, "Sports day", DateTime.SpecifyKind(now.Date.AddDays(3), DateTimeKind.Utc), 1, "Wear house colours");
        var essayOverride = new AssignmentOverride(essay.Id, profile.Id, null, ownEssayDue, null, null, "Extension");
        db.AddRange(tenant, campus, department, program, semester, course, teacher, offering, student, profile, other, otherProfile, outsider, outsiderProfile,
            parent, new Enrollment(profile.Id, offering.Id), new Enrollment(otherProfile.Id, offering.Id), essay, handedIn, draft, quiz, session, keyDate,
            essayOverride, new QuizAvailabilityOverride(quiz.Id, profile.Id, null, ownQuizClose, "Make-up sitting"),
            new AssignmentSubmission(handedIn.Id, profile.Id, null, "My map."),
            new ParentStudentLink(parent.Id, profile.Id, "Mother"));
        await db.SaveChangesAsync();
        return new Seed(teacher.Id, student.Id, profile.Id, other.Id, outsiderProfile.Id, parent.Id, essay.Id, draft.Id, handedIn.Id, quiz.Id,
            essayOverride.Id, essayDue, ownEssayDue, ownQuizClose, classStart, studentName, code);
    }

    /// <summary>SQL Server keeps datetime2 to 100 ns; whole seconds keep the comparisons simple.</summary>
    private static DateTime Trim(DateTime d) => new(d.Ticks - d.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);

    private static string Range(int fromDays, int toDays)
        => $"from={DateTime.UtcNow.AddDays(fromDays):yyyy-MM-ddTHH:mm:ssZ}&to={DateTime.UtcNow.AddDays(toDays):yyyy-MM-ddTHH:mm:ssZ}";

    private static async Task<List<JsonElement>> EventsAsync(HttpClient client, string query)
    {
        var response = await client.GetAsync("api/v1/calendar/events?" + query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("events").EnumerateArray().ToList();
    }

    private static JsonElement Find(List<JsonElement> events, string title)
        => events.Single(e => e.GetProperty("title").GetString() == title);

    private static DateTime Utc(JsonElement e, string name) => DateTime.SpecifyKind(e.GetProperty(name).GetDateTime(), DateTimeKind.Utc);

    [Fact]
    public async Task StudentCalendar_HasOwnDatesHandedInWorkClassesAndKeyDates_ButNoDrafts()
    {
        var seed = await SeedAsync();
        var events = await EventsAsync(Client("Student", seed.StudentUserId), Range(-1, 7));

        var essay = Find(events, "Essay on rivers");
        Assert.Equal(seed.OwnEssayDue, Utc(essay, "startsAt")); // their extension, not the class's date
        Assert.True(essay.GetProperty("ownDates").GetBoolean());
        Assert.False(essay.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.String); // still to do (nulls are left out)
        Assert.Equal(seed.Code, essay.GetProperty("courseCode").GetString());
        Assert.Equal("Done", Find(events, "Map work").GetProperty("status").GetString());
        Assert.DoesNotContain(events, e => e.GetProperty("title").GetString() == "Poem (draft)");
        var quiz = Find(events, "Week 3 quiz");
        Assert.Equal(("Quiz", seed.OwnQuizClose), (quiz.GetProperty("kind").GetString(), Utc(quiz, "startsAt")));
        Assert.Equal($"/Portal/TakeQuiz?quizId={seed.QuizId}", quiz.GetProperty("url").GetString());
        var live = Find(events, "Revision class");
        Assert.Equal((seed.ClassStart, seed.ClassStart.AddMinutes(45)), (Utc(live, "startsAt"), Utc(live, "endsAt")));
        var keyDate = Find(events, "Sports day");
        Assert.True(keyDate.GetProperty("allDay").GetBoolean());
        Assert.Equal("Wear house colours", keyDate.GetProperty("note").GetString());

        var outside = await EventsAsync(Client("Student", seed.StudentUserId), Range(5, 20));
        Assert.DoesNotContain(outside, e => e.GetProperty("title").GetString() == "Essay on rivers");
        var backwards = await Client("Student", seed.StudentUserId).GetAsync("api/v1/calendar/events?" + Range(3, 1));
        Assert.Equal(HttpStatusCode.BadRequest, backwards.StatusCode);
    }

    [Fact]
    public async Task TeacherAndParentCalendars_ShowTheirOwnView_AndParentsOnlyTheirChildren()
    {
        var seed = await SeedAsync();
        var teacher = await EventsAsync(Client("Faculty", seed.TeacherUserId), Range(-1, 7));
        Assert.Equal("Draft", Find(teacher, "Poem (draft)").GetProperty("status").GetString());
        Assert.Equal(1, Find(teacher, "Map work").GetProperty("toMark").GetInt32());
        var essay = Find(teacher, "Essay on rivers");
        Assert.Equal(seed.EssayDue, Utc(essay, "startsAt")); // the class's date
        Assert.True(essay.GetProperty("someHaveOwnDates").GetBoolean());

        var parent = Client("Parent", seed.ParentUserId);
        var all = await EventsAsync(parent, Range(-1, 7));
        // Every entry of a child names the child (key dates are the school's, for everyone).
        Assert.All(all.Where(e => e.GetProperty("kind").GetString() != "KeyDate"),
            e => Assert.Equal(seed.StudentName, e.GetProperty("studentName").GetString()));
        Assert.Contains(all, e => e.GetProperty("title").GetString() == "Sports day");
        var one = await EventsAsync(parent, Range(-1, 7) + $"&studentProfileId={seed.StudentProfileId}");
        Assert.Equal(seed.OwnEssayDue, Utc(Find(one, "Essay on rivers"), "startsAt"));
        var notTheirs = await parent.GetAsync("api/v1/calendar/events?" + Range(-1, 7) + $"&studentProfileId={seed.OutsiderProfileId}");
        Assert.Equal(HttpStatusCode.Forbidden, notTheirs.StatusCode);
    }

    [Fact]
    public async Task CalendarLink_OpensWithoutSigningIn_AndStopsWhenReplacedOrTurnedOff()
    {
        var seed = await SeedAsync();
        var student = Client("Student", seed.StudentUserId);
        Assert.Equal(HttpStatusCode.NoContent, (await student.GetAsync("api/v1/calendar/feed")).StatusCode);
        var created = await (await student.PostAsync("api/v1/calendar/feed", null)).Content.ReadFromJsonAsync<JsonElement>();
        var token = created.GetProperty("token").GetString()!;
        Assert.Equal($"/api/v1/calendar/feed/{token}.ics", created.GetProperty("path").GetString());
        Assert.Equal(token, (await student.GetFromJsonAsync<JsonElement>("api/v1/calendar/feed")).GetProperty("token").GetString());

        var anonymous = _factory.CreateClient();
        var feed = await anonymous.GetAsync($"api/v1/calendar/feed/{token}.ics");
        Assert.Equal(HttpStatusCode.OK, feed.StatusCode);
        Assert.Equal("text/calendar", feed.Content.Headers.ContentType?.MediaType);
        var ics = (await feed.Content.ReadAsStringAsync()).Replace("\r\n ", "");
        Assert.StartsWith("BEGIN:VCALENDAR", ics);
        Assert.Contains($"X-WR-CALNAME:EduSphere — {seed.StudentName}", ics);
        Assert.Contains($"SUMMARY:Due: Essay on rivers ({seed.Code})", ics);
        Assert.Contains($"SUMMARY:Due: Map work ({seed.Code}) — handed in", ics);
        Assert.Contains($"DTSTART:{seed.OwnEssayDue:yyyyMMdd'T'HHmmss'Z'}", ics);
        Assert.DoesNotContain("Poem (draft)", ics);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.NotNull((await db.CalendarFeedTokens.AsNoTracking().SingleAsync(t => t.UserId == seed.StudentUserId)).LastUsedAt);
        }

        var replaced = (await (await student.PostAsync("api/v1/calendar/feed", null)).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        Assert.NotEqual(token, replaced);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"api/v1/calendar/feed/{token}.ics")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync($"api/v1/calendar/feed/{replaced}.ics")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync("api/v1/calendar/feed/not-a-real-link.ics")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await student.DeleteAsync("api/v1/calendar/feed")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"api/v1/calendar/feed/{replaced}.ics")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await student.GetAsync("api/v1/calendar/feed")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("api/v1/calendar/feed")).StatusCode);
    }

    [Fact]
    public async Task DueReminders_GoOnlyToStudentsWhoHaveNotHandedIn_OncePerDueDate()
    {
        var seed = await SeedAsync();
        await RemindAsync();

        var otherTitles = await ReminderTitlesAsync(seed.OtherUserId);
        Assert.Contains("Due in 10 hours: Essay on rivers", otherTitles);
        Assert.Contains("Due in 5 hours: Map work", otherTitles);
        Assert.DoesNotContain(otherTitles, t => t.Contains("Week 3 quiz")); // their quiz closes in three days
        var studentTitles = await ReminderTitlesAsync(seed.StudentUserId);
        Assert.Equal(new[] { "Quiz closes in 12 hours: Week 3 quiz" }, studentTitles); // own date is later; Map work is handed in

        await RemindAsync();
        Assert.Equal(otherTitles.Count, (await ReminderTitlesAsync(seed.OtherUserId)).Count); // not again
        Assert.Single(await ReminderTitlesAsync(seed.StudentUserId));

        // The teacher brings the student's extension forward into the next day: the new date is announced.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.AssignmentOverrides.Remove(await db.AssignmentOverrides.SingleAsync(o => o.Id == seed.OverrideId));
            db.AssignmentOverrides.Add(new AssignmentOverride(seed.EssayId, seed.StudentProfileId, null, Trim(DateTime.UtcNow.AddHours(20)), null, null, "Earlier"));
            await db.SaveChangesAsync();
        }
        await RemindAsync();
        Assert.Contains("Due in 20 hours: Essay on rivers", await ReminderTitlesAsync(seed.StudentUserId));
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(2, await db.DueReminderLogs.CountAsync(l => l.Kind == DueReminderKind.Assignment && l.ItemId == seed.EssayId));
            Assert.False(await db.DueReminderLogs.AnyAsync(l => l.ItemId == seed.HandedInId && l.StudentProfileId == seed.StudentProfileId));
            Assert.False(await db.DueReminderLogs.AnyAsync(l => l.ItemId == seed.DraftId));
        }
    }

    private async Task RemindAsync()
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CalendarService>().SendDueRemindersAsync(DateTime.UtcNow);
    }

    private async Task<List<string>> ReminderTitlesAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.NotificationRecipients.AsNoTracking()
            .Where(r => r.RecipientUserId == userId && r.Notification.Type == NotificationType.DueReminder)
            .Select(r => r.Notification.Title).ToListAsync();
    }
}
