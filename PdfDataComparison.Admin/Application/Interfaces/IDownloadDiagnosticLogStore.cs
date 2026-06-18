using PdfDataComparison.Admin.Application.ViewModels;

namespace PdfDataComparison.Admin.Application.Interfaces;

public interface IDownloadDiagnosticLogStore
{
    Task WriteAsync(DownloadDiagnosticLogEntryVm entry);
    Task<IReadOnlyList<DownloadDiagnosticLogEntryVm>> ReadLatestAsync(int take = 200);
}
