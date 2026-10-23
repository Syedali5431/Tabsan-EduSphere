namespace Tabsan.EduSphere.Application.Lti;

// Plan M5 — LTI 1.3 / LTI Advantage platform contracts.

/// <summary>Settings (section "Lti").</summary>
public sealed class LtiOptions
{
    public const string SectionName = "Lti";

    /// <summary>
    /// The portal's public https address (e.g. https://portal.uni.edu). It is the platform issuer, and the
    /// authorization, token, JWKS, deep-linking and service URLs given to tools are under it. Empty = LTI off.
    /// </summary>
    public string? PlatformUrl { get; set; }

    /// <summary>Where the platform's RSA signing keys live (generated on first use). Default: &lt;app-data&gt;/Tabsan/lti-platform-keys.json.</summary>
    public string? KeyFile { get; set; }

    /// <summary>How long a rotated-out key stays in the JWKS so tokens signed just before rotation still verify.</summary>
    public int RetiredKeyDays { get; set; } = 7;

    public int LaunchMinutes { get; set; } = 5;
    public int AccessTokenMinutes { get; set; } = 60;
    public string PlatformName { get; set; } = "Tabsan EduSphere";

    public bool IsConfigured => Uri.TryCreate(PlatformUrl, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttps || u.IsLoopback);

    private string Base => (PlatformUrl ?? string.Empty).TrimEnd('/');
    public string Issuer => Base;
    public string AuthorizationUrl => Base + "/lti/authorize";
    public string TokenUrl => Base + "/lti/token";
    public string JwksUrl => Base + "/lti/jwks";
    public string DeepLinkReturnUrl => Base + "/lti/deep-link-return";
    public string LineItemsUrl(Guid offeringId) => $"{Base}/lti/ags/{offeringId}/lineitems";
    public string LineItemUrl(Guid offeringId, Guid lineItemId) => $"{Base}/lti/ags/{offeringId}/lineitems/{lineItemId}";
    public string MembershipsUrl(Guid offeringId) => $"{Base}/lti/nrps/{offeringId}/memberships";
}

/// <summary>Well-known LTI strings.</summary>
public static class LtiConstants
{
    public const string Version = "1.3.0";
    public const string ResourceLinkRequest = "LtiResourceLinkRequest";
    public const string DeepLinkingRequest = "LtiDeepLinkingRequest";
    public const string DeepLinkingResponse = "LtiDeepLinkingResponse";

    public const string ClaimMessageType = "https://purl.imsglobal.org/spec/lti/claim/message_type";
    public const string ClaimVersion = "https://purl.imsglobal.org/spec/lti/claim/version";
    public const string ClaimDeploymentId = "https://purl.imsglobal.org/spec/lti/claim/deployment_id";
    public const string ClaimTargetLinkUri = "https://purl.imsglobal.org/spec/lti/claim/target_link_uri";
    public const string ClaimResourceLink = "https://purl.imsglobal.org/spec/lti/claim/resource_link";
    public const string ClaimRoles = "https://purl.imsglobal.org/spec/lti/claim/roles";
    public const string ClaimContext = "https://purl.imsglobal.org/spec/lti/claim/context";
    public const string ClaimPlatform = "https://purl.imsglobal.org/spec/lti/claim/tool_platform";
    public const string ClaimLaunchPresentation = "https://purl.imsglobal.org/spec/lti/claim/launch_presentation";
    public const string ClaimCustom = "https://purl.imsglobal.org/spec/lti/claim/custom";
    public const string ClaimAgs = "https://purl.imsglobal.org/spec/lti-ags/claim/endpoint";
    public const string ClaimNrps = "https://purl.imsglobal.org/spec/lti-nrps/claim/namesroleservice";
    public const string ClaimDeepLinkingSettings = "https://purl.imsglobal.org/spec/lti-dl/claim/deep_linking_settings";
    public const string ClaimDeepLinkingContentItems = "https://purl.imsglobal.org/spec/lti-dl/claim/content_items";
    public const string ClaimDeepLinkingData = "https://purl.imsglobal.org/spec/lti-dl/claim/data";
    public const string ClaimDeepLinkingMessage = "https://purl.imsglobal.org/spec/lti-dl/claim/msg";

    public const string ScopeLineItem = "https://purl.imsglobal.org/spec/lti-ags/scope/lineitem";
    public const string ScopeLineItemReadOnly = "https://purl.imsglobal.org/spec/lti-ags/scope/lineitem.readonly";
    public const string ScopeResultReadOnly = "https://purl.imsglobal.org/spec/lti-ags/scope/result.readonly";
    public const string ScopeScore = "https://purl.imsglobal.org/spec/lti-ags/scope/score";
    public const string ScopeMemberships = "https://purl.imsglobal.org/spec/lti-nrps/scope/contextmembership.readonly";

    public static readonly IReadOnlyList<string> AgsScopes = [ScopeLineItem, ScopeLineItemReadOnly, ScopeResultReadOnly, ScopeScore];

    public const string RoleLearner = "http://purl.imsglobal.org/vocab/lis/v2/membership#Learner";
    public const string RoleInstructor = "http://purl.imsglobal.org/vocab/lis/v2/membership#Instructor";
    public const string RoleAdministrator = "http://purl.imsglobal.org/vocab/lis/v2/membership#Administrator";
    public const string InstitutionStudent = "http://purl.imsglobal.org/vocab/lis/v2/institution/person#Student";
    public const string InstitutionFaculty = "http://purl.imsglobal.org/vocab/lis/v2/institution/person#Faculty";
    public const string InstitutionAdministrator = "http://purl.imsglobal.org/vocab/lis/v2/institution/person#Administrator";
    public const string InstitutionStaff = "http://purl.imsglobal.org/vocab/lis/v2/institution/person#Staff";
    public const string SystemAdministrator = "http://purl.imsglobal.org/vocab/lis/v2/system/person#Administrator";
    public const string ContextCourseOffering = "http://purl.imsglobal.org/vocab/lis/v2/course#CourseOffering";

    /// <summary>LIS roles for an EduSphere role (in the course context and at institution level).</summary>
    public static IReadOnlyList<string> RolesFor(string role, bool isCourseInstructor) => role switch
    {
        "SuperAdmin" => [RoleAdministrator, InstitutionAdministrator, SystemAdministrator],
        "Admin" => [RoleAdministrator, InstitutionAdministrator],
        "Faculty" => [RoleInstructor, InstitutionFaculty],
        "Student" => [RoleLearner, InstitutionStudent],
        _ => isCourseInstructor ? [RoleInstructor, InstitutionStaff] : [InstitutionStaff]
    };
}

/// <summary>The platform's RSA signing keys (RS256) and its public JWKS.</summary>
public interface ILtiPlatformKeys
{
    string Sign(string payloadJson);
    string JwksJson();
    IReadOnlyList<LtiKeyInfo> Keys { get; }
    void Rotate();
}

public sealed record LtiKeyInfo(string Kid, DateTime CreatedAt, DateTime? RetiredAt, bool Active);

/// <summary>Validates JWTs signed by a tool (deep-linking responses, client assertions) against the tool's registered keys.</summary>
public interface ILtiToolJwtValidator
{
    /// <summary>Returns the validated payload JSON, or an error message.</summary>
    Task<(string? PayloadJson, string? Error)> ValidateAsync(string jwt, string? jwksUrl, string? publicKeyPem, string expectedIssuer,
        IReadOnlyCollection<string> validAudiences, CancellationToken ct = default);
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record LtiPlatformInfo(bool Configured, string Issuer, string AuthorizationUrl, string TokenUrl, string JwksUrl,
    string DeepLinkReturnUrl, IReadOnlyList<LtiKeyInfo> Keys);

public sealed record LtiDeploymentResponse(Guid Id, string DeploymentId, string Scope, Guid? ScopeId, bool IsActive);

public sealed record LtiToolResponse(Guid Id, string Name, string? Description, string ClientId, string OidcInitiationUrl, string TargetLinkUri,
    string? DeepLinkingUrl, IReadOnlyList<string> RedirectUris, string? JwksUrl, bool HasPublicKey, string? CustomParameters,
    string PrivacyLevel, bool AllowGradeService, bool AllowRosterService, bool IsActive, IReadOnlyList<LtiDeploymentResponse> Deployments);

public sealed record SaveLtiToolRequest(string Name, string? Description, string OidcInitiationUrl, string TargetLinkUri, string? DeepLinkingUrl,
    IReadOnlyList<string> RedirectUris, string? JwksUrl, string? PublicKeyPem, string? CustomParameters, string PrivacyLevel,
    bool AllowGradeService, bool AllowRosterService);

public sealed record AddLtiDeploymentRequest(string Scope, Guid? ScopeId);

public sealed record LtiCourseToolResponse(Guid ToolId, string Name, string? Description, bool SupportsDeepLinking);

public sealed record LtiLinkResponse(Guid Id, Guid ToolId, string ToolName, Guid? ModuleId, string Title, string? Description,
    string PresentationTarget, bool IsVisible, Guid? LineItemId, Guid? AssignmentId);

public sealed record CreateLtiLinkRequest(Guid ToolId, Guid? ModuleId, string Title, string? Description, string? Url,
    string? CustomParameters, string PresentationTarget, decimal? GradeMaximum);

/// <summary>What the portal posts to the tool's OIDC login initiation URL.</summary>
public sealed record LtiLoginInitiation(string Url, IReadOnlyDictionary<string, string> Parameters, string PresentationTarget);

/// <summary>The authentication request the tool sends to the platform (OIDC, response_type=id_token).</summary>
public sealed record LtiAuthorizationRequest(string? Scope, string? ResponseType, string? ResponseMode, string? Prompt, string? ClientId,
    string? RedirectUri, string? LoginHint, string? LtiMessageHint, string? State, string? Nonce);

/// <summary>The auto-posted response: id_token and state to the tool's redirect URI.</summary>
public sealed record LtiAuthorizationResponse(string RedirectUri, string IdToken, string? State);

public sealed record LtiDeepLinkResult(Guid CourseOfferingId, int Created, IReadOnlyList<string> Titles, string? Message);

public sealed record LtiTokenRequest(string? GrantType, string? ClientAssertionType, string? ClientAssertion, string? Scope);

public sealed record LtiTokenResponse(string AccessToken, string TokenType, int ExpiresIn, string Scope);

/// <summary>An AGS/NRPS caller after its access token was checked.</summary>
public sealed record LtiServiceCaller(Guid ToolId, IReadOnlyCollection<string> Scopes);

public sealed record LtiResult<T>(T? Value, int Status, string? Error)
{
    public bool Ok => Status is >= 200 and < 300;
    public static LtiResult<T> Success(T value, int status = 200) => new(value, status, null);
    public static LtiResult<T> Fail(int status, string error) => new(default, status, error);
}
