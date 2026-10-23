using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.DTOs.Notifications;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Notifications;

namespace Tabsan.EduSphere.Application.Notifications;

/// <summary>
/// Orchestrates notification composition, fan-out dispatch, and read-state management.
/// Business rules enforced here:
///   - Fan-out: one NotificationRecipient row is created per recipient on dispatch.
///   - Only active notifications appear in user inboxes.
///   - MarkRead is idempotent — calling it twice is safe.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;
    private readonly INotificationFanoutQueue? _fanoutQueue;
    private readonly IMemoryCache _cache;
    private readonly IEmailDeliveryProvider? _emailDeliveryProvider;
    private readonly ISmsDeliveryProvider? _smsDeliveryProvider;
    private readonly ILogger<NotificationService> _logger;
    private readonly NotificationEmailOptions _emailOptions;
    private readonly NotificationSmsOptions _smsOptions;
    private readonly IPushNotificationDispatcher? _pushDispatcher;

    private const int DeferredFanoutThreshold = 250;
    private static readonly TimeSpan InboxCacheTtl = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan BadgeCacheTtl = TimeSpan.FromSeconds(8);
    private static int _cacheVersion;

    // Plan M11 — notification emails use each recipient's language.
    private readonly Tabsan.EduSphere.Application.Localization.IPortalLanguageService? _languages;

    public NotificationService(
        INotificationRepository repo,
        INotificationFanoutQueue? fanoutQueue = null,
        IMemoryCache? cache = null,
        IEmailDeliveryProvider? emailDeliveryProvider = null,
        ISmsDeliveryProvider? smsDeliveryProvider = null,
        ILogger<NotificationService>? logger = null,
        IOptions<NotificationEmailOptions>? emailOptions = null,
        IOptions<NotificationSmsOptions>? smsOptions = null,
        IPushNotificationDispatcher? pushDispatcher = null,
        Tabsan.EduSphere.Application.Localization.IPortalLanguageService? languages = null)
    {
        _languages = languages;
        _pushDispatcher = pushDispatcher;
        _repo = repo;
        _fanoutQueue = fanoutQueue;
        _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
        _emailDeliveryProvider = emailDeliveryProvider;
        _smsDeliveryProvider = smsDeliveryProvider;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationService>.Instance;
        _emailOptions = emailOptions?.Value ?? new NotificationEmailOptions();
        _smsOptions = smsOptions?.Value ?? new NotificationSmsOptions();
    }

    /// <summary>
    /// Creates a notification and fans it out to the provided list of user IDs.
    /// Returns the new notification ID.
    /// </summary>
    public async Task<Guid> SendAsync(SendNotificationRequest request, Guid senderUserId, CancellationToken ct = default)
    {
        var notification = new Notification(request.Title, request.Body, request.Type, senderUserId);
        await _repo.AddAsync(notification, ct);
        await _repo.SaveChangesAsync(ct);

        await FanOutRecipientsAsync(notification.Id, request.RecipientUserIds, ct);
        await DispatchEmailsAsync(notification.Title, notification.Body, notification.Type, request.RecipientUserIds, ct);
        await DispatchSmsAsync(notification.Title, notification.Body, notification.Type, request.RecipientUserIds, ct);
        await DispatchPushAsync(notification.Title, notification.Body, notification.Type, request.RecipientUserIds, ct);
        BumpCacheVersion();

        return notification.Id;
    }

    /// <summary>
    /// Creates a system-generated notification and fans it out.
    /// Used by background jobs (no human sender ID).
    /// Returns the new notification ID.
    /// </summary>
    public async Task<Guid> SendSystemAsync(string title, string body, NotificationType type,
        IReadOnlyList<Guid> recipientUserIds, CancellationToken ct = default)
    {
        var notification = new Notification(title, body, type);
        await _repo.AddAsync(notification, ct);
        await _repo.SaveChangesAsync(ct);

        await FanOutRecipientsAsync(notification.Id, recipientUserIds, ct);
        await DispatchEmailsAsync(notification.Title, notification.Body, notification.Type, recipientUserIds, ct);
        await DispatchSmsAsync(notification.Title, notification.Body, notification.Type, recipientUserIds, ct);
        await DispatchPushAsync(notification.Title, notification.Body, notification.Type, recipientUserIds, ct);
        BumpCacheVersion();

        return notification.Id;
    }

    /// <summary>
    /// Deactivates a notification, hiding it from all inboxes.
    /// Returns false when not found.
    /// </summary>
    public async Task<bool> DeactivateAsync(Guid notificationId, CancellationToken ct = default)
    {
        var notification = await _repo.GetByIdAsync(notificationId, ct);
        if (notification is null) return false;

        notification.Deactivate();
        _repo.Update(notification);
        await _repo.SaveChangesAsync(ct);
        BumpCacheVersion();
        return true;
    }

    /// <summary>Returns the user's inbox, optionally filtered to unread-only, with pagination.</summary>
    public async Task<IReadOnlyList<NotificationResponse>> GetInboxAsync(
        Guid userId, bool unreadOnly = false, int page = 0, int pageSize = 20, CancellationToken ct = default)
    {
        var cacheKey = $"notif:inbox:{CurrentCacheVersion()}:{userId}:{unreadOnly}:{page}:{pageSize}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<NotificationResponse>? cached) && cached is not null)
            return cached;

        var records = await _repo.GetForUserAsync(userId, unreadOnly, page * pageSize, pageSize, asNoTracking: true, ct);
        var response = records.Select(ToResponse).ToList();
        _cache.Set(cacheKey, response, InboxCacheTtl);
        return response;
    }

    /// <summary>Returns the unread notification count for the notification bell badge.</summary>
    public async Task<NotificationBadgeResponse> GetBadgeAsync(Guid userId, CancellationToken ct = default)
    {
        var cacheKey = $"notif:badge:{CurrentCacheVersion()}:{userId}";
        if (_cache.TryGetValue(cacheKey, out NotificationBadgeResponse? cached) && cached is not null)
            return cached;

        var count = await _repo.GetUnreadCountAsync(userId, ct);
        var response = new NotificationBadgeResponse(count);
        _cache.Set(cacheKey, response, BadgeCacheTtl);
        return response;
    }

    /// <summary>
    /// Marks a specific notification as read for the user.
    /// Idempotent — returns true even if already read.
    /// Returns false when the delivery record is not found.
    /// </summary>
    public async Task<bool> MarkReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var recipient = await _repo.GetRecipientAsync(notificationId, userId, ct);
        if (recipient is null) return false;

        recipient.MarkRead();
        _repo.UpdateRecipient(recipient);
        await _repo.SaveChangesAsync(ct);
        BumpCacheVersion();
        return true;
    }

    /// <summary>
    /// Marks all unread notifications as read for the user.
    /// Fetches a large page (500) to cover typical inbox sizes in one pass.
    /// </summary>
    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        var unread = await _repo.GetForUserAsync(userId, unreadOnly: true, skip: 0, take: 500, asNoTracking: false, ct);
        foreach (var r in unread)
        {
            r.MarkRead();
            _repo.UpdateRecipient(r);
        }

        if (unread.Count > 0)
        {
            await _repo.SaveChangesAsync(ct);
            BumpCacheVersion();
        }
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    /// <summary>Maps a NotificationRecipient (with navigation) to a NotificationResponse DTO.</summary>
    private static NotificationResponse ToResponse(NotificationRecipient r) =>
        new(r.NotificationId,
            r.Notification.Title,
            r.Notification.Body,
            r.Notification.Type.ToString(),
            r.Notification.IsSystemGenerated,
            r.Notification.CreatedAt,
            r.IsRead,
            r.ReadAt);

    private async Task FanOutRecipientsAsync(Guid notificationId, IReadOnlyList<Guid> recipientUserIds, CancellationToken ct)
    {
        var distinctRecipients = recipientUserIds.Distinct().ToList();
        if (distinctRecipients.Count == 0)
            return;

        if (_fanoutQueue is not null && distinctRecipients.Count >= DeferredFanoutThreshold)
        {
            _fanoutQueue.Enqueue(new NotificationFanoutWorkItem(notificationId, distinctRecipients));
            return;
        }

        var recipients = distinctRecipients
            .Select(uid => new NotificationRecipient(notificationId, uid))
            .ToList();

        await _repo.AddRecipientsAsync(recipients, ct);
        await _repo.SaveChangesAsync(ct);
    }

    private async Task DispatchEmailsAsync(
        string title,
        string body,
        NotificationType type,
        IReadOnlyList<Guid> recipientUserIds,
        CancellationToken ct)
    {
        if (!_emailOptions.Enabled || _emailDeliveryProvider is null)
            return;

        var emails = await _repo.GetActiveUserEmailLanguagesAsync(recipientUserIds, ct);
        if (emails.Count == 0)
            return;

        var subjectPrefix = _emailOptions.SubjectPrefix?.Trim() ?? string.Empty;
        var subject = string.IsNullOrWhiteSpace(subjectPrefix)
            ? title
            : $"{subjectPrefix} {title}";

        foreach (var (email, preferred) in emails)
        {
            try
            {
                var language = _languages is null
                    ? Tabsan.EduSphere.Application.Localization.PortalLanguages.Normalize(preferred) ?? "en"
                    : await _languages.ForRecipientAsync(preferred, ct);
                await _emailDeliveryProvider.SendTemplateAsync(
                    email,
                    subject,
                    "notification-alert",
                    new Dictionary<string, string>
                    {
                        ["TITLE"] = title,
                        ["BODY"] = body,
                        ["TYPE"] = type.ToString(),
                        ["CREATED_AT_UTC"] = DateTime.UtcNow.ToString("u"),
                        ["PORTAL_URL"] = _emailOptions.PortalUrl,
                        ["LANGUAGE"] = language
                    },
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to send notification email to {RecipientEmail} for notification '{Title}'.",
                    email,
                    title);
            }
        }
    }

    private async Task DispatchSmsAsync(
        string title,
        string body,
        NotificationType type,
        IReadOnlyList<Guid> recipientUserIds,
        CancellationToken ct)
    {
        if (!_smsOptions.Enabled || _smsDeliveryProvider is null)
            return;

        var phoneNumbers = await _repo.GetActiveUserPhoneNumbersAsync(recipientUserIds, ct);
        if (phoneNumbers.Count == 0)
            return;

        foreach (var phoneNumber in phoneNumbers)
        {
            try
            {
                await _smsDeliveryProvider.SendTemplateAsync(
                    phoneNumber,
                    "notification-alert",
                    new Dictionary<string, string>
                    {
                        ["TITLE"] = title,
                        ["BODY"] = body,
                        ["TYPE"] = type.ToString(),
                        ["CREATED_AT_UTC"] = DateTime.UtcNow.ToString("u"),
                        ["PORTAL_URL"] = _smsOptions.PortalUrl ?? string.Empty
                    },
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to send notification SMS to {RecipientPhoneNumber} for notification '{Title}'.",
                    phoneNumber,
                    title);
            }
        }
    }

    /// <summary>Plan M4.3: hands the notification to Web Push delivery (queued; never fails the send).</summary>
    private async Task DispatchPushAsync(string title, string body, NotificationType type, IReadOnlyList<Guid> recipientUserIds, CancellationToken ct)
    {
        if (_pushDispatcher is null || recipientUserIds.Count == 0) return;
        try { await _pushDispatcher.DispatchAsync(type, title, body, recipientUserIds.Distinct().ToList(), ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Push dispatch failed for notification '{Title}'.", title);
        }
    }

    private static void BumpCacheVersion() => Interlocked.Increment(ref _cacheVersion);
    private static int CurrentCacheVersion() => Volatile.Read(ref _cacheVersion);
}
