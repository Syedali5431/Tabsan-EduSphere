using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tabsan.EduSphere.Web;

namespace Tabsan.EduSphere.WebIntegrationTests;

/// <summary>
/// The Pastoral Care and Wellbeing pages against a fake API: the overview with check-ins waiting and follow-ups, a student's
/// page (plans, check-ins, notes with follow-ups), writing a note for students of a class or for one student (what was
/// typed kept when refused), a support plan, the set-up page, a student's check-in, the menu guard and Urdu.
/// </summary>
public class PastoralWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private const string AliId = "a3000000-0000-0000-0000-000000000001";
    private const string SaraId = "a3000000-0000-0000-0000-000000000002";
    private const string RecordId = "a3000000-0000-0000-0000-000000000003";
    private const string PlanId = "a3000000-0000-0000-0000-000000000004";
    private const string CheckInId = "a3000000-0000-0000-0000-000000000005";
    private const string ClassId = "a3000000-0000-0000-0000-000000000006";
    private const string CampusId = "a3000000-0000-0000-0000-000000000007";
    private const string CounsellorId = "a3000000-0000-0000-0000-000000000008";
    private const string MemberId = "a3000000-0000-0000-0000-000000000009";
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public PastoralWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task Overview_ShowsCheckInsWaitingAndStudents_AndTheStudentPage()
    {
        var api = new FakeApi("Admin");
        using var client = await SignedInClientAsync(api);

        var overview = WebUtility.HtmlDecode(await client.GetStringAsync("/Portal/Pastoral"));
        Assert.Contains("data-needs-attention", overview);
        Assert.Contains("Would like to talk", overview);
        Assert.Contains("“Worried about the exams”", overview);
        Assert.Contains($"href=\"/Portal/PastoralStudent/{AliId}\"", overview);
        Assert.Contains("data-pastoral-students", overview);
        Assert.Contains("Excellent effort · 4", overview);
        Assert.Contains("href=\"/Portal/PastoralSetup\"", overview);           // admins set the school up
        Assert.Contains("href=\"/Portal/Pastoral\"", overview);                // the sidebar's Pastoral Care link
        Assert.Contains("Notes waiting for you to follow up", overview);

        var page = WebUtility.HtmlDecode(await client.GetStringAsync($"/Portal/PastoralStudent/{AliId}"));
        Assert.Contains("data-student-name>Ali Raza", page);
        Assert.Contains("Give 25% extra time.", page);                        // the support plan
        Assert.Contains("Crying at break", page);
        Assert.Contains("Confidential", page);
        Assert.Contains("Met him and his mother.", page);                     // a follow-up note
        Assert.Contains("Remove the note", page);
        Assert.Contains("data-check-ins", page);
        Assert.Contains($"href=\"/Portal/PastoralNote?studentId={AliId}\"", page);

        var seen = await client.PostAsync("/Portal/PastoralCheckInSeen", Form(TokenOf(overview), ("id", CheckInId), ("note", "Met at lunch.")));
        Assert.Equal("/Portal/Pastoral", seen.Headers.Location!.ToString());
        Assert.Equal("""{"text":"Met at lunch."}""", api.Body("POST", $"/api/v1/pastoral/check-ins/{CheckInId}/seen"));

        var followed = await client.PostAsync("/Portal/AddPastoralFollowUpNote", Form(TokenOf(page), ("id", RecordId), ("studentId", AliId), ("text", "Phoned home.")));
        Assert.Equal($"/Portal/PastoralStudent/{AliId}", followed.Headers.Location!.ToString());
        Assert.Equal("""{"text":"Phoned home."}""", api.Body("POST", $"/api/v1/pastoral/records/{RecordId}/notes"));
    }

    [Fact]
    public async Task WritingANote_ForAClassOrOneStudent_SendsWhatWasChosen_AndKeepsItWhenRefused()
    {
        var api = new FakeApi("Faculty");
        using var client = await SignedInClientAsync(api);

        var form = WebUtility.HtmlDecode(await client.GetStringAsync($"/Portal/PastoralNote?classId={ClassId}"));
        Assert.Contains($"name=\"StudentProfileIds\" value=\"{AliId}\"", form);
        Assert.Contains($"name=\"StudentProfileIds\" value=\"{SaraId}\"", form);
        Assert.Contains("data-kind=\"Behaviour\" data-tone=\"Positive\"", form);
        Assert.Contains("data-utc-for=\"OccurredAt\"", form);
        Assert.DoesNotContain("name=\"FollowUpUserId\"", form);              // follow-ups are asked for one student at a time

        var saved = await client.PostAsync("/Portal/SavePastoralNote", Form(TokenOf(form), ("classId", ClassId), ("CourseOfferingId", ClassId),
            ("StudentProfileIds", AliId), ("StudentProfileIds", SaraId), ("Kind", "Behaviour"), ("Tone", "Positive"), ("Severity", "Low"),
            ("Category", "Excellent effort"), ("Summary", "Great group work"), ("OccurredAt", "2026-11-28T04:30:00.000Z"), ("ShareWithParents", "true")));
        Assert.Equal("/Portal/Pastoral", saved.Headers.Location!.ToString());
        using (var body = JsonDocument.Parse(api.Body("POST", "/api/v1/pastoral/records")))
        {
            var r = body.RootElement;
            Assert.Equal([AliId, SaraId], r.GetProperty("studentProfileIds").EnumerateArray().Select(x => x.GetString()));
            Assert.Equal(("Positive", "Excellent effort", true, false), (r.GetProperty("tone").GetString(), r.GetProperty("category").GetString(),
                r.GetProperty("shareWithParents").GetBoolean(), r.GetProperty("confidential").GetBoolean()));
            Assert.Equal(new DateTime(2026, 11, 28, 4, 30, 0, DateTimeKind.Utc), r.GetProperty("occurredAt").GetDateTime().ToUniversalTime());
            Assert.Equal(ClassId, r.GetProperty("courseOfferingId").GetString());
        }

        var one = WebUtility.HtmlDecode(await client.GetStringAsync($"/Portal/PastoralNote?studentId={AliId}"));
        Assert.Contains("data-note-student>Ali Raza", one);
        Assert.Contains($"<option value=\"{CounsellorId}\" data-sees-confidential=\"true\">Mrs Counsellor — School counsellor</option>", one);
        var single = await client.PostAsync("/Portal/SavePastoralNote", Form(TokenOf(one), ("studentId", AliId), ("Kind", "Wellbeing"), ("Tone", "Concern"),
            ("Severity", "High"), ("Summary", "Crying at break"), ("FollowUpUserId", CounsellorId), ("FollowUpBy", "2026-12-01")));
        Assert.Equal($"/Portal/PastoralStudent/{AliId}", single.Headers.Location!.ToString());
        using (var body = JsonDocument.Parse(api.Body("POST", "/api/v1/pastoral/records")))
        {
            Assert.Equal([AliId], body.RootElement.GetProperty("studentProfileIds").EnumerateArray().Select(x => x.GetString()));
            Assert.Equal(CounsellorId, body.RootElement.GetProperty("followUpUserId").GetString());
            Assert.Equal(new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), body.RootElement.GetProperty("followUpBy").GetDateTime().ToUniversalTime());
        }

        api.Refuse = true;
        var refused = await client.PostAsync("/Portal/SavePastoralNote", Form(TokenOf(one), ("studentId", AliId), ("Kind", "Wellbeing"), ("Tone", "Concern"),
            ("Summary", "Kept summary"), ("Details", "Kept details"), ("Confidential", "true"), ("ShareWithParents", "true")));
        Assert.Equal(HttpStatusCode.OK, refused.StatusCode);
        var again = WebUtility.HtmlDecode(await refused.Content.ReadAsStringAsync());
        Assert.Contains("A confidential note cannot be shared with the student or the family.", again);
        Assert.Contains("value=\"Kept summary\"", again);
        Assert.Contains(">Kept details</textarea>", again);
    }

    [Fact]
    public async Task SupportPlanAndSetup_SendTheirChoices()
    {
        var api = new FakeApi("Admin");
        using var client = await SignedInClientAsync(api);

        var plan = WebUtility.HtmlDecode(await client.GetStringAsync($"/Portal/PastoralPlan?studentId={AliId}"));
        Assert.Contains("What staff should do", plan);
        var saved = await client.PostAsync("/Portal/SavePastoralPlan", Form(TokenOf(plan), ("StudentProfileId", AliId), ("Kind", "LearningAdjustments"),
            ("Title", "Extra time"), ("Strategies", "Give 25% extra time."), ("StartsOn", "2026-11-28"), ("ReviewOn", "2027-01-15"), ("ShareWithParents", "true")));
        Assert.Equal($"/Portal/PastoralStudent/{AliId}", saved.Headers.Location!.ToString());
        using (var body = JsonDocument.Parse(api.Body("POST", "/api/v1/pastoral/plans")))
        {
            Assert.Equal(new DateTime(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc), body.RootElement.GetProperty("reviewOn").GetDateTime().ToUniversalTime());
            Assert.True(body.RootElement.GetProperty("shareWithParents").GetBoolean());
        }
        var edit = WebUtility.HtmlDecode(await client.GetStringAsync($"/Portal/PastoralPlan?id={PlanId}&studentId={AliId}"));
        Assert.Contains("value=\"2027-01-15\"", edit);

        var setup = WebUtility.HtmlDecode(await client.GetStringAsync("/Portal/PastoralSetup"));
        Assert.Contains("data-team", setup);
        Assert.Contains("School counsellor", setup);
        Assert.Contains("Every year level", setup);
        Assert.Contains("data-categories", setup);
        var added = await client.PostAsync("/Portal/AddPastoralTeamMember", Form(TokenOf(setup), ("userId", CounsellorId), ("title", "Head of Class 7"),
            ("campusId", CampusId), ("seesConfidential", "true")));
        Assert.Equal("/Portal/PastoralSetup", added.Headers.Location!.ToString());
        using (var body = JsonDocument.Parse(api.Body("POST", "/api/v1/pastoral/team")))
        {
            Assert.Equal(("Head of Class 7", CampusId, true), (body.RootElement.GetProperty("title").GetString(), body.RootElement.GetProperty("campusId").GetString(),
                body.RootElement.GetProperty("seesConfidential").GetBoolean()));
        }
        await client.PostAsync("/Portal/SavePastoralCategory", Form(TokenOf(setup), ("id", Guid.Empty.ToString()), ("kind", "Behaviour"), ("tone", "Concern"),
            ("name", "Uniform"), ("isActive", "false")));
        Assert.Contains("\"isActive\":false", api.Body("POST", "/api/v1/pastoral/categories"));
        await client.PostAsync("/Portal/SavePastoralSettings", Form(TokenOf(setup), ("checkInsEnabled", "true"), ("concernAlertCount", "3"), ("concernAlertDays", "14")));
        Assert.Contains("\"checkInsEnabled\":true", api.Body("PUT", "/api/v1/pastoral/settings"));
    }

    [Fact]
    public async Task Students_CheckIn_AndSeeSharedNotes_StaffPagesStayClosed_AndUrdu()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);

        var overview = await client.GetAsync("/Portal/Pastoral");
        Assert.Equal("/Portal/Wellbeing", overview.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync($"/Portal/PastoralStudent/{AliId}")).StatusCode);

        var page = WebUtility.HtmlDecode(await client.GetStringAsync("/Portal/Wellbeing"));
        Assert.Contains("How are you feeling today?", page);
        Assert.Contains("data-shared-note", page);
        Assert.Contains("Top marks in the test", page);
        Assert.Contains("I would like to talk to someone", page);
        var sent = await client.PostAsync("/Portal/WellbeingCheckIn", Form(TokenOf(page), ("mood", "2"), ("note", "Worried"), ("wantsToTalk", "true")));
        Assert.Equal("/Portal/Wellbeing", sent.Headers.Location!.ToString());
        Assert.Equal("""{"mood":2,"note":"Worried","wantsToTalk":true}""", api.Body("POST", "/api/v1/pastoral/check-ins"));
        Assert.Contains("Thank you. Someone from the wellbeing team will find you soon.", WebUtility.HtmlDecode(await client.GetStringAsync("/Portal/Wellbeing")));

        await client.PostAsync("/Language/Set", Form(TokenOf(page), ("lang", "ur"), ("returnUrl", "/")));
        var urdu = WebUtility.HtmlDecode(await client.GetStringAsync("/Portal/Wellbeing"));
        Assert.Contains("<html lang=\"ur\" dir=\"rtl\"", urdu);
        Assert.Contains("آج آپ کیسا محسوس کر رہے ہیں؟", urdu);   // How are you feeling today?
        Assert.Contains("بہت اداس", urdu);                       // Very low
    }

    // ── helpers ──

    private static string TokenOf(string html)
        => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields)
        => new(new[] { new KeyValuePair<string, string>("__RequestVerificationToken", token) }.Concat(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value))));

    private async Task<HttpClient> SignedInClientAsync(FakeApi api)
    {
        var factory = _baseFactory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.UseSetting("EduApi:BaseUrl", FakeApi.Base);
            b.ConfigureTestServices(s => s.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>(new FakeHttpClientFactory(api))));
        });
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true, BaseAddress = new Uri("https://localhost") });
        var page = await client.GetStringAsync("/Login");
        var login = await client.PostAsync("/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = TokenOf(page), ["username"] = "tester", ["password"] = "Password#123"
        }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        return client;
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = new Uri(FakeApi.Base + "/") };
    }

    private sealed class FakeApi(string role) : HttpMessageHandler
    {
        public const string Base = "https://api.test";
        private readonly List<(string Method, string Path, string Body)> _writes = new();
        public bool Refuse { get; set; }
        public string Body(string method, string path) => _writes.Last(w => w.Method == method && w.Path == path).Body;

        private string Jwt()
        {
            static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var payload = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["sub"] = Guid.NewGuid().ToString(), ["unique_name"] = "tester", ["role"] = role, ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
            return $"{B64("{\"alg\":\"none\"}")}.{B64(payload)}.sig";
        }

        private const string Ali = $$"""{"id":"{{AliId}}","name":"Ali Raza","registrationNumber":"GUL-001","className":"Class 7"}""";
        private const string CheckIn = $$"""{"id":"{{CheckInId}}","studentProfileId":"{{AliId}}","studentName":"Ali Raza","className":"Class 7","mood":2,"note":"Worried about the exams","wantsToTalk":true,"checkedInAt":"2026-11-28T03:00:00","needsAttention":true}""";
        private const string Record = $$"""{"id":"{{RecordId}}","studentProfileId":"{{AliId}}","studentName":"Ali Raza","kind":"Wellbeing","tone":"Concern","severity":"High","summary":"Crying at break","occurredAt":"2026-11-27T05:00:00","visibility":"Confidential","sharedWithStudent":false,"sharedWithParents":false,"authorName":"Mr Siddiqui","recordedAt":"2026-11-27T05:10:00","followUp":"Open","followUpUserId":"{{CounsellorId}}","followUpName":"Mrs Counsellor","followUpBy":"2026-11-29T00:00:00","isRemoved":false,"canManage":true,"canFollowUp":true,"notes":[{"id":"{{MemberId}}","authorName":"Mrs Counsellor","text":"Met him and his mother.","at":"2026-11-27T09:00:00"}]}""";
        private const string Plan = $$"""{"id":"{{PlanId}}","studentProfileId":"{{AliId}}","kind":"LearningAdjustments","title":"Extra time in tests","strategies":"Give 25% extra time.","startsOn":"2026-11-01T00:00:00","reviewOn":"2027-01-15T00:00:00","status":"Active","visibility":"Staff","sharedWithStudent":false,"sharedWithParents":true,"ownerName":"Mrs Counsellor","canManage":true}""";
        private const string Categories = """[{"id":"00000000-0000-0000-0000-000000000000","kind":"Behaviour","tone":"Positive","name":"Excellent effort","isActive":true},{"id":"00000000-0000-0000-0000-000000000000","kind":"Behaviour","tone":"Concern","name":"Uniform","isActive":true}]""";
        private const string Places = $$"""{"campuses":[{"id":"{{CampusId}}","name":"Gulberg"}],"semesters":[],"classes":[{"id":"{{ClassId}}","name":"MTH07 — Mathematics","campusId":"{{CampusId}}"}]}""";

        private string Me => $$"""{"role":"{{role}}","isStaff":true,"isTeam":true,"seesConfidential":true,"seesCheckIns":true,"canSetUp":{{(role == "Admin" ? "true" : "false")}},"checkInsEnabled":true}""";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            if (request.Method != HttpMethod.Get && path != "/api/v1/auth/login") _writes.Add((request.Method.Method, path, body));

            if (path == "/api/v1/auth/login")
                return Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role, userId = Guid.NewGuid(),
                    username = "tester", mustChangePassword = false, mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                }));
            if (path == "/api/v1/sidebar-menu/my-visible")
                return Json("[" + (role == "Student"
                        ? """{"id":"00000000-0000-0000-0000-0000000000f5","key":"wellbeing","name":"Wellbeing","purpose":"","parentId":null,"displayOrder":51,"isActive":true,"isSystemMenu":false,"roleAccesses":[],"subMenus":[]},"""
                        : """{"id":"00000000-0000-0000-0000-0000000000f4","key":"pastoral","name":"Pastoral Care","purpose":"","parentId":null,"displayOrder":50,"isActive":true,"isSystemMenu":false,"roleAccesses":[],"subMenus":[]},""")
                    + """{"id":"00000000-0000-0000-0000-0000000000e3","key":"dashboard","name":"Dashboard","purpose":"","parentId":null,"displayOrder":1,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]}]""");

            const string p = "/api/v1/pastoral/";
            if (path == p + "overview")
                return Json($$"""{"from":"2026-10-29T00:00:00","to":"2026-11-28T09:00:00","totals":{"positive":4,"concerns":2,"wellbeing":1,"other":0,"openFollowUps":1,"activePlans":1},"categories":[{"kind":"Behaviour","tone":"Positive","category":"Excellent effort","count":4}],"students":[{"student":{{Ali}},"positive":4,"concerns":2,"wellbeing":1,"other":0,"openFollowUps":1,"activePlans":1,"lastMood":2,"needsAttention":true}],"needsAttention":[{{CheckIn}}],"myFollowUps":[{{Record}}],"filters":{{Places}},"me":{{Me}}}""");
            if (path == p + $"students/{AliId}")
                return Json($$"""{"student":{{Ali}},"campusName":"Gulberg","counts":{"positive":4,"concerns":2,"wellbeing":1,"other":0,"openFollowUps":1,"activePlans":1},"records":[{{Record}}],"plans":[{{Plan}}],"checkIns":[{{CheckIn}}],"canWrite":true,"canPlan":true,"seesConfidential":true,"followUpOptions":[{"userId":"{{CounsellorId}}","name":"Mrs Counsellor","role":"School counsellor","seesConfidential":true}],"categories":{{Categories}}}""");
            if (path == p + "filters") return Json(Places);
            if (path == p + "categories" && request.Method == HttpMethod.Get) return Json(Categories);
            if (path == p + "students")
                return Json($$"""[{{Ali}},{"id":"{{SaraId}}","name":"Sara Khan","registrationNumber":"GUL-002","className":"Class 7"}]""");
            if (path == p + "records" && request.Method == HttpMethod.Post)
                return Refuse
                    ? Json("""{"message":"A confidential note cannot be shared with the student or the family."}""", HttpStatusCode.BadRequest)
                    : Json($$"""{"saved":1,"recordIds":["{{RecordId}}"],"skipped":[]}""", HttpStatusCode.Created);
            if (path == p + $"records/{RecordId}/notes" || path == p + $"check-ins/{CheckInId}/seen") return Json("{}");
            if (path == p + "plans") return Json(Plan, HttpStatusCode.Created);
            if (path == p + "setup")
                return Json($$"""{"tenantId":"{{CampusId}}","schools":[],"team":[{"id":"{{MemberId}}","userId":"{{CounsellorId}}","name":"Mrs Counsellor","title":"School counsellor","seesConfidential":true}],"staff":[{"userId":"{{CounsellorId}}","name":"Mrs Counsellor","role":"Teacher","seesConfidential":false}],"settings":{"checkInsEnabled":false,"concernAlertCount":3,"concernAlertDays":14},"categories":{{Categories}},"places":{{Places}}}""");
            if (path == p + "team" || path == p + "categories" || path == p + "settings") return Json("{}");
            if (path == p + "mine")
                return Json("""{"checkInsEnabled":true,"history":[],"records":[{"id":"a3000000-0000-0000-0000-0000000000aa","kind":"Behaviour","tone":"Positive","category":"Outstanding work","summary":"Top marks in the test","occurredAt":"2026-11-27T05:00:00","authorName":"Mr Siddiqui"}],"plans":[]}""");
            if (path == p + "check-ins") return Json($$"""{"id":"{{CheckInId}}","mood":2,"wantsToTalk":true,"checkedInAt":"2026-11-28T09:00:00"}""", HttpStatusCode.Created);
            if (path == "/api/v1/live/upcoming") return Json("[]");

            return path.Contains("menu", StringComparison.OrdinalIgnoreCase) || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                || path.Contains("offering", StringComparison.OrdinalIgnoreCase) || path.Contains("course", StringComparison.OrdinalIgnoreCase)
                ? Json("[]")
                : Json("{}");
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
