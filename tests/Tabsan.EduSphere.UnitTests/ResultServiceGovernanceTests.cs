using FluentAssertions;
using Tabsan.EduSphere.Application.Assignments;
using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.UnitTests;

public class ResultServiceGovernanceTests
{
    [Fact]
    public async Task CorrectAsync_WhenResultIsDraft_ThrowsInvalidOperationException()
    {
        var studentId = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        var draft = new Result(studentId, offeringId, "Final", 78m, 100m);

        var repo = new StubResultRepository(draft);
        var audit = new CapturingAuditService();
        var sut = new ResultService(repo, audit);

        var act = () => sut.CorrectAsync(
            studentId,
            offeringId,
            "Final",
            new CorrectResultRequest(80m, 100m, "Recheck approved"),
            Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*published results*");
        audit.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task CorrectAsync_WhenPublished_LogsAuditWithReason()
    {
        var studentId = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        var published = new Result(studentId, offeringId, "Final", 78m, 100m);
        published.Publish(Guid.NewGuid());

        var repo = new StubResultRepository(published);
        var audit = new CapturingAuditService();
        var sut = new ResultService(repo, audit);

        var ok = await sut.CorrectAsync(
            studentId,
            offeringId,
            "Final",
            new CorrectResultRequest(84m, 100m, "Answer-sheet recount complete"),
            Guid.NewGuid());

        ok.Should().BeTrue();
        published.MarksObtained.Should().Be(84m);
        audit.Entries.Should().ContainSingle(e => e.Action == "CorrectResult");
        audit.Entries.Single(e => e.Action == "CorrectResult").NewValuesJson
            .Should().Contain("Answer-sheet recount complete");
    }

    [Fact]
    public async Task PublishAllForOffering_PublishesOnlyDraftRows()
    {
        var offeringId = Guid.NewGuid();
        var published = new Result(Guid.NewGuid(), offeringId, "Final", 90m, 100m);
        published.Publish(Guid.NewGuid());
        var draft = new Result(Guid.NewGuid(), offeringId, "Midterm", 33m, 50m);

        var repo = new StubResultRepository(published, draft);
        var audit = new CapturingAuditService();
        var sut = new ResultService(repo, audit);

        var count = await sut.PublishAllForOfferingAsync(offeringId, Guid.NewGuid());

        count.Should().Be(1);
        draft.IsPublished.Should().BeTrue();
        audit.Entries.Should().ContainSingle(e => e.Action == "BulkPublishResults");
    }

    private sealed class CapturingAuditService : IAuditService
    {
        public List<AuditLog> Entries { get; } = new();

        public Task LogAsync(AuditLog entry, CancellationToken ct = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(string? query = null, Guid? actorUserId = null, string? action = null, string? entityName = null, DateTime? fromUtc = null, DateTime? toUtc = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
            => Task.FromResult(((IReadOnlyList<AuditLog>)Entries, Entries.Count));
    }

    private sealed class StubResultRepository : IResultRepository
    {
        private readonly List<Result> _results;

        public StubResultRepository(params Result[] seed)
        {
            _results = seed.ToList();
        }

        public Task<Result?> GetAsync(Guid studentProfileId, Guid courseOfferingId, string resultType, CancellationToken ct = default)
            => Task.FromResult(_results.FirstOrDefault(r =>
                r.StudentProfileId == studentProfileId
                && r.CourseOfferingId == courseOfferingId
                && string.Equals(r.ResultType, resultType, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<Result>> GetByStudentAsync(Guid studentProfileId, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<Result>)_results.Where(r => r.StudentProfileId == studentProfileId).ToList());

        public Task<IReadOnlyList<Result>> GetPublishedByStudentAsync(Guid studentProfileId, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<Result>)_results.Where(r => r.StudentProfileId == studentProfileId && r.IsPublished).ToList());

        public Task<IReadOnlyList<Result>> GetByOfferingAsync(Guid courseOfferingId, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<Result>)_results.Where(r => r.CourseOfferingId == courseOfferingId).ToList());

        public Task<IReadOnlyList<Result>> GetByStudentAndOfferingAsync(Guid studentProfileId, Guid courseOfferingId, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<Result>)_results.Where(r => r.StudentProfileId == studentProfileId && r.CourseOfferingId == courseOfferingId).ToList());

        public Task<IReadOnlyList<Result>> GetByStudentAndSemesterAsync(Guid studentProfileId, Guid semesterId, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<Result>)new List<Result>());

        public Task<bool> ExistsAsync(Guid studentProfileId, Guid courseOfferingId, string resultType, CancellationToken ct = default)
            => Task.FromResult(_results.Any(r => r.StudentProfileId == studentProfileId && r.CourseOfferingId == courseOfferingId && string.Equals(r.ResultType, resultType, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(Result result, CancellationToken ct = default)
        {
            _results.Add(result);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<Result> results, CancellationToken ct = default)
        {
            _results.AddRange(results);
            return Task.CompletedTask;
        }

        public void Update(Result result)
        {
        }

        public Task<IReadOnlyList<ResultComponentRule>> GetActiveComponentRulesAsync(CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<ResultComponentRule>)new List<ResultComponentRule>());

        public Task<IReadOnlyList<ResultComponentRule>> GetAllComponentRulesAsync(CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<ResultComponentRule>)new List<ResultComponentRule>());

        public Task<IReadOnlyList<ResultComponentRule>> GetAllComponentRulesAsync(InstitutionType institutionType, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<ResultComponentRule>)new List<ResultComponentRule>());

        public Task<IReadOnlyList<GpaScaleRule>> GetGpaScaleRulesAsync(CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<GpaScaleRule>)new List<GpaScaleRule>());

        public Task<IReadOnlyList<GpaScaleRule>> GetGpaScaleRulesAsync(InstitutionType institutionType, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<GpaScaleRule>)new List<GpaScaleRule>());

        public Task ReplaceCalculationRulesAsync(IEnumerable<GpaScaleRule> gpaScaleRules, IEnumerable<ResultComponentRule> componentRules, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task ReplaceCalculationRulesAsync(InstitutionType institutionType, IEnumerable<GpaScaleRule> gpaScaleRules, IEnumerable<ResultComponentRule> componentRules, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<StudentProfile?> GetStudentProfileAsync(Guid studentProfileId, CancellationToken ct = default)
            => Task.FromResult<StudentProfile?>(null);

        public Task<IReadOnlyList<Enrollment>> GetActiveEnrollmentsForSemesterAsync(Guid studentProfileId, Guid semesterId, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<Enrollment>)new List<Enrollment>());

        public Task<IReadOnlyList<Enrollment>> GetActiveEnrollmentsForStudentAsync(Guid studentProfileId, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<Enrollment>)new List<Enrollment>());

        public Task<Guid?> GetSemesterIdForOfferingAsync(Guid courseOfferingId, CancellationToken ct = default)
            => Task.FromResult<Guid?>(null);

        public Task<int?> GetCreditHoursForOfferingAsync(Guid courseOfferingId, CancellationToken ct = default)
            => Task.FromResult<int?>(null);

        public void UpdateStudentProfile(StudentProfile studentProfile)
        {
        }

        public Task<IReadOnlyList<TranscriptExportLog>> GetExportLogsAsync(Guid studentProfileId, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<TranscriptExportLog>)new List<TranscriptExportLog>());

        public Task AddExportLogAsync(TranscriptExportLog log, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<bool> HasPassedCourseAsync(Guid studentProfileId, Guid courseId, decimal passThresholdPercentage, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
            => Task.FromResult(1);
    }
}
