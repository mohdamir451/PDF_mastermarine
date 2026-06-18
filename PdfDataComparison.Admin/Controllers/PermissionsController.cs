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
    public async Task<IActionResult> Save(string roleId)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return BadRequest("A role must be selected before saving permissions.");
        }

        var pages = await dbContext.PagePermissions
            .OrderBy(x => x.PageName)
            .ToListAsync();

        var entities = pages.Select((page, index) => new RolePermission
        {
            RoleId = roleId,
            PagePermissionId = ReadPostedInt($"permissions[{index}].PagePermissionId", page.Id),
            CanView = ReadPostedBool($"permissions[{index}].CanView"),
            CanCreate = ReadPostedBool($"permissions[{index}].CanCreate"),
            CanEdit = ReadPostedBool($"permissions[{index}].CanEdit"),
            CanDelete = ReadPostedBool($"permissions[{index}].CanDelete"),
            CanSubmit = ReadPostedBool($"permissions[{index}].CanSubmit"),
            CanExport = ReadPostedBool($"permissions[{index}].CanExport")
        })
            .Where(x => x.PagePermissionId > 0)
            .ToList();

        await permissionService.UpsertRolePermissionsAsync(roleId, entities);
        TempData["SuccessMessage"] = "Permission matrix saved successfully.";
        return RedirectToAction(nameof(Index), new { roleId });
    }

    private bool ReadPostedBool(string key)
    {
        var values = Request.Form[key];
        return values.Any(value => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));
    }

    private int ReadPostedInt(string key, int fallback)
    {
        var value = Request.Form[key].FirstOrDefault();
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }
}
