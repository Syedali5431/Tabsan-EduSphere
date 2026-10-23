using Microsoft.Extensions.Logging;
using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Auditing;

namespace Tabsan.EduSphere.Application.Assignments;

public sealed partial class SubmissionWorkspaceService
{
    public async Task<WorkspaceResult<SubmissionDetailResponse>> GetSubmissionAsync(WorkspaceActor actor, Guid submissionId, CancellationToken ct = default)
    {
        var submission = await _repo.GetSubmissionAsync(submissionId, true, ct);
        if (submission is null || !CanSee(actor, submission)) return WorkspaceResult<SubmissionDetailResponse>.Fail(WorkspaceError.NotFound);
        return WorkspaceResult<SubmissionDetailResponse>.Success(await BuildDetailAsync(actor, submission, ct));
    }

    // ── Files ───────────────────────────────────────────────────────────────

    public async Task<WorkspaceResult<FileDownload>> DownloadFileAsync(WorkspaceActor actor, Guid fileId, bool pdfRendition, CancellationToken ct = default)
    {
        var found = await _repo.GetFileAsync(fileId, ct);
        if (found is not { } pair) return WorkspaceResult<FileDownload>.Fail(WorkspaceError.NotFound);
        // Plan M8.4: a peer reviewer may open the files of the work they review.
        var reviewer = !CanSee(actor, pair.Submission) && actor.IsStudent && _policies is not null
            && await _policies.IsPeerReviewerAsync(pair.Submission.Id, actor.StudentProfileId!.Value, ct);
        if (!CanSee(actor, pair.Submission) && !reviewer) return WorkspaceResult<FileDownload>.Fail(WorkspaceError.NotFound);
        var file = pair.File;
        if (file.StorageKey is null || _media is null)
            return WorkspaceResult<FileDownload>.Fail(WorkspaceError.NotFound, "This file was uploaded before Plan M3 and is served from its original link.");

        if (pdfRendition && !file.IsPdf)
        {
            if (file.ConversionStatus != FileConversionStatus.Ready || file.PdfStorageKey is null)
                return WorkspaceResult<FileDownload>.Fail(WorkspaceError.Conflict, "The PDF preview is not ready.");
            var pdf = await _media.ReadAsBytesAsync(file.PdfStorageKey, ct);
            return pdf is null
                ? WorkspaceResult<FileDownload>.Fail(WorkspaceError.NotFound)
                : WorkspaceResult<FileDownload>.Success(new FileDownload(pdf, "application/pdf", Path.ChangeExtension(file.FileName, ".pdf")));
        }

        var bytes = await _media.ReadAsBytesAsync(file.StorageKey, ct);
        return bytes is null
            ? WorkspaceResult<FileDownload>.Fail(WorkspaceError.NotFound)
            : WorkspaceResult<FileDownload>.Success(new FileDownload(bytes, file.ContentType, file.FileName));
    }

    public async Task<WorkspaceResult<FileDownload>> DownloadAnnotatedPdfAsync(WorkspaceActor actor, Guid fileId, CancellationToken ct = default)
    {
        var found = await _repo.GetFileAsync(fileId, ct);
        if (found is not { } pair || !CanSee(actor, pair.Submission)) return WorkspaceResult<FileDownload>.Fail(WorkspaceError.NotFound);
        if (!actor.IsStaff && !pair.Submission.IsGradeVisibleToStudent)
            return WorkspaceResult<FileDownload>.Fail(WorkspaceError.Conflict, "Feedback on this file is shown once your grade is posted.");
        if (_renderer is null || _media is null) return WorkspaceResult<FileDownload>.Fail(WorkspaceError.Invalid, "Annotated PDFs are not available on this server.");

        var file = pair.File;
        var key = file.IsPdf || file.IsImage ? file.StorageKey : file.ConversionStatus == FileConversionStatus.Ready ? file.PdfStorageKey : null;
        if (key is null) return WorkspaceResult<FileDownload>.Fail(WorkspaceError.Conflict, "This file has no PDF view to annotate.");
        var source = await _media.ReadAsBytesAsync(key, ct);
        if (source is null) return WorkspaceResult<FileDownload>.Fail(WorkspaceError.NotFound);

        var annotations = await _repo.GetAnnotationsAsync([file.Id], ct);
        try
        {
            var pdf = _renderer.Render(source, file.IsImage, annotations.OrderBy(a => a.PageNumber).ThenBy(a => a.CreatedAt).ToList(),
                $"{pair.Submission.Assignment.Title} — {file.FileName}");
            return WorkspaceResult<FileDownload>.Success(new FileDownload(pdf, "application/pdf",
                Path.GetFileNameWithoutExtension(file.FileName) + "-annotated.pdf"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Annotated PDF failed for submission file {FileId}.", file.Id);
            return WorkspaceResult<FileDownload>.Fail(WorkspaceError.Invalid, "The annotated PDF could not be produced: " + ex.Message);
        }
    }

    public async Task<WorkspaceResult<bool>> RequeueConversionAsync(WorkspaceActor actor, Guid fileId, CancellationToken ct = default)
    {
        if (!actor.IsStaff) return WorkspaceResult<bool>.Fail(WorkspaceError.Forbidden);
        var file = await _repo.GetFileForUpdateAsync(fileId, ct);
        if (file is null) return WorkspaceResult<bool>.Fail(WorkspaceError.NotFound);
        if (!file.RequeueConversion()) return WorkspaceResult<bool>.Fail(WorkspaceError.Invalid, "This file type is not converted.");
        await _repo.SaveChangesAsync(ct);
        return WorkspaceResult<bool>.Success(true);
    }

    // ── Comments ────────────────────────────────────────────────────────────

    public async Task<WorkspaceResult<SubmissionCommentResponse>> AddCommentAsync(WorkspaceActor actor, Guid submissionId, int? attemptNumber,
        string? body, bool isPrivate, IncomingFile? media, CancellationToken ct = default)
    {
        static WorkspaceResult<SubmissionCommentResponse> Fail(WorkspaceError e, string? m = null) => WorkspaceResult<SubmissionCommentResponse>.Fail(e, m);

        var submission = await _repo.GetSubmissionAsync(submissionId, false, ct);
        if (submission is null || !CanSee(actor, submission)) return Fail(WorkspaceError.NotFound);
        if (string.IsNullOrWhiteSpace(body) && media is null) return Fail(WorkspaceError.Invalid, "Write a comment or attach a recording.");
        if (attemptNumber is < 1 || attemptNumber > submission.CurrentAttemptNumber) return Fail(WorkspaceError.Invalid, "That attempt does not exist.");
        if (media is not null)
        {
            if (!CommentMediaExtensions.Contains(Path.GetExtension(media.FileName)))
                return Fail(WorkspaceError.Invalid, $"Recordings must be one of: {string.Join(", ", CommentMediaExtensions)}.");
            if (media.Length <= 0 || media.Length > CommentMediaMaxMb * 1024L * 1024L)
                return Fail(WorkspaceError.Invalid, $"Recordings must be under {CommentMediaMaxMb} MB.");
        }

        SubmissionComment comment;
        try { comment = new SubmissionComment(submissionId, attemptNumber, actor.UserId, actor.IsStaff, body, isPrivate); }
        catch (ArgumentException ex) { return Fail(WorkspaceError.Invalid, ex.Message); }

        string? storedKey = null;
        try
        {
            if (media is not null)
            {
                var (saved, error) = await StoreAsync(media, CommentMediaCategory, ct);
                if (saved is null) return Fail(WorkspaceError.Invalid, error);
                storedKey = saved.StorageKey;
                comment.AttachMedia(saved.StorageKey, saved.ContentType, media.FileName);
            }
            _repo.Add(comment);
            await _repo.SaveChangesAsync(ct);
        }
        catch
        {
            if (storedKey is not null) await DeleteStoredQuietlyAsync([storedKey]);
            throw;
        }

        var name = (await _repo.GetUserNamesAsync([actor.UserId], ct)).GetValueOrDefault(actor.UserId, actor.IsStaff ? "Teacher" : "Student");
        return WorkspaceResult<SubmissionCommentResponse>.Success(new SubmissionCommentResponse(comment.Id, comment.AttemptNumber, name,
            comment.AuthorIsStaff, true, comment.Body, comment.MediaStorageKey is not null, comment.MediaContentType, comment.MediaFileName,
            comment.IsPrivateToGraders, comment.CreatedAt));
    }

    public async Task<WorkspaceResult<bool>> DeleteCommentAsync(WorkspaceActor actor, Guid commentId, CancellationToken ct = default)
    {
        var comment = await _repo.GetCommentAsync(commentId, ct);
        if (comment is null || comment.IsDeleted) return WorkspaceResult<bool>.Fail(WorkspaceError.NotFound);
        if (comment.AuthorUserId != actor.UserId && !actor.IsAdmin) return WorkspaceResult<bool>.Fail(WorkspaceError.Forbidden, "Only the author can delete a comment.");
        var mediaKey = comment.MediaStorageKey;
        comment.Delete();
        await _repo.SaveChangesAsync(ct);
        if (mediaKey is not null) await DeleteStoredQuietlyAsync([mediaKey]);
        await _audit.LogAsync(new AuditLog("DeleteSubmissionComment", "SubmissionComment", comment.Id.ToString(), actorUserId: actor.UserId), ct);
        return WorkspaceResult<bool>.Success(true);
    }

    public async Task<WorkspaceResult<FileDownload>> DownloadCommentMediaAsync(WorkspaceActor actor, Guid commentId, CancellationToken ct = default)
    {
        var comment = await _repo.GetCommentAsync(commentId, ct);
        if (comment is null || comment.IsDeleted || comment.MediaStorageKey is null || _media is null) return WorkspaceResult<FileDownload>.Fail(WorkspaceError.NotFound);
        var submission = await _repo.GetSubmissionAsync(comment.SubmissionId, false, ct);
        if (submission is null || !CanSee(actor, submission)) return WorkspaceResult<FileDownload>.Fail(WorkspaceError.NotFound);
        if (!actor.IsStaff)
        {
            var studentUserId = await _repo.GetStudentUserIdAsync(submission.StudentProfileId, ct) ?? Guid.Empty;
            if (!StudentSeesComment(comment, submission, studentUserId)) return WorkspaceResult<FileDownload>.Fail(WorkspaceError.NotFound);
        }
        var bytes = await _media.ReadAsBytesAsync(comment.MediaStorageKey, ct);
        return bytes is null
            ? WorkspaceResult<FileDownload>.Fail(WorkspaceError.NotFound)
            : WorkspaceResult<FileDownload>.Success(new FileDownload(bytes, comment.MediaContentType ?? "application/octet-stream", comment.MediaFileName ?? "recording"));
    }

    // ── Annotations ─────────────────────────────────────────────────────────

    public async Task<WorkspaceResult<IReadOnlyList<AnnotationResponse>>> GetAnnotationsAsync(WorkspaceActor actor, Guid fileId, CancellationToken ct = default)
    {
        var found = await _repo.GetFileAsync(fileId, ct);
        if (found is not { } pair || !CanSee(actor, pair.Submission)) return WorkspaceResult<IReadOnlyList<AnnotationResponse>>.Fail(WorkspaceError.NotFound);
        if (!actor.IsStaff && !pair.Submission.IsGradeVisibleToStudent)
            return WorkspaceResult<IReadOnlyList<AnnotationResponse>>.Success([]);

        var annotations = await _repo.GetAnnotationsAsync([fileId], ct);
        var names = await _repo.GetUserNamesAsync(annotations.Select(a => a.AuthorUserId).Distinct().ToList(), ct);
        return WorkspaceResult<IReadOnlyList<AnnotationResponse>>.Success(
            annotations.OrderBy(a => a.PageNumber).ThenBy(a => a.CreatedAt).Select(a => ToAnnotationResponse(a, names, actor)).ToList());
    }

    public async Task<WorkspaceResult<AnnotationResponse>> AddAnnotationAsync(WorkspaceActor actor, Guid fileId, SaveAnnotationRequest request, CancellationToken ct = default)
    {
        if (!actor.IsStaff) return WorkspaceResult<AnnotationResponse>.Fail(WorkspaceError.Forbidden);
        var found = await _repo.GetFileAsync(fileId, ct);
        if (found is not { } pair) return WorkspaceResult<AnnotationResponse>.Fail(WorkspaceError.NotFound);
        if (pair.File.ViewerKind == "download") return WorkspaceResult<AnnotationResponse>.Fail(WorkspaceError.Invalid, "This file cannot be annotated.");

        var (type, geometry, color, error) = AnnotationGeometry.Validate(request);
        if (error is not null) return WorkspaceResult<AnnotationResponse>.Fail(WorkspaceError.Invalid, error);

        SubmissionAnnotation annotation;
        try { annotation = new SubmissionAnnotation(fileId, request.PageNumber, type, geometry!, request.Comment, color, actor.UserId); }
        catch (ArgumentException ex) { return WorkspaceResult<AnnotationResponse>.Fail(WorkspaceError.Invalid, ex.Message); }
        _repo.Add(annotation);
        await _repo.SaveChangesAsync(ct);
        var names = await _repo.GetUserNamesAsync([actor.UserId], ct);
        return WorkspaceResult<AnnotationResponse>.Success(ToAnnotationResponse(annotation, names, actor));
    }

    public async Task<WorkspaceResult<AnnotationResponse>> UpdateAnnotationAsync(WorkspaceActor actor, Guid annotationId, SaveAnnotationRequest request, CancellationToken ct = default)
    {
        if (!actor.IsStaff) return WorkspaceResult<AnnotationResponse>.Fail(WorkspaceError.Forbidden);
        var annotation = await _repo.GetAnnotationAsync(annotationId, ct);
        if (annotation is null) return WorkspaceResult<AnnotationResponse>.Fail(WorkspaceError.NotFound);

        var (type, geometry, color, error) = AnnotationGeometry.Validate(request with { PageNumber = annotation.PageNumber });
        if (error is not null) return WorkspaceResult<AnnotationResponse>.Fail(WorkspaceError.Invalid, error);
        if (type != annotation.Type) return WorkspaceResult<AnnotationResponse>.Fail(WorkspaceError.Invalid, "An annotation's type cannot change.");
        try { annotation.Update(geometry!, request.Comment, color); }
        catch (ArgumentException ex) { return WorkspaceResult<AnnotationResponse>.Fail(WorkspaceError.Invalid, ex.Message); }
        await _repo.SaveChangesAsync(ct);
        var names = await _repo.GetUserNamesAsync([annotation.AuthorUserId], ct);
        return WorkspaceResult<AnnotationResponse>.Success(ToAnnotationResponse(annotation, names, actor));
    }

    public async Task<WorkspaceResult<bool>> DeleteAnnotationAsync(WorkspaceActor actor, Guid annotationId, CancellationToken ct = default)
    {
        if (!actor.IsStaff) return WorkspaceResult<bool>.Fail(WorkspaceError.Forbidden);
        var annotation = await _repo.GetAnnotationAsync(annotationId, ct);
        if (annotation is null) return WorkspaceResult<bool>.Fail(WorkspaceError.NotFound);
        _repo.Remove(annotation);
        await _repo.SaveChangesAsync(ct);
        return WorkspaceResult<bool>.Success(true);
    }

    private static AnnotationResponse ToAnnotationResponse(SubmissionAnnotation a, IReadOnlyDictionary<Guid, string> names, WorkspaceActor actor)
        => new(a.Id, a.SubmissionFileId, a.PageNumber, a.Type.ToString(), a.GeometryJson, a.Comment, a.Color,
            names.GetValueOrDefault(a.AuthorUserId, "Teacher"), a.AuthorUserId == actor.UserId, a.CreatedAt);
}
