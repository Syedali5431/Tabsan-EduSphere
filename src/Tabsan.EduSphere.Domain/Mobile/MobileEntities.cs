using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Mobile;

// Plan M12 — the EduSphere mobile app: devices registered for native push, and crash reports.

public enum MobilePlatform
{
    Android = 0,
    Ios = 1
}

/// <summary>
/// A phone or tablet registered for native push (Firebase Cloud Messaging token; FCM also delivers to iOS through APNs).
/// A token belongs to one user at a time: signing in as someone else moves it.
/// </summary>
public class MobileDevice : BaseEntity
{
    public const int MaxTokenLength = 512;

    public Guid UserId { get; private set; }
    public MobilePlatform Platform { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public string? AppVersion { get; private set; }
    public string? DeviceModel { get; private set; }

    /// <summary>The app's language ("en", "ur", "ar") — notifications are written in it.</summary>
    public string? Language { get; private set; }

    public bool IsActive { get; private set; } = true;
    public DateTime LastSeenAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastSuccessAt { get; private set; }
    public int ConsecutiveFailures { get; private set; }

    private MobileDevice() { }

    public MobileDevice(Guid userId, MobilePlatform platform, string token, string? appVersion, string? deviceModel, string? language)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A device belongs to a user.", nameof(userId));
        if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength) throw new ArgumentException("The push token is not valid.", nameof(token));
        UserId = userId;
        Platform = platform;
        Token = token.Trim();
        Refresh(userId, platform, appVersion, deviceModel, language);
    }

    /// <summary>The app registered again (start-up, sign-in, new version or language): owner and details follow.</summary>
    public void Refresh(Guid userId, MobilePlatform platform, string? appVersion, string? deviceModel, string? language)
    {
        UserId = userId;
        Platform = platform;
        AppVersion = Trim(appVersion, 32);
        DeviceModel = Trim(deviceModel, 100);
        Language = Trim(language, 8)?.ToLowerInvariant();
        IsActive = true;
        ConsecutiveFailures = 0;
        LastSeenAt = DateTime.UtcNow;
        Touch();
    }

    public void RecordSuccess(DateTime at)
    {
        LastSuccessAt = at;
        ConsecutiveFailures = 0;
        Touch();
    }

    /// <summary>Counts a failed delivery; after <paramref name="maxFailures"/> in a row the device stops receiving.</summary>
    public void RecordFailure(int maxFailures)
    {
        ConsecutiveFailures++;
        if (ConsecutiveFailures >= Math.Max(1, maxFailures)) IsActive = false;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim();
        return v.Length > max ? v[..max] : v;
    }
}

/// <summary>An unhandled error reported by the app (self-hosted crash reporting: nothing leaves the institution).</summary>
public class MobileCrashReport : BaseEntity
{
    public const int MaxStackTraceLength = 16_000;

    public Guid? UserId { get; private set; }
    public MobilePlatform Platform { get; private set; }
    public string AppVersion { get; private set; } = string.Empty;
    public string? OsVersion { get; private set; }
    public string? DeviceModel { get; private set; }
    public string ExceptionType { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string StackTrace { get; private set; } = string.Empty;

    /// <summary>The screen or action the app was on.</summary>
    public string? Screen { get; private set; }

    public DateTime OccurredAt { get; private set; }

    private MobileCrashReport() { }

    public MobileCrashReport(Guid? userId, MobilePlatform platform, string appVersion, string? osVersion, string? deviceModel,
        string exceptionType, string message, string stackTrace, string? screen, DateTime occurredAt)
    {
        UserId = userId;
        Platform = platform;
        AppVersion = Cut(appVersion, 32) ?? "unknown";
        OsVersion = Cut(osVersion, 64);
        DeviceModel = Cut(deviceModel, 100);
        ExceptionType = Cut(exceptionType, 200) ?? "Exception";
        Message = Cut(message, 1000) ?? string.Empty;
        StackTrace = Cut(stackTrace, MaxStackTraceLength) ?? string.Empty;
        Screen = Cut(screen, 200);
        OccurredAt = occurredAt > DateTime.UtcNow.AddMinutes(5) || occurredAt < DateTime.UtcNow.AddDays(-30) ? DateTime.UtcNow : occurredAt;
    }

    private static string? Cut(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim();
        return v.Length > max ? v[..max] : v;
    }
}
