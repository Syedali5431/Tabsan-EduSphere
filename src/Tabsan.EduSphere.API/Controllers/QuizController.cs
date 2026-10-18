using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.DTOs.Quizzes;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Manages quiz authoring, publishing, attempt lifecycle, grading and analysis.
/// Plan M2: question types, bank groups, randomisation, accommodations, security, review options,
/// manual grading queue, regrade and item analysis. Students only ever receive their own attempts
/// and never receive correct answers before review allows it.
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

    /// <summary>Creates a new unpublished quiz for a course offering.</summary>
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

    /// <summary>Updates the metadata of a quiz.</summary>
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

    /// <summary>Plan M2: delivery, security and review settings.</summary>
    [HttpPut("{id:guid}/settings")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> UpdateSettings(Guid id, [FromBody] QuizDeliverySettingsRequest request, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(null, null, null, id, ct);
        if (scope.Error is not null) return scope.Error;

        var error = await _quizService.ConfigureDeliveryAsync(id, request, ct);
        return error is null ? NoContent() : BadRequest(new { message = error });
    }

    /// <summary>Publishes a quiz, making it visible and accessible to students.</summary>
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

    /// <summary>Unpublishes a quiz, hiding it from students without deleting it.</summary>
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

    /// <summary>Soft-deletes a quiz. Accessible by Admin only.</summary>
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

    /// <summary>Adds a validated question (with options / type settings) to a quiz.</summary>
    [HttpPost("question")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> AddQuestion([FromBody] AddQuestionRequest request, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(null, null, null, request.QuizId, ct);
        if (scope.Error is not null) return scope.Error;

        var result = await _quizService.AddQuestionAsync(request, ct);
        return result.Error is null ? Ok(new { questionId = result.QuestionId }) : BadRequest(new { message = result.Error });
    }

    /// <summary>Updates an existing question's text, marks, and ordering.</summary>
    [HttpPut("question/{questionId:guid}")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> UpdateQuestion(Guid questionId, [FromBody] UpdateQuestionRequest request, CancellationToken ct)
    {
        var denied = await AuthorizeQuestionAsync(questionId, ct);
        if (denied is not null) return denied;

        var ok = await _quizService.UpdateQuestionAsync(questionId, request, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>Plan M2: full question edit (quiz or bank question).</summary>
    [HttpPut("question/{questionId:guid}/full")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> SaveQuestion(Guid questionId, [FromBody] SaveQuestionRequest request, CancellationToken ct)
    {
        var denied = await AuthorizeQuestionAsync(questionId, ct);
        if (denied is not null) return denied;

        var result = await _quizService.SaveQuestionAsync(questionId, request, ct);
        return result.Error is null ? NoContent() : BadRequest(new { message = result.Error });
    }

    /// <summary>Removes a question (bank questions with answers are archived instead).</summary>
    [HttpDelete("question/{questionId:guid}")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> RemoveQuestion(Guid questionId, CancellationToken ct)
    {
        var denied = await AuthorizeQuestionAsync(questionId, ct);
        if (denied is not null) return denied;

        var (found, error) = await _quizService.RemoveQuestionAsync(questionId, ct);
        if (!found) return NotFound();
        return error is null ? NoContent() : Conflict(new { message = error });
    }

    // ── Plan M2.2: bank groups ────────────────────────────────────────────────

    [HttpPost("{id:guid}/groups")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> AddGroup(Guid id, [FromBody] SaveQuestionGroupRequest request, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(null, null, null, id, ct);
        if (scope.Error is not null) return scope.Error;
        var denied = await AuthorizeBankReadAsync(request.QuestionBankId, ct);
        if (denied is not null) return denied;

        var result = await _quizService.AddGroupAsync(id, request, ct);
        return result.Error is null ? Ok(new { groupId = result.QuestionId }) : BadRequest(new { message = result.Error });
    }

    [HttpPut("groups/{groupId:guid}")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> UpdateGroup(Guid groupId, [FromBody] SaveQuestionGroupRequest request, CancellationToken ct)
    {
        var quizId = await _db.QuizQuestionGroups.Where(g => g.Id == groupId).Select(g => (Guid?)g.QuizId).FirstOrDefaultAsync(ct);
        if (quizId is null) return NotFound();
        var scope = await ResolveEffectiveScopeAsync(null, null, null, quizId, ct);
        if (scope.Error is not null) return scope.Error;

        var result = await _quizService.UpdateGroupAsync(groupId, request, ct);
        return result.Error is null ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpDelete("groups/{groupId:guid}")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> RemoveGroup(Guid groupId, CancellationToken ct)
    {
        var quizId = await _db.QuizQuestionGroups.Where(g => g.Id == groupId).Select(g => (Guid?)g.QuizId).FirstOrDefaultAsync(ct);
        if (quizId is null) return NotFound();
        var scope = await ResolveEffectiveScopeAsync(null, null, null, quizId, ct);
        if (scope.Error is not null) return scope.Error;

        return await _quizService.RemoveGroupAsync(groupId, ct) ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/copy-from-bank")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> CopyFromBank(Guid id, [FromBody] CopyBankQuestionsRequest request, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(null, null, null, id, ct);
        if (scope.Error is not null) return scope.Error;

        var bankIds = await _db.QuizQuestions.Where(q => request.QuestionIds.Contains(q.Id) && q.QuestionBankId != null)
            .Select(q => q.QuestionBankId!.Value).Distinct().ToListAsync(ct);
        foreach (var bankId in bankIds)
        {
            var denied = await AuthorizeBankReadAsync(bankId, ct);
            if (denied is not null) return denied;
        }

        var (copied, error) = await _quizService.CopyFromBankAsync(id, request, ct);
        return error is null ? Ok(new { copied }) : BadRequest(new { message = error });
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Returns all active quizzes for a course offering.</summary>
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

        var canIncludeInactive = includeInactive && IsStaff();
        return Ok(await _quizService.GetByOfferingAsync(courseOfferingId, canIncludeInactive, ct));
    }

    /// <summary>
    /// Faculty/Admin: full quiz detail with correct answers and settings.
    /// Students: only the quiz information (no questions, no answers) — questions come with an attempt.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id, [FromQuery] Guid? tenantId, [FromQuery] Guid? campusId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(tenantId, campusId, null, id, ct);
        if (scope.Error is not null)
            return scope.Error;

        if (!IsStaff())
        {
            var info = await _quizService.GetStudentInfoAsync(id, ct);
            return info is null ? NotFound() : Ok(info);
        }

        var result = await _quizService.GetDetailAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Plan M2: what a student sees before starting (all roles).</summary>
    [HttpGet("{id:guid}/info")]
    public async Task<IActionResult> GetInfo(Guid id, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(null, null, null, id, ct);
        if (scope.Error is not null) return scope.Error;
        var info = await _quizService.GetStudentInfoAsync(id, ct);
        return info is null ? NotFound() : Ok(info);
    }

    // ── Attempts (student) ────────────────────────────────────────────────────

    /// <summary>
    /// Opens (or resumes) the current student's attempt. Returns 409 with an error code when the quiz
    /// is closed, attempts are used up, or the access code / network is not accepted.
    /// </summary>
    [HttpPost("{id:guid}/start")]
    [Authorize(Policy = "Student")]
    public async Task<IActionResult> StartAttempt(Guid id, [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] StartAttemptRequest? request, CancellationToken ct)
    {
        var studentProfileId = GetStudentProfileId();
        if (studentProfileId == Guid.Empty) return Forbid();

        var result = await _quizService.StartAttemptAsync(id, studentProfileId, request?.AccessCode, ClientIp(), ct);
        return result.Attempt is not null
            ? Ok(result.Attempt)
            : Conflict(new { error = result.ErrorCode, message = result.Message });
    }

    /// <summary>Plan M2: the student's paper (questions without answers, saved responses, deadline).</summary>
    [HttpGet("attempt/{attemptId:guid}/paper")]
    [Authorize(Policy = "Student")]
    public async Task<IActionResult> GetPaper(Guid attemptId, CancellationToken ct)
    {
        var studentProfileId = GetStudentProfileId();
        if (studentProfileId == Guid.Empty) return Forbid();

        var (paper, error) = await _quizService.GetPaperAsync(attemptId, studentProfileId, ct);
        return paper is not null ? Ok(paper) : AttemptError(error);
    }

    /// <summary>Plan M2: save answers without submitting (autosave, one question at a time).</summary>
    [HttpPost("attempt/{attemptId:guid}/answers")]
    [Authorize(Policy = "Student")]
    public async Task<IActionResult> SaveAnswers(Guid attemptId, [FromBody] SaveAnswersRequest request, CancellationToken ct)
    {
        var studentProfileId = GetStudentProfileId();
        if (studentProfileId == Guid.Empty) return Forbid();

        var (saved, locked, error) = await _quizService.SaveAnswersAsync(attemptId, studentProfileId, request, ct);
        return error is null ? Ok(new { saved, lockedSkipped = locked }) : AttemptError(error);
    }

    /// <summary>Plan M2: upload the file for a FileUpload question.</summary>
    [HttpPost("attempt/{attemptId:guid}/files/{questionId:guid}")]
    [Authorize(Policy = "Student")]
    [RequestSizeLimit(55 * 1024 * 1024)]
    public async Task<IActionResult> UploadAnswerFile(Guid attemptId, Guid questionId, IFormFile file, CancellationToken ct)
    {
        var studentProfileId = GetStudentProfileId();
        if (studentProfileId == Guid.Empty) return Forbid();
        if (file is null || file.Length == 0) return BadRequest(new { message = "Choose a file to upload." });

        await using var stream = file.OpenReadStream();
        var (_, error) = await _quizService.SaveFileAnswerAsync(attemptId, studentProfileId, questionId, file.FileName, file.ContentType, file.Length, stream, ct);
        return error is null ? Ok(new { fileName = file.FileName }) : AttemptError(error);
    }

    /// <summary>Submits the current student's attempt (answers in the body are saved first).</summary>
    [HttpPost("attempt/submit")]
    [Authorize(Policy = "Student")]
    public async Task<IActionResult> SubmitAttempt([FromBody] SubmitAttemptRequest request, CancellationToken ct)
    {
        var studentProfileId = GetStudentProfileId();
        if (studentProfileId == Guid.Empty) return Forbid();

        var (review, error) = await _quizService.SubmitAttemptAsync(request, studentProfileId, ct);
        return review is not null ? Ok(review) : AttemptError(error);
    }

    /// <summary>Plan M2.6: the student's review, as far as the quiz's review option allows.</summary>
    [HttpGet("attempt/{attemptId:guid}/review")]
    [Authorize(Policy = "Student")]
    public async Task<IActionResult> GetReview(Guid attemptId, CancellationToken ct)
    {
        var studentProfileId = GetStudentProfileId();
        if (studentProfileId == Guid.Empty) return Forbid();

        var (review, error) = await _quizService.GetStudentReviewAsync(attemptId, studentProfileId, ct);
        return review is not null ? Ok(review) : AttemptError(error);
    }

    /// <summary>Returns all attempts the current student has made across all quizzes (portal summary).</summary>
    [HttpGet("my-attempts")]
    [Authorize(Policy = "Student")]
    public async Task<IActionResult> GetAllMyAttempts(CancellationToken ct)
    {
        var studentProfileId = GetStudentProfileId();
        if (studentProfileId == Guid.Empty) return Forbid();
        return Ok(await AttemptSummariesAsync(a => a.StudentProfileId == studentProfileId, ct));
    }

    /// <summary>Returns all attempts the current student has made on a quiz.</summary>
    [HttpGet("{id:guid}/my-attempts")]
    [Authorize(Policy = "Student")]
    public async Task<IActionResult> GetMyAttempts(Guid id, CancellationToken ct)
    {
        var studentProfileId = GetStudentProfileId();
        if (studentProfileId == Guid.Empty) return Forbid();
        return Ok(await AttemptSummariesAsync(a => a.QuizId == id && a.StudentProfileId == studentProfileId, ct));
    }

    /// <summary>
    /// Attempt detail. Faculty/Admin (within scope) see everything; students only their own attempt,
    /// filtered by the quiz's review option.
    /// </summary>
    [HttpGet("attempt/{attemptId:guid}")]
    public async Task<IActionResult> GetAttemptDetail(Guid attemptId, CancellationToken ct)
    {
        if (!IsStaff())
        {
            var studentProfileId = GetStudentProfileId();
            if (studentProfileId == Guid.Empty) return Forbid();
            var (review, error) = await _quizService.GetStudentReviewAsync(attemptId, studentProfileId, ct);
            return review is not null ? Ok(review) : AttemptError(error);
        }

        var denied = await AuthorizeAttemptAsync(attemptId, ct);
        if (denied is not null) return denied;
        var result = await _quizService.GetAttemptDetailAsync(attemptId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // ── Grading & analysis (faculty) ──────────────────────────────────────────

    /// <summary>Plan M2: every attempt on a quiz (faculty).</summary>
    [HttpGet("{id:guid}/attempts")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> GetAttempts(Guid id, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(null, null, null, id, ct);
        if (scope.Error is not null) return scope.Error;

        var rows = await (from a in _db.QuizAttempts.AsNoTracking()
                          where a.QuizId == id
                          join sp in _db.StudentProfiles.IgnoreQueryFilters().AsNoTracking() on a.StudentProfileId equals sp.Id into sps
                          from sp in sps.DefaultIfEmpty()
                          join u in _db.Users.IgnoreQueryFilters().AsNoTracking() on sp.UserId equals u.Id into us
                          from u in us.DefaultIfEmpty()
                          orderby a.StartedAt descending
                          select new
                          {
                              attemptId = a.Id,
                              studentProfileId = a.StudentProfileId,
                              studentName = u == null ? null : (u.FullName ?? u.Username),
                              registrationNumber = sp == null ? null : sp.RegistrationNumber,
                              status = a.Status.ToString(),
                              startedAt = a.StartedAt,
                              finishedAt = a.FinishedAt,
                              totalScore = a.TotalScore
                          }).ToListAsync(ct);
        return Ok(rows);
    }

    /// <summary>Plan M2.6: answers waiting for manual grading.</summary>
    [HttpGet("{id:guid}/grading-queue")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> GetGradingQueue(Guid id, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(null, null, null, id, ct);
        if (scope.Error is not null) return scope.Error;
        return Ok(await _quizService.GetGradingQueueAsync(id, ct));
    }

    /// <summary>Awards marks (and optional feedback) for a response.</summary>
    [HttpPost("attempt/grade-answer")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> GradeAnswer([FromBody] GradeAnswerRequest request, CancellationToken ct)
    {
        var attemptId = await _db.QuizAnswers.Where(a => a.Id == request.AnswerId).Select(a => (Guid?)a.QuizAttemptId).FirstOrDefaultAsync(ct);
        if (attemptId is null) return NotFound();
        var denied = await AuthorizeAttemptAsync(attemptId.Value, ct);
        if (denied is not null) return denied;

        var (found, error) = await _quizService.GradeAnswerAsync(request, GetCurrentUserId(), ct);
        if (!found) return NotFound();
        return error is null ? NoContent() : BadRequest(new { message = error });
    }

    /// <summary>Plan M2.6: regrade a question for every finished attempt (Recalculate | FullCredit).</summary>
    [HttpPost("{id:guid}/questions/{questionId:guid}/regrade")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> Regrade(Guid id, Guid questionId, [FromBody] RegradeQuestionRequest request, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(null, null, null, id, ct);
        if (scope.Error is not null) return scope.Error;

        var result = await _quizService.RegradeQuestionAsync(id, questionId, request, ct);
        return result.Error is null ? Ok(new { attemptsUpdated = result.AttemptsUpdated }) : BadRequest(new { message = result.Error });
    }

    /// <summary>Plan M2.7: item analysis.</summary>
    [HttpGet("{id:guid}/statistics")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> GetStatistics(Guid id, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(null, null, null, id, ct);
        if (scope.Error is not null) return scope.Error;
        var stats = await _quizService.GetStatisticsAsync(id, ct);
        return stats is null ? NotFound() : Ok(stats);
    }

    /// <summary>Downloads a file answer (faculty within scope, or the student who uploaded it).</summary>
    [HttpGet("answer/{answerId:guid}/file")]
    public async Task<IActionResult> GetAnswerFile(Guid answerId, CancellationToken ct)
    {
        Guid? studentProfileId = null;
        if (IsStaff())
        {
            var attemptId = await _db.QuizAnswers.Where(a => a.Id == answerId).Select(a => (Guid?)a.QuizAttemptId).FirstOrDefaultAsync(ct);
            if (attemptId is null) return NotFound();
            var denied = await AuthorizeAttemptAsync(attemptId.Value, ct);
            if (denied is not null) return denied;
        }
        else
        {
            studentProfileId = GetStudentProfileId();
            if (studentProfileId == Guid.Empty) return Forbid();
        }

        var file = await _quizService.GetAnswerFileAsync(answerId, studentProfileId, ct);
        return file is null ? NotFound() : File(file.Value.Content, file.Value.ContentType, file.Value.FileName);
    }

    // ── Plan M2.5: accommodations & overrides ─────────────────────────────────

    [HttpGet("accommodations/by-offering/{courseOfferingId:guid}")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> GetAccommodations(Guid courseOfferingId, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(null, null, courseOfferingId, null, ct);
        if (scope.Error is not null) return scope.Error;
        return Ok(await _quizService.GetAccommodationsAsync(courseOfferingId, ct));
    }

    [HttpPost("accommodations")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> SaveAccommodation([FromBody] SaveAccommodationRequest request, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(null, null, request.CourseOfferingId, request.QuizId, ct);
        if (scope.Error is not null) return scope.Error;

        var (id, error) = await _quizService.SaveAccommodationAsync(request, ct);
        return error is null ? Ok(new { accommodationId = id }) : BadRequest(new { message = error });
    }

    [HttpDelete("accommodations/{accommodationId:guid}")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> RemoveAccommodation(Guid accommodationId, CancellationToken ct)
    {
        var offeringId = await _db.QuizAccommodations.Where(a => a.Id == accommodationId).Select(a => (Guid?)a.CourseOfferingId).FirstOrDefaultAsync(ct);
        if (offeringId is null) return NotFound();
        var scope = await ResolveEffectiveScopeAsync(null, null, offeringId, null, ct);
        if (scope.Error is not null) return scope.Error;
        return await _quizService.RemoveAccommodationAsync(accommodationId, ct) ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/overrides")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> GetOverrides(Guid id, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(null, null, null, id, ct);
        if (scope.Error is not null) return scope.Error;
        return Ok(await _quizService.GetOverridesAsync(id, ct));
    }

    [HttpPost("{id:guid}/overrides")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> SaveOverride(Guid id, [FromBody] SaveAvailabilityOverrideRequest request, CancellationToken ct)
    {
        var scope = await ResolveEffectiveScopeAsync(null, null, null, id, ct);
        if (scope.Error is not null) return scope.Error;

        var (overrideId, error) = await _quizService.SaveOverrideAsync(id, request, ct);
        return error is null ? Ok(new { overrideId }) : BadRequest(new { message = error });
    }

    [HttpDelete("overrides/{overrideId:guid}")]
    [Authorize(Policy = "Faculty")]
    public async Task<IActionResult> RemoveOverride(Guid overrideId, CancellationToken ct)
    {
        var quizId = await _db.QuizAvailabilityOverrides.Where(o => o.Id == overrideId).Select(o => (Guid?)o.QuizId).FirstOrDefaultAsync(ct);
        if (quizId is null) return NotFound();
        var scope = await ResolveEffectiveScopeAsync(null, null, null, quizId, ct);
        if (scope.Error is not null) return scope.Error;
        return await _quizService.RemoveOverrideAsync(overrideId, ct) ? NoContent() : NotFound();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool IsStaff() => User.IsInRole("Faculty") || User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private IActionResult AttemptError(string? error) => error switch
    {
        "NOT_FOUND" => NotFound(),
        "ATTEMPT_CLOSED" => Conflict(new { error, message = "This attempt has already been submitted." }),
        "TIME_EXPIRED" => Conflict(new { error, message = "Time is up — the attempt was submitted automatically with your saved answers." }),
        "IN_PROGRESS" => Conflict(new { error, message = "Finish the attempt before viewing the review." }),
        "LOCKED" => Conflict(new { error, message = "This answer is locked and cannot be changed." }),
        _ => BadRequest(new { message = error })
    };

    private async Task<object> AttemptSummariesAsync(System.Linq.Expressions.Expression<Func<Domain.Quizzes.QuizAttempt, bool>> filter, CancellationToken ct)
        => await _db.QuizAttempts
            .AsNoTracking()
            .Where(filter)
            .Join(
                _db.Quizzes.AsNoTracking(),
                a => a.QuizId,
                q => q.Id,
                (a, q) => new
                {
                    Attempt = a,
                    QuizTitle = q.Title,
                    // Snapshot marks when present (bank draws), else the quiz's questions.
                    MaxScore = a.Items.Any() ? a.Items.Sum(i => i.Marks) : q.Questions.Sum(qq => qq.Marks),
                    ReviewVisibility = q.ReviewVisibility
                })
            .OrderByDescending(x => x.Attempt.StartedAt)
            .Select(x => new
            {
                attemptId = x.Attempt.Id,
                quizId = x.Attempt.QuizId,
                quizTitle = x.QuizTitle,
                startedAt = x.Attempt.StartedAt,
                submittedAt = x.Attempt.FinishedAt,
                deadlineAt = x.Attempt.DeadlineAt,
                status = x.Attempt.Status.ToString(),
                // Students see their score only when the review option allows it.
                totalScore = x.ReviewVisibility == Domain.Quizzes.QuizReviewVisibility.None ? null : x.Attempt.TotalScore,
                maxScore = (int)x.MaxScore
            })
            .ToListAsync(ct);

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

    /// <summary>Scope check for a quiz question (via its quiz) or a bank question (via its bank).</summary>
    private async Task<IActionResult?> AuthorizeQuestionAsync(Guid questionId, CancellationToken ct)
    {
        var owner = await _db.QuizQuestions.IgnoreQueryFilters().Where(q => q.Id == questionId)
            .Select(q => new { q.QuizId, q.QuestionBankId }).FirstOrDefaultAsync(ct);
        if (owner is null) return NotFound();
        if (owner.QuizId.HasValue)
            return (await ResolveEffectiveScopeAsync(null, null, null, owner.QuizId, ct)).Error;
        return await AuthorizeBankWriteAsync(owner.QuestionBankId!.Value, ct);
    }

    private async Task<IActionResult?> AuthorizeAttemptAsync(Guid attemptId, CancellationToken ct)
    {
        var quizId = await _db.QuizAttempts.Where(a => a.Id == attemptId).Select(a => (Guid?)a.QuizId).FirstOrDefaultAsync(ct);
        if (quizId is null) return NotFound();
        return (await ResolveEffectiveScopeAsync(null, null, null, quizId, ct)).Error;
    }

    private async Task<IActionResult?> AuthorizeBankReadAsync(Guid bankId, CancellationToken ct)
    {
        var bank = await _db.QuestionBanks.AsNoTracking().Where(b => b.Id == bankId)
            .Select(b => new { b.TenantId }).FirstOrDefaultAsync(ct);
        if (bank is null) return NotFound(new { message = "Question bank not found." });
        if (_accessScope.IsSuperAdmin()) return null;
        var callerTenant = _accessScope.GetTenantId();
        return callerTenant.HasValue && bank.TenantId != callerTenant ? Forbid() : null;
    }

    private async Task<IActionResult?> AuthorizeBankWriteAsync(Guid bankId, CancellationToken ct)
    {
        var denied = await AuthorizeBankReadAsync(bankId, ct);
        if (denied is not null) return denied;
        if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin")) return null;
        var creator = await _db.QuestionBanks.AsNoTracking().Where(b => b.Id == bankId).Select(b => b.CreatedByUserId).FirstAsync(ct);
        var shared = await _db.QuestionBanks.AsNoTracking().Where(b => b.Id == bankId).Select(b => b.IsShared).FirstAsync(ct);
        return creator == GetCurrentUserId() || shared ? null : Forbid();
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
