using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tabsan.EduSphere.Application.DTOs.Assignments;

namespace Tabsan.EduSphere.Web.Services;

// Plan M3 — submission history, grading workspace, annotations, comments.

/// <summary>A raw API answer handed straight to the page's script (status + JSON body).</summary>
public sealed record ApiRaw(int Status, string Body)
{
    public bool Ok => Status is >= 200 and < 300;

    public string? Message
    {
        get
        {
            try
            {
                using var doc = JsonDocument.Parse(Body);
                return doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String
                    ? m.GetString() : null;
            }
            catch (JsonException) { return string.IsNullOrWhiteSpace(Body) ? null : Body; }
        }
    }
}

/// <summary>A file streamed through the portal.</summary>
public sealed record ApiFile(byte[] Content, string ContentType, string FileName);

/// <summary>A file the portal received from the browser and passes on to the API.</summary>
public sealed record UploadPart(string FileName, string? ContentType, byte[] Content);

public partial interface IEduApiClient
{
    Task<AssignmentSettingsResponse?> GetAssignmentSettingsAsync(Guid assignmentId, CancellationToken ct);
    Task<ApiRaw> SaveAssignmentSettingsAsync(Guid assignmentId, AssignmentSettingsRequest request, CancellationToken ct);
    Task<StudentAssignmentView?> GetStudentAssignmentViewAsync(Guid assignmentId, CancellationToken ct);
    Task<ApiRaw> SubmitAssignmentWithFilesAsync(Guid assignmentId, string? textContent, IReadOnlyList<UploadPart> files, CancellationToken ct);
    Task<SubmissionDetailResponse?> GetSubmissionDetailAsync(Guid submissionId, CancellationToken ct);
    Task<GradingWorkspaceResponse?> GetGradingWorkspaceAsync(Guid assignmentId, string? filter, CancellationToken ct);
    Task<ApiRaw> AddSubmissionCommentAsync(Guid submissionId, string? body, int? attemptNumber, bool isPrivate, UploadPart? media, CancellationToken ct);

    /// <summary>JSON pass-through for the workspace script. <paramref name="path"/> is relative to api/v1/assignment/.</summary>
    Task<ApiRaw> AssignmentWorkspaceSendAsync(HttpMethod method, string path, string? jsonBody, CancellationToken ct);

    /// <summary>Downloads a file under api/v1/assignment/ (null when the API refuses).</summary>
    Task<(ApiFile? File, ApiRaw? Error)> AssignmentWorkspaceDownloadAsync(string path, CancellationToken ct);

    /// <summary>Plan M4.3: JSON pass-through under api/v1/push/.</summary>
    Task<ApiRaw> PushSendAsync(HttpMethod method, string path, string? jsonBody, CancellationToken ct);

    /// <summary>Plan M5: JSON call under api/v1/lti/ with the signed-in user's token.</summary>
    Task<ApiRaw> LtiSendAsync(HttpMethod method, string path, string? jsonBody, CancellationToken ct);
}

public partial class EduApiClient
{
    private const string WorkspaceBase = "api/v1/assignment/";

    public Task<AssignmentSettingsResponse?> GetAssignmentSettingsAsync(Guid assignmentId, CancellationToken ct)
        => TryGetAsync<AssignmentSettingsResponse>($"{WorkspaceBase}{assignmentId}/settings", ct);

    public Task<ApiRaw> SaveAssignmentSettingsAsync(Guid assignmentId, AssignmentSettingsRequest request, CancellationToken ct)
        => AssignmentWorkspaceSendAsync(HttpMethod.Put, $"{assignmentId}/settings", JsonSerializer.Serialize(request, _jsonOptions), ct);

    public Task<StudentAssignmentView?> GetStudentAssignmentViewAsync(Guid assignmentId, CancellationToken ct)
        => TryGetAsync<StudentAssignmentView>($"{WorkspaceBase}{assignmentId}/student-view", ct);

    public Task<SubmissionDetailResponse?> GetSubmissionDetailAsync(Guid submissionId, CancellationToken ct)
        => TryGetAsync<SubmissionDetailResponse>($"{WorkspaceBase}submissions/{submissionId}", ct);

    public Task<GradingWorkspaceResponse?> GetGradingWorkspaceAsync(Guid assignmentId, string? filter, CancellationToken ct)
        => TryGetAsync<GradingWorkspaceResponse>($"{WorkspaceBase}{assignmentId}/workspace" + (string.IsNullOrWhiteSpace(filter) ? string.Empty : $"?filter={Uri.EscapeDataString(filter)}"), ct);

    public async Task<ApiRaw> SubmitAssignmentWithFilesAsync(Guid assignmentId, string? textContent, IReadOnlyList<UploadPart> files, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(HttpMethod.Post, $"{WorkspaceBase}{assignmentId}/submissions");
            var form = new MultipartFormDataContent();
            if (!string.IsNullOrWhiteSpace(textContent)) form.Add(new StringContent(textContent, Encoding.UTF8), "textContent");
            foreach (var f in files)
            {
                var part = new ByteArrayContent(f.Content);
                part.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(f.ContentType) ? "application/octet-stream" : f.ContentType);
                form.Add(part, "files", f.FileName);
            }
            request.Content = form;
            return request;
        }, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<ApiRaw> AddSubmissionCommentAsync(Guid submissionId, string? body, int? attemptNumber, bool isPrivate, UploadPart? media, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(HttpMethod.Post, $"{WorkspaceBase}submissions/{submissionId}/comments");
            var form = new MultipartFormDataContent();
            if (!string.IsNullOrWhiteSpace(body)) form.Add(new StringContent(body, Encoding.UTF8), "body");
            if (attemptNumber.HasValue) form.Add(new StringContent(attemptNumber.Value.ToString()), "attemptNumber");
            form.Add(new StringContent(isPrivate ? "true" : "false"), "isPrivate");
            if (media is not null)
            {
                var part = new ByteArrayContent(media.Content);
                part.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(media.ContentType) ? "application/octet-stream" : media.ContentType);
                form.Add(part, "media", media.FileName);
            }
            request.Content = form;
            return request;
        }, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<ApiRaw> AssignmentWorkspaceSendAsync(HttpMethod method, string path, string? jsonBody, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(method, WorkspaceBase + path.TrimStart('/'));
            if (jsonBody is not null) request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            return request;
        }, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<ApiRaw> LtiSendAsync(HttpMethod method, string path, string? jsonBody, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(method, "api/v1/lti/" + path.TrimStart('/'));
            if (jsonBody is not null) request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            return request;
        }, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<ApiRaw> PushSendAsync(HttpMethod method, string path, string? jsonBody, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(method, "api/v1/push" + (string.IsNullOrEmpty(path) ? string.Empty : "/" + path.TrimStart('/')));
            if (jsonBody is not null) request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            return request;
        }, ct);
        return new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<(ApiFile? File, ApiRaw? Error)> AssignmentWorkspaceDownloadAsync(string path, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() => CreateRequest(HttpMethod.Get, WorkspaceBase + path.TrimStart('/')), ct);
        if (!response.IsSuccessStatusCode)
            return (null, new ApiRaw((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct)));
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var type = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var name = response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "file";
        return (new ApiFile(bytes, type, name), null);
    }
}
