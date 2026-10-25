namespace Tabsan.EduSphere.Domain.Lms;

// SCORM 2004 Simple Sequencing (IMS SS / ADL SN) as read from a package's manifest, and the learner's sequencing state.
// Defaults follow the specification, so a property left out of the manifest behaves as the standard says.

/// <summary>The activity tree of a SCORM 2004 package that has sequencing information.</summary>
public sealed class SeqTree
{
    public SeqActivity Root { get; set; } = new();
    /// <summary>adlseq:objectivesGlobalToSystem — global objectives outlive the attempt (carried to the learner's next attempt).</summary>
    public bool ObjectivesGlobalToSystem { get; set; } = true;
}

/// <summary>One activity: the organization (root), a cluster (item with items) or a leaf (item that launches a part).</summary>
public sealed class SeqActivity
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    /// <summary>isvisible: shown in the contents list.</summary>
    public bool Visible { get; set; } = true;
    /// <summary>The part a leaf delivers (null for clusters and items without a resource).</summary>
    public string? PartId { get; set; }
    public List<SeqActivity> Children { get; set; } = [];

    // Control modes (imsss:controlMode) — how the learner may move among this activity's children.
    public bool Choice { get; set; } = true;
    public bool ChoiceExit { get; set; } = true;
    public bool Flow { get; set; }
    public bool ForwardOnly { get; set; }

    public List<SeqRule> PreRules { get; set; } = [];
    public List<SeqRule> PostRules { get; set; } = [];
    public List<SeqRule> ExitRules { get; set; } = [];

    /// <summary>imsss:limitConditions attemptLimit (0 = no limit).</summary>
    public int AttemptLimit { get; set; }

    public List<SeqRollupRule> RollupRules { get; set; } = [];
    public bool RollupObjectiveSatisfied { get; set; } = true;
    public bool RollupProgressCompletion { get; set; } = true;
    public decimal ObjectiveMeasureWeight { get; set; } = 1m;
    /// <summary>adlseq:rollupConsiderations: always, ifAttempted, ifNotSkipped or ifNotSuspended.</summary>
    public string RequiredForSatisfied { get; set; } = "always";
    public string RequiredForNotSatisfied { get; set; } = "always";
    public string RequiredForCompleted { get; set; } = "always";
    public string RequiredForIncomplete { get; set; } = "always";
    public bool MeasureSatisfactionIfActive { get; set; } = true;

    /// <summary>The primary objective (always present) followed by the other objectives.</summary>
    public List<SeqObjective> Objectives { get; set; } = [new SeqObjective { Primary = true }];

    // Delivery controls.
    public bool Tracked { get; set; } = true;
    public bool CompletionSetByContent { get; set; }
    public bool ObjectiveSetByContent { get; set; }

    // Randomization controls.
    public int? SelectCount { get; set; }
    public bool ReorderChildren { get; set; }
    /// <summary>never, once or onEachNewAttempt.</summary>
    public string RandomizationTiming { get; set; } = "never";

    /// <summary>adlnav:hideLMSUI — portal navigation to hide while this activity is open (previous, continue, exit, exitAll, suspendAll, abandon…).</summary>
    public List<string> HideLmsUi { get; set; } = [];

    public bool IsLeaf => Children.Count == 0;
    public SeqObjective PrimaryObjective => Objectives.FirstOrDefault(o => o.Primary) ?? Objectives[0];
}

/// <summary>A pre-condition, post-condition or exit rule: when its conditions hold, the action applies.</summary>
public sealed class SeqRule
{
    /// <summary>all or any.</summary>
    public string Combination { get; set; } = "all";
    public List<SeqCondition> Conditions { get; set; } = [];
    /// <summary>Pre: skip, disabled, hiddenFromChoice, stopForwardTraversal. Post: exitParent, exitAll, retry, retryAll, continue, previous. Exit: exit.</summary>
    public string Action { get; set; } = "";
}

public sealed class SeqCondition
{
    /// <summary>satisfied, objectiveStatusKnown, objectiveMeasureKnown, objectiveMeasureGreaterThan, objectiveMeasureLessThan, completed,
    /// activityProgressKnown, attempted, attemptLimitExceeded, timeLimitExceeded, outsideAvailableTimeRange, always.</summary>
    public string Condition { get; set; } = "always";
    /// <summary>operator="not".</summary>
    public bool Not { get; set; }
    /// <summary>The objective the condition reads (the primary objective when empty).</summary>
    public string? Objective { get; set; }
    public decimal Threshold { get; set; }
}

public sealed class SeqRollupRule
{
    /// <summary>all, any, none, atLeastCount or atLeastPercent.</summary>
    public string ChildActivitySet { get; set; } = "all";
    public int MinimumCount { get; set; }
    public decimal MinimumPercent { get; set; }
    /// <summary>Rollup conditions combine with "any" unless the manifest says "all".</summary>
    public string Combination { get; set; } = "any";
    public List<SeqCondition> Conditions { get; set; } = [];
    /// <summary>satisfied, notSatisfied, completed or incomplete.</summary>
    public string Action { get; set; } = "";
}

public sealed class SeqObjective
{
    public string Id { get; set; } = "";
    public bool Primary { get; set; }
    public bool SatisfiedByMeasure { get; set; }
    public decimal MinNormalizedMeasure { get; set; } = 1m;
    public List<SeqMapInfo> Maps { get; set; } = [];
}

/// <summary>imsss:mapInfo — links a local objective to a shared (global) objective.</summary>
public sealed class SeqMapInfo
{
    public string Target { get; set; } = "";
    public bool ReadSatisfied { get; set; } = true;
    public bool ReadMeasure { get; set; } = true;
    public bool WriteSatisfied { get; set; }
    public bool WriteMeasure { get; set; }
}

/// <summary>A learner's sequencing state for one attempt at a sequenced package.</summary>
public sealed class SeqState
{
    public Dictionary<string, SeqActivityState> Activities { get; set; } = new(StringComparer.Ordinal);
    /// <summary>Shared (global) objectives by target objective id.</summary>
    public Dictionary<string, SeqObjectiveState> Globals { get; set; } = new(StringComparer.Ordinal);
    /// <summary>The activity being delivered (a leaf), or null before the start and after the end.</summary>
    public string? Current { get; set; }
    /// <summary>The learner left with suspendAll; Current is resumed next time.</summary>
    public bool Suspended { get; set; }
    /// <summary>The package was exited (exitAll or the end of the content); the next launch starts again from the beginning.</summary>
    public bool Ended { get; set; }
    /// <summary>Clusters whose children were reordered or selected at random: their children for the current attempt.</summary>
    public Dictionary<string, List<string>> ChildOrder { get; set; } = new(StringComparer.Ordinal);
}

public sealed class SeqActivityState
{
    public int Attempts { get; set; }
    public bool Active { get; set; }
    /// <summary>completed, incomplete or null (unknown).</summary>
    public string? Completion { get; set; }
    /// <summary>Primary objective: satisfied (true), not satisfied (false) or unknown (null).</summary>
    public bool? Satisfied { get; set; }
    /// <summary>Primary objective normalized measure (-1..1).</summary>
    public decimal? Measure { get; set; }
    /// <summary>Other objectives of the activity by objective id.</summary>
    public Dictionary<string, SeqObjectiveState> Objectives { get; set; } = new(StringComparer.Ordinal);
    /// <summary>Skipped by a skip rule during the current attempt of its parent.</summary>
    public bool Skipped { get; set; }
}

public sealed class SeqObjectiveState
{
    public bool? Satisfied { get; set; }
    public decimal? Measure { get; set; }
}
