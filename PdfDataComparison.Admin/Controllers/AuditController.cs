using PdfDataComparison.Admin.Constants;
using PdfDataComparison.Admin.Data;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PdfDataComparison.Admin.Controllers;

[Authorize(Policy = PermissionCatalog.AuditLogsView)]
public class AuditController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index() => View(await dbContext.AuditLogs.OrderByDescending(x => x.Timestamp).Take(200).ToListAsync());

    public async Task<IActionResult> DownloadExcel()
    {
        var logs = await dbContext.AuditLogs
            .OrderByDescending(x => x.Timestamp)
            .Take(200)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Audit Logs");

        var headers = new[] { "Action", "Performed By", "User ID", "Target", "Timestamp UTC", "IP Address", "Notes" };
        for (var column = 0; column < headers.Length; column++)
        {
            ws.Cell(1, column + 1).Value = ToSafeExcelText(headers[column]);
        }

        var row = 2;
        foreach (var log in logs)
        {
            ws.Cell(row, 1).Value = ToSafeExcelText(log.Action);
            ws.Cell(row, 2).Value = ToSafeExcelText(log.PerformedByName);
            ws.Cell(row, 3).Value = ToSafeExcelText(log.PerformedByUserId);
            ws.Cell(row, 4).Value = ToSafeExcelText(log.TargetEntity);
            ws.Cell(row, 5).Value = log.Timestamp;
            ws.Cell(row, 6).Value = ToSafeExcelText(log.IpAddress);
            ws.Cell(row, 7).Value = ToSafeExcelText(log.Notes);
            row++;
        }

        ApplySafeWorksheetLayout(ws, headers.Length);
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return File(
            ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"audit-logs-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
    }

    private static void ApplySafeWorksheetLayout(IXLWorksheet ws, int columnCount)
    {
        if (columnCount <= 0) return;

        var usedRange = ws.RangeUsed();
        if (usedRange != null)
        {
            usedRange.Style.Alignment.WrapText = true;
            usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        }

        var headerRange = ws.Range(1, 1, 1, columnCount);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF2F7");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        for (var column = 1; column <= columnCount; column++)
        {
            ws.Column(column).Width = column == 7 ? 48 : 24;
        }

        ws.SheetView.FreezeRows(1);
    }

    private static string ToSafeExcelText(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var safeChars = value
            .Where(c => c == '\t' || c == '\n' || c == '\r' || !char.IsControl(c))
            .ToArray();

        var safeValue = new string(safeChars);
        return safeValue.Length <= 32767 ? safeValue : safeValue[..32767];
    }
}
