using System.Text;

namespace Tabsan.EduSphere.Web.Services;

// Plan M7 — module items, progress and release conditions (api/v1/lms/).

public partial interface IEduApiClient
{
    /// <summary>JSON call under api/v1/lms/ with the signed-in user's token.</summary>
    Task<ApiRaw> ModuleSendAsync(HttpMethod method, string path, string? jsonBody, CancellationToken ct);
}

public partial class EduApiClient
{
    public async Task<ApiRaw> ModuleSendAsync(HttpMethod method, string path, string? jsonBody, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(method, "api/v1/lms/" + path.TrimStart('/'));
            if (jsonBody is not null) request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            return request;
        }, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }
}
