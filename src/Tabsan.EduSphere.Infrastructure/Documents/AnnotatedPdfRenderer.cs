using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Tabsan.EduSphere.Application.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Assignments;

namespace Tabsan.EduSphere.Infrastructure.Documents;

/// <summary>
/// Plan M3.3: draws a grader's annotations onto the submitted PDF (or onto an image placed on a PDF page)
/// with PDFsharp, numbers every annotation that has a comment, and appends a "Comments" page listing them.
/// Geometry is page-relative (0–1), so it maps onto the page whatever its size.
/// </summary>
public sealed class AnnotatedPdfRenderer : IAnnotatedPdfRenderer
{
    private const string FontFamily = "EduSphereSans";

    static AnnotatedPdfRenderer() => EmbeddedOrSystemFontResolver.EnsureRegistered();

    public byte[] Render(byte[] source, bool sourceIsImage, IReadOnlyList<SubmissionAnnotation> annotations, string title)
    {
        using var document = sourceIsImage ? ImageDocument(source) : PdfReader.Open(new MemoryStream(source), PdfDocumentOpenMode.Modify);
        var numbered = new List<(int Number, int Page, string Comment)>();
        var number = 0;

        foreach (var group in annotations.GroupBy(a => a.PageNumber).OrderBy(g => g.Key))
        {
            if (group.Key > document.PageCount) continue; // drawn on a page that no longer exists
            var page = document.Pages[group.Key - 1];
            using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            double pw = page.Width.Point, ph = page.Height.Point;

            foreach (var a in group)
            {
                var color = ParseColor(a.Color);
                double? anchorX = null, anchorY = null;
                switch (a.Type)
                {
                    case AnnotationType.Highlight:
                    case AnnotationType.Strikeout:
                    case AnnotationType.Box:
                        if (!AnnotationGeometry.TryReadRect(a.GeometryJson, out var x, out var y, out var w, out var h)) continue;
                        var rect = new XRect(x * pw, y * ph, w * pw, h * ph);
                        if (a.Type == AnnotationType.Highlight)
                            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(90, color.R, color.G, color.B)), rect);
                        else if (a.Type == AnnotationType.Box)
                            gfx.DrawRectangle(new XPen(color, 1.5), rect);
                        else
                            gfx.DrawLine(new XPen(color, 1.5), rect.Left, rect.Top + rect.Height / 2, rect.Right, rect.Top + rect.Height / 2);
                        (anchorX, anchorY) = (rect.Right, rect.Top);
                        break;

                    case AnnotationType.Freehand:
                        if (!AnnotationGeometry.TryReadPath(a.GeometryJson, out var points, out var width)) continue;
                        var pen = new XPen(color, Math.Max(0.5, width * pw)) { LineCap = XLineCap.Round, LineJoin = XLineJoin.Round };
                        gfx.DrawLines(pen, points.Select(p => new XPoint(p.X * pw, p.Y * ph)).ToArray());
                        (anchorX, anchorY) = (points[0].X * pw, points[0].Y * ph);
                        break;

                    case AnnotationType.Text:
                        if (!AnnotationGeometry.TryReadPoint(a.GeometryJson, out var tx, out var ty)) continue;
                        DrawTextBlock(gfx, a.Comment ?? string.Empty, tx * pw, ty * ph, pw, color);
                        continue; // the text is the annotation; no number needed

                    case AnnotationType.Pin:
                        if (!AnnotationGeometry.TryReadPoint(a.GeometryJson, out var px, out var py)) continue;
                        (anchorX, anchorY) = (px * pw, py * ph);
                        break;
                }

                if (anchorX is null) continue;
                if (a.Type == AnnotationType.Pin || !string.IsNullOrWhiteSpace(a.Comment))
                {
                    number++;
                    DrawMarker(gfx, number, anchorX.Value, anchorY!.Value, pw, ph);
                    numbered.Add((number, a.PageNumber, a.Comment ?? string.Empty));
                }
            }
        }

        if (numbered.Count > 0) AppendCommentsPage(document, numbered, title);

        using var output = new MemoryStream();
        document.Save(output, closeStream: false);
        return output.ToArray();
    }

    private static PdfDocument ImageDocument(byte[] image)
    {
        var document = new PdfDocument();
        using var stream = new MemoryStream(image);
        using var img = XImage.FromStream(stream);
        var page = document.AddPage();
        // 96 dpi → points; keep within PDF's page size limit.
        var scale = Math.Min(1.0, 14000.0 / Math.Max(img.PixelWidth, img.PixelHeight) / 0.75);
        page.Width = XUnit.FromPoint(img.PixelWidth * 0.75 * scale);
        page.Height = XUnit.FromPoint(img.PixelHeight * 0.75 * scale);
        using (var gfx = XGraphics.FromPdfPage(page))
            gfx.DrawImage(img, 0, 0, page.Width.Point, page.Height.Point);
        return document;
    }

    private static void DrawTextBlock(XGraphics gfx, string text, double x, double y, double pageWidth, XColor color)
    {
        var font = new XFont(FontFamily, 10, XFontStyleEx.Regular);
        var lines = Wrap(gfx, text, font, Math.Max(80, pageWidth - x - 10)).ToList();
        var lineHeight = font.GetHeight();
        var width = lines.Count == 0 ? 0 : lines.Max(l => gfx.MeasureString(l, font).Width);
        gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(220, 255, 255, 255)), x - 2, y - 1, width + 4, lineHeight * lines.Count + 2);
        for (var i = 0; i < lines.Count; i++)
            gfx.DrawString(lines[i], font, new XSolidBrush(color), new XPoint(x, y + lineHeight * (i + 1) - 2));
    }

    private static void DrawMarker(XGraphics gfx, int number, double x, double y, double pw, double ph)
    {
        const double r = 7;
        var cx = Math.Clamp(x, r, pw - r);
        var cy = Math.Clamp(y, r, ph - r);
        gfx.DrawEllipse(new XPen(XColors.White, 1), new XSolidBrush(XColor.FromArgb(255, 200, 30, 30)), cx - r, cy - r, r * 2, r * 2);
        var font = new XFont(FontFamily, 7.5, XFontStyleEx.Bold);
        gfx.DrawString(number.ToString(), font, XBrushes.White, new XRect(cx - r, cy - r, r * 2, r * 2), XStringFormats.Center);
    }

    private static void AppendCommentsPage(PdfDocument document, List<(int Number, int Page, string Comment)> comments, string title)
    {
        var heading = new XFont(FontFamily, 14, XFontStyleEx.Bold);
        var body = new XFont(FontFamily, 10, XFontStyleEx.Regular);
        PdfPage? page = null;
        XGraphics? gfx = null;
        double y = 0;
        const double margin = 50;

        void NewPage()
        {
            gfx?.Dispose();
            page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            gfx = XGraphics.FromPdfPage(page);
            gfx.DrawString("Teacher comments", heading, XBrushes.Black, new XPoint(margin, margin));
            gfx.DrawString(title, body, XBrushes.Gray, new XPoint(margin, margin + 18));
            y = margin + 42;
        }

        NewPage();
        foreach (var (number, pageNumber, comment) in comments)
        {
            var lines = Wrap(gfx!, string.IsNullOrWhiteSpace(comment) ? "(marker)" : comment, body, page!.Width.Point - margin * 2 - 60).ToList();
            var needed = body.GetHeight() * Math.Max(1, lines.Count) + 10;
            if (y + needed > page.Height.Point - margin) NewPage();
            gfx!.DrawString($"{number}.", body, XBrushes.Black, new XPoint(margin, y));
            gfx.DrawString($"p. {pageNumber}", body, XBrushes.Gray, new XPoint(margin + 22, y));
            for (var i = 0; i < lines.Count; i++)
                gfx.DrawString(lines[i], body, XBrushes.Black, new XPoint(margin + 60, y + body.GetHeight() * i));
            y += needed;
        }
        gfx?.Dispose();
    }

    private static IEnumerable<string> Wrap(XGraphics gfx, string text, XFont font, double maxWidth)
    {
        foreach (var paragraph in text.Replace("\r", string.Empty).Split('\n'))
        {
            var line = string.Empty;
            foreach (var word in paragraph.Split(' '))
            {
                var candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && gfx.MeasureString(candidate, font).Width > maxWidth)
                {
                    yield return line;
                    line = word;
                }
                else line = candidate;
            }
            yield return line;
        }
    }

    private static XColor ParseColor(string? hex)
    {
        var h = (hex ?? "#f5c400").TrimStart('#');
        if (h.Length == 3) h = string.Concat(h.Select(c => $"{c}{c}"));
        return h.Length == 6 && int.TryParse(h, System.Globalization.NumberStyles.HexNumber, null, out var v)
            ? XColor.FromArgb(255, (v >> 16) & 0xff, (v >> 8) & 0xff, v & 0xff)
            : XColor.FromArgb(255, 245, 196, 0);
    }
}

/// <summary>
/// Font resolver for the annotated PDFs: a sans-serif system font when one is installed
/// (Arial, DejaVu Sans, Liberation Sans, Noto Sans), otherwise the Segoe WP faces embedded in PDFsharp.
/// </summary>
internal sealed class EmbeddedOrSystemFontResolver : IFontResolver
{
    private static readonly object Gate = new();
    private static bool _registered;

    private static readonly string[] RegularCandidates =
    [
        @"C:\Windows\Fonts\arial.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
        "/usr/share/fonts/truetype/noto/NotoSans-Regular.ttf",
        "/usr/share/fonts/TTF/DejaVuSans.ttf",
        "/Library/Fonts/Arial.ttf"
    ];

    private static readonly string[] BoldCandidates =
    [
        @"C:\Windows\Fonts\arialbd.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
        "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",
        "/usr/share/fonts/truetype/noto/NotoSans-Bold.ttf",
        "/usr/share/fonts/TTF/DejaVuSans-Bold.ttf",
        "/Library/Fonts/Arial Bold.ttf"
    ];

    public static void EnsureRegistered()
    {
        lock (Gate)
        {
            if (_registered) return;
            // Another component may already have installed a resolver; keep it and add ours as the fallback.
            if (GlobalFontSettings.FontResolver is null) GlobalFontSettings.FontResolver = new EmbeddedOrSystemFontResolver();
            else GlobalFontSettings.FallbackFontResolver ??= new EmbeddedOrSystemFontResolver();
            _registered = true;
        }
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
        => new(bold ? "edusphere-bold" : "edusphere-regular", false, italic);

    public byte[]? GetFont(string faceName)
    {
        var bold = faceName == "edusphere-bold";
        var path = (bold ? BoldCandidates : RegularCandidates).FirstOrDefault(File.Exists);
        if (path is not null) return File.ReadAllBytes(path);
        return bold ? PdfSharp.WPFonts.FontDataHelper.SegoeWPBold : PdfSharp.WPFonts.FontDataHelper.SegoeWP;
    }
}
