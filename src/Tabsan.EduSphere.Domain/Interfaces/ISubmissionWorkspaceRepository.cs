using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>
/// Plan M3 data access for submission history, files, comments, annotations, provisional grades
/// and the conversion queue. Kept apart from <see cref="IAssignmentRepository"/> so its existing
/// implementations are unaffected.
/// </summary>
public interface ISubmissionWorkspaceRepository
{
    Task<Assignment?> GetAssignmentAsync(Guid assignmentId, CancellationToken ct = default);

    /// <summary>Loads a submission with its assignment; with history also its attempts and files.</summary>
    Task<AssignmentSubmission?> GetSubmissionAsync(Guid submissionId, bool withHistory, CancellationToken ct = default);

    Task<AssignmentSubmission?> GetSubmissionAsync(Guid assignmentId, Guid studentProfileId, bool withHistory, CancellationToken ct = default);

    Task<IReadOnlyList<AssignmentSubmission>> GetSubmissionsAsync(Guid assignmentId, bool withHistory, CancellationToken ct = default);

    /// <summary>A file with its attempt's submission (and that submission's assignment).</summary>
    Task<(SubmissionFile File, AssignmentSubmission Submission)?> GetFileAsync(Guid fileId, CancellationToken ct = default);

    Task<IReadOnlyList<SubmissionComment>> GetCommentsAsync(Guid submissionId, CancellationToken ct = default);
    Task<SubmissionComment?> GetCommentAsync(Guid commentId, CancellationToken ct = default);

    Task<IReadOnlyList<SubmissionAnnotation>> GetAnnotationsAsync(IReadOnlyCollection<Guid> fileIds, CancellationToken ct = default);
    Task<SubmissionAnnotation?> GetAnnotationAsync(Guid annotationId, CancellationToken ct = default);

    Task<IReadOnlyList<ProvisionalGrade>> GetProvisionalGradesAsync(IReadOnlyCollection<Guid> submissionIds, CancellationToken ct = default);

    /// <summary>Files waiting for conversion, or claimed before <paramref name="staleBefore"/>.</summary>
    Task<IReadOnlyList<SubmissionFile>> GetConversionCandidatesAsync(DateTime staleBefore, int take, CancellationToken ct = default);

    Task<SubmissionFile?> GetFileForUpdateAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>Posted, graded submissions of one student in one offering (for the gradebook).</summary>
    Task<IReadOnlyList<(decimal Marks, decimal MaxMarks, Guid AssignmentId)>> GetPostedGradesAsync(Guid courseOfferingId, Guid studentProfileId, CancellationToken ct = default);

    Task<bool> IsEnrolledAsync(Guid courseOfferingId, Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Student display names and registration numbers.</summary>
    Task<IReadOnlyDictionary<Guid, (string Name, string? RegistrationNumber)>> GetStudentNamesAsync(IReadOnlyCollection<Guid> studentProfileIds, CancellationToken ct = default);

    /// <summary>User display names (graders, comment authors).</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetUserNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);

    /// <summary>Student-profile → user id (to tell whether a comment author is the submitting student).</summary>
    Task<Guid?> GetStudentUserIdAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Queues a new entity for insertion (children found through navigations are not added automatically).</summary>
    void Add<T>(T entity) where T : BaseEntity;

    void Remove<T>(T entity) where T : BaseEntity;

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Saves a conversion claim. Returns false (and forgets the stale row) when another worker changed it first.</summary>
    Task<bool> TrySaveClaimAsync(CancellationToken ct = default);
}
