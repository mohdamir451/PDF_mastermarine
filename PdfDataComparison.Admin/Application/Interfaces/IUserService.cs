using PdfDataComparison.Admin.Application.ViewModels;

namespace PdfDataComparison.Admin.Application.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserListItemVm>> GetUsersAsync(string? search, int page, int pageSize);
}
