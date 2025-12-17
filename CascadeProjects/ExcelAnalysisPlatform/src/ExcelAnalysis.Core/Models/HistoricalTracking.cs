namespace ExcelAnalysis.Core.Models;

/// <summary>
/// Tracks historical analysis snapshots for trend analysis
/// </summary>
public class HistoricalAnalysisSnapshot
{
    public int Id { get; set; }
    public int ExcelFileInfoId { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public string AnalysisType { get; set; } = string.Empty; // "keyword" or "ai-enhanced"
    
    // Snapshot metrics
    public double OverallSentiment { get; set; }
    public int TotalOrganizations { get; set; }
    public int TotalResponses { get; set; }
    public int HighRiskCount { get; set; }
    public int MediumRiskCount { get; set; }
    public int LowRiskCount { get; set; }
    
    // JSON storage for full details
    public string SnapshotDataJson { get; set; } = string.Empty;
    
    // Navigation
    public virtual ExcelFileInfo? ExcelFileInfo { get; set; }
    public virtual ICollection<HistoricalChallenge> Challenges { get; set; } = new List<HistoricalChallenge>();
    public virtual ICollection<OrganizationSnapshot> OrganizationSnapshots { get; set; } = new List<OrganizationSnapshot>();
}

/// <summary>
/// Tracks individual challenges over time
/// </summary>
public class HistoricalChallenge
{
    public int Id { get; set; }
    public string ChallengeName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Staffing, Funding, Capacity, Operations
    public DateTime FirstIdentified { get; set; }
    public DateTime LastSeen { get; set; }
    public int TotalOccurrences { get; set; }
    public string Status { get; set; } = string.Empty; // Active, Resolved, Recurring
    
    // Navigation
    public int? HistoricalAnalysisSnapshotId { get; set; }
    public virtual HistoricalAnalysisSnapshot? Snapshot { get; set; }
    public virtual ICollection<RemediationAttempt> RemediationAttempts { get; set; } = new List<RemediationAttempt>();
}

/// <summary>
/// Tracks remediation attempts and their effectiveness
/// </summary>
public class RemediationAttempt
{
    public int Id { get; set; }
    public int HistoricalChallengeId { get; set; }
    public DateTime AttemptedOn { get; set; }
    public string ActionTaken { get; set; } = string.Empty;
    public string ResponsibleParty { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty; // Successful, Partial, Failed, InProgress
    public double SentimentBefore { get; set; }
    public double SentimentAfter { get; set; }
    public double EffectivenessScore { get; set; } // Calculated: (SentimentAfter - SentimentBefore)
    public string Notes { get; set; } = string.Empty;
    
    // Navigation
    public virtual HistoricalChallenge? Challenge { get; set; }
}

/// <summary>
/// Organization-level snapshot for trend tracking
/// </summary>
public class OrganizationSnapshot
{
    public int Id { get; set; }
    public int HistoricalAnalysisSnapshotId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public double SentimentScore { get; set; }
    public int ChallengeCount { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string TopChallengesJson { get; set; } = string.Empty; // JSON array of challenges
    
    // Navigation
    public virtual HistoricalAnalysisSnapshot? Snapshot { get; set; }
}

/// <summary>
/// Comparative analysis between two time periods
/// </summary>
public class ComparativeAnalysis
{
    public string OrganizationName { get; set; } = string.Empty;
    public DateTime PreviousDate { get; set; }
    public DateTime CurrentDate { get; set; }
    
    // Sentiment comparison
    public double PreviousSentiment { get; set; }
    public double CurrentSentiment { get; set; }
    public double SentimentChange { get; set; }
    public string SentimentTrend { get; set; } = string.Empty; // Improving, Worsening, Stable
    
    // Challenge comparison
    public List<string> NewChallenges { get; set; } = new();
    public List<string> ResolvedChallenges { get; set; } = new();
    public List<string> PersistentChallenges { get; set; } = new();
    
    // Risk comparison
    public string PreviousRiskLevel { get; set; } = string.Empty;
    public string CurrentRiskLevel { get; set; } = string.Empty;
    public string RiskTrend { get; set; } = string.Empty; // Escalating, Improving, Stable
    
    // Insights
    public List<string> KeyChanges { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Trend analysis over multiple time periods
/// </summary>
public class TrendAnalysis
{
    public string ChallengeName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TrendDirection { get; set; } = string.Empty; // Increasing, Decreasing, Stable, Cyclical
    public List<TrendDataPoint> DataPoints { get; set; } = new();
    public string Prediction { get; set; } = string.Empty;
    public double ConfidenceLevel { get; set; }
    public List<string> ContributingFactors { get; set; } = new();
}

/// <summary>
/// Individual data point in a trend
/// </summary>
public class TrendDataPoint
{
    public DateTime Date { get; set; }
    public int OccurrenceCount { get; set; }
    public double AverageSentiment { get; set; }
    public int AffectedOrganizations { get; set; }
}

/// <summary>
/// Remediation effectiveness report
/// </summary>
public class RemediationEffectivenessReport
{
    public string ChallengeName { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int SuccessfulAttempts { get; set; }
    public int PartialSuccessAttempts { get; set; }
    public int FailedAttempts { get; set; }
    public double SuccessRate { get; set; }
    public double AverageEffectivenessScore { get; set; }
    public List<RemediationAttempt> MostEffectiveActions { get; set; } = new();
    public List<RemediationAttempt> LeastEffectiveActions { get; set; } = new();
    public List<string> LessonsLearned { get; set; } = new();
    public List<string> BestPractices { get; set; } = new();
}
