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
        var rolePermissions = await (
            from ur in dbContext.UserRoles
            join rp in dbContext.RolePermissions on ur.RoleId equals rp.RoleId
            join pp in dbContext.PagePermissions on rp.PagePermissionId equals pp.Id
            where ur.UserId == userId
            select new
            {
                pp.PermissionKey,
                rp.CanView,
                rp.CanCreate,
                rp.CanEdit,
                rp.CanDelete,
                rp.CanSubmit,
                rp.CanExport
            }
        )
            .AsNoTracking()
            .ToListAsync();

        return rolePermissions
            .Where(x => GrantsPermission(x.PermissionKey, x.CanView, x.CanCreate, x.CanEdit, x.CanDelete, x.CanSubmit, x.CanExport))
            .Select(x => x.PermissionKey)
            .Distinct()
            .ToList();
    }

    public Task<List<RolePermission>> GetRolePermissionsAsync(string roleId)
        => dbContext.RolePermissions.AsNoTracking().Include(x => x.PagePermission).Where(x => x.RoleId == roleId).ToListAsync();

    public async Task UpsertRolePermissionsAsync(string roleId, IEnumerable<RolePermission> permissions)
    {
        var existing = dbContext.RolePermissions.Where(x => x.RoleId == roleId);
        dbContext.RolePermissions.RemoveRange(existing);
        await dbContext.RolePermissions.AddRangeAsync(permissions);
        await dbContext.SaveChangesAsync();
    }

    private static bool GrantsPermission(
        string permissionKey,
        bool canView,
        bool canCreate,
        bool canEdit,
        bool canDelete,
        bool canSubmit,
        bool canExport)
    {
        if (permissionKey.EndsWith(".Submit", StringComparison.OrdinalIgnoreCase)) return canSubmit;
        if (permissionKey.EndsWith(".Export", StringComparison.OrdinalIgnoreCase)) return canExport;
        if (permissionKey.EndsWith(".Edit", StringComparison.OrdinalIgnoreCase)) return canEdit;
        if (permissionKey.EndsWith(".Create", StringComparison.OrdinalIgnoreCase)) return canCreate;
        if (permissionKey.EndsWith(".Delete", StringComparison.OrdinalIgnoreCase)) return canDelete;
        return canView;
    }
}
