using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Core.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ExcelAnalysis.Infrastructure.Services;

public class GeminiAnalyzer : IAIAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelName;
    private readonly bool _useFastSentiment;
    private readonly bool _useDynamicKeywords;
    private readonly int _delayBetweenCallsMs;
    private DynamicSentimentAnalyzer? _dynamicSentiment;
    private int _apiCallCount = 0;
    private int _successfulCalls = 0;
    private int _failedCalls = 0;

    public GeminiAnalyzer(string apiKey, string modelName = "gemini-1.5-flash-latest", bool useFastSentiment = true, bool useDynamicKeywords = true, int delayBetweenCallsMs = 12000)
    {
        _httpClient = new HttpClient();
        _apiKey = apiKey;
        _modelName = modelName;
        _useFastSentiment = useFastSentiment;
        _useDynamicKeywords = useDynamicKeywords;
        _delayBetweenCallsMs = delayBetweenCallsMs;
        
        Console.WriteLine($"\n🚀 Google Gemini Analyzer Initialized");
        Console.WriteLine($"   Model: {modelName}");
        Console.WriteLine($"   Endpoint: https://generativelanguage.googleapis.com/v1");
        Console.WriteLine($"   Sentiment Mode: {(useFastSentiment ? "Keyword-Based (Fast)" : "AI-Based")}");
        Console.WriteLine($"   Dynamic Keywords: {(useDynamicKeywords ? "Enabled (extracts from file)" : "Disabled (uses static)")}");
        Console.WriteLine($"   Rate Limit Delay: {delayBetweenCallsMs}ms between calls");
        Console.WriteLine($"   API Key: {apiKey.Substring(0, Math.Min(20, apiKey.Length))}...");
    }

    public async Task<AnalysisResult> AnalyzeAsync(ExcelFileInfo fileInfo)
    {
        Console.WriteLine($"\n🔍 Starting Google Gemini Analysis for file: {fileInfo.FileName}");
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

        analysisResult.SentimentSummary = analysisResult.OverallSentimentScore switch
        {
            >= 0.5 => "Very Positive - Strong progress and satisfaction",
            >= 0.2 => "Positive - Good progress with minor concerns",
            > 0 => "Neutral - Mixed feedback",
            >= -0.5 => "Negative - Several concerns need attention",
            _ => "Very Negative - Significant issues requiring immediate action"
        };

        var allComments = comments.Select(c => c.Comment).ToList();
        analysisResult.IdentifiedIssues = await ExtractIssuesAsync(allComments);
        analysisResult.Blockers = await ExtractBlockersAsync(allComments);
        analysisResult.Recommendations = new List<string> { "Review high-risk items and implement mitigation strategies" };

        Console.WriteLine($"\n✅ Google Gemini Analysis Complete!");
        Console.WriteLine($"   Total API Calls: {_apiCallCount}");
        Console.WriteLine($"   ✅ Successful: {_successfulCalls}");
        Console.WriteLine($"   ❌ Failed: {_failedCalls}");
        Console.WriteLine($"   Risks Found: {analysisResult.RiskItems.Count}");
        Console.WriteLine($"   Estimated Cost: ~${(_successfulCalls * 0.0):F4} (FREE)");

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
                var level = await ClassifyRiskAsync(comment.Comment);
                if (level != RiskLevel.Low)
                {
                    var riskItem = new RiskItem
                    {
                        Description = comment.Comment,
                        Level = level,
                        SheetName = comment.RowData.ContainsKey("_SheetName") ? comment.RowData["_SheetName"]?.ToString() ?? "" : "",
                        RowNumber = comment.RowNumber,
                        FieldName = comment.Field,
                        Mitigation = await GenerateMitigationAsync(comment.Comment, level)
                    };
                    lock (riskItems)
                    {
                        riskItems.Add(riskItem);
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return riskItems.OrderByDescending(r => r.Level).ToList();
    }

    private async Task<List<ProgressMetric>> CalculateProgressAsync(List<QuestionData> questions)
    {
        var metrics = new List<ProgressMetric>();
        var yesCount = questions.Count(q => q.Answer.Equals("yes", StringComparison.OrdinalIgnoreCase) || q.Answer.Equals("y", StringComparison.OrdinalIgnoreCase));
        var totalCount = questions.Count;

        if (totalCount > 0)
        {
            metrics.Add(new ProgressMetric
            {
                Deliverable = "Overall Deliverables",
                CompletionPercentage = (yesCount * 100.0 / totalCount),
                YesCount = yesCount,
                NoCount = totalCount - yesCount,
                TotalQuestions = totalCount,
                Status = yesCount == totalCount ? ProgressStatus.Completed : 
                        yesCount > 0 ? ProgressStatus.InProgress : ProgressStatus.NotStarted
            });
        }

        return metrics;
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
            
            // Rate limit delay
            if (_apiCallCount > 1 && _delayBetweenCallsMs > 0)
            {
                Console.WriteLine($"   ⏳ Waiting {_delayBetweenCallsMs}ms for rate limit...");
                await Task.Delay(_delayBetweenCallsMs);
            }
            
            Console.WriteLine($"   🌐 Gemini API Call #{_apiCallCount}: Risk Classification");
            Console.WriteLine($"      URL: https://generativelanguage.googleapis.com/v1/models/{_modelName}:generateContent");
            Console.WriteLine($"      Model: {_modelName}");
            
            var response = await CallGeminiApiAsync(prompt);
            
            _successfulCalls++;
            Console.WriteLine($"      ✅ Status: SUCCESS (200 OK)");

            var result = response?.Trim().ToLowerInvariant() ?? "low";

            if (result.Contains("critical")) return RiskLevel.Critical;
            if (result.Contains("high")) return RiskLevel.High;
            if (result.Contains("medium")) return RiskLevel.Medium;
            return RiskLevel.Low;
        }
        catch (Exception ex)
        {
            _failedCalls++;
            Console.WriteLine($"      ❌ Status: FAILED");
            Console.WriteLine($"      Error: {ex.Message}");
            if (ex.Message.Contains("quota") || ex.Message.Contains("limit"))
            {
                Console.WriteLine($"      💡 Reason: Rate limit or quota exceeded");
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
            
            // Rate limit delay
            if (_apiCallCount > 1 && _delayBetweenCallsMs > 0)
            {
                Console.WriteLine($"   ⏳ Waiting {_delayBetweenCallsMs}ms for rate limit...");
                await Task.Delay(_delayBetweenCallsMs);
            }
            
            Console.WriteLine($"   🌐 Gemini API Call #{_apiCallCount}: Sentiment Analysis");
            Console.WriteLine($"      URL: https://generativelanguage.googleapis.com/v1/models/{_modelName}:generateContent");
            Console.WriteLine($"      Model: {_modelName}");
            
            var response = await CallGeminiApiAsync(prompt);
            
            _successfulCalls++;
            Console.WriteLine($"      ✅ Status: SUCCESS (200 OK)");

            if (double.TryParse(response?.Trim() ?? "0", out var score))
            {
                return Math.Clamp(score, -1, 1);
            }
        }
        catch (Exception ex)
        {
            _failedCalls++;
            Console.WriteLine($"      ❌ Status: FAILED");
            Console.WriteLine($"      Error: {ex.Message}");
            if (ex.Message.Contains("quota") || ex.Message.Contains("limit"))
            {
                Console.WriteLine($"      💡 Reason: Rate limit or quota exceeded");
            }
        }

        return 0;
    }

    private async Task<double> AnalyzeSentimentBatchAsync(List<string> texts)
    {
        if (!texts.Any()) return 0;
        
        var textsToAnalyze = texts.Take(20).ToList();
        var semaphore = new SemaphoreSlim(5);
        var scores = new List<double>();

        var tasks = textsToAnalyze.Select(async text =>
        {
            await semaphore.WaitAsync();
            try
            {
                var score = await AnalyzeSentimentAsync(text);
                lock (scores)
                {
                    scores.Add(score);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return scores.Any() ? scores.Average() : 0;
    }

    public async Task<List<string>> ExtractIssuesAsync(List<string> comments)
    {
        var issues = new List<string>();
        var issueKeywords = new[] { "issue", "problem", "concern", "challenge", "difficulty", "blocker", "risk" };

        foreach (var comment in comments.Take(20))
        {
            if (issueKeywords.Any(k => comment.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(comment.Substring(0, Math.Min(200, comment.Length)));
            }
        }

        return issues.Distinct().Take(10).ToList();
    }

    private async Task<List<string>> ExtractBlockersAsync(List<string> comments)
    {
        var blockers = new List<string>();
        var blockerKeywords = new[] { "blocked", "blocker", "blocking", "cannot", "unable", "waiting", "dependency" };

        foreach (var comment in comments)
        {
            if (blockerKeywords.Any(k => comment.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                blockers.Add(comment.Substring(0, Math.Min(200, comment.Length)));
            }
        }

        return blockers.Distinct().Take(5).ToList();
    }

    private async Task<string> GenerateMitigationAsync(string riskDescription, RiskLevel level)
    {
        var prompt = $@"You are a project risk management expert. Generate a mitigation strategy for this risk.

Risk Level: {level}
Risk Description: {riskDescription.Substring(0, Math.Min(300, riskDescription.Length))}

Provide a concise mitigation plan (2-3 sentences) that includes:
1. Immediate action to take
2. Who should be involved
3. Expected outcome

Mitigation:";

        try
        {
            _apiCallCount++;
            
            // Rate limit delay
            if (_apiCallCount > 1 && _delayBetweenCallsMs > 0)
            {
                Console.WriteLine($"   ⏳ Waiting {_delayBetweenCallsMs}ms for rate limit...");
                await Task.Delay(_delayBetweenCallsMs);
            }
            
            Console.WriteLine($"   🌐 Gemini API Call #{_apiCallCount}: Mitigation Generation");
            Console.WriteLine($"      URL: https://generativelanguage.googleapis.com/v1/models/{_modelName}:generateContent");
            Console.WriteLine($"      Model: {_modelName}");
            
            var response = await CallGeminiApiAsync(prompt);
            
            _successfulCalls++;
            Console.WriteLine($"      ✅ Status: SUCCESS (200 OK)");

            return response?.Trim() ?? "Review this issue with the project team and develop an action plan.";
        }
        catch (Exception ex)
        {
            _failedCalls++;
            Console.WriteLine($"      ❌ Status: FAILED");
            Console.WriteLine($"      Error: {ex.Message}");
            if (ex.Message.Contains("quota") || ex.Message.Contains("limit"))
            {
                Console.WriteLine($"      💡 Reason: Rate limit or quota exceeded");
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
            
            // Rate limit delay
            if (_apiCallCount > 1 && _delayBetweenCallsMs > 0)
            {
                Console.WriteLine($"   ⏳ Waiting {_delayBetweenCallsMs}ms for rate limit...");
                await Task.Delay(_delayBetweenCallsMs);
            }
            
            Console.WriteLine($"   🌐 Gemini API Call #{_apiCallCount}: Executive Summary");
            Console.WriteLine($"      URL: https://generativelanguage.googleapis.com/v1/models/{_modelName}:generateContent");
            Console.WriteLine($"      Model: {_modelName}");
            
            var response = await CallGeminiApiAsync(prompt);
            
            _successfulCalls++;
            Console.WriteLine($"      ✅ Status: SUCCESS (200 OK)");

            return response?.Trim() ?? $"Analysis completed on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC.";
        }
        catch (Exception ex)
        {
            _failedCalls++;
            Console.WriteLine($"      ❌ Status: FAILED");
            Console.WriteLine($"      Error: {ex.Message}");
            if (ex.Message.Contains("quota") || ex.Message.Contains("limit"))
            {
                Console.WriteLine($"      💡 Reason: Rate limit or quota exceeded");
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

    private async Task<string?> CallGeminiApiAsync(string prompt)
    {
        var url = $"https://generativelanguage.googleapis.com/v1/models/{_modelName}:generateContent?key={_apiKey}";
        
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"The request was not successful. Last API response:\n{responseBody}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;
        
        if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
        {
            var firstCandidate = candidates[0];
            if (firstCandidate.TryGetProperty("content", out var contentObj))
            {
                if (contentObj.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                {
                    var firstPart = parts[0];
                    if (firstPart.TryGetProperty("text", out var textElement))
                    {
                        return textElement.GetString();
                    }
                }
            }
        }

        return null;
    }
}
