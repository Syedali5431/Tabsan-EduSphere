using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Notifications;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lti;

namespace Tabsan.EduSphere.Application.Lti;

/// <summary>
/// Plan M5.4 — connects AGS to the EduSphere gradebook. Every line item is backed by an assignment marked as completed in
/// the tool; a FullyGraded score becomes that student's graded submission (scaled to the line item's maximum), so posting
/// policy, the grading workspace and the "Assignment(s)" gradebook sync apply as for any other assignment.
/// </summary>
public sealed class LtiGradeBridge
{
    private readonly ILtiRepository _lti;
    private readonly IAssignmentRepository _assignments;
    private readonly ISubmissionWorkspaceRepository _submissions;
    private readonly IAssignmentGradebookSync? _gradebook;
    private readonly Assignments.IAssignmentPolicyService? _policies;

    public LtiGradeBridge(ILtiRepository lti, IAssignmentRepository assignments, ISubmissionWorkspaceRepository submissions, IAssignmentGradebookSync? gradebook = null,
        Assignments.IAssignmentPolicyService? policies = null)
    {
        _lti = lti;
        _assignments = assignments;
        _submissions = submissions;
        _gradebook = gradebook;
        _policies = policies;
    }

    public async Task<LtiLineItem> CreateLineItemAsync(Guid toolId, Guid offeringId, Guid? resourceLinkId, string label, decimal scoreMaximum,
        string? tag, string? resourceId, DateTime? start, DateTime? end, CancellationToken ct = default)
    {
        var title = string.IsNullOrWhiteSpace(label) ? "Tool activity" : label.Trim();
        var assignment = Assignment.ForExternalTool(offeringId, title[..Math.Min(title.Length, 300)], scoreMaximum, end ?? DateTime.UtcNow.AddYears(1), toolId);
        await _assignments.AddAsync(assignment, ct);
        var item = new LtiLineItem(toolId, offeringId, resourceLinkId, assignment.Id, title, scoreMaximum, tag, resourceId, start, end);
        _lti.Add(item);
        await _lti.SaveChangesAsync(ct);
        return item;
    }

    public async Task UpdateLineItemAsync(LtiLineItem item, string label, decimal scoreMaximum, string? tag, string? resourceId, DateTime? start, DateTime? end, CancellationToken ct = default)
    {
        item.Update(label, scoreMaximum, tag, resourceId, start, end);
        if (await _assignments.GetByIdAsync(item.AssignmentId, ct) is { ExternalToolId: not null } assignment)
        {
            assignment.SyncExternal(item.Label, item.ScoreMaximum);
            _assignments.Update(assignment);
        }
        await _lti.SaveChangesAsync(ct);
    }

    /// <summary>Removes the line item. Its assignment goes too, unless students already have grades there.</summary>
    public async Task DeleteLineItemAsync(LtiLineItem item, CancellationToken ct = default)
    {
        var graded = (await _submissions.GetSubmissionsAsync(item.AssignmentId, false, ct)).Any(s => s.MarksAwarded.HasValue);
        if (!graded && await _assignments.GetByIdAsync(item.AssignmentId, ct) is { } assignment)
        {
            assignment.SoftDelete();
            _assignments.Update(assignment);
        }
        foreach (var link in (await _lti.GetLinksForOfferingAsync(item.CourseOfferingId, ct)).Where(l => l.LineItemId == item.Id))
            link.DetachLineItem();
        _lti.Remove(item);
        await _lti.SaveChangesAsync(ct);
    }

    /// <summary>Records a tool's score. Returns false when an equal or newer score was already recorded (ignored, as AGS allows).</summary>
    public async Task<bool> RecordScoreAsync(LtiLineItem item, Guid studentProfileId, decimal? scoreGiven, decimal? scoreMaximum, string activityProgress,
        string gradingProgress, string? comment, DateTime timestamp, string toolName, CancellationToken ct = default)
    {
        var latest = (await _lti.GetScoresAsync(item.Id, ct)).Where(s => s.StudentProfileId == studentProfileId).MaxBy(s => s.Timestamp);
        if (latest is not null && timestamp <= latest.Timestamp) return false;
        _lti.Add(new LtiScore(item.Id, studentProfileId, scoreGiven, scoreMaximum, activityProgress, gradingProgress, comment, timestamp));

        var assignment = await _submissions.GetAssignmentAsync(item.AssignmentId, ct);
        if (assignment is null) { await _lti.SaveChangesAsync(ct); return true; }

        var submission = await _submissions.GetSubmissionAsync(item.AssignmentId, studentProfileId, false, ct);
        var started = activityProgress is "Submitted" or "Completed" || gradingProgress == "FullyGraded";
        if (submission is null && started)
        {
            submission = AssignmentSubmission.Start(item.AssignmentId, studentProfileId);
            _submissions.Add(submission);
            var attempt = submission.AddAttempt($"Completed in {toolName}.", null, timestamp, false);
            _submissions.Add(attempt);
        }

        var posted = false;
        if (submission is not null && gradingProgress == "FullyGraded" && scoreGiven is { } given)
        {
            var max = scoreMaximum is > 0 ? scoreMaximum.Value : item.ScoreMaximum;
            var marks = Math.Round(Math.Clamp(given / max * item.ScoreMaximum, 0, item.ScoreMaximum), 2, MidpointRounding.AwayFromZero);
            if (submission.Status != SubmissionStatus.Rejected)
            {
                // Plan M8: tool scores follow the course's late policy too.
                var penalty = _policies is null ? 0 : await _policies.LatePenaltyAsync(assignment, submission, marks, ct);
                submission.Grade(marks, comment, Guid.Empty, postNow: assignment.PostingPolicy == GradePostingPolicy.Automatic, latePenalty: penalty);
                posted = submission.PostedAt.HasValue;
            }
        }
        await _lti.SaveChangesAsync(ct);
        if (posted && _gradebook is not null) await _gradebook.RefreshAsync(item.CourseOfferingId, studentProfileId, ct);
        return true;
    }
}

/// <summary>Plan M5.4 — OAuth 2.0 client-credentials grant with a JWT client assertion (LTI Security Framework).</summary>
public sealed class LtiTokenService
{
    private const string AssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";

    private readonly ILtiRepository _repo;
    private readonly ILtiToolJwtValidator _validator;
    private readonly LtiOptions _options;

    public LtiTokenService(ILtiRepository repo, ILtiToolJwtValidator validator, IOptions<LtiOptions> options)
    {
        _repo = repo;
        _validator = validator;
        _options = options.Value;
    }

    /// <summary>On failure the value is null and the error is an OAuth error code plus a description ("code: text").</summary>
    public async Task<LtiResult<LtiTokenResponse>> IssueAsync(LtiTokenRequest r, CancellationToken ct = default)
    {
        static LtiResult<LtiTokenResponse> Fail(int status, string code, string text) => LtiResult<LtiTokenResponse>.Fail(status, $"{code}: {text}");
        if (!_options.IsConfigured) return Fail(503, "temporarily_unavailable", "LTI is not configured.");
        if (r.GrantType != "client_credentials") return Fail(400, "unsupported_grant_type", "Only client_credentials is supported.");
        if (r.ClientAssertionType != AssertionType || string.IsNullOrWhiteSpace(r.ClientAssertion)) return Fail(400, "invalid_request", "A JWT client assertion is required.");

        var issuer = LtiLaunchService.UnverifiedIssuer(r.ClientAssertion);
        var tool = issuer is null ? null : await _repo.GetToolByClientIdAsync(issuer, ct);
        if (tool is not { IsActive: true }) return Fail(401, "invalid_client", "Unknown client.");
        var (payload, error) = await _validator.ValidateAsync(r.ClientAssertion, tool.JwksUrl, tool.PublicKeyPem, tool.ClientId, [_options.TokenUrl, _options.Issuer], ct);
        if (payload is null) return Fail(401, "invalid_client", error ?? "The client assertion is not valid.");

        using (var doc = JsonDocument.Parse(payload))
        {
            var root = doc.RootElement;
            var sub = root.TryGetProperty("sub", out var s) ? s.GetString() : null;
            var jti = root.TryGetProperty("jti", out var j) ? j.GetString() : null;
            if (sub != tool.ClientId) return Fail(401, "invalid_client", "sub must be the client id.");
            if (string.IsNullOrWhiteSpace(jti) || !await _repo.TryUseNonceAsync(tool.Id, "jti:" + jti, DateTime.UtcNow.AddHours(2), ct))
                return Fail(401, "invalid_client", "The client assertion was already used.");
        }

        var allowed = new List<string>();
        if (tool.AllowGradeService) allowed.AddRange(LtiConstants.AgsScopes);
        if (tool.AllowRosterService) allowed.Add(LtiConstants.ScopeMemberships);
        var granted = (r.Scope ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().Where(allowed.Contains).ToList();
        if (granted.Count == 0) return Fail(400, "invalid_scope", "None of the requested scopes is allowed for this tool.");

        var token = Base64Url.Encode(RandomNumberGenerator.GetBytes(32));
        var lifetime = Math.Clamp(_options.AccessTokenMinutes, 5, 120);
        _repo.Add(new LtiAccessToken(tool.Id, Hash(token), granted, DateTime.UtcNow.AddMinutes(lifetime)));
        await _repo.SaveChangesAsync(ct);
        return LtiResult<LtiTokenResponse>.Success(new LtiTokenResponse(token, "Bearer", lifetime * 60, string.Join(" ", granted)));
    }

    /// <summary>Checks a bearer token for a service call (null when missing, unknown, expired, or the tool is off).</summary>
    public async Task<LtiServiceCaller?> AuthenticateAsync(string? authorizationHeader, CancellationToken ct = default)
    {
        if (authorizationHeader is null || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;
        var token = await _repo.GetAccessTokenAsync(Hash(authorizationHeader[7..].Trim()), ct);
        if (token is null || token.ExpiresAt < DateTime.UtcNow) return null;
        var tool = await _repo.GetToolAsync(token.ToolId, ct);
        return tool is { IsActive: true } ? new LtiServiceCaller(tool.Id, token.Scopes.Split(' ')) : null;
    }

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}

/// <summary>Plan M5.4/M5.5 — Assignment and Grade Services and Names and Role Provisioning Services.</summary>
public sealed class LtiAgsNrpsService
{
    public const string LineItemContainerType = "application/vnd.ims.lis.v2.lineitemcontainer+json";
    public const string LineItemType = "application/vnd.ims.lis.v2.lineitem+json";
    public const string ScoreType = "application/vnd.ims.lis.v1.score+json";
    public const string ResultContainerType = "application/vnd.ims.lis.v2.resultcontainer+json";
    public const string MembershipType = "application/vnd.ims.lti-nrps.v2.membershipcontainer+json";

    private static readonly string[] ActivityValues = ["Initialized", "Started", "InProgress", "Submitted", "Completed"];
    private static readonly string[] GradingValues = ["FullyGraded", "Pending", "PendingManual", "Failed", "NotReady"];

    private readonly ILtiRepository _repo;
    private readonly LtiGradeBridge _grades;
    private readonly ISubmissionWorkspaceRepository _submissions;
    private readonly LtiOptions _options;

    public LtiAgsNrpsService(ILtiRepository repo, LtiGradeBridge grades, ISubmissionWorkspaceRepository submissions, IOptions<LtiOptions> options)
    {
        _repo = repo;
        _grades = grades;
        _submissions = submissions;
        _options = options.Value;
    }

    private async Task<(LtiTool? Tool, string? Error, int Status)> CheckAsync(LtiServiceCaller caller, Guid offeringId, string[] anyOfScopes, CancellationToken ct)
    {
        if (!anyOfScopes.Any(caller.Scopes.Contains)) return (null, "The access token does not have the required scope.", 403);
        var tool = await _repo.GetToolAsync(caller.ToolId, ct);
        if (tool is null) return (null, "Unknown tool.", 401);
        if (await _repo.FindDeploymentForOfferingAsync(tool.Id, offeringId, ct) is null) return (null, "The tool is not deployed for this course.", 404);
        return (tool, null, 200);
    }

    private JsonObject LineItemJson(Guid offeringId, LtiLineItem i)
    {
        var o = new JsonObject
        {
            ["id"] = _options.LineItemUrl(offeringId, i.Id),
            ["scoreMaximum"] = i.ScoreMaximum,
            ["label"] = i.Label
        };
        if (i.ResourceLinkId is { } rl) o["resourceLinkId"] = rl.ToString();
        if (i.ResourceId is not null) o["resourceId"] = i.ResourceId;
        if (i.Tag is not null) o["tag"] = i.Tag;
        if (i.StartDateTime is { } s) o["startDateTime"] = s.ToString("o");
        if (i.EndDateTime is { } e) o["endDateTime"] = e.ToString("o");
        return o;
    }

    public async Task<LtiResult<JsonNode>> ListLineItemsAsync(LtiServiceCaller caller, Guid offeringId, string? resourceLinkId, string? resourceId, string? tag, int? limit,
        CancellationToken ct = default, int? page = null)
    {
        var (tool, error, status) = await CheckAsync(caller, offeringId, [LtiConstants.ScopeLineItem, LtiConstants.ScopeLineItemReadOnly], ct);
        if (tool is null) return LtiResult<JsonNode>.Fail(status, error!);
        var filtered = (await _repo.GetLineItemsAsync(tool.Id, offeringId, ct))
            .Where(i => resourceLinkId is null || i.ResourceLinkId?.ToString() == resourceLinkId)
            .Where(i => resourceId is null || i.ResourceId == resourceId)
            .Where(i => tag is null || i.Tag == tag);
        var (items, next) = Page(filtered, limit, page, 100, 500, _options.LineItemsUrl(offeringId),
            ("resource_link_id", resourceLinkId), ("resource_id", resourceId), ("tag", tag));
        return LtiResult<JsonNode>.Success(new JsonArray(items.Select(i => (JsonNode)LineItemJson(offeringId, i)).ToArray())) with { NextUrl = next };
    }

    /// <summary>
    /// One page of an already filtered container (LTI AGS/NRPS paging): <paramref name="limit"/> items from page <paramref name="page"/>
    /// (1-based), and the URL of the next page when more remain — the same filters, limit and the following page number.
    /// </summary>
    public static (List<T> Items, string? NextUrl) Page<T>(IEnumerable<T> all, int? limit, int? page, int defaultLimit, int maxLimit, string url,
        params (string Name, string? Value)[] filters)
    {
        var size = Math.Clamp(limit ?? defaultLimit, 1, maxLimit);
        var number = Math.Max(page ?? 1, 1);
        var slice = all.Skip((number - 1) * size).Take(size + 1).ToList();
        if (slice.Count <= size) return (slice, null);
        var query = filters.Where(f => !string.IsNullOrEmpty(f.Value)).Select(f => $"{f.Name}={Uri.EscapeDataString(f.Value!)}")
            .Append($"limit={size}").Append($"page={number + 1}");
        return (slice.Take(size).ToList(), url + "?" + string.Join("&", query));
    }

    public async Task<LtiResult<JsonNode>> GetLineItemAsync(LtiServiceCaller caller, Guid offeringId, Guid lineItemId, CancellationToken ct = default)
    {
        var (tool, error, status) = await CheckAsync(caller, offeringId, [LtiConstants.ScopeLineItem, LtiConstants.ScopeLineItemReadOnly], ct);
        if (tool is null) return LtiResult<JsonNode>.Fail(status, error!);
        var item = await _repo.GetLineItemAsync(lineItemId, ct);
        return item is null || item.ToolId != tool.Id || item.CourseOfferingId != offeringId
            ? LtiResult<JsonNode>.Fail(404, "Line item not found.")
            : LtiResult<JsonNode>.Success(LineItemJson(offeringId, item));
    }

    public async Task<LtiResult<JsonNode>> SaveLineItemAsync(LtiServiceCaller caller, Guid offeringId, Guid? lineItemId, string body, CancellationToken ct = default)
    {
        var (tool, error, status) = await CheckAsync(caller, offeringId, [LtiConstants.ScopeLineItem], ct);
        if (tool is null) return LtiResult<JsonNode>.Fail(status, error!);

        JsonElement root;
        try { using var doc = JsonDocument.Parse(body); root = doc.RootElement.Clone(); }
        catch (JsonException) { return LtiResult<JsonNode>.Fail(400, "The body is not valid JSON."); }
        if (!root.TryGetProperty("scoreMaximum", out var maxEl) || !maxEl.TryGetDecimal(out var max) || max <= 0)
            return LtiResult<JsonNode>.Fail(400, "scoreMaximum must be a positive number.");
        string? Str(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        DateTime? Date(string name) => DateTime.TryParse(Str(name), null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var d) ? d : null;
        var label = Str("label");
        if (string.IsNullOrWhiteSpace(label)) return LtiResult<JsonNode>.Fail(400, "label is required.");

        Guid? linkId = null;
        if (Str("resourceLinkId") is { } rl)
        {
            if (!Guid.TryParse(rl, out var parsed) || await _repo.GetLinkAsync(parsed, ct) is not { } link || link.ToolId != tool.Id || link.CourseOfferingId != offeringId)
                return LtiResult<JsonNode>.Fail(400, "resourceLinkId does not belong to this tool and course.");
            linkId = parsed;
        }

        if (lineItemId is null)
        {
            var created = await _grades.CreateLineItemAsync(tool.Id, offeringId, linkId, label, max, Str("tag"), Str("resourceId"), Date("startDateTime"), Date("endDateTime"), ct);
            return LtiResult<JsonNode>.Success(LineItemJson(offeringId, created), 201);
        }

        var item = await _repo.GetLineItemAsync(lineItemId.Value, ct);
        if (item is null || item.ToolId != tool.Id || item.CourseOfferingId != offeringId) return LtiResult<JsonNode>.Fail(404, "Line item not found.");
        await _grades.UpdateLineItemAsync(item, label, max, Str("tag"), Str("resourceId"), Date("startDateTime"), Date("endDateTime"), ct);
        return LtiResult<JsonNode>.Success(LineItemJson(offeringId, item));
    }

    public async Task<LtiResult<bool>> DeleteLineItemAsync(LtiServiceCaller caller, Guid offeringId, Guid lineItemId, CancellationToken ct = default)
    {
        var (tool, error, status) = await CheckAsync(caller, offeringId, [LtiConstants.ScopeLineItem], ct);
        if (tool is null) return LtiResult<bool>.Fail(status, error!);
        var item = await _repo.GetLineItemAsync(lineItemId, ct);
        if (item is null || item.ToolId != tool.Id || item.CourseOfferingId != offeringId) return LtiResult<bool>.Fail(404, "Line item not found.");
        await _grades.DeleteLineItemAsync(item, ct);
        return LtiResult<bool>.Success(true, 204);
    }

    public async Task<LtiResult<bool>> PostScoreAsync(LtiServiceCaller caller, Guid offeringId, Guid lineItemId, string body, CancellationToken ct = default)
    {
        var (tool, error, status) = await CheckAsync(caller, offeringId, [LtiConstants.ScopeScore], ct);
        if (tool is null) return LtiResult<bool>.Fail(status, error!);
        var item = await _repo.GetLineItemAsync(lineItemId, ct);
        if (item is null || item.ToolId != tool.Id || item.CourseOfferingId != offeringId) return LtiResult<bool>.Fail(404, "Line item not found.");

        JsonElement root;
        try { using var doc = JsonDocument.Parse(body); root = doc.RootElement.Clone(); }
        catch (JsonException) { return LtiResult<bool>.Fail(400, "The body is not valid JSON."); }
        string? Str(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        decimal? Num(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d) ? d : null;

        if (!Guid.TryParse(Str("userId"), out var userId)) return LtiResult<bool>.Fail(400, "userId is required.");
        var activity = Str("activityProgress");
        var grading = Str("gradingProgress");
        if (activity is null || !ActivityValues.Contains(activity)) return LtiResult<bool>.Fail(400, "activityProgress is missing or not valid.");
        if (grading is null || !GradingValues.Contains(grading)) return LtiResult<bool>.Fail(400, "gradingProgress is missing or not valid.");
        if (!DateTime.TryParse(Str("timestamp"), null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var timestamp))
            return LtiResult<bool>.Fail(400, "timestamp is required (ISO 8601).");
        var given = Num("scoreGiven");
        var max = Num("scoreMaximum");
        if (given is < 0) return LtiResult<bool>.Fail(400, "scoreGiven cannot be negative.");
        if (given is not null && max is not > 0) return LtiResult<bool>.Fail(400, "scoreMaximum is required with scoreGiven.");

        var person = await _repo.GetPersonAsync(userId, ct);
        if (person?.StudentProfileId is not { } studentId || !await _submissions.IsEnrolledAsync(offeringId, studentId, ct))
            return LtiResult<bool>.Fail(422, "userId is not a student enrolled in this course.");

        await _grades.RecordScoreAsync(item, studentId, given, max, activity, grading, Str("comment"), timestamp, tool.Name, ct);
        return LtiResult<bool>.Success(true, 204);
    }

    public async Task<LtiResult<JsonNode>> ResultsAsync(LtiServiceCaller caller, Guid offeringId, Guid lineItemId, string? userId, int? limit,
        CancellationToken ct = default, int? page = null)
    {
        var (tool, error, status) = await CheckAsync(caller, offeringId, [LtiConstants.ScopeResultReadOnly], ct);
        if (tool is null) return LtiResult<JsonNode>.Fail(status, error!);
        var item = await _repo.GetLineItemAsync(lineItemId, ct);
        if (item is null || item.ToolId != tool.Id || item.CourseOfferingId != offeringId) return LtiResult<JsonNode>.Fail(404, "Line item not found.");

        var members = (await _repo.GetOfferingMembersAsync(offeringId, ct)).Where(m => m.Person.StudentProfileId.HasValue)
            .ToDictionary(m => m.Person.StudentProfileId!.Value, m => m.Person.UserId);
        var graded = (await _submissions.GetSubmissionsAsync(item.AssignmentId, false, ct))
            .Where(s => s.MarksAwarded.HasValue && members.ContainsKey(s.StudentProfileId))
            .Where(s => userId is null || userId == members[s.StudentProfileId].ToString());
        var (submissions, next) = Page(graded, limit, page, 500, 1000, _options.LineItemUrl(offeringId, item.Id) + "/results", ("user_id", userId));
        var results = new JsonArray();
        foreach (var s in submissions)
        {
            var uid = members[s.StudentProfileId];
            var r = new JsonObject
            {
                ["id"] = $"{_options.LineItemUrl(offeringId, item.Id)}/results/{uid}",
                ["scoreOf"] = _options.LineItemUrl(offeringId, item.Id),
                ["userId"] = uid.ToString(),
                ["resultScore"] = s.MarksAwarded!.Value,
                ["resultMaximum"] = item.ScoreMaximum
            };
            if (s.Feedback is not null) r["comment"] = s.Feedback;
            results.Add(r);
        }
        return LtiResult<JsonNode>.Success(results) with { NextUrl = next };
    }

    public async Task<LtiResult<JsonNode>> MembershipsAsync(LtiServiceCaller caller, Guid offeringId, string? role, int? limit,
        CancellationToken ct = default, int? page = null)
    {
        var (tool, error, status) = await CheckAsync(caller, offeringId, [LtiConstants.ScopeMemberships], ct);
        if (tool is null) return LtiResult<JsonNode>.Fail(status, error!);
        var context = await _repo.GetOfferingContextAsync(offeringId, ct);
        if (context is null) return LtiResult<JsonNode>.Fail(404, "Course not found.");

        // The role filter applies before paging, so every page holds only matching members.
        var matching = (await _repo.GetOfferingMembersAsync(offeringId, ct))
            .Select(m => (m.Person, Roles: LtiConstants.RolesFor(m.Person.Role, m.IsInstructor)))
            .Where(m => role is null || m.Roles.Contains(role) || m.Roles.Any(r => r.EndsWith("#" + role, StringComparison.Ordinal)));
        var (page1, next) = Page(matching, limit, page, 1000, 5000, _options.MembershipsUrl(offeringId), ("role", role));
        var members = new JsonArray();
        foreach (var (person, roles) in page1)
        {
            var m = new JsonObject
            {
                ["status"] = person.IsActive ? "Active" : "Inactive",
                ["user_id"] = person.UserId.ToString(),
                ["roles"] = new JsonArray(roles.Select(r => (JsonNode)r).ToArray())
            };
            if (tool.PrivacyLevel is LtiPrivacyLevel.NameOnly or LtiPrivacyLevel.Public && !string.IsNullOrWhiteSpace(person.FullName))
            {
                var parts = person.FullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                m["name"] = person.FullName.Trim();
                m["given_name"] = parts[0];
                if (parts.Length > 1) m["family_name"] = parts[1];
            }
            if (tool.PrivacyLevel is LtiPrivacyLevel.EmailOnly or LtiPrivacyLevel.Public && !string.IsNullOrWhiteSpace(person.Email))
                m["email"] = person.Email;
            members.Add(m);
        }
        return LtiResult<JsonNode>.Success(new JsonObject
        {
            ["id"] = _options.MembershipsUrl(offeringId),
            ["context"] = new JsonObject { ["id"] = offeringId.ToString(), ["label"] = context.CourseCode, ["title"] = context.CourseTitle },
            ["members"] = members
        }) with { NextUrl = next };
    }
}
