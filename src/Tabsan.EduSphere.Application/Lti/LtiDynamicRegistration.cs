using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lti;

namespace Tabsan.EduSphere.Application.Lti;

/// <summary>
/// Plan M5.1 — 1EdTech LTI Dynamic Registration 1.0. An administrator starts a registration with the tool's registration
/// URL; the platform opens it with its OpenID configuration and a single-use registration token (one hour). The tool posts
/// its client registration (RFC 7591 with the LTI tool configuration) to the registration endpoint, and the platform
/// creates the tool with an installation-wide deployment — switched off until an administrator has reviewed its privacy
/// and services — and answers with the client_id and deployment_id.
/// </summary>
public sealed class LtiDynamicRegistrationService
{
    public const string ToolConfigurationClaim = "https://purl.imsglobal.org/spec/lti-tool-configuration";
    public const string PlatformConfigurationClaim = "https://purl.imsglobal.org/spec/lti-platform-configuration";
    private const string TokenKeyPrefix = "lti:registration:";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);
    private static readonly string[] NameClaims = ["name", "given_name", "family_name"];

    private readonly ILtiRepository _repo;
    private readonly IAuditService _audit;
    private readonly IDistributedCache _cache;
    private readonly LtiOptions _options;

    public LtiDynamicRegistrationService(ILtiRepository repo, IAuditService audit, IDistributedCache cache, IOptions<LtiOptions> options)
    {
        _repo = repo;
        _audit = audit;
        _cache = cache;
        _options = options.Value;
    }

    /// <summary>Issues a registration token and builds the URL that opens the tool's registration page.</summary>
    public async Task<LtiResult<LtiRegistrationStart>> StartAsync(string? registrationUrl, Guid actorUserId, CancellationToken ct = default)
    {
        if (!_options.IsConfigured)
            return LtiResult<LtiRegistrationStart>.Fail(400, "Set the portal's public address (Lti:PlatformUrl) before registering tools.");
        if (!LtiAdminService.SafeUrl(registrationUrl) || registrationUrl!.Length > 1000)
            return LtiResult<LtiRegistrationStart>.Fail(400, "The registration URL must be an https address.");

        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        await _cache.SetStringAsync(TokenKeyPrefix + Hash(token), actorUserId.ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TokenLifetime }, ct);

        var url = registrationUrl.Trim();
        var launch = url + (url.Contains('?') ? "&" : "?")
                     + "openid_configuration=" + Uri.EscapeDataString(_options.OpenIdConfigurationUrl)
                     + "&registration_token=" + Uri.EscapeDataString(token);
        var origin = new Uri(url).GetLeftPart(UriPartial.Authority);
        await _audit.LogAsync(new AuditLog("LtiDynamicRegistrationStarted", "LtiTool", null, actorUserId: actorUserId,
            newValuesJson: JsonSerializer.Serialize(new { tool = origin })), ct);
        return LtiResult<LtiRegistrationStart>.Success(new LtiRegistrationStart(launch, origin));
    }

    /// <summary>The platform's OpenID configuration with the LTI platform configuration (what the tool reads first).</summary>
    public string OpenIdConfigurationJson()
    {
        var config = new JsonObject
        {
            ["issuer"] = _options.Issuer,
            ["authorization_endpoint"] = _options.AuthorizationUrl,
            ["registration_endpoint"] = _options.RegistrationUrl,
            ["jwks_uri"] = _options.JwksUrl,
            ["token_endpoint"] = _options.TokenUrl,
            ["token_endpoint_auth_methods_supported"] = new JsonArray("private_key_jwt"),
            ["token_endpoint_auth_signing_alg_values_supported"] = new JsonArray("RS256"),
            ["scopes_supported"] = new JsonArray(["openid", .. LtiConstants.AgsScopes.Select(s => (JsonNode?)s), LtiConstants.ScopeMemberships]),
            ["response_types_supported"] = new JsonArray("id_token"),
            ["id_token_signing_alg_values_supported"] = new JsonArray("RS256"),
            ["claims_supported"] = new JsonArray("iss", "sub", "aud", "name", "given_name", "family_name", "email"),
            ["subject_types_supported"] = new JsonArray("public"),
            [PlatformConfigurationClaim] = new JsonObject
            {
                ["product_family_code"] = "tabsan-edusphere",
                ["version"] = typeof(LtiDynamicRegistrationService).Assembly.GetName().Version?.ToString(3) ?? "1.0.0",
                ["messages_supported"] = new JsonArray(
                    new JsonObject { ["type"] = LtiConstants.ResourceLinkRequest },
                    new JsonObject { ["type"] = LtiConstants.DeepLinkingRequest })
            }
        };
        return config.ToJsonString();
    }

    /// <summary>
    /// The tool's client registration. Errors use RFC 7591 codes ("code: description"): invalid_token (401),
    /// invalid_client_metadata / invalid_redirect_uri (400). On success the registration JSON is returned with the
    /// issued client_id, the granted scopes and the deployment_id.
    /// </summary>
    public async Task<LtiResult<string>> RegisterAsync(string? authorization, string body, CancellationToken ct = default)
    {
        if (!_options.IsConfigured)
            return LtiResult<string>.Fail(400, "invalid_client_metadata: The platform address is not configured.");

        var token = authorization is not null && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..].Trim() : null;
        if (string.IsNullOrEmpty(token) || token.Length > 200)
            return LtiResult<string>.Fail(401, "invalid_token: A registration token is required.");
        var key = TokenKeyPrefix + Hash(token);
        var actor = await _cache.GetStringAsync(key, ct);
        if (actor is null || !Guid.TryParse(actor, out var actorUserId))
            return LtiResult<string>.Fail(401, "invalid_token: The registration token is unknown, used or expired.");
        await _cache.RemoveAsync(key, ct); // single use; a refused registration is started again from the platform

        JsonObject root;
        try { root = JsonNode.Parse(body) as JsonObject ?? throw new JsonException(); }
        catch (JsonException) { return Invalid("The registration must be a JSON object."); }

        var tool = root[ToolConfigurationClaim] as JsonObject;
        if (tool is null) return Invalid($"The registration has no {ToolConfigurationClaim} object.");
        if (Str(root["application_type"]) is { } type && type != "web") return Invalid("application_type must be \"web\".");
        if (!Strings(root["response_types"]).Contains("id_token")) return Invalid("response_types must include \"id_token\".");
        var grants = Strings(root["grant_types"]);
        if (!grants.Contains("implicit") || !grants.Contains("client_credentials"))
            return Invalid("grant_types must include \"implicit\" and \"client_credentials\".");
        if (Str(root["token_endpoint_auth_method"]) is { } auth && auth != "private_key_jwt")
            return Invalid("token_endpoint_auth_method must be \"private_key_jwt\".");
        var redirectUris = Strings(root["redirect_uris"]);
        if (redirectUris.Count == 0 || redirectUris.Any(u => !LtiAdminService.SafeUrl(u)))
            return LtiResult<string>.Fail(400, "invalid_redirect_uri: redirect_uris must be https addresses.");
        var jwksUri = Str(root["jwks_uri"]);
        if (!LtiAdminService.SafeUrl(jwksUri)) return Invalid("jwks_uri must be an https address.");
        if (string.IsNullOrWhiteSpace(Str(tool["domain"]))) return Invalid("The tool configuration needs its domain.");

        var targetLinkUri = Str(tool["target_link_uri"]);
        var deepLinking = (tool["messages"] as JsonArray ?? [])
            .OfType<JsonObject>()
            .FirstOrDefault(m => Str(m["type"]) == LtiConstants.DeepLinkingRequest);
        var claims = Strings(tool["claims"]);
        var scopes = (Str(root["scope"]) ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var granted = scopes.Where(s => LtiConstants.AgsScopes.Contains(s) || s == LtiConstants.ScopeMemberships).Distinct().ToList();
        var privacy = (claims.Contains("email"), claims.Any(c => NameClaims.Contains(c))) switch
        {
            (true, true) => LtiPrivacyLevel.Public,
            (true, false) => LtiPrivacyLevel.EmailOnly,
            (false, true) => LtiPrivacyLevel.NameOnly,
            _ => LtiPrivacyLevel.Anonymous
        };
        var custom = tool["custom_parameters"] is JsonObject parameters
            ? string.Join("\n", parameters.Select(p => $"{p.Key}={Str(p.Value)}"))
            : null;

        var request = new SaveLtiToolRequest(
            Name: (Str(root["client_name"]) ?? string.Empty).Trim(),
            Description: Str(tool["description"]) is { } d ? (d.Length > 1000 ? d[..1000] : d) : null,
            OidcInitiationUrl: Str(root["initiate_login_uri"]) ?? string.Empty,
            TargetLinkUri: targetLinkUri ?? string.Empty,
            DeepLinkingUrl: deepLinking is null ? null : Str(deepLinking["target_link_uri"]) ?? targetLinkUri,
            RedirectUris: redirectUris,
            JwksUrl: jwksUri,
            PublicKeyPem: null,
            CustomParameters: custom,
            PrivacyLevel: privacy.ToString(),
            AllowGradeService: granted.Any(s => LtiConstants.AgsScopes.Contains(s)),
            AllowRosterService: granted.Contains(LtiConstants.ScopeMemberships));
        if (LtiAdminService.Validate(request) is { } error) return Invalid(error);

        var registered = new LtiTool(request.Name, actorUserId);
        registered.Configure(request.Name, request.Description, request.OidcInitiationUrl, request.TargetLinkUri, request.DeepLinkingUrl,
            request.RedirectUris, request.JwksUrl, null, request.CustomParameters, privacy, request.AllowGradeService, request.AllowRosterService);
        registered.MarkDynamicallyRegistered();
        var deployment = registered.AddDeployment(LtiDeploymentScope.Installation, null);
        _repo.Add(registered);
        _repo.Add(deployment);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("LtiToolRegisteredDynamically", "LtiTool", registered.Id.ToString(), actorUserId: actorUserId,
            newValuesJson: JsonSerializer.Serialize(new { name = registered.Name, clientId = registered.ClientId, privacy = privacy.ToString(), scopes = granted })), ct);

        // The answer is the registration as received, with what the platform issued.
        var answer = (JsonObject)root.DeepClone();
        answer["client_id"] = registered.ClientId;
        answer["scope"] = string.Join(' ', granted);
        ((JsonObject)answer[ToolConfigurationClaim]!)["deployment_id"] = deployment.DeploymentId;
        return LtiResult<string>.Success(answer.ToJsonString());
    }

    private static LtiResult<string> Invalid(string message) => LtiResult<string>.Fail(400, "invalid_client_metadata: " + message);

    private static string? Str(JsonNode? node)
        => node is JsonValue value && value.TryGetValue<string>(out var s) ? s : null;

    private static List<string> Strings(JsonNode? node)
        => node is JsonArray array ? array.Select(Str).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()).ToList() : [];

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
