using QuestPDF.Fluent;
using Tabsan.EduSphere.Infrastructure.Reporting;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.Infrastructure.Exporters;

/// <summary>
/// Generates a timetable PDF using QuestPDF.
/// Layout: landscape A4, header with timetable metadata, weekly grid table.
/// </summary>
public class TimetablePdfExporter : ITimetablePdfExporter
{
    private static readonly string[] DayNames =
        ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    public byte[] Export(TimetableDto timetable)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).WithFonts());
                PdfLanguage.Apply(page); // Plan M11

                page.Header().Element(ComposeHeader(timetable));
                page.Content().Element(ComposeGrid(timetable));
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static Action<IContainer> ComposeHeader(TimetableDto tt)
    {
        return container => container
            .Padding(4)
            .Column(col =>
            {
                col.Item().Text(tt.Title)
                   .Bold().FontSize(14).FontColor(Colors.Blue.Darken3);

                col.Item().Text($"Department: {tt.DepartmentName}   |   Semester: {tt.SemesterName}")
                   .FontSize(10).FontColor(Colors.Grey.Darken2);

                if (tt.PublishedAt.HasValue)
                    col.Item().Text($"Published: {tt.PublishedAt:yyyy-MM-dd HH:mm} UTC")
                       .FontSize(8).FontColor(Colors.Grey.Medium);

                col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Blue.Darken3);
            });
    }

    private static Action<IContainer> ComposeGrid(TimetableDto tt)
    {
        // Distinct time slots ordered ascending
        var timeSlots = tt.Entries
            .Select(e => (e.StartTime, e.EndTime))
            .Distinct()
            .OrderBy(s => s.StartTime)
            .ToList();

        // Only include days that have at least one entry
        var activeDays = DayNames
            .Select((name, idx) => (name, idx))
            .Where(d => tt.Entries.Any(e => e.DayOfWeek == d.idx))
            .ToList();

        return container => container
            .PaddingTop(8)
            .Table(table =>
            {
                // Define columns: time slot col + one col per active day
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(2); // Time slot
                    foreach (var _ in activeDays)
                        cols.RelativeColumn(3);
                });

                // Header row
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Time Slot");
                    foreach (var (name, _) in activeDays)
                        header.Cell().Element(HeaderCell).Text(name);
                });

                // Data rows
                foreach (var (start, end) in timeSlots)
                {
                    // Time slot label
                    table.Cell().Element(SlotCell)
                         .Text($"{start:HH\\:mm}\n{end:HH\\:mm}")
                         .FontSize(8).Bold();

                    // One cell per active day
                    foreach (var (_, dayIndex) in activeDays)
                    {
                        var entry = tt.Entries.FirstOrDefault(e =>
                            e.DayOfWeek == dayIndex &&
                            e.StartTime == start &&
                            e.EndTime == end);

                        if (entry is not null)
                        {
                            table.Cell().Element(EntryCell).Column(inner =>
                            {
                                inner.Item().Text(entry.SubjectName).Bold().FontSize(8);

                                if (!string.IsNullOrWhiteSpace(entry.FacultyName))
                                    inner.Item().Text(entry.FacultyName).FontSize(7)
                                         .FontColor(Colors.Grey.Darken2);

                                if (!string.IsNullOrWhiteSpace(entry.RoomNumber))
                                    inner.Item().Text($"Room: {entry.RoomNumber}").FontSize(7)
                                         .FontColor(Colors.Blue.Medium);
                            });
                        }
                        else
                        {
                            table.Cell().Element(EmptyCell).Text(string.Empty);
                        }
                    }
                }
            });
    }

    // ── Cell style helpers ────────────────────────────────────────────────

    private static IContainer HeaderCell(IContainer container) =>
        container
            .Background(Colors.Blue.Darken3)
            .Padding(4)
            .AlignCenter()
            .DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(9));

    private static IContainer SlotCell(IContainer container) =>
        container
            .Background(Colors.Blue.Lighten4)
            .Padding(4)
            .AlignCenter()
            .AlignMiddle()
            .Border(0.5f)
            .BorderColor(Colors.Grey.Lighten1);

    private static IContainer EntryCell(IContainer container) =>
        container
            .Padding(4)
            .AlignCenter()
            .AlignMiddle()
            .Border(0.5f)
            .BorderColor(Colors.Grey.Lighten1);

    private static IContainer EmptyCell(IContainer container) =>
        container
            .Background(Colors.Grey.Lighten5)
            .Padding(4)
            .Border(0.5f)
            .BorderColor(Colors.Grey.Lighten1);
}
