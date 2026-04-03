using PdfDataComparison.Admin.Application.Authorization;
using PdfDataComparison.Admin.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace PdfDataComparison.Admin.Infrastructure.Authorization;

public class PermissionAuthorizationHandler(IPermissionService permissionService)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var granted = await permissionService.HasPermissionAsync(userId, requirement.Permission);
        if (granted)
        {
            context.Succeed(requirement);
        }
    }
}
