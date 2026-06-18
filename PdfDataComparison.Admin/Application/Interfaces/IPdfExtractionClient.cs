namespace PdfDataComparison.Admin.Application.Interfaces;

public interface IPdfExtractionClient
{
    Task<(bool Success, int StatusCode, string ResponseText)> ExtractAsync(
        byte[] pdfBytes,
        string fileName,
        CancellationToken cancellationToken = default);
}
