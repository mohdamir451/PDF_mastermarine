namespace PdfDataComparison.Admin.Domain.Entities;

public class PagePermission
{
    public int Id { get; set; }
    public string PageName { get; set; } = string.Empty;
    public string PermissionKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
