namespace ExcelAnalysis.Core.Models;

/// <summary>
/// Comparison between keyword-based and AI-based sentiment analysis
/// </summary>
public class SentimentComparison
{
    public string Organization { get; set; } = string.Empty;
    public double KeywordSentiment { get; set; }
    public double AISentiment { get; set; }
    public double Difference { get; set; }
    public string Analysis { get; set; } = string.Empty;
}

public class MethodologyComparison
{
    public string Method { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double AverageSentiment { get; set; }
    public int PositiveCount { get; set; }
    public int NeutralCount { get; set; }
    public int NegativeCount { get; set; }
    public double ProcessingTimeSeconds { get; set; }
    public int ApiCallsUsed { get; set; }
    public int TokensUsed { get; set; }
    public double EstimatedCost { get; set; }
}

public class ComparisonAnalysisResult
{
    public DateTime AnalyzedAt { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int TotalOrganizations { get; set; }
    public int TotalComments { get; set; }
    
    // Keyword-based results
    public EnhancedAnalysisResult KeywordBasedAnalysis { get; set; } = new();
    public MethodologyComparison KeywordMethodology { get; set; } = new();
    
    // AI-based results
    public EnhancedAnalysisResult AIBasedAnalysis { get; set; } = new();
    public MethodologyComparison AIMethodology { get; set; } = new();
    
    // Comparison insights
    public List<SentimentComparison> OrganizationComparisons { get; set; } = new();
    public double AverageSentimentDifference { get; set; }
    public string ComparisonSummary { get; set; } = string.Empty;
    public List<string> KeyFindings { get; set; } = new();
    
    // Recommendations
    public string RecommendedApproach { get; set; } = string.Empty;
    public List<string> UseCasesForKeyword { get; set; } = new();
    public List<string> UseCasesForAI { get; set; } = new();
}
