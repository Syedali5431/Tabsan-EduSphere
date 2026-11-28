using Tabsan.EduSphere.Application.Community;
using Tabsan.EduSphere.Application.DTOs.Notifications;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Community;
using Tabsan.EduSphere.Domain.Notifications;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>
/// School news, events and forms: who sees what (school, campus, year level, class; students, parents through each child,
/// staff), who may write and manage, scheduled news announced once, event replies with guests and places, forms answered
/// per child by either parent, questions locked once answered, tracking, reminders and the CSV export.
/// </summary>
public class CommunityTests
{
    private static readonly DateTime Now = new(2026, 11, 28, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid School = Guid.NewGuid(), OtherSchool = Guid.NewGuid(), Gulberg = Guid.NewGuid(), Dha = Guid.NewGuid(),
        Class7 = Guid.NewGuid(), Class8 = Guid.NewGuid(), Maths7 = Guid.NewGuid(), Maths8 = Guid.NewGuid(), OtherClass = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid(), TeacherId = Guid.NewGuid(), AliId = Guid.NewGuid(), AliProfile = Guid.NewGuid(),
        SaraId = Guid.NewGuid(), SaraProfile = Guid.NewGuid(), MumId = Guid.NewGuid(), DadId = Guid.NewGuid(), ZoyaId = Guid.NewGuid(), ZoyaProfile = Guid.NewGuid();

    private static HashSet<Guid> Set(params Guid[] ids) => [.. ids];

    private static CommunityMember Admin(Guid? campus = null) => new(AdminId, "Admin", "Mrs Admin", School, campus is { } c ? Set(c) : null, Set(), Set(), Set(), [], Set());
    private static CommunityMember Teacher() => new(TeacherId, "Faculty", "Mr Ali Siddiqui", School, null, Set(Gulberg), Set(Class7), Set(Maths7), [], Set(Maths7));
    private static CommunityMember Ali() => new(AliId, "Student", "Ali", School, null, Set(Gulberg), Set(Class7), Set(Maths7), [], Set());
    private static CommunityMember Sara() => new(SaraId, "Student", "Sara", School, null, Set(Dha), Set(Class8), Set(Maths8), [], Set());
    private static CommunityChild AliChild => new(AliProfile, AliId, "Ali", Set(Gulberg), Set(Class7), Set(Maths7));
    private static CommunityChild ZoyaChild => new(ZoyaProfile, ZoyaId, "Zoya", Set(Dha), Set(Class8), Set(Maths8));
    /// <summary>Ali's and Zoya's mother; Ali's father.</summary>
    private static CommunityMember Mum() => new(MumId, "Parent", "Mrs Raza", School, null, Set(Gulberg, Dha), Set(Class7, Class8), Set(Maths7, Maths8), [AliChild, ZoyaChild], Set());
    private static CommunityMember Dad() => new(DadId, "Parent", "Mr Raza", School, null, Set(Gulberg), Set(Class7), Set(Maths7), [AliChild], Set());

    private sealed class Store : ICommunityStore
    {
        public readonly List<NewsPost> News = [];
        public readonly List<NewsRead> Reads = [];
        public readonly List<SchoolEvent> Events = [];
        public readonly List<EventReply> Replies = [];
        public readonly List<SchoolForm> Forms = [];
        public readonly List<FormField> Fields = [];
        public readonly List<FormResponse> Responses = [];
        public readonly Dictionary<Guid, AudienceTarget> Targets = [];
        public Func<CommunityItem, AudiencePeople> People = _ => new AudiencePeople();

        public Task<CommunityMember?> MemberAsync(Guid userId, string role, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<AudienceOptionsDto> AudienceOptionsAsync(CommunityMember member, CancellationToken ct = default) => Task.FromResult(new AudienceOptionsDto(true, [], [], []));
        public Task<AudienceNames> AudienceNamesAsync(IEnumerable<CommunityItem> items, CancellationToken ct = default)
            => Task.FromResult(new AudienceNames(new Dictionary<Guid, string> { [Gulberg] = "Gulberg" }, new Dictionary<Guid, string> { [Class7] = "Class 7" },
                new Dictionary<Guid, string> { [Maths7] = "MTH07 — Mathematics" }));
        public Task<AudienceTarget?> CampusAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Targets.GetValueOrDefault(id));
        public Task<AudienceTarget?> SemesterAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Targets.GetValueOrDefault(id));
        public Task<AudienceTarget?> OfferingAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Targets.GetValueOrDefault(id));
        public Task<AudiencePeople> PeopleAsync(CommunityItem item, CancellationToken ct = default) => Task.FromResult(People(item));
        public Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyDictionary<Guid, string>>(userIds.ToDictionary(id => id, id => id == MumId ? "Mrs Raza" : id == DadId ? "Mr Raza" : "Someone"));
        public Task<IReadOnlyList<NewsPost>> NewsAsync(Guid? tenantId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<NewsPost>>(News.ToList());
        public Task<NewsPost?> NewsPostAsync(Guid id, CancellationToken ct = default) => Task.FromResult(News.FirstOrDefault(p => p.Id == id));
        public void Add(NewsPost post) => News.Add(post);
        public void Remove(NewsPost post) => News.Remove(post);
        public Task<IReadOnlySet<Guid>> ReadPostsAsync(Guid userId, IReadOnlyCollection<Guid> postIds, CancellationToken ct = default)
            => Task.FromResult<IReadOnlySet<Guid>>(Reads.Where(r => r.UserId == userId && postIds.Contains(r.PostId)).Select(r => r.PostId).ToHashSet());
        public Task<IReadOnlyDictionary<Guid, int>> SeenCountsAsync(IReadOnlyCollection<Guid> postIds, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyDictionary<Guid, int>>(Reads.GroupBy(r => r.PostId).ToDictionary(g => g.Key, g => g.Count()));
        public void Add(NewsRead read) => Reads.Add(read);
        public Task<IReadOnlyList<NewsPost>> NewsToAnnounceAsync(DateTime now, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<NewsPost>>(News.Where(p => p.IsLive(now) && p.NotifiedAt is null).ToList());
        public Task<IReadOnlyList<SchoolEvent>> EventsAsync(Guid? tenantId, DateTime from, DateTime to, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SchoolEvent>>(Events.Where(e => e.StartsAt < to && e.EndsAt >= from).ToList());
        public Task<SchoolEvent?> EventAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Events.FirstOrDefault(e => e.Id == id));
        public void Add(SchoolEvent schoolEvent) => Events.Add(schoolEvent);
        public Task<Guid?> EventOfFormAsync(Guid formId, CancellationToken ct = default) => Task.FromResult(Events.FirstOrDefault(e => e.FormId == formId)?.Id);
        public Task<IReadOnlyList<EventReply>> RepliesAsync(IReadOnlyCollection<Guid> eventIds, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<EventReply>>(Replies.Where(r => eventIds.Contains(r.EventId)).ToList());
        public void Add(EventReply reply) => Replies.Add(reply);
        public Task<IReadOnlyList<SchoolForm>> FormsAsync(Guid? tenantId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<SchoolForm>>(Forms.ToList());
        public Task<SchoolForm?> FormAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Forms.FirstOrDefault(f => f.Id == id));
        public void Add(SchoolForm form) => Forms.Add(form);
        public void Remove(SchoolForm form) => Forms.Remove(form);
        public Task<IReadOnlyList<FormField>> FieldsAsync(Guid formId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<FormField>>(Fields.Where(f => f.FormId == formId).OrderBy(f => f.Position).ToList());
        public void ReplaceFields(IReadOnlyList<FormField> old, IReadOnlyList<FormField> fields) { foreach (var f in old) Fields.Remove(f); Fields.AddRange(fields); }
        public Task<IReadOnlyList<FormResponse>> ResponsesAsync(IReadOnlyCollection<Guid> formIds, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<FormResponse>>(Responses.Where(r => formIds.Contains(r.FormId)).ToList());
        public void Add(FormResponse response) => Responses.Add(response);
        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class Notifications : INotificationService
    {
        public readonly List<(string Title, string Body, NotificationType Type, IReadOnlyList<Guid> To)> Sent = [];
        public Task<Guid> SendSystemAsync(string title, string body, NotificationType type, IReadOnlyList<Guid> recipientUserIds, CancellationToken ct = default)
        { Sent.Add((title, body, type, recipientUserIds)); return Task.FromResult(Guid.NewGuid()); }
        public Task<Guid> SendAsync(SendNotificationRequest request, Guid senderUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> DeactivateAsync(Guid notificationId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<NotificationResponse>> GetInboxAsync(Guid userId, bool unreadOnly = false, int page = 1, int pageSize = 20, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<NotificationBadgeResponse> GetBadgeAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> MarkReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task MarkAllReadAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private static (Store Store, Notifications Sent, CommunityService Service) World()
    {
        var store = new Store();
        store.Targets[Gulberg] = new(Gulberg, School, Gulberg, null);
        store.Targets[Dha] = new(Dha, School, Dha, null);
        store.Targets[Class7] = new(Class7, null, null, Class7);
        store.Targets[Maths7] = new(Maths7, School, Gulberg, Class7);
        store.Targets[OtherClass] = new(OtherClass, OtherSchool, Guid.NewGuid(), Class7);
        store.People = item =>
        {
            var p = new AudiencePeople();
            var students = new[] { new AudienceStudent(AliProfile, AliId, "Ali", "Class 7"), new AudienceStudent(ZoyaProfile, ZoyaId, "Zoya", "Class 8") }
                .Where(s => item.CampusId is null || (s.StudentProfileId == AliProfile ? Gulberg : Dha) == item.CampusId)
                .Where(s => item.SemesterId is null || (s.StudentProfileId == AliProfile ? Class7 : Class8) == item.SemesterId).ToList();
            p.Students.AddRange(students);
            if (students.Any(s => s.StudentProfileId == AliProfile)) p.Parents.AddRange([(MumId, "Mrs Raza", AliProfile), (DadId, "Mr Raza", AliProfile)]);
            if (students.Any(s => s.StudentProfileId == ZoyaProfile)) p.Parents.Add((MumId, "Mrs Raza", ZoyaProfile));
            p.Staff.Add((TeacherId, "Mr Ali Siddiqui", "Faculty"));
            return p;
        };
        var sent = new Notifications();
        return (store, sent, new CommunityService(store, sent));
    }

    private static AudienceRequest To(Guid? campus = null, Guid? semester = null, Guid? offering = null, params string[] roles) => new(campus, semester, offering, roles);

    private static SaveNewsRequest News(string title, AudienceRequest audience, DateTime? at = null, bool publish = true, bool pinned = false)
        => new(title, null, "All the details.", null, audience, at, null, pinned, publish);

    [Fact]
    public async Task News_ReachesItsAudienceOnly_AndParentsThroughEachChild()
    {
        var (store, sent, service) = World();
        Assert.Equal(201, (await service.SaveNewsAsync(Admin(), null, News("Sports day", To(Gulberg)), Now)).Status);
        await service.SaveNewsAsync(Admin(), null, News("Class 8 trip", To(Dha)), Now);
        await service.SaveNewsAsync(Admin(), null, News("Staff meeting", To(roles: "Staff")), Now);
        await service.SaveNewsAsync(Admin(), null, News("Parents' evening", To(Gulberg, roles: "Parents")), Now);
        await service.SaveNewsAsync(Admin(), null, News("Draft", To(), publish: false), Now);
        await service.SaveNewsAsync(Admin(), null, News("Next week", To(), Now.AddDays(7)), Now);

        async Task<string[]> Titles(CommunityMember m) => (await service.NewsFeedAsync(m, Now)).Select(n => n.Title).OrderBy(t => t).ToArray();
        Assert.Equal(["Sports day"], await Titles(Ali()));
        Assert.Equal(["Class 8 trip"], await Titles(Sara()));
        Assert.Equal(["Class 8 trip", "Parents' evening", "Sports day"], await Titles(Mum())); // through Ali and through Zoya
        Assert.Equal(["Parents' evening", "Sports day"], await Titles(Dad()));
        Assert.Equal(["Sports day", "Staff meeting"], await Titles(Teacher()));
        Assert.Equal(["Class 8 trip", "Parents' evening", "Sports day", "Staff meeting"], await Titles(Admin()));
        Assert.Equal(["Parents' evening", "Sports day", "Staff meeting"], await Titles(Admin(Gulberg))); // an admin of one campus
        Assert.Equal(4, sent.Sent.Count); // live posts only; the draft and next week's post wait
        Assert.All(sent.Sent, s => Assert.Equal(NotificationType.SchoolNews, s.Type));
        Assert.Equal([MumId, DadId], sent.Sent.Single(s => s.Title == "Parents' evening").To); // parents only, not the students

        // Next week's post is announced once when its time comes.
        Assert.Equal(1, await service.NotifyDueAsync(Now.AddDays(7).AddMinutes(1)));
        Assert.Equal(0, await service.NotifyDueAsync(Now.AddDays(7).AddMinutes(2)));
        Assert.Equal(5, sent.Sent.Count);
    }

    [Fact]
    public async Task News_ReadMarks_SeenCounts_PinningAndWithdrawing()
    {
        var (store, _, service) = World();
        var first = (await service.SaveNewsAsync(Admin(), null, News("Uniform", To()), Now.AddHours(-2))).Value!;
        var pinned = (await service.SaveNewsAsync(Admin(), null, News("Term dates", To(), Now.AddHours(-5), pinned: true), Now)).Value!;
        var feed = await service.NewsFeedAsync(Ali(), Now);
        Assert.Equal(["Term dates", "Uniform"], feed.Select(n => n.Title)); // pinned first
        Assert.All(feed, n => Assert.True(n.IsNew));
        Assert.Equal("All the details.", feed[0].Summary);                  // no summary: the start of the post

        var opened = await service.NewsPostAsync(Ali(), first.Id, Now);
        Assert.True(opened.Ok);
        Assert.Null(opened.Value!.SeenBy);                                    // students do not see counts
        await service.NewsPostAsync(Ali(), first.Id, Now);                     // read once
        Assert.False((await service.NewsFeedAsync(Ali(), Now)).Single(n => n.Id == first.Id).IsNew);
        Assert.Equal(1, (await service.ManageNewsAsync(Admin(), Now)).Value!.Single(n => n.Id == first.Id).SeenBy);
        Assert.Equal(5, (await service.NewsPostAsync(Admin(), first.Id, Now)).Value!.AudienceSize); // Ali, Zoya, two parents, the teacher

        Assert.True((await service.WithdrawNewsAsync(Admin(), pinned.Id)).Ok);
        Assert.Equal(["Uniform"], (await service.NewsFeedAsync(Ali(), Now)).Select(n => n.Title));
        Assert.Equal("Withdrawn", (await service.ManageNewsAsync(Admin(), Now)).Value!.Single(n => n.Id == pinned.Id).Status);
        Assert.Equal(404, (await service.NewsPostAsync(Ali(), pinned.Id, Now)).Status);
        var draft = (await service.SaveNewsAsync(Admin(), null, News("Unseen", To(), publish: false), Now)).Value!;
        await service.WithdrawNewsAsync(Admin(), draft.Id);
        Assert.DoesNotContain(store.News, n => n.Id == draft.Id);             // a draft nobody saw is simply deleted
    }

    [Fact]
    public async Task Writers_AdminsForTheirSchoolOrCampus_TeachersForTheirOwnClasses()
    {
        var (_, _, service) = World();
        Assert.Equal(403, (await service.SaveNewsAsync(Ali(), null, News("Hi", To()), Now)).Status);
        Assert.Equal(403, (await service.SaveNewsAsync(Mum(), null, News("Hi", To()), Now)).Status);
        Assert.Equal("Teachers write to their own classes: choose one of your classes.", (await service.SaveNewsAsync(Teacher(), null, News("Hi", To(Gulberg)), Now)).Error);
        Assert.Equal(400, (await service.SaveNewsAsync(Teacher(), null, News("Hi", To(offering: OtherClass)), Now)).Status);
        var mine = await service.SaveNewsAsync(Teacher(), null, News("Maths test on Friday", To(offering: Maths7)), Now);
        Assert.Equal(201, mine.Status);
        Assert.Equal((null, "MTH07 — Mathematics"), (mine.Value!.Audience.CampusName, mine.Value.Audience.CourseName));
        Assert.Equal("That campus or class is not part of your school.", (await service.SaveNewsAsync(Admin(), null, News("Hi", To(offering: OtherClass)), Now)).Error);
        Assert.Equal("You write for your own campus.", (await service.SaveNewsAsync(Admin(Gulberg), null, News("Hi", To(Dha)), Now)).Error);
        var campusAdmin = await service.SaveNewsAsync(Admin(Gulberg), null, News("Gulberg only", To()), Now);
        Assert.Equal(Gulberg, campusAdmin.Value!.Audience.CampusId);           // an admin of one campus writes for it
        Assert.Equal("Unknown audience \"Visitors\".", (await service.SaveNewsAsync(Admin(), null, News("Hi", To(roles: "Visitors")), Now)).Error);

        // Teachers manage what they wrote; admins everything of their school.
        var other = new CommunityMember(Guid.NewGuid(), "Faculty", "Ms Other", School, null, Set(Gulberg), Set(Class7), Set(Maths7), [], Set(Maths7));
        Assert.Equal(403, (await service.WithdrawNewsAsync(other, mine.Value.Id)).Status);
        Assert.True((await service.WithdrawNewsAsync(Admin(), mine.Value.Id)).Ok);
    }

    [Fact]
    public async Task EventReplies_WithGuestsAndPlaces_UntilRepliesClose()
    {
        var (_, sent, service) = World();
        var start = Now.AddDays(10);
        var saved = await service.SaveEventAsync(Admin(), null, new SaveEventRequest("Parents' evening", "Meet the teachers.", "Main hall", "Meeting", start,
            start.AddHours(3), false, true, start.AddDays(-2), 4, 2, null, To(Gulberg, roles: "Parents"), true), Now);
        Assert.Equal(201, saved.Status);
        var id = saved.Value!.Id;
        Assert.Equal("Parents' evening", sent.Sent.Single().Title);
        Assert.Contains("Main hall", sent.Sent.Single().Body);

        Assert.Equal(404, (await service.ReplyAsync(Ali(), id, new EventReplyRequest("Going", 0, null), Now)).Status); // for parents
        Assert.Equal("Bring at most 2 guests.", (await service.ReplyAsync(Mum(), id, new EventReplyRequest("Going", 3, null), Now)).Error);
        var mum = await service.ReplyAsync(Mum(), id, new EventReplyRequest("Going", 2, "Both of us and grandma"), Now);
        Assert.Equal(("Going", 2, 3), (mum.Value!.MyAnswer, mum.Value.MyGuests, mum.Value.Counts!.Places));
        Assert.Equal("Only 1 place is left.", (await service.ReplyAsync(Dad(), id, new EventReplyRequest("Going", 1, null), Now)).Error);
        Assert.True((await service.ReplyAsync(Dad(), id, new EventReplyRequest("Going", 0, null), Now)).Ok);
        Assert.Equal("The event is full.", (await service.ReplyAsync(Mum() with { UserId = Guid.NewGuid() }, id, new EventReplyRequest("Going", 0, null), Now)).Error);
        Assert.True((await service.ReplyAsync(Mum(), id, new EventReplyRequest("NotGoing", 2, null), Now)).Ok); // changing a reply frees places
        var replies = await service.EventRepliesAsync(Admin(), id, Now);
        Assert.Equal([("Mr Raza", "Going"), ("Mrs Raza", "NotGoing")], replies.Value!.Replies.Select(r => (r.Name, r.Answer)));
        Assert.Equal(0, replies.Value.Replies.Single(r => r.Name == "Mrs Raza").Guests);
        Assert.Equal(403, (await service.EventRepliesAsync(Teacher(), id, Now)).Status);
        Assert.Equal("Replies are closed.", (await service.ReplyAsync(Dad(), id, new EventReplyRequest("Maybe", 0, null), start.AddDays(-1))).Error);

        var cancelled = await service.CancelEventAsync(Admin(), id, "Snow day", Now);
        Assert.Equal(("Cancelled", "Snow day"), (cancelled.Value!.Status, cancelled.Value.CancelReason));
        Assert.Equal(("Cancelled: Parents' evening", "Snow day"), (sent.Sent.Last().Title, sent.Sent.Last().Body));
        Assert.Equal("The event is cancelled.", (await service.ReplyAsync(Dad(), id, new EventReplyRequest("Going", 0, null), Now)).Error);
        Assert.Single((await service.EventsAsync(Dad(), Now, Now.AddDays(30), Now))); // still listed, as cancelled
    }

    [Fact]
    public void Events_CheckTheirTimesAndReplies()
    {
        Assert.Throws<ArgumentException>(() => new SchoolEvent(AdminId, "Trip", Now, Now, false));
        Assert.Throws<ArgumentException>(() => new SchoolEvent(AdminId, "Trip", Now, Now.AddDays(40), false));
        var allDay = new SchoolEvent(AdminId, "Holiday", Now.AddHours(5), Now.AddHours(5), true);
        Assert.Equal((Now.Date, Now.Date), (allDay.StartsAt, allDay.EndsAt));
        var trip = new SchoolEvent(AdminId, "Trip", Now.AddDays(3), Now.AddDays(3).AddHours(6), false);
        Assert.Throws<ArgumentException>(() => trip.ConfigureReplies(true, Now.AddDays(4), null, 0, null));
        Assert.Throws<ArgumentException>(() => trip.ConfigureReplies(true, null, null, 11, null));
    }

    private static SaveFormRequest Consent(AudienceRequest audience, bool open = true, DateTime? due = null) => new("Zoo trip consent", "Friday's trip to the zoo.",
        due ?? Now.AddDays(5), audience,
        [
            new FormFieldRequest("Information", "The bus leaves at 8:00.", null, false, null),
            new FormFieldRequest("YesNo", "May your child go on the trip?", null, true, null),
            new FormFieldRequest("SingleChoice", "Lunch", null, true, ["Packed lunch", "School lunch"]),
            new FormFieldRequest("MultipleChoice", "Allergies", null, false, ["Nuts", "Dairy", "Eggs"]),
            new FormFieldRequest("Signature", "Your full name", null, true, null)
        ], open);

    [Fact]
    public async Task ParentForms_OneAnswerPerChild_EitherParent_Tracked()
    {
        var (store, sent, service) = World();
        var form = (await service.SaveFormAsync(Admin(), null, Consent(To(roles: "Parents")), Now)).Value!;
        Assert.Equal("Please fill in: Zoo trip consent", sent.Sent.Single().Title);
        Assert.Equal([MumId, DadId], sent.Sent.Single().To);

        var mumRows = await service.FormsToFillAsync(Mum(), Now);
        Assert.Equal(["Ali", "Zoya"], mumRows.Select(r => r.StudentName).OrderBy(n => n));
        Assert.Empty(await service.FormsToFillAsync(Ali(), Now));            // a parent form
        Assert.Equal("Choose which child you are answering for.", (await service.FormAsync(Mum(), form.Id, null, Now)).Error);
        Assert.Equal("Ali", (await service.FormAsync(Dad(), form.Id, null, Now)).Value!.StudentName); // one child: chosen for them

        var fields = form.Fields;
        Guid F(string label) => fields.Single(f => f.Label == label).Id;
        Dictionary<Guid, string?> Answers(string go, string lunch, string? allergies = null) => new()
        {
            [F("May your child go on the trip?")] = go, [F("Lunch")] = lunch, [F("Allergies")] = allergies, [F("Your full name")] = "Nadia Raza"
        };
        Assert.Equal("Answer \"Lunch\".", (await service.SubmitFormAsync(Mum(), form.Id, new SubmitFormRequest(AliProfile, Answers("Yes", "")), Now)).Error);
        Assert.Equal("Choose one of the answers to \"Lunch\".", (await service.SubmitFormAsync(Mum(), form.Id, new SubmitFormRequest(AliProfile, Answers("Yes", "Pizza")), Now)).Error);
        Assert.Equal("Choose which child you are answering for.", (await service.SubmitFormAsync(Dad(), form.Id, new SubmitFormRequest(ZoyaProfile, Answers("Yes", "School lunch")), Now)).Error);
        var done = await service.SubmitFormAsync(Mum(), form.Id, new SubmitFormRequest(AliProfile, Answers("yes", "Packed lunch", "Eggs\nNuts")), Now);
        Assert.True(done.Ok, done.Error);
        Assert.Equal("Nuts\nEggs", done.Value!.Answers[F("Allergies")]);    // in the form's order
        Assert.Equal("Yes", done.Value.Answers[F("May your child go on the trip?")]);

        // Either parent: Dad sees Mum's answer for Ali and may change it.
        var dadView = (await service.FormsToFillAsync(Dad(), Now)).Single();
        Assert.Equal((true, "Mrs Raza"), (dadView.Answered, dadView.AnsweredBy));
        await service.SubmitFormAsync(Dad(), form.Id, new SubmitFormRequest(AliProfile, Answers("No", "School lunch")), Now.AddMinutes(5));
        Assert.Single(store.Responses);

        var tracking = (await service.FormTrackingAsync(Admin(), form.Id, Now)).Value!;
        Assert.Equal((2, 1), (tracking.Expected, tracking.Answered));
        var ali = tracking.Rows.Single(r => r.Name == "Ali");
        Assert.Equal(("Parent", true, "Mr Raza", "Mrs Raza, Mr Raza"), (ali.Kind, ali.Answered, ali.AnsweredBy, ali.Parents));
        Assert.Equal(1, tracking.Summary.Single(s => s.Label == "May your child go on the trip?").Counts["No"]);

        // A reminder goes to the parents of children without an answer (Zoya's mother), at most hourly.
        Assert.Equal(1, (await service.RemindFormAsync(Admin(), form.Id, Now)).Value);
        Assert.Equal([MumId], sent.Sent.Last().To);
        Assert.Equal(409, (await service.RemindFormAsync(Admin(), form.Id, Now.AddMinutes(30))).Status);

        var csv = System.Text.Encoding.UTF8.GetString((await service.ExportFormAsync(Admin(), form.Id)).Value.Content).TrimStart('﻿');
        Assert.StartsWith("For,Class,Answered by,Answered at (UTC),May your child go on the trip?,Lunch,Allergies,Your full name\r\n", csv);
        Assert.Contains("Ali,Class 7,Mr Raza,2026-11-28 09:05,No,School lunch,,Nadia Raza", csv);

        // Questions are locked once answered; the title is not.
        Assert.Equal(409, (await service.SaveFormAsync(Admin(), form.Id, Consent(To(roles: "Parents")) with { Fields = [new("YesNo", "Different?", null, true, null)] }, Now)).Status);
        Assert.True((await service.SaveFormAsync(Admin(), form.Id, Consent(To(roles: "Parents")) with { Title = "Zoo trip (Friday)" }, Now)).Ok);

        await service.OpenFormAsync(Admin(), form.Id, false, Now);
        Assert.Equal("The form is closed.", (await service.SubmitFormAsync(Mum(), form.Id, new SubmitFormRequest(ZoyaProfile, Answers("Yes", "Packed lunch")), Now)).Error);
    }

    [Fact]
    public async Task StudentForms_DueReminders_AndFormChecks()
    {
        var (_, sent, service) = World();
        Assert.Equal("Add at least one question before opening the form.",
            (await service.SaveFormAsync(Admin(), null, Consent(To()) with { Fields = [new("Information", "Read this", null, false, null)] }, Now)).Error);
        Assert.Equal("\"Lunch\" needs at least two choices.",
            (await service.SaveFormAsync(Admin(), null, Consent(To()) with { Fields = [new("SingleChoice", "Lunch", null, true, ["Only one"])] }, Now)).Error);
        Assert.Equal("Question 1: choose a kind of question.",
            (await service.SaveFormAsync(Admin(), null, Consent(To()) with { Fields = [new("Drawing", "Draw", null, true, null)] }, Now)).Error);

        var survey = (await service.SaveFormAsync(Admin(), null, new SaveFormRequest("Club survey", null, Now.AddHours(20), To(Gulberg, roles: "Students"),
            [new("ShortText", "Which club?", null, true, null), new("Number", "Hours a week", null, false, null), new("Date", "Start date", null, false, null)], true), Now)).Value!;
        var row = Assert.Single(await service.FormsToFillAsync(Ali(), Now));
        Assert.Null(row.StudentProfileId);
        Assert.Empty(await service.FormsToFillAsync(Sara(), Now));           // another campus
        Assert.Empty(await service.FormsToFillAsync(Mum(), Now));            // a student form
        var q = survey.Fields.ToDictionary(f => f.Label, f => f.Id);
        Assert.Equal("Enter a number for \"Hours a week\".", (await service.SubmitFormAsync(Ali(), survey.Id,
            new SubmitFormRequest(null, new Dictionary<Guid, string?> { [q["Which club?"]] = "Chess", [q["Hours a week"]] = "two" }), Now)).Error);
        Assert.Equal("Enter a date for \"Start date\".", (await service.SubmitFormAsync(Ali(), survey.Id,
            new SubmitFormRequest(null, new Dictionary<Guid, string?> { [q["Which club?"]] = "Chess", [q["Start date"]] = "next week" }), Now)).Error);

        // A day before it is due, everyone who has not answered is reminded automatically, once a day.
        var before = sent.Sent.Count;
        Assert.Equal(1, await service.NotifyDueAsync(Now.AddHours(1)));
        Assert.Equal("Reminder: Club survey", sent.Sent.Last().Title);
        Assert.Equal([AliId], sent.Sent.Last().To);
        Assert.Equal(0, await service.NotifyDueAsync(Now.AddHours(2)));
        Assert.Equal(before + 1, sent.Sent.Count);
    }

    [Fact]
    public void FormFields_CheckTheirQuestions()
    {
        var form = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => new FormField(form, 1, FormFieldType.ShortText, " ", null, true, null));
        Assert.Throws<ArgumentException>(() => new FormField(form, 1, FormFieldType.MultipleChoice, "Pick", null, true, ["a", "A"])); // the same choice twice
        Assert.False(new FormField(form, 1, FormFieldType.Information, "Note", null, true, null).Required);
        Assert.Equal(["Nuts", "Dairy"], new FormField(form, 1, FormFieldType.MultipleChoice, "Allergies", null, false, [" Nuts ", "Dairy", ""]).Options);
        Assert.Throws<ArgumentException>(() => new NewsPost(AdminId, "Hi", null, "Body", "http://insecure.example/pic.jpg", Now));
    }
}
