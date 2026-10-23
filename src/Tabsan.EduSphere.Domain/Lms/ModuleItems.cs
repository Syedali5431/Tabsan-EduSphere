using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Lms;

// Plan M7 — ordered module items, completion requirements, progress and release conditions.

/// <summary>What a module item shows or links to.</summary>
public enum ModuleItemType
{
    /// <summary>A text heading that groups the items below it.</summary>
    Header = 0,
    /// <summary>A text page written in the module.</summary>
    Page = 1,
    Video = 2,
    /// <summary>A course material (file or link).</summary>
    Material = 3,
    Assignment = 4,
    Quiz = 5,
    Discussion = 6,
    /// <summary>An LTI course item (Plan M5).</summary>
    ExternalTool = 7,
    ExternalUrl = 8,
    /// <summary>A SCORM package (Plan M13).</summary>
    ScormPackage = 9
}

/// <summary>What a student must do to complete an item (one requirement per item).</summary>
public enum ModuleRequirementType
{
    None = 0,
    MustView = 1,
    MustMarkDone = 2,
    MustSubmit = 3,
    /// <summary>A posted grade (or best quiz score) of at least <see cref="ModuleItem.MinScorePercent"/>.</summary>
    MustScoreAtLeast = 4,
    /// <summary>Post in the discussion.</summary>
    MustContribute = 5
}

/// <summary>When a module counts as complete.</summary>
public enum ModuleCompletionMode
{
    /// <summary>Every item with a requirement is complete.</summary>
    All = 0,
    /// <summary>At least one item with a requirement is complete.</summary>
    Any = 1
}

/// <summary>An entry in a module: a heading, page, link, or a pointer to course content (assignment, quiz, tool…).</summary>
public class ModuleItem : BaseEntity
{
    public Guid ModuleId { get; private set; }
    public Guid OfferingId { get; private set; }
    public ModuleItemType ItemType { get; private set; }
    /// <summary>The linked record (video, material, assignment, quiz, discussion thread, LTI item or SCORM package).</summary>
    public Guid? TargetId { get; private set; }
    public string Title { get; private set; } = default!;
    /// <summary>ExternalUrl only.</summary>
    public string? Url { get; private set; }
    /// <summary>Page only (plain text; shown with line breaks).</summary>
    public string? Body { get; private set; }
    public int Position { get; private set; }
    /// <summary>0–3, for visual nesting.</summary>
    public int IndentLevel { get; private set; }
    public bool IsPublished { get; private set; } = true;
    public ModuleRequirementType RequirementType { get; private set; }
    public decimal? MinScorePercent { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private ModuleItem() { }

    public ModuleItem(Guid moduleId, Guid offeringId, ModuleItemType type, Guid? targetId, string title, string? url, string? body, int position, Guid createdByUserId)
    {
        if (NeedsTarget(type) && targetId is null) throw new ArgumentException($"A {type} item needs the item it points to.", nameof(targetId));
        ModuleId = moduleId;
        OfferingId = offeringId;
        ItemType = type;
        TargetId = NeedsTarget(type) ? targetId : null;
        Position = position;
        CreatedByUserId = createdByUserId;
        Update(title, url, body, 0);
    }

    public static bool NeedsTarget(ModuleItemType type) => type is not (ModuleItemType.Header or ModuleItemType.Page or ModuleItemType.ExternalUrl);

    /// <summary>The requirements that make sense for an item type.</summary>
    public static IReadOnlyList<ModuleRequirementType> AllowedRequirements(ModuleItemType type) => type switch
    {
        ModuleItemType.Header => [ModuleRequirementType.None],
        ModuleItemType.Assignment or ModuleItemType.Quiz or ModuleItemType.ExternalTool or ModuleItemType.ScormPackage =>
            [ModuleRequirementType.None, ModuleRequirementType.MustView, ModuleRequirementType.MustMarkDone, ModuleRequirementType.MustSubmit, ModuleRequirementType.MustScoreAtLeast],
        ModuleItemType.Discussion => [ModuleRequirementType.None, ModuleRequirementType.MustView, ModuleRequirementType.MustMarkDone, ModuleRequirementType.MustContribute],
        _ => [ModuleRequirementType.None, ModuleRequirementType.MustView, ModuleRequirementType.MustMarkDone]
    };

    public void Update(string title, string? url, string? body, int indentLevel)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Enter a title.", nameof(title));
        if (title.Trim().Length > 300) throw new ArgumentException("The title is limited to 300 characters.", nameof(title));
        if (ItemType == ModuleItemType.ExternalUrl)
        {
            if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var u) || (u.Scheme != Uri.UriSchemeHttps && u.Scheme != Uri.UriSchemeHttp))
                throw new ArgumentException("Enter a web address starting with https:// or http://.", nameof(url));
            Url = u.ToString();
        }
        else Url = null;
        if (body is { Length: > 50_000 }) throw new ArgumentException("A page is limited to 50,000 characters.", nameof(body));
        Body = ItemType == ModuleItemType.Page ? (string.IsNullOrWhiteSpace(body) ? null : body.Trim()) : null;
        Title = title.Trim();
        IndentLevel = Math.Clamp(indentLevel, 0, 3);
        Touch();
    }

    /// <summary>Sets the completion requirement. Returns true when it changed (earlier completions must then be re-evaluated).</summary>
    public bool SetRequirement(ModuleRequirementType type, decimal? minScorePercent)
    {
        if (!AllowedRequirements(ItemType).Contains(type)) throw new ArgumentException($"A {ItemType} item cannot use \"{type}\".", nameof(type));
        decimal? min = null;
        if (type == ModuleRequirementType.MustScoreAtLeast)
        {
            if (minScorePercent is not (> 0 and <= 100)) throw new ArgumentException("Enter the minimum score as a percentage between 1 and 100.", nameof(minScorePercent));
            min = Math.Round(minScorePercent.Value, 2);
        }
        var changed = RequirementType != type || MinScorePercent != min;
        RequirementType = type;
        MinScorePercent = min;
        if (changed) Touch();
        return changed;
    }

    public void SetPublished(bool published) { IsPublished = published; Touch(); }

    public void MoveTo(int position) { Position = position; Touch(); }
}

/// <summary>A student's progress on one module item.</summary>
public class ModuleItemProgress : BaseEntity
{
    public Guid StudentProfileId { get; private set; }
    public Guid ModuleItemId { get; private set; }
    public Guid OfferingId { get; private set; }
    public DateTime? ViewedAt { get; private set; }
    public DateTime? MarkedDoneAt { get; private set; }
    /// <summary>First time the requirement was met; once set the item stays complete (unless "mark as done" is undone or the requirement changes).</summary>
    public DateTime? CompletedAt { get; private set; }

    private ModuleItemProgress() { }

    public ModuleItemProgress(Guid studentProfileId, Guid moduleItemId, Guid offeringId)
    {
        StudentProfileId = studentProfileId;
        ModuleItemId = moduleItemId;
        OfferingId = offeringId;
    }

    public void MarkViewed(DateTime now) { ViewedAt ??= now; Touch(); }

    public void MarkDone(DateTime now) { MarkedDoneAt ??= now; Touch(); }

    public void UndoDone() { MarkedDoneAt = null; CompletedAt = null; Touch(); }

    public void Complete(DateTime now) { CompletedAt ??= now; Touch(); }

    public void ResetCompletion() { CompletedAt = null; Touch(); }
}

/// <summary>Module <see cref="ModuleId"/> stays locked until module <see cref="PrerequisiteModuleId"/> is complete.</summary>
public class ModulePrerequisite : BaseEntity
{
    public Guid ModuleId { get; private set; }
    public Guid PrerequisiteModuleId { get; private set; }

    private ModulePrerequisite() { }

    public ModulePrerequisite(Guid moduleId, Guid prerequisiteModuleId)
    {
        if (moduleId == prerequisiteModuleId) throw new ArgumentException("A module cannot require itself.", nameof(prerequisiteModuleId));
        ModuleId = moduleId;
        PrerequisiteModuleId = prerequisiteModuleId;
    }
}
