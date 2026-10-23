using System.Text.Json;

namespace Tabsan.EduSphere.Web.Services;

// Plan M11 — portal language: the signed-in user's choice and the institution default.

public partial interface IEduApiClient
{
    /// <summary>The signed-in user's saved language (null = the institution default).</summary>
    Task<string?> GetMyLanguageAsync(CancellationToken ct);

    /// <summary>Saves (or clears with null) the signed-in user's language.</summary>
    Task<ApiRaw> SetMyLanguageAsync(string? language, CancellationToken ct);

    /// <summary>The institution default ("en", "ur", "ar" or "auto"); readable without signing in (login page).</summary>
    Task<string?> GetDefaultLanguageAsync(CancellationToken ct);

    /// <summary>Sets the institution default (SuperAdmin).</summary>
    Task<ApiRaw> SetDefaultLanguageAsync(string language, CancellationToken ct);
}

public partial class EduApiClient
{
    public async Task<string?> GetMyLanguageAsync(CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() => CreateRequest(HttpMethod.Get, "api/v1/localization/me"), ct);
        if (!response.IsSuccessStatusCode) return null;
        return ReadLanguage(await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<ApiRaw> SetMyLanguageAsync(string? language, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(new { language }, CamelCase);
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(HttpMethod.Put, "api/v1/localization/me");
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return request;
        }, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<string?> GetDefaultLanguageAsync(CancellationToken ct)
    {
        var baseUrl = GetConnection().ApiBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = _httpContextAccessor.HttpContext?.RequestServices.GetService<IConfiguration>()?["EduApi:BaseUrl"];
        if (!TryBuildApiBaseUri(baseUrl, out var baseUri)) return null;
        using var client = _httpClientFactory.CreateClient("EduApi");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        using var response = await client.GetAsync(new Uri(baseUri, "api/v1/localization"), timeout.Token);
        if (!response.IsSuccessStatusCode) return null;
        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("defaultLanguage", out var value)
                && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        }
        catch (JsonException) { return null; }
    }

    public async Task<ApiRaw> SetDefaultLanguageAsync(string language, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(new { language }, CamelCase);
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(HttpMethod.Put, "api/v1/localization/default");
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return request;
        }, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    private static string? ReadLanguage(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("language", out var value)
                && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        }
        catch (JsonException) { return null; }
    }
}
