using Tabsan.EduSphere.Domain.Messaging;

namespace Tabsan.EduSphere.Application.Messaging;

/// <summary>
/// Someone the person may write to. <see cref="Role"/> is the role name (Faculty, Student, Parent, Admin, Finance,
/// SuperAdmin); <see cref="Detail"/> is data only (a course, a registration number, the child of a parent) and
/// <see cref="Group"/> groups the list (a course title, or Teachers, Office, Students, Parents, Staff), so apps word it in
/// their own language.
/// </summary>
public sealed record MessageContactDto(Guid UserId, string Name, string Role, string? Detail, string? Group);

public sealed record ConversationPersonDto(Guid UserId, string Name, string Role);

/// <summary>An inbox line: the other people, the last message, how many messages the person has not read.</summary>
public sealed record ConversationSummaryDto(Guid Id, string Subject, IReadOnlyList<ConversationPersonDto> With, DateTime LastMessageAt,
    string? LastMessage, Guid? LastSenderId, string? LastSenderName, int Unread, bool Archived, bool LastFromMe = false);

public sealed record ConversationMessageDto(Guid Id, Guid SenderUserId, string SenderName, string Body, DateTime SentAt, bool Mine);

public sealed record ConversationDto(Guid Id, string Subject, Guid? CourseOfferingId, IReadOnlyList<ConversationPersonDto> People,
    IReadOnlyList<ConversationMessageDto> Messages, bool Archived);

/// <summary>
/// A new message to one or more people. Each student or parent among them gets a conversation of their own (with the staff
/// recipients), so pupils and families never see each other's messages; staff alone share one conversation.
/// </summary>
public sealed record StartConversationRequest(List<Guid> RecipientIds, string Subject, string Body, Guid? CourseOfferingId = null);

public sealed record ReplyRequest(string Body);

public sealed record StartConversationResult(IReadOnlyList<Guid> ConversationIds);

public sealed record UnreadMessagesDto(int Conversations);

/// <summary>An answer, or the HTTP status and message to give instead (400 not valid, 403 not allowed, 404 not found).</summary>
public sealed record MessagingResult<T>(T? Value, int Status, string? Error)
{
    public bool Ok => Status == 200;
    public static MessagingResult<T> Success(T value) => new(value, 200, null);
    public static MessagingResult<T> Invalid(string error) => new(default, 400, error);
    public static MessagingResult<T> Forbidden(string error) => new(default, 403, error);
    public static MessagingResult<T> NotFound() => new(default, 404, "Conversation not found.");
}

/// <summary>Reads who may write to whom, inboxes and conversations, and keeps them (Infrastructure: the database).</summary>
public interface IMessagingQueries
{
    /// <summary>
    /// The people the user may write to: students their teachers and their department's office; parents their children's
    /// teachers and office (and finance); teachers their students, those students' parents, fellow teachers and the office;
    /// admins and finance everyone in their departments or campus; SuperAdmins anyone. Never the user themself, never
    /// deactivated accounts. <paramref name="search"/> narrows by name, username or detail.
    /// </summary>
    Task<IReadOnlyList<MessageContactDto>> ContactsAsync(Guid userId, string role, string? search, int limit, CancellationToken ct = default);

    /// <summary>Names and role names of users (missing ones are left out).</summary>
    Task<IReadOnlyDictionary<Guid, ConversationPersonDto>> PeopleAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);

    Task<IReadOnlyList<ConversationSummaryDto>> InboxAsync(Guid userId, bool archived, int skip, int take, CancellationToken ct = default);

    /// <summary>How many of the user's conversations have messages they have not read.</summary>
    Task<int> UnreadConversationsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>The conversation with its participants (tracked, for changes).</summary>
    Task<Conversation?> ConversationAsync(Guid conversationId, CancellationToken ct = default);

    /// <summary>The latest messages of a conversation, oldest first.</summary>
    Task<IReadOnlyList<ConversationMessage>> MessagesAsync(Guid conversationId, int take, CancellationToken ct = default);

    void Add(Conversation conversation);
    void Add(ConversationMessage message);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
