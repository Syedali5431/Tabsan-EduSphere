using System.Security.Cryptography;
using System.Text;
using Tabsan.EduSphere.Application.DTOs.Quizzes;
using Tabsan.EduSphere.Application.Quizzes.Grading;
using Tabsan.EduSphere.Domain.Quizzes;

namespace Tabsan.EduSphere.Application.Quizzes;

/// <summary>Plan M2 delivery: starting attempts, the student paper, saving and submitting answers.</summary>
public sealed partial class QuizService
{
    private const int MaxTextLength = 4000;
    private const int MaxEssayLength = 50_000;
    private const int MaxResponseJsonLength = 20_000;

    /// <summary>One question of an attempt, whether from the snapshot or (legacy attempts) the quiz.</summary>
    private sealed record DeliveredItem(int Order, QuizQuestion Question, decimal Marks, string? PresentationJson, string? GeneratedValuesJson);

    // ── Start ─────────────────────────────────────────────────────────────────

    public async Task<StartAttemptResult> StartAttemptAsync(Guid quizId, Guid studentProfileId, string? accessCode, string? clientIp, CancellationToken ct = default)
    {
        var quiz = await _repo.GetForAuthoringAsync(quizId, ct);
        if (quiz is null || !quiz.IsPublished)
            return StartAttemptResult.Fail("UNAVAILABLE", "This quiz is not available.");

        var now = DateTime.UtcNow;
        var window = await _repo.GetOverrideForStudentAsync(quizId, studentProfileId, ct);
        var availableFrom = window?.AvailableFrom ?? quiz.AvailableFrom;
        var availableUntil = window?.AvailableUntil ?? quiz.AvailableUntil;
        if (availableFrom.HasValue && now < availableFrom) return StartAttemptResult.Fail("NOT_OPEN", "This quiz has not opened yet.");
        if (availableUntil.HasValue && now > availableUntil) return StartAttemptResult.Fail("CLOSED", "This quiz has closed.");

        if (!IsIpAllowed(quiz.AllowedIpRanges, clientIp))
            return StartAttemptResult.Fail("IP_NOT_ALLOWED", "This quiz can only be taken from an approved network.");

        // Resume an open attempt; finalise one whose time has run out.
        var inProgress = await _repo.GetInProgressAttemptAsync(quizId, studentProfileId, ct);
        if (inProgress is not null)
        {
            if (inProgress.IsOpenAt(now))
                return StartAttemptResult.Ok(ToAttemptResponse(inProgress)); // no access code needed to resume
            var expired = await _repo.GetAttemptForDeliveryAsync(inProgress.Id, ct);
            if (expired is not null) FinalizeAttempt(expired, timedOut: true);
            await _repo.SaveChangesAsync(ct);
            if (expired is not null) await SyncGradebookAsync(quiz.CourseOfferingId, [studentProfileId], ct);
        }

        if (quiz.AccessCode is not null && !FixedTimeEquals(quiz.AccessCode, accessCode?.Trim()))
            return StartAttemptResult.Fail(string.IsNullOrWhiteSpace(accessCode) ? "ACCESS_CODE_REQUIRED" : "ACCESS_CODE_INVALID",
                string.IsNullOrWhiteSpace(accessCode) ? "Enter the access code for this quiz." : "The access code is not correct.");

        var accommodation = await _repo.GetEffectiveAccommodationAsync(quiz.CourseOfferingId, quizId, studentProfileId, ct);
        if (quiz.MaxAttempts > 0)
        {
            var used = await _repo.GetAttemptCountAsync(quizId, studentProfileId, ct);
            if (used >= quiz.MaxAttempts + (accommodation?.ExtraAttempts ?? 0))
                return StartAttemptResult.Fail("NO_ATTEMPTS_LEFT", "You have used all your attempts for this quiz.");
        }

        var limit = accommodation is null ? quiz.TimeLimitMinutes : accommodation.ApplyTo(quiz.TimeLimitMinutes);
        DateTime? deadline = limit.HasValue ? now.AddMinutes(limit.Value) : null;
        if (availableUntil.HasValue)
            deadline = deadline is null || availableUntil < deadline ? availableUntil : deadline;

        var attempt = new QuizAttempt(quizId, studentProfileId, deadline, clientIp);
        // A quiz without questions can still be started and submitted (the portal's legacy
        // "submit a file" quizzes rely on this); it simply scores 0.
        var items = await BuildItemsAsync(quiz, attempt.Id, ct);

        await _repo.AddAttemptAsync(attempt, ct);
        await _repo.AddAttemptItemsAsync(items, ct);
        await _repo.SaveChangesAsync(ct);
        return StartAttemptResult.Ok(ToAttemptResponse(attempt));
    }

    /// <summary>Plan M2.2/M2.4: the quiz's own questions plus bank draws, in order (shuffled if configured).</summary>
    private async Task<List<QuizAttemptItem>> BuildItemsAsync(Quiz quiz, Guid attemptId, CancellationToken ct)
    {
        var rng = Random.Shared;
        var slots = new List<(int Position, QuizQuestion Question, decimal Marks, Guid? GroupId)>();
        slots.AddRange(quiz.Questions.Select(q => (q.OrderIndex * 1000, q, q.Marks, (Guid?)null)));

        foreach (var group in quiz.QuestionGroups.OrderBy(g => g.OrderIndex))
        {
            var pool = (await _repo.GetDrawableQuestionsAsync(group.QuestionBankId, group.TagFilter, ct))
                .Where(q => slots.All(s => s.Question.Id != q.Id))
                .ToList();
            var drawn = pool.OrderBy(_ => rng.Next()).Take(group.PickCount).ToList();
            slots.AddRange(drawn.Select((q, i) => (group.OrderIndex * 1000 + i + 1, q, group.MarksPerQuestion, (Guid?)group.Id)));
        }

        var ordered = quiz.ShuffleQuestions
            ? slots.OrderBy(_ => rng.Next()).ToList()
            : slots.OrderBy(s => s.Position).ToList();

        return ordered.Select((s, i) =>
        {
            var (presentation, values) = QuizPresentation.PlanItem(s.Question, quiz.ShuffleOptions, rng);
            return new QuizAttemptItem(attemptId, s.Question.Id, i + 1, s.Marks, s.GroupId, presentation, values);
        }).ToList();
    }

    // ── Paper ─────────────────────────────────────────────────────────────────

    public async Task<(StudentAttemptPaper? Paper, string? Error)> GetPaperAsync(Guid attemptId, Guid studentProfileId, CancellationToken ct = default)
    {
        var attempt = await _repo.GetAttemptForDeliveryAsync(attemptId, ct);
        if (attempt is null || attempt.StudentProfileId != studentProfileId) return (null, "NOT_FOUND");

        if (attempt.Status == AttemptStatus.InProgress && !attempt.IsOpenAt(DateTime.UtcNow))
        {
            FinalizeAttempt(attempt, timedOut: true);
            await _repo.SaveChangesAsync(ct);
            await SyncGradebookAsync(attempt.Quiz.CourseOfferingId, [studentProfileId], ct);
        }
        if (attempt.Status != AttemptStatus.InProgress) return (null, "ATTEMPT_CLOSED");

        var quiz = attempt.Quiz;
        var items = DeliveredItems(attempt);
        var questions = items.Select(i =>
        {
            var answer = attempt.Answers.FirstOrDefault(a => a.QuizQuestionId == i.Question.Id);
            return QuizPresentation.ToStudentView(i.Question, i.Order, i.Marks, i.PresentationJson, i.GeneratedValuesJson,
                answer, locked: quiz.LockAnsweredQuestions && answer?.SavedAt is not null);
        }).ToList();

        return (new StudentAttemptPaper(attempt.Id, quiz.Id, quiz.Title, quiz.Instructions, attempt.Status.ToString(),
            attempt.StartedAt, attempt.DeadlineAt, DateTime.UtcNow, quiz.OneQuestionAtATime, quiz.LockAnsweredQuestions,
            items.Sum(i => i.Marks), questions), null);
    }

    // ── Save / submit ─────────────────────────────────────────────────────────

    public async Task<(int Saved, int LockedSkipped, string? Error)> SaveAnswersAsync(Guid attemptId, Guid studentProfileId, SaveAnswersRequest request, CancellationToken ct = default)
    {
        var attempt = await _repo.GetAttemptForDeliveryAsync(attemptId, ct);
        if (attempt is null || attempt.StudentProfileId != studentProfileId) return (0, 0, "NOT_FOUND");

        var openError = await EnsureOpenAsync(attempt, ct);
        if (openError is not null) return (0, 0, openError);

        var (saved, locked, error) = await ApplyAnswersAsync(attempt, request.Answers ?? [], ct);
        if (error is not null) return (0, 0, error);
        await _repo.SaveChangesAsync(ct);
        return (saved, locked, null);
    }

    public async Task<(int Saved, string? Error)> SaveFileAnswerAsync(Guid attemptId, Guid studentProfileId, Guid questionId,
        string fileName, string? contentType, long length, Stream content, CancellationToken ct = default)
    {
        if (_media is null) return (0, "File uploads are not configured.");

        var attempt = await _repo.GetAttemptForDeliveryAsync(attemptId, ct);
        if (attempt is null || attempt.StudentProfileId != studentProfileId) return (0, "NOT_FOUND");
        var openError = await EnsureOpenAsync(attempt, ct);
        if (openError is not null) return (0, openError);

        var item = DeliveredItems(attempt).FirstOrDefault(i => i.Question.Id == questionId);
        if (item is null || item.Question.Type != QuestionType.FileUpload) return (0, "This question does not take a file.");

        var existing = attempt.Answers.FirstOrDefault(a => a.QuizQuestionId == questionId);
        if (attempt.Quiz.LockAnsweredQuestions && existing?.SavedAt is not null) return (0, "LOCKED");

        var config = QuizJson.Read<FileUploadConfig>(item.Question.ConfigJson) ?? new FileUploadConfig();
        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension)) return (0, "The file must have an extension.");
        if (config.AllowedExtensions is { Count: > 0 } allowed && !allowed.Contains(extension))
            return (0, $"Allowed file types: {string.Join(", ", allowed)}.");
        if (length <= 0) return (0, "The file is empty.");
        if (length > config.MaxSizeMb * 1024L * 1024L) return (0, $"The file is larger than {config.MaxSizeMb} MB.");

        var stored = await _media.SaveAsync(content, "quiz-answers", extension, contentType, Path.GetFileName(fileName), ct);
        if (existing?.ResponseJson is not null && QuizJson.Read<FileUploadResponse>(existing.ResponseJson) is { } previous)
            await _media.DeleteAsync(previous.StorageKey, ct);

        var answer = existing ?? new QuizAnswer(attempt.Id, questionId);
        answer.SetResponse(null, null, QuizJson.Write(new FileUploadResponse(stored.StorageKey, Path.GetFileName(fileName)!, stored.ContentType, stored.Length)), singleSelection: false);
        if (existing is null) await _repo.AddAnswersAsync([answer], ct);
        await _repo.SaveChangesAsync(ct);
        return (1, null);
    }

    public async Task<(AttemptDetailResponse? Review, string? Error)> SubmitAttemptAsync(SubmitAttemptRequest request, Guid studentProfileId, CancellationToken ct = default)
    {
        var attempt = await _repo.GetAttemptForDeliveryAsync(request.AttemptId, ct);
        if (attempt is null || attempt.StudentProfileId != studentProfileId) return (null, "NOT_FOUND");
        if (attempt.Status != AttemptStatus.InProgress) return (null, "ATTEMPT_CLOSED");

        var timedOut = !attempt.IsOpenAt(DateTime.UtcNow);
        if (!timedOut && request.Answers is { Count: > 0 })
        {
            var (_, _, error) = await ApplyAnswersAsync(attempt, request.Answers, ct);
            if (error is not null) return (null, error);
        }

        FinalizeAttempt(attempt, timedOut);
        await _repo.SaveChangesAsync(ct);
        await SyncGradebookAsync(attempt.Quiz.CourseOfferingId, [studentProfileId], ct);
        return (BuildReview(attempt, forStudent: true), null);
    }

    private async Task<string?> EnsureOpenAsync(QuizAttempt attempt, CancellationToken ct)
    {
        if (attempt.Status != AttemptStatus.InProgress) return "ATTEMPT_CLOSED";
        if (attempt.IsOpenAt(DateTime.UtcNow)) return null;
        FinalizeAttempt(attempt, timedOut: true);
        await _repo.SaveChangesAsync(ct);
        await SyncGradebookAsync(attempt.Quiz.CourseOfferingId, [attempt.StudentProfileId], ct);
        return "TIME_EXPIRED";
    }

    /// <summary>Validates and stores answers (upsert). Unknown questions are ignored; locked answers are skipped.</summary>
    private async Task<(int Saved, int Locked, string? Error)> ApplyAnswersAsync(QuizAttempt attempt, IReadOnlyList<SubmitAnswerEntry> entries, CancellationToken ct)
    {
        var items = DeliveredItems(attempt).ToDictionary(i => i.Question.Id);
        var added = new List<QuizAnswer>();
        int saved = 0, locked = 0;

        foreach (var entry in entries.GroupBy(e => e.QuizQuestionId).Select(g => g.Last()))
        {
            if (!items.TryGetValue(entry.QuizQuestionId, out var item)) continue;
            var question = item.Question;
            if (question.Type == QuestionType.FileUpload) continue; // uploaded through its own endpoint

            var existing = attempt.Answers.FirstOrDefault(a => a.QuizQuestionId == question.Id)
                           ?? added.FirstOrDefault(a => a.QuizQuestionId == question.Id);
            if (attempt.Quiz.LockAnsweredQuestions && existing?.SavedAt is not null) { locked++; continue; }

            var (selected, text, json, error) = NormalizeEntry(question, entry);
            if (error is not null) return (0, 0, error);

            var answer = existing ?? new QuizAnswer(attempt.Id, question.Id);
            answer.SetResponse(selected, text, json, singleSelection: question.Type is QuestionType.MultipleChoice or QuestionType.TrueFalse);
            if (existing is null) added.Add(answer);
            saved++;
        }

        if (added.Count > 0) await _repo.AddAnswersAsync(added, ct);
        return (saved, locked, null);
    }

    /// <summary>Accepts only data that belongs to the question type (no foreign option ids, bounded sizes, canonical JSON).</summary>
    private static (IReadOnlyList<Guid>? Selected, string? Text, string? Json, string? Error) NormalizeEntry(QuizQuestion q, SubmitAnswerEntry e)
    {
        switch (q.Type)
        {
            case QuestionType.MultipleChoice:
            case QuestionType.TrueFalse:
            case QuestionType.MultipleAnswers:
            {
                var ids = (e.SelectedOptionIds ?? []).ToList();
                if (e.SelectedOptionId.HasValue) ids.Insert(0, e.SelectedOptionId.Value);
                var valid = q.Options.Select(o => o.Id).ToHashSet();
                return (ids.Where(valid.Contains).Distinct().ToList(), null, null, null);
            }
            case QuestionType.ShortAnswer:
                return e.TextResponse is { Length: > MaxTextLength }
                    ? (null, null, null, $"Answers are limited to {MaxTextLength} characters.")
                    : (null, e.TextResponse?.Trim(), null, null);
            case QuestionType.Essay:
                return e.TextResponse is { Length: > MaxEssayLength }
                    ? (null, null, null, $"Essays are limited to {MaxEssayLength} characters.")
                    : (null, e.TextResponse, null, null);
        }

        if (e.ResponseJson is { Length: > MaxResponseJsonLength }) return (null, null, null, "The answer is too large.");

        string? json = q.Type switch
        {
            QuestionType.Matching => QuizJson.Read<MatchingResponse>(e.ResponseJson) is { Matches: { } m }
                ? QuizJson.Write(new MatchingResponse(m.Where(kv => kv.Key.Length <= 20 && kv.Value.Length <= 1000).ToDictionary(kv => kv.Key, kv => kv.Value)))
                : null,
            QuestionType.Ordering => QuizJson.Read<OrderingResponse>(e.ResponseJson) is { Order: { } o }
                ? QuizJson.Write(new OrderingResponse(o.Where(id => id.Length <= 20).Take(50).ToList()))
                : null,
            QuestionType.FillInBlank => QuizJson.Read<FillInBlankResponse>(e.ResponseJson) is { Blanks: { } b }
                ? QuizJson.Write(new FillInBlankResponse(b.Where(kv => kv.Key.Length <= 20 && kv.Value.Length <= 500).ToDictionary(kv => kv.Key, kv => kv.Value)))
                : null,
            QuestionType.Numeric or QuestionType.Formula => QuizJson.Read<NumericResponse>(e.ResponseJson) is { Value: { } v }
                ? QuizJson.Write(new NumericResponse(v.Length <= 50 ? v.Trim() : v[..50]))
                : null,
            _ => null
        };
        return (null, null, json, null);
    }

    // ── Finalising & grading an attempt ───────────────────────────────────────

    private static IReadOnlyList<DeliveredItem> DeliveredItems(QuizAttempt attempt)
        => attempt.Items.Count > 0
            ? attempt.Items.OrderBy(i => i.OrderIndex)
                .Select(i => new DeliveredItem(i.OrderIndex, i.Question, i.Marks, i.PresentationJson, i.GeneratedValuesJson)).ToList()
            : attempt.Quiz.Questions.OrderBy(q => q.OrderIndex)   // attempts started before Plan M2
                .Select((q, i) => new DeliveredItem(i + 1, q, q.Marks, null, null)).ToList();

    /// <summary>Submits (or times out) the attempt and auto-grades every answer it can.</summary>
    private static void FinalizeAttempt(QuizAttempt attempt, bool timedOut)
    {
        if (timedOut) attempt.TimeOut(); else attempt.Submit();
        foreach (var item in DeliveredItems(attempt))
        {
            var answer = attempt.Answers.FirstOrDefault(a => a.QuizQuestionId == item.Question.Id);
            if (answer is null) continue;
            var outcome = QuestionGrading.Grade(ToGradable(item), ToSubmission(answer));
            if (outcome.Marks.HasValue) answer.AwardMarks(outcome.Marks.Value);
        }
        RecomputeTotal(attempt);
    }

    /// <summary>Total = sum of marks once nothing is waiting for manual grading; otherwise pending (null).</summary>
    private static void RecomputeTotal(QuizAttempt attempt)
    {
        if (attempt.Status == AttemptStatus.InProgress) return;
        // By id: grading loads the attempt's items without their questions (questions drawn from a bank are not the quiz's own).
        var questionIds = attempt.Items.Count > 0
            ? attempt.Items.Select(i => i.QuizQuestionId).ToHashSet()
            : attempt.Quiz.Questions.Select(q => q.Id).ToHashSet(); // attempts started before Plan M2
        var answers = attempt.Answers.Where(a => questionIds.Contains(a.QuizQuestionId)).ToList();
        if (answers.Any(a => !a.MarksAwarded.HasValue))
            attempt.ClearScore();
        else
            attempt.RecordScore(answers.Sum(a => a.MarksAwarded ?? 0));
    }

    private static GradableQuestion ToGradable(DeliveredItem item) => new(
        item.Question.Id, item.Question.Type, item.Marks, item.Question.ConfigJson,
        item.Question.Options.Select(o => new GradableOption(o.Id, o.Text, o.IsCorrect)).ToList(),
        QuizPresentation.ReadValues(item.GeneratedValuesJson));

    private static AnswerSubmission ToSubmission(QuizAnswer a) => new(a.SelectedOptionIds, a.TextResponse, a.ResponseJson);

    private static bool FixedTimeEquals(string expected, string? given)
        => given is not null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(given));

    // ── Reviews ───────────────────────────────────────────────────────────────

    public async Task<(AttemptDetailResponse? Review, string? Error)> GetStudentReviewAsync(Guid attemptId, Guid studentProfileId, CancellationToken ct = default)
    {
        var attempt = await _repo.GetAttemptForDeliveryAsync(attemptId, ct);
        if (attempt is null || attempt.StudentProfileId != studentProfileId) return (null, "NOT_FOUND");

        if (attempt.Status == AttemptStatus.InProgress && !attempt.IsOpenAt(DateTime.UtcNow))
        {
            FinalizeAttempt(attempt, timedOut: true);
            await _repo.SaveChangesAsync(ct);
            await SyncGradebookAsync(attempt.Quiz.CourseOfferingId, [studentProfileId], ct);
        }
        if (attempt.Status == AttemptStatus.InProgress) return (null, "IN_PROGRESS");
        return (BuildReview(attempt, forStudent: true), null);
    }

    /// <summary>
    /// Faculty get everything. Students get what the quiz's review option allows
    /// (limited to the score until the quiz closes when "after close" is set).
    /// </summary>
    private static AttemptDetailResponse BuildReview(QuizAttempt attempt, bool forStudent)
    {
        var quiz = attempt.Quiz;
        var visibility = quiz.ReviewVisibility;
        if (forStudent && quiz.ReviewAfterCloseOnly && !(quiz.AvailableUntil.HasValue && quiz.AvailableUntil < DateTime.UtcNow))
            visibility = visibility > QuizReviewVisibility.ScoreOnly ? QuizReviewVisibility.ScoreOnly : visibility;
        if (!forStudent) visibility = QuizReviewVisibility.CorrectAnswers;

        var items = DeliveredItems(attempt);
        var maxScore = items.Sum(i => i.Marks);
        var showScore = visibility >= QuizReviewVisibility.ScoreOnly;
        var showResponses = visibility >= QuizReviewVisibility.ResponsesAndScore;
        var showCorrect = visibility >= QuizReviewVisibility.CorrectAnswers;

        var reviewItems = showResponses
            ? items.Select(i =>
            {
                var answer = attempt.Answers.FirstOrDefault(a => a.QuizQuestionId == i.Question.Id);
                var view = QuizPresentation.ToStudentView(i.Question, i.Order, i.Marks, i.PresentationJson, i.GeneratedValuesJson, answer, locked: true);
                var pending = answer is not null && !answer.MarksAwarded.HasValue;
                return new ReviewItemResponse(i.Order, view, i.Marks,
                    answer is null ? 0 : answer.MarksAwarded, answer?.Feedback,
                    showCorrect ? QuizPresentation.DescribeCorrectAnswer(i.Question, i.GeneratedValuesJson) : null,
                    pending);
            }).ToList()
            : null;

        var answers = showResponses
            ? attempt.Answers.Select(a => new AnswerResponse(a.Id, a.QuizQuestionId, a.SelectedOptionId, a.TextResponse,
                a.MarksAwarded, a.SelectedOptionIds, a.ResponseJson, a.Feedback)).ToList()
            : new List<AnswerResponse>();

        return new AttemptDetailResponse(attempt.Id, attempt.QuizId, attempt.StudentProfileId, attempt.Status.ToString(),
            attempt.StartedAt, attempt.FinishedAt, showScore ? attempt.TotalScore : null, answers,
            showScore ? maxScore : 0, reviewItems, visibility.ToString());
    }

    public async Task<IReadOnlyList<AttemptResponse>> GetStudentAttemptsAsync(Guid quizId, Guid studentProfileId, CancellationToken ct = default)
        => (await _repo.GetAttemptsAsync(quizId, studentProfileId, ct)).Select(ToAttemptResponse).ToList();

    public async Task<IReadOnlyList<AttemptResponse>> GetAllMyAttemptsAsync(Guid studentProfileId, CancellationToken ct = default)
        => (await _repo.GetAllAttemptsForStudentAsync(studentProfileId, ct)).Select(ToAttemptResponse).ToList();
}
