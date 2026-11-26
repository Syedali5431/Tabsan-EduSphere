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
/// The Messages pages against a fake API: the inbox with unread counts, a conversation with its reply box, writing a new
/// message (people grouped and described, what was written kept when the API refuses), archiving, the menu guard and Urdu.
/// </summary>
public class MessagingWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>
{
    private const string ConversationId = "c0000000-0000-0000-0000-000000000001";
    private const string TeacherId = "70000000-0000-0000-0000-000000000001";
    private const string ParentId = "70000000-0000-0000-0000-000000000002";
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;

    public MessagingWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    [Fact]
    public async Task Inbox_Conversation_AndReply()
    {
        var api = new FakeApi("Student");
        using var client = await SignedInClientAsync(api);

        var inbox = WebUtility.HtmlDecode(await client.GetStringAsync("/Portal/Messages"));
        Assert.Contains("data-conversation=\"Science fair\"", inbox);
        Assert.Contains("data-unread=\"2\"", inbox);
        Assert.Contains("Ms Khan: Bring your models on Friday.", inbox);
        Assert.Contains($"href=\"/Portal/MessageThread/{ConversationId}\"", inbox.Replace($"MessageThread?id={ConversationId}", $"MessageThread/{ConversationId}"));
        Assert.Contains("href=\"/Portal/NewMessage\"", inbox);
        Assert.Contains("href=\"/Portal/Messages\"", inbox); // the sidebar's Messages link

        var thread = WebUtility.HtmlDecode(await client.GetStringAsync($"/Portal/MessageThread?id={ConversationId}"));
        Assert.Contains("data-person=\"Ms Khan\"", thread);
        Assert.Contains("(Teacher)", thread);
        Assert.Contains("message-mine", thread);
        Assert.Contains("message-theirs", thread);
        Assert.Contains("Can I bring a friend?", thread);

        var replied = await client.PostAsync("/Portal/ReplyMessage", Form(TokenOf(thread), ("id", ConversationId), ("body", "Thank you!")));
        Assert.Equal(HttpStatusCode.Redirect, replied.StatusCode);
        Assert.Contains(replied.Headers.Location!.ToString(), new[] { $"/Portal/MessageThread?id={ConversationId}", $"/Portal/MessageThread/{ConversationId}" });
        Assert.Equal("""{"body":"Thank you!"}""", api.Body("POST", $"/api/v1/messages/conversations/{ConversationId}/messages"));

        var archived = await client.PostAsync("/Portal/ArchiveConversation", Form(TokenOf(thread), ("id", ConversationId), ("archive", "true")));
        Assert.Equal("/Portal/Messages?archived=False", archived.Headers.Location!.ToString());
        Assert.Contains($"POST /api/v1/messages/conversations/{ConversationId}/archive", api.Writes);

        api.ConversationMissing = true;
        var gone = await client.GetAsync($"/Portal/MessageThread?id={ConversationId}");
        Assert.Equal("/Portal/Messages", gone.Headers.Location!.ToString()); // someone else's or unknown: back to the inbox
    }

    [Fact]
    public async Task NewMessage_GroupsPeople_SendsAndKeepsTheTextWhenRefused()
    {
        var api = new FakeApi("Faculty");
        using var client = await SignedInClientAsync(api);

        var page = WebUtility.HtmlDecode(await client.GetStringAsync($"/Portal/NewMessage?to={ParentId}"));
        Assert.Contains("Science (Class 9)", page);              // a course is its own group
        Assert.Contains("Parent of Ali Raza", page);
        Assert.Contains("School office", page);                  // an admin
        Assert.Matches($"value=\"{ParentId}\" id=\"[^\"]+\" checked=\"checked\"", page); // ?to= ticks the parent
        Assert.Contains("/js/messages.js", page);

        var sent = await client.PostAsync("/Portal/SendMessage", Form(TokenOf(page), ("recipientIds", ParentId), ("recipientIds", TeacherId),
            ("subject", "Homework"), ("body", "Ali did well.")));
        Assert.Equal($"/Portal/Messages", sent.Headers.Location!.ToString()); // two conversations: the inbox says so
        using (var doc = JsonDocument.Parse(api.Body("POST", "/api/v1/messages/conversations")))
        {
            Assert.Equal(2, doc.RootElement.GetProperty("recipientIds").GetArrayLength());
            Assert.Equal("Homework", doc.RootElement.GetProperty("subject").GetString());
        }
        Assert.Contains("Sent: 2 conversations, one for each student or parent.", WebUtility.HtmlDecode(await client.GetStringAsync("/Portal/Messages")));

        api.Refuse = true;
        var refused = await client.PostAsync("/Portal/SendMessage", Form(TokenOf(page), ("recipientIds", ParentId), ("subject", "Again"), ("body", "Kept text")));
        Assert.Equal(HttpStatusCode.OK, refused.StatusCode);
        var again = WebUtility.HtmlDecode(await refused.Content.ReadAsStringAsync());
        Assert.Contains("You cannot write to one of these people.", again);
        Assert.Contains("value=\"Again\"", again);
        Assert.Contains(">Kept text</textarea>", again);
    }

    [Fact]
    public async Task MenuWithoutMessages_ClosesThePages_AndUrduIsRightToLeft()
    {
        var closed = new FakeApi("Student") { MenuHasMessages = false };
        using (var client = await SignedInClientAsync(closed))
            Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Portal/Messages")).StatusCode);

        var api = new FakeApi("Student");
        using var urduClient = await SignedInClientAsync(api);
        var html = await urduClient.GetStringAsync("/Portal/Messages");
        await urduClient.PostAsync("/Language/Set", Form(TokenOf(html), ("lang", "ur"), ("returnUrl", "/")));
        var urdu = WebUtility.HtmlDecode(await urduClient.GetStringAsync("/Portal/Messages"));
        Assert.Contains("<html lang=\"ur\" dir=\"rtl\"", urdu);
        Assert.Contains("نیا پیغام", urdu);   // New message
        Assert.Contains("2 نئے", urdu);        // 2 new
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
        public bool MenuHasMessages { get; set; } = true;
        public bool ConversationMissing { get; set; }
        public bool Refuse { get; set; }
        public IEnumerable<string> Writes => _writes.Select(w => $"{w.Method} {w.Path}");
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
                return Json("[" + (MenuHasMessages ? """{"id":"00000000-0000-0000-0000-0000000000e1","key":"messages","name":"Messages","purpose":"","parentId":null,"displayOrder":46,"isActive":true,"isSystemMenu":false,"roleAccesses":[],"subMenus":[]},""" : "")
                    + """{"id":"00000000-0000-0000-0000-0000000000e3","key":"dashboard","name":"Dashboard","purpose":"","parentId":null,"displayOrder":1,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]}]""");
            if (path == "/api/v1/messages/conversations" && request.Method == HttpMethod.Get)
                return Json($$"""[{"id":"{{ConversationId}}","subject":"Science fair","with":[{"userId":"{{TeacherId}}","name":"Ms Khan","role":"Faculty"}],"lastMessageAt":"2026-11-27T09:05:00","lastMessage":"Bring your models on Friday.","lastSenderId":"{{TeacherId}}","lastSenderName":"Ms Khan","unread":2,"archived":false}]""");
            if (path == $"/api/v1/messages/conversations/{ConversationId}" && request.Method == HttpMethod.Get)
                return ConversationMissing
                    ? Json("""{"message":"Conversation not found."}""", HttpStatusCode.NotFound)
                    : Json($$"""{"id":"{{ConversationId}}","subject":"Science fair","people":[{"userId":"{{TeacherId}}","name":"Ms Khan","role":"Faculty"},{"userId":"70000000-0000-0000-0000-000000000009","name":"Ali Raza","role":"Student"}],"messages":[{"id":"m0000000-0000-0000-0000-000000000001","senderUserId":"{{TeacherId}}","senderName":"Ms Khan","body":"Bring your models on Friday.","sentAt":"2026-11-27T09:00:00","mine":false},{"id":"m0000000-0000-0000-0000-000000000002","senderUserId":"70000000-0000-0000-0000-000000000009","senderName":"Ali Raza","body":"Can I bring a friend?","sentAt":"2026-11-27T09:05:00","mine":true}],"archived":false}""".Replace("m0000000", "a0000000"));
            if (path == "/api/v1/messages/contacts")
                return Json($$"""[{"userId":"{{ParentId}}","name":"Nadia Raza","role":"Parent","detail":"Ali Raza","group":"Science (Class 9)"},{"userId":"{{TeacherId}}","name":"Mr Shah","role":"Admin","group":"Staff"}]""");
            if (path == "/api/v1/messages/conversations" && request.Method == HttpMethod.Post)
                return Refuse
                    ? Json("""{"message":"You cannot write to one of these people."}""", HttpStatusCode.Forbidden)
                    : Json("""{"conversationIds":["c0000000-0000-0000-0000-000000000002","c0000000-0000-0000-0000-000000000003"]}""");
            if (path.StartsWith("/api/v1/messages/", StringComparison.Ordinal) && request.Method == HttpMethod.Post) return Json("{}");
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
