using Tabsan.EduSphere.Domain.Outcomes;

namespace Tabsan.EduSphere.Domain.Interfaces;

// Plan M6 — read models used to turn graded work into outcome evidence and to build reports.

/// <summary>A posted assignment grade.</summary>
public sealed record AssignmentEvidence(Guid SubmissionId, Guid AssignmentId, Guid StudentProfileId, decimal Marks, decimal MaxMarks, DateTime AssessedAt);

/// <summary>A rubric criterion level chosen on a submission.</summary>
public sealed record RubricEvidence(Guid SubmissionId, Guid CriterionId, decimal Points, decimal MaxPoints);

/// <summary>A graded answer in a finished quiz attempt.</summary>
public sealed record QuizEvidence(Guid AttemptId, Guid QuizId, Guid StudentProfileId, Guid QuestionId, decimal MarksAwarded, decimal MaxMarks, DateTime AssessedAt);

/// <summary>A course offering's identity for outcome pages and permission checks.</summary>
public sealed record OutcomeOfferingInfo(Guid OfferingId, Guid CourseId, string CourseCode, string CourseTitle, Guid DepartmentId, string? SemesterName,
    Guid SemesterId, Guid? TenantId, Guid? CampusId);

/// <summary>What an alignment points at.</summary>
public sealed record OutcomeTargetInfo(OutcomeTargetType Type, Guid Id, string Title, Guid? CourseOfferingId, Guid? TenantId, Guid? DepartmentId);

public sealed record OutcomeStudentInfo(Guid StudentProfileId, string Name, string? RegistrationNumber);

public sealed record OutcomeProgramInfo(Guid Id, string Code, string Name, Guid DepartmentId, string? DepartmentName);

public sealed record OutcomeCourseInfo(Guid Id, string Code, string Title, Guid DepartmentId, Guid? TenantId);

public interface IOutcomeRepository
{
    // ── outcomes ──
    Task<Outcome?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Outcome>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    /// <summary>Outcomes visible to a tenant (its own and installation-wide ones); all of them when <paramref name="allTenants"/>.</summary>
    Task<IReadOnlyList<Outcome>> ListAsync(Guid? tenantId, bool allTenants, OutcomeLevel? level, Guid? programId, Guid? courseId, bool includeInactive, CancellationToken ct = default);
    Task<Outcome?> FindByCodeAsync(Guid? tenantId, OutcomeLevel level, Guid? programId, Guid? courseId, string code, CancellationToken ct = default);
    void Add(Outcome outcome);

    // ── mappings ──
    Task<IReadOnlyList<OutcomeMapping>> GetMappingsAsync(IReadOnlyCollection<Guid>? courseOutcomeIds, IReadOnlyCollection<Guid>? programOutcomeIds, CancellationToken ct = default);
    void Add(OutcomeMapping mapping);
    void Remove(OutcomeMapping mapping);

    // ── alignments ──
    Task<IReadOnlyList<OutcomeAlignment>> GetAlignmentsAsync(IReadOnlyCollection<(OutcomeTargetType Type, Guid Id)> targets, CancellationToken ct = default);
    Task<IReadOnlyList<OutcomeAlignment>> GetAlignmentsForOfferingAsync(Guid offeringId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, int>> CountAlignmentsAsync(IReadOnlyCollection<Guid> outcomeIds, CancellationToken ct = default);
    Task<bool> AnyAlignmentsAsync(CancellationToken ct = default);
    void Add(OutcomeAlignment alignment);
    void Remove(OutcomeAlignment alignment);

    // ── results ──
    Task<IReadOnlyList<StudentOutcomeResult>> GetResultsAsync(Guid? offeringId, IReadOnlyCollection<Guid>? studentIds, IReadOnlyCollection<Guid>? outcomeIds,
        OutcomeSourceType? sourceType, CancellationToken ct = default);
    void Add(StudentOutcomeResult result);
    void Remove(StudentOutcomeResult result);
    Task<int> CountResultsAsync(Guid outcomeId, CancellationToken ct = default);

    // ── evidence ──
    Task<IReadOnlyList<AssignmentEvidence>> GetAssignmentEvidenceAsync(Guid offeringId, IReadOnlyCollection<Guid>? studentIds, CancellationToken ct = default);
    Task<IReadOnlyList<RubricEvidence>> GetRubricEvidenceAsync(IReadOnlyCollection<Guid> submissionIds, CancellationToken ct = default);
    Task<IReadOnlyList<QuizEvidence>> GetQuizEvidenceAsync(Guid offeringId, IReadOnlyCollection<Guid>? studentIds, CancellationToken ct = default);
    /// <summary>Offerings with finished attempts that contain the question (where a bank question was used).</summary>
    Task<IReadOnlyList<Guid>> GetOfferingsUsingQuestionAsync(Guid questionId, CancellationToken ct = default);
    /// <summary>Titles of the submissions' assignments and the attempts' quizzes.</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetSourceTitlesAsync(IReadOnlyCollection<Guid> submissionIds, IReadOnlyCollection<Guid> attemptIds, CancellationToken ct = default);

    // ── context ──
    Task<OutcomeOfferingInfo?> GetOfferingAsync(Guid offeringId, CancellationToken ct = default);
    Task<IReadOnlyList<OutcomeOfferingInfo>> GetOfferingsAsync(IReadOnlyCollection<Guid> offeringIds, CancellationToken ct = default);
    Task<OutcomeTargetInfo?> GetTargetAsync(OutcomeTargetType type, Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<OutcomeStudentInfo>> GetEnrolledStudentsAsync(Guid offeringId, CancellationToken ct = default);
    Task<IReadOnlyList<OutcomeStudentInfo>> GetStudentsAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken ct = default);
    /// <summary>Offerings the student is (or was) enrolled in.</summary>
    Task<IReadOnlyList<Guid>> GetStudentOfferingsAsync(Guid studentProfileId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetProgramStudentIdsAsync(Guid programId, CancellationToken ct = default);
    Task<OutcomeProgramInfo?> GetProgramAsync(Guid programId, CancellationToken ct = default);
    Task<IReadOnlyList<OutcomeProgramInfo>> GetProgramsAsync(IReadOnlyCollection<Guid>? ids, Guid? departmentId, CancellationToken ct = default);
    Task<OutcomeProgramInfo?> FindProgramByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<OutcomeCourseInfo>> GetCoursesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<IReadOnlyList<OutcomeCourseInfo>> FindCoursesByCodeAsync(string code, Guid? tenantId, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
