namespace PdfDataComparison.Admin.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, string targetEntity, string notes, string? performedByUserId = null, string? performedByName = null);
}
