using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// GET api/v1/prerequisite?departmentId= gives a department's prerequisites in one call. The portal's Prerequisites page
/// asked course by course before: with the School demo data (610 courses) one page load used up the per-user request limit.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class PrerequisiteDepartmentIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public PrerequisiteDepartmentIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Department_ReturnsAllItsCoursesPrerequisites_InOneCall_AndOnlyThose()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        Guid maths, science, algebra, calculus, statistics;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var mathsDept = new Department($"Maths {suffix}", $"PM{suffix}");
            var scienceDept = new Department($"Science {suffix}", $"PS{suffix}");
            var alg = new Course($"Algebra {suffix}", $"ALG{suffix}", 3, mathsDept.Id);
            var calc = new Course($"Calculus {suffix}", $"CAL{suffix}", 3, mathsDept.Id);
            var stats = new Course($"Statistics {suffix}", $"STA{suffix}", 3, mathsDept.Id);
            var phys = new Course($"Physics {suffix}", $"PHY{suffix}", 3, scienceDept.Id);
            db.AddRange(mathsDept, scienceDept, alg, calc, stats, phys,
                new CoursePrerequisite(calc.Id, alg.Id), new CoursePrerequisite(stats.Id, alg.Id),
                new CoursePrerequisite(phys.Id, calc.Id)); // a Science course needing a Maths one
            await db.SaveChangesAsync();
            (maths, science, algebra, calculus, statistics) = (mathsDept.Id, scienceDept.Id, alg.Id, calc.Id, stats.Id);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken("Admin", Guid.NewGuid().ToString()));

        var links = await client.GetFromJsonAsync<JsonElement>($"api/v1/prerequisite?departmentId={maths}");
        var pairs = links.EnumerateArray()
            .Select(l => (Course: l.GetProperty("courseId").GetGuid(), Needs: l.GetProperty("prerequisiteCourseId").GetGuid()))
            .ToList();
        Assert.Equal(new[] { (calculus, algebra), (statistics, algebra) }.OrderBy(p => p.Item1), pairs.OrderBy(p => p.Course));
        Assert.All(links.EnumerateArray(), l => Assert.StartsWith("ALG", l.GetProperty("prerequisiteCourseCode").GetString()));

        // Science's own course is listed under Science, not under Maths (whose course it needs).
        var scienceLinks = await client.GetFromJsonAsync<JsonElement>($"api/v1/prerequisite?departmentId={science}");
        Assert.Equal(calculus, scienceLinks.EnumerateArray().Single().GetProperty("prerequisiteCourseId").GetGuid());

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("api/v1/prerequisite")).StatusCode);
        // the per-course call still works
        Assert.Single((await client.GetFromJsonAsync<JsonElement>($"api/v1/prerequisite/{calculus}")).EnumerateArray());
    }
}
