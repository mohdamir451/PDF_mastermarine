namespace PdfDataComparison.Admin.Domain.Entities;

public class PdfComparisonSubmission
{
    public int Id { get; set; }
    public string BillOfLadingNumber { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public string SubmittedByUserId { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
