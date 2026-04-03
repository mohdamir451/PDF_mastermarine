using System.Security.Claims;
using PdfDataComparison.Admin.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;

namespace PdfDataComparison.Admin.Infrastructure.Authorization;

public class PermissionClaimsTransformation(IPermissionService permissionService) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true) return principal;
        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null) return principal;

        var userId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return principal;
        if (identity.HasClaim(c => c.Type == "permission")) return principal;

        var permissions = await permissionService.GetUserPermissionKeysAsync(userId);
        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim("permission", permission));
        }

        return principal;
    }
}
