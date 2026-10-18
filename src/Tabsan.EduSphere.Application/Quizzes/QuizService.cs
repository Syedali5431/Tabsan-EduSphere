using System.Net;
using Tabsan.EduSphere.Application.DTOs.Quizzes;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Quizzes.Grading;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Quizzes;

namespace Tabsan.EduSphere.Application.Quizzes;

/// <summary>
/// Implements <see cref="IQuizService"/> — authoring (this file), delivery
/// (<c>QuizService.Delivery.cs</c>) and grading / analysis (<c>QuizService.Grading.cs</c>).
/// </summary>
public sealed partial class QuizService : IQuizService
{
    private readonly IQuizRepository _repo;
    private readonly IMediaStorageService? _media;
    private readonly IQuizGradebookSync? _gradebook;

    /// <summary>
    /// Initialises the service. Media storage is needed only for file-upload answers; the gradebook
    /// sync (Plan M2) copies finished scores into the "Quiz" result component when registered.
    /// </summary>
    public QuizService(IQuizRepository repo, IMediaStorageService? media = null, IQuizGradebookSync? gradebook = null)
    {
        _repo = repo;
        _media = media;
        _gradebook = gradebook;
    }

    /// <summary>Plan M2: refreshes the students' quiz component after a finish, grade or regrade has been saved.</summary>
    private async Task SyncGradebookAsync(Guid courseOfferingId, IEnumerable<Guid> studentProfileIds, CancellationToken ct)
    {
        if (_gradebook is null) return;
        foreach (var studentProfileId in studentProfileIds.Distinct())
            await _gradebook.RefreshAsync(courseOfferingId, studentProfileId, ct);
    }

    private async Task SyncGradebookForQuizAsync(Guid quizId, IEnumerable<Guid> studentProfileIds, CancellationToken ct)
    {
        if (_gradebook is null) return;
        var quiz = await _repo.GetByIdIncludingInactiveAsync(quizId, ct);
        if (quiz is not null) await SyncGradebookAsync(quiz.CourseOfferingId, studentProfileIds, ct);
    }

    // ── Quiz ──────────────────────────────────────────────────────────────────

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

    /// <summary>Updates the metadata of a quiz. Returns false if not found.</summary>
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

    public Task<bool> PublishAsync(Guid quizId, CancellationToken ct = default) => MutateAsync(quizId, q => q.Publish(), false, ct);
    public Task<bool> UnpublishAsync(Guid quizId, CancellationToken ct = default) => MutateAsync(quizId, q => q.Unpublish(), false, ct);
    public Task<bool> DeactivateAsync(Guid quizId, CancellationToken ct = default) => MutateAsync(quizId, q => q.Deactivate(), false, ct);
    public Task<bool> ActivateAsync(Guid quizId, CancellationToken ct = default) => MutateAsync(quizId, q => q.Activate(), true, ct);

    private async Task<bool> MutateAsync(Guid quizId, Action<Quiz> change, bool includeInactive, CancellationToken ct)
    {
        var quiz = includeInactive ? await _repo.GetByIdIncludingInactiveAsync(quizId, ct) : await _repo.GetByIdAsync(quizId, ct);
        if (quiz is null) return false;
        change(quiz);
        _repo.Update(quiz);
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Plan M2: delivery, security and review settings. Returns an error message, or null.</summary>
    public async Task<string?> ConfigureDeliveryAsync(Guid quizId, QuizDeliverySettingsRequest request, CancellationToken ct = default)
    {
        var quiz = await _repo.GetByIdAsync(quizId, ct);
        if (quiz is null) return "Quiz not found.";

        if (!Enum.TryParse<QuizReviewVisibility>(request.ReviewVisibility, true, out var visibility) || !Enum.IsDefined(visibility))
            return "Unknown review option.";
        if (request.AccessCode is { Length: > 100 }) return "The access code must be 100 characters or fewer.";
        var rangeError = ValidateIpRanges(request.AllowedIpRanges);
        if (rangeError is not null) return rangeError;

        quiz.ConfigureDelivery(request.ShuffleQuestions, request.ShuffleOptions, request.OneQuestionAtATime,
            request.LockAnsweredQuestions, request.AccessCode, request.AllowedIpRanges, visibility, request.ReviewAfterCloseOnly);
        _repo.Update(quiz);
        await _repo.SaveChangesAsync(ct);
        return null;
    }

    // ── Questions ─────────────────────────────────────────────────────────────

    /// <summary>Adds a validated question (with options) to a quiz.</summary>
    public async Task<QuestionSaveResult> AddQuestionAsync(AddQuestionRequest request, CancellationToken ct = default)
    {
        if (await _repo.GetByIdAsync(request.QuizId, ct) is null) return QuestionSaveResult.Fail("Quiz not found.");
        if (!TryParseType(request.Type, out var type)) return QuestionSaveResult.Fail("Unknown question type.");

        var options = (request.Options ?? []).OrderBy(o => o.OrderIndex).ToList();
        var (config, error) = QuestionAuthoring.Validate(type, request.Text, request.Marks, request.ConfigJson,
            options.Select(o => new AuthoredOption(o.Text, o.IsCorrect)).ToList());
        if (error is not null) return QuestionSaveResult.Fail(error);

        var question = new QuizQuestion(request.QuizId, request.Text.Trim(), type, request.Marks, request.OrderIndex, config);
        await _repo.AddQuestionAsync(question, ct);
        if (options.Count > 0)
            await _repo.AddOptionsAsync(options.Select((o, i) => new QuizOption(question.Id, o.Text.Trim(), o.IsCorrect, i + 1)), ct);
        await _repo.SaveChangesAsync(ct);
        return QuestionSaveResult.Ok(question.Id);
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

    /// <summary>
    /// Plan M2: full edit. Once students have answered, options can be edited in place (text,
    /// correctness) but not added or removed — use Regrade to rescore existing attempts.
    /// </summary>
    public async Task<QuestionSaveResult> SaveQuestionAsync(Guid questionId, SaveQuestionRequest request, CancellationToken ct = default)
    {
        var question = await _repo.GetQuestionByIdAsync(questionId, ct);
        if (question is null) return QuestionSaveResult.Fail("Question not found.");

        var options = (request.Options ?? []).OrderBy(o => o.OrderIndex).ToList();
        var (config, error) = QuestionAuthoring.Validate(question.Type, request.Text, request.Marks, request.ConfigJson,
            options.Select(o => new AuthoredOption(o.Text, o.IsCorrect)).ToList());
        if (error is not null) return QuestionSaveResult.Fail(error);

        var existing = question.Options.OrderBy(o => o.OrderIndex).ToList();
        var hasAnswers = await _repo.QuestionHasAnswersAsync(questionId, ct);
        if (hasAnswers && existing.Count != options.Count)
            return QuestionSaveResult.Fail("Students have already answered this question: you can edit option text and correctness, but not add or remove options.");

        question.Update(request.Text.Trim(), request.Marks, request.OrderIndex);
        question.SetConfig(config);
        if (question.IsBankQuestion)
            question.SetBankMetadata(request.Tags, request.Difficulty);

        if (existing.Count == options.Count)
        {
            for (var i = 0; i < options.Count; i++)
                existing[i].Update(options[i].Text.Trim(), options[i].IsCorrect, i + 1);
        }
        else
        {
            _repo.RemoveOptions(existing);
            await _repo.AddOptionsAsync(options.Select((o, i) => new QuizOption(question.Id, o.Text.Trim(), o.IsCorrect, i + 1)), ct);
        }

        await _repo.SaveChangesAsync(ct);
        return QuestionSaveResult.Ok(question.Id);
    }

    /// <summary>Removes a question and its options. Questions with answers are kept (history).</summary>
    public async Task<(bool Found, string? Error)> RemoveQuestionAsync(Guid questionId, CancellationToken ct = default)
    {
        var question = await _repo.GetQuestionByIdAsync(questionId, ct);
        if (question is null) return (false, null);

        if (await _repo.QuestionHasAnswersAsync(questionId, ct))
        {
            if (!question.IsBankQuestion)
                return (true, "Students have already answered this question, so it cannot be deleted. Edit it or give everyone full credit with Regrade.");
            question.Archive(); // bank question: stop drawing it, keep history
        }
        else
        {
            _repo.RemoveOptions(question.Options);
            _repo.RemoveQuestion(question);
        }

        await _repo.SaveChangesAsync(ct);
        return (true, null);
    }

    // ── Plan M2.2: bank draw groups and copying ───────────────────────────────

    public async Task<QuestionSaveResult> AddGroupAsync(Guid quizId, SaveQuestionGroupRequest request, CancellationToken ct = default)
    {
        if (await _repo.GetByIdAsync(quizId, ct) is null) return QuestionSaveResult.Fail("Quiz not found.");
        if (await _repo.GetBankAsync(request.QuestionBankId, false, ct) is null) return QuestionSaveResult.Fail("Question bank not found.");
        try
        {
            var group = new QuizQuestionGroup(quizId, request.QuestionBankId, request.Title, request.PickCount,
                request.MarksPerQuestion, request.OrderIndex, request.TagFilter);
            await _repo.AddGroupAsync(group, ct);
            await _repo.SaveChangesAsync(ct);
            return QuestionSaveResult.Ok(group.Id);
        }
        catch (ArgumentException ex) { return QuestionSaveResult.Fail(ex.Message); }
    }

    public async Task<QuestionSaveResult> UpdateGroupAsync(Guid groupId, SaveQuestionGroupRequest request, CancellationToken ct = default)
    {
        var group = await _repo.GetGroupAsync(groupId, ct);
        if (group is null) return QuestionSaveResult.Fail("Question group not found.");
        try
        {
            group.Update(request.Title, request.PickCount, request.MarksPerQuestion, request.OrderIndex, request.TagFilter);
            await _repo.SaveChangesAsync(ct);
            return QuestionSaveResult.Ok(group.Id);
        }
        catch (ArgumentException ex) { return QuestionSaveResult.Fail(ex.Message); }
    }

    public async Task<bool> RemoveGroupAsync(Guid groupId, CancellationToken ct = default)
    {
        var group = await _repo.GetGroupAsync(groupId, ct);
        if (group is null) return false;
        _repo.RemoveGroup(group); // attempts keep their own snapshot items
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<(int Copied, string? Error)> CopyFromBankAsync(Guid quizId, CopyBankQuestionsRequest request, CancellationToken ct = default)
    {
        var quiz = await _repo.GetWithQuestionsAsync(quizId, ct);
        if (quiz is null) return (0, "Quiz not found.");
        if (request.QuestionIds is not { Count: > 0 }) return (0, "Select at least one question.");

        var source = (await _repo.GetQuestionsWithOptionsAsync(request.QuestionIds.Distinct().ToList(), ct))
            .Where(q => q.IsBankQuestion && !q.IsArchived)
            .OrderBy(q => request.QuestionIds.ToList().IndexOf(q.Id))
            .ToList();
        if (source.Count == 0) return (0, "None of the selected questions were found in a question bank.");

        var next = quiz.Questions.Select(q => q.OrderIndex).DefaultIfEmpty(0).Max() + 1;
        foreach (var bankQuestion in source)
        {
            var copy = bankQuestion.CloneToQuiz(quizId, next++);
            await _repo.AddQuestionAsync(copy, ct);
            await _repo.AddOptionsAsync(bankQuestion.Options.OrderBy(o => o.OrderIndex)
                .Select((o, i) => new QuizOption(copy.Id, o.Text, o.IsCorrect, i + 1)), ct);
        }

        await _repo.SaveChangesAsync(ct);
        return (source.Count, null);
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Returns all quizzes for a course offering.</summary>
    public async Task<IReadOnlyList<QuizSummaryResponse>> GetByOfferingAsync(Guid courseOfferingId, bool includeInactive = false, CancellationToken ct = default)
    {
        var quizzes = await _repo.GetByOfferingAsync(courseOfferingId, includeInactive, ct);
        return quizzes.Select(ToSummary).ToList();
    }

    /// <summary>Faculty view of a quiz, including correctness and settings.</summary>
    public async Task<QuizDetailResponse?> GetDetailAsync(Guid quizId, CancellationToken ct = default)
    {
        var quiz = await _repo.GetForAuthoringAsync(quizId, ct);
        if (quiz is null) return null;

        var groups = new List<QuestionGroupResponse>();
        foreach (var g in quiz.QuestionGroups.OrderBy(g => g.OrderIndex))
            groups.Add(new QuestionGroupResponse(g.Id, g.QuestionBankId, g.QuestionBank?.Title ?? "(deleted bank)", g.Title,
                g.PickCount, g.MarksPerQuestion, g.OrderIndex, g.TagFilter, await _repo.CountDrawableAsync(g.QuestionBankId, g.TagFilter, ct)));

        var totalMarks = quiz.Questions.Sum(q => q.Marks) + groups.Sum(g => Math.Min(g.PickCount, g.AvailableQuestions) * g.MarksPerQuestion);

        return new QuizDetailResponse(
            quiz.Id, quiz.CourseOfferingId, quiz.Title, quiz.Instructions,
            quiz.TimeLimitMinutes, quiz.MaxAttempts, quiz.AvailableFrom, quiz.AvailableUntil, quiz.IsPublished,
            quiz.Questions.OrderBy(x => x.OrderIndex).Select(ToQuestionResponse).ToList(),
            ToSettings(quiz), groups, totalMarks);
    }

    /// <summary>What a student may see before starting — never the questions or answers.</summary>
    public async Task<StudentQuizInfoResponse?> GetStudentInfoAsync(Guid quizId, CancellationToken ct = default)
    {
        var quiz = await _repo.GetForAuthoringAsync(quizId, ct);
        if (quiz is null || !quiz.IsPublished) return null;
        var count = quiz.Questions.Count + quiz.QuestionGroups.Sum(g => g.PickCount);
        return new StudentQuizInfoResponse(quiz.Id, quiz.Title, quiz.Instructions, quiz.TimeLimitMinutes, quiz.MaxAttempts,
            quiz.AvailableFrom, quiz.AvailableUntil, quiz.AccessCode is not null, count);
    }

    public async Task<Guid?> GetCourseOfferingIdAsync(Guid quizId, CancellationToken ct = default)
        => (await _repo.GetByIdIncludingInactiveAsync(quizId, ct))?.CourseOfferingId;

    // ── Mapping helpers ───────────────────────────────────────────────────────

    internal static bool TryParseType(string? value, out QuestionType type)
        => Enum.TryParse(value?.Trim(), true, out type) && Enum.IsDefined(type);

    private static QuizSummaryResponse ToSummary(Quiz q) => new(
        q.Id, q.CourseOfferingId, q.Title,
        q.TimeLimitMinutes, q.MaxAttempts,
        q.AvailableFrom, q.AvailableUntil, q.IsPublished, q.IsActive,
        q.Questions.Count + q.QuestionGroups.Sum(g => g.PickCount));

    internal static QuestionResponse ToQuestionResponse(QuizQuestion q) => new(
        q.Id, q.Text, q.Type.ToString(), q.Marks, q.OrderIndex,
        q.Options.OrderBy(o => o.OrderIndex)
                 .Select(o => new OptionResponse(o.Id, o.Text, o.OrderIndex, o.IsCorrect))
                 .ToList(),
        q.ConfigJson, q.Tags, q.Difficulty, q.IsArchived);

    private static QuizDeliverySettingsResponse ToSettings(Quiz q) => new(
        q.ShuffleQuestions, q.ShuffleOptions, q.OneQuestionAtATime, q.LockAnsweredQuestions,
        q.AccessCode, q.AllowedIpRanges, q.ReviewVisibility.ToString(), q.ReviewAfterCloseOnly);

    private static AttemptResponse ToAttemptResponse(QuizAttempt a) => new(
        a.Id, a.QuizId, a.StudentProfileId,
        a.Status.ToString(), a.StartedAt, a.FinishedAt, a.TotalScore, a.DeadlineAt);

    // ── IP allow-list ─────────────────────────────────────────────────────────

    public static string? ValidateIpRanges(string? ranges)
    {
        if (string.IsNullOrWhiteSpace(ranges)) return null;
        foreach (var entry in SplitRanges(ranges))
            if (!IPNetwork.TryParse(entry, out _) && !IPAddress.TryParse(entry, out _))
                return $"'{entry}' is not a valid IP address or CIDR range.";
        return null;
    }

    public static bool IsIpAllowed(string? ranges, string? clientIp)
    {
        if (string.IsNullOrWhiteSpace(ranges)) return true;
        if (string.IsNullOrWhiteSpace(clientIp) || !IPAddress.TryParse(clientIp, out var ip)) return false;
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();

        foreach (var entry in SplitRanges(ranges))
        {
            if (IPAddress.TryParse(entry, out var single))
            {
                if (single.IsIPv4MappedToIPv6) single = single.MapToIPv4();
                if (single.Equals(ip)) return true;
            }
            else if (IPNetwork.TryParse(entry, out var network) && network.Contains(ip))
            {
                return true;
            }
        }
        return false;
    }

    private static IEnumerable<string> SplitRanges(string ranges)
        => ranges.Split([',', ';', ' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
