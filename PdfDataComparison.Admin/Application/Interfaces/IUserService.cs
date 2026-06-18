using PdfDataComparison.Admin.Application.ViewModels;

namespace PdfDataComparison.Admin.Application.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserListItemVm>> GetUsersAsync(string? search, int page, int pageSize);
    Task<(bool Succeeded, string Error)> CreateUserAsync(UserCreateVm vm);
    Task<UserEditVm?> GetUserForEditAsync(string id);
    Task<(bool Succeeded, string Error)> UpdateUserAsync(UserEditVm vm);
    Task<UserResetPasswordVm?> GetUserForPasswordResetAsync(string id);
    Task<(bool Succeeded, string Error)> ResetPasswordAsync(UserResetPasswordVm vm);
}
