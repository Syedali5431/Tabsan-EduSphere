using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Tenancy;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// The result query endpoints return the course, period and student labels with each row, so the portal's Results page
/// can show "CODE — Title", "Name (Reg No)" and marks out of the real maximum (it showed "—" and "33.00/0" before).
/// </summary>
[Collection(EduSphereCollection.Name)]
public class ResultLabelsIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public ResultLabelsIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    private sealed record Seed(Guid StudentUserId, Guid StudentProfileId, Guid OfferingId, string CourseCode, string Suffix);

    private async Task<Seed> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var studentRole = db.Roles.First(r => r.Name == "Student");
        var facultyRole = db.Roles.First(r => r.Name == "Faculty");
        var tenant = new Tenant($"RLT{suffix}", $"Result Tenant {suffix}");
        var campus = new Campus(tenant.Id, $"RLK{suffix}", $"Result Campus {suffix}");
        var department = new Department($"Result Dept {suffix}", $"RLD{suffix}", InstitutionType.University);
        department.SetTenantCampus(tenant.Id, campus.Id);
        var program = new AcademicProgram($"Result Program {suffix}", $"RLP{suffix}", department.Id, 8);
        var semester = new Semester($"Result Sem {suffix}", DateTime.UtcNow.Date.AddDays(-30), DateTime.UtcNow.Date.AddDays(90));
        var code = $"RL{suffix}".ToUpperInvariant();
        var course = new Course($"Result Course {suffix}", code, 3, department.Id);
        var teacher = new User($"rl_t_{suffix}", "integration-hash", facultyRole.Id, $"rl_t_{suffix}@tabsan.local");
        var offering = new CourseOffering(course.Id, semester.Id, 40, teacher.Id);
        offering.SetInstitutionScope(InstitutionType.University, tenant.Id, campus.Id);
        var student = new User($"rl_s_{suffix}", "integration-hash", studentRole.Id, $"rl_s_{suffix}@tabsan.local");
        student.UpdateFullName($"Student {suffix}");
        var profile = new StudentProfile(student.Id, $"RL-1-{suffix}", program.Id, department.Id, DateTime.UtcNow.Date);

        var midterm = new Result(profile.Id, offering.Id, "Midterm", 32, 50);
        midterm.SetGradePoint(2.67m);
        midterm.Publish(teacher.Id);
        // The system-calculated aggregate row: marks out of the entered components' weightage.
        var total = new Result(profile.Id, offering.Id, "Total", 19.2m, 30);
        total.SetGradePoint(2.67m);
        total.Publish(teacher.Id);

        db.AddRange(tenant, campus, department, program, semester, course, teacher, offering, student, profile,
            new Enrollment(profile.Id, offering.Id), midterm, total);
        await db.SaveChangesAsync();
        return new Seed(student.Id, profile.Id, offering.Id, code, suffix);
    }

    private HttpClient Client(string role, Guid userId, Guid? studentProfileId = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            JwtTestHelper.GenerateToken(role, userId.ToString(), studentProfileId: studentProfileId?.ToString()));
        return client;
    }

    [Fact]
    public async Task MyResults_CarryCourseSemesterAndStudentLabels_AndTheRealMaximum()
    {
        var seed = await SeedAsync();
        var student = Client("Student", seed.StudentUserId, seed.StudentProfileId);

        var rows = (await student.GetFromJsonAsync<JsonElement>("api/v1/result/my-results")).EnumerateArray().ToList();

        Assert.Equal(2, rows.Count);
        var midterm = rows.Single(r => r.GetProperty("resultType").GetString() == "Midterm");
        Assert.Equal(seed.CourseCode, midterm.GetProperty("courseCode").GetString());
        Assert.Equal($"Result Course {seed.Suffix}", midterm.GetProperty("courseTitle").GetString());
        Assert.Equal($"Result Sem {seed.Suffix}", midterm.GetProperty("semesterName").GetString());
        Assert.Equal($"Student {seed.Suffix}", midterm.GetProperty("studentName").GetString());
        Assert.Equal($"RL-1-{seed.Suffix}", midterm.GetProperty("registrationNumber").GetString());
        Assert.Equal(32m, midterm.GetProperty("marksObtained").GetDecimal());
        Assert.Equal(50m, midterm.GetProperty("maxMarks").GetDecimal());
        Assert.Equal(2.67m, midterm.GetProperty("gradePoint").GetDecimal());

        var total = rows.Single(r => r.GetProperty("resultType").GetString() == "Total");
        Assert.Equal(seed.CourseCode, total.GetProperty("courseCode").GetString());
        Assert.Equal(30m, total.GetProperty("maxMarks").GetDecimal());
    }

    [Fact]
    public async Task ByOffering_CarriesStudentLabels()
    {
        var seed = await SeedAsync();
        var admin = Client("SuperAdmin", Guid.NewGuid());

        var rows = (await admin.GetFromJsonAsync<JsonElement>($"api/v1/result/by-offering/{seed.OfferingId}")).EnumerateArray().ToList();

        Assert.Equal(2, rows.Count);
        Assert.All(rows, r =>
        {
            Assert.Equal($"Student {seed.Suffix}", r.GetProperty("studentName").GetString());
            Assert.Equal($"RL-1-{seed.Suffix}", r.GetProperty("registrationNumber").GetString());
            Assert.Equal(seed.CourseCode, r.GetProperty("courseCode").GetString());
        });
    }
}
