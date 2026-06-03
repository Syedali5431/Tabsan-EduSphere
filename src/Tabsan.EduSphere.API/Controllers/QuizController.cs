using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.DTOs.Quizzes;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Manages quiz authoring, publishing, attempt lifecycle, and grading.
/// </summary>
[ApiController]
[Route("api/v1/quiz")]
[Authorize]
public sealed class QuizController : ControllerBase
{
    private readonly IQuizService _quizService;
    private readonly IAccessScopeResolver _accessScope;
    private readonly ApplicationDbContext _db;

    /// <summary>Initialises the controller with the quiz service.</summary>
    public QuizController(IQuizService quizService, IAccessScopeResolver accessScope, ApplicationDbContext db)
    {
        _quizService = quizService;
        _accessScope = accessScope;
        _db = db;
    }

    // ── Authoring ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new unpublished quiz for a course offering.
    /// Accessible by Faculty and Admin.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> Create([FromBody] CreateQuizRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, request.CourseOfferingId, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        var id = await _quizService.CreateAsync(request, GetCurrentUserId(), ct);
        return CreatedAtAction(nameof(GetDetail), new { id }, new { quizId = id });
    }

    /// <summary>
    /// Updates the metadata of an unpublished quiz.
    /// Accessible by Faculty and Admin.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateQuizRequest request, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, id, ct);
        if (scope.Error is not null)
            return scope.Error;

        var ok = await _quizService.UpdateAsync(id, request, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Publishes a quiz, making it visible and accessible to students.
    /// Accessible by Faculty and Admin.
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> Publish(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, id, ct);
        if (scope.Error is not null)
            return scope.Error;

        var ok = await _quizService.PublishAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Unpublishes a quiz, hiding it from students without deleting it.
    /// Accessible by Faculty and Admin.
    /// </summary>
    [HttpPost("{id:guid}/unpublish")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> Unpublish(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, id, ct);
        if (scope.Error is not null)
            return scope.Error;

        var ok = await _quizService.UnpublishAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> Activate(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, id, ct);
        if (scope.Error is not null)
            return scope.Error;

        var ok = await _quizService.ActivateAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Soft-deletes a quiz.
    /// Accessible by Admin only.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Deactivate(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, id, ct);
        if (scope.Error is not null)
            return scope.Error;

        var ok = await _quizService.DeactivateAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = "Faculty")]
    public Task<IActionResult> DeactivatePost(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
        => Deactivate(id, tenantId, campusId, ct);

    // ── Question management ───────────────────────────────────────────────────

    /// <summary>
    /// Adds a question (with options) to a quiz.
    /// Accessible by Faculty and Admin.
    /// </summary>
    [HttpPost("question")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> AddQuestion([FromBody] AddQuestionRequest request, CancellationToken ct)
    {
        var id = await _quizService.AddQuestionAsync(request, ct);
        return Ok(new { questionId = id });
    }

    /// <summary>
    /// Updates an existing question's text, marks, and ordering.
    /// Accessible by Faculty and Admin.
    /// </summary>
    [HttpPut("question/{questionId:guid}")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> UpdateQuestion(Guid questionId, [FromBody] UpdateQuestionRequest request, CancellationToken ct)
    {
        var ok = await _quizService.UpdateQuestionAsync(questionId, request, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Removes a question and all its options.
    /// Accessible by Faculty and Admin.
    /// </summary>
    [HttpDelete("question/{questionId:guid}")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> RemoveQuestion(Guid questionId, CancellationToken ct)
    {
        var ok = await _quizService.RemoveQuestionAsync(questionId, ct);
        return ok ? NoContent() : NotFound();
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all active quizzes for a course offering.
    /// Accessible by all authenticated users.
    /// </summary>
    [HttpGet("by-offering/{courseOfferingId:guid}")]
    public async Task<IActionResult> GetByOffering(
        Guid courseOfferingId,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromQuery] bool includeInactive,
        CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, courseOfferingId, null, ct);
        if (scope.Error is not null)
            return scope.Error;

        var canIncludeInactive = includeInactive && (User.IsInRole("Admin") || User.IsInRole("SuperAdmin") || User.IsInRole("Faculty"));
        return Ok(await _quizService.GetByOfferingAsync(courseOfferingId, canIncludeInactive, ct));
    }

    /// <summary>
    /// Returns full quiz detail with questions and options.
    /// Faculty and Admin see correct answer flags; students see options without IsCorrect.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, id, ct);
        if (scope.Error is not null)
            return scope.Error;

        var result = await _quizService.GetDetailAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // ── Attempts ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Opens a new quiz attempt for the current student.
    /// Returns 409 when the attempt cap has been reached or an attempt is in-progress.
    /// </summary>
    [HttpPost("{id:guid}/start")]
    [Authorize(Policy = "Student")]
    public async Task<IActionResult> StartAttempt(Guid id, CancellationToken ct)
    {
        var studentProfileId = GetStudentProfileId();
        if (studentProfileId == Guid.Empty) return Forbid();

        var attempt = await _quizService.StartAttemptAsync(id, studentProfileId, ct);
        return attempt is null ? Conflict("Quiz unavailable or attempt cap reached.") : Ok(attempt);
    }

    /// <summary>
    /// Submits an in-progress attempt with all answers and returns the scored result.
    /// </summary>
    [HttpPost("attempt/submit")]
    [Authorize(Policy = "Student")]
    public async Task<IActionResult> SubmitAttempt([FromBody] SubmitAttemptRequest request, CancellationToken ct)
    {
        var result = await _quizService.SubmitAttemptAsync(request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Returns all attempts the current student has made across all quizzes (portal summary).
    /// </summary>
    [HttpGet("my-attempts")]
    [Authorize(Policy = "Student")]
    public async Task<IActionResult> GetAllMyAttempts(CancellationToken ct)
    {
        var studentProfileId = GetStudentProfileId();
        if (studentProfileId == Guid.Empty) return Forbid();

        var attempts = await _db.QuizAttempts
            .AsNoTracking()
            .Where(a => a.StudentProfileId == studentProfileId)
            .Join(
                _db.Quizzes.AsNoTracking(),
                a => a.QuizId,
                q => q.Id,
                (a, q) => new
                {
                    Attempt = a,
                    QuizTitle = q.Title,
                    MaxScore = (int)q.Questions.Sum(qq => qq.Marks)
                })
            .OrderByDescending(x => x.Attempt.StartedAt)
            .Select(x => new
            {
                attemptId = x.Attempt.Id,
                quizId = x.Attempt.QuizId,
                quizTitle = x.QuizTitle,
                startedAt = x.Attempt.StartedAt,
                submittedAt = x.Attempt.FinishedAt,
                status = x.Attempt.Status.ToString(),
                totalScore = x.Attempt.TotalScore,
                maxScore = x.MaxScore
            })
            .ToListAsync(ct);

        return Ok(attempts);
    }

    /// <summary>
    /// Returns all attempts the current student has made on a quiz.
    /// </summary>
    [HttpGet("{id:guid}/my-attempts")]
    [Authorize(Policy = "Student")]
    public async Task<IActionResult> GetMyAttempts(Guid id, CancellationToken ct)
    {
        var studentProfileId = GetStudentProfileId();
        if (studentProfileId == Guid.Empty) return Forbid();

        var attempts = await _db.QuizAttempts
            .AsNoTracking()
            .Where(a => a.QuizId == id && a.StudentProfileId == studentProfileId)
            .Join(
                _db.Quizzes.AsNoTracking(),
                a => a.QuizId,
                q => q.Id,
                (a, q) => new
                {
                    Attempt = a,
                    QuizTitle = q.Title,
                    MaxScore = (int)q.Questions.Sum(qq => qq.Marks)
                })
            .OrderByDescending(x => x.Attempt.StartedAt)
            .Select(x => new
            {
                attemptId = x.Attempt.Id,
                quizId = x.Attempt.QuizId,
                quizTitle = x.QuizTitle,
                startedAt = x.Attempt.StartedAt,
                submittedAt = x.Attempt.FinishedAt,
                status = x.Attempt.Status.ToString(),
                totalScore = x.Attempt.TotalScore,
                maxScore = x.MaxScore
            })
            .ToListAsync(ct);

        return Ok(attempts);
    }

    /// <summary>
    /// Returns full attempt detail including answers.
    /// Faculty/Admin can view any attempt; students can only view their own.
    /// </summary>
    [HttpGet("attempt/{attemptId:guid}")]
    public async Task<IActionResult> GetAttemptDetail(Guid attemptId, CancellationToken ct)
    {
        var result = await _quizService.GetAttemptDetailAsync(attemptId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Awards marks for a ShortAnswer response (faculty manual grading).
    /// </summary>
    [HttpPost("attempt/grade-answer")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> GradeAnswer([FromBody] GradeAnswerRequest request, CancellationToken ct)
    {
        var ok = await _quizService.GradeAnswerAsync(request, ct);
        return ok ? NoContent() : NotFound();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Extracts the authenticated user ID from the JWT NameIdentifier claim.</summary>
    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    /// <summary>Extracts the student profile ID from the "studentProfileId" JWT claim.</summary>
    private Guid GetStudentProfileId()
    {
        var value = User.FindFirstValue("studentProfileId");
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    private async Task<(Guid? TenantId, Guid? CampusId, IActionResult? Error)> ResolveEffectiveScopeAsync(
        Guid? requestedTenantId,
        Guid? requestedCampusId,
        Guid? requestedCourseOfferingId,
        Guid? requestedQuizId,
        CancellationToken ct)
    {
        Guid? effectiveTenantId = requestedTenantId;
        Guid? effectiveCampusId = requestedCampusId;

        if (!_accessScope.IsSuperAdmin())
        {
            var callerTenantId = _accessScope.GetTenantId();
            var callerCampusId = _accessScope.GetCampusId();

            if (callerTenantId.HasValue)
            {
                if (requestedTenantId.HasValue && requestedTenantId.Value != callerTenantId.Value)
                    return (null, null, Forbid());

                effectiveTenantId = callerTenantId.Value;
            }

            if (callerCampusId.HasValue)
            {
                if (requestedCampusId.HasValue && requestedCampusId.Value != callerCampusId.Value)
                    return (null, null, Forbid());

                effectiveCampusId = callerCampusId.Value;
            }
        }

        if (effectiveTenantId.HasValue != effectiveCampusId.HasValue)
            return (null, null, BadRequest(new { message = "TenantId and CampusId must be provided together." }));

        if (requestedCourseOfferingId.HasValue)
        {
            var offeringScope = await _db.CourseOfferings
                .AsNoTracking()
                .Where(o => o.Id == requestedCourseOfferingId.Value)
                .Select(o => new { o.TenantId, o.CampusId })
                .FirstOrDefaultAsync(ct);

            if (offeringScope is null)
                return (null, null, NotFound(new { message = $"Course offering {requestedCourseOfferingId.Value} not found." }));

            if (effectiveTenantId.HasValue && offeringScope.TenantId != effectiveTenantId.Value)
                return (null, null, Forbid());

            if (effectiveCampusId.HasValue && offeringScope.CampusId != effectiveCampusId.Value)
                return (null, null, Forbid());

            effectiveTenantId ??= offeringScope.TenantId;
            effectiveCampusId ??= offeringScope.CampusId;
        }

        if (requestedQuizId.HasValue)
        {
            var quizScope = await _db.Quizzes
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(q => q.Id == requestedQuizId.Value)
                .Join(
                    _db.CourseOfferings.AsNoTracking(),
                    q => q.CourseOfferingId,
                    o => o.Id,
                    (_, o) => new { o.TenantId, o.CampusId })
                .FirstOrDefaultAsync(ct);

            if (quizScope is null)
                return (null, null, NotFound(new { message = $"Quiz {requestedQuizId.Value} not found." }));

            if (effectiveTenantId.HasValue && quizScope.TenantId != effectiveTenantId.Value)
                return (null, null, Forbid());

            if (effectiveCampusId.HasValue && quizScope.CampusId != effectiveCampusId.Value)
                return (null, null, Forbid());

            effectiveTenantId ??= quizScope.TenantId;
            effectiveCampusId ??= quizScope.CampusId;
        }

        return (effectiveTenantId, effectiveCampusId, null);
    }
}
