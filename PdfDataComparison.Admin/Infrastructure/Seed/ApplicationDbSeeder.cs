using PdfDataComparison.Admin.Constants;
using PdfDataComparison.Admin.Data;
using PdfDataComparison.Admin.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PdfDataComparison.Admin.Infrastructure.Seed;

public static class ApplicationDbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var hasMigrations = dbContext.Database.GetMigrations().Any();
        if (hasMigrations)
        {
            await dbContext.Database.MigrateAsync();
        }
        else
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var roles = new[] { "SuperAdmin", "Admin", "Manager", "Reviewer", "DataEntry", "ReadOnly" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role, Description = $"{role} default role" });
            }
        }

        var admin = await userManager.FindByNameAsync("superadmin");
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = "superadmin",
                Email = "superadmin@pdfcompare.local",
                FullName = "System Super Admin",
                Department = "IT",
                IsActive = true
            };
            await userManager.CreateAsync(admin, "SuperAdmin!23");
            await userManager.AddToRoleAsync(admin, "SuperAdmin");
        }

        if (!await dbContext.PagePermissions.AnyAsync())
        {
            var pages = new[]
            {
                new PagePermission{PageName = "Dashboard", PermissionKey = PermissionCatalog.DashboardView, DisplayName = "Dashboard"},
                new PagePermission{PageName = "Users", PermissionKey = PermissionCatalog.UsersManage, DisplayName = "User Management"},
                new PagePermission{PageName = "Roles", PermissionKey = PermissionCatalog.RolesManage, DisplayName = "Role Management"},
                new PagePermission{PageName = "Permissions", PermissionKey = PermissionCatalog.PermissionsManage, DisplayName = "Page Permissions"},
                new PagePermission{PageName = "ComparisonJobs", PermissionKey = PermissionCatalog.ComparisonJobsView, DisplayName = "Comparison Jobs"},
                new PagePermission{PageName = "Comparison", PermissionKey = PermissionCatalog.ComparisonEdit, DisplayName = "Comparison Edit"},
                new PagePermission{PageName = "Comparison", PermissionKey = PermissionCatalog.ComparisonSubmit, DisplayName = "Comparison Submit"},
                new PagePermission{PageName = "Reports", PermissionKey = PermissionCatalog.ReportsView, DisplayName = "Reports"},
                new PagePermission{PageName = "Reports", PermissionKey = PermissionCatalog.ReportsExport, DisplayName = "Export Excel"},
                new PagePermission{PageName = "Audit", PermissionKey = PermissionCatalog.AuditLogsView, DisplayName = "Audit Logs"},
                new PagePermission{PageName = "Settings", PermissionKey = PermissionCatalog.SettingsManage, DisplayName = "Settings"}
            };
            dbContext.PagePermissions.AddRange(pages);
            await dbContext.SaveChangesAsync();
        }


        if (!await dbContext.RolePermissions.AnyAsync())
        {
            var allPages = await dbContext.PagePermissions.ToListAsync();
            var allRoles = await dbContext.Roles.ToListAsync();

            foreach (var role in allRoles)
            {
                foreach (var page in allPages)
                {
                    var full = role.Name == "SuperAdmin";
                    var canView = full || role.Name is "Admin" ||
                                  (role.Name == "Manager" && page.PermissionKey is PermissionCatalog.DashboardView or PermissionCatalog.ReportsView or PermissionCatalog.ComparisonJobsView) ||
                                  (role.Name == "Reviewer" && page.PermissionKey is PermissionCatalog.ComparisonJobsView or PermissionCatalog.ComparisonEdit or PermissionCatalog.ComparisonSubmit or PermissionCatalog.ReportsView or PermissionCatalog.ReportsExport) ||
                                  (role.Name == "DataEntry" && page.PermissionKey is PermissionCatalog.ComparisonJobsView or PermissionCatalog.ComparisonEdit) ||
                                  (role.Name == "ReadOnly" && page.PermissionKey is PermissionCatalog.DashboardView or PermissionCatalog.ReportsView);

                    if (!canView) continue;

                    dbContext.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PagePermissionId = page.Id,
                        CanView = true,
                        CanCreate = full || role.Name is "Admin" or "DataEntry",
                        CanEdit = full || role.Name is "Admin" or "DataEntry" or "Reviewer",
                        CanDelete = full || role.Name == "Admin",
                        CanSubmit = full || role.Name == "Reviewer",
                        CanExport = full || role.Name is "Reviewer" or "Manager" or "ReadOnly"
                    });
                }
            }
        }

                if (!await dbContext.ComparisonJobs.AnyAsync())
        {
            var job = new ComparisonJob
            {
                JobNumber = "CMP-1001",
                Title = "Marine Policy Schedule Validation",
                PdfFilePath = "/sample/sample-pdf.pdf",
                Status = "Pending",
                CreatedByUserId = admin.Id
            };
            job.Fields.Add(new ComparisonField { FieldLabel = "Policy Number", FieldType = "Text", ExpectedValue = "MMR-55392", ActualValue = "MMR-55392", IsMatch = true });
            job.Fields.Add(new ComparisonField { FieldLabel = "Premium", FieldType = "Number", ExpectedValue = "4250", ActualValue = "4200", IsMatch = false, IsBlocking = true });
            job.Fields.Add(new ComparisonField { FieldLabel = "Effective Date", FieldType = "Date", ExpectedValue = "2026-02-10", ActualValue = "2026-02-10", IsMatch = true });
            dbContext.ComparisonJobs.Add(job);
        }

        if (!await dbContext.AuditLogs.AnyAsync())
        {
            dbContext.AuditLogs.Add(new AuditLog { Action = "Seed", TargetEntity = "System", Notes = "Database seeded", PerformedByUserId = "system", PerformedByName = "System" });
        }

        await dbContext.SaveChangesAsync();
    }
}
