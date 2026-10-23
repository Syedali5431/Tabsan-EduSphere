using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Similarity;

namespace Tabsan.EduSphere.Domain.Interfaces;

// Plan M9 — similarity checking.

/// <summary>A handed-in submission (its current attempt) that can be checked.</summary>
public sealed record SimilarityCandidate(Guid SubmissionId, Guid AssignmentId, Guid CourseOfferingId, Guid? TenantId, Guid StudentProfileId,
    Guid? GroupId, int AttemptNumber, DateTime SubmittedAt);

/// <summary>A file of an attempt that may hold text.</summary>
public sealed record SimilarityAttemptFile(string FileName, string ContentType, string StorageKey);

/// <summary>A fingerprint of another document that matches the checked work.</summary>
public sealed record SimilarityMatch(Guid DocumentId, int Position, long Hash);

/// <summary>How a submission is shown in a report.</summary>
public sealed record SimilaritySubmissionLabel(Guid SubmissionId, string StudentName, string AssignmentTitle, bool NamesHidden);

public interface ISimilarityRepository
{
    Task<SimilaritySetting?> GetSettingAsync(Guid? tenantId, CancellationToken ct = default);
    void Add(SimilaritySetting setting);

    Task<Assignment?> GetAssignmentAsync(Guid assignmentId, CancellationToken ct = default);
    Task<Guid?> GetOfferingTenantAsync(Guid offeringId, CancellationToken ct = default);
    Task<(Guid? TenantId, Guid? CampusId)?> GetOfferingScopeAsync(Guid offeringId, CancellationToken ct = default);

    /// <summary>Handed-in submissions of assignments with checking turned on whose current attempt has no check yet.</summary>
    Task<IReadOnlyList<SimilarityCandidate>> GetUncheckedSubmissionsAsync(int take, CancellationToken ct = default);
    Task<SimilarityCandidate?> GetSubmissionAsync(Guid submissionId, CancellationToken ct = default);
    Task<IReadOnlyList<SimilarityCandidate>> GetAssignmentSubmissionsAsync(Guid assignmentId, CancellationToken ct = default);
    Task<(string? Text, IReadOnlyList<SimilarityAttemptFile> Files)?> GetAttemptContentAsync(Guid submissionId, int attemptNumber, CancellationToken ct = default);
    Task<IReadOnlyList<SimilaritySubmissionLabel>> GetSubmissionLabelsAsync(IReadOnlyCollection<Guid> submissionIds, CancellationToken ct = default);

    Task<IReadOnlyList<SimilarityCheck>> GetDueChecksAsync(DateTime now, int take, CancellationToken ct = default);
    Task<SimilarityCheck?> GetCheckAsync(Guid checkId, CancellationToken ct = default);
    Task<IReadOnlyList<SimilarityCheck>> GetChecksForSubmissionAsync(Guid submissionId, CancellationToken ct = default);
    Task<IReadOnlyList<SimilarityCheck>> GetChecksForAssignmentAsync(Guid assignmentId, CancellationToken ct = default);

    Task<SimilarityDocument?> GetDocumentAsync(Guid submissionId, int attemptNumber, CancellationToken ct = default);
    Task<IReadOnlyList<SimilarityDocument>> GetDocumentsAsync(IReadOnlyCollection<Guid> documentIds, CancellationToken ct = default);
    Task<IReadOnlyList<SimilarityFingerprint>> GetFingerprintsAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>Fingerprints of other documents in the scope that share any of <paramref name="hashes"/> (the checked student's and group's own work excluded).</summary>
    Task<IReadOnlyList<SimilarityMatch>> FindMatchesAsync(IReadOnlyCollection<long> hashes, SimilarityScope scope, SimilarityDocument document, CancellationToken ct = default);

    void Add(SimilarityCheck check);
    void Add(SimilarityDocument document);
    void AddRange(IEnumerable<SimilarityFingerprint> fingerprints);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
