using PdfDataComparison.Admin.Application.Interfaces;
using PdfDataComparison.Admin.Constants;
using PdfDataComparison.Admin.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PdfDataComparison.Admin.Controllers;

[Authorize(Policy = PermissionCatalog.PermissionsManage)]
public class PermissionsController(ApplicationDbContext dbContext, IRoleService roleService, IPermissionService permissionService) : Controller
{
    public async Task<IActionResult> Index(string? roleId)
    {
        ViewBag.Roles = await roleService.GetRolesAsync();
        ViewBag.SelectedRoleId = roleId;
        ViewBag.Pages = await dbContext.PagePermissions.OrderBy(x => x.PageName).ToListAsync();
        ViewBag.RolePermissions = string.IsNullOrWhiteSpace(roleId)
            ? new List<Domain.Entities.RolePermission>()
            : await permissionService.GetRolePermissionsAsync(roleId);
        return View();
    }
}
