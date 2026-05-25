using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Tabsan.EduSphere.API.Services.PlanK;

/// <summary>
/// Plan K add-on service: exports default Degree/Transcript Word templates.
/// This service is isolated and does not alter existing template/export modules.
/// </summary>
public sealed class TemplateExportService
{
    public Task<TemplateExportResult> GetDegreeTemplateAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var content = BuildTemplateDocument(new[]
        {
            "Degree Certificate",
            "",
            "This certifies that {{StudentName}} son/daughter of {{FatherName}}",
            "has completed the degree requirements for {{DegreeTitle}}.",
            "CGPA: {{CGPA}}",
            "Issue Date: {{IssueDate}}",
            "Serial Number: {{SerialNumber}}",
            "Verification: {{QR_CODE}}"
        });

        return Task.FromResult(new TemplateExportResult(
            "degree-template.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            content));
    }

    public Task<TemplateExportResult> GetTranscriptTemplateAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var content = BuildTemplateDocument(new[]
        {
            "Transcript",
            "",
            "Student: {{StudentName}}",
            "Father Name: {{FatherName}}",
            "Degree: {{DegreeTitle}}",
            "CGPA: {{CGPA}}",
            "Issue Date: {{IssueDate}}",
            "Serial Number: {{SerialNumber}}",
            "",
            "{{COURSE_TABLE}}",
            "",
            "Verification: {{QR_CODE}}"
        });

        return Task.FromResult(new TemplateExportResult(
            "transcript-template.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            content));
    }

    private static byte[] BuildTemplateDocument(IEnumerable<string> lines)
    {
        using var stream = new MemoryStream();
        using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            foreach (var line in lines)
            {
                var paragraph = new Paragraph(
                    new Run(
                        new Text(line)
                        {
                            Space = SpaceProcessingModeValues.Preserve
                        }));
                body.Append(paragraph);
            }

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }
}

public sealed record TemplateExportResult(string FileName, string ContentType, byte[] Content);
