using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Tabsan.EduSphere.Mobile.Core;

/// <summary>Where the app keeps its session (the MAUI app uses the platform's secure storage).</summary>
public interface ISecureStore
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    void Remove(string key);
}

/// <summary>Outcome of a sign-in attempt.</summary>
public enum SignInStatus { Success, MfaRequired, InvalidMfaCode, SsoRequired, InvalidCredentials, Blocked, UpdateRequired, Failed }

public sealed record SignInResult(SignInStatus Status, string? Message = null, Session? Session = null);

/// <summary>An API call that failed with a message for the user.</summary>
public sealed class ApiException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

/// <summary>A PKCE pair (RFC 7636): the app keeps the verifier, the portal only sees the challenge.</summary>
public sealed record Pkce(string Verifier, string Challenge)
{
    public static Pkce Create()
    {
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        return new Pkce(verifier, Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))));
    }

    public static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

/// <summary>
/// Plan M12 — the app's API client. Adds the token, the app's language (Accept-Language) and version (X-App-Version),
/// refreshes the token once when it has expired (concurrent calls share one refresh), and turns error answers into
/// <see cref="ApiException"/> with the API's (translated) message.
/// </summary>
public sealed class EduSphereClient
{
    private const string SessionKey = "edusphere.session";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ISecureStore _store;
    private readonly AppSettings _settings;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private Session? _session;

    public EduSphereClient(HttpClient http, ISecureStore store, AppSettings settings)
    {
        _http = http;
        _store = store;
        _settings = settings;
        if (_http.BaseAddress is null) _http.BaseAddress = new Uri(settings.ApiBase + "/");
    }

    /// <summary>The app's language ("en", "ur", "ar"); sent with every call so answers come translated.</summary>
    public string Language { get; set; } = "en";

    public Session? Session => _session;

    public bool IsSignedIn => _session is not null;

    /// <summary>Raised when the session ends (expired refresh token, sign-out) so the app shows the sign-in page.</summary>
    public event EventHandler? SignedOut;

    /// <summary>Restores the session saved on the device (start-up).</summary>
    public async Task<bool> RestoreAsync()
    {
        var raw = await _store.GetAsync(SessionKey);
        if (string.IsNullOrEmpty(raw)) return false;
        try { _session = JsonSerializer.Deserialize<Session>(raw, Json); }
        catch (JsonException) { _session = null; }
        return _session is not null;
    }

    // ── sign-in ────────────────────────────────────────────────────────────────────────────────

    public Task<MobileConfig?> GetConfigAsync(string? tenant = null, CancellationToken ct = default)
        => SendAsync<MobileConfig>(HttpMethod.Get, "api/v1/mobile/config" + (string.IsNullOrWhiteSpace(tenant) ? "" : "?tenant=" + Uri.EscapeDataString(tenant)), null, anonymous: true, ct);

    /// <summary>Username and password (and the authenticator code when the account has one).</summary>
    public async Task<SignInResult> SignInAsync(string username, string password, string? mfaCode = null, CancellationToken ct = default)
    {
        using var request = Request(HttpMethod.Post, "api/v1/auth/login", new { username, password, mfaCode = string.IsNullOrWhiteSpace(mfaCode) ? null : mfaCode.Trim() }, anonymous: true);
        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (response.IsSuccessStatusCode) return new SignInResult(SignInStatus.Success, Session: await SaveSessionAsync(body));
        var (error, message) = ReadError(body);
        return (response.StatusCode, error) switch
        {
            (HttpStatusCode.BadRequest, "MFA_CODE_REQUIRED") => new SignInResult(SignInStatus.MfaRequired, message),
            (HttpStatusCode.Unauthorized, "INVALID_MFA_CODE") => new SignInResult(SignInStatus.InvalidMfaCode, message),
            (HttpStatusCode.Forbidden, "SSO_REQUIRED") => new SignInResult(SignInStatus.SsoRequired, message),
            (HttpStatusCode.Unauthorized, _) => new SignInResult(SignInStatus.InvalidCredentials, message),
            ((HttpStatusCode)423, _) or (HttpStatusCode.Forbidden, _) => new SignInResult(SignInStatus.Blocked, message),
            ((HttpStatusCode)426, _) => new SignInResult(SignInStatus.UpdateRequired, message),
            _ => new SignInResult(SignInStatus.Failed, message)
        };
    }

    /// <summary>
    /// The portal page that runs single sign-on for the app in the system browser. The portal returns
    /// <c>{AppRedirectUri}?code=…</c>; the app then calls <see cref="CompleteSingleSignOnAsync"/> with the same PKCE pair.
    /// </summary>
    public static string SingleSignOnUrl(string portalBaseUrl, string provider, Pkce pkce, string? tenant = null)
        => $"{portalBaseUrl.TrimEnd('/')}/Login/Sso/{Uri.EscapeDataString(provider)}?appChallenge={pkce.Challenge}"
           + (string.IsNullOrWhiteSpace(tenant) ? "" : "&tenant=" + Uri.EscapeDataString(tenant));

    /// <summary>Redeems the one-time code from the portal's redirect (with the verifier only this app knows).</summary>
    public async Task<SignInResult> CompleteSingleSignOnAsync(Uri callback, Pkce pkce, string? deviceInfo = null, CancellationToken ct = default)
    {
        var code = Query(callback, "code");
        if (string.IsNullOrEmpty(code)) return new SignInResult(SignInStatus.Failed, Query(callback, "error") ?? "Single sign-on was cancelled.");
        using var request = Request(HttpMethod.Post, "api/v1/mobile/handoff/redeem", new { code, codeVerifier = pkce.Verifier, deviceInfo }, anonymous: true);
        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        return response.IsSuccessStatusCode
            ? new SignInResult(SignInStatus.Success, Session: await SaveSessionAsync(body))
            : new SignInResult(response.StatusCode == HttpStatusCode.Forbidden ? SignInStatus.Blocked : SignInStatus.Failed, ReadError(body).Message);
    }

    /// <summary>Signs out: stops push to this phone, ends the session on the server and forgets it on the device.</summary>
    public async Task SignOutAsync(string? pushToken = null, CancellationToken ct = default)
    {
        if (_session is { } s)
        {
            try
            {
                if (!string.IsNullOrEmpty(pushToken))
                    await SendAsync<object>(HttpMethod.Post, "api/v1/mobile/devices/unregister", new { platform = _settings.Platform, token = pushToken }, false, ct);
                await SendAsync<object>(HttpMethod.Post, "api/v1/auth/logout", new { refreshToken = s.RefreshToken }, false, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or ApiException or TaskCanceledException) { /* offline: the session expires on its own */ }
        }
        await ForgetSessionAsync();
    }

    private async Task<Session?> SaveSessionAsync(string body)
    {
        var login = JsonSerializer.Deserialize<LoginApiResponse>(body, Json);
        if (login is null || string.IsNullOrEmpty(login.AccessToken)) return null;
        _session = new Session(login.AccessToken, login.RefreshToken, login.AccessTokenExpiry, login.Role, login.UserId, login.Username);
        await _store.SetAsync(SessionKey, JsonSerializer.Serialize(_session, Json));
        return _session;
    }

    private Task ForgetSessionAsync()
    {
        _session = null;
        _store.Remove(SessionKey);
        SignedOut?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    private async Task<bool> RefreshAsync(string staleAccessToken, CancellationToken ct)
    {
        await _refreshLock.WaitAsync(ct);
        try
        {
            if (_session is null) return false;
            if (_session.AccessToken != staleAccessToken) return true; // another call already refreshed
            using var request = Request(HttpMethod.Post, "api/v1/auth/refresh", new { refreshToken = _session.RefreshToken }, anonymous: true);
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                await ForgetSessionAsync();
                return false;
            }
            var body = await response.Content.ReadAsStringAsync(ct);
            var login = JsonSerializer.Deserialize<LoginApiResponse>(body, Json);
            if (login is null || string.IsNullOrEmpty(login.AccessToken)) return false;
            _session = _session with
            {
                AccessToken = login.AccessToken,
                RefreshToken = string.IsNullOrEmpty(login.RefreshToken) ? _session.RefreshToken : login.RefreshToken,
                AccessTokenExpiry = login.AccessTokenExpiry
            };
            await _store.SetAsync(SessionKey, JsonSerializer.Serialize(_session, Json));
            return true;
        }
        finally { _refreshLock.Release(); }
    }

    // ── data ───────────────────────────────────────────────────────────────────────────────────

    public Task<HomeData?> GetHomeAsync(CancellationToken ct = default) => GetAsync<HomeData>("api/v1/mobile/home", ct);
    public async Task<List<ClassSlot>> GetTimetableAsync(CancellationToken ct = default) => await GetAsync<List<ClassSlot>>("api/v1/mobile/timetable", ct) ?? [];
    public async Task<List<CourseSummary>> GetCoursesAsync(CancellationToken ct = default) => await GetAsync<List<CourseSummary>>("api/v1/mobile/courses", ct) ?? [];
    public Task<CourseDetail?> GetCourseAsync(Guid offeringId, CancellationToken ct = default) => GetAsync<CourseDetail>($"api/v1/mobile/courses/{offeringId}", ct);
    public async Task<List<ChildSummary>> GetChildrenAsync(CancellationToken ct = default) => await GetAsync<List<ChildSummary>>("api/v1/mobile/children", ct) ?? [];
    /// <summary>Published results (own, or a linked child's for parents).</summary>
    public async Task<List<ResultItem>> GetResultsAsync(Guid? childProfileId = null, CancellationToken ct = default)
        => await GetAsync<List<ResultItem>>("api/v1/mobile/results" + Child(childProfileId), ct) ?? [];

    /// <summary>Attendance per course (own, or a linked child's for parents).</summary>
    public async Task<List<AttendanceCourse>> GetAttendanceAsync(Guid? childProfileId = null, CancellationToken ct = default)
        => await GetAsync<List<AttendanceCourse>>("api/v1/mobile/attendance" + Child(childProfileId), ct) ?? [];

    /// <summary>Report cards, newest first (own, or a linked child's for parents).</summary>
    public async Task<List<ReportCard>> GetReportCardsAsync(Guid? childProfileId = null, CancellationToken ct = default)
        => await GetAsync<List<ReportCard>>("api/v1/mobile/report-cards" + Child(childProfileId), ct) ?? [];

    private static string Child(Guid? id) => id is { } v ? $"?studentProfileId={v}" : "";

    public async Task<List<AnnouncementItem>> GetChildAnnouncementsAsync(Guid studentProfileId, CancellationToken ct = default)
        => await GetAsync<List<AnnouncementItem>>($"api/v1/parent-portal/me/students/{studentProfileId}/announcements", ct) ?? [];
    public async Task<List<NotificationItem>> GetNotificationsAsync(int page = 0, int pageSize = 20, CancellationToken ct = default)
        => await GetAsync<List<NotificationItem>>($"api/v1/notification/inbox?page={page}&pageSize={pageSize}", ct) ?? [];
    public Task MarkNotificationReadAsync(Guid id, CancellationToken ct = default) => SendAsync<object>(HttpMethod.Post, $"api/v1/notification/{id}/read", null, false, ct);
    public Task MarkAllNotificationsReadAsync(CancellationToken ct = default) => SendAsync<object>(HttpMethod.Post, "api/v1/notification/read-all", null, false, ct);

    /// <summary>Hands in an assignment: text and/or files (photos from the camera, documents).</summary>
    public async Task SubmitAssignmentAsync(Guid assignmentId, string? text, IReadOnlyList<UploadFile> files, CancellationToken ct = default)
    {
        async Task<HttpRequestMessage> Build()
        {
            var form = new MultipartFormDataContent();
            if (!string.IsNullOrWhiteSpace(text)) form.Add(new StringContent(text), "textContent");
            foreach (var file in files)
            {
                var content = new StreamContent(await file.Open());
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
                form.Add(content, "files", file.FileName);
            }
            var request = Request(HttpMethod.Post, $"api/v1/assignment/{assignmentId}/submissions", null, anonymous: false);
            request.Content = form;
            return request;
        }
        using var _ = await SendRawAsync(Build, ct);
    }

    // Teachers
    public async Task<List<RosterStudent>> GetRosterAsync(Guid offeringId, CancellationToken ct = default)
        => await GetAsync<List<RosterStudent>>($"api/v1/enrollment/roster/{offeringId}", ct) ?? [];

    /// <summary>Marks a whole class for one day (1 present, 2 absent, 3 late, 4 excused).</summary>
    public Task TakeAttendanceAsync(Guid offeringId, DateTime date, IEnumerable<(Guid StudentProfileId, int Status)> marks, CancellationToken ct = default)
        => SendAsync<object>(HttpMethod.Post, "api/v1/attendance/bulk",
            new { courseOfferingId = offeringId, date = date.Date, entries = marks.Select(m => new { studentProfileId = m.StudentProfileId, status = m.Status }) }, false, ct);

    public async Task<List<SubmissionItem>> GetSubmissionsAsync(Guid assignmentId, CancellationToken ct = default)
        => await GetAsync<List<SubmissionItem>>($"api/v1/assignment/{assignmentId}/submissions", ct) ?? [];

    public Task GradeAsync(Guid submissionId, decimal? marks, string? feedback, CancellationToken ct = default)
        => SendAsync<object>(HttpMethod.Put, $"api/v1/assignment/submissions/{submissionId}/grade", new { marksAwarded = marks, feedback }, false, ct);

    public Task PostAnnouncementAsync(Guid offeringId, string title, string body, CancellationToken ct = default)
        => SendAsync<object>(HttpMethod.Post, "api/v1/announcement", new { offeringId, authorId = _session?.UserId ?? Guid.Empty, title, body }, false, ct);

    // Course discussions — how teachers and students message each other (the portal's Discussion pages)
    public async Task<List<DiscussionThread>> GetDiscussionsAsync(Guid offeringId, CancellationToken ct = default)
        => await GetAsync<List<DiscussionThread>>($"api/v1/discussion/{offeringId}/threads", ct) ?? [];

    public Task<DiscussionThread?> GetDiscussionAsync(Guid threadId, CancellationToken ct = default)
        => GetAsync<DiscussionThread>($"api/v1/discussion/thread/{threadId}", ct);

    /// <summary>Starts a discussion; the first message is its first reply (as in the portal).</summary>
    public async Task<DiscussionThread?> StartDiscussionAsync(Guid offeringId, string title, string message, CancellationToken ct = default)
    {
        var thread = await SendAsync<DiscussionThread>(HttpMethod.Post, "api/v1/discussion/thread", new { offeringId, authorId = Guid.Empty, title }, false, ct);
        if (thread is not null && !string.IsNullOrWhiteSpace(message)) await ReplyAsync(thread.Id, message, ct);
        return thread;
    }

    public Task ReplyAsync(Guid threadId, string body, CancellationToken ct = default)
        => SendAsync<object>(HttpMethod.Post, "api/v1/discussion/reply", new { threadId, authorId = Guid.Empty, body }, false, ct);

    // Device and crash reports
    public Task RegisterDeviceAsync(string token, string? deviceModel, CancellationToken ct = default)
        => SendAsync<object>(HttpMethod.Post, "api/v1/mobile/devices",
            new { platform = _settings.Platform, token, appVersion = _settings.AppVersion, deviceModel, language = Language }, false, ct);

    public Task ReportCrashAsync(CrashReport report, CancellationToken ct = default)
        => SendAsync<object>(HttpMethod.Post, "api/v1/mobile/crash-reports", report, false, ct);

    // ── plumbing ───────────────────────────────────────────────────────────────────────────────

    private Task<T?> GetAsync<T>(string path, CancellationToken ct) => SendAsync<T>(HttpMethod.Get, path, null, false, ct);

    private async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body, bool anonymous, CancellationToken ct)
    {
        using var response = await SendRawAsync(() => Task.FromResult(Request(method, path, body, anonymous)), ct, anonymous);
        if (response.StatusCode == HttpStatusCode.NoContent || typeof(T) == typeof(object)) return default;
        var text = await response.Content.ReadAsStringAsync(ct);
        return string.IsNullOrWhiteSpace(text) ? default : JsonSerializer.Deserialize<T>(text, Json);
    }

    private async Task<HttpResponseMessage> SendRawAsync(Func<Task<HttpRequestMessage>> build, CancellationToken ct, bool anonymous = false)
    {
        if (!anonymous && _session is null) throw new ApiException(HttpStatusCode.Unauthorized, "Sign in first.");
        var token = _session?.AccessToken;
        using (var first = await build())
        {
            var response = await _http.SendAsync(first, ct);
            if (response.StatusCode != HttpStatusCode.Unauthorized || anonymous || token is null) return await Check(response, ct);
            response.Dispose();
        }
        if (!await RefreshAsync(token, ct)) throw new ApiException(HttpStatusCode.Unauthorized, "Your session has ended. Please sign in again.");
        using var retry = await build();
        return await Check(await _http.SendAsync(retry, ct), ct);
    }

    private static async Task<HttpResponseMessage> Check(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return response;
        var message = ReadError(await response.Content.ReadAsStringAsync(ct)).Message
                      ?? (response.StatusCode == (HttpStatusCode)426 ? "Please update the app." : $"The request failed ({(int)response.StatusCode}).");
        var status = response.StatusCode;
        response.Dispose();
        throw new ApiException(status, message);
    }

    private HttpRequestMessage Request(HttpMethod method, string path, object? body, bool anonymous)
    {
        var request = new HttpRequestMessage(method, path.TrimStart('/'));
        if (!anonymous && _session is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
        request.Headers.AcceptLanguage.ParseAdd(string.IsNullOrWhiteSpace(Language) ? "en" : Language);
        request.Headers.TryAddWithoutValidation("X-App-Version", _settings.AppVersion);
        if (body is not null) request.Content = JsonContent.Create(body, options: Json);
        return request;
    }

    private static (string? Error, string? Message) ReadError(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return (null, null);
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return (null, null);
            string? Get(string name) => doc.RootElement.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            var message = Get("message") ?? Get("detail");
            if (message is null && doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                message = errors.EnumerateObject().SelectMany(e => e.Value.EnumerateArray()).Select(e => e.GetString()).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
            return (Get("error"), message ?? Get("title"));
        }
        catch (JsonException) { return (null, null); }
    }

    private static string? Query(Uri uri, string name)
    {
        foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 && pair[0] == name) return Uri.UnescapeDataString(pair[1]);
        }
        return null;
    }
}
