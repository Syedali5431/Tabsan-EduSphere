using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Similarity;

namespace Tabsan.EduSphere.Infrastructure.Similarity;

/// <summary>
/// Plan M9.1: Copyleaks API v3 — <c>POST {identity}/v3/account/login/api</c> for an access token (cached until shortly
/// before it expires) and <c>PUT {api}/v3/scans/submit/file/{scanId}</c> with the text as a base64 .txt file and a status
/// webhook. Results arrive at the webhook.
/// </summary>
public sealed class CopyleaksClient : ICopyleaksClient
{
    private static readonly ConcurrentDictionary<string, (string Token, DateTime ExpiresAt)> Tokens = new();
    private readonly HttpClient _http;
    private readonly SimilarityOptions _options;

    public CopyleaksClient(HttpClient http, IOptions<SimilarityOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<string> LoginAsync(string email, string apiKey, CancellationToken ct = default)
    {
        var cacheKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email + "\n" + apiKey)));
        if (Tokens.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow.AddMinutes(10)) return cached.Token;

        using var response = await _http.PostAsJsonAsync($"{_options.CopyleaksIdentityUrl.TrimEnd('/')}/v3/account/login/api", new { email, key = apiKey }, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Copyleaks login returned {(int)response.StatusCode}.");
        using var doc = JsonDocument.Parse(body);
        var token = doc.RootElement.GetProperty("access_token").GetString() ?? throw new HttpRequestException("Copyleaks login returned no token.");
        var expires = doc.RootElement.TryGetProperty(".expires", out var e) && e.TryGetDateTime(out var at) ? at.ToUniversalTime() : DateTime.UtcNow.AddHours(24);
        Tokens[cacheKey] = (token, expires);
        return token;
    }

    public async Task SubmitTextAsync(string accessToken, string scanId, string text, string statusWebhookUrl, bool sandbox, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"{_options.CopyleaksApiUrl.TrimEnd('/')}/v3/scans/submit/file/{Uri.EscapeDataString(scanId)}")
        {
            Content = JsonContent.Create(new
            {
                base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(text)),
                filename = "submission.txt",
                properties = new { webhooks = new { status = statusWebhookUrl }, sandbox }
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Copyleaks refused the scan ({(int)response.StatusCode}): {(body.Length > 300 ? body[..300] : body)}");
        }
    }
}
