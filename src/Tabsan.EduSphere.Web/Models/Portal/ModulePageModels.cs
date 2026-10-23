using Tabsan.EduSphere.Application.Lms;

namespace Tabsan.EduSphere.Web.Models.Portal;

// Plan M7 — module items, progress and release conditions.

/// <summary>The item editor shown inside each module card on LMS Manage.</summary>
public sealed class ModuleItemsEditorModel
{
    public Guid OfferingId { get; init; }
    public required ModuleStructureModule Module { get; init; }
    public IReadOnlyList<ModuleStructureModule> AllModules { get; init; } = [];
    public IReadOnlyList<LinkableResponse> Linkables { get; init; } = [];
}

/// <summary>A module item opened by a student (or previewed by staff).</summary>
public sealed class ModuleItemPageModel
{
    public ModuleItemOpenResponse? Item { get; set; }
    public ModuleItemNavResponse? Nav { get; set; }
    public string? Message { get; set; }
    public bool IsStaff { get; set; }
}

public sealed class ModuleProgressPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public Guid OfferingId { get; set; }
    public IReadOnlyList<LookupItem> Offerings { get; set; } = [];
    public ModuleProgressReport? Report { get; set; }
    public Guid? StudentId { get; set; }
    public StudentModulesResponse? StudentDetail { get; set; }
}

/// <summary>The previous/next bar.</summary>
public sealed class ModuleNavModel
{
    public required ModuleItemNavResponse Nav { get; init; }
    public string ReturnUrl { get; init; } = "/";
    public bool IsStudent { get; init; }
}

public static class ModuleItemText
{
    public static string Requirement(string type, decimal? min) => type switch
    {
        "MustView" => "View",
        "MustMarkDone" => "Mark as done",
        "MustSubmit" => "Submit",
        "MustScoreAtLeast" => $"Score at least {min:0.##}%",
        "MustContribute" => "Post in the discussion",
        _ => ""
    };

    public static string TypeLabel(string type) => type switch
    {
        "ExternalUrl" => "Link",
        "ExternalTool" => "External tool",
        "ScormPackage" => "SCORM package",
        _ => type
    };
}
