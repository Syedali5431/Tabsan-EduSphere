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
/// Messaging through the real API and database: who each role may write to (students their teachers and office, parents
/// their child's teachers, teachers their students and the parents, admins and finance their departments), refusals for
/// anyone else, a conversation per student, privacy of conversations, replies, unread counts, archiving and notifications.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class MessagingIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public MessagingIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    private sealed record Seed(Guid Teacher, Guid Admin, Guid Finance, Guid Ali, Guid Sara, Guid AliMum, Guid OtherTeacher, Guid Outsider, string Course);

    private HttpClient Client(string role, Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(role, userId.ToString()));
        return client;
    }

    private async Task<Seed> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var s = Guid.NewGuid().ToString("N")[..6];
        int Role(string name) => db.Roles.First(r => r.Name == name).Id;
        User Person(string key, string role, string name)
        {
            var u = new User($"mg_{key}_{s}", "integration-hash", Role(role), $"mg_{key}_{s}@tabsan.local");
            u.UpdateFullName($"{name} {s}");
            return u;
        }
        var tenant = new Tenant($"MGT{s}", $"Messaging Tenant {s}");
        var campus = new Campus(tenant.Id, $"MGK{s}", $"Messaging Campus {s}");
        var department = new Department($"Messaging Dept {s}", $"MGD{s}", InstitutionType.School);
        department.SetTenantCampus(tenant.Id, campus.Id);
        var otherDepartment = new Department($"Other Dept {s}", $"MGO{s}", InstitutionType.School);
        var program = new AcademicProgram($"Messaging Program {s}", $"MGP{s}", department.Id, 10);
        var otherProgram = new AcademicProgram($"Other Program {s}", $"MGQ{s}", otherDepartment.Id, 10);
        var semester = new Semester($"Messaging Term {s}", DateTime.UtcNow.Date.AddDays(-10), DateTime.UtcNow.Date.AddDays(90));
        var course = new Course($"Science {s}", $"MG{s}".ToUpperInvariant(), 3, department.Id);
        var teacher = Person("t", "Faculty", "Ms Khan");
        var otherTeacher = Person("t2", "Faculty", "Mr Iqbal");
        var stranger = Person("t3", "Faculty", "Mr Elsewhere");
        var admin = Person("a", "Admin", "Mr Shah");
        var finance = Person("f", "Finance", "Ms Bursar");
        finance.SetTenantCampus(tenant.Id, campus.Id);
        var offering = new CourseOffering(course.Id, semester.Id, 40, teacher.Id);
        offering.SetInstitutionScope(InstitutionType.School, tenant.Id, campus.Id);
        var ali = Person("ali", "Student", "Ali");
        var aliProfile = new StudentProfile(ali.Id, $"MG-1-{s}", program.Id, department.Id, DateTime.UtcNow.Date);
        var sara = Person("sara", "Student", "Sara");
        var saraProfile = new StudentProfile(sara.Id, $"MG-2-{s}", program.Id, department.Id, DateTime.UtcNow.Date);
        var outsider = Person("x", "Student", "Omar");
        var outsiderProfile = new StudentProfile(outsider.Id, $"MG-3-{s}", otherProgram.Id, otherDepartment.Id, DateTime.UtcNow.Date);
        var mum = Person("mum", "Parent", "Nadia");
        db.AddRange(tenant, campus, department, otherDepartment, program, otherProgram, semester, course, teacher, otherTeacher, stranger, admin, finance,
            offering, ali, aliProfile, sara, saraProfile, outsider, outsiderProfile, mum,
            new Enrollment(aliProfile.Id, offering.Id), new Enrollment(saraProfile.Id, offering.Id),
            new ParentStudentLink(mum.Id, aliProfile.Id, "Mother"),
            new AdminDepartmentAssignment(admin.Id, department.Id),
            new FacultyDepartmentAssignment(otherTeacher.Id, department.Id), new FacultyDepartmentAssignment(teacher.Id, department.Id),
            new FacultyDepartmentAssignment(stranger.Id, otherDepartment.Id));
        await db.SaveChangesAsync();
        return new Seed(teacher.Id, admin.Id, finance.Id, ali.Id, sara.Id, mum.Id, otherTeacher.Id, outsider.Id, course.Title);
    }

    private static async Task<Dictionary<Guid, JsonElement>> ContactsAsync(HttpClient client, string query = "")
    {
        var rows = await client.GetFromJsonAsync<List<JsonElement>>("api/v1/messages/contacts" + query);
        return rows!.ToDictionary(r => r.GetProperty("userId").GetGuid());
    }

    [Fact]
    public async Task Contacts_FollowEachRole()
    {
        var seed = await SeedAsync();

        var student = await ContactsAsync(Client("Student", seed.Ali));
        Assert.Equal(new[] { seed.Teacher, seed.Admin }.OrderBy(x => x), student.Keys.OrderBy(x => x)); // no other students, no unrelated staff
        Assert.Equal(("Faculty", seed.Course, "Teachers"), (student[seed.Teacher].GetProperty("role").GetString(), student[seed.Teacher].GetProperty("detail").GetString(),
            student[seed.Teacher].GetProperty("group").GetString()));

        var parent = await ContactsAsync(Client("Parent", seed.AliMum));
        Assert.Contains(seed.Teacher, parent.Keys);
        Assert.Contains(seed.Admin, parent.Keys);
        Assert.Contains(seed.Finance, parent.Keys); // fee questions
        Assert.DoesNotContain(seed.Ali, parent.Keys);
        Assert.Contains("Ali", parent[seed.Teacher].GetProperty("detail").GetString()); // whose teacher

        var teacher = await ContactsAsync(Client("Faculty", seed.Teacher));
        Assert.Contains(seed.Ali, teacher.Keys);
        Assert.Contains(seed.Sara, teacher.Keys);
        Assert.Equal(("Parent", $"Ali {seed.Course[^6..]}"), (teacher[seed.AliMum].GetProperty("role").GetString(), teacher[seed.AliMum].GetProperty("detail").GetString()));
        Assert.Contains(seed.OtherTeacher, teacher.Keys);
        Assert.Contains(seed.Admin, teacher.Keys);
        Assert.DoesNotContain(seed.Outsider, teacher.Keys);
        Assert.DoesNotContain(seed.Teacher, teacher.Keys); // never oneself
        Assert.Equal(new[] { seed.Ali }, (await ContactsAsync(Client("Faculty", seed.Teacher), "?search=ali")).Keys
            .Where(k => k == seed.Ali || k == seed.Sara)); // the search narrows

        var admin = await ContactsAsync(Client("Admin", seed.Admin));
        Assert.True(new[] { seed.Ali, seed.Sara, seed.AliMum, seed.Teacher, seed.OtherTeacher, seed.Finance }.All(admin.ContainsKey));
        Assert.DoesNotContain(seed.Outsider, admin.Keys);

        var finance = await ContactsAsync(Client("Finance", seed.Finance));
        Assert.True(new[] { seed.Ali, seed.Sara, seed.AliMum, seed.Admin }.All(finance.ContainsKey));
        Assert.DoesNotContain(seed.Teacher, finance.Keys);
    }

    [Fact]
    public async Task Conversations_ArePrivate_OnePerStudent_WithRepliesUnreadAndArchive()
    {
        var seed = await SeedAsync();
        var ali = Client("Student", seed.Ali);
        var teacher = Client("Faculty", seed.Teacher);

        var refused = await ali.PostAsJsonAsync("api/v1/messages/conversations", new { recipientIds = new[] { seed.Sara }, subject = "Hi", body = "Hello" });
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ali.PostAsJsonAsync("api/v1/messages/conversations", new { recipientIds = new[] { seed.Teacher }, subject = "", body = "Hello" })).StatusCode);

        var toClass = await teacher.PostAsJsonAsync("api/v1/messages/conversations",
            new { recipientIds = new[] { seed.Ali, seed.Sara, seed.AliMum }, subject = "Science fair", body = "Bring your models on Friday." });
        Assert.Equal(HttpStatusCode.OK, toClass.StatusCode);
        var ids = (await toClass.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("conversationIds").EnumerateArray().Select(e => e.GetGuid()).ToList();
        Assert.Equal(3, ids.Count);

        var aliInbox = await ali.GetFromJsonAsync<List<JsonElement>>("api/v1/messages/conversations");
        var mine = Assert.Single(aliInbox!);
        Assert.Equal(("Science fair", 1), (mine.GetProperty("subject").GetString(), mine.GetProperty("unread").GetInt32()));
        Assert.Equal(new[] { seed.Teacher }, mine.GetProperty("with").EnumerateArray().Select(p => p.GetProperty("userId").GetGuid())); // not Sara or the parent
        var aliConversation = mine.GetProperty("id").GetGuid();
        Assert.Equal(1, (await ali.GetFromJsonAsync<JsonElement>("api/v1/messages/unread")).GetProperty("conversations").GetInt32());

        var sara = Client("Student", seed.Sara);
        Assert.Equal(HttpStatusCode.NotFound, (await sara.GetAsync($"api/v1/messages/conversations/{aliConversation}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await sara.PostAsJsonAsync($"api/v1/messages/conversations/{aliConversation}/messages", new { body = "hi" })).StatusCode);

        var opened = await ali.GetFromJsonAsync<JsonElement>($"api/v1/messages/conversations/{aliConversation}");
        Assert.Equal("Bring your models on Friday.", opened.GetProperty("messages")[0].GetProperty("body").GetString());
        Assert.Equal(0, (await ali.GetFromJsonAsync<JsonElement>("api/v1/messages/unread")).GetProperty("conversations").GetInt32());

        Assert.Equal(HttpStatusCode.OK, (await teacher.PostAsync($"api/v1/messages/conversations/{aliConversation}/archive", null)).StatusCode);
        Assert.Empty((await teacher.GetFromJsonAsync<List<JsonElement>>("api/v1/messages/conversations"))!.Where(c => c.GetProperty("id").GetGuid() == aliConversation));
        Assert.Contains((await teacher.GetFromJsonAsync<List<JsonElement>>("api/v1/messages/conversations?archived=true"))!, c => c.GetProperty("id").GetGuid() == aliConversation);

        var reply = await ali.PostAsJsonAsync($"api/v1/messages/conversations/{aliConversation}/messages", new { body = "Can I bring a friend?" });
        Assert.Equal(HttpStatusCode.OK, reply.StatusCode);
        var back = (await teacher.GetFromJsonAsync<List<JsonElement>>("api/v1/messages/conversations"))!.Single(c => c.GetProperty("id").GetGuid() == aliConversation);
        Assert.Equal(("Can I bring a friend?", 1), (back.GetProperty("lastMessage").GetString(), back.GetProperty("unread").GetInt32())); // back in the inbox
        Assert.False(back.GetProperty("lastFromMe").GetBoolean());
        var aliSide = (await ali.GetFromJsonAsync<List<JsonElement>>("api/v1/messages/conversations"))!.Single();
        Assert.True(aliSide.GetProperty("lastFromMe").GetBoolean()); // "You: …" in Ali's inbox

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var told = await db.NotificationRecipients.AsNoTracking().Where(r => r.Notification.Type == NotificationType.Message
                && (r.RecipientUserId == seed.Ali || r.RecipientUserId == seed.Sara || r.RecipientUserId == seed.AliMum || r.RecipientUserId == seed.Teacher))
            .Select(r => new { r.RecipientUserId, r.Notification.Title }).ToListAsync();
        Assert.Equal(4, told.Count); // the three families once, the teacher for the reply
        Assert.All(told.Where(t => t.RecipientUserId != seed.Teacher), t => Assert.StartsWith("Message from Ms Khan", t.Title));
        Assert.StartsWith("Message from Ali", told.Single(t => t.RecipientUserId == seed.Teacher).Title);
    }
}
