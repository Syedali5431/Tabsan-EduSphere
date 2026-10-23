using System.Security.Cryptography;
using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Lti;

// Plan M5 — EduSphere as an LTI 1.3 / LTI Advantage platform.

/// <summary>What a tool learns about the person launching it.</summary>
public enum LtiPrivacyLevel
{
    /// <summary>Only the opaque EduSphere user ID (no name or email) and roles.</summary>
    Anonymous = 0,
    NameOnly = 1,
    EmailOnly = 2,
    /// <summary>Name and email.</summary>
    Public = 3
}

/// <summary>Where a tool deployment may be used.</summary>
public enum LtiDeploymentScope
{
    Installation = 0,
    Department = 1,
    CourseOffering = 2
}

/// <summary>An external tool registered with the platform (manual LTI 1.3 registration).</summary>
public class LtiTool : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    /// <summary>The client id the platform issued to the tool.</summary>
    public string ClientId { get; private set; } = default!;

    public string OidcInitiationUrl { get; private set; } = default!;
    public string TargetLinkUri { get; private set; } = default!;
    public string? DeepLinkingUrl { get; private set; }

    /// <summary>Newline-separated redirect URIs the platform may post id_tokens to.</summary>
    public string RedirectUris { get; private set; } = default!;

    /// <summary>The tool's public keys: a JWKS URL, or a PEM public key (one of the two).</summary>
    public string? JwksUrl { get; private set; }
    public string? PublicKeyPem { get; private set; }

    /// <summary>"key=value" lines sent as the custom claim.</summary>
    public string? CustomParameters { get; private set; }

    public LtiPrivacyLevel PrivacyLevel { get; private set; }
    public bool AllowGradeService { get; private set; }
    public bool AllowRosterService { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid CreatedByUserId { get; private set; }

    private readonly List<LtiToolDeployment> _deployments = new();
    public IReadOnlyCollection<LtiToolDeployment> Deployments => _deployments;

    private LtiTool() { }

    public LtiTool(string name, Guid createdByUserId)
    {
        Name = name.Trim();
        CreatedByUserId = createdByUserId;
        ClientId = "edusphere-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();
    }

    public void Configure(string name, string? description, string oidcInitiationUrl, string targetLinkUri, string? deepLinkingUrl,
        IEnumerable<string> redirectUris, string? jwksUrl, string? publicKeyPem, string? customParameters,
        LtiPrivacyLevel privacy, bool allowGradeService, bool allowRosterService)
    {
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        OidcInitiationUrl = oidcInitiationUrl.Trim();
        TargetLinkUri = targetLinkUri.Trim();
        DeepLinkingUrl = string.IsNullOrWhiteSpace(deepLinkingUrl) ? null : deepLinkingUrl.Trim();
        RedirectUris = string.Join("\n", redirectUris.Select(u => u.Trim()).Where(u => u.Length > 0).Distinct());
        JwksUrl = string.IsNullOrWhiteSpace(jwksUrl) ? null : jwksUrl.Trim();
        PublicKeyPem = string.IsNullOrWhiteSpace(publicKeyPem) ? null : publicKeyPem.Trim();
        CustomParameters = string.IsNullOrWhiteSpace(customParameters) ? null : customParameters.Trim();
        PrivacyLevel = privacy;
        AllowGradeService = allowGradeService;
        AllowRosterService = allowRosterService;
        Touch();
    }

    public IReadOnlyList<string> RedirectUriList => RedirectUris.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    public void SetActive(bool active) { IsActive = active; Touch(); }

    public LtiToolDeployment AddDeployment(LtiDeploymentScope scope, Guid? scopeId)
    {
        var deployment = new LtiToolDeployment(Id, scope, scopeId);
        _deployments.Add(deployment);
        return deployment;
    }
}

/// <summary>A deployment of a tool (its own deployment_id) for the installation, a department or one course offering.</summary>
public class LtiToolDeployment : BaseEntity
{
    public Guid ToolId { get; private set; }
    public string DeploymentId { get; private set; } = default!;
    public LtiDeploymentScope Scope { get; private set; }
    public Guid? ScopeId { get; private set; }
    public bool IsActive { get; private set; } = true;

    private LtiToolDeployment() { }

    internal LtiToolDeployment(Guid toolId, LtiDeploymentScope scope, Guid? scopeId)
    {
        if (scope != LtiDeploymentScope.Installation && scopeId is null)
            throw new ArgumentException("Department and course deployments need the department or course.", nameof(scopeId));
        ToolId = toolId;
        Scope = scope;
        ScopeId = scope == LtiDeploymentScope.Installation ? null : scopeId;
        DeploymentId = "dep-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
    }

    public void SetActive(bool active) { IsActive = active; Touch(); }
}

/// <summary>An "External tool" item in a course (an LTI resource link).</summary>
public class LtiResourceLink : BaseEntity
{
    public Guid ToolId { get; private set; }
    public Guid DeploymentRowId { get; private set; }
    public Guid CourseOfferingId { get; private set; }
    public Guid? ModuleId { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }

    /// <summary>The launch URL for this item (null = the tool's target link URI).</summary>
    public string? Url { get; private set; }

    public string? CustomParameters { get; private set; }

    /// <summary>"window" (default) or "iframe".</summary>
    public string PresentationTarget { get; private set; } = "window";

    public Guid? LineItemId { get; private set; }
    public bool IsVisible { get; private set; } = true;
    public Guid CreatedByUserId { get; private set; }

    private LtiResourceLink() { }

    public LtiResourceLink(Guid toolId, Guid deploymentRowId, Guid courseOfferingId, Guid? moduleId, string title, string? description,
        string? url, string? customParameters, string presentationTarget, Guid createdByUserId)
    {
        ToolId = toolId;
        DeploymentRowId = deploymentRowId;
        CourseOfferingId = courseOfferingId;
        ModuleId = moduleId;
        Title = string.IsNullOrWhiteSpace(title) ? "External tool" : title.Trim()[..Math.Min(title.Trim().Length, 200)];
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()[..Math.Min(description.Trim().Length, 1000)];
        Url = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        CustomParameters = string.IsNullOrWhiteSpace(customParameters) ? null : customParameters.Trim();
        PresentationTarget = presentationTarget == "iframe" ? "iframe" : "window";
        CreatedByUserId = createdByUserId;
    }

    public void AttachLineItem(Guid lineItemId) { LineItemId = lineItemId; Touch(); }
    public void DetachLineItem() { LineItemId = null; Touch(); }
    public void SetVisible(bool visible) { IsVisible = visible; Touch(); }
}

/// <summary>An AGS line item (a gradebook column a tool can post scores to). Backed by an EduSphere assignment.</summary>
public class LtiLineItem : BaseEntity
{
    public Guid ToolId { get; private set; }
    public Guid CourseOfferingId { get; private set; }
    public Guid? ResourceLinkId { get; private set; }
    public Guid AssignmentId { get; private set; }
    public string Label { get; private set; } = default!;
    public decimal ScoreMaximum { get; private set; }
    public string? Tag { get; private set; }
    public string? ResourceId { get; private set; }
    public DateTime? StartDateTime { get; private set; }
    public DateTime? EndDateTime { get; private set; }

    private LtiLineItem() { }

    public LtiLineItem(Guid toolId, Guid courseOfferingId, Guid? resourceLinkId, Guid assignmentId, string label, decimal scoreMaximum,
        string? tag, string? resourceId, DateTime? start, DateTime? end)
    {
        ToolId = toolId;
        CourseOfferingId = courseOfferingId;
        ResourceLinkId = resourceLinkId;
        AssignmentId = assignmentId;
        Update(label, scoreMaximum, tag, resourceId, start, end);
    }

    public void Update(string label, decimal scoreMaximum, string? tag, string? resourceId, DateTime? start, DateTime? end)
    {
        if (scoreMaximum <= 0) throw new ArgumentOutOfRangeException(nameof(scoreMaximum), "scoreMaximum must be positive.");
        Label = string.IsNullOrWhiteSpace(label) ? "Tool activity" : label.Trim()[..Math.Min(label.Trim().Length, 200)];
        ScoreMaximum = scoreMaximum;
        Tag = string.IsNullOrWhiteSpace(tag) ? null : tag.Trim()[..Math.Min(tag.Trim().Length, 200)];
        ResourceId = string.IsNullOrWhiteSpace(resourceId) ? null : resourceId.Trim()[..Math.Min(resourceId.Trim().Length, 200)];
        StartDateTime = start;
        EndDateTime = end;
        Touch();
    }
}

/// <summary>Every score a tool posted (AGS Score service), kept for audit and the Result service.</summary>
public class LtiScore : BaseEntity
{
    public Guid LineItemId { get; private set; }
    public Guid StudentProfileId { get; private set; }
    public decimal? ScoreGiven { get; private set; }
    public decimal? ScoreMaximum { get; private set; }
    public string ActivityProgress { get; private set; } = default!;
    public string GradingProgress { get; private set; } = default!;
    public string? Comment { get; private set; }
    public DateTime Timestamp { get; private set; }

    private LtiScore() { }

    public LtiScore(Guid lineItemId, Guid studentProfileId, decimal? scoreGiven, decimal? scoreMaximum, string activityProgress,
        string gradingProgress, string? comment, DateTime timestamp)
    {
        LineItemId = lineItemId;
        StudentProfileId = studentProfileId;
        ScoreGiven = scoreGiven;
        ScoreMaximum = scoreMaximum;
        ActivityProgress = activityProgress;
        GradingProgress = gradingProgress;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()[..Math.Min(comment.Trim().Length, 2000)];
        Timestamp = timestamp;
    }
}

/// <summary>
/// A pending launch: its id is the login_hint (resource-link launch) or the deep-linking "data" value.
/// Single use, valid for a few minutes, bound to one user and one tool.
/// </summary>
public class LtiLaunch : BaseEntity
{
    public Guid ToolId { get; private set; }
    public Guid DeploymentRowId { get; private set; }
    public Guid UserId { get; private set; }
    public string MessageType { get; private set; } = default!;
    public Guid? ResourceLinkId { get; private set; }
    public Guid? CourseOfferingId { get; private set; }
    public Guid? ModuleId { get; private set; }
    public string? ReturnUrl { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? AuthorizedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private LtiLaunch() { }

    public LtiLaunch(Guid toolId, Guid deploymentRowId, Guid userId, string messageType, Guid? resourceLinkId, Guid? courseOfferingId,
        Guid? moduleId, string? returnUrl, DateTime expiresAt)
    {
        ToolId = toolId;
        DeploymentRowId = deploymentRowId;
        UserId = userId;
        MessageType = messageType;
        ResourceLinkId = resourceLinkId;
        CourseOfferingId = courseOfferingId;
        ModuleId = moduleId;
        ReturnUrl = returnUrl;
        ExpiresAt = expiresAt;
    }

    public bool CanAuthorize(DateTime now) => AuthorizedAt is null && now <= ExpiresAt;

    public void MarkAuthorized(DateTime now) { AuthorizedAt = now; Touch(); }

    /// <summary>Deep linking: the tool's response may arrive once, within an hour of the authorization.</summary>
    public bool CanComplete(DateTime now) => AuthorizedAt is not null && CompletedAt is null && now <= AuthorizedAt.Value.AddHours(1);

    public void MarkCompleted(DateTime now) { CompletedAt = now; Touch(); }
}

/// <summary>A used nonce / client-assertion jti (replay protection), kept until it expires.</summary>
public class LtiUsedNonce : BaseEntity
{
    public Guid ToolId { get; private set; }
    public string Value { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }

    private LtiUsedNonce() { }

    public LtiUsedNonce(Guid toolId, string value, DateTime expiresAt)
    {
        ToolId = toolId;
        Value = value.Length > 200 ? value[..200] : value;
        ExpiresAt = expiresAt;
    }
}

/// <summary>An access token issued to a tool for AGS/NRPS (only its SHA-256 is stored).</summary>
public class LtiAccessToken : BaseEntity
{
    public Guid ToolId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public string Scopes { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }

    private LtiAccessToken() { }

    public LtiAccessToken(Guid toolId, string tokenHash, IEnumerable<string> scopes, DateTime expiresAt)
    {
        ToolId = toolId;
        TokenHash = tokenHash;
        Scopes = string.Join(" ", scopes);
        ExpiresAt = expiresAt;
    }

    public bool Allows(string scope) => Scopes.Split(' ').Contains(scope);
}
