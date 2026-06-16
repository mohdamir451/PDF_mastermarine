using PdfDataComparison.Admin.Domain.Entities;

namespace PdfDataComparison.Admin.Application.Interfaces;

public interface IRoleService
{
    Task<List<ApplicationRole>> GetRolesAsync();
    Task<ApplicationRole?> GetRoleAsync(string id);
    Task<(bool Succeeded, string Error)> CreateRoleAsync(string name, string description);
    Task<(bool Succeeded, string Error)> UpdateRoleAsync(string id, string name, string description);
}
