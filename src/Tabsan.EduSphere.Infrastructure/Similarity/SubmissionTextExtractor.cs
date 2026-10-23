using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using PdfSharp.Pdf.IO;
using Tabsan.EduSphere.Application.Similarity;

namespace Tabsan.EduSphere.Infrastructure.Similarity;

/// <summary>
/// Plan M9: reads the text of submitted files for similarity checking — plain text, Markdown, CSV, HTML, RTF, Word (.docx)
/// and PDF (text drawn with simple fonts; scanned or CID-encoded PDFs give no text). Unreadable files return null.
/// </summary>
public sealed partial class SubmissionTextExtractor : ISubmissionTextExtractor
{
    private const int MaxChars = 200_000;

    public Task<string?> ExtractAsync(string fileName, string contentType, byte[] content, CancellationToken ct = default)
    {
        string? text = null;
        try
        {
            text = Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".txt" or ".md" or ".csv" or ".text" => Decode(content),
                ".html" or ".htm" => Html(Decode(content)),
                ".rtf" => Rtf(Decode(content)),
                ".docx" => Docx(content),
                ".pdf" => Pdf(content),
                _ => contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ? Decode(content) : null
            };
        }
        catch (Exception) { text = null; } // a damaged file simply contributes no text
        if (string.IsNullOrWhiteSpace(text)) return Task.FromResult<string?>(null);
        return Task.FromResult<string?>(text.Length > MaxChars ? text[..MaxChars] : text);
    }

    private static string Decode(byte[] bytes)
    {
        using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    [GeneratedRegex(@"<(script|style)[^>]*>.*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptOrStyle();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Tag();

    private static string Html(string html) => WebUtility.HtmlDecode(Tag().Replace(ScriptOrStyle().Replace(html, " "), " "));

    [GeneratedRegex(@"\\[a-z]+-?\d* ?|[{}]|\\'[0-9a-f]{2}", RegexOptions.IgnoreCase)]
    private static partial Regex RtfControl();

    private static string Rtf(string rtf) => RtfControl().Replace(rtf.Replace("\\par", "\n"), " ");

    private static string Docx(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null) return string.Empty;
        return string.Join("\n", body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Select(p => p.InnerText));
    }

    private static string Pdf(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var doc = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        var sb = new StringBuilder();
        foreach (var page in doc.Pages)
        {
            Walk(ContentReader.ReadContent(page), sb);
            sb.Append('\n');
        }
        var text = sb.ToString();
        // CID-encoded fonts give control characters rather than letters: treat such PDFs as unreadable.
        var letters = text.Count(char.IsLetter);
        return letters < text.Count(c => !char.IsWhiteSpace(c)) / 2 ? string.Empty : text;
    }

    private static void Walk(CObject obj, StringBuilder sb)
    {
        switch (obj)
        {
            case COperator op:
                switch (op.OpCode.OpCodeName)
                {
                    case OpCodeName.Tj or OpCodeName.QuoteSingle or OpCodeName.QuoteDouble or OpCodeName.TJ:
                        foreach (var operand in op.Operands) Walk(operand, sb);
                        if (op.OpCode.OpCodeName != OpCodeName.TJ) sb.Append(' ');
                        break;
                    case OpCodeName.Td or OpCodeName.TD or OpCodeName.Tx or OpCodeName.ET:
                        sb.Append(' ');
                        break;
                }
                break;
            case CString s:
                sb.Append(s.Value);
                break;
            case CArray array:
                foreach (var item in array)
                {
                    // In TJ arrays a large negative kerning number is a word gap.
                    if (item is CInteger { Value: < -200 } or CReal { Value: < -200 }) sb.Append(' ');
                    else Walk(item, sb);
                }
                break;
            case CSequence seq:
                foreach (var item in seq) Walk(item, sb);
                break;
        }
    }
}
