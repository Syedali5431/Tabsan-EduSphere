using Tabsan.EduSphere.Application.Lms.Scorm;

namespace Tabsan.EduSphere.Web.Models.Portal;

// Plan M13 — SCORM packages and the xAPI statement store in the portal.

/// <summary>The player shell (no layout): runtime settings and the package's launch URL.</summary>
public sealed class ScormShellModel
{
    public string Title { get; set; } = "";
    public string Version { get; set; } = "";
    public string Mode { get; set; } = "normal";
    public string LaunchUrl { get; set; } = "";
    public string CmiJson { get; set; } = "{}";
    /// <summary>Where the runtime commits ("commit", "commit/{part}" for a part of a multi-part package, "commit/{part}/{attempt}" when sequenced).</summary>
    public string CommitUrl { get; set; } = "commit";
    public string? PartId { get; set; }
    /// <summary>SCORM 2004 sequencing decides the parts: the SCO's navigation requests go to the page; Next / Previous validity for the SCO.</summary>
    public bool Sequenced { get; set; }
    public bool CanContinue { get; set; } = true;
    public bool CanPrevious { get; set; } = true;
}

/// <summary>The player frame when a sequenced package has nothing to open (it has ended, or the learner has to choose a part).</summary>
public sealed class ScormEndedModel
{
    public string Title { get; set; } = "";
    public string? Message { get; set; }
    public string? MessageArg { get; set; }
    public bool Ended { get; set; }
}

/// <summary>"SCORM packages" card of a course: students' list, or the teacher's management.</summary>
public sealed class ScormCardModel
{
    public Guid OfferingId { get; set; }
    public bool Manage { get; set; }
    public List<ScormPackageResponse> Packages { get; set; } = [];
    public long MaxUploadMb { get; set; } = 500;
}

/// <summary>A student's page for one package (status, attempts, start/continue/review).</summary>
public sealed class ScormPageModel
{
    public ScormPackageResponse? Package { get; set; }
    public string? Message { get; set; }
    public Guid? ModuleItemId { get; set; }
}

/// <summary>The portal page around the player frame.</summary>
public sealed class ScormPlayerPageModel
{
    public Guid PackageId { get; set; }
    public Guid OfferingId { get; set; }
    public string Title { get; set; } = "";
    public string Mode { get; set; } = "normal";
    public int AttemptNumber { get; set; }
    public string PlayerUrl { get; set; } = "";
    public bool IsStaff { get; set; }
    public Guid? ModuleItemId { get; set; }
    /// <summary>The parts of a multi-part package (contents list); one entry for a one-part package.</summary>
    public List<ScormPartStatus> Parts { get; set; } = [];
    /// <summary>The package's SCORM 2004 sequencing decides what opens (Next, Previous and the contents ask it).</summary>
    public bool Sequenced { get; set; }
}

public sealed class ScormReportPageModel
{
    public ScormReportResponse? Report { get; set; }
    public string? Message { get; set; }
}

public sealed class XapiSettingsPageModel
{
    public bool IsConnected { get; set; }
    public List<XapiCredentialResponse> Credentials { get; set; } = [];
    public List<XapiStatementSummary> Recent { get; set; } = [];
    /// <summary>Shown once after creating credentials.</summary>
    public XapiCredentialCreated? Created { get; set; }
    public string Endpoint { get; set; } = "";
    public string? Message { get; set; }
}

/// <summary>Plan M14.3 — the API Tokens page.</summary>
public sealed class ApiTokensPageModel
{
    public string ApiBaseUrl { get; set; } = "";
    public List<Tabsan.EduSphere.Application.Auth.ApiTokenResponse> Mine { get; set; } = [];
    /// <summary>Every user's tokens (administrators).</summary>
    public List<Tabsan.EduSphere.Application.Auth.ApiTokenResponse> All { get; set; } = [];
    /// <summary>Shown once after creating a token.</summary>
    public Tabsan.EduSphere.Application.Auth.ApiTokenCreated? Created { get; set; }
    public string? Message { get; set; }
}
