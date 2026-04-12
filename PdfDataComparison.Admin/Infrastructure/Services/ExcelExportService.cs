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

        ws.Cell(1, 1).Value = "Field Name";
        ws.Cell(1, 2).Value = "Final Value";
        ws.Cell(1, 3).Value = "Match Status";
        ws.Cell(1, 4).Value = "Submitted By";
        ws.Cell(1, 5).Value = "Timestamp";

        var row = 2;
        foreach (var field in fields)
        {
            ws.Cell(row, 1).Value = field.FieldLabel;
            ws.Cell(row, 2).Value = field.ActualValue;
            ws.Cell(row, 3).Value = field.IsMatch ? "Match" : "Mismatch";
            ws.Cell(row, 4).Value = submittedByName;
            ws.Cell(row, 5).Value = submission.SubmittedAt;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }

    public Task<byte[]> GenerateComparisonMatrixExportAsync(IEnumerable<(string Field, string ApiValue, string PdfValue, bool IsMatch)> rows, string exportedByName, string sourceFileName)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Index Comparison Export");

        ws.Cell(1, 1).Value = "Field";
        ws.Cell(1, 2).Value = "API Value";
        ws.Cell(1, 3).Value = "PDF Value";
        ws.Cell(1, 4).Value = "Match Status";
        ws.Cell(1, 5).Value = "Exported By";
        ws.Cell(1, 6).Value = "Source File";
        ws.Cell(1, 7).Value = "Exported At (UTC)";

        var rowIndex = 2;
        var exportedAt = DateTime.UtcNow;
        foreach (var row in rows)
        {
            ws.Cell(rowIndex, 1).Value = row.Field;
            ws.Cell(rowIndex, 2).Value = row.ApiValue;
            ws.Cell(rowIndex, 3).Value = row.PdfValue;
            ws.Cell(rowIndex, 4).Value = row.IsMatch ? "Match" : "Mismatch";
            ws.Cell(rowIndex, 5).Value = exportedByName;
            ws.Cell(rowIndex, 6).Value = sourceFileName;
            ws.Cell(rowIndex, 7).Value = exportedAt;
            rowIndex++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }
}
