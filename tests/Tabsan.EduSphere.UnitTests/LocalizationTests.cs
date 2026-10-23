using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using QuestPDF.Infrastructure;
using Tabsan.EduSphere.API.Middleware;
using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Localization;
using Tabsan.EduSphere.Infrastructure.Email;
using Tabsan.EduSphere.Infrastructure.Exporters;
using Tabsan.EduSphere.Web.Localization;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>
/// Plan M11 — languages, request language, API catalogue, portal text (HTML-safe, formats, patterns), cultures, translation
/// files (complete, placeholders kept, every view/script text has a key), Excel round trip, localized emails, API messages, RTL PDFs.
/// </summary>
public class LocalizationTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string WebRoot = Path.Combine(RepoRoot, "src", "Tabsan.EduSphere.Web");
    private static readonly string Shipped = Path.Combine(WebRoot, "Localization", "translations");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Tabsan.EduSphere.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private static Dictionary<string, string> Json(string path) => JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))!;

    private static string Holes(string text) => string.Join(",", Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).OrderBy(v => v, StringComparer.Ordinal));

    /// <summary>Runs <paramref name="body"/> with the UI culture of <paramref name="lang"/>.</summary>
    private static T InLanguage<T>(string lang, Func<T> body)
    {
        var (culture, ui) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = PortalCultures.For(lang);
            return body();
        }
        finally { (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (culture, ui); }
    }

    // ── languages ───────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("ur-PK", "ur")]
    [InlineData("AR_sa", "ar")]
    [InlineData("en-AU", "en")]
    [InlineData("fr", null)]
    [InlineData(" ", null)]
    public void Normalize_KeepsSupportedLanguagesOnly(string input, string? expected)
        => Assert.Equal(expected, PortalLanguages.Normalize(input));

    [Theory]
    [InlineData("ar-SA,ar;q=0.9,en;q=0.8", "ar")]
    [InlineData("en-US,en;q=0.9,ar;q=0.8", "en")]
    [InlineData("fr-FR,ur;q=0.5,ar;q=0.4", "ur")]
    [InlineData("de;q=1, ar;q=0", null)]
    [InlineData("", null)]
    public void AcceptLanguage_PicksTheBestSupportedLanguage(string header, string? expected)
        => Assert.Equal(expected, PortalLanguages.FromAcceptLanguage(header));

    [Fact]
    public void UrduAndArabicAreRightToLeft()
    {
        Assert.True(PortalLanguages.IsRtl("ur"));
        Assert.True(PortalLanguages.IsRtl("ar"));
        Assert.False(PortalLanguages.IsRtl("en"));
        Assert.Equal("اردو", PortalLanguages.NativeName("ur"));
    }

    // ── cultures used for formatting and form input ─────────────────────────────────────────────

    [Theory]
    [InlineData("ur")]
    [InlineData("ar")]
    public void RightToLeftCultures_UseGregorianDatesAndDotDecimals(string lang)
    {
        var culture = PortalCultures.For(lang);
        Assert.IsType<GregorianCalendar>(culture.DateTimeFormat.Calendar);
        Assert.Equal(".", culture.NumberFormat.NumberDecimalSeparator);
        // What forms post (datetime-local, number inputs) must parse to the same values as in English.
        Assert.Equal(new DateTime(2026, 11, 3, 14, 0, 0), DateTime.Parse("2026-11-03T14:00", culture));
        Assert.Equal(12.5m, decimal.Parse("12.5", culture));
        Assert.Equal("2026", new DateTime(2026, 11, 3).ToString("yyyy", culture));
        Assert.Equal("12.50", 12.5m.ToString("F2", culture));
    }

    // ── API catalogue ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TextLocalizer_TranslatesExactAndPatternMessages_AndLeavesEnglishAlone()
    {
        var text = new TextLocalizer();
        Assert.Equal("لم يتم العثور على المستخدم.", text.Translate("User not found.", "ar"));
        Assert.Equal("صارف 42 نہیں ملا۔", text.Translate("User 42 not found.", "ur"));
        Assert.Equal("الحقل Email مطلوب.", text.Translate("The Email field is required.", "ar"));
        Assert.Equal("User not found.", text.Translate("User not found.", "en"));
        Assert.Equal("Something nobody translated.", text.Translate("Something nobody translated.", "ur"));
        Assert.True(text.Has("Your password has been reset", "ur"));
        Assert.Equal("لم يتم العثور على المستخدم.", InLanguage("ar", () => text.Translate("User not found.")));
    }

    [Fact]
    public void ApiCatalogue_IsCompleteInBothLanguages_WithTheSamePlaceholders()
    {
        var resources = Path.Combine(RepoRoot, "src", "Tabsan.EduSphere.Application", "Localization", "Resources");
        var ur = Json(Path.Combine(resources, "api-ur.json"));
        var ar = Json(Path.Combine(resources, "api-ar.json"));
        Assert.Equal(ur.Keys.OrderBy(k => k), ar.Keys.OrderBy(k => k));
        foreach (var (key, value) in ur)
        {
            Assert.False(string.IsNullOrWhiteSpace(value), key);
            Assert.False(string.IsNullOrWhiteSpace(ar[key]), key);
            Assert.Equal(Holes(key), Holes(value));
            Assert.Equal(Holes(key), Holes(ar[key]));
        }
        Assert.Equal(ur.Count, TextLocalizer.Keys().Count); // the files are embedded in the assembly
    }

    // ── portal translation files (the CI missing-key report) ────────────────────────────────────

    [Fact]
    public void PortalTranslations_CoverEveryKey_WithTheSamePlaceholders()
    {
        var keys = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(Path.Combine(Shipped, "keys.json")))!;
        foreach (var lang in new[] { "ur", "ar" })
        {
            var values = Json(Path.Combine(Shipped, $"{lang}.json"));
            var missing = keys.Keys.Where(k => !values.TryGetValue(k, out var v) || string.IsNullOrWhiteSpace(v)).ToList();
            Assert.True(missing.Count == 0, $"{lang}: {missing.Count} missing — {string.Join(" | ", missing.Take(10))}");
            var broken = values.Where(v => Holes(v.Key) != Holes(v.Value)).Select(v => v.Key).ToList();
            Assert.True(broken.Count == 0, $"{lang}: placeholders differ — {string.Join(" | ", broken.Take(10))}");
            var unused = values.Keys.Where(k => !keys.ContainsKey(k)).ToList();
            Assert.True(unused.Count == 0, $"{lang}: translations without a key — {string.Join(" | ", unused.Take(10))}");
        }
    }

    [Fact]
    public void EveryTextInTheViewsAndScripts_HasATranslationKey()
    {
        var keys = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(Path.Combine(Shipped, "keys.json")))!;
        var missing = new List<string>();
        var literal = new Regex(@"\bT(?:\[|\.(?:Format|Text|Js)\()\s*""((?:[^""\\]|\\.)*)""", RegexOptions.Compiled);
        foreach (var file in Directory.EnumerateFiles(Path.Combine(WebRoot, "Views"), "*.cshtml", SearchOption.AllDirectories))
        {
            foreach (Match m in literal.Matches(File.ReadAllText(file)))
            {
                var key = WebUtility.HtmlDecode(Regex.Unescape(m.Groups[1].Value)).Trim();
                if (key.Length > 0 && !keys.ContainsKey(key)) missing.Add($"{Path.GetFileName(file)}: {key}");
            }
        }
        var script = new Regex(@"\bt\('((?:[^'\\]|\\.)*)'", RegexOptions.Compiled);
        foreach (var file in Directory.EnumerateFiles(Path.Combine(WebRoot, "wwwroot", "js"), "*.js").Where(f => !f.EndsWith("i18n.js", StringComparison.Ordinal)))
        {
            foreach (Match m in script.Matches(File.ReadAllText(file)))
            {
                var key = m.Groups[1].Value.Replace("\\'", "'").Trim();
                if (!keys.ContainsKey(key)) missing.Add($"{Path.GetFileName(file)}: {key}");
            }
        }
        Assert.True(missing.Count == 0, $"{missing.Count} text(s) without a key: {string.Join(" | ", missing.Take(15))}");
    }

    // ── portal text service ─────────────────────────────────────────────────────────────────────

    private sealed class Env(string root) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = root;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "test";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Testing";
    }

    /// <summary>A store over a temporary copy of small translation files.</summary>
    private static (TranslationStore Store, string Root) TempStore(Dictionary<string, string>? ur = null)
    {
        var root = Path.Combine(Path.GetTempPath(), "edusphere-l10n-" + Guid.NewGuid().ToString("N"));
        var shipped = Path.Combine(root, "Localization", "translations");
        Directory.CreateDirectory(shipped);
        ur ??= new() { ["Save"] = "محفوظ کریں", ["Due {0} · out of {1}"] = "آخری تاریخ {0} · کل {1} میں سے", ["A & B"] = "الف اور ب <ج>", ["Attendance marked for {0} student(s)."] = "{0} طلبہ کی حاضری لگ گئی۔" };
        File.WriteAllText(Path.Combine(shipped, "ur.json"), JsonSerializer.Serialize(ur));
        File.WriteAllText(Path.Combine(shipped, "ar.json"), JsonSerializer.Serialize(new Dictionary<string, string> { ["Save"] = "حفظ" }));
        File.WriteAllText(Path.Combine(shipped, "keys.json"), JsonSerializer.Serialize(ur.Keys.ToDictionary(k => k, _ => new[] { "Views/Test.cshtml" })));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Localization:OverridesPath"] = Path.Combine(root, "overrides") }).Build();
        return (new TranslationStore(new Env(root), config), root);
    }

    private static string Html(IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
        return writer.ToString();
    }

    [Fact]
    public void PortalText_EnglishKeepsTheMarkup_OtherLanguagesAreTranslatedAndEncoded()
    {
        var (store, _) = TempStore();
        var text = new PortalText(store);
        Assert.Equal("A &amp; B", InLanguage("en", () => Html(text["A &amp; B"])));
        Assert.Equal("Save", InLanguage("en", () => Html(text["Save"])));
        Assert.Equal("محفوظ کریں", WebUtility.HtmlDecode(InLanguage("ur", () => Html(text["Save"]))));
        var encoded = InLanguage("ur", () => Html(text["A &amp; B"]));
        Assert.Contains("&lt;ج&gt;", encoded); // a translation can never inject markup
        Assert.Equal("Untranslated", InLanguage("ur", () => Html(text["Untranslated"])));
        Assert.Equal("ur", InLanguage("ur", () => text.Language));
        Assert.True(InLanguage("ar", () => text.IsRtl));
    }

    [Fact]
    public void PortalText_FormatsValues_EncodingTextAndKeepingHtml()
    {
        var (store, _) = TempStore();
        var text = new PortalText(store);
        Assert.Equal("Due &lt;b&gt; · out of 10", InLanguage("en", () => Html(text.Format("Due {0} · out of {1}", "<b>", 10))));
        var urdu = WebUtility.HtmlDecode(InLanguage("ur", () => Html(text.Format("Due {0} · out of {1}", new HtmlString("<em>x</em>"), 10))));
        Assert.Equal("آخری تاریخ <em>x</em> · کل 10 میں سے", urdu);
        Assert.Equal("{2} stays", InLanguage("en", () => Html(text.Format("{2} stays", "a"))));
    }

    [Fact]
    public void PortalText_TranslatesMessagesWithValuesThroughPatterns_AndEscapesForScripts()
    {
        var (store, _) = TempStore();
        var text = new PortalText(store);
        Assert.Equal("12 طلبہ کی حاضری لگ گئی۔", InLanguage("ur", () => text.Text("Attendance marked for 12 student(s).")));
        Assert.Equal("Attendance marked for 12 student(s).", InLanguage("en", () => text.Text("Attendance marked for 12 student(s).")));
        Assert.Equal("It\\u0027s", InLanguage("en", () => text.Js("It's")));
        Assert.DoesNotContain("'", InLanguage("en", () => text.Js("Drop {0}?", "O'Brien's course")));
    }

    [Fact]
    public void Overrides_WinOverShippedTexts_AndOnlyChangesAreStored()
    {
        var (store, root) = TempStore();
        Assert.Equal(1, store.SaveOverrides("ur", new Dictionary<string, string> { ["Save"] = "محفوظ کریں", ["A & B"] = "نیا ترجمہ" }));
        Assert.Equal("نیا ترجمہ", store.Get("ur", "A & B"));
        Assert.Equal("محفوظ کریں", store.Get("ur", "Save"));
        Assert.True(File.Exists(Path.Combine(root, "overrides", "ur.json")));
        Assert.Equal(1, store.OverrideCount("ur"));
    }

    [Fact]
    public void Workbook_ExportsEveryKey_AndImportsTranslatorChanges()
    {
        var (store, _) = TempStore();
        var bytes = TranslationWorkbook.Export(store);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet("Translations");
        Assert.Equal("English (key)", sheet.Cell(1, 1).GetString());
        Assert.Equal("Urdu (ur)", sheet.Cell(1, 2).GetString());
        Assert.Equal("Arabic (ar)", sheet.Cell(1, 3).GetString());
        var saveRow = sheet.RowsUsed().First(r => r.Cell(1).GetString() == "Save");
        Assert.Equal("حفظ", saveRow.Cell(3).GetString());

        saveRow.Cell(2).Value = "محفوظ کیجیے";
        var dueRow = sheet.RowsUsed().First(r => r.Cell(1).GetString() == "Due {0} · out of {1}");
        dueRow.Cell(3).Value = "الموعد بدون عناصر نائبة"; // placeholders dropped → refused
        var extra = sheet.LastRowUsed()!.RowNumber() + 1;
        sheet.Cell(extra, 1).Value = "Not a portal text";
        sheet.Cell(extra, 2).Value = "کچھ";
        using var edited = new MemoryStream();
        workbook.SaveAs(edited);
        edited.Position = 0;

        var result = TranslationWorkbook.Import(store, edited);
        Assert.Equal(1, result.Saved["ur"]);
        Assert.Equal(1, result.UnknownKeys);
        Assert.Single(result.Problems);
        Assert.Equal("محفوظ کیجیے", store.Get("ur", "Save"));
        Assert.Null(store.Get("ar", "Due {0} · out of {1}"));
        var coverage = TranslationWorkbook.Coverage(store);
        Assert.Equal(4, coverage.Single(c => c.Language == "ur").Translated);
        Assert.Equal(1, coverage.Single(c => c.Language == "ar").Translated);
    }

    // ── API messages, emails, PDFs ──────────────────────────────────────────────────────────────

    private static async Task<object?> Filter(object value, string lang)
    {
        var context = new ResultExecutingContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(), new BadRequestObjectResult(value), new object());
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = PortalCultures.For(lang);
            await new LocalizedResponseFilter(new TextLocalizer()).OnResultExecutionAsync(context, () => Task.FromResult<ResultExecutedContext>(null!));
        }
        finally { CultureInfo.CurrentUICulture = previous; }
        return ((ObjectResult)context.Result).Value;
    }

    [Fact]
    public async Task ApiMessages_AreTranslated_KeepingTheJsonShape()
    {
        var arabic = await Filter(new { message = "User not found.", code = 7, detail = (string?)null }, "ar");
        var dict = Assert.IsType<Dictionary<string, object?>>(arabic);
        Assert.Equal("لم يتم العثور على المستخدم.", dict["message"]);
        Assert.Equal(7, dict["code"]);
        Assert.False(dict.ContainsKey("detail")); // nulls stay out, as before

        var english = new { message = "User not found." };
        Assert.Same(english, await Filter(english, "en"));

        var problem = new ValidationProblemDetails(new Dictionary<string, string[]> { ["Email"] = ["The Email field is required."] });
        var validated = Assert.IsType<ValidationProblemDetails>(await Filter(problem, "ur"));
        Assert.Equal("Email کا خانہ لازمی ہے۔", validated.Errors["Email"].Single());
        Assert.Equal("ایک یا زیادہ توثیقی غلطیاں ہوئیں۔", validated.Title);
    }

    [Fact]
    public void Emails_UseTheRecipientsLanguage_WhenATranslatedTemplateExists()
    {
        var renderer = new EmailTemplateRenderer(NullLogger<EmailTemplateRenderer>.Instance);
        var urdu = renderer.Render("password-reset", new Dictionary<string, string> { ["USERNAME"] = "ali<x>", ["LANGUAGE"] = "ur" });
        Assert.Contains("dir=\"rtl\"", urdu);
        Assert.Contains("آپ کا پاس ورڈ ری سیٹ کر دیا گیا ہے", urdu);
        Assert.Contains("ali&lt;x&gt;", urdu);
        var arabic = renderer.Render("notification-alert", new Dictionary<string, string> { ["TITLE"] = "t", ["BODY"] = "b", ["LANGUAGE"] = "ar" });
        Assert.Contains("فتح الإشعارات", arabic);
        var english = renderer.Render("password-reset", new Dictionary<string, string> { ["USERNAME"] = "ali", ["LANGUAGE"] = "en" });
        Assert.Contains("Your Password Has Been Reset", english);
        var unsafeLanguage = renderer.Render("password-reset", new Dictionary<string, string> { ["USERNAME"] = "ali", ["LANGUAGE"] = "../ur" });
        Assert.Contains("Your Password Has Been Reset", unsafeLanguage);
    }

    [Theory]
    [InlineData("ar")]
    [InlineData("ur")]
    [InlineData("en")]
    public void Pdfs_RenderInEveryLanguage(string lang)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var timetable = new TimetableDto(Guid.NewGuid(), Guid.NewGuid(), "کمپیوٹر سائنس", Guid.NewGuid(), "علوم الحاسب", "BSCS", Guid.NewGuid(),
            "Fall 2026", 5, new DateTime(2026, 9, 1), "Timetable", true, DateTime.UtcNow,
            [new TimetableEntryDto(Guid.NewGuid(), 1, "Monday", new TimeOnly(9, 0), new TimeOnly(10, 0), null, "برمجة", null, "احمد علی", null, "101", null, "A")],
            DateTime.UtcNow, null);
        var pdf = InLanguage(lang, () => new TimetablePdfExporter().Export(timetable));
        Assert.True(pdf.Length > 1000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.Equal(PortalLanguages.IsRtl(lang), InLanguage(lang, () => Tabsan.EduSphere.Infrastructure.Reporting.PdfLanguage.IsRtl));
        Assert.Equal(lang == "ar" ? "ملخص الحضور" : lang == "ur" ? "حاضری کا خلاصہ" : "Attendance Summary",
            InLanguage(lang, () => Tabsan.EduSphere.Infrastructure.Reporting.PdfLanguage.Text("Attendance Summary")));
    }
}
