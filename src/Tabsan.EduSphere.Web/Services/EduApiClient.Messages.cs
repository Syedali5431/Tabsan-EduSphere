using System.Text.Json;

namespace Tabsan.EduSphere.Web.Services;

// Messages between teachers, students, parents and the school office.

public partial interface IEduApiClient
{
    /// <summary>JSON call under api/v1/messages/ (null body = none).</summary>
    Task<ApiRaw> MessagesSendAsync(HttpMethod method, string path, object? body, CancellationToken ct);
}

public partial class EduApiClient
{
    public async Task<ApiRaw> MessagesSendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var json = body is null ? null : JsonSerializer.Serialize(body, CamelCase);
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(method, "api/v1/messages/" + path.TrimStart('/'));
            if (json is not null) request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return request;
        }, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }
}
