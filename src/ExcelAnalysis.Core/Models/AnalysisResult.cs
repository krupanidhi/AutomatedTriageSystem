namespace ExcelAnalysis.Core.Models;

/// <summary>
/// Represents the AI analysis results for an Excel file
/// </summary>
public class AnalysisResult
{
    public int Id { get; set; }
    public int ExcelFileInfoId { get; set; }
    public DateTime AnalyzedAt { get; set; }
    
    // Overall metrics
    public double CompletionPercentage { get; set; }
    public int TotalDeliverables { get; set; }
    public int CompletedDeliverables { get; set; }
    public int InProgressDeliverables { get; set; }
    public int NotStartedDeliverables { get; set; }
    
    // Risk assessment
    public int HighRiskCount { get; set; }
    public int MediumRiskCount { get; set; }
    public int LowRiskCount { get; set; }
    public string RiskSummary { get; set; } = string.Empty;
    
    // Sentiment analysis
    public double OverallSentimentScore { get; set; } // -1 to 1
    public string SentimentSummary { get; set; } = string.Empty;
    
    // Issues and blockers
    public List<string> IdentifiedIssues { get; set; } = new();
    public List<string> Blockers { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    
    // Detailed insights
    public string ExecutiveSummary { get; set; } = string.Empty;
    public string DetailedAnalysisJson { get; set; } = string.Empty;
    
    // Analysis type and metadata
    public string AnalysisType { get; set; } = string.Empty; // "realistic", "comparison", "basic"
    public string RawResultJson { get; set; } = string.Empty; // Full JSON result for report generation
    
    // Navigation
    public ExcelFileInfo ExcelFileInfo { get; set; } = null!;
    public List<RiskItem> RiskItems { get; set; } = new();
    public List<ProgressMetric> ProgressMetrics { get; set; } = new();
}
