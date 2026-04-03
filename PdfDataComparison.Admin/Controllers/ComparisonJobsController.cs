using System.Security.Claims;
using PdfDataComparison.Admin.Application.Interfaces;
using PdfDataComparison.Admin.Application.ViewModels;
using PdfDataComparison.Admin.Constants;
using PdfDataComparison.Admin.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PdfDataComparison.Admin.Controllers;

[Authorize(Policy = PermissionCatalog.ComparisonJobsView)]
public class ComparisonJobsController(IComparisonService comparisonService, ApplicationDbContext dbContext, IExportService exportService) : Controller
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
}
