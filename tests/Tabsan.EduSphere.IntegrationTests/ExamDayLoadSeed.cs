using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Quizzes;
using Tabsan.EduSphere.Domain.Tenancy;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Plan M14.2 — seeds the integration-test database for tests/load/k6-exam-day.js: one course offering with a published
/// 10-question quiz (unlimited attempts) and EDUSPHERE_LOAD_STUDENTS (default 500) enrolled students. Writes a CSV of
/// student tokens ("token" column) to EDUSPHERE_LOAD_SEED_FILE and the quiz/offering ids and a teacher token next to it.
/// Does nothing unless EDUSPHERE_LOAD_SEED_FILE is set. The data stays until the next integration-test run drops the database.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class ExamDayLoadSeed
{
    private readonly EduSphereWebFactory _factory;

    public ExamDayLoadSeed(EduSphereWebFactory factory) => _factory = factory;

    [Fact]
    public async Task SeedForTheExamDayLoadTest_WhenAsked()
    {
        var file = Environment.GetEnvironmentVariable("EDUSPHERE_LOAD_SEED_FILE");
        if (string.IsNullOrWhiteSpace(file)) return;
        var count = int.TryParse(Environment.GetEnvironmentVariable("EDUSPHERE_LOAD_STUDENTS"), out var n) ? Math.Clamp(n, 1, 20_000) : 500;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var studentRole = db.Roles.First(r => r.Name == "Student");
        var facultyRole = db.Roles.First(r => r.Name == "Faculty");
        var tenant = new Tenant($"LDT{suffix}", $"Load Tenant {suffix}");
        var campus = new Campus(tenant.Id, $"LDK{suffix}", $"Load Campus {suffix}");
        var department = new Department($"Load Dept {suffix}", $"LDD{suffix}", InstitutionType.University);
        department.SetTenantCampus(tenant.Id, campus.Id);
        var program = new AcademicProgram($"Load Program {suffix}", $"LDP{suffix}", department.Id, 8);
        var semester = new Semester($"Load Sem {suffix}", DateTime.UtcNow.Date.AddDays(-10), DateTime.UtcNow.Date.AddDays(120));
        var course = new Course($"Exam Day {suffix}", $"LD{suffix}", 3, department.Id);
        var teacher = new User($"ld_t_{suffix}", "load-hash", facultyRole.Id, $"ld_t_{suffix}@tabsan.local");
        var offering = new CourseOffering(course.Id, semester.Id, count + 10, teacher.Id);
        offering.SetInstitutionScope(InstitutionType.University, tenant.Id, campus.Id);
        var quiz = new Quiz(offering.Id, "Exam day quiz", teacher.Id, "Answer every question.", timeLimitMinutes: 60, maxAttempts: 0);
        db.AddRange(tenant, campus, department, program, semester, course, teacher, offering, quiz);
        for (var q = 0; q < 10; q++)
        {
            var question = new QuizQuestion(quiz.Id, $"Question {q + 1}: pick the right answer.", QuestionType.MultipleChoice, 2, q);
            db.Add(question);
            for (var o = 0; o < 4; o++) db.Add(new QuizOption(question.Id, $"Option {o + 1}", o == 0, o));
        }
        quiz.Publish();

        var lines = new List<string> { "token" };
        for (var i = 0; i < count; i++)
        {
            var user = new User($"ld_s_{suffix}_{i}", "load-hash", studentRole.Id, $"ld_s_{suffix}_{i}@tabsan.local");
            var profile = new StudentProfile(user.Id, $"LD-{suffix}-{i:00000}", program.Id, department.Id, DateTime.UtcNow.Date);
            db.AddRange(user, profile, new Enrollment(profile.Id, offering.Id));
            lines.Add(JwtTestHelper.GenerateToken("Student", user.Id.ToString(), studentProfileId: profile.Id.ToString()));
            if (i % 1000 == 999) await db.SaveChangesAsync();
        }
        await db.SaveChangesAsync();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
        await File.WriteAllLinesAsync(file, lines);
        await File.WriteAllTextAsync(Path.ChangeExtension(file, ".env"),
            $"QUIZ_ID={quiz.Id}\nOFFERING_ID={offering.Id}\nTEACHER_TOKEN={JwtTestHelper.GenerateToken("Faculty", teacher.Id.ToString())}\n");
    }
}
