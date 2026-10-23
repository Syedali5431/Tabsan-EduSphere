using Tabsan.EduSphere.Application.Assignments;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>Plan M8 — late deductions, missing grades, availability, overrides and peer-review allocation.</summary>
public class AssignmentEnhancementTests
{
    private static readonly DateTime Due = new(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);

    private static LatePolicy Policy(decimal percent, LateInterval interval, decimal floor = 0, bool missing = false, decimal missingPercent = 0)
    {
        var p = new LatePolicy(Guid.NewGuid());
        p.Configure(true, percent, interval, floor, missing, missingPercent);
        return p;
    }

    // ── M8.1 late deduction ──

    [Theory]
    [InlineData(0, 0)]          // on time
    [InlineData(1, 10)]         // 1 minute late = 1 started day
    [InlineData(60 * 24, 10)]   // exactly a day
    [InlineData(60 * 25, 20)]   // 25 hours = 2 started days
    [InlineData(60 * 24 * 30, 80)] // capped by the raw mark (100% deduction, floor 0) — raw 80
    public void DailyDeduction_CountsStartedDays(int minutesLate, double expected)
        => Assert.Equal((decimal)expected, Policy(10, LateInterval.Day).PenaltyFor(Due, Due.AddMinutes(minutesLate), 100, 80));

    [Fact]
    public void HourlyDeduction_And_Floor()
    {
        var hourly = Policy(5, LateInterval.Hour, floor: 50);
        Assert.Equal(15m, hourly.PenaltyFor(Due, Due.AddMinutes(150), 100, 90));   // 3 started hours × 5%
        Assert.Equal(40m, hourly.PenaltyFor(Due, Due.AddDays(3), 100, 90));        // never below 50% of the points
        Assert.Equal(0m, hourly.PenaltyFor(Due, Due.AddDays(3), 100, 45));         // already below the floor: not reduced
    }

    [Fact]
    public void DisabledPolicy_DeductsNothing_And_ValidatesInput()
    {
        var p = new LatePolicy(Guid.NewGuid());
        p.Configure(false, 0, LateInterval.Day, 0, true, 0);
        Assert.Equal(0m, p.PenaltyFor(Due, Due.AddDays(5), 100, 100));
        Assert.Throws<ArgumentException>(() => p.Configure(true, 0, LateInterval.Day, 0, false, 0));
        Assert.Throws<ArgumentException>(() => p.Configure(true, 10, LateInterval.Day, 120, false, 0));
        Assert.Throws<ArgumentException>(() => p.Configure(false, 0, LateInterval.Day, 0, true, -1));
    }

    [Fact]
    public void Grade_AppliesPenalty_KeepsRawMark_AndCanBeWaived()
    {
        var s = AssignmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid());
        s.AddAttempt("work", null, Due.AddHours(5), isLate: true);
        s.Grade(80, "ok", Guid.NewGuid(), postNow: true, latePenalty: 10);
        Assert.Equal(80m, s.RawMarks);
        Assert.Equal(10m, s.LatePenalty);
        Assert.Equal(70m, s.MarksAwarded);

        s.SetLatePenaltyWaived(true, 10);
        Assert.Equal(0m, s.LatePenalty);
        Assert.Equal(80m, s.MarksAwarded);

        // A waived submission keeps no deduction on regrading; restoring the deduction applies it again.
        s.Grade(60, null, Guid.NewGuid(), postNow: true, latePenalty: 10);
        Assert.Equal(60m, s.MarksAwarded);
        s.SetLatePenaltyWaived(false, 10);
        Assert.Equal(50m, s.MarksAwarded);

        // The deduction never exceeds the mark.
        s.SetLatePenaltyWaived(false, 0);
        s.Grade(4, null, Guid.NewGuid(), postNow: true, latePenalty: 10);
        Assert.Equal(0m, s.MarksAwarded);
        Assert.Equal(4m, s.LatePenalty);
    }

    // ── M8.1 missing grades ──

    [Fact]
    public void MissingGrade_OnlyWithoutWork_AndRealWorkReplacesIt()
    {
        var s = AssignmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid());
        Assert.True(s.MarkMissing(0, Due.AddDays(1), postNow: true));
        Assert.True(s.IsMissing);
        Assert.Equal(0m, s.MarksAwarded);
        Assert.Equal(SubmissionStatus.Graded, s.Status);
        Assert.NotNull(s.PostedAt);
        Assert.False(s.MarkMissing(0, Due.AddDays(2), postNow: true)); // unchanged: no second update

        s.AddAttempt("late work", null, Due.AddDays(3), isLate: true);
        Assert.False(s.IsMissing);
        Assert.Null(s.MarksAwarded);
        Assert.Null(s.PostedAt);
        Assert.Equal(SubmissionStatus.Submitted, s.Status);
        Assert.False(s.MarkMissing(0, Due.AddDays(4), postNow: true)); // work handed in: never "missing" again
        Assert.Equal(25m, Policy(10, LateInterval.Day, missing: true, missingPercent: 25).MissingMarks(100));
    }

    // ── M8.2 availability and overrides ──

    [Fact]
    public void Availability_And_PeerReviewOptions_AreValidated()
    {
        var a = new Assignment(Guid.NewGuid(), "Essay", null, Due, 100);
        Assert.Equal(Due, a.ClosesAt);
        a.ConfigureAvailability(Due.AddDays(-7), Due.AddDays(2));
        Assert.Equal(Due.AddDays(2), a.ClosesAt);
        Assert.Throws<ArgumentException>(() => a.ConfigureAvailability(Due.AddDays(1), null));
        Assert.Throws<ArgumentException>(() => a.ConfigureAvailability(null, Due.AddDays(-1)));
        Assert.Throws<ArgumentException>(() => a.ConfigurePeerReview(6, null, true));
        Assert.Throws<ArgumentException>(() => a.ConfigurePeerReview(2, Due.AddDays(-1), true));
        a.ConfigurePeerReview(2, Due.AddDays(3), anonymous: false);
        Assert.Equal(2, a.PeerReviewCount);
        Assert.False(a.PeerReviewAnonymous);
    }

    [Fact]
    public void Override_IsForOneStudentOrOneGroup()
    {
        Assert.Throws<ArgumentException>(() => new AssignmentOverride(Guid.NewGuid(), null, null, Due, null, null, null));
        Assert.Throws<ArgumentException>(() => new AssignmentOverride(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Due, null, null, null));
        Assert.Throws<ArgumentException>(() => new AssignmentOverride(Guid.NewGuid(), Guid.NewGuid(), null, Due, null, Due.AddDays(-1), null));
        var o = new AssignmentOverride(Guid.NewGuid(), Guid.NewGuid(), null, Due, null, null, new string('x', 400));
        Assert.Equal(300, o.Note!.Length);
    }

    [Fact]
    public async Task EffectiveDates_StudentOverrideBeatsGroup_LatestGroupWins()
    {
        var offering = Guid.NewGuid();
        var a = new Assignment(offering, "Lab", null, Due, 10);
        var (ann, ben, cara) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var (g1, g2, set) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var repo = new FakePolicyRepo();
        repo.Members.AddRange([new StudentGroupMember(g1, set, ann), new StudentGroupMember(g1, set, ben), new StudentGroupMember(g2, Guid.NewGuid(), ben)]);
        repo.Overrides.AddRange([
            new AssignmentOverride(a.Id, null, g1, Due.AddDays(2), null, null, null),
            new AssignmentOverride(a.Id, null, g2, Due.AddDays(4), null, Due.AddDays(5), null),
            new AssignmentOverride(a.Id, ann, null, Due.AddDays(1), null, null, "illness")]);
        var service = new AssignmentPolicyService(repo, null!, null!);

        var annDates = await service.DatesForAsync(a, ann);
        Assert.Equal((Due.AddDays(1), "student"), (annDates.DueDate, annDates.Source));
        var benDates = await service.DatesForAsync(a, ben);
        Assert.Equal((Due.AddDays(4), Due.AddDays(5), "group"), (benDates.DueDate, benDates.ClosesAt, benDates.Source));
        var caraDates = await service.DatesForAsync(a, cara);
        Assert.Equal((Due, "assignment"), (caraDates.DueDate, caraDates.Source));
    }

    // ── M8.4 peer review ──

    [Fact]
    public void Allocator_SpreadsReviewsEvenly_NeverOwnOrGroupWork()
    {
        var students = Enumerable.Range(0, 9).Select(_ => Guid.NewGuid()).ToList();
        // Three groups of three: group work is one piece per group.
        var groups = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList();
        var work = groups.Select(g => new PeerWork(Guid.NewGuid(), g)).ToList();
        var reviewers = students.Select((s, i) => new PeerReviewer(s, groups[i / 3])).ToList();

        var pairs = PeerReviewAllocator.Allocate(work, reviewers, 2, new Random(42));
        Assert.Equal(18, pairs.Count);
        var ownerOf = work.ToDictionary(w => w.SubmissionId, w => w.OwnerKey);
        var groupOf = reviewers.ToDictionary(r => r.StudentProfileId, r => r.OwnerKey);
        Assert.All(pairs, p => Assert.NotEqual(groupOf[p.ReviewerId], ownerOf[p.SubmissionId]));
        Assert.All(pairs.GroupBy(p => p.SubmissionId), g => Assert.Equal(6, g.Count()));
        Assert.Equal(pairs.Count, pairs.Distinct().Count());
    }

    [Fact]
    public void Allocator_IndividualWork_IsBalanced_AndCountIsCappedByAvailableWork()
    {
        var students = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        var work = students.Select(s => new PeerWork(Guid.NewGuid(), s)).ToList();
        var reviewers = students.Select(s => new PeerReviewer(s, s)).ToList();
        var pairs = PeerReviewAllocator.Allocate(work, reviewers, 3, new Random(7));
        var loads = pairs.GroupBy(p => p.SubmissionId).Select(g => g.Count()).ToList();
        Assert.Equal(15, pairs.Count);
        Assert.True(loads.Max() - loads.Min() <= 1);

        // Two students can each review only one piece, even when asked for three.
        var two = students.Take(2).ToList();
        Assert.Equal(2, PeerReviewAllocator.Allocate(two.Select(s => new PeerWork(Guid.NewGuid(), s)).ToList(),
            two.Select(s => new PeerReviewer(s, s)).ToList(), 3, new Random(1)).Count);
    }

    [Fact]
    public void PeerReview_Complete_NeedsContent()
    {
        var r = new PeerReview(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Assert.Throws<ArgumentException>(() => r.Complete(null, "  ", null, Due));
        Assert.Throws<ArgumentException>(() => r.Complete(-1, "x", null, Due));
        r.Complete(7.5m, " Clear argument ", null, Due);
        Assert.Equal(PeerReviewStatus.Completed, r.Status);
        Assert.Equal("Clear argument", r.Comment);
        r.Complete(8, "Edited", null, Due.AddDays(1));
        Assert.Equal(Due, r.CompletedAt); // first completion time is kept
    }

    [Fact]
    public void GroupSet_ValidatesNameAndSize()
    {
        Assert.Throws<ArgumentException>(() => new GroupSet(Guid.NewGuid(), " ", false, null));
        Assert.Throws<ArgumentException>(() => new GroupSet(Guid.NewGuid(), "Teams", false, 0));
        var set = new GroupSet(Guid.NewGuid(), " Teams ", true, 4);
        Assert.Equal("Teams", set.Name);
    }

    /// <summary>Only what the date resolution reads.</summary>
    private sealed class FakePolicyRepo : IAssignmentPolicyRepository
    {
        public List<AssignmentOverride> Overrides { get; } = [];
        public List<StudentGroupMember> Members { get; } = [];

        public Task<IReadOnlyList<AssignmentOverride>> GetOverridesAsync(Guid assignmentId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AssignmentOverride>>(Overrides.Where(o => o.AssignmentId == assignmentId).ToList());
        public Task<IReadOnlyList<StudentGroupMember>> GetMembershipsAsync(Guid studentProfileId, Guid offeringId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<StudentGroupMember>>(Members.Where(m => m.StudentProfileId == studentProfileId).ToList());

        public Task<LatePolicy?> GetLatePolicyAsync(Guid offeringId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<LatePolicy>> GetMissingPoliciesAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public void Add(LatePolicy policy) => throw new NotSupportedException();
        public Task<IReadOnlyList<Assignment>> GetGradableAssignmentsAsync(Guid offeringId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Assignment>> GetAssignmentsAwaitingPeerAllocationAsync(DateTime now, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Guid>> GetEnrolledStudentIdsAsync(Guid offeringId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<AssignmentOverride?> GetOverrideAsync(Guid overrideId, CancellationToken ct = default) => throw new NotSupportedException();
        public void Add(AssignmentOverride item) => throw new NotSupportedException();
        public void Remove(AssignmentOverride item) => throw new NotSupportedException();
        public Task<IReadOnlyList<GroupSet>> GetGroupSetsAsync(Guid offeringId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<GroupSet?> GetGroupSetAsync(Guid groupSetId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<StudentGroup>> GetGroupsAsync(IReadOnlyCollection<Guid> groupSetIds, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<StudentGroup?> GetGroupAsync(Guid groupId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<StudentGroupMember>> GetMembersAsync(IReadOnlyCollection<Guid> groupSetIds, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> GroupHasSubmissionsAsync(Guid groupId, CancellationToken ct = default) => throw new NotSupportedException();
        public void Add(GroupSet set) => throw new NotSupportedException();
        public void Add(StudentGroup group) => throw new NotSupportedException();
        public void Add(StudentGroupMember member) => throw new NotSupportedException();
        public void Remove(GroupSet set) => throw new NotSupportedException();
        public void Remove(StudentGroup group) => throw new NotSupportedException();
        public void Remove(StudentGroupMember member) => throw new NotSupportedException();
        public Task<IReadOnlyList<PeerReview>> GetPeerReviewsAsync(Guid assignmentId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<PeerReview?> GetPeerReviewAsync(Guid peerReviewId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> IsReviewerOfAsync(Guid submissionId, Guid reviewerStudentProfileId, CancellationToken ct = default) => throw new NotSupportedException();
        public void Add(PeerReview review) => throw new NotSupportedException();
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }
}
