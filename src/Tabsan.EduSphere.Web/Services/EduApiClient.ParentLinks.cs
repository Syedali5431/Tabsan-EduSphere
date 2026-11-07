using System.Text;

namespace Tabsan.EduSphere.Web.Services;

// Parent Links screen (2026-10-30) — api/v1/parent-portal/ lookups and links.

public partial interface IEduApiClient
{
    /// <summary>JSON call under api/v1/parent-portal/ with the signed-in user's token.</summary>
    Task<ApiRaw> ParentPortalSendAsync(HttpMethod method, string path, string? jsonBody, CancellationToken ct);
}

public partial class EduApiClient
{
    public async Task<ApiRaw> ParentPortalSendAsync(HttpMethod method, string path, string? jsonBody, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(method, "api/v1/parent-portal/" + path.TrimStart('/'));
            if (jsonBody is not null) request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            return request;
        }, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }
}
