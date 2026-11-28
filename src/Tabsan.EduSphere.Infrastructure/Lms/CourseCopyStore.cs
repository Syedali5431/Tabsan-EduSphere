using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Lms;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Lms;
using Tabsan.EduSphere.Domain.Outcomes;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Lms;

/// <summary>Reads a course's content for copying and saves the copy in one go (see <see cref="CourseCopyService"/>).</summary>
public sealed class CourseCopyStore(ApplicationDbContext db) : ICourseCopyStore
{
    public Task<CourseCopyOffering?> OfferingAsync(Guid offeringId, CancellationToken ct = default)
        => Project(db.CourseOfferings.AsNoTracking().Where(o => o.Id == offeringId)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<CourseCopyOffering>> OfferingsAsync(Guid? tenantId, IReadOnlyCollection<Guid>? departmentIds, Guid? teacherUserId, int limit,
        CancellationToken ct = default)
    {
        var query = db.CourseOfferings.AsNoTracking().Where(o => o.TenantId == tenantId);
        if (departmentIds is not null)
        {
            var departments = departmentIds.ToList();
            query = query.Where(o => departments.Contains(o.Course.DepartmentId) || (teacherUserId != null && o.FacultyUserId == teacherUserId));
        }
        return await Project(query.OrderByDescending(o => o.Semester.StartDate).ThenBy(o => o.Course.Code).Take(limit)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Guid>> DepartmentsOfAsync(Guid userId, string role, CancellationToken ct = default)
    {
        if (CourseCopyService.IsAdmin(role))
            return await db.AdminDepartmentAssignments.AsNoTracking().Where(a => a.AdminUserId == userId && a.RemovedAt == null).Select(a => a.DepartmentId).ToListAsync(ct);
        if (CourseCopyService.IsFaculty(role))
            return await db.FacultyDepartmentAssignments.AsNoTracking().Where(a => a.FacultyUserId == userId && a.RemovedAt == null).Select(a => a.DepartmentId).ToListAsync(ct);
        return [];
    }

    public Task<CourseTemplate?> TemplateAsync(Guid offeringId, CancellationToken ct = default)
        => db.CourseTemplates.FirstOrDefaultAsync(t => t.OfferingId == offeringId, ct);

    public async Task<IReadOnlyList<(CourseTemplate Template, CourseCopyOffering Offering)>> TemplatesAsync(Guid? tenantId, CancellationToken ct = default)
    {
        var templates = await db.CourseTemplates.AsNoTracking()
            .Where(t => db.CourseOfferings.Any(o => o.Id == t.OfferingId && o.TenantId == tenantId))
            .ToListAsync(ct);
        if (templates.Count == 0) return [];
        var ids = templates.Select(t => t.OfferingId).ToList();
        var offerings = await Project(db.CourseOfferings.AsNoTracking().Where(o => ids.Contains(o.Id))).ToListAsync(ct);
        return templates.Join(offerings, t => t.OfferingId, o => o.OfferingId, (t, o) => (t, o)).ToList();
    }

    public void AddTemplate(CourseTemplate template) => db.CourseTemplates.Add(template);

    public void RemoveTemplate(CourseTemplate template) => db.CourseTemplates.Remove(template);

    public async Task<CourseContent> ReadAsync(CourseCopyOffering offering, CancellationToken ct = default)
    {
        var id = offering.OfferingId;
        var c = new CourseContent();

        c.Modules.AddRange(await db.CourseContentModules.AsNoTracking().Where(m => m.OfferingId == id).ToListAsync(ct));
        var moduleIds = c.Modules.Select(m => m.Id).ToList();
        c.Videos.AddRange(await db.ContentVideos.AsNoTracking().Where(v => moduleIds.Contains(v.ModuleId)).ToListAsync(ct));
        c.Items.AddRange(await db.ModuleItems.AsNoTracking().Where(i => i.OfferingId == id).ToListAsync(ct));
        c.Prerequisites.AddRange(await db.ModulePrerequisites.AsNoTracking().Where(p => moduleIds.Contains(p.ModuleId)).ToListAsync(ct));
        c.Materials.AddRange(await db.CourseMaterials.AsNoTracking().Where(m => m.CourseId == offering.CourseId && m.IsActive).ToListAsync(ct));

        c.Assignments.AddRange(await db.Assignments.AsNoTracking().Where(a => a.CourseOfferingId == id).ToListAsync(ct));
        var assignmentIds = c.Assignments.Select(a => a.Id).ToList();
        c.Rubrics.AddRange(await db.Rubrics.AsNoTracking().Where(r => assignmentIds.Contains(r.AssignmentId) && r.IsActive).ToListAsync(ct));
        var rubricIds = c.Rubrics.Select(r => r.Id).ToList();
        c.Criteria.AddRange(await db.RubricCriteria.AsNoTracking().Where(x => rubricIds.Contains(x.RubricId)).ToListAsync(ct));
        var criterionIds = c.Criteria.Select(x => x.Id).ToList();
        c.Levels.AddRange(await db.RubricLevels.AsNoTracking().Where(l => criterionIds.Contains(l.CriterionId)).ToListAsync(ct));
        c.GroupSets.AddRange(await db.GroupSets.AsNoTracking().Where(g => g.CourseOfferingId == id).ToListAsync(ct));
        c.LatePolicy = await db.LatePolicies.AsNoTracking().FirstOrDefaultAsync(p => p.CourseOfferingId == id, ct);

        c.Quizzes.AddRange(await db.Quizzes.AsNoTracking().Where(q => q.CourseOfferingId == id).ToListAsync(ct));
        c.Banks.AddRange(await db.QuestionBanks.AsNoTracking().Where(b => b.CourseOfferingId == id).ToListAsync(ct));
        var quizIds = c.Quizzes.Select(q => q.Id).ToList();
        var bankIds = c.Banks.Select(b => b.Id).ToList();
        c.Questions.AddRange(await db.QuizQuestions.AsNoTracking()
            .Where(q => (q.QuizId != null && quizIds.Contains(q.QuizId.Value)) || (q.QuestionBankId != null && bankIds.Contains(q.QuestionBankId.Value)))
            .ToListAsync(ct));
        var questionIds = c.Questions.Select(q => q.Id).ToList();
        c.Options.AddRange(await db.QuizOptions.AsNoTracking().Where(o => questionIds.Contains(o.QuizQuestionId)).ToListAsync(ct));
        c.QuestionGroups.AddRange(await db.QuizQuestionGroups.AsNoTracking().Where(g => quizIds.Contains(g.QuizId)).ToListAsync(ct));

        c.ToolLinks.AddRange(await db.LtiResourceLinks.AsNoTracking().Where(l => l.CourseOfferingId == id).ToListAsync(ct));
        c.LineItems.AddRange(await db.LtiLineItems.AsNoTracking().Where(l => l.CourseOfferingId == id).ToListAsync(ct));

        var bankQuestionIds = c.Questions.Where(q => q.QuestionBankId != null).Select(q => q.Id).ToList();
        c.Alignments.AddRange(await db.OutcomeAlignments.AsNoTracking()
            .Where(a => a.CourseOfferingId == id || (a.TargetType == OutcomeTargetType.BankQuestion && bankQuestionIds.Contains(a.TargetId)))
            .ToListAsync(ct));
        var outcomeIds = c.Alignments.Select(a => a.OutcomeId).Distinct().ToList();
        foreach (var o in await db.Outcomes.AsNoTracking().Where(o => outcomeIds.Contains(o.Id)).Select(o => new { o.Id, o.Level, o.CourseId }).ToListAsync(ct))
            c.OutcomeCourses[o.Id] = o.Level == OutcomeLevel.Course ? o.CourseId : null;

        c.ScormPackages.AddRange(await db.ScormPackages.AsNoTracking().Where(p => p.OfferingId == id && !p.IsRemoved).ToListAsync(ct));
        foreach (var t in await db.DiscussionThreads.AsNoTracking().Where(t => t.OfferingId == id).Select(t => new { t.Id, t.Title }).ToListAsync(ct))
            c.DiscussionTitles[t.Id] = t.Title;
        return c;
    }

    public async Task SaveCopyAsync(CourseContent copy, CourseCopyRun run, CancellationToken ct = default)
    {
        db.CourseContentModules.AddRange(copy.Modules);
        db.ContentVideos.AddRange(copy.Videos);
        db.ModulePrerequisites.AddRange(copy.Prerequisites);
        db.CourseMaterials.AddRange(copy.Materials);
        db.GroupSets.AddRange(copy.GroupSets);
        db.Assignments.AddRange(copy.Assignments);
        db.Rubrics.AddRange(copy.Rubrics);
        db.RubricCriteria.AddRange(copy.Criteria);
        db.RubricLevels.AddRange(copy.Levels);
        db.QuestionBanks.AddRange(copy.Banks);
        db.Quizzes.AddRange(copy.Quizzes);
        db.QuizQuestions.AddRange(copy.Questions);
        db.QuizOptions.AddRange(copy.Options);
        db.QuizQuestionGroups.AddRange(copy.QuestionGroups);
        db.LtiResourceLinks.AddRange(copy.ToolLinks);
        db.LtiLineItems.AddRange(copy.LineItems);
        db.ModuleItems.AddRange(copy.Items);
        db.OutcomeAlignments.AddRange(copy.Alignments);
        if (copy.LatePolicy is { } policy)
        {
            // One policy per course: an existing one takes the copied settings.
            if (await db.LatePolicies.FirstOrDefaultAsync(p => p.CourseOfferingId == policy.CourseOfferingId, ct) is { } existing)
                existing.Configure(policy.LateDeductionEnabled, policy.DeductionPercent, policy.Interval, policy.LowestGradePercent,
                    policy.MissingEnabled, policy.MissingGradePercent);
            else db.LatePolicies.Add(policy);
        }
        db.CourseCopyRuns.Add(run);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<(CourseCopyRun Run, string CopiedBy)>> RunsAsync(Guid targetOfferingId, int limit, CancellationToken ct = default)
    {
        var rows = await db.CourseCopyRuns.AsNoTracking()
            .Where(r => r.TargetOfferingId == targetOfferingId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .Select(r => new { Run = r, Name = db.Users.IgnoreQueryFilters().Where(u => u.Id == r.CopiedByUserId).Select(u => u.FullName ?? u.Username).FirstOrDefault() })
            .ToListAsync(ct);
        return rows.Select(r => (r.Run, r.Name ?? "")).ToList();
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    private IQueryable<CourseCopyOffering> Project(IQueryable<CourseOffering> offerings)
        => offerings.Select(o => new CourseCopyOffering(
            o.Id, o.CourseId, o.Course.Code, o.Course.Title, o.Course.DepartmentId, o.TenantId, o.CampusId,
            o.SemesterId, o.Semester.Name, o.Semester.StartDate, o.FacultyUserId,
            db.Users.IgnoreQueryFilters().Where(u => u.Id == o.FacultyUserId).Select(u => u.FullName ?? u.Username).FirstOrDefault()));
}
