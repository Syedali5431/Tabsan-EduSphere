using Tabsan.EduSphere.Application.DTOs.Quizzes;
using Tabsan.EduSphere.Application.Quizzes.Grading;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Quizzes;

namespace Tabsan.EduSphere.Application.Quizzes;

// ── Plan M2.2 contracts ──────────────────────────────────────────────────────

public record BankSummaryResponse(Guid BankId, string Title, string? Description, Guid? CourseOfferingId, Guid? DepartmentId,
    bool IsShared, Guid CreatedByUserId, int QuestionCount);

public record BankDetailResponse(Guid BankId, Guid? TenantId, string Title, string? Description, Guid? CourseOfferingId, Guid? DepartmentId,
    bool IsShared, Guid CreatedByUserId, IReadOnlyList<QuestionResponse> Questions);

public record SaveBankRequest(string Title, string? Description, Guid? CourseOfferingId, Guid? DepartmentId, bool IsShared);

public record AddBankQuestionRequest(string Text, string Type, decimal Marks, IReadOnlyList<AddOptionRequest>? Options,
    string? ConfigJson, string? Tags, int? Difficulty);

/// <summary>One spreadsheet row (import/export).</summary>
public record QuestionSheetRow(int RowNumber, string Type, string Text, decimal Marks, IReadOnlyList<string> Options,
    IReadOnlyList<int> CorrectOptions, string? ConfigJson, string? Tags, int? Difficulty);

public record BankImportResult(int Imported, IReadOnlyList<string> Errors);

/// <summary>Reads/writes question banks as Excel workbooks (implemented in Infrastructure).</summary>
public interface IQuestionBankSpreadsheet
{
    byte[] Export(string bankTitle, IReadOnlyList<QuestionSheetRow> rows);
    IReadOnlyList<QuestionSheetRow> Read(Stream workbook, out IReadOnlyList<string> errors);
}

public interface IQuestionBankService
{
    Task<IReadOnlyList<BankSummaryResponse>> ListAsync(Guid? tenantId, Guid? courseOfferingId, Guid? departmentId, Guid userId, CancellationToken ct = default);
    Task<BankDetailResponse?> GetAsync(Guid bankId, CancellationToken ct = default);
    Task<Guid> CreateAsync(SaveBankRequest request, Guid? tenantId, Guid userId, CancellationToken ct = default);
    Task<string?> UpdateAsync(Guid bankId, SaveBankRequest request, CancellationToken ct = default);
    Task<(bool Found, string? Error)> DeleteAsync(Guid bankId, CancellationToken ct = default);
    Task<QuestionSaveResult> AddQuestionAsync(Guid bankId, AddBankQuestionRequest request, CancellationToken ct = default);
    Task<(byte[] Content, string FileName)?> ExportAsync(Guid bankId, CancellationToken ct = default);
    Task<BankImportResult> ImportAsync(Guid bankId, Stream workbook, CancellationToken ct = default);
}

/// <summary>Plan M2.2: question bank management.</summary>
public sealed class QuestionBankService : IQuestionBankService
{
    private const int MaxImportRows = 1000;
    private readonly IQuizRepository _repo;
    private readonly IQuestionBankSpreadsheet? _sheet;

    public QuestionBankService(IQuizRepository repo, IQuestionBankSpreadsheet? sheet = null)
    {
        _repo = repo;
        _sheet = sheet;
    }

    public async Task<IReadOnlyList<BankSummaryResponse>> ListAsync(Guid? tenantId, Guid? courseOfferingId, Guid? departmentId, Guid userId, CancellationToken ct = default)
    {
        var banks = await _repo.GetBanksAsync(tenantId, courseOfferingId, departmentId, userId, ct);
        var result = new List<BankSummaryResponse>();
        foreach (var b in banks)
            result.Add(new BankSummaryResponse(b.Id, b.Title, b.Description, b.CourseOfferingId, b.DepartmentId, b.IsShared,
                b.CreatedByUserId, await _repo.CountDrawableAsync(b.Id, null, ct)));
        return result;
    }

    public async Task<BankDetailResponse?> GetAsync(Guid bankId, CancellationToken ct = default)
    {
        var bank = await _repo.GetBankAsync(bankId, includeQuestions: true, ct);
        return bank is null ? null : new BankDetailResponse(bank.Id, bank.TenantId, bank.Title, bank.Description, bank.CourseOfferingId,
            bank.DepartmentId, bank.IsShared, bank.CreatedByUserId,
            bank.Questions.Where(q => !q.IsArchived).OrderBy(q => q.OrderIndex).Select(QuizService.ToQuestionResponse).ToList());
    }

    public async Task<Guid> CreateAsync(SaveBankRequest request, Guid? tenantId, Guid userId, CancellationToken ct = default)
    {
        var bank = new QuestionBank(tenantId, request.CourseOfferingId, request.DepartmentId, request.Title, request.Description, request.IsShared, userId);
        await _repo.AddBankAsync(bank, ct);
        await _repo.SaveChangesAsync(ct);
        return bank.Id;
    }

    public async Task<string?> UpdateAsync(Guid bankId, SaveBankRequest request, CancellationToken ct = default)
    {
        var bank = await _repo.GetBankAsync(bankId, false, ct);
        if (bank is null) return "Question bank not found.";
        try { bank.Update(request.Title, request.Description, request.IsShared); }
        catch (ArgumentException ex) { return ex.Message; }
        await _repo.SaveChangesAsync(ct);
        return null;
    }

    public async Task<(bool Found, string? Error)> DeleteAsync(Guid bankId, CancellationToken ct = default)
    {
        var bank = await _repo.GetBankAsync(bankId, false, ct);
        if (bank is null) return (false, null);
        if (await _repo.IsBankUsedAsync(bankId, ct))
            return (true, "Quizzes still draw questions from this bank. Remove those question groups first.");
        bank.SoftDelete();
        await _repo.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<QuestionSaveResult> AddQuestionAsync(Guid bankId, AddBankQuestionRequest request, CancellationToken ct = default)
    {
        var bank = await _repo.GetBankAsync(bankId, includeQuestions: true, ct);
        if (bank is null) return QuestionSaveResult.Fail("Question bank not found.");
        return await AddInternalAsync(bank, request, ct, save: true);
    }

    private async Task<QuestionSaveResult> AddInternalAsync(QuestionBank bank, AddBankQuestionRequest request, CancellationToken ct, bool save)
    {
        if (!QuizService.TryParseType(request.Type, out var type)) return QuestionSaveResult.Fail($"Unknown question type '{request.Type}'.");

        var options = (request.Options ?? []).OrderBy(o => o.OrderIndex).ToList();
        var (config, error) = QuestionAuthoring.Validate(type, request.Text, request.Marks, request.ConfigJson,
            options.Select(o => new AuthoredOption(o.Text, o.IsCorrect)).ToList());
        if (error is not null) return QuestionSaveResult.Fail(error);

        var next = bank.Questions.Select(q => q.OrderIndex).DefaultIfEmpty(0).Max() + 1;
        var question = QuizQuestion.ForBank(bank.Id, request.Text.Trim(), type, request.Marks, next, config, request.Tags, request.Difficulty);
        await _repo.AddQuestionAsync(question, ct);
        if (options.Count > 0)
            await _repo.AddOptionsAsync(options.Select((o, i) => new QuizOption(question.Id, o.Text.Trim(), o.IsCorrect, i + 1)), ct);
        // EF relationship fix-up adds the tracked question to bank.Questions, so OrderIndex keeps increasing within one import.
        if (save) await _repo.SaveChangesAsync(ct);
        return QuestionSaveResult.Ok(question.Id);
    }

    public async Task<(byte[] Content, string FileName)?> ExportAsync(Guid bankId, CancellationToken ct = default)
    {
        if (_sheet is null) return null;
        var bank = await _repo.GetBankAsync(bankId, includeQuestions: true, ct);
        if (bank is null) return null;

        var rows = bank.Questions.Where(q => !q.IsArchived).OrderBy(q => q.OrderIndex).Select((q, i) =>
        {
            var options = q.Options.OrderBy(o => o.OrderIndex).ToList();
            return new QuestionSheetRow(i + 2, q.Type.ToString(), q.Text, q.Marks,
                options.Select(o => o.Text).ToList(),
                options.Select((o, idx) => (o, idx)).Where(x => x.o.IsCorrect).Select(x => x.idx + 1).ToList(),
                QuestionGrading.UsesOptions(q.Type) && q.Type != QuestionType.MultipleAnswers ? null : q.ConfigJson,
                q.Tags, q.Difficulty);
        }).ToList();

        var safeName = string.Concat(bank.Title.Select(c => char.IsLetterOrDigit(c) ? c : '-')).Trim('-');
        return (_sheet.Export(bank.Title, rows), $"question-bank-{(safeName.Length == 0 ? "export" : safeName)}.xlsx");
    }

    /// <summary>Imports every valid row; invalid rows are reported and skipped (nothing partial per row).</summary>
    public async Task<BankImportResult> ImportAsync(Guid bankId, Stream workbook, CancellationToken ct = default)
    {
        if (_sheet is null) return new BankImportResult(0, ["Spreadsheet import is not available."]);
        var bank = await _repo.GetBankAsync(bankId, includeQuestions: true, ct);
        if (bank is null) return new BankImportResult(0, ["Question bank not found."]);

        var rows = _sheet.Read(workbook, out var readErrors);
        var errors = readErrors.ToList();
        if (rows.Count > MaxImportRows) return new BankImportResult(0, [$"A workbook can contain at most {MaxImportRows} questions."]);

        var imported = 0;
        foreach (var row in rows)
        {
            var request = new AddBankQuestionRequest(row.Text, row.Type, row.Marks,
                row.Options.Select((text, i) => new AddOptionRequest(text, row.CorrectOptions.Contains(i + 1), i + 1)).ToList(),
                row.ConfigJson, row.Tags, row.Difficulty);
            var result = await AddInternalAsync(bank, request, ct, save: false);
            if (result.Error is not null) errors.Add($"Row {row.RowNumber}: {result.Error}");
            else imported++;
        }

        if (imported > 0) await _repo.SaveChangesAsync(ct);
        return new BankImportResult(imported, errors);
    }
}
