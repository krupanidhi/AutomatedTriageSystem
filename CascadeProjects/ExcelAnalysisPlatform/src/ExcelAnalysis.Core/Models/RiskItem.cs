namespace ExcelAnalysis.Core.Models;

/// <summary>
/// Represents an identified risk from the analysis
/// </summary>
public class RiskItem
{
    public int Id { get; set; }
    public int AnalysisResultId { get; set; }
    public string Deliverable { get; set; } = string.Empty;
    public RiskLevel Level { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // Which comment/field triggered this
    public string SheetName { get; set; } = string.Empty; // Which sheet this risk came from
    public int RowNumber { get; set; } // Excel row number for traceability
    public string FieldName { get; set; } = string.Empty; // Specific field/column name
    public string Mitigation { get; set; } = string.Empty;
    public DateTime IdentifiedAt { get; set; }
    
    // Navigation
    public AnalysisResult AnalysisResult { get; set; } = null!;
}

public enum RiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
