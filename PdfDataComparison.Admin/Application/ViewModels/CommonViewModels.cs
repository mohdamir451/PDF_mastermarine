using System.ComponentModel.DataAnnotations;

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

public class UserCreateVm
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, ErrorMessage = "Full name cannot exceed 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required.")]
    [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters.")]
    public string Department { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Password and confirm password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Default role is required.")]
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<string> AvailableRoles { get; set; } = new();
}

public class UserEditVm
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, ErrorMessage = "Full name cannot exceed 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required.")]
    [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters.")]
    public string Department { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<string> AvailableRoles { get; set; } = new();
}

public class UserResetPasswordVm
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Password and confirm password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ProfileChangePasswordVm
{
    [Required(ErrorMessage = "Current password is required.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "New password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "New password and confirm password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class DashboardVm
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers => Math.Max(0, TotalUsers - ActiveUsers);
    public int TotalRoles { get; set; }
    public int TotalComparisonJobs { get; set; }
    public int PendingMismatches { get; set; }
    public int CompletedSubmissions { get; set; }
    public int JobSubmissions { get; set; }
    public int ActivePdfSubmissions { get; set; }
    public int ReplacedPdfSubmissions { get; set; }
    public int CompletePdfSubmissions { get; set; }
    public int PdfSubmissionsNeedingReview { get; set; }
    public int TotalPdfSubmissions => ActivePdfSubmissions + ReplacedPdfSubmissions;
    public int TotalComparisonFields { get; set; }
    public int MatchedComparisonFields { get; set; }
    public int BlockingComparisonFields { get; set; }
    public DateTime? LastPdfSubmittedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public List<DashboardStatusMetricVm> JobStatusBreakdown { get; set; } = new();
    public List<DashboardDailyMetricVm> PdfSubmissionTrend { get; set; } = new();
    public List<DashboardRecentPdfVm> RecentPdfSubmissions { get; set; } = new();
    public List<DashboardAuditActivityVm> RecentActivity { get; set; } = new();
}

public class DashboardStatusMetricVm
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DashboardDailyMetricVm
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class DashboardRecentPdfVm
{
    public int Id { get; set; }
    public string BillOfLadingNumber { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public string SubmittedByUserId { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public bool IsActive { get; set; }
    public int FieldCount { get; set; }
    public int IssueCount { get; set; }
    public double ValidationPercent { get; set; }
}

public class DashboardAuditActivityVm
{
    public string Action { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string PerformedByName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
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

    [Required(ErrorMessage = "Role name is required.")]
    [StringLength(100, ErrorMessage = "Role name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "Description cannot exceed 250 characters.")]
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
    [Range(1, int.MaxValue, ErrorMessage = "A comparison job is required.")]
    public int JobId { get; set; }

    [MinLength(1, ErrorMessage = "At least one comparison field is required.")]
    public List<ComparisonFieldInputVm> Fields { get; set; } = new();
}

public class ComparisonFieldInputVm
{
    [Range(1, int.MaxValue, ErrorMessage = "A comparison field is required.")]
    public int Id { get; set; }
    public string? ActualValue { get; set; }
}

public class LoginVm
{
    [Required(ErrorMessage = "Username is required.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}
