using System.Text.Json;

namespace Tabsan.EduSphere.Application.Similarity;

// Plan M9 — similarity checking contracts.

public sealed class SimilarityOptions
{
    public const string SectionName = "Similarity";
    /// <summary>Public https address of the API, used for provider webhooks (e.g. https://api.uni.edu). Needed for Copyleaks.</summary>
    public string? PublicApiBaseUrl { get; set; }
    public string CopyleaksIdentityUrl { get; set; } = "https://id.copyleaks.com";
    public string CopyleaksApiUrl { get; set; } = "https://api.copyleaks.com";
    /// <summary>Seconds between worker runs (new submissions and queued checks).</summary>
    public int WorkerIntervalSeconds { get; set; } = 30;
}

public sealed record SimilaritySettingsDto(Guid? TenantId, string Provider, bool IsEnabled, string Scope, string? AccountEmail, bool HasApiKey,
    bool Sandbox, bool IsComplete, bool WebhookReady, bool IsDefault);

public sealed record SaveSimilaritySettingsRequest(string Provider, bool IsEnabled, string Scope, string? AccountEmail, string? ApiKey, bool Sandbox);

public sealed record AssignmentSimilarityDto(Guid AssignmentId, bool Enabled, bool VisibleToStudents, string Provider, bool ProviderEnabled);

public sealed record SaveAssignmentSimilarityRequest(bool Enabled, bool VisibleToStudents);

public sealed record SimilarityPassageDto(string Text, string? SourceText);

/// <summary>A matching source: another submission (built-in) or a web page / publication (Copyleaks).</summary>
public sealed record SimilaritySourceDto(string Kind, string Label, string? Url, decimal Percent, int? MatchedWords, IReadOnlyList<SimilarityPassageDto> Passages);

public sealed record SimilarityReportDto(Guid CheckId, Guid SubmissionId, Guid AssignmentId, string AssignmentTitle, string StudentLabel, int AttemptNumber,
    string Provider, string Status, decimal? Score, string? Error, DateTime CreatedAt, DateTime? CompletedAt, string? ReportUrl,
    IReadOnlyList<SimilaritySourceDto> Sources);

public sealed record SimilarityScoreDto(Guid SubmissionId, Guid CheckId, string Status, decimal? Score, int AttemptNumber);

public sealed record StudentSimilarityDto(Guid SubmissionId, string Status, decimal? Score);

public sealed record SimilarityRunResult(int Queued, int Processed);

public sealed record SimilarityResult<T>(T? Value, int Status, string? Error)
{
    public bool Ok => Status is >= 200 and < 300;
    public static SimilarityResult<T> Success(T value, int status = 200) => new(value, status, null);
    public static SimilarityResult<T> Fail(int status, string error) => new(default, status, error);
}

/// <summary>Reads the text of a submitted file (null when the type is not readable).</summary>
public interface ISubmissionTextExtractor
{
    Task<string?> ExtractAsync(string fileName, string contentType, byte[] content, CancellationToken ct = default);
}

/// <summary>Plan M9.1: the Copyleaks API (login and scan submission; results arrive by webhook).</summary>
public interface ICopyleaksClient
{
    Task<string> LoginAsync(string email, string apiKey, CancellationToken ct = default);
    Task SubmitTextAsync(string accessToken, string scanId, string text, string statusWebhookUrl, bool sandbox, CancellationToken ct = default);
}

/// <summary>Plan M9.2: what a Copyleaks status webhook says.</summary>
public sealed record CopyleaksWebhookResult(bool IsCompleted, bool IsError, decimal? Score, IReadOnlyList<SimilaritySourceDto> Sources, string? Error);

public static class CopyleaksWebhook
{
    /// <summary>
    /// Parses a Copyleaks v3 status webhook. "completed" carries <c>results.score.aggregatedScore</c> and the matching
    /// <c>internet</c> / <c>database</c> / <c>batch</c> / <c>repositories</c> results; "error" carries <c>error.message</c>;
    /// other statuses (creditsChecked, indexed) are informational.
    /// </summary>
    public static CopyleaksWebhookResult Parse(string status, string json)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        var root = doc.RootElement;
        if (string.Equals(status, "error", StringComparison.OrdinalIgnoreCase))
        {
            var message = root.TryGetProperty("error", out var e) && e.TryGetProperty("message", out var m) ? m.GetString() : null;
            return new CopyleaksWebhookResult(false, true, null, [], string.IsNullOrWhiteSpace(message) ? "Copyleaks reported an error." : message);
        }
        if (!string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase)) return new CopyleaksWebhookResult(false, false, null, [], null);

        decimal score = 0;
        var totalWords = 0;
        if (root.TryGetProperty("scannedDocument", out var scanned) && scanned.TryGetProperty("totalWords", out var tw) && tw.TryGetInt32(out var t)) totalWords = t;
        var sources = new List<SimilaritySourceDto>();
        if (root.TryGetProperty("results", out var results))
        {
            if (results.TryGetProperty("score", out var s) && s.TryGetProperty("aggregatedScore", out var agg) && agg.TryGetDecimal(out var a)) score = a;
            foreach (var (kind, label) in new[] { ("internet", "Web page"), ("database", "Copyleaks database"), ("batch", "Batch document"), ("repositories", "Repository document") })
            {
                if (!results.TryGetProperty(kind, out var list) || list.ValueKind != JsonValueKind.Array) continue;
                foreach (var item in list.EnumerateArray())
                {
                    var matched = item.TryGetProperty("matchedWords", out var mw) && mw.TryGetInt32(out var mwi) ? mwi : 0;
                    var title = item.TryGetProperty("title", out var ti) ? ti.GetString() : null;
                    var url = item.TryGetProperty("url", out var u) ? u.GetString() : null;
                    var intro = item.TryGetProperty("introduction", out var intr) ? intr.GetString() : null;
                    var percent = totalWords > 0 ? Math.Round(100m * matched / totalWords, 1, MidpointRounding.AwayFromZero) : 0;
                    sources.Add(new SimilaritySourceDto(kind, string.IsNullOrWhiteSpace(title) ? label : title!,
                        url is not null && Uri.TryCreate(url, UriKind.Absolute, out var parsed) && (parsed.Scheme == Uri.UriSchemeHttps || parsed.Scheme == Uri.UriSchemeHttp) ? url : null,
                        percent, matched, string.IsNullOrWhiteSpace(intro) ? [] : [new SimilarityPassageDto(intro!, null)]));
                }
            }
        }
        return new CopyleaksWebhookResult(true, false, score, sources.OrderByDescending(x => x.Percent).Take(25).ToList(), null);
    }
}
