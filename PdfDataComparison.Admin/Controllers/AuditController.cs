using PdfDataComparison.Admin.Constants;
using PdfDataComparison.Admin.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PdfDataComparison.Admin.Controllers;

[Authorize(Policy = PermissionCatalog.AuditLogsView)]
public class AuditController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index() => View(await dbContext.AuditLogs.OrderByDescending(x => x.Timestamp).Take(200).ToListAsync());
}
