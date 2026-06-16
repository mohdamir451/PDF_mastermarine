using PdfDataComparison.Admin.Application.Interfaces;
using PdfDataComparison.Admin.Application.ViewModels;
using PdfDataComparison.Admin.Constants;
using PdfDataComparison.Admin.Data;
using PdfDataComparison.Admin.Domain.Entities;
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string roleId, List<RolePermissionInputVm> permissions)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return BadRequest("A role must be selected before saving permissions.");
        }

        var entities = permissions.Select(x => new RolePermission
        {
            RoleId = roleId,
            PagePermissionId = x.PagePermissionId,
            CanView = x.CanView,
            CanCreate = x.CanCreate,
            CanEdit = x.CanEdit,
            CanDelete = x.CanDelete,
            CanSubmit = x.CanSubmit,
            CanExport = x.CanExport
        });

        await permissionService.UpsertRolePermissionsAsync(roleId, entities);
        return RedirectToAction(nameof(Index), new { roleId });
    }
}
