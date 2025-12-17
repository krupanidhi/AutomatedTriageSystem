namespace ExcelAnalysis.Core.Models;

/// <summary>
/// Represents metadata and content of an uploaded Excel file
/// </summary>
public class ExcelFileInfo
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public long FileSizeBytes { get; set; }
    public int TotalRows { get; set; }
    public int TotalColumns { get; set; }
    public List<string> SheetNames { get; set; } = new();
    public List<string> ColumnNames { get; set; } = new();
    public string FileHash { get; set; } = string.Empty;
    
    // Navigation properties
    public List<ExcelRow> Rows { get; set; } = new();
    public AnalysisResult? AnalysisResult { get; set; }
}
