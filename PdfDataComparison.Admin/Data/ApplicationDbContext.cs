using PdfDataComparison.Admin.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PdfDataComparison.Admin.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<PagePermission> PagePermissions => Set<PagePermission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<ComparisonJob> ComparisonJobs => Set<ComparisonJob>();
    public DbSet<ComparisonField> ComparisonFields => Set<ComparisonField>();
    public DbSet<ComparisonSubmission> ComparisonSubmissions => Set<ComparisonSubmission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RolePermission>()
            .HasIndex(x => new { x.RoleId, x.PagePermissionId })
            .IsUnique();

        builder.Entity<ComparisonField>()
            .Property(x => x.IsMatch)
            .HasDefaultValue(false);
    }
}
