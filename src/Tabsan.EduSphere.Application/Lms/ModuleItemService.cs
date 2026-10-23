using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Application.Lms;

/// <summary>
/// Plan M7 — module items (ordered content of a module), completion requirements, release conditions, the student's
/// progress view, the teacher's progress report and the gate that keeps locked assignments, quizzes and tools closed.
/// Progress is evaluated from current data (views, "mark as done", submissions, posted grades, finished quizzes,
/// discussion posts) each time it is read; first completions are stored so they stick.
/// </summary>
public sealed class ModuleItemService
{
    private readonly IModuleItemRepository _repo;
    private readonly IAuditService _audit;
    private readonly Func<DateTime> _now;

    public ModuleItemService(IModuleItemRepository repo, IAuditService audit) : this(repo, audit, () => DateTime.UtcNow) { }

    public ModuleItemService(IModuleItemRepository repo, IAuditService audit, Func<DateTime> now)
    {
        _repo = repo;
        _audit = audit;
        _now = now;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Teacher: structure
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<ModuleStructureResponse?> StructureAsync(Guid offeringId, CancellationToken ct = default)
    {
        var offering = await _repo.GetOfferingAsync(offeringId, ct);
        if (offering is null) return null;
        var modules = Ordered(await _repo.GetModulesAsync(offeringId, ct));
        var items = await _repo.GetItemsAsync(offeringId, ct);
        var prerequisites = await _repo.GetPrerequisitesAsync(modules.Select(m => m.Id).ToList(), ct);
        var linkables = (await _repo.GetLinkablesAsync(offeringId, ct)).Select(l => (l.Type, l.Id)).ToHashSet();
        return new ModuleStructureResponse(offeringId, offering.CourseCode, offering.CourseTitle, modules.Select(m => new ModuleStructureModule(
            m.Id, m.Title, m.WeekNumber, m.Body, m.IsPublished, m.UnlockAt, m.RequireSequentialProgress, m.CompletionMode.ToString(),
            prerequisites.Where(p => p.ModuleId == m.Id).Select(p => p.PrerequisiteModuleId).ToList(),
            items.Where(i => i.ModuleId == m.Id).OrderBy(i => i.Position).Select(i => ToResponse(i, linkables)).ToList())).ToList());
    }

    public async Task<IReadOnlyList<LinkableResponse>> LinkablesAsync(Guid offeringId, CancellationToken ct = default)
        => (await _repo.GetLinkablesAsync(offeringId, ct)).Select(l => new LinkableResponse(l.Type.ToString(), l.Id, l.Title, l.Detail)).ToList();

    public async Task<ModuleResult<ModuleItemResponse>> AddItemAsync(Guid moduleId, AddModuleItemRequest r, Guid actorUserId, CancellationToken ct = default)
    {
        var module = await _repo.GetModuleAsync(moduleId, ct);
        if (module is null) return ModuleResult<ModuleItemResponse>.Fail(404, "Module not found.");
        if (!Enum.TryParse<ModuleItemType>(r.ItemType, true, out var type) || !Enum.IsDefined(type))
            return ModuleResult<ModuleItemResponse>.Fail(400, "Choose what to add.");

        var linkables = await _repo.GetLinkablesAsync(module.OfferingId, ct);
        var title = r.Title?.Trim();
        if (ModuleItem.NeedsTarget(type))
        {
            var target = linkables.FirstOrDefault(l => l.Type == type && l.Id == r.TargetId);
            if (target is null) return ModuleResult<ModuleItemResponse>.Fail(400, "Choose an item of this course.");
            if (string.IsNullOrWhiteSpace(title)) title = target.Title;
        }

        ModuleItem item;
        try
        {
            var items = await _repo.GetItemsAsync(module.OfferingId, ct);
            var position = items.Where(i => i.ModuleId == moduleId).Select(i => i.Position).DefaultIfEmpty(0).Max() + 1;
            item = new ModuleItem(moduleId, module.OfferingId, type, r.TargetId, title ?? "", r.Url, r.Body, position, actorUserId);
            item.Update(item.Title, r.Url, r.Body, r.IndentLevel);
            item.SetRequirement(ParseRequirement(r.RequirementType), r.MinScorePercent);
        }
        catch (ArgumentException ex) { return ModuleResult<ModuleItemResponse>.Fail(400, ex.Message); }
        _repo.Add(item);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("ModuleItemAdded", "ModuleItem", item.Id.ToString(), actorUserId: actorUserId,
            newValuesJson: $"{{\"module\":\"{moduleId}\",\"type\":\"{type}\"}}"), ct);
        return ModuleResult<ModuleItemResponse>.Success(ToResponse(item, linkables.Select(l => (l.Type, l.Id)).ToHashSet()), 201);
    }

    public async Task<ModuleResult<ModuleItemResponse>> UpdateItemAsync(Guid itemId, UpdateModuleItemRequest r, Guid actorUserId, CancellationToken ct = default)
    {
        var item = await _repo.GetItemAsync(itemId, ct);
        if (item is null) return ModuleResult<ModuleItemResponse>.Fail(404, "Item not found.");
        bool requirementChanged;
        try
        {
            item.Update(r.Title, r.Url, r.Body, r.IndentLevel);
            requirementChanged = item.SetRequirement(ParseRequirement(r.RequirementType), r.MinScorePercent);
            if (item.IsPublished != r.IsPublished) item.SetPublished(r.IsPublished);
        }
        catch (ArgumentException ex) { return ModuleResult<ModuleItemResponse>.Fail(400, ex.Message); }
        if (requirementChanged)
            foreach (var p in await _repo.GetProgressForItemAsync(itemId, ct)) p.ResetCompletion(); // re-evaluated against the new requirement
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("ModuleItemUpdated", "ModuleItem", item.Id.ToString(), actorUserId: actorUserId), ct);
        var linkables = (await _repo.GetLinkablesAsync(item.OfferingId, ct)).Select(l => (l.Type, l.Id)).ToHashSet();
        return ModuleResult<ModuleItemResponse>.Success(ToResponse(item, linkables));
    }

    public async Task<ModuleResult<bool>> RemoveItemAsync(Guid itemId, Guid actorUserId, CancellationToken ct = default)
    {
        var item = await _repo.GetItemAsync(itemId, ct);
        if (item is null) return ModuleResult<bool>.Fail(404, "Item not found.");
        _repo.Remove(item);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("ModuleItemRemoved", "ModuleItem", itemId.ToString(), actorUserId: actorUserId), ct);
        return ModuleResult<bool>.Success(true);
    }

    public async Task<ModuleResult<bool>> ReorderAsync(Guid moduleId, ReorderModuleItemsRequest r, CancellationToken ct = default)
    {
        var module = await _repo.GetModuleAsync(moduleId, ct);
        if (module is null) return ModuleResult<bool>.Fail(404, "Module not found.");
        var items = (await _repo.GetItemsAsync(module.OfferingId, ct)).Where(i => i.ModuleId == moduleId).ToList();
        var ids = (r.ItemIds ?? []).ToList();
        if (ids.Count != items.Count || ids.Distinct().Count() != ids.Count || ids.Any(id => items.All(i => i.Id != id)))
            return ModuleResult<bool>.Fail(400, "List every item of the module once.");
        for (var i = 0; i < ids.Count; i++) items.Single(x => x.Id == ids[i]).MoveTo(i + 1);
        await _repo.SaveChangesAsync(ct);
        return ModuleResult<bool>.Success(true);
    }

    public async Task<ModuleResult<bool>> SetReleaseAsync(Guid moduleId, ModuleReleaseRequest r, Guid actorUserId, CancellationToken ct = default)
    {
        var module = await _repo.GetModuleAsync(moduleId, ct);
        if (module is null) return ModuleResult<bool>.Fail(404, "Module not found.");
        var mode = ModuleCompletionMode.All;
        if (r.CompletionMode is not null && (!Enum.TryParse(r.CompletionMode, true, out mode) || !Enum.IsDefined(mode)))
            return ModuleResult<bool>.Fail(400, "Completion must be All or Any.");

        var modules = await _repo.GetModulesAsync(module.OfferingId, ct);
        var wanted = (r.PrerequisiteModuleIds ?? []).Distinct().ToList();
        if (wanted.Contains(moduleId)) return ModuleResult<bool>.Fail(400, "A module cannot require itself.");
        if (wanted.Any(id => modules.All(m => m.Id != id))) return ModuleResult<bool>.Fail(400, "Prerequisites must be modules of this course.");

        var all = await _repo.GetPrerequisitesAsync(modules.Select(m => m.Id).ToList(), ct);
        var graph = all.Where(p => p.ModuleId != moduleId).GroupBy(p => p.ModuleId).ToDictionary(g => g.Key, g => g.Select(p => p.PrerequisiteModuleId).ToList());
        graph[moduleId] = wanted;
        if (HasCycle(graph, moduleId)) return ModuleResult<bool>.Fail(400, "These prerequisites would make modules wait for each other.");

        module.ConfigureRelease(r.UnlockAt is { } u ? DateTime.SpecifyKind(u, u.Kind == DateTimeKind.Unspecified ? DateTimeKind.Utc : u.Kind).ToUniversalTime() : null,
            r.RequireSequentialProgress, mode);
        var current = all.Where(p => p.ModuleId == moduleId).ToList();
        foreach (var p in current.Where(p => !wanted.Contains(p.PrerequisiteModuleId))) _repo.Remove(p);
        foreach (var id in wanted.Where(id => current.All(p => p.PrerequisiteModuleId != id))) _repo.Add(new ModulePrerequisite(moduleId, id));
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("ModuleReleaseChanged", "CourseContentModule", moduleId.ToString(), actorUserId: actorUserId,
            newValuesJson: $"{{\"sequential\":{r.RequireSequentialProgress.ToString().ToLowerInvariant()},\"prerequisites\":{wanted.Count}}}"), ct);
        return ModuleResult<bool>.Success(true);
    }

    private static bool HasCycle(Dictionary<Guid, List<Guid>> graph, Guid start)
    {
        var stack = new Stack<Guid>(graph.TryGetValue(start, out var first) ? first : []);
        var seen = new HashSet<Guid>();
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            if (id == start) return true;
            if (!seen.Add(id)) continue;
            if (graph.TryGetValue(id, out var next)) foreach (var n in next) stack.Push(n);
        }
        return false;
    }

    public async Task<Guid?> OfferingOfModuleAsync(Guid moduleId, CancellationToken ct = default) => (await _repo.GetModuleAsync(moduleId, ct))?.OfferingId;

    public async Task<Guid?> OfferingOfItemAsync(Guid itemId, CancellationToken ct = default) => (await _repo.GetItemAsync(itemId, ct))?.OfferingId;

    // ═════════════════════════════════════════════════════════════════════════
    // Student
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<StudentModulesResponse?> StudentViewAsync(Guid offeringId, Guid studentProfileId, CancellationToken ct = default)
    {
        var context = await LoadAsync(offeringId, [studentProfileId], ct);
        if (context is null) return null;
        var states = await EvaluateAsync(context, studentProfileId, persist: true, ct);
        return new StudentModulesResponse(offeringId, context.Offering.CourseCode, context.Offering.CourseTitle, ModuleProgressEvaluator.CoursePercent(states),
            context.Modules.Zip(states).Select(z => new StudentModule(z.First.Id, z.First.Title, z.First.WeekNumber, z.First.Body, z.Second.Locked,
                z.Second.LockReason, z.Second.Completed, z.Second.Required, z.Second.Done, z.Second.Percent,
                context.ItemsOf(z.First.Id).Zip(z.Second.Items).Select(i => new StudentModuleItem(i.First.Id, i.First.ItemType.ToString(), i.First.Title,
                    i.First.IndentLevel, i.First.RequirementType.ToString(), i.First.MinScorePercent, Status(i.Second), i.Second.LockReason,
                    i.First.RequirementType == ModuleRequirementType.MustMarkDone && !i.Second.Locked)).ToList())).ToList());
    }

    /// <summary>Opens an item: records the view for a student (unless locked) and says where it leads. Staff get a preview.</summary>
    public async Task<ModuleResult<ModuleItemOpenResponse>> OpenAsync(Guid itemId, Guid? studentProfileId, CancellationToken ct = default)
    {
        var item = await _repo.GetItemAsync(itemId, ct);
        if (item is null) return ModuleResult<ModuleItemOpenResponse>.Fail(404, "This item is not available.");
        ModuleItemState? state = null;
        if (studentProfileId is { } studentId)
        {
            var context = await LoadAsync(item.OfferingId, [studentId], ct);
            if (context is null || context.Items.All(i => i.Id != itemId)) return ModuleResult<ModuleItemOpenResponse>.Fail(404, "This item is not available.");
            state = Find(await EvaluateAsync(context, studentId, persist: false, ct), itemId);
            if (state is { Locked: false })
            {
                var progress = await ProgressRowAsync(context, studentId, item);
                progress.MarkViewed(_now());
                await _repo.SaveChangesAsync(ct);
                context = await LoadAsync(item.OfferingId, [studentId], ct);
                state = Find(await EvaluateAsync(context!, studentId, persist: true, ct), itemId);
            }
        }
        return ModuleResult<ModuleItemOpenResponse>.Success(await OpenResponseAsync(item, state, ct));
    }

    public async Task<ModuleResult<ModuleItemOpenResponse>> SetDoneAsync(Guid itemId, Guid studentProfileId, bool done, CancellationToken ct = default)
    {
        var item = await _repo.GetItemAsync(itemId, ct);
        if (item is null) return ModuleResult<ModuleItemOpenResponse>.Fail(404, "This item is not available.");
        if (item.RequirementType != ModuleRequirementType.MustMarkDone) return ModuleResult<ModuleItemOpenResponse>.Fail(400, "This item is not marked as done by hand.");
        var context = await LoadAsync(item.OfferingId, [studentProfileId], ct);
        if (context is null || context.Items.All(i => i.Id != itemId)) return ModuleResult<ModuleItemOpenResponse>.Fail(404, "This item is not available.");
        var state = Find(await EvaluateAsync(context, studentProfileId, persist: false, ct), itemId);
        if (state is { Locked: true }) return ModuleResult<ModuleItemOpenResponse>.Fail(403, state.LockReason ?? "This item is locked.");

        var progress = await ProgressRowAsync(context, studentProfileId, item);
        if (done) { progress.MarkViewed(_now()); progress.MarkDone(_now()); }
        else progress.UndoDone();
        await _repo.SaveChangesAsync(ct);
        context = await LoadAsync(item.OfferingId, [studentProfileId], ct);
        state = Find(await EvaluateAsync(context!, studentProfileId, persist: true, ct), itemId);
        return ModuleResult<ModuleItemOpenResponse>.Success(await OpenResponseAsync(item, state, ct));
    }

    /// <summary>Previous/next items (headers skipped) for the navigation bar.</summary>
    public async Task<ModuleResult<ModuleItemNavResponse>> NavAsync(Guid itemId, Guid? studentProfileId, CancellationToken ct = default)
    {
        var item = await _repo.GetItemAsync(itemId, ct);
        if (item is null) return ModuleResult<ModuleItemNavResponse>.Fail(404, "Not found.");
        var context = await LoadAsync(item.OfferingId, studentProfileId is { } s ? [s] : [], ct, includeUnpublished: studentProfileId is null);
        if (context is null || context.Items.All(i => i.Id != itemId)) return ModuleResult<ModuleItemNavResponse>.Fail(404, "Not found.");
        var states = studentProfileId is { } id ? await EvaluateAsync(context, id, persist: false, ct) : null;
        var ordered = context.Modules.SelectMany(m => context.ItemsOf(m.Id)).Where(i => i.ItemType != ModuleItemType.Header).ToList();
        var index = ordered.FindIndex(i => i.Id == itemId);
        ModuleNavLink? Link(int i) => i < 0 || i >= ordered.Count ? null
            : new ModuleNavLink(ordered[i].Id, ordered[i].Title, states is not null && Find(states, ordered[i].Id) is { Locked: true });
        var state = states is null ? null : Find(states, itemId);
        var module = context.Modules.First(m => m.Id == item.ModuleId);
        return ModuleResult<ModuleItemNavResponse>.Success(new ModuleItemNavResponse(itemId, module.Title, Link(index - 1), Link(index + 1),
            state is null ? "none" : Status(state), state is { Locked: false } && item.RequirementType == ModuleRequirementType.MustMarkDone, item.OfferingId));
    }

    /// <summary>
    /// Why a student may not open an assignment, quiz or tool yet; null when it is open (or not placed in any published module).
    /// </summary>
    public async Task<string?> LockReasonAsync(Guid studentProfileId, ModuleItemType type, Guid targetId, CancellationToken ct = default)
    {
        var placed = (await _repo.GetItemsByTargetAsync(type, targetId, ct)).Where(i => i.IsPublished).ToList();
        if (placed.Count == 0) return null;
        string? reason = null;
        foreach (var offeringId in placed.Select(i => i.OfferingId).Distinct())
        {
            var context = await LoadAsync(offeringId, [studentProfileId], ct);
            if (context is null) continue;
            var states = await EvaluateAsync(context, studentProfileId, persist: false, ct);
            foreach (var item in placed.Where(i => i.OfferingId == offeringId && context.Items.Any(x => x.Id == i.Id)))
            {
                var state = Find(states, item.Id);
                if (state is null) continue;
                if (!state.Locked) return null; // open from at least one place
                reason ??= state.LockReason;
            }
        }
        return reason is null ? null : $"This is locked in the course modules: {reason}.";
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Teacher: progress report
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<ModuleProgressReport?> ProgressReportAsync(Guid offeringId, CancellationToken ct = default)
    {
        var students = await _repo.GetEnrolledStudentsAsync(offeringId, ct);
        var context = await LoadAsync(offeringId, students.Select(s => s.StudentProfileId).ToList(), ct);
        if (context is null) return null;
        var rows = new List<ProgressStudentRow>();
        foreach (var s in students.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
        {
            var states = await EvaluateAsync(context, s.StudentProfileId, persist: false, ct);
            rows.Add(new ProgressStudentRow(s.StudentProfileId, s.Name, s.RegistrationNumber, ModuleProgressEvaluator.CoursePercent(states),
                states.Select(m => new ProgressCell(m.ModuleId, m.Done, m.Required, m.Percent, m.Completed, m.Locked)).ToList()));
        }
        return new ModuleProgressReport(offeringId, context.Offering.CourseCode, context.Offering.CourseTitle,
            context.Modules.Select(m => new ProgressModuleColumn(m.Id, m.Title, context.ItemsOf(m.Id).Count(i => i.RequirementType != ModuleRequirementType.None))).ToList(),
            rows);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Evaluation
    // ═════════════════════════════════════════════════════════════════════════

    private sealed class Context
    {
        public required ModuleOfferingInfo Offering { get; init; }
        public required IReadOnlyList<CourseContentModule> Modules { get; init; }
        public required IReadOnlyList<ModuleItem> Items { get; init; }
        public required IReadOnlyList<ModulePrerequisite> Prerequisites { get; init; }
        public required List<ModuleItemProgress> Progress { get; init; }
        public required IReadOnlyDictionary<(Guid Target, Guid Student), ModuleWorkEvidence> Assignments { get; init; }
        public required IReadOnlyDictionary<(Guid Target, Guid Student), ModuleWorkEvidence> Quizzes { get; init; }
        public required HashSet<(Guid Thread, Guid User)> Contributions { get; init; }
        public required IReadOnlyDictionary<Guid, Guid> LtiAssignments { get; init; }
        public required IReadOnlyDictionary<(Guid Target, Guid Student), ModuleWorkEvidence> Scorm { get; init; }
        public required IReadOnlyDictionary<Guid, Guid> StudentUsers { get; init; }
        public IEnumerable<ModuleItem> ItemsOf(Guid moduleId) => Items.Where(i => i.ModuleId == moduleId).OrderBy(i => i.Position);
    }

    private static List<CourseContentModule> Ordered(IEnumerable<CourseContentModule> modules)
        => modules.OrderBy(m => m.WeekNumber).ThenBy(m => m.CreatedAt).ThenBy(m => m.Title).ToList();

    /// <summary>Loads everything needed to evaluate the given students (published content only, unless asked).</summary>
    private async Task<Context?> LoadAsync(Guid offeringId, IReadOnlyCollection<Guid> studentIds, CancellationToken ct, bool includeUnpublished = false)
    {
        var offering = await _repo.GetOfferingAsync(offeringId, ct);
        if (offering is null) return null;
        var modules = Ordered((await _repo.GetModulesAsync(offeringId, ct)).Where(m => includeUnpublished || m.IsPublished));
        var linkables = (await _repo.GetLinkablesAsync(offeringId, ct)).Select(l => (l.Type, l.Id)).ToHashSet();
        var items = (await _repo.GetItemsAsync(offeringId, ct))
            .Where(i => modules.Any(m => m.Id == i.ModuleId) && (includeUnpublished || i.IsPublished))
            .Where(i => !ModuleItem.NeedsTarget(i.ItemType) || linkables.Contains((i.ItemType, i.TargetId!.Value)))
            .ToList();
        var prerequisites = await _repo.GetPrerequisitesAsync(modules.Select(m => m.Id).ToList(), ct);
        var progress = studentIds.Count == 0 ? new List<ModuleItemProgress>() : (await _repo.GetProgressAsync(offeringId, studentIds, ct)).ToList();

        var ltiLinks = items.Where(i => i.ItemType == ModuleItemType.ExternalTool).Select(i => i.TargetId!.Value).ToList();
        var ltiAssignments = ltiLinks.Count == 0 ? new Dictionary<Guid, Guid>() : await _repo.GetLtiAssignmentsAsync(ltiLinks, ct);
        var assignmentIds = items.Where(i => i.ItemType == ModuleItemType.Assignment).Select(i => i.TargetId!.Value).Concat(ltiAssignments.Values).Distinct().ToList();
        var quizIds = items.Where(i => i.ItemType == ModuleItemType.Quiz).Select(i => i.TargetId!.Value).Distinct().ToList();
        var threadIds = items.Where(i => i.ItemType == ModuleItemType.Discussion).Select(i => i.TargetId!.Value).Distinct().ToList();
        var scormIds = items.Where(i => i.ItemType == ModuleItemType.ScormPackage).Select(i => i.TargetId!.Value).Distinct().ToList();

        var students = new Dictionary<Guid, Guid>();
        if (threadIds.Count > 0)
            foreach (var id in studentIds)
                if (await _repo.GetStudentAsync(id, ct) is { } s) students[id] = s.UserId;

        return new Context
        {
            Offering = offering, Modules = modules, Items = items, Prerequisites = prerequisites, Progress = progress,
            Assignments = studentIds.Count == 0 || assignmentIds.Count == 0 ? new Dictionary<(Guid, Guid), ModuleWorkEvidence>()
                : (await _repo.GetAssignmentEvidenceAsync(assignmentIds, studentIds, ct)).ToDictionary(e => (e.TargetId, e.StudentProfileId)),
            Quizzes = studentIds.Count == 0 || quizIds.Count == 0 ? new Dictionary<(Guid, Guid), ModuleWorkEvidence>()
                : (await _repo.GetQuizEvidenceAsync(quizIds, studentIds, ct)).ToDictionary(e => (e.TargetId, e.StudentProfileId)),
            Contributions = students.Count == 0 ? [] : (await _repo.GetDiscussionContributionsAsync(threadIds, students.Values.ToList(), ct)).ToHashSet(),
            LtiAssignments = ltiAssignments,
            Scorm = studentIds.Count == 0 || scormIds.Count == 0 ? new Dictionary<(Guid, Guid), ModuleWorkEvidence>()
                : (await _repo.GetScormEvidenceAsync(scormIds, studentIds, ct)).ToDictionary(e => (e.TargetId, e.StudentProfileId)),
            StudentUsers = students
        };
    }

    private async Task<IReadOnlyList<ModuleState>> EvaluateAsync(Context c, Guid studentId, bool persist, CancellationToken ct)
    {
        var snapshots = c.Modules.Select(m => new ModuleSnapshot(m.Id, m.Title, m.UnlockAt, m.RequireSequentialProgress, m.CompletionMode,
            c.Prerequisites.Where(p => p.ModuleId == m.Id).Select(p => p.PrerequisiteModuleId).ToList(),
            c.ItemsOf(m.Id).Select(i => new ModuleItemSnapshot(i.Id, i.Title, i.RequirementType, i.MinScorePercent)).ToList())).ToList();
        var items = c.Items.ToDictionary(i => i.Id);
        var progress = c.Progress.Where(p => p.StudentProfileId == studentId).ToDictionary(p => p.ModuleItemId);

        ModuleItemEvidence Evidence(Guid itemId)
        {
            var item = items[itemId];
            progress.TryGetValue(itemId, out var p);
            ModuleWorkEvidence? work = null;
            var contributed = false;
            switch (item.ItemType)
            {
                case ModuleItemType.Assignment: c.Assignments.TryGetValue((item.TargetId!.Value, studentId), out work); break;
                case ModuleItemType.Quiz: c.Quizzes.TryGetValue((item.TargetId!.Value, studentId), out work); break;
                case ModuleItemType.ExternalTool:
                    if (c.LtiAssignments.TryGetValue(item.TargetId!.Value, out var assignmentId)) c.Assignments.TryGetValue((assignmentId, studentId), out work);
                    break;
                case ModuleItemType.ScormPackage: c.Scorm.TryGetValue((item.TargetId!.Value, studentId), out work); break;
                case ModuleItemType.Discussion:
                    contributed = c.StudentUsers.TryGetValue(studentId, out var userId) && c.Contributions.Contains((item.TargetId!.Value, userId));
                    break;
            }
            return new ModuleItemEvidence(p?.ViewedAt is not null, p?.MarkedDoneAt is not null, work?.Submitted == true, work?.ScorePercent, contributed,
                p?.CompletedAt is not null);
        }

        var now = _now();
        var states = ModuleProgressEvaluator.Evaluate(snapshots, Evidence, now);
        if (persist)
        {
            var changed = false;
            foreach (var state in states.SelectMany(m => m.Items).Where(i => i.NewlyCompleted))
            {
                if (!progress.TryGetValue(state.ItemId, out var row))
                {
                    row = new ModuleItemProgress(studentId, state.ItemId, c.Offering.OfferingId);
                    _repo.Add(row);
                    c.Progress.Add(row);
                    progress[state.ItemId] = row;
                }
                row.Complete(now);
                changed = true;
            }
            if (changed) await _repo.SaveChangesAsync(ct);
        }
        return states;
    }

    private async Task<ModuleItemProgress> ProgressRowAsync(Context c, Guid studentId, ModuleItem item)
    {
        var row = c.Progress.FirstOrDefault(p => p.StudentProfileId == studentId && p.ModuleItemId == item.Id);
        if (row is not null) return row;
        row = new ModuleItemProgress(studentId, item.Id, item.OfferingId);
        _repo.Add(row);
        c.Progress.Add(row);
        await Task.CompletedTask;
        return row;
    }

    private async Task<ModuleItemOpenResponse> OpenResponseAsync(ModuleItem item, ModuleItemState? state, CancellationToken ct)
    {
        string? embed = null, storage = null, url = item.Url;
        var hasFile = false;
        if (state is not { Locked: true })
        {
            if (item.ItemType == ModuleItemType.Video && await _repo.GetVideoAsync(item.TargetId!.Value, ct) is { } video)
                (embed, storage) = video;
            if (item.ItemType == ModuleItemType.Material && await _repo.GetMaterialAsync(item.TargetId!.Value, ct) is { } material)
            {
                url = material.LinkUrl;
                hasFile = material.HasFile;
            }
        }
        var locked = state?.Locked == true;
        return new ModuleItemOpenResponse(item.Id, item.ItemType.ToString(), item.TargetId, item.Title, item.OfferingId, locked, state?.LockReason,
            locked ? null : url, locked ? null : item.Body, embed, storage, hasFile, item.RequirementType.ToString(),
            state is null ? "none" : Status(state), state is { Locked: false } && item.RequirementType == ModuleRequirementType.MustMarkDone);
    }

    private static ModuleItemState? Find(IReadOnlyList<ModuleState> states, Guid itemId)
        => states.SelectMany(m => m.Items).FirstOrDefault(i => i.ItemId == itemId);

    private static string Status(ModuleItemState s) => s.Locked && !s.Completed ? "locked" : !s.HasRequirement ? "none" : s.Completed ? "done" : "todo";

    private static ModuleRequirementType ParseRequirement(string? value)
        => string.IsNullOrWhiteSpace(value) ? ModuleRequirementType.None
            : Enum.TryParse<ModuleRequirementType>(value, true, out var r) && Enum.IsDefined(r) ? r
            : throw new ArgumentException("Unknown completion requirement.");

    private static ModuleItemResponse ToResponse(ModuleItem i, HashSet<(ModuleItemType, Guid)> linkables)
        => new(i.Id, i.ModuleId, i.ItemType.ToString(), i.TargetId, i.Title, i.Url, i.Body, i.Position, i.IndentLevel, i.IsPublished,
            i.RequirementType.ToString(), i.MinScorePercent,
            ModuleItem.NeedsTarget(i.ItemType) && !linkables.Contains((i.ItemType, i.TargetId!.Value)),
            ModuleItem.AllowedRequirements(i.ItemType).Select(r => r.ToString()).ToList());
}
