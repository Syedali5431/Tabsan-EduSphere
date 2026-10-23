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
