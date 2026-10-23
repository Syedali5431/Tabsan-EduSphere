using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Auditing;

namespace Tabsan.EduSphere.Application.Assignments;

public sealed partial class SubmissionWorkspaceService
{
    public async Task<WorkspaceResult<StudentAssignmentView>> GetStudentViewAsync(WorkspaceActor actor, Guid assignmentId, CancellationToken ct = default)
    {
        if (!actor.IsStudent) return WorkspaceResult<StudentAssignmentView>.Fail(WorkspaceError.Forbidden);
        var assignment = await _repo.GetAssignmentAsync(assignmentId, ct);
        if (assignment is null || !assignment.IsPublished) return WorkspaceResult<StudentAssignmentView>.Fail(WorkspaceError.NotFound);
        if (!await _repo.IsEnrolledAsync(assignment.CourseOfferingId, actor.StudentProfileId!.Value, ct))
            return WorkspaceResult<StudentAssignmentView>.Fail(WorkspaceError.Forbidden, "You are not enrolled in this course.");

        var submission = await _repo.GetSubmissionAsync(assignmentId, actor.StudentProfileId.Value, true, ct);
        var detail = submission is null ? null : await BuildDetailAsync(actor, submission, ct);
        // Plan M8: the student's own due date and window (overrides, late cut-off).
        var dates = await DatesAsync(assignment, actor.StudentProfileId.Value, ct);
        var now = DateTime.UtcNow;
        return WorkspaceResult<StudentAssignmentView>.Success(new StudentAssignmentView(
            assignment.Id, assignment.Title, assignment.Description, dates.DueDate, assignment.MaxMarks,
            now <= dates.ClosesAt && (dates.AvailableFrom is null || now >= dates.AvailableFrom), await ToSettingsAsync(assignment, ct), detail));
    }

    public Task<WorkspaceResult<SubmissionDetailResponse>> SubmitAsync(WorkspaceActor actor, Guid assignmentId, string? textContent,
        IReadOnlyList<IncomingFile> files, CancellationToken ct = default)
        => SubmitCoreAsync(actor, assignmentId, textContent, files, legacyFileUrl: null, ct);

    public Task<WorkspaceResult<SubmissionDetailResponse>> SubmitLegacyAsync(WorkspaceActor actor, Guid assignmentId, string? fileUrl,
        string? textContent, CancellationToken ct = default)
        => SubmitCoreAsync(actor, assignmentId, textContent, [], string.IsNullOrWhiteSpace(fileUrl) ? null : fileUrl.Trim(), ct);

    private async Task<WorkspaceResult<SubmissionDetailResponse>> SubmitCoreAsync(WorkspaceActor actor, Guid assignmentId, string? textContent,
        IReadOnlyList<IncomingFile> files, string? legacyFileUrl, CancellationToken ct)
    {
        static WorkspaceResult<SubmissionDetailResponse> Fail(WorkspaceError e, string m) => WorkspaceResult<SubmissionDetailResponse>.Fail(e, m);

        if (!actor.IsStudent) return Fail(WorkspaceError.Forbidden, "Only students submit assignments.");
        var studentId = actor.StudentProfileId!.Value;
        var assignment = await _repo.GetAssignmentAsync(assignmentId, ct);
        if (assignment is null || !assignment.IsPublished) return Fail(WorkspaceError.NotFound, "This assignment is not open for submissions.");
        if (assignment.IsCompletedElsewhere)
            return Fail(WorkspaceError.Conflict, "This activity is completed in its course tool or SCORM package, not by uploading here.");
        if (!await _repo.IsEnrolledAsync(assignment.CourseOfferingId, studentId, ct))
            return Fail(WorkspaceError.Forbidden, "You are not enrolled in this course.");

        var now = DateTime.UtcNow;
        // Plan M8: the student's own window — opening date, due date (late after it) and the late cut-off.
        var dates = await DatesAsync(assignment, studentId, ct);
        if (dates.AvailableFrom is { } opens && now < opens) return Fail(WorkspaceError.Conflict, $"Submissions open on {opens:dd MMM yyyy HH:mm} (UTC).");
        if (now > dates.ClosesAt)
            return Fail(WorkspaceError.Conflict, dates.ClosesAt > dates.DueDate ? "Late submissions are closed." : "The due date has passed.");
        var isLate = now > dates.DueDate;

        // Plan M8.3: a group assignment is handed in once for the whole group.
        (Guid GroupId, IReadOnlyList<Guid> MemberIds)? group = null;
        if (assignment.GroupSetId is not null)
        {
            group = _policies is null ? null : await _policies.GroupForAsync(assignment, studentId, ct);
            if (group is null) return Fail(WorkspaceError.Conflict, "This is a group assignment. Join a group before handing in work.");
        }

        var text = string.IsNullOrWhiteSpace(textContent) ? null : textContent.Trim();
        if (text is { Length: > 8000 }) return Fail(WorkspaceError.Invalid, "Text submissions are limited to 8000 characters.");
        if (text is null && files.Count == 0 && legacyFileUrl is null) return Fail(WorkspaceError.Invalid, "Attach a file or add text before submitting.");
        if (files.Count > Assignment.MaxFilesPerAttempt) return Fail(WorkspaceError.Invalid, $"Attach at most {Assignment.MaxFilesPerAttempt} files.");
        if (files.Sum(f => f.Length) > Assignment.MaxAttemptTotalMb * 1024L * 1024L)
            return Fail(WorkspaceError.Invalid, $"All files together must be under {Assignment.MaxAttemptTotalMb} MB.");
        foreach (var f in files)
        {
            if (f.Length <= 0) return Fail(WorkspaceError.Invalid, $"\"{f.FileName}\" is empty.");
            if (!assignment.AcceptsFileType(f.FileName))
                return Fail(WorkspaceError.Invalid, $"\"{f.FileName}\" is not an accepted file type. Accepted: {string.Join(", ", assignment.EffectiveFileTypes)}.");
            if (f.Length > assignment.MaxFileSizeMb * 1024L * 1024L)
                return Fail(WorkspaceError.Invalid, $"\"{f.FileName}\" is larger than {assignment.MaxFileSizeMb} MB.");
        }
        if (files.Count > 0 && _media is null) return Fail(WorkspaceError.Invalid, "File uploads are not configured.");

        var submission = await _repo.GetSubmissionAsync(assignmentId, studentId, false, ct);
        var isNew = submission is null;
        if (submission is not null)
        {
            if (submission.Status == SubmissionStatus.Rejected) return Fail(WorkspaceError.Conflict, "This submission was rejected. Ask your teacher before submitting again.");
            if (!assignment.AllowsAttempt(submission.CurrentAttemptNumber + 1))
                return Fail(WorkspaceError.Conflict, assignment.AllowedAttempts == 1
                    ? "You have already submitted this assignment."
                    : $"You have used all {assignment.AllowedAttempts} attempts.");
        }

        // Store the files first; undo the storage if anything fails before the database commit.
        var stored = new List<(IncomingFile File, MediaStorageSaveResult Saved)>();
        try
        {
            foreach (var f in files)
            {
                var (saved, error) = await StoreAsync(f, SubmissionCategory, ct);
                if (saved is null) return Fail(WorkspaceError.Invalid, error ?? "The file could not be stored.");
                stored.Add((f, saved));
            }

            if (submission is null)
            {
                submission = AssignmentSubmission.Start(assignmentId, studentId);
                _repo.Add(submission);
            }
            var attempt = submission.AddAttempt(text, legacyFileUrl, now, isLate);
            _repo.Add(attempt);
            foreach (var legacy in attempt.Files) _repo.Add(legacy);
            foreach (var (file, saved) in stored)
            {
                var entity = attempt.AddFile(file.FileName, saved.ContentType, saved.Length, saved.StorageKey, saved.ContentHashSha256);
                _repo.Add(entity);
            }

            // Plan M8.3: every member gets the same attempt (the stored files are shared, not copied).
            if (group is { } g)
            {
                submission.SetGroup(g.GroupId);
                foreach (var memberId in g.MemberIds.Where(id => id != studentId))
                {
                    var mate = await _repo.GetSubmissionAsync(assignmentId, memberId, false, ct);
                    if (mate is { Status: SubmissionStatus.Rejected }) continue;
                    if (mate is null)
                    {
                        mate = AssignmentSubmission.Start(assignmentId, memberId);
                        _repo.Add(mate);
                    }
                    mate.SetGroup(g.GroupId);
                    var mateAttempt = mate.AddAttempt(text, legacyFileUrl, now, isLate);
                    _repo.Add(mateAttempt);
                    foreach (var legacy in mateAttempt.Files) _repo.Add(legacy);
                    foreach (var (file, saved) in stored)
                        _repo.Add(mateAttempt.AddFile(file.FileName, saved.ContentType, saved.Length, saved.StorageKey, saved.ContentHashSha256));
                }
            }
            await _repo.SaveChangesAsync(ct);
        }
        catch
        {
            await DeleteStoredQuietlyAsync(stored.Select(s => s.Saved.StorageKey));
            throw;
        }

        await _audit.LogAsync(new AuditLog(isNew ? "Submit" : "Resubmit", "AssignmentSubmission", submission.Id.ToString(),
            actorUserId: actor.UserId, newValuesJson: $"{{\"attempt\":{submission.CurrentAttemptNumber},\"files\":{stored.Count}}}"), ct);

        var reloaded = await _repo.GetSubmissionAsync(submission.Id, true, ct);
        return WorkspaceResult<SubmissionDetailResponse>.Success(await BuildDetailAsync(actor, reloaded!, ct));
    }
}
