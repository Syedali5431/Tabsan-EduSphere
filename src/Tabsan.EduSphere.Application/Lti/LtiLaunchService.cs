using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lti;

namespace Tabsan.EduSphere.Application.Lti;

/// <summary>
/// Plan M5.2/M5.3 — course placements, the OIDC third-party-initiated launch and Deep Linking 2.0.
/// <list type="bullet">
///   <item>A launch starts when a signed-in user opens a course item: a single-use launch record (5 minutes) is created and
///         its id becomes the <c>login_hint</c>/<c>lti_message_hint</c>, so the authorization step does not depend on the
///         portal cookie (tools often post back cross-site or run in an iframe).</item>
///   <item>Authorization checks client id, registered redirect URI, response type/mode, scope, nonce (no replay) and that the
///         launch belongs to this tool and is unused; when the caller is signed in it must be the launching user.</item>
///   <item>Deep-linking responses must be signed by the tool's registered keys, address this platform, carry the
///         deployment id and the launch's <c>data</c>, and are accepted once.</item>
/// </list>
/// </summary>
public sealed class LtiLaunchService
{
    private readonly ILtiRepository _repo;
    private readonly ILtiPlatformKeys _keys;
    private readonly ILtiToolJwtValidator _validator;
    private readonly LtiGradeBridge _grades;
    private readonly IAuditService _audit;
    private readonly LtiOptions _options;

    public LtiLaunchService(ILtiRepository repo, ILtiPlatformKeys keys, ILtiToolJwtValidator validator, LtiGradeBridge grades,
        IAuditService audit, IOptions<LtiOptions> options)
    {
        _repo = repo;
        _keys = keys;
        _validator = validator;
        _grades = grades;
        _audit = audit;
        _options = options.Value;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Course placements
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<LtiCourseToolResponse>> ToolsForOfferingAsync(Guid offeringId, CancellationToken ct = default)
        => (await _repo.GetToolsForOfferingAsync(offeringId, ct))
            .Select(t => new LtiCourseToolResponse(t.Id, t.Name, t.Description, t.DeepLinkingUrl is not null)).ToList();

    public async Task<IReadOnlyList<LtiLinkResponse>> LinksAsync(Guid offeringId, bool includeHidden, CancellationToken ct = default)
    {
        var tools = (await _repo.GetToolsAsync(ct)).ToDictionary(t => t.Id);
        var links = await _repo.GetLinksForOfferingAsync(offeringId, ct);
        var result = new List<LtiLinkResponse>();
        foreach (var l in links.Where(l => includeHidden || (l.IsVisible && tools.TryGetValue(l.ToolId, out var t) && t.IsActive)))
        {
            Guid? assignmentId = l.LineItemId is { } li && await _repo.GetLineItemAsync(li, ct) is { } item ? item.AssignmentId : null;
            result.Add(new LtiLinkResponse(l.Id, l.ToolId, tools.TryGetValue(l.ToolId, out var tool) ? tool.Name : "Tool", l.ModuleId, l.Title,
                l.Description, l.PresentationTarget, l.IsVisible, l.LineItemId, assignmentId));
        }
        return result;
    }

    public async Task<LtiResult<LtiLinkResponse>> CreateLinkAsync(Guid offeringId, CreateLtiLinkRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var tool = await _repo.GetToolAsync(request.ToolId, ct);
        if (tool is not { IsActive: true }) return LtiResult<LtiLinkResponse>.Fail(404, "Tool not found.");
        var deployment = await _repo.FindDeploymentForOfferingAsync(tool.Id, offeringId, ct);
        if (deployment is null) return LtiResult<LtiLinkResponse>.Fail(403, "This tool is not deployed for this course.");
        if (!string.IsNullOrWhiteSpace(request.Url) && !LtiAdminService.SafeUrl(request.Url)) return LtiResult<LtiLinkResponse>.Fail(400, "The launch URL must be an https address.");
        if (request.GradeMaximum is <= 0) return LtiResult<LtiLinkResponse>.Fail(400, "Maximum points must be positive.");
        if (request.GradeMaximum is not null && !tool.AllowGradeService) return LtiResult<LtiLinkResponse>.Fail(400, "This tool may not send grades.");

        var link = new LtiResourceLink(tool.Id, deployment.Id, offeringId, request.ModuleId, request.Title, request.Description, request.Url,
            request.CustomParameters, request.PresentationTarget, actorUserId);
        _repo.Add(link);
        await _repo.SaveChangesAsync(ct);
        if (request.GradeMaximum is { } max)
        {
            var item = await _grades.CreateLineItemAsync(tool.Id, offeringId, link.Id, link.Title, max, null, null, null, null, ct);
            link.AttachLineItem(item.Id);
            await _repo.SaveChangesAsync(ct);
        }
        await _audit.LogAsync(new AuditLog("LtiLinkCreated", "LtiResourceLink", link.Id.ToString(), actorUserId: actorUserId), ct);
        return LtiResult<LtiLinkResponse>.Success((await LinksAsync(offeringId, true, ct)).First(l => l.Id == link.Id), 201);
    }

    public async Task<bool> SetLinkVisibleAsync(Guid linkId, bool visible, Guid actorUserId, CancellationToken ct = default)
    {
        var link = await _repo.GetLinkAsync(linkId, ct);
        if (link is null) return false;
        link.SetVisible(visible);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog(visible ? "LtiLinkShown" : "LtiLinkHidden", "LtiResourceLink", link.Id.ToString(), actorUserId: actorUserId), ct);
        return true;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Starting launches
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Starts a resource-link launch for <paramref name="userId"/> (access to the course is checked by the caller).</summary>
    public async Task<LtiResult<LtiLoginInitiation>> StartLaunchAsync(Guid userId, Guid linkId, bool isStaff, string? returnUrl, CancellationToken ct = default)
    {
        if (!_options.IsConfigured) return LtiResult<LtiLoginInitiation>.Fail(503, "External tools are not configured on this server.");
        var link = await _repo.GetLinkAsync(linkId, ct);
        if (link is null || (!link.IsVisible && !isStaff)) return LtiResult<LtiLoginInitiation>.Fail(404, "This item is not available.");
        var tool = await _repo.GetToolAsync(link.ToolId, ct);
        var deployment = await _repo.GetDeploymentAsync(link.DeploymentRowId, ct);
        if (tool is not { IsActive: true } || deployment is not { IsActive: true }) return LtiResult<LtiLoginInitiation>.Fail(409, "This tool is switched off.");

        var launch = new LtiLaunch(tool.Id, deployment.Id, userId, LtiConstants.ResourceLinkRequest, link.Id, link.CourseOfferingId, link.ModuleId,
            returnUrl, DateTime.UtcNow.AddMinutes(Math.Clamp(_options.LaunchMinutes, 1, 15)));
        _repo.Add(launch);
        await _repo.SaveChangesAsync(ct);
        return LtiResult<LtiLoginInitiation>.Success(Initiation(tool, deployment, launch, link.Url ?? tool.TargetLinkUri, link.PresentationTarget));
    }

    /// <summary>Starts a Deep Linking request from a course (staff only; checked by the caller).</summary>
    public async Task<LtiResult<LtiLoginInitiation>> StartDeepLinkingAsync(Guid userId, Guid offeringId, Guid toolId, Guid? moduleId, string? returnUrl, CancellationToken ct = default)
    {
        if (!_options.IsConfigured) return LtiResult<LtiLoginInitiation>.Fail(503, "External tools are not configured on this server.");
        var tool = await _repo.GetToolAsync(toolId, ct);
        if (tool is not { IsActive: true }) return LtiResult<LtiLoginInitiation>.Fail(404, "Tool not found.");
        if (tool.DeepLinkingUrl is null) return LtiResult<LtiLoginInitiation>.Fail(400, "This tool does not offer content selection.");
        var deployment = await _repo.FindDeploymentForOfferingAsync(toolId, offeringId, ct);
        if (deployment is null) return LtiResult<LtiLoginInitiation>.Fail(403, "This tool is not deployed for this course.");

        var launch = new LtiLaunch(tool.Id, deployment.Id, userId, LtiConstants.DeepLinkingRequest, null, offeringId, moduleId, returnUrl,
            DateTime.UtcNow.AddMinutes(Math.Clamp(_options.LaunchMinutes, 1, 15)));
        _repo.Add(launch);
        await _repo.SaveChangesAsync(ct);
        return LtiResult<LtiLoginInitiation>.Success(Initiation(tool, deployment, launch, tool.DeepLinkingUrl, "window"));
    }

    private LtiLoginInitiation Initiation(LtiTool tool, LtiToolDeployment deployment, LtiLaunch launch, string targetLinkUri, string presentation)
        => new(tool.OidcInitiationUrl, new Dictionary<string, string>
        {
            ["iss"] = _options.Issuer,
            ["login_hint"] = launch.Id.ToString("N"),
            ["lti_message_hint"] = launch.Id.ToString("N"),
            ["target_link_uri"] = targetLinkUri,
            ["client_id"] = tool.ClientId,
            ["lti_deployment_id"] = deployment.DeploymentId
        }, presentation);

    // ═════════════════════════════════════════════════════════════════════════
    // Authorization (id_token)
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<LtiResult<LtiAuthorizationResponse>> AuthorizeAsync(LtiAuthorizationRequest r, Guid? signedInUserId, CancellationToken ct = default)
    {
        static LtiResult<LtiAuthorizationResponse> Fail(string message) => LtiResult<LtiAuthorizationResponse>.Fail(400, message);
        if (!_options.IsConfigured) return LtiResult<LtiAuthorizationResponse>.Fail(503, "External tools are not configured on this server.");

        if (!(r.Scope ?? string.Empty).Split(' ').Contains("openid")) return Fail("invalid_scope: openid is required.");
        if (r.ResponseType != "id_token") return Fail("unsupported_response_type: id_token is required.");
        if (r.ResponseMode != "form_post") return Fail("invalid_request: response_mode must be form_post.");
        if (r.Prompt is not null && r.Prompt != "none") return Fail("invalid_request: prompt must be none.");
        if (string.IsNullOrWhiteSpace(r.Nonce) || r.Nonce.Length > 200) return Fail("invalid_request: a nonce is required.");

        var tool = r.ClientId is null ? null : await _repo.GetToolByClientIdAsync(r.ClientId, ct);
        if (tool is not { IsActive: true }) return Fail("unauthorized_client: unknown client_id.");
        if (r.RedirectUri is null || !tool.RedirectUriList.Contains(r.RedirectUri, StringComparer.Ordinal)) return Fail("invalid_request: redirect_uri is not registered for this tool.");
        if (!Guid.TryParseExact(r.LoginHint, "N", out var launchId) || (r.LtiMessageHint is not null && r.LtiMessageHint != r.LoginHint))
            return Fail("invalid_request: unknown login_hint.");

        var launch = await _repo.GetLaunchAsync(launchId, ct);
        var now = DateTime.UtcNow;
        if (launch is null || launch.ToolId != tool.Id) return Fail("invalid_request: unknown launch.");
        if (!launch.CanAuthorize(now)) return Fail("login_required: this launch has expired or was already used — open the item again.");
        if (signedInUserId is { } current && current != launch.UserId) return Fail("login_required: the launch belongs to another user.");
        var deployment = await _repo.GetDeploymentAsync(launch.DeploymentRowId, ct);
        if (deployment is not { IsActive: true }) return Fail("invalid_request: the deployment is switched off.");
        if (!await _repo.TryUseNonceAsync(tool.Id, "auth:" + r.Nonce, now.AddMinutes(30), ct)) return Fail("invalid_request: nonce already used.");

        var person = await _repo.GetPersonAsync(launch.UserId, ct);
        if (person is not { IsActive: true }) return Fail("login_required: the user is not active.");
        var context = launch.CourseOfferingId is { } offeringId ? await _repo.GetOfferingContextAsync(offeringId, ct) : null;
        var link = launch.ResourceLinkId is { } linkId ? await _repo.GetLinkAsync(linkId, ct) : null;
        if (launch.MessageType == LtiConstants.ResourceLinkRequest && link is null) return Fail("invalid_request: the course item no longer exists.");

        var claims = await BuildClaimsAsync(tool, deployment, launch, person, context, link, r.Nonce, now, ct);
        var idToken = _keys.Sign(claims.ToJsonString());
        launch.MarkAuthorized(now);
        await _repo.SaveChangesAsync(ct);
        return LtiResult<LtiAuthorizationResponse>.Success(new LtiAuthorizationResponse(r.RedirectUri, idToken, r.State));
    }

    private async Task<JsonObject> BuildClaimsAsync(LtiTool tool, LtiToolDeployment deployment, LtiLaunch launch, LtiPerson person,
        LtiOfferingContext? context, LtiResourceLink? link, string nonce, DateTime now, CancellationToken ct)
    {
        var epoch = new DateTimeOffset(now).ToUnixTimeSeconds();
        var isInstructor = person.Role is "Faculty" or "Admin" or "SuperAdmin";
        var claims = new JsonObject
        {
            ["iss"] = _options.Issuer,
            ["aud"] = tool.ClientId,
            ["azp"] = tool.ClientId,
            ["sub"] = person.UserId.ToString(),
            ["iat"] = epoch,
            ["exp"] = epoch + 300,
            ["nonce"] = nonce,
            [LtiConstants.ClaimMessageType] = launch.MessageType,
            [LtiConstants.ClaimVersion] = LtiConstants.Version,
            [LtiConstants.ClaimDeploymentId] = deployment.DeploymentId,
            [LtiConstants.ClaimRoles] = new JsonArray(LtiConstants.RolesFor(person.Role, isInstructor).Select(r => (JsonNode)r).ToArray()),
            [LtiConstants.ClaimPlatform] = new JsonObject
            {
                ["guid"] = PlatformGuid(),
                ["name"] = _options.PlatformName,
                ["product_family_code"] = "tabsan-edusphere",
                ["version"] = typeof(LtiLaunchService).Assembly.GetName().Version?.ToString() ?? "1.0"
            },
            [LtiConstants.ClaimLaunchPresentation] = new JsonObject
            {
                ["document_target"] = link?.PresentationTarget ?? "window",
                ["locale"] = "en"
            }
        };
        if (launch.ReturnUrl is { } returnUrl && Uri.TryCreate(new Uri(_options.Issuer + "/"), returnUrl, out var absolute))
            ((JsonObject)claims[LtiConstants.ClaimLaunchPresentation]!)["return_url"] = absolute.ToString();

        // Personal information the tool's privacy level allows.
        if (tool.PrivacyLevel is LtiPrivacyLevel.NameOnly or LtiPrivacyLevel.Public && !string.IsNullOrWhiteSpace(person.FullName))
        {
            var parts = person.FullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            claims["name"] = person.FullName.Trim();
            claims["given_name"] = parts[0];
            if (parts.Length > 1) claims["family_name"] = parts[1];
        }
        if (tool.PrivacyLevel is LtiPrivacyLevel.EmailOnly or LtiPrivacyLevel.Public && !string.IsNullOrWhiteSpace(person.Email))
            claims["email"] = person.Email;

        if (context is not null)
        {
            claims[LtiConstants.ClaimContext] = new JsonObject
            {
                ["id"] = context.OfferingId.ToString(),
                ["label"] = context.CourseCode,
                ["title"] = string.IsNullOrWhiteSpace(context.SemesterName) ? context.CourseTitle : $"{context.CourseTitle} ({context.SemesterName})",
                ["type"] = new JsonArray(LtiConstants.ContextCourseOffering)
            };
        }

        var custom = ParseCustom(tool.CustomParameters);
        foreach (var kv in ParseCustom(link?.CustomParameters)) custom[kv.Key] = kv.Value;

        if (launch.MessageType == LtiConstants.ResourceLinkRequest && link is not null)
        {
            claims[LtiConstants.ClaimTargetLinkUri] = link.Url ?? tool.TargetLinkUri;
            claims[LtiConstants.ClaimResourceLink] = new JsonObject { ["id"] = link.Id.ToString(), ["title"] = link.Title, ["description"] = link.Description };
        }
        else
        {
            claims[LtiConstants.ClaimTargetLinkUri] = tool.DeepLinkingUrl ?? tool.TargetLinkUri;
            claims[LtiConstants.ClaimDeepLinkingSettings] = new JsonObject
            {
                ["deep_link_return_url"] = _options.DeepLinkReturnUrl,
                ["accept_types"] = new JsonArray("ltiResourceLink"),
                ["accept_presentation_document_targets"] = new JsonArray("iframe", "window"),
                ["accept_multiple"] = true,
                ["accept_lineitem"] = tool.AllowGradeService,
                ["auto_create"] = true,
                ["data"] = launch.Id.ToString("N"),
                ["title"] = "Add to " + (context?.CourseTitle ?? "course")
            };
        }
        if (custom.Count > 0) claims[LtiConstants.ClaimCustom] = new JsonObject(custom.Select(kv => KeyValuePair.Create(kv.Key, (JsonNode?)kv.Value)));

        if (context is not null && tool.AllowGradeService)
        {
            var ags = new JsonObject
            {
                ["scope"] = new JsonArray(LtiConstants.AgsScopes.Select(s => (JsonNode)s).ToArray()),
                ["lineitems"] = _options.LineItemsUrl(context.OfferingId)
            };
            if (link?.LineItemId is { } lineItemId) ags["lineitem"] = _options.LineItemUrl(context.OfferingId, lineItemId);
            claims[LtiConstants.ClaimAgs] = ags;
        }
        if (context is not null && tool.AllowRosterService)
        {
            claims[LtiConstants.ClaimNrps] = new JsonObject
            {
                ["context_memberships_url"] = _options.MembershipsUrl(context.OfferingId),
                ["service_versions"] = new JsonArray("2.0")
            };
        }
        await Task.CompletedTask;
        return claims;
    }

    private string PlatformGuid()
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(_options.Issuer));
        return new Guid(hash[..16]).ToString();
    }

    /// <summary>"key=value" lines; keys are letters, digits and underscores.</summary>
    public static Dictionary<string, string> ParseCustom(string? text)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in (text ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var i = line.IndexOf('=');
            if (i <= 0) continue;
            var key = line[..i].Trim();
            if (key.Length is 0 or > 100 || !key.All(c => char.IsLetterOrDigit(c) || c == '_')) continue;
            result[key] = line[(i + 1)..].Trim();
        }
        return result;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Deep Linking response
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<LtiResult<LtiDeepLinkResult>> CompleteDeepLinkingAsync(string jwt, CancellationToken ct = default)
    {
        static LtiResult<LtiDeepLinkResult> Fail(string message) => LtiResult<LtiDeepLinkResult>.Fail(400, message);
        if (!_options.IsConfigured) return LtiResult<LtiDeepLinkResult>.Fail(503, "External tools are not configured on this server.");

        var issuer = UnverifiedIssuer(jwt);
        var tool = issuer is null ? null : await _repo.GetToolByClientIdAsync(issuer, ct);
        if (tool is not { IsActive: true }) return Fail("The response did not come from a registered tool.");
        var (payload, error) = await _validator.ValidateAsync(jwt, tool.JwksUrl, tool.PublicKeyPem, tool.ClientId, [_options.Issuer], ct);
        if (payload is null) return Fail(error ?? "The response could not be verified.");

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        string? Str(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        if (Str(LtiConstants.ClaimMessageType) != LtiConstants.DeepLinkingResponse) return Fail("Wrong message type.");
        if (Str(LtiConstants.ClaimVersion) != LtiConstants.Version) return Fail("Unsupported LTI version.");
        var nonce = Str("nonce");
        if (string.IsNullOrWhiteSpace(nonce) || !await _repo.TryUseNonceAsync(tool.Id, "dl:" + nonce, DateTime.UtcNow.AddHours(2), ct))
            return Fail("The response was already used.");
        if (!Guid.TryParseExact(Str(LtiConstants.ClaimDeepLinkingData), "N", out var launchId)) return Fail("The response does not belong to a selection started here.");

        var launch = await _repo.GetLaunchAsync(launchId, ct);
        var now = DateTime.UtcNow;
        if (launch is null || launch.ToolId != tool.Id || launch.MessageType != LtiConstants.DeepLinkingRequest || !launch.CanComplete(now) || launch.CourseOfferingId is null)
            return Fail("The selection has expired or was already completed — start it again.");
        var deployment = await _repo.GetDeploymentAsync(launch.DeploymentRowId, ct);
        if (deployment is null || Str(LtiConstants.ClaimDeploymentId) != deployment.DeploymentId) return Fail("The response is for another deployment.");

        var titles = new List<string>();
        var skipped = 0;
        if (root.TryGetProperty(LtiConstants.ClaimDeepLinkingContentItems, out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray().Take(50))
            {
                if (!item.TryGetProperty("type", out var type) || type.GetString() != "ltiResourceLink") { skipped++; continue; }
                string? Item(string name) => item.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                var url = Item("url");
                if (url is not null && !LtiAdminService.SafeUrl(url)) { skipped++; continue; }
                var custom = item.TryGetProperty("custom", out var c) && c.ValueKind == JsonValueKind.Object
                    ? string.Join("\n", c.EnumerateObject().Where(p => p.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number).Select(p => $"{p.Name}={p.Value}"))
                    : null;
                var target = item.TryGetProperty("iframe", out _) ? "iframe" : "window";
                var link = new LtiResourceLink(tool.Id, deployment.Id, launch.CourseOfferingId.Value, launch.ModuleId, Item("title") ?? "External tool",
                    Item("text"), url, custom, target, launch.UserId);
                _repo.Add(link);
                await _repo.SaveChangesAsync(ct);

                if (tool.AllowGradeService && item.TryGetProperty("lineItem", out var li) && li.ValueKind == JsonValueKind.Object
                    && li.TryGetProperty("scoreMaximum", out var maxEl) && maxEl.TryGetDecimal(out var max) && max > 0)
                {
                    string? Li(string name) => li.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                    var lineItem = await _grades.CreateLineItemAsync(tool.Id, launch.CourseOfferingId.Value, link.Id, Li("label") ?? link.Title, max,
                        Li("tag"), Li("resourceId"), null, null, ct);
                    link.AttachLineItem(lineItem.Id);
                    await _repo.SaveChangesAsync(ct);
                }
                titles.Add(link.Title);
            }
        }

        launch.MarkCompleted(now);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("LtiDeepLinkingCompleted", "LtiTool", tool.Id.ToString(), actorUserId: launch.UserId,
            newValuesJson: $"{{\"created\":{titles.Count},\"skipped\":{skipped}}}"), ct);
        var message = Str(LtiConstants.ClaimDeepLinkingMessage);
        if (skipped > 0) message = $"{message} {skipped} item(s) of a kind EduSphere cannot place were skipped.".Trim();
        return LtiResult<LtiDeepLinkResult>.Success(new LtiDeepLinkResult(launch.CourseOfferingId.Value, titles.Count, titles, message));
    }

    /// <summary>Reads "iss" without verifying (only to find which tool's keys to verify with).</summary>
    public static string? UnverifiedIssuer(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3) return null;
            using var doc = JsonDocument.Parse(Notifications.Base64Url.Decode(parts[1]));
            return doc.RootElement.TryGetProperty("iss", out var iss) && iss.ValueKind == JsonValueKind.String ? iss.GetString() : null;
        }
        catch (Exception ex) when (ex is FormatException or JsonException) { return null; }
    }
}
