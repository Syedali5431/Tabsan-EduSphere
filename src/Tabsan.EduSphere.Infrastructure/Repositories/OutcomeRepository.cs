using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Outcomes;
using Tabsan.EduSphere.Domain.Quizzes;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>Plan M6 — outcomes, alignments and mastery evidence, plus read models over graded work.</summary>
public sealed class OutcomeRepository : IOutcomeRepository
{
    private readonly ApplicationDbContext _db;

    public OutcomeRepository(ApplicationDbContext db) => _db = db;

    // ── outcomes ──

    public Task<Outcome?> GetAsync(Guid id, CancellationToken ct = default)
        => _db.Outcomes.Include(o => o.Ratings).FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Outcome>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        => ids.Count == 0 ? [] : await _db.Outcomes.Include(o => o.Ratings).Where(o => ids.Contains(o.Id)).ToListAsync(ct);

    public async Task<IReadOnlyList<Outcome>> ListAsync(Guid? tenantId, bool allTenants, OutcomeLevel? level, Guid? programId, Guid? courseId,
        bool includeInactive, CancellationToken ct = default)
    {
        var q = _db.Outcomes.Include(o => o.Ratings).AsQueryable();
        if (!allTenants) q = q.Where(o => o.TenantId == null || o.TenantId == tenantId);
        if (level is { } l) q = q.Where(o => o.Level == l);
        if (programId is { } p) q = q.Where(o => o.ProgramId == p);
        if (courseId is { } c) q = q.Where(o => o.CourseId == c);
        if (!includeInactive) q = q.Where(o => o.IsActive);
        return await q.OrderBy(o => o.Level).ThenBy(o => o.Code).ToListAsync(ct);
    }

    public Task<Outcome?> FindByCodeAsync(Guid? tenantId, OutcomeLevel level, Guid? programId, Guid? courseId, string code, CancellationToken ct = default)
        => _db.Outcomes.Include(o => o.Ratings).FirstOrDefaultAsync(o => o.TenantId == tenantId && o.Level == level && o.ProgramId == programId
            && o.CourseId == courseId && o.Code == code, ct);

    public void Add(Outcome outcome)
    {
        if (_db.Entry(outcome).State == EntityState.Detached) _db.Outcomes.Add(outcome);
    }

    // ── mappings ──

    public async Task<IReadOnlyList<OutcomeMapping>> GetMappingsAsync(IReadOnlyCollection<Guid>? courseOutcomeIds, IReadOnlyCollection<Guid>? programOutcomeIds, CancellationToken ct = default)
    {
        var q = _db.OutcomeMappings.AsQueryable();
        if (courseOutcomeIds is not null) q = q.Where(m => courseOutcomeIds.Contains(m.CourseOutcomeId));
        if (programOutcomeIds is not null) q = q.Where(m => programOutcomeIds.Contains(m.ProgramOutcomeId));
        return await q.ToListAsync(ct);
    }

    public void Add(OutcomeMapping mapping) => _db.OutcomeMappings.Add(mapping);
    public void Remove(OutcomeMapping mapping) => _db.OutcomeMappings.Remove(mapping);

    // ── alignments ──

    public async Task<IReadOnlyList<OutcomeAlignment>> GetAlignmentsAsync(IReadOnlyCollection<(OutcomeTargetType Type, Guid Id)> targets, CancellationToken ct = default)
    {
        if (targets.Count == 0) return [];
        var ids = targets.Select(t => t.Id).Distinct().ToList();
        var rows = new List<OutcomeAlignment>();
        foreach (var chunk in ids.Chunk(1000))
            rows.AddRange(await _db.OutcomeAlignments.Where(a => chunk.Contains(a.TargetId)).ToListAsync(ct));
        var wanted = targets.ToHashSet();
        return rows.Where(a => wanted.Contains((a.TargetType, a.TargetId))).ToList();
    }

    public async Task<IReadOnlyList<OutcomeAlignment>> GetAlignmentsForOfferingAsync(Guid offeringId, CancellationToken ct = default)
        => await _db.OutcomeAlignments.Where(a => a.CourseOfferingId == offeringId).ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, int>> CountAlignmentsAsync(IReadOnlyCollection<Guid> outcomeIds, CancellationToken ct = default)
        => outcomeIds.Count == 0 ? new Dictionary<Guid, int>()
            : await _db.OutcomeAlignments.Where(a => outcomeIds.Contains(a.OutcomeId)).GroupBy(a => a.OutcomeId)
                .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);

    public Task<bool> AnyAlignmentsAsync(CancellationToken ct = default) => _db.OutcomeAlignments.AnyAsync(ct);

    public void Add(OutcomeAlignment alignment) => _db.OutcomeAlignments.Add(alignment);
    public void Remove(OutcomeAlignment alignment) => _db.OutcomeAlignments.Remove(alignment);

    // ── results ──

    public async Task<IReadOnlyList<StudentOutcomeResult>> GetResultsAsync(Guid? offeringId, IReadOnlyCollection<Guid>? studentIds, IReadOnlyCollection<Guid>? outcomeIds,
        OutcomeSourceType? sourceType, CancellationToken ct = default)
    {
        var q = _db.StudentOutcomeResults.AsQueryable();
        if (offeringId is { } o) q = q.Where(r => r.CourseOfferingId == o);
        if (studentIds is not null) q = q.Where(r => studentIds.Contains(r.StudentProfileId));
        if (outcomeIds is not null) q = q.Where(r => outcomeIds.Contains(r.OutcomeId));
        if (sourceType is { } s) q = q.Where(r => r.SourceType == s);
        return await q.ToListAsync(ct);
    }

    public void Add(StudentOutcomeResult result) => _db.StudentOutcomeResults.Add(result);
    public void Remove(StudentOutcomeResult result) => _db.StudentOutcomeResults.Remove(result);

    public Task<int> CountResultsAsync(Guid outcomeId, CancellationToken ct = default)
        => _db.StudentOutcomeResults.CountAsync(r => r.OutcomeId == outcomeId, ct);

    // ── evidence ──

    public async Task<IReadOnlyList<AssignmentEvidence>> GetAssignmentEvidenceAsync(Guid offeringId, IReadOnlyCollection<Guid>? studentIds, CancellationToken ct = default)
    {
        var q = _db.AssignmentSubmissions.AsNoTracking()
            .Where(s => s.Assignment.CourseOfferingId == offeringId && s.PostedAt != null && s.MarksAwarded != null && s.Status == SubmissionStatus.Graded
                && s.Assignment.MaxMarks > 0);
        if (studentIds is not null) q = q.Where(s => studentIds.Contains(s.StudentProfileId));
        return await q.Select(s => new AssignmentEvidence(s.Id, s.AssignmentId, s.StudentProfileId, s.MarksAwarded!.Value, s.Assignment.MaxMarks,
            s.GradedAt ?? s.PostedAt!.Value)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<RubricEvidence>> GetRubricEvidenceAsync(IReadOnlyCollection<Guid> submissionIds, CancellationToken ct = default)
    {
        var rows = new List<RubricEvidence>();
        foreach (var chunk in submissionIds.Chunk(1000))
            rows.AddRange(await (from g in _db.RubricStudentGrades.AsNoTracking()
                                 join c in _db.RubricCriteria.AsNoTracking() on g.RubricCriterionId equals c.Id
                                 where chunk.Contains(g.AssignmentSubmissionId) && c.MaxPoints > 0
                                 select new RubricEvidence(g.AssignmentSubmissionId, c.Id, g.PointsAwarded, c.MaxPoints)).ToListAsync(ct));
        return rows;
    }

    public async Task<IReadOnlyList<QuizEvidence>> GetQuizEvidenceAsync(Guid offeringId, IReadOnlyCollection<Guid>? studentIds, CancellationToken ct = default)
    {
        var q = from a in _db.QuizAnswers.AsNoTracking()
                where a.Attempt.Quiz.CourseOfferingId == offeringId && a.MarksAwarded != null
                      && (a.Attempt.Status == AttemptStatus.Submitted || a.Attempt.Status == AttemptStatus.TimedOut)
                select new
                {
                    a.QuizAttemptId, a.Attempt.QuizId, a.Attempt.StudentProfileId, a.QuizQuestionId, Marks = a.MarksAwarded!.Value,
                    ItemMarks = a.Attempt.Items.Where(i => i.QuizQuestionId == a.QuizQuestionId).Select(i => (decimal?)i.Marks).FirstOrDefault(),
                    QuestionMarks = a.Question.Marks,
                    At = a.Attempt.FinishedAt ?? a.Attempt.StartedAt
                };
        if (studentIds is not null) q = q.Where(x => studentIds.Contains(x.StudentProfileId));
        var rows = await q.ToListAsync(ct);
        return rows.Select(r => new QuizEvidence(r.QuizAttemptId, r.QuizId, r.StudentProfileId, r.QuizQuestionId, r.Marks, r.ItemMarks ?? r.QuestionMarks, r.At))
            .Where(r => r.MaxMarks > 0).ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetOfferingsUsingQuestionAsync(Guid questionId, CancellationToken ct = default)
        => await _db.QuizAnswers.AsNoTracking().Where(a => a.QuizQuestionId == questionId).Select(a => a.Attempt.Quiz.CourseOfferingId)
            .Distinct().ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, string>> GetSourceTitlesAsync(IReadOnlyCollection<Guid> submissionIds, IReadOnlyCollection<Guid> attemptIds, CancellationToken ct = default)
    {
        var result = new Dictionary<Guid, string>();
        if (submissionIds.Count > 0)
            foreach (var r in await _db.AssignmentSubmissions.AsNoTracking().Where(s => submissionIds.Contains(s.Id)).Select(s => new { s.Id, s.Assignment.Title }).ToListAsync(ct))
                result[r.Id] = r.Title;
        if (attemptIds.Count > 0)
            foreach (var r in await _db.QuizAttempts.AsNoTracking().Where(a => attemptIds.Contains(a.Id)).Select(a => new { a.Id, a.Quiz.Title }).ToListAsync(ct))
                result[r.Id] = r.Title;
        return result;
    }

    // ── context ──

    // Filters go before the projection: EF cannot translate members of a record built by its constructor.
    private static IQueryable<OutcomeOfferingInfo> Project(IQueryable<CourseOffering> offerings)
        => offerings.Select(o => new OutcomeOfferingInfo(o.Id, o.CourseId, o.Course.Code, o.Course.Title, o.Course.DepartmentId, o.Semester.Name, o.SemesterId, o.TenantId, o.CampusId));

    public Task<OutcomeOfferingInfo?> GetOfferingAsync(Guid offeringId, CancellationToken ct = default)
        => Project(_db.CourseOfferings.IgnoreQueryFilters().AsNoTracking().Where(o => o.Id == offeringId)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<OutcomeOfferingInfo>> GetOfferingsAsync(IReadOnlyCollection<Guid> offeringIds, CancellationToken ct = default)
        => offeringIds.Count == 0 ? [] : await Project(_db.CourseOfferings.IgnoreQueryFilters().AsNoTracking().Where(o => offeringIds.Contains(o.Id))).ToListAsync(ct);

    public async Task<OutcomeTargetInfo?> GetTargetAsync(OutcomeTargetType type, Guid id, CancellationToken ct = default)
    {
        var offerings = _db.CourseOfferings.IgnoreQueryFilters().AsNoTracking();
        switch (type)
        {
            case OutcomeTargetType.Assignment:
                return await (from a in _db.Assignments.AsNoTracking()
                              join o in offerings on a.CourseOfferingId equals o.Id
                              where a.Id == id
                              select new OutcomeTargetInfo(type, a.Id, a.Title, (Guid?)o.Id, o.TenantId, (Guid?)o.Course.DepartmentId)).FirstOrDefaultAsync(ct);
            case OutcomeTargetType.RubricCriterion:
                return await (from c in _db.RubricCriteria.AsNoTracking()
                              join r in _db.Rubrics.AsNoTracking() on c.RubricId equals r.Id
                              join a in _db.Assignments.AsNoTracking() on r.AssignmentId equals a.Id
                              join o in offerings on a.CourseOfferingId equals o.Id
                              where c.Id == id
                              select new OutcomeTargetInfo(type, c.Id, a.Title + " — " + c.Name, (Guid?)o.Id, o.TenantId, (Guid?)o.Course.DepartmentId))
                    .FirstOrDefaultAsync(ct);
            default:
                var question = await _db.QuizQuestions.AsNoTracking().Where(q => q.Id == id)
                    .Select(q => new { q.Id, q.Text, q.QuizId, q.QuestionBankId }).FirstOrDefaultAsync(ct);
                if (question is null) return null;
                var title = question.Text.Length > 120 ? question.Text[..120] + "…" : question.Text;
                if (type == OutcomeTargetType.QuizQuestion)
                {
                    if (question.QuizId is null) return null;
                    return await (from q in _db.Quizzes.AsNoTracking()
                                  join o in offerings on q.CourseOfferingId equals o.Id
                                  where q.Id == question.QuizId
                                  select new OutcomeTargetInfo(type, question.Id, q.Title + " — " + title, (Guid?)o.Id, o.TenantId, (Guid?)o.Course.DepartmentId))
                        .FirstOrDefaultAsync(ct);
                }
                if (question.QuestionBankId is null) return null;
                return await _db.QuestionBanks.AsNoTracking().Where(b => b.Id == question.QuestionBankId)
                    .Select(b => new OutcomeTargetInfo(type, question.Id, b.Title + " — " + title, b.CourseOfferingId, b.TenantId, b.DepartmentId))
                    .FirstOrDefaultAsync(ct);
        }
    }

    private IQueryable<OutcomeStudentInfo> Students(IQueryable<Guid> ids)
        => from sp in _db.StudentProfiles.IgnoreQueryFilters().AsNoTracking()
           join u in _db.Users.IgnoreQueryFilters().AsNoTracking() on sp.UserId equals u.Id
           where ids.Contains(sp.Id)
           select new OutcomeStudentInfo(sp.Id, u.FullName ?? u.Username, sp.RegistrationNumber);

    public async Task<IReadOnlyList<OutcomeStudentInfo>> GetEnrolledStudentsAsync(Guid offeringId, CancellationToken ct = default)
        => await Students(_db.Enrollments.Where(e => e.CourseOfferingId == offeringId && e.Status == EnrollmentStatus.Active).Select(e => e.StudentProfileId))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<OutcomeStudentInfo>> GetStudentsAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken ct = default)
        => studentIds.Count == 0 ? [] : await Students(_db.StudentProfiles.IgnoreQueryFilters().Where(sp => studentIds.Contains(sp.Id)).Select(sp => sp.Id)).ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetStudentOfferingsAsync(Guid studentProfileId, CancellationToken ct = default)
        => await _db.Enrollments.AsNoTracking().Where(e => e.StudentProfileId == studentProfileId).Select(e => e.CourseOfferingId).Distinct().ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetProgramStudentIdsAsync(Guid programId, CancellationToken ct = default)
        => await _db.StudentProfiles.AsNoTracking().Where(sp => sp.ProgramId == programId).Select(sp => sp.Id).ToListAsync(ct);

    private IQueryable<AcademicProgram> ProgramSource() => _db.AcademicPrograms.IgnoreQueryFilters().AsNoTracking();

    private static IQueryable<OutcomeProgramInfo> Project(IQueryable<AcademicProgram> programs)
        => programs.Select(p => new OutcomeProgramInfo(p.Id, p.Code, p.Name, p.DepartmentId, p.Department.Name));

    public Task<OutcomeProgramInfo?> GetProgramAsync(Guid programId, CancellationToken ct = default)
        => Project(ProgramSource().Where(p => p.Id == programId)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<OutcomeProgramInfo>> GetProgramsAsync(IReadOnlyCollection<Guid>? ids, Guid? departmentId, CancellationToken ct = default)
    {
        var q = ProgramSource();
        if (ids is not null) { if (ids.Count == 0) return []; q = q.Where(p => ids.Contains(p.Id)); }
        if (departmentId is { } d) q = q.Where(p => p.DepartmentId == d);
        return await Project(q.OrderBy(p => p.Code)).ToListAsync(ct);
    }

    public Task<OutcomeProgramInfo?> FindProgramByCodeAsync(string code, CancellationToken ct = default)
        => Project(ProgramSource().Where(p => p.Code == code)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<OutcomeCourseInfo>> GetCoursesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        => ids.Count == 0 ? [] : await _db.Courses.IgnoreQueryFilters().AsNoTracking().Where(c => ids.Contains(c.Id))
            .Select(c => new OutcomeCourseInfo(c.Id, c.Code, c.Title, c.DepartmentId, c.TenantId)).ToListAsync(ct);

    public async Task<IReadOnlyList<OutcomeCourseInfo>> FindCoursesByCodeAsync(string code, Guid? tenantId, CancellationToken ct = default)
    {
        var q = _db.Courses.AsNoTracking().Where(c => c.Code == code);
        if (tenantId is { } t) q = q.Where(c => c.TenantId == null || c.TenantId == t);
        return await q.Select(c => new OutcomeCourseInfo(c.Id, c.Code, c.Title, c.DepartmentId, c.TenantId)).ToListAsync(ct);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
