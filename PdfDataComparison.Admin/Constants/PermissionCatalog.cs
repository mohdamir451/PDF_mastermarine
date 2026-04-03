namespace PdfDataComparison.Admin.Constants;

public static class PermissionCatalog
{
    public const string DashboardView = "Permissions.Dashboard.View";
    public const string UsersManage = "Permissions.Users.Manage";
    public const string RolesManage = "Permissions.Roles.Manage";
    public const string PermissionsManage = "Permissions.Permissions.Manage";
    public const string ComparisonJobsView = "Permissions.ComparisonJobs.View";
    public const string ComparisonEdit = "Permissions.Comparison.Edit";
    public const string ComparisonSubmit = "Permissions.Comparison.Submit";
    public const string ReportsView = "Permissions.Reports.View";
    public const string ReportsExport = "Permissions.Reports.Export";
    public const string AuditLogsView = "Permissions.AuditLogs.View";
    public const string SettingsManage = "Permissions.Settings.Manage";

    public static readonly string[] AllPolicies =
    {
        DashboardView, UsersManage, RolesManage, PermissionsManage,
        ComparisonJobsView, ComparisonEdit, ComparisonSubmit,
        ReportsView, ReportsExport, AuditLogsView, SettingsManage
    };
}
