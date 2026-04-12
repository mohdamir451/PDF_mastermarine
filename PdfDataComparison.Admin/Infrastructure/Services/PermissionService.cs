using PdfDataComparison.Admin.Application.Interfaces;
using PdfDataComparison.Admin.Data;
using PdfDataComparison.Admin.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PdfDataComparison.Admin.Infrastructure.Services;

public class PermissionService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : IPermissionService
{
    public async Task<bool> HasPermissionAsync(string userId, string permissionKey)
    {
        var permissions = await GetUserPermissionKeysAsync(userId);
        return permissions.Contains(permissionKey);
    }

    public async Task<IReadOnlyCollection<string>> GetUserPermissionKeysAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return Array.Empty<string>();

        // Keep the full query server-side with joins so it works across older
        // SQL Server compatibility levels (avoids OPENJSON translation).
        return await (
            from ur in dbContext.UserRoles
            join rp in dbContext.RolePermissions on ur.RoleId equals rp.RoleId
            join pp in dbContext.PagePermissions on rp.PagePermissionId equals pp.Id
            where ur.UserId == userId && rp.CanView
            select pp.PermissionKey
        )
            .Distinct()
            .ToListAsync();
    }

    public Task<List<RolePermission>> GetRolePermissionsAsync(string roleId)
        => dbContext.RolePermissions.Include(x => x.PagePermission).Where(x => x.RoleId == roleId).ToListAsync();

    public async Task UpsertRolePermissionsAsync(string roleId, IEnumerable<RolePermission> permissions)
    {
        var existing = dbContext.RolePermissions.Where(x => x.RoleId == roleId);
        dbContext.RolePermissions.RemoveRange(existing);
        await dbContext.RolePermissions.AddRangeAsync(permissions);
        await dbContext.SaveChangesAsync();
    }
}
