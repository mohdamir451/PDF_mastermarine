namespace PdfDataComparison.Admin.Application.ViewModels;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class UserListItemVm
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class DashboardVm
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalRoles { get; set; }
    public int TotalComparisonJobs { get; set; }
    public int PendingMismatches { get; set; }
    public int CompletedSubmissions { get; set; }
    public List<string> RecentActivity { get; set; } = new();
}

public class ComparisonJobListItemVm
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ComparisonJobsIndexVm
{
    public PagedResult<ComparisonJobListItemVm> Jobs { get; set; } = new();
    public List<PdfComparisonSubmissionListItemVm> PdfSubmissions { get; set; } = new();
}

public class PdfComparisonSubmissionListItemVm
{
    public int Id { get; set; }
    public string BillOfLadingNumber { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public string SubmittedByUserId { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public bool IsActive { get; set; }
    public int TotalFields { get; set; }
    public int IssueCount { get; set; }
    public string ValidationStatus => IssueCount == 0 ? "Complete" : "Needs Review";
}

public class ReportHistoryItemVm
{
    public int Id { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public string SubmittedByUserId { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public bool HasDetailsPage { get; set; }
}

public class RoleFormVm
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class RolePermissionInputVm
{
    public int PagePermissionId { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanExport { get; set; }
}

public class ComparisonScreenVm
{
    public int JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string PdfUrl { get; set; } = string.Empty;
    public bool CanEdit { get; set; }
    public bool CanSubmit { get; set; }
    public List<ComparisonFieldVm> Fields { get; set; } = new();
}

public class ComparisonFieldVm
{
    public int Id { get; set; }
    public string FieldLabel { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
    public string? ActualValue { get; set; }
    public bool IsRequired { get; set; }
    public bool IsBlocking { get; set; }
    public bool IsMatch { get; set; }
    public string? MismatchReason { get; set; }
}

public class ComparisonSubmitVm
{
    public int JobId { get; set; }
    public List<ComparisonFieldInputVm> Fields { get; set; } = new();
}

public class ComparisonFieldInputVm
{
    public int Id { get; set; }
    public string? ActualValue { get; set; }
}

public class LoginVm
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}
