using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Lms;
using Tabsan.EduSphere.Domain.Lti;
using Tabsan.EduSphere.Domain.Outcomes;
using Tabsan.EduSphere.Domain.Quizzes;
using P = Tabsan.EduSphere.Application.Lms.CourseCopyParts;

namespace Tabsan.EduSphere.Application.Lms;

/// <summary>The new content a copy made, how much of each kind, and what was left out.</summary>
public sealed class CourseCopyOutcome
{
    public CourseContent Copy { get; } = new();
    public Dictionary<string, int> Copied { get; } = [];
    public List<CourseCopyNoteDto> NotCopied { get; } = [];
}

/// <summary>
/// Builds a copy of a course's content for another offering. Nothing of the students' is copied (submissions, grades,
/// attempts, groups' members, extensions, progress); only what the teacher made. Copies point at each other: a module
/// item opens the copied quiz, a quiz draws from the copied bank, a rubric belongs to the copied assignment.
/// <para>A chosen module brings along what its items open, and a chosen quiz the course's banks it draws from, so
/// nothing in the copy points back at the old course. Every date moves by <see cref="CourseCopyPlan.ShiftDays"/>.
/// Unless <see cref="CourseCopyPlan.KeepPublished"/>, everything arrives hidden from students.</para>
/// </summary>
public static class CourseContentCloner
{
    public static CourseCopyOutcome Clone(CourseContent source, CourseCopyPlan plan)
    {
        var outcome = new CourseCopyOutcome();
        new Cloning(source, plan, outcome).Run();
        return outcome;
    }

    private sealed class Cloning(CourseContent src, CourseCopyPlan plan, CourseCopyOutcome outcome)
    {
        private readonly CourseContent _copy = outcome.Copy;
        private readonly Guid _target = plan.Target.OfferingId;
        private readonly Guid _user = plan.UserId;

        private readonly Dictionary<Guid, Guid> _modules = [];
        private readonly Dictionary<Guid, Guid> _videos = [];
        private readonly Dictionary<Guid, Guid?> _groupSets = [];
        private readonly Dictionary<Guid, Guid> _assignments = [];
        private readonly Dictionary<Guid, Guid> _criteria = [];
        private readonly Dictionary<Guid, Guid> _banks = [];
        private readonly Dictionary<Guid, Guid> _questions = [];
        private readonly Dictionary<Guid, Guid> _quizzes = [];
        private readonly Dictionary<Guid, Guid> _materials = [];
        private readonly Dictionary<Guid, Guid> _tools = [];
        private int _materialsCreated;

        public void Run()
        {
            // ── What to copy: the chosen items, plus what they need ──
            var modules = src.Modules.Where(m => Chosen(P.Modules, m.Id)).OrderBy(m => m.WeekNumber).ThenBy(m => m.CreatedAt).ToList();
            var moduleIds = modules.Select(m => m.Id).ToHashSet();
            var items = src.Items.Where(i => moduleIds.Contains(i.ModuleId)).OrderBy(i => i.Position).ToList();

            var assignmentIds = src.Assignments.Where(a => !a.IsCompletedElsewhere && Chosen(P.Assignments, a.Id)).Select(a => a.Id).ToHashSet();
            var quizIds = src.Quizzes.Where(q => Chosen(P.Quizzes, q.Id)).Select(q => q.Id).ToHashSet();
            var bankIds = src.Banks.Where(b => Chosen(P.QuestionBanks, b.Id)).Select(b => b.Id).ToHashSet();
            var materialIds = src.Materials.Where(m => m.SemesterId == plan.Source.SemesterId && m.CourseId == plan.Source.CourseId && Chosen(P.Files, m.Id))
                .Select(m => m.Id).ToHashSet();
            var toolIds = src.ToolLinks.Where(l => Chosen(P.ExternalTools, l.Id)).Select(l => l.Id).ToHashSet();

            foreach (var item in items)
            {
                if (item.TargetId is not { } t) continue;
                switch (item.ItemType)
                {
                    case ModuleItemType.Assignment:
                        if (src.Assignments.FirstOrDefault(a => a.Id == t) is { ExternalToolId: not null } toolAssignment)
                        {
                            if (ToolLinkOf(toolAssignment.Id) is { } link) toolIds.Add(link.Id);
                        }
                        else assignmentIds.Add(t);
                        break;
                    case ModuleItemType.Quiz: quizIds.Add(t); break;
                    case ModuleItemType.Material: materialIds.Add(t); break;
                    case ModuleItemType.ExternalTool: toolIds.Add(t); break;
                }
            }
            foreach (var group in src.QuestionGroups.Where(g => quizIds.Contains(g.QuizId)))
                if (src.Banks.Any(b => b.Id == group.QuestionBankId)) bankIds.Add(group.QuestionBankId);

            // ── Copy, in the order the links need ──
            CopyModules(modules);
            CopyAssignments(src.Assignments.Where(a => assignmentIds.Contains(a.Id) && !a.IsCompletedElsewhere).OrderBy(a => a.DueDate).ToList());
            CopyRubrics();
            CopyBanks(src.Banks.Where(b => bankIds.Contains(b.Id)).ToList());
            CopyQuizzes(src.Quizzes.Where(q => quizIds.Contains(q.Id)).ToList());
            CopyMaterials(src.Materials.Where(m => materialIds.Contains(m.Id)).ToList());
            CopyToolLinks(src.ToolLinks.Where(l => toolIds.Contains(l.Id)).ToList());
            CopyItems(items);
            CopySettings();
            CopyAlignments();

            foreach (var package in src.ScormPackages.Where(p => !p.IsRemoved))
                outcome.NotCopied.Add(new(CourseCopyNotes.Scorm, package.Title));

            Count(P.Modules, _copy.Modules.Count);
            Count("items", _copy.Items.Count);
            Count("videos", _copy.Videos.Count);
            Count(P.Assignments, _copy.Assignments.Count);
            Count("rubrics", _copy.Rubrics.Count);
            Count(P.Quizzes, _copy.Quizzes.Count);
            Count("questions", _copy.Questions.Count);
            Count(P.QuestionBanks, _copy.Banks.Count);
            Count(P.Files, _materialsCreated);
            Count(P.ExternalTools, _copy.ToolLinks.Count);
            Count(P.Settings, _copy.LatePolicy is null ? 0 : 1);
            Count("outcomes", _copy.Alignments.Count);
        }

        private bool Chosen(string part, Guid id) => plan.Parts.Contains(part) && (plan.ItemIds is null || plan.ItemIds.Contains(id));

        private DateTime Shift(DateTime d) => d.AddDays(plan.ShiftDays);
        private DateTime? Shift(DateTime? d) => d?.AddDays(plan.ShiftDays);
        private bool Publish(bool wasPublished) => plan.KeepPublished && wasPublished;

        private void Count(string key, int n) { if (n > 0) outcome.Copied[key] = n; }

        private void Skip(string reason, string? title) => outcome.NotCopied.Add(new(reason, title));

        private LtiResourceLink? ToolLinkOf(Guid assignmentId)
            => src.LineItems.FirstOrDefault(l => l.AssignmentId == assignmentId && l.ResourceLinkId is not null) is { } line
                ? src.ToolLinks.FirstOrDefault(t => t.Id == line.ResourceLinkId)
                : null;

        private void CopyModules(List<CourseContentModule> modules)
        {
            foreach (var m in modules)
            {
                var copy = new CourseContentModule(_target, m.Title, m.WeekNumber, m.Body);
                copy.ConfigureRelease(Shift(m.UnlockAt), m.RequireSequentialProgress, m.CompletionMode);
                if (Publish(m.IsPublished)) copy.Publish();
                _modules[m.Id] = copy.Id;
                _copy.Modules.Add(copy);
            }
            foreach (var v in src.Videos.Where(v => _modules.ContainsKey(v.ModuleId)))
            {
                var copy = new ContentVideo(_modules[v.ModuleId], v.Title, v.StorageUrl, v.EmbedUrl, v.DurationSeconds);
                _videos[v.Id] = copy.Id;
                _copy.Videos.Add(copy);
            }
            foreach (var p in src.Prerequisites)
                if (_modules.TryGetValue(p.ModuleId, out var module) && _modules.TryGetValue(p.PrerequisiteModuleId, out var prerequisite))
                    _copy.Prerequisites.Add(new ModulePrerequisite(module, prerequisite));
        }

        private Guid? GroupSetFor(Guid groupSetId)
        {
            if (_groupSets.TryGetValue(groupSetId, out var known)) return known;
            Guid? id = null;
            if (src.GroupSets.FirstOrDefault(g => g.Id == groupSetId) is { } set)
            {
                // The set's name and rules only: groups are made for each class.
                var copy = new GroupSet(_target, set.Name, set.SelfSignup, set.MaxSize);
                _copy.GroupSets.Add(copy);
                id = copy.Id;
            }
            return _groupSets[groupSetId] = id;
        }

        private void CopyAssignments(List<Assignment> assignments)
        {
            foreach (var a in assignments)
            {
                try
                {
                    var copy = new Assignment(_target, a.Title, a.Description, Shift(a.DueDate), a.MaxMarks);
                    copy.ConfigureSubmissions(a.AllowedAttempts, Assignment.ParseFileTypes(a.AllowedFileTypes), a.MaxFileSizeMb);
                    copy.ConfigureAvailability(Shift(a.AvailableFrom), Shift(a.AvailableUntil));
                    copy.ConfigureGrading(a.AnonymousGrading, a.PostingPolicy, a.ModeratedGrading, a.ModeratorUserId);
                    copy.ConfigureGroups(a.GroupSetId is { } set ? GroupSetFor(set) : null, a.GradeGroupIndividually);
                    copy.ConfigurePeerReview(a.PeerReviewCount, Shift(a.PeerReviewDueDate), a.PeerReviewAnonymous);
                    copy.ConfigureSimilarity(a.SimilarityCheckEnabled, a.SimilarityVisibleToStudents);
                    if (Publish(a.IsPublished)) copy.Publish();
                    _assignments[a.Id] = copy.Id;
                    _copy.Assignments.Add(copy);
                }
                catch (ArgumentException) { Skip(CourseCopyNotes.Invalid, a.Title); }
            }
        }

        private void CopyRubrics()
        {
            foreach (var r in src.Rubrics.Where(r => r.IsActive && _assignments.ContainsKey(r.AssignmentId)))
            {
                var copy = Rubric.Create(_assignments[r.AssignmentId], r.Title, _user);
                foreach (var c in src.Criteria.Where(c => c.RubricId == r.Id).OrderBy(c => c.DisplayOrder))
                {
                    var criterion = copy.AddCriterion(c.Name, c.MaxPoints, c.DisplayOrder);
                    _criteria[c.Id] = criterion.Id;
                    _copy.Criteria.Add(criterion);
                    foreach (var l in src.Levels.Where(l => l.CriterionId == c.Id).OrderBy(l => l.DisplayOrder))
                        _copy.Levels.Add(criterion.AddLevel(l.Label, l.PointsAwarded, l.DisplayOrder));
                }
                _copy.Rubrics.Add(copy);
            }
        }

        private void CopyOptions(QuizQuestion from, QuizQuestion to)
        {
            foreach (var o in src.Options.Where(o => o.QuizQuestionId == from.Id).OrderBy(o => o.OrderIndex))
                _copy.Options.Add(new QuizOption(to.Id, o.Text, o.IsCorrect, o.OrderIndex));
        }

        private void CopyBanks(List<QuestionBank> banks)
        {
            foreach (var b in banks)
            {
                var copy = new QuestionBank(b.TenantId, _target, b.DepartmentId, b.Title, b.Description, b.IsShared, _user);
                _banks[b.Id] = copy.Id;
                _copy.Banks.Add(copy);
                foreach (var q in src.Questions.Where(q => q.QuestionBankId == b.Id && !q.IsArchived).OrderBy(q => q.OrderIndex))
                {
                    var question = QuizQuestion.ForBank(copy.Id, q.Text, q.Type, q.Marks, q.OrderIndex, q.ConfigJson, q.Tags, q.Difficulty);
                    _questions[q.Id] = question.Id;
                    _copy.Questions.Add(question);
                    CopyOptions(q, question);
                }
            }
        }

        private void CopyQuizzes(List<Quiz> quizzes)
        {
            foreach (var q in quizzes)
            {
                var copy = new Quiz(_target, q.Title, _user, q.Instructions, q.TimeLimitMinutes, q.MaxAttempts, Shift(q.AvailableFrom), Shift(q.AvailableUntil));
                copy.ConfigureDelivery(q.ShuffleQuestions, q.ShuffleOptions, q.OneQuestionAtATime, q.LockAnsweredQuestions, q.AccessCode,
                    q.AllowedIpRanges, q.ReviewVisibility, q.ReviewAfterCloseOnly);
                if (Publish(q.IsPublished)) copy.Publish();
                _quizzes[q.Id] = copy.Id;
                _copy.Quizzes.Add(copy);
                foreach (var question in src.Questions.Where(x => x.QuizId == q.Id && !x.IsArchived).OrderBy(x => x.OrderIndex))
                {
                    var clone = question.CloneToQuiz(copy.Id, question.OrderIndex);
                    _questions[question.Id] = clone.Id;
                    _copy.Questions.Add(clone);
                    CopyOptions(question, clone);
                }
                // Draw from the copied bank where the course's own bank was copied; shared and department banks stay as they are.
                foreach (var g in src.QuestionGroups.Where(g => g.QuizId == q.Id).OrderBy(g => g.OrderIndex))
                    _copy.QuestionGroups.Add(new QuizQuestionGroup(copy.Id, _banks.GetValueOrDefault(g.QuestionBankId, g.QuestionBankId), g.Title,
                        g.PickCount, g.MarksPerQuestion, g.OrderIndex, g.TagFilter));
            }
        }

        private void CopyMaterials(List<CourseMaterial> materials)
        {
            foreach (var m in materials)
            {
                // Files of the same course and term are already there (another section of the same class).
                if (m.CourseId == plan.Target.CourseId && m.SemesterId == plan.Target.SemesterId) { _materials[m.Id] = m.Id; continue; }
                try
                {
                    // The stored file is shared: course files are never deleted, only hidden.
                    var copy = new CourseMaterial(plan.Target.TenantId ?? m.TenantId, plan.Target.CampusId ?? m.CampusId, plan.Target.DepartmentId,
                        m.AcademicProgramId, plan.Target.SemesterId, plan.Target.CourseId, m.Name, _user, m.MaterialType, m.LinkUrl, m.FilePath, m.Description);
                    _materials[m.Id] = copy.Id;
                    _copy.Materials.Add(copy);
                    _materialsCreated++;
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { Skip(CourseCopyNotes.Invalid, m.Name); }
            }
        }

        private void CopyToolLinks(List<LtiResourceLink> links)
        {
            foreach (var l in links)
            {
                Guid? module = l.ModuleId is { } m && _modules.TryGetValue(m, out var copiedModule) ? copiedModule : null;
                var copy = new LtiResourceLink(l.ToolId, l.DeploymentRowId, _target, module, l.Title, l.Description, l.Url, l.CustomParameters,
                    l.PresentationTarget, _user);
                copy.SetVisible(l.IsVisible);
                // A graded tool brings its gradebook column (an assignment the tool posts scores to).
                if (l.LineItemId is { } lineId && src.LineItems.FirstOrDefault(x => x.Id == lineId) is { } line
                    && src.Assignments.FirstOrDefault(a => a.Id == line.AssignmentId) is { } graded)
                {
                    var assignment = Assignment.ForExternalTool(_target, graded.Title, graded.MaxMarks, Shift(graded.DueDate), graded.ExternalToolId ?? l.ToolId);
                    if (!Publish(graded.IsPublished)) assignment.Retract();
                    _assignments[graded.Id] = assignment.Id;
                    _copy.Assignments.Add(assignment);
                    var lineCopy = new LtiLineItem(line.ToolId, _target, copy.Id, assignment.Id, line.Label, line.ScoreMaximum, line.Tag, line.ResourceId,
                        Shift(line.StartDateTime), Shift(line.EndDateTime));
                    copy.AttachLineItem(lineCopy.Id);
                    _copy.LineItems.Add(lineCopy);
                }
                _tools[l.Id] = copy.Id;
                _copy.ToolLinks.Add(copy);
            }
        }

        private void CopyItems(List<ModuleItem> items)
        {
            foreach (var item in items)
            {
                Guid? target = null;
                if (item.TargetId is { } t)
                {
                    target = item.ItemType switch
                    {
                        ModuleItemType.Video => _videos.TryGetValue(t, out var v) ? v : null,
                        ModuleItemType.Material => _materials.TryGetValue(t, out var m) ? m : null,
                        ModuleItemType.Assignment => _assignments.TryGetValue(t, out var a) ? a : null,
                        ModuleItemType.Quiz => _quizzes.TryGetValue(t, out var q) ? q : null,
                        ModuleItemType.ExternalTool => _tools.TryGetValue(t, out var l) ? l : null,
                        _ => null
                    };
                }
                if (ModuleItem.NeedsTarget(item.ItemType) && target is null)
                {
                    var reason = item.ItemType switch
                    {
                        ModuleItemType.Discussion => CourseCopyNotes.Discussion,
                        ModuleItemType.ScormPackage => CourseCopyNotes.Scorm,
                        ModuleItemType.Assignment when src.Assignments.Any(a => a.Id == item.TargetId && a.ScormPackageId is not null) => CourseCopyNotes.Scorm,
                        _ => CourseCopyNotes.Missing
                    };
                    // SCORM packages are listed once each, below; their items need no second line.
                    if (reason != CourseCopyNotes.Scorm) Skip(reason, item.Title);
                    continue;
                }
                try
                {
                    var copy = new ModuleItem(_modules[item.ModuleId], _target, item.ItemType, target, item.Title, item.Url, item.Body, item.Position, _user);
                    copy.Update(item.Title, item.Url, item.Body, item.IndentLevel);
                    copy.SetRequirement(item.RequirementType, item.MinScorePercent);
                    copy.SetPublished(item.IsPublished);
                    _copy.Items.Add(copy);
                }
                catch (ArgumentException) { Skip(CourseCopyNotes.Invalid, item.Title); }
            }
        }

        private void CopySettings()
        {
            if (!plan.Parts.Contains(P.Settings) || src.LatePolicy is not { } p) return;
            var copy = new LatePolicy(_target);
            copy.Configure(p.LateDeductionEnabled, p.DeductionPercent, p.Interval, p.LowestGradePercent, p.MissingEnabled, p.MissingGradePercent);
            _copy.LatePolicy = copy;
        }

        private void CopyAlignments()
        {
            var otherCourse = 0;
            foreach (var al in src.Alignments)
            {
                Guid? target = al.TargetType switch
                {
                    OutcomeTargetType.Assignment => _assignments.TryGetValue(al.TargetId, out var a) ? a : null,
                    OutcomeTargetType.RubricCriterion => _criteria.TryGetValue(al.TargetId, out var c) ? c : null,
                    OutcomeTargetType.QuizQuestion or OutcomeTargetType.BankQuestion => _questions.TryGetValue(al.TargetId, out var q) ? q : null,
                    _ => null
                };
                if (target is null) continue;
                // A course's own outcomes belong to that course; program and school outcomes apply anywhere.
                if (src.OutcomeCourses.TryGetValue(al.OutcomeId, out var course) && course is { } c2 && c2 != plan.Target.CourseId) { otherCourse++; continue; }
                _copy.Alignments.Add(new OutcomeAlignment(al.OutcomeId, al.TargetType, target.Value,
                    al.TargetType == OutcomeTargetType.BankQuestion ? null : _target, _user));
            }
            if (otherCourse > 0) outcome.NotCopied.Add(new(CourseCopyNotes.OtherCourseOutcomes, null, otherCourse));
        }
    }
}
