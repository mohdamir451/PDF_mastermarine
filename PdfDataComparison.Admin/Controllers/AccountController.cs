using System.Security.Claims;
using PdfDataComparison.Admin.Application.Interfaces;
using PdfDataComparison.Admin.Application.ViewModels;
using PdfDataComparison.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace PdfDataComparison.Admin.Controllers;

public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IAuditService auditService) : Controller
{
    [AllowAnonymous]
    public IActionResult Login() => View(new LoginVm());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await userManager.FindByNameAsync(vm.Username);
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Invalid credentials.");
            return View(vm);
        }

        var result = await signInManager.PasswordSignInAsync(vm.Username, vm.Password, vm.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid credentials.");
            return View(vm);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        await auditService.LogAsync("Login", "Account", "User login success", user.Id, user.FullName);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Dashboard");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await signInManager.SignOutAsync();
        await auditService.LogAsync("Logout", "Account", "User logout", userId);
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    public IActionResult AccessDenied() => View();
}
