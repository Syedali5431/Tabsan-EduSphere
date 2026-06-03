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
            ReplaceCourseTable(document, transcriptRows);
            document.MainDocumentPart?.Document.Save();
        }

        return output.ToArray();
    }

    private static void ReplaceInMainBody(WordprocessingDocument document, IReadOnlyDictionary<string, string> replacements)
    {
        var textNodes = document.MainDocumentPart?.Document.Body?.Descendants<Text>() ?? Enumerable.Empty<Text>();
        foreach (var textNode in textNodes)
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
                {
                    current = current.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
                }

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
                {
                    current = current.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
                }

                textNode.Text = current;
            }

            footerPart.Footer.Save();
        }
    }

    private static void ReplaceCourseTable(WordprocessingDocument document, IReadOnlyList<TranscriptCourseRow>? transcriptRows)
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

        var table = BuildCourseTable(transcriptRows);
        markerParagraph.InsertAfterSelf(table);
        markerParagraph.Remove();
    }

    private static Table BuildCourseTable(IReadOnlyList<TranscriptCourseRow> rows)
    {
        var table = new Table(
            new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 8 },
                    new BottomBorder { Val = BorderValues.Single, Size = 8 },
                    new LeftBorder { Val = BorderValues.Single, Size = 8 },
                    new RightBorder { Val = BorderValues.Single, Size = 8 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 6 })));

        table.Append(BuildRow("Sr No", "Subject Name", "Credit Hours", "Obtained Marks", "Total Marks", "GPA/Percent", isHeader: true));
        foreach (var row in rows)
        {
            table.Append(BuildRow(
                row.SerialNumber,
                row.CourseName,
                row.CreditHours <= 0 ? "-" : row.CreditHours.ToString("0.##"),
                row.ObtainedMarks,
                row.TotalMarks,
                row.SgpaOrMarks));
        }

        return table;
    }

    private static TableRow BuildRow(string c1, string c2, string c3, string c4, string c5, string c6, bool isHeader = false)
    {
        return new TableRow(
            BuildCell(c1, isHeader),
            BuildCell(c2, isHeader),
            BuildCell(c3, isHeader),
            BuildCell(c4, isHeader),
            BuildCell(c5, isHeader),
            BuildCell(c6, isHeader));
    }

    private static TableCell BuildCell(string value, bool isHeader)
    {
        var runProperties = isHeader ? new RunProperties(new Bold()) : null;
        var run = runProperties == null
            ? new Run(new Text(value))
            : new Run(runProperties, new Text(value));

        return new TableCell(
            new Paragraph(run),
            new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Auto }));
    }
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
    string VerificationUrl);

public sealed record TranscriptCourseRow(
    string SerialNumber,
    string CourseName,
    decimal CreditHours,
    string ObtainedMarks,
    string TotalMarks,
    string SgpaOrMarks);
