using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Tabsan.EduSphere.Application.LiveClasses;
using Tabsan.EduSphere.Domain.LiveClasses;

namespace Tabsan.EduSphere.Infrastructure.LiveClasses;

/// <summary>
/// Plan M10.1: BigBlueButton API. Every call is signed with <c>checksum = SHA1(callName + query + sharedSecret)</c> (the
/// algorithm every BigBlueButton version accepts). Meetings are created on demand when someone joins (<c>create</c> is
/// idempotent), because an empty BigBlueButton meeting expires after a few minutes; join links are signed per person.
/// BigBlueButton has no participant report after a meeting, so attendance comes from the portal's own join log.
/// </summary>
public sealed class BigBlueButtonProvider : IConferenceProvider
{
    private readonly HttpClient _http;

    public BigBlueButtonProvider(HttpClient http) => _http = http;

    public ConferenceProviderType Type => ConferenceProviderType.BigBlueButton;

    public static string SignedUrl(string baseUrl, string secret, string call, IEnumerable<(string Key, string Value)> parameters)
    {
        var query = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        var checksum = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(call + query + secret))).ToLowerInvariant();
        return $"{baseUrl.TrimEnd('/')}/{call}?{query}{(query.Length > 0 ? "&" : "")}checksum={checksum}";
    }

    private static (string BaseUrl, string Secret) Require(ConferenceConfig c)
        => (c.BaseUrl ?? throw new InvalidOperationException("No BigBlueButton address."), c.Secret ?? throw new InvalidOperationException("No BigBlueButton secret."));

    private async Task<XElement> CallAsync(ConferenceConfig c, string call, IEnumerable<(string, string)> parameters, CancellationToken ct)
    {
        var (baseUrl, secret) = Require(c);
        var body = await _http.GetStringAsync(SignedUrl(baseUrl, secret, call, parameters), ct);
        var root = XDocument.Parse(body).Root ?? throw new InvalidOperationException("BigBlueButton returned nothing.");
        if (!string.Equals((string?)root.Element("returncode"), "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            var key = (string?)root.Element("messageKey");
            throw new InvalidOperationException(key == "checksumError" ? "the shared secret is wrong" : (string?)root.Element("message") ?? key ?? "request failed");
        }
        return root;
    }

    public Task<ConferenceMeeting> CreateAsync(ConferenceConfig config, ConferenceMeetingRequest request, CancellationToken ct = default)
        => Task.FromResult(new ConferenceMeeting(request.SessionId.ToString("N"), null, null));

    public async Task<string?> PersonalJoinUrlAsync(ConferenceConfig config, ConferenceJoinRequest r, CancellationToken ct = default)
    {
        await CallAsync(config, "create",
        [
            ("name", r.MeetingName), ("meetingID", r.MeetingId), ("attendeePW", r.AttendeePassword), ("moderatorPW", r.ModeratorPassword),
            ("record", "true"), ("autoStartRecording", "false"), ("allowStartStopRecording", "true"), ("duration", (r.DurationMinutes + 30).ToString()),
            ("meta_bbb-origin", "Tabsan EduSphere")
        ], ct);
        var (baseUrl, secret) = Require(config);
        return SignedUrl(baseUrl, secret, "join",
        [
            ("fullName", r.FullName), ("meetingID", r.MeetingId), ("password", r.Moderator ? r.ModeratorPassword : r.AttendeePassword),
            ("role", r.Moderator ? "MODERATOR" : "VIEWER"), ("userID", r.UserId), ("redirect", "true")
        ]);
    }

    public Task<IReadOnlyList<ConferenceParticipant>?> ParticipantsAsync(ConferenceConfig config, string externalId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ConferenceParticipant>?>(null);

    public async Task<string?> RecordingUrlAsync(ConferenceConfig config, string externalId, CancellationToken ct = default)
    {
        var root = await CallAsync(config, "getRecordings", [("meetingID", externalId)], ct);
        return root.Descendants("playback").Descendants("format").Select(f => (string?)f.Element("url")).FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));
    }

    public async Task CancelAsync(ConferenceConfig config, string externalId, CancellationToken ct = default)
    {
        try { await CallAsync(config, "end", [("meetingID", externalId)], ct); }
        catch (InvalidOperationException) { /* not running — nothing to end */ }
    }

    public async Task TestAsync(ConferenceConfig config, CancellationToken ct = default) => await CallAsync(config, "getMeetings", [], ct);
}

/// <summary>Bearer tokens cached per credentials until shortly before they expire.</summary>
internal static class TokenCache
{
    private static readonly ConcurrentDictionary<string, (string Token, DateTime ExpiresAt)> Tokens = new();

    public static async Task<string> GetAsync(string key, Func<Task<(string Token, int ExpiresInSeconds)>> fetch)
    {
        var cacheKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        if (Tokens.TryGetValue(cacheKey, out var c) && c.ExpiresAt > DateTime.UtcNow.AddMinutes(2)) return c.Token;
        var (token, expires) = await fetch();
        Tokens[cacheKey] = (token, DateTime.UtcNow.AddSeconds(Math.Max(60, expires)));
        return token;
    }
}

/// <summary>
/// Plan M10.1: Zoom through a Server-to-Server OAuth app (account credentials). Scopes: meeting:write, meeting:read,
/// report:read (participants), cloud_recording:read and user:read.
/// </summary>
public sealed class ZoomProvider : IConferenceProvider
{
    public const string OAuthUrl = "https://zoom.us/oauth/token";
    public const string ApiUrl = "https://api.zoom.us/v2";
    private readonly HttpClient _http;

    public ZoomProvider(HttpClient http) => _http = http;

    public ConferenceProviderType Type => ConferenceProviderType.Zoom;

    private Task<string> TokenAsync(ConferenceConfig c, CancellationToken ct) => TokenCache.GetAsync($"zoom\n{c.AccountId}\n{c.ClientId}\n{c.Secret}", async () =>
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{OAuthUrl}?grant_type=account_credentials&account_id={Uri.EscapeDataString(c.AccountId!)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{c.ClientId}:{c.Secret}")));
        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"sign-in returned {(int)response.StatusCode}");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return (doc.RootElement.GetProperty("access_token").GetString()!, doc.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 3600);
    });

    private async Task<HttpResponseMessage> SendAsync(ConferenceConfig c, HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, ApiUrl + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(c, ct));
        if (body is not null) request.Content = JsonContent.Create(body);
        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            response.Dispose();
            throw new InvalidOperationException($"{(int)response.StatusCode} {(text.Length > 200 ? text[..200] : text)}");
        }
        return response;
    }

    private static string Host(ConferenceConfig c) => Uri.EscapeDataString(string.IsNullOrWhiteSpace(c.OrganizerUserId) ? "me" : c.OrganizerUserId);

    public async Task<ConferenceMeeting> CreateAsync(ConferenceConfig config, ConferenceMeetingRequest r, CancellationToken ct = default)
    {
        using var response = await SendAsync(config, HttpMethod.Post, $"/users/{Host(config)}/meetings", new
        {
            topic = r.Title, type = 2, start_time = r.StartsAt.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"), duration = r.DurationMinutes, timezone = "UTC",
            agenda = r.Description ?? "", settings = new { join_before_host = false, waiting_room = true, auto_recording = "cloud", mute_upon_entry = true }
        }, ct);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return new ConferenceMeeting(doc.RootElement.GetProperty("id").GetRawText().Trim('"'), doc.RootElement.GetProperty("join_url").GetString(),
            doc.RootElement.TryGetProperty("start_url", out var s) ? s.GetString() : null);
    }

    public Task<string?> PersonalJoinUrlAsync(ConferenceConfig config, ConferenceJoinRequest request, CancellationToken ct = default) => Task.FromResult<string?>(null);

    public async Task<IReadOnlyList<ConferenceParticipant>?> ParticipantsAsync(ConferenceConfig config, string externalId, CancellationToken ct = default)
    {
        using var response = await SendAsync(config, HttpMethod.Get, $"/report/meetings/{Uri.EscapeDataString(externalId)}/participants?page_size=300", null, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return [];
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("participants").EnumerateArray()
            .Select(p => (Email: p.TryGetProperty("user_email", out var e) ? e.GetString() : null, Name: p.TryGetProperty("name", out var n) ? n.GetString() : null,
                Seconds: p.TryGetProperty("duration", out var d) && d.TryGetInt32(out var s) ? s : 0))
            .GroupBy(p => (p.Email ?? p.Name ?? "").ToLowerInvariant())
            .Select(g => new ConferenceParticipant(g.First().Email, g.First().Name, g.Sum(x => x.Seconds) / 60)).ToList();
    }

    public async Task<string?> RecordingUrlAsync(ConferenceConfig config, string externalId, CancellationToken ct = default)
    {
        using var response = await SendAsync(config, HttpMethod.Get, $"/meetings/{Uri.EscapeDataString(externalId)}/recordings", null, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return doc.RootElement.TryGetProperty("share_url", out var u) ? u.GetString() : null;
    }

    public async Task CancelAsync(ConferenceConfig config, string externalId, CancellationToken ct = default)
    {
        using var _ = await SendAsync(config, HttpMethod.Delete, $"/meetings/{Uri.EscapeDataString(externalId)}", null, ct);
    }

    public async Task TestAsync(ConferenceConfig config, CancellationToken ct = default)
    {
        using var _ = await SendAsync(config, HttpMethod.Get, $"/users/{Host(config)}", null, ct);
    }
}

/// <summary>
/// Plan M10.1: Microsoft Teams online meetings through Microsoft Graph with application permissions
/// (OnlineMeetings.ReadWrite.All, OnlineMeetingArtifact.Read.All, User.Read.All and an application access policy that lets
/// the app organise meetings for the organiser user).
/// </summary>
public sealed class TeamsProvider : IConferenceProvider
{
    public const string GraphUrl = "https://graph.microsoft.com/v1.0";
    private readonly HttpClient _http;

    public TeamsProvider(HttpClient http) => _http = http;

    public ConferenceProviderType Type => ConferenceProviderType.MicrosoftTeams;

    private Task<string> TokenAsync(ConferenceConfig c, CancellationToken ct) => TokenCache.GetAsync($"teams\n{c.AccountId}\n{c.ClientId}\n{c.Secret}", async () =>
    {
        using var response = await _http.PostAsync($"https://login.microsoftonline.com/{Uri.EscapeDataString(c.AccountId!)}/oauth2/v2.0/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = c.ClientId!, ["client_secret"] = c.Secret!, ["scope"] = "https://graph.microsoft.com/.default", ["grant_type"] = "client_credentials"
            }), ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"sign-in returned {(int)response.StatusCode}");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return (doc.RootElement.GetProperty("access_token").GetString()!, doc.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 3600);
    });

    private async Task<JsonDocument?> SendAsync(ConferenceConfig c, HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path.StartsWith("http") ? path : GraphUrl + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(c, ct));
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = await _http.SendAsync(request, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"{(int)response.StatusCode} {(text.Length > 200 ? text[..200] : text)}");
        return string.IsNullOrWhiteSpace(text) ? null : JsonDocument.Parse(text);
    }

    private static string Organizer(ConferenceConfig c) => Uri.EscapeDataString(c.OrganizerUserId ?? throw new InvalidOperationException("No organiser user."));

    public async Task<ConferenceMeeting> CreateAsync(ConferenceConfig config, ConferenceMeetingRequest r, CancellationToken ct = default)
    {
        using var doc = await SendAsync(config, HttpMethod.Post, $"/users/{Organizer(config)}/onlineMeetings", new
        {
            subject = r.Title, startDateTime = r.StartsAt.ToString("o"), endDateTime = r.StartsAt.AddMinutes(r.DurationMinutes).ToString("o")
        }, ct) ?? throw new InvalidOperationException("the organiser user was not found");
        return new ConferenceMeeting(doc.RootElement.GetProperty("id").GetString()!, doc.RootElement.GetProperty("joinWebUrl").GetString(), null);
    }

    public Task<string?> PersonalJoinUrlAsync(ConferenceConfig config, ConferenceJoinRequest request, CancellationToken ct = default) => Task.FromResult<string?>(null);

    public async Task<IReadOnlyList<ConferenceParticipant>?> ParticipantsAsync(ConferenceConfig config, string externalId, CancellationToken ct = default)
    {
        var basePath = $"/users/{Organizer(config)}/onlineMeetings/{Uri.EscapeDataString(externalId)}/attendanceReports";
        using var reports = await SendAsync(config, HttpMethod.Get, basePath, null, ct);
        var reportId = reports?.RootElement.GetProperty("value").EnumerateArray().Select(v => v.GetProperty("id").GetString()).FirstOrDefault();
        if (reportId is null) return [];
        using var records = await SendAsync(config, HttpMethod.Get, $"{basePath}/{Uri.EscapeDataString(reportId)}/attendanceRecords", null, ct);
        return (records?.RootElement.GetProperty("value").EnumerateArray() ?? Enumerable.Empty<JsonElement>())
            .Select(r => new ConferenceParticipant(
                r.TryGetProperty("emailAddress", out var e) ? e.GetString() : null,
                r.TryGetProperty("identity", out var i) && i.TryGetProperty("displayName", out var n) ? n.GetString() : null,
                (r.TryGetProperty("totalAttendanceInSeconds", out var s) && s.TryGetInt32(out var sec) ? sec : 0) / 60))
            .ToList();
    }

    public Task<string?> RecordingUrlAsync(ConferenceConfig config, string externalId, CancellationToken ct = default) => Task.FromResult<string?>(null);

    public async Task CancelAsync(ConferenceConfig config, string externalId, CancellationToken ct = default)
    {
        using var _ = await SendAsync(config, HttpMethod.Delete, $"/users/{Organizer(config)}/onlineMeetings/{Uri.EscapeDataString(externalId)}", null, ct);
    }

    public async Task TestAsync(ConferenceConfig config, CancellationToken ct = default)
    {
        using var user = await SendAsync(config, HttpMethod.Get, $"/users/{Organizer(config)}", null, ct)
            ?? throw new InvalidOperationException("the organiser user was not found");
    }
}
