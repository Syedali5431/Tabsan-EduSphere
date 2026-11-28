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
/// The School News, Events and Forms pages against a fake API: the news list and a post with its read count, writing news
/// (audience, UTC times, draft or publish, what was typed kept when refused), replying to an event, filling in a form with
/// several choices, the answers page, the menu guard and Urdu.
/// </summary>
public class CommunityWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private const string PostId = "e2000000-0000-0000-0000-000000000001";
    private const string EventId = "e2000000-0000-0000-0000-000000000002";
    private const string FormId = "e2000000-0000-0000-0000-000000000003";
    private const string CampusId = "e2000000-0000-0000-0000-000000000004";
    private const string ClassId = "e2000000-0000-0000-0000-000000000005";
    private const string GoField = "f2000000-0000-0000-0000-000000000001";
    private const string AllergyField = "f2000000-0000-0000-0000-000000000002";
    private const string NameField = "f2000000-0000-0000-0000-000000000003";
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public CommunityWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task News_ListAndPost_AndWritingSendsTheAudienceAndUtcTimes()
    {
        var api = new FakeApi("Admin");
        using var client = await SignedInClientAsync(api);

        var list = WebUtility.HtmlDecode(await client.GetStringAsync("/Portal/News"));
        Assert.Contains("data-news=\"Sports day\"", list);
        Assert.Contains("data-news-new", list);
        Assert.Contains("Gulberg · Everyone", list);
        Assert.Contains("href=\"/Portal/EditNews\"", list);       // admins write news
        Assert.Contains("href=\"/Portal/News\"", list);           // the sidebar's School News link

        var post = WebUtility.HtmlDecode(await client.GetStringAsync($"/Portal/NewsPost?id={PostId}"));
        Assert.Contains("Read by 12 of the 40 people it is for.", post);
        Assert.Contains("<p>", post);
        Assert.Contains("Bring your kit.", post);

        var editor = WebUtility.HtmlDecode(await client.GetStringAsync("/Portal/EditNews"));
        Assert.Contains($"<option value=\"{ClassId}\" data-campus=\"{CampusId}\">MTH07 — Mathematics</option>", editor);
        Assert.Contains("data-utc-for=\"PublishAt\"", editor);
        Assert.Matches("id=\"aud-role-Parents\" checked=\"checked\"", editor); // a new post is for everyone

        var saved = await client.PostAsync("/Portal/SaveNews", Form(TokenOf(editor), ("Title", "Concert"), ("Body", "Hall at 6."),
            ("Audience.CampusId", CampusId), ("Audience.Roles", "Parents"), ("Audience.Roles", "Students"), ("PublishAt", "2026-12-01T13:00:00.000Z"),
            ("Pinned", "true"), ("submit", "publish")));
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        Assert.Equal($"/Portal/NewsPost/{PostId}", saved.Headers.Location!.ToString());
        using (var body = JsonDocument.Parse(api.Body("POST", "/api/v1/community/news")))
        {
            var r = body.RootElement;
            Assert.Equal(("Concert", true, true), (r.GetProperty("title").GetString(), r.GetProperty("pinned").GetBoolean(), r.GetProperty("publish").GetBoolean()));
            Assert.Equal(new DateTime(2026, 12, 1, 13, 0, 0, DateTimeKind.Utc), r.GetProperty("publishAt").GetDateTime().ToUniversalTime());
            Assert.Equal(CampusId, r.GetProperty("audience").GetProperty("campusId").GetString());
            Assert.Equal(["Parents", "Students"], r.GetProperty("audience").GetProperty("roles").EnumerateArray().Select(x => x.GetString()));
        }

        api.Refuse = true;
        var refused = await client.PostAsync("/Portal/SaveNews", Form(TokenOf(editor), ("Title", "Kept title"), ("Body", "Kept body"), ("submit", "draft")));
        Assert.Equal(HttpStatusCode.OK, refused.StatusCode);
        var again = WebUtility.HtmlDecode(await refused.Content.ReadAsStringAsync());
        Assert.Contains("You write for your own campus.", again);
        Assert.Contains("value=\"Kept title\"", again);
        Assert.Contains(">Kept body</textarea>", again);
        Assert.False(JsonDocument.Parse(api.Body("POST", "/api/v1/community/news")).RootElement.GetProperty("publish").GetBoolean()); // a draft
    }

    [Fact]
    public async Task Events_ReplyAndForms_FillInWithSeveralChoices_AndTheAnswersPage()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);

        var events = WebUtility.HtmlDecode(await client.GetStringAsync("/Portal/SchoolEvents"));
        Assert.Contains("data-event=\"Parents' evening\"", events);
        Assert.Contains("3 going · 1 maybe · 0 not going", events);
        Assert.Contains("5 of 40 places taken", events);
        Assert.Contains($"href=\"/Portal/FillForm/{FormId}\"", events);
        Assert.DoesNotContain("data-new-event", events);           // students do not write events
        var replied = await client.PostAsync("/Portal/ReplyEvent", Form(TokenOf(events), ("id", EventId), ("answer", "Going"), ("guests", "1"), ("note", "With my brother")));
        Assert.Equal($"/Portal/SchoolEvents#event-{EventId}", replied.Headers.Location!.ToString());
        Assert.Equal("""{"answer":"Going","guests":1,"note":"With my brother"}""", api.Body("POST", $"/api/v1/community/events/{EventId}/reply"));

        var forms = WebUtility.HtmlDecode(await client.GetStringAsync("/Portal/SchoolForms"));
        Assert.Contains("data-form-to-fill=\"waiting\"", forms);
        var fill = WebUtility.HtmlDecode(await client.GetStringAsync($"/Portal/FillForm?id={FormId}"));
        Assert.Contains("type=\"radio\" name=\"q_" + GoField + "\"", fill);
        Assert.Contains("type=\"checkbox\" name=\"q_" + AllergyField + "\"", fill);
        Assert.Contains("Type your full name to sign.", fill);
        var sent = await client.PostAsync("/Portal/SubmitForm", Form(TokenOf(fill), ("id", FormId), ("q_" + GoField, "Yes"), ("q_" + AllergyField, "Nuts"),
            ("q_" + AllergyField, "Eggs"), ("q_" + NameField, "Ali Raza")));
        Assert.Equal("/Portal/SchoolForms", sent.Headers.Location!.ToString());
        using (var body = JsonDocument.Parse(api.Body("POST", $"/api/v1/community/forms/{FormId}/responses")))
        {
            var a = body.RootElement.GetProperty("answers");
            Assert.Equal("Nuts\nEggs", a.GetProperty(AllergyField).GetString());
            Assert.Equal("Yes", a.GetProperty(GoField).GetString());
        }
        Assert.Contains("Thank you. Your answers are saved.", WebUtility.HtmlDecode(await client.GetStringAsync("/Portal/SchoolForms")));

        api.Refuse = true;
        var refused = await client.PostAsync("/Portal/SubmitForm", Form(TokenOf(fill), ("id", FormId), ("q_" + NameField, "Typed name")));
        var kept = WebUtility.HtmlDecode(await refused.Content.ReadAsStringAsync());
        Assert.Contains("Answer \"May you go?\".", kept);
        Assert.Contains("value=\"Typed name\"", kept);
    }

    [Fact]
    public async Task FormAnswers_TrackingRemindAndDownload_ForTheAuthor()
    {
        var api = new FakeApi("Faculty");
        using var client = await SignedInClientAsync(api);
        var page = WebUtility.HtmlDecode(await client.GetStringAsync($"/Portal/FormResponses?id={FormId}"));
        Assert.Contains("1 of 2 answered", page);
        Assert.Contains("data-tracking-row=\"waiting\"", page);
        Assert.Contains("data-no-parent", page);
        Assert.Contains("Yes: 1", page);
        Assert.Contains($"href=\"/Portal/ExportForm/{FormId}\"", page);

        var reminded = await client.PostAsync("/Portal/RemindForm", Form(TokenOf(page), ("id", FormId)));
        Assert.Contains("Reminder sent to 2 people.", WebUtility.HtmlDecode(await client.GetStringAsync(reminded.Headers.Location!.ToString())));

        var file = await client.GetAsync($"/Portal/ExportForm?id={FormId}");
        Assert.Equal("text/csv", file.Content.Headers.ContentType!.MediaType);
        Assert.StartsWith("For,Class", await file.Content.ReadAsStringAsync());

        var builder = WebUtility.HtmlDecode(await client.GetStringAsync($"/Portal/EditForm?id={FormId}"));
        Assert.Contains("data-questions-locked", builder);               // answers have come in
        Assert.Contains("name=\"Fields[1].Label\" value=\"Allergies\"", builder);
    }

    [Fact]
    public async Task MenuWithoutNews_ClosesThePage_AndUrduIsTranslated()
    {
        using (var closed = await SignedInClientAsync(new FakeApi("Student") { MenuHasCommunity = false }))
            Assert.Equal(HttpStatusCode.Redirect, (await closed.GetAsync("/Portal/News")).StatusCode);

        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);
        var html = await client.GetStringAsync("/Portal/News");
        await client.PostAsync("/Language/Set", Form(TokenOf(html), ("lang", "ur"), ("returnUrl", "/")));
        var urdu = WebUtility.HtmlDecode(await client.GetStringAsync("/Portal/SchoolEvents"));
        Assert.Contains("<html lang=\"ur\" dir=\"rtl\"", urdu);
        Assert.Contains("آ رہے ہیں", urdu);   // Going
        Assert.Contains("3 آ رہے ہیں · 1 شاید · 0 نہیں آ رہے", urdu);
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
        public bool MenuHasCommunity { get; set; } = true;
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

        private const string Audience = """{"campusId":"e2000000-0000-0000-0000-000000000004","campusName":"Gulberg","roles":["Students","Parents","Staff"]}""";

        private static string Fields =>
            $$"""[{"id":"{{GoField}}","type":"YesNo","label":"May you go?","required":true,"options":[]},{"id":"{{AllergyField}}","type":"MultipleChoice","label":"Allergies","required":false,"options":["Nuts","Dairy","Eggs"]},{"id":"{{NameField}}","type":"Signature","label":"Your full name","required":true,"options":[]}]""";

        private static string FormJson => $$"""{"id":"{{FormId}}","title":"Zoo trip","status":"Open","takesAnswers":true,"audience":{{Audience}},"fields":{{Fields}},"answers":{},"canManage":true}""";

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
                return Json("[" + (MenuHasCommunity
                        ? """{"id":"00000000-0000-0000-0000-0000000000f1","key":"news","name":"School News","purpose":"","parentId":null,"displayOrder":47,"isActive":true,"isSystemMenu":false,"roleAccesses":[],"subMenus":[]},{"id":"00000000-0000-0000-0000-0000000000f2","key":"events","name":"Events","purpose":"","parentId":null,"displayOrder":48,"isActive":true,"isSystemMenu":false,"roleAccesses":[],"subMenus":[]},{"id":"00000000-0000-0000-0000-0000000000f3","key":"forms","name":"Forms","purpose":"","parentId":null,"displayOrder":49,"isActive":true,"isSystemMenu":false,"roleAccesses":[],"subMenus":[]},"""
                        : "")
                    + """{"id":"00000000-0000-0000-0000-0000000000e3","key":"dashboard","name":"Dashboard","purpose":"","parentId":null,"displayOrder":1,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]}]""");

            const string c = "/api/v1/community/";
            if (path == c + "news" && request.Method == HttpMethod.Get)
                return Json($$"""[{"id":"{{PostId}}","title":"Sports day","summary":"Friday on the big field.","publishAt":"2026-11-27T08:00:00","isPinned":true,"isNew":true,"authorName":"Mrs Admin","audience":{{Audience}}}]""");
            if (path == c + $"news/{PostId}")
                return Json($$"""{"id":"{{PostId}}","title":"Sports day","body":"Friday on the big field.\n\nBring your kit.","publishAt":"2026-11-27T08:00:00","isPinned":true,"status":"Published","authorName":"Mrs Admin","audience":{{Audience}},"canManage":true,"seenBy":12,"audienceSize":40}""");
            if (path == c + "audiences")
                return Json($$"""{"wholeSchool":true,"campuses":[{"id":"{{CampusId}}","name":"Gulberg"}],"semesters":[],"classes":[{"id":"{{ClassId}}","name":"MTH07 — Mathematics","campusId":"{{CampusId}}"}]}""");
            if (path == c + "news" && request.Method == HttpMethod.Post)
                return Refuse
                    ? Json("""{"message":"You write for your own campus."}""", HttpStatusCode.BadRequest)
                    : Json($$"""{"id":"{{PostId}}","title":"Concert","body":"Hall at 6.","publishAt":"2026-12-01T13:00:00","isPinned":true,"status":"Published","authorName":"Mrs Admin","audience":{{Audience}},"canManage":true}""", HttpStatusCode.Created);
            if (path == c + "events")
                return Json($$"""[{"id":"{{EventId}}","title":"Parents' evening","location":"Main hall","startsAt":"2026-12-05T13:00:00","endsAt":"2026-12-05T15:00:00","allDay":false,"category":"Meeting","status":"Published","repliesOpen":true,"repliesCloseAt":"2026-12-05T13:00:00","capacity":40,"maxGuests":2,"formId":"{{FormId}}","myGuests":0,"counts":{"going":3,"notGoing":0,"maybe":1,"places":5},"canReply":true,"canManage":false,"audience":{{Audience}},"authorName":"Mrs Admin"}]""");
            if (path == c + $"events/{EventId}/reply") return Json("{}");
            if (path == c + "forms")
                return Json($$"""[{"formId":"{{FormId}}","title":"Zoo trip","answered":false,"open":true}]""");
            if (path == c + $"forms/{FormId}") return Json(FormJson);
            if (path == c + "forms/manage")
                return Json($$"""[{"id":"{{FormId}}","title":"Zoo trip","status":"Open","audience":{{Audience}},"answered":1,"authorName":"Mr Siddiqui"}]""");
            if (path == c + $"forms/{FormId}/responses")
                return Refuse ? Json("""{"message":"Answer \"May you go?\"."}""", HttpStatusCode.BadRequest) : Json(FormJson);
            if (path == c + $"forms/{FormId}/tracking")
                return Json($$"""{"form":{{FormJson}},"expected":2,"answered":1,"rows":[{"kind":"Parent","name":"Ali Raza","className":"Class 7","parents":"Nadia Raza","answered":true,"answeredBy":"Nadia Raza","answeredAt":"2026-11-28T09:00:00","noParentAccount":false},{"kind":"Parent","name":"Bilal Khan","className":"Class 7","answered":false,"noParentAccount":true}],"summary":[{"fieldId":"{{GoField}}","label":"May you go?","type":"YesNo","counts":{"Yes":1,"No":0} } ]}""");
            if (path == c + $"forms/{FormId}/remind") return Json("""{"reminded":2}""");
            if (path == c + $"forms/{FormId}/export")
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("For,Class\r\nAli Raza,Class 7\r\n", Encoding.UTF8, "text/csv") };
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
