using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Application.Lms.Scorm;

/// <summary>
/// Plan M13.2 — a minimal xAPI 1.0.3 Learning Record Store: activity providers send statements with HTTP Basic credentials
/// created by an administrator; statements are validated (actor with an inverse-functional identifier, verb and object
/// IRIs), stored with "stored", "authority" and "version", voided by "voided" statements, and queried with the common
/// filters. Documents/state APIs are not included.
/// </summary>
public sealed class XapiService
{
    public const string Version = "1.0.3";
    public const string VoidedVerb = "http://adlnet.gov/expapi/verbs/voided";
    private const int MaxBatch = 100;

    private readonly IScormRepository _repo;
    private readonly IAuditService _audit;
    private readonly Func<DateTime> _now;

    public XapiService(IScormRepository repo, IAuditService audit) : this(repo, audit, () => DateTime.UtcNow) { }

    public XapiService(IScormRepository repo, IAuditService audit, Func<DateTime> now)
    {
        _repo = repo;
        _audit = audit;
        _now = now;
    }

    // ── credentials ──

    public async Task<XapiResult<XapiCredentialCreated>> CreateCredentialAsync(XapiCredentialRequest r, Guid actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(r.Name)) return XapiResult<XapiCredentialCreated>.Fail(400, "Enter a name for the credentials.");
        var key = "xapi_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(10)).ToLowerInvariant();
        var secret = Base64Url(RandomNumberGenerator.GetBytes(32));
        var credential = new XapiCredential(r.Name, key, Hash(secret), r.OfferingId, actorUserId);
        _repo.Add(credential);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("XapiCredentialCreated", "XapiCredential", credential.Id.ToString(), actorUserId: actorUserId), ct);
        return XapiResult<XapiCredentialCreated>.Success(new XapiCredentialCreated(ToResponse(credential), secret), 201);
    }

    public async Task<IReadOnlyList<XapiCredentialResponse>> CredentialsAsync(CancellationToken ct = default)
        => (await _repo.GetCredentialsAsync(ct)).OrderByDescending(c => c.CreatedAt).Select(ToResponse).ToList();

    public async Task<bool> DeactivateAsync(Guid id, Guid actorUserId, CancellationToken ct = default)
    {
        var credential = await _repo.GetCredentialAsync(id, ct);
        if (credential is null) return false;
        credential.Deactivate();
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("XapiCredentialDeactivated", "XapiCredential", id.ToString(), actorUserId: actorUserId), ct);
        return true;
    }

    /// <summary>The active credentials named by an "Authorization: Basic key:secret" header, or null.</summary>
    public async Task<XapiCredential?> AuthenticateAsync(string? authorization, CancellationToken ct = default)
    {
        if (authorization is null || !authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) return null;
        string decoded;
        try { decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authorization[6..].Trim())); }
        catch (FormatException) { return null; }
        var colon = decoded.IndexOf(':');
        if (colon <= 0) return null;
        var credential = await _repo.GetCredentialByKeyAsync(decoded[..colon], ct);
        if (credential is not { IsActive: true }) return null;
        var expected = Convert.FromHexString(credential.SecretHash);
        var given = SHA256.HashData(Encoding.UTF8.GetBytes(decoded[(colon + 1)..]));
        if (!CryptographicOperations.FixedTimeEquals(expected, given)) return null;
        if (credential.LastUsedAt is null || credential.LastUsedAt < _now().AddMinutes(-5))
        {
            credential.Used(_now());
            await _repo.SaveChangesAsync(ct);
        }
        return credential;
    }

    // ── statements ──

    /// <summary>Stores one statement or an array of them (POST), or the statement with <paramref name="statementId"/> (PUT). Returns the ids.</summary>
    public async Task<XapiResult<IReadOnlyList<Guid>>> StoreAsync(XapiCredential credential, string body, Guid? statementId, string authorityHome,
        CancellationToken ct = default)
    {
        JsonNode? node;
        try { node = JsonNode.Parse(body); }
        catch (JsonException) { return XapiResult<IReadOnlyList<Guid>>.Fail(400, "The body is not valid JSON."); }

        var items = node switch
        {
            JsonArray array when statementId is null => array.ToList(),
            JsonObject o => [o],
            _ => null
        };
        if (items is null || items.Count == 0) return XapiResult<IReadOnlyList<Guid>>.Fail(400, "Send a statement object (or, with POST, an array of statements).");
        if (items.Count > MaxBatch) return XapiResult<IReadOnlyList<Guid>>.Fail(413, $"Send at most {MaxBatch} statements at a time.");

        var now = _now();
        var prepared = new List<(XapiStatement Entity, Guid? Voids)>();
        var ids = new List<Guid>();
        foreach (var item in items)
        {
            if (item is not JsonObject statement) return XapiResult<IReadOnlyList<Guid>>.Fail(400, "Every statement must be an object.");
            var result = Prepare(statement, credential, statementId, now, authorityHome);
            if (result.Error is not null) return XapiResult<IReadOnlyList<Guid>>.Fail(400, result.Error);
            var (entity, voids) = result.Value!.Value;
            if (ids.Contains(entity.StatementId)) return XapiResult<IReadOnlyList<Guid>>.Fail(400, "The batch contains the same statement id twice.");

            if (await _repo.GetStatementAsync(entity.StatementId, ct) is { } existing)
            {
                // The same statement again is fine; a different one with the same id is a conflict.
                if (!SameStatement(existing.Json, entity.Json)) return XapiResult<IReadOnlyList<Guid>>.Fail(409, $"Statement {entity.StatementId} already exists with different content.");
                ids.Add(entity.StatementId);
                continue;
            }
            ids.Add(entity.StatementId);
            prepared.Add((entity, voids));
        }

        foreach (var (entity, voids) in prepared)
        {
            if (voids is { } target)
            {
                var voided = await _repo.GetStatementAsync(target, ct);
                if (voided is not null && voided.VerbId != VoidedVerb) voided.Void();
            }
            _repo.Add(entity);
        }
        await _repo.SaveChangesAsync(ct);
        return XapiResult<IReadOnlyList<Guid>>.Success(ids);
    }

    public async Task<XapiStatement?> GetAsync(Guid id, bool voided, CancellationToken ct = default)
    {
        var statement = await _repo.GetStatementAsync(id, ct);
        return statement is not null && statement.IsVoided == voided ? statement : null;
    }

    /// <summary>Statements matching the filters, newest first unless ascending; <c>more</c> continues the list (empty when complete).</summary>
    public async Task<(IReadOnlyList<XapiStatement> Statements, DateTime? Continue)> QueryAsync(XapiQuery query, CancellationToken ct = default)
    {
        var limit = query.Limit <= 0 ? 100 : Math.Min(query.Limit, 500);
        var rows = await _repo.QueryStatementsAsync(query with { Limit = limit + 1 }, ct);
        var page = rows.Take(limit).ToList();
        DateTime? next = rows.Count > limit ? page[^1].Stored : null;
        return (page, next);
    }

    public async Task<IReadOnlyList<XapiStatementSummary>> RecentAsync(int take, CancellationToken ct = default)
    {
        var credentials = (await _repo.GetCredentialsAsync(ct)).ToDictionary(c => c.Id, c => c.Name);
        var rows = await _repo.QueryStatementsAsync(new XapiQuery(null, null, null, null, null, null, Math.Clamp(take, 1, 200), false, null, IncludeVoided: true), ct);
        return rows.Select(s => new XapiStatementSummary(s.StatementId, s.ActorKey, s.VerbId, s.ObjectId, s.ScoreScaled, s.Success, s.Completion, s.Timestamp,
            s.Stored, s.IsVoided, credentials.GetValueOrDefault(s.CredentialId, "?"))).ToList();
    }

    /// <summary>The actor key used for storage and the "agent" filter; null when the agent has no inverse-functional identifier.</summary>
    public static string? ActorKey(JsonNode? actor)
    {
        if (actor is not JsonObject a) return null;
        var found = new List<string>();
        if (Str(a["mbox"]) is { } mbox)
        {
            if (!mbox.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) || mbox.Length < 10) return null;
            found.Add("mailto:" + mbox[7..].Trim().ToLowerInvariant());
        }
        if (Str(a["mbox_sha1sum"]) is { } sha1)
        {
            if (sha1.Length != 40 || !sha1.All(Uri.IsHexDigit)) return null;
            found.Add("sha1:" + sha1.ToLowerInvariant());
        }
        if (Str(a["openid"]) is { } openid)
        {
            if (!Uri.TryCreate(openid, UriKind.Absolute, out _)) return null;
            found.Add("openid:" + openid);
        }
        if (a["account"] is JsonObject account)
        {
            var home = Str(account["homePage"]);
            var name = Str(account["name"]);
            if (!Uri.TryCreate(home, UriKind.Absolute, out _) || string.IsNullOrWhiteSpace(name)) return null;
            found.Add($"account:{home}|{name}");
        }
        if (found.Count != 1) return null;
        return found[0].Length > 300 ? null : found[0];
    }

    // ── pieces ──

    private static ((XapiStatement Entity, Guid? Voids)? Value, string? Error) Prepare(JsonObject s, XapiCredential credential, Guid? pathId, DateTime now,
        string authorityHome)
    {
        static ((XapiStatement, Guid?)?, string?) Bad(string message) => (null, message);

        Guid id;
        if (s["id"] is { } idNode)
        {
            if (idNode.GetValueKind() != JsonValueKind.String || !Guid.TryParse(idNode.GetValue<string>(), out id)) return Bad("\"id\" must be a UUID.");
            if (pathId is { } p && p != id) return Bad("The statementId parameter and the statement's id differ.");
        }
        else id = pathId ?? Guid.NewGuid();

        var actorKey = ActorKey(s["actor"]);
        if (actorKey is null) return Bad("\"actor\" needs exactly one of mbox, mbox_sha1sum, openid or account.");
        var verb = (s["verb"] as JsonObject)?["id"];
        if (verb?.GetValueKind() != JsonValueKind.String || !IsIri(verb.GetValue<string>())) return Bad("\"verb.id\" must be an IRI.");
        var verbId = verb.GetValue<string>();
        if (s["object"] is not JsonObject obj) return Bad("\"object\" is required.");
        var objectType = Str(obj["objectType"]) ?? "Activity";
        string objectId;
        Guid? voids = null;
        switch (objectType)
        {
            case "Activity":
                if (obj["id"]?.GetValueKind() != JsonValueKind.String || !IsIri(obj["id"]!.GetValue<string>())) return Bad("\"object.id\" must be an IRI.");
                objectId = obj["id"]!.GetValue<string>();
                break;
            case "StatementRef":
                if (obj["id"]?.GetValueKind() != JsonValueKind.String || !Guid.TryParse(obj["id"]!.GetValue<string>(), out var refId)) return Bad("A StatementRef needs a UUID \"id\".");
                objectId = "statement:" + refId;
                if (verbId == VoidedVerb) voids = refId;
                break;
            case "Agent" or "Group":
                objectId = ActorKey(obj) is { } agent ? "agent:" + agent : null!;
                if (objectId is null) return Bad("An Agent object needs exactly one identifier.");
                break;
            case "SubStatement":
                objectId = "substatement";
                break;
            default:
                return Bad("Unknown object type.");
        }
        if (verbId == VoidedVerb && voids is null) return Bad("A voiding statement must point to a StatementRef.");
        if (objectId.Length > 1000) return Bad("\"object.id\" is too long.");

        var timestamp = now;
        if (s["timestamp"] is { } ts)
        {
            if (ts.GetValueKind() != JsonValueKind.String || !DateTimeOffset.TryParse(ts.GetValue<string>(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
                return Bad("\"timestamp\" must be an ISO 8601 date and time.");
            timestamp = parsed.UtcDateTime;
        }

        decimal? scaled = null;
        bool? success = null, completion = null;
        if (s["result"] is JsonObject result)
        {
            if ((result["score"] as JsonObject)?["scaled"] is JsonValue sv)
            {
                if (!sv.TryGetValue<decimal>(out var v) || v is < -1 or > 1) return Bad("\"result.score.scaled\" must be between -1 and 1.");
                scaled = v;
            }
            if (result["success"] is JsonValue su && su.TryGetValue<bool>(out var sb)) success = sb;
            if (result["completion"] is JsonValue co && co.TryGetValue<bool>(out var cb)) completion = cb;
        }
        Guid? registration = null;
        if ((s["context"] as JsonObject)?["registration"] is { } reg)
        {
            if (reg.GetValueKind() != JsonValueKind.String || !Guid.TryParse(reg.GetValue<string>(), out var r)) return Bad("\"context.registration\" must be a UUID.");
            registration = r;
        }

        s["id"] = id.ToString();
        s["timestamp"] ??= timestamp.ToString("o", CultureInfo.InvariantCulture);
        s["stored"] = now.ToString("o", CultureInfo.InvariantCulture);
        s["version"] ??= "1.0.0";
        s["authority"] = new JsonObject
        {
            ["objectType"] = "Agent",
            ["name"] = credential.Name,
            ["account"] = new JsonObject { ["homePage"] = authorityHome, ["name"] = credential.Key }
        };
        var json = s.ToJsonString();
        if (json.Length > XapiStatement.MaxJsonLength) return Bad("The statement is too large (64 KB at most).");
        return ((new XapiStatement(id, credential.Id, actorKey, verbId, objectId, registration, scaled, success, completion, timestamp, now, json), voids), null);
    }

    private static bool SameStatement(string storedJson, string newJson)
    {
        static JsonObject? Comparable(string json)
        {
            if (JsonNode.Parse(json) is not JsonObject o) return null;
            o.Remove("stored");
            o.Remove("authority");
            o.Remove("version");
            o.Remove("timestamp");
            return o;
        }
        return JsonNode.DeepEquals(Comparable(storedJson), Comparable(newJson));
    }

    private static string? Str(JsonNode? node) => node is JsonValue v && v.GetValueKind() == JsonValueKind.String ? v.GetValue<string>() : null;

    private static bool IsIri(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var u) && u.Scheme.Length > 1 && value!.Length <= 1000;

    private static string Hash(string secret) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static XapiCredentialResponse ToResponse(XapiCredential c) => new(c.Id, c.Name, c.Key, c.OfferingId, c.IsActive, c.CreatedAt, c.LastUsedAt);
}
