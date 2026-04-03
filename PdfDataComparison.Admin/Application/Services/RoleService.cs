using PdfDataComparison.Admin.Application.Interfaces;
using PdfDataComparison.Admin.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PdfDataComparison.Admin.Application.Services;

public class RoleService(RoleManager<ApplicationRole> roleManager) : IRoleService
{
    public Task<List<ApplicationRole>> GetRolesAsync() => roleManager.Roles.OrderBy(x => x.Name).ToListAsync();
}
