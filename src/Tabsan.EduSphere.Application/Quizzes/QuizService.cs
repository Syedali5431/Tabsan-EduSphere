using Tabsan.EduSphere.Application.DTOs.Quizzes;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Quizzes;

namespace Tabsan.EduSphere.Application.Quizzes;

/// <summary>
/// Implements <see cref="IQuizService"/> — quiz authoring, publishing,
/// attempt lifecycle management, and MCQ/TrueFalse auto-grading.
/// </summary>
public sealed class QuizService : IQuizService
{
    private readonly IQuizRepository _repo;

    /// <summary>Initialises the service with the quiz repository.</summary>
    public QuizService(IQuizRepository repo) => _repo = repo;

    // ── Authoring ─────────────────────────────────────────────────────────────

    /// <summary>Creates a new unpublished quiz and returns its ID.</summary>
    public async Task<Guid> CreateAsync(CreateQuizRequest request, Guid createdByUserId, CancellationToken ct = default)
    {
        var quiz = new Quiz(
            request.CourseOfferingId,
            request.Title,
            createdByUserId,
            request.Instructions,
            request.TimeLimitMinutes,
            request.MaxAttempts,
            request.AvailableFrom,
            request.AvailableUntil);

        await _repo.AddAsync(quiz, ct);
        await _repo.SaveChangesAsync(ct);
        return quiz.Id;
    }

    /// <summary>Updates the metadata of an unpublished quiz. Returns false if not found.</summary>
    public async Task<bool> UpdateAsync(Guid quizId, UpdateQuizRequest request, CancellationToken ct = default)
    {
        var quiz = await _repo.GetByIdAsync(quizId, ct);
        if (quiz is null) return false;

        quiz.Update(request.Title, request.Instructions, request.TimeLimitMinutes,
                    request.MaxAttempts, request.AvailableFrom, request.AvailableUntil);
        _repo.Update(quiz);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Publishes a quiz so students can start attempts. Returns false if not found.</summary>
    public async Task<bool> PublishAsync(Guid quizId, CancellationToken ct = default)
    {
        var quiz = await _repo.GetByIdAsync(quizId, ct);
        if (quiz is null) return false;

        quiz.Publish();
        _repo.Update(quiz);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Unpublishes a quiz, hiding it from students. Returns false if not found.</summary>
    public async Task<bool> UnpublishAsync(Guid quizId, CancellationToken ct = default)
    {
        var quiz = await _repo.GetByIdAsync(quizId, ct);
        if (quiz is null) return false;

        quiz.Unpublish();
        _repo.Update(quiz);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Soft-deletes a quiz. Returns false if not found.</summary>
    public async Task<bool> DeactivateAsync(Guid quizId, CancellationToken ct = default)
    {
        var quiz = await _repo.GetByIdAsync(quizId, ct);
        if (quiz is null) return false;

        quiz.Deactivate();
        _repo.Update(quiz);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Reactivates a previously deactivated quiz. Returns false if not found.</summary>
    public async Task<bool> ActivateAsync(Guid quizId, CancellationToken ct = default)
    {
        var quiz = await _repo.GetByIdIncludingInactiveAsync(quizId, ct);
        if (quiz is null) return false;

        quiz.Activate();
        _repo.Update(quiz);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Adds a question (with options) to a quiz and returns the new question ID.</summary>
    public async Task<Guid> AddQuestionAsync(AddQuestionRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<QuestionType>(request.Type, ignoreCase: true, out var questionType))
            questionType = QuestionType.MultipleChoice;

        var question = new QuizQuestion(request.QuizId, request.Text, questionType,
                                        request.Marks, request.OrderIndex);
        await _repo.AddQuestionAsync(question, ct);
        await _repo.SaveChangesAsync(ct); // flush to get question ID

        if (request.Options is { Count: > 0 })
        {
            var options = request.Options.Select(o =>
                new QuizOption(question.Id, o.Text, o.IsCorrect, o.OrderIndex));
            await _repo.AddOptionsAsync(options, ct);
            await _repo.SaveChangesAsync(ct);
        }

        return question.Id;
    }

    /// <summary>Updates a question's text, marks, and ordering. Returns false if not found.</summary>
    public async Task<bool> UpdateQuestionAsync(Guid questionId, UpdateQuestionRequest request, CancellationToken ct = default)
    {
        var question = await _repo.GetQuestionByIdAsync(questionId, ct);
        if (question is null) return false;

        question.Update(request.Text, request.Marks, request.OrderIndex);
        _repo.UpdateQuestion(question);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Removes a question and its options. Returns false if not found.</summary>
    public async Task<bool> RemoveQuestionAsync(Guid questionId, CancellationToken ct = default)
    {
        var question = await _repo.GetQuestionByIdAsync(questionId, ct);
        if (question is null) return false;

        _repo.RemoveOptions(question.Options);
        _repo.RemoveQuestion(question);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Returns all active quizzes for a course offering.</summary>
    public async Task<IReadOnlyList<QuizSummaryResponse>> GetByOfferingAsync(Guid courseOfferingId, bool includeInactive = false, CancellationToken ct = default)
    {
        var quizzes = await _repo.GetByOfferingAsync(courseOfferingId, includeInactive, ct);
        return quizzes.Select(ToSummary).ToList();
    }

    /// <summary>Returns full quiz detail with questions and options, or null if not found.</summary>
    public async Task<QuizDetailResponse?> GetDetailAsync(Guid quizId, CancellationToken ct = default)
    {
        var quiz = await _repo.GetWithQuestionsAsync(quizId, ct);
        return quiz is null ? null : ToDetail(quiz);
    }

    // ── Attempts ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Opens a new attempt for a student.
    /// Returns null when the quiz is unavailable or the student has reached the attempt cap.
    /// </summary>
    public async Task<AttemptResponse?> StartAttemptAsync(Guid quizId, Guid studentProfileId, CancellationToken ct = default)
    {
        var quiz = await _repo.GetByIdAsync(quizId, ct);
        if (quiz is null || !quiz.IsPublished) return null;

        var now = DateTime.UtcNow;
        if (quiz.AvailableFrom.HasValue && now < quiz.AvailableFrom) return null;
        if (quiz.AvailableUntil.HasValue && now > quiz.AvailableUntil) return null;

        // Reject if an in-progress attempt already exists
        var inProgress = await _repo.GetInProgressAttemptAsync(quizId, studentProfileId, ct);
        if (inProgress is not null) return null;

        // Enforce attempt cap (0 = unlimited)
        if (quiz.MaxAttempts > 0)
        {
            var count = await _repo.GetAttemptCountAsync(quizId, studentProfileId, ct);
            if (count >= quiz.MaxAttempts) return null;
        }

        var attempt = new QuizAttempt(quizId, studentProfileId);
        await _repo.AddAttemptAsync(attempt, ct);
        await _repo.SaveChangesAsync(ct);
        return ToAttemptResponse(attempt);
    }

    /// <summary>
    /// Submits an in-progress attempt, auto-grades MCQ/TrueFalse, and returns the full result.
    /// Returns null if the attempt is not found or is already finalised.
    /// </summary>
    public async Task<AttemptDetailResponse?> SubmitAttemptAsync(SubmitAttemptRequest request, CancellationToken ct = default)
    {
        var attempt = await _repo.GetAttemptByIdAsync(request.AttemptId, ct);
        if (attempt is null || attempt.Status != AttemptStatus.InProgress) return null;

        var quiz = await _repo.GetWithQuestionsAsync(attempt.QuizId, ct);
        if (quiz is null) return null;

        // Build a lookup of correct options per question
        var correctOptions = quiz.Questions
            .SelectMany(q => q.Options)
            .Where(o => o.IsCorrect)
            .GroupBy(o => o.QuizQuestionId)
            .ToDictionary(g => g.Key, g => g.Select(o => o.Id).ToHashSet());

        var questionMap = quiz.Questions.ToDictionary(q => q.Id);
        var answers = new List<QuizAnswer>();
        decimal totalScore = 0;

        foreach (var entry in request.Answers)
        {
            if (!questionMap.TryGetValue(entry.QuizQuestionId, out var question)) continue;

            QuizAnswer answer;
            if (question.Type == QuestionType.ShortAnswer)
            {
                answer = new QuizAnswer(attempt.Id, entry.QuizQuestionId, entry.TextResponse ?? string.Empty);
            }
            else
            {
                if (!entry.SelectedOptionId.HasValue) continue;
                answer = new QuizAnswer(attempt.Id, entry.QuizQuestionId, entry.SelectedOptionId.Value);

                // Auto-grade MCQ/TrueFalse
                if (correctOptions.TryGetValue(entry.QuizQuestionId, out var correct)
                    && correct.Contains(entry.SelectedOptionId.Value))
                {
                    answer.AwardMarks(question.Marks);
                    totalScore += question.Marks;
                }
                else
                {
                    answer.AwardMarks(0);
                }
            }

            answers.Add(answer);
        }

        await _repo.AddAnswersAsync(answers, ct);
        attempt.Submit();
        attempt.RecordScore(totalScore);
        _repo.UpdateAttempt(attempt);
        await _repo.SaveChangesAsync(ct);

        return ToAttemptDetail(attempt, answers);
    }

    /// <summary>Returns all attempts a student has made on a quiz.</summary>
    public async Task<IReadOnlyList<AttemptResponse>> GetStudentAttemptsAsync(Guid quizId, Guid studentProfileId, CancellationToken ct = default)
    {
        var attempts = await _repo.GetAttemptsAsync(quizId, studentProfileId, ct);
        return attempts.Select(ToAttemptResponse).ToList();
    }

    /// <summary>Returns all attempts across all quizzes for a student.</summary>
    public async Task<IReadOnlyList<AttemptResponse>> GetAllMyAttemptsAsync(Guid studentProfileId, CancellationToken ct = default)
    {
        var attempts = await _repo.GetAllAttemptsForStudentAsync(studentProfileId, ct);
        return attempts.Select(ToAttemptResponse).ToList();
    }

    /// <summary>Returns full attempt detail including answers, or null if not found.</summary>
    public async Task<AttemptDetailResponse?> GetAttemptDetailAsync(Guid attemptId, CancellationToken ct = default)
    {
        var attempt = await _repo.GetAttemptByIdAsync(attemptId, ct);
        return attempt is null ? null : ToAttemptDetail(attempt, attempt.Answers);
    }

    /// <summary>Awards marks for a ShortAnswer response. Returns false if not found.</summary>
    public async Task<bool> GradeAnswerAsync(GradeAnswerRequest request, CancellationToken ct = default)
    {
        var answer = await _repo.GetAnswerByIdAsync(request.AnswerId, ct);
        if (answer is null) return false;

        answer.AwardMarks(request.MarksAwarded);
        _repo.UpdateAnswer(answer);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>Maps a <see cref="Quiz"/> to a <see cref="QuizSummaryResponse"/>.</summary>
    private static QuizSummaryResponse ToSummary(Quiz q) => new(
        q.Id, q.CourseOfferingId, q.Title,
        q.TimeLimitMinutes, q.MaxAttempts,
        q.AvailableFrom, q.AvailableUntil, q.IsPublished, q.IsActive);

    /// <summary>Maps a <see cref="Quiz"/> (with questions loaded) to a <see cref="QuizDetailResponse"/>.</summary>
    private static QuizDetailResponse ToDetail(Quiz q) => new(
        q.Id, q.CourseOfferingId, q.Title, q.Instructions,
        q.TimeLimitMinutes, q.MaxAttempts,
        q.AvailableFrom, q.AvailableUntil, q.IsPublished,
        q.Questions.OrderBy(x => x.OrderIndex)
                   .Select(ToQuestionResponse)
                   .ToList());

    /// <summary>Maps a <see cref="QuizQuestion"/> to a <see cref="QuestionResponse"/>.</summary>
    private static QuestionResponse ToQuestionResponse(QuizQuestion q) => new(
        q.Id, q.Text, q.Type.ToString(), q.Marks, q.OrderIndex,
        q.Options.OrderBy(o => o.OrderIndex)
                 .Select(o => new OptionResponse(o.Id, o.Text, o.OrderIndex, o.IsCorrect))
                 .ToList());

    /// <summary>Maps a <see cref="QuizAttempt"/> to an <see cref="AttemptResponse"/>.</summary>
    private static AttemptResponse ToAttemptResponse(QuizAttempt a) => new(
        a.Id, a.QuizId, a.StudentProfileId,
        a.Status.ToString(), a.StartedAt, a.FinishedAt, a.TotalScore);

    /// <summary>Maps a <see cref="QuizAttempt"/> and its answers to an <see cref="AttemptDetailResponse"/>.</summary>
    private static AttemptDetailResponse ToAttemptDetail(QuizAttempt a, IEnumerable<QuizAnswer> answers) => new(
        a.Id, a.QuizId, a.StudentProfileId,
        a.Status.ToString(), a.StartedAt, a.FinishedAt, a.TotalScore,
        answers.Select(ans => new AnswerResponse(
            ans.Id, ans.QuizQuestionId, ans.SelectedOptionId, ans.TextResponse, ans.MarksAwarded))
               .ToList());
}
