using System.Reflection;
using FluentAssertions;
using Tabsan.EduSphere.Application.Assignments;
using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Quizzes;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Quizzes;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>
/// Plan M2 acceptance: finished quiz scores feed the gradebook's "Quiz" component —
/// calculation rules, the hand-entry ownership rule, and the sync service wiring.
/// </summary>
public class QuizGradebookSyncTests
{
    private static readonly Guid QuizA = Guid.NewGuid();
    private static readonly Guid QuizB = Guid.NewGuid();

    // ── Calculator ────────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_TakesBestAttemptPerQuiz_AndScalesToComponentWeight()
    {
        var score = QuizGradebookCalculator.Calculate(
        [
            new QuizAttemptScore(QuizA, 4, 10),
            new QuizAttemptScore(QuizA, 8, 10),
            new QuizAttemptScore(QuizA, 6, 10)
        ], componentMax: 20);

        score.Should().Be(new QuizComponentScore(16m, 20m, 1));
    }

    [Fact]
    public void Calculate_CombinesQuizzesByPoints()
    {
        // 8/10 and 15/30 => 23/40 = 57.5% of a 20-mark component = 11.5
        var score = QuizGradebookCalculator.Calculate(
        [
            new QuizAttemptScore(QuizA, 8, 10),
            new QuizAttemptScore(QuizB, 15, 30)
        ], componentMax: 20);

        score!.MarksObtained.Should().Be(11.5m);
        score.MaxMarks.Should().Be(20m);
        score.QuizzesCounted.Should().Be(2);
    }

    [Fact]
    public void Calculate_PendingAttemptsNeverPostAPartialScore()
    {
        QuizGradebookCalculator.Calculate([new QuizAttemptScore(QuizA, null, 10)], 20).Should().BeNull();

        // A graded earlier attempt still counts while a later one waits for essay marks.
        var score = QuizGradebookCalculator.Calculate(
        [
            new QuizAttemptScore(QuizA, 5, 10),
            new QuizAttemptScore(QuizA, null, 10)
        ], 10);
        score!.MarksObtained.Should().Be(5m);
    }

    [Fact]
    public void Calculate_IgnoresZeroMarkQuizzes_AndRoundsToTwoPlaces()
    {
        QuizGradebookCalculator.Calculate([new QuizAttemptScore(QuizA, 0, 0)], 20).Should().BeNull();

        var score = QuizGradebookCalculator.Calculate(
        [
            new QuizAttemptScore(QuizA, 1, 3),
            new QuizAttemptScore(QuizB, 0, 0)
        ], 10);
        score!.MarksObtained.Should().Be(3.33m);
        score.QuizzesCounted.Should().Be(1);
    }

    [Fact]
    public void FindComponent_PrefersQuiz_ThenQuizzes_CaseInsensitive()
    {
        var mid = new ResultComponentRule("Midterm", 30, 1);
        var quizzes = new ResultComponentRule("QUIZZES", 20, 2);
        var quiz = new ResultComponentRule("quiz", 10, 3);

        QuizGradebookCalculator.FindComponent([mid, quizzes, quiz]).Should().BeSameAs(quiz);
        QuizGradebookCalculator.FindComponent([mid, quizzes]).Should().BeSameAs(quizzes);
        QuizGradebookCalculator.FindComponent([mid]).Should().BeNull();
    }

    [Fact]
    public void ToScore_UsesItemSnapshotMarks_AndNullWhileUngraded()
    {
        var graded = FinishedAttempt(QuizA, score: 7, itemMarks: [5, 5]);
        QuizGradebookCalculator.ToScore(graded).Should().Be(new QuizAttemptScore(QuizA, 7, 10));

        var pending = FinishedAttempt(QuizA, score: null, itemMarks: [5, 5]);
        QuizGradebookCalculator.ToScore(pending).Score.Should().BeNull();
    }

    // ── Result ownership rule ─────────────────────────────────────────────────

    [Fact]
    public void Result_SystemRowsSync_ManualAndPublishedRowsDoNot()
    {
        var synced = Result.CreateSystemManaged(Guid.NewGuid(), Guid.NewGuid(), "Quiz", 5, 10, ResultSource.QuizSync);
        synced.Source.Should().Be(ResultSource.QuizSync);
        synced.ApplySystemSync(7, 10);
        synced.MarksObtained.Should().Be(7);

        var manual = new Result(Guid.NewGuid(), Guid.NewGuid(), "Quiz", 5, 10);
        manual.Invoking(r => r.ApplySystemSync(7, 10)).Should().Throw<InvalidOperationException>();

        synced.Publish(Guid.NewGuid());
        synced.Invoking(r => r.ApplySystemSync(9, 10)).Should().Throw<InvalidOperationException>();

        FluentActions.Invoking(() => Result.CreateSystemManaged(Guid.NewGuid(), Guid.NewGuid(), "Quiz", 5, 10, ResultSource.Manual))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Result_HandEditsTakeOwnershipFromTheSync()
    {
        var draft = Result.CreateSystemManaged(Guid.NewGuid(), Guid.NewGuid(), "Quiz", 5, 10, ResultSource.QuizSync);
        draft.ReviseDraft(6, 10);
        draft.Source.Should().Be(ResultSource.Manual);

        var published = Result.CreateSystemManaged(Guid.NewGuid(), Guid.NewGuid(), "Quiz", 5, 10, ResultSource.QuizSync);
        published.Publish(Guid.NewGuid());
        published.CorrectMarks(8, 10);
        published.Source.Should().Be(ResultSource.Manual);
    }

    // ── ResultService.SyncSystemComponentAsync ────────────────────────────────

    [Fact]
    public async Task Sync_CreatesQuizRow_UsingConfiguredSpelling_AndRecalculatesTotal()
    {
        var (student, offering) = (Guid.NewGuid(), Guid.NewGuid());
        var repo = new FakeResultRepository([new ResultComponentRule("Quiz", 20, 1), new ResultComponentRule("Final", 80, 2)]);
        var sut = new ResultService(repo, new NullAudit());

        var outcome = await sut.SyncSystemComponentAsync(student, offering, "quiz", 15, 20, ResultSource.QuizSync);

        outcome.Should().Be(SystemResultSyncOutcome.Created);
        var row = repo.Results.Single(r => r.ResultType == "Quiz");
        row.Source.Should().Be(ResultSource.QuizSync);
        row.MarksObtained.Should().Be(15);
        row.IsPublished.Should().BeFalse();
        var total = repo.Results.Single(r => r.ResultType == "Total");
        total.MarksObtained.Should().Be(15);
        total.MaxMarks.Should().Be(20);

        (await sut.SyncSystemComponentAsync(student, offering, "Quiz", 15, 20, ResultSource.QuizSync))
            .Should().Be(SystemResultSyncOutcome.Unchanged);
        (await sut.SyncSystemComponentAsync(student, offering, "Quiz", 18, 20, ResultSource.QuizSync))
            .Should().Be(SystemResultSyncOutcome.Updated);
        row.MarksObtained.Should().Be(18);
        total.MarksObtained.Should().Be(18);
    }

    [Fact]
    public async Task Sync_NeverOverwritesHandEnteredOrPublishedRows()
    {
        var (student, offering) = (Guid.NewGuid(), Guid.NewGuid());
        var manual = new Result(student, offering, "Quiz", 12, 20);
        var repo = new FakeResultRepository([new ResultComponentRule("Quiz", 20, 1)], manual);
        var sut = new ResultService(repo, new NullAudit());

        (await sut.SyncSystemComponentAsync(student, offering, "Quiz", 19, 20, ResultSource.QuizSync))
            .Should().Be(SystemResultSyncOutcome.SkippedManual);
        manual.MarksObtained.Should().Be(12);

        var other = Guid.NewGuid();
        var published = Result.CreateSystemManaged(other, offering, "Quiz", 10, 20, ResultSource.QuizSync);
        published.Publish(Guid.NewGuid());
        repo.Results.Add(published);
        (await sut.SyncSystemComponentAsync(other, offering, "Quiz", 19, 20, ResultSource.QuizSync))
            .Should().Be(SystemResultSyncOutcome.SkippedPublished);
        published.MarksObtained.Should().Be(10);
    }

    [Fact]
    public async Task Sync_SkipsWhenComponentIsNotConfigured_AndRefusesTotal()
    {
        var repo = new FakeResultRepository([new ResultComponentRule("Final", 100, 1)]);
        var sut = new ResultService(repo, new NullAudit());

        (await sut.SyncSystemComponentAsync(Guid.NewGuid(), Guid.NewGuid(), "Quiz", 5, 10, ResultSource.QuizSync))
            .Should().Be(SystemResultSyncOutcome.SkippedNotConfigured);
        repo.Results.Should().BeEmpty();

        await sut.Invoking(s => s.SyncSystemComponentAsync(Guid.NewGuid(), Guid.NewGuid(), "Total", 5, 10, ResultSource.QuizSync))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GradebookCellEdit_OnSyncedDraft_TakesOwnership()
    {
        var (student, offering) = (Guid.NewGuid(), Guid.NewGuid());
        var synced = Result.CreateSystemManaged(student, offering, "Quiz", 10, 20, ResultSource.QuizSync);
        var repo = new FakeResultRepository([new ResultComponentRule("Quiz", 20, 1)], synced);
        var gradebook = new GradebookService(Proxy<IGradebookRepository>.Create(), repo);

        await gradebook.UpsertEntryAsync(new UpsertGradebookEntryRequest
        {
            StudentProfileId = student, CourseOfferingId = offering, ComponentName = "Quiz", MarksObtained = 14, MaxMarks = 20
        }, Guid.NewGuid());

        synced.MarksObtained.Should().Be(14);
        synced.Source.Should().Be(ResultSource.Manual);
        (await new ResultService(repo, new NullAudit()).SyncSystemComponentAsync(student, offering, "Quiz", 20, 20, ResultSource.QuizSync))
            .Should().Be(SystemResultSyncOutcome.SkippedManual);
    }

    // ── QuizGradebookSync service ─────────────────────────────────────────────

    [Fact]
    public async Task Refresh_PostsBestScoredAttempts_ThroughResultService()
    {
        var (student, offering) = (Guid.NewGuid(), Guid.NewGuid());
        var attempts = new List<QuizAttempt>
        {
            FinishedAttempt(QuizA, score: 6, itemMarks: [5, 5]),
            FinishedAttempt(QuizA, score: 9, itemMarks: [5, 5]),
            FinishedAttempt(QuizB, score: null, itemMarks: [10]) // essay pending: not counted
        };
        var repo = new FakeResultRepository([new ResultComponentRule("Quiz", 25, 1)]);
        var sync = new QuizGradebookSync(QuizRepo(attempts), repo, new ResultService(repo, new NullAudit()));

        var outcome = await sync.RefreshAsync(offering, student);

        outcome.Should().Be(SystemResultSyncOutcome.Created);
        var row = repo.Results.Single(r => r.ResultType == "Quiz");
        row.MarksObtained.Should().Be(22.5m); // 9/10 of 25
        row.MaxMarks.Should().Be(25m);
    }

    [Fact]
    public async Task Refresh_NothingGraded_PostsNothing_AndNoComponent_IsReported()
    {
        var pendingOnly = new List<QuizAttempt> { FinishedAttempt(QuizA, score: null, itemMarks: [10]) };
        var repo = new FakeResultRepository([new ResultComponentRule("Quiz", 25, 1)]);
        var sync = new QuizGradebookSync(QuizRepo(pendingOnly), repo, new ResultService(repo, new NullAudit()));
        (await sync.RefreshAsync(Guid.NewGuid(), Guid.NewGuid())).Should().BeNull();
        repo.Results.Should().BeEmpty();

        var noQuizComponent = new FakeResultRepository([new ResultComponentRule("Final", 100, 1)]);
        sync = new QuizGradebookSync(QuizRepo(pendingOnly), noQuizComponent, new ResultService(noQuizComponent, new NullAudit()));
        (await sync.RefreshAsync(Guid.NewGuid(), Guid.NewGuid())).Should().Be(SystemResultSyncOutcome.SkippedNotConfigured);
    }

    [Fact]
    public async Task Refresh_SwallowsFailures_SoTheQuizActionStillSucceeds()
    {
        var failing = Proxy<IQuizRepository>.Create(); // every call throws NotSupportedException
        var repo = new FakeResultRepository([new ResultComponentRule("Quiz", 25, 1)]);
        var sync = new QuizGradebookSync(failing, repo, new ResultService(repo, new NullAudit()));

        (await sync.RefreshAsync(Guid.NewGuid(), Guid.NewGuid())).Should().BeNull();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static QuizAttempt FinishedAttempt(Guid quizId, decimal? score, decimal[] itemMarks)
    {
        var attempt = new QuizAttempt(quizId, Guid.NewGuid());
        var items = (ICollection<QuizAttemptItem>)attempt.Items;
        for (var i = 0; i < itemMarks.Length; i++)
            items.Add(new QuizAttemptItem(attempt.Id, Guid.NewGuid(), i + 1, itemMarks[i], null, null, null));
        attempt.Submit();
        if (score.HasValue) attempt.RecordScore(score.Value);
        return attempt;
    }

    private static IQuizRepository QuizRepo(IReadOnlyList<QuizAttempt> attempts)
        => Proxy<IQuizRepository>.Create(new()
        {
            [nameof(IQuizRepository.GetFinishedAttemptsForGradebookAsync)] = _ => Task.FromResult(attempts)
        });

    /// <summary>Interface fake: configured methods by name, everything else throws.</summary>
    public class Proxy<T> : DispatchProxy where T : class
    {
        private Dictionary<string, Func<object?[]?, object?>> _handlers = new();

        public static T Create(Dictionary<string, Func<object?[]?, object?>>? handlers = null)
        {
            var proxy = DispatchProxy.Create<T, Proxy<T>>();
            ((Proxy<T>)(object)proxy)._handlers = handlers ?? new();
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => _handlers.TryGetValue(targetMethod!.Name, out var handler)
                ? handler(args)
                : throw new NotSupportedException(targetMethod.Name);
    }

    private sealed class NullAudit : IAuditService
    {
        public Task LogAsync(AuditLog entry, CancellationToken ct = default) => Task.CompletedTask;

        public Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(string? query = null, Guid? actorUserId = null,
            string? action = null, string? entityName = null, DateTime? fromUtc = null, DateTime? toUtc = null, int page = 1,
            int pageSize = 50, string? actorRole = null, string? severity = null, string? eventCategory = null,
            string? correlationId = null, CancellationToken ct = default)
            => Task.FromResult(((IReadOnlyList<AuditLog>)[], 0));
    }

    /// <summary>In-memory result store with configurable component rules (University, no GPA scale).</summary>
    private sealed class FakeResultRepository : IResultRepository
    {
        private readonly IReadOnlyList<ResultComponentRule> _rules;
        public List<Result> Results { get; }

        public FakeResultRepository(IReadOnlyList<ResultComponentRule> rules, params Result[] seed)
        {
            _rules = rules;
            Results = seed.ToList();
        }

        private IReadOnlyList<Result> Where(Func<Result, bool> predicate) => Results.Where(predicate).ToList();

        public Task<Result?> GetAsync(Guid studentProfileId, Guid courseOfferingId, string resultType, CancellationToken ct = default)
            => Task.FromResult(Results.FirstOrDefault(r => r.StudentProfileId == studentProfileId && r.CourseOfferingId == courseOfferingId
                && string.Equals(r.ResultType, resultType, StringComparison.OrdinalIgnoreCase)));
        public Task<IReadOnlyList<Result>> GetByStudentAsync(Guid studentProfileId, CancellationToken ct = default)
            => Task.FromResult(Where(r => r.StudentProfileId == studentProfileId));
        public Task<IReadOnlyList<Result>> GetPublishedByStudentAsync(Guid studentProfileId, CancellationToken ct = default)
            => Task.FromResult(Where(r => r.StudentProfileId == studentProfileId && r.IsPublished));
        public Task<IReadOnlyList<Result>> GetByOfferingAsync(Guid courseOfferingId, CancellationToken ct = default)
            => Task.FromResult(Where(r => r.CourseOfferingId == courseOfferingId));
        public Task<IReadOnlyList<Result>> GetByStudentAndOfferingAsync(Guid studentProfileId, Guid courseOfferingId, CancellationToken ct = default)
            => Task.FromResult(Where(r => r.StudentProfileId == studentProfileId && r.CourseOfferingId == courseOfferingId));
        public Task<IReadOnlyList<Result>> GetByStudentAndSemesterAsync(Guid studentProfileId, Guid semesterId, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<Result>)[]);
        public async Task<bool> ExistsAsync(Guid studentProfileId, Guid courseOfferingId, string resultType, CancellationToken ct = default)
            => await GetAsync(studentProfileId, courseOfferingId, resultType, ct) is not null;
        public Task AddAsync(Result result, CancellationToken ct = default) { Results.Add(result); return Task.CompletedTask; }
        public Task AddRangeAsync(IEnumerable<Result> results, CancellationToken ct = default) { Results.AddRange(results); return Task.CompletedTask; }
        public void Update(Result result) { }
        public Task<IReadOnlyList<ResultComponentRule>> GetActiveComponentRulesAsync(CancellationToken ct = default) => Task.FromResult(_rules);
        public Task<IReadOnlyList<ResultComponentRule>> GetActiveComponentRulesAsync(InstitutionType institutionType, CancellationToken ct = default) => Task.FromResult(_rules);
        public Task<IReadOnlyList<ResultComponentRule>> GetAllComponentRulesAsync(CancellationToken ct = default) => Task.FromResult(_rules);
        public Task<IReadOnlyList<ResultComponentRule>> GetAllComponentRulesAsync(InstitutionType institutionType, CancellationToken ct = default) => Task.FromResult(_rules);
        public Task<IReadOnlyList<GpaScaleRule>> GetGpaScaleRulesAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<GpaScaleRule>)[]);
        public Task<IReadOnlyList<GpaScaleRule>> GetGpaScaleRulesAsync(InstitutionType institutionType, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<GpaScaleRule>)[]);
        public Task ReplaceCalculationRulesAsync(IEnumerable<GpaScaleRule> gpaScaleRules, IEnumerable<ResultComponentRule> componentRules, CancellationToken ct = default) => Task.CompletedTask;
        public Task ReplaceCalculationRulesAsync(InstitutionType institutionType, IEnumerable<GpaScaleRule> gpaScaleRules, IEnumerable<ResultComponentRule> componentRules, CancellationToken ct = default) => Task.CompletedTask;
        public Task<StudentProfile?> GetStudentProfileAsync(Guid studentProfileId, CancellationToken ct = default) => Task.FromResult<StudentProfile?>(null);
        public Task<IReadOnlyList<Enrollment>> GetActiveEnrollmentsForSemesterAsync(Guid studentProfileId, Guid semesterId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<Enrollment>)[]);
        public Task<IReadOnlyList<Enrollment>> GetActiveEnrollmentsForStudentAsync(Guid studentProfileId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<Enrollment>)[]);
        public Task<InstitutionType> GetInstitutionTypeForOfferingAsync(Guid courseOfferingId, CancellationToken ct = default) => Task.FromResult(InstitutionType.University);
        public Task<Guid?> GetSemesterIdForOfferingAsync(Guid courseOfferingId, CancellationToken ct = default) => Task.FromResult<Guid?>(null);
        public Task<int?> GetCreditHoursForOfferingAsync(Guid courseOfferingId, CancellationToken ct = default) => Task.FromResult<int?>(null);
        public void UpdateStudentProfile(StudentProfile studentProfile) { }
        public Task<IReadOnlyList<TranscriptExportLog>> GetExportLogsAsync(Guid studentProfileId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<TranscriptExportLog>)[]);
        public Task AddExportLogAsync(TranscriptExportLog log, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> HasPassedCourseAsync(Guid studentProfileId, Guid courseId, decimal passThresholdPercentage, CancellationToken ct = default) => Task.FromResult(false);
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
    }
}
