using System.Net.Http.Headers;
using System.Text;

namespace Tabsan.EduSphere.Web.Services;

// Plan M6 — learning outcomes & mastery (api/v1/outcomes/).

public partial interface IEduApiClient
{
    /// <summary>JSON call under api/v1/outcomes/ with the signed-in user's token.</summary>
    Task<ApiRaw> OutcomeSendAsync(HttpMethod method, string path, string? jsonBody, CancellationToken ct);

    /// <summary>Downloads a file under api/v1/outcomes/ (the error when the API refuses).</summary>
    Task<(ApiFile? File, ApiRaw? Error)> OutcomeDownloadAsync(string path, CancellationToken ct);

    /// <summary>Uploads the outcome import workbook.</summary>
    Task<ApiRaw> OutcomeImportAsync(UploadPart file, CancellationToken ct);
}

public partial class EduApiClient
{
    private const string OutcomeBase = "api/v1/outcomes";

    private static string OutcomePath(string path) => string.IsNullOrEmpty(path) ? OutcomeBase : OutcomeBase + (path.StartsWith('?') ? "" : "/") + path.TrimStart('/');

    public async Task<ApiRaw> OutcomeSendAsync(HttpMethod method, string path, string? jsonBody, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(method, OutcomePath(path));
            if (jsonBody is not null) request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            return request;
        }, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<(ApiFile? File, ApiRaw? Error)> OutcomeDownloadAsync(string path, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() => CreateRequest(HttpMethod.Get, OutcomePath(path)), ct);
        if (!response.IsSuccessStatusCode)
            return (null, new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct)));
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var type = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var name = response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "file";
        return (new ApiFile(bytes, type, name), null);
    }

    public async Task<ApiRaw> OutcomeImportAsync(UploadPart file, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(HttpMethod.Post, OutcomePath("import"));
            var part = new ByteArrayContent(file.Content);
            part.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            request.Content = new MultipartFormDataContent { { part, "file", file.FileName } };
            return request;
        }, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }
}
