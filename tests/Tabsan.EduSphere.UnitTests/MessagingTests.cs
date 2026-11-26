using Tabsan.EduSphere.Application.DTOs.Notifications;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Messaging;
using Tabsan.EduSphere.Domain.Messaging;
using Tabsan.EduSphere.Domain.Notifications;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>
/// Messaging: only people the role connects may be written to; each student or parent gets a conversation of their own;
/// replies only from the people in a conversation (others see "not found"); reading, archiving and a new message bringing
/// a conversation back; one notification until the person has read; the notification's wording.
/// </summary>
public class MessagingTests
{
    private static readonly DateTime Now = new(2026, 11, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Teacher = Guid.NewGuid(), Office = Guid.NewGuid(), Ali = Guid.NewGuid(), Sara = Guid.NewGuid(), AliMum = Guid.NewGuid(),
        OtherTeacher = Guid.NewGuid(), Stranger = Guid.NewGuid();

    private sealed class Queries : IMessagingQueries
    {
        public readonly Dictionary<Guid, List<MessageContactDto>> Contacts = new();
        public readonly Dictionary<Guid, ConversationPersonDto> People = new();
        public readonly List<Conversation> Conversations = [];
        public readonly List<ConversationMessage> Messages = [];
        public int Saves;

        public Task<IReadOnlyList<MessageContactDto>> ContactsAsync(Guid userId, string role, string? search, int limit, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MessageContactDto>>(Contacts.GetValueOrDefault(userId) ?? []);

        public Task<IReadOnlyDictionary<Guid, ConversationPersonDto>> PeopleAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyDictionary<Guid, ConversationPersonDto>>(People.Where(p => userIds.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value));

        public Task<IReadOnlyList<ConversationSummaryDto>> InboxAsync(Guid userId, bool archived, int skip, int take, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> UnreadConversationsAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Conversation?> ConversationAsync(Guid conversationId, CancellationToken ct = default) => Task.FromResult(Conversations.FirstOrDefault(c => c.Id == conversationId));
        public Task<IReadOnlyList<ConversationMessage>> MessagesAsync(Guid conversationId, int take, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ConversationMessage>>(Messages.Where(m => m.ConversationId == conversationId).OrderBy(m => m.CreatedAt).TakeLast(take).ToList());
        public void Add(Conversation conversation) => Conversations.Add(conversation);
        public void Add(ConversationMessage message) => Messages.Add(message);
        public Task<int> SaveChangesAsync(CancellationToken ct = default) { Saves++; return Task.FromResult(1); }
    }

    private sealed class Notifications : INotificationService
    {
        public readonly List<(string Title, string Body, NotificationType Type, IReadOnlyList<Guid> To)> Sent = [];
        public Task<Guid> SendSystemAsync(string title, string body, NotificationType type, IReadOnlyList<Guid> recipientUserIds, CancellationToken ct = default)
        {
            Sent.Add((title, body, type, recipientUserIds));
            return Task.FromResult(Guid.NewGuid());
        }
        public Task<Guid> SendAsync(SendNotificationRequest request, Guid senderUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> DeactivateAsync(Guid notificationId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<NotificationResponse>> GetInboxAsync(Guid userId, bool unreadOnly = false, int page = 0, int pageSize = 20, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<NotificationBadgeResponse> GetBadgeAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> MarkReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task MarkAllReadAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private static (MessagingService Service, Queries Q, Notifications N) Setup()
    {
        var q = new Queries();
        void Person(Guid id, string name, string role) => q.People[id] = new ConversationPersonDto(id, name, role);
        Person(Teacher, "Ms Khan", "Faculty");
        Person(Office, "Mr Shah", "Admin");
        Person(Ali, "Ali Raza", "Student");
        Person(Sara, "Sara Malik", "Student");
        Person(AliMum, "Nadia Raza", "Parent");
        Person(OtherTeacher, "Mr Iqbal", "Faculty");
        MessageContactDto C(Guid id) => new(id, q.People[id].Name, q.People[id].Role, null, null);
        q.Contacts[Teacher] = [C(Office), C(Ali), C(Sara), C(AliMum), C(OtherTeacher)];
        q.Contacts[Ali] = [C(Teacher), C(Office)];
        q.Contacts[AliMum] = [C(Teacher), C(Office)];
        var n = new Notifications();
        return (new MessagingService(q, n), q, n);
    }

    private static StartConversationRequest Message(params Guid[] to) => new([.. to], "Science fair", "Please bring your models on Friday.");

    [Fact]
    public async Task Start_ChecksTheMessage_AndOnlyPeopleTheRoleConnects()
    {
        var (service, q, n) = Setup();
        Assert.Equal((400, "Choose who the message is for."), Shape(await service.StartAsync(Ali, "Student", Message(), Now)));
        Assert.Equal((400, "Choose who the message is for."), Shape(await service.StartAsync(Ali, "Student", Message(Ali), Now))); // not to oneself
        Assert.Equal((400, "Enter a subject."), Shape(await service.StartAsync(Ali, "Student", Message(Teacher) with { Subject = "  " }, Now)));
        Assert.Equal((400, "Write a message."), Shape(await service.StartAsync(Ali, "Student", Message(Teacher) with { Body = "" }, Now)));
        Assert.Equal(400, (await service.StartAsync(Ali, "Student", Message(Teacher) with { Body = new string('x', ConversationMessage.MaxBodyLength + 1) }, Now)).Status);

        Assert.Equal((403, "You cannot write to one of these people."), Shape(await service.StartAsync(Ali, "Student", Message(Sara), Now))); // no student-to-student
        Assert.Equal((403, "You cannot write to 2 of these people."), Shape(await service.StartAsync(Ali, "Student", Message(Teacher, Sara, Stranger), Now)));
        Assert.Empty(q.Conversations);
        Assert.Empty(n.Sent);
    }

    private static (int, string?) Shape<T>(MessagingResult<T> r) => (r.Status, r.Error);

    [Fact]
    public async Task Start_GivesEachStudentAndParentTheirOwnConversation_WithTheStaffRecipients()
    {
        var (service, q, n) = Setup();
        var result = await service.StartAsync(Teacher, "Faculty", Message(Ali, Sara, AliMum, Office), Now);

        Assert.True(result.Ok);
        Assert.Equal(3, result.Value!.ConversationIds.Count);
        foreach (var c in q.Conversations)
        {
            var people = c.Participants.Select(p => p.UserId).ToList();
            Assert.Contains(Teacher, people);
            Assert.Contains(Office, people);
            Assert.Equal(1, people.Count(p => p == Ali || p == Sara || p == AliMum)); // never two families together
            Assert.Equal(Now, c.Participants.Single(p => p.UserId == Teacher).LastReadAt); // the writer has read their own message
        }
        Assert.Equal(3, q.Messages.Count);
        var told = Assert.Single(n.Sent); // one notification for everyone
        Assert.Equal(NotificationType.Message, told.Type);
        Assert.Equal(("Message from Ms Khan", "Science fair: Please bring your models on Friday."), (told.Title, told.Body));
        Assert.Equal(new[] { Ali, Sara, AliMum, Office }.OrderBy(x => x), told.To.OrderBy(x => x));

        var staff = await service.StartAsync(Teacher, "Faculty", Message(Office, OtherTeacher), Now);
        var shared = Assert.Single(staff.Value!.ConversationIds); // staff alone share one conversation
        Assert.Equal(3, q.Conversations.Single(c => c.Id == shared).Participants.Count);
    }

    [Fact]
    public async Task Reply_IsForThePeopleInTheConversation_AndBringsItBack()
    {
        var (service, q, n) = Setup();
        var id = (await service.StartAsync(Ali, "Student", Message(Teacher), Now)).Value!.ConversationIds.Single();
        Assert.Equal(404, (await service.ReplyAsync(Sara, id, "Me too", Now.AddMinutes(1))).Status); // not hers: "not found"
        Assert.Equal(404, (await service.OpenAsync(Sara, id, Now)).Status);
        Assert.Equal(404, (await service.ReplyAsync(Teacher, Guid.NewGuid(), "Hello", Now)).Status);
        Assert.Equal(400, (await service.ReplyAsync(Teacher, id, "   ", Now)).Status);

        Assert.True((await service.ArchiveAsync(Ali, id, true, Now)).Ok);
        var conversation = q.Conversations.Single();
        Assert.NotNull(conversation.Participants.Single(p => p.UserId == Ali).ArchivedAt);

        var reply = await service.ReplyAsync(Teacher, id, "Yes, Friday at 9.", Now.AddMinutes(5));
        Assert.True(reply.Ok);
        Assert.Equal(("Ms Khan", true), (reply.Value!.SenderName, reply.Value.Mine));
        Assert.Null(conversation.Participants.Single(p => p.UserId == Ali).ArchivedAt); // back in Ali's inbox
        Assert.Equal(Now.AddMinutes(5), conversation.LastMessageAt);
        Assert.Equal("Message from Ms Khan", n.Sent.Last().Title);
        Assert.Equal(new[] { Ali }, n.Sent.Last().To);
    }

    [Fact]
    public async Task Notifications_OnceUntilRead_ThenAgain()
    {
        var (service, _, n) = Setup();
        var id = (await service.StartAsync(Ali, "Student", Message(Teacher), Now)).Value!.ConversationIds.Single();
        Assert.Single(n.Sent); // the teacher is told about the first message

        await service.ReplyAsync(Ali, id, "Also: can I bring a friend?", Now.AddMinutes(1));
        Assert.Single(n.Sent); // not again: she has not read the first one yet

        var opened = await service.OpenAsync(Teacher, id, Now.AddMinutes(2));
        Assert.Equal(new[] { false, false }, opened.Value!.Messages.Select(m => m.Mine));
        Assert.Equal(2, opened.Value.Messages.Count);
        await service.ReplyAsync(Ali, id, "Thank you!", Now.AddMinutes(3));
        Assert.Equal(2, n.Sent.Count); // she read, so the next message is announced
        Assert.Equal(new[] { Teacher }, n.Sent.Last().To);
    }

    [Fact]
    public void NotificationText_IsShort_AndOnOneLine()
    {
        var (title, body) = MessagingService.NotificationText("Ms Khan", "Trip", "Line one\nline two " + new string('a', 300));
        Assert.Equal("Message from Ms Khan", title);
        Assert.StartsWith("Trip: Line one line two aaa", body);
        Assert.EndsWith("…", body);
        Assert.True(body.Length <= "Trip: ".Length + 160);
        Assert.Equal("Message from EduSphere", MessagingService.NotificationText(null, "x", "y").Title);
        Assert.True(MessagingService.IsStaff("Finance"));
        Assert.False(MessagingService.IsStaff("Parent"));
    }

    [Fact]
    public void Domain_KeepsSubjectsAndMessagesWithinLimits_AndRemembersWhoWasTold()
    {
        Assert.Throws<ArgumentException>(() => new Conversation(" ", Guid.NewGuid(), null, Now));
        Assert.Equal(Conversation.MaxSubjectLength, new Conversation(new string('s', 500), Guid.NewGuid(), null, Now).Subject.Length);
        Assert.Throws<ArgumentException>(() => new ConversationMessage(Guid.NewGuid(), Guid.NewGuid(), "", Now));
        Assert.Equal("Hi", new ConversationMessage(Guid.NewGuid(), Guid.NewGuid(), "  Hi  ", Now).Body);

        var p = new ConversationParticipant(Guid.NewGuid(), Guid.NewGuid());
        Assert.True(p.ShouldNotify(Now));
        Assert.False(p.ShouldNotify(Now.AddMinutes(1)));
        p.Read(Now.AddMinutes(2));
        Assert.True(p.ShouldNotify(Now.AddMinutes(3)));
        p.Archive(Now);
        p.Archive(Now.AddDays(1));
        Assert.Equal(Now, p.ArchivedAt); // the first archiving time is kept
    }
}
