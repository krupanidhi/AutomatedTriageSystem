namespace ExcelAnalysis.Core.Interfaces;

/// <summary>
/// Service for generating HTML reports from analysis results
/// </summary>
public interface IReportGenerator
{
    /// <summary>
    /// Generate HTML report for comparison analysis
    /// </summary>
    string GenerateComparisonReport(Models.ComparisonAnalysisResult result);
    
    /// <summary>
    /// Generate HTML report for realistic analysis
    /// </summary>
    string GenerateRealisticReport(Models.EnhancedAnalysisResult result);
    
    /// <summary>
    /// Generate HTML report for basic analysis
    /// </summary>
    string GenerateBasicReport(Models.AnalysisResult result);
}
