using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Fyp;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Quizzes;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

// ══════════════════════════════════════════════════════════════════════════════
// QuizRepository
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// EF Core implementation of <see cref="IQuizRepository"/>.
/// Handles quiz authoring, question/option management, and attempt/answer persistence.
/// </summary>
public sealed class QuizRepository : IQuizRepository
{
    private readonly ApplicationDbContext _db;

    /// <summary>Initialises the repository with the application DbContext.</summary>
    public QuizRepository(ApplicationDbContext db) => _db = db;

    // ── Quiz ──────────────────────────────────────────────────────────────────

    /// <summary>Returns a quiz by its ID (active only via query filter), or null.</summary>
    public Task<Quiz?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Quizzes.FirstOrDefaultAsync(q => q.Id == id, ct);

    /// <summary>Returns a quiz by its ID including inactive rows, or null.</summary>
    public Task<Quiz?> GetByIdIncludingInactiveAsync(Guid id, CancellationToken ct = default)
        => _db.Quizzes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(q => q.Id == id, ct);

    /// <summary>Returns a quiz with its questions and options loaded, or null.</summary>
    public Task<Quiz?> GetWithQuestionsAsync(Guid id, CancellationToken ct = default)
        => _db.Quizzes
              .Include(q => q.Questions.OrderBy(x => x.OrderIndex))
                  .ThenInclude(q => q.Options.OrderBy(o => o.OrderIndex))
              .FirstOrDefaultAsync(q => q.Id == id, ct);

    /// <summary>Returns quizzes for a course offering, optionally including inactive rows.</summary>
    public async Task<IReadOnlyList<Quiz>> GetByOfferingAsync(Guid courseOfferingId, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = includeInactive ? _db.Quizzes.IgnoreQueryFilters() : _db.Quizzes;

        return await query
              .Include(q => q.Questions)        // Plan M2: question counts in listings
              .Include(q => q.QuestionGroups)
              .AsSplitQuery()
              .Where(q => q.CourseOfferingId == courseOfferingId)
              .OrderBy(q => q.AvailableFrom)
              .ToListAsync(ct);
    }

    /// <summary>Queues a quiz for insertion.</summary>
    public async Task AddAsync(Quiz quiz, CancellationToken ct = default)
        => await _db.Quizzes.AddAsync(quiz, ct);

    /// <summary>Marks a quiz entity as modified.</summary>
    public void Update(Quiz quiz) => _db.Quizzes.Update(quiz);

    // ── Questions ─────────────────────────────────────────────────────────────

    /// <summary>Returns a question by its ID, or null.</summary>
    public Task<QuizQuestion?> GetQuestionByIdAsync(Guid questionId, CancellationToken ct = default)
        => _db.QuizQuestions
              .Include(q => q.Options.OrderBy(o => o.OrderIndex))
              .FirstOrDefaultAsync(q => q.Id == questionId, ct);

    /// <summary>Queues a question for insertion.</summary>
    public async Task AddQuestionAsync(QuizQuestion question, CancellationToken ct = default)
        => await _db.QuizQuestions.AddAsync(question, ct);

    /// <summary>Marks a question entity as modified.</summary>
    public void UpdateQuestion(QuizQuestion question) => _db.QuizQuestions.Update(question);

    /// <summary>Removes a question and cascades to its options.</summary>
    public void RemoveQuestion(QuizQuestion question) => _db.QuizQuestions.Remove(question);

    /// <summary>Queues multiple options for bulk insertion.</summary>
    public async Task AddOptionsAsync(IEnumerable<QuizOption> options, CancellationToken ct = default)
        => await _db.QuizOptions.AddRangeAsync(options, ct);

    /// <summary>Removes a collection of options from the context.</summary>
    public void RemoveOptions(IEnumerable<QuizOption> options) => _db.QuizOptions.RemoveRange(options);

    // ── Attempts ──────────────────────────────────────────────────────────────

    /// <summary>Returns an attempt by its ID including its answers, or null.</summary>
    public Task<QuizAttempt?> GetAttemptByIdAsync(Guid attemptId, CancellationToken ct = default)
        => _db.QuizAttempts
              .Include(a => a.Answers)
              .FirstOrDefaultAsync(a => a.Id == attemptId, ct);

    /// <summary>Returns all attempts a student has made on a specific quiz.</summary>
    public async Task<IReadOnlyList<QuizAttempt>> GetAttemptsAsync(Guid quizId, Guid studentProfileId, CancellationToken ct = default)
        => await _db.QuizAttempts
              .Where(a => a.QuizId == quizId && a.StudentProfileId == studentProfileId)
              .OrderByDescending(a => a.StartedAt)
              .ToListAsync(ct);

    /// <summary>Returns all attempts across all quizzes for a student.</summary>
    public async Task<IReadOnlyList<QuizAttempt>> GetAllAttemptsForStudentAsync(Guid studentProfileId, CancellationToken ct = default)
        => await _db.QuizAttempts
              .Where(a => a.StudentProfileId == studentProfileId)
              .OrderByDescending(a => a.StartedAt)
              .ToListAsync(ct);

    /// <summary>Returns the number of attempts the student has made on the quiz.</summary>
    public Task<int> GetAttemptCountAsync(Guid quizId, Guid studentProfileId, CancellationToken ct = default)
        => _db.QuizAttempts
              .CountAsync(a => a.QuizId == quizId && a.StudentProfileId == studentProfileId, ct);

    /// <summary>Returns the in-progress attempt for a student on a quiz, or null.</summary>
    public Task<QuizAttempt?> GetInProgressAttemptAsync(Guid quizId, Guid studentProfileId, CancellationToken ct = default)
        => _db.QuizAttempts
              .FirstOrDefaultAsync(
                  a => a.QuizId == quizId
                    && a.StudentProfileId == studentProfileId
                    && a.Status == AttemptStatus.InProgress, ct);

    /// <summary>Queues a new attempt for insertion.</summary>
    public async Task AddAttemptAsync(QuizAttempt attempt, CancellationToken ct = default)
        => await _db.QuizAttempts.AddAsync(attempt, ct);

    /// <summary>Marks an attempt entity as modified.</summary>
    public void UpdateAttempt(QuizAttempt attempt) => _db.QuizAttempts.Update(attempt);

    /// <summary>Queues multiple answers for bulk insertion.</summary>
    public async Task AddAnswersAsync(IEnumerable<QuizAnswer> answers, CancellationToken ct = default)
        => await _db.QuizAnswers.AddRangeAsync(answers, ct);

    /// <summary>Returns a single answer by its ID, or null.</summary>
    public Task<QuizAnswer?> GetAnswerByIdAsync(Guid answerId, CancellationToken ct = default)
        => _db.QuizAnswers.FirstOrDefaultAsync(a => a.Id == answerId, ct);

    /// <summary>Marks an answer entity as modified (for manual short-answer grading).</summary>
    public void UpdateAnswer(QuizAnswer answer) => _db.QuizAnswers.Update(answer);

    /// <summary>Commits all pending changes.</summary>
    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    // ── Plan M2 ───────────────────────────────────────────────────────────────

    public Task<Quiz?> GetForAuthoringAsync(Guid quizId, CancellationToken ct = default)
        => _db.Quizzes
              .Include(q => q.Questions).ThenInclude(q => q.Options)
              .Include(q => q.QuestionGroups).ThenInclude(g => g.QuestionBank)
              .AsSplitQuery()
              .FirstOrDefaultAsync(q => q.Id == quizId, ct);

    public Task<bool> QuestionHasAnswersAsync(Guid questionId, CancellationToken ct = default)
        => _db.QuizAnswers.IgnoreQueryFilters().AnyAsync(a => a.QuizQuestionId == questionId, ct);

    public Task<QuestionBank?> GetBankAsync(Guid bankId, bool includeQuestions, CancellationToken ct = default)
    {
        IQueryable<QuestionBank> query = _db.QuestionBanks;
        if (includeQuestions)
            query = query.Include(b => b.Questions.Where(q => !q.IsArchived)).ThenInclude(q => q.Options).AsSplitQuery();
        return query.FirstOrDefaultAsync(b => b.Id == bankId, ct);
    }

    public async Task<IReadOnlyList<QuestionBank>> GetBanksAsync(Guid? tenantId, Guid? courseOfferingId, Guid? departmentId, Guid? userId, CancellationToken ct = default)
    {
        var query = _db.QuestionBanks.AsNoTracking().Where(b => b.TenantId == tenantId);
        // Visible: shared banks, banks of the requested course/department, and the caller's own banks.
        query = query.Where(b => b.IsShared
                                 || (courseOfferingId != null && b.CourseOfferingId == courseOfferingId)
                                 || (departmentId != null && b.DepartmentId == departmentId)
                                 || (userId != null && b.CreatedByUserId == userId));
        return await query.OrderBy(b => b.Title).ToListAsync(ct);
    }

    public async Task AddBankAsync(QuestionBank bank, CancellationToken ct = default)
        => await _db.QuestionBanks.AddAsync(bank, ct);

    public Task<bool> IsBankUsedAsync(Guid bankId, CancellationToken ct = default)
        => _db.QuizQuestionGroups.AnyAsync(g => g.QuestionBankId == bankId, ct);

    private IQueryable<QuizQuestion> Drawable(Guid bankId, string? tagFilter)
    {
        var query = _db.QuizQuestions.Where(q => q.QuestionBankId == bankId && !q.IsArchived);
        if (!string.IsNullOrWhiteSpace(tagFilter))
        {
            var tag = tagFilter.Trim().ToLowerInvariant();
            // Tags are stored normalised as "a,b,c".
            query = query.Where(q => q.Tags != null && ("," + q.Tags + ",").Contains("," + tag + ","));
        }
        return query;
    }

    public Task<int> CountDrawableAsync(Guid bankId, string? tagFilter, CancellationToken ct = default)
        => Drawable(bankId, tagFilter).CountAsync(ct);

    public async Task<IReadOnlyList<QuizQuestion>> GetDrawableQuestionsAsync(Guid bankId, string? tagFilter, CancellationToken ct = default)
        => await Drawable(bankId, tagFilter).Include(q => q.Options).AsSplitQuery().ToListAsync(ct);

    public async Task<IReadOnlyList<QuizQuestion>> GetQuestionsWithOptionsAsync(IReadOnlyCollection<Guid> questionIds, CancellationToken ct = default)
        => await _db.QuizQuestions.Include(q => q.Options).Where(q => questionIds.Contains(q.Id)).AsSplitQuery().ToListAsync(ct);

    public Task<QuizQuestionGroup?> GetGroupAsync(Guid groupId, CancellationToken ct = default)
        => _db.QuizQuestionGroups.Include(g => g.QuestionBank).FirstOrDefaultAsync(g => g.Id == groupId, ct);

    public async Task AddGroupAsync(QuizQuestionGroup group, CancellationToken ct = default)
        => await _db.QuizQuestionGroups.AddAsync(group, ct);

    public void RemoveGroup(QuizQuestionGroup group) => _db.QuizQuestionGroups.Remove(group);

    public Task<QuizAttempt?> GetAttemptForDeliveryAsync(Guid attemptId, CancellationToken ct = default)
        => _db.QuizAttempts
              .Include(a => a.Quiz).ThenInclude(q => q.Questions).ThenInclude(q => q.Options)
              .Include(a => a.Items).ThenInclude(i => i.Question).ThenInclude(q => q.Options)
              .Include(a => a.Answers).ThenInclude(an => an.Selections)
              .AsSplitQuery()
              .FirstOrDefaultAsync(a => a.Id == attemptId, ct);

    public async Task AddAttemptItemsAsync(IEnumerable<QuizAttemptItem> items, CancellationToken ct = default)
        => await _db.QuizAttemptItems.AddRangeAsync(items, ct);

    public Task<QuizAnswer?> GetAnswerForGradingAsync(Guid answerId, CancellationToken ct = default)
        => _db.QuizAnswers
              .Include(a => a.Question).ThenInclude(q => q.Options)
              .Include(a => a.Attempt).ThenInclude(at => at.Items)
              .Include(a => a.Attempt).ThenInclude(at => at.Answers)
              .Include(a => a.Attempt).ThenInclude(at => at.Quiz).ThenInclude(q => q.Questions)
              .AsSplitQuery()
              .FirstOrDefaultAsync(a => a.Id == answerId, ct);

    public async Task<IReadOnlyList<QuizAttempt>> GetFinishedAttemptsAsync(Guid quizId, CancellationToken ct = default)
        => await _db.QuizAttempts
              .Include(a => a.Items)
              .Include(a => a.Answers).ThenInclude(an => an.Selections)
              .Where(a => a.QuizId == quizId && (a.Status == AttemptStatus.Submitted || a.Status == AttemptStatus.TimedOut))
              .AsSplitQuery()
              .ToListAsync(ct);

    public async Task<IReadOnlyList<QuizAttempt>> GetFinishedAttemptsForGradebookAsync(Guid courseOfferingId, Guid studentProfileId, CancellationToken ct = default)
        => await _db.QuizAttempts
              .Include(a => a.Items)
              .Include(a => a.Quiz).ThenInclude(q => q.Questions)
              .Where(a => a.StudentProfileId == studentProfileId
                       && a.Quiz.CourseOfferingId == courseOfferingId
                       && a.Status != AttemptStatus.InProgress)
              .AsSplitQuery()
              .ToListAsync(ct);

    public async Task<IReadOnlyList<QuizAnswer>> GetManualGradingQueueAsync(Guid quizId, CancellationToken ct = default)
    {
        var manualTypes = new[] { QuestionType.ShortAnswer, QuestionType.Essay, QuestionType.FileUpload };
        return await _db.QuizAnswers
              .Include(a => a.Question)
              .Include(a => a.Attempt).ThenInclude(at => at.Items)
              .Where(a => a.Attempt.QuizId == quizId
                          && (a.Attempt.Status == AttemptStatus.Submitted || a.Attempt.Status == AttemptStatus.TimedOut)
                          && a.MarksAwarded == null
                          && manualTypes.Contains(a.Question.Type))
              .OrderBy(a => a.Attempt.FinishedAt)
              .AsSplitQuery()
              .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<QuizAccommodation>> GetAccommodationsAsync(Guid courseOfferingId, CancellationToken ct = default)
        => await _db.QuizAccommodations.Where(a => a.CourseOfferingId == courseOfferingId).ToListAsync(ct);

    public Task<QuizAccommodation?> GetAccommodationAsync(Guid id, CancellationToken ct = default)
        => _db.QuizAccommodations.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<QuizAccommodation?> GetEffectiveAccommodationAsync(Guid courseOfferingId, Guid quizId, Guid studentProfileId, CancellationToken ct = default)
    {
        var rows = await _db.QuizAccommodations
            .Where(a => a.CourseOfferingId == courseOfferingId && a.StudentProfileId == studentProfileId && (a.QuizId == quizId || a.QuizId == null))
            .ToListAsync(ct);
        return rows.FirstOrDefault(a => a.QuizId == quizId) ?? rows.FirstOrDefault(a => a.QuizId == null);
    }

    public async Task AddAccommodationAsync(QuizAccommodation accommodation, CancellationToken ct = default)
        => await _db.QuizAccommodations.AddAsync(accommodation, ct);

    public void RemoveAccommodation(QuizAccommodation accommodation) => _db.QuizAccommodations.Remove(accommodation);

    public async Task<IReadOnlyList<QuizAvailabilityOverride>> GetOverridesAsync(Guid quizId, CancellationToken ct = default)
        => await _db.QuizAvailabilityOverrides.Where(o => o.QuizId == quizId).ToListAsync(ct);

    public Task<QuizAvailabilityOverride?> GetOverrideAsync(Guid id, CancellationToken ct = default)
        => _db.QuizAvailabilityOverrides.FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<QuizAvailabilityOverride?> GetOverrideForStudentAsync(Guid quizId, Guid studentProfileId, CancellationToken ct = default)
        => _db.QuizAvailabilityOverrides.FirstOrDefaultAsync(o => o.QuizId == quizId && o.StudentProfileId == studentProfileId, ct);

    public async Task AddOverrideAsync(QuizAvailabilityOverride availabilityOverride, CancellationToken ct = default)
        => await _db.QuizAvailabilityOverrides.AddAsync(availabilityOverride, ct);

    public void RemoveOverride(QuizAvailabilityOverride availabilityOverride) => _db.QuizAvailabilityOverrides.Remove(availabilityOverride);

    public async Task<IReadOnlyDictionary<Guid, string>> GetStudentNamesAsync(IReadOnlyCollection<Guid> studentProfileIds, CancellationToken ct = default)
    {
        if (studentProfileIds.Count == 0) return new Dictionary<Guid, string>();
        var rows = await (from sp in _db.StudentProfiles.IgnoreQueryFilters().AsNoTracking()
                          join u in _db.Users.IgnoreQueryFilters().AsNoTracking() on sp.UserId equals u.Id
                          where studentProfileIds.Contains(sp.Id)
                          select new { sp.Id, Name = u.FullName ?? u.Username, sp.RegistrationNumber })
                         .ToListAsync(ct);
        return rows.ToDictionary(r => r.Id, r => $"{r.Name} ({r.RegistrationNumber})");
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// FypRepository
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// EF Core implementation of <see cref="IFypRepository"/>.
/// Handles FYP project proposals, panel assignments, and meeting scheduling.
/// </summary>
public sealed class FypRepository : IFypRepository
{
    private readonly ApplicationDbContext _db;

    /// <summary>Initialises the repository with the application DbContext.</summary>
    public FypRepository(ApplicationDbContext db) => _db = db;

    // ── Projects ──────────────────────────────────────────────────────────────

    /// <summary>Returns an FYP project by its ID, or null.</summary>
    public Task<FypProject?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.FypProjects.FirstOrDefaultAsync(p => p.Id == id, ct);

    /// <summary>Returns a project with panel members and meetings loaded.</summary>
    public Task<FypProject?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
        => _db.FypProjects
              .Include(p => p.PanelMembers)
              .Include(p => p.Meetings.OrderByDescending(m => m.ScheduledAt))
              .FirstOrDefaultAsync(p => p.Id == id, ct);

    /// <summary>Returns all projects for a student.</summary>
    public async Task<IReadOnlyList<FypProject>> GetByStudentAsync(Guid studentProfileId, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default)
        => await BuildScopedProjectQuery(tenantId, campusId)
            .Where(p => p.StudentProfileId == studentProfileId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    /// <summary>Returns all projects in a department, optionally filtered by status.</summary>
    public async Task<IReadOnlyList<FypProject>> GetByDepartmentAsync(Guid departmentId, FypProjectStatus? status = null, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default)
        => await BuildScopedProjectQuery(tenantId, campusId)
            .Where(p => p.DepartmentId == departmentId && (status == null || p.Status == status))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    /// <summary>Returns all projects across all departments, optionally filtered by status.</summary>
    public async Task<IReadOnlyList<FypProject>> GetAllAsync(FypProjectStatus? status = null, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default)
        => await BuildScopedProjectQuery(tenantId, campusId)
            .Where(p => status == null || p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    /// <summary>Returns all projects supervised by a specific faculty user.</summary>
    public async Task<IReadOnlyList<FypProject>> GetBySupervisorAsync(Guid supervisorUserId, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default)
        => await BuildScopedProjectQuery(tenantId, campusId)
            .Where(p => p.SupervisorUserId == supervisorUserId)
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .ToListAsync(ct);

    /// <summary>Returns student eligibility and scope details for FYP validation.</summary>
    public async Task<FypStudentEligibility?> GetStudentEligibilityAsync(Guid studentProfileId, CancellationToken ct = default)
    {
        return await _db.StudentProfiles
            .AsNoTracking()
            .Where(sp => sp.Id == studentProfileId)
            .Join(
                _db.Departments.AsNoTracking(),
                sp => sp.DepartmentId,
                d => d.Id,
                (sp, d) => new { sp, d })
            .Join(
                _db.AcademicPrograms.AsNoTracking(),
                x => x.sp.ProgramId,
                p => p.Id,
                (x, p) => new FypStudentEligibility(
                    x.sp.Id,
                    x.sp.DepartmentId,
                    x.d.InstitutionType == InstitutionType.University,
                    x.sp.CurrentSemesterNumber,
                    p.TotalSemesters,
                    x.d.TenantId,
                    x.d.CampusId))
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>Returns tenant/campus scope for a project.</summary>
    public async Task<(Guid? TenantId, Guid? CampusId)?> GetProjectScopeAsync(Guid projectId, CancellationToken ct = default)
    {
        var scope = await _db.FypProjects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Join(
                _db.Departments.AsNoTracking(),
                p => p.DepartmentId,
                d => d.Id,
                (_, d) => new { d.TenantId, d.CampusId })
            .FirstOrDefaultAsync(ct);

        return scope is null ? null : (scope.TenantId, scope.CampusId);
    }

    /// <summary>Returns tenant/campus scope for a department.</summary>
    public async Task<(Guid? TenantId, Guid? CampusId)?> GetDepartmentScopeAsync(Guid departmentId, CancellationToken ct = default)
    {
        var scope = await _db.Departments
            .AsNoTracking()
            .Where(d => d.Id == departmentId)
            .Select(d => new { d.TenantId, d.CampusId })
            .FirstOrDefaultAsync(ct);

        return scope is null ? null : (scope.TenantId, scope.CampusId);
    }

    /// <summary>Returns tenant/campus scope for a student profile.</summary>
    public async Task<(Guid? TenantId, Guid? CampusId)?> GetStudentScopeAsync(Guid studentProfileId, CancellationToken ct = default)
    {
        var scope = await _db.StudentProfiles
            .AsNoTracking()
            .Where(sp => sp.Id == studentProfileId)
            .Join(
                _db.Departments.AsNoTracking(),
                sp => sp.DepartmentId,
                d => d.Id,
                (_, d) => new { d.TenantId, d.CampusId })
            .FirstOrDefaultAsync(ct);

        return scope is null ? null : (scope.TenantId, scope.CampusId);
    }

    /// <summary>Queues a new project for insertion.</summary>
    public async Task AddAsync(FypProject project, CancellationToken ct = default)
        => await _db.FypProjects.AddAsync(project, ct);

    /// <summary>Marks a project entity as modified.</summary>
    public void Update(FypProject project) => _db.FypProjects.Update(project);

    // ── Panel Members ─────────────────────────────────────────────────────────

    /// <summary>Returns panel members for a given project.</summary>
    public Task<IReadOnlyList<FypPanelMember>> GetPanelMembersAsync(Guid projectId, CancellationToken ct = default)
        => _db.FypPanelMembers
              .Where(m => m.FypProjectId == projectId)
              .ToListAsync(ct)
              .ContinueWith<IReadOnlyList<FypPanelMember>>(t => t.Result, ct);

    /// <summary>Returns a panel member by project and user IDs, or null.</summary>
    public Task<FypPanelMember?> GetPanelMemberAsync(Guid projectId, Guid userId, CancellationToken ct = default)
        => _db.FypPanelMembers
              .FirstOrDefaultAsync(m => m.FypProjectId == projectId && m.UserId == userId, ct);

    /// <summary>Queues a panel member for insertion.</summary>
    public async Task AddPanelMemberAsync(FypPanelMember member, CancellationToken ct = default)
        => await _db.FypPanelMembers.AddAsync(member, ct);

    /// <summary>Removes a panel member from the context.</summary>
    public void RemovePanelMember(FypPanelMember member) => _db.FypPanelMembers.Remove(member);

    // ── Meetings ──────────────────────────────────────────────────────────────

    /// <summary>Returns a meeting by its ID, or null.</summary>
    public Task<FypMeeting?> GetMeetingByIdAsync(Guid meetingId, CancellationToken ct = default)
        => _db.FypMeetings.FirstOrDefaultAsync(m => m.Id == meetingId, ct);

    /// <summary>Returns all meetings for a project ordered by scheduled time ascending.</summary>
    public Task<IReadOnlyList<FypMeeting>> GetMeetingsByProjectAsync(Guid projectId, CancellationToken ct = default)
        => _db.FypMeetings
              .Where(m => m.FypProjectId == projectId)
              .OrderBy(m => m.ScheduledAt)
              .ToListAsync(ct)
              .ContinueWith<IReadOnlyList<FypMeeting>>(t => t.Result, ct);

    /// <summary>Returns upcoming scheduled meetings organised by a supervisor.</summary>
    public Task<IReadOnlyList<FypMeeting>> GetUpcomingMeetingsAsync(Guid supervisorUserId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return _db.FypMeetings
                  .Where(m => m.OrganiserUserId == supervisorUserId
                           && m.Status == MeetingStatus.Scheduled
                           && m.ScheduledAt >= now)
                  .OrderBy(m => m.ScheduledAt)
                  .ToListAsync(ct)
                  .ContinueWith<IReadOnlyList<FypMeeting>>(t => t.Result, ct);
    }

    /// <summary>Queues a new meeting for insertion.</summary>
    public async Task AddMeetingAsync(FypMeeting meeting, CancellationToken ct = default)
        => await _db.FypMeetings.AddAsync(meeting, ct);

    /// <summary>Marks a meeting entity as modified.</summary>
    public void UpdateMeeting(FypMeeting meeting) => _db.FypMeetings.Update(meeting);

    /// <summary>Commits all pending changes.</summary>
    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    private IQueryable<FypProject> BuildScopedProjectQuery(Guid? tenantId, Guid? campusId)
    {
        var query = _db.FypProjects
            .Include(p => p.PanelMembers)
            .AsQueryable();

        if (tenantId.HasValue || campusId.HasValue)
        {
            var scopedDepartmentIds = _db.Departments
                .AsNoTracking()
                .Where(d => (!tenantId.HasValue || d.TenantId == tenantId.Value)
                         && (!campusId.HasValue || d.CampusId == campusId.Value))
                .Select(d => d.Id);

            query = query.Where(p => scopedDepartmentIds.Contains(p.DepartmentId));
        }

        return query;
    }
}
