using Microsoft.AspNetCore.Identity;

namespace PdfDataComparison.Admin.Domain.Entities;

public class ApplicationRole : IdentityRole
{
    public string Description { get; set; } = string.Empty;
}
