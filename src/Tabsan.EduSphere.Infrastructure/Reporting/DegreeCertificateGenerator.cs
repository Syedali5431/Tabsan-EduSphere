using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Tabsan.EduSphere.Infrastructure.Reporting;

/// <summary>
/// Generates a formal university degree certificate PDF using QuestPDF.
/// Styling matches the Tabsan University degree template with dark red borders,
/// gold accents, serif typography, seal, and signature blocks.
/// </summary>
public static class DegreeCertificateGenerator
{
    // Brand colours from the certificate template
    private static readonly Color DarkRed = Color.FromHex("#8B0000");
    private static readonly Color Gold = Color.FromHex("#C9A84C");
    private static readonly Color DarkText = Color.FromHex("#1A1A1A");
    private static readonly Color BodyText = Color.FromHex("#333333");
    private static readonly Color MutedText = Color.FromHex("#555555");
    private static readonly Color PageBg = Color.FromHex("#FDFCF8");

    public static byte[] Generate(DegreeCertificateData data)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(t => t.FontFamily("Times New Roman").FontSize(11));

                // ── Outer double border ──
                page.Background().Border(4).BorderColor(DarkRed);

                // ── Page content ──
                page.Content().Padding(20).Column(col =>
                {
                    col.Spacing(0);

                    // ── Crest ──
                    col.Item().AlignCenter().Text("\U0001F3DB")
                        .FontSize(28).FontColor(DarkRed);

                    // ── University name ──
                    col.Item().AlignCenter().Text("TABSAN UNIVERSITY")
                        .FontSize(18).Bold().FontColor(DarkRed)
                        .LetterSpacing(0.15f);

                    col.Item().AlignCenter().Text("Faculty of Information Technology")
                        .FontSize(9).FontColor(MutedText)
                        .LetterSpacing(0.2f);

                    // ── Ornament ──
                    col.Item().AlignCenter().PaddingVertical(8)
                        .Text("\u25C6  \u25C7  \u25C6")
                        .FontSize(12).FontColor(Gold);

                    // ── Title ──
                    col.Item().AlignCenter().PaddingTop(4)
                        .Text("Degree of Bachelor")
                        .FontSize(28).FontColor(DarkText).Italic();

                    col.Item().AlignCenter().PaddingBottom(8)
                        .Text("This is to certify that")
                        .FontSize(12).FontColor(BodyText);

                    // ── Student name ──
                    col.Item().AlignCenter().PaddingTop(4)
                        .Text(data.StudentName)
                        .FontSize(24).FontColor(DarkRed).Italic();

                    // ── Degree body ──
                    col.Item().AlignCenter().PaddingTop(8)
                        .Text($"has been awarded the degree of")
                        .FontSize(12).FontColor(BodyText);

                    col.Item().AlignCenter().PaddingTop(2)
                        .Text(data.DegreeTitle)
                        .FontSize(14).Bold().FontColor(DarkText);

                    col.Item().AlignCenter().PaddingTop(8)
                        .Text("having completed all prescribed requirements")
                        .FontSize(11).FontColor(BodyText);

                    col.Item().AlignCenter().PaddingTop(2)
                        .Text("with a Cumulative Grade Point Average of")
                        .FontSize(11).FontColor(BodyText);

                    col.Item().AlignCenter().PaddingTop(2)
                        .Text($"{data.Cgpa:F2} / 4.00")
                        .FontSize(16).Bold().FontColor(DarkRed);

                    // ── Details row ──
                    col.Item().PaddingTop(12).Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(Gold)
                            .Padding(6).AlignCenter()
                            .Text($"\U0001F393 {data.ProgramCode}")
                            .FontSize(10).FontColor(DarkRed).Bold();
                        row.ConstantItem(8);
                        row.RelativeItem().Border(1).BorderColor(Gold)
                            .Padding(6).AlignCenter()
                            .Text($"\U0001F4C5 {data.DurationYears}")
                            .FontSize(10).FontColor(DarkRed).Bold();
                        row.ConstantItem(8);
                        row.RelativeItem().Border(1).BorderColor(Gold)
                            .Padding(6).AlignCenter()
                            .Text($"\U0001F4CA CGPA {data.Cgpa:F2}")
                            .FontSize(10).FontColor(DarkRed).Bold();
                    });

                    // ── FYP / Honours line ──
                    if (!string.IsNullOrWhiteSpace(data.FypTitle))
                    {
                        col.Item().AlignCenter().PaddingTop(12)
                            .Text($"\"{data.FypTitle}\" — Final Year Project")
                            .FontSize(10).Italic().FontColor(DarkRed);
                    }

                    // ── Signatures ──
                    col.Item().PaddingTop(24).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Column(sig =>
                        {
                            sig.Item().Width(120).BorderBottom(1).BorderColor(DarkText);
                            sig.Item().PaddingTop(4).Text("Prof. Dr. Hassan Raza")
                                .FontSize(10).Bold();
                            sig.Item().Text("Vice Chancellor")
                                .FontSize(8).FontColor(MutedText);
                        });
                        row.RelativeItem().AlignCenter().Column(sig =>
                        {
                            sig.Item().Width(120).BorderBottom(1).BorderColor(DarkText);
                            sig.Item().PaddingTop(4).Text("Dr. Ayesha Siddiqui")
                                .FontSize(10).Bold();
                            sig.Item().Text("Dean, Faculty of IT")
                                .FontSize(8).FontColor(MutedText);
                        });
                        row.RelativeItem().AlignCenter().Column(sig =>
                        {
                            sig.Item().Width(120).BorderBottom(1).BorderColor(DarkText);
                            sig.Item().PaddingTop(4).Text("Mr. Kamran Ali")
                                .FontSize(10).Bold();
                            sig.Item().Text("Controller of Examinations")
                                .FontSize(8).FontColor(MutedText);
                        });
                    });

                    // ── Date & certificate number ──
                    col.Item().AlignCenter().PaddingTop(16)
                        .Text($"Conferred on: {data.IssueDate:MMMM dd, yyyy}  |  Degree No: {data.CertificateNumber}")
                        .FontSize(9).FontColor(MutedText);
                });

                // ── Seal ──
                page.Foreground().AlignRight().AlignBottom().PaddingRight(40).PaddingBottom(30)
                    .Width(80).Height(80)
                    .Border(3).BorderColor(DarkRed)
                    .AlignCenter().AlignMiddle()
                    .Text("TABSAN\nUNIVERSITY\n\u2605\nSEAL")
                    .FontSize(8).Bold().FontColor(DarkRed)
                    .LineHeight(1.2f);
            });
        }).GeneratePdf();
    }
}

/// <summary>
/// Data required to generate a degree certificate.
/// </summary>
public sealed record DegreeCertificateData(
    string StudentName,
    string RegistrationNumber,
    string ProgramName,
    string ProgramCode,
    string DegreeTitle,
    double Cgpa,
    string DurationYears,
    string? FypTitle,
    DateTime IssueDate,
    string CertificateNumber
);
