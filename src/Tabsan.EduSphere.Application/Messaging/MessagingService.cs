using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Messaging;
using Tabsan.EduSphere.Domain.Notifications;

namespace Tabsan.EduSphere.Application.Messaging;

/// <summary>
/// Messages between teachers, students, parents and the school office. People write only to those their role connects them
/// with (see <see cref="IMessagingQueries.ContactsAsync"/>); each student or parent written to gets a conversation of their
/// own; messages are kept (archiving only tidies the inbox); the people in a conversation are told about a new message in
/// the app, by push and by e-mail where switched on — once until they have read it.
/// </summary>
public sealed class MessagingService
{
    /// <summary>People one message may go to (a teacher writing to a class and its parents).</summary>
    public const int MaxRecipients = 300;
    /// <summary>Messages shown when a conversation is opened (the latest ones).</summary>
    public const int MaxMessagesShown = 200;
    public const int ContactLimit = 500;

    private static readonly HashSet<string> Staff = new(StringComparer.OrdinalIgnoreCase) { "SuperAdmin", "Admin", "Faculty", "Finance" };

    private readonly IMessagingQueries _queries;
    private readonly INotificationService? _notifications;
    private readonly ILogger<MessagingService> _logger;

    public MessagingService(IMessagingQueries queries, INotificationService? notifications = null, ILogger<MessagingService>? logger = null)
    {
        _queries = queries;
        _notifications = notifications;
        _logger = logger ?? NullLogger<MessagingService>.Instance;
    }

    /// <summary>Teachers, admins, SuperAdmins and finance officers; students, parents and anyone else are not.</summary>
    public static bool IsStaff(string? role) => role is not null && Staff.Contains(role);

    public Task<IReadOnlyList<MessageContactDto>> ContactsAsync(Guid userId, string role, string? search, CancellationToken ct = default)
        => _queries.ContactsAsync(userId, role, string.IsNullOrWhiteSpace(search) ? null : search.Trim(), ContactLimit, ct);

    public Task<IReadOnlyList<ConversationSummaryDto>> InboxAsync(Guid userId, bool archived, int page, CancellationToken ct = default)
        => _queries.InboxAsync(userId, archived, Math.Max(0, page) * 50, 50, ct);

    public async Task<UnreadMessagesDto> UnreadAsync(Guid userId, CancellationToken ct = default)
        => new(await _queries.UnreadConversationsAsync(userId, ct));

    // ═════════════════════════════════════════════════════════════════════════
    // Writing
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<MessagingResult<StartConversationResult>> StartAsync(Guid userId, string role, StartConversationRequest request, DateTime now,
        CancellationToken ct = default)
    {
        var subject = (request.Subject ?? "").Trim();
        var body = (request.Body ?? "").Trim();
        var recipients = (request.RecipientIds ?? []).Where(id => id != Guid.Empty && id != userId).Distinct().ToList();
        if (recipients.Count == 0) return MessagingResult<StartConversationResult>.Invalid("Choose who the message is for.");
        if (recipients.Count > MaxRecipients) return MessagingResult<StartConversationResult>.Invalid($"Write to at most {MaxRecipients} people at a time.");
        if (subject.Length == 0) return MessagingResult<StartConversationResult>.Invalid("Enter a subject.");
        if (subject.Length > Conversation.MaxSubjectLength) return MessagingResult<StartConversationResult>.Invalid($"A subject has at most {Conversation.MaxSubjectLength} characters.");
        if (body.Length == 0) return MessagingResult<StartConversationResult>.Invalid("Write a message.");
        if (body.Length > ConversationMessage.MaxBodyLength) return MessagingResult<StartConversationResult>.Invalid($"A message has at most {ConversationMessage.MaxBodyLength} characters.");

        var allowed = (await _queries.ContactsAsync(userId, role, null, int.MaxValue, ct)).Select(c => c.UserId).ToHashSet();
        var refused = recipients.Count(r => !allowed.Contains(r));
        if (refused > 0)
            return MessagingResult<StartConversationResult>.Forbidden(refused == 1 ? "You cannot write to one of these people." : $"You cannot write to {refused} of these people.");

        var people = await _queries.PeopleAsync([.. recipients, userId], ct);
        var staff = recipients.Where(r => IsStaff(people.GetValueOrDefault(r)?.Role)).ToList();
        var families = recipients.Except(staff).ToList();
        // One conversation per student or parent, each with the staff recipients; staff alone share one.
        List<List<Guid>> groups = families.Count == 0 ? [staff] : families.Select(f => (List<Guid>)[.. staff, f]).ToList();

        var ids = new List<Guid>();
        var told = new List<Guid>();
        foreach (var group in groups)
        {
            var conversation = new Conversation(subject, userId, request.CourseOfferingId, now);
            var me = new ConversationParticipant(conversation.Id, userId);
            me.Read(now);
            conversation.Participants.Add(me);
            foreach (var other in group)
            {
                var participant = new ConversationParticipant(conversation.Id, other);
                participant.ShouldNotify(now);
                conversation.Participants.Add(participant);
                told.Add(other);
            }
            _queries.Add(conversation);
            _queries.Add(new ConversationMessage(conversation.Id, userId, body, now));
            ids.Add(conversation.Id);
        }
        await _queries.SaveChangesAsync(ct);
        await NotifyAsync(people.GetValueOrDefault(userId)?.Name, subject, body, told.Distinct().ToList(), ct);
        return MessagingResult<StartConversationResult>.Success(new StartConversationResult(ids));
    }

    public async Task<MessagingResult<ConversationMessageDto>> ReplyAsync(Guid userId, Guid conversationId, string? text, DateTime now, CancellationToken ct = default)
    {
        var body = (text ?? "").Trim();
        if (body.Length == 0) return MessagingResult<ConversationMessageDto>.Invalid("Write a message.");
        if (body.Length > ConversationMessage.MaxBodyLength) return MessagingResult<ConversationMessageDto>.Invalid($"A message has at most {ConversationMessage.MaxBodyLength} characters.");
        var conversation = await _queries.ConversationAsync(conversationId, ct);
        var me = conversation?.Participants.FirstOrDefault(p => p.UserId == userId);
        if (conversation is null || me is null) return MessagingResult<ConversationMessageDto>.NotFound();

        var message = new ConversationMessage(conversation.Id, userId, body, now);
        _queries.Add(message);
        conversation.MessageAdded(now);
        me.Read(now);
        me.Unarchive();
        var tell = new List<Guid>();
        foreach (var other in conversation.Participants.Where(p => p.UserId != userId))
        {
            other.Unarchive(); // a new message brings the conversation back to the inbox
            if (other.ShouldNotify(now)) tell.Add(other.UserId);
        }
        await _queries.SaveChangesAsync(ct);
        var sender = (await _queries.PeopleAsync([userId], ct)).GetValueOrDefault(userId)?.Name ?? "";
        await NotifyAsync(sender, conversation.Subject, body, tell, ct);
        return MessagingResult<ConversationMessageDto>.Success(new ConversationMessageDto(message.Id, userId, sender, message.Body, now, true));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Reading
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>The conversation with its latest messages; opening it marks it read. Not found unless the user is in it.</summary>
    public async Task<MessagingResult<ConversationDto>> OpenAsync(Guid userId, Guid conversationId, DateTime now, CancellationToken ct = default)
    {
        var conversation = await _queries.ConversationAsync(conversationId, ct);
        var me = conversation?.Participants.FirstOrDefault(p => p.UserId == userId);
        if (conversation is null || me is null) return MessagingResult<ConversationDto>.NotFound();
        var messages = await _queries.MessagesAsync(conversation.Id, MaxMessagesShown, ct);
        var people = await _queries.PeopleAsync(conversation.Participants.Select(p => p.UserId).Union(messages.Select(m => m.SenderUserId)).ToList(), ct);
        me.Read(now);
        await _queries.SaveChangesAsync(ct);
        return MessagingResult<ConversationDto>.Success(new ConversationDto(conversation.Id, conversation.Subject, conversation.CourseOfferingId,
            conversation.Participants.Select(p => people.GetValueOrDefault(p.UserId) ?? new ConversationPersonDto(p.UserId, "", ""))
                .OrderBy(p => p.UserId == userId ? 1 : 0).ThenBy(p => p.Name, StringComparer.CurrentCulture).ToList(),
            messages.Select(m => new ConversationMessageDto(m.Id, m.SenderUserId, people.GetValueOrDefault(m.SenderUserId)?.Name ?? "", m.Body, m.CreatedAt, m.SenderUserId == userId)).ToList(),
            me.ArchivedAt is not null));
    }

    /// <summary>Moves the conversation out of (or back into) the user's inbox; the others keep theirs as it is.</summary>
    public async Task<MessagingResult<bool>> ArchiveAsync(Guid userId, Guid conversationId, bool archive, DateTime now, CancellationToken ct = default)
    {
        var conversation = await _queries.ConversationAsync(conversationId, ct);
        var me = conversation?.Participants.FirstOrDefault(p => p.UserId == userId);
        if (me is null) return MessagingResult<bool>.NotFound();
        if (archive) me.Archive(now); else me.Unarchive();
        await _queries.SaveChangesAsync(ct);
        return MessagingResult<bool>.Success(true);
    }

    /// <summary>"Message from Ms Khan" — "Science fair: Please bring …" (a short preview).</summary>
    public static (string Title, string Body) NotificationText(string? senderName, string subject, string body)
    {
        var preview = body.ReplaceLineEndings(" ").Trim();
        if (preview.Length > 160) preview = preview[..159].TrimEnd() + "…";
        return ($"Message from {(string.IsNullOrWhiteSpace(senderName) ? "EduSphere" : senderName)}", $"{subject}: {preview}");
    }

    private async Task NotifyAsync(string? senderName, string subject, string body, IReadOnlyList<Guid> userIds, CancellationToken ct)
    {
        if (_notifications is null || userIds.Count == 0) return;
        var (title, text) = NotificationText(senderName, subject, body);
        try { await _notifications.SendSystemAsync(title, text, NotificationType.Message, userIds, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException) { _logger.LogWarning(ex, "Message notification failed."); }
    }
}
