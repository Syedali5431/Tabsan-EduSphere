using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Application.Lms;

// Plan M7.2/M7.3 — completion and release rules (pure, so they can be unit tested).

public sealed record ModuleItemSnapshot(Guid Id, string Title, ModuleRequirementType Requirement, decimal? MinScorePercent);

public sealed record ModuleSnapshot(Guid Id, string Title, DateTime? UnlockAt, bool RequireSequentialProgress, ModuleCompletionMode CompletionMode,
    IReadOnlyList<Guid> PrerequisiteModuleIds, IReadOnlyList<ModuleItemSnapshot> Items);

/// <summary>What the student has done on an item. <see cref="CompletedBefore"/> is the stored completion.</summary>
public sealed record ModuleItemEvidence(bool Viewed, bool MarkedDone, bool Submitted, decimal? ScorePercent, bool Contributed, bool CompletedBefore)
{
    public static readonly ModuleItemEvidence None = new(false, false, false, null, false, false);
}

public sealed record ModuleItemState(Guid ItemId, bool HasRequirement, bool Completed, bool Locked, string? LockReason, bool NewlyCompleted);

public sealed record ModuleState(Guid ModuleId, bool Locked, string? LockReason, bool Completed, int Required, int Done, IReadOnlyList<ModuleItemState> Items)
{
    public int Percent => Required == 0 ? (Completed ? 100 : 0) : (int)Math.Round(Done * 100m / Required, MidpointRounding.AwayFromZero);
}

/// <summary>
/// <list type="bullet">
///   <item>A module is locked before its unlock date, or until every prerequisite module is complete (a cycle keeps them locked).</item>
///   <item>With sequential progress, an item is locked until every earlier item with a requirement is complete.</item>
///   <item>Requirements: viewed, marked done, submitted, posted score ≥ minimum %, or posted in the discussion. Once met an item stays
///         complete (the stored completion), so a later regrade does not lock students out again.</item>
///   <item>A module is complete when all (or, in "any" mode, one) of its required items are complete; a module without requirements is
///         complete as soon as it is unlocked. Locked modules and items never complete.</item>
/// </list>
/// </summary>
public static class ModuleProgressEvaluator
{
    public static bool RequirementMet(ModuleRequirementType requirement, decimal? minScorePercent, ModuleItemEvidence e) => requirement switch
    {
        ModuleRequirementType.MustView => e.Viewed,
        ModuleRequirementType.MustMarkDone => e.MarkedDone,
        ModuleRequirementType.MustSubmit => e.Submitted,
        ModuleRequirementType.MustScoreAtLeast => e.ScorePercent is { } s && minScorePercent is { } min && s >= min,
        ModuleRequirementType.MustContribute => e.Contributed,
        _ => false
    };

    /// <summary>Evaluates modules given in course order.</summary>
    public static IReadOnlyList<ModuleState> Evaluate(IReadOnlyList<ModuleSnapshot> modules, Func<Guid, ModuleItemEvidence> evidence, DateTime now)
    {
        var byId = modules.ToDictionary(m => m.Id);
        var results = new Dictionary<Guid, ModuleState>();
        var visiting = new HashSet<Guid>();

        ModuleState Eval(ModuleSnapshot module)
        {
            if (results.TryGetValue(module.Id, out var done)) return done;
            visiting.Add(module.Id);

            string? lockReason = null;
            if (module.UnlockAt is { } unlock && unlock > now)
                lockReason = $"Opens {unlock:dd MMM yyyy, HH:mm} (UTC)";
            else
            {
                var missing = new List<string>();
                foreach (var prereqId in module.PrerequisiteModuleIds.Distinct())
                {
                    if (!byId.TryGetValue(prereqId, out var prereq)) continue; // unpublished or removed modules do not block
                    if (visiting.Contains(prereqId)) { missing.Add(prereq.Title); continue; } // cycle
                    if (!Eval(prereq).Completed) missing.Add(prereq.Title);
                }
                if (missing.Count > 0) lockReason = "Complete " + string.Join(", ", missing.Select(t => $"“{t}”")) + " first";
            }
            var moduleLocked = lockReason is not null;

            var items = new List<ModuleItemState>();
            string? blockedBy = null;
            foreach (var item in module.Items)
            {
                var hasRequirement = item.Requirement != ModuleRequirementType.None;
                var e = evidence(item.Id);
                var itemLocked = moduleLocked || (module.RequireSequentialProgress && blockedBy is not null);
                var reason = moduleLocked ? lockReason : itemLocked ? $"Complete “{blockedBy}” first" : null;
                var met = !itemLocked && hasRequirement && RequirementMet(item.Requirement, item.MinScorePercent, e);
                var completed = hasRequirement && (e.CompletedBefore || met);
                items.Add(new ModuleItemState(item.Id, hasRequirement, completed, itemLocked, reason, met && !e.CompletedBefore));
                if (hasRequirement && !completed && blockedBy is null) blockedBy = item.Title;
            }

            var required = items.Count(i => i.HasRequirement);
            var doneCount = items.Count(i => i.HasRequirement && i.Completed);
            var moduleCompleted = !moduleLocked && (required == 0
                || (module.CompletionMode == ModuleCompletionMode.Any ? doneCount > 0 : doneCount == required));
            var state = new ModuleState(module.Id, moduleLocked, lockReason, moduleCompleted, required, doneCount, items);
            visiting.Remove(module.Id);
            results[module.Id] = state;
            return state;
        }

        return modules.Select(Eval).ToList();
    }

    /// <summary>Course progress: completed requirements out of all requirements (published modules).</summary>
    public static int CoursePercent(IReadOnlyList<ModuleState> modules)
    {
        var required = modules.Sum(m => m.Required);
        return required == 0 ? 0 : (int)Math.Round(modules.Sum(m => m.Done) * 100m / required, MidpointRounding.AwayFromZero);
    }
}
