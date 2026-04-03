using PdfDataComparison.Admin.Constants;
using PdfDataComparison.Admin.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PdfDataComparison.Admin.Controllers;

[Authorize(Policy = PermissionCatalog.ReportsView)]
public class ReportsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index() => View(await dbContext.ComparisonSubmissions.OrderByDescending(x => x.SubmittedAt).ToListAsync());
    public async Task<IActionResult> Details(int id) => View(await dbContext.ComparisonSubmissions.FirstAsync(x => x.Id == id));
}
