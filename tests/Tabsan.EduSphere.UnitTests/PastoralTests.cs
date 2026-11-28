using Tabsan.EduSphere.Application.Community;
using Tabsan.EduSphere.Application.DTOs.Notifications;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Pastoral;
using Tabsan.EduSphere.Domain.Notifications;
using Tabsan.EduSphere.Domain.Pastoral;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>
/// Pastoral care and wellbeing: who sees which notes (teachers, the wellbeing team by campus and year level, confidential
/// notes, admins of some campuses), what students and parents see, alerts for serious concerns and runs of concerns (the
/// admins when nobody on the team covers the student), follow-ups and their reminders, removed notes, notes for a whole
/// class, support plans, check-ins, the overview and setting a school up.
/// </summary>
public class PastoralTests
{
    private static readonly DateTime Now = new(2026, 11, 28, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid School = Guid.NewGuid(), Gulberg = Guid.NewGuid(), Dha = Guid.NewGuid(), Class7 = Guid.NewGuid(), Class8 = Guid.NewGuid(),
        Maths7 = Guid.NewGuid(), Maths8 = Guid.NewGuid();
    private static readonly Guid TeacherId = Guid.NewGuid(), Teacher2Id = Guid.NewGuid(), CounsellorId = Guid.NewGuid(), HeadId = Guid.NewGuid(),
        AdminId = Guid.NewGuid(), CampusAdminId = Guid.NewGuid(), AliId = Guid.NewGuid(), AliProfile = Guid.NewGuid(), ZoyaId = Guid.NewGuid(),
        ZoyaProfile = Guid.NewGuid(), MumId = Guid.NewGuid(), DadId = Guid.NewGuid();

    private static HashSet<Guid> Set(params Guid[] ids) => [.. ids];

    private static readonly PastoralStudent Ali = new(AliProfile, AliId, "Ali Raza", "GUL-001", School, Gulberg, Set(Gulberg), Set(Class7), Set(Maths7), "Class 7",
        Set(TeacherId), [MumId, DadId]);
    private static readonly PastoralStudent Zoya = new(ZoyaProfile, ZoyaId, "Zoya Raza", "DHA-001", School, Dha, Set(Dha), Set(Class8), Set(Maths8), "Class 8",
        Set(Teacher2Id), [MumId]);

    private sealed class Store : IPastoralStore
    {
        public readonly Dictionary<Guid, PastoralStudent> Students = new() { [AliProfile] = Ali, [ZoyaProfile] = Zoya };
        public readonly List<PastoralRecord> Records = [];
        public readonly List<PastoralNote> Notes = [];
        public readonly List<SupportPlan> Plans = [];
        public readonly List<WellbeingCheckIn> CheckIns = [];
        public readonly List<PastoralTeamMember> Team = [];
        public readonly List<PastoralCategory> Categories = [];
        public readonly List<PastoralSettings> Settings = [];
        public readonly List<Guid> Admins = [AdminId];
        public static readonly Dictionary<Guid, string> People = new()
        {
            [TeacherId] = "Mr Siddiqui", [Teacher2Id] = "Ms Khan", [CounsellorId] = "Mrs Counsellor", [HeadId] = "Mr Head", [AdminId] = "Mrs Admin",
            [CampusAdminId] = "Mr Dha Admin", [AliId] = "Ali Raza", [ZoyaId] = "Zoya Raza", [MumId] = "Mrs Raza", [DadId] = "Mr Raza"
        };

        public Task<PastoralViewer?> ViewerAsync(Guid userId, string role, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PastoralStudent>> StudentsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PastoralStudent>>(ids.Where(Students.ContainsKey).Select(id => Students[id]).ToList());
        public Task<IReadOnlyList<PastoralStudent>> FindStudentsAsync(Guid? tenantId, IReadOnlyCollection<Guid>? offerings, string? search, int take, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PastoralStudent>>(Students.Values.Where(s => offerings is null || s.Offerings.Any(offerings.Contains))
                .Where(s => search is null || s.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).Take(take).ToList());
        public Task<FilterOptionsDto> FilterOptionsAsync(Guid? tenantId, IReadOnlySet<Guid>? campuses, IReadOnlySet<Guid>? offerings, CancellationToken ct = default)
            => Task.FromResult(new FilterOptionsDto(new[] { new OptionDto(Gulberg, "Gulberg"), new OptionDto(Dha, "DHA") }.Where(c => campuses is null || campuses.Contains(c.Id)).ToList(),
                [new OptionDto(Class7, "Class 7"), new OptionDto(Class8, "Class 8")], []));
        public Task<IReadOnlyList<OptionDto>> SchoolsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<OptionDto>>([new OptionDto(School, "Tabsan School")]);
        public Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyDictionary<Guid, string>>(userIds.Where(People.ContainsKey).Distinct().ToDictionary(id => id, id => People[id]));
        public Task<IReadOnlyDictionary<Guid, string>> PlaceNamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyDictionary<Guid, string>>(ids.Distinct().ToDictionary(id => id, id => id == Gulberg ? "Gulberg" : id == Class7 ? "Class 7" : "Somewhere"));
        public Task<IReadOnlyList<Guid>> AdminsAsync(Guid? tenantId, IReadOnlySet<Guid> campuses, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Guid>>(Admins);
        public Task<IReadOnlyList<PersonOptionDto>> StaffAsync(Guid tenantId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PersonOptionDto>>(new[] { TeacherId, Teacher2Id, CounsellorId, HeadId, AdminId }.Select(id => new PersonOptionDto(id, People[id], "Teacher")).ToList());

        public Task<IReadOnlyList<PastoralRecord>> RecordsOfAsync(Guid studentProfileId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PastoralRecord>>(Records.Where(r => r.StudentProfileId == studentProfileId).ToList());
        public Task<IReadOnlyList<PastoralRecord>> RecordsAsync(Guid? tenantId, DateTime from, DateTime to, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PastoralRecord>>(Records.Where(r => !r.IsRemoved && r.OccurredAt >= from && r.OccurredAt < to).ToList());
        public Task<IReadOnlyList<PastoralRecord>> OpenFollowUpsAsync(Guid? tenantId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PastoralRecord>>(Records.Where(r => !r.IsRemoved && r.FollowUp == FollowUpStatus.Open).ToList());
        public Task<PastoralRecord?> RecordAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Records.FirstOrDefault(r => r.Id == id));
        public Task<int> ConcernCountAsync(Guid studentProfileId, DateTime since, CancellationToken ct = default)
            => Task.FromResult(Records.Count(r => r.StudentProfileId == studentProfileId && !r.IsRemoved && r.Tone == PastoralTone.Concern
                                                  && r.Kind is PastoralKind.Behaviour or PastoralKind.Wellbeing && r.OccurredAt >= since));
        public void Add(PastoralRecord record) => Records.Add(record);
        public Task<IReadOnlyList<PastoralNote>> NotesAsync(IReadOnlyCollection<Guid> recordIds, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PastoralNote>>(Notes.Where(n => recordIds.Contains(n.RecordId)).ToList());
        public void Add(PastoralNote note) => Notes.Add(note);
        public Task<IReadOnlyList<PastoralRecord>> FollowUpsDueAsync(DateTime now, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PastoralRecord>>(Records.Where(r => !r.IsRemoved && r.FollowUp == FollowUpStatus.Open && r.FollowUpBy <= now && r.FollowUpRemindedAt is null).ToList());

        public Task<IReadOnlyList<SupportPlan>> PlansOfAsync(Guid studentProfileId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SupportPlan>>(Plans.Where(p => p.StudentProfileId == studentProfileId).ToList());
        public Task<IReadOnlyList<SupportPlan>> ActivePlansAsync(Guid? tenantId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SupportPlan>>(Plans.Where(p => p.Status == SupportPlanStatus.Active).ToList());
        public Task<SupportPlan?> PlanAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Plans.FirstOrDefault(p => p.Id == id));
        public void Add(SupportPlan plan) => Plans.Add(plan);
        public Task<IReadOnlyList<SupportPlan>> PlansToReviewAsync(DateTime now, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SupportPlan>>(Plans.Where(p => p.Status == SupportPlanStatus.Active && p.ReviewOn <= now && p.ReviewRemindedAt is null).ToList());

        public Task<IReadOnlyList<WellbeingCheckIn>> CheckInsOfAsync(Guid studentProfileId, int take, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<WellbeingCheckIn>>(CheckIns.Where(c => c.StudentProfileId == studentProfileId).OrderByDescending(c => c.CheckedInAt).Take(take).ToList());
        public Task<IReadOnlyList<WellbeingCheckIn>> CheckInsAsync(Guid? tenantId, DateTime since, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<WellbeingCheckIn>>(CheckIns.Where(c => c.CheckedInAt >= since).ToList());
        public Task<IReadOnlyList<WellbeingCheckIn>> CheckInsNeedingAttentionAsync(Guid? tenantId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<WellbeingCheckIn>>(CheckIns.Where(c => c.NeedsAttention).ToList());
        public Task<WellbeingCheckIn?> CheckInAsync(Guid id, CancellationToken ct = default) => Task.FromResult(CheckIns.FirstOrDefault(c => c.Id == id));
        public void Add(WellbeingCheckIn checkIn) => CheckIns.Add(checkIn);

        public Task<IReadOnlyList<TeamMemberRow>> TeamAsync(Guid? tenantId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TeamMemberRow>>(Team.Where(t => tenantId is null || t.TenantId == tenantId).Select(t => new TeamMemberRow(t, People[t.UserId])).ToList());
        public Task<PastoralTeamMember?> TeamMemberAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Team.FirstOrDefault(t => t.Id == id));
        public void Add(PastoralTeamMember member) => Team.Add(member);
        public void Remove(PastoralTeamMember member) => Team.Remove(member);
        public Task<IReadOnlyList<PastoralCategory>> CategoriesAsync(Guid? tenantId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PastoralCategory>>(Categories.Where(c => c.TenantId == tenantId).ToList());
        public Task<PastoralCategory?> CategoryAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Categories.FirstOrDefault(c => c.Id == id));
        public void Add(PastoralCategory category) => Categories.Add(category);
        public Task<PastoralSettings?> SettingsAsync(Guid tenantId, CancellationToken ct = default) => Task.FromResult(Settings.FirstOrDefault(s => s.TenantId == tenantId));
        public void Add(PastoralSettings settings) => Settings.Add(settings);
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

        public IReadOnlyList<Guid> To(string title) => Sent.Where(s => s.Title == title).SelectMany(s => s.To).ToList();
    }

    /// <summary>A school where the counsellor looks after everyone (and sees confidential notes) and Mr Head looks after
    /// Class 7 at Gulberg.</summary>
    private static (Store Store, Notifications Sent, PastoralService Service) World(bool withTeam = true)
    {
        var store = new Store();
        if (withTeam)
        {
            store.Team.Add(new PastoralTeamMember(School, CounsellorId, null, null, "School counsellor", true, AdminId));
            store.Team.Add(new PastoralTeamMember(School, HeadId, Gulberg, Class7, "Head of Class 7", false, AdminId));
        }
        var sent = new Notifications();
        return (store, sent, new PastoralService(store, sent));
    }

    private static PastoralViewer Staff(Store store, Guid id, string role = "Faculty", IReadOnlySet<Guid>? adminCampuses = null)
    {
        var teaching = id == TeacherId ? Set(Maths7) : id == Teacher2Id ? Set(Maths8) : Set();
        var member = new CommunityMember(id, role, Store.People[id], School, adminCampuses, Set(), Set(), teaching, [], teaching);
        var team = store.Team.Where(t => t.UserId == id).Select(t => new TeamScope(t.Id, t.TenantId, t.CampusId, t.SemesterId, t.SeesConfidential, t.Title)).ToList();
        return new PastoralViewer(member, null, team);
    }

    private static PastoralViewer Teacher(Store s) => Staff(s, TeacherId);
    private static PastoralViewer Teacher2(Store s) => Staff(s, Teacher2Id);
    private static PastoralViewer Counsellor(Store s) => Staff(s, CounsellorId);
    private static PastoralViewer Head(Store s) => Staff(s, HeadId);
    private static PastoralViewer Admin(Store s) => Staff(s, AdminId, "Admin");
    private static PastoralViewer DhaAdmin(Store s) => Staff(s, CampusAdminId, "Admin", Set(Dha));

    private static PastoralViewer AliHimself()
        => new(new CommunityMember(AliId, "Student", "Ali Raza", School, null, Set(Gulberg), Set(Class7), Set(Maths7), [], Set()), AliProfile, []);

    private static CommunityChild Child(PastoralStudent s) => new(s.StudentProfileId, s.UserId, s.Name, s.Campuses, s.Semesters, s.Offerings);
    private static PastoralViewer Mum() => new(new CommunityMember(MumId, "Parent", "Mrs Raza", School, null, Set(), Set(), Set(), [Child(Ali), Child(Zoya)], Set()), null, []);
    private static PastoralViewer Dad() => new(new CommunityMember(DadId, "Parent", "Mr Raza", School, null, Set(), Set(), Set(), [Child(Ali)], Set()), null, []);

    private static SaveRecordRequest Note(Guid student, string kind = "Behaviour", string tone = "Positive", string severity = "Low", string summary = "Great effort in class",
        bool confidential = false, bool student_ = false, bool parents = false, Guid? followUp = null, DateTime? by = null, string? category = null, DateTime? at = null)
        => new([student], null, kind, tone, severity, category, summary, null, null, at, confidential, student_, parents, followUp, by);

    private static async Task<PastoralRecord> WriteAsync(PastoralService service, Store store, PastoralViewer by, SaveRecordRequest request)
    {
        var result = await service.CreateRecordsAsync(by, request, Now);
        Assert.True(result.Ok, result.Error);
        return store.Records.Single(r => r.Id == result.Value!.RecordIds.Single());
    }

    [Fact]
    public async Task Teachers_write_for_their_students_and_see_staff_notes_but_not_confidential_ones()
    {
        var (store, _, service) = World();
        var praise = await WriteAsync(service, store, Teacher(store), Note(AliProfile));
        var secret = await WriteAsync(service, store, Counsellor(store), Note(AliProfile, "Wellbeing", "Concern", summary: "Family difficulties", confidential: true));

        Assert.Equal(403, (await service.CreateRecordsAsync(Teacher(store), Note(ZoyaProfile), Now)).Status);
        Assert.Equal(403, (await service.StudentAsync(Teacher2(store), AliProfile, Now)).Status);

        async Task<List<Guid>> Sees(PastoralViewer v) => (await service.StudentAsync(v, AliProfile, Now)).Value!.Records.Select(r => r.Id).ToList();
        Assert.Equal([praise.Id], await Sees(Teacher(store)));
        Assert.Equal([praise.Id], await Sees(Head(store)));
        Assert.Equal([praise.Id], await Sees(Admin(store)));
        Assert.Equal(2, (await Sees(Counsellor(store))).Count);
        Assert.Contains(secret.Id, await Sees(Counsellor(store)));

        // An admin of the DHA campus looks after Zoya, not Ali.
        Assert.Equal(403, (await service.StudentAsync(DhaAdmin(store), AliProfile, Now)).Status);
        Assert.True((await service.StudentAsync(DhaAdmin(store), ZoyaProfile, Now)).Ok);

        var teacherPage = (await service.StudentAsync(Teacher(store), AliProfile, Now)).Value!;
        Assert.False(teacherPage.CanPlan);
        Assert.Null(teacherPage.CheckIns);
        Assert.True((await service.StudentAsync(Head(store), AliProfile, Now)).Value!.CanPlan);
    }

    [Fact]
    public async Task Students_and_parents_see_only_what_is_shared_with_them()
    {
        var (store, sent, service) = World();
        var forParents = await WriteAsync(service, store, Teacher(store), Note(AliProfile, summary: "Helped a new classmate", parents: true, category: "Helping others"));
        var forAli = await WriteAsync(service, store, Teacher(store), Note(AliProfile, summary: "Top marks in the test", student_: true));
        await WriteAsync(service, store, Teacher(store), Note(AliProfile, tone: "Concern", summary: "Talking in class"));

        Assert.Equal([forAli.Id], (await service.MineAsync(AliHimself())).Value!.Records.Select(r => r.Id));
        var mum = (await service.FamilyAsync(Mum())).Value!;
        Assert.Equal([forParents.Id], mum.Single(c => c.StudentProfileId == AliProfile).Records.Select(r => r.Id));
        Assert.Empty(mum.Single(c => c.StudentProfileId == ZoyaProfile).Records);
        Assert.Equal([forParents.Id], (await service.FamilyAsync(Dad())).Value!.Single().Records.Select(r => r.Id));
        Assert.Equal(403, (await service.StudentAsync(Mum(), AliProfile, Now)).Status);
        Assert.Equal(403, (await service.FamilyAsync(AliHimself())).Status);

        // Both parents were told about theirs, Ali about his; good news says so.
        Assert.Equal(new[] { MumId, DadId }.Order(), sent.Sent.Single(s => s.To.Contains(MumId)).To.Order());
        Assert.Contains(sent.Sent, s => s.Title == "Good news from school" && s.To.SequenceEqual([AliId]));
        Assert.Contains("Helping others", sent.Sent.Single(s => s.To.Contains(MumId)).Body);

        // A confidential note is never shared.
        var bad = await service.CreateRecordsAsync(Counsellor(store), Note(AliProfile, confidential: true, parents: true), Now);
        Assert.Equal(400, bad.Status);
        Assert.Equal("A confidential note cannot be shared with the student or the family.", bad.Error);
    }

    [Fact]
    public async Task Serious_concerns_and_runs_of_concerns_tell_the_wellbeing_team()
    {
        var (store, sent, service) = World();
        store.Add(new PastoralSettings(School));
        store.Settings.Single().Set(false, 3, 14);

        await WriteAsync(service, store, Teacher(store), Note(AliProfile, "Wellbeing", "Concern", "High", "Crying at break"));
        Assert.Equal(new[] { CounsellorId, HeadId }.Order(), sent.To("Serious concern").Order());
        Assert.DoesNotContain("Crying", sent.Sent.Single(s => s.Title == "Serious concern").Body);

        // Confidential: only the team members who see confidential notes.
        await WriteAsync(service, store, Teacher(store), Note(AliProfile, "Wellbeing", "Concern", "High", "Told me something at home", confidential: true));
        Assert.Equal([CounsellorId], sent.Sent.Where(s => s.Title == "Serious concern").Last().To);

        // The third behaviour or wellbeing concern within 14 days: the team is told once.
        Assert.Empty(sent.To("A run of concerns"));
        await WriteAsync(service, store, Teacher(store), Note(AliProfile, "Behaviour", "Concern", summary: "Pushed a classmate"));
        Assert.Equal(new[] { CounsellorId, HeadId }.Order(), sent.To("A run of concerns").Order());
        await WriteAsync(service, store, Teacher(store), Note(AliProfile, "Behaviour", "Concern", summary: "Late again"));
        Assert.Single(sent.Sent, s => s.Title == "A run of concerns");

        // Nobody on the team: the school's admins.
        var (store2, sent2, service2) = World(withTeam: false);
        await WriteAsync(service2, store2, Teacher2(store2), Note(ZoyaProfile, "Behaviour", "Concern", "High", "Fight in the playground"));
        Assert.Equal([AdminId], sent2.To("Serious concern"));
    }

    [Fact]
    public async Task Follow_ups_are_passed_on_added_to_finished_and_reminded()
    {
        var (store, sent, service) = World();
        var note = await WriteAsync(service, store, Teacher(store), Note(AliProfile, "Behaviour", "Concern", summary: "Upset after lunch", followUp: HeadId, by: Now.AddDays(1)));
        Assert.Equal([HeadId], sent.To("A note for you to follow up"));
        Assert.Equal(FollowUpStatus.Open, note.FollowUp);

        Assert.Equal(400, (await service.CreateRecordsAsync(Teacher(store), Note(AliProfile, followUp: Teacher2Id), Now)).Status);
        var conf = await service.CreateRecordsAsync(Teacher(store), Note(AliProfile, "Wellbeing", "Concern", confidential: true, followUp: HeadId), Now);
        Assert.Equal("A confidential note can only be followed up by someone who sees confidential notes.", conf.Error);
        Assert.True((await service.CreateRecordsAsync(Teacher(store), Note(AliProfile, "Wellbeing", "Concern", confidential: true, followUp: CounsellorId), Now)).Ok);

        var added = await service.AddNoteAsync(Head(store), note.Id, "Spoke with Ali; all fine now.", Now);
        Assert.True(added.Ok, added.Error);
        Assert.Contains(sent.Sent, s => s.Title == "Follow-up note" && s.To.SequenceEqual([TeacherId]));
        Assert.Equal("Spoke with Ali; all fine now.", added.Value!.Notes.Single().Text);
        Assert.Equal(403, (await service.AddNoteAsync(Staff(store, TeacherId), (await WriteAsync(service, store, Counsellor(store), Note(AliProfile))).Id, "x", Now)).Status);
        Assert.Equal("Done", (await service.SetFollowUpDoneAsync(Head(store), note.Id, true, Now)).Value!.FollowUp);
        Assert.Equal(400, (await service.SetFollowUpDoneAsync(Head(store), note.Id, true, Now)).Status);

        // Reminded once when the date passes.
        var late = await WriteAsync(service, store, Teacher(store), Note(AliProfile, "Behaviour", "Concern", summary: "Homework missing", followUp: HeadId, by: Now.AddDays(-1)));
        Assert.Equal(1, await service.RemindDueAsync(Now));
        Assert.Equal([HeadId], sent.To("Follow-up due"));
        Assert.Equal(0, await service.RemindDueAsync(Now.AddHours(1)));
        Assert.NotNull(late.FollowUpRemindedAt);
    }

    [Fact]
    public async Task Removed_notes_stay_for_the_team_with_the_reason_and_disappear_for_families()
    {
        var (store, _, service) = World();
        var note = await WriteAsync(service, store, Teacher(store), Note(AliProfile, parents: true));
        Assert.Equal(400, (await service.RemoveRecordAsync(Teacher(store), note.Id, " ", Now)).Status);
        Assert.Equal(404, (await service.RemoveRecordAsync(Teacher2(store), note.Id, "Wrong student", Now)).Status);
        var removed = await service.RemoveRecordAsync(Teacher(store), note.Id, "Written for the wrong student", Now);
        Assert.True(removed.Ok, removed.Error);

        Assert.Empty((await service.FamilyAsync(Mum())).Value!.Single(c => c.StudentProfileId == AliProfile).Records);
        var forHead = (await service.StudentAsync(Head(store), AliProfile, Now)).Value!.Records.Single();
        Assert.True(forHead.IsRemoved);
        Assert.Equal("Written for the wrong student", forHead.RemovedReason);
        Assert.False(forHead.CanManage);
        Assert.Equal(400, (await service.UpdateRecordAsync(Teacher(store), note.Id, Note(AliProfile), Now)).Status);
    }

    [Fact]
    public async Task A_note_for_a_whole_group_skips_students_the_writer_does_not_teach()
    {
        var (store, _, service) = World();
        var result = await service.CreateRecordsAsync(Teacher(store), Note(AliProfile) with { StudentProfileIds = [AliProfile, ZoyaProfile] }, Now);
        Assert.True(result.Ok, result.Error);
        Assert.Equal(1, result.Value!.Saved);
        Assert.Equal(["Zoya Raza: you do not teach or look after this student."], result.Value.Skipped);

        var many = Enumerable.Range(0, PastoralService.MaxStudentsPerNote + 1).Select(_ => Guid.NewGuid()).ToList();
        Assert.Equal(400, (await service.CreateRecordsAsync(Counsellor(store), Note(AliProfile) with { StudentProfileIds = many }, Now)).Status);

        // The counsellor finds students across the school; a teacher only their own.
        Assert.Equal(["Ali Raza"], (await service.FindStudentsAsync(Teacher(store), null, null)).Value!.Select(s => s.Name));
        Assert.Equal(2, (await service.FindStudentsAsync(Counsellor(store), "raza", null)).Value!.Count);
        Assert.Empty((await service.FindStudentsAsync(Counsellor(store), null, null)).Value!);
    }

    [Fact]
    public async Task Support_plans_are_written_by_the_team_and_shown_to_the_students_teachers()
    {
        var (store, sent, service) = World();
        var request = new SavePlanRequest(AliProfile, "LearningAdjustments", "Extra time in tests", "Reads slowly.", "Give 25% extra time; seat him at the front.",
            Now.Date.AddDays(-10), Now.Date.AddDays(-1), false, false, true);
        Assert.Equal(403, (await service.SavePlanAsync(Teacher(store), null, request, Now)).Status);
        var plan = await service.SavePlanAsync(Head(store), null, request, Now);
        Assert.Equal(201, plan.Status);
        Assert.Equal([TeacherId], sent.To("New support plan"));
        Assert.Equal(new[] { MumId, DadId }.Order(), sent.To("A support plan from school").Order());

        Assert.Equal("Give 25% extra time; seat him at the front.", (await service.StudentAsync(Teacher(store), AliProfile, Now)).Value!.Plans.Single().Strategies);
        Assert.Single((await service.FamilyAsync(Mum())).Value!.Single(c => c.StudentProfileId == AliProfile).Plans);
        Assert.Empty((await service.MineAsync(AliHimself())).Value!.Plans);

        var secret = await service.SavePlanAsync(Counsellor(store), null, request with { Title = "Counselling", Confidential = true, ShareWithParents = false }, Now);
        Assert.True(secret.Ok, secret.Error);
        Assert.Single((await service.StudentAsync(Teacher(store), AliProfile, Now)).Value!.Plans);
        Assert.Equal(2, (await service.StudentAsync(Counsellor(store), AliProfile, Now)).Value!.Plans.Count);
        Assert.Equal(400, (await service.SavePlanAsync(Counsellor(store), null, request with { Confidential = true }, Now)).Status);

        // The review date has come: the owner is reminded once.
        Assert.Equal(2, await service.RemindDueAsync(Now));
        Assert.Equal(new[] { HeadId, CounsellorId }.Order(), sent.To("Support plan to review").Order());
        Assert.Equal(0, await service.RemindDueAsync(Now));

        Assert.True((await service.SetPlanEndedAsync(Head(store), plan.Value!.Id, true, Now)).Ok);
        Assert.Empty((await service.FamilyAsync(Mum())).Value!.Single(c => c.StudentProfileId == AliProfile).Plans);
    }

    [Fact]
    public async Task Check_ins_need_the_school_to_switch_them_on_and_asking_to_talk_tells_the_team()
    {
        var (store, sent, service) = World();
        Assert.Equal(403, (await service.CheckInAsync(AliHimself(), new CheckInRequest(4, null, false), Now)).Status);
        Assert.Equal(403, (await service.SaveSettingsAsync(DhaAdmin(store), new SaveSettingsRequest(null, true, 3, 14))).Status);
        Assert.True((await service.SaveSettingsAsync(Admin(store), new SaveSettingsRequest(null, true, 3, 14))).Ok);
        Assert.Equal(400, (await service.SaveSettingsAsync(Admin(store), new SaveSettingsRequest(null, true, 1, 14))).Status);

        var first = await service.CheckInAsync(AliHimself(), new CheckInRequest(4, null, false), Now);
        Assert.Equal(201, first.Status);
        Assert.Empty(sent.Sent);
        Assert.Equal(400, (await service.CheckInAsync(AliHimself(), new CheckInRequest(0, null, false), Now)).Status);

        // Changed within six hours: the same check-in, and the team is told once.
        var changed = await service.CheckInAsync(AliHimself(), new CheckInRequest(2, "Worried about exams", true), Now.AddHours(1));
        Assert.Equal(200, changed.Status);
        Assert.Single(store.CheckIns);
        Assert.Equal(new[] { CounsellorId, HeadId }.Order(), sent.To("A student would like to talk").Order());
        await service.CheckInAsync(AliHimself(), new CheckInRequest(2, "Still worried", true), Now.AddHours(2));
        Assert.Single(sent.Sent);

        var week = new OverviewQuery(null, null, null, Now.AddDays(-7), Now.AddDays(1));
        var counsellor = (await service.OverviewAsync(Counsellor(store), week, Now)).Value!;
        Assert.Equal("Still worried", counsellor.NeedsAttention.Single().Note);
        Assert.True(counsellor.Students.Single(s => s.Student.Id == AliProfile).NeedsAttention);
        var teacher = (await service.OverviewAsync(Teacher(store), week, Now)).Value!;
        Assert.False(teacher.Me.SeesCheckIns);
        Assert.Empty(teacher.NeedsAttention);

        Assert.Equal(404, (await service.AcknowledgeCheckInAsync(Teacher(store), store.CheckIns.Single().Id, null, Now)).Status);
        Assert.True((await service.AcknowledgeCheckInAsync(Counsellor(store), store.CheckIns.Single().Id, "Met at lunch.", Now.AddHours(3))).Ok);
        Assert.Empty((await service.OverviewAsync(Counsellor(store), week, Now)).Value!.NeedsAttention);
        var mine = (await service.MineAsync(AliHimself())).Value!;
        Assert.True(mine.CheckInsEnabled);
        Assert.Equal("Mrs Counsellor", mine.Latest!.AcknowledgedBy);
        Assert.Null(mine.Latest.AcknowledgeNote);
        Assert.Equal(403, (await service.CheckInAsync(Teacher(store), new CheckInRequest(3, null, false), Now)).Status);
    }

    [Fact]
    public async Task The_overview_counts_what_each_person_may_see()
    {
        var (store, _, service) = World();
        await WriteAsync(service, store, Teacher(store), Note(AliProfile, category: "Excellent effort"));
        await WriteAsync(service, store, Counsellor(store), Note(AliProfile, "Wellbeing", "Concern", confidential: true));
        await WriteAsync(service, store, Teacher2(store), Note(ZoyaProfile, category: "Excellent effort"));
        await WriteAsync(service, store, Teacher(store), Note(AliProfile, summary: "Last month", at: Now.AddDays(-40)));

        var month = new OverviewQuery(null, null, null, Now.AddDays(-30), Now.AddMinutes(1));
        var teacher = (await service.OverviewAsync(Teacher(store), month, Now)).Value!;
        Assert.Equal(["Ali Raza"], teacher.Students.Select(s => s.Student.Name));
        Assert.Equal((1, 0), (teacher.Totals.Positive, teacher.Totals.Concerns));

        var counsellor = (await service.OverviewAsync(Counsellor(store), month, Now)).Value!;
        Assert.Equal((2, 1), (counsellor.Totals.Positive, counsellor.Totals.Concerns));
        Assert.Equal("Ali Raza", counsellor.Students.First().Student.Name);
        Assert.Equal(2, counsellor.Categories.Single(c => c.Category == "Excellent effort").Count);
        Assert.True(counsellor.Me.SeesConfidential);

        var dha = (await service.OverviewAsync(Counsellor(store), month with { CampusId = Dha }, Now)).Value!;
        Assert.Equal(["Zoya Raza"], dha.Students.Select(s => s.Student.Name));
        Assert.Equal(403, (await service.OverviewAsync(Mum(), month, Now)).Status);
        Assert.Equal(400, (await service.OverviewAsync(Counsellor(store), month with { From = Now.AddDays(-500) }, Now)).Status);
    }

    [Fact]
    public async Task Admins_set_up_the_team_and_categories_and_campus_admins_only_for_their_campuses()
    {
        var (store, sent, service) = World(withTeam: false);
        Assert.Equal(403, (await service.SetupAsync(Teacher(store), null)).Status);

        var added = await service.AddTeamMemberAsync(Admin(store), new AddTeamMemberRequest(null, TeacherId, "Head of Class 7", Gulberg, Class7, false));
        Assert.Equal(201, added.Status);
        Assert.Equal([TeacherId], sent.To("You are on the wellbeing team"));
        Assert.Equal(400, (await service.AddTeamMemberAsync(Admin(store), new AddTeamMemberRequest(null, TeacherId, "Again", Gulberg, Class7, false))).Status);
        Assert.Equal(400, (await service.AddTeamMemberAsync(Admin(store), new AddTeamMemberRequest(null, AliId, "Student", null, null, false))).Status);
        Assert.Equal(400, (await service.AddTeamMemberAsync(DhaAdmin(store), new AddTeamMemberRequest(null, Teacher2Id, "Counsellor", Gulberg, null, true))).Status);
        Assert.Equal(400, (await service.AddTeamMemberAsync(DhaAdmin(store), new AddTeamMemberRequest(null, Teacher2Id, "Counsellor", null, null, true))).Status);
        Assert.Equal(201, (await service.AddTeamMemberAsync(DhaAdmin(store), new AddTeamMemberRequest(null, Teacher2Id, "Counsellor", Dha, null, true))).Status);
        Assert.Equal(403, (await service.RemoveTeamMemberAsync(DhaAdmin(store), added.Value!.Id)).Status);

        var setup = (await service.SetupAsync(Admin(store), null)).Value!;
        Assert.Equal(2, setup.Team.Count);
        Assert.Equal(PastoralCategory.Defaults.Count, setup.Categories.Count);
        Assert.All(setup.Categories, c => Assert.Equal(Guid.Empty, c.Id));

        // The first change keeps the starting list: hiding one, then adding one.
        Assert.False((await service.SaveCategoryAsync(Admin(store), Guid.Empty, new SaveCategoryRequest(null, "Behaviour", "Concern", "Uniform", false))).Value!.IsActive);
        Assert.Equal(PastoralCategory.Defaults.Count, store.Categories.Count);
        Assert.True((await service.SaveCategoryAsync(Admin(store), null, new SaveCategoryRequest(null, "Behaviour", "Positive", "House point", true))).Ok);
        var categories = (await service.SetupAsync(Admin(store), null)).Value!.Categories;
        Assert.Equal(PastoralCategory.Defaults.Count + 1, categories.Count);
        Assert.False(categories.Single(c => c.Name == "Uniform").IsActive);
        Assert.Equal(403, (await service.SaveCategoryAsync(DhaAdmin(store), null, new SaveCategoryRequest(null, "Behaviour", "Positive", "Star", true))).Status);
        Assert.DoesNotContain((await service.CategoriesAsync(Teacher(store))).Value!, c => c.Name == "Uniform");

        Assert.True((await service.RemoveTeamMemberAsync(Admin(store), added.Value.Id)).Ok);
        Assert.Single(store.Team);
    }
}
