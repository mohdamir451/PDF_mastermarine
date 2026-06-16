using PdfDataComparison.Admin.Constants;
using PdfDataComparison.Admin.Application.ViewModels;
using PdfDataComparison.Admin.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PdfDataComparison.Admin.Controllers;

[Authorize(Policy = PermissionCatalog.ReportsView)]
public class ReportsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var jobSubmissions = await dbContext.ComparisonSubmissions
            .Select(x => new ReportHistoryItemVm
            {
                Id = x.Id,
                SourceType = "Comparison Job",
                Reference = $"Job #{x.ComparisonJobId}",
                SourceFileName = x.ExcelFilePath,
                SubmittedByUserId = x.SubmittedByUserId,
                SubmittedAt = x.SubmittedAt,
                HasDetailsPage = true
            })
            .ToListAsync();

        var pdfSubmissions = await dbContext.PdfComparisonSubmissions
            .Where(x => x.IsActive)
            .Select(x => new ReportHistoryItemVm
            {
                Id = x.Id,
                SourceType = "PDF Upload",
                Reference = string.IsNullOrWhiteSpace(x.BillOfLadingNumber) ? $"PDF #{x.Id}" : x.BillOfLadingNumber,
                SourceFileName = x.SourceFileName,
                SubmittedByUserId = x.SubmittedByUserId,
                SubmittedAt = x.SubmittedAt,
                HasDetailsPage = false
            })
            .ToListAsync();

        return View(jobSubmissions.Concat(pdfSubmissions).OrderByDescending(x => x.SubmittedAt).ToList());
    }

    public async Task<IActionResult> Details(int id) => View(await dbContext.ComparisonSubmissions.FirstAsync(x => x.Id == id));
}
