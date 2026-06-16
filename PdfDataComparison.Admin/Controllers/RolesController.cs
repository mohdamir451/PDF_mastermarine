using PdfDataComparison.Admin.Application.Interfaces;
using PdfDataComparison.Admin.Application.ViewModels;
using PdfDataComparison.Admin.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PdfDataComparison.Admin.Controllers;

[Authorize(Policy = PermissionCatalog.RolesManage)]
public class RolesController(IRoleService roleService)
    : Controller
{
    public async Task<IActionResult> Index() => View(await roleService.GetRolesAsync());

    public IActionResult Create() => View(new RoleFormVm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleFormVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Name))
        {
            ModelState.AddModelError(nameof(vm.Name), "Role name is required.");
        }

        if (!ModelState.IsValid) return View(vm);

        var result = await roleService.CreateRoleAsync(vm.Name, vm.Description);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(vm);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var role = await roleService.GetRoleAsync(id);
        if (role == null) return NotFound();

        return View(new RoleFormVm
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RoleFormVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Id)) return BadRequest();
        if (string.IsNullOrWhiteSpace(vm.Name))
        {
            ModelState.AddModelError(nameof(vm.Name), "Role name is required.");
        }

        if (!ModelState.IsValid) return View(vm);

        var result = await roleService.UpdateRoleAsync(vm.Id, vm.Name, vm.Description);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(vm);
        }

        return RedirectToAction(nameof(Index));
    }
}
