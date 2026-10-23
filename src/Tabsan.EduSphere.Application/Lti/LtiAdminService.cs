using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lti;

namespace Tabsan.EduSphere.Application.Lti;

/// <summary>
/// Plan M5.1 — tool registry (manual LTI 1.3 registration), deployments, platform details for tool vendors and
/// signing-key rotation. Every change is audited.
/// </summary>
public sealed class LtiAdminService
{
    private readonly ILtiRepository _repo;
    private readonly ILtiPlatformKeys _keys;
    private readonly IAuditService _audit;
    private readonly LtiOptions _options;

    public LtiAdminService(ILtiRepository repo, ILtiPlatformKeys keys, IAuditService audit, IOptions<LtiOptions> options)
    {
        _repo = repo;
        _keys = keys;
        _audit = audit;
        _options = options.Value;
    }

    public LtiPlatformInfo PlatformInfo()
        => new(_options.IsConfigured, _options.Issuer, _options.AuthorizationUrl, _options.TokenUrl, _options.JwksUrl, _options.DeepLinkReturnUrl,
            _options.IsConfigured ? _keys.Keys : []);

    public async Task<IReadOnlyList<LtiToolResponse>> ListAsync(CancellationToken ct = default)
        => (await _repo.GetToolsAsync(ct)).Select(ToResponse).ToList();

    public async Task<LtiToolResponse?> GetAsync(Guid toolId, CancellationToken ct = default)
        => await _repo.GetToolAsync(toolId, ct) is { } tool ? ToResponse(tool) : null;

    public async Task<LtiResult<LtiToolResponse>> CreateAsync(SaveLtiToolRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        if (Validate(request) is { } error) return LtiResult<LtiToolResponse>.Fail(400, error);
        var tool = new LtiTool(request.Name, actorUserId);
        Apply(tool, request);
        // Every tool starts with one installation-wide deployment; narrow it on the tool page if needed.
        tool.AddDeployment(LtiDeploymentScope.Installation, null);
        _repo.Add(tool);
        foreach (var d in tool.Deployments) _repo.Add(d);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("LtiToolRegistered", "LtiTool", tool.Id.ToString(), actorUserId: actorUserId, newValuesJson: $"{{\"name\":\"{tool.Name}\",\"clientId\":\"{tool.ClientId}\"}}"), ct);
        return LtiResult<LtiToolResponse>.Success(ToResponse(tool), 201);
    }

    public async Task<LtiResult<LtiToolResponse>> UpdateAsync(Guid toolId, SaveLtiToolRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var tool = await _repo.GetToolAsync(toolId, ct);
        if (tool is null) return LtiResult<LtiToolResponse>.Fail(404, "Tool not found.");
        // The stored key is never shown again, so a blank key on an edit keeps it; pasting a new one replaces it.
        // A JWKS URL supersedes it (a stored PEM would otherwise win when tokens are checked).
        if (string.IsNullOrWhiteSpace(request.PublicKeyPem) && tool.PublicKeyPem is not null && string.IsNullOrWhiteSpace(request.JwksUrl))
            request = request with { PublicKeyPem = tool.PublicKeyPem };
        if (Validate(request) is { } error) return LtiResult<LtiToolResponse>.Fail(400, error);
        Apply(tool, request);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("LtiToolUpdated", "LtiTool", tool.Id.ToString(), actorUserId: actorUserId), ct);
        return LtiResult<LtiToolResponse>.Success(ToResponse(tool));
    }

    public async Task<bool> SetActiveAsync(Guid toolId, bool active, Guid actorUserId, CancellationToken ct = default)
    {
        var tool = await _repo.GetToolAsync(toolId, ct);
        if (tool is null) return false;
        tool.SetActive(active);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog(active ? "LtiToolEnabled" : "LtiToolDisabled", "LtiTool", tool.Id.ToString(), actorUserId: actorUserId), ct);
        return true;
    }

    public async Task<LtiResult<LtiToolResponse>> AddDeploymentAsync(Guid toolId, AddLtiDeploymentRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var tool = await _repo.GetToolAsync(toolId, ct);
        if (tool is null) return LtiResult<LtiToolResponse>.Fail(404, "Tool not found.");
        if (!Enum.TryParse<LtiDeploymentScope>(request.Scope, true, out var scope) || !Enum.IsDefined(scope))
            return LtiResult<LtiToolResponse>.Fail(400, "Scope must be Installation, Department or CourseOffering.");
        try
        {
            var deployment = tool.AddDeployment(scope, request.ScopeId);
            _repo.Add(deployment);
        }
        catch (ArgumentException ex) { return LtiResult<LtiToolResponse>.Fail(400, ex.Message); }
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("LtiDeploymentAdded", "LtiTool", tool.Id.ToString(), actorUserId: actorUserId, newValuesJson: $"{{\"scope\":\"{scope}\",\"scopeId\":\"{request.ScopeId}\"}}"), ct);
        return LtiResult<LtiToolResponse>.Success(ToResponse(tool));
    }

    public async Task<LtiResult<LtiToolResponse>> SetDeploymentActiveAsync(Guid toolId, Guid deploymentRowId, bool active, Guid actorUserId, CancellationToken ct = default)
    {
        var tool = await _repo.GetToolAsync(toolId, ct);
        var deployment = tool?.Deployments.FirstOrDefault(d => d.Id == deploymentRowId);
        if (tool is null || deployment is null) return LtiResult<LtiToolResponse>.Fail(404, "Deployment not found.");
        deployment.SetActive(active);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog(active ? "LtiDeploymentEnabled" : "LtiDeploymentDisabled", "LtiTool", tool.Id.ToString(), actorUserId: actorUserId), ct);
        return LtiResult<LtiToolResponse>.Success(ToResponse(tool));
    }

    public async Task RotateKeysAsync(Guid actorUserId, CancellationToken ct = default)
    {
        _keys.Rotate();
        await _audit.LogAsync(new AuditLog("LtiPlatformKeyRotated", "LtiPlatform", null, actorUserId: actorUserId), ct);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static void Apply(LtiTool tool, SaveLtiToolRequest r)
    {
        Enum.TryParse<LtiPrivacyLevel>(r.PrivacyLevel, true, out var privacy);
        tool.Configure(r.Name, r.Description, r.OidcInitiationUrl, r.TargetLinkUri, r.DeepLinkingUrl, r.RedirectUris, r.JwksUrl, r.PublicKeyPem,
            r.CustomParameters, privacy, r.AllowGradeService, r.AllowRosterService);
    }

    /// <summary>Tool URLs must be https (loopback http is allowed for local testing); keys must be present and readable.</summary>
    public static string? Validate(SaveLtiToolRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Name) || r.Name.Length > 200) return "Enter the tool's name (up to 200 characters).";
        if (!SafeUrl(r.OidcInitiationUrl)) return "The login initiation URL must be an https address.";
        if (!SafeUrl(r.TargetLinkUri)) return "The target link URI must be an https address.";
        if (!string.IsNullOrWhiteSpace(r.DeepLinkingUrl) && !SafeUrl(r.DeepLinkingUrl)) return "The deep linking URL must be an https address.";
        if (r.RedirectUris is null || r.RedirectUris.Count == 0 || r.RedirectUris.Any(u => !SafeUrl(u))) return "Add at least one https redirect URI.";
        if (string.IsNullOrWhiteSpace(r.JwksUrl) && string.IsNullOrWhiteSpace(r.PublicKeyPem)) return "Give the tool's JWKS URL or its public key.";
        if (!string.IsNullOrWhiteSpace(r.JwksUrl) && !SafeUrl(r.JwksUrl)) return "The JWKS URL must be an https address.";
        if (!string.IsNullOrWhiteSpace(r.PublicKeyPem))
        {
            try { using var rsa = RSA.Create(); rsa.ImportFromPem(r.PublicKeyPem); if (rsa.KeySize < 2048) return "The tool's RSA key must be at least 2048 bits."; }
            catch (Exception ex) when (ex is ArgumentException or CryptographicException) { return "The public key is not a valid PEM RSA key."; }
        }
        if (!Enum.TryParse<LtiPrivacyLevel>(r.PrivacyLevel, true, out _)) return "Privacy must be Anonymous, NameOnly, EmailOnly or Public.";
        if (r.CustomParameters is { Length: > 4000 }) return "Custom parameters are limited to 4000 characters.";
        return null;
    }

    public static bool SafeUrl(string? value)
        => Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var u) && string.IsNullOrEmpty(u.UserInfo)
            && (u.Scheme == Uri.UriSchemeHttps || (u.Scheme == Uri.UriSchemeHttp && u.IsLoopback));

    public static LtiToolResponse ToResponse(LtiTool t)
        => new(t.Id, t.Name, t.Description, t.ClientId, t.OidcInitiationUrl, t.TargetLinkUri, t.DeepLinkingUrl, t.RedirectUriList, t.JwksUrl,
            t.PublicKeyPem is not null, t.CustomParameters, t.PrivacyLevel.ToString(), t.AllowGradeService, t.AllowRosterService, t.IsActive,
            t.Deployments.OrderBy(d => d.Scope).ThenBy(d => d.CreatedAt)
                .Select(d => new LtiDeploymentResponse(d.Id, d.DeploymentId, d.Scope.ToString(), d.ScopeId, d.IsActive)).ToList());
}
