using ClosedXML.Excel;
using PdfDataComparison.Admin.Application.Interfaces;
using PdfDataComparison.Admin.Domain.Entities;

namespace PdfDataComparison.Admin.Infrastructure.Services;

public class ExcelExportService : IExportService
{
    public Task<byte[]> GenerateComparisonExportAsync(ComparisonSubmission submission, IEnumerable<ComparisonField> fields, string submittedByName)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Comparison Export");

        ws.Cell(1, 1).Value = ToSafeExcelText("Field Name");
        ws.Cell(1, 2).Value = ToSafeExcelText("Final Value");
        ws.Cell(1, 3).Value = ToSafeExcelText("Match Status");
        ws.Cell(1, 4).Value = ToSafeExcelText("Submitted By");
        ws.Cell(1, 5).Value = ToSafeExcelText("Timestamp");

        var row = 2;
        foreach (var field in fields)
        {
            ws.Cell(row, 1).Value = ToSafeExcelText(field.FieldLabel);
            ws.Cell(row, 2).Value = ToSafeExcelText(field.ActualValue);
            ws.Cell(row, 3).Value = ToSafeExcelText(field.IsMatch ? "Match" : "Mismatch");
            ws.Cell(row, 4).Value = ToSafeExcelText(submittedByName);
            ws.Cell(row, 5).Value = submission.SubmittedAt;
            row++;
        }

        ApplySafeWorksheetLayout(ws, 5);
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }

    public Task<byte[]> GenerateComparisonMatrixExportAsync(IEnumerable<(string Field, string ApiValue, string PdfValue, bool IsMatch)> rows, string exportedByName, string sourceFileName)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Index Comparison Export");

        ws.Cell(1, 1).Value = ToSafeExcelText("Field");
        ws.Cell(1, 2).Value = ToSafeExcelText("API Value");
        ws.Cell(1, 3).Value = ToSafeExcelText("PDF Value");
        ws.Cell(1, 4).Value = ToSafeExcelText("Match Status");
        ws.Cell(1, 5).Value = ToSafeExcelText("Exported By");
        ws.Cell(1, 6).Value = ToSafeExcelText("Source File");
        ws.Cell(1, 7).Value = ToSafeExcelText("Exported At (UTC)");

        var rowIndex = 2;
        var exportedAt = DateTime.UtcNow;
        foreach (var row in rows)
        {
            ws.Cell(rowIndex, 1).Value = ToSafeExcelText(row.Field);
            ws.Cell(rowIndex, 2).Value = ToSafeExcelText(row.ApiValue);
            ws.Cell(rowIndex, 3).Value = ToSafeExcelText(row.PdfValue);
            ws.Cell(rowIndex, 4).Value = ToSafeExcelText(row.IsMatch ? "Match" : "Mismatch");
            ws.Cell(rowIndex, 5).Value = ToSafeExcelText(exportedByName);
            ws.Cell(rowIndex, 6).Value = ToSafeExcelText(sourceFileName);
            ws.Cell(rowIndex, 7).Value = exportedAt;
            rowIndex++;
        }

        ApplySafeWorksheetLayout(ws, 7);
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
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
            ws.Column(column).Width = 24;
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
        if (safeValue.StartsWith('=') || safeValue.StartsWith('+') || safeValue.StartsWith('-') || safeValue.StartsWith('@'))
        {
            safeValue = "'" + safeValue;
        }

        return safeValue.Length <= 32767 ? safeValue : safeValue[..32767];
    }
}
