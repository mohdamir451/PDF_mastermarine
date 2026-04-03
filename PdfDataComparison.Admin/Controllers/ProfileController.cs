using PdfDataComparison.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace PdfDataComparison.Admin.Controllers;

[Authorize]
public class ProfileController(UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> ChangePassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View();
        }
        TempData["Message"] = "Password changed.";
        return RedirectToAction(nameof(ChangePassword));
    }
}
