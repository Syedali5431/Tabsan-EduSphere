using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tabsan.EduSphere.Application.DTOs.Quizzes;
using Tabsan.EduSphere.Application.Quizzes;

namespace Tabsan.EduSphere.Web.Services;

// Plan M2 — quiz engine calls used by the builder, bank, take-quiz, review, grading and statistics pages.

/// <summary>Result of a start request (attempt or a coded refusal such as ACCESS_CODE_REQUIRED).</summary>
public sealed record QuizStartOutcome(AttemptResponse? Attempt, string? ErrorCode, string? Message);

/// <summary>Result of saving answers (or a coded refusal such as TIME_EXPIRED).</summary>
public sealed record QuizSaveOutcome(bool Ok, int Saved, int LockedSkipped, string? ErrorCode, string? Message);

/// <summary>One row of the faculty attempts list.</summary>
public sealed record QuizAttemptRow(Guid AttemptId, Guid StudentProfileId, string? StudentName, string? RegistrationNumber,
    string Status, DateTime StartedAt, DateTime? FinishedAt, decimal? TotalScore);

public sealed record BankImportOutcome(int Imported, List<string> Errors);

public partial interface IEduApiClient
{
    // Authoring
    Task<QuizDetailResponse?> GetQuizBuilderAsync(Guid quizId, CancellationToken ct);
    Task SaveQuizSettingsAsync(Guid quizId, QuizDeliverySettingsRequest request, CancellationToken ct);
    Task<Guid> AddQuizQuestionAsync(AddQuestionRequest request, CancellationToken ct);
    Task SaveQuizQuestionAsync(Guid questionId, SaveQuestionRequest request, CancellationToken ct);
    Task DeleteQuizQuestionAsync(Guid questionId, CancellationToken ct);
    Task AddQuizGroupAsync(Guid quizId, SaveQuestionGroupRequest request, CancellationToken ct);
    Task DeleteQuizGroupAsync(Guid groupId, CancellationToken ct);
    Task<int> CopyBankQuestionsAsync(Guid quizId, IReadOnlyList<Guid> questionIds, CancellationToken ct);

    // Banks
    Task<List<BankSummaryResponse>> GetQuestionBanksAsync(Guid? courseOfferingId, CancellationToken ct);
    Task<BankDetailResponse?> GetQuestionBankAsync(Guid bankId, CancellationToken ct);
    Task<Guid> CreateQuestionBankAsync(SaveBankRequest request, CancellationToken ct);
    Task DeleteQuestionBankAsync(Guid bankId, CancellationToken ct);
    Task<Guid> AddBankQuestionAsync(Guid bankId, AddBankQuestionRequest request, CancellationToken ct);
    Task<byte[]> ExportQuestionBankAsync(Guid bankId, CancellationToken ct);
    Task<BankImportOutcome> ImportQuestionBankAsync(Guid bankId, byte[] workbook, string fileName, CancellationToken ct);

    // Student delivery
    Task<StudentQuizInfoResponse?> GetQuizInfoAsync(Guid quizId, CancellationToken ct);
    Task<QuizStartOutcome> StartQuizAsync(Guid quizId, string? accessCode, CancellationToken ct);
    Task<StudentAttemptPaper?> GetQuizPaperAsync(Guid attemptId, CancellationToken ct);
    Task<QuizSaveOutcome> SaveQuizAnswersAsync(Guid attemptId, IReadOnlyList<SubmitAnswerEntry> answers, CancellationToken ct);
    Task<QuizSaveOutcome> UploadQuizAnswerFileAsync(Guid attemptId, Guid questionId, byte[] content, string fileName, string? contentType, CancellationToken ct);
    Task<AttemptDetailResponse?> SubmitQuizAsync(Guid attemptId, IReadOnlyList<SubmitAnswerEntry> answers, CancellationToken ct);
    Task<AttemptDetailResponse?> GetQuizReviewAsync(Guid attemptId, CancellationToken ct);

    // Grading & analysis
    Task<List<QuizAttemptRow>> GetQuizAttemptsAsync(Guid quizId, CancellationToken ct);
    Task<AttemptDetailResponse?> GetQuizAttemptDetailAsync(Guid attemptId, CancellationToken ct);
    Task<List<GradingQueueItem>> GetQuizGradingQueueAsync(Guid quizId, CancellationToken ct);
    Task GradeQuizAnswerAsync(Guid answerId, decimal marks, string? feedback, CancellationToken ct);
    Task<int> RegradeQuizQuestionAsync(Guid quizId, Guid questionId, string mode, CancellationToken ct);
    Task<QuizStatisticsResponse?> GetQuizStatisticsAsync(Guid quizId, CancellationToken ct);
    Task<(byte[] Content, string FileName, string ContentType)?> DownloadQuizAnswerFileAsync(Guid answerId, CancellationToken ct);

    // Accommodations & overrides
    Task<List<AccommodationResponse>> GetQuizAccommodationsAsync(Guid courseOfferingId, CancellationToken ct);
    Task SaveQuizAccommodationAsync(SaveAccommodationRequest request, CancellationToken ct);
    Task DeleteQuizAccommodationAsync(Guid id, CancellationToken ct);
    Task<List<AvailabilityOverrideResponse>> GetQuizOverridesAsync(Guid quizId, CancellationToken ct);
    Task SaveQuizOverrideAsync(Guid quizId, SaveAvailabilityOverrideRequest request, CancellationToken ct);
    Task DeleteQuizOverrideAsync(Guid id, CancellationToken ct);
}

public partial class EduApiClient
{
    // ── Authoring ─────────────────────────────────────────────────────────────

    public Task<QuizDetailResponse?> GetQuizBuilderAsync(Guid quizId, CancellationToken ct)
        => GetAsync<QuizDetailResponse>($"api/v1/quiz/{quizId}", ct);

    public Task SaveQuizSettingsAsync(Guid quizId, QuizDeliverySettingsRequest request, CancellationToken ct)
        => PutAsync<QuizDeliverySettingsRequest, object>($"api/v1/quiz/{quizId}/settings", request, ct);

    public async Task<Guid> AddQuizQuestionAsync(AddQuestionRequest request, CancellationToken ct)
        => ReadGuid(await PostAsync<AddQuestionRequest, JsonElement>("api/v1/quiz/question", request, ct), "questionId");

    public Task SaveQuizQuestionAsync(Guid questionId, SaveQuestionRequest request, CancellationToken ct)
        => PutAsync<SaveQuestionRequest, object>($"api/v1/quiz/question/{questionId}/full", request, ct);

    public Task DeleteQuizQuestionAsync(Guid questionId, CancellationToken ct)
        => DeleteAsync($"api/v1/quiz/question/{questionId}", ct);

    public Task AddQuizGroupAsync(Guid quizId, SaveQuestionGroupRequest request, CancellationToken ct)
        => PostAsync<SaveQuestionGroupRequest, object>($"api/v1/quiz/{quizId}/groups", request, ct);

    public Task DeleteQuizGroupAsync(Guid groupId, CancellationToken ct)
        => DeleteAsync($"api/v1/quiz/groups/{groupId}", ct);

    public async Task<int> CopyBankQuestionsAsync(Guid quizId, IReadOnlyList<Guid> questionIds, CancellationToken ct)
    {
        var result = await PostAsync<CopyBankQuestionsRequest, JsonElement>($"api/v1/quiz/{quizId}/copy-from-bank", new CopyBankQuestionsRequest(questionIds), ct);
        return result.TryGetProperty("copied", out var copied) ? copied.GetInt32() : 0;
    }

    // ── Banks ─────────────────────────────────────────────────────────────────

    public async Task<List<BankSummaryResponse>> GetQuestionBanksAsync(Guid? courseOfferingId, CancellationToken ct)
        => await GetAsync<List<BankSummaryResponse>>("api/v1/quiz/banks" + (courseOfferingId.HasValue ? $"?courseOfferingId={courseOfferingId}" : string.Empty), ct) ?? new();

    public Task<BankDetailResponse?> GetQuestionBankAsync(Guid bankId, CancellationToken ct)
        => GetAsync<BankDetailResponse>($"api/v1/quiz/banks/{bankId}", ct);

    public async Task<Guid> CreateQuestionBankAsync(SaveBankRequest request, CancellationToken ct)
        => ReadGuid(await PostAsync<SaveBankRequest, JsonElement>("api/v1/quiz/banks", request, ct), "bankId");

    public Task DeleteQuestionBankAsync(Guid bankId, CancellationToken ct)
        => DeleteAsync($"api/v1/quiz/banks/{bankId}", ct);

    public async Task<Guid> AddBankQuestionAsync(Guid bankId, AddBankQuestionRequest request, CancellationToken ct)
        => ReadGuid(await PostAsync<AddBankQuestionRequest, JsonElement>($"api/v1/quiz/banks/{bankId}/questions", request, ct), "questionId");

    public Task<byte[]> ExportQuestionBankAsync(Guid bankId, CancellationToken ct)
        => GetBytesAsync($"api/v1/quiz/banks/{bankId}/export", ct);

    public async Task<BankImportOutcome> ImportQuestionBankAsync(Guid bankId, byte[] workbook, string fileName, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(HttpMethod.Post, $"api/v1/quiz/banks/{bankId}/import");
            var content = new MultipartFormDataContent();
            var file = new ByteArrayContent(workbook);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            content.Add(file, "file", fileName);
            request.Content = content;
            return request;
        }, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw BuildException(response.StatusCode, body);

        using var doc = JsonDocument.Parse(body);
        var errors = doc.RootElement.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Array
            ? e.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList()
            : new List<string>();
        return new BankImportOutcome(doc.RootElement.GetProperty("imported").GetInt32(), errors);
    }

    // ── Student delivery ──────────────────────────────────────────────────────

    public Task<StudentQuizInfoResponse?> GetQuizInfoAsync(Guid quizId, CancellationToken ct)
        => TryGetAsync<StudentQuizInfoResponse>($"api/v1/quiz/{quizId}/info", ct);

    public async Task<QuizStartOutcome> StartQuizAsync(Guid quizId, string? accessCode, CancellationToken ct)
    {
        var (ok, body) = await SendJsonAsync(HttpMethod.Post, $"api/v1/quiz/{quizId}/start", new StartAttemptRequest(accessCode), ct);
        if (ok) return new QuizStartOutcome(JsonSerializer.Deserialize<AttemptResponse>(body, _jsonOptions), null, null);
        var (code, message) = ReadError(body);
        return new QuizStartOutcome(null, code, message ?? "The quiz could not be started.");
    }

    /// <summary>The paper, or null when the attempt is closed (submitted, or time ran out — the API answers 409).</summary>
    public async Task<StudentAttemptPaper?> GetQuizPaperAsync(Guid attemptId, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() => CreateRequest(HttpMethod.Get, $"api/v1/quiz/attempt/{attemptId}/paper"), ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (response.IsSuccessStatusCode)
            return string.IsNullOrWhiteSpace(body) ? null : JsonSerializer.Deserialize<StudentAttemptPaper>(body, _jsonOptions);
        if (response.StatusCode == HttpStatusCode.Conflict) return null;
        var (_, message) = ReadError(body);
        throw new InvalidOperationException(message ?? "This attempt is not available.");
    }

    public async Task<QuizSaveOutcome> SaveQuizAnswersAsync(Guid attemptId, IReadOnlyList<SubmitAnswerEntry> answers, CancellationToken ct)
    {
        var (ok, body) = await SendJsonAsync(HttpMethod.Post, $"api/v1/quiz/attempt/{attemptId}/answers", new SaveAnswersRequest(answers), ct);
        if (!ok)
        {
            var (code, message) = ReadError(body);
            return new QuizSaveOutcome(false, 0, 0, code, message);
        }
        using var doc = JsonDocument.Parse(body);
        return new QuizSaveOutcome(true, doc.RootElement.GetProperty("saved").GetInt32(), doc.RootElement.GetProperty("lockedSkipped").GetInt32(), null, null);
    }

    public async Task<QuizSaveOutcome> UploadQuizAnswerFileAsync(Guid attemptId, Guid questionId, byte[] content, string fileName, string? contentType, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(HttpMethod.Post, $"api/v1/quiz/attempt/{attemptId}/files/{questionId}");
            var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(content);
            file.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
            form.Add(file, "file", fileName);
            request.Content = form;
            return request;
        }, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (response.IsSuccessStatusCode) return new QuizSaveOutcome(true, 1, 0, null, null);
        var (code, message) = ReadError(body);
        return new QuizSaveOutcome(false, 0, 0, code, message ?? "The file could not be uploaded.");
    }

    public async Task<AttemptDetailResponse?> SubmitQuizAsync(Guid attemptId, IReadOnlyList<SubmitAnswerEntry> answers, CancellationToken ct)
    {
        var (ok, body) = await SendJsonAsync(HttpMethod.Post, "api/v1/quiz/attempt/submit", new SubmitAttemptRequest(attemptId, answers), ct);
        if (ok) return JsonSerializer.Deserialize<AttemptDetailResponse>(body, _jsonOptions);
        var (code, message) = ReadError(body);
        throw new InvalidOperationException(message ?? code ?? "The quiz could not be submitted.");
    }

    public Task<AttemptDetailResponse?> GetQuizReviewAsync(Guid attemptId, CancellationToken ct)
        => TryGetAsync<AttemptDetailResponse>($"api/v1/quiz/attempt/{attemptId}/review", ct);

    // ── Grading & analysis ────────────────────────────────────────────────────

    public async Task<List<QuizAttemptRow>> GetQuizAttemptsAsync(Guid quizId, CancellationToken ct)
        => await GetAsync<List<QuizAttemptRow>>($"api/v1/quiz/{quizId}/attempts", ct) ?? new();

    public Task<AttemptDetailResponse?> GetQuizAttemptDetailAsync(Guid attemptId, CancellationToken ct)
        => GetAsync<AttemptDetailResponse>($"api/v1/quiz/attempt/{attemptId}", ct);

    public async Task<List<GradingQueueItem>> GetQuizGradingQueueAsync(Guid quizId, CancellationToken ct)
        => await GetAsync<List<GradingQueueItem>>($"api/v1/quiz/{quizId}/grading-queue", ct) ?? new();

    public Task GradeQuizAnswerAsync(Guid answerId, decimal marks, string? feedback, CancellationToken ct)
        => PostAsync<GradeAnswerRequest, object>("api/v1/quiz/attempt/grade-answer", new GradeAnswerRequest(answerId, marks, feedback), ct);

    public async Task<int> RegradeQuizQuestionAsync(Guid quizId, Guid questionId, string mode, CancellationToken ct)
    {
        var result = await PostAsync<RegradeQuestionRequest, JsonElement>($"api/v1/quiz/{quizId}/questions/{questionId}/regrade", new RegradeQuestionRequest(mode), ct);
        return result.TryGetProperty("attemptsUpdated", out var n) ? n.GetInt32() : 0;
    }

    public Task<QuizStatisticsResponse?> GetQuizStatisticsAsync(Guid quizId, CancellationToken ct)
        => GetAsync<QuizStatisticsResponse>($"api/v1/quiz/{quizId}/statistics", ct);

    public async Task<(byte[] Content, string FileName, string ContentType)?> DownloadQuizAnswerFileAsync(Guid answerId, CancellationToken ct)
    {
        using var response = await SendWithAutoRefreshAsync(() => CreateRequest(HttpMethod.Get, $"api/v1/quiz/answer/{answerId}/file"), ct);
        if (!response.IsSuccessStatusCode) return null;
        var name = response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "answer";
        return (await response.Content.ReadAsByteArrayAsync(ct), name, response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
    }

    // ── Accommodations & overrides ────────────────────────────────────────────

    public async Task<List<AccommodationResponse>> GetQuizAccommodationsAsync(Guid courseOfferingId, CancellationToken ct)
        => await GetAsync<List<AccommodationResponse>>($"api/v1/quiz/accommodations/by-offering/{courseOfferingId}", ct) ?? new();

    public Task SaveQuizAccommodationAsync(SaveAccommodationRequest request, CancellationToken ct)
        => PostAsync<SaveAccommodationRequest, object>("api/v1/quiz/accommodations", request, ct);

    public Task DeleteQuizAccommodationAsync(Guid id, CancellationToken ct)
        => DeleteAsync($"api/v1/quiz/accommodations/{id}", ct);

    public async Task<List<AvailabilityOverrideResponse>> GetQuizOverridesAsync(Guid quizId, CancellationToken ct)
        => await GetAsync<List<AvailabilityOverrideResponse>>($"api/v1/quiz/{quizId}/overrides", ct) ?? new();

    public Task SaveQuizOverrideAsync(Guid quizId, SaveAvailabilityOverrideRequest request, CancellationToken ct)
        => PostAsync<SaveAvailabilityOverrideRequest, object>($"api/v1/quiz/{quizId}/overrides", request, ct);

    public Task DeleteQuizOverrideAsync(Guid id, CancellationToken ct)
        => DeleteAsync($"api/v1/quiz/overrides/{id}", ct);

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<(bool Ok, string Body)> SendJsonAsync(HttpMethod method, string path, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        using var response = await SendWithAutoRefreshAsync(() =>
        {
            var request = CreateRequest(method, path);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return request;
        }, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw BuildException(response.StatusCode, body);
        return (response.IsSuccessStatusCode, body);
    }

    private static (string? Code, string? Message) ReadError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return (null, body);
            string? Str(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            return (Str("error"), Str("message") ?? Str("detail") ?? Str("title"));
        }
        catch (JsonException)
        {
            return (null, string.IsNullOrWhiteSpace(body) ? null : body);
        }
    }

    private static Guid ReadGuid(JsonElement element, string name)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var v) && v.TryGetGuid(out var id) ? id : Guid.Empty;
}
