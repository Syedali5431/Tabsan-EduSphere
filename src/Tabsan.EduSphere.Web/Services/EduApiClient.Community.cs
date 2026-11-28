using System.Text.Json;

namespace Tabsan.EduSphere.Web.Services;

// School news, events and online forms.

public partial interface IEduApiClient
{
    /// <summary>JSON call under api/v1/community/ (null body = none).</summary>
    Task<ApiRaw> CommunitySendAsync(HttpMethod method, string path, object? body, CancellationToken ct);

    /// <summary>A file under api/v1/community/ (status, bytes, content type, file name).</summary>
    Task<(int Status, byte[] Content, string ContentType, string? FileName)> CommunityFileAsync(string path, CancellationToken ct);
}

public partial class EduApiClient
{
    public async Task<ApiRaw> CommunitySendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var json = body is null ? null : JsonSerializer.Serialize(body, CamelCase);
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(method, "api/v1/community/" + path.TrimStart('/'));
            if (json is not null) request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return request;
        }, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<(int Status, byte[] Content, string ContentType, string? FileName)> CommunityFileAsync(string path, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() => CreateRequest(HttpMethod.Get, "api/v1/community/" + path.TrimStart('/')), ct);
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        return ((int)response.StatusCode, bytes, response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream",
            response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
    }
}
