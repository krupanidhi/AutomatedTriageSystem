namespace ExcelAnalysis.Core.Models;

/// <summary>
/// Represents a single row from the Excel file
/// </summary>
public class ExcelRow
{
    public int Id { get; set; }
    public int ExcelFileInfoId { get; set; }
    public int RowNumber { get; set; }
    public string SheetName { get; set; } = string.Empty; // Which sheet this row came from
    public string DataJson { get; set; } = string.Empty; // Store row data as JSON
    
    // Navigation
    public ExcelFileInfo ExcelFileInfo { get; set; } = null!;
}
