using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Similarity;

// Plan M9 — similarity (plagiarism) checking of assignment submissions.

public enum SimilarityProviderType
{
    /// <summary>Built-in comparison with other submissions in the institution (no external service).</summary>
    Internal = 0,
    /// <summary>Copyleaks API (internet, publications and the Copyleaks database).</summary>
    Copyleaks = 1
}

/// <summary>Which earlier submissions the built-in comparison looks at.</summary>
public enum SimilarityScope
{
    Assignment = 0,
    Course = 1,
    Institution = 2
}

public enum SimilarityCheckStatus
{
    Queued = 0,
    /// <summary>Sent to an external provider; waiting for its webhook.</summary>
    Submitted = 1,
    Completed = 2,
    Failed = 3,
    /// <summary>Nothing to check (no readable text).</summary>
    Skipped = 4
}

/// <summary>Plan M9.1: the similarity provider of one tenant (TenantId null = the installation default).</summary>
public class SimilaritySetting : BaseEntity
{
    public Guid? TenantId { get; private set; }
    public SimilarityProviderType Provider { get; private set; }
    public bool IsEnabled { get; private set; } = true;
    public SimilarityScope Scope { get; private set; } = SimilarityScope.Course;
    /// <summary>Copyleaks account email.</summary>
    public string? AccountEmail { get; private set; }
    /// <summary>Copyleaks API key, encrypted at rest (ciphertext only).</summary>
    public string? ApiKeyEncrypted { get; private set; }
    /// <summary>Copyleaks sandbox mode (no credits used, fake results).</summary>
    public bool Sandbox { get; private set; }

    private SimilaritySetting() { }

    public SimilaritySetting(Guid? tenantId) => TenantId = tenantId;

    public void Configure(SimilarityProviderType provider, bool enabled, SimilarityScope scope, string? accountEmail, bool sandbox)
    {
        if (!Enum.IsDefined(provider)) throw new ArgumentException("Unknown provider.", nameof(provider));
        if (!Enum.IsDefined(scope)) throw new ArgumentException("Unknown comparison scope.", nameof(scope));
        var email = string.IsNullOrWhiteSpace(accountEmail) ? null : accountEmail.Trim();
        if (email is { Length: > 256 } || (email is not null && !email.Contains('@'))) throw new ArgumentException("Enter the Copyleaks account email.", nameof(accountEmail));
        Provider = provider;
        IsEnabled = enabled;
        Scope = scope;
        AccountEmail = email;
        Sandbox = sandbox;
        Touch();
    }

    public void SetEncryptedApiKey(string? cipher)
    {
        ApiKeyEncrypted = string.IsNullOrWhiteSpace(cipher) ? null : cipher;
        Touch();
    }

    /// <summary>Copyleaks needs an account email and an API key.</summary>
    public bool IsComplete => Provider == SimilarityProviderType.Internal || (AccountEmail is not null && ApiKeyEncrypted is not null);
}

/// <summary>Plan M9.2: one check of one attempt of a submission (the queue item and the stored report).</summary>
public class SimilarityCheck : BaseEntity
{
    public const int MaxTries = 5;

    public Guid SubmissionId { get; private set; }
    public Guid AssignmentId { get; private set; }
    public Guid CourseOfferingId { get; private set; }
    public int AttemptNumber { get; private set; }
    public SimilarityProviderType Provider { get; private set; }
    public SimilarityCheckStatus Status { get; private set; }
    /// <summary>Percentage of the work found elsewhere (0–100).</summary>
    public decimal? Score { get; private set; }
    /// <summary>Matching sources as JSON (other submissions with passages, or internet sources).</summary>
    public string? SourcesJson { get; private set; }
    public string? ExternalId { get; private set; }
    /// <summary>SHA-256 (hex) of the one-time token the provider's webhook must present.</summary>
    public string? WebhookTokenHash { get; private set; }
    public string? ReportUrl { get; private set; }
    public string? Error { get; private set; }
    public int Tries { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? RequestedByUserId { get; private set; }

    private SimilarityCheck() { }

    public SimilarityCheck(Guid submissionId, Guid assignmentId, Guid courseOfferingId, int attemptNumber, SimilarityProviderType provider, Guid? requestedBy)
    {
        SubmissionId = submissionId;
        AssignmentId = assignmentId;
        CourseOfferingId = courseOfferingId;
        AttemptNumber = attemptNumber;
        Provider = provider;
        RequestedByUserId = requestedBy;
        Status = SimilarityCheckStatus.Queued;
    }

    public bool IsFinished => Status is SimilarityCheckStatus.Completed or SimilarityCheckStatus.Failed or SimilarityCheckStatus.Skipped;

    public void MarkSubmitted(string externalId, string webhookTokenHash, DateTime now)
    {
        ExternalId = externalId;
        WebhookTokenHash = webhookTokenHash;
        Status = SimilarityCheckStatus.Submitted;
        SubmittedAt = now;
        Error = null;
        Touch();
    }

    public void Complete(decimal score, string? sourcesJson, string? reportUrl, DateTime now)
    {
        Score = Math.Clamp(Math.Round(score, 1, MidpointRounding.AwayFromZero), 0, 100);
        SourcesJson = sourcesJson;
        ReportUrl = reportUrl;
        Status = SimilarityCheckStatus.Completed;
        CompletedAt = now;
        Error = null;
        NextAttemptAt = null;
        Touch();
    }

    public void Skip(string reason, DateTime now)
    {
        Status = SimilarityCheckStatus.Skipped;
        Error = reason;
        CompletedAt = now;
        NextAttemptAt = null;
        Touch();
    }

    /// <summary>A failed try: retried with back-off until <see cref="MaxTries"/>, then failed for good.</summary>
    public void RecordFailure(string error, DateTime now)
    {
        Tries++;
        Error = error.Length > 1000 ? error[..1000] : error;
        if (Tries >= MaxTries)
        {
            Status = SimilarityCheckStatus.Failed;
            CompletedAt = now;
            NextAttemptAt = null;
        }
        else
        {
            Status = SimilarityCheckStatus.Queued;
            NextAttemptAt = now.AddMinutes(Math.Pow(2, Tries));
        }
        Touch();
    }

    /// <summary>The provider reported an error in its webhook (no retry).</summary>
    public void Fail(string error, DateTime now)
    {
        Error = error.Length > 1000 ? error[..1000] : error;
        Status = SimilarityCheckStatus.Failed;
        CompletedAt = now;
        NextAttemptAt = null;
        Touch();
    }
}

/// <summary>Plan M9: the extracted text of one submission attempt, kept for the built-in comparison and passage display.</summary>
public class SimilarityDocument : BaseEntity
{
    public const int MaxTextLength = 200_000;

    public Guid SubmissionId { get; private set; }
    public Guid AssignmentId { get; private set; }
    public Guid CourseOfferingId { get; private set; }
    public Guid? TenantId { get; private set; }
    public Guid StudentProfileId { get; private set; }
    public Guid? GroupId { get; private set; }
    public int AttemptNumber { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public int WordCount { get; private set; }

    private SimilarityDocument() { }

    public SimilarityDocument(Guid submissionId, Guid assignmentId, Guid courseOfferingId, Guid? tenantId, Guid studentProfileId, Guid? groupId,
        int attemptNumber, string text, int wordCount)
    {
        SubmissionId = submissionId;
        AssignmentId = assignmentId;
        CourseOfferingId = courseOfferingId;
        TenantId = tenantId;
        StudentProfileId = studentProfileId;
        GroupId = groupId;
        AttemptNumber = attemptNumber;
        Text = text.Length > MaxTextLength ? text[..MaxTextLength] : text;
        WordCount = wordCount;
    }
}

/// <summary>Plan M9: one winnowed fingerprint (hash of a run of words) of a document, with the word position it starts at.</summary>
public class SimilarityFingerprint
{
    public Guid DocumentId { get; private set; }
    public int Position { get; private set; }
    public long Hash { get; private set; }

    private SimilarityFingerprint() { }

    public SimilarityFingerprint(Guid documentId, int position, long hash)
    {
        DocumentId = documentId;
        Position = position;
        Hash = hash;
    }
}
