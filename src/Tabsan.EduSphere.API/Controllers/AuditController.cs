using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Text;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Stage 34.2: searchable audit history for compliance operations.
/// </summary>
[ApiController]
[Route("api/v1/audit")]
[Authorize(Roles = "SuperAdmin,Admin")]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditService _audit;
    private const int ExportMaxRows = 5000;

    public AuditController(IAuditService audit)
    {
        _audit = audit;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [HttpGet("logs")]
    public async Task<IActionResult> SearchLogs(
        [FromQuery] string? query,
        [FromQuery] Guid? actorUserId,
        [FromQuery] string? action,
        [FromQuery] string? entityName,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (toUtc.HasValue && fromUtc.HasValue && toUtc.Value < fromUtc.Value)
            return BadRequest(new { message = "toUtc must be greater than or equal to fromUtc." });

        var (items, totalCount) = await _audit.SearchAsync(
            query: query,
            actorUserId: actorUserId,
            action: action,
            entityName: entityName,
            fromUtc: fromUtc,
            toUtc: toUtc,
            page: page,
            pageSize: pageSize,
            ct: ct);

        await _audit.LogAsync(new Domain.Auditing.AuditLog(
            action: "View",
            entityName: "AuditLog",
            newValuesJson: "{\"surface\":\"SearchLogs\"}"), ct);

                var response = new
        {
            page = page < 1 ? 1 : page,
            pageSize = Math.Clamp(pageSize, 1, 200),
            totalCount,
            items = items.Select(x => new
            {
                x.Id,
                x.OccurredAt,
                x.Action,
                x.EntityName,
                x.EntityId,
                x.ActorUserId,
                x.ActorRole,
                x.IpAddress,
                x.UserAgent,
                x.DeviceInfo,
                x.OldValuesJson,
                x.NewValuesJson
            })
        };

        return Ok(response);
    }

    [HttpGet("logs/export/{format}")]
    public async Task<IActionResult> ExportLogs(
        [FromRoute] string format,
        [FromQuery] string? query,
        [FromQuery] Guid? actorUserId,
        [FromQuery] string? action,
        [FromQuery] string? entityName,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int maxRows = 2000,
        CancellationToken ct = default)
    {
        if (toUtc.HasValue && fromUtc.HasValue && toUtc.Value < fromUtc.Value)
            return BadRequest(new { message = "toUtc must be greater than or equal to fromUtc." });

        var normalizedFormat = (format ?? string.Empty).Trim().ToLowerInvariant();
        if (normalizedFormat is not ("csv" or "excel" or "pdf"))
            return BadRequest(new { message = "format must be csv, excel, or pdf." });

        var requestedRows = Math.Clamp(maxRows, 1, ExportMaxRows);
        var (items, _) = await _audit.SearchAsync(
            query: query,
            actorUserId: actorUserId,
            action: action,
            entityName: entityName,
            fromUtc: fromUtc,
            toUtc: toUtc,
            page: 1,
            pageSize: requestedRows,
            ct: ct);

        await _audit.LogAsync(new Domain.Auditing.AuditLog(
            action: "Export",
            entityName: "AuditLog",
            newValuesJson: $"{{\"format\":\"{normalizedFormat}\",\"count\":{items.Count}}}"), ct);

        var fileStamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

        return normalizedFormat switch
        {
            "csv" => File(BuildCsv(items), "text/csv", $"audit-logs-{fileStamp}.csv"),
            "excel" => File(BuildExcel(items), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"audit-logs-{fileStamp}.xlsx"),
            "pdf" => File(BuildPdf(items), "application/pdf", $"audit-logs-{fileStamp}.pdf"),
            _ => BadRequest()
        };
    }

    private static byte[] BuildCsv(IReadOnlyList<Domain.Auditing.AuditLog> items)
    {
        var sb = new StringBuilder();
                sb.AppendLine("Id,OccurredAtUtc,Action,EntityName,EntityId,ActorUserId,ActorRole,IpAddress,UserAgent,DeviceInfo,OldValuesJson,NewValuesJson");
        foreach (var item in items)
        {
            sb
                .Append(Csv(item.Id.ToString(CultureInfo.InvariantCulture))).Append(',')
                .Append(Csv(item.OccurredAt.ToString("O", CultureInfo.InvariantCulture))).Append(',')
                .Append(Csv(item.Action)).Append(',')
                .Append(Csv(item.EntityName)).Append(',')
                .Append(Csv(item.EntityId)).Append(',')
                .Append(Csv(item.ActorUserId?.ToString())).Append(',')
                .Append(Csv(item.ActorRole)).Append(',')
                .Append(Csv(item.IpAddress)).Append(',')
                .Append(Csv(item.UserAgent)).Append(',')
                .Append(Csv(item.DeviceInfo)).Append(',')
                .Append(Csv(item.OldValuesJson)).Append(',')
                .Append(Csv(item.NewValuesJson))
                .AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] BuildExcel(IReadOnlyList<Domain.Auditing.AuditLog> items)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("AuditLogs");

                var headers = new[]
        {
            "Id", "OccurredAtUtc", "Action", "EntityName", "EntityId", "ActorUserId",
            "ActorRole", "IpAddress", "UserAgent", "DeviceInfo", "OldValuesJson", "NewValuesJson"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var row = 2;
        foreach (var item in items)
        {
                        ws.Cell(row, 1).Value = item.Id;
            ws.Cell(row, 2).Value = item.OccurredAt.ToString("O", CultureInfo.InvariantCulture);
            ws.Cell(row, 3).Value = item.Action;
            ws.Cell(row, 4).Value = item.EntityName;
            ws.Cell(row, 5).Value = item.EntityId ?? string.Empty;
            ws.Cell(row, 6).Value = item.ActorUserId?.ToString() ?? string.Empty;
            ws.Cell(row, 7).Value = item.ActorRole ?? string.Empty;
            ws.Cell(row, 8).Value = item.IpAddress ?? string.Empty;
            ws.Cell(row, 9).Value = item.UserAgent ?? string.Empty;
            ws.Cell(row, 10).Value = item.DeviceInfo ?? string.Empty;
            ws.Cell(row, 11).Value = item.OldValuesJson ?? string.Empty;
            ws.Cell(row, 12).Value = item.NewValuesJson ?? string.Empty;
            row++;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] BuildPdf(IReadOnlyList<Domain.Auditing.AuditLog> items)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(24);
                page.Size(PageSizes.A4.Landscape());
                page.DefaultTextStyle(TextStyle.Default.FontSize(8));

                page.Header().Text($"Audit Logs Export ({items.Count} rows)").SemiBold().FontSize(12);
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1.8f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.4f);
                    });

                    static IContainer HeaderCell(IContainer container)
                        => container.Background(Colors.Grey.Lighten2).Padding(4);

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("At (UTC)").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Action").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Entity").SemiBold();
                        header.Cell().Element(HeaderCell).Text("EntityId").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Actor").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Role").SemiBold();
                        header.Cell().Element(HeaderCell).Text("IP").SemiBold();
                    });

                    foreach (var item in items)
                    {
                        table.Cell().Padding(3).Text(item.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                        table.Cell().Padding(3).Text(item.Action);
                        table.Cell().Padding(3).Text(item.EntityName);
                        table.Cell().Padding(3).Text(item.EntityId ?? "-");
                        table.Cell().Padding(3).Text(item.ActorUserId?.ToString() ?? "-");
                        table.Cell().Padding(3).Text(item.ActorRole ?? "-");
                        table.Cell().Padding(3).Text(item.IpAddress ?? "-");
                    }
                });
                page.Footer().AlignRight().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            });
        }).GeneratePdf();
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
