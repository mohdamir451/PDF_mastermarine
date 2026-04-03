using PdfDataComparison.Admin.Application.Interfaces;
using PdfDataComparison.Admin.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PdfDataComparison.Admin.Controllers;

[Authorize(Policy = PermissionCatalog.RolesManage)]
public class RolesController(IRoleService roleService)
    : Controller
{
    public async Task<IActionResult> Index() => View(await roleService.GetRolesAsync());
    public IActionResult Create() => View();
    public IActionResult Edit(string id) => View(model: id);
}
