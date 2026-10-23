using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Application.Lms.Scorm;

// Plan M13 — SCORM packages and the xAPI statement store: settings, requests/answers and the ports the services use.

/// <summary>Settings (section "Scorm").</summary>
public sealed class ScormOptions
{
    public const string SectionName = "Scorm";

    /// <summary>Where extracted packages are kept (relative to the API's content root, or absolute).</summary>
    public string StoragePath { get; set; } = "App_Data/scorm";

    /// <summary>Largest package upload (zip), in MB.</summary>
    public int MaxPackageMb { get; set; } = 500;

    /// <summary>Largest total size of a package's files after extraction, in MB (zip-bomb guard).</summary>
    public int MaxUncompressedMb { get; set; } = 2048;

    /// <summary>Most files in one package.</summary>
    public int MaxFiles { get; set; } = 20_000;

    /// <summary>How long a launch link works (a learner's session).</summary>
    public int PlayTokenHours { get; set; } = 12;
}

public sealed record ScormImportRequest(string? Title, bool Graded, decimal? MaxMarks, int MaxAttempts, bool Publish);

public sealed record ScormUpdateRequest(string Title, int MaxAttempts, bool IsPublished, bool Graded, decimal? MaxMarks);

public sealed record ScormPackageResponse(Guid Id, Guid OfferingId, string Title, string Version, string LaunchPath, int FileCount, long SizeBytes,
    decimal? MasteryScore, int MaxAttempts, bool IsPublished, bool Graded, decimal MaxMarks, Guid? AssignmentId, DateTime CreatedAt,
    int ScoCount, ScormLearnerSummary? Mine);

/// <summary>A learner's state on a package (their attempts so far).</summary>
public sealed record ScormLearnerSummary(int AttemptsUsed, string? Status, decimal? BestScorePercent, bool CanStartNew, DateTime? LastActivityAt);

public sealed record ScormLaunchRequest(bool NewAttempt);

public sealed record ScormLaunchResponse(string Token, Guid PackageId, string Title, string Version, string LaunchPath, int AttemptNumber, string Mode,
    int AttemptsUsed, int MaxAttempts);

/// <summary>What the player shell needs: the SCO to load and the CMI data to start from.</summary>
public sealed record ScormPlayResponse(Guid PackageId, string Title, string Version, string LaunchPath, string Mode, int AttemptNumber, string CmiJson);

public sealed record ScormCommitResponse(bool Result, int ErrorCode);

public sealed record ScormReportRow(Guid StudentProfileId, string Name, string? RegistrationNumber, int Attempts, string? Status, string? SuccessStatus,
    decimal? BestScorePercent, decimal? LastScorePercent, int TotalSeconds, DateTime? LastActivityAt);

public sealed record ScormReportResponse(ScormPackageResponse Package, IReadOnlyList<ScormReportRow> Learners);

/// <summary>Who a play link belongs to (sealed in the link; never trusted from the browser otherwise).</summary>
public sealed record ScormPlayClaims(Guid AttemptId, Guid PackageId, Guid StudentProfileId, bool Review);

public sealed class ScormResult<T>
{
    public bool Ok { get; private init; }
    public int Status { get; private init; }
    public string? Error { get; private init; }
    public T? Value { get; private init; }
    public static ScormResult<T> Success(T value, int status = 200) => new() { Ok = true, Status = status, Value = value };
    public static ScormResult<T> Fail(int status, string error) => new() { Status = status, Error = error };
}

/// <summary>Seals and opens play links (implemented with ASP.NET data protection in the API).</summary>
public interface IScormPlayTokens
{
    string Issue(ScormPlayClaims claims, TimeSpan lifetime);
    ScormPlayClaims? Read(string token);
}

/// <summary>Extracted package files by package and path.</summary>
public interface IScormContentStore
{
    Task SaveAsync(Guid packageId, string path, Stream content, CancellationToken ct = default);
    /// <summary>Null when the file does not exist.</summary>
    Task<Stream?> OpenAsync(Guid packageId, string path, CancellationToken ct = default);
    Task DeleteAsync(Guid packageId, CancellationToken ct = default);
}

public sealed record ScormLearnerInfo(Guid StudentProfileId, Guid UserId, string Name, string? RegistrationNumber);

public interface IScormRepository
{
    Task<ScormPackage?> GetPackageAsync(Guid packageId, CancellationToken ct = default);
    Task<IReadOnlyList<ScormPackage>> GetPackagesAsync(Guid offeringId, CancellationToken ct = default);
    Task<IReadOnlyList<ScormAttempt>> GetAttemptsAsync(Guid packageId, Guid? studentProfileId, CancellationToken ct = default);
    Task<IReadOnlyList<ScormAttempt>> GetAttemptsForStudentAsync(IReadOnlyCollection<Guid> packageIds, Guid studentProfileId, CancellationToken ct = default);
    Task<ScormAttempt?> GetAttemptAsync(Guid attemptId, CancellationToken ct = default);
    Task<bool> IsEnrolledAsync(Guid offeringId, Guid studentProfileId, CancellationToken ct = default);
    Task<ScormLearnerInfo?> GetLearnerAsync(Guid studentProfileId, CancellationToken ct = default);
    Task<Guid?> GetStudentProfileIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<ScormLearnerInfo>> GetEnrolledLearnersAsync(Guid offeringId, CancellationToken ct = default);
    /// <summary>Tenant and campus of an offering (for staff scope checks); null when it does not exist.</summary>
    Task<(Guid? TenantId, Guid? CampusId)?> GetOfferingScopeAsync(Guid offeringId, CancellationToken ct = default);

    Task<XapiCredential?> GetCredentialByKeyAsync(string key, CancellationToken ct = default);
    Task<XapiCredential?> GetCredentialAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<XapiCredential>> GetCredentialsAsync(CancellationToken ct = default);
    Task<XapiStatement?> GetStatementAsync(Guid statementId, CancellationToken ct = default);
    Task<IReadOnlyList<XapiStatement>> QueryStatementsAsync(XapiQuery query, CancellationToken ct = default);

    void Add(ScormPackage package);
    void Add(ScormAttempt attempt);
    void Add(XapiCredential credential);
    void Add(XapiStatement statement);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

// ── xAPI (Plan M13.2) ─────────────────────────────────────────────────────────

public sealed record XapiQuery(string? ActorKey, string? VerbId, string? ActivityId, Guid? Registration, DateTime? Since, DateTime? Until,
    int Limit, bool Ascending, Guid? CredentialId, bool IncludeVoided = false);

public sealed record XapiCredentialRequest(string Name, Guid? OfferingId);

public sealed record XapiCredentialResponse(Guid Id, string Name, string Key, Guid? OfferingId, bool IsActive, DateTime CreatedAt, DateTime? LastUsedAt);

/// <summary>Returned once, when the credentials are created.</summary>
public sealed record XapiCredentialCreated(XapiCredentialResponse Credential, string Secret);

public sealed record XapiStatementSummary(Guid Id, string Actor, string Verb, string Object, decimal? ScoreScaled, bool? Success, bool? Completion,
    DateTime Timestamp, DateTime Stored, bool Voided, string Credential);

public sealed class XapiResult<T>
{
    public bool Ok { get; private init; }
    public int Status { get; private init; }
    public string? Error { get; private init; }
    public T? Value { get; private init; }
    public static XapiResult<T> Success(T value, int status = 200) => new() { Ok = true, Status = status, Value = value };
    public static XapiResult<T> Fail(int status, string error) => new() { Status = status, Error = error };
}
