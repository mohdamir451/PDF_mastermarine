using PdfDataComparison.Admin.Application.Interfaces;
using PdfDataComparison.Admin.Application.ViewModels;
using PdfDataComparison.Admin.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PdfDataComparison.Admin.Controllers;

[Authorize(Policy = PermissionCatalog.UsersManage)]
public class UsersController(IUserService userService, IRoleService roleService, IAuditService auditService) : Controller
{
    public async Task<IActionResult> Index(string? search, int page = 1)
        => View(await userService.GetUsersAsync(search, page, 10));

    public async Task<IActionResult> Create()
    {
        var vm = new UserCreateVm
        {
            AvailableRoles = (await roleService.GetRolesAsync()).Select(x => x.Name ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateVm vm)
    {
        vm.AvailableRoles = (await roleService.GetRolesAsync()).Select(x => x.Name ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        if (!ModelState.IsValid) return View(vm);

        var result = await userService.CreateUserAsync(vm);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(vm);
        }

        await auditService.LogAsync("UserCreated", "Users", $"Created user {vm.Email}", User.Identity?.Name, User.Identity?.Name ?? "Unknown");
        TempData["SuccessMessage"] = "User created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var vm = await userService.GetUserForEditAsync(id);
        if (vm == null) return NotFound();

        vm.AvailableRoles = (await roleService.GetRolesAsync())
            .Select(x => x.Name ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditVm vm)
    {
        vm.AvailableRoles = (await roleService.GetRolesAsync())
            .Select(x => x.Name ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (!ModelState.IsValid) return View(vm);

        var result = await userService.UpdateUserAsync(vm);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(vm);
        }

        await auditService.LogAsync("UserUpdated", "Users", $"Updated user {vm.Email}", User.Identity?.Name, User.Identity?.Name ?? "Unknown");
        TempData["SuccessMessage"] = "User updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ResetPassword(string id)
    {
        var vm = await userService.GetUserForPasswordResetAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(UserResetPasswordVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await userService.ResetPasswordAsync(vm);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(vm);
        }

        await auditService.LogAsync("UserPasswordReset", "Users", $"Reset password for user {vm.Email}", User.Identity?.Name, User.Identity?.Name ?? "Unknown");
        TempData["SuccessMessage"] = $"Password reset successfully for {vm.Email}.";
        return RedirectToAction(nameof(Index));
    }
}
