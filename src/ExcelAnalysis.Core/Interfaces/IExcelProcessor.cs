using ExcelAnalysis.Core.Models;

namespace ExcelAnalysis.Core.Interfaces;

/// <summary>
/// Service for processing Excel files
/// </summary>
public interface IExcelProcessor
{
    /// <summary>
    /// Process an uploaded Excel file and extract data
    /// </summary>
    Task<ExcelFileInfo> ProcessFileAsync(Stream fileStream, string fileName);
    
    /// <summary>
    /// Extract comments and yes/no questions from Excel data
    /// </summary>
    Task<(List<CommentData> comments, List<QuestionData> questions)> ExtractCommentsAndQuestionsAsync(ExcelFileInfo fileInfo);
}

public record CommentData(string Field, string Comment, int RowNumber, Dictionary<string, object> RowData);
public record QuestionData(string Question, string Answer, int RowNumber, Dictionary<string, object> RowData);
