using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Domain.Assignments;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>Who is calling. Tenant/campus scope is checked by the API before the service runs.</summary>
public sealed record WorkspaceActor(Guid UserId, Guid? StudentProfileId, bool IsStaff, bool IsAdmin)
{
    public bool IsStudent => StudentProfileId.HasValue && !IsStaff;
}

public enum WorkspaceError
{
    None = 0,
    NotFound,
    Forbidden,
    Invalid,
    Conflict
}

public sealed record WorkspaceResult<T>(T? Value, WorkspaceError Error, string? Message)
{
    public bool Ok => Error == WorkspaceError.None;
    public static WorkspaceResult<T> Success(T value) => new(value, WorkspaceError.None, null);
    public static WorkspaceResult<T> Fail(WorkspaceError error, string? message = null) => new(default, error, message);
}

/// <summary>
/// Plan M3: submission history (attempts, files, comments), annotations, the grading workspace,
/// posting policy, anonymous and moderated grading.
/// </summary>
public interface ISubmissionWorkspaceService
{
    // Settings
    Task<WorkspaceResult<AssignmentSettingsResponse>> GetSettingsAsync(Guid assignmentId, CancellationToken ct = default);
    Task<WorkspaceResult<AssignmentSettingsResponse>> SaveSettingsAsync(WorkspaceActor actor, Guid assignmentId, AssignmentSettingsRequest request, CancellationToken ct = default);

    // Students
    Task<WorkspaceResult<StudentAssignmentView>> GetStudentViewAsync(WorkspaceActor actor, Guid assignmentId, CancellationToken ct = default);
    Task<WorkspaceResult<SubmissionDetailResponse>> SubmitAsync(WorkspaceActor actor, Guid assignmentId, string? textContent, IReadOnlyList<IncomingFile> files, CancellationToken ct = default);

    /// <summary>The pre-M3 JSON submit (file URL and/or text) recorded as an attempt.</summary>
    Task<WorkspaceResult<SubmissionDetailResponse>> SubmitLegacyAsync(WorkspaceActor actor, Guid assignmentId, string? fileUrl, string? textContent, CancellationToken ct = default);

    // Shared
    Task<WorkspaceResult<SubmissionDetailResponse>> GetSubmissionAsync(WorkspaceActor actor, Guid submissionId, CancellationToken ct = default);
    Task<WorkspaceResult<FileDownload>> DownloadFileAsync(WorkspaceActor actor, Guid fileId, bool pdfRendition, CancellationToken ct = default);
    Task<WorkspaceResult<FileDownload>> DownloadAnnotatedPdfAsync(WorkspaceActor actor, Guid fileId, CancellationToken ct = default);
    Task<WorkspaceResult<SubmissionCommentResponse>> AddCommentAsync(WorkspaceActor actor, Guid submissionId, int? attemptNumber, string? body, bool isPrivate, IncomingFile? media, CancellationToken ct = default);
    Task<WorkspaceResult<bool>> DeleteCommentAsync(WorkspaceActor actor, Guid commentId, CancellationToken ct = default);
    Task<WorkspaceResult<FileDownload>> DownloadCommentMediaAsync(WorkspaceActor actor, Guid commentId, CancellationToken ct = default);
    Task<WorkspaceResult<IReadOnlyList<AnnotationResponse>>> GetAnnotationsAsync(WorkspaceActor actor, Guid fileId, CancellationToken ct = default);

    // Graders
    Task<WorkspaceResult<GradingWorkspaceResponse>> GetWorkspaceAsync(WorkspaceActor actor, Guid assignmentId, string? filter, CancellationToken ct = default);
    Task<WorkspaceResult<SaveGradeResponse>> SaveGradeAsync(WorkspaceActor actor, Guid submissionId, SaveGradeRequest request, CancellationToken ct = default);
    Task<WorkspaceResult<SubmissionDetailResponse>> ScoreRubricAsync(WorkspaceActor actor, Guid submissionId, RubricScoreRequest request, CancellationToken ct = default);
    Task<WorkspaceResult<AnnotationResponse>> AddAnnotationAsync(WorkspaceActor actor, Guid fileId, SaveAnnotationRequest request, CancellationToken ct = default);
    Task<WorkspaceResult<AnnotationResponse>> UpdateAnnotationAsync(WorkspaceActor actor, Guid annotationId, SaveAnnotationRequest request, CancellationToken ct = default);
    Task<WorkspaceResult<bool>> DeleteAnnotationAsync(WorkspaceActor actor, Guid annotationId, CancellationToken ct = default);
    Task<WorkspaceResult<PostGradesResponse>> PostGradesAsync(WorkspaceActor actor, Guid assignmentId, PostGradesRequest request, CancellationToken ct = default);
    Task<WorkspaceResult<PostGradesResponse>> HideGradesAsync(WorkspaceActor actor, Guid assignmentId, PostGradesRequest request, CancellationToken ct = default);
    Task<WorkspaceResult<bool>> RevealNamesAsync(WorkspaceActor actor, Guid assignmentId, CancellationToken ct = default);
    Task<WorkspaceResult<SubmissionDetailResponse>> ModerateAsync(WorkspaceActor actor, Guid submissionId, ModerateRequest request, CancellationToken ct = default);
    Task<WorkspaceResult<bool>> RequeueConversionAsync(WorkspaceActor actor, Guid fileId, CancellationToken ct = default);

    /// <summary>Course offering of an assignment / submission / file / comment / annotation (for the API's scope check).</summary>
    Task<Guid?> ResolveOfferingAsync(Guid? assignmentId = null, Guid? submissionId = null, Guid? fileId = null, Guid? commentId = null, Guid? annotationId = null, CancellationToken ct = default);
}

/// <summary>Plan M3.2: turns office documents into PDF (Gotenberg). Disabled when not configured.</summary>
public interface IDocumentConverter
{
    bool IsEnabled { get; }
    Task<byte[]> ConvertToPdfAsync(byte[] content, string fileName, CancellationToken ct = default);
}

/// <summary>Plan M3.2: processes the conversion queue (called by the background worker).</summary>
public interface ISubmissionConversionProcessor
{
    /// <summary>Converts up to <paramref name="batchSize"/> waiting files. Returns how many were converted.</summary>
    Task<int> ProcessBatchAsync(int batchSize, CancellationToken ct = default);
}

/// <summary>Plan M3.3: draws annotations onto a PDF (or an image turned into a PDF page).</summary>
public interface IAnnotatedPdfRenderer
{
    byte[] Render(byte[] source, bool sourceIsImage, IReadOnlyList<SubmissionAnnotation> annotations, string title);
}

/// <summary>Plan M3.5: copies posted assignment grades into the gradebook ("Assignment"/"Assignments" component).</summary>
public interface IAssignmentGradebookSync
{
    /// <summary>Never throws (except cancellation); a failure is logged and returned as null.</summary>
    Task<SystemResultSyncOutcome?> RefreshAsync(Guid courseOfferingId, Guid studentProfileId, CancellationToken ct = default);
}
