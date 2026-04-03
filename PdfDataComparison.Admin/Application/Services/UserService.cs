using PdfDataComparison.Admin.Application.Interfaces;
using PdfDataComparison.Admin.Application.ViewModels;
using PdfDataComparison.Admin.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PdfDataComparison.Admin.Application.Services;

public class UserService(UserManager<ApplicationUser> userManager) : IUserService
{
    public async Task<PagedResult<UserListItemVm>> GetUsersAsync(string? search, int page, int pageSize)
    {
        var query = userManager.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.FullName.Contains(search) || x.Email!.Contains(search) || x.UserName!.Contains(search));
        }

        var total = await query.CountAsync();
        var users = await query.OrderByDescending(x => x.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<UserListItemVm>();
        foreach (var user in users)
        {
            items.Add(new UserListItemVm
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Department = user.Department,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                LastLoginAt = user.LastLoginAt,
                Roles = (await userManager.GetRolesAsync(user)).ToList()
            });
        }

        return new PagedResult<UserListItemVm> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }
}
