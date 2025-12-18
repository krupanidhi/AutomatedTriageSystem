namespace ExcelAnalysis.Core.Models;

/// <summary>
/// Enhanced analysis result with organization-level insights, thematic analysis, and detailed recommendations
/// Matches the quality and depth of manual GRANTEE_CHALLENGES_REPORT.md
/// </summary>
public class OrganizationInsight
{
    public string OrganizationName { get; set; } = string.Empty;
    public double AverageSentiment { get; set; }
    public int TotalComments { get; set; }
    public int ChallengeCount { get; set; }
    public List<string> TopChallenges { get; set; } = new();
    public string ActionNeeded { get; set; } = string.Empty;
    
    // Enhanced fields for detailed reviewer insights
    public string RiskLevel { get; set; } = string.Empty; // High, Medium, Low
    public List<ChallengeWithRemedy> DetailedChallenges { get; set; } = new();
    public List<string> SpecificRecommendations { get; set; } = new();
    public string ContextualBackground { get; set; } = string.Empty;
    public List<string> PositiveAspects { get; set; } = new();
    public string ReviewerNotes { get; set; } = string.Empty;
}

public class ChallengeWithRemedy
{
    public string Challenge { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string SuggestedRemedy { get; set; } = string.Empty;
    public List<string> ActionSteps { get; set; } = new();
    public string Timeline { get; set; } = string.Empty; // Immediate, Short-term, Long-term
    public string ResponsibleParty { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty; // Exact quote from Excel that supports this challenge
}

public class ThematicChallenge
{
    public string Theme { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = new();
    public int MentionCount { get; set; }
    public List<string> KeyIssues { get; set; } = new();
    public string Impact { get; set; } = string.Empty;
}

public class DetailedRecommendation
{
    public string Priority { get; set; } = string.Empty; // High, Medium, Low
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> ActionSteps { get; set; } = new();
}

public class ReviewerAnalysis
{
    public int TotalReviewerComments { get; set; }
    public int PositiveCount { get; set; }
    public int NeutralCount { get; set; }
    public int NegativeCount { get; set; }
    public List<ReviewerComment> MostNegativeComments { get; set; } = new();
}

public class ReviewerComment
{
    public string OrganizationName { get; set; } = string.Empty;
    public double SentimentScore { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

public class SentimentDistribution
{
    public int PositiveCount { get; set; }
    public int NeutralCount { get; set; }
    public int NegativeCount { get; set; }
    public double PositivePercentage { get; set; }
    public double NeutralPercentage { get; set; }
    public double NegativePercentage { get; set; }
}

public class ChallengeFrequency
{
    public string ChallengeName { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<string> Examples { get; set; } = new();
}

/// <summary>
/// Extended AnalysisResult with comprehensive insights
/// </summary>
public class EnhancedAnalysisResult
{
    // Basic info
    public int Id { get; set; }
    public int ExcelFileInfoId { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int TotalGranteesAnalyzed { get; set; }
    public int TotalResponsesAnalyzed { get; set; }
    
    // Sentiment distribution
    public SentimentDistribution SentimentDistribution { get; set; } = new();
    public double OverallAverageSentiment { get; set; }
    
    // Organization-level insights
    public List<OrganizationInsight> LowestSentimentOrganizations { get; set; } = new();
    public List<OrganizationInsight> HighestChallengeOrganizations { get; set; } = new();
    
    // Thematic analysis
    public List<ThematicChallenge> ThematicChallenges { get; set; } = new();
    
    // Challenge frequency
    public List<ChallengeFrequency> TopChallenges { get; set; } = new();
    
    // Reviewer analysis
    public ReviewerAnalysis ReviewerAnalysis { get; set; } = new();
    
    // Recommendations
    public List<DetailedRecommendation> ImmediateActions { get; set; } = new();
    public List<DetailedRecommendation> MediumTermStrategies { get; set; } = new();
    public List<DetailedRecommendation> LongTermConsiderations { get; set; } = new();
    
    // Blockers and issues
    public List<string> CriticalBlockers { get; set; } = new();
    public List<string> IdentifiedIssues { get; set; } = new();
    
    // Risk assessment
    public int HighRiskCount { get; set; }
    public int MediumRiskCount { get; set; }
    public int LowRiskCount { get; set; }
    public string RiskSummary { get; set; } = string.Empty;
    
    // Executive summary
    public string ExecutiveSummary { get; set; } = string.Empty;
    public List<string> KeyFindings { get; set; } = new();
    
    // Positive highlights
    public List<string> SuccessStories { get; set; } = new();
    public List<string> PositiveHighlights { get; set; } = new();
    
    // Methodology
    public string MethodologyDescription { get; set; } = string.Empty;
    
    // Detailed JSON for backward compatibility
    public string DetailedAnalysisJson { get; set; } = string.Empty;
}
