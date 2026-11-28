using System.Text.Json;

namespace Tabsan.EduSphere.Web.Services;

// Pastoral care and wellbeing.

public partial interface IEduApiClient
{
    /// <summary>JSON call under api/v1/pastoral/ (null body = none).</summary>
    Task<ApiRaw> PastoralSendAsync(HttpMethod method, string path, object? body, CancellationToken ct);
}

public partial class EduApiClient
{
    public async Task<ApiRaw> PastoralSendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var json = body is null ? null : JsonSerializer.Serialize(body, CamelCase);
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(method, "api/v1/pastoral/" + path.TrimStart('/'));
            if (json is not null) request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return request;
        }, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }
}
