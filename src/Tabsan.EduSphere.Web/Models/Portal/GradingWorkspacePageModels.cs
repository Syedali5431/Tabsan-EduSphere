using Tabsan.EduSphere.Application.DTOs.Assignments;

namespace Tabsan.EduSphere.Web.Models.Portal;

// Plan M3 — student submission page and grading workspace.

public sealed class AssignmentSubmissionPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public StudentAssignmentView? View { get; set; }
    /// <summary>Plan M8: the student's own dates, late policy, group and peer reviews (null when unavailable).</summary>
    public Tabsan.EduSphere.Application.Assignments.StudentAssignmentExtrasDto? Extras { get; set; }
    /// <summary>Plan M9: the student's similarity score, when the assignment shows it.</summary>
    public Tabsan.EduSphere.Application.Similarity.StudentSimilarityDto? Similarity { get; set; }
}

/// <summary>Plan M5: a page that posts hidden fields to a tool and submits itself.</summary>
public sealed class LtiAutoPostModel
{
    public string Action { get; set; } = default!;
    public IReadOnlyDictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();
    public string Title { get; set; } = "Opening the tool…";
}

/// <summary>Plan M5: an iframe-presented tool inside the portal.</summary>
public sealed class LtiFramePageModel
{
    public string FrameUrl { get; set; } = default!;
    public string? ReturnUrl { get; set; }
}

/// <summary>Plan M5: the "External tools" card on course pages.</summary>
public sealed class ExternalToolsCardModel
{
    public Guid OfferingId { get; set; }
    public bool Manage { get; set; }
    public string ReturnUrl { get; set; } = "/";
    public IReadOnlyList<Tabsan.EduSphere.Application.Lti.LtiLinkResponse> Links { get; set; } = [];
    public IReadOnlyList<Tabsan.EduSphere.Application.Lti.LtiCourseToolResponse> Tools { get; set; } = [];
}

/// <summary>Plan M5: the admin tool registry page.</summary>
public sealed class LtiToolsPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public Tabsan.EduSphere.Application.Lti.LtiPlatformInfo? Platform { get; set; }
    public IReadOnlyList<Tabsan.EduSphere.Application.Lti.LtiToolResponse> Tools { get; set; } = [];
    public Tabsan.EduSphere.Application.Lti.LtiToolResponse? Editing { get; set; }
    public bool ModuleOff { get; set; }
    public IReadOnlyList<LookupItem> Departments { get; set; } = [];
    public IReadOnlyList<CourseOfferingItem> Offerings { get; set; } = [];
}

/// <summary>Plan M5: the register/edit tool form.</summary>
public sealed class LtiToolFormModel
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? OidcInitiationUrl { get; set; }
    public string? TargetLinkUri { get; set; }
    public string? DeepLinkingUrl { get; set; }
    /// <summary>One per line.</summary>
    public string? RedirectUris { get; set; }
    public string? JwksUrl { get; set; }
    public string? PublicKeyPem { get; set; }
    public string? CustomParameters { get; set; }
    public string? PrivacyLevel { get; set; }
    public bool AllowGradeService { get; set; }
    public bool AllowRosterService { get; set; }
}

/// <summary>Drives the shared assignment settings form.</summary>
public sealed class AssignmentSettingsFormModel
{
    public AssignmentSettingsResponse Settings { get; set; } = default!;
    public string? ReturnUrl { get; set; }
    public string FormId { get; set; } = "assignment-settings";
}

public sealed class GradingWorkspacePageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public string Filter { get; set; } = "all";
    public GradingWorkspaceResponse? Workspace { get; set; }
    public Guid? SelectedSubmissionId { get; set; }
    /// <summary>Plan M9: latest similarity check per submission (empty when the module is off).</summary>
    public Dictionary<Guid, Tabsan.EduSphere.Application.Similarity.SimilarityScoreDto> Similarity { get; set; } = [];

    public static readonly (string Value, string Label)[] Filters =
    [
        ("all", "All"),
        ("ungraded", "Needs grading"),
        ("late", "Late"),
        ("unposted", "Graded, not posted")
    ];
}
