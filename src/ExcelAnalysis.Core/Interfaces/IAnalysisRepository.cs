using ExcelAnalysis.Core.Models;

namespace ExcelAnalysis.Core.Interfaces;

/// <summary>
/// Repository for analysis data persistence
/// </summary>
public interface IAnalysisRepository
{
    Task<ExcelFileInfo?> GetFileInfoAsync(int id);
    Task<List<ExcelFileInfo>> GetAllFilesAsync();
    Task<int> SaveFileInfoAsync(ExcelFileInfo fileInfo);
    Task DeleteFileAsync(int id);
    
    Task<AnalysisResult?> GetAnalysisResultAsync(int fileId);
    Task<List<AnalysisResult>> GetAllAnalysisResultsAsync();
    Task<int> SaveAnalysisResultAsync(AnalysisResult analysisResult);
    Task DeleteAnalysisResultAsync(int id);
}
