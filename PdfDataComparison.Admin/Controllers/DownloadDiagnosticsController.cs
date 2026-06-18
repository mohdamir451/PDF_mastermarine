using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PdfDataComparison.Admin.Application.Interfaces;
using PdfDataComparison.Admin.Application.ViewModels;
using PdfDataComparison.Admin.Constants;

namespace PdfDataComparison.Admin.Controllers;

[Authorize(Policy = PermissionCatalog.AuditLogsView)]
public class DownloadDiagnosticsController(IDownloadDiagnosticLogStore diagnosticLogStore) : Controller
{
    public async Task<IActionResult> Index()
    {
        var entries = await diagnosticLogStore.ReadLatestAsync();
        return View(new DownloadDiagnosticLogsVm { Entries = entries.ToList() });
    }
}
