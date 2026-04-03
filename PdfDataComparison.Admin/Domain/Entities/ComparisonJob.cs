namespace PdfDataComparison.Admin.Domain.Entities;

public class ComparisonJob
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PdfFilePath { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
    public ICollection<ComparisonField> Fields { get; set; } = new List<ComparisonField>();
}
