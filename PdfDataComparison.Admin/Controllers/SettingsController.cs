using PdfDataComparison.Admin.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PdfDataComparison.Admin.Controllers;

[Authorize(Policy = PermissionCatalog.SettingsManage)]
public class SettingsController : Controller
{
    public IActionResult Index() => View();
}
