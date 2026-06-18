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

    public async Task<(bool Succeeded, string Error)> CreateUserAsync(UserCreateVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.FullName)) return (false, "Full name is required.");
        if (string.IsNullOrWhiteSpace(vm.Email)) return (false, "Email address is required.");
        if (string.IsNullOrWhiteSpace(vm.Password)) return (false, "Password is required.");
        if (!string.Equals(vm.Password, vm.ConfirmPassword, StringComparison.Ordinal)) return (false, "Password and confirm password do not match.");

        var email = vm.Email.Trim();
        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null) return (false, "Email address is already used by another user.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = vm.FullName.Trim(),
            Department = vm.Department?.Trim() ?? string.Empty,
            IsActive = vm.IsActive,
            CreatedDate = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
        {
            return (false, string.Join(" ", result.Errors.Select(x => x.Description)));
        }

        if (!string.IsNullOrWhiteSpace(vm.RoleName))
        {
            var roleResult = await userManager.AddToRoleAsync(user, vm.RoleName);
            if (!roleResult.Succeeded)
            {
                return (false, $"User was created but role assignment failed: {string.Join(" ", roleResult.Errors.Select(x => x.Description))}");
            }
        }

        return (true, string.Empty);
    }

    public async Task<UserEditVm?> GetUserForEditAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return null;

        var roles = await userManager.GetRolesAsync(user);
        return new UserEditVm
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Department = user.Department,
            IsActive = user.IsActive,
            RoleName = roles.FirstOrDefault() ?? string.Empty
        };
    }

    public async Task<(bool Succeeded, string Error)> UpdateUserAsync(UserEditVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Id)) return (false, "User id is required.");
        if (string.IsNullOrWhiteSpace(vm.FullName)) return (false, "Full name is required.");
        if (string.IsNullOrWhiteSpace(vm.Email)) return (false, "Email address is required.");

        var user = await userManager.FindByIdAsync(vm.Id);
        if (user == null) return (false, "User was not found.");

        var email = vm.Email.Trim();
        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null && existing.Id != user.Id)
        {
            return (false, "Email address is already used by another user.");
        }

        user.UserName = email;
        user.Email = email;
        user.FullName = vm.FullName.Trim();
        user.Department = vm.Department?.Trim() ?? string.Empty;
        user.IsActive = vm.IsActive;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return (false, string.Join(" ", updateResult.Errors.Select(x => x.Description)));
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                return (false, $"User was updated but role removal failed: {string.Join(" ", removeResult.Errors.Select(x => x.Description))}");
            }
        }

        if (!string.IsNullOrWhiteSpace(vm.RoleName))
        {
            var addResult = await userManager.AddToRoleAsync(user, vm.RoleName);
            if (!addResult.Succeeded)
            {
                return (false, $"User was updated but role assignment failed: {string.Join(" ", addResult.Errors.Select(x => x.Description))}");
            }
        }

        return (true, string.Empty);
    }

    public async Task<UserResetPasswordVm?> GetUserForPasswordResetAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return null;

        return new UserResetPasswordVm
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty
        };
    }

    public async Task<(bool Succeeded, string Error)> ResetPasswordAsync(UserResetPasswordVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Id)) return (false, "User id is required.");
        if (string.IsNullOrWhiteSpace(vm.NewPassword)) return (false, "New password is required.");
        if (!string.Equals(vm.NewPassword, vm.ConfirmPassword, StringComparison.Ordinal)) return (false, "Password and confirm password do not match.");

        var user = await userManager.FindByIdAsync(vm.Id);
        if (user == null) return (false, "User was not found.");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, vm.NewPassword);
        return (result.Succeeded, string.Join(" ", result.Errors.Select(x => x.Description)));
    }
}
