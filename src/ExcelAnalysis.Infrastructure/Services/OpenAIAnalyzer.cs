using OpenAI;
using OpenAI.Chat;
using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Core.Models;
using System.ClientModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ExcelAnalysis.Infrastructure.Services;

public class OpenAIAnalyzer : IAIAnalyzer
{
    private readonly ChatClient _chatClient;
    private readonly string _modelName;
    private readonly string _endpoint;
    private readonly bool _useFastSentiment;
    private readonly bool _useDynamicKeywords;
    private DynamicSentimentAnalyzer? _dynamicSentiment;
    private int _apiCallCount = 0;
    private int _successfulCalls = 0;
    private int _failedCalls = 0;

    public OpenAIAnalyzer(string apiKey, string modelName, string endpoint, bool useFastSentiment = true, bool useDynamicKeywords = true)
    {
        _modelName = modelName;
        _endpoint = endpoint;
        _useFastSentiment = useFastSentiment;
        _useDynamicKeywords = useDynamicKeywords;
        Console.WriteLine($"\n🚀 OpenAI Analyzer Initialized");
        Console.WriteLine($"   Model: {modelName}");
        Console.WriteLine($"   Endpoint: {endpoint}");
        Console.WriteLine($"   Sentiment Mode: {(useFastSentiment ? "Keyword-Based (Fast)" : "AI-Based")}");
        Console.WriteLine($"   Dynamic Keywords: {(useDynamicKeywords ? "Enabled (extracts from file)" : "Disabled (uses static)")}");
        Console.WriteLine($"   API Key: {apiKey.Substring(0, Math.Min(20, apiKey.Length))}...");
        var client = new OpenAIClient(new ApiKeyCredential(apiKey));
        _chatClient = client.GetChatClient(modelName);
    }

    public async Task<AnalysisResult> AnalyzeAsync(ExcelFileInfo fileInfo)
    {
        Console.WriteLine($"\n🔍 Starting OpenAI Analysis for file: {fileInfo.FileName}");
        Console.WriteLine($"   Using Model: {_modelName}");
        _apiCallCount = 0;
        
        var processor = new ExcelProcessor();
        var (comments, questions) = await processor.ExtractCommentsAndQuestionsAsync(fileInfo);
        
        Console.WriteLine($"   Extracted {comments.Count} comments and {questions.Count} questions");
        
        // Extract dynamic keywords if enabled
        if (_useFastSentiment && _useDynamicKeywords)
        {
            _dynamicSentiment = await DynamicSentimentAnalyzer.CreateFromFileAsync(fileInfo, processor, minFrequency: 2);
        }

        var analysisResult = new AnalysisResult
        {
            ExcelFileInfoId = fileInfo.Id,
            AnalyzedAt = DateTime.UtcNow
        };

        // Parallel analysis
        var riskTask = AnalyzeRisksAsync(comments);
        var progressTask = CalculateProgressAsync(questions);
        var sentimentTask = AnalyzeSentimentBatchAsync(comments.Select(c => c.Comment).ToList());

        await Task.WhenAll(riskTask, progressTask, sentimentTask);

        analysisResult.RiskItems = await riskTask;
        analysisResult.ProgressMetrics = await progressTask;
        analysisResult.OverallSentimentScore = await sentimentTask;

        // Calculate metrics
        analysisResult.TotalDeliverables = analysisResult.ProgressMetrics.Count;
        analysisResult.CompletedDeliverables = analysisResult.ProgressMetrics.Count(m => m.CompletionPercentage >= 100);
        analysisResult.InProgressDeliverables = analysisResult.ProgressMetrics.Count(m => m.CompletionPercentage > 0 && m.CompletionPercentage < 100);
        analysisResult.NotStartedDeliverables = analysisResult.ProgressMetrics.Count(m => m.CompletionPercentage == 0);
        analysisResult.CompletionPercentage = analysisResult.ProgressMetrics.Any() 
            ? analysisResult.ProgressMetrics.Average(m => m.CompletionPercentage) 
            : 0;

        analysisResult.HighRiskCount = analysisResult.RiskItems.Count(r => r.Level == RiskLevel.High || r.Level == RiskLevel.Critical);
        analysisResult.MediumRiskCount = analysisResult.RiskItems.Count(r => r.Level == RiskLevel.Medium);
        analysisResult.LowRiskCount = analysisResult.RiskItems.Count(r => r.Level == RiskLevel.Low);

        // Generate summaries
        analysisResult.ExecutiveSummary = await GenerateSummaryAsync(analysisResult);
        analysisResult.SentimentSummary = analysisResult.OverallSentimentScore switch
        {
            > 0.5 => "Very Positive - Strong progress and team morale",
            > 0 => "Positive - Generally good progress with minor concerns",
            0 => "Neutral - Mixed feedback",
            > -0.5 => "Negative - Several concerns need attention",
            _ => "Very Negative - Significant issues requiring immediate action"
        };

        var allComments = comments.Select(c => c.Comment).ToList();
        analysisResult.IdentifiedIssues = await ExtractIssuesAsync(allComments);
        analysisResult.Blockers = await ExtractBlockersAsync(allComments);
        analysisResult.Recommendations = new List<string> { "Review high-risk items and implement mitigation strategies" };

        Console.WriteLine($"\n✅ OpenAI Analysis Complete!");
        Console.WriteLine($"   Total API Calls: {_apiCallCount}");
        Console.WriteLine($"   ✅ Successful: {_successfulCalls}");
        Console.WriteLine($"   ❌ Failed: {_failedCalls}");
        Console.WriteLine($"   Risks Found: {analysisResult.RiskItems.Count}");
        Console.WriteLine($"   Estimated Cost: ~${(_successfulCalls * 0.0003):F4}");

        return analysisResult;
    }

    private async Task<List<RiskItem>> AnalyzeRisksAsync(List<CommentData> comments)
    {
        var riskItems = new List<RiskItem>();
        var commentsToAnalyze = comments.Take(50).ToList();

        // Batch process in parallel
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

        foreach (var (comment, riskLevel) in results.Where(r => r.riskLevel != RiskLevel.Low))
        {
            var deliverable = comment.RowData.ContainsKey("Organization Name") 
                ? comment.RowData["Organization Name"]?.ToString()
                : comment.RowData.ContainsKey("Deliverable")
                    ? comment.RowData["Deliverable"]?.ToString()
                    : comment.RowData.ContainsKey("Grant Number")
                        ? comment.RowData["Grant Number"]?.ToString()
                        : $"Row {comment.RowNumber}";

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
                Mitigation = "Review and address with team"
            });
        }

        if (riskItems.Any())
        {
            await EnhanceMitigationsAsync(riskItems);
        }

        return riskItems;
    }

    private async Task EnhanceMitigationsAsync(List<RiskItem> riskItems)
    {
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
            _apiCallCount++;
            Console.WriteLine($"   🌐 OpenAI API Call #{_apiCallCount}: Risk Classification");
            Console.WriteLine($"      URL: {_endpoint}/chat/completions");
            Console.WriteLine($"      Model: {_modelName}");
            
            var messages = new ChatMessage[]
            {
                new SystemChatMessage("You are a project risk assessment expert."),
                new UserChatMessage(prompt)
            };
            
            var completion = await _chatClient.CompleteChatAsync(messages);
            
            _successfulCalls++;
            Console.WriteLine($"      ✅ Status: SUCCESS (200 OK)");

            var result = completion.Value.Content[0].Text.Trim().ToLowerInvariant();

            return result switch
            {
                string r when r.Contains("critical") => RiskLevel.Critical,
                string r when r.Contains("high") => RiskLevel.High,
                string r when r.Contains("medium") => RiskLevel.Medium,
                _ => RiskLevel.Low
            };
        }
        catch (Exception ex)
        {
            _failedCalls++;
            Console.WriteLine($"      ❌ Status: FAILED");
            Console.WriteLine($"      Error: {ex.Message}");
            if (ex.Message.Contains("insufficient_quota") || ex.Message.Contains("quota"))
            {
                Console.WriteLine($"      💡 Reason: Insufficient OpenAI credits/quota");
            }
            return RiskLevel.Low;
        }
    }

    public async Task<double> AnalyzeSentimentAsync(string text)
    {
        // Use keyword-based sentiment if enabled (fast, no API calls)
        if (_useFastSentiment)
        {
            // Use dynamic keywords if available, otherwise fall back to static
            if (_dynamicSentiment != null)
            {
                return _dynamicSentiment.CalculateSentiment(text);
            }
            return SentimentKeywords.CalculateSentimentScore(text);
        }

        // Otherwise use AI-based sentiment
        var prompt = $@"Analyze the sentiment of this text and respond with a score from -1 (very negative) to 1 (very positive).
Respond with ONLY a number between -1 and 1.

Text: ""{text}""";

        try
        {
            _apiCallCount++;
            Console.WriteLine($"   🌐 OpenAI API Call #{_apiCallCount}: Sentiment Analysis");
            Console.WriteLine($"      URL: {_endpoint}/chat/completions");
            Console.WriteLine($"      Model: {_modelName}");
            
            var messages = new ChatMessage[]
            {
                new SystemChatMessage("You are a sentiment analysis expert."),
                new UserChatMessage(prompt)
            };
            
            var completion = await _chatClient.CompleteChatAsync(messages);
            
            _successfulCalls++;
            Console.WriteLine($"      ✅ Status: SUCCESS (200 OK)");

            if (double.TryParse(completion.Value.Content[0].Text.Trim(), out var score))
            {
                return Math.Clamp(score, -1, 1);
            }
        }
        catch (Exception ex)
        {
            _failedCalls++;
            Console.WriteLine($"      ❌ Status: FAILED");
            Console.WriteLine($"      Error: {ex.Message}");
            if (ex.Message.Contains("insufficient_quota") || ex.Message.Contains("quota"))
            {
                Console.WriteLine($"      💡 Reason: Insufficient OpenAI credits/quota");
            }
        }

        return 0;
    }

    private async Task<double> AnalyzeSentimentBatchAsync(List<string> texts)
    {
        if (!texts.Any()) return 0;
        
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

    private Task<List<ProgressMetric>> CalculateProgressAsync(List<QuestionData> questions)
    {
        var metrics = new List<ProgressMetric>();
        var groupedByRow = questions.GroupBy(q => q.RowNumber);

        foreach (var group in groupedByRow)
        {
            var totalQuestions = group.Count();
            var yesAnswers = group.Count(q => 
                q.Answer.Equals("yes", StringComparison.OrdinalIgnoreCase) || 
                q.Answer.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                q.Answer.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                q.Answer == "1");

            var completionPercentage = totalQuestions > 0 
                ? (double)yesAnswers / totalQuestions * 100 
                : 0;

            var firstQuestion = group.First();
            var deliverable = firstQuestion.RowData.ContainsKey("Deliverable")
                ? firstQuestion.RowData["Deliverable"]?.ToString()
                : $"Row {group.Key}";

            metrics.Add(new ProgressMetric
            {
                Deliverable = deliverable ?? "Unknown",
                CompletionPercentage = completionPercentage,
                YesCount = yesAnswers,
                NoCount = totalQuestions - yesAnswers,
                TotalQuestions = totalQuestions
            });
        }

        return Task.FromResult(metrics);
    }

    public async Task<List<string>> ExtractIssuesAsync(List<string> comments)
    {
        var issues = new List<string>();
        
        foreach (var comment in comments.Take(50))
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
            _apiCallCount++;
            Console.WriteLine($"   🌐 OpenAI API Call #{_apiCallCount}: Mitigation Generation");
            Console.WriteLine($"      URL: {_endpoint}/chat/completions");
            Console.WriteLine($"      Model: {_modelName}");
            
            var messages = new ChatMessage[]
            {
                new SystemChatMessage("You are a project risk management expert."),
                new UserChatMessage(prompt)
            };
            
            var completion = await _chatClient.CompleteChatAsync(messages);
            
            _successfulCalls++;
            Console.WriteLine($"      ✅ Status: SUCCESS (200 OK)");

            return completion.Value.Content[0].Text.Trim();
        }
        catch (Exception ex)
        {
            _failedCalls++;
            Console.WriteLine($"      ❌ Status: FAILED");
            Console.WriteLine($"      Error: {ex.Message}");
            if (ex.Message.Contains("insufficient_quota") || ex.Message.Contains("quota"))
            {
                Console.WriteLine($"      💡 Reason: Insufficient OpenAI credits/quota");
            }
            return "Review this issue with the project team and develop an action plan.";
        }
    }

    public async Task<string> GenerateSummaryAsync(AnalysisResult result)
    {
        var prompt = $@"Generate a concise executive summary (3-4 sentences) for this project analysis:

- Total Deliverables: {result.TotalDeliverables}
- Completed: {result.CompletedDeliverables}
- In Progress: {result.InProgressDeliverables}
- Overall Completion: {result.CompletionPercentage:F1}%
- High/Critical Risks: {result.HighRiskCount}
- Medium Risks: {result.MediumRiskCount}
- Sentiment Score: {result.OverallSentimentScore:F2}

Summary:";

        try
        {
            _apiCallCount++;
            Console.WriteLine($"   🌐 OpenAI API Call #{_apiCallCount}: Executive Summary");
            Console.WriteLine($"      URL: {_endpoint}/chat/completions");
            Console.WriteLine($"      Model: {_modelName}");
            
            var messages = new ChatMessage[]
            {
                new SystemChatMessage("You are an executive report writer."),
                new UserChatMessage(prompt)
            };
            
            var completion = await _chatClient.CompleteChatAsync(messages);
            
            _successfulCalls++;
            Console.WriteLine($"      ✅ Status: SUCCESS (200 OK)");

            return completion.Value.Content[0].Text.Trim();
        }
        catch (Exception ex)
        {
            _failedCalls++;
            Console.WriteLine($"      ❌ Status: FAILED");
            Console.WriteLine($"      Error: {ex.Message}");
            if (ex.Message.Contains("insufficient_quota") || ex.Message.Contains("quota"))
            {
                Console.WriteLine($"      💡 Reason: Insufficient OpenAI credits/quota");
            }
            return $"Analysis completed on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC.\n\n" +
                   $"Overall Progress: {result.CompletionPercentage:F1}%\n" +
                   $"- Completed: {result.CompletedDeliverables}/{result.TotalDeliverables} deliverables\n" +
                   $"- In Progress: {result.InProgressDeliverables}\n" +
                   $"- Not Started: {result.NotStartedDeliverables}\n\n" +
                   $"Risk Assessment:\n" +
                   $"- High/Critical Risks: {result.HighRiskCount}\n" +
                   $"- Medium Risks: {result.MediumRiskCount}\n" +
                   $"- Low Risks: {result.LowRiskCount}\n\n" +
                   $"Sentiment: {result.SentimentSummary}";
        }
    }
}
