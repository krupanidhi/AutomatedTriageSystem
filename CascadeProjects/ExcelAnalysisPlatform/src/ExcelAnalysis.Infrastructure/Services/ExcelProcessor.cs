using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Core.Models;
using OfficeOpenXml;
using System.Text.Json;

namespace ExcelAnalysis.Infrastructure.Services;

/// <summary>
/// Excel file processing implementation using EPPlus
/// </summary>
public class ExcelProcessor : IExcelProcessor
{
    public ExcelProcessor()
    {
        // Set EPPlus license context (NonCommercial or Commercial)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<ExcelFileInfo> ProcessFileAsync(Stream fileStream, string fileName)
    {
        using var package = new ExcelPackage(fileStream);
        
        var fileInfo = new ExcelFileInfo
        {
            FileName = fileName,
            UploadedAt = DateTime.UtcNow,
            FileSizeBytes = fileStream.Length,
            SheetNames = package.Workbook.Worksheets.Select(ws => ws.Name).ToList()
        };

        var allRows = new List<Core.Models.ExcelRow>();
        var allColumnNames = new List<string>();
        int totalRowCount = 0;
        int maxColumnCount = 0;

        // Process ALL sheets
        foreach (var worksheet in package.Workbook.Worksheets)
        {
            if (worksheet.Dimension == null)
                continue; // Skip empty sheets

            var start = worksheet.Dimension.Start;
            var end = worksheet.Dimension.End;
            
            totalRowCount += (end.Row - start.Row); // Don't count header
            maxColumnCount = Math.Max(maxColumnCount, end.Column - start.Column + 1);

            // Extract column names from first row of this sheet
            var sheetColumnNames = new List<string>();
            for (int col = start.Column; col <= end.Column; col++)
            {
                var headerValue = worksheet.Cells[start.Row, col].Text;
                var columnName = string.IsNullOrWhiteSpace(headerValue) ? $"Column{col}" : headerValue;
                sheetColumnNames.Add(columnName);
                
                // Add to global column list if not already present
                if (!allColumnNames.Contains(columnName))
                    allColumnNames.Add(columnName);
            }

            // Extract all rows from this sheet
            for (int row = start.Row + 1; row <= end.Row; row++) // Skip header row
            {
                var rowData = new Dictionary<string, object>();
                
                // Add sheet name to row data
                rowData["_SheetName"] = worksheet.Name;
                
                for (int col = start.Column; col <= end.Column; col++)
                {
                    var cellValue = worksheet.Cells[row, col].Value;
                    var columnName = sheetColumnNames[col - start.Column];
                    rowData[columnName] = cellValue ?? string.Empty;
                }

                allRows.Add(new Core.Models.ExcelRow
                {
                    RowNumber = row,
                    SheetName = worksheet.Name,
                    DataJson = JsonSerializer.Serialize(rowData)
                });
            }
        }
        
        fileInfo.TotalRows = totalRowCount;
        fileInfo.TotalColumns = maxColumnCount;
        fileInfo.ColumnNames = allColumnNames;
        fileInfo.Rows = allRows;
        
        // Generate file hash for deduplication
        fileStream.Position = 0;
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = await md5.ComputeHashAsync(fileStream);
        fileInfo.FileHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        return fileInfo;
    }

    public async Task<(List<CommentData> comments, List<QuestionData> questions)> ExtractCommentsAndQuestionsAsync(ExcelFileInfo fileInfo)
    {
        var comments = new List<CommentData>();
        var questions = new List<QuestionData>();

        // Skip metadata sheets that don't contain actual project data
        var metadataSheets = new[] { "Search Parameters", "Instructions", "Metadata", "Template" };

        await Task.Run(() =>
        {
            foreach (var row in fileInfo.Rows)
            {
                // Skip rows from metadata sheets
                if (metadataSheets.Any(ms => row.SheetName.Contains(ms, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var rowData = JsonSerializer.Deserialize<Dictionary<string, object>>(row.DataJson);
                if (rowData == null) continue;

                // Skip header rows (rows that contain mostly field names)
                if (IsHeaderRow(rowData))
                    continue;

                foreach (var kvp in rowData)
                {
                    var fieldName = kvp.Key;
                    var value = kvp.Value?.ToString() ?? string.Empty;
                    
                    // Skip internal fields
                    if (fieldName.StartsWith("_"))
                        continue;
                    
                    // Check if it's a yes/no question
                    if (IsYesNoAnswer(value))
                    {
                        questions.Add(new QuestionData(
                            fieldName,
                            value,
                            row.RowNumber,
                            rowData.ToDictionary(k => k.Key, k => k.Value ?? string.Empty)
                        ));
                    }
                    // Check if it's a meaningful comment (longer text with actual content)
                    else if (IsMeaningfulComment(fieldName, value))
                    {
                        comments.Add(new CommentData(
                            fieldName,
                            value,
                            row.RowNumber,
                            rowData.ToDictionary(k => k.Key, k => k.Value ?? string.Empty)
                        ));
                    }
                }
            }
        });

        return (comments, questions);
    }

    private bool IsHeaderRow(Dictionary<string, object> rowData)
    {
        // If row contains mostly field names or column headers, skip it
        var values = rowData.Values.Select(v => v?.ToString() ?? "").ToList();
        var fieldNamePatterns = new[] { "Column", "Field", "Status", "Comments", "Question", "Narrative" };
        
        var matchCount = values.Count(v => 
            fieldNamePatterns.Any(p => v.Contains(p, StringComparison.OrdinalIgnoreCase)) &&
            v.Split(',').Length > 5  // Multiple comma-separated field names
        );

        return matchCount > values.Count / 2;
    }

    private bool IsMeaningfulComment(string fieldName, string value)
    {
        // Must be substantial text
        if (value.Length < 30 || string.IsNullOrWhiteSpace(value))
            return false;

        // Focus on comment fields, status updates, and narratives
        var commentFieldPatterns = new[] 
        {
            "comment", "comments", "narrative", "description", "notes",
            "issue", "barrier", "challenge", "concern", "risk",
            "status", "progress", "update", "review"
        };

        var isCommentField = commentFieldPatterns.Any(p => 
            fieldName.Contains(p, StringComparison.OrdinalIgnoreCase));

        // Or contains meaningful sentences (not just field names or codes)
        var hasMeaningfulContent = value.Split(' ').Length > 5 && 
                                   value.Any(char.IsLower); // Has actual text, not just codes

        return isCommentField || hasMeaningfulContent;
    }

    private bool IsYesNoAnswer(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized is "yes" or "no" or "y" or "n" or "true" or "false" or "1" or "0";
    }
}
