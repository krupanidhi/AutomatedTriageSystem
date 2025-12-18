using ExcelAnalysis.Core.Models;

namespace ExcelAnalysis.Core.Interfaces;

/// <summary>
/// Service for AI-powered analysis of Excel data
/// </summary>
public interface IAIAnalyzer
{
    /// <summary>
    /// Analyze Excel file data and generate insights
    /// </summary>
    Task<AnalysisResult> AnalyzeAsync(ExcelFileInfo fileInfo);
    
    /// <summary>
    /// Classify risk level from comment text
    /// </summary>
    Task<RiskLevel> ClassifyRiskAsync(string commentText);
    
    /// <summary>
    /// Analyze sentiment of comments
    /// </summary>
    Task<double> AnalyzeSentimentAsync(string text);
    
    /// <summary>
    /// Extract issues and blockers from comments
    /// </summary>
    Task<List<string>> ExtractIssuesAsync(List<string> comments);
    
    /// <summary>
    /// Generate executive summary
    /// </summary>
    Task<string> GenerateSummaryAsync(AnalysisResult analysisResult);
}
