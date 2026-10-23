using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace Tabsan.EduSphere.API.Services;

/// <summary>Settings (section "RateLimiting").</summary>
public sealed class ApiRateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;

    /// <summary>Requests per minute for one signed-in user (portal, mobile app).</summary>
    public int PerUserPerMinute { get; set; } = 600;

    /// <summary>Requests per minute for one API token (integrations, Plan M14.3).</summary>
    public int PerTokenPerMinute { get; set; } = 1200;

    /// <summary>Requests per minute from one address without a sign-in.</summary>
    public int AnonymousPerIpPerMinute { get; set; } = 300;

    /// <summary>
    /// Callers never limited by address (single addresses or ranges such as 10.0.0.0/8), e.g. the portal server, which makes
    /// the sign-in and other anonymous calls for all its users. Signed-in requests are still limited per user.
    /// </summary>
    public string[] ExemptIps { get; set; } = ["127.0.0.1", "::1"];
}

/// <summary>
/// Plan M14.2 — the API's request limits: per API token, per signed-in user, otherwise per client address (sliding one-minute
/// windows). Over the limit the API answers 429 with Retry-After.
/// </summary>
public static class ApiRateLimiting
{
    public const string TokenIdClaim = "api_token_id";

    public static RateLimitPartition<string> Partition(HttpContext context)
    {
        var options = context.RequestServices.GetRequiredService<IOptionsMonitor<ApiRateLimitOptions>>().CurrentValue;
        if (!options.Enabled) return RateLimitPartition.GetNoLimiter("off");

        if (context.User.FindFirstValue(TokenIdClaim) is { Length: > 0 } token)
            return Window($"token:{token}", options.PerTokenPerMinute);
        if ((context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub")) is { Length: > 0 } user)
            return Window($"user:{user}", options.PerUserPerMinute);

        var address = context.Connection.RemoteIpAddress;
        if (address is null) return Window("ip:unknown", options.AnonymousPerIpPerMinute);
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (options.ExemptIps.Any(e => IsExempt(e, address))) return RateLimitPartition.GetNoLimiter("exempt");
        return Window($"ip:{address}", options.AnonymousPerIpPerMinute);
    }

    private static bool IsExempt(string entry, IPAddress address)
        => entry.Contains('/')
            ? System.Net.IPNetwork.TryParse(entry, out var network) && network.Contains(address)
            : IPAddress.TryParse(entry, out var exempt) && exempt.Equals(address);

    private static RateLimitPartition<string> Window(string key, int perMinute)
        => RateLimitPartition.GetSlidingWindowLimiter($"{key}:{perMinute}", _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = Math.Max(1, perMinute),
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueLimit = 0
        });

    /// <summary>429 with Retry-After and a short message.</summary>
    public static async ValueTask OnRejectedAsync(Microsoft.AspNetCore.RateLimiting.OnRejectedContext rejected, CancellationToken ct)
    {
        var retry = rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var after) ? after : TimeSpan.FromSeconds(10);
        rejected.HttpContext.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)).ToString();
        await rejected.HttpContext.Response.WriteAsJsonAsync(new { message = "Too many requests. Please wait a moment and try again." }, ct);
    }
}
