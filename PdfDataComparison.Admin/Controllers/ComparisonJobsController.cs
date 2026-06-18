using System.Security.Claims;
using System.Text.Json;
using System.IO.Compression;
using System.Globalization;
using ClosedXML.Excel;
using PdfDataComparison.Admin.Application.Interfaces;
using PdfDataComparison.Admin.Application.ViewModels;
using PdfDataComparison.Admin.Constants;
using PdfDataComparison.Admin.Data;
using PdfDataComparison.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace PdfDataComparison.Admin.Controllers;

[Authorize(Policy = PermissionCatalog.ComparisonJobsView)]
public class ComparisonJobsController(
    IComparisonService comparisonService,
    ApplicationDbContext dbContext,
    IExportService exportService,
    IConfiguration configuration,
    IAuditService auditService,
    IDownloadDiagnosticLogStore diagnosticLogStore,
    ILogger<ComparisonJobsController> logger) : Controller
{
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var jobs = await comparisonService.GetJobsAsync(search, page, 10);

        return View(new ComparisonJobsIndexVm
        {
            Jobs = jobs
        });
    }

    public async Task<IActionResult> SubmittedPdfData()
    {
        return View(await GetPdfSubmissionListItemsAsync());
    }

    [HttpGet]
    public async Task<IActionResult> ActiveBillOfLadingExists(string billOfLadingNumber)
    {
        var normalizedBillOfLadingNumber = NormalizeToken(billOfLadingNumber);
        if (string.IsNullOrWhiteSpace(normalizedBillOfLadingNumber))
        {
            return Ok(new { exists = false });
        }

        var activeSubmissions = await dbContext.PdfComparisonSubmissions
            .Where(x => x.IsActive)
            .Select(x => new { x.Id, x.BillOfLadingNumber })
            .ToListAsync();

        var existing = activeSubmissions.FirstOrDefault(x => NormalizeToken(x.BillOfLadingNumber) == normalizedBillOfLadingNumber);
        return Ok(new
        {
            exists = existing != null,
            jobId = existing == null ? string.Empty : $"PDF-JOB-{existing.Id:D5}"
        });
    }

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
            return (Field: row.Field ?? string.Empty, ApiValue: apiValue, PdfValue: pdfValue, IsMatch: IsConfirmedMatch(apiValue, pdfValue));
        }).ToList();

        if (exportRows.Any(x => !x.IsMatch))
        {
            return BadRequest(new { error = "Submit is allowed only when all rows have confirmed non-blank matching values." });
        }

        var billOfLadingNumber = ExtractBillOfLadingNumber(rows).Trim();
        var replacedSubmissionIds = new List<int>();
        if (!string.IsNullOrWhiteSpace(billOfLadingNumber))
        {
            var normalizedBillOfLadingNumber = NormalizeToken(billOfLadingNumber);
            var existingActiveSubmissions = await dbContext.PdfComparisonSubmissions
                .Where(x => x.IsActive)
                .ToListAsync();

            foreach (var existingSubmission in existingActiveSubmissions.Where(x => NormalizeToken(x.BillOfLadingNumber) == normalizedBillOfLadingNumber))
            {
                existingSubmission.IsActive = false;
                replacedSubmissionIds.Add(existingSubmission.Id);
            }
        }

        var submission = new PdfComparisonSubmission
        {
            BillOfLadingNumber = billOfLadingNumber,
            PayloadJson = JsonSerializer.Serialize(rows),
            SourceFileName = sourceFileName ?? string.Empty,
            SubmittedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            SubmittedAt = DateTime.UtcNow,
            IsActive = true
        };

        dbContext.PdfComparisonSubmissions.Add(submission);
        await dbContext.SaveChangesAsync();
        await auditService.LogAsync(
            "PdfComparisonSubmitted",
            $"PdfComparisonSubmission:{submission.Id}",
            replacedSubmissionIds.Count == 0
                ? $"PDF comparison saved for {sourceFileName ?? "uploaded.pdf"} with {exportRows.Count} rows"
                : $"PDF comparison saved for {sourceFileName ?? "uploaded.pdf"} with {exportRows.Count} rows; replaced active PDF submissions: {string.Join(", ", replacedSubmissionIds)}",
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.Identity?.Name ?? "Unknown");

        return Ok(new
        {
            message = "PDF comparison saved successfully.",
            submissionId = submission.Id,
            redirectUrl = Url.Action(nameof(SubmittedPdfData))
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadSelectedPdfSubmissionExcel(List<int> selectedPdfSubmissionIds)
    {
        var errorReference = HttpContext.TraceIdentifier;

        if (selectedPdfSubmissionIds == null || selectedPdfSubmissionIds.Count == 0)
        {
            return BadRequest("Select at least one submitted PDF data row.");
        }

        var selectedIds = selectedPdfSubmissionIds.Distinct().ToList();
        try
        {
            logger.LogInformation(
                "Selected PDF Excel ZIP download started. ErrorReference={ErrorReference}; SelectedIds={SelectedIds}; User={User}",
                errorReference,
                string.Join(",", selectedIds),
                User.Identity?.Name ?? "Unknown");
            await WriteDownloadDiagnosticAsync(
                "Info",
                "Selected PDF Excel ZIP download started.",
                errorReference,
                selectedIds);

            var activeSubmissions = await dbContext.PdfComparisonSubmissions
                .Where(x => x.IsActive)
                .OrderBy(x => x.Id)
                .ToListAsync();

            var selectedIdSet = selectedIds.ToHashSet();
            var submissions = activeSubmissions
                .Where(x => selectedIdSet.Contains(x.Id))
                .ToList();

            if (submissions.Count == 0)
            {
                logger.LogWarning(
                    "Selected PDF Excel ZIP download found no active submissions. ErrorReference={ErrorReference}; SelectedIds={SelectedIds}; User={User}",
                    errorReference,
                    string.Join(",", selectedIds),
                    User.Identity?.Name ?? "Unknown");
                await WriteDownloadDiagnosticAsync(
                    "Warning",
                    "No active submitted PDF records were found for the selected rows.",
                    errorReference,
                    selectedIds);
                return NotFound("No active submitted PDF records were found for the selected rows.");
            }

            var selectedExports = new List<IReadOnlyCollection<(string Field, string ApiValue, string PdfValue, bool IsMatch)>>();
            foreach (var submission in submissions)
            {
                var exportRows = TryBuildExportRows(submission.PayloadJson ?? string.Empty);
                logger.LogInformation(
                    "Selected PDF export rows parsed. ErrorReference={ErrorReference}; SubmissionId={SubmissionId}; FieldCount={FieldCount}; PayloadLength={PayloadLength}",
                    errorReference,
                    submission.Id,
                    exportRows.Count,
                    submission.PayloadJson?.Length ?? 0);
                await WriteDownloadDiagnosticAsync(
                    "Info",
                    $"Parsed submission {submission.Id}. Fields={exportRows.Count}; PayloadLength={submission.PayloadJson?.Length ?? 0}.",
                    errorReference,
                    selectedIds,
                    new[] { submission.Id });

                if (exportRows.Count > 0)
                {
                    selectedExports.Add(exportRows);
                }
            }

            if (selectedExports.Count == 0)
            {
                logger.LogWarning(
                    "Selected PDF Excel ZIP download had no valid export rows. ErrorReference={ErrorReference}; SubmissionIds={SubmissionIds}",
                    errorReference,
                    string.Join(",", submissions.Select(x => x.Id)));
                await WriteDownloadDiagnosticAsync(
                    "Warning",
                    "Selected comparison data is empty or invalid.",
                    errorReference,
                    selectedIds,
                    submissions.Select(x => x.Id));
                return BadRequest("Selected comparison data is empty or invalid.");
            }

            var zipBytes = await BuildComparisonZipAsync(selectedExports);
            var downloadName = $"comparison-files-selected-{DateTime.UtcNow:yyyyMMddHHmmss}.zip";

            try
            {
                await auditService.LogAsync(
                    "PdfComparisonExcelDownloaded",
                    "PdfComparisonSubmissions",
                    $"Downloaded Excel ZIP for active PDF submissions: {string.Join(", ", submissions.Select(x => x.Id))}",
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    User.Identity?.Name ?? "Unknown");
            }
            catch (Exception auditException)
            {
                logger.LogError(
                    auditException,
                    "Audit logging failed after selected PDF Excel ZIP generation. ErrorReference={ErrorReference}; SubmissionIds={SubmissionIds}",
                    errorReference,
                    string.Join(",", submissions.Select(x => x.Id)));
                await WriteDownloadDiagnosticAsync(
                    "Error",
                    "Audit logging failed after selected PDF Excel ZIP generation.",
                    errorReference,
                    selectedIds,
                    submissions.Select(x => x.Id),
                    auditException);
            }

            logger.LogInformation(
                "Selected PDF Excel ZIP download completed. ErrorReference={ErrorReference}; SubmissionIds={SubmissionIds}; ZipBytes={ZipBytes}",
                errorReference,
                string.Join(",", submissions.Select(x => x.Id)),
                zipBytes.Length);
            await WriteDownloadDiagnosticAsync(
                "Info",
                $"Selected PDF Excel ZIP download completed. ZipBytes={zipBytes.Length}.",
                errorReference,
                selectedIds,
                submissions.Select(x => x.Id));

            return File(zipBytes, "application/zip", downloadName);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Selected PDF Excel ZIP download failed. ErrorReference={ErrorReference}; SelectedIds={SelectedIds}; User={User}",
                errorReference,
                string.Join(",", selectedIds),
                User.Identity?.Name ?? "Unknown");
            await WriteDownloadDiagnosticAsync(
                "Error",
                "Selected PDF Excel ZIP download failed.",
                errorReference,
                selectedIds,
                exception: ex);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                $"Unable to download selected Excel ZIP. Error reference: {errorReference}");
        }
    }

    private async Task WriteDownloadDiagnosticAsync(
        string level,
        string message,
        string errorReference,
        IEnumerable<int>? selectedIds = null,
        IEnumerable<int>? submissionIds = null,
        Exception? exception = null)
    {
        try
        {
            await diagnosticLogStore.WriteAsync(new DownloadDiagnosticLogEntryVm
            {
                TimestampUtc = DateTime.UtcNow,
                Level = level,
                ErrorReference = errorReference,
                Message = message,
                SelectedIds = selectedIds == null ? string.Empty : string.Join(",", selectedIds),
                SubmissionIds = submissionIds == null ? string.Empty : string.Join(",", submissionIds),
                UserName = User.Identity?.Name ?? "Unknown",
                Exception = exception?.ToString() ?? string.Empty
            });
        }
        catch (Exception logException)
        {
            logger.LogError(logException, "Failed to write download diagnostic log. ErrorReference={ErrorReference}", errorReference);
        }
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
                var apiValue = TryGetString(item, "apiValue", "expected", "expectedValue", "valueFromApi", "description", "value", "text");
                var pdfValue = TryGetString(item, "pdfValue", "actual", "actualValue", "valueFromPdf", "description", "value", "text");
                if (string.IsNullOrWhiteSpace(field)) continue;

                directRows.Add(new CompareRowDto
                {
                    Field = field,
                    ApiValue = apiValue,
                    PdfValue = pdfValue,
                    IsMatch = IsConfirmedMatch(apiValue, pdfValue)
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
                    IsMatch = IsConfirmedMatch(apiValue, pdfValue)
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
                    IsMatch = IsConfirmedMatch(value, value)
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
        if (element.ValueKind == JsonValueKind.Object)
        {
            // Common API style: { value: "..."} or { description: "..." }.
            var objectText = TryGetString(element, "value", "description", "text", "name");
            if (!string.IsNullOrWhiteSpace(objectText))
            {
                return objectText;
            }
        }

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

    private static bool IsConfirmedMatch(string? apiValue, string? pdfValue)
    {
        if (string.IsNullOrWhiteSpace(apiValue))
        {
            return !string.IsNullOrWhiteSpace(pdfValue);
        }

        return !string.IsNullOrWhiteSpace(pdfValue) &&
               Normalize(apiValue) == Normalize(pdfValue);
    }

    private static bool IsContainerField(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName)) return false;
        return fieldName.Contains("container", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCargoField(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName)) return false;
        return fieldName.Contains("cargo", StringComparison.OrdinalIgnoreCase) ||
               fieldName.Contains("item", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task AddWorkbookEntryAsync(
        ZipArchive archive,
        string fileName,
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyCollection<(string Field, string ApiValue, string PdfValue, bool IsMatch)> rows)
    {
        var workbookBytes = BuildApiWorkbookBytes(sheetName, headers, rows);
        var entry = archive.CreateEntry(fileName, CompressionLevel.Fastest);
        await using var entryStream = entry.Open();
        await entryStream.WriteAsync(workbookBytes, 0, workbookBytes.Length);
    }

    private static async Task AddWorkbookEntryAsync(
        ZipArchive archive,
        string fileName,
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyCollection<IReadOnlyCollection<(string Field, string ApiValue, string PdfValue, bool IsMatch)>> selectedRows)
    {
        var workbookBytes = BuildApiWorkbookBytes(sheetName, headers, selectedRows);
        var entry = archive.CreateEntry(fileName, CompressionLevel.Fastest);
        await using var entryStream = entry.Open();
        await entryStream.WriteAsync(workbookBytes, 0, workbookBytes.Length);
    }

    private static async Task<byte[]> BuildComparisonZipAsync(
        IReadOnlyCollection<(string Field, string ApiValue, string PdfValue, bool IsMatch)> exportRows)
        => await BuildComparisonZipAsync(new[] { exportRows });

    private static async Task<byte[]> BuildComparisonZipAsync(
        IReadOnlyCollection<IReadOnlyCollection<(string Field, string ApiValue, string PdfValue, bool IsMatch)>> selectedExports)
    {
        await using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            await AddWorkbookEntryAsync(archive, "ContainerData.xlsx", "Container Data", ContainerHeaders, selectedExports);
            await AddWorkbookEntryAsync(archive, "CargoItemsData.xlsx", "Cargo Items Data", CargoHeaders, selectedExports);
            await AddWorkbookEntryAsync(archive, "BLData.xlsx", "BL Data", BlHeaders, selectedExports);
        }

        return zipStream.ToArray();
    }

    private static List<(string Field, string ApiValue, string PdfValue, bool IsMatch)> TryBuildExportRows(string payloadJson)
    {
        try
        {
            var rows = JsonSerializer.Deserialize<List<IndexComparisonExportRow>>(payloadJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return rows?
                .Select(row =>
                {
                    var apiValue = row.ApiValue ?? string.Empty;
                    var pdfValue = row.PdfValue ?? string.Empty;
                    return (Field: row.Field ?? string.Empty, ApiValue: apiValue, PdfValue: pdfValue, IsMatch: IsConfirmedMatch(apiValue, pdfValue));
                })
                .ToList() ?? new List<(string Field, string ApiValue, string PdfValue, bool IsMatch)>();
        }
        catch (JsonException)
        {
            return new List<(string Field, string ApiValue, string PdfValue, bool IsMatch)>();
        }
    }

    private static string SanitizeFileName(string input)
    {
        var fileName = input.Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '-');
        }

        return string.IsNullOrWhiteSpace(fileName) ? "comparison" : fileName;
    }

    private static byte[] BuildApiWorkbookBytes(
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyCollection<(string Field, string ApiValue, string PdfValue, bool IsMatch)> rows)
        => BuildApiWorkbookBytes(sheetName, headers, new[] { rows });

    private static byte[] BuildApiWorkbookBytes(
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyCollection<IReadOnlyCollection<(string Field, string ApiValue, string PdfValue, bool IsMatch)>> selectedRows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(sheetName);

        for (var i = 0; i < headers.Count; i++)
        {
            var header = headers[i];
            ws.Cell(1, i + 1).Value = ToSafeExcelText(header);
        }

        var rowIndex = 2;
        foreach (var rows in selectedRows)
        {
            var rowLookup = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.Field))
                .GroupBy(x => NormalizeToken(x.Field))
                .ToDictionary(g => g.Key, g => PreferActualPdfValue(g.First().ApiValue, g.First().PdfValue), StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < headers.Count; i++)
            {
                ws.Cell(rowIndex, i + 1).Value = ToSafeExcelText(ResolveApiValueForHeader(headers[i], rowLookup));
            }

            rowIndex++;
        }

        ApplySafeWorkbookLayout(ws, headers.Count);
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
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

    private static void ApplySafeWorkbookLayout(IXLWorksheet ws, int columnCount)
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
            ws.Column(column).Width = columnCount > 20 ? 18 : 24;
        }

        ws.SheetView.FreezeRows(1);
    }

    private static string ResolveApiValueForHeader(string header, IReadOnlyDictionary<string, string> rowLookup)
    {
        var key = NormalizeToken(header);
        if (rowLookup.TryGetValue(key, out var directValue)) return directValue;

        if (key == NormalizeToken("CONTAINER Tare Weight (KGS)"))
        {
            var gross = ResolveFirstAvailableValue(rowLookup, new[] { NormalizeToken("gross_weight_total_kg") });
            var net = ResolveFirstAvailableValue(rowLookup, new[] { NormalizeToken("net_weight_total_kg") });
            if (TryParseDecimal(gross, out var grossVal) && TryParseDecimal(net, out var netVal))
            {
                return (grossVal - netVal).ToString("0.###", CultureInfo.InvariantCulture);
            }
        }

        if (HeaderToApiFieldAliases.TryGetValue(key, out var aliases))
        {
            foreach (var alias in aliases)
            {
                if (rowLookup.TryGetValue(alias, out var mappedValue))
                {
                    return mappedValue;
                }
            }
        }

        if (key == NormalizeToken("ISO CODE"))
        {
            var isoCode = DeriveIsoCode(rowLookup);
            if (!string.IsNullOrWhiteSpace(isoCode)) return isoCode;
        }

        var derivedPartyValue = ResolvePartyFieldFromCombinedSource(key, rowLookup);
        if (!string.IsNullOrWhiteSpace(derivedPartyValue)) return derivedPartyValue;

        // Fallback: match closest API field by substring containment in either direction.
        var best = rowLookup.FirstOrDefault(x => x.Key.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                                                 key.Contains(x.Key, StringComparison.OrdinalIgnoreCase));
        return best.Equals(default(KeyValuePair<string, string>)) ? string.Empty : best.Value;
    }

    private static string DeriveIsoCode(IReadOnlyDictionary<string, string> rowLookup)
    {
        var direct = ResolveFirstAvailableValue(rowLookup, new[]
        {
            NormalizeToken("iso_code"),
            NormalizeToken("container_iso_code")
        });
        if (!string.IsNullOrWhiteSpace(direct)) return direct;

        var size = ResolveFirstAvailableValue(rowLookup, new[]
        {
            NormalizeToken("container_size"),
            NormalizeToken("size"),
            NormalizeToken("size_type"),
            NormalizeToken("container_size_type")
        });
        var type = ResolveFirstAvailableValue(rowLookup, new[]
        {
            NormalizeToken("container_type"),
            NormalizeToken("type"),
            NormalizeToken("equipment_type"),
            NormalizeToken("size_type"),
            NormalizeToken("container_size_type")
        });

        var combined = $"{size} {type}".ToUpperInvariant();
        if (combined.Contains("45") || (combined.Contains("40") && (combined.Contains("HC") || combined.Contains("HQ") || combined.Contains("HIGH"))))
        {
            return "45G1";
        }

        if (combined.Contains("40")) return "42G1";
        if (combined.Contains("20")) return "22G1";
        return string.Empty;
    }

    private static bool TryParseDecimal(string? input, out decimal value)
    {
        return decimal.TryParse(
            (input ?? string.Empty).Trim(),
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture,
            out value)
            || decimal.TryParse(
                (input ?? string.Empty).Trim(),
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands,
                CultureInfo.CurrentCulture,
                out value);
    }

    private static string ResolvePartyFieldFromCombinedSource(string headerKey, IReadOnlyDictionary<string, string> rowLookup)
    {
        if (PartyNameHeaderToSourceAliases.TryGetValue(headerKey, out var nameAliases))
        {
            var full = ResolveFirstAvailableValue(rowLookup, nameAliases);
            var (name, _) = SplitPartyNameAndAddress(full);
            return name;
        }

        if (PartyAddressHeaderToSourceAliases.TryGetValue(headerKey, out var addressConfig))
        {
            var full = ResolveFirstAvailableValue(rowLookup, addressConfig.SourceAliases);
            var (_, addressLines) = SplitPartyNameAndAddress(full);
            return addressConfig.LineIndex < addressLines.Count ? addressLines[addressConfig.LineIndex] : string.Empty;
        }

        return string.Empty;
    }

    private static string ResolveFirstAvailableValue(
        IReadOnlyDictionary<string, string> rowLookup,
        IReadOnlyCollection<string> aliases)
    {
        foreach (var alias in aliases)
        {
            if (rowLookup.TryGetValue(alias, out var val) && !string.IsNullOrWhiteSpace(val))
            {
                return val;
            }
        }

        return string.Empty;
    }

    private static (string Name, List<string> AddressLines) SplitPartyNameAndAddress(string? fullText)
    {
        if (string.IsNullOrWhiteSpace(fullText)) return (string.Empty, new List<string>());
        var clean = fullText.Replace("\r", string.Empty).Trim();

        var lines = clean
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length >= 2)
        {
            return (lines[0], lines.Skip(1).Where(x => !string.IsNullOrWhiteSpace(x)).ToList());
        }

        var commaParts = clean
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (commaParts.Length >= 2)
        {
            return (commaParts[0], commaParts.Skip(1).Where(x => !string.IsNullOrWhiteSpace(x)).ToList());
        }

        var doubleSpaceIndex = clean.IndexOf("  ", StringComparison.Ordinal);
        if (doubleSpaceIndex > 0)
        {
            var name = clean[..doubleSpaceIndex].Trim();
            var address = clean[(doubleSpaceIndex + 2)..].Trim();
            return (name, new List<string> { address });
        }

        return (clean, new List<string>());
    }

    private static string NormalizeToken(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var chars = input
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();
        return new string(chars);
    }

    private static string ExtractBillOfLadingNumber(IEnumerable<IndexComparisonExportRow> rows)
    {
        var billOfLadingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            NormalizeToken("bill_of_lading_number"),
            NormalizeToken("BL NO"),
            NormalizeToken("MBL No.")
        };

        var row = rows.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Field) &&
                                           billOfLadingKeys.Contains(NormalizeToken(x.Field)));
        return row == null ? string.Empty : PreferActualPdfValue(row.ApiValue, row.PdfValue);
    }

    private static string PreferActualPdfValue(string? apiValue, string? pdfValue)
    {
        return !string.IsNullOrWhiteSpace(pdfValue) ? pdfValue : apiValue ?? string.Empty;
    }

    private async Task<List<PdfComparisonSubmissionListItemVm>> GetPdfSubmissionListItemsAsync()
    {
        var submissions = await dbContext.PdfComparisonSubmissions
            .OrderByDescending(x => x.SubmittedAt)
            .Select(x => new PdfComparisonSubmissionListItemVm
            {
                Id = x.Id,
                BillOfLadingNumber = x.BillOfLadingNumber,
                PayloadJson = x.PayloadJson,
                SourceFileName = x.SourceFileName,
                SubmittedByUserId = x.SubmittedByUserId,
                SubmittedAt = x.SubmittedAt,
                IsActive = x.IsActive
            })
            .ToListAsync();

        foreach (var submission in submissions)
        {
            var exportRows = TryBuildExportRows(submission.PayloadJson);
            submission.TotalFields = exportRows.Count;
            submission.IssueCount = exportRows.Count(x => !x.IsMatch);
        }

        return submissions;
    }

    private static readonly string[] ContainerHeaders =
    {
        "BL NO",
        "CONTAINER NO.",
        "PACKAGE TYPE",
        "NO OF PACKAGES",
        "CARGO GROSS WEIGHT (KGS) ",
        "CONTAINER WEIGHT (VGM/Gross) (KGS)",
        "CONTAINER Tare Weight (KGS)",
        "Gross Volume (CBM)",
        "ISO CODE",
        "IMO CODE",
        "UNO NO.",
        "TEMPERATURE",
        "TEMPERATURE UOM",
        "Over Dimension at Front",
        "Over Dimension at Rear",
        "Over Dimension at Height",
        "Over Dimension at Left width",
        "Over Dimension at Right width",
        "SOC FLAG",
        "SEAL TYPE",
        "CUSTOM SEAL",
        "AGENT SEAL",
        "CONTAINER STATUS",
        "Container Agent Code"
    };

    // Explicit aliases for BLDATA column-to-field mapping to avoid ambiguous token matches.
    private static readonly IReadOnlyDictionary<string, string[]> HeaderToApiFieldAliases =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [NormalizeToken("BL NO")] = new[] { NormalizeToken("bill_of_lading_number") },
            [NormalizeToken("BL DATE")] = new[] { NormalizeToken("bill_date"), NormalizeToken("bl_date"), NormalizeToken("date_of_issue") },
            [NormalizeToken("CONTAINER NO.")] = new[] { NormalizeToken("container_numbers") },
            [NormalizeToken("PACKAGE TYPE")] = new[] { NormalizeToken("package_type"), NormalizeToken("type_of_packages"), NormalizeToken("packages_type") },
            [NormalizeToken("TYPE OF PACKAGES")] = new[] { NormalizeToken("type_of_packages"), NormalizeToken("package_type"), NormalizeToken("packages_type") },
            [NormalizeToken("NO OF PACKAGES")] = new[] { NormalizeToken("number_of_packages"), NormalizeToken("no_of_packages"), NormalizeToken("total_packages") },
            [NormalizeToken("CARGO GROSS WEIGHT (KGS)")] = new[] { NormalizeToken("gross_weight_total_kg"), NormalizeToken("cargo_gross_weight_kgs"), NormalizeToken("gross_weight") },
            [NormalizeToken("CONTAINER WEIGHT (VGM/Gross) (KGS)")] = new[] { NormalizeToken("net_weight_total_kg"), NormalizeToken("vgm_gross_weight_kg"), NormalizeToken("container_weight_kg") },
            [NormalizeToken("Gross Volume (CBM)")] = new[] { NormalizeToken("gross_volume_cbm"), NormalizeToken("volume_cbm"), NormalizeToken("measurement_cbm") },
            [NormalizeToken("ISO CODE")] = new[] { NormalizeToken("iso_code"), NormalizeToken("container_iso_code") },
            [NormalizeToken("SEAL TYPE")] = new[] { NormalizeToken("seal_type") },
            [NormalizeToken("CUSTOM SEAL")] = new[] { NormalizeToken("custom_seal"), NormalizeToken("seal_number"), NormalizeToken("seal_numbers") },
            [NormalizeToken("AGENT SEAL")] = new[] { NormalizeToken("agent_seal"), NormalizeToken("seal_number"), NormalizeToken("seal_numbers") },
            [NormalizeToken("HS CODE")] = new[] { NormalizeToken("hs_code") },
            [NormalizeToken("CARGO ITEM DESCRIPTION")] = new[] { NormalizeToken("goods_description") },
            [NormalizeToken("POA")] = new[] { NormalizeToken("place_of_receipt") },
            [NormalizeToken("POL")] = new[] { NormalizeToken("port_of_loading") },
            [NormalizeToken("POD")] = new[] { NormalizeToken("port_of_discharge") },
            [NormalizeToken("FPOD")] = new[] { NormalizeToken("place_of_delivery") },
            [NormalizeToken("MARKS & NUMBERS")] = new[] { NormalizeToken("marks_and_numbers"), NormalizeToken("marks_numbers"), NormalizeToken("marks_number"), NormalizeToken("shipping_marks") },
            [NormalizeToken("CARGO DESCRIPTION")] = new[] { NormalizeToken("goods_description"), NormalizeToken("cargo_description") },
            [NormalizeToken("CONSIGNEE NAME")] = new[] { NormalizeToken("consignee_name") },
            [NormalizeToken("CONSIGNEE ADDRESS1")] = new[] { NormalizeToken("consignee_address1"), NormalizeToken("consignee_address_line1") },
            [NormalizeToken("CONSIGNEE ADDRESS2")] = new[] { NormalizeToken("consignee_address2"), NormalizeToken("consignee_address_line2") },
            [NormalizeToken("CONSIGNEE ADDRESS3")] = new[] { NormalizeToken("consignee_address3"), NormalizeToken("consignee_address_line3") },
            [NormalizeToken("CONSIGNEE CITY")] = new[] { NormalizeToken("consignee_city") },
            [NormalizeToken("Consignee Country Sub- division/ State name")] = new[] { NormalizeToken("consignee_state_name"), NormalizeToken("consignee_state") },
            [NormalizeToken("Consignee Country Sub- division/ State Code")] = new[] { NormalizeToken("consignee_state_code") },
            [NormalizeToken("CONSIGNEE COUNTRY CODE")] = new[] { NormalizeToken("consignee_country_code") },
            [NormalizeToken("CONSIGNEE ZIP CODE")] = new[] { NormalizeToken("consignee_zip_code"), NormalizeToken("consignee_postal_code") },
            [NormalizeToken("CONSIGNEE IEC")] = new[] { NormalizeToken("consignee_iec"), NormalizeToken("iec") },
            [NormalizeToken("CONSIGNEE GST")] = new[] { NormalizeToken("consignee_gst"), NormalizeToken("gst") },
            [NormalizeToken("CONSIGNEE PAN")] = new[] { NormalizeToken("consignee_pan"), NormalizeToken("pan") },
            [NormalizeToken("CONSIGNEE EMAIL")] = new[] { NormalizeToken("consignee_email"), NormalizeToken("email") },
            [NormalizeToken("NOTIFY NAME")] = new[] { NormalizeToken("notify_name"), NormalizeToken("notify_party_name") },
            [NormalizeToken("NOTIFY ADDRESS1")] = new[] { NormalizeToken("notify_address1"), NormalizeToken("notify_address_line1") },
            [NormalizeToken("NOTIFY ADDRESS2")] = new[] { NormalizeToken("notify_address2"), NormalizeToken("notify_address_line2") },
            [NormalizeToken("NOTIFY ADDRESS3")] = new[] { NormalizeToken("notify_address3"), NormalizeToken("notify_address_line3") },
            [NormalizeToken("NOTIFY CITY")] = new[] { NormalizeToken("notify_city") },
            [NormalizeToken("Notify party Country Sub- division/ State name")] = new[] { NormalizeToken("notify_state_name"), NormalizeToken("notify_state") },
            [NormalizeToken("Notify party Country Sub- division/ State code")] = new[] { NormalizeToken("notify_state_code") },
            [NormalizeToken("NOTIFY COUNTRY CODE")] = new[] { NormalizeToken("notify_country_code") },
            [NormalizeToken("NOTIFY ZIP CODE")] = new[] { NormalizeToken("notify_zip_code"), NormalizeToken("notify_postal_code") },
            [NormalizeToken("NOTIFY PAN")] = new[] { NormalizeToken("notify_pan") }
        };

    private static readonly IReadOnlyDictionary<string, string[]> PartyNameHeaderToSourceAliases =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [NormalizeToken("NOTIFY NAME")] = new[]
            {
                NormalizeToken("notify_party"),
                NormalizeToken("notify_name")
            },
            [NormalizeToken("CONSIGNEE NAME")] = new[]
            {
                NormalizeToken("consignee"),
                NormalizeToken("consignee_name")
            },
            [NormalizeToken("CONSIGNOR NAME")] = new[]
            {
                NormalizeToken("shipper"),
                NormalizeToken("consignor"),
                NormalizeToken("consignor_name")
            }
        };

    private static readonly IReadOnlyDictionary<string, (string[] SourceAliases, int LineIndex)> PartyAddressHeaderToSourceAliases =
        new Dictionary<string, (string[] SourceAliases, int LineIndex)>(StringComparer.OrdinalIgnoreCase)
        {
            [NormalizeToken("NOTIFY ADDRESS1")] = (new[]
            {
                NormalizeToken("notify_party"),
                NormalizeToken("notify_name")
            }, 0),
            [NormalizeToken("NOTIFY ADDRESS2")] = (new[]
            {
                NormalizeToken("notify_party"),
                NormalizeToken("notify_name")
            }, 1),
            [NormalizeToken("NOTIFY ADDRESS3")] = (new[]
            {
                NormalizeToken("notify_party"),
                NormalizeToken("notify_name")
            }, 2),
            [NormalizeToken("CONSIGNEE ADDRESS1")] = (new[]
            {
                NormalizeToken("consignee"),
                NormalizeToken("consignee_name")
            }, 0),
            [NormalizeToken("CONSIGNEE ADDRESS2")] = (new[]
            {
                NormalizeToken("consignee"),
                NormalizeToken("consignee_name")
            }, 1),
            [NormalizeToken("CONSIGNEE ADDRESS3")] = (new[]
            {
                NormalizeToken("consignee"),
                NormalizeToken("consignee_name")
            }, 2),
            [NormalizeToken("CONSIGNOR ADDRESS1")] = (new[]
            {
                NormalizeToken("shipper"),
                NormalizeToken("consignor"),
                NormalizeToken("consignor_name")
            }, 0),
            [NormalizeToken("CONSIGNOR ADDRESS2")] = (new[]
            {
                NormalizeToken("shipper"),
                NormalizeToken("consignor"),
                NormalizeToken("consignor_name")
            }, 1),
            [NormalizeToken("CONSIGNOR ADDRESS3")] = (new[]
            {
                NormalizeToken("shipper"),
                NormalizeToken("consignor"),
                NormalizeToken("consignor_name")
            }, 2)
        };

    private static readonly string[] CargoHeaders =
    {
        "BL NO",
        "IMO CODE",
        "UNO NO.",
        "HS CODE",
        "CARGO ITEM DESCRIPTION",
        "NO OF PACKAGES",
        "TYPE OF PACKAGES"
    };

    private static readonly string[] BlHeaders =
    {
        "BL NO",
        "BL DATE",
        "CIN Type",
        "CIN Number",
        "CIN DATE",
        "CSN Number",
        "CSN DATE",
        "POA",
        "POL",
        "POD",
        "FPOD",
        "CFS",
        "Port of Transshipment",
        "MARKS & NUMBERS",
        "CARGO DESCRIPTION",
        "ITEM TYPE",
        "NATURE OF CARGO",
        "INVOICE VALUE OF CONSIGNMENT",
        "CURRENCY CODE",
        "CONSIGNEE NAME",
        "CONSIGNEE ADDRESS1",
        "CONSIGNEE ADDRESS2",
        "CONSIGNEE ADDRESS3",
        "CONSIGNEE CITY",
        "Consignee Country Sub- division/ State name",
        "Consignee Country Sub- division/ State Code",
        "CONSIGNEE COUNTRY CODE",
        "CONSIGNEE ZIP CODE",
        "CONSIGNEE IEC",
        "CONSIGNEE GST",
        "CONSIGNEE PAN",
        "CONSIGNEE EMAIL",
        "NOTIFY PAN",
        "NOTIFY NAME",
        "NOTIFY ADDRESS1",
        "NOTIFY ADDRESS2",
        "NOTIFY ADDRESS3",
        "NOTIFY CITY",
        "Notify party Country Sub- division/ State name",
        "Notify party Country Sub- division/ State code",
        "NOTIFY COUNTRY CODE",
        "NOTIFY ZIP CODE",
        "CONSIGNOR NAME",
        "CONSIGNOR ADDRESS1",
        "CONSIGNOR ADDRESS2",
        "CONSIGNOR ADDRESS3",
        "CONSIGNOR CITY",
        "Consignor Country Sub- division/ State name",
        "Consignor Country Sub- division/ State Code",
        "CONSIGNOR COUNTRY CODE",
        "CONSIGNOR ZIP CODE",
        "CONSIGNOR IEC",
        "CONSIGNOR GST",
        "CONSIGNOR PAN",
        "CONSIGNOR EMAIL",
        "MBL No.",
        "MBL DATE",
        "Transit Bond No.",
        "Carrier PAN No.",
        "Carrier/ Vendor Name",
        "Mode of Transport",
        "Forwarder's PAN No."
    };

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
