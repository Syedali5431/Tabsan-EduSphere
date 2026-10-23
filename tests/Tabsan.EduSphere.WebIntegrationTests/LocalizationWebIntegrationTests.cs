using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tabsan.EduSphere.Web;

namespace Tabsan.EduSphere.WebIntegrationTests;

/// <summary>
/// Plan M11 — the portal's language: ?lang, cookie, the signed-in user's saved choice, the institution default and the
/// browser's language; right-to-left pages; translated menus and flash messages; the API told the language; the switcher;
/// and the Language &amp; Translations page (SuperAdmin only, Excel export/import as overrides, default language).
/// </summary>
public class LocalizationWebIntegrationTests : IClassFixture<WebApplicationFactory<WebEntryPointMarker>>, IDisposable
{
    private readonly WebApplicationFactory<WebEntryPointMarker> _baseFactory;
    private readonly string _overrides = Path.Combine(Path.GetTempPath(), "edusphere-overrides-" + Guid.NewGuid().ToString("N"));

    public LocalizationWebIntegrationTests(WebApplicationFactory<WebEntryPointMarker> factory) => _baseFactory = factory;

    public void Dispose()
    {
        if (Directory.Exists(_overrides)) Directory.Delete(_overrides, recursive: true);
    }

    [Fact]
    public async Task LoginPage_FollowsQuery_Default_Browser_AndTheSwitcherCookie()
    {
        var english = Client(new FakeApi("Student"));
        var page = await english.GetStringAsync("/Login");
        Assert.Contains("<html lang=\"en\" dir=\"ltr\">", page);
        Assert.Contains("Sign in to your account", page);
        Assert.Contains("bootstrap.min.css", page);
        Assert.Contains("data-lang-switcher", page);

        var arabic = await english.GetStringAsync("/Login?lang=ar");
        Assert.Contains("<html lang=\"ar\" dir=\"rtl\">", arabic);
        Assert.Contains("سجّل الدخول إلى حسابك", arabic);
        Assert.Contains("bootstrap.rtl.min.css", arabic);
        Assert.Contains("id=\"portal-text\"", arabic); // script texts for this language

        Assert.Contains("اپنے اکاؤنٹ میں سائن اِن کریں", await Client(new FakeApi("Student") { DefaultLanguage = "ur" }).GetStringAsync("/Login"));

        var auto = Client(new FakeApi("Student") { DefaultLanguage = "auto" });
        Assert.Contains("lang=\"ar\"", await Get(auto, "/Login", "ar-SA,ar;q=0.9"));
        Assert.Contains("lang=\"en\"", await Get(auto, "/Login", "fr-FR"));
        Assert.Contains("lang=\"en\"", await Get(Client(new FakeApi("Student")), "/Login", "ar")); // default "en": the browser is not asked

        // The switcher remembers the choice in a cookie, before anyone signs in.
        var switched = await english.PostAsync("/Language/Set", Form(Token(page), ("lang", "ur"), ("returnUrl", "/Login")));
        Assert.Equal(HttpStatusCode.Redirect, switched.StatusCode);
        Assert.Equal("/Login", switched.Headers.Location!.ToString());
        Assert.Contains("lang=\"ur\"", await english.GetStringAsync("/Login"));
        var unsafeReturn = await english.PostAsync("/Language/Set", Form(Token(page), ("lang", "ar"), ("returnUrl", "https://evil.example/")));
        Assert.Equal("/", unsafeReturn.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, (await english.PostAsync("/Language/Set", Form(Token(page), ("lang", "fr")))).StatusCode);
        await english.PostAsync("/Language/Set", Form(Token(page), ("lang", ""), ("returnUrl", "/Login")));
        Assert.Contains("lang=\"en\"", await english.GetStringAsync("/Login"));
    }

    [Fact]
    public async Task SignedIn_TheSavedLanguageWins_MenusAreTranslated_AndTheApiIsTold()
    {
        var api = new FakeApi("SuperAdmin") { MyLanguage = "ar" };
        var client = await SignedInAsync(api);
        var page = await client.GetStringAsync("/Portal/LanguageSettings");
        Assert.Contains("<html lang=\"ar\" dir=\"rtl\"", page);
        Assert.Contains("اللغة والترجمات", page);
        Assert.Contains("class=\"menu-link-text\">الحصص المباشرة</span>", page); // the menu name from the API, translated
        Assert.Contains("ar", api.LanguagesSent);
        // t() must exist before page scripts run (they call it while loading): i18n.js after the texts, not deferred.
        var helper = Regex.Match(page, "<script src=\"/js/i18n\\.js[^>]*>");
        Assert.True(helper.Success);
        Assert.DoesNotContain("defer", helper.Value);
        Assert.True(page.IndexOf("id=\"portal-text\"", StringComparison.Ordinal) < helper.Index);

        // Switching saves the choice for the user and changes this session at once.
        var switched = await client.PostAsync("/Language/Set", Form(Token(page), ("lang", "ur"), ("returnUrl", "/Portal/LanguageSettings")));
        Assert.Equal("/Portal/LanguageSettings", switched.Headers.Location!.ToString());
        Assert.Contains("\"language\":\"ur\"", api.Body("PUT", "/api/v1/localization/me"));
        var urdu = await client.GetStringAsync("/Portal/LanguageSettings");
        Assert.Contains("lang=\"ur\"", urdu);
        Assert.Contains("زبان اور تراجم", urdu);

        // Flash messages come back in the language of the request that set them.
        await client.PostAsync("/Portal/SaveDefaultLanguage", Form(Token(urdu), ("language", "ar")));
        Assert.Contains("\"language\":\"ar\"", api.Body("PUT", "/api/v1/localization/default"));
        Assert.Contains("طے شدہ زبان محفوظ ہو گئی۔", await client.GetStringAsync("/Portal/LanguageSettings"));
    }

    [Fact]
    public async Task Translations_SuperAdminExportsAndImports_AndOverridesAreUsedAtOnce()
    {
        var denied = await (await SignedInAsync(new FakeApi("Faculty"))).GetAsync("/Portal/LanguageSettings");
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);

        var client = await SignedInAsync(new FakeApi("SuperAdmin"));
        var page = await client.GetStringAsync("/Portal/LanguageSettings");
        Assert.Contains("data-translation-coverage", page);
        Assert.Matches(new Regex("data-language=\"ur\">\\s*<td>Urdu"), page);

        var export = await client.GetAsync("/Portal/ExportTranslations");
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", export.Content.Headers.ContentType!.MediaType);
        using var workbook = new XLWorkbook(new MemoryStream(await export.Content.ReadAsByteArrayAsync()));
        var sheet = workbook.Worksheet("Translations");
        Assert.True(sheet.LastRowUsed()!.RowNumber() > 2500);
        var row = sheet.RowsUsed().First(r => r.Cell(1).GetString() == "Export to Excel");
        row.Cell(2).Value = "ایکسل فائل بنائیں";
        using var edited = new MemoryStream();
        workbook.SaveAs(edited);

        var upload = new MultipartFormDataContent
        {
            { new StringContent(Token(page)), "__RequestVerificationToken" },
            { new ByteArrayContent(edited.ToArray()), "file", "translations.xlsx" }
        };
        var imported = await client.PostAsync("/Portal/ImportTranslations", upload);
        Assert.Equal(HttpStatusCode.Redirect, imported.StatusCode);
        var after = await client.GetStringAsync("/Portal/LanguageSettings");
        Assert.Contains("Translations imported", WebUtility.HtmlDecode(after));
        Assert.True(File.Exists(Path.Combine(_overrides, "ur.json")));
        Assert.Contains("ایکسل فائل بنائیں", await client.GetStringAsync("/Portal/LanguageSettings?lang=ur"));

        var wrong = new MultipartFormDataContent { { new StringContent(Token(page)), "__RequestVerificationToken" }, { new StringContent("x"), "file", "notes.txt" } };
        await client.PostAsync("/Portal/ImportTranslations", wrong);
        Assert.Contains("Choose an .xlsx file to import.", await client.GetStringAsync("/Portal/LanguageSettings"));
    }

    // ── helpers ──

    private HttpClient Client(FakeApi api)
    {
        var factory = _baseFactory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.UseSetting("EduApi:BaseUrl", FakeApi.Base);
            b.UseSetting("Localization:OverridesPath", _overrides);
            b.ConfigureTestServices(s => s.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>(new FakeHttpClientFactory(api))));
        });
        return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true, BaseAddress = new Uri("https://localhost") });
    }

    private async Task<HttpClient> SignedInAsync(FakeApi api)
    {
        var client = Client(api);
        var page = await client.GetStringAsync("/Login");
        var login = await client.PostAsync("/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(page), ["username"] = "tester", ["password"] = "Password#123"
        }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        return client;
    }

    private static async Task<string> Get(HttpClient client, string path, string acceptLanguage)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.AcceptLanguage.ParseAdd(acceptLanguage);
        using var response = await client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    private static string Token(string html)
        => Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields)
        => new(new[] { new KeyValuePair<string, string>("__RequestVerificationToken", token) }.Concat(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value))));

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = new Uri(FakeApi.Base + "/") };
    }

    private sealed class FakeApi(string role) : HttpMessageHandler
    {
        public const string Base = "https://api.test";
        private readonly List<(string Method, string Path, string Body)> _writes = new();
        public string DefaultLanguage { get; set; } = "en";
        public string? MyLanguage { get; set; }
        public HashSet<string> LanguagesSent { get; } = new();

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
            foreach (var language in request.Headers.AcceptLanguage) LanguagesSent.Add(language.Value!);
            if (request.Method != HttpMethod.Get && path != "/api/v1/auth/login") _writes.Add((request.Method.Method, path, body));

            if (path == "/api/v1/auth/login")
                return Json(JsonSerializer.Serialize(new
                {
                    accessToken = Jwt(), refreshToken = "r", accessTokenExpiry = DateTime.UtcNow.AddMinutes(15), role, userId = Guid.NewGuid(),
                    username = "tester", mustChangePassword = false, mfaEnabled = false, ssoEnabled = false, sessionRiskLevel = "low"
                }));
            if (path == "/api/v1/localization" && request.Method == HttpMethod.Get)
                return Json(JsonSerializer.Serialize(new { defaultLanguage = DefaultLanguage }));
            if (path == "/api/v1/localization/me" && request.Method == HttpMethod.Get)
                return Json(MyLanguage is null ? "{}" : JsonSerializer.Serialize(new { language = MyLanguage }));
            if (path == "/api/v1/localization/me")
            {
                MyLanguage = JsonDocument.Parse(body).RootElement.GetProperty("language").GetString();
                return Json(body);
            }
            if (path == "/api/v1/localization/default")
            {
                DefaultLanguage = JsonDocument.Parse(body).RootElement.GetProperty("language").GetString()!;
                return Json(body);
            }
            if (path == "/api/v1/sidebar-menu/my-visible")
                return Json("""[{"id":"00000000-0000-0000-0000-0000000000c1","key":"live_classes","name":"Live Classes","purpose":"","parentId":null,"displayOrder":2,"isActive":true,"isSystemMenu":false,"roleAccesses":[],"subMenus":[]},{"id":"00000000-0000-0000-0000-0000000000c2","key":"language_settings","name":"Language & Translations","purpose":"","parentId":null,"displayOrder":3,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]},{"id":"00000000-0000-0000-0000-0000000000c3","key":"dashboard","name":"Dashboard","purpose":"","parentId":null,"displayOrder":1,"isActive":true,"isSystemMenu":true,"roleAccesses":[],"subMenus":[]}]""");

            return path.Contains("menu", StringComparison.OrdinalIgnoreCase) || path.Contains("notification", StringComparison.OrdinalIgnoreCase)
                || path.Contains("offering", StringComparison.OrdinalIgnoreCase) || path.Contains("course", StringComparison.OrdinalIgnoreCase)
                || path.Contains("live", StringComparison.OrdinalIgnoreCase)
                ? Json("[]")
                : Json("{}");
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
