using PdfDataComparison.Admin.Application.Interfaces;
using PdfDataComparison.Admin.Data;
using PdfDataComparison.Admin.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace PdfDataComparison.Admin.Infrastructure.Services;

public class AuditService(ApplicationDbContext dbContext, IHttpContextAccessor accessor) : IAuditService
{
    public async Task LogAsync(string action, string targetEntity, string notes, string? performedByUserId = null, string? performedByName = null)
    {
        var ip = accessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "N/A";
        dbContext.AuditLogs.Add(new AuditLog
        {
            Action = action,
            TargetEntity = targetEntity,
            Notes = notes,
            PerformedByUserId = performedByUserId ?? "system",
            PerformedByName = performedByName,
            IpAddress = ip,
            Timestamp = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }
}
