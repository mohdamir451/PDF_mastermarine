using PdfDataComparison.Admin.Domain.Entities;

namespace PdfDataComparison.Admin.Application.Interfaces;

public interface IRoleService
{
    Task<List<ApplicationRole>> GetRolesAsync();
}
