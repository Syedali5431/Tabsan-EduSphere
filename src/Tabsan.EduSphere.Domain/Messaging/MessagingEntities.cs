using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Messaging;

/// <summary>
/// A private conversation: staff with one student or one parent (each pupil or family gets their own conversation, so
/// students never see each other's messages), or staff among themselves. Messages are kept: people archive a conversation
/// from their inbox, they do not delete what was said.
/// </summary>
public class Conversation : BaseEntity
{
    public const int MaxSubjectLength = 200;

    public string Subject { get; private set; } = default!;
    public Guid CreatedByUserId { get; private set; }
    /// <summary>The course it is about, when started from one (optional).</summary>
    public Guid? CourseOfferingId { get; private set; }
    public DateTime LastMessageAt { get; private set; }

    public ICollection<ConversationParticipant> Participants { get; private set; } = new List<ConversationParticipant>();

    private Conversation() { }

    public Conversation(string subject, Guid createdByUserId, Guid? courseOfferingId, DateTime now)
    {
        var text = (subject ?? "").Trim();
        if (text.Length == 0) throw new ArgumentException("Enter a subject.", nameof(subject));
        Subject = text.Length > MaxSubjectLength ? text[..MaxSubjectLength] : text;
        CreatedByUserId = createdByUserId;
        CourseOfferingId = courseOfferingId;
        LastMessageAt = now;
    }

    public void MessageAdded(DateTime at)
    {
        if (at > LastMessageAt) LastMessageAt = at;
        Touch();
    }
}

/// <summary>A person in a conversation, with what they have read and whether they archived it.</summary>
public class ConversationParticipant : BaseEntity
{
    public Guid ConversationId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime? LastReadAt { get; private set; }
    /// <summary>When they were last told about new messages (they are told again only after reading).</summary>
    public DateTime? LastNotifiedAt { get; private set; }
    public DateTime? ArchivedAt { get; private set; }

    private ConversationParticipant() { }

    public ConversationParticipant(Guid conversationId, Guid userId)
    {
        ConversationId = conversationId;
        UserId = userId;
    }

    public void Read(DateTime at)
    {
        if (LastReadAt is null || at > LastReadAt) LastReadAt = at;
    }

    /// <summary>
    /// Whether to notify them about a new message: the first message they have not read is announced; more messages are
    /// not announced again until they have read the conversation.
    /// </summary>
    public bool ShouldNotify(DateTime now)
    {
        if (LastNotifiedAt is { } notified && (LastReadAt is null || LastReadAt < notified)) return false;
        LastNotifiedAt = now;
        return true;
    }

    public void Archive(DateTime at) => ArchivedAt ??= at;

    public void Unarchive() => ArchivedAt = null;
}

/// <summary>One message. Its time is <see cref="BaseEntity.CreatedAt"/>.</summary>
public class ConversationMessage : BaseEntity
{
    public const int MaxBodyLength = 4000;

    public Guid ConversationId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string Body { get; private set; } = default!;

    private ConversationMessage() { }

    public ConversationMessage(Guid conversationId, Guid senderUserId, string body, DateTime at)
    {
        var text = (body ?? "").Trim();
        if (text.Length == 0) throw new ArgumentException("Write a message.", nameof(body));
        if (text.Length > MaxBodyLength) throw new ArgumentException($"A message has at most {MaxBodyLength} characters.", nameof(body));
        ConversationId = conversationId;
        SenderUserId = senderUserId;
        Body = text;
        CreatedAt = at;
    }
}
