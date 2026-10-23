using Microsoft.Extensions.Logging;
using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Assignments;

/// <summary>
/// Plan M3 — submission history, comments, annotations and the grading workspace.
/// Rules that apply everywhere:
/// <list type="bullet">
///   <item>Students only ever reach their own submission. Tenant/campus scope for staff is checked by the API.</item>
///   <item>Marks, feedback, rubric scores, annotations and (under manual posting) grader comments reach the
///         student only once the grade is posted. Grader-only comments never do.</item>
///   <item>While an assignment is anonymous, graders see "Student N" instead of names (stable per assignment).</item>
///   <item>Under moderated grading, graders other than the moderator save provisional grades; the moderator sets the final grade.</item>
/// </list>
/// </summary>
public sealed partial class SubmissionWorkspaceService : ISubmissionWorkspaceService
{
    internal const string SubmissionCategory = "assignment-submissions";
    internal const string RenditionCategory = "assignment-submissions-pdf";
    internal const string CommentMediaCategory = "submission-comments";
    internal const int CommentMediaMaxMb = 25;

    internal static readonly IReadOnlySet<string> CommentMediaExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".webm", ".ogg", ".mp3", ".m4a", ".wav", ".mp4"
    };

    private readonly ISubmissionWorkspaceRepository _repo;
    private readonly IRubricRepository _rubrics;
    private readonly IAuditService _audit;
    private readonly IMediaStorageService? _media;
    private readonly IDocumentConverter? _converter;
    private readonly IAnnotatedPdfRenderer? _renderer;
    private readonly IAssignmentGradebookSync? _gradebook;
    private readonly IAssignmentPolicyService? _policies;
    private readonly ILogger<SubmissionWorkspaceService>? _logger;

    public SubmissionWorkspaceService(
        ISubmissionWorkspaceRepository repo,
        IRubricRepository rubrics,
        IAuditService audit,
        IMediaStorageService? media = null,
        IDocumentConverter? converter = null,
        IAnnotatedPdfRenderer? renderer = null,
        IAssignmentGradebookSync? gradebook = null,
        ILogger<SubmissionWorkspaceService>? logger = null,
        IAssignmentPolicyService? policies = null)
    {
        _policies = policies;
        _repo = repo;
        _rubrics = rubrics;
        _audit = audit;
        _media = media;
        _converter = converter;
        _renderer = renderer;
        _gradebook = gradebook;
        _logger = logger;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Settings
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<WorkspaceResult<AssignmentSettingsResponse>> GetSettingsAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var assignment = await _repo.GetAssignmentAsync(assignmentId, ct);
        return assignment is null
            ? WorkspaceResult<AssignmentSettingsResponse>.Fail(WorkspaceError.NotFound)
            : WorkspaceResult<AssignmentSettingsResponse>.Success(await ToSettingsAsync(assignment, ct));
    }

    public async Task<WorkspaceResult<AssignmentSettingsResponse>> SaveSettingsAsync(WorkspaceActor actor, Guid assignmentId, AssignmentSettingsRequest request, CancellationToken ct = default)
    {
        if (!actor.IsStaff) return WorkspaceResult<AssignmentSettingsResponse>.Fail(WorkspaceError.Forbidden);
        var assignment = await _repo.GetAssignmentAsync(assignmentId, ct);
        if (assignment is null) return WorkspaceResult<AssignmentSettingsResponse>.Fail(WorkspaceError.NotFound);

        if (!Enum.TryParse<GradePostingPolicy>(request.PostingPolicy, true, out var policy))
            return WorkspaceResult<AssignmentSettingsResponse>.Fail(WorkspaceError.Invalid, "Posting policy must be Automatic or Manual.");

        try
        {
            assignment.ConfigureSubmissions(request.AllowedAttempts, request.AllowedFileTypes, request.MaxFileSizeMb);
            // Turning moderation on makes the person who did it the moderator; keeping it on keeps the moderator.
            var moderator = request.ModeratedGrading ? assignment.ModeratorUserId ?? actor.UserId : (Guid?)null;
            assignment.ConfigureGrading(request.AnonymousGrading, policy, request.ModeratedGrading, moderator);
        }
        catch (ArgumentException ex)
        {
            return WorkspaceResult<AssignmentSettingsResponse>.Fail(WorkspaceError.Invalid, ex.Message);
        }

        await _repo.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditLog("UpdateAssignmentSettings", "Assignment", assignment.Id.ToString(), actorUserId: actor.UserId,
            newValuesJson: System.Text.Json.JsonSerializer.Serialize(request)), ct);
        return WorkspaceResult<AssignmentSettingsResponse>.Success(await ToSettingsAsync(assignment, ct));
    }

    public async Task<Guid?> ResolveOfferingAsync(Guid? assignmentId = null, Guid? submissionId = null, Guid? fileId = null,
        Guid? commentId = null, Guid? annotationId = null, CancellationToken ct = default)
    {
        if (annotationId.HasValue)
        {
            var annotation = await _repo.GetAnnotationAsync(annotationId.Value, ct);
            if (annotation is null) return null;
            fileId = annotation.SubmissionFileId;
        }
        if (commentId.HasValue)
        {
            var comment = await _repo.GetCommentAsync(commentId.Value, ct);
            if (comment is null) return null;
            submissionId = comment.SubmissionId;
        }
        if (fileId.HasValue)
            return (await _repo.GetFileAsync(fileId.Value, ct))?.Submission.Assignment.CourseOfferingId;
        if (submissionId.HasValue)
            return (await _repo.GetSubmissionAsync(submissionId.Value, false, ct))?.Assignment.CourseOfferingId;
        if (assignmentId.HasValue)
            return (await _repo.GetAssignmentAsync(assignmentId.Value, ct))?.CourseOfferingId;
        return null;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Shared helpers
    // ═════════════════════════════════════════════════════════════════════════

    private static bool CanModerate(WorkspaceActor actor, Assignment assignment)
        => actor.IsStaff && assignment.ModeratedGrading && (actor.UserId == assignment.ModeratorUserId || actor.IsAdmin);

    private static bool OwnsSubmission(WorkspaceActor actor, AssignmentSubmission submission)
        => actor.StudentProfileId.HasValue && submission.StudentProfileId == actor.StudentProfileId.Value;

    private static bool CanSee(WorkspaceActor actor, AssignmentSubmission submission) => actor.IsStaff || OwnsSubmission(actor, submission);

    /// <summary>Students see grader comments made under manual posting only once the grade is posted.</summary>
    private static bool StudentSeesComment(SubmissionComment c, AssignmentSubmission submission, Guid studentUserId)
    {
        if (c.IsDeleted || c.IsPrivateToGraders) return false;
        if (c.AuthorUserId == studentUserId || !c.AuthorIsStaff) return true;
        return submission.Assignment.PostingPolicy == GradePostingPolicy.Automatic || submission.IsGradeVisibleToStudent;
    }

    /// <summary>Stable "Student N" numbers for anonymous grading (order of submission ids).</summary>
    private static Dictionary<Guid, int> AnonymousNumbers(IEnumerable<AssignmentSubmission> submissions)
        => submissions.Select(s => s.Id).OrderBy(id => id).Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i + 1);

    private async Task<AssignmentSettingsResponse> ToSettingsAsync(Assignment a, CancellationToken ct)
    {
        string? moderatorName = null;
        if (a.ModeratorUserId.HasValue)
            (await _repo.GetUserNamesAsync([a.ModeratorUserId.Value], ct)).TryGetValue(a.ModeratorUserId.Value, out moderatorName);
        return new AssignmentSettingsResponse(a.Id, a.AllowedAttempts, a.EffectiveFileTypes, a.AllowedFileTypes is not null,
            a.MaxFileSizeMb, a.AnonymousGrading, a.NamesHidden, a.PostingPolicy.ToString(), a.ModeratedGrading, a.ModeratorUserId, moderatorName);
    }

    private static SubmissionFileResponse ToFileResponse(SubmissionFile f, int annotationCount)
        => new(f.Id, f.FileName, f.ContentType, f.SizeBytes, f.ViewerKind, f.ConversionStatus.ToString(), f.ConversionError,
            f.LegacyUrl is not null, f.LegacyUrl, annotationCount);

    /// <summary>
    /// Builds the detail view for <paramref name="actor"/>: students get only what they may see;
    /// graders get everything (names hidden while anonymous).
    /// </summary>
    private async Task<SubmissionDetailResponse> BuildDetailAsync(WorkspaceActor actor, AssignmentSubmission submission, CancellationToken ct)
    {
        var assignment = submission.Assignment;
        var forStudent = !actor.IsStaff;
        var gradeVisible = !forStudent || submission.IsGradeVisibleToStudent;

        // Who is this?
        string label;
        string? regNo = null;
        Guid? studentId = submission.StudentProfileId;
        List<AssignmentSubmission>? siblings = null;
        if (!forStudent && assignment.NamesHidden)
        {
            siblings = (await _repo.GetSubmissionsAsync(assignment.Id, false, ct)).ToList();
            label = $"Student {AnonymousNumbers(siblings).GetValueOrDefault(submission.Id)}";
            studentId = null;
        }
        else
        {
            var names = await _repo.GetStudentNamesAsync([submission.StudentProfileId], ct);
            (label, regNo) = names.TryGetValue(submission.StudentProfileId, out var n) ? (n.Name, n.RegistrationNumber) : ("Student", null);
        }

        // Attempts and files (annotation counts only where the viewer may see them).
        var files = submission.Attempts.SelectMany(a => a.Files).ToList();
        var annotations = gradeVisible && files.Count > 0
            ? await _repo.GetAnnotationsAsync(files.Select(f => f.Id).ToList(), ct)
            : [];
        var counts = annotations.GroupBy(a => a.SubmissionFileId).ToDictionary(g => g.Key, g => g.Count());
        var attempts = submission.Attempts.OrderByDescending(a => a.AttemptNumber)
            .Select(a => new SubmissionAttemptResponse(a.AttemptNumber, a.SubmittedAt, a.IsLate, a.TextContent,
                a.Files.OrderBy(f => f.CreatedAt).Select(f => ToFileResponse(f, counts.GetValueOrDefault(f.Id))).ToList()))
            .ToList();

        // Comments.
        var studentUserId = await _repo.GetStudentUserIdAsync(submission.StudentProfileId, ct) ?? Guid.Empty;
        var comments = (await _repo.GetCommentsAsync(submission.Id, ct))
            .Where(c => !c.IsDeleted && (!forStudent || StudentSeesComment(c, submission, studentUserId)))
            .OrderBy(c => c.CreatedAt).ToList();
        var authorNames = await _repo.GetUserNamesAsync(comments.Select(c => c.AuthorUserId).Distinct().ToList(), ct);
        var commentViews = comments.Select(c => new SubmissionCommentResponse(
            c.Id, c.AttemptNumber,
            c.AuthorUserId == studentUserId && !forStudent && assignment.NamesHidden ? label : authorNames.GetValueOrDefault(c.AuthorUserId, c.AuthorIsStaff ? "Teacher" : "Student"),
            c.AuthorIsStaff, c.AuthorUserId == actor.UserId, c.Body, c.MediaStorageKey is not null, c.MediaContentType, c.MediaFileName,
            c.IsPrivateToGraders, c.CreatedAt)).ToList();

        // Moderation.
        List<ProvisionalGradeResponse>? provisional = null;
        var canModerate = CanModerate(actor, assignment);
        if (!forStudent && assignment.ModeratedGrading)
        {
            var grades = (await _repo.GetProvisionalGradesAsync([submission.Id], ct))
                .Where(g => canModerate || g.GraderUserId == actor.UserId).ToList();
            var graderNames = await _repo.GetUserNamesAsync(grades.Select(g => g.GraderUserId).Distinct().ToList(), ct);
            provisional = grades.OrderBy(g => g.CreatedAt)
                .Select(g => new ProvisionalGradeResponse(g.Id, g.GraderUserId, graderNames.GetValueOrDefault(g.GraderUserId, "Grader"),
                    g.Marks, g.Feedback, g.IsSelected, g.UpdatedAt ?? g.CreatedAt)).ToList();
        }

        var rubric = gradeVisible ? await BuildRubricViewAsync(assignment.Id, submission.Id, ct) : null;
        (decimal? Average, int Completed) peer = !forStudent && _policies is not null ? await _policies.PeerSummaryAsync(submission, ct) : (null, 0);

        var nextNumber = submission.CurrentAttemptNumber + 1;
        // Plan M8: the student's own dates (overrides, late cut-off).
        var dates = await DatesAsync(assignment, submission.StudentProfileId, ct);
        var now = DateTime.UtcNow;
        var open = assignment.IsPublished && now <= dates.ClosesAt && (dates.AvailableFrom is null || now >= dates.AvailableFrom);
        var canResubmit = forStudent && open && assignment.AllowsAttempt(nextNumber) && submission.Status != SubmissionStatus.Rejected;
        int? remaining = assignment.AllowedAttempts == 0 ? null : Math.Max(0, assignment.AllowedAttempts - submission.CurrentAttemptNumber);

        Guid? previous = null, next = null;
        if (!forStudent)
        {
            var order = await OrderedSubmissionIdsAsync(assignment, siblings, ct);
            var index = order.IndexOf(submission.Id);
            if (index > 0) previous = order[index - 1];
            if (index >= 0 && index < order.Count - 1) next = order[index + 1];
        }

        return new SubmissionDetailResponse(
            submission.Id, assignment.Id, assignment.Title, assignment.CourseOfferingId, assignment.MaxMarks, assignment.DueDate,
            studentId, label, regNo, submission.Status.ToString(), submission.CurrentAttemptNumber, submission.IsLate, submission.SubmittedAt,
            gradeVisible ? submission.MarksAwarded : null,
            gradeVisible ? submission.Feedback : null,
            gradeVisible ? submission.GradedAt : null,
            submission.PostedAt.HasValue, gradeVisible ? submission.PostedAt : null,
            submission.MarksAwarded.HasValue && !submission.PostedAt.HasValue,
            attempts, commentViews, provisional, rubric, await ToSettingsAsync(assignment, ct),
            canResubmit, remaining, canModerate, previous, next,
            RawMarks: gradeVisible ? submission.RawMarks : null,
            LatePenalty: gradeVisible ? submission.LatePenalty : 0,
            LatePenaltyWaived: submission.LatePenaltyWaived,
            IsMissing: submission.IsMissing,
            GroupId: submission.GroupId,
            GroupName: submission.GroupId is { } groupId && _policies is not null ? await _policies.GroupNameAsync(groupId, ct) : null,
            PeerReviewAverage: peer.Average,
            PeerReviewsCompleted: peer.Completed);
    }

    /// <summary>Plan M8: the student's dates (the assignment's own when no policy service is configured).</summary>
    private async Task<EffectiveDates> DatesAsync(Assignment assignment, Guid studentProfileId, CancellationToken ct)
        => _policies is null
            ? new EffectiveDates(assignment.DueDate, assignment.AvailableFrom, assignment.ClosesAt, "assignment")
            : await _policies.DatesForAsync(assignment, studentProfileId, ct);

    /// <summary>Plan M8: grades with the late deduction (when a policy service is configured).</summary>
    private async Task GradeWithPolicyAsync(AssignmentSubmission submission, decimal marks, string? feedback, Guid gradedBy, CancellationToken ct)
    {
        var assignment = submission.Assignment;
        var penalty = _policies is null ? 0 : await _policies.LatePenaltyAsync(assignment, submission, marks, ct);
        submission.Grade(marks, feedback, gradedBy, postNow: assignment.PostingPolicy == GradePostingPolicy.Automatic, latePenalty: penalty);
    }

    /// <summary>The workspace order: by name (or by "Student N" while anonymous).</summary>
    private async Task<List<Guid>> OrderedSubmissionIdsAsync(Assignment assignment, List<AssignmentSubmission>? submissions, CancellationToken ct)
    {
        submissions ??= (await _repo.GetSubmissionsAsync(assignment.Id, false, ct)).ToList();
        if (assignment.NamesHidden)
        {
            var numbers = AnonymousNumbers(submissions);
            return submissions.OrderBy(s => numbers[s.Id]).Select(s => s.Id).ToList();
        }
        var names = await _repo.GetStudentNamesAsync(submissions.Select(s => s.StudentProfileId).Distinct().ToList(), ct);
        return submissions
            .OrderBy(s => names.TryGetValue(s.StudentProfileId, out var n) ? n.Name : "~", StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.Id)
            .Select(s => s.Id).ToList();
    }

    private async Task<RubricGradeView?> BuildRubricViewAsync(Guid assignmentId, Guid submissionId, CancellationToken ct)
    {
        var rubric = await _rubrics.GetByAssignmentAsync(assignmentId, ct);
        if (rubric is null) return null;
        var chosen = (await _rubrics.GetStudentGradesForSubmissionAsync(submissionId, ct)).ToDictionary(g => g.RubricCriterionId);
        var criteria = rubric.Criteria.OrderBy(c => c.DisplayOrder).Select(c =>
        {
            chosen.TryGetValue(c.Id, out var grade);
            return new RubricCriterionView(c.Id, c.Name, c.MaxPoints,
                c.Levels.OrderBy(l => l.DisplayOrder).Select(l => new RubricLevelChoice(l.Id, l.Label, l.PointsAwarded)).ToList(),
                grade?.RubricLevelId, grade?.PointsAwarded);
        }).ToList();
        return new RubricGradeView(rubric.Id, rubric.Title, criteria.Sum(c => c.Points ?? 0), rubric.Criteria.Sum(c => c.MaxPoints), criteria);
    }

    private async Task RefreshGradebookAsync(Guid courseOfferingId, IEnumerable<Guid> studentProfileIds, CancellationToken ct)
    {
        if (_gradebook is null) return;
        foreach (var id in studentProfileIds.Distinct())
            await _gradebook.RefreshAsync(courseOfferingId, id, ct);
    }

    private async Task<(MediaStorageSaveResult? Stored, string? Error)> StoreAsync(IncomingFile file, string category, CancellationToken ct)
    {
        if (_media is null) return (null, "File storage is not configured.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var stored = await _media.SaveAsync(file.Content, category, extension,
            string.IsNullOrWhiteSpace(file.ContentType) ? null : file.ContentType, Path.GetFileName(file.FileName), ct);
        return (stored, null);
    }

    private async Task DeleteStoredQuietlyAsync(IEnumerable<string> keys)
    {
        if (_media is null) return;
        foreach (var key in keys)
        {
            try { await _media.DeleteAsync(key, CancellationToken.None); }
            catch (Exception ex) { _logger?.LogWarning(ex, "Could not remove stored file {StorageKey} after a failed submission.", key); }
        }
    }
}
