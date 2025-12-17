using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Core.Models;
using ExcelAnalysis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ExcelAnalysis.Infrastructure.Services;

/// <summary>
/// Service for tracking historical analysis data and generating trend reports
/// </summary>
public class HistoricalTrackingService
{
    private readonly AnalysisDbContext _context;

    public HistoricalTrackingService(AnalysisDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Save analysis snapshot for historical tracking
    /// </summary>
    public async Task<HistoricalAnalysisSnapshot> SaveSnapshotAsync(EnhancedAnalysisResult analysisResult, string analysisType)
    {
        var snapshot = new HistoricalAnalysisSnapshot
        {
            ExcelFileInfoId = analysisResult.ExcelFileInfoId,
            AnalyzedAt = DateTime.UtcNow,
            AnalysisType = analysisType,
            OverallSentiment = analysisResult.OverallAverageSentiment,
            TotalOrganizations = analysisResult.TotalGranteesAnalyzed,
            TotalResponses = analysisResult.TotalResponsesAnalyzed,
            HighRiskCount = analysisResult.HighRiskCount,
            MediumRiskCount = analysisResult.MediumRiskCount,
            LowRiskCount = analysisResult.LowRiskCount,
            SnapshotDataJson = JsonSerializer.Serialize(analysisResult)
        };

        // Save organization snapshots
        foreach (var org in analysisResult.LowestSentimentOrganizations.Concat(analysisResult.HighestChallengeOrganizations).DistinctBy(o => o.OrganizationName))
        {
            snapshot.OrganizationSnapshots.Add(new OrganizationSnapshot
            {
                OrganizationName = org.OrganizationName,
                SentimentScore = org.AverageSentiment,
                ChallengeCount = org.ChallengeCount,
                RiskLevel = org.RiskLevel,
                TopChallengesJson = JsonSerializer.Serialize(org.TopChallenges)
            });
        }

        // Track challenges
        foreach (var challenge in analysisResult.TopChallenges)
        {
            var existingChallenge = await _context.HistoricalChallenges
                .FirstOrDefaultAsync(c => c.ChallengeName == challenge.ChallengeName && c.Status == "Active");

            if (existingChallenge != null)
            {
                existingChallenge.LastSeen = DateTime.UtcNow;
                existingChallenge.TotalOccurrences += challenge.Count;
            }
            else
            {
                snapshot.Challenges.Add(new HistoricalChallenge
                {
                    ChallengeName = challenge.ChallengeName,
                    Category = DetermineChallengeCategory(challenge.ChallengeName),
                    FirstIdentified = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow,
                    TotalOccurrences = challenge.Count,
                    Status = "Active"
                });
            }
        }

        _context.HistoricalSnapshots.Add(snapshot);
        await _context.SaveChangesAsync();

        return snapshot;
    }

    /// <summary>
    /// Get comparative analysis between two time periods
    /// </summary>
    public async Task<List<ComparativeAnalysis>> GetComparativeAnalysisAsync(int fileId, DateTime? compareDate = null)
    {
        var snapshots = await _context.HistoricalSnapshots
            .Include(s => s.OrganizationSnapshots)
            .Where(s => s.ExcelFileInfoId == fileId)
            .OrderByDescending(s => s.AnalyzedAt)
            .Take(2)
            .ToListAsync();

        if (snapshots.Count < 2)
        {
            return new List<ComparativeAnalysis>();
        }

        var current = snapshots[0];
        var previous = snapshots[1];

        var comparisons = new List<ComparativeAnalysis>();

        // Get all unique organizations
        var allOrgs = current.OrganizationSnapshots
            .Select(o => o.OrganizationName)
            .Union(previous.OrganizationSnapshots.Select(o => o.OrganizationName))
            .Distinct();

        foreach (var orgName in allOrgs)
        {
            var currentOrg = current.OrganizationSnapshots.FirstOrDefault(o => o.OrganizationName == orgName);
            var previousOrg = previous.OrganizationSnapshots.FirstOrDefault(o => o.OrganizationName == orgName);

            if (currentOrg == null || previousOrg == null)
                continue;

            var currentChallenges = JsonSerializer.Deserialize<List<string>>(currentOrg.TopChallengesJson) ?? new();
            var previousChallenges = JsonSerializer.Deserialize<List<string>>(previousOrg.TopChallengesJson) ?? new();

            var sentimentChange = currentOrg.SentimentScore - previousOrg.SentimentScore;
            var comparison = new ComparativeAnalysis
            {
                OrganizationName = orgName,
                PreviousDate = previous.AnalyzedAt,
                CurrentDate = current.AnalyzedAt,
                PreviousSentiment = previousOrg.SentimentScore,
                CurrentSentiment = currentOrg.SentimentScore,
                SentimentChange = sentimentChange,
                SentimentTrend = DetermineTrend(sentimentChange, 0.05),
                NewChallenges = currentChallenges.Except(previousChallenges).ToList(),
                ResolvedChallenges = previousChallenges.Except(currentChallenges).ToList(),
                PersistentChallenges = currentChallenges.Intersect(previousChallenges).ToList(),
                PreviousRiskLevel = previousOrg.RiskLevel,
                CurrentRiskLevel = currentOrg.RiskLevel,
                RiskTrend = DetermineRiskTrend(previousOrg.RiskLevel, currentOrg.RiskLevel)
            };

            // Generate insights
            comparison.KeyChanges = GenerateKeyChanges(comparison);
            comparison.Recommendations = GenerateRecommendations(comparison);

            comparisons.Add(comparison);
        }

        return comparisons;
    }

    /// <summary>
    /// Get trend analysis for a specific challenge
    /// </summary>
    public async Task<TrendAnalysis> GetChallengeTrendAsync(string challengeName, int months = 6)
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-months);
        
        var snapshots = await _context.HistoricalSnapshots
            .Include(s => s.Challenges)
            .Include(s => s.OrganizationSnapshots)
            .Where(s => s.AnalyzedAt >= cutoffDate)
            .OrderBy(s => s.AnalyzedAt)
            .ToListAsync();

        var dataPoints = new List<TrendDataPoint>();

        foreach (var snapshot in snapshots)
        {
            var challenge = snapshot.Challenges.FirstOrDefault(c => c.ChallengeName == challengeName);
            if (challenge != null)
            {
                var affectedOrgs = snapshot.OrganizationSnapshots
                    .Where(o => JsonSerializer.Deserialize<List<string>>(o.TopChallengesJson)?.Contains(challengeName) ?? false)
                    .ToList();

                dataPoints.Add(new TrendDataPoint
                {
                    Date = snapshot.AnalyzedAt,
                    OccurrenceCount = challenge.TotalOccurrences,
                    AverageSentiment = affectedOrgs.Any() ? affectedOrgs.Average(o => o.SentimentScore) : 0,
                    AffectedOrganizations = affectedOrgs.Count
                });
            }
        }

        var trendDirection = DetermineTrendDirection(dataPoints);
        var prediction = GeneratePrediction(dataPoints, trendDirection);

        return new TrendAnalysis
        {
            ChallengeName = challengeName,
            Category = DetermineChallengeCategory(challengeName),
            TrendDirection = trendDirection,
            DataPoints = dataPoints,
            Prediction = prediction,
            ConfidenceLevel = CalculateConfidence(dataPoints),
            ContributingFactors = await IdentifyContributingFactors(challengeName, snapshots)
        };
    }

    /// <summary>
    /// Get remediation effectiveness report
    /// </summary>
    public async Task<RemediationEffectivenessReport> GetRemediationEffectivenessAsync(string challengeName)
    {
        var challenge = await _context.HistoricalChallenges
            .Include(c => c.RemediationAttempts)
            .FirstOrDefaultAsync(c => c.ChallengeName == challengeName);

        if (challenge == null || !challenge.RemediationAttempts.Any())
        {
            return new RemediationEffectivenessReport
            {
                ChallengeName = challengeName,
                TotalAttempts = 0,
                LessonsLearned = new List<string> { "No remediation attempts recorded yet" }
            };
        }

        var attempts = challenge.RemediationAttempts.ToList();
        var successful = attempts.Count(a => a.Outcome == "Successful");
        var partial = attempts.Count(a => a.Outcome == "Partial");
        var failed = attempts.Count(a => a.Outcome == "Failed");

        return new RemediationEffectivenessReport
        {
            ChallengeName = challengeName,
            TotalAttempts = attempts.Count,
            SuccessfulAttempts = successful,
            PartialSuccessAttempts = partial,
            FailedAttempts = failed,
            SuccessRate = attempts.Count > 0 ? (double)successful / attempts.Count * 100 : 0,
            AverageEffectivenessScore = attempts.Any() ? attempts.Average(a => a.EffectivenessScore) : 0,
            MostEffectiveActions = attempts.OrderByDescending(a => a.EffectivenessScore).Take(3).ToList(),
            LeastEffectiveActions = attempts.OrderBy(a => a.EffectivenessScore).Take(3).ToList(),
            LessonsLearned = ExtractLessonsLearned(attempts),
            BestPractices = ExtractBestPractices(attempts)
        };
    }

    /// <summary>
    /// Record a remediation attempt
    /// </summary>
    public async Task<RemediationAttempt> RecordRemediationAttemptAsync(
        string challengeName, 
        string actionTaken, 
        string responsibleParty,
        double sentimentBefore,
        double sentimentAfter,
        string outcome,
        string notes = "")
    {
        var challenge = await _context.HistoricalChallenges
            .FirstOrDefaultAsync(c => c.ChallengeName == challengeName && c.Status == "Active");

        if (challenge == null)
        {
            throw new InvalidOperationException($"Challenge '{challengeName}' not found or not active");
        }

        var attempt = new RemediationAttempt
        {
            HistoricalChallengeId = challenge.Id,
            AttemptedOn = DateTime.UtcNow,
            ActionTaken = actionTaken,
            ResponsibleParty = responsibleParty,
            Outcome = outcome,
            SentimentBefore = sentimentBefore,
            SentimentAfter = sentimentAfter,
            EffectivenessScore = sentimentAfter - sentimentBefore,
            Notes = notes
        };

        _context.RemediationAttempts.Add(attempt);

        // Update challenge status if resolved
        if (outcome == "Successful" && sentimentAfter > 0.7)
        {
            challenge.Status = "Resolved";
        }

        await _context.SaveChangesAsync();

        return attempt;
    }

    // Helper methods
    private string DetermineChallengeCategory(string challengeName)
    {
        var lower = challengeName.ToLower();
        if (lower.Contains("staff") || lower.Contains("recruit") || lower.Contains("turnover") || lower.Contains("employee"))
            return "Staffing";
        if (lower.Contains("fund") || lower.Contains("budget") || lower.Contains("financial"))
            return "Funding";
        if (lower.Contains("capacity") || lower.Contains("resource") || lower.Contains("equipment"))
            return "Capacity";
        if (lower.Contains("process") || lower.Contains("approval") || lower.Contains("delay") || lower.Contains("operation"))
            return "Operations";
        return "Other";
    }

    private string DetermineTrend(double change, double threshold)
    {
        if (Math.Abs(change) < threshold) return "Stable";
        return change > 0 ? "Improving" : "Worsening";
    }

    private string DetermineRiskTrend(string previousRisk, string currentRisk)
    {
        var riskLevels = new Dictionary<string, int> { { "Low", 1 }, { "Medium", 2 }, { "High", 3 } };
        var prevLevel = riskLevels.GetValueOrDefault(previousRisk, 2);
        var currLevel = riskLevels.GetValueOrDefault(currentRisk, 2);

        if (currLevel == prevLevel) return "Stable";
        return currLevel > prevLevel ? "Escalating" : "Improving";
    }

    private List<string> GenerateKeyChanges(ComparativeAnalysis comparison)
    {
        var changes = new List<string>();

        if (comparison.NewChallenges.Any())
            changes.Add($"New challenges emerged: {string.Join(", ", comparison.NewChallenges.Take(3))}");

        if (comparison.ResolvedChallenges.Any())
            changes.Add($"Resolved challenges: {string.Join(", ", comparison.ResolvedChallenges.Take(3))}");

        if (Math.Abs(comparison.SentimentChange) > 0.1)
            changes.Add($"Sentiment {comparison.SentimentTrend.ToLower()} by {Math.Abs(comparison.SentimentChange):F2} points");

        if (comparison.RiskTrend != "Stable")
            changes.Add($"Risk level {comparison.RiskTrend.ToLower()} from {comparison.PreviousRiskLevel} to {comparison.CurrentRiskLevel}");

        return changes;
    }

    private List<string> GenerateRecommendations(ComparativeAnalysis comparison)
    {
        var recommendations = new List<string>();

        if (comparison.SentimentTrend == "Worsening")
            recommendations.Add("Immediate intervention required - sentiment declining");

        if (comparison.NewChallenges.Any())
            recommendations.Add($"Address new challenges: {string.Join(", ", comparison.NewChallenges.Take(2))}");

        if (comparison.PersistentChallenges.Count > 3)
            recommendations.Add("Review strategy for persistent challenges - current approach may need adjustment");

        if (comparison.RiskTrend == "Escalating")
            recommendations.Add("Escalate to senior management - risk level increasing");

        return recommendations;
    }

    private string DetermineTrendDirection(List<TrendDataPoint> dataPoints)
    {
        if (dataPoints.Count < 2) return "Insufficient Data";

        var firstHalf = dataPoints.Take(dataPoints.Count / 2).Average(d => d.OccurrenceCount);
        var secondHalf = dataPoints.Skip(dataPoints.Count / 2).Average(d => d.OccurrenceCount);

        var change = secondHalf - firstHalf;
        if (Math.Abs(change) < 2) return "Stable";
        return change > 0 ? "Increasing" : "Decreasing";
    }

    private string GeneratePrediction(List<TrendDataPoint> dataPoints, string trendDirection)
    {
        if (dataPoints.Count < 3) return "Insufficient data for prediction";

        var latest = dataPoints.Last();
        return trendDirection switch
        {
            "Increasing" => $"Challenge likely to continue increasing. Projected {latest.OccurrenceCount * 1.2:F0} occurrences next period.",
            "Decreasing" => $"Challenge showing improvement. Projected {latest.OccurrenceCount * 0.8:F0} occurrences next period.",
            _ => "Challenge expected to remain stable at current levels."
        };
    }

    private double CalculateConfidence(List<TrendDataPoint> dataPoints)
    {
        if (dataPoints.Count < 2) return 0.3;
        if (dataPoints.Count < 4) return 0.6;
        return 0.85;
    }

    private async Task<List<string>> IdentifyContributingFactors(string challengeName, List<HistoricalAnalysisSnapshot> snapshots)
    {
        var factors = new List<string>();

        // Analyze patterns in the data
        var orgCounts = new Dictionary<string, int>();
        foreach (var snapshot in snapshots)
        {
            foreach (var org in snapshot.OrganizationSnapshots)
            {
                var challenges = JsonSerializer.Deserialize<List<string>>(org.TopChallengesJson) ?? new();
                if (challenges.Contains(challengeName))
                {
                    orgCounts[org.OrganizationName] = orgCounts.GetValueOrDefault(org.OrganizationName, 0) + 1;
                }
            }
        }

        if (orgCounts.Any())
        {
            var mostAffected = orgCounts.OrderByDescending(kv => kv.Value).First();
            factors.Add($"Most frequently affects: {mostAffected.Key} ({mostAffected.Value} occurrences)");
        }

        return factors;
    }

    private List<string> ExtractLessonsLearned(List<RemediationAttempt> attempts)
    {
        var lessons = new List<string>();

        var successful = attempts.Where(a => a.Outcome == "Successful").ToList();
        if (successful.Any())
        {
            lessons.Add($"Successful approaches: {string.Join(", ", successful.Select(a => a.ActionTaken).Distinct().Take(3))}");
        }

        var failed = attempts.Where(a => a.Outcome == "Failed").ToList();
        if (failed.Any())
        {
            lessons.Add($"Approaches to avoid: {string.Join(", ", failed.Select(a => a.ActionTaken).Distinct().Take(2))}");
        }

        return lessons;
    }

    private List<string> ExtractBestPractices(List<RemediationAttempt> attempts)
    {
        return attempts
            .Where(a => a.Outcome == "Successful" && a.EffectivenessScore > 0.2)
            .OrderByDescending(a => a.EffectivenessScore)
            .Select(a => $"{a.ActionTaken} (Effectiveness: +{a.EffectivenessScore:F2})")
            .Take(5)
            .ToList();
    }
}
