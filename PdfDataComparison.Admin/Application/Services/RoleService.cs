using PdfDataComparison.Admin.Application.Interfaces;
using PdfDataComparison.Admin.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PdfDataComparison.Admin.Application.Services;

public class RoleService(RoleManager<ApplicationRole> roleManager) : IRoleService
{
    public Task<List<ApplicationRole>> GetRolesAsync() => roleManager.Roles.OrderBy(x => x.Name).ToListAsync();

    public Task<ApplicationRole?> GetRoleAsync(string id) => roleManager.FindByIdAsync(id);

    public async Task<(bool Succeeded, string Error)> CreateRoleAsync(string name, string description)
    {
        var role = new ApplicationRole
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty
        };

        var result = await roleManager.CreateAsync(role);
        return (result.Succeeded, string.Join(" ", result.Errors.Select(x => x.Description)));
    }

    public async Task<(bool Succeeded, string Error)> UpdateRoleAsync(string id, string name, string description)
    {
        var role = await roleManager.FindByIdAsync(id);
        if (role == null) return (false, "Role was not found.");

        role.Name = name.Trim();
        role.Description = description?.Trim() ?? string.Empty;

        var result = await roleManager.UpdateAsync(role);
        return (result.Succeeded, string.Join(" ", result.Errors.Select(x => x.Description)));
    }
}
