namespace ExcelAnalysis.Core.Models;

/// <summary>
/// Result from Sentence Transformers semantic analysis
/// </summary>
public class SemanticAnalysisResult
{
    public int TotalComments { get; set; }
    public int TotalOrganizations { get; set; }
    public List<SemanticTheme> Themes { get; set; } = new();
    public Dictionary<string, OrganizationSemanticInsight> OrganizationInsights { get; set; } = new();
    public List<SimilarCommentPair> SimilarCommentPairs { get; set; } = new();
    public SemanticSentimentDistribution SentimentDistribution { get; set; } = new();
    public SemanticModelInfo ModelInfo { get; set; } = new();
}

public class SemanticTheme
{
    public int ThemeId { get; set; }
    public string ThemeName { get; set; } = string.Empty;
    public int CommentCount { get; set; }
    public List<string> Keywords { get; set; } = new();
    public string RepresentativeComment { get; set; } = string.Empty;
    public List<string> SampleComments { get; set; } = new();
}

public class OrganizationSemanticInsight
{
    public int CommentCount { get; set; }
    public double CoherenceScore { get; set; }
    public List<string> TopKeywords { get; set; } = new();
}

public class SimilarCommentPair
{
    public string Comment1 { get; set; } = string.Empty;
    public string Comment2 { get; set; } = string.Empty;
    public double Similarity { get; set; }
}

public class SemanticSentimentDistribution
{
    public string Pattern { get; set; } = string.Empty;
    public double Variance { get; set; }
    public string Note { get; set; } = string.Empty;
}

public class SemanticModelInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int EmbeddingDimension { get; set; }
}

/// <summary>
/// Hybrid analysis result combining Claude AI and Semantic analysis
/// </summary>
public class HybridAnalysisResult
{
    public DateTime AnalyzedAt { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int TotalGranteesAnalyzed { get; set; }
    public int TotalResponsesAnalyzed { get; set; }
    
    // Claude AI Results
    public EnhancedAnalysisResult? ClaudeResults { get; set; }
    
    // Semantic Analysis Results
    public SemanticAnalysisResult? SemanticResults { get; set; }
    
    // Integrated insights combining both
    public List<HybridOrganizationInsight> IntegratedOrganizationInsights { get; set; } = new();
    public List<HybridTheme> IntegratedThemes { get; set; } = new();
    
    public string ExecutiveSummary { get; set; } = string.Empty;
    public List<string> KeyFindings { get; set; } = new();
    
    public string MethodologyDescription { get; set; } = 
        "Hybrid analysis combining Claude AI (contextual understanding) and Sentence Transformers (semantic clustering)";
}

public class HybridOrganizationInsight
{
    public string OrganizationName { get; set; } = string.Empty;
    
    // From Claude
    public double ClaudeSentiment { get; set; }
    public string ClaudeRiskLevel { get; set; } = string.Empty;
    public List<string> ClaudeTopChallenges { get; set; } = new();
    public List<ChallengeWithRemedy> ClaudeDetailedChallenges { get; set; } = new();
    public List<string> ClaudeRecommendations { get; set; } = new();
    
    // From Semantic Analysis
    public double SemanticCoherence { get; set; }
    public List<string> SemanticKeywords { get; set; } = new();
    public int SemanticThemeId { get; set; }
    public string SemanticThemeName { get; set; } = string.Empty;
    
    // Integrated
    public int TotalComments { get; set; }
    public string IntegratedRiskAssessment { get; set; } = string.Empty;
    public string ActionNeeded { get; set; } = string.Empty;
}

public class HybridTheme
{
    public string ThemeName { get; set; } = string.Empty;
    
    // From Claude
    public List<string> ClaudeKeyIssues { get; set; } = new();
    public string ClaudeImpact { get; set; } = string.Empty;
    
    // From Semantic Analysis
    public int SemanticCommentCount { get; set; }
    public List<string> SemanticKeywords { get; set; } = new();
    public string SemanticRepresentativeComment { get; set; } = string.Empty;
    
    // Integrated
    public int TotalMentions { get; set; }
    public string IntegratedDescription { get; set; } = string.Empty;
}
