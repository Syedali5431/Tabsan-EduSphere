using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Plan M11 — languages through the real API and database: the institution default (readable before sign-in, SuperAdmin
/// to change), each user's own language, answers in the caller's language (messages, validation errors, Content-Language),
/// PDFs in Arabic, and recipients' languages for notification emails.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class LocalizationIntegrationTests
{
    private readonly EduSphereWebFactory _factory;

    public LocalizationIntegrationTests(EduSphereWebFactory factory) => _factory = factory;

    private HttpClient Client(string role, Guid? userId = null, string? language = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            JwtTestHelper.GenerateToken(role, (userId ?? Guid.NewGuid()).ToString()));
        if (language is not null) client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);
        return client;
    }

    private async Task<Guid> SeedUserAsync(string? email = null, string? language = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var role = db.Roles.First(r => r.Name == "Faculty");
        var user = new User($"l10n_{suffix}", "integration-hash", role.Id, email ?? $"l10n_{suffix}@tabsan.local");
        user.SetPreferredLanguage(language);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task InstitutionDefault_IsPublic_AndOnlySuperAdminChangesIt()
    {
        var anonymous = _factory.CreateClient();
        var info = await anonymous.GetFromJsonAsync<JsonElement>("api/v1/localization");
        var original = info.GetProperty("defaultLanguage").GetString()!;
        Assert.Equal(new[] { "en", "ur", "ar" }, info.GetProperty("languages").EnumerateArray().Select(l => l.GetProperty("code").GetString()));
        Assert.Contains(info.GetProperty("languages").EnumerateArray(), l => l.GetProperty("code").GetString() == "ar" && l.GetProperty("rtl").GetBoolean());

        var super = Client("SuperAdmin");
        try
        {
            Assert.Equal(HttpStatusCode.OK, (await super.PutAsJsonAsync("api/v1/localization/default", new { language = "ur" })).StatusCode);
            Assert.Equal("ur", (await anonymous.GetFromJsonAsync<JsonElement>("api/v1/localization")).GetProperty("defaultLanguage").GetString());
            Assert.Equal(HttpStatusCode.OK, (await super.PutAsJsonAsync("api/v1/localization/default", new { language = "auto" })).StatusCode);
            Assert.Equal("auto", (await anonymous.GetFromJsonAsync<JsonElement>("api/v1/localization")).GetProperty("defaultLanguage").GetString());
            Assert.Equal(HttpStatusCode.BadRequest, (await super.PutAsJsonAsync("api/v1/localization/default", new { language = "fr" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await Client("Admin").PutAsJsonAsync("api/v1/localization/default", new { language = "ar" })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PutAsJsonAsync("api/v1/localization/default", new { language = "ar" })).StatusCode);
        }
        finally
        {
            await super.PutAsJsonAsync("api/v1/localization/default", new { language = original });
        }
    }

    private static string? Language(JsonElement body)
        => body.TryGetProperty("language", out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    [Fact]
    public async Task UsersChooseTheirOwnLanguage_AndAnswersComeInIt()
    {
        var userId = await SeedUserAsync();
        var me = Client("Faculty", userId);
        Assert.Null(Language(await me.GetFromJsonAsync<JsonElement>("api/v1/localization/me"))); // nulls are left out of answers

        Assert.Equal(HttpStatusCode.OK, (await me.PutAsJsonAsync("api/v1/localization/me", new { language = "AR" })).StatusCode);
        Assert.Equal("ar", Language(await me.GetFromJsonAsync<JsonElement>("api/v1/localization/me")));

        // Errors come back in the caller's language (Accept-Language), with the same JSON shape.
        var arabic = Client("Faculty", userId, "ar");
        var refused = await arabic.PutAsJsonAsync("api/v1/localization/me", new { language = "fr" });
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Equal("ar", refused.Content.Headers.ContentLanguage.Single());
        Assert.Equal("اختر الإنجليزية أو الأردية أو العربية.", (await refused.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
        var english = await me.PutAsJsonAsync("api/v1/localization/me", new { language = "fr" });
        Assert.Equal("Choose English, Urdu or Arabic.", (await english.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
        Assert.Empty(english.Content.Headers.ContentLanguage);

        Assert.Equal(HttpStatusCode.OK, (await me.PutAsJsonAsync("api/v1/localization/me", new { language = (string?)null })).StatusCode);
        Assert.Null(Language(await me.GetFromJsonAsync<JsonElement>("api/v1/localization/me")));
        Assert.Equal(HttpStatusCode.NotFound, (await Client("Faculty").GetAsync("api/v1/localization/me")).StatusCode);
    }

    [Fact]
    public async Task ValidationErrors_AreTranslated()
    {
        var urdu = _factory.CreateClient();
        urdu.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ur-PK, en;q=0.5");
        var response = await urdu.PostAsJsonAsync("api/v1/auth/login", new { username = "", password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("صارف نام لازمی ہے۔", body);
        Assert.Contains("پاس ورڈ لازمی ہے۔", body);

        var english = await _factory.CreateClient().PostAsJsonAsync("api/v1/auth/login", new { username = "", password = "" });
        Assert.Contains("Username is required.", await english.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Pdfs_RenderInArabic()
    {
        var super = Client("SuperAdmin", Guid.Parse("00000000-0000-0000-0000-000000000992"));
        if (!(await super.GetFromJsonAsync<JsonElement>("api/v1/module/reports/status")).GetProperty("isActive").GetBoolean())
            (await super.PostAsync("api/v1/module/reports/activate", null)).EnsureSuccessStatusCode();
        var arabic = Client("SuperAdmin", Guid.Parse("00000000-0000-0000-0000-000000000777"), "ar");
        var pdf = await arabic.GetAsync("api/analytics/performance/export/pdf");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType!.MediaType);
        var bytes = await pdf.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public async Task NotificationEmails_KnowEachRecipientsLanguage()
    {
        var urduUser = await SeedUserAsync(language: "ur");
        var plainUser = await SeedUserAsync();
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var rows = await repo.GetActiveUserEmailLanguagesAsync([urduUser, plainUser]);
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.Language == "ur");
        Assert.Contains(rows, r => r.Language is null);

        var languages = scope.ServiceProvider.GetRequiredService<Tabsan.EduSphere.Application.Localization.IPortalLanguageService>();
        Assert.Equal("ur", await languages.ForRecipientAsync("ur"));
        var fallback = await languages.ForRecipientAsync(null);
        Assert.Contains(fallback, new[] { "en", "ur", "ar" });
    }
}
