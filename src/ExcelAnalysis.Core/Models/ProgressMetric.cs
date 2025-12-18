namespace ExcelAnalysis.Core.Models;

/// <summary>
/// Represents progress tracking for deliverables
/// </summary>
public class ProgressMetric
{
    public int Id { get; set; }
    public int AnalysisResultId { get; set; }
    public string Deliverable { get; set; } = string.Empty;
    public double CompletionPercentage { get; set; }
    public ProgressStatus Status { get; set; }
    public int YesCount { get; set; }
    public int NoCount { get; set; }
    public int TotalQuestions { get; set; }
    public string Notes { get; set; } = string.Empty;
    
    // Navigation
    public AnalysisResult AnalysisResult { get; set; } = null!;
}

public enum ProgressStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Blocked = 3,
    OnHold = 4
}
