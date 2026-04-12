using System.Security.Claims;
using System.Text.Json;
using PdfDataComparison.Admin.Application.Interfaces;
using PdfDataComparison.Admin.Application.ViewModels;
using PdfDataComparison.Admin.Constants;
using PdfDataComparison.Admin.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace PdfDataComparison.Admin.Controllers;

[Authorize(Policy = PermissionCatalog.ComparisonJobsView)]
public class ComparisonJobsController(IComparisonService comparisonService, ApplicationDbContext dbContext, IExportService exportService, IConfiguration configuration) : Controller
{
    public async Task<IActionResult> Index(string? search, int page = 1)
        => View(await comparisonService.GetJobsAsync(search, page, 10));

    [Authorize(Policy = PermissionCatalog.ComparisonEdit)]
    public async Task<IActionResult> Screen(int id)
    {
        var vm = await comparisonService.GetJobScreenAsync(id);
        if (vm == null) return NotFound();
        vm.CanEdit = User.HasClaim("permission", PermissionCatalog.ComparisonEdit) || User.IsInRole("SuperAdmin");
        vm.CanSubmit = User.HasClaim("permission", PermissionCatalog.ComparisonSubmit) || User.IsInRole("SuperAdmin");
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.ComparisonSubmit)]
    public async Task<IActionResult> Submit(ComparisonSubmitVm vm)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var submissionId = await comparisonService.SubmitAsync(vm, userId);
        return RedirectToAction("Details", "Reports", new { id = submissionId });
    }

    [Authorize(Policy = PermissionCatalog.ReportsExport)]
    public async Task<IActionResult> DownloadExcel(int submissionId)
    {
        var submission = await dbContext.ComparisonSubmissions.FirstAsync(x => x.Id == submissionId);
        var fields = await dbContext.ComparisonFields.Where(x => x.ComparisonJobId == submission.ComparisonJobId).ToListAsync();
        var bytes = await exportService.GenerateComparisonExportAsync(submission, fields, User.Identity?.Name ?? "Unknown");
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"comparison-{submissionId}.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitIndexComparison(string rowsJson, string? sourceFileName)
    {
        if (string.IsNullOrWhiteSpace(rowsJson))
        {
            return BadRequest(new { error = "No comparison data was provided." });
        }

        List<IndexComparisonExportRow>? rows;
        try
        {
            rows = JsonSerializer.Deserialize<List<IndexComparisonExportRow>>(rowsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            return BadRequest(new { error = "Invalid comparison data format." });
        }

        if (rows == null || rows.Count == 0)
        {
            return BadRequest(new { error = "Comparison matrix is empty." });
        }

        var exportRows = rows.Select(row =>
        {
            var apiValue = row.ApiValue ?? string.Empty;
            var pdfValue = row.PdfValue ?? string.Empty;
            return (Field: row.Field ?? string.Empty, ApiValue: apiValue, PdfValue: pdfValue, IsMatch: Normalize(apiValue) == Normalize(pdfValue));
        }).ToList();

        if (exportRows.Any(x => !x.IsMatch))
        {
            return BadRequest(new { error = "Submit is allowed only when all values match." });
        }

        var uploadedFileName = string.IsNullOrWhiteSpace(sourceFileName) ? "uploaded-pdf" : sourceFileName!;
        var bytes = await exportService.GenerateComparisonMatrixExportAsync(
            exportRows,
            User.Identity?.Name ?? "Unknown",
            uploadedFileName);

        var baseName = Path.GetFileNameWithoutExtension(uploadedFileName);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "comparison";
        }

        foreach (var c in Path.GetInvalidFileNameChars())
        {
            baseName = baseName.Replace(c, '-');
        }

        var fileName = $"comparison-{baseName}-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAndCompare(IFormFile pdfFile)
    {
        if (pdfFile == null || pdfFile.Length == 0)
        {
            return BadRequest(new { error = "Please select a PDF file." });
        }

        if (!pdfFile.ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase) &&
            !pdfFile.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Only PDF files are supported." });
        }

        var configuredEndpoint = configuration["PdfExtractionApi:ExtractBlEndpoint"];
        if (string.IsNullOrWhiteSpace(configuredEndpoint) || !Uri.TryCreate(configuredEndpoint, UriKind.Absolute, out var rawEndpoint))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "PDF extraction API endpoint is not configured. Set PdfExtractionApi:ExtractBlEndpoint in appsettings."
            });
        }

        var endpoint = NormalizeExtractBlEndpoint(rawEndpoint);

        byte[] pdfBytes;
        using (var ms = new MemoryStream())
        {
            await pdfFile.CopyToAsync(ms);
            pdfBytes = ms.ToArray();
        }

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        using var content = new MultipartFormDataContent();

        // Add common field names to maximize compatibility with FastAPI handlers.
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", pdfFile.FileName);

        var fileContentAlt1 = new ByteArrayContent(pdfBytes);
        fileContentAlt1.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContentAlt1, "pdf_file", pdfFile.FileName);

        var fileContentAlt2 = new ByteArrayContent(pdfBytes);
        fileContentAlt2.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContentAlt2, "upload_file", pdfFile.FileName);

        HttpResponseMessage apiResponse;
        try
        {
            apiResponse = await client.PostAsync(endpoint, content);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = $"API call failed: {ex.Message}" });
        }

        var responseText = await apiResponse.Content.ReadAsStringAsync();
        if (!apiResponse.IsSuccessStatusCode)
        {
            return StatusCode((int)apiResponse.StatusCode, new
            {
                error = "API returned an error response.",
                details = responseText
            });
        }

        List<CompareRowDto> rows;
        try
        {
            using var doc = JsonDocument.Parse(responseText);
            var payloadRoot = doc.RootElement;
            if (payloadRoot.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Object)
            {
                payloadRoot = dataElement;
            }
            rows = BuildComparisonRows(payloadRoot);
        }
        catch (JsonException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "API did not return valid JSON.",
                details = responseText
            });
        }

        if (rows.Count == 0)
        {
            return Ok(new
            {
                fileName = pdfFile.FileName,
                pdfBase64 = Convert.ToBase64String(pdfBytes),
                matches = 0,
                mismatches = 0,
                comparisons = rows,
                note = "No comparable fields found in API response. Expected: fields[] or apiValues/pdfValues."
            });
        }

        var matches = rows.Count(x => x.IsMatch);
        var mismatches = rows.Count - matches;

        return Ok(new
        {
            fileName = pdfFile.FileName,
            pdfBase64 = Convert.ToBase64String(pdfBytes),
            matches,
            mismatches,
            comparisons = rows
        });
    }

    private static List<CompareRowDto> BuildComparisonRows(JsonElement root)
    {
        if (root.TryGetProperty("fields", out var fieldsElement) && fieldsElement.ValueKind == JsonValueKind.Array)
        {
            var directRows = new List<CompareRowDto>();
            foreach (var item in fieldsElement.EnumerateArray())
            {
                var field = TryGetString(item, "field", "name", "label", "key");
                var apiValue = TryGetString(item, "apiValue", "expected", "expectedValue", "valueFromApi");
                var pdfValue = TryGetString(item, "pdfValue", "actual", "actualValue", "valueFromPdf");
                if (string.IsNullOrWhiteSpace(field)) continue;

                directRows.Add(new CompareRowDto
                {
                    Field = field,
                    ApiValue = apiValue,
                    PdfValue = pdfValue,
                    IsMatch = Normalize(apiValue) == Normalize(pdfValue)
                });
            }

            if (directRows.Count > 0) return directRows;
        }

        if (root.TryGetProperty("apiValues", out var apiValues) &&
            root.TryGetProperty("pdfValues", out var pdfValues) &&
            apiValues.ValueKind == JsonValueKind.Object &&
            pdfValues.ValueKind == JsonValueKind.Object)
        {
            var map = pdfValues.EnumerateObject()
                .ToDictionary(p => p.Name, p => ToCompactString(p.Value), StringComparer.OrdinalIgnoreCase);

            var rows = new List<CompareRowDto>();
            foreach (var prop in apiValues.EnumerateObject())
            {
                map.TryGetValue(prop.Name, out var pdfValue);
                var apiValue = ToCompactString(prop.Value);
                rows.Add(new CompareRowDto
                {
                    Field = prop.Name,
                    ApiValue = apiValue,
                    PdfValue = pdfValue ?? string.Empty,
                    IsMatch = Normalize(apiValue) == Normalize(pdfValue)
                });
            }

            return rows.OrderBy(x => x.Field).ToList();
        }

        // Fallback: plain object (e.g., { data: { bill_of_lading_number: "...", ... } }).
        if (root.ValueKind == JsonValueKind.Object)
        {
            var rows = new List<CompareRowDto>();
            foreach (var prop in root.EnumerateObject())
            {
                // Skip verbose/internal keys that are not useful in the comparison grid.
                if (prop.Name.StartsWith("_", StringComparison.Ordinal) ||
                    prop.Name.Equals("Source_File", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = ToCompactString(prop.Value);
                rows.Add(new CompareRowDto
                {
                    Field = prop.Name,
                    ApiValue = value,
                    PdfValue = value,
                    IsMatch = true
                });
            }
            return rows.OrderBy(x => x.Field).ToList();
        }

        return new List<CompareRowDto>();
    }

    private static string TryGetString(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var val))
            {
                return ToCompactString(val);
            }
        }
        return string.Empty;
    }

    private static string ToCompactString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => string.Empty,
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Array => string.Join(", ", element.EnumerateArray().Select(ToCompactString)),
            _ => element.GetRawText()
        };
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant();
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

    private class CompareRowDto
    {
        public string Field { get; set; } = string.Empty;
        public string ApiValue { get; set; } = string.Empty;
        public string PdfValue { get; set; } = string.Empty;
        public bool IsMatch { get; set; }
    }

    private class IndexComparisonExportRow
    {
        public string? Field { get; set; }
        public string? ApiValue { get; set; }
        public string? PdfValue { get; set; }
    }
}
