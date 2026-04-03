using PdfDataComparison.Admin.Application.ViewModels;
using PdfDataComparison.Admin.Constants;
using PdfDataComparison.Admin.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PdfDataComparison.Admin.Controllers;

[Authorize(Policy = PermissionCatalog.DashboardView)]
public class DashboardController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var vm = new DashboardVm
        {
            TotalUsers = await dbContext.Users.CountAsync(),
            ActiveUsers = await dbContext.Users.CountAsync(x => x.IsActive),
            TotalRoles = await dbContext.Roles.CountAsync(),
            TotalComparisonJobs = await dbContext.ComparisonJobs.CountAsync(),
            PendingMismatches = await dbContext.ComparisonFields.CountAsync(x => !x.IsMatch && x.IsBlocking),
            CompletedSubmissions = await dbContext.ComparisonSubmissions.CountAsync(),
            RecentActivity = await dbContext.AuditLogs.OrderByDescending(x => x.Timestamp).Take(5).Select(x => $"{x.Action} · {x.TargetEntity}").ToListAsync()
        };
        return View(vm);
    }
}
