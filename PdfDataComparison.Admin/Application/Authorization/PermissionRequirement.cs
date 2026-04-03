using Microsoft.AspNetCore.Authorization;

namespace PdfDataComparison.Admin.Application.Authorization;

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
