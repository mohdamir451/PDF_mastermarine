namespace PdfDataComparison.Admin.Domain.Entities;

public class RolePermission
{
    public int Id { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public ApplicationRole? Role { get; set; }
    public int PagePermissionId { get; set; }
    public PagePermission? PagePermission { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanExport { get; set; }
}
