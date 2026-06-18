using PdfDataComparison.Admin.Application.ViewModels;
using PdfDataComparison.Admin.Constants;
using PdfDataComparison.Admin.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PdfDataComparison.Admin.Controllers;

[Authorize(Policy = PermissionCatalog.DashboardView)]
public class DashboardController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var completedJobSubmissions = await dbContext.ComparisonSubmissions.CountAsync();
        var completedPdfSubmissions = await dbContext.PdfComparisonSubmissions.CountAsync(x => x.IsActive);
        var allPdfSubmissions = await dbContext.PdfComparisonSubmissions
            .AsNoTracking()
            .OrderByDescending(x => x.SubmittedAt)
            .ToListAsync();
        var activePdfSubmissions = allPdfSubmissions
            .Where(x => x.IsActive)
            .ToList();
        var today = DateTime.UtcNow.Date;
        var trendStartDate = today.AddDays(-6);

        var vm = new DashboardVm
        {
            TotalUsers = await dbContext.Users.CountAsync(),
            ActiveUsers = await dbContext.Users.CountAsync(x => x.IsActive),
            TotalRoles = await dbContext.Roles.CountAsync(),
            TotalComparisonJobs = await dbContext.ComparisonJobs.CountAsync(),
            PendingMismatches = await dbContext.ComparisonFields.CountAsync(x => !x.IsMatch && x.IsBlocking),
            TotalComparisonFields = await dbContext.ComparisonFields.CountAsync(),
            MatchedComparisonFields = await dbContext.ComparisonFields.CountAsync(x => x.IsMatch),
            BlockingComparisonFields = await dbContext.ComparisonFields.CountAsync(x => x.IsBlocking),
            JobSubmissions = completedJobSubmissions,
            ActivePdfSubmissions = activePdfSubmissions.Count,
            ReplacedPdfSubmissions = allPdfSubmissions.Count(x => !x.IsActive),
            CompletePdfSubmissions = activePdfSubmissions.Count(x =>
            {
                var matches = TryBuildExportRows(x.PayloadJson);
                return matches.Count > 0 && matches.All(isMatch => isMatch);
            }),
            PdfSubmissionsNeedingReview = activePdfSubmissions.Count(x => TryBuildExportRows(x.PayloadJson).Any(isMatch => !isMatch)),
            LastPdfSubmittedAt = allPdfSubmissions.FirstOrDefault()?.SubmittedAt,
            CompletedSubmissions = completedJobSubmissions + completedPdfSubmissions,
            JobStatusBreakdown = await dbContext.ComparisonJobs
                .AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(x => new DashboardStatusMetricVm { Status = x.Key, Count = x.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync(),
            PdfSubmissionTrend = allPdfSubmissions
                .Where(x => x.SubmittedAt.Date >= trendStartDate)
                .GroupBy(x => x.SubmittedAt.Date)
                .Select(x => new DashboardDailyMetricVm { Date = x.Key, Count = x.Count() })
                .OrderBy(x => x.Date)
                .ToList(),
            RecentPdfSubmissions = allPdfSubmissions
                .Take(6)
                .Select(x => new DashboardRecentPdfVm
                {
                    Id = x.Id,
                    BillOfLadingNumber = x.BillOfLadingNumber,
                    SourceFileName = x.SourceFileName,
                    SubmittedByUserId = x.SubmittedByUserId,
                    SubmittedAt = x.SubmittedAt,
                    IsActive = x.IsActive
                })
                .ToList(),
            RecentActivity = await dbContext.AuditLogs
                .AsNoTracking()
                .OrderByDescending(x => x.Timestamp)
                .Take(6)
                .Select(x => new DashboardAuditActivityVm
                {
                    Action = x.Action,
                    TargetEntity = x.TargetEntity,
                    Notes = x.Notes,
                    PerformedByName = x.PerformedByName ?? x.PerformedByUserId,
                    Timestamp = x.Timestamp
                })
                .ToListAsync()
        };
        vm.LastActivityAt = vm.RecentActivity.FirstOrDefault()?.Timestamp;

        for (var date = trendStartDate; date <= today; date = date.AddDays(1))
        {
            if (vm.PdfSubmissionTrend.All(x => x.Date != date))
            {
                vm.PdfSubmissionTrend.Add(new DashboardDailyMetricVm { Date = date, Count = 0 });
            }
        }

        vm.PdfSubmissionTrend = vm.PdfSubmissionTrend.OrderBy(x => x.Date).ToList();
        return View(vm);
    }

    private static List<bool> TryBuildExportRows(string payloadJson)
    {
        try
        {
            var rows = System.Text.Json.JsonSerializer.Deserialize<List<DashboardComparisonRow>>(payloadJson, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return rows?
                .Select(row => IsConfirmedMatch(row.ApiValue, row.PdfValue))
                .ToList() ?? new List<bool>();
        }
        catch (System.Text.Json.JsonException)
        {
            return new List<bool>();
        }
    }

    private static bool IsConfirmedMatch(string? apiValue, string? pdfValue)
    {
        if (string.IsNullOrWhiteSpace(apiValue))
        {
            return !string.IsNullOrWhiteSpace(pdfValue);
        }

        return !string.IsNullOrWhiteSpace(pdfValue) &&
               string.Equals(apiValue.Trim(), pdfValue.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private class DashboardComparisonRow
    {
        public string? ApiValue { get; set; }
        public string? PdfValue { get; set; }
    }
}
