using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Tabsan.EduSphere.API.Services.DegreeTranscriptGeneration;

/// <summary>
/// Degree/Transcript Generation add-on service: exports professional Word templates
/// with elegant typography, borders, and layout matching the Tabsan brand identity.
/// </summary>
public sealed class TemplateExportService
{
    // Brand colors
    private const string NavyBlue = "1A3A5C";
    private const string DarkRed = "8B0000";
    private const string Gold = "C9A84C";
    private const string LightCream = "FDFCF8";
    private const string LightGray = "F5F6FA";
    private const string DarkNavy = "1A1A2E";
    private const string White = "FFFFFF";
    private const string DarkText = "1A1A1A";
    private const string MutedText = "666666";
    private const string TableBorder = "C9A84C";

    // Brand fonts (system fallbacks that look professional in Word)
    private const string SerifFont = "Georgia";
    private const string SansFont = "Calibri";

    // ── Degree / Completion Certificate ──────────────────────────────────────

    public Task<TemplateExportResult> GetDegreeTemplateAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var content = BuildDegreeTemplate();
        return Task.FromResult(new TemplateExportResult(
            "degree-template.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            content));
    }

    // ── Transcript ───────────────────────────────────────────────────────────

    public Task<TemplateExportResult> GetTranscriptTemplateAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var content = BuildTranscriptTemplate();
        return Task.FromResult(new TemplateExportResult(
            "transcript-template.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            content));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Degree Template Builder
    // ═══════════════════════════════════════════════════════════════════════════

    private static byte[] BuildDegreeTemplate()
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            // Page setup: margins, page border
            body.Append(PageProperties(NavyBlue));

            // ── Logo / Crest ──
            AddCenteredParagraph(body, "★ T A B S A N   E D U S P H E R E ★", SerifFont, "14", MutedText, spacingAfter: "60");
            AddCenteredParagraph(body, "Tabsan International Institution", SansFont, "32", NavyBlue, bold: true, spacingAfter: "200");

            // ── Badge ──
            AddBadge(body, "CERTIFICATE OF COMPLETION");
            AddCenteredParagraph(body, "", SansFont, "12", White, spacingAfter: "100");

            // ── Title ──
            AddCenteredParagraph(body, "Completion Certificate", SerifFont, "48", NavyBlue, bold: true, spacingAfter: "60");
            AddCenteredParagraph(body, "This is proudly presented to", SerifFont, "24", MutedText, italic: true, spacingAfter: "200");

            // ── Student Name ──
            AddCenteredParagraph(body, "{{StudentName}}", SerifFont, "44", DarkRed, bold: true, spacingAfter: "100");
            AddCenteredParagraph(body, "Son / Daughter of {{FatherName}}", SansFont, "20", MutedText, spacingAfter: "300");

            // ── Details ──
            AddCenteredParagraph(body, "for successfully completing the", SansFont, "22", DarkText, spacingAfter: "60");
            AddCenteredParagraph(body, "{{DegreeTitle}}", SerifFont, "28", NavyBlue, bold: true, spacingAfter: "60");
            AddCenteredParagraph(body, "{{ClassName}}", SansFont, "20", DarkText, spacingAfter: "300");

            // Info rows
            AddInfoRow(body, "Registration No", "{{RegistrationNumber}}");
            AddInfoRow(body, "Department", "{{DepartmentName}}");
            AddInfoRow(body, "Program", "{{ProgramName}}");
            AddCenteredParagraph(body, "CGPA: {{CGPA}}  |  Final GPA: {{FinalGPA}}", SansFont, "24", NavyBlue, bold: true, spacingAfter: "200");
            AddCenteredParagraph(body, "{{FinalPercentage}}", SansFont, "20", Gold, bold: true, spacingAfter: "400");

            // ── Date & Serial ──
            AddCenteredParagraph(body, "Issue Date: {{IssueDate}}", SansFont, "20", MutedText, spacingAfter: "60");
            AddCenteredParagraph(body, "Serial No: {{SerialNumber}}", SansFont, "20", MutedText, spacingAfter: "300");

            // ── Signatures ──
            AddSignatureBlock(body);

            // ── Verification ──
            AddCenteredParagraph(body, "Scan to verify: {{QR_CODE}}", SansFont, "16", MutedText, spacingAfter: "200");

            // ── Footer ──
            AddFooterSection(mainPart);

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }
        return stream.ToArray();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Transcript Template Builder
    // ═══════════════════════════════════════════════════════════════════════════

    private static byte[] BuildTranscriptTemplate()
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            // Page setup
            body.Append(PagePropertiesLandscape(DarkNavy));

            // ── Header ──
            AddTranscriptHeader(body);

            // ── Student Info Grid ──
            AddStudentInfoGrid(body);

            // ── Summary Cards ──
            AddSummaryCards(body);

            // ── Section Title ──
            AddSectionTitle(body, "📚  Academic Record");

            // ── Course Table Placeholder ──
            AddCenteredParagraph(body, "{{COURSE_TABLE}}", SansFont, "22", DarkText, spacingAfter: "200");

            // ── Semester GPA Summary ──
            if (!string.IsNullOrWhiteSpace("{{SemesterGpaSummary}}") && "{{SemesterGpaSummary}}" != "")
            {
                AddCenteredParagraph(body, "{{SemesterGpaSummary}}", SansFont, "20", MutedText, spacingAfter: "200");
            }

            // ── Verification ──
            AddCenteredParagraph(body, "Verification: {{QR_CODE}}  |  Serial: {{SerialNumber}}  |  Issue Date: {{IssueDate}}", SansFont, "16", MutedText, spacingAfter: "200");

            // ── Footer ──
            AddFooterSection(mainPart);

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }
        return stream.ToArray();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Reusable Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private static SectionProperties PageProperties(string accentColor)
    {
        return new SectionProperties(
            new PageMargin { Top = 720, Right = 720, Bottom = 720, Left = 720, Header = 360, Footer = 360 },
            new PageBorders(
                new TopBorder { Val = BorderValues.Double, Size = 24, Color = accentColor, Space = 12 },
                new BottomBorder { Val = BorderValues.Double, Size = 24, Color = accentColor, Space = 12 },
                new LeftBorder { Val = BorderValues.Double, Size = 24, Color = accentColor, Space = 12 },
                new RightBorder { Val = BorderValues.Double, Size = 24, Color = accentColor, Space = 12 }));
    }

    private static SectionProperties PagePropertiesLandscape(string accentColor)
    {
        return new SectionProperties(
            new PageSize { Width = 16838, Height = 11906, Orient = PageOrientationValues.Landscape },
            new PageMargin { Top = 600, Right = 600, Bottom = 600, Left = 600, Header = 300, Footer = 300 },
            new PageBorders(
                new TopBorder { Val = BorderValues.Single, Size = 12, Color = accentColor, Space = 8 },
                new BottomBorder { Val = BorderValues.Single, Size = 12, Color = accentColor, Space = 8 },
                new LeftBorder { Val = BorderValues.Single, Size = 12, Color = accentColor, Space = 8 },
                new RightBorder { Val = BorderValues.Single, Size = 12, Color = accentColor, Space = 8 }));
    }

    private static void AddCenteredParagraph(Body body, string text, string font, string fontSize, string color,
        bool bold = false, bool italic = false, string spacingAfter = "120")
    {
        var runProps = new RunProperties(
            new RunFonts { Ascii = font, HighAnsi = font, EastAsia = font },
            new FontSize { Val = fontSize },
            new Color { Val = color });
        if (bold) runProps.Append(new Bold());
        if (italic) runProps.Append(new Italic());

        var para = new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Center },
                new SpacingBetweenLines { After = spacingAfter }),
            new Run(runProps, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        body.Append(para);
    }

    private static void AddLeftParagraph(Body body, string text, string font, string fontSize, string color,
        bool bold = false, string spacingAfter = "80")
    {
        var runProps = new RunProperties(
            new RunFonts { Ascii = font, HighAnsi = font },
            new FontSize { Val = fontSize },
            new Color { Val = color });
        if (bold) runProps.Append(new Bold());

        var para = new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { After = spacingAfter }),
            new Run(runProps, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        body.Append(para);
    }

    private static void AddBadge(Body body, string text)
    {
        var para = new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Center },
                new SpacingBetweenLines { After = "200" }),
            new Run(
                new RunProperties(
                    new RunFonts { Ascii = SansFont, HighAnsi = SansFont },
                    new Bold(),
                    new FontSize { Val = "22" },
                    new Color { Val = White },
                    new Shading { Val = ShadingPatternValues.Clear, Fill = Gold }),
                new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        body.Append(para);
    }

    private static void AddInfoRow(Body body, string label, string value)
    {
        var para = new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Center },
                new SpacingBetweenLines { After = "60" }),
            new Run(
                new RunProperties(
                    new RunFonts { Ascii = SansFont },
                    new FontSize { Val = "20" },
                    new Color { Val = MutedText }),
                new Text(label + ": ") { Space = SpaceProcessingModeValues.Preserve }),
            new Run(
                new RunProperties(
                    new RunFonts { Ascii = SerifFont },
                    new FontSize { Val = "22" },
                    new Bold(),
                    new Color { Val = NavyBlue }),
                new Text(value) { Space = SpaceProcessingModeValues.Preserve }));
        body.Append(para);
    }

    private static void AddSignatureBlock(Body body)
    {
        // Separator line
        AddCenteredParagraph(body, "___________________________________", SansFont, "20", Gold, spacingAfter: "300");

        // Signature table
        var table = new Table(
            new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                new TableBorders(
                    new InsideHorizontalBorder { Val = BorderValues.None },
                    new InsideVerticalBorder { Val = BorderValues.None },
                    new TopBorder { Val = BorderValues.None },
                    new BottomBorder { Val = BorderValues.None },
                    new LeftBorder { Val = BorderValues.None },
                    new RightBorder { Val = BorderValues.None }),
                new TableJustification { Val = TableRowAlignmentValues.Center }));

        // Row 1: Signature lines
        var sigRow = new TableRow();
        sigRow.Append(SigCell(""));
        sigRow.Append(SigCell(""));
        sigRow.Append(SigCell(""));
        table.Append(sigRow);

        // Row 2: Names
        var nameRow = new TableRow();
        nameRow.Append(SigNameCell("Principal / Director"));
        nameRow.Append(SigNameCell("Class Teacher"));
        nameRow.Append(SigNameCell("Examination Controller"));
        table.Append(nameRow);

        body.Append(table);
        AddCenteredParagraph(body, "", SansFont, "12", White, spacingAfter: "100");
    }

    private static TableCell SigCell(string text)
    {
        var cell = new TableCell(
            new TableCellProperties(
                new TableCellWidth { Width = "1600", Type = TableWidthUnitValues.Pct }),
            new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { Before = "200", After = "60" }),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = SansFont },
                        new FontSize { Val = "18" },
                        new Color { Val = NavyBlue }),
                    new Text(text) { Space = SpaceProcessingModeValues.Preserve })));
        // Add a line above
        cell.Append(new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
            new Run(
                new RunProperties(new Color { Val = NavyBlue }),
                new Text("________________________") { Space = SpaceProcessingModeValues.Preserve })));
        return cell;
    }

    private static TableCell SigNameCell(string role)
    {
        return new TableCell(
            new TableCellProperties(
                new TableCellWidth { Width = "1600", Type = TableWidthUnitValues.Pct }),
            new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center }),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = SerifFont },
                        new FontSize { Val = "20" },
                        new Bold(),
                        new Color { Val = DarkText }),
                    new Text(role) { Space = SpaceProcessingModeValues.Preserve })));
    }

    private static void AddTranscriptHeader(Body body)
    {
        var table = new Table(
            new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                new TableBorders(
                    new BottomBorder { Val = BorderValues.Single, Size = 12, Color = Gold }),
                new Shading { Val = ShadingPatternValues.Clear, Fill = DarkNavy },
                new TableJustification { Val = TableRowAlignmentValues.Center }));

        var row = new TableRow();
        var leftCell = new TableCell(
            new TableCellProperties(
                new TableCellWidth { Width = "3500", Type = TableWidthUnitValues.Pct }),
            new Paragraph(
                new ParagraphProperties(
                    new SpacingBetweenLines { After = "40" }),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = SerifFont },
                        new Bold(),
                        new FontSize { Val = "36" },
                        new Color { Val = White }),
                    new Text("Tabsan International Institution") { Space = SpaceProcessingModeValues.Preserve })),
            new Paragraph(
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = SansFont },
                        new FontSize { Val = "20" },
                        new Color { Val = Gold }),
                    new Text("Official Academic Transcript") { Space = SpaceProcessingModeValues.Preserve })));
        row.Append(leftCell);

        var rightCell = new TableCell(
            new TableCellProperties(
                new TableCellWidth { Width = "1500", Type = TableWidthUnitValues.Pct },
                new Shading { Val = ShadingPatternValues.Clear, Fill = Gold }),
            new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { After = "0" }),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = SansFont },
                        new Bold(),
                        new FontSize { Val = "22" },
                        new Color { Val = DarkNavy }),
                    new Text("TRANSCRIPT") { Space = SpaceProcessingModeValues.Preserve })),
            new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center }),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = SansFont },
                        new FontSize { Val = "16" },
                        new Color { Val = DarkNavy }),
                    new Text("{{SerialNumber}}") { Space = SpaceProcessingModeValues.Preserve })));
        row.Append(rightCell);
        table.Append(row);
        body.Append(table);
    }

    private static void AddStudentInfoGrid(Body body)
    {
        var table = new Table(
            new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                new TableBorders(
                    new BottomBorder { Val = BorderValues.Single, Size = 6, Color = Gold }),
                new Shading { Val = ShadingPatternValues.Clear, Fill = LightGray },
                new TableJustification { Val = TableRowAlignmentValues.Center }));

        var row = new TableRow();
        row.Append(InfoCell("Student Name", "{{StudentName}}"));
        row.Append(InfoCell("Father Name", "{{FatherName}}"));
        row.Append(InfoCell("Registration No", "{{RegistrationNumber}}"));
        row.Append(InfoCell("Department", "{{DepartmentName}}"));
        row.Append(InfoCell("Program", "{{ProgramName}}"));
        table.Append(row);
        body.Append(table);
    }

    private static TableCell InfoCell(string label, string value)
    {
        return new TableCell(
            new TableCellProperties(
                new TableCellWidth { Width = "1000", Type = TableWidthUnitValues.Pct }),
            new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { Before = "40", After = "20" }),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = SansFont },
                        new FontSize { Val = "14" },
                        new Color { Val = MutedText }),
                    new Text(label) { Space = SpaceProcessingModeValues.Preserve })),
            new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { After = "40" }),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = SerifFont },
                        new Bold(),
                        new FontSize { Val = "20" },
                        new Color { Val = DarkNavy }),
                    new Text(value) { Space = SpaceProcessingModeValues.Preserve })));
    }

    private static void AddSummaryCards(Body body)
    {
        var table = new Table(
            new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                new TableBorders(
                    new BottomBorder { Val = BorderValues.Single, Size = 6, Color = Gold }),
                new Shading { Val = ShadingPatternValues.Clear, Fill = DarkNavy },
                new TableJustification { Val = TableRowAlignmentValues.Center }));

        var row = new TableRow();
        row.Append(SummaryCell("{{CGPA}}", "Cumulative GPA"));
        row.Append(SummaryCell("{{ClassName}}", "Duration / Class"));
        row.Append(SummaryCell("{{FinalPercentage}}", "Final Percentage"));
        row.Append(SummaryCell("{{FinalGPA}}", "Final GPA"));
        table.Append(row);
        body.Append(table);
    }

    private static TableCell SummaryCell(string value, string label)
    {
        return new TableCell(
            new TableCellProperties(
                new TableCellWidth { Width = "1250", Type = TableWidthUnitValues.Pct }),
            new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { Before = "60", After = "20" }),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = SerifFont },
                        new Bold(),
                        new FontSize { Val = "36" },
                        new Color { Val = Gold }),
                    new Text(value) { Space = SpaceProcessingModeValues.Preserve })),
            new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { After = "60" }),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = SansFont },
                        new FontSize { Val = "16" },
                        new Color { Val = White }),
                    new Text(label) { Space = SpaceProcessingModeValues.Preserve })));
    }

    private static void AddSectionTitle(Body body, string text)
    {
        var para = new Paragraph(
            new ParagraphProperties(
                new ParagraphBorders(
                    new BottomBorder { Val = BorderValues.Single, Size = 8, Color = Gold, Space = 6 }),
                new Shading { Val = ShadingPatternValues.Clear, Fill = LightGray },
                new SpacingBetweenLines { Before = "200", After = "100" }),
            new Run(
                new RunProperties(
                    new RunFonts { Ascii = SerifFont },
                    new Bold(),
                    new FontSize { Val = "24" },
                    new Color { Val = DarkNavy }),
                new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        body.Append(para);
    }

    private static void AddFooterSection(MainDocumentPart mainPart)
    {
        var footerPart = mainPart.AddNewPart<FooterPart>();
        footerPart.Footer = new Footer();
        var footerPara = new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Center }),
            new Run(
                new RunProperties(
                    new RunFonts { Ascii = SansFont },
                    new FontSize { Val = "16" },
                    new Color { Val = MutedText }),
                new Text("Tabsan EduSphere — Official Document  |  Generated on {{IssueDate}}  |  This document is electronically generated and valid without signature.") { Space = SpaceProcessingModeValues.Preserve }));
        footerPart.Footer.Append(footerPara);
        footerPart.Footer.Save();
    }
}

public sealed record TemplateExportResult(string FileName, string ContentType, byte[] Content);
