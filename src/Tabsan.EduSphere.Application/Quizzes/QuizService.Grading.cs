using Tabsan.EduSphere.Application.DTOs.Quizzes;
using Tabsan.EduSphere.Application.Quizzes.Grading;
using Tabsan.EduSphere.Domain.Quizzes;

namespace Tabsan.EduSphere.Application.Quizzes;

/// <summary>Plan M2.5–M2.7: grading, regrade, statistics, accommodations and availability overrides.</summary>
public sealed partial class QuizService
{
    // ── Faculty attempt view ──────────────────────────────────────────────────

    public async Task<AttemptDetailResponse?> GetAttemptDetailAsync(Guid attemptId, CancellationToken ct = default)
    {
        var attempt = await _repo.GetAttemptForDeliveryAsync(attemptId, ct);
        return attempt is null ? null : BuildReview(attempt, forStudent: false);
    }

    public async Task<Guid?> GetAttemptOwnerAsync(Guid attemptId, CancellationToken ct = default)
        => (await _repo.GetAttemptByIdAsync(attemptId, ct))?.StudentProfileId;

    // ── Manual grading (M2.6) ─────────────────────────────────────────────────

    public async Task<IReadOnlyList<GradingQueueItem>> GetGradingQueueAsync(Guid quizId, CancellationToken ct = default)
    {
        var answers = await _repo.GetManualGradingQueueAsync(quizId, ct);
        var names = await _repo.GetStudentNamesAsync(answers.Select(a => a.Attempt.StudentProfileId).Distinct().ToList(), ct);

        return answers.Select(a =>
        {
            var marks = a.Attempt.Items.FirstOrDefault(i => i.QuizQuestionId == a.QuizQuestionId)?.Marks ?? a.Question.Marks;
            var file = a.Question.Type == QuestionType.FileUpload ? QuizJson.Read<FileUploadResponse>(a.ResponseJson) : null;
            return new GradingQueueItem(a.Id, a.QuizAttemptId, a.Attempt.StudentProfileId,
                names.TryGetValue(a.Attempt.StudentProfileId, out var n) ? n : null,
                a.QuizQuestionId, a.Question.Type.ToString(), a.Question.Text, marks,
                a.TextResponse, file?.FileName, file?.StorageKey, a.Attempt.FinishedAt);
        }).ToList();
    }

    /// <summary>Awards marks (and optional feedback) to one answer, then refreshes the attempt total.</summary>
    public async Task<(bool Found, string? Error)> GradeAnswerAsync(GradeAnswerRequest request, Guid gradedByUserId, CancellationToken ct = default)
    {
        var answer = await _repo.GetAnswerForGradingAsync(request.AnswerId, ct);
        if (answer is null) return (false, null);

        var attempt = answer.Attempt;
        if (attempt.Status == AttemptStatus.InProgress) return (true, "The attempt is still in progress.");

        var max = attempt.Items.FirstOrDefault(i => i.QuizQuestionId == answer.QuizQuestionId)?.Marks ?? answer.Question.Marks;
        if (request.MarksAwarded < 0 || request.MarksAwarded > max)
            return (true, $"Marks must be between 0 and {max:0.##}.");

        answer.Grade(request.MarksAwarded, request.Feedback, gradedByUserId);
        RecomputeTotal(attempt);
        await _repo.SaveChangesAsync(ct);
        await SyncGradebookForQuizAsync(attempt.QuizId, [attempt.StudentProfileId], ct);
        return (true, null);
    }

    // ── Regrade (M2.6) ────────────────────────────────────────────────────────

    /// <summary>
    /// Modes: <c>Recalculate</c> re-runs the auto-grader with the question's current settings
    /// (e.g. after fixing the correct option); <c>FullCredit</c> gives every attempt full marks.
    /// Manually graded types only support FullCredit.
    /// </summary>
    public async Task<RegradeResult> RegradeQuestionAsync(Guid quizId, Guid questionId, RegradeQuestionRequest request, CancellationToken ct = default)
    {
        var fullCredit = string.Equals(request.Mode, "FullCredit", StringComparison.OrdinalIgnoreCase);
        if (!fullCredit && !string.Equals(request.Mode, "Recalculate", StringComparison.OrdinalIgnoreCase))
            return new RegradeResult(0, "Mode must be Recalculate or FullCredit.");

        var question = (await _repo.GetQuestionsWithOptionsAsync([questionId], ct)).FirstOrDefault();
        if (question is null) return new RegradeResult(0, "Question not found.");
        if (!fullCredit && QuestionGrading.IsManual(question.Type))
            return new RegradeResult(0, "Manually graded questions can only be regraded with full credit.");

        var attempts = await _repo.GetFinishedAttemptsAsync(quizId, ct);
        var quizQuestionIds = question.QuizId == quizId ? new HashSet<Guid> { questionId } : [];
        var added = new List<QuizAnswer>();
        var regradedStudents = new List<Guid>();
        var updated = 0;

        foreach (var attempt in attempts)
        {
            var item = attempt.Items.FirstOrDefault(i => i.QuizQuestionId == questionId);
            var appliesToLegacy = attempt.Items.Count == 0 && quizQuestionIds.Contains(questionId);
            if (item is null && !appliesToLegacy) continue;

            var marks = item?.Marks ?? question.Marks;
            var answer = attempt.Answers.FirstOrDefault(a => a.QuizQuestionId == questionId);

            if (fullCredit)
            {
                if (answer is null)
                {
                    answer = new QuizAnswer(attempt.Id, questionId);
                    added.Add(answer);
                }
                answer.AwardMarks(marks);
            }
            else
            {
                if (answer is null) continue; // unanswered stays at zero
                var gradable = new GradableQuestion(question.Id, question.Type, marks, question.ConfigJson,
                    question.Options.Select(o => new GradableOption(o.Id, o.Text, o.IsCorrect)).ToList(),
                    QuizPresentation.ReadValues(item?.GeneratedValuesJson));
                var outcome = QuestionGrading.Grade(gradable, ToSubmission(answer));
                if (outcome.Marks.HasValue) answer.AwardMarks(outcome.Marks.Value);
            }

            RecomputeTotalForAttempt(attempt, added);
            regradedStudents.Add(attempt.StudentProfileId);
            updated++;
        }

        if (added.Count > 0) await _repo.AddAnswersAsync(added, ct);
        await _repo.SaveChangesAsync(ct);
        await SyncGradebookForQuizAsync(quizId, regradedStudents, ct);
        return new RegradeResult(updated, null);
    }

    /// <summary>Regrade path: attempts are loaded without questions, so totals are computed from answers + items.</summary>
    private static void RecomputeTotalForAttempt(QuizAttempt attempt, IReadOnlyCollection<QuizAnswer> newAnswers)
    {
        var all = attempt.Answers.Concat(newAnswers.Where(n => n.QuizAttemptId == attempt.Id && !attempt.Answers.Contains(n))).ToList();
        var relevant = attempt.Items.Count > 0
            ? all.Where(a => attempt.Items.Any(i => i.QuizQuestionId == a.QuizQuestionId)).ToList()
            : all;
        if (relevant.Any(a => !a.MarksAwarded.HasValue))
            attempt.ClearScore();
        else
            attempt.RecordScore(relevant.Sum(a => a.MarksAwarded ?? 0));
    }

    // ── Item analysis (M2.7) ──────────────────────────────────────────────────

    public async Task<QuizStatisticsResponse?> GetStatisticsAsync(Guid quizId, CancellationToken ct = default)
    {
        var quiz = await _repo.GetForAuthoringAsync(quizId, ct);
        if (quiz is null) return null;

        var attempts = (await _repo.GetFinishedAttemptsAsync(quizId, ct)).Where(a => a.TotalScore.HasValue).ToList();
        var totals = attempts.Select(a => a.TotalScore!.Value).OrderBy(x => x).ToList();
        var nominalMax = quiz.Questions.Sum(q => q.Marks) + quiz.QuestionGroups.Sum(g => g.PickCount * g.MarksPerQuestion);

        // Every question that appeared in any attempt (quiz questions + bank draws).
        var questionIds = attempts.SelectMany(a => a.Items.Select(i => i.QuizQuestionId))
            .Concat(quiz.Questions.Select(q => q.Id)).Distinct().ToList();
        var questions = (await _repo.GetQuestionsWithOptionsAsync(questionIds, ct)).ToDictionary(q => q.Id);

        // Upper/lower 27% groups by total score (needs at least 4 attempts).
        var ranked = attempts.OrderByDescending(a => a.TotalScore).ToList();
        var groupSize = ranked.Count >= 4 ? Math.Max(1, (int)Math.Round(ranked.Count * 0.27)) : 0;
        var upper = ranked.Take(groupSize).ToList();
        var lower = ranked.Skip(ranked.Count - groupSize).ToList();

        var stats = new List<QuestionStatistics>();
        var order = 0;
        foreach (var qid in questionIds)
        {
            if (!questions.TryGetValue(qid, out var q)) continue;
            order++;

            var sitting = attempts.Where(a => a.Items.Any(i => i.QuizQuestionId == qid) || (a.Items.Count == 0 && q.QuizId == quizId)).ToList();
            if (sitting.Count == 0 && q.IsBankQuestion) continue;

            double? FractionFor(IEnumerable<QuizAttempt> group)
            {
                var fractions = group.Where(sitting.Contains).Select(a =>
                {
                    var marks = a.Items.FirstOrDefault(i => i.QuizQuestionId == qid)?.Marks ?? q.Marks;
                    var awarded = a.Answers.FirstOrDefault(x => x.QuizQuestionId == qid)?.MarksAwarded ?? 0;
                    return marks > 0 ? (double)(awarded / marks) : 0d;
                }).ToList();
                return fractions.Count == 0 ? null : fractions.Average();
            }

            var difficulty = FractionFor(sitting);
            double? discrimination = groupSize > 0 && FractionFor(upper) is { } up && FractionFor(lower) is { } low ? up - low : null;
            var responses = sitting.Count(a => a.Answers.Any(x => x.QuizQuestionId == qid));
            var meanMarks = sitting.Count == 0 ? (decimal?)null
                : Math.Round(sitting.Average(a => a.Answers.FirstOrDefault(x => x.QuizQuestionId == qid)?.MarksAwarded ?? 0), 2);

            IReadOnlyList<OptionStatistics>? options = null;
            if (QuestionGrading.UsesOptions(q.Type) && responses > 0)
            {
                var picks = sitting.SelectMany(a => a.Answers.Where(x => x.QuizQuestionId == qid).SelectMany(x => x.SelectedOptionIds)).ToList();
                options = q.Options.OrderBy(o => o.OrderIndex).Select(o =>
                {
                    var count = picks.Count(p => p == o.Id);
                    return new OptionStatistics(o.Id, o.Text, o.IsCorrect, count, Math.Round((decimal)count / responses, 4));
                }).ToList();
            }

            stats.Add(new QuestionStatistics(q.Id, order, q.Text, q.Type.ToString(), responses,
                difficulty is null ? null : Math.Round((decimal)difficulty.Value, 4),
                discrimination is null ? null : Math.Round((decimal)discrimination.Value, 4),
                meanMarks, q.Marks, options));
        }

        decimal? Median()
        {
            if (totals.Count == 0) return null;
            var mid = totals.Count / 2;
            return totals.Count % 2 == 1 ? totals[mid] : Math.Round((totals[mid - 1] + totals[mid]) / 2, 2);
        }

        decimal? StdDev()
        {
            if (totals.Count < 2) return null;
            var mean = totals.Average();
            var variance = totals.Sum(t => (double)((t - mean) * (t - mean))) / totals.Count;
            return Math.Round((decimal)Math.Sqrt(variance), 2);
        }

        return new QuizStatisticsResponse(quizId, attempts.Count,
            totals.Count == 0 ? null : Math.Round(totals.Average(), 2), Median(),
            totals.Count == 0 ? null : totals.Max(), totals.Count == 0 ? null : totals.Min(), StdDev(),
            nominalMax, stats);
    }

    // ── File answers ──────────────────────────────────────────────────────────

    /// <summary>Downloads a file answer. Students (non-null <paramref name="studentProfileId"/>) only get their own.</summary>
    public async Task<(byte[] Content, string FileName, string ContentType)?> GetAnswerFileAsync(Guid answerId, Guid? studentProfileId, CancellationToken ct = default)
    {
        if (_media is null) return null;
        var answer = await _repo.GetAnswerForGradingAsync(answerId, ct);
        if (answer is null || answer.Question.Type != QuestionType.FileUpload) return null;
        if (studentProfileId.HasValue && answer.Attempt.StudentProfileId != studentProfileId.Value) return null;

        var file = QuizJson.Read<FileUploadResponse>(answer.ResponseJson);
        if (file is null) return null;
        var bytes = await _media.ReadAsBytesAsync(file.StorageKey, ct);
        return bytes is null ? null : (bytes, file.FileName, file.ContentType ?? "application/octet-stream");
    }

    // ── Accommodations & overrides (M2.5) ─────────────────────────────────────

    public async Task<IReadOnlyList<AccommodationResponse>> GetAccommodationsAsync(Guid courseOfferingId, CancellationToken ct = default)
    {
        var rows = await _repo.GetAccommodationsAsync(courseOfferingId, ct);
        var names = await _repo.GetStudentNamesAsync(rows.Select(r => r.StudentProfileId).Distinct().ToList(), ct);
        return rows.Select(r => new AccommodationResponse(r.Id, r.CourseOfferingId, r.QuizId, r.StudentProfileId,
            names.TryGetValue(r.StudentProfileId, out var n) ? n : null,
            r.ExtraTimeMinutes, r.TimeMultiplier, r.ExtraAttempts, r.Note)).ToList();
    }

    public async Task<(Guid? Id, string? Error)> SaveAccommodationAsync(SaveAccommodationRequest request, CancellationToken ct = default)
    {
        try
        {
            var existing = (await _repo.GetAccommodationsAsync(request.CourseOfferingId, ct))
                .FirstOrDefault(a => a.StudentProfileId == request.StudentProfileId && a.QuizId == request.QuizId);
            if (existing is not null)
            {
                existing.Update(request.ExtraTimeMinutes, request.TimeMultiplier, request.ExtraAttempts, request.Note);
                await _repo.SaveChangesAsync(ct);
                return (existing.Id, null);
            }

            var created = new QuizAccommodation(request.CourseOfferingId, request.QuizId, request.StudentProfileId,
                request.ExtraTimeMinutes, request.TimeMultiplier, request.ExtraAttempts, request.Note);
            await _repo.AddAccommodationAsync(created, ct);
            await _repo.SaveChangesAsync(ct);
            return (created.Id, null);
        }
        catch (ArgumentException ex) { return (null, ex.Message); }
    }

    public async Task<bool> RemoveAccommodationAsync(Guid id, CancellationToken ct = default)
    {
        var row = await _repo.GetAccommodationAsync(id, ct);
        if (row is null) return false;
        _repo.RemoveAccommodation(row);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<AvailabilityOverrideResponse>> GetOverridesAsync(Guid quizId, CancellationToken ct = default)
    {
        var rows = await _repo.GetOverridesAsync(quizId, ct);
        var names = await _repo.GetStudentNamesAsync(rows.Select(r => r.StudentProfileId).Distinct().ToList(), ct);
        return rows.Select(r => new AvailabilityOverrideResponse(r.Id, r.QuizId, r.StudentProfileId,
            names.TryGetValue(r.StudentProfileId, out var n) ? n : null, r.AvailableFrom, r.AvailableUntil, r.Note)).ToList();
    }

    public async Task<(Guid? Id, string? Error)> SaveOverrideAsync(Guid quizId, SaveAvailabilityOverrideRequest request, CancellationToken ct = default)
    {
        if (await _repo.GetByIdAsync(quizId, ct) is null) return (null, "Quiz not found.");
        try
        {
            var existing = await _repo.GetOverrideForStudentAsync(quizId, request.StudentProfileId, ct);
            if (existing is not null)
            {
                existing.Update(request.AvailableFrom, request.AvailableUntil, request.Note);
                await _repo.SaveChangesAsync(ct);
                return (existing.Id, null);
            }

            var created = new QuizAvailabilityOverride(quizId, request.StudentProfileId, request.AvailableFrom, request.AvailableUntil, request.Note);
            await _repo.AddOverrideAsync(created, ct);
            await _repo.SaveChangesAsync(ct);
            return (created.Id, null);
        }
        catch (ArgumentException ex) { return (null, ex.Message); }
    }

    public async Task<bool> RemoveOverrideAsync(Guid id, CancellationToken ct = default)
    {
        var row = await _repo.GetOverrideAsync(id, ct);
        if (row is null) return false;
        _repo.RemoveOverride(row);
        await _repo.SaveChangesAsync(ct);
        return true;
    }
}
