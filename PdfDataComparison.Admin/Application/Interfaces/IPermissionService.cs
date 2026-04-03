using PdfDataComparison.Admin.Domain.Entities;

namespace PdfDataComparison.Admin.Application.Interfaces;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(string userId, string permissionKey);
    Task<IReadOnlyCollection<string>> GetUserPermissionKeysAsync(string userId);
    Task<List<RolePermission>> GetRolePermissionsAsync(string roleId);
    Task UpsertRolePermissionsAsync(string roleId, IEnumerable<RolePermission> permissions);
}
