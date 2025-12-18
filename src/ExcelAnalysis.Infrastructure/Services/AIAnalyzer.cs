using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Core.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OllamaSharp;
using OllamaSharp.Models;
using System.Text.Json;

namespace ExcelAnalysis.Infrastructure.Services;

/// <summary>
/// AI-powered analysis using Semantic Kernel and Ollama
/// </summary>
public class AIAnalyzer : IAIAnalyzer
{
    private readonly IExcelProcessor _excelProcessor;
    private readonly OllamaApiClient _ollamaClient;
    private readonly string _modelName;

    public AIAnalyzer(IExcelProcessor excelProcessor, string ollamaEndpoint = "http://localhost:11434", string modelName = "llama3.2")
    {
        _excelProcessor = excelProcessor;
        _ollamaClient = new OllamaApiClient(ollamaEndpoint);
        _modelName = modelName;
    }

    public async Task<AnalysisResult> AnalyzeAsync(ExcelFileInfo fileInfo)
    {
        var (comments, questions) = await _excelProcessor.ExtractCommentsAndQuestionsAsync(fileInfo);

        var analysisResult = new AnalysisResult
        {
            ExcelFileInfoId = fileInfo.Id,
            AnalyzedAt = DateTime.UtcNow
        };

        // Analyze questions for progress metrics
        var progressMetrics = await AnalyzeProgressAsync(questions, fileInfo);
        analysisResult.ProgressMetrics = progressMetrics;
        
        // Calculate overall completion
        if (progressMetrics.Any())
        {
            analysisResult.CompletionPercentage = progressMetrics.Average(pm => pm.CompletionPercentage);
            analysisResult.TotalDeliverables = progressMetrics.Count;
            analysisResult.CompletedDeliverables = progressMetrics.Count(pm => pm.Status == ProgressStatus.Completed);
            analysisResult.InProgressDeliverables = progressMetrics.Count(pm => pm.Status == ProgressStatus.InProgress);
            analysisResult.NotStartedDeliverables = progressMetrics.Count(pm => pm.Status == ProgressStatus.NotStarted);
        }

        // Analyze comments for risks
        var riskItems = await AnalyzeRisksAsync(comments);
        analysisResult.RiskItems = riskItems;
        analysisResult.HighRiskCount = riskItems.Count(r => r.Level == RiskLevel.High || r.Level == RiskLevel.Critical);
        analysisResult.MediumRiskCount = riskItems.Count(r => r.Level == RiskLevel.Medium);
        analysisResult.LowRiskCount = riskItems.Count(r => r.Level == RiskLevel.Low);

        // Sentiment analysis
        if (comments.Any())
        {
            var commentTexts = comments.Select(c => c.Comment).ToList();
            analysisResult.OverallSentimentScore = await AnalyzeSentimentBatchAsync(commentTexts);
        }

        // Extract issues and blockers
        var commentList = comments.Select(c => c.Comment).ToList();
        analysisResult.IdentifiedIssues = await ExtractIssuesAsync(commentList);
        analysisResult.Blockers = await ExtractBlockersAsync(commentList);
        analysisResult.Recommendations = await GenerateRecommendationsAsync(analysisResult);

        // Generate summaries
        analysisResult.ExecutiveSummary = await GenerateSummaryAsync(analysisResult);
        analysisResult.RiskSummary = await GenerateRiskSummaryAsync(riskItems);
        analysisResult.SentimentSummary = GenerateSentimentSummary(analysisResult.OverallSentimentScore);

        return analysisResult;
    }

    private Task<List<ProgressMetric>> AnalyzeProgressAsync(List<QuestionData> questions, ExcelFileInfo fileInfo)
    {
        var metrics = new List<ProgressMetric>();
        
        // Group questions by deliverable (assuming there's a deliverable column)
        var deliverableGroups = questions
            .GroupBy(q => q.RowData.ContainsKey("Deliverable") ? q.RowData["Deliverable"]?.ToString() : $"Row {q.RowNumber}")
            .ToList();

        foreach (var group in deliverableGroups)
        {
            var yesCount = group.Count(q => q.Answer.ToLowerInvariant() is "yes" or "y" or "true" or "1");
            var totalQuestions = group.Count();
            var completionPercentage = totalQuestions > 0 ? (double)yesCount / totalQuestions * 100 : 0;

            var status = completionPercentage switch
            {
                100 => ProgressStatus.Completed,
                > 0 => ProgressStatus.InProgress,
                _ => ProgressStatus.NotStarted
            };

            metrics.Add(new ProgressMetric
            {
                Deliverable = group.Key ?? "Unknown",
                CompletionPercentage = Math.Round(completionPercentage, 2),
                Status = status,
                YesCount = yesCount,
                NoCount = totalQuestions - yesCount,
                TotalQuestions = totalQuestions
            });
        }

        return Task.FromResult(metrics);
    }

    private async Task<List<RiskItem>> AnalyzeRisksAsync(List<CommentData> comments)
    {
        var riskItems = new List<RiskItem>();
        var commentsToAnalyze = comments.Take(50).ToList();

        // Batch process risk classification in parallel (max 5 concurrent)
        var semaphore = new SemaphoreSlim(5);
        var tasks = commentsToAnalyze.Select(async comment =>
        {
            await semaphore.WaitAsync();
            try
            {
                var riskLevel = await ClassifyRiskAsync(comment.Comment);
                return (comment, riskLevel);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        // Process results and generate mitigations for non-low risks
        foreach (var (comment, riskLevel) in results.Where(r => r.riskLevel != RiskLevel.Low))
        {
            // Extract deliverable info from row data
            var deliverable = comment.RowData.ContainsKey("Organization Name") 
                ? comment.RowData["Organization Name"]?.ToString()
                : comment.RowData.ContainsKey("Deliverable")
                    ? comment.RowData["Deliverable"]?.ToString()
                    : comment.RowData.ContainsKey("Grant Number")
                        ? comment.RowData["Grant Number"]?.ToString()
                        : $"Row {comment.RowNumber}";

            // Get sheet name from row data
            var sheetName = comment.RowData.ContainsKey("_SheetName")
                ? comment.RowData["_SheetName"]?.ToString() ?? "Unknown"
                : "Unknown";

            riskItems.Add(new RiskItem
            {
                Deliverable = deliverable ?? "Unknown",
                Level = riskLevel,
                Description = comment.Comment,
                Source = comment.Field,
                SheetName = sheetName,
                RowNumber = comment.RowNumber,
                FieldName = comment.Field,
                IdentifiedAt = DateTime.UtcNow,
                Mitigation = "Review and address with team" // Will be enhanced by batch mitigation
            });
        }

        // Batch generate mitigations for all risk items
        if (riskItems.Any())
        {
            await EnhanceMitigationsAsync(riskItems);
        }

        return riskItems;
    }

    private async Task EnhanceMitigationsAsync(List<RiskItem> riskItems)
    {
        // Generate mitigations in parallel batches
        var semaphore = new SemaphoreSlim(3);
        var tasks = riskItems.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                item.Mitigation = await GenerateMitigationAsync(item.Description, item.Level);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    public async Task<RiskLevel> ClassifyRiskAsync(string commentText)
    {
        // Use enhanced AI prompt for better risk classification
        var prompt = $@"Analyze this project comment for risks. Consider:
- Delays, blockers, or impediments
- Budget or resource concerns
- Compliance or regulatory issues
- Stakeholder concerns or conflicts
- Technical challenges or failures

Classify as: Critical, High, Medium, or Low
Respond with ONLY ONE WORD.

Comment: {commentText.Substring(0, Math.Min(300, commentText.Length))}";

        try
        {
            var request = new GenerateRequest
            {
                Model = _modelName,
                Prompt = prompt,
                Stream = false
            };
            
            var responseText = new System.Text.StringBuilder();
            await foreach (var response in _ollamaClient.Generate(request))
            {
                if (response?.Response != null)
                    responseText.Append(response.Response);
            }
            
            var result = responseText.ToString().Trim().ToLowerInvariant();

            return result switch
            {
                string r when r.Contains("critical") => RiskLevel.Critical,
                string r when r.Contains("high") => RiskLevel.High,
                string r when r.Contains("medium") => RiskLevel.Medium,
                _ => RiskLevel.Low
            };
        }
        catch
        {
            // Fallback to enhanced keyword-based
            return ClassifyRiskByKeywords(commentText);
        }
    }

    private RiskLevel ClassifyRiskByKeywords(string text)
    {
        var lowerText = text.ToLowerInvariant();
        
        // Critical risk indicators
        var criticalKeywords = new[] 
        {
            "critical", "blocker", "cannot proceed", "stopped", "failed",
            "emergency", "urgent", "immediate attention", "crisis"
        };
        if (criticalKeywords.Any(k => lowerText.Contains(k)))
            return RiskLevel.Critical;
        
        // High risk indicators
        var highKeywords = new[] 
        {
            "high risk", "major issue", "significant delay", "behind schedule",
            "budget overrun", "non-compliant", "violation", "serious concern",
            "escalate", "not meeting", "falling short"
        };
        if (highKeywords.Any(k => lowerText.Contains(k)))
            return RiskLevel.High;
        
        // Medium risk indicators
        var mediumKeywords = new[] 
        {
            "concern", "issue", "problem", "challenge", "difficulty",
            "delay", "barrier", "obstacle", "risk", "pending",
            "waiting", "unclear", "uncertain", "needs attention"
        };
        if (mediumKeywords.Any(k => lowerText.Contains(k)))
            return RiskLevel.Medium;
        
        return RiskLevel.Low;
    }

    public async Task<double> AnalyzeSentimentAsync(string text)
    {
        var prompt = $@"Analyze the sentiment of this text and respond with a score from -1 (very negative) to 1 (very positive).
Respond with ONLY a number between -1 and 1.

Text: ""{text}""";

        try
        {
            var request = new GenerateRequest
            {
                Model = _modelName,
                Prompt = prompt,
                Stream = false
            };
            
            var responseText = new System.Text.StringBuilder();
            await foreach (var response in _ollamaClient.Generate(request))
            {
                if (response?.Response != null)
                    responseText.Append(response.Response);
            }
            
            if (double.TryParse(responseText.ToString().Trim(), out var score))
            {
                return Math.Clamp(score, -1, 1);
            }
        }
        catch { }

        // Fallback to keyword-based sentiment
        return AnalyzeSentimentByKeywords(text);
    }

    private async Task<double> AnalyzeSentimentBatchAsync(List<string> texts)
    {
        if (!texts.Any()) return 0;
        
        // Parallel sentiment analysis with batching
        var textsToAnalyze = texts.Take(20).ToList();
        var semaphore = new SemaphoreSlim(5);
        
        var tasks = textsToAnalyze.Select(async text =>
        {
            await semaphore.WaitAsync();
            try
            {
                return await AnalyzeSentimentAsync(text);
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        var sentiments = await Task.WhenAll(tasks);
        return sentiments.Average();
    }

    private double AnalyzeSentimentByKeywords(string text)
    {
        var lowerText = text.ToLowerInvariant();
        var score = 0.0;

        // Positive keywords
        if (lowerText.Contains("excellent") || lowerText.Contains("great") || lowerText.Contains("good"))
            score += 0.3;
        if (lowerText.Contains("complete") || lowerText.Contains("done") || lowerText.Contains("finished"))
            score += 0.2;

        // Negative keywords
        if (lowerText.Contains("problem") || lowerText.Contains("issue") || lowerText.Contains("concern"))
            score -= 0.3;
        if (lowerText.Contains("delay") || lowerText.Contains("late") || lowerText.Contains("behind"))
            score -= 0.2;
        if (lowerText.Contains("blocker") || lowerText.Contains("critical") || lowerText.Contains("failed"))
            score -= 0.4;

        return Math.Clamp(score, -1, 1);
    }

    public async Task<List<string>> ExtractIssuesAsync(List<string> comments)
    {
        var issues = new List<string>();
        
        foreach (var comment in comments.Take(30))
        {
            if (ContainsIssueKeywords(comment))
            {
                issues.Add(comment);
            }
        }

        return issues.Distinct().ToList();
    }

    private Task<List<string>> ExtractBlockersAsync(List<string> comments)
    {
        var blockers = new List<string>();
        
        foreach (var comment in comments)
        {
            var lowerComment = comment.ToLowerInvariant();
            if (lowerComment.Contains("blocker") || 
                lowerComment.Contains("blocked") || 
                lowerComment.Contains("cannot proceed") ||
                lowerComment.Contains("waiting for"))
            {
                blockers.Add(comment);
            }
        }

        return Task.FromResult(blockers.Distinct().ToList());
    }

    private bool ContainsIssueKeywords(string text)
    {
        var lowerText = text.ToLowerInvariant();
        return lowerText.Contains("issue") || 
               lowerText.Contains("problem") || 
               lowerText.Contains("concern") ||
               lowerText.Contains("risk") ||
               lowerText.Contains("delay");
    }

    private async Task<string> GenerateMitigationAsync(string issue, RiskLevel level)
    {
        var prompt = $@"You are a project risk management expert. Analyze this {level} risk issue and provide a specific, actionable mitigation strategy.

Issue: ""{issue.Substring(0, Math.Min(400, issue.Length))}""

Provide a concise mitigation plan (2-3 sentences) that includes:
1. Immediate action to take
2. Who should be involved
3. Expected outcome

Mitigation:";

        try
        {
            var request = new GenerateRequest
            {
                Model = _modelName,
                Prompt = prompt,
                Stream = false
            };
            
            var responseText = new System.Text.StringBuilder();
            await foreach (var response in _ollamaClient.Generate(request))
            {
                if (response?.Response != null)
                    responseText.Append(response.Response);
            }
            
            var result = responseText.ToString().Trim();
            return string.IsNullOrEmpty(result) ? "Review and address this issue with the team. Escalate if necessary." : result;
        }
        catch
        {
            return "Review and address this issue with the team. Escalate if necessary.";
        }
    }

    private Task<List<string>> GenerateRecommendationsAsync(AnalysisResult result)
    {
        var recommendations = new List<string>();

        if (result.HighRiskCount > 0)
            recommendations.Add($"Address {result.HighRiskCount} high-risk items immediately");

        if (result.CompletionPercentage < 50)
            recommendations.Add("Completion rate is below 50% - consider resource allocation");

        if (result.OverallSentimentScore < -0.3)
            recommendations.Add("Negative sentiment detected - schedule team review meeting");

        if (result.Blockers.Any())
            recommendations.Add($"Remove {result.Blockers.Count} identified blockers");

        return Task.FromResult(recommendations);
    }

    public Task<string> GenerateSummaryAsync(AnalysisResult analysisResult)
    {
        var summary = $@"Analysis completed on {analysisResult.AnalyzedAt:yyyy-MM-dd HH:mm UTC}.

Overall Progress: {analysisResult.CompletionPercentage:F1}%
- Completed: {analysisResult.CompletedDeliverables}/{analysisResult.TotalDeliverables} deliverables
- In Progress: {analysisResult.InProgressDeliverables}
- Not Started: {analysisResult.NotStartedDeliverables}

Risk Assessment:
- High/Critical Risks: {analysisResult.HighRiskCount}
- Medium Risks: {analysisResult.MediumRiskCount}
- Low Risks: {analysisResult.LowRiskCount}

Sentiment: {GenerateSentimentSummary(analysisResult.OverallSentimentScore)}

Key Issues: {analysisResult.IdentifiedIssues.Count} identified
Blockers: {analysisResult.Blockers.Count} active blockers";

        return Task.FromResult(summary);
    }

    private Task<string> GenerateRiskSummaryAsync(List<RiskItem> risks)
    {
        if (!risks.Any())
            return Task.FromResult("No significant risks identified.");

        var highRisks = risks.Where(r => r.Level >= RiskLevel.High).ToList();
        if (highRisks.Any())
        {
            return Task.FromResult($"Critical attention required: {highRisks.Count} high-priority risks detected. " +
                   $"Top concern: {highRisks.First().Description.Substring(0, Math.Min(100, highRisks.First().Description.Length))}...");
        }

        return Task.FromResult($"{risks.Count} risks identified. Majority are low to medium priority.");
    }

    private string GenerateSentimentSummary(double score)
    {
        return score switch
        {
            > 0.5 => "Very Positive - Team morale and progress are strong",
            > 0.2 => "Positive - Generally good progress with minor concerns",
            > -0.2 => "Neutral - Mixed feedback, monitor closely",
            > -0.5 => "Negative - Significant concerns raised, action needed",
            _ => "Very Negative - Critical issues require immediate attention"
        };
    }
}
