namespace PdfDataComparison.Admin.Application.ViewModels;

public class DownloadDiagnosticLogsVm
{
    public List<DownloadDiagnosticLogEntryVm> Entries { get; set; } = new();
}

public class DownloadDiagnosticLogEntryVm
{
    public DateTime TimestampUtc { get; set; }
    public string Level { get; set; } = string.Empty;
    public string ErrorReference { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string SelectedIds { get; set; } = string.Empty;
    public string SubmissionIds { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Exception { get; set; } = string.Empty;
}
