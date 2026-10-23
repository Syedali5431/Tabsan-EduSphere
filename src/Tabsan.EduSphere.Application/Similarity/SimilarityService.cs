using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Similarity;

namespace Tabsan.EduSphere.Application.Similarity;

/// <summary>
/// Plan M9 — similarity checking. A worker queues every new attempt of an assignment with checking turned on, reads its
/// text (typed text and readable files), fingerprints it, and either compares it with the institution's other submissions
/// (built-in provider) or sends it to Copyleaks, whose webhook completes the check. Staff see scores and reports; students
/// see their own score only when the assignment allows it.
/// </summary>
public sealed class SimilarityService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ISimilarityRepository _repo;
    private readonly ISubmissionTextExtractor _extractor;
    private readonly IMediaStorageService? _media;
    private readonly ICopyleaksClient? _copyleaks;
    private readonly IEncryptionService _encryption;
    private readonly IOutboundIntegrationGateway? _gateway;
    private readonly IAuditService _audit;
    private readonly SimilarityOptions _options;
    private readonly ILogger<SimilarityService> _logger;

    public SimilarityService(ISimilarityRepository repo, ISubmissionTextExtractor extractor, IEncryptionService encryption, IAuditService audit,
        IOptions<SimilarityOptions> options, IMediaStorageService? media = null, ICopyleaksClient? copyleaks = null,
        IOutboundIntegrationGateway? gateway = null, ILogger<SimilarityService>? logger = null)
    {
        _repo = repo;
        _extractor = extractor;
        _encryption = encryption;
        _audit = audit;
        _options = options.Value;
        _media = media;
        _copyleaks = copyleaks;
        _gateway = gateway;
        _logger = logger ?? NullLogger<SimilarityService>.Instance;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Settings (per tenant; the installation default applies when a tenant has none)
    // ═════════════════════════════════════════════════════════════════════════

    private async Task<SimilaritySetting> EffectiveSettingAsync(Guid? tenantId, CancellationToken ct)
        => (tenantId is not null ? await _repo.GetSettingAsync(tenantId, ct) : null) ?? await _repo.GetSettingAsync(null, ct) ?? new SimilaritySetting(null);

    private bool WebhookReady => Uri.TryCreate(_options.PublicApiBaseUrl, UriKind.Absolute, out var u) && u.Scheme == Uri.UriSchemeHttps;

    public async Task<SimilaritySettingsDto> GetSettingsAsync(Guid? tenantId, CancellationToken ct = default)
    {
        var own = await _repo.GetSettingAsync(tenantId, ct);
        var s = own ?? await EffectiveSettingAsync(tenantId, ct);
        return new SimilaritySettingsDto(tenantId, s.Provider.ToString(), s.IsEnabled, s.Scope.ToString(), s.AccountEmail, s.ApiKeyEncrypted is not null,
            s.Sandbox, s.IsComplete, WebhookReady, own is null);
    }

    public async Task<SimilarityResult<SimilaritySettingsDto>> SaveSettingsAsync(Guid? tenantId, SaveSimilaritySettingsRequest r, Guid actor, CancellationToken ct = default)
    {
        if (!Enum.TryParse<SimilarityProviderType>(r.Provider, true, out var provider) || !Enum.IsDefined(provider))
            return SimilarityResult<SimilaritySettingsDto>.Fail(400, "Choose the built-in comparison or Copyleaks.");
        if (!Enum.TryParse<SimilarityScope>(r.Scope, true, out var scope) || !Enum.IsDefined(scope))
            return SimilarityResult<SimilaritySettingsDto>.Fail(400, "Choose what the built-in comparison looks at.");
        var setting = await _repo.GetSettingAsync(tenantId, ct);
        var isNew = setting is null;
        setting ??= new SimilaritySetting(tenantId);
        try { setting.Configure(provider, r.IsEnabled, scope, r.AccountEmail, r.Sandbox); }
        catch (ArgumentException ex) { return SimilarityResult<SimilaritySettingsDto>.Fail(400, ex.Message); }
        if (!string.IsNullOrWhiteSpace(r.ApiKey)) setting.SetEncryptedApiKey(_encryption.Encrypt(r.ApiKey.Trim()));
        if (provider == SimilarityProviderType.Copyleaks && r.IsEnabled && !setting.IsComplete)
            return SimilarityResult<SimilaritySettingsDto>.Fail(400, "Enter the Copyleaks account email and API key.");
        if (isNew) _repo.Add(setting);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("SimilaritySettingsChanged", "SimilaritySetting", setting.Id.ToString(), actorUserId: actor,
            newValuesJson: JsonSerializer.Serialize(new { r.Provider, r.IsEnabled, r.Scope, r.AccountEmail, apiKeyChanged = !string.IsNullOrWhiteSpace(r.ApiKey), r.Sandbox })), ct);
        return SimilarityResult<SimilaritySettingsDto>.Success(await GetSettingsAsync(tenantId, ct));
    }

    /// <summary>Copyleaks: signs in with the saved credentials. Built-in: always ready.</summary>
    public async Task<SimilarityResult<string>> TestAsync(Guid? tenantId, CancellationToken ct = default)
    {
        var s = await EffectiveSettingAsync(tenantId, ct);
        if (s.Provider == SimilarityProviderType.Internal) return SimilarityResult<string>.Success("The built-in comparison is ready.");
        if (!s.IsComplete || _copyleaks is null) return SimilarityResult<string>.Fail(400, "Enter the Copyleaks account email and API key first.");
        try
        {
            await _copyleaks.LoginAsync(s.AccountEmail!, _encryption.Decrypt(s.ApiKeyEncrypted!), ct);
            return SimilarityResult<string>.Success(WebhookReady
                ? "Connected to Copyleaks."
                : "Connected to Copyleaks, but results cannot arrive until Similarity:PublicApiBaseUrl is set to the API's public https address.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return SimilarityResult<string>.Fail(502, $"Copyleaks refused the sign-in: {ex.Message}");
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Assignment settings
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<AssignmentSimilarityDto?> GetAssignmentSettingsAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var a = await _repo.GetAssignmentAsync(assignmentId, ct);
        if (a is null) return null;
        var s = await EffectiveSettingAsync(await _repo.GetOfferingTenantAsync(a.CourseOfferingId, ct), ct);
        return new AssignmentSimilarityDto(a.Id, a.SimilarityCheckEnabled, a.SimilarityVisibleToStudents, s.Provider.ToString(), s.IsEnabled && s.IsComplete);
    }

    public async Task<SimilarityResult<AssignmentSimilarityDto>> SaveAssignmentSettingsAsync(Guid assignmentId, SaveAssignmentSimilarityRequest r, Guid actor, CancellationToken ct = default)
    {
        var a = await _repo.GetAssignmentAsync(assignmentId, ct);
        if (a is null) return SimilarityResult<AssignmentSimilarityDto>.Fail(404, "Assignment not found.");
        try { a.ConfigureSimilarity(r.Enabled, r.VisibleToStudents); }
        catch (ArgumentException ex) { return SimilarityResult<AssignmentSimilarityDto>.Fail(400, ex.Message); }
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("SimilarityAssignmentSettings", "Assignment", a.Id.ToString(), actorUserId: actor,
            newValuesJson: JsonSerializer.Serialize(r)), ct);
        return SimilarityResult<AssignmentSimilarityDto>.Success((await GetAssignmentSettingsAsync(assignmentId, ct))!);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Queue
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Queues a check for every new attempt (group work: once per group hand-in).</summary>
    public async Task<int> EnqueueNewAsync(int take = 200, CancellationToken ct = default)
    {
        var candidates = await _repo.GetUncheckedSubmissionsAsync(take, ct);
        var settings = new Dictionary<Guid, SimilaritySetting>();
        var queued = 0;
        var groupsDone = new HashSet<(Guid, Guid, DateTime)>();
        foreach (var c in candidates.OrderBy(c => c.SubmittedAt).ThenBy(c => c.SubmissionId))
        {
            if (c.GroupId is { } g && !groupsDone.Add((c.AssignmentId, g, c.SubmittedAt))) continue;
            var key = c.TenantId ?? Guid.Empty;
            if (!settings.TryGetValue(key, out var s)) settings[key] = s = await EffectiveSettingAsync(c.TenantId, ct);
            if (!s.IsEnabled) continue;
            _repo.Add(new SimilarityCheck(c.SubmissionId, c.AssignmentId, c.CourseOfferingId, c.AttemptNumber, s.Provider, null));
            queued++;
        }
        if (queued > 0) await _repo.SaveChangesAsync(ct);
        return queued;
    }

    /// <summary>Staff: checks the current attempt again (e.g. after other students have handed in).</summary>
    public async Task<SimilarityResult<SimilarityScoreDto>> RecheckAsync(Guid submissionId, Guid actor, CancellationToken ct = default)
    {
        var c = await _repo.GetSubmissionAsync(submissionId, ct);
        if (c is null || c.AttemptNumber == 0) return SimilarityResult<SimilarityScoreDto>.Fail(404, "Nothing has been handed in yet.");
        var s = await EffectiveSettingAsync(c.TenantId, ct);
        if (!s.IsEnabled) return SimilarityResult<SimilarityScoreDto>.Fail(409, "Similarity checking is turned off for this institution.");
        var pending = (await _repo.GetChecksForSubmissionAsync(submissionId, ct)).FirstOrDefault(k => !k.IsFinished);
        if (pending is not null) return SimilarityResult<SimilarityScoreDto>.Success(ToScore(pending), 202);
        var check = new SimilarityCheck(c.SubmissionId, c.AssignmentId, c.CourseOfferingId, c.AttemptNumber, s.Provider, actor);
        _repo.Add(check);
        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("SimilarityRecheck", "AssignmentSubmission", submissionId.ToString(), actorUserId: actor), ct);
        return SimilarityResult<SimilarityScoreDto>.Success(ToScore(check), 202);
    }

    /// <summary>Works through queued checks that are due.</summary>
    public async Task<int> ProcessDueAsync(DateTime now, int take = 10, CancellationToken ct = default)
    {
        var processed = 0;
        foreach (var check in await _repo.GetDueChecksAsync(now, take, ct))
        {
            try { await ProcessAsync(check, now, ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Similarity check {CheckId} failed.", check.Id);
                check.RecordFailure(ex.Message, now);
            }
            await _repo.SaveChangesAsync(ct);
            processed++;
        }
        return processed;
    }

    public async Task<SimilarityRunResult> RunAsync(DateTime now, CancellationToken ct = default)
        => new(await EnqueueNewAsync(ct: ct), await ProcessDueAsync(now, ct: ct));

    private async Task ProcessAsync(SimilarityCheck check, DateTime now, CancellationToken ct)
    {
        var document = await DocumentAsync(check, ct);
        if (document is null) { check.Skip("No readable text was handed in (typed text, .txt, .docx, .pdf, .html or .rtf files are read).", now); return; }

        if (check.Provider == SimilarityProviderType.Internal)
        {
            var setting = await EffectiveSettingAsync(document.TenantId, ct);
            var (score, sources) = await CompareAsync(document, setting.Scope, ct);
            check.Complete(score, JsonSerializer.Serialize(sources, Json), null, now);
            return;
        }

        var s = await EffectiveSettingAsync(document.TenantId, ct);
        if (!s.IsComplete || _copyleaks is null) { check.Fail("Copyleaks is not set up (account email and API key).", now); return; }
        if (!WebhookReady) { check.Fail("Copyleaks results need Similarity:PublicApiBaseUrl (the API's public https address).", now); return; }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var scanId = check.Id.ToString("N");
        var webhook = $"{_options.PublicApiBaseUrl!.TrimEnd('/')}/api/v1/similarity/webhooks/copyleaks/{{STATUS}}?check={check.Id}&token={token}";
        var key = _encryption.Decrypt(s.ApiKeyEncrypted!);
        async Task Submit(CancellationToken innerCt)
        {
            var access = await _copyleaks.LoginAsync(s.AccountEmail!, key, innerCt);
            await _copyleaks.SubmitTextAsync(access, scanId, document.Text, webhook, s.Sandbox, innerCt);
        }
        if (_gateway is not null) await _gateway.ExecuteAsync("similarity", "copyleaks.submit", Submit, ct);
        else await Submit(ct);
        check.MarkSubmitted(scanId, Sha256(token), now);
    }

    /// <summary>The attempt's text, fingerprinted once and reused by later checks.</summary>
    private async Task<SimilarityDocument?> DocumentAsync(SimilarityCheck check, CancellationToken ct)
    {
        var existing = await _repo.GetDocumentAsync(check.SubmissionId, check.AttemptNumber, ct);
        if (existing is not null) return existing;
        var candidate = await _repo.GetSubmissionAsync(check.SubmissionId, ct);
        var content = await _repo.GetAttemptContentAsync(check.SubmissionId, check.AttemptNumber, ct);
        if (candidate is null || content is null) return null;

        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(content.Value.Text)) text.AppendLine(content.Value.Text);
        if (_media is not null)
            foreach (var f in content.Value.Files)
            {
                var bytes = await _media.ReadAsBytesAsync(f.StorageKey, ct);
                if (bytes is null) continue;
                var extracted = await _extractor.ExtractAsync(f.FileName, f.ContentType, bytes, ct);
                if (!string.IsNullOrWhiteSpace(extracted)) text.AppendLine(extracted);
            }
        var full = text.ToString();
        if (full.Length > SimilarityDocument.MaxTextLength) full = full[..SimilarityDocument.MaxTextLength];
        var words = Fingerprinter.Words(full);
        if (words.Count < Fingerprinter.ShingleSize) return null;

        var document = new SimilarityDocument(candidate.SubmissionId, candidate.AssignmentId, candidate.CourseOfferingId, candidate.TenantId,
            candidate.StudentProfileId, candidate.GroupId, check.AttemptNumber, full, words.Count);
        _repo.Add(document);
        _repo.AddRange(Fingerprinter.Fingerprints(words).Select(f => new SimilarityFingerprint(document.Id, f.Position, f.Hash)));
        await _repo.SaveChangesAsync(ct);
        return document;
    }

    /// <summary>Built-in comparison: the share of the work found in other submissions, with the strongest sources and passages.</summary>
    private async Task<(decimal Score, List<SimilaritySourceDto> Sources)> CompareAsync(SimilarityDocument document, SimilarityScope scope, CancellationToken ct)
    {
        var words = Fingerprinter.Words(document.Text);
        var own = await _repo.GetFingerprintsAsync(document.Id, ct);
        var positionsByHash = own.GroupBy(f => f.Hash).ToDictionary(g => g.Key, g => g.Select(f => f.Position).ToList());
        var matches = await _repo.FindMatchesAsync(positionsByHash.Keys.ToList(), scope, document, ct);
        if (matches.Count == 0) return (0, []);

        var pairsByDocument = matches.GroupBy(m => m.DocumentId).ToDictionary(g => g.Key,
            g => g.SelectMany(m => positionsByHash[m.Hash].Select(p => (Position: p, SourcePosition: m.Position))).ToList());
        var positions = own.Select(f => f.Position).Distinct().OrderBy(p => p).ToList();
        var score = SimilarityScorer.Score(words.Count, positions, pairsByDocument.Values.SelectMany(p => p.Select(x => x.Position)).ToHashSet());

        var docs = (await _repo.GetDocumentsAsync(pairsByDocument.Keys.ToList(), ct)).ToDictionary(d => d.Id);
        // One entry per other submission: its strongest attempt.
        var best = pairsByDocument
            .Where(kv => docs.ContainsKey(kv.Key))
            .Select(kv => (Doc: docs[kv.Key], Pairs: kv.Value, Percent: SimilarityScorer.Score(words.Count, positions, kv.Value.Select(p => p.Position).ToHashSet())))
            .GroupBy(x => x.Doc.SubmissionId).Select(g => g.OrderByDescending(x => x.Percent).First())
            .OrderByDescending(x => x.Percent).Take(10).ToList();
        var labels = (await _repo.GetSubmissionLabelsAsync(best.Select(b => b.Doc.SubmissionId).ToList(), ct)).ToDictionary(l => l.SubmissionId);

        var sources = new List<SimilaritySourceDto>();
        foreach (var (doc, pairs, percent) in best)
        {
            var sourceWords = Fingerprinter.Words(doc.Text);
            var passages = SimilarityScorer.Passages(words, sourceWords, pairs)
                .Select(p => new SimilarityPassageDto(Cut(document.Text, p.Start, p.Length), Cut(doc.Text, p.SourceStart, p.SourceLength))).ToList();
            var label = labels.TryGetValue(doc.SubmissionId, out var l)
                ? (l.NamesHidden ? $"Another submission — {l.AssignmentTitle} (anonymous)" : $"{l.StudentName} — {l.AssignmentTitle}")
                : "Another submission";
            if (doc.AttemptNumber > 1) label += $" (attempt {doc.AttemptNumber})";
            sources.Add(new SimilaritySourceDto("submission", label, null, percent, null, passages));
        }
        return (score, sources);
    }

    private static string Cut(string text, int start, int length)
    {
        if (start < 0 || start >= text.Length) return string.Empty;
        length = Math.Min(length, text.Length - start);
        var s = text.Substring(start, length);
        return s.Length > 600 ? s[..600] + "…" : s;
    }

    private static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    // ═════════════════════════════════════════════════════════════════════════
    // Provider webhook
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Copyleaks status webhook. 404 when the check or token is unknown (nothing is revealed to callers).</summary>
    public async Task<bool> HandleCopyleaksWebhookAsync(Guid checkId, string? token, string status, string body, DateTime now, CancellationToken ct = default)
    {
        var check = await _repo.GetCheckAsync(checkId, ct);
        if (check is null || check.WebhookTokenHash is null || string.IsNullOrEmpty(token)) return false;
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(check.WebhookTokenHash), Encoding.ASCII.GetBytes(Sha256(token)))) return false;
        if (check.IsFinished) return true;
        CopyleaksWebhookResult result;
        try { result = CopyleaksWebhook.Parse(status, body); }
        catch (JsonException) { return true; }
        if (result.IsError) check.Fail(result.Error ?? "Copyleaks reported an error.", now);
        else if (result.IsCompleted) check.Complete(result.Score ?? 0, JsonSerializer.Serialize(result.Sources, Json), null, now);
        else return true;
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Reading
    // ═════════════════════════════════════════════════════════════════════════

    private static SimilarityScoreDto ToScore(SimilarityCheck c) => new(c.SubmissionId, c.Id, c.Status.ToString(), c.Score, c.AttemptNumber);

    /// <summary>The latest check of every submission of an assignment (group mates share their group's check).</summary>
    public async Task<IReadOnlyList<SimilarityScoreDto>> AssignmentScoresAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var latest = (await _repo.GetChecksForAssignmentAsync(assignmentId, ct))
            .GroupBy(c => c.SubmissionId).ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.CreatedAt).First());
        var result = new List<SimilarityScoreDto>();
        var submissions = await _repo.GetAssignmentSubmissionsAsync(assignmentId, ct);
        foreach (var s in submissions)
        {
            if (latest.TryGetValue(s.SubmissionId, out var own)) { result.Add(ToScore(own) with { SubmissionId = s.SubmissionId }); continue; }
            if (s.GroupId is { } g)
            {
                var shared = submissions.Where(m => m.GroupId == g && latest.ContainsKey(m.SubmissionId)).Select(m => latest[m.SubmissionId])
                    .OrderByDescending(c => c.CreatedAt).FirstOrDefault();
                if (shared is not null) result.Add(ToScore(shared) with { SubmissionId = s.SubmissionId });
            }
        }
        return result;
    }

    public async Task<IReadOnlyList<SimilarityScoreDto>> SubmissionChecksAsync(Guid submissionId, CancellationToken ct = default)
        => (await _repo.GetChecksForSubmissionAsync(submissionId, ct)).OrderByDescending(c => c.CreatedAt).Select(ToScore).ToList();

    public async Task<SimilarityReportDto?> ReportAsync(Guid checkId, CancellationToken ct = default)
    {
        var c = await _repo.GetCheckAsync(checkId, ct);
        if (c is null) return null;
        var label = (await _repo.GetSubmissionLabelsAsync([c.SubmissionId], ct)).FirstOrDefault();
        var sources = string.IsNullOrWhiteSpace(c.SourcesJson) ? [] : JsonSerializer.Deserialize<List<SimilaritySourceDto>>(c.SourcesJson, Json) ?? [];
        return new SimilarityReportDto(c.Id, c.SubmissionId, c.AssignmentId, label?.AssignmentTitle ?? "Assignment",
            label is null ? "Student" : label.NamesHidden ? "Anonymous student" : label.StudentName, c.AttemptNumber,
            c.Provider == SimilarityProviderType.Internal ? "Built-in comparison" : "Copyleaks", c.Status.ToString(), c.Score, c.Error,
            c.CreatedAt, c.CompletedAt, c.ReportUrl, sources);
    }

    public async Task<Guid?> OfferingOfCheckAsync(Guid checkId, CancellationToken ct = default) => (await _repo.GetCheckAsync(checkId, ct))?.CourseOfferingId;

    /// <summary>A student's own latest score — only when the assignment shows scores to students.</summary>
    public async Task<StudentSimilarityDto?> StudentViewAsync(Guid submissionId, Guid studentProfileId, CancellationToken ct = default)
    {
        var s = await _repo.GetSubmissionAsync(submissionId, ct);
        if (s is null || s.StudentProfileId != studentProfileId) return null;
        var a = await _repo.GetAssignmentAsync(s.AssignmentId, ct);
        if (a is not { SimilarityVisibleToStudents: true }) return null;
        var score = (await AssignmentScoresAsync(s.AssignmentId, ct)).FirstOrDefault(x => x.SubmissionId == submissionId);
        return score is null ? null : new StudentSimilarityDto(submissionId, score.Status, score.Score);
    }
}
