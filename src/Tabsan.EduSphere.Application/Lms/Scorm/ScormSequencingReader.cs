using System.Globalization;
using System.Xml.Linq;
using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Application.Lms.Scorm;

/// <summary>
/// Reads the SCORM 2004 sequencing of the default organization into an activity tree: control modes, pre/post/exit rules,
/// attempt limits, rollup rules and considerations, objectives with their global maps, delivery and randomization controls,
/// and hidden portal navigation. Shared definitions (imsss:sequencingCollection, IDRef) apply first and the item's own
/// sequencing overrides them. Returns null when the package has no sequencing information.
/// </summary>
public static class ScormSequencingReader
{
    public static SeqTree? Read(XElement organization, XElement manifestRoot, IReadOnlyDictionary<XElement, string> partIds)
    {
        if (!organization.Descendants().Any(e => e.Name.LocalName == "sequencing")) return null;
        var collection = manifestRoot.Elements().FirstOrDefault(e => e.Name.LocalName == "sequencingCollection")?
            .Elements().Where(e => e.Name.LocalName == "sequencing" && Attr(e, "ID") is not null)
            .GroupBy(e => Attr(e, "ID")!).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal) ?? [];

        var root = Activity(organization, collection, partIds, isOrganization: true);
        var global = organization.Attributes().FirstOrDefault(a => a.Name.LocalName == "objectivesGlobalToSystem")?.Value;
        return new SeqTree { Root = root, ObjectivesGlobalToSystem = !string.Equals(global, "false", StringComparison.OrdinalIgnoreCase) };
    }

    private static SeqActivity Activity(XElement element, Dictionary<string, XElement> collection, IReadOnlyDictionary<XElement, string> partIds, bool isOrganization)
    {
        var activity = new SeqActivity
        {
            Id = Attr(element, "identifier") ?? (isOrganization ? "organization" : Guid.NewGuid().ToString("N")),
            Title = Child(element, "title")?.Value.Trim() ?? "",
            Visible = isOrganization || !string.Equals(Attr(element, "isvisible"), "false", StringComparison.OrdinalIgnoreCase),
            PartId = partIds.TryGetValue(element, out var part) ? part : null
        };
        if (activity.Title.Length > 200) activity.Title = activity.Title[..200];

        var sequencing = Child(element, "sequencing");
        if (sequencing is not null)
        {
            if (Attr(sequencing, "IDRef") is { } idRef && collection.TryGetValue(idRef, out var shared)) Apply(activity, shared);
            Apply(activity, sequencing);
        }
        var hide = element.Descendants().Where(e => e.Name.LocalName == "hideLMSUI" && e.Ancestors().FirstOrDefault(a => a.Name.LocalName == "item") == (isOrganization ? null : element))
            .Select(e => e.Value.Trim()).Where(v => v.Length > 0).Distinct().ToList();
        activity.HideLmsUi = hide;

        foreach (var child in element.Elements().Where(e => e.Name.LocalName == "item"))
            activity.Children.Add(Activity(child, collection, partIds, isOrganization: false));
        return activity;
    }

    private static void Apply(SeqActivity a, XElement s)
    {
        if (Child(s, "controlMode") is { } control)
        {
            a.Choice = Bool(control, "choice", a.Choice);
            a.ChoiceExit = Bool(control, "choiceExit", a.ChoiceExit);
            a.Flow = Bool(control, "flow", a.Flow);
            a.ForwardOnly = Bool(control, "forwardOnly", a.ForwardOnly);
        }

        if (Child(s, "sequencingRules") is { } rules)
        {
            var pre = Rules(rules, "preConditionRule");
            var exit = Rules(rules, "exitConditionRule");
            var post = Rules(rules, "postConditionRule");
            if (pre.Count > 0) a.PreRules = pre;
            if (exit.Count > 0) a.ExitRules = exit;
            if (post.Count > 0) a.PostRules = post;
        }

        if (Child(s, "limitConditions") is { } limits && int.TryParse(Attr(limits, "attemptLimit"), out var limit) && limit >= 0)
            a.AttemptLimit = limit;

        if (Child(s, "rollupRules") is { } rollup)
        {
            a.RollupObjectiveSatisfied = Bool(rollup, "rollupObjectiveSatisfied", a.RollupObjectiveSatisfied);
            a.RollupProgressCompletion = Bool(rollup, "rollupProgressCompletion", a.RollupProgressCompletion);
            if (Dec(rollup, "objectiveMeasureWeight") is { } weight) a.ObjectiveMeasureWeight = Math.Clamp(weight, 0m, 1m);
            var list = rollup.Elements().Where(e => e.Name.LocalName == "rollupRule").Select(RollupRule).Where(r => r.Action.Length > 0).ToList();
            if (list.Count > 0) a.RollupRules = list;
        }

        if (Child(s, "rollupConsiderations") is { } considerations)
        {
            a.RequiredForSatisfied = Attr(considerations, "requiredForSatisfied") ?? a.RequiredForSatisfied;
            a.RequiredForNotSatisfied = Attr(considerations, "requiredForNotSatisfied") ?? a.RequiredForNotSatisfied;
            a.RequiredForCompleted = Attr(considerations, "requiredForCompleted") ?? a.RequiredForCompleted;
            a.RequiredForIncomplete = Attr(considerations, "requiredForIncomplete") ?? a.RequiredForIncomplete;
            a.MeasureSatisfactionIfActive = Bool(considerations, "measureSatisfactionIfActive", a.MeasureSatisfactionIfActive);
        }

        // imsss:objectives (adlseq:objectives of the 4th edition only add completion maps, which are not used here).
        if (s.Elements().FirstOrDefault(e => e.Name.LocalName == "objectives" && e.Name.NamespaceName.Contains("imsss", StringComparison.OrdinalIgnoreCase)) is { } objectives)
        {
            var list = new List<SeqObjective>();
            if (Child(objectives, "primaryObjective") is { } primary) list.Add(Objective(primary, isPrimary: true));
            else list.Add(new SeqObjective { Primary = true });
            list.AddRange(objectives.Elements().Where(e => e.Name.LocalName == "objective").Select(o => Objective(o, isPrimary: false)));
            a.Objectives = list;
        }

        if (Child(s, "randomizationControls") is { } random)
        {
            a.RandomizationTiming = Attr(random, "randomizationTiming") ?? a.RandomizationTiming;
            if (int.TryParse(Attr(random, "selectCount"), out var select) && select > 0) a.SelectCount = select;
            a.ReorderChildren = Bool(random, "reorderChildren", a.ReorderChildren);
        }

        if (Child(s, "deliveryControls") is { } delivery)
        {
            a.Tracked = Bool(delivery, "tracked", a.Tracked);
            a.CompletionSetByContent = Bool(delivery, "completionSetByContent", a.CompletionSetByContent);
            a.ObjectiveSetByContent = Bool(delivery, "objectiveSetByContent", a.ObjectiveSetByContent);
        }
    }

    private static List<SeqRule> Rules(XElement rules, string kind)
        => rules.Elements().Where(e => e.Name.LocalName == kind).Select(r =>
        {
            var conditions = Child(r, "ruleConditions");
            return new SeqRule
            {
                Combination = conditions is null ? "all" : Attr(conditions, "conditionCombination") ?? "all",
                Conditions = conditions?.Elements().Where(e => e.Name.LocalName == "ruleCondition").Select(Condition).ToList() ?? [],
                Action = Attr(Child(r, "ruleAction"), "action") ?? ""
            };
        }).Where(r => r.Action.Length > 0).ToList();

    private static SeqRollupRule RollupRule(XElement r)
    {
        var conditions = Child(r, "rollupConditions");
        return new SeqRollupRule
        {
            ChildActivitySet = Attr(r, "childActivitySet") ?? "all",
            MinimumCount = int.TryParse(Attr(r, "minimumCount"), out var count) ? Math.Max(0, count) : 0,
            MinimumPercent = Dec(r, "minimumPercent") is { } percent ? Math.Clamp(percent, 0m, 1m) : 0m,
            Combination = conditions is null ? "any" : Attr(conditions, "conditionCombination") ?? "any",
            Conditions = conditions?.Elements().Where(e => e.Name.LocalName == "rollupCondition").Select(Condition).ToList() ?? [],
            Action = Attr(Child(r, "rollupAction"), "action") ?? ""
        };
    }

    private static SeqCondition Condition(XElement c) => new()
    {
        Condition = Attr(c, "condition") ?? "always",
        Not = string.Equals(Attr(c, "operator"), "not", StringComparison.OrdinalIgnoreCase),
        Objective = Attr(c, "referencedObjective") is { Length: > 0 } o ? o : null,
        Threshold = Dec(c, "measureThreshold") ?? 0m
    };

    private static SeqObjective Objective(XElement o, bool isPrimary) => new()
    {
        Id = Attr(o, "objectiveID") ?? "",
        Primary = isPrimary,
        SatisfiedByMeasure = Bool(o, "satisfiedByMeasure", false),
        MinNormalizedMeasure = decimal.TryParse(Child(o, "minNormalizedMeasure")?.Value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var m)
            ? Math.Clamp(m, -1m, 1m) : 1m,
        Maps = o.Elements().Where(e => e.Name.LocalName == "mapInfo" && Attr(e, "targetObjectiveID") is { Length: > 0 }).Select(map => new SeqMapInfo
        {
            Target = Attr(map, "targetObjectiveID")!,
            ReadSatisfied = Bool(map, "readSatisfiedStatus", true),
            ReadMeasure = Bool(map, "readNormalizedMeasure", true),
            WriteSatisfied = Bool(map, "writeSatisfiedStatus", false),
            WriteMeasure = Bool(map, "writeNormalizedMeasure", false)
        }).ToList()
    };

    private static XElement? Child(XElement? e, string localName) => e?.Elements().FirstOrDefault(x => x.Name.LocalName == localName);

    private static string? Attr(XElement? e, string localName) => e?.Attributes().FirstOrDefault(a => a.Name.LocalName == localName)?.Value.Trim();

    private static bool Bool(XElement e, string name, bool fallback)
        => Attr(e, name) is { } v ? v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1" : fallback;

    private static decimal? Dec(XElement e, string name)
        => decimal.TryParse(Attr(e, name), NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;
}
