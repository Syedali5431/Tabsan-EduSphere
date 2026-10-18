using System.Globalization;
using ClosedXML.Excel;
using Tabsan.EduSphere.Application.Quizzes;

namespace Tabsan.EduSphere.Infrastructure.Reporting;

/// <summary>
/// Plan M2.2: question bank workbook. Sheet "Questions": one row per question —
/// Type | Question | Marks | Option 1..6 | Correct (e.g. "2" or "1,3") | Settings JSON | Tags | Difficulty.
/// Option types use the option columns; other types put their settings in "Settings JSON"
/// (see the "Help" sheet). Read never evaluates formulas or macros — values only.
/// </summary>
public sealed class QuestionBankSpreadsheet : IQuestionBankSpreadsheet
{
    private const int OptionColumns = 6;
    private static readonly string[] Headers =
    [
        "Type", "Question", "Marks", "Option 1", "Option 2", "Option 3", "Option 4", "Option 5", "Option 6",
        "Correct", "Settings JSON", "Tags", "Difficulty"
    ];

    public byte[] Export(string bankTitle, IReadOnlyList<QuestionSheetRow> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Questions");
        for (var c = 0; c < Headers.Length; c++)
            sheet.Cell(1, c + 1).Value = Headers[c];
        sheet.Row(1).Style.Font.Bold = true;

        var r = 2;
        foreach (var row in rows)
        {
            sheet.Cell(r, 1).Value = row.Type;
            sheet.Cell(r, 2).Value = row.Text;
            sheet.Cell(r, 3).Value = row.Marks;
            for (var i = 0; i < Math.Min(OptionColumns, row.Options.Count); i++)
                sheet.Cell(r, 4 + i).Value = row.Options[i];
            sheet.Cell(r, 10).Value = string.Join(",", row.CorrectOptions);
            sheet.Cell(r, 11).Value = row.ConfigJson ?? string.Empty;
            sheet.Cell(r, 12).Value = row.Tags ?? string.Empty;
            if (row.Difficulty.HasValue) sheet.Cell(r, 13).Value = row.Difficulty.Value;
            r++;
        }
        sheet.Columns().AdjustToContents(1, Math.Max(2, r), 10, 80);

        AddHelpSheet(workbook, bankTitle);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public IReadOnlyList<QuestionSheetRow> Read(Stream workbookStream, out IReadOnlyList<string> errors)
    {
        var rows = new List<QuestionSheetRow>();
        var problems = new List<string>();
        errors = problems;

        XLWorkbook workbook;
        try { workbook = new XLWorkbook(workbookStream); }
        catch (Exception) { problems.Add("The file is not a readable Excel workbook (.xlsx)."); return rows; }

        using (workbook)
        {
            var sheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("Questions", StringComparison.OrdinalIgnoreCase))
                        ?? workbook.Worksheets.FirstOrDefault();
            if (sheet is null) { problems.Add("The workbook has no worksheets."); return rows; }

            var last = sheet.LastRowUsed()?.RowNumber() ?? 1;
            for (var r = 2; r <= last; r++)
            {
                string Text(int col) => sheet.Cell(r, col).GetFormattedString().Trim();
                var type = Text(1);
                var question = Text(2);
                if (type.Length == 0 && question.Length == 0) continue;

                if (!decimal.TryParse(Text(3), NumberStyles.Number, CultureInfo.InvariantCulture, out var marks))
                {
                    problems.Add($"Row {r}: Marks must be a number.");
                    continue;
                }

                var options = Enumerable.Range(4, OptionColumns).Select(Text).Where(t => t.Length > 0).ToList();
                var correct = new List<int>();
                var correctText = Text(10);
                var badCorrect = false;
                foreach (var part in correctText.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(part, out var n) && n >= 1 && n <= options.Count) correct.Add(n);
                    else badCorrect = true;
                }
                if (badCorrect)
                {
                    problems.Add($"Row {r}: Correct must list option numbers such as 1 or 1,3.");
                    continue;
                }

                int? difficulty = int.TryParse(Text(13), out var d) ? d : null;
                var config = Text(11);
                rows.Add(new QuestionSheetRow(r, type, question, marks, options, correct,
                    config.Length == 0 ? null : config, Text(12) is { Length: > 0 } tags ? tags : null, difficulty));
            }
        }

        return rows;
    }

    private static void AddHelpSheet(XLWorkbook workbook, string bankTitle)
    {
        var help = workbook.AddWorksheet("Help");
        var lines = new[]
        {
            $"Question bank: {bankTitle}",
            "",
            "Types: MultipleChoice, TrueFalse, MultipleAnswers, ShortAnswer, Essay, Matching, Ordering, FillInBlank, Numeric, Formula, FileUpload",
            "MultipleChoice / TrueFalse: fill the option columns and put the number of the correct option in Correct.",
            "MultipleAnswers: options + Correct like 1,3. Settings JSON optional: {\"partialCredit\":false}",
            "ShortAnswer / Essay: no options. Essay settings optional: {\"wordLimit\":500}",
            "Matching: {\"pairs\":[{\"prompt\":\"France\",\"answer\":\"Paris\"},{\"prompt\":\"Italy\",\"answer\":\"Rome\"}],\"distractors\":[\"Berlin\"]}",
            "Ordering (correct order): {\"items\":[{\"text\":\"First\"},{\"text\":\"Second\"},{\"text\":\"Third\"}]}",
            "FillInBlank: put [[1]], [[2]] in the question text, then {\"blanks\":[{\"id\":\"1\",\"answers\":[\"Paris\"]}]}",
            "Numeric: {\"answer\":3.14,\"tolerance\":0.01} or {\"min\":1,\"max\":2}",
            "Formula: text shows [a] and [b]; {\"expression\":\"a*b\",\"variables\":[{\"name\":\"a\",\"min\":1,\"max\":9},{\"name\":\"b\",\"min\":1,\"max\":9}],\"decimals\":2}",
            "FileUpload: {\"allowedExtensions\":[\".pdf\",\".docx\"],\"maxSizeMb\":10}",
            "Tags: comma-separated (used by question-group tag filters). Difficulty: 1 (easy) to 5 (hard)."
        };
        for (var i = 0; i < lines.Length; i++)
            help.Cell(i + 1, 1).Value = lines[i];
        help.Column(1).Width = 140;
    }
}
