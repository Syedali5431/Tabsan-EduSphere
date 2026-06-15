using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Tabsan.EduSphere.API.Services.DegreeTranscriptGeneration;

/// <summary>
/// Degree/Transcript Generation add-on service: performs placeholder replacement in .docx templates.
/// Uses Open XML SDK and remains fully isolated from existing report/template flows.
/// </summary>
public sealed class TemplateProcessorService
{
    public byte[] PopulateTemplate(
        byte[] templateBytes,
        DocumentTemplatePayload payload,
        IReadOnlyList<TranscriptCourseRow>? transcriptRows = null)
    {
        using var input = new MemoryStream(templateBytes);
        using var output = new MemoryStream();
        input.CopyTo(output);
        output.Position = 0;

        using (var document = WordprocessingDocument.Open(output, true))
        {
            var replacements = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["{{StudentName}}"] = payload.StudentName,
                ["{{FatherName}}"] = payload.FatherName,
                ["{{RegistrationNumber}}"] = payload.RegistrationNumber,
                ["{{DepartmentName}}"] = payload.DepartmentName,
                ["{{ProgramName}}"] = payload.ProgramName,
                ["{{ClassName}}"] = payload.ClassName,
                ["{{DegreeTitle}}"] = payload.DegreeTitle,
                ["{{CGPA}}"] = payload.Cgpa,
                ["{{FinalPercentage}}"] = payload.FinalPercentage,
                ["{{FinalGPA}}"] = payload.FinalGpa,
                ["{{SemesterGpaSummary}}"] = payload.SemesterGpaSummary,
                ["{{IssueDate}}"] = payload.IssueDate,
                ["{{SerialNumber}}"] = payload.SerialNumber,
                ["{{QR_CODE}}"] = payload.VerificationUrl
            };

            ReplaceInMainBody(document, replacements);
            ReplaceInHeadersAndFooters(document, replacements);
            ReplaceLogo(document);
            ReplaceCourseTable(document, transcriptRows, payload.InstitutionType);
            document.MainDocumentPart?.Document.Save();
        }

        return output.ToArray();
    }

    private static void ReplaceInMainBody(WordprocessingDocument document, IReadOnlyDictionary<string, string> replacements)
    {
        var body = document.MainDocumentPart?.Document.Body;
        if (body == null) return;

        // Concatenate all Text elements within each Run, replace, then write back.
        // This is safe because a Run's text should be contiguous in the document.
        foreach (var run in body.Descendants<Run>())
        {
            var textElements = run.Elements<Text>().ToList();
            if (textElements.Count <= 1) continue;

            var combined = string.Concat(textElements.Select(t => t.Text));
            var hasPlaceholder = false;
            foreach (var key in replacements.Keys)
            {
                if (combined.Contains(key, StringComparison.Ordinal))
                {
                    hasPlaceholder = true;
                    break;
                }
            }
            if (!hasPlaceholder) continue;

            foreach (var pair in replacements)
            {
                combined = combined.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
            }

            // Keep only the first Text element, remove the rest
            for (var i = textElements.Count - 1; i > 0; i--)
                textElements[i].Remove();
            textElements[0].Text = combined;
        }

        // Now do standard replacement on remaining single-Text runs
        foreach (var textNode in body.Descendants<Text>())
        {
            var current = textNode.Text;
            foreach (var pair in replacements)
            {
                current = current.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
            }
            textNode.Text = current;
        }
    }

    private static void ReplaceInHeadersAndFooters(WordprocessingDocument document, IReadOnlyDictionary<string, string> replacements)
    {
        foreach (var headerPart in document.MainDocumentPart?.HeaderParts ?? Enumerable.Empty<HeaderPart>())
        {
            foreach (var textNode in headerPart.Header.Descendants<Text>())
            {
                var current = textNode.Text;
                foreach (var pair in replacements)
                    current = current.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
                textNode.Text = current;
            }
            headerPart.Header.Save();
        }

        foreach (var footerPart in document.MainDocumentPart?.FooterParts ?? Enumerable.Empty<FooterPart>())
        {
            foreach (var textNode in footerPart.Footer.Descendants<Text>())
            {
                var current = textNode.Text;
                foreach (var pair in replacements)
                    current = current.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
                textNode.Text = current;
            }

            var firstParagraph = footerPart.Footer.Descendants<Paragraph>().FirstOrDefault();
            if (firstParagraph != null)
            {
                var logoRun = new Run(
                    new RunProperties(
                        new RunFonts { Ascii = "Georgia", HighAnsi = "Georgia" },
                        new Bold(),
                        new FontSize { Val = "18" },
                        new Color { Val = "1A3A5C" }),
                    new Text("Tabsan EduSphere  |  ") { Space = SpaceProcessingModeValues.Preserve });
                firstParagraph.InsertAt(logoRun, 0);
            }
            footerPart.Footer.Save();
        }
    }

    private static void ReplaceLogo(WordprocessingDocument document)
    {
        var body = document.MainDocumentPart?.Document.Body;
        if (body == null) return;

        var markerParagraph = body.Descendants<Paragraph>()
            .FirstOrDefault(p => p.InnerText.Contains("{{LOGO}}", StringComparison.Ordinal));

        if (markerParagraph == null) return;

        // Replace {{LOGO}} placeholder with a styled institution name acting as logo.
        // Clear the marker paragraph and add formatted runs.
        markerParagraph.RemoveAllChildren<Run>();

        var logoRun = new Run(
            new RunProperties(
                new RunFonts { Ascii = "Georgia", HighAnsi = "Georgia" },
                new Bold(),
                new FontSize { Val = "40" },
                new Color { Val = "1A3A5C" }),
            new Text("★ T A B S A N   E D U S P H E R E ★"));

        markerParagraph.Append(logoRun);

        // Gold separator below the logo
        var separatorRun = new Run(
            new RunProperties(
                new RunFonts { Ascii = "Calibri" },
                new FontSize { Val = "16" },
                new Color { Val = "C9A84C" }),
            new Text("________________________________") { Space = SpaceProcessingModeValues.Preserve });

        // Insert separator after the logo paragraph
        var sepParagraph = new Paragraph(separatorRun);
        markerParagraph.InsertAfterSelf(sepParagraph);
    }

    private static void ReplaceCourseTable(WordprocessingDocument document, IReadOnlyList<TranscriptCourseRow>? transcriptRows, int institutionType)
    {
        var body = document.MainDocumentPart?.Document.Body;
        if (body == null)
        {
            return;
        }

        var markerParagraph = body.Descendants<Paragraph>()
            .FirstOrDefault(p => p.InnerText.Contains("{{COURSE_TABLE}}", StringComparison.Ordinal));

        if (markerParagraph == null)
        {
            return;
        }

        if (transcriptRows == null || transcriptRows.Count == 0)
        {
            markerParagraph.RemoveAllChildren<Run>();
            markerParagraph.Append(new Run(new Text("No courses available.")));
            return;
        }

        var table = BuildCourseTable(transcriptRows, institutionType);
        markerParagraph.InsertAfterSelf(table);
        markerParagraph.Remove();
    }

    private static Table BuildCourseTable(IReadOnlyList<TranscriptCourseRow> rows, int institutionType)
    {
        var table = new Table(
            new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 8, Color = "C9A84C" },
                    new BottomBorder { Val = BorderValues.Single, Size = 8, Color = "C9A84C" },
                    new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "E0E0E0" },
                    new RightBorder { Val = BorderValues.Single, Size = 4, Color = "E0E0E0" },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "E8ECF1" },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "E8ECF1" }),
                new TableJustification { Val = TableRowAlignmentValues.Center }));

        // ── Column Headers ──
        var headerRow = new TableRow();
        headerRow.Append(BuildHeaderCell("Sr.", "600"));
        headerRow.Append(BuildHeaderCell("Subject", "2000"));
        headerRow.Append(BuildHeaderCell("Credit Hrs", "800"));
        headerRow.Append(BuildHeaderCell("Marks", "800"));
        headerRow.Append(BuildHeaderCell("Max", "800"));
        headerRow.Append(BuildHeaderCell(institutionType == 0 ? "GPA" : "%", "800"));
        table.Append(headerRow);

        // ── Group by Semester ──
        var grouped = rows
            .GroupBy(r => r.SemesterName)
            .OrderBy(g => g.Key)
            .ToList();

        var serial = 0;
        var rowIndex = 0;

        foreach (var semesterGroup in grouped)
        {
            var semLabel = string.IsNullOrWhiteSpace(semesterGroup.Key) ? "Semester" : semesterGroup.Key;
            
            // Semester header row
            var semRow = new TableRow();
            var semCell = new TableCell(
                new TableCellProperties(
                    new GridSpan { Val = 6 },
                    new Shading { Val = ShadingPatternValues.Clear, Fill = "F0F4F8" }),
                new Paragraph(
                    new ParagraphProperties(
                        new SpacingBetweenLines { Before = "40", After = "40" }),
                    new Run(
                        new RunProperties(
                            new RunFonts { Ascii = "Georgia", HighAnsi = "Georgia" },
                            new Bold(),
                            new FontSize { Val = "22" },
                            new Color { Val = "1A1A2E" }),
                        new Text("📖  " + semLabel) { Space = SpaceProcessingModeValues.Preserve })));
            semRow.Append(semCell);
            table.Append(semRow);

            foreach (var row in semesterGroup)
            {
                serial++;
                var isEven = rowIndex % 2 == 1;
                var bgColor = isEven ? "FAFBFC" : "FFFFFF";

                var dataRow = new TableRow();
                dataRow.Append(BuildDataCell(serial.ToString(), "600", bgColor, "1A1A2E", bold: true));
                dataRow.Append(BuildDataCell(row.CourseName, "2000", bgColor, "1A1A2E", alignLeft: true));
                var credits = row.CreditHours <= 0 ? "-" : row.CreditHours.ToString("0.##");
                dataRow.Append(BuildDataCell(credits, "800", bgColor, MutedText));
                dataRow.Append(BuildDataCell(row.ObtainedMarks, "800", bgColor, "1A1A2E", bold: true));
                dataRow.Append(BuildDataCell(row.TotalMarks, "800", bgColor, MutedText));
                dataRow.Append(BuildDataCell(row.SgpaOrMarks, "800", bgColor, NavyBlue, bold: true));
                table.Append(dataRow);
                rowIndex++;
            }
        }

        // ── Totals Row ──
        var totalRow = new TableRow();
        totalRow.Append(new TableCell(
            new TableCellProperties(
                new GridSpan { Val = 3 },
                new Shading { Val = ShadingPatternValues.Clear, Fill = "FEF9E7" }),
            new Paragraph(
                new ParagraphProperties(new Justification { Val = JustificationValues.Right }),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = "Georgia" },
                        new Bold(),
                        new FontSize { Val = "22" },
                        new Color { Val = NavyBlue }),
                    new Text("Cumulative GPA: ") { Space = SpaceProcessingModeValues.Preserve }))));
        totalRow.Append(BuildDataCell("", "800", "FEF9E7", NavyBlue));
        totalRow.Append(BuildDataCell("", "800", "FEF9E7", NavyBlue));
        totalRow.Append(BuildDataCell("{{CGPA}}", "800", "FEF9E7", NavyBlue, bold: true));
        table.Append(totalRow);

        return table;
    }

    private static TableCell BuildHeaderCell(string text, string width)
    {
        return new TableCell(
            new TableCellProperties(
                new TableCellWidth { Width = width, Type = TableWidthUnitValues.Dxa },
                new Shading { Val = ShadingPatternValues.Clear, Fill = "1A1A2E" }),
            new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { Before = "40", After = "40" }),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
                        new Bold(),
                        new FontSize { Val = "20" },
                        new Color { Val = "C9A84C" }),
                    new Text(text) { Space = SpaceProcessingModeValues.Preserve })));
    }

    private static TableCell BuildDataCell(string text, string width, string bgColor, string textColor,
        bool bold = false, bool alignLeft = false)
    {
        var runProps = new RunProperties(
            new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
            new FontSize { Val = "20" },
            new Color { Val = textColor });
        if (bold) runProps.Append(new Bold());

        var paraProps = new ParagraphProperties(
            new Justification { Val = alignLeft ? JustificationValues.Left : JustificationValues.Center },
            new SpacingBetweenLines { Before = "20", After = "20" });

        return new TableCell(
            new TableCellProperties(
                new TableCellWidth { Width = width, Type = TableWidthUnitValues.Dxa },
                new Shading { Val = ShadingPatternValues.Clear, Fill = bgColor }),
            new Paragraph(paraProps, new Run(runProps, new Text(text) { Space = SpaceProcessingModeValues.Preserve })));
    }

    // Color constants for table formatting
    private const string NavyBlue = "1A3A5C";
    private const string MutedText = "666666";
}

public sealed record DocumentTemplatePayload(
    string StudentName,
    string FatherName,
    string RegistrationNumber,
    string DepartmentName,
    string ProgramName,
    string ClassName,
    string DegreeTitle,
    string Cgpa,
    string FinalPercentage,
    string FinalGpa,
    string SemesterGpaSummary,
    string IssueDate,
    string SerialNumber,
    string VerificationUrl,
    int InstitutionType = 2); // 0=School, 1=College, 2=University

public sealed record TranscriptCourseRow(
    string SerialNumber,
    string CourseName,
    decimal CreditHours,
    string ObtainedMarks,
    string TotalMarks,
    string SgpaOrMarks,
    string SemesterName = "");
