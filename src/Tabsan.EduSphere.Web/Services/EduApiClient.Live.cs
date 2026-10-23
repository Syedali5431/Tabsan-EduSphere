using System.Text.Json;

namespace Tabsan.EduSphere.Web.Services;

// Plan M10 — live classes.

public partial interface IEduApiClient
{
    /// <summary>JSON call under api/v1/live/ (null body = none).</summary>
    Task<ApiRaw> LiveSendAsync(HttpMethod method, string path, object? body, CancellationToken ct);

    /// <summary>Downloads a file under api/v1/live/ (null when the API refuses).</summary>
    Task<(ApiFile? File, ApiRaw? Error)> LiveDownloadAsync(string path, CancellationToken ct);
}

public partial class EduApiClient
{
    public async Task<ApiRaw> LiveSendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var json = body is null ? null : JsonSerializer.Serialize(body, CamelCase);
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(method, "api/v1/live/" + path.TrimStart('/'));
            if (json is not null) request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return request;
        }, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<(ApiFile? File, ApiRaw? Error)> LiveDownloadAsync(string path, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() => CreateRequest(HttpMethod.Get, "api/v1/live/" + path.TrimStart('/')), ct);
        if (!response.IsSuccessStatusCode) return (null, new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct)));
        var name = response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "live-class.ics";
        return (new ApiFile(await response.Content.ReadAsByteArrayAsync(ct), response.Content.Headers.ContentType?.MediaType ?? "text/calendar", name), null);
    }
}
