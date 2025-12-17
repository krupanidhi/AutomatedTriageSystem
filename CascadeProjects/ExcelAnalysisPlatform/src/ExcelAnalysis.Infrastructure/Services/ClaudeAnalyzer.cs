using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Core.Models;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using System.Text.Json;

namespace ExcelAnalysis.Infrastructure.Services;

public class CommentAnalysis
{
    public string CommentText { get; set; } = "";
    public RiskLevel RiskLevel { get; set; }
    public double SentimentScore { get; set; }
    public string Category { get; set; } = "";
    public string Mitigation { get; set; } = "";
}

public class ClaudeAnalyzer : IAIAnalyzer
{
    private readonly AnthropicClient _client;
    private readonly string _modelName;
    private readonly bool _useFastSentiment;
    private readonly bool _useDynamicKeywords;
    private readonly int _delayBetweenCallsMs;
    private readonly int _maxTokensPerRequest;
    private readonly bool _enableBatching;
    private readonly int _batchSize;
    private DynamicSentimentAnalyzer? _dynamicSentiment;
    private int _apiCallCount = 0;
    private int _successfulCalls = 0;
    private int _failedCalls = 0;
    private int _totalTokensUsed = 0;
    private bool _claudeDisabledForRun = false;
    private string? _claudeDisableReason;

    public ClaudeAnalyzer(string apiKey, string modelName = "claude-sonnet-4-5-20250514", bool useFastSentiment = true, bool useDynamicKeywords = true, int delayBetweenCallsMs = 0, int maxTokensPerRequest = 1024, bool enableBatching = true, int batchSize = 10)
    {
        _client = new AnthropicClient(apiKey);
        _modelName = modelName;
        _useFastSentiment = useFastSentiment;
        _useDynamicKeywords = useDynamicKeywords;
        _delayBetweenCallsMs = delayBetweenCallsMs;
        _maxTokensPerRequest = maxTokensPerRequest;
        _enableBatching = enableBatching;
        _batchSize = batchSize;
        
        Console.WriteLine($"\n🚀 Claude (Anthropic) Analyzer Initialized");
        Console.WriteLine($"   Model: {modelName}");
        Console.WriteLine($"   Endpoint: https://api.anthropic.com");
        Console.WriteLine($"   Sentiment Mode: {(useFastSentiment ? "Keyword-Based (Fast)" : "AI-Based")}");
        Console.WriteLine($"   Dynamic Keywords: {(useDynamicKeywords ? "Enabled (extracts from file)" : "Disabled (uses static)")}");
        Console.WriteLine($"   Rate Limit Delay: {delayBetweenCallsMs}ms between calls");
        Console.WriteLine($"   Max Tokens/Request: {maxTokensPerRequest}");
        Console.WriteLine($"   Batching: {(enableBatching ? $"Enabled ({batchSize} items/batch)" : "Disabled")}");
        Console.WriteLine($"   API Key: {apiKey.Substring(0, Math.Min(20, apiKey.Length))}...");
    }

    public async Task<AnalysisResult> AnalyzeAsync(ExcelFileInfo fileInfo)
    {
        Console.WriteLine($"\n🔍 Starting Claude Analysis for file: {fileInfo.FileName}");
        Console.WriteLine($"   Using Model: {_modelName}");
        _apiCallCount = 0;
        _successfulCalls = 0;
        _failedCalls = 0;
        _totalTokensUsed = 0;
        _claudeDisabledForRun = false;
        _claudeDisableReason = null;
        
        var processor = new ExcelProcessor();
        var (comments, questions) = await processor.ExtractCommentsAndQuestionsAsync(fileInfo);
        
        Console.WriteLine($"   Extracted {comments.Count} comments and {questions.Count} questions");
        
        // Extract dynamic keywords if enabled
        if (_useDynamicKeywords)
        {
            Console.WriteLine($"\n📊 Extracting buzzwords for dynamic sentiment analysis...");
            var buzzwords = BuzzwordExtractor.ExtractBuzzwords(comments);
            
            _dynamicSentiment = new DynamicSentimentAnalyzer(
                buzzwords.NegativeKeywords,
                buzzwords.PositiveKeywords
            );
            
            Console.WriteLine($"   ✅ Extracted {buzzwords.NegativeKeywords.Count + buzzwords.PositiveKeywords.Count} keywords");
            Console.WriteLine($"   🔴 Negative: {buzzwords.NegativeKeywords.Count}");
            Console.WriteLine($"   🟢 Positive: {buzzwords.PositiveKeywords.Count}");
            Console.WriteLine($"   💭 Using dynamic keywords for sentiment analysis");
        }

        var result = new AnalysisResult
        {
            AnalyzedAt = DateTime.UtcNow,
            TotalDeliverables = questions.Count(q => q.Question.Contains("deliverable", StringComparison.OrdinalIgnoreCase)),
            CompletedDeliverables = questions.Count(q => q.Answer?.Equals("Yes", StringComparison.OrdinalIgnoreCase) == true),
            InProgressDeliverables = 0,
            NotStartedDeliverables = 0,
            ProgressMetrics = new List<ProgressMetric>()
        };

        // Analyze comments for risks and sentiment
        var commentAnalyses = new List<CommentAnalysis>();
        var limitedComments = comments.Take(50).ToList();
        
        Console.WriteLine($"\n🔍 Analyzing {limitedComments.Count} comments...");
        Console.WriteLine($"   💡 Token Optimization: Batching {(_enableBatching ? "ENABLED" : "DISABLED")}");
        
        if (_enableBatching)
        {
            // BATCH PROCESSING - Analyze multiple comments in one API call
            for (int i = 0; i < limitedComments.Count; i += _batchSize)
            {
                if (_claudeDisabledForRun)
                {
                    break;
                }
                var batch = limitedComments.Skip(i).Take(_batchSize).ToList();
                var batchResults = await ClassifyRiskBatchAsync(batch.Select(c => c.Comment).ToList());
                
                if (_claudeDisabledForRun)
                {
                    break;
                }
                
                for (int j = 0; j < batch.Count; j++)
                {
                    var comment = batch[j];
                    var riskLevel = j < batchResults.Count ? batchResults[j] : RiskLevel.Low;
                    var sentiment = await AnalyzeSentimentAsync(comment.Comment);
                    
                    var analysis = new CommentAnalysis
                    {
                        CommentText = comment.Comment,
                        RiskLevel = riskLevel,
                        SentimentScore = sentiment,
                        Category = comment.Field,
                        Mitigation = riskLevel >= RiskLevel.High ? await GenerateMitigationAsync(comment.Comment, riskLevel) : ""
                    };
                    
                    commentAnalyses.Add(analysis);
                }
            }
        }
        else
        {
            // INDIVIDUAL PROCESSING - One comment per API call
            foreach (var comment in limitedComments)
            {
                if (_claudeDisabledForRun)
                {
                    break;
                }
                var riskLevel = await ClassifyRiskAsync(comment.Comment);
                var sentiment = await AnalyzeSentimentAsync(comment.Comment);
                
                var analysis = new CommentAnalysis
                {
                    CommentText = comment.Comment,
                    RiskLevel = riskLevel,
                    SentimentScore = sentiment,
                    Category = comment.Field,
                    Mitigation = riskLevel >= RiskLevel.High ? await GenerateMitigationAsync(comment.Comment, riskLevel) : ""
                };
                
                commentAnalyses.Add(analysis);
            }
        }

        // Calculate statistics
        result.HighRiskCount = commentAnalyses.Count(c => c.RiskLevel >= RiskLevel.High);
        result.MediumRiskCount = commentAnalyses.Count(c => c.RiskLevel == RiskLevel.Medium);
        result.LowRiskCount = commentAnalyses.Count(c => c.RiskLevel == RiskLevel.Low);
        result.OverallSentimentScore = commentAnalyses.Average(c => c.SentimentScore);
        
        result.CompletionPercentage = result.TotalDeliverables > 0 
            ? (double)result.CompletedDeliverables / result.TotalDeliverables * 100 
            : 0;

        // Generate summaries
        if (_claudeDisabledForRun)
        {
            result.ExecutiveSummary = _claudeDisableReason ?? "Claude calls were disabled for this run.";
        }
        else
        {
            result.ExecutiveSummary = await GenerateSummaryAsync(result);
        }
        result.SentimentSummary = result.OverallSentimentScore switch
        {
            >= 0.5 => "Positive",
            <= -0.5 => "Negative",
            _ => "Neutral"
        };
        
        result.RiskSummary = result.HighRiskCount > 0 
            ? $"{result.HighRiskCount} high-risk issues require immediate attention"
            : "No critical risks identified";

        result.DetailedAnalysisJson = JsonSerializer.Serialize(commentAnalyses, new JsonSerializerOptions { WriteIndented = true });

        Console.WriteLine($"\n✅ Claude Analysis Complete!");
        Console.WriteLine($"   Total API Calls: {_apiCallCount}");
        Console.WriteLine($"   ✅ Successful: {_successfulCalls}");
        Console.WriteLine($"   ❌ Failed: {_failedCalls}");
        Console.WriteLine($"   🎯 Total Tokens Used: ~{_totalTokensUsed}");
        Console.WriteLine($"   💰 Token Savings: {(_enableBatching ? $"{((50 - _apiCallCount) * 100 / 50):F0}% (batching enabled)" : "0% (batching disabled)")}");
        Console.WriteLine($"   Risks Found: {result.HighRiskCount + result.MediumRiskCount}");
        Console.WriteLine($"   Estimated Cost: ~${(_totalTokensUsed * 0.000003):F4} (Claude Sonnet 4.5 pricing)");

        return result;
    }

    public async Task<List<string>> ExtractIssuesAsync(List<string> comments)
    {
        var issues = new List<string>();
        
        foreach (var comment in comments.Take(50))
        {
            var riskLevel = await ClassifyRiskAsync(comment);
            if (riskLevel >= RiskLevel.Medium)
            {
                issues.Add(comment);
            }
        }
        
        return issues;
    }

    // BATCH PROCESSING - Analyze multiple comments in one API call to save tokens
    private async Task<List<RiskLevel>> ClassifyRiskBatchAsync(List<string> comments)
    {
        var results = new List<RiskLevel>();

        if (_claudeDisabledForRun)
        {
            return Enumerable.Repeat(RiskLevel.Low, comments.Count).ToList();
        }
        
        // Build batch prompt - analyze multiple comments at once
        var batchPrompt = @"Analyze these project comments for risk levels. For each comment, classify as: Critical, High, Medium, or Low.

Respond with ONLY the risk levels, one per line, in the same order as the comments.

Comments:
";
        
        for (int i = 0; i < comments.Count; i++)
        {
            var truncated = comments[i].Substring(0, Math.Min(200, comments[i].Length));
            batchPrompt += $"{i + 1}. {truncated}\n";
        }
        
        batchPrompt += "\nRisk levels (one per line):";

        try
        {
            _apiCallCount++;
            
            if (_apiCallCount > 1 && _delayBetweenCallsMs > 0)
            {
                await Task.Delay(_delayBetweenCallsMs);
            }
            
            Console.WriteLine($"   🌐 Claude API Call #{_apiCallCount}: BATCH Risk Classification ({comments.Count} comments)");
            Console.WriteLine($"      URL: https://api.anthropic.com/v1/messages");
            Console.WriteLine($"      Model: {_modelName}");
            Console.WriteLine($"      💡 Token Savings: ~{(comments.Count - 1) * 300} tokens (batch vs individual)");
            
            var messages = new List<Message>
            {
                new Message(RoleType.User, batchPrompt)
            };

            var parameters = new MessageParameters
            {
                Messages = messages,
                MaxTokens = _maxTokensPerRequest,
                Model = _modelName,
                Stream = false,
                Temperature = 0.0m
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters);
            
            _successfulCalls++;
            
            var textContent = response.Content.FirstOrDefault() as Anthropic.SDK.Messaging.TextContent;
            var responseText = textContent?.Text?.Trim() ?? "";
            
            // Estimate tokens used
            var estimatedTokens = (batchPrompt.Length + responseText.Length) / 4;
            _totalTokensUsed += estimatedTokens;
            
            Console.WriteLine($"      ✅ Status: SUCCESS (200 OK)");
            Console.WriteLine($"      🎯 Tokens Used: ~{estimatedTokens}");
            
            // Parse batch response
            var lines = responseText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines.Take(comments.Count))
            {
                var risk = line.Trim().ToLowerInvariant();
                if (risk.Contains("critical")) results.Add(RiskLevel.Critical);
                else if (risk.Contains("high")) results.Add(RiskLevel.High);
                else if (risk.Contains("medium")) results.Add(RiskLevel.Medium);
                else results.Add(RiskLevel.Low);
            }
            
            // Fill remaining with Low if response was incomplete
            while (results.Count < comments.Count)
            {
                results.Add(RiskLevel.Low);
            }
        }
        catch (Exception ex)
        {
            _failedCalls++;
            Console.WriteLine($"      ❌ Status: FAILED");
            Console.WriteLine($"      Error: {ex.Message}");

            if (IsModelNotFoundError(ex.Message))
            {
                DisableClaudeForRun(BuildModelNotFoundMessage(ex.Message));
            }
            
            // Fallback: return all Low risk
            results = Enumerable.Repeat(RiskLevel.Low, comments.Count).ToList();
        }
        
        return results;
    }

    public async Task<RiskLevel> ClassifyRiskAsync(string commentText)
    {
        if (_claudeDisabledForRun)
        {
            return RiskLevel.Low;
        }
        var prompt = $@"Analyze this project comment for risk level. Consider:
- Schedule delays or timeline concerns
- Budget overruns or resource constraints
- Quality issues or defects
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
            
            Console.WriteLine($"   🌐 Claude API Call #{_apiCallCount}: Risk Classification");
            Console.WriteLine($"      URL: https://api.anthropic.com/v1/messages");
            Console.WriteLine($"      Model: {_modelName}");
            
            var messages = new List<Message>
            {
                new Message(RoleType.User, prompt)
            };

            var parameters = new MessageParameters
            {
                Messages = messages,
                MaxTokens = 10,
                Model = _modelName,
                Stream = false,
                Temperature = 0.0m
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters);
            
            _successfulCalls++;
            Console.WriteLine($"      ✅ Status: SUCCESS (200 OK)");

            var textContent = response.Content.FirstOrDefault() as Anthropic.SDK.Messaging.TextContent;
            var result = textContent?.Text?.Trim().ToLowerInvariant() ?? "low";

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
            if (IsModelNotFoundError(ex.Message))
            {
                DisableClaudeForRun(BuildModelNotFoundMessage(ex.Message));
            }
            if (ex.Message.Contains("quota") || ex.Message.Contains("limit"))
            {
                Console.WriteLine($"      💡 Reason: Rate limit or quota exceeded");
            }
            return RiskLevel.Low;
        }
    }

    public async Task<double> AnalyzeSentimentAsync(string text)
    {
        // Use keyword-based sentiment if enabled
        if (_useFastSentiment)
        {
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
            
            Console.WriteLine($"   🌐 Claude API Call #{_apiCallCount}: Sentiment Analysis");
            Console.WriteLine($"      URL: https://api.anthropic.com/v1/messages");
            Console.WriteLine($"      Model: {_modelName}");
            
            var messages = new List<Message>
            {
                new Message(RoleType.User, prompt)
            };

            var parameters = new MessageParameters
            {
                Messages = messages,
                MaxTokens = 10,
                Model = _modelName,
                Stream = false,
                Temperature = 0.0m
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters);
            
            _successfulCalls++;
            Console.WriteLine($"      ✅ Status: SUCCESS (200 OK)");

            var textContent = response.Content.FirstOrDefault() as Anthropic.SDK.Messaging.TextContent;
            if (double.TryParse(textContent?.Text?.Trim() ?? "0", out var score))
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

    public async Task<string> GenerateMitigationAsync(string issue, RiskLevel riskLevel)
    {
        if (_claudeDisabledForRun)
        {
            return "";
        }
        var prompt = $@"Generate a mitigation plan for this {riskLevel} risk issue:

Issue: {issue}

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
            
            Console.WriteLine($"   🌐 Claude API Call #{_apiCallCount}: Mitigation Generation");
            Console.WriteLine($"      URL: https://api.anthropic.com/v1/messages");
            Console.WriteLine($"      Model: {_modelName}");
            
            var messages = new List<Message>
            {
                new Message(RoleType.User, prompt)
            };

            var parameters = new MessageParameters
            {
                Messages = messages,
                MaxTokens = 150,
                Model = _modelName,
                Stream = false,
                Temperature = 0.7m
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters);
            
            _successfulCalls++;
            Console.WriteLine($"      ✅ Status: SUCCESS (200 OK)");

            var textContent = response.Content.FirstOrDefault() as Anthropic.SDK.Messaging.TextContent;
            return textContent?.Text?.Trim() ?? "Review this issue with the project team and develop an action plan.";
        }
        catch (Exception ex)
        {
            _failedCalls++;
            Console.WriteLine($"      ❌ Status: FAILED");
            Console.WriteLine($"      Error: {ex.Message}");
            if (IsModelNotFoundError(ex.Message))
            {
                DisableClaudeForRun(BuildModelNotFoundMessage(ex.Message));
            }
            if (ex.Message.Contains("quota") || ex.Message.Contains("limit"))
            {
                Console.WriteLine($"      💡 Reason: Rate limit or quota exceeded");
            }
            return "Review this issue with the project team and develop an action plan.";
        }
    }

    public async Task<string> GenerateSummaryAsync(AnalysisResult result)
    {
        if (_claudeDisabledForRun)
        {
            return _claudeDisableReason ?? "Claude calls were disabled for this run.";
        }
        var prompt = $@"Generate a concise executive summary (3-4 sentences) for this project analysis:

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
            
            Console.WriteLine($"   🌐 Claude API Call #{_apiCallCount}: Executive Summary");
            Console.WriteLine($"      URL: https://api.anthropic.com/v1/messages");
            Console.WriteLine($"      Model: {_modelName}");
            
            var messages = new List<Message>
            {
                new Message(RoleType.User, prompt)
            };

            var parameters = new MessageParameters
            {
                Messages = messages,
                MaxTokens = 200,
                Model = _modelName,
                Stream = false,
                Temperature = 0.7m
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters);
            
            _successfulCalls++;
            Console.WriteLine($"      ✅ Status: SUCCESS (200 OK)");

            var textContent = response.Content.FirstOrDefault() as Anthropic.SDK.Messaging.TextContent;
            return textContent?.Text?.Trim() ?? $"Analysis completed on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC.";
        }
        catch (Exception ex)
        {
            _failedCalls++;
            Console.WriteLine($"      ❌ Status: FAILED");
            Console.WriteLine($"      Error: {ex.Message}");
            if (IsModelNotFoundError(ex.Message))
            {
                DisableClaudeForRun(BuildModelNotFoundMessage(ex.Message));
            }
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

    private static bool IsModelNotFoundError(string message)
    {
        return message.Contains("not_found_error", StringComparison.OrdinalIgnoreCase)
            && message.Contains("model", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildModelNotFoundMessage(string rawError)
    {
        return "Claude model not found. This usually means the model name in appsettings.json is not enabled for your Anthropic account. " +
               "Open Anthropic Console > Workbench, copy the exact model identifier (left of \"latest\") and paste it into AI:Claude:Model. " +
               $"Raw error: {rawError}";
    }

    private void DisableClaudeForRun(string reason)
    {
        if (_claudeDisabledForRun)
        {
            return;
        }

        _claudeDisabledForRun = true;
        _claudeDisableReason = reason;
        Console.WriteLine("\n🛑 Disabling further Claude API calls for this run to avoid wasting tokens.");
        Console.WriteLine($"   Reason: {reason}");
    }
}
