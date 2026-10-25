using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Application.Lms.Scorm;

/// <summary>A message for the learner: a fixed text (translated by the portal) with at most one value in place of {0}.</summary>
public sealed record SeqProblem(string Text, string? Arg = null)
{
    public override string ToString() => Arg is null ? Text : Text.Replace("{0}", Arg);
}

/// <summary>What a navigation request led to: a part to open (a new attempt or a resumed one), the end of the package, or a refusal.</summary>
public sealed record SeqDelivery(string? ActivityId, string? PartId, bool NewAttempt, bool Ended, SeqProblem? Message)
{
    public bool Delivered => PartId is not null;
}

/// <summary>One line of the contents list: a visible cluster (heading) or a leaf with whether the learner may choose it now.</summary>
public sealed record SeqItemView(string ActivityId, string? PartId, string Title, int Depth, bool IsLeaf, bool CanChoose, SeqProblem? Reason, bool IsCurrent);

public sealed record SeqView(IReadOnlyList<SeqItemView> Items, bool CanContinue, bool CanPrevious, bool Ended, IReadOnlyList<string> HideLmsUi);

/// <summary>A part's reported result: completion, success and scaled score, plus its cmi.objectives.</summary>
public sealed record SeqReport(string Completion, string Success, decimal? Scaled, IReadOnlyList<SeqObjectiveReport> Objectives, string? Exit);

public sealed record SeqObjectiveReport(string Id, string? Success, decimal? Scaled, string? Completion);

/// <summary>
/// SCORM 2004 sequencing for one learner (the ADL Sequencing and Navigation behaviour for the parts used by real packages):
/// start / resume, continue, previous, choice, exit, exit all and suspend all; control modes (choice, choice exit, flow,
/// forward only); pre-condition (skip, disabled, hidden from choice, stop forward traversal), exit and post-condition
/// (exit parent, exit all, retry, retry all, continue, previous) rules; attempt limits; randomized order and selection;
/// delivery controls; objectives with global (shared) objectives; measure, satisfaction and completion rollup with rollup
/// rules and rollup considerations. Time limits are not used. The caller saves <see cref="State"/> afterwards.
/// </summary>
public sealed class ScormSequencer
{
    private readonly SeqTree _tree;
    private readonly Dictionary<string, SeqActivity> _byId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SeqActivity> _parent = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SeqActivity> _byPart = new(StringComparer.Ordinal);
    private readonly Random _random;

    public ScormSequencer(SeqTree tree, SeqState state, Random? random = null)
    {
        _tree = tree;
        State = state;
        _random = random ?? Random.Shared;
        Index(tree.Root, null);
    }

    public SeqState State { get; }

    public SeqActivity Root => _tree.Root;

    private void Index(SeqActivity a, SeqActivity? parent)
    {
        _byId[a.Id] = a;
        if (parent is not null) _parent[a.Id] = parent;
        if (a.PartId is not null) _byPart.TryAdd(a.PartId, a);
        foreach (var child in a.Children) Index(child, a);
    }

    public SeqActivity? ActivityOfPart(string partId) => _byPart.GetValueOrDefault(partId);

    // ═════════════════════════════════════════════════════════════════════════
    // Navigation
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Opening the package: the part being worked on (resumed when its attempt is still open, otherwise a new attempt on it),
    /// or the first part the flow reaches from the beginning.
    /// </summary>
    public SeqDelivery Start()
    {
        if (!State.Ended && State.Current is { } id && _byId.TryGetValue(id, out var current) && current.IsLeaf)
        {
            // The part already open carries on (the player was opened again), even where a new attempt on it would now be
            // refused (a post test on its last allowed attempt is "disabled" for further attempts).
            if (St(current).Active) return Deliver(current, check: false);
            if (PathProblem(current) is null) return Deliver(current);
        }
        State.Ended = false;
        State.Suspended = false;
        State.Current = null;
        return FlowInto(Root, checkFlow: true);
    }

    /// <summary>
    /// A navigation request: start, resumeAll, continue, previous, choice (with a target), exit, exitAll, suspendAll, abandon,
    /// abandonAll. Continue, previous and choice are first tried on a copy of the state: a request that cannot deliver
    /// anything is refused without ending the open part.
    /// </summary>
    public SeqDelivery Navigate(string request, string? target = null)
    {
        if (request is "continue" or "previous" or "choice")
        {
            var copy = System.Text.Json.JsonSerializer.Deserialize<SeqState>(System.Text.Json.JsonSerializer.Serialize(State))!;
            var trial = new ScormSequencer(_tree, copy, new Random(0)).Apply(request, target);
            if (!trial.Delivered && !trial.Ended) return trial;
        }
        return Apply(request, target);
    }

    private SeqDelivery Apply(string request, string? target)
    {
        var current = State.Current is { } id ? _byId.GetValueOrDefault(id) : null;
        switch (request)
        {
            case "start":
            case "resumeAll":
                return Start();
            case "continue":
                if (current is null) return Start();
                if (Parent(current) is not { Flow: true }) return Refuse("Next is not used in this part of the package; choose from the contents.");
                return Terminate(current, "continue", null);
            case "previous":
                if (current is null) return Refuse("There is nothing before this.");
                if (Parent(current) is not { Flow: true, ForwardOnly: false }) return Refuse("This part of the package only goes forward.");
                return Terminate(current, "previous", null);
            case "choice":
                if (target is null || !_byId.TryGetValue(target, out var chosen) && !_byPart.TryGetValue(target, out chosen))
                    return Refuse("That part is not in this package.");
                if (ChoiceProblem(chosen) is { } problem) return Refuse(problem);
                if (current is null) return Choose(chosen);
                if (current == chosen && State.Activities.GetValueOrDefault(current.Id)?.Active == true) return Deliver(current);
                return Terminate(current, "choice", chosen);
            case "exit":
            case "abandon":
                return current is null ? Refuse("Nothing is open.") : Terminate(current, null, null);
            case "exitAll":
            case "abandonAll":
                EndAll();
                return End();
            case "suspendAll":
                State.Suspended = current is not null;
                return new SeqDelivery(null, null, false, false, new SeqProblem("Your place is saved."));
            default:
                return Refuse("Unknown navigation request.");
        }
    }

    /// <summary>Ends the open attempt, applies exit and post-condition rules, then carries out the resulting request.</summary>
    private SeqDelivery Terminate(SeqActivity current, string? request, SeqActivity? target)
    {
        EndAttempt(current);

        // Exit rules, from the root down: an ancestor whose exit rule holds ends its attempt and becomes the current activity.
        foreach (var ancestor in PathFromRoot(current).Where(a => a != current))
        {
            if (RuleAction(ancestor, ancestor.ExitRules, "exit") is null) continue;
            for (var a = current; a != ancestor && a is not null; a = Parent(a)) EndAttempt(a);
            EndAttempt(ancestor);
            current = ancestor;
            break;
        }

        // Post-condition rules (exit parent repeats them on the parent).
        while (true)
        {
            var action = RuleAction(current, current.PostRules, "exitParent", "exitAll", "retry", "retryAll", "continue", "previous");
            if (action == "exitParent" && Parent(current) is { } parent)
            {
                EndAttempt(parent);
                current = parent;
                continue;
            }
            switch (action)
            {
                case "exitAll":
                    EndAll();
                    return End();
                case "retry":
                    return Retry(current);
                case "retryAll":
                    EndAll();
                    State.Ended = false;
                    return Retry(Root);
                case "continue":
                    request = "continue";
                    target = null;
                    break;
                case "previous":
                    request = "previous";
                    target = null;
                    break;
            }
            break;
        }

        RollupAll();
        return request switch
        {
            "continue" => current == Root ? EndAt(current) : FlowForward(current),
            "previous" => FlowBackward(current),
            "choice" when target is not null => Choose(target),
            _ => new SeqDelivery(null, null, false, false, null)
        };
    }

    private SeqDelivery EndAt(SeqActivity _)
    {
        EndAll();
        return End();
    }

    private SeqDelivery Retry(SeqActivity activity)
    {
        // A new attempt: the activity and everything under it start again when delivered (global objectives stay).
        foreach (var a in Subtree(activity))
        {
            var st = St(a);
            st.Active = false;
            st.Skipped = false;
        }
        State.Current = null;
        if (activity.IsLeaf)
        {
            var problem = PathProblem(activity);
            return problem is null ? Deliver(activity, forceNewAttempt: true) : Refuse(problem);
        }
        return FlowInto(activity, checkFlow: true);
    }

    private SeqDelivery Choose(SeqActivity target)
        => target.IsLeaf ? Deliver(target) : FlowInto(target, checkFlow: false);

    /// <summary>The next leaf after <paramref name="from"/> (whose attempt has ended) in the order of the tree.</summary>
    private SeqDelivery FlowForward(SeqActivity from)
    {
        // Stop forward traversal: the learner cannot flow past an activity (or its subtree) whose rule holds.
        foreach (var a in PathFromRoot(from))
            if (RuleAction(a, a.PreRules, "stopForwardTraversal") is not null)
                return Refuse("The package does not go further than \"{0}\" here.", a.Title);
        var candidate = NextInTree(from, enterChildren: false);
        return Traverse(candidate, forward: true);
    }

    private SeqDelivery FlowBackward(SeqActivity from)
    {
        var candidate = PreviousInTree(from);
        return Traverse(candidate, forward: false);
    }

    /// <summary>Flows into a cluster (or the root) from its first child.</summary>
    private SeqDelivery FlowInto(SeqActivity cluster, bool checkFlow)
    {
        if (cluster.IsLeaf) return Deliver(cluster);
        if (checkFlow && !cluster.Flow)
            return new SeqDelivery(null, null, false, false, new SeqProblem("Choose a part from the contents to begin."));
        PrepareChildren(cluster);
        var first = ChildrenOf(cluster).FirstOrDefault();
        return first is null ? End() : Traverse(first, forward: true, checkFlow);
    }

    private SeqDelivery Traverse(SeqActivity? candidate, bool forward, bool checkFlow = true)
    {
        var guard = 0;
        while (candidate is not null && guard++ < 10_000)
        {
            var parent = Parent(candidate);
            if (checkFlow && parent is not null && (!parent.Flow || (!forward && parent.ForwardOnly)))
                return Refuse(forward ? "Next is not used in this part of the package; choose from the contents." : "This part of the package only goes forward.");
            if (RuleAction(candidate, candidate.PreRules, "skip") is not null)
            {
                St(candidate).Skipped = true;
                candidate = forward ? NextInTree(candidate, enterChildren: false) : PreviousInTree(candidate);
                continue;
            }
            if (candidate.IsLeaf && candidate.PartId is null)
            {
                // An item with nothing to launch is passed over.
                candidate = forward ? NextInTree(candidate, enterChildren: false) : PreviousInTree(candidate);
                continue;
            }
            if (ActivityProblem(candidate) is { } problem) return Refuse(problem);
            if (!candidate.IsLeaf)
            {
                // Entering a cluster for a new attempt: what its children skipped last time no longer counts.
                if (!St(candidate).Active) foreach (var child in candidate.Children) St(child).Skipped = false;
                PrepareChildren(candidate);
                var children = ChildrenOf(candidate);
                if (children.Count == 0)
                {
                    candidate = forward ? NextInTree(candidate, enterChildren: false) : PreviousInTree(candidate);
                    continue;
                }
                candidate = forward || candidate.ForwardOnly ? children[0] : children[^1];
                continue;
            }
            return Deliver(candidate);
        }
        if (forward)
        {
            EndAll();
            return End();
        }
        return Refuse("There is nothing before this.");
    }

    /// <summary>Delivers a leaf: new attempts on it and on its ancestors that are not active; a resumed attempt when it is still open.</summary>
    private SeqDelivery Deliver(SeqActivity leaf, bool forceNewAttempt = false, bool check = true)
    {
        if (check && PathProblem(leaf) is { } problem) return Refuse(problem);
        // Leaving another part ends its attempt and those of its groups that do not contain the new part.
        if (State.Current is { } previous && previous != leaf.Id && _byId.TryGetValue(previous, out var old))
            foreach (var a in Enumerable.Reverse(PathFromRoot(old)).Where(a => !IsAncestorOrSelf(a, leaf)))
                EndAttempt(a);
        var newAttempt = forceNewAttempt || !St(leaf).Active;
        foreach (var a in PathFromRoot(leaf))
        {
            var st = St(a);
            if (st.Active && !(a == leaf && forceNewAttempt)) continue;
            st.Attempts++;
            st.Active = true;
            st.Skipped = false;
            // A new attempt starts with unknown completion and objective progress (shared objectives are kept).
            st.Completion = null;
            st.Satisfied = null;
            st.Measure = null;
            st.Objectives.Clear();
        }
        State.Current = leaf.Id;
        State.Ended = false;
        State.Suspended = false;
        RollupAll();
        return new SeqDelivery(leaf.Id, leaf.PartId, newAttempt, false, null);
    }

    private SeqDelivery End()
    {
        State.Current = null;
        State.Ended = true;
        State.Suspended = false;
        RollupAll();
        return new SeqDelivery(null, null, false, true, new SeqProblem("You have reached the end of this package."));
    }

    private static SeqDelivery Refuse(string text, string? arg = null) => new(null, null, false, false, new SeqProblem(text, arg));

    private static SeqDelivery Refuse(SeqProblem problem) => new(null, null, false, false, problem);

    // ═════════════════════════════════════════════════════════════════════════
    // Results reported by parts
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>A part's commit: its completion, success, measure and objectives. The final commit of a session ends its attempt (unless suspended).</summary>
    public void Report(string partId, SeqReport report, bool final)
    {
        if (!_byPart.TryGetValue(partId, out var leaf)) return;
        var st = St(leaf);
        // The SCO's current values; "unknown" means not known (the LMS may complete or satisfy the activity when it ends).
        st.Completion = report.Completion switch { "completed" => "completed", "incomplete" or "not attempted" => "incomplete", _ => null };
        st.Satisfied = report.Success switch { "passed" => true, "failed" => false, _ => null };
        if (report.Scaled is { } scaled) st.Measure = Math.Clamp(scaled, -1m, 1m);
        foreach (var o in report.Objectives)
        {
            if (string.IsNullOrEmpty(o.Id)) continue;
            if (o.Id == leaf.PrimaryObjective.Id)
            {
                // cmi.objectives entry of the primary objective: used when the SCO left success_status / score.scaled unset.
                if (report.Success is not ("passed" or "failed")) st.Satisfied = o.Success switch { "passed" => true, "failed" => false, _ => st.Satisfied };
                if (report.Scaled is null && o.Scaled is { } primaryMeasure) st.Measure = Math.Clamp(primaryMeasure, -1m, 1m);
                continue;
            }
            if (leaf.Objectives.All(x => x.Id != o.Id)) continue;
            var local = st.Objectives.TryGetValue(o.Id, out var existing) ? existing : st.Objectives[o.Id] = new SeqObjectiveState();
            local.Satisfied = o.Success switch { "passed" => true, "failed" => false, _ => local.Satisfied };
            if (o.Scaled is { } m) local.Measure = Math.Clamp(m, -1m, 1m);
        }
        WriteGlobals(leaf);
        if (final && report.Exit is not ("suspend" or "logout")) EndAttempt(leaf);
        RollupAll();
    }

    /// <summary>
    /// Progress made before the package had sequencing (an attempt from before the upgrade): each part's reported result
    /// becomes its activity's, so skip and prerequisite rules see what the learner already did.
    /// </summary>
    public void Adopt(string partId, string completion, string success, decimal? scaled)
    {
        if (!_byPart.TryGetValue(partId, out var leaf)) return;
        var st = St(leaf);
        st.Attempts = Math.Max(st.Attempts, 1);
        st.Completion = completion switch { "completed" => "completed", "incomplete" => "incomplete", _ => st.Completion };
        st.Satisfied = success switch { "passed" => true, "failed" => false, _ => st.Satisfied };
        if (scaled is { } m) st.Measure = Math.Clamp(m, -1m, 1m);
        WriteGlobals(leaf);
        RollupAll();
    }

    /// <summary>A page without the runtime (an asset) is complete, and its objective met, once it has been delivered.</summary>
    public void Viewed(string partId)
    {
        if (!_byPart.TryGetValue(partId, out var leaf)) return;
        var st = St(leaf);
        st.Completion ??= "completed";
        if (!leaf.ObjectiveSetByContent) st.Satisfied ??= true;
        WriteGlobals(leaf);
        RollupAll();
    }

    /// <summary>The package's result: the root activity's completion, success and measure.</summary>
    public (string Completion, string Success, decimal? Measure) Result()
    {
        var st = St(Root);
        var anyStarted = State.Activities.Values.Any(a => a.Attempts > 0);
        var completion = st.Completion ?? (anyStarted ? "incomplete" : "not attempted");
        var satisfied = Satisfied(Root);
        return (completion, satisfied is true ? "passed" : satisfied is false ? "failed" : "unknown", Measure(Root));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // What the learner may do now (contents list and buttons)
    // ═════════════════════════════════════════════════════════════════════════

    public SeqView View()
    {
        var items = new List<SeqItemView>();
        void Walk(SeqActivity a, int depth)
        {
            foreach (var child in ChildrenOf(a))
            {
                var hidden = !child.Visible || RuleAction(child, child.PreRules, "hiddenFromChoice") is not null;
                if (!hidden)
                {
                    var problem = child.IsLeaf ? ChoiceProblem(child) : null;
                    items.Add(new SeqItemView(child.Id, child.PartId, child.Title, depth, child.IsLeaf, child.IsLeaf && problem is null, problem, child.Id == State.Current));
                }
                Walk(child, hidden ? depth : depth + 1);
            }
        }
        Walk(Root, 0);

        var current = State.Current is { } id ? _byId.GetValueOrDefault(id) : null;
        var hide = current is null ? [] : PathFromRoot(current).SelectMany(a => a.HideLmsUi).Distinct(StringComparer.Ordinal).ToList();
        var parent = current is null ? null : Parent(current);
        var canContinue = current is null ? Root.Flow : parent?.Flow == true && !hide.Contains("continue");
        var canPrevious = parent is { Flow: true, ForwardOnly: false } && !hide.Contains("previous");
        return new SeqView(items, canContinue, canPrevious, State.Ended, hide);
    }

    /// <summary>Why the learner may not choose an activity now (null when they may).</summary>
    public SeqProblem? ChoiceProblem(SeqActivity target)
    {
        if (target == Root) return new SeqProblem("Choose a part of the package.");
        foreach (var a in PathFromRoot(target).Where(a => a != Root))
        {
            if (Parent(a) is { Choice: false }) return new SeqProblem("This part opens through the package's own order; use Next.");
            if (RuleAction(a, a.PreRules, "hiddenFromChoice") is not null) return new SeqProblem("This part is not offered.");
        }
        if (PathProblem(target) is { } problem) return problem;
        if (State.Current is { } id && _byId.TryGetValue(id, out var current) && current != target)
        {
            foreach (var a in PathFromRoot(current))
                if (St(a).Active && !IsAncestorOrSelf(a, target) && !a.ChoiceExit)
                    return new SeqProblem("Finish \"{0}\" before choosing another part.", a.Title);
            var common = PathFromRoot(target).LastOrDefault(a => IsAncestorOrSelf(a, current));
            if (common is { ForwardOnly: true })
            {
                var children = ChildrenOf(common);
                var targetBranch = PathFromRoot(target).FirstOrDefault(a => Parent(a) == common);
                var currentBranch = PathFromRoot(current).FirstOrDefault(a => Parent(a) == common);
                if (targetBranch is not null && currentBranch is not null && children.IndexOf(targetBranch) < children.IndexOf(currentBranch))
                    return new SeqProblem("This part of the package only goes forward.");
            }
        }
        return null;
    }

    /// <summary>Disabled or out of attempts anywhere from the root down to the activity.</summary>
    private SeqProblem? PathProblem(SeqActivity activity)
    {
        foreach (var a in PathFromRoot(activity))
            if (ActivityProblem(a) is { } problem) return problem;
        return null;
    }

    private SeqProblem? ActivityProblem(SeqActivity a)
    {
        if (RuleAction(a, a.PreRules, "disabled") is not null) return new SeqProblem("\"{0}\" is not available yet.", a.Title);
        var st = St(a);
        if (a.AttemptLimit > 0 && !st.Active && st.Attempts >= a.AttemptLimit) return new SeqProblem("You have used all attempts at \"{0}\".", a.Title);
        return null;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Attempts and rollup
    // ═════════════════════════════════════════════════════════════════════════

    private void EndAttempt(SeqActivity a)
    {
        var st = St(a);
        if (!st.Active) return;
        if (a.IsLeaf && a.Tracked)
        {
            // Delivery controls: when the content does not set them, ending the attempt completes it and meets its objective.
            if (!a.CompletionSetByContent && st.Completion is null) st.Completion = "completed";
            if (!a.ObjectiveSetByContent && st.Satisfied is null) st.Satisfied = true;
        }
        st.Active = false;
        WriteGlobals(a);
        RollupAll();
    }

    private void EndAll()
    {
        if (State.Current is { } id && _byId.TryGetValue(id, out var current))
            for (var a = current; a is not null; a = Parent(a)) EndAttempt(a);
        foreach (var a in _byId.Values) St(a).Active = false;
        State.Current = null;
    }

    /// <summary>Rolls every cluster up from its children, deepest first.</summary>
    private void RollupAll()
    {
        void Post(SeqActivity a)
        {
            foreach (var child in a.Children) Post(child);
            if (!a.IsLeaf) RollupCluster(a);
        }
        Post(Root);
    }

    private void RollupCluster(SeqActivity c)
    {
        var st = St(c);
        var children = ChildrenOf(c).Where(k => k.Tracked).ToList();

        // Measure: the weighted average of the children's measures.
        decimal total = 0, counted = 0;
        var valid = false;
        foreach (var k in children.Where(k => k.ObjectiveMeasureWeight > 0))
        {
            counted += k.ObjectiveMeasureWeight;
            if (Measure(k) is { } m) { valid = true; total += m * k.ObjectiveMeasureWeight; }
        }
        if (counted > 0) st.Measure = valid ? Math.Round(total / counted, 4) : null;

        // Objective: by measure; else by the rollup rules (a "satisfied" rule wins over a "not satisfied" one); else by
        // default — satisfied when every counted child is, not satisfied as soon as a known one is not.
        var primary = c.PrimaryObjective;
        var objectiveRules = c.RollupRules.Where(r => r.Action is "satisfied" or "notSatisfied").ToList();
        if (primary.SatisfiedByMeasure && st.Measure is { } measure && (!st.Active || c.MeasureSatisfactionIfActive))
            st.Satisfied = measure >= primary.MinNormalizedMeasure;
        else if (objectiveRules.Count > 0)
        {
            if (objectiveRules.Where(r => r.Action == "satisfied").Any(r => RollupFires(c, r))) st.Satisfied = true;
            else if (objectiveRules.Where(r => r.Action == "notSatisfied").Any(r => RollupFires(c, r))) st.Satisfied = false;
        }
        else
        {
            var members = children.Where(k => Contributes(k, "satisfied") && Contributes(k, "notSatisfied")).ToList();
            if (members.Count > 0)
            {
                var values = members.Select(k => Satisfied(k)).ToList();
                if (values.Any(v => v == false)) st.Satisfied = false;
                else if (values.All(v => v == true)) st.Satisfied = true;
            }
        }

        // Completion: the same way ("completed" rules before "incomplete" ones; by default completed when every counted
        // child is, incomplete as soon as a known one is not, or when no child counts).
        var completionRules = c.RollupRules.Where(r => r.Action is "completed" or "incomplete").ToList();
        if (completionRules.Count > 0)
        {
            if (completionRules.Where(r => r.Action == "completed").Any(r => RollupFires(c, r))) st.Completion = "completed";
            else if (completionRules.Where(r => r.Action == "incomplete").Any(r => RollupFires(c, r))) st.Completion = "incomplete";
        }
        else
        {
            var members = children.Where(k => Contributes(k, "completed") && Contributes(k, "incomplete")).ToList();
            var values = members.Select(k => St(k).Completion).ToList();
            if (members.Count == 0 || values.Any(v => v == "incomplete")) st.Completion = members.Count == 0 && st.Attempts == 0 ? st.Completion : "incomplete";
            else if (values.All(v => v == "completed")) st.Completion = "completed";
        }

        WriteGlobals(c);
    }

    private bool RollupFires(SeqActivity cluster, SeqRollupRule rule)
    {
        var results = new List<bool?>();
        foreach (var child in ChildrenOf(cluster).Where(k => k.Tracked && Contributes(k, rule.Action)))
        {
            var values = rule.Conditions.Select(c => Condition(c, child)).ToList();
            bool? result = values.Count == 0 ? null
                : rule.Combination == "all" ? (values.All(v => v == true) ? true : values.Any(v => v == false) ? false : null)
                : (values.Any(v => v == true) ? true : values.All(v => v == false) ? false : null);
            results.Add(result);
        }
        if (results.Count == 0) return false;
        var trueCount = results.Count(r => r == true);
        return rule.ChildActivitySet switch
        {
            "any" => trueCount > 0,
            "none" => results.All(r => r == false),
            "atLeastCount" => trueCount >= rule.MinimumCount,
            "atLeastPercent" => (decimal)trueCount / results.Count >= rule.MinimumPercent,
            _ => trueCount == results.Count
        };
    }

    /// <summary>Rollup considerations: whether a child counts for an action.</summary>
    private bool Contributes(SeqActivity child, string action)
    {
        var (enabled, required) = action switch
        {
            "satisfied" => (child.RollupObjectiveSatisfied, child.RequiredForSatisfied),
            "notSatisfied" => (child.RollupObjectiveSatisfied, child.RequiredForNotSatisfied),
            "completed" => (child.RollupProgressCompletion, child.RequiredForCompleted),
            _ => (child.RollupProgressCompletion, child.RequiredForIncomplete)
        };
        if (!enabled) return false;
        var st = St(child);
        return required switch
        {
            "ifAttempted" => st.Attempts > 0,
            "ifNotSkipped" => !st.Skipped, // skipped on the way through during the parent's current attempt
            "ifNotSuspended" => !(st.Active && State.Suspended && State.Current == child.Id),
            _ => true
        };
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Rules and objectives
    // ═════════════════════════════════════════════════════════════════════════

    private string? RuleAction(SeqActivity a, List<SeqRule> rules, params string[] actions)
    {
        foreach (var rule in rules)
        {
            if (!actions.Contains(rule.Action) || rule.Conditions.Count == 0) continue;
            var values = rule.Conditions.Select(c => Condition(c, a)).ToList();
            var fires = rule.Combination == "any" ? values.Any(v => v == true) : values.All(v => v == true);
            if (fires) return rule.Action;
        }
        return null;
    }

    /// <summary>A rule or rollup condition: true, false or unknown (null). "not" keeps unknown unknown.</summary>
    private bool? Condition(SeqCondition c, SeqActivity a)
    {
        var st = St(a);
        bool? value = c.Condition switch
        {
            "satisfied" => Satisfied(a, c.Objective),
            "objectiveStatusKnown" => Satisfied(a, c.Objective) is not null,
            "objectiveMeasureKnown" => Measure(a, c.Objective) is not null,
            "objectiveMeasureGreaterThan" => Measure(a, c.Objective) is { } m ? m > c.Threshold : null,
            "objectiveMeasureLessThan" => Measure(a, c.Objective) is { } m2 ? m2 < c.Threshold : null,
            "completed" => st.Completion is { } completion ? completion == "completed" : null,
            "activityProgressKnown" => st.Completion is not null,
            "attempted" => st.Attempts > 0,
            "attemptLimitExceeded" => a.AttemptLimit > 0 && st.Attempts >= a.AttemptLimit,
            "always" => true,
            _ => false // timeLimitExceeded, outsideAvailableTimeRange: no time limits are applied
        };
        return c.Not && value is { } b ? !b : value;
    }

    private SeqObjective? ObjectiveOf(SeqActivity a, string? id)
        => string.IsNullOrEmpty(id) ? a.PrimaryObjective : a.Objectives.FirstOrDefault(o => o.Id == id);

    /// <summary>An objective's satisfied status: a shared objective it reads wins when known; by measure when the objective says so.</summary>
    public bool? Satisfied(SeqActivity a, string? objectiveId = null)
    {
        var objective = ObjectiveOf(a, objectiveId);
        if (objective is null) return null;
        if (objective.SatisfiedByMeasure && Measure(a, objectiveId) is { } m && (!St(a).Active || a.MeasureSatisfactionIfActive))
            return m >= objective.MinNormalizedMeasure;
        foreach (var map in objective.Maps.Where(m => m.ReadSatisfied))
            if (State.Globals.TryGetValue(map.Target, out var g) && g.Satisfied is { } global) return global;
        return objective.Primary ? St(a).Satisfied : St(a).Objectives.GetValueOrDefault(objective.Id)?.Satisfied;
    }

    public decimal? Measure(SeqActivity a, string? objectiveId = null)
    {
        var objective = ObjectiveOf(a, objectiveId);
        if (objective is null) return null;
        foreach (var map in objective.Maps.Where(m => m.ReadMeasure))
            if (State.Globals.TryGetValue(map.Target, out var g) && g.Measure is { } global) return global;
        return objective.Primary ? St(a).Measure : St(a).Objectives.GetValueOrDefault(objective.Id)?.Measure;
    }

    private void WriteGlobals(SeqActivity a)
    {
        var st = St(a);
        foreach (var objective in a.Objectives)
        {
            var satisfied = objective.Primary ? st.Satisfied : st.Objectives.GetValueOrDefault(objective.Id)?.Satisfied;
            var measure = objective.Primary ? st.Measure : st.Objectives.GetValueOrDefault(objective.Id)?.Measure;
            if (objective.Primary && objective.SatisfiedByMeasure && measure is { } m) satisfied = m >= objective.MinNormalizedMeasure;
            foreach (var map in objective.Maps)
            {
                if (!map.WriteSatisfied && !map.WriteMeasure) continue;
                var g = State.Globals.TryGetValue(map.Target, out var existing) ? existing : State.Globals[map.Target] = new SeqObjectiveState();
                if (map.WriteSatisfied && satisfied is not null) g.Satisfied = satisfied;
                if (map.WriteMeasure && measure is not null) g.Measure = measure;
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Tree
    // ═════════════════════════════════════════════════════════════════════════

    private SeqActivityState St(SeqActivity a)
        => State.Activities.TryGetValue(a.Id, out var st) ? st : State.Activities[a.Id] = new SeqActivityState();

    private SeqActivity? Parent(SeqActivity a) => _parent.GetValueOrDefault(a.Id);

    /// <summary>A cluster's children for its current attempt (randomized order or selection when the package asks for it).</summary>
    private List<SeqActivity> ChildrenOf(SeqActivity a)
        => State.ChildOrder.TryGetValue(a.Id, out var order)
            ? order.Select(id => _byId.GetValueOrDefault(id)).Where(x => x is not null && a.Children.Contains(x)).Select(x => x!).ToList()
            : a.Children;

    /// <summary>Randomizes a cluster's children when it is about to start a new attempt (or once), as its randomization controls say.</summary>
    private void PrepareChildren(SeqActivity cluster)
    {
        if (cluster.IsLeaf || (!cluster.ReorderChildren && cluster.SelectCount is null)) return;
        var randomize = cluster.RandomizationTiming switch
        {
            "onEachNewAttempt" => !St(cluster).Active,
            "once" => !State.ChildOrder.ContainsKey(cluster.Id),
            _ => false
        };
        if (!randomize) return;
        var ids = cluster.Children.Select(c => c.Id).ToList();
        if (cluster.SelectCount is { } count && count < ids.Count) ids = ids.OrderBy(_ => _random.Next()).Take(count).OrderBy(id => cluster.Children.FindIndex(c => c.Id == id)).ToList();
        if (cluster.ReorderChildren) ids = ids.OrderBy(_ => _random.Next()).ToList();
        State.ChildOrder[cluster.Id] = ids;
    }

    private List<SeqActivity> PathFromRoot(SeqActivity a)
    {
        var path = new List<SeqActivity>();
        for (var x = a; x is not null; x = Parent(x)) path.Insert(0, x);
        return path;
    }

    private bool IsAncestorOrSelf(SeqActivity ancestor, SeqActivity a)
    {
        for (var x = a; x is not null; x = Parent(x)) if (x == ancestor) return true;
        return false;
    }

    private static IEnumerable<SeqActivity> Subtree(SeqActivity a)
    {
        yield return a;
        foreach (var child in a.Children)
            foreach (var x in Subtree(child)) yield return x;
    }

    private SeqActivity? NextInTree(SeqActivity from, bool enterChildren)
    {
        if (enterChildren && !from.IsLeaf && ChildrenOf(from) is { Count: > 0 } children) return children[0];
        for (var a = from; Parent(a) is { } parent; a = parent)
        {
            var siblings = ChildrenOf(parent);
            var i = siblings.IndexOf(a);
            if (i >= 0 && i < siblings.Count - 1) return siblings[i + 1];
        }
        return null;
    }

    private SeqActivity? PreviousInTree(SeqActivity from)
    {
        for (var a = from; Parent(a) is { } parent; a = parent)
        {
            var siblings = ChildrenOf(parent);
            var i = siblings.IndexOf(a);
            if (i > 0) return siblings[i - 1];
        }
        return null;
    }
}
