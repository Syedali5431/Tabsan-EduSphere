using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Application.LiveClasses;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Attendance;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Tenancy;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Plan M10 — live classes through the real API and database: settings per provider, scheduling (links, BigBlueButton,
/// Zoom, timetable weeks), notifications and reminders, joining (window, enrolment, moderator/host links), calendar files,
/// cancelling and moving, and attendance from joins and participant minutes without overwriting teachers' marks.
/// The conferencing services are stand-ins registered in the test host.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class LiveClassesIntegrationTests
{
    private readonly EduSphereWebFactory _factory;
    private Seed _seed = default!;

    public LiveClassesIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    [Fact]
    public async Task MeetingLinks_Join_Calendar_Reminders_Cancel_AndAttendance()
    {
        await ActivateModuleAsync();
        _seed = await SeedAsync();
        var admin = Client("Admin");
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync("api/v1/live/settings", Settings("ExternalLink"))).StatusCode);
        var faculty = Client("Faculty", userId: _seed.FacultyUserId);
        var (ann, ben, cara) = (Student(0), Student(1), Student(2));

        // Without a service, a link is required; it must be https; the start must not be long past.
        var noLink = await faculty.PostAsJsonAsync(Sessions(), Schedule("Week 1", DateTime.UtcNow.AddMinutes(10)));
        Assert.Equal(HttpStatusCode.BadRequest, noLink.StatusCode);
        Assert.Contains("Paste the meeting link", await noLink.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PostAsJsonAsync(Sessions(), Schedule("Week 1", DateTime.UtcNow.AddMinutes(10), url: "http://meet.example.test/x"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await faculty.PostAsJsonAsync(Sessions(), Schedule("Week 1", DateTime.UtcNow.AddHours(-2), url: "https://meet.example.test/x"))).StatusCode);

        var soon = await CreateAsync(faculty, Schedule("Week 1 — soon", DateTime.UtcNow.AddMinutes(10), url: "https://meet.example.test/soon"));
        var later = await CreateAsync(faculty, Schedule("Week 2 — later", DateTime.UtcNow.AddDays(2), url: "https://meet.example.test/later"));
        Assert.True(await NotifiedAsync(_seed.StudentUserIds[0], "Live class: Week 1 — soon"));

        // Joining: enrolled students, from shortly before the start; outsiders never.
        var join = await ann.PostAsync($"api/v1/live/sessions/{soon}/join", null);
        Assert.Equal(HttpStatusCode.OK, join.StatusCode);
        Assert.Equal("https://meet.example.test/soon", (await join.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("url").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await Client("Student", _seed.Outsider).PostAsync($"api/v1/live/sessions/{soon}/join", null)).StatusCode);
        var early = await ann.PostAsync($"api/v1/live/sessions/{later}/join", null);
        Assert.Equal(HttpStatusCode.Conflict, early.StatusCode);
        Assert.Contains("Joining opens 15 minutes before", await early.Content.ReadAsStringAsync());

        // Upcoming (students: enrolled courses; teachers: courses they teach) and the calendar file.
        var upcoming = await ann.GetFromJsonAsync<JsonElement>("api/v1/live/upcoming");
        Assert.Contains(upcoming.EnumerateArray(), s => s.GetProperty("id").GetGuid() == soon && s.GetProperty("canJoin").GetBoolean());
        Assert.Contains(upcoming.EnumerateArray(), s => s.GetProperty("id").GetGuid() == later && !s.GetProperty("canJoin").GetBoolean());
        Assert.Contains((await faculty.GetFromJsonAsync<JsonElement>("api/v1/live/upcoming")).EnumerateArray(), s => s.GetProperty("id").GetGuid() == soon);
        var ics = await ann.GetAsync($"api/v1/live/sessions/{later}/calendar");
        Assert.Equal("text/calendar", ics.Content.Headers.ContentType!.MediaType);
        Assert.Contains("BEGIN:VEVENT", await ics.Content.ReadAsStringAsync());

        // A class that ended 20 minutes ago: Ann joined, Ben did not, Cara already has the teacher's mark for that day.
        var ended = await CreateAsync(faculty, Schedule("Week 0 — ended", DateTime.UtcNow.AddMinutes(-50), duration: 30, url: "https://meet.example.test/ended", auto: true));
        Assert.Equal(HttpStatusCode.OK, (await ann.PostAsync($"api/v1/live/sessions/{ended}/join", null)).StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.AttendanceRecords.Add(new AttendanceRecord(_seed.Students[2], _seed.OfferingId, DateTime.UtcNow.AddMinutes(-50), AttendanceStatus.Excused, _seed.FacultyUserId, "Medical"));
            await db.SaveChangesAsync();
        }
        var report = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/live/sessions/{ended}/attendance");
        Assert.True(Row(report, _seed.Students[0]).GetProperty("present").GetBoolean());
        Assert.False(Row(report, _seed.Students[1]).GetProperty("joined").GetBoolean());

        // The worker reminds about the class starting soon and marks the ended one.
        var run = await RunAsync();
        Assert.True(run.Reminded >= 1);
        Assert.True(run.Attendance >= 1);
        Assert.True(await NotifiedAsync(_seed.StudentUserIds[1], "Starting soon: Week 1 — soon"));
        var marks = await AttendanceAsync(DateTime.UtcNow.AddMinutes(-50));
        Assert.Equal(AttendanceStatus.Present, marks[_seed.Students[0]].Status);
        Assert.Equal(AttendanceStatus.Absent, marks[_seed.Students[1]].Status);
        Assert.Equal((AttendanceStatus.Excused, "Medical"), (marks[_seed.Students[2]].Status, marks[_seed.Students[2]].Remarks)); // kept
        var again = await faculty.PostAsync($"api/v1/live/sessions/{ended}/attendance", null);
        Assert.Equal(0, (await again.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("marked").GetInt32());

        // Moving and cancelling notify students; cancelled classes cannot be joined.
        var moved = await faculty.PutAsJsonAsync($"api/v1/live/sessions/{later}", Schedule("Week 2 — later", DateTime.UtcNow.AddDays(3), url: "https://meet.example.test/later"));
        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        Assert.True(await NotifiedAsync(_seed.StudentUserIds[0], "Live class moved: Week 2 — later"));
        Assert.Equal(HttpStatusCode.NoContent, (await faculty.PostAsync($"api/v1/live/sessions/{soon}/cancel", null)).StatusCode);
        Assert.True(await NotifiedAsync(_seed.StudentUserIds[0], "Live class cancelled: Week 1 — soon"));
        Assert.Equal(HttpStatusCode.Conflict, (await ann.PostAsync($"api/v1/live/sessions/{soon}/join", null)).StatusCode);

        // Scope: students cannot schedule, other institutions cannot see.
        Assert.Equal(HttpStatusCode.Forbidden, (await ben.PostAsJsonAsync(Sessions(), Schedule("x", DateTime.UtcNow.AddDays(1), url: "https://meet.example.test/x"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client("Faculty", tenantId: Guid.NewGuid()).GetAsync(Sessions())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client("Student", _seed.Outsider).GetAsync(Sessions())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cara.GetAsync(Sessions())).StatusCode);
    }

    [Fact]
    public async Task BigBlueButton_Settings_ModeratorAndViewerLinks_AndTimetableWeeks()
    {
        await ActivateModuleAsync();
        _seed = await SeedAsync();
        var bbb = _factory.Services.GetRequiredService<StandInBigBlueButton>();
        var admin = Client("Admin");

        var incomplete = await admin.PutAsJsonAsync("api/v1/live/settings", Settings("BigBlueButton", baseUrl: "https://bbb.example.test/bigbluebutton/api"));
        Assert.Equal(HttpStatusCode.BadRequest, incomplete.StatusCode);
        var saved = await admin.PutAsJsonAsync("api/v1/live/settings", Settings("BigBlueButton", baseUrl: "https://bbb.example.test/bigbluebutton/api", secret: "wrong"));
        var savedBody = await saved.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.DoesNotContain("wrong", savedBody);
        Assert.True(JsonDocument.Parse(savedBody).RootElement.GetProperty("hasSecret").GetBoolean());
        Assert.Equal(HttpStatusCode.BadGateway, (await admin.PostAsync("api/v1/live/settings/test", null)).StatusCode);
        await admin.PutAsJsonAsync("api/v1/live/settings", Settings("BigBlueButton", baseUrl: "https://bbb.example.test/bigbluebutton/api", secret: "good-secret"));
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync("api/v1/live/settings/test", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("api/v1/live/settings?installationDefault=true")).StatusCode);

        var faculty = Client("Faculty", userId: _seed.FacultyUserId);
        var session = await CreateAsync(faculty, Schedule("BBB class", DateTime.UtcNow.AddMinutes(5)));
        Assert.Contains(bbb.Created, r => r.SessionId == session);
        Assert.Equal("good-secret", bbb.Configs.Last().Secret); // decrypted only for the provider

        var viewer = await (await Student(0).PostAsync($"api/v1/live/sessions/{session}/join", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.EndsWith("role=VIEWER", viewer.GetProperty("url").GetString());
        var moderator = await (await faculty.PostAsync($"api/v1/live/sessions/{session}/join", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.EndsWith("role=MODERATOR", moderator.GetProperty("url").GetString());
        Assert.Contains(bbb.Joins, j => j.MeetingId == session.ToString("N") && j.FullName == $"Live Student 0 {_seed.Suffix}" && !j.Moderator);

        // Timetable: the next weeks of the course's slot, never twice.
        var slotsResponse = await faculty.GetAsync($"api/v1/live/offerings/{_seed.OfferingId}/timetable");
        Assert.True(slotsResponse.IsSuccessStatusCode, await slotsResponse.Content.ReadAsStringAsync());
        var slots = await slotsResponse.Content.ReadFromJsonAsync<JsonElement>();
        var slot = Assert.Single(slots.EnumerateArray());
        Assert.Equal("Wednesday", slot.GetProperty("day").GetString());
        var weeks = await faculty.PostAsJsonAsync($"api/v1/live/offerings/{_seed.OfferingId}/sessions/from-timetable",
            new { entryId = _seed.TimetableEntryId, weeks = 3, autoAttendance = true, minimumMinutes = 0, meetingUrl = (string?)null });
        var created = (await weeks.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        Assert.Equal(3, created.Count);
        Assert.All(created, s => Assert.Equal(DayOfWeek.Wednesday, s.GetProperty("startsAt").GetDateTime().ToUniversalTime().DayOfWeek));
        Assert.All(created, s => Assert.Equal(90, s.GetProperty("durationMinutes").GetInt32()));
        var repeat = await faculty.PostAsJsonAsync($"api/v1/live/offerings/{_seed.OfferingId}/sessions/from-timetable",
            new { entryId = _seed.TimetableEntryId, weeks = 3, autoAttendance = true, minimumMinutes = 0, meetingUrl = (string?)null });
        Assert.Equal(0, (await repeat.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength());

        // Cancelling ends the meeting at the provider; switching provider forgets the secret.
        Assert.Equal(HttpStatusCode.NoContent, (await faculty.PostAsync($"api/v1/live/sessions/{session}/cancel", null)).StatusCode);
        Assert.Contains(session.ToString("N"), bbb.Cancelled);
        var links = await (await admin.PutAsJsonAsync("api/v1/live/settings", Settings("ExternalLink"))).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(links.GetProperty("hasSecret").GetBoolean());
    }

    [Fact]
    public async Task Zoom_HostLink_AndAttendanceFromParticipantMinutes()
    {
        await ActivateModuleAsync();
        _seed = await SeedAsync();
        var zoom = _factory.Services.GetRequiredService<StandInZoom>();
        var admin = Client("Admin");
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync("api/v1/live/settings", Settings("Zoom", accountId: "acct", clientId: "client", secret: "zoom-secret"))).StatusCode);
        var faculty = Client("Faculty", userId: _seed.FacultyUserId);

        var session = await CreateAsync(faculty, Schedule("Zoom class", DateTime.UtcNow.AddMinutes(-50), duration: 30, auto: false, minimum: 20));
        var host = await (await faculty.PostAsync($"api/v1/live/sessions/{session}/join", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.EndsWith("?host=1", host.GetProperty("url").GetString()); // teachers get the host link
        var student = await (await Student(0).PostAsync($"api/v1/live/sessions/{session}/join", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal($"https://meet.example.test/j/{session:N}", student.GetProperty("url").GetString());
        await Student(1).PostAsync($"api/v1/live/sessions/{session}/join", null);

        // Zoom says Ann stayed 25 minutes and Ben 5: with a 20-minute minimum only Ann is present.
        zoom.Participants[session.ToString("N")] = [new ConferenceParticipant(_seed.Emails[0].ToUpperInvariant(), "Ann", 25), new ConferenceParticipant(_seed.Emails[1], "Ben", 5)];
        var marked = await faculty.PostAsync($"api/v1/live/sessions/{session}/attendance", null);
        Assert.Equal(3, (await marked.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("marked").GetInt32());
        var report = await faculty.GetFromJsonAsync<JsonElement>($"api/v1/live/sessions/{session}/attendance");
        Assert.Equal((25, true), (Row(report, _seed.Students[0]).GetProperty("minutes").GetInt32(), Row(report, _seed.Students[0]).GetProperty("present").GetBoolean()));
        Assert.Equal((5, false), (Row(report, _seed.Students[1]).GetProperty("minutes").GetInt32(), Row(report, _seed.Students[1]).GetProperty("present").GetBoolean()));
        var marks = await AttendanceAsync(DateTime.UtcNow.AddMinutes(-50));
        Assert.Equal(AttendanceStatus.Present, marks[_seed.Students[0]].Status);
        Assert.Equal(AttendanceStatus.Absent, marks[_seed.Students[1]].Status);
        Assert.Contains("25 min", marks[_seed.Students[0]].Remarks);

        // The worker picks up the recording link once the class has ended.
        await RunAsync();
        var sessions = await faculty.GetFromJsonAsync<JsonElement>(Sessions());
        Assert.Equal($"https://meet.example.test/rec/{session:N}", sessions.EnumerateArray().Single(s => s.GetProperty("id").GetGuid() == session).GetProperty("recordingUrl").GetString());
    }

    // ── helpers ──

    private sealed record Seed(Guid OfferingId, Guid FacultyUserId, IReadOnlyList<Guid> Students, IReadOnlyList<Guid> StudentUserIds, IReadOnlyList<string> Emails,
        Guid Outsider, Guid TenantId, Guid CampusId, Guid TimetableEntryId, string Suffix);

    private async Task<Seed> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var studentRole = db.Roles.First(r => r.Name == "Student");
        var facultyRole = db.Roles.First(r => r.Name == "Faculty");
        var tenant = new Tenant($"LCT{suffix}", $"Live Tenant {suffix}");
        var campus = new Campus(tenant.Id, $"LCK{suffix}", $"Live Campus {suffix}");
        var department = new Department($"Live Dept {suffix}", $"LCD{suffix}", InstitutionType.University);
        department.SetTenantCampus(tenant.Id, campus.Id);
        var program = new AcademicProgram($"Live Program {suffix}", $"LCP{suffix}", department.Id, 8);
        var semester = new Semester($"Live Sem {suffix}", DateTime.UtcNow.Date.AddDays(-10), DateTime.UtcNow.Date.AddDays(120));
        var course = new Course($"Live Course {suffix}", $"LC{suffix}", 3, department.Id);
        var teacher = new User($"lc_t_{suffix}", "integration-hash", facultyRole.Id, $"lc_t_{suffix}@tabsan.local");
        var offering = new CourseOffering(course.Id, semester.Id, 40, teacher.Id);
        offering.SetInstitutionScope(InstitutionType.University, tenant.Id, campus.Id);
        var timetable = new Timetable(department.Id, program.Id, semester.Id, 1, DateTime.UtcNow.Date);
        var entry = new TimetableEntry(timetable.Id, (int)DayOfWeek.Wednesday, new TimeOnly(10, 30), new TimeOnly(12, 0), $"Live Course {suffix}", course.Id, teacher.Id);
        db.AddRange(tenant, campus, department, program, semester, course, teacher, offering, timetable, entry);
        var students = new List<Guid>();
        var users = new List<Guid>();
        var emails = new List<string>();
        for (var i = 0; i <= 3; i++)
        {
            var email = $"lc_{i}_{suffix}@tabsan.local";
            var user = new User($"lc_{i}_{suffix}", "integration-hash", studentRole.Id, email);
            user.UpdateFullName($"Live Student {i} {suffix}");
            var profile = new StudentProfile(user.Id, $"LC-{i}-{suffix}", program.Id, department.Id, DateTime.UtcNow.Date);
            db.AddRange(user, profile);
            if (i < 3) db.Add(new Enrollment(profile.Id, offering.Id));
            students.Add(profile.Id);
            users.Add(user.Id);
            emails.Add(email);
        }
        await db.SaveChangesAsync();
        return new Seed(offering.Id, teacher.Id, students.Take(3).ToList(), users, emails, students[3], tenant.Id, campus.Id, entry.Id, suffix);
    }

    private HttpClient Client(string role, Guid? studentProfileId = null, Guid? tenantId = null, Guid? userId = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            JwtTestHelper.GenerateToken(role, (userId ?? Guid.NewGuid()).ToString(), studentProfileId: studentProfileId?.ToString(),
                tenantId: (tenantId ?? _seed.TenantId).ToString(), campusId: (tenantId is null ? _seed.CampusId : Guid.NewGuid()).ToString()));
        return client;
    }

    private HttpClient Student(int i) => Client("Student", _seed.Students[i], userId: _seed.StudentUserIds[i]);

    private string Sessions() => $"api/v1/live/offerings/{_seed.OfferingId}/sessions";

    private static object Settings(string provider, string? baseUrl = null, string? secret = null, string? accountId = null, string? clientId = null)
        => new { provider, isEnabled = true, baseUrl, secret, accountId, clientId, organizerUserId = (string?)null, joinEarlyMinutes = 15 };

    private static object Schedule(string title, DateTime start, int duration = 60, string? url = null, bool auto = true, int minimum = 0)
        => new { title, description = (string?)null, startsAt = start, durationMinutes = duration, autoAttendance = auto, minimumMinutes = minimum, meetingUrl = url };

    private async Task<Guid> CreateAsync(HttpClient faculty, object body)
    {
        var response = await faculty.PostAsJsonAsync(Sessions(), body);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, text);
        return JsonDocument.Parse(text).RootElement.GetProperty("id").GetGuid();
    }

    private static JsonElement Row(JsonElement report, Guid student)
        => report.GetProperty("students").EnumerateArray().Single(s => s.GetProperty("studentProfileId").GetGuid() == student);

    private async Task<bool> NotifiedAsync(Guid userId, string title)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await (from r in db.NotificationRecipients join n in db.Notifications on r.NotificationId equals n.Id
                      where r.RecipientUserId == userId && n.Title == title select r).AnyAsync();
    }

    private async Task<Dictionary<Guid, AttendanceRecord>> AttendanceAsync(DateTime date)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var day = date.Date;
        return await db.AttendanceRecords.AsNoTracking().Where(r => r.CourseOfferingId == _seed.OfferingId && r.Date == day).ToDictionaryAsync(r => r.StudentProfileId);
    }

    private async Task<(int Reminded, int Attendance, int Recordings)> RunAsync()
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<LiveClassService>().RunAsync(DateTime.UtcNow);
    }

    private async Task ActivateModuleAsync()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken("SuperAdmin", Guid.NewGuid().ToString()));
        var response = await client.PostAsync("api/v1/module/live_classes/activate", null);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }
}
