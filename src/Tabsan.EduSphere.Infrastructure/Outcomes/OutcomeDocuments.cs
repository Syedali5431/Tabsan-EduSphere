using ClosedXML.Excel;
using QuestPDF.Fluent;
using Tabsan.EduSphere.Infrastructure.Reporting;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Tabsan.EduSphere.Application.Outcomes;

namespace Tabsan.EduSphere.Infrastructure.Outcomes;

/// <summary>Plan M6 — outcome import workbook, Learning Mastery Gradebook export and program attainment report (Excel and PDF).</summary>
public sealed class OutcomeDocuments : IOutcomeDocuments
{
    private static readonly string[] ImportHeaders =
        ["Level", "Program code", "Course code", "Code", "Title", "Description", "Mastery points", "Method", "Parameter", "Parent code", "Maps to"];

    static OutcomeDocuments() => QuestPDF.Settings.License = LicenseType.Community;

    public (IReadOnlyList<OutcomeImportRow> Rows, string? Error) ReadImport(Stream workbook)
    {
        XLWorkbook book;
        try { book = new XLWorkbook(workbook); }
        catch (Exception) { return ([], "The file is not an Excel workbook (.xlsx)."); }
        using (book)
        {
            var sheet = book.Worksheets.FirstOrDefault(w => string.Equals(w.Name, "Outcomes", StringComparison.OrdinalIgnoreCase)) ?? book.Worksheets.First();
            var header = sheet.Row(1).CellsUsed().ToDictionary(c => c.GetString().Trim(), c => c.Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);
            foreach (var required in new[] { "Level", "Code", "Title" })
                if (!header.ContainsKey(required)) return ([], $"The sheet needs a \"{required}\" column (download the template).");

            string? Cell(IXLRow row, string name) => header.TryGetValue(name, out var col) && row.Cell(col).GetString().Trim() is { Length: > 0 } v ? v : null;
            decimal? Dec(IXLRow row, string name) => decimal.TryParse(Cell(row, name), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;
            int? Int(IXLRow row, string name) => int.TryParse(Cell(row, name), out var i) ? i : null;

            var rows = new List<OutcomeImportRow>();
            var last = sheet.LastRowUsed()?.RowNumber() ?? 1;
            for (var r = 2; r <= Math.Min(last, 2001); r++)
            {
                var row = sheet.Row(r);
                var code = Cell(row, "Code");
                var title = Cell(row, "Title");
                var level = Cell(row, "Level");
                if (code is null && title is null && level is null) continue;
                rows.Add(new OutcomeImportRow(r, level ?? "", Cell(row, "Program code"), Cell(row, "Course code"), code ?? "", title ?? "",
                    Cell(row, "Description"), Dec(row, "Mastery points"), Cell(row, "Method"), Int(row, "Parameter"), Cell(row, "Parent code"), Cell(row, "Maps to")));
            }
            return rows.Count == 0 ? ([], "The sheet has no outcome rows.") : (rows, null);
        }
    }

    public byte[] ImportTemplate()
    {
        using var book = new XLWorkbook();
        var sheet = book.Worksheets.Add("Outcomes");
        for (var i = 0; i < ImportHeaders.Length; i++) sheet.Cell(1, i + 1).Value = ImportHeaders[i];
        object[][] examples =
        [
            ["Institution", "", "", "ILO1", "Communicate effectively in writing and speech", "", 3, "DecayingAverage", 65, "", ""],
            ["Program", "BSCS", "", "PLO1", "Apply knowledge of computing and mathematics", "", 3, "DecayingAverage", 65, "", ""],
            ["Program", "BSCS", "", "PLO2", "Analyse a problem and design a computing solution", "", 3, "Highest", "", "", ""],
            ["Course", "", "CS101", "CLO1", "Write programs using control structures and functions", "", 3, "Latest", "", "", "PLO1"],
            ["Course", "", "CS101", "CLO2", "Design an algorithm for a given problem", "", 3, "NMastery", 2, "", "BSCS:PLO2"]
        ];
        for (var r = 0; r < examples.Length; r++)
            for (var c = 0; c < examples[r].Length; c++)
                sheet.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(examples[r][c]);
        StyleHeader(sheet.Range(1, 1, 1, ImportHeaders.Length));
        sheet.Columns().AdjustToContents(1, 20, 8, 60);

        var help = book.Worksheets.Add("How to fill");
        string[] notes =
        [
            "Level: Institution, Program or Course.",
            "Program code: the academic program's code (Program outcomes only).",
            "Course code: the course's code (Course outcomes only).",
            "Code and Title are required; codes are unique per program or course.",
            "Mastery points: on the default 0–4 scale (Exceeds 4, Meets 3, Near 2, Below 1, No evidence 0). Default 3.",
            "Method: Highest, Latest, Average, DecayingAverage (Parameter = weight of the latest score in %, default 65) or NMastery (Parameter = times, default 5).",
            "Parent code: another outcome on the same level (optional).",
            "Maps to: for course outcomes, the program outcome codes it contributes to, separated by commas; write PROGRAM:CODE when a code is used in several programs.",
            "Existing outcomes with the same level, program/course and code are updated."
        ];
        for (var i = 0; i < notes.Length; i++) help.Cell(i + 1, 1).Value = notes[i];
        help.Column(1).Width = 130;
        return Save(book);
    }

    public byte[] MasteryWorkbook(MasteryGradebookResponse g)
    {
        using var book = new XLWorkbook();
        var sheet = book.Worksheets.Add("Mastery");
        sheet.Cell(1, 1).Value = $"Learning Mastery Gradebook — {g.CourseCode} {g.CourseTitle}{(g.SemesterName is null ? "" : $" ({g.SemesterName})")}";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(3, 1).Value = "Student";
        sheet.Cell(3, 2).Value = "Registration no.";
        for (var i = 0; i < g.Outcomes.Count; i++)
        {
            var o = g.Outcomes[i];
            sheet.Cell(3, i + 3).Value = $"{o.Code} (mastery {o.MasteryPoints:0.##}/{o.MaxPoints:0.##})";
            sheet.Cell(4, i + 3).Value = o.Title;
        }
        StyleHeader(sheet.Range(3, 1, 4, Math.Max(2, g.Outcomes.Count + 2)));
        var row = 5;
        foreach (var s in g.Students)
        {
            sheet.Cell(row, 1).Value = s.Name;
            sheet.Cell(row, 2).Value = s.RegistrationNumber ?? "";
            for (var i = 0; i < s.Cells.Count; i++)
            {
                var cell = sheet.Cell(row, i + 3);
                if (s.Cells[i].Score is { } score)
                {
                    cell.Value = score;
                    cell.Style.Fill.BackgroundColor = s.Cells[i].Status switch
                    {
                        "mastered" => XLColor.FromHtml("#D1E7DD"),
                        "near" => XLColor.FromHtml("#FFF3CD"),
                        _ => XLColor.FromHtml("#F8D7DA")
                    };
                }
                else cell.Value = "";
            }
            row++;
        }
        sheet.Cell(row + 1, 1).Value = "Mastered";
        for (var i = 0; i < g.Outcomes.Count; i++)
            sheet.Cell(row + 1, i + 3).Value = $"{g.Outcomes[i].MasteredCount} of {g.Outcomes[i].AssessedCount}";
        sheet.Cell(row + 1, 1).Style.Font.Bold = true;
        sheet.Column(1).Width = 32;
        sheet.Column(2).Width = 18;
        for (var i = 0; i < g.Outcomes.Count; i++) sheet.Column(i + 3).Width = 18;
        sheet.SheetView.FreezeRows(4);
        return Save(book);
    }

    public byte[] AttainmentWorkbook(ProgramAttainmentReport r)
    {
        using var book = new XLWorkbook();
        var sheet = book.Worksheets.Add("Attainment");
        sheet.Cell(1, 1).Value = $"Program outcome attainment — {r.ProgramCode} {r.ProgramName}";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(2, 1).Value = $"Students in program: {r.StudentsInProgram} · Target: {r.TargetPercent:0.#}% of assessed students attain each outcome · Generated {r.GeneratedAt:yyyy-MM-dd HH:mm} UTC";
        string[] headers = ["Code", "Program outcome", "Assessed", "Attained", "Attainment %", "Average score", "Mastery / max", "Target met", "Mapped course outcomes", "Evidence from courses"];
        for (var i = 0; i < headers.Length; i++) sheet.Cell(4, i + 1).Value = headers[i];
        StyleHeader(sheet.Range(4, 1, 4, headers.Length));
        var row = 5;
        foreach (var o in r.Outcomes)
        {
            sheet.Cell(row, 1).Value = o.Code;
            sheet.Cell(row, 2).Value = o.Title;
            sheet.Cell(row, 3).Value = o.StudentsAssessed;
            sheet.Cell(row, 4).Value = o.StudentsMastered;
            sheet.Cell(row, 5).Value = o.AttainmentPercent is { } a ? a : "";
            sheet.Cell(row, 6).Value = o.AverageScore is { } s ? s : "";
            sheet.Cell(row, 7).Value = $"{o.MasteryPoints:0.##} / {o.MaxPoints:0.##}";
            sheet.Cell(row, 8).Value = o.TargetMet switch { true => "Yes", false => "No", _ => "No evidence" };
            sheet.Cell(row, 9).Value = string.Join("; ", o.MappedCourseOutcomes);
            sheet.Cell(row, 10).Value = string.Join("; ", o.Contributions.Select(c => $"{c.CourseCode}{(c.SemesterName is null ? "" : $" ({c.SemesterName})")}: {c.Students} students, avg {c.AveragePercent:0.#}%"));
            if (o.TargetMet is { } met) sheet.Cell(row, 8).Style.Fill.BackgroundColor = met ? XLColor.FromHtml("#D1E7DD") : XLColor.FromHtml("#F8D7DA");
            row++;
        }
        sheet.Columns().AdjustToContents(4, row, 8, 70);
        return Save(book);
    }

    public byte[] AttainmentPdf(ProgramAttainmentReport r, string institutionName)
        => Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(28);
            page.DefaultTextStyle(t => t.FontSize(9).WithFonts());
            PdfLanguage.Apply(page); // Plan M11
            page.Header().Column(col =>
            {
                col.Item().Text(institutionName).FontSize(10).FontColor(Colors.Grey.Darken2);
                col.Item().Text($"Program Learning Outcome Attainment — {r.ProgramCode} {r.ProgramName}").FontSize(15).Bold();
                col.Item().Text($"{(r.DepartmentName is null ? "" : r.DepartmentName + " · ")}Students in program: {r.StudentsInProgram} · Target: {r.TargetPercent:0.#}% of assessed students attain each outcome")
                    .FontSize(9).FontColor(Colors.Grey.Darken2);
            });
            page.Content().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(48); c.RelativeColumn(4); c.ConstantColumn(52); c.ConstantColumn(52); c.ConstantColumn(62);
                    c.ConstantColumn(56); c.ConstantColumn(50); c.RelativeColumn(3);
                });
                foreach (var h in new[] { "Code", "Program outcome", "Assessed", "Attained", "Attainment", "Avg score", "Target", "Evidence (course: students, average)" })
                    table.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(h).Bold();
                foreach (var o in r.Outcomes)
                {
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(o.Code).Bold();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Column(c =>
                    {
                        c.Item().Text(o.Title);
                        if (o.MappedCourseOutcomes.Count > 0) c.Item().Text("Mapped: " + string.Join("; ", o.MappedCourseOutcomes)).FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                    });
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(o.StudentsAssessed.ToString());
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(o.StudentsMastered.ToString());
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(o.AttainmentPercent is { } a ? $"{a:0.#}%" : "—");
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(o.AverageScore is { } s ? $"{s:0.##} / {o.MaxPoints:0.##}" : "—");
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Background(o.TargetMet switch { true => "#D1E7DD", false => "#F8D7DA", _ => Colors.White })
                        .Text(o.TargetMet switch { true => "Met", false => "Not met", _ => "No evidence" });
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text(o.Contributions.Count == 0 ? "—" : string.Join("\n", o.Contributions.Select(c => $"{c.CourseCode}{(c.SemesterName is null ? "" : $" ({c.SemesterName})")}: {c.Students}, {c.AveragePercent:0.#}%")));
                }
            });
            page.Footer().Row(row =>
            {
                row.RelativeItem().Text($"Generated {r.GeneratedAt:dd MMM yyyy HH:mm} UTC · Tabsan EduSphere").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                row.ConstantItem(80).AlignRight().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(7.5f).FontColor(Colors.Grey.Darken1));
                    t.Span("Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });
            });
        })).GeneratePdf();

    private static void StyleHeader(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#E9EEF5");
    }

    private static byte[] Save(XLWorkbook book)
    {
        using var stream = new MemoryStream();
        book.SaveAs(stream);
        return stream.ToArray();
    }
}
