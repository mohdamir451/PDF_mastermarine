using PdfDataComparison.Admin.Application.Interfaces;
using PdfDataComparison.Admin.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PdfDataComparison.Admin.Controllers;

[Authorize(Policy = PermissionCatalog.UsersManage)]
public class UsersController(IUserService userService) : Controller
{
    public async Task<IActionResult> Index(string? search, int page = 1)
        => View(await userService.GetUsersAsync(search, page, 10));

    public IActionResult Create() => View();
    public IActionResult Edit(string id) => View(model: id);
}
