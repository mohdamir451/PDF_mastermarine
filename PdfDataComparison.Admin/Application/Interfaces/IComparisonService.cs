using PdfDataComparison.Admin.Application.ViewModels;

namespace PdfDataComparison.Admin.Application.Interfaces;

public interface IComparisonService
{
    Task<PagedResult<ComparisonJobListItemVm>> GetJobsAsync(string? search, int page, int pageSize);
    Task<ComparisonScreenVm?> GetJobScreenAsync(int id);
    Task<int> SubmitAsync(ComparisonSubmitVm model, string userId);
}
