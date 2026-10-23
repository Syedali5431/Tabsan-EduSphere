using System.Net.Http.Headers;
using System.Text;

namespace Tabsan.EduSphere.Web.Services;

// Plan M13 — SCORM packages (api/v1/scorm/) and the xAPI statement store administration (api/v1/xapi/).

public partial interface IEduApiClient
{
    /// <summary>JSON call under api/v1/scorm/ with the signed-in user's token.</summary>
    Task<ApiRaw> ScormSendAsync(HttpMethod method, string path, string? jsonBody, CancellationToken ct);

    /// <summary>Uploads a package zip (streamed) with its settings.</summary>
    Task<ApiRaw> ScormUploadAsync(Guid offeringId, IFormFile file, string? title, bool graded, decimal? maxMarks, int maxAttempts, bool publish, CancellationToken ct);

    /// <summary>JSON call under api/v1/xapi/ with the signed-in user's token.</summary>
    Task<ApiRaw> XapiSendAsync(HttpMethod method, string path, string? jsonBody, CancellationToken ct);
}

public partial class EduApiClient
{
    /// <summary>Named client for package uploads (large files: a long timeout).</summary>
    public const string ScormUploadClient = "ScormUpload";

    public async Task<ApiRaw> ScormSendAsync(HttpMethod method, string path, string? jsonBody, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(method, "api/v1/scorm/" + path.TrimStart('/'));
            if (jsonBody is not null) request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            return request;
        }, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<ApiRaw> ScormUploadAsync(Guid offeringId, IFormFile file, string? title, bool graded, decimal? maxMarks, int maxAttempts, bool publish,
        CancellationToken ct)
    {
        using var request = CreateRequest(HttpMethod.Post, $"api/v1/scorm/offerings/{offeringId}/packages");
        var part = new StreamContent(file.OpenReadStream(), 81920);
        part.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        var form = new MultipartFormDataContent { { part, "file", Path.GetFileName(file.FileName) } };
        if (!string.IsNullOrWhiteSpace(title)) form.Add(new StringContent(title), "title");
        form.Add(new StringContent(graded ? "true" : "false"), "graded");
        if (maxMarks is { } marks) form.Add(new StringContent(marks.ToString(System.Globalization.CultureInfo.InvariantCulture)), "maxMarks");
        form.Add(new StringContent(maxAttempts.ToString(System.Globalization.CultureInfo.InvariantCulture)), "maxAttempts");
        form.Add(new StringContent(publish ? "true" : "false"), "publish");
        request.Content = form;
        var client = _httpClientFactory.CreateClient(ScormUploadClient);
        using var response = await client.SendAsync(request, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<ApiRaw> XapiSendAsync(HttpMethod method, string path, string? jsonBody, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(method, "api/v1/xapi/" + path.TrimStart('/'));
            if (jsonBody is not null) request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            return request;
        }, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }
}

// Plan M14.3 — personal access tokens (api/v1/api-tokens/).
public partial interface IEduApiClient
{
    /// <summary>JSON call under api/v1/api-tokens/ with the signed-in user's token.</summary>
    Task<ApiRaw> ApiTokensSendAsync(HttpMethod method, string path, string? jsonBody, CancellationToken ct);
}

public partial class EduApiClient
{
    public async Task<ApiRaw> ApiTokensSendAsync(HttpMethod method, string path, string? jsonBody, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(method, ("api/v1/api-tokens/" + path.TrimStart('/')).TrimEnd('/'));
            if (jsonBody is not null) request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            return request;
        }, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }
}
