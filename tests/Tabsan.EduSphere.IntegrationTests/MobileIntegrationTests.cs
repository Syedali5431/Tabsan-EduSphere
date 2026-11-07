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
        profile.UpdateAcademicStanding(3.4m, 3.4m);
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
        Assert.Equal(3.4m, summary.GetProperty("cgpa").GetDecimal()); // University students keep their CGPA
        Assert.Equal(80m, summary.GetProperty("averagePercent").GetDecimal());

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

    /// <summary>
    /// What the School-only demo data showed on a phone: a "CGPA" for school children, Monday's classes on a Tuesday morning
    /// east of UTC, "To grade" on assignments nobody had handed in, and no way to have a parent account at all.
    /// </summary>
    [Fact]
    public async Task School_AverageNotCgpa_TodayFromThePhone_WorkToGrade_AndARealParent()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var utcDay = (int)DateTime.UtcNow.DayOfWeek;
        var phoneDay = (utcDay + 1) % 7;
        Guid teacherId, studentId, profileId, parentId, offeringId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var parentRole = db.Roles.FirstOrDefault(r => r.Name == "Parent");
            Assert.NotNull(parentRole); // seeded at start-up: parent links (and the app's parent tabs) need it
            var tenant = new Tenant($"SCT{suffix}", $"School Tenant {suffix}");
            var campus = new Campus(tenant.Id, $"SCK{suffix}", $"School Campus {suffix}");
            var department = new Department($"School Dept {suffix}", $"SCD{suffix}", InstitutionType.School);
            department.SetTenantCampus(tenant.Id, campus.Id);
            var program = new AcademicProgram($"Class 1 to 10 {suffix}", $"SCP{suffix}", department.Id, 10);
            var semester = new Semester($"Class 9 {suffix}", DateTime.UtcNow.Date.AddDays(-30), DateTime.UtcNow.Date.AddDays(200));
            var course = new Course($"Mathematics {suffix}", $"SC{suffix}".ToUpperInvariant(), 3, department.Id);
            var teacher = new User($"sc_t_{suffix}", "integration-hash", db.Roles.First(r => r.Name == "Faculty").Id, $"sc_t_{suffix}@tabsan.local");
            var offering = new CourseOffering(course.Id, semester.Id, 40, teacher.Id);
            offering.SetInstitutionScope(InstitutionType.School, tenant.Id, campus.Id);
            var timetable = new Timetable(department.Id, program.Id, semester.Id, 9, DateTime.UtcNow.Date);
            timetable.Publish();
            var student = new User($"sc_s_{suffix}", "integration-hash", db.Roles.First(r => r.Name == "Student").Id, $"sc_s_{suffix}@tabsan.local");
            var profile = new StudentProfile(student.Id, $"SC-9-{suffix}", program.Id, department.Id, DateTime.UtcNow.Date);
            profile.UpdateAcademicStanding(3.2m, 3.2m); // a GPA must not reach School users
            var parent = new User($"sc_p_{suffix}", "integration-hash", parentRole.Id, $"sc_p_{suffix}@tabsan.local");
            var handedIn = new Assignment(offering.Id, "Exercise 2.1", "Questions 1-10.", DateTime.UtcNow.AddDays(2), 20);
            var notYet = new Assignment(offering.Id, "Word problems", null, DateTime.UtcNow.AddDays(9), 20);
            handedIn.Publish();
            notYet.Publish();
            // Components 36/50 and 15/20; the "Total" row repeats them and must not be counted twice: 51/70 = 72.9 %.
            var results = new[] { new Result(profile.Id, offering.Id, "Midterm", 36, 50), new Result(profile.Id, offering.Id, "Quiz", 15, 20), new Result(profile.Id, offering.Id, "Total", 51, 70) };
            foreach (var r in results) r.Publish(teacher.Id);
            db.AddRange(tenant, campus, department, program, semester, course, teacher, offering, timetable, student, profile, parent, handedIn, notYet,
                new TimetableEntry(timetable.Id, utcDay, new TimeOnly(9, 0), new TimeOnly(9, 40), "Mathematics", course.Id, teacher.Id),
                new TimetableEntry(timetable.Id, phoneDay, new TimeOnly(8, 0), new TimeOnly(8, 40), "Mathematics", course.Id, teacher.Id),
                new Enrollment(profile.Id, offering.Id), new AssignmentSubmission(handedIn.Id, profile.Id, null, "My answers"),
                new CourseAnnouncement(offering.Id, teacher.Id, "Sports day", "Friday, after the second break."));
            db.AddRange(results);
            await db.SaveChangesAsync();
            (teacherId, studentId, profileId, parentId, offeringId) = (teacher.Id, student.Id, profile.Id, parent.Id, offering.Id);
        }

        var app = Client("Student", studentId, profileId);
        var home = await app.GetFromJsonAsync<JsonElement>($"api/v1/mobile/home?day={phoneDay}");
        var summary = home.GetProperty("student");
        Assert.False(summary.TryGetProperty("cgpa", out _));
        Assert.Equal(72.9m, summary.GetProperty("averagePercent").GetDecimal());
        Assert.Equal(["08:00"], home.GetProperty("today").EnumerateArray().Select(c => c.GetProperty("start").GetString()));
        var olderApp = await app.GetFromJsonAsync<JsonElement>("api/v1/mobile/home"); // no day: today in UTC, as before
        Assert.Equal(["09:00"], olderApp.GetProperty("today").EnumerateArray().Select(c => c.GetProperty("start").GetString()));
        Assert.Equal(HttpStatusCode.BadRequest, (await app.GetAsync("api/v1/mobile/home?day=7")).StatusCode);

        var teacherView = await Client("Faculty", teacherId).GetFromJsonAsync<JsonElement>($"api/v1/mobile/courses/{offeringId}");
        var toGrade = teacherView.GetProperty("assignments").EnumerateArray().ToDictionary(a => a.GetProperty("title").GetString()!, a => a.GetProperty("toGrade").GetInt32());
        Assert.Equal((1, 0), (toGrade["Exercise 2.1"], toGrade["Word problems"]));
        var studentView = await app.GetFromJsonAsync<JsonElement>($"api/v1/mobile/courses/{offeringId}");
        Assert.All(studentView.GetProperty("assignments").EnumerateArray(), a => Assert.Equal(0, a.GetProperty("toGrade").GetInt32()));

        // A Parent-role account is linked through the admin API (which insists on the role and on a School student).
        var link = await Client("SuperAdmin", Guid.NewGuid()).PutAsJsonAsync("api/v1/parent-portal/links",
            new { parentUserId = parentId, studentProfileId = profileId, relationship = "Mother" });
        Assert.Equal(HttpStatusCode.OK, link.StatusCode);
        var parentApp = Client("Parent", parentId);
        var child = (await parentApp.GetFromJsonAsync<JsonElement>("api/v1/mobile/children")).EnumerateArray().Single();
        Assert.False(child.TryGetProperty("cgpa", out _));
        Assert.Equal(72.9m, child.GetProperty("averagePercent").GetDecimal());

        // The child's announcements: parent accounts have no tenant scope, which the parent-portal route insists on.
        var announcements = await parentApp.GetFromJsonAsync<JsonElement>($"api/v1/mobile/announcements?studentProfileId={profileId}");
        Assert.Equal("Sports day", announcements.EnumerateArray().Single().GetProperty("title").GetString());
        Assert.Equal("Sports day", (await app.GetFromJsonAsync<JsonElement>("api/v1/mobile/announcements")).EnumerateArray().Single().GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await parentApp.GetAsync("api/v1/mobile/announcements")).StatusCode); // a parent names the child
        Assert.Equal(HttpStatusCode.Forbidden, (await Client("Parent", Guid.NewGuid()).GetAsync($"api/v1/mobile/announcements?studentProfileId={profileId}")).StatusCode);
    }

    [Fact]
    public async Task Classes_Campuses_Homework_DayByDayAttendance_AndTheAdminsCampus()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var today = DateTime.UtcNow.Date;
        Guid teacherId, studentId, profileId, parentId, adminId, offeringA, campusA, campusB;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tenant = new Tenant($"CCT{suffix}", $"Campus School {suffix}");
            var north = new Campus(tenant.Id, $"CCN{suffix}", $"North {suffix}");
            var south = new Campus(tenant.Id, $"CCS{suffix}", $"South {suffix}");
            var deptNorth = new Department($"North School {suffix}", $"CDN{suffix}", InstitutionType.School);
            deptNorth.SetTenantCampus(tenant.Id, north.Id);
            var deptSouth = new Department($"South School {suffix}", $"CDS{suffix}", InstitutionType.School);
            deptSouth.SetTenantCampus(tenant.Id, south.Id);
            var program = new AcademicProgram($"Classes {suffix}", $"CCP{suffix}", deptNorth.Id, 10);
            var class9 = new Semester($"Class 9 {suffix}", today.AddDays(-30), today.AddDays(200));
            var class8 = new Semester($"Class 8 {suffix}", today.AddDays(-30), today.AddDays(200));
            var maths = new Course($"Mathematics {suffix}", $"CM{suffix}".ToUpperInvariant(), 3, deptNorth.Id);
            var science = new Course($"Science {suffix}", $"CS{suffix}".ToUpperInvariant(), 3, deptSouth.Id);
            var teacher = new User($"cc_t_{suffix}", "integration-hash", db.Roles.First(r => r.Name == "Faculty").Id, $"cc_t_{suffix}@tabsan.local");
            var inNorth = new CourseOffering(maths.Id, class9.Id, 40, teacher.Id);
            inNorth.SetInstitutionScope(InstitutionType.School, tenant.Id, north.Id);
            var inSouth = new CourseOffering(science.Id, class8.Id, 40, teacher.Id);
            inSouth.SetInstitutionScope(InstitutionType.School, tenant.Id, south.Id);
            var student = new User($"cc_s_{suffix}", "integration-hash", db.Roles.First(r => r.Name == "Student").Id, $"cc_s_{suffix}@tabsan.local");
            var profile = new StudentProfile(student.Id, $"CC-9-{suffix}", program.Id, deptNorth.Id, today);
            var parent = new User($"cc_p_{suffix}", "integration-hash", db.Roles.First(r => r.Name == "Parent").Id, $"cc_p_{suffix}@tabsan.local");
            var admin = new User($"cc_a_{suffix}", "integration-hash", db.Roles.First(r => r.Name == "Admin").Id, $"cc_a_{suffix}@tabsan.local");
            var homework = new Assignment(inNorth.Id, "Exercise 3.2", "Questions 1-5.", today.AddDays(3), 20);
            homework.Publish();
            var southHomework = new Assignment(inSouth.Id, "Plant cells", null, today.AddDays(5), 10);
            southHomework.Publish();
            db.AddRange(tenant, north, south, deptNorth, deptSouth, program, class9, class8, maths, science, teacher, inNorth, inSouth,
                student, profile, parent, admin, homework, southHomework,
                new Enrollment(profile.Id, inNorth.Id), new AssignmentSubmission(homework.Id, profile.Id, null, "My answers"),
                new AttendanceRecord(profile.Id, inNorth.Id, today.AddDays(-1), AttendanceStatus.Absent, teacher.Id),
                new AttendanceRecord(profile.Id, inNorth.Id, today, AttendanceStatus.Present, teacher.Id),
                new AdminDepartmentAssignment(admin.Id, deptNorth.Id));
            await db.SaveChangesAsync();
            (teacherId, studentId, profileId, parentId, adminId, offeringA, campusA, campusB) =
                (teacher.Id, student.Id, profile.Id, parent.Id, admin.Id, inNorth.Id, north.Id, south.Id);
        }

        // Courses carry their class and campus (the app's class and campus pickers).
        var app = Client("Student", studentId, profileId);
        var course = (await app.GetFromJsonAsync<JsonElement>("api/v1/mobile/courses")).EnumerateArray().Single();
        Assert.Equal(($"Class 9 {suffix}", $"North {suffix}", $"Campus School {suffix}"),
            (course.GetProperty("semesterName").GetString(), course.GetProperty("campusName").GetString(), course.GetProperty("tenantName").GetString()));

        // Homework across courses, with the student's own state; a teacher sees what is left to grade in both campuses.
        var mine = (await app.GetFromJsonAsync<JsonElement>("api/v1/mobile/homework")).EnumerateArray().Single();
        Assert.Equal(("Exercise 3.2", true), (mine.GetProperty("title").GetString(), mine.GetProperty("submitted").GetBoolean()));
        var teaching = (await Client("Faculty", teacherId).GetFromJsonAsync<JsonElement>("api/v1/mobile/homework")).EnumerateArray()
            .ToDictionary(h => h.GetProperty("title").GetString()!, h => h.GetProperty("toGrade").GetInt32());
        Assert.Equal((1, 0), (teaching["Exercise 3.2"], teaching["Plant cells"]));

        // Day-by-day attendance, newest first, optionally for one course.
        var days = (await app.GetFromJsonAsync<JsonElement>("api/v1/mobile/attendance/records")).EnumerateArray().ToList();
        Assert.Equal(["Present", "Absent"], days.Select(d => d.GetProperty("status").GetString()));
        Assert.Equal(2, (await app.GetFromJsonAsync<JsonElement>($"api/v1/mobile/attendance/records?offeringId={offeringA}")).GetArrayLength());

        // Parents see a linked child's courses, timetable, homework and attendance, and nobody else's.
        Assert.Equal(HttpStatusCode.OK, (await Client("SuperAdmin", Guid.NewGuid()).PutAsJsonAsync("api/v1/parent-portal/links",
            new { parentUserId = parentId, studentProfileId = profileId, relationship = "Father" })).StatusCode);
        var parentApp = Client("Parent", parentId);
        Assert.Equal(1, (await parentApp.GetFromJsonAsync<JsonElement>($"api/v1/mobile/courses?studentProfileId={profileId}")).GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, (await parentApp.GetAsync($"api/v1/mobile/timetable?studentProfileId={profileId}")).StatusCode);
        Assert.True((await parentApp.GetFromJsonAsync<JsonElement>($"api/v1/mobile/homework?studentProfileId={profileId}")).EnumerateArray().Single().GetProperty("submitted").GetBoolean());
        Assert.Equal(2, (await parentApp.GetFromJsonAsync<JsonElement>($"api/v1/mobile/attendance/records?studentProfileId={profileId}")).GetArrayLength());
        var childCourse = await parentApp.GetFromJsonAsync<JsonElement>($"api/v1/mobile/courses/{offeringA}?studentProfileId={profileId}");
        Assert.True(childCourse.GetProperty("assignments").EnumerateArray().Single().GetProperty("submitted").GetBoolean());
        var stranger = Client("Parent", Guid.NewGuid());
        foreach (var path in new[] { "courses", "timetable", "homework", "attendance/records" })
            Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"api/v1/mobile/{path}?studentProfileId={profileId}")).StatusCode);

        // Campuses: the teacher's two (through their courses), the admin's one (through their department).
        var teacherScopes = (await Client("Faculty", teacherId).GetFromJsonAsync<JsonElement>("api/v1/mobile/scopes")).EnumerateArray()
            .Select(s => s.GetProperty("campusId").GetGuid()).ToHashSet();
        Assert.True(teacherScopes.SetEquals([campusA, campusB]));
        var adminApp = Client("Admin", adminId);
        Assert.Equal([campusA], (await adminApp.GetFromJsonAsync<JsonElement>("api/v1/mobile/scopes")).EnumerateArray().Select(s => s.GetProperty("campusId").GetGuid()));

        // The admin's campus at a glance; a campus they do not manage is refused, and other roles cannot ask.
        var summary = await adminApp.GetFromJsonAsync<JsonElement>($"api/v1/mobile/campus-summary?campusId={campusA}&date={today:yyyy-MM-dd}");
        Assert.Equal((1, 1, 1, 1, 100m), (summary.GetProperty("students").GetInt32(), summary.GetProperty("teachers").GetInt32(),
            summary.GetProperty("courses").GetInt32(), summary.GetProperty("attendanceTodayMarked").GetInt32(), summary.GetProperty("attendanceTodayPercent").GetDecimal()));
        Assert.Equal(HttpStatusCode.Forbidden, (await adminApp.GetAsync($"api/v1/mobile/campus-summary?campusId={campusB}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await app.GetAsync("api/v1/mobile/campus-summary")).StatusCode);
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
