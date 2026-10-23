using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Attendance;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Lms;
using Tabsan.EduSphere.Domain.Quizzes;
using Tabsan.EduSphere.Domain.StudentLifecycle;
using Tabsan.EduSphere.Domain.Tenancy;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Plan M12 — the mobile app's API through the real API and database: configuration before sign-in, the single sign-on
/// hand-off (PKCE, single use), the home screen, timetable, courses, results and attendance for students, teachers and
/// parents (with access checks), device registration, crash reports and the minimum-version gate.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class MobileIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public MobileIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    private sealed record Seed(Guid TeacherUserId, Guid StudentUserId, Guid StudentProfileId, Guid OutsiderUserId, Guid OutsiderProfileId,
        Guid ParentUserId, Guid OfferingId, Guid AssignmentId, Guid QuizId, string CourseCode);

    private HttpClient Client(string role, Guid userId, Guid? studentProfileId = null, string? appVersion = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            JwtTestHelper.GenerateToken(role, userId.ToString(), studentProfileId: studentProfileId?.ToString()));
        if (appVersion is not null) client.DefaultRequestHeaders.Add("X-App-Version", appVersion);
        return client;
    }

    private async Task<Seed> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var studentRole = db.Roles.First(r => r.Name == "Student");
        var facultyRole = db.Roles.First(r => r.Name == "Faculty");
        var tenant = new Tenant($"MBT{suffix}", $"Mobile Tenant {suffix}");
        var campus = new Campus(tenant.Id, $"MBK{suffix}", $"Mobile Campus {suffix}");
        var department = new Department($"Mobile Dept {suffix}", $"MBD{suffix}", InstitutionType.University);
        department.SetTenantCampus(tenant.Id, campus.Id);
        var program = new AcademicProgram($"Mobile Program {suffix}", $"MBP{suffix}", department.Id, 8);
        var semester = new Semester($"Mobile Sem {suffix}", DateTime.UtcNow.Date.AddDays(-10), DateTime.UtcNow.Date.AddDays(120));
        var code = $"MB{suffix}".ToUpperInvariant(); // courses store their code upper-case
        var course = new Course($"Mobile Course {suffix}", code, 3, department.Id);
        var teacher = new User($"mb_t_{suffix}", "integration-hash", facultyRole.Id, $"mb_t_{suffix}@tabsan.local");
        teacher.UpdateFullName($"Teacher {suffix}");
        var offering = new CourseOffering(course.Id, semester.Id, 40, teacher.Id);
        offering.SetInstitutionScope(InstitutionType.University, tenant.Id, campus.Id);
        var timetable = new Timetable(department.Id, program.Id, semester.Id, 1, DateTime.UtcNow.Date);
        var entry = new TimetableEntry(timetable.Id, (int)DateTime.UtcNow.DayOfWeek, new TimeOnly(9, 0), new TimeOnly(10, 30), $"Mobile Course {suffix}", course.Id, teacher.Id);
        timetable.Publish();
        var student = new User($"mb_s_{suffix}", "integration-hash", studentRole.Id, $"mb_s_{suffix}@tabsan.local");
        student.UpdateFullName($"Student {suffix}");
        var profile = new StudentProfile(student.Id, $"MB-1-{suffix}", program.Id, department.Id, DateTime.UtcNow.Date);
        var outsider = new User($"mb_o_{suffix}", "integration-hash", studentRole.Id, $"mb_o_{suffix}@tabsan.local");
        var outsiderProfile = new StudentProfile(outsider.Id, $"MB-2-{suffix}", program.Id, department.Id, DateTime.UtcNow.Date);
        var parent = new User($"mb_p_{suffix}", "integration-hash", facultyRole.Id, $"mb_p_{suffix}@tabsan.local");
        var assignment = new Assignment(offering.Id, "Essay on rivers", "Two pages.", DateTime.UtcNow.AddDays(1), 20);
        assignment.Publish();
        var quiz = new Quiz(offering.Id, "Week quiz", teacher.Id, maxAttempts: 2, availableUntil: DateTime.UtcNow.AddDays(3));
        quiz.Publish();
        var result = new Result(profile.Id, offering.Id, "Midterm", 40, 50);
        result.Publish(teacher.Id);
        db.AddRange(tenant, campus, department, program, semester, course, teacher, offering, timetable, entry, student, profile, outsider, outsiderProfile, parent,
            new Enrollment(profile.Id, offering.Id), assignment, quiz, result,
            new CourseAnnouncement(offering.Id, teacher.Id, "Room change", "Thursday's class is in B12."),
            new AttendanceRecord(profile.Id, offering.Id, DateTime.UtcNow.Date.AddDays(-2), AttendanceStatus.Present, teacher.Id),
            new AttendanceRecord(profile.Id, offering.Id, DateTime.UtcNow.Date.AddDays(-1), AttendanceStatus.Absent, teacher.Id),
            new PaymentReceipt(profile.Id, teacher.Id, 12500, $"MB-R-{suffix}", "Tuition", DateTime.UtcNow.AddDays(10)),
            new ParentStudentLink(parent.Id, profile.Id, "Mother"),
            new StudentReportCard(profile.Id, InstitutionType.University, "Semester 1",
                """{"gpa":3.4,"subjects":[{"subject":"Mobile Course","grade":"A"}],"remarks":"Steady progress."}""", teacher.Id));
        await db.SaveChangesAsync();
        return new Seed(teacher.Id, student.Id, profile.Id, outsider.Id, outsiderProfile.Id, parent.Id, offering.Id, assignment.Id, quiz.Id, code);
    }

    [Fact]
    public async Task Config_IsAvailableBeforeSignIn()
    {
        var config = await _factory.CreateClient().GetFromJsonAsync<JsonElement>("api/v1/mobile/config");
        Assert.False(string.IsNullOrWhiteSpace(config.GetProperty("institutionName").GetString()));
        Assert.Equal("edusphere://sso", config.GetProperty("appRedirectUri").GetString());
        Assert.Equal(3, config.GetProperty("languages").GetArrayLength());
        Assert.False(config.GetProperty("pushEnabled").GetBoolean());
    }

    [Fact]
    public async Task Handoff_GivesTheAppItsOwnTokens_OnceAndOnlyWithTheVerifier()
    {
        var seed = await SeedAsync();
        var verifier = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var challenge = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var portal = Client("Faculty", seed.TeacherUserId);
        Assert.Equal(HttpStatusCode.BadRequest, (await portal.PostAsJsonAsync("api/v1/mobile/handoff", new { codeChallenge = "too-short" })).StatusCode);
        var handoff = await (await portal.PostAsJsonAsync("api/v1/mobile/handoff", new { codeChallenge = challenge })).Content.ReadFromJsonAsync<JsonElement>();
        var code = handoff.GetProperty("code").GetString();

        var app = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await app.PostAsJsonAsync("api/v1/mobile/handoff", new { codeChallenge = challenge })).StatusCode);
        var redeemed = await app.PostAsJsonAsync("api/v1/mobile/handoff/redeem", new { code, codeVerifier = verifier, deviceInfo = "Pixel 8" });
        Assert.Equal(HttpStatusCode.OK, redeemed.StatusCode);
        var login = await redeemed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(seed.TeacherUserId, login.GetProperty("userId").GetGuid());
        Assert.False(string.IsNullOrEmpty(login.GetProperty("refreshToken").GetString()));
        var reuse = await app.PostAsJsonAsync("api/v1/mobile/handoff/redeem", new { code, codeVerifier = verifier });
        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
        Assert.Equal("INVALID_HANDOFF", (await reuse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString());

        var other = await (await portal.PostAsJsonAsync("api/v1/mobile/handoff", new { codeChallenge = challenge })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.BadRequest, (await app.PostAsJsonAsync("api/v1/mobile/handoff/redeem",
            new { code = other.GetProperty("code").GetString(), codeVerifier = new string('a', 43) })).StatusCode);

        // The app's tokens work against the API like any sign-in.
        var signedIn = _factory.CreateClient();
        signedIn.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await signedIn.GetAsync("api/v1/mobile/home")).StatusCode);
    }

    [Fact]
    public async Task Student_SeesTodayDueWorkCoursesResultsAndAttendance()
    {
        var seed = await SeedAsync();
        var student = Client("Student", seed.StudentUserId, seed.StudentProfileId);
        var home = await student.GetFromJsonAsync<JsonElement>("api/v1/mobile/home");
        Assert.Equal("Student", home.GetProperty("role").GetString());
        Assert.Contains(home.GetProperty("today").EnumerateArray(), c => c.GetProperty("courseCode").GetString() == seed.CourseCode && c.GetProperty("start").GetString() == "09:00");
        var due = home.GetProperty("due").EnumerateArray().ToList();
        Assert.Contains(due, d => d.GetProperty("kind").GetString() == "Assignment" && d.GetProperty("id").GetGuid() == seed.AssignmentId && !d.GetProperty("done").GetBoolean());
        Assert.Contains(due, d => d.GetProperty("kind").GetString() == "Quiz" && d.GetProperty("id").GetGuid() == seed.QuizId);
        Assert.Contains(home.GetProperty("announcements").EnumerateArray(), a => a.GetProperty("title").GetString() == "Room change");
        var summary = home.GetProperty("student");
        Assert.Equal(50m, summary.GetProperty("attendancePercent").GetDecimal());
        Assert.Equal(12500m, summary.GetProperty("outstandingFees").GetDecimal());

        var timetable = await student.GetFromJsonAsync<JsonElement>("api/v1/mobile/timetable");
        Assert.Contains(timetable.EnumerateArray(), c => c.GetProperty("offeringId").GetGuid() == seed.OfferingId);

        var courses = await student.GetFromJsonAsync<JsonElement>("api/v1/mobile/courses");
        Assert.Contains(courses.EnumerateArray(), c => c.GetProperty("offeringId").GetGuid() == seed.OfferingId && c.GetProperty("teacher").GetString()!.StartsWith("Teacher"));
        var course = await student.GetFromJsonAsync<JsonElement>($"api/v1/mobile/courses/{seed.OfferingId}");
        Assert.False(course.GetProperty("isTeacher").GetBoolean());
        var assignment = course.GetProperty("assignments").EnumerateArray().Single();
        Assert.False(assignment.GetProperty("submitted").GetBoolean());
        Assert.Equal(2, course.GetProperty("quizzes").EnumerateArray().Single().GetProperty("maxAttempts").GetInt32());

        var results = await student.GetFromJsonAsync<JsonElement>("api/v1/mobile/results");
        var midterm = results.EnumerateArray().Single(r => r.GetProperty("courseCode").GetString() == seed.CourseCode);
        Assert.Equal(80m, midterm.GetProperty("percentage").GetDecimal());
        var attendance = await student.GetFromJsonAsync<JsonElement>("api/v1/mobile/attendance");
        var row = attendance.EnumerateArray().Single(r => r.GetProperty("offeringId").GetGuid() == seed.OfferingId);
        Assert.Equal((2, 1, 1), (row.GetProperty("total").GetInt32(), row.GetProperty("attended").GetInt32(), row.GetProperty("absent").GetInt32()));
        var card = (await student.GetFromJsonAsync<JsonElement>("api/v1/mobile/report-cards")).EnumerateArray().Single();
        Assert.Equal("Semester 1", card.GetProperty("periodLabel").GetString());
        var lines = card.GetProperty("lines").EnumerateArray().ToList();
        Assert.Contains(lines, l => l.GetProperty("label").GetString() == "GPA" && l.GetProperty("value").GetString() == "3.4");
        Assert.Contains(lines, l => l.GetProperty("label").GetString() == "Mobile Course" && l.GetProperty("value").GetString() == "Grade: A");

        // Someone else's course, results or attendance: not shown.
        var outsider = Client("Student", seed.OutsiderUserId, seed.OutsiderProfileId);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"api/v1/mobile/courses/{seed.OfferingId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync($"api/v1/mobile/results?studentProfileId={seed.StudentProfileId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync($"api/v1/mobile/report-cards?studentProfileId={seed.StudentProfileId}")).StatusCode);
        Assert.Empty((await outsider.GetFromJsonAsync<JsonElement>("api/v1/mobile/report-cards")).EnumerateArray());
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync($"api/v1/mobile/children")).StatusCode);
    }

    [Fact]
    public async Task Teacher_AndParent_SeeTheirOwnApp()
    {
        var seed = await SeedAsync();
        var teacher = Client("Faculty", seed.TeacherUserId);
        var home = await teacher.GetFromJsonAsync<JsonElement>("api/v1/mobile/home");
        Assert.Equal(1, home.GetProperty("teacher").GetProperty("courses").GetInt32());
        Assert.Equal(1, home.GetProperty("teacher").GetProperty("classesToday").GetInt32());
        var course = await teacher.GetFromJsonAsync<JsonElement>($"api/v1/mobile/courses/{seed.OfferingId}");
        Assert.True(course.GetProperty("isTeacher").GetBoolean());
        Assert.Single((await teacher.GetFromJsonAsync<JsonElement>("api/v1/mobile/timetable")).EnumerateArray());

        var parent = Client("Parent", seed.ParentUserId);
        var children = await parent.GetFromJsonAsync<JsonElement>("api/v1/mobile/children");
        var child = children.EnumerateArray().Single();
        Assert.Equal(seed.StudentProfileId, child.GetProperty("studentProfileId").GetGuid());
        Assert.Equal(1, child.GetProperty("publishedResults").GetInt32());
        Assert.Equal(12500m, child.GetProperty("outstandingFees").GetDecimal());
        var parentHome = await parent.GetFromJsonAsync<JsonElement>("api/v1/mobile/home");
        Assert.Single(parentHome.GetProperty("children").EnumerateArray());
        Assert.Contains(parentHome.GetProperty("announcements").EnumerateArray(), a => a.GetProperty("title").GetString() == "Room change");
        Assert.Equal(HttpStatusCode.OK, (await parent.GetAsync($"api/v1/mobile/results?studentProfileId={seed.StudentProfileId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await parent.GetAsync($"api/v1/mobile/attendance?studentProfileId={seed.StudentProfileId}")).StatusCode);
        var cards = await parent.GetFromJsonAsync<JsonElement>($"api/v1/mobile/report-cards?studentProfileId={seed.StudentProfileId}");
        Assert.Equal("Semester 1", cards.EnumerateArray().Single().GetProperty("periodLabel").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await parent.GetAsync("api/v1/mobile/report-cards")).StatusCode); // a parent names the child
        Assert.Equal(HttpStatusCode.Forbidden, (await teacher.GetAsync($"api/v1/mobile/report-cards?studentProfileId={seed.StudentProfileId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await parent.GetAsync($"api/v1/mobile/results?studentProfileId={seed.OutsiderProfileId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client("Parent", Guid.NewGuid()).GetAsync($"api/v1/mobile/attendance?studentProfileId={seed.StudentProfileId}")).StatusCode);
    }

    [Fact]
    public async Task Devices_CrashReports_AndTheMinimumVersion()
    {
        var seed = await SeedAsync();
        var phone = Client("Faculty", seed.TeacherUserId, appVersion: "1.0.0");
        var token = "fcm-" + Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.OK, (await phone.PostAsJsonAsync("api/v1/mobile/devices", new { platform = "android", token, appVersion = "1.0.0", deviceModel = "Pixel 8", language = "ar" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await phone.PostAsJsonAsync("api/v1/mobile/devices", new { platform = "symbian", token = "x" })).StatusCode);
        var devices = await phone.GetFromJsonAsync<JsonElement>("api/v1/mobile/devices");
        Assert.Equal("Pixel 8", devices.EnumerateArray().Single().GetProperty("deviceModel").GetString());
        // The same phone signed in by someone else moves to them.
        var other = Client("Student", seed.StudentUserId, seed.StudentProfileId);
        await other.PostAsJsonAsync("api/v1/mobile/devices", new { platform = "android", token, appVersion = "1.0.1" });
        Assert.Empty((await phone.GetFromJsonAsync<JsonElement>("api/v1/mobile/devices")).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await phone.PostAsJsonAsync("api/v1/mobile/devices/unregister", new { platform = "android", token })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await other.PostAsJsonAsync("api/v1/mobile/devices/unregister", new { platform = "android", token })).StatusCode);

        var crash = await phone.PostAsJsonAsync("api/v1/mobile/crash-reports", new
        {
            platform = "ios", appVersion = "1.0.0", osVersion = "17.4", deviceModel = "iPhone 15", exceptionType = "System.NullReferenceException",
            message = "Object reference not set", stackTrace = "at Home.Load()", screen = "Home", occurredAt = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.OK, crash.StatusCode);
        var id = (await crash.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var report = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().MobileCrashReports.SingleAsync(r => r.Id == id);
            Assert.Equal((seed.TeacherUserId, "Home"), (report.UserId!.Value, report.Screen));
        }

        var outdated = Client("Faculty", seed.TeacherUserId, appVersion: "0.9.0");
        var refused = await outdated.GetAsync("api/v1/mobile/home");
        Assert.Equal((HttpStatusCode)426, refused.StatusCode);
        Assert.Equal("UPDATE_REQUIRED", (await refused.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString());
    }
}
