namespace PdfDataComparison.Admin.Domain.Entities;

public class ComparisonSubmission
{
    public int Id { get; set; }
    public int ComparisonJobId { get; set; }
    public ComparisonJob? ComparisonJob { get; set; }
    public string FinalJson { get; set; } = string.Empty;
    public string SubmittedByUserId { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string ExcelFilePath { get; set; } = string.Empty;
}
