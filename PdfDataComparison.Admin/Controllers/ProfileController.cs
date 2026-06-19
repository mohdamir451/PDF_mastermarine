using PdfDataComparison.Admin.Application.ViewModels;
using PdfDataComparison.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace PdfDataComparison.Admin.Controllers;

[Authorize]
public class ProfileController(UserManager<ApplicationUser> userManager) : Controller
{
    public IActionResult ChangePassword() => View(new ProfileChangePasswordVm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ProfileChangePasswordVm vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");

        var result = await userManager.ChangePasswordAsync(user, vm.CurrentPassword, vm.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(vm);
        }
        TempData["Message"] = "Password changed.";
        return RedirectToAction(nameof(ChangePassword));
    }
}
