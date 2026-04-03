using PdfDataComparison.Admin.Domain.Entities;

namespace PdfDataComparison.Admin.Application.Interfaces;

public interface IExportService
{
    Task<byte[]> GenerateComparisonExportAsync(ComparisonSubmission submission, IEnumerable<ComparisonField> fields, string submittedByName);
}
