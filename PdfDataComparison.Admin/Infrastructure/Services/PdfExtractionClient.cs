using System.Net.Http.Headers;
using PdfDataComparison.Admin.Application.Interfaces;

namespace PdfDataComparison.Admin.Infrastructure.Services;

public class PdfExtractionClient(HttpClient httpClient, IConfiguration configuration) : IPdfExtractionClient
{
    public async Task<(bool Success, int StatusCode, string ResponseText)> ExtractAsync(
        byte[] pdfBytes,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var configuredEndpoint = configuration["PdfExtractionApi:ExtractBlEndpoint"];
        if (string.IsNullOrWhiteSpace(configuredEndpoint) ||
            !Uri.TryCreate(configuredEndpoint, UriKind.Absolute, out var rawEndpoint))
        {
            throw new InvalidOperationException("PDF extraction API endpoint is not configured.");
        }

        using var content = new MultipartFormDataContent();
        AddPdfContent(content, pdfBytes, fileName, "file");

        using var apiResponse = await httpClient.PostAsync(NormalizeExtractBlEndpoint(rawEndpoint), content, cancellationToken);
        var responseText = await apiResponse.Content.ReadAsStringAsync(cancellationToken);
        return (apiResponse.IsSuccessStatusCode, (int)apiResponse.StatusCode, responseText);
    }

    private static void AddPdfContent(MultipartFormDataContent content, byte[] pdfBytes, string fileName, string fieldName)
    {
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, fieldName, Path.GetFileName(fileName));
    }

    private static Uri NormalizeExtractBlEndpoint(Uri input)
    {
        var baseUri = input.GetLeftPart(UriPartial.Authority);
        var path = input.AbsolutePath.TrimEnd('/').ToLowerInvariant();

        if (path.EndsWith("/docs") || path == "/docs")
        {
            return new Uri($"{baseUri}/extract_bl");
        }

        if (path.EndsWith("/extract_bl"))
        {
            return input;
        }

        return new Uri($"{baseUri}/extract_bl");
    }
}
