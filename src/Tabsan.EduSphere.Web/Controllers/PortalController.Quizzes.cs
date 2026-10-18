using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.DTOs.Quizzes;
using Tabsan.EduSphere.Application.Quizzes;
using Tabsan.EduSphere.Web.Models.Portal;

namespace Tabsan.EduSphere.Web.Controllers;

/// <summary>
/// Plan M2 — quiz engine pages: builder (questions, settings, bank groups, accommodations,
/// overrides), question banks, take-quiz, review, grading and statistics.
/// The API enforces every rule; these actions only translate forms and show its messages.
/// </summary>
public partial class PortalController
{
    private static readonly JsonSerializerOptions QuizFormJson = new(JsonSerializerDefaults.Web);

    private IActionResult? RequireQuizStaff()
        => CanManageQuizzes(_api.GetSessionIdentity()) ? null : AccessDenied("Quiz management is available to Faculty, Admin and SuperAdmin.");

    private IActionResult? RequireStudent()
        => _api.GetSessionIdentity()?.IsStudent == true ? null : AccessDenied("Taking quizzes is available to students.");

    private static List<AddOptionRequest> ParseOptions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            var items = JsonSerializer.Deserialize<List<OptionForm>>(json, QuizFormJson) ?? new();
            return items.Where(o => !string.IsNullOrWhiteSpace(o.Text))
                .Select((o, i) => new AddOptionRequest(o.Text!.Trim(), o.IsCorrect, i + 1)).ToList();
        }
        catch (JsonException) { return new(); }
    }

    private sealed record OptionForm(string? Text, bool IsCorrect);

    // ═════════════════════════════════════════════════════════════════════════
    // Builder
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> QuizBuilder(Guid quizId, Guid? copyBankId, CancellationToken ct)
    {
        ViewData["Title"] = "Quiz builder";
        if (RequireQuizStaff() is { } denied) return denied;

        var model = new QuizBuilderPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        try
        {
            model.Quiz = await _api.GetQuizBuilderAsync(quizId, ct);
            if (model.Quiz is null) return RedirectToAction(nameof(Quizzes));
            model.Banks = await _api.GetQuestionBanksAsync(model.Quiz.CourseOfferingId, ct);
            if (copyBankId.HasValue) model.CopyBank = await _api.GetQuestionBankAsync(copyBankId.Value, ct);
            var identity = _api.GetSessionIdentity();
            model.Roster = await _api.GetEnrollmentRosterAsync(model.Quiz.CourseOfferingId, identity?.TenantId, identity?.CampusId, ct);
            model.Accommodations = await _api.GetQuizAccommodationsAsync(model.Quiz.CourseOfferingId, ct);
            model.Overrides = await _api.GetQuizOverridesAsync(quizId, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    private IActionResult BackToBuilder(Guid quizId, string? message = null, Guid? copyBankId = null, string? anchor = null)
    {
        if (message is not null) TempData["PortalMessage"] = message;
        var url = Url.Action(nameof(QuizBuilder), new { quizId, copyBankId })!;
        return Redirect(anchor is null ? url : url + "#" + anchor);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveQuizSettings(Guid quizId, bool shuffleQuestions, bool shuffleOptions, bool oneQuestionAtATime,
        bool lockAnsweredQuestions, string? accessCode, string? allowedIpRanges, string reviewVisibility, bool reviewAfterCloseOnly, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        try
        {
            await _api.SaveQuizSettingsAsync(quizId, new QuizDeliverySettingsRequest(shuffleQuestions, shuffleOptions, oneQuestionAtATime,
                lockAnsweredQuestions, accessCode, allowedIpRanges, reviewVisibility, reviewAfterCloseOnly), ct);
            return BackToBuilder(quizId, "Settings saved.", anchor: "settings");
        }
        catch (Exception ex) { return BackToBuilder(quizId, ex.Message, anchor: "settings"); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuizQuestion(Guid quizId, QuizQuestionFormModel form, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        try
        {
            await _api.AddQuizQuestionAsync(new AddQuestionRequest(quizId, form.Text, form.Type, form.Marks, form.OrderIndex,
                ParseOptions(form.OptionsJson), form.ConfigJson), ct);
            return BackToBuilder(quizId, "Question added.", anchor: "questions");
        }
        catch (Exception ex) { return BackToBuilder(quizId, ex.Message, anchor: "add-question"); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveQuizQuestion(Guid quizId, Guid questionId, QuizQuestionFormModel form, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        try
        {
            await _api.SaveQuizQuestionAsync(questionId, new SaveQuestionRequest(form.Text, form.Marks, form.OrderIndex,
                ParseOptions(form.OptionsJson), form.ConfigJson, form.Tags, form.Difficulty), ct);
            return BackToBuilder(quizId, "Question saved.", anchor: $"q-{questionId}");
        }
        catch (Exception ex) { return BackToBuilder(quizId, ex.Message, anchor: $"q-{questionId}"); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuizQuestion(Guid quizId, Guid questionId, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        try
        {
            await _api.DeleteQuizQuestionAsync(questionId, ct);
            return BackToBuilder(quizId, "Question removed.", anchor: "questions");
        }
        catch (Exception ex) { return BackToBuilder(quizId, ex.Message, anchor: $"q-{questionId}"); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuizGroup(Guid quizId, Guid questionBankId, string? title, int pickCount, decimal marksPerQuestion, int orderIndex, string? tagFilter, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        try
        {
            await _api.AddQuizGroupAsync(quizId, new SaveQuestionGroupRequest(questionBankId, title ?? "Random questions", pickCount, marksPerQuestion, orderIndex, tagFilter), ct);
            return BackToBuilder(quizId, "Question group added.", anchor: "questions");
        }
        catch (Exception ex) { return BackToBuilder(quizId, ex.Message, anchor: "banks"); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuizGroup(Guid quizId, Guid groupId, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        try
        {
            await _api.DeleteQuizGroupAsync(groupId, ct);
            return BackToBuilder(quizId, "Question group removed.", anchor: "questions");
        }
        catch (Exception ex) { return BackToBuilder(quizId, ex.Message, anchor: "questions"); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CopyBankQuestions(Guid quizId, Guid bankId, List<Guid> questionIds, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        try
        {
            var copied = await _api.CopyBankQuestionsAsync(quizId, questionIds ?? new(), ct);
            return BackToBuilder(quizId, $"{copied} question(s) copied into the quiz.", anchor: "questions");
        }
        catch (Exception ex) { return BackToBuilder(quizId, ex.Message, bankId, "banks"); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveQuizAccommodation(Guid quizId, Guid courseOfferingId, Guid studentProfileId, bool allQuizzes,
        int extraTimeMinutes, decimal timeMultiplier, int extraAttempts, string? note, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        try
        {
            await _api.SaveQuizAccommodationAsync(new SaveAccommodationRequest(courseOfferingId, allQuizzes ? null : quizId, studentProfileId,
                extraTimeMinutes, timeMultiplier <= 0 ? 1 : timeMultiplier, extraAttempts, note), ct);
            return BackToBuilder(quizId, "Accommodation saved.", anchor: "accommodations");
        }
        catch (Exception ex) { return BackToBuilder(quizId, ex.Message, anchor: "accommodations"); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuizAccommodation(Guid quizId, Guid id, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        try { await _api.DeleteQuizAccommodationAsync(id, ct); return BackToBuilder(quizId, "Accommodation removed.", anchor: "accommodations"); }
        catch (Exception ex) { return BackToBuilder(quizId, ex.Message, anchor: "accommodations"); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveQuizOverride(Guid quizId, Guid studentProfileId, DateTime? availableFrom, DateTime? availableUntil, string? note, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        try
        {
            // The form posts local date-times; the API stores UTC.
            await _api.SaveQuizOverrideAsync(quizId, new SaveAvailabilityOverrideRequest(studentProfileId,
                availableFrom?.ToUniversalTime(), availableUntil?.ToUniversalTime(), note), ct);
            return BackToBuilder(quizId, "Availability override saved.", anchor: "accommodations");
        }
        catch (Exception ex) { return BackToBuilder(quizId, ex.Message, anchor: "accommodations"); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuizOverride(Guid quizId, Guid id, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        try { await _api.DeleteQuizOverrideAsync(id, ct); return BackToBuilder(quizId, "Override removed.", anchor: "accommodations"); }
        catch (Exception ex) { return BackToBuilder(quizId, ex.Message, anchor: "accommodations"); }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Question banks
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> QuestionBanks(Guid? offeringId, Guid? bankId, CancellationToken ct)
    {
        ViewData["Title"] = "Question banks";
        if (RequireQuizStaff() is { } denied) return denied;

        var model = new QuestionBanksPageModel
        {
            IsConnected = _api.IsConnected(),
            OfferingId = offeringId,
            Message = TempData["PortalMessage"]?.ToString(),
            ImportErrors = TempData["BankImportErrors"] is string errs ? JsonSerializer.Deserialize<List<string>>(errs) : null
        };
        if (!model.IsConnected) return View(model);
        try
        {
            model.Banks = await _api.GetQuestionBanksAsync(offeringId, ct);
            // The course filter is a convenience; the banks still work without it.
            try { model.Offerings = await GetOfferingFilterOptionsAsync(_api.GetSessionIdentity(), ct); }
            catch (Exception) { model.Offerings = new(); }
            if (bankId.HasValue) model.SelectedBank = await _api.GetQuestionBankAsync(bankId.Value, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    private IActionResult BackToBanks(Guid? offeringId, Guid? bankId, string? message)
    {
        if (message is not null) TempData["PortalMessage"] = message;
        return RedirectToAction(nameof(QuestionBanks), new { offeringId, bankId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateQuestionBank(string title, string? description, Guid? courseOfferingId, bool isShared, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        try
        {
            var id = await _api.CreateQuestionBankAsync(new SaveBankRequest(title, description, courseOfferingId, null, isShared), ct);
            return BackToBanks(courseOfferingId, id, "Question bank created.");
        }
        catch (Exception ex) { return BackToBanks(courseOfferingId, null, ex.Message); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestionBank(Guid bankId, Guid? offeringId, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        try { await _api.DeleteQuestionBankAsync(bankId, ct); return BackToBanks(offeringId, null, "Question bank deleted."); }
        catch (Exception ex) { return BackToBanks(offeringId, bankId, ex.Message); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBankQuestion(Guid bankId, Guid? offeringId, QuizQuestionFormModel form, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        try
        {
            await _api.AddBankQuestionAsync(bankId, new AddBankQuestionRequest(form.Text, form.Type, form.Marks, ParseOptions(form.OptionsJson),
                form.ConfigJson, form.Tags, form.Difficulty), ct);
            return BackToBanks(offeringId, bankId, "Question added to the bank.");
        }
        catch (Exception ex) { return BackToBanks(offeringId, bankId, ex.Message); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBankQuestion(Guid bankId, Guid? offeringId, Guid questionId, QuizQuestionFormModel form, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        try
        {
            await _api.SaveQuizQuestionAsync(questionId, new SaveQuestionRequest(form.Text, form.Marks, form.OrderIndex,
                ParseOptions(form.OptionsJson), form.ConfigJson, form.Tags, form.Difficulty), ct);
            return BackToBanks(offeringId, bankId, "Question saved.");
        }
        catch (Exception ex) { return BackToBanks(offeringId, bankId, ex.Message); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBankQuestion(Guid bankId, Guid? offeringId, Guid questionId, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        try { await _api.DeleteQuizQuestionAsync(questionId, ct); return BackToBanks(offeringId, bankId, "Question removed from the bank."); }
        catch (Exception ex) { return BackToBanks(offeringId, bankId, ex.Message); }
    }

    [HttpGet]
    public async Task<IActionResult> ExportQuestionBank(Guid bankId, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        var bytes = await _api.ExportQuestionBankAsync(bankId, ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"question-bank-{bankId:N}.xlsx");
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ImportQuestionBank(Guid bankId, Guid? offeringId, IFormFile? workbook, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        if (workbook is not { Length: > 0 }) return BackToBanks(offeringId, bankId, "Choose an .xlsx file to import.");
        try
        {
            using var buffer = new MemoryStream();
            await workbook.CopyToAsync(buffer, ct);
            var result = await _api.ImportQuestionBankAsync(bankId, buffer.ToArray(), workbook.FileName, ct);
            if (result.Errors.Count > 0) TempData["BankImportErrors"] = JsonSerializer.Serialize(result.Errors.Take(50).ToList());
            return BackToBanks(offeringId, bankId, $"{result.Imported} question(s) imported{(result.Errors.Count > 0 ? $", {result.Errors.Count} row(s) skipped" : string.Empty)}.");
        }
        catch (Exception ex) { return BackToBanks(offeringId, bankId, ex.Message); }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Student: take a quiz
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> TakeQuiz(Guid quizId, CancellationToken ct)
    {
        ViewData["Title"] = "Quiz";
        if (RequireStudent() is { } denied) return denied;

        var model = new TakeQuizPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        try
        {
            model.Info = await _api.GetQuizInfoAsync(quizId, ct);
            if (model.Info is null) { TempData["PortalMessage"] = "This quiz is not available."; return RedirectToAction(nameof(ViewQuizzes)); }
            model.Attempts = await _api.GetMyAttemptsByQuizAsync(quizId, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StartQuiz(Guid quizId, string? accessCode, CancellationToken ct)
    {
        if (RequireStudent() is { } denied) return denied;
        try
        {
            var outcome = await _api.StartQuizAsync(quizId, accessCode, ct);
            if (outcome.Attempt is not null)
                return RedirectToAction(nameof(TakeQuizAttempt), new { attemptId = outcome.Attempt.AttemptId });
            TempData["PortalMessage"] = outcome.Message;
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(TakeQuiz), new { quizId });
    }

    [HttpGet]
    public async Task<IActionResult> TakeQuizAttempt(Guid attemptId, CancellationToken ct)
    {
        ViewData["Title"] = "Quiz";
        if (RequireStudent() is { } denied) return denied;
        try
        {
            var paper = await _api.GetQuizPaperAsync(attemptId, ct);
            if (paper is null)
            {
                // Closed (submitted or time ran out) — show the review instead.
                return RedirectToAction(nameof(QuizReview), new { attemptId });
            }
            ViewData["Title"] = paper.QuizTitle;
            return View(new QuizPaperPageModel { Paper = paper });
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
            return RedirectToAction(nameof(ViewQuizzes));
        }
    }

    /// <summary>Autosave from the paper page (JSON body; antiforgery token in the RequestVerificationToken header).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveQuizAnswers(Guid attemptId, [FromBody] List<SubmitAnswerEntry> answers, CancellationToken ct)
    {
        if (RequireStudent() is not null) return Forbid();
        var outcome = await _api.SaveQuizAnswersAsync(attemptId, answers ?? new(), ct);
        return Json(new { ok = outcome.Ok, saved = outcome.Saved, lockedSkipped = outcome.LockedSkipped, error = outcome.ErrorCode, message = outcome.Message });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(55 * 1024 * 1024)]
    public async Task<IActionResult> UploadQuizAnswerFile(Guid attemptId, Guid questionId, IFormFile? file, CancellationToken ct)
    {
        if (RequireStudent() is not null) return Forbid();
        if (file is not { Length: > 0 }) return Json(new { ok = false, message = "Choose a file to upload." });
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        var outcome = await _api.UploadQuizAnswerFileAsync(attemptId, questionId, buffer.ToArray(), Path.GetFileName(file.FileName), file.ContentType, ct);
        return Json(new { ok = outcome.Ok, fileName = Path.GetFileName(file.FileName), error = outcome.ErrorCode, message = outcome.Message });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitQuiz(Guid attemptId, string? answersJson, CancellationToken ct)
    {
        if (RequireStudent() is { } denied) return denied;
        try
        {
            var answers = string.IsNullOrWhiteSpace(answersJson)
                ? new List<SubmitAnswerEntry>()
                : JsonSerializer.Deserialize<List<SubmitAnswerEntry>>(answersJson, QuizFormJson) ?? new();
            await _api.SubmitQuizAsync(attemptId, answers, ct);
            TempData["PortalMessage"] = "Your quiz has been submitted.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(QuizReview), new { attemptId });
    }

    [HttpGet]
    public async Task<IActionResult> QuizReview(Guid attemptId, Guid? quizId, CancellationToken ct)
    {
        ViewData["Title"] = "Quiz review";
        var identity = _api.GetSessionIdentity();
        var model = new QuizReviewPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString(), QuizId = quizId };
        if (!model.IsConnected) return View(model);
        try
        {
            if (identity?.IsStudent == true)
            {
                model.Review = await _api.GetQuizReviewAsync(attemptId, ct);
            }
            else if (CanManageQuizzes(identity))
            {
                model.Review = await _api.GetQuizAttemptDetailAsync(attemptId, ct);
                model.IsFacultyView = true;
            }
            else return AccessDenied();

            if (model.Review is null) model.Message ??= "This attempt is not available.";
            else model.QuizTitle = (await _api.GetQuizInfoAsync(model.Review.QuizId, ct))?.Title;
            model.QuizId ??= model.Review?.QuizId;
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Faculty: grading & statistics
    // ═════════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> QuizGrading(Guid quizId, CancellationToken ct)
    {
        ViewData["Title"] = "Attempts & grading";
        if (RequireQuizStaff() is { } denied) return denied;

        var model = new QuizGradingPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        try
        {
            model.Quiz = await _api.GetQuizBuilderAsync(quizId, ct);
            model.Queue = await _api.GetQuizGradingQueueAsync(quizId, ct);
            model.Attempts = await _api.GetQuizAttemptsAsync(quizId, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GradeQuizAnswer(Guid quizId, Guid answerId, decimal marksAwarded, string? feedback, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        try { await _api.GradeQuizAnswerAsync(answerId, marksAwarded, feedback, ct); TempData["PortalMessage"] = "Marks saved."; }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(QuizGrading), new { quizId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegradeQuizQuestion(Guid quizId, Guid questionId, string mode, string? returnTo, CancellationToken ct)
    {
        if (RequireQuizStaff() is { } denied) return denied;
        try
        {
            var updated = await _api.RegradeQuizQuestionAsync(quizId, questionId, mode, ct);
            TempData["PortalMessage"] = $"Regraded {updated} attempt(s).";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return returnTo == "stats"
            ? RedirectToAction(nameof(QuizStatistics), new { quizId })
            : BackToBuilder(quizId, null, anchor: $"q-{questionId}");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadQuizAnswerFile(Guid answerId, CancellationToken ct)
    {
        var file = await _api.DownloadQuizAnswerFileAsync(answerId, ct);
        return file is null ? NotFound() : File(file.Value.Content, file.Value.ContentType, file.Value.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> QuizStatistics(Guid quizId, CancellationToken ct)
    {
        ViewData["Title"] = "Quiz statistics";
        if (RequireQuizStaff() is { } denied) return denied;

        var model = new QuizStatisticsPageModel { IsConnected = _api.IsConnected(), Message = TempData["PortalMessage"]?.ToString() };
        if (!model.IsConnected) return View(model);
        try
        {
            model.Quiz = await _api.GetQuizBuilderAsync(quizId, ct);
            model.Stats = await _api.GetQuizStatisticsAsync(quizId, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }
}
