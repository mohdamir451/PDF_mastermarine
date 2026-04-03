namespace PdfDataComparison.Admin.Domain.Entities;

public class ComparisonField
{
    public int Id { get; set; }
    public int ComparisonJobId { get; set; }
    public ComparisonJob? ComparisonJob { get; set; }
    public string FieldLabel { get; set; } = string.Empty;
    public string FieldType { get; set; } = "Text";
    public string ExpectedValue { get; set; } = string.Empty;
    public string? ActualValue { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool IsBlocking { get; set; } = true;
    public bool IsMatch { get; set; }
    public string? MismatchReason { get; set; }
}
