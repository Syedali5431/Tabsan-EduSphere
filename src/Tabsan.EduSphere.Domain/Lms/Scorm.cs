using System.Text.Json;
using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Lms;

// Plan M13 — SCORM 1.2 / 2004 packages, learners' attempts (CMI data) and a minimal xAPI statement store.

public enum ScormVersion
{
    Scorm12 = 1,
    Scorm2004 = 2
}

/// <summary>An uploaded SCORM package of a course offering: its files are extracted to the SCORM content store under <see cref="BaseEntity.Id"/>.</summary>
public class ScormPackage : BaseEntity
{
    public const int MaxTitleLength = 300;

    public Guid OfferingId { get; private set; }
    public string Title { get; private set; } = default!;
    public ScormVersion Version { get; private set; }
    /// <summary>The manifest's identifier attribute.</summary>
    public string? Identifier { get; private set; }
    /// <summary>Path of the launched SCO inside the package (with its parameters), e.g. "story_html5.html" or "sco/index.html?mode=lms".</summary>
    public string LaunchPath { get; private set; } = default!;
    public int FileCount { get; private set; }
    /// <summary>SCOs in the default organization; the first one is launched.</summary>
    public int ScoCount { get; private set; } = 1;
    public long SizeBytes { get; private set; }
    /// <summary>Passing score in percent (SCORM 1.2 masteryscore / SCORM 2004 minNormalizedMeasure), when the package sets one.</summary>
    public decimal? MasteryScore { get; private set; }
    /// <summary>0 = unlimited.</summary>
    public int MaxAttempts { get; private set; }
    public bool IsPublished { get; private set; }
    /// <summary>Graded packages post the best score to <see cref="AssignmentId"/> (and from there to the gradebook).</summary>
    public Guid? AssignmentId { get; private set; }
    public decimal MaxMarks { get; private set; } = 100;
    public Guid CreatedByUserId { get; private set; }
    /// <summary>Removed packages keep their attempts (and grades) but are no longer shown or launched.</summary>
    public bool IsRemoved { get; private set; }
    /// <summary>The parts of a multi-part package (JSON list of <see cref="ScormPart"/>, in manifest order); null for one part.</summary>
    public string? PartsJson { get; private set; }
    /// <summary>SCORM 2004 sequencing (JSON <see cref="SeqTree"/>); null when the package has none and learners choose parts freely.</summary>
    public string? SequencingJson { get; private set; }
    /// <summary>How the package's structure was read: below <see cref="CurrentStructureVersion"/> it is read again from its manifest.</summary>
    public int StructureVersion { get; private set; }

    public const int MaxParts = 200;
    /// <summary>1 = parts (October 2026), 2 = parts with SCORM 2004 sequencing and SCORM 1.2 prerequisites.</summary>
    public const int CurrentStructureVersion = 2;

    private ScormPackage() { }

    public ScormPackage(Guid offeringId, string title, ScormVersion version, string? identifier, string launchPath, int fileCount, long sizeBytes,
        decimal? masteryScore, Guid createdByUserId, int scoCount = 1)
    {
        if (string.IsNullOrWhiteSpace(launchPath)) throw new ArgumentException("The package has no launch file.", nameof(launchPath));
        OfferingId = offeringId;
        Version = version;
        Identifier = identifier?.Trim() is { Length: > 0 } id ? id[..Math.Min(id.Length, 200)] : null;
        LaunchPath = launchPath.Trim();
        FileCount = fileCount;
        SizeBytes = sizeBytes;
        ScoCount = Math.Max(1, scoCount);
        MasteryScore = masteryScore is >= 0 and <= 100 ? Math.Round(masteryScore.Value, 2) : null;
        CreatedByUserId = createdByUserId;
        Rename(title);
    }

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Enter a title.", nameof(title));
        var t = title.Trim();
        Title = t.Length > MaxTitleLength ? t[..MaxTitleLength] : t;
        Touch();
    }

    public void SetMaxAttempts(int maxAttempts)
    {
        if (maxAttempts is < 0 or > 100) throw new ArgumentException("Allowed attempts must be between 0 (unlimited) and 100.", nameof(maxAttempts));
        MaxAttempts = maxAttempts;
        Touch();
    }

    public void SetPublished(bool published) { IsPublished = published; Touch(); }

    /// <summary>Links the gradebook assignment (graded packages).</summary>
    public void AttachAssignment(Guid assignmentId, decimal maxMarks)
    {
        if (maxMarks is <= 0 or > 1000) throw new ArgumentException("Maximum marks must be between 1 and 1000.", nameof(maxMarks));
        AssignmentId = assignmentId;
        MaxMarks = maxMarks;
        Touch();
    }

    public void SetMaxMarks(decimal maxMarks)
    {
        if (maxMarks is <= 0 or > 1000) throw new ArgumentException("Maximum marks must be between 1 and 1000.", nameof(maxMarks));
        MaxMarks = maxMarks;
        Touch();
    }

    public void Remove() { IsRemoved = true; IsPublished = false; Touch(); }

    public bool AllowsAnotherAttempt(int attemptsUsed) => MaxAttempts == 0 || attemptsUsed < MaxAttempts;

    /// <summary>Stores the package's parts (more than one: the learner moves between them in the player).</summary>
    public void SetParts(IReadOnlyList<ScormPart> parts)
    {
        if (parts.Count > MaxParts) throw new ArgumentException($"The package has more than {MaxParts} parts.", nameof(parts));
        PartsJson = parts.Count > 1 ? JsonSerializer.Serialize(parts) : null;
        Touch();
    }

    /// <summary>The parts in manifest order; a one-part package is its launch file.</summary>
    public IReadOnlyList<ScormPart> Parts => PartsJson is { Length: > 0 } json && JsonSerializer.Deserialize<List<ScormPart>>(json) is { Count: > 0 } list
        ? list
        : [new ScormPart(SinglePartId, Title, LaunchPath, true, MasteryScore)];

    public bool IsMultiPart => PartsJson is { Length: > 0 };

    /// <summary>Stores the package's SCORM 2004 sequencing (null: none) and marks the structure as read by the current rules.</summary>
    public void SetSequencing(SeqTree? tree)
    {
        SequencingJson = tree is null ? null : JsonSerializer.Serialize(tree);
        StructureVersion = CurrentStructureVersion;
        Touch();
    }

    /// <summary>The activity tree of a sequenced package (null when the package has no sequencing).</summary>
    public SeqTree? Sequencing => SequencingJson is { Length: > 0 } json ? JsonSerializer.Deserialize<SeqTree>(json) : null;

    public bool IsSequenced => SequencingJson is { Length: > 0 };

    /// <summary>Re-reads the structure of a package uploaded under older rules (its launch file and SCO count may change too).</summary>
    public void UpgradeStructure(string launchPath, int scoCount, IReadOnlyList<ScormPart> parts, SeqTree? tree)
    {
        if (!string.IsNullOrWhiteSpace(launchPath)) LaunchPath = launchPath.Trim();
        ScoCount = Math.Max(1, scoCount);
        SetParts(parts);
        SetSequencing(tree);
    }

    public const string SinglePartId = "package";
}

/// <summary>
/// One launchable part of a package (an item of the default organization): a SCO that talks to the runtime, or an asset
/// (a plain page) that counts as done once opened. SCORM 1.2 items may name prerequisites (adlcp:prerequisites, AICC script).
/// </summary>
public sealed record ScormPart(string Id, string Title, string LaunchPath, bool IsSco, decimal? MasteryScore, string? Prerequisites = null);

/// <summary>A learner's data for one part of a multi-part package (kept on the attempt).</summary>
public sealed class ScormPartState
{
    public string CompletionStatus { get; set; } = "not attempted";
    public string SuccessStatus { get; set; } = "unknown";
    public decimal? ScorePercent { get; set; }
    public int TotalSeconds { get; set; }
    public int CommitCount { get; set; }
    public string? Exit { get; set; }
    public string? CmiJson { get; set; }
    public DateTime? LastCommitAt { get; set; }

    public bool IsDone => CompletionStatus == "completed" || SuccessStatus is "passed" or "failed";
}

/// <summary>What a SCO reported in one commit, already read from the version's data model (see ScormCmi in the application layer).</summary>
public sealed record ScormCommitValues(
    string CompletionStatus,
    string SuccessStatus,
    decimal? ScoreRaw,
    decimal? ScoreMin,
    decimal? ScoreMax,
    decimal? ScoreScaled,
    string? Location,
    string? SuspendData,
    string? Exit,
    int SessionSeconds);

/// <summary>One learner's attempt at a package: the latest CMI data (to resume) and the values the LMS reports on.</summary>
public class ScormAttempt : BaseEntity
{
    public const int MaxSuspendDataLength = 64_000;
    public const int MaxCmiJsonLength = 2_000_000;

    public Guid PackageId { get; private set; }
    public Guid StudentProfileId { get; private set; }
    public int Number { get; private set; }
    /// <summary>completed, incomplete, not attempted, unknown (SCORM 1.2 statuses are mapped).</summary>
    public string CompletionStatus { get; private set; } = "not attempted";
    /// <summary>passed, failed, unknown.</summary>
    public string SuccessStatus { get; private set; } = "unknown";
    public decimal? ScoreRaw { get; private set; }
    public decimal? ScoreMin { get; private set; }
    public decimal? ScoreMax { get; private set; }
    public decimal? ScoreScaled { get; private set; }
    /// <summary>The score as a percentage (scaled score, else raw within min–max, else raw out of 100).</summary>
    public decimal? ScorePercent { get; private set; }
    public string? Location { get; private set; }
    public string? SuspendData { get; private set; }
    public string? Exit { get; private set; }
    /// <summary>The CMI object of the last commit (JSON), loaded again when the learner resumes.</summary>
    public string? CmiJson { get; private set; }
    public int TotalSeconds { get; private set; }
    public int CommitCount { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? LastCommitAt { get; private set; }
    /// <summary>First time the attempt was completed, passed or failed.</summary>
    public DateTime? FinishedAt { get; private set; }
    /// <summary>Multi-part packages: each part's data (JSON object part id → <see cref="ScormPartState"/>); the attempt's own values are the roll-up.</summary>
    public string? PartsStateJson { get; private set; }

    private ScormAttempt() { }

    public ScormAttempt(Guid packageId, Guid studentProfileId, int number, DateTime now)
    {
        PackageId = packageId;
        StudentProfileId = studentProfileId;
        Number = number;
        StartedAt = now;
    }

    public bool IsFinished => FinishedAt is not null;

    /// <summary>Stores a commit. Session time is added to the total only on the final commit of a session.</summary>
    public void Apply(ScormCommitValues v, string? cmiJson, bool finalCommit, DateTime now)
    {
        CompletionStatus = Clip(v.CompletionStatus, 20) ?? "unknown";
        SuccessStatus = Clip(v.SuccessStatus, 20) ?? "unknown";
        ScoreRaw = v.ScoreRaw;
        ScoreMin = v.ScoreMin;
        ScoreMax = v.ScoreMax;
        ScoreScaled = v.ScoreScaled is { } s ? Math.Clamp(s, -1, 1) : null;
        ScorePercent = Percent(v);
        Location = Clip(v.Location, 1000);
        SuspendData = Clip(v.SuspendData, MaxSuspendDataLength);
        Exit = Clip(v.Exit, 20);
        if (cmiJson is not null) CmiJson = cmiJson.Length <= MaxCmiJsonLength ? cmiJson : null;
        if (finalCommit && v.SessionSeconds > 0) TotalSeconds = (int)Math.Min(int.MaxValue, (long)TotalSeconds + v.SessionSeconds);
        CommitCount++;
        LastCommitAt = now;
        if (FinishedAt is null && (CompletionStatus == "completed" || SuccessStatus is "passed" or "failed")) FinishedAt = now;
        Touch();
    }

    /// <summary>Each part's data (multi-part packages).</summary>
    public Dictionary<string, ScormPartState> PartStates()
        => PartsStateJson is { Length: > 0 } json && JsonSerializer.Deserialize<Dictionary<string, ScormPartState>>(json) is { } states
            ? states : new Dictionary<string, ScormPartState>();

    /// <summary>
    /// Stores a commit of one part of a multi-part package and rolls the parts up: completed when every part is done (failed
    /// when a part failed, passed when every SCO passed), the score the average of the parts' scores, the time their sum.
    /// </summary>
    public void ApplyPart(IReadOnlyList<ScormPart> parts, string partId, ScormCommitValues v, string? cmiJson, bool finalCommit, DateTime now)
        => RollUp(parts, StorePartData(parts, partId, v, cmiJson, finalCommit, now), now);

    /// <summary>Sequenced packages: stores one part's commit; the sequencing rules work out the attempt's result (<see cref="SetSequencedResult"/>).</summary>
    public void StorePart(IReadOnlyList<ScormPart> parts, string partId, ScormCommitValues v, string? cmiJson, bool finalCommit, DateTime now)
    {
        PartsStateJson = JsonSerializer.Serialize(StorePartData(parts, partId, v, cmiJson, finalCommit, now));
        Touch();
    }

    private Dictionary<string, ScormPartState> StorePartData(IReadOnlyList<ScormPart> parts, string partId, ScormCommitValues v, string? cmiJson, bool finalCommit, DateTime now)
    {
        var states = PartStates();
        var state = states.GetValueOrDefault(partId) ?? new ScormPartState();
        state.CompletionStatus = Clip(v.CompletionStatus, 20) ?? "unknown";
        state.SuccessStatus = Clip(v.SuccessStatus, 20) ?? "unknown";
        state.ScorePercent = Percent(v) ?? state.ScorePercent;
        state.Exit = Clip(v.Exit, 20);
        // The parts share the attempt's room for saved data (each part keeps at least twice the largest suspend data).
        var cmiLimit = Math.Max(MaxSuspendDataLength * 2, MaxCmiJsonLength / Math.Max(1, parts.Count));
        if (cmiJson is not null) state.CmiJson = cmiJson.Length <= cmiLimit ? cmiJson : null;
        if (finalCommit && v.SessionSeconds > 0) state.TotalSeconds = (int)Math.Min(int.MaxValue, (long)state.TotalSeconds + v.SessionSeconds);
        state.CommitCount++;
        state.LastCommitAt = now;
        states[partId] = state;
        Location = Clip(v.Location, 1000);
        Exit = state.Exit;
        return states;
    }

    /// <summary>A new attempt on a part (sequencing delivered it again): it starts from fresh data; its time so far is kept.</summary>
    public void ResetPart(string partId)
    {
        var states = PartStates();
        states[partId] = new ScormPartState { TotalSeconds = states.GetValueOrDefault(partId)?.TotalSeconds ?? 0 };
        PartsStateJson = JsonSerializer.Serialize(states);
        Touch();
    }

    /// <summary>
    /// A package that became multi-part (read again under newer rules): the data of an attempt made when only its first part
    /// launched moves to that part, so the learner resumes where they were.
    /// </summary>
    public void AdoptSinglePartData(string partId)
    {
        if (PartsStateJson is { Length: > 0 } || CommitCount == 0) return;
        PartsStateJson = JsonSerializer.Serialize(new Dictionary<string, ScormPartState>
        {
            [partId] = new()
            {
                CompletionStatus = CompletionStatus, SuccessStatus = SuccessStatus, ScorePercent = ScorePercent, TotalSeconds = TotalSeconds,
                CommitCount = CommitCount, Exit = Exit, CmiJson = CmiJson, LastCommitAt = LastCommitAt
            }
        });
        Touch();
    }

    /// <summary>SCORM 2004 sequencing state of this attempt (JSON <see cref="SeqState"/>).</summary>
    public string? SequencingStateJson { get; private set; }

    public SeqState SequencingState()
        => SequencingStateJson is { Length: > 0 } json && JsonSerializer.Deserialize<SeqState>(json) is { } state ? state : new SeqState();

    public void SetSequencingState(SeqState state)
    {
        SequencingStateJson = JsonSerializer.Serialize(state);
        Touch();
    }

    /// <summary>
    /// Sequenced packages: the attempt's result is the package's (root activity's) rolled-up status. It is finished once the
    /// package is completed without being failed, or when the learner has left it for good (exit all, or the end of the content).
    /// </summary>
    public void SetSequencedResult(string completion, string success, decimal? scorePercent, bool ended, DateTime now)
    {
        CompletionStatus = Clip(completion, 20) ?? "unknown";
        SuccessStatus = Clip(success, 20) ?? "unknown";
        ScorePercent = scorePercent is { } p ? Math.Round(Math.Clamp(p, 0, 100), 2) : null;
        ScoreRaw = ScorePercent;
        ScoreMin = ScorePercent is null ? null : 0;
        ScoreMax = ScorePercent is null ? null : 100;
        ScoreScaled = ScorePercent / 100;
        TotalSeconds = (int)Math.Min(int.MaxValue, PartStates().Values.Sum(s => (long)s.TotalSeconds));
        CommitCount++;
        LastCommitAt = now;
        var done = (CompletionStatus == "completed" && SuccessStatus != "failed") || (ended && (CompletionStatus == "completed" || SuccessStatus is "passed" or "failed"));
        if (FinishedAt is null && done) FinishedAt = now;
        Touch();
    }

    /// <summary>An asset part (a plain page without the runtime) is done once the learner opens it.</summary>
    public void MarkPartViewed(IReadOnlyList<ScormPart> parts, string partId, DateTime now)
    {
        var states = PartStates();
        if (states.TryGetValue(partId, out var existing) && existing.IsDone) return;
        states[partId] = new ScormPartState { CompletionStatus = "completed", CommitCount = 1, LastCommitAt = now };
        RollUp(parts, states, now);
    }

    private void RollUp(IReadOnlyList<ScormPart> parts, Dictionary<string, ScormPartState> states, DateTime now)
    {
        PartsStateJson = JsonSerializer.Serialize(states);
        var ofParts = parts.Select(p => (Part: p, State: states.GetValueOrDefault(p.Id))).ToList();
        var allDone = ofParts.All(x => x.State?.IsDone == true);
        CompletionStatus = allDone ? "completed" : ofParts.Any(x => x.State is not null) ? "incomplete" : "not attempted";
        // A package of pages only (no SCOs) is completed by opening them; it has nothing to pass.
        var scoParts = ofParts.Where(x => x.Part.IsSco).ToList();
        SuccessStatus = !allDone ? "unknown"
            : ofParts.Any(x => x.State!.SuccessStatus == "failed") ? "failed"
            : scoParts.Count > 0 && scoParts.All(x => x.State!.SuccessStatus == "passed") ? "passed"
            : "unknown";
        var scores = ofParts.Where(x => x.State?.ScorePercent is not null).Select(x => x.State!.ScorePercent!.Value).ToList();
        ScorePercent = scores.Count > 0 ? Math.Round(scores.Average(), 2) : null;
        ScoreRaw = ScorePercent;
        ScoreMin = ScorePercent is null ? null : 0;
        ScoreMax = ScorePercent is null ? null : 100;
        ScoreScaled = ScorePercent / 100;
        TotalSeconds = (int)Math.Min(int.MaxValue, ofParts.Sum(x => (long)(x.State?.TotalSeconds ?? 0)));
        CommitCount++;
        LastCommitAt = now;
        if (FinishedAt is null && allDone) FinishedAt = now;
        Touch();
    }

    public static decimal? Percent(ScormCommitValues v)
    {
        decimal? p = v.ScoreScaled is { } scaled ? scaled * 100
            : v.ScoreRaw is { } raw && v.ScoreMax is { } max && max > (v.ScoreMin ?? 0) ? (raw - (v.ScoreMin ?? 0)) / (max - (v.ScoreMin ?? 0)) * 100
            : v.ScoreRaw;
        return p is { } value ? Math.Round(Math.Clamp(value, 0, 100), 2) : null;
    }

    private static string? Clip(string? value, int max) => value is null ? null : value.Length > max ? value[..max] : value;
}

/// <summary>Plan M13.2 — credentials an xAPI activity provider uses (HTTP Basic: key and secret) to send statements.</summary>
public class XapiCredential : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Key { get; private set; } = default!;
    /// <summary>SHA-256 of the secret (hex); the secret itself is shown once.</summary>
    public string SecretHash { get; private set; } = default!;
    /// <summary>Statements from these credentials count for this course (optional).</summary>
    public Guid? OfferingId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? LastUsedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private XapiCredential() { }

    public XapiCredential(string name, string key, string secretHash, Guid? offeringId, Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Enter a name.", nameof(name));
        Name = name.Trim().Length > 200 ? name.Trim()[..200] : name.Trim();
        Key = key;
        SecretHash = secretHash;
        OfferingId = offeringId;
        CreatedByUserId = createdByUserId;
    }

    public void Used(DateTime now) => LastUsedAt = now;

    public void Deactivate() { IsActive = false; Touch(); }
}

/// <summary>Plan M13.2 — one stored xAPI statement with the fields used for queries.</summary>
public class XapiStatement : BaseEntity
{
    public const int MaxJsonLength = 65_536;

    public Guid StatementId { get; private set; }
    public Guid CredentialId { get; private set; }
    /// <summary>The actor as an inverse-functional identifier: "mailto:…", "sha1:…", "openid:…" or "account:{homePage}|{name}".</summary>
    public string ActorKey { get; private set; } = default!;
    public string VerbId { get; private set; } = default!;
    public string ObjectId { get; private set; } = default!;
    public Guid? Registration { get; private set; }
    public decimal? ScoreScaled { get; private set; }
    public bool? Success { get; private set; }
    public bool? Completion { get; private set; }
    public DateTime Timestamp { get; private set; }
    public DateTime Stored { get; private set; }
    public bool IsVoided { get; private set; }
    public string Json { get; private set; } = default!;

    private XapiStatement() { }

    public XapiStatement(Guid statementId, Guid credentialId, string actorKey, string verbId, string objectId, Guid? registration,
        decimal? scoreScaled, bool? success, bool? completion, DateTime timestamp, DateTime stored, string json)
    {
        StatementId = statementId;
        CredentialId = credentialId;
        ActorKey = actorKey;
        VerbId = verbId;
        ObjectId = objectId;
        Registration = registration;
        ScoreScaled = scoreScaled;
        Success = success;
        Completion = completion;
        Timestamp = timestamp;
        Stored = stored;
        Json = json;
    }

    public void Void() { IsVoided = true; Touch(); }
}
