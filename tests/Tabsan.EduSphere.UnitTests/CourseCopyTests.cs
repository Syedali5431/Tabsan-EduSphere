using Tabsan.EduSphere.Application.Lms;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Lms;
using Tabsan.EduSphere.Domain.Lti;
using Tabsan.EduSphere.Domain.Outcomes;
using Tabsan.EduSphere.Domain.Quizzes;
using P = Tabsan.EduSphere.Application.Lms.CourseCopyParts;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>
/// Course copy: copies point at each other (module items, rubrics, bank draws, tool columns), a module brings what its items
/// open, dates move together, copies arrive hidden unless published state is kept, files of the same term are reused,
/// SCORM and discussions are reported, other courses' outcomes are left out. Access: who looks after which course, templates
/// for a department or the whole school, never across schools; the API's checks on parts, dates and empty copies.
/// </summary>
public class CourseCopyTests
{
    private static readonly Guid Teacher = Guid.NewGuid(), Tenant = Guid.NewGuid(), Campus = Guid.NewGuid(), Dept = Guid.NewGuid(), OtherDept = Guid.NewGuid(),
        Course = Guid.NewGuid(), OldTerm = Guid.NewGuid(), NewTerm = Guid.NewGuid();
    private static readonly DateTime OldStart = new(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc), NewStart = new(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc);

    private static CourseCopyOffering Offering(Guid? id = null, Guid? term = null, DateTime? start = null, Guid? dept = null, Guid? teacher = null,
        Guid? tenant = null, Guid? course = null)
        => new(id ?? Guid.NewGuid(), course ?? Course, "ENG7", "English 7", dept ?? Dept, tenant ?? Tenant, Campus, term ?? OldTerm, "Term", start ?? OldStart,
            teacher, "Ms Khan");

    private static readonly CourseCopyOffering Source = Offering(), Target = Offering(term: NewTerm, start: NewStart);

    private static CourseCopyPlan Plan(string[]? parts = null, Guid[]? ids = null, int shift = 0, bool keep = false, CourseCopyOffering? target = null)
        => new(Source, target ?? Target, (parts ?? [.. P.All]).ToHashSet(), ids?.ToHashSet(), shift, keep, Teacher);

    private static CourseMaterial Material(string name, Guid term, Guid? course = null)
        => new(Tenant, Campus, Dept, Guid.NewGuid(), term, course ?? Course, name, Teacher, CourseMaterialType.File, filePath: $"materials/{name}.pdf");

    /// <summary>A week-1 module that opens an assignment (with a rubric), a quiz drawing from the course bank, a file, a
    /// video, a page, a discussion and a SCORM package; plus an assignment and a quiz outside any module.</summary>
    private sealed class Course7
    {
        public readonly CourseContent C = new();
        public readonly CourseContentModule Week1, Week2;
        public readonly Assignment Essay, Unlinked;
        public readonly Quiz Quiz, LooseQuiz;
        public readonly QuestionBank Bank;
        public readonly CourseMaterial Handout;
        public readonly ContentVideo Video;
        public readonly GroupSet Groups;
        public readonly Guid DiscussionId = Guid.NewGuid();
        public readonly ScormPackage Scorm;

        public Course7()
        {
            Week1 = new CourseContentModule(Source.OfferingId, "Week 1 — Rivers", 1, "Read chapter 1.");
            Week1.ConfigureRelease(OldStart.AddDays(7), true, ModuleCompletionMode.All);
            Week1.Publish();
            Week2 = new CourseContentModule(Source.OfferingId, "Week 2 — Mountains", 2);
            C.Modules.AddRange([Week1, Week2]);
            C.Prerequisites.Add(new ModulePrerequisite(Week2.Id, Week1.Id));
            Video = new ContentVideo(Week1.Id, "River video", null, "https://video.example/rivers");
            C.Videos.Add(Video);

            Groups = new GroupSet(Source.OfferingId, "Project groups", true, 4);
            C.GroupSets.Add(Groups);
            Essay = new Assignment(Source.OfferingId, "Essay on rivers", "Two pages.", OldStart.AddDays(14), 20);
            Essay.ConfigureAvailability(OldStart.AddDays(10), OldStart.AddDays(16));
            Essay.ConfigureGroups(Groups.Id, false);
            Essay.ConfigurePeerReview(2, OldStart.AddDays(18), true);
            Essay.Publish();
            Unlinked = new Assignment(Source.OfferingId, "Poem", null, OldStart.AddDays(30), 10);
            Unlinked.ConfigureGroups(Groups.Id, true);
            C.Assignments.AddRange([Essay, Unlinked]);
            var rubric = Rubric.Create(Essay.Id, "Essay rubric", Teacher);
            var ideas = rubric.AddCriterion("Ideas", 10, 1);
            var good = ideas.AddLevel("Good", 10, 1);
            var weak = ideas.AddLevel("Weak", 4, 2);
            C.Rubrics.Add(rubric);
            C.Criteria.Add(ideas);
            C.Levels.AddRange([good, weak]);

            Bank = new QuestionBank(Tenant, Source.OfferingId, Dept, "Rivers bank", null, false, Teacher);
            C.Banks.Add(Bank);
            var bankQuestion = QuizQuestion.ForBank(Bank.Id, "Longest river?", QuestionType.MultipleChoice, 1, 1, tags: "rivers");
            C.Questions.Add(bankQuestion);
            C.Options.AddRange([new QuizOption(bankQuestion.Id, "Nile", true, 1), new QuizOption(bankQuestion.Id, "Ravi", false, 2)]);
            Quiz = new Quiz(Source.OfferingId, "Rivers quiz", Teacher, "Answer all.", 20, 2, OldStart.AddDays(8), OldStart.AddDays(9));
            Quiz.Publish();
            LooseQuiz = new Quiz(Source.OfferingId, "Spare quiz", Teacher);
            C.Quizzes.AddRange([Quiz, LooseQuiz]);
            var written = new QuizQuestion(Quiz.Id, "Name a river.", QuestionType.MultipleChoice, 2, 1);
            C.Questions.Add(written);
            C.Options.Add(new QuizOption(written.Id, "Indus", true, 1));
            C.QuestionGroups.Add(new QuizQuestionGroup(Quiz.Id, Bank.Id, "From the bank", 1, 1, 2));

            Handout = Material("Handout", OldTerm);
            C.Materials.Add(Handout);
            Scorm = new ScormPackage(Source.OfferingId, "River tour", ScormVersion.Scorm12, "tour", "index.html", 3, 1000, null, Teacher);
            C.ScormPackages.Add(Scorm);
            C.DiscussionTitles[DiscussionId] = "Your river";

            var position = 0;
            void Item(ModuleItemType type, Guid? target, string title, string? url = null, string? body = null)
                => C.Items.Add(new ModuleItem(Week1.Id, Source.OfferingId, type, target, title, url, body, ++position, Teacher));
            Item(ModuleItemType.Header, null, "Start here");
            Item(ModuleItemType.Page, null, "Welcome", body: "Hello class");
            Item(ModuleItemType.ExternalUrl, null, "Atlas", url: "https://atlas.example/");
            Item(ModuleItemType.Assignment, Essay.Id, "Essay on rivers");
            Item(ModuleItemType.Quiz, Quiz.Id, "Rivers quiz");
            Item(ModuleItemType.Material, Handout.Id, "Handout");
            Item(ModuleItemType.Video, Video.Id, "River video");
            Item(ModuleItemType.Discussion, DiscussionId, "Your river");
            Item(ModuleItemType.ScormPackage, Scorm.Id, "River tour");
            C.Items.Last(i => i.ItemType == ModuleItemType.Quiz).SetRequirement(ModuleRequirementType.MustScoreAtLeast, 60);
        }
    }

    [Fact]
    public void Copy_PointsCopiesAtEachOther_MovesDates_AndHidesEverything()
    {
        var course = new Course7();
        var outcome = CourseContentCloner.Clone(course.C, Plan(shift: 182));
        var copy = outcome.Copy;

        Assert.Equal(2, copy.Modules.Count);
        Assert.All(copy.Modules, m => { Assert.Equal(Target.OfferingId, m.OfferingId); Assert.False(m.IsPublished); });
        var week1 = copy.Modules.Single(m => m.WeekNumber == 1);
        Assert.Equal(OldStart.AddDays(7 + 182), week1.UnlockAt);
        Assert.True(week1.RequireSequentialProgress);
        Assert.Equal(week1.Id, copy.Prerequisites.Single().PrerequisiteModuleId);

        var essay = copy.Assignments.Single(a => a.Title == "Essay on rivers");
        Assert.False(essay.IsPublished);
        Assert.Equal(OldStart.AddDays(14 + 182), essay.DueDate);
        Assert.Equal(OldStart.AddDays(10 + 182), essay.AvailableFrom);
        Assert.Equal(OldStart.AddDays(16 + 182), essay.AvailableUntil);
        Assert.Equal(OldStart.AddDays(18 + 182), essay.PeerReviewDueDate);
        // One group set definition for both assignments, without its groups.
        var set = Assert.Single(copy.GroupSets);
        Assert.Equal(("Project groups", true, 4), (set.Name, set.SelfSignup, set.MaxSize));
        Assert.All(copy.Assignments, a => Assert.Equal(set.Id, a.GroupSetId));
        Assert.Equal(essay.Id, copy.Rubrics.Single().AssignmentId);
        Assert.Equal(["Good", "Weak"], copy.Levels.Select(l => l.Label));
        Assert.Equal(copy.Criteria.Single().Id, copy.Levels[0].CriterionId);

        var quiz = copy.Quizzes.Single(q => q.Title == "Rivers quiz");
        Assert.Equal((OldStart.AddDays(8 + 182), OldStart.AddDays(9 + 182), 20, 2), (quiz.AvailableFrom, quiz.AvailableUntil, quiz.TimeLimitMinutes, quiz.MaxAttempts));
        var bank = copy.Banks.Single();
        Assert.Equal(Target.OfferingId, bank.CourseOfferingId);
        Assert.Equal(bank.Id, copy.QuestionGroups.Single().QuestionBankId); // the copied quiz draws from the copied bank
        Assert.Equal("rivers", copy.Questions.Single(q => q.QuestionBankId == bank.Id).Tags);
        Assert.Equal(3, copy.Options.Count);

        var handout = copy.Materials.Single();
        Assert.Equal((NewTerm, "materials/Handout.pdf"), (handout.SemesterId, handout.FilePath));

        // Items open the copies; the discussion and SCORM items are left out.
        var items = copy.Items;
        Assert.Equal(7, items.Count);
        Assert.Equal(essay.Id, items.Single(i => i.ItemType == ModuleItemType.Assignment).TargetId);
        Assert.Equal(quiz.Id, items.Single(i => i.ItemType == ModuleItemType.Quiz).TargetId);
        Assert.Equal(60, items.Single(i => i.ItemType == ModuleItemType.Quiz).MinScorePercent);
        Assert.Equal(handout.Id, items.Single(i => i.ItemType == ModuleItemType.Material).TargetId);
        Assert.Equal(copy.Videos.Single().Id, items.Single(i => i.ItemType == ModuleItemType.Video).TargetId);
        Assert.Equal("Hello class", items.Single(i => i.ItemType == ModuleItemType.Page).Body);
        Assert.All(items, i => Assert.Equal(week1.Id, i.ModuleId));

        Assert.Contains(outcome.NotCopied, n => n is { Reason: CourseCopyNotes.Discussion, Title: "Your river" });
        Assert.Single(outcome.NotCopied, n => n.Reason == CourseCopyNotes.Scorm); // the package once, not again for its item
        Assert.Equal(2, outcome.Copied[P.Modules]);
        Assert.Equal(2, outcome.Copied[P.Assignments]);
        Assert.Equal(2, outcome.Copied[P.Quizzes]);
        Assert.Equal(2, outcome.Copied["questions"]); // one written in the quiz, one in the bank
        Assert.False(outcome.Copied.ContainsKey(P.Settings));
    }

    [Fact]
    public void Copy_KeepsPublishedState_WhenAsked()
    {
        var course = new Course7();
        var copy = CourseContentCloner.Clone(course.C, Plan(keep: true)).Copy;
        Assert.True(copy.Modules.Single(m => m.WeekNumber == 1).IsPublished);
        Assert.False(copy.Modules.Single(m => m.WeekNumber == 2).IsPublished);
        Assert.True(copy.Assignments.Single(a => a.Title == "Essay on rivers").IsPublished);
        Assert.False(copy.Assignments.Single(a => a.Title == "Poem").IsPublished);
        Assert.True(copy.Quizzes.Single(q => q.Title == "Rivers quiz").IsPublished);
    }

    [Fact]
    public void ChosenModule_BringsWhatItsItemsOpen_AndNothingElse()
    {
        var course = new Course7();
        var outcome = CourseContentCloner.Clone(course.C, Plan(parts: [P.Modules], ids: [course.Week1.Id]));
        var copy = outcome.Copy;
        Assert.Equal("Week 1 — Rivers", Assert.Single(copy.Modules).Title);
        Assert.Empty(copy.Prerequisites);                                       // week 2 was not copied
        Assert.Equal("Essay on rivers", Assert.Single(copy.Assignments).Title); // not the poem
        Assert.Equal("Rivers quiz", Assert.Single(copy.Quizzes).Title);         // not the spare quiz
        Assert.Single(copy.Banks);                                              // the bank the quiz draws from
        Assert.Single(copy.Materials);
        Assert.Single(copy.Rubrics);
    }

    [Fact]
    public void ChosenItems_OnlyThose_AndSettingsOnlyWhenAsked()
    {
        var course = new Course7();
        course.C.LatePolicy = new LatePolicy(Source.OfferingId);
        course.C.LatePolicy.Configure(true, 10, LateInterval.Day, 50, true, 0);

        var outcome = CourseContentCloner.Clone(course.C, Plan(parts: [P.Assignments], ids: [course.Unlinked.Id]));
        Assert.Equal("Poem", Assert.Single(outcome.Copy.Assignments).Title);
        Assert.Empty(outcome.Copy.Modules);
        Assert.Null(outcome.Copy.LatePolicy);

        var settings = CourseContentCloner.Clone(course.C, Plan(parts: [P.Settings])).Copy.LatePolicy;
        Assert.NotNull(settings);
        Assert.Equal((Target.OfferingId, 10m, LateInterval.Day, 50m), (settings!.CourseOfferingId, settings.DeductionPercent, settings.Interval, settings.LowestGradePercent));
    }

    [Fact]
    public void FilesOfTheSameCourseAndTerm_AreReused()
    {
        var course = new Course7();
        var sameTerm = Offering(term: OldTerm); // another section of the same class
        var copy = CourseContentCloner.Clone(course.C, Plan(target: sameTerm)).Copy;
        Assert.Empty(copy.Materials);
        Assert.Equal(course.Handout.Id, copy.Items.Single(i => i.ItemType == ModuleItemType.Material).TargetId);
    }

    [Fact]
    public void GradedTool_BringsItsGradebookColumn()
    {
        var c = new CourseContent();
        var toolId = Guid.NewGuid();
        var link = new LtiResourceLink(toolId, Guid.NewGuid(), Source.OfferingId, null, "Maths lab", null, null, "level=2", "iframe", Teacher);
        var column = Assignment.ForExternalTool(Source.OfferingId, "Maths lab", 50, OldStart.AddDays(20), toolId);
        var line = new LtiLineItem(toolId, Source.OfferingId, link.Id, column.Id, "Maths lab", 50, "lab", null, null, OldStart.AddDays(20));
        link.AttachLineItem(line.Id);
        c.ToolLinks.Add(link);
        c.Assignments.Add(column);
        c.LineItems.Add(line);

        var copy = CourseContentCloner.Clone(c, Plan(parts: [P.ExternalTools], shift: 7)).Copy;
        var newLink = Assert.Single(copy.ToolLinks);
        var newColumn = Assert.Single(copy.Assignments);
        var newLine = Assert.Single(copy.LineItems);
        Assert.Equal((toolId, "level=2", "iframe"), (newLink.ToolId, newLink.CustomParameters, newLink.PresentationTarget));
        Assert.Equal(newLine.Id, newLink.LineItemId);
        Assert.Equal((newLink.Id, newColumn.Id, OldStart.AddDays(27)), (newLine.ResourceLinkId!.Value, newLine.AssignmentId, newLine.EndDateTime!.Value));
        Assert.Equal((toolId, false, OldStart.AddDays(27)), (newColumn.ExternalToolId!.Value, newColumn.IsPublished, newColumn.DueDate));
    }

    [Fact]
    public void OutcomeLinks_KeepSchoolAndOwnCourseOutcomes_NotOtherCourses()
    {
        var course = new Course7();
        Guid own = Guid.NewGuid(), school = Guid.NewGuid(), other = Guid.NewGuid();
        course.C.OutcomeCourses[own] = Course;
        course.C.OutcomeCourses[school] = null;
        course.C.OutcomeCourses[other] = Guid.NewGuid();
        var bankQuestion = course.C.Questions.Single(q => q.QuestionBankId is not null);
        course.C.Alignments.AddRange([
            new OutcomeAlignment(own, OutcomeTargetType.Assignment, course.Essay.Id, Source.OfferingId, Teacher),
            new OutcomeAlignment(school, OutcomeTargetType.RubricCriterion, course.C.Criteria[0].Id, Source.OfferingId, Teacher),
            new OutcomeAlignment(school, OutcomeTargetType.BankQuestion, bankQuestion.Id, null, Teacher),
            new OutcomeAlignment(other, OutcomeTargetType.Assignment, course.Essay.Id, Source.OfferingId, Teacher)]);

        var outcome = CourseContentCloner.Clone(course.C, Plan());
        var links = outcome.Copy.Alignments;
        Assert.Equal(3, links.Count);
        Assert.Equal(outcome.Copy.Assignments.Single(a => a.Title == "Essay on rivers").Id, links.Single(l => l.OutcomeId == own).TargetId);
        Assert.Equal(outcome.Copy.Criteria.Single().Id, links.Single(l => l.TargetType == OutcomeTargetType.RubricCriterion).TargetId);
        Assert.Null(links.Single(l => l.TargetType == OutcomeTargetType.BankQuestion).CourseOfferingId);
        Assert.Equal(1, Assert.Single(outcome.NotCopied, n => n.Reason == CourseCopyNotes.OtherCourseOutcomes).Count);
    }

    // ── Service: access, templates and checks ──

    private sealed class Store : ICourseCopyStore
    {
        public readonly List<CourseCopyOffering> Offerings = [];
        public readonly Dictionary<Guid, List<Guid>> Departments = [];
        public readonly List<CourseTemplate> Templates = [];
        public readonly Dictionary<Guid, CourseContent> Content = [];
        public readonly List<(CourseContent Copy, CourseCopyRun Run)> Saved = [];

        public Task<CourseCopyOffering?> OfferingAsync(Guid offeringId, CancellationToken ct = default) => Task.FromResult(Offerings.FirstOrDefault(o => o.OfferingId == offeringId));
        public Task<IReadOnlyList<CourseCopyOffering>> OfferingsAsync(Guid? tenantId, IReadOnlyCollection<Guid>? departmentIds, Guid? teacherUserId, int limit, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CourseCopyOffering>>(Offerings.Where(o => o.TenantId == tenantId
                && (departmentIds is null || departmentIds.Contains(o.DepartmentId) || (teacherUserId != null && o.TeacherUserId == teacherUserId))).Take(limit).ToList());
        public Task<IReadOnlyList<Guid>> DepartmentsOfAsync(Guid userId, string role, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Guid>>(Departments.GetValueOrDefault(userId) ?? []);
        public Task<CourseTemplate?> TemplateAsync(Guid offeringId, CancellationToken ct = default) => Task.FromResult(Templates.FirstOrDefault(t => t.OfferingId == offeringId));
        public Task<IReadOnlyList<(CourseTemplate Template, CourseCopyOffering Offering)>> TemplatesAsync(Guid? tenantId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<(CourseTemplate, CourseCopyOffering)>>(Templates
                .Select(t => (t, Offerings.Single(o => o.OfferingId == t.OfferingId))).Where(x => x.Item2.TenantId == tenantId).ToList());
        public void AddTemplate(CourseTemplate template) => Templates.Add(template);
        public void RemoveTemplate(CourseTemplate template) => Templates.Remove(template);
        public Task<CourseContent> ReadAsync(CourseCopyOffering offering, CancellationToken ct = default) => Task.FromResult(Content.GetValueOrDefault(offering.OfferingId) ?? new CourseContent());
        public Task SaveCopyAsync(CourseContent copy, CourseCopyRun run, CancellationToken ct = default) { Saved.Add((copy, run)); return Task.CompletedTask; }
        public Task<IReadOnlyList<(CourseCopyRun Run, string CopiedBy)>> RunsAsync(Guid targetOfferingId, int limit, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<(CourseCopyRun, string)>>(Saved.Where(s => s.Run.TargetOfferingId == targetOfferingId).Select(s => (s.Run, "Ms Khan")).Reverse().ToList());
        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private static readonly Guid OtherTeacher = Guid.NewGuid(), Admin = Guid.NewGuid();

    private static (Store Store, CourseCopyService Service, CourseCopyOffering Old, CourseCopyOffering New, CourseCopyOffering Science, CourseCopyOffering Elsewhere) World()
    {
        var store = new Store();
        var old = Offering(teacher: Teacher);
        var current = Offering(term: NewTerm, start: NewStart, teacher: Teacher);
        var science = Offering(dept: OtherDept, teacher: OtherTeacher, course: Guid.NewGuid());
        var elsewhere = Offering(tenant: Guid.NewGuid(), teacher: Teacher);
        store.Offerings.AddRange([old, current, science, elsewhere]);
        store.Departments[Teacher] = [Dept];
        store.Departments[OtherTeacher] = [OtherDept];
        store.Departments[Admin] = [Dept];
        var content = new CourseContent();
        content.Assignments.Add(new Assignment(old.OfferingId, "Essay", null, OldStart.AddDays(14), 20));
        store.Content[old.OfferingId] = content;
        return (store, new CourseCopyService(store), old, current, science, elsewhere);
    }

    private static CourseCopyActor As(Guid user, string role = "Faculty") => new(user, role, Tenant);

    [Fact]
    public async Task Sources_AreTheTeachersOtherCourses_WithTheSuggestedShift()
    {
        var (_, service, old, current, science, elsewhere) = World();
        var sources = await service.SourcesAsync(As(Teacher), current.OfferingId);
        Assert.True(sources.Ok);
        Assert.Equal(current.OfferingId, sources.Value!.Target.OfferingId);
        Assert.Equal([old.OfferingId], sources.Value.Sources.Select(s => s.OfferingId)); // not itself, other departments or schools
        Assert.True(sources.Value.Sources[0].TaughtByYou);
        Assert.Equal(182, CourseCopyService.SuggestedShift(old, current));

        Assert.Equal(403, (await service.SourcesAsync(As(OtherTeacher), current.OfferingId)).Status);
        Assert.Equal(403, (await service.PreviewAsync(As(Teacher), current.OfferingId, science.OfferingId)).Status);
        Assert.Equal(403, (await service.PreviewAsync(As(Teacher), current.OfferingId, elsewhere.OfferingId)).Status); // never across schools
        Assert.Equal(403, (await service.SourcesAsync(As(Teacher, "Student"), current.OfferingId)).Status);
        var preview = await service.PreviewAsync(As(Teacher), current.OfferingId, old.OfferingId);
        Assert.Equal((182, 1), (preview.Value!.SuggestedShiftDays, preview.Value.Parts.Single().Count));
    }

    [Fact]
    public async Task Templates_ForTheDepartmentOrTheSchool_OnlyAdminsShare()
    {
        var (store, service, old, _, science, _) = World();
        Assert.Equal(403, (await service.SaveTemplateAsync(As(Teacher), old.OfferingId, new("English starter", null, "Institution"))).Status);
        Assert.Equal(400, (await service.SaveTemplateAsync(As(Admin, "Admin"), old.OfferingId, new("English starter", null, "Everyone"))).Status);
        Assert.Equal("Enter a name for the template.", (await service.SaveTemplateAsync(As(Admin, "Admin"), old.OfferingId, new(" ", null, "Department"))).Error);

        var shared = await service.SaveTemplateAsync(As(Admin, "Admin"), old.OfferingId, new("English starter", "Weeks 1-10", "Department"));
        Assert.Equal(201, shared.Status);
        // A department template: not for the science teacher...
        Assert.DoesNotContain((await service.SourcesAsync(As(OtherTeacher), science.OfferingId)).Value!.Sources, s => s.OfferingId == old.OfferingId);
        // ...until it is shared with the whole school.
        Assert.Equal(200, (await service.SaveTemplateAsync(As(Admin, "Admin"), old.OfferingId, new("English starter", "Weeks 1-10", "Institution"))).Status);
        var source = Assert.Single((await service.SourcesAsync(As(OtherTeacher), science.OfferingId)).Value!.Sources);
        Assert.Equal((old.OfferingId, true, "English starter", "Institution"), (source.OfferingId, source.IsTemplate, source.TemplateName, source.TemplateScope));
        var copied = await service.CopyAsync(As(OtherTeacher), science.OfferingId, new(old.OfferingId, null, null, 0, false), NewStart);
        Assert.Equal(201, copied.Status);
        Assert.Equal(science.OfferingId, store.Saved.Single().Copy.Assignments.Single().CourseOfferingId);

        Assert.Equal(403, (await service.RemoveTemplateAsync(As(OtherTeacher, "Admin"), old.OfferingId)).Status); // admin of another department
        Assert.True((await service.RemoveTemplateAsync(As(Admin, "Admin"), old.OfferingId)).Ok);
        Assert.Empty(store.Templates);
    }

    [Fact]
    public async Task Copy_ChecksWhatWasAsked_AndKeepsAHistory()
    {
        var (store, service, old, current, _, _) = World();
        Assert.Equal(400, (await service.CopyAsync(As(Teacher), current.OfferingId, new(old.OfferingId, ["homework"], null, 0, false), NewStart)).Status);
        Assert.Equal(400, (await service.CopyAsync(As(Teacher), current.OfferingId, new(old.OfferingId, null, null, 4000, false), NewStart)).Status);
        Assert.Equal(400, (await service.CopyAsync(As(Teacher), current.OfferingId, new(current.OfferingId, null, null, 0, false), NewStart)).Status);
        Assert.Equal(400, (await service.CopyAsync(As(Teacher), current.OfferingId, new(old.OfferingId, [P.Quizzes], null, 0, false), NewStart)).Status); // nothing there
        Assert.Empty(store.Saved);

        var done = await service.CopyAsync(As(Teacher), current.OfferingId, new(old.OfferingId, [P.Assignments], null, 182, false), NewStart);
        Assert.Equal(201, done.Status);
        Assert.Equal(1, done.Value!.Copied[P.Assignments]);
        var run = store.Saved.Single().Run;
        Assert.Equal((old.OfferingId, current.OfferingId, Teacher, 182), (run.SourceOfferingId, run.TargetOfferingId, run.CopiedByUserId, run.DateShiftDays));

        var history = await service.HistoryAsync(As(Teacher), current.OfferingId);
        var row = Assert.Single(history.Value!);
        Assert.Equal(("ENG7 — English 7", "Ms Khan", 1), (row.SourceTitle, row.CopiedBy, row.Copied[P.Assignments]));
        Assert.Equal(NewStart, (await service.PreviewAsync(As(Teacher), current.OfferingId, old.OfferingId)).Value!.PreviouslyCopiedAt);
    }

    [Fact]
    public void Template_ChecksItsName_AndARunCannotCopyIntoItself()
    {
        Assert.Throws<ArgumentException>(() => new CourseTemplate(Guid.NewGuid(), new string('x', CourseTemplate.MaxNameLength + 1), null, CourseTemplateScope.Department, Teacher));
        Assert.Throws<ArgumentException>(() => new CourseTemplate(Guid.NewGuid(), "Starter", null, (CourseTemplateScope)9, Teacher));
        var same = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => new CourseCopyRun(same, same, Teacher, 0, false, "{}", NewStart));
    }
}
