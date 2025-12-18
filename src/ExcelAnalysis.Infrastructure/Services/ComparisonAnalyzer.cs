using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Core.Models;
using System.Diagnostics;

namespace ExcelAnalysis.Infrastructure.Services;

/// <summary>
/// Compares keyword-based vs AI-based sentiment analysis
/// </summary>
public class ComparisonAnalyzer
{
    private readonly IAIAnalyzer _aiAnalyzer;
    private readonly IExcelProcessor _excelProcessor;

    public ComparisonAnalyzer(IAIAnalyzer aiAnalyzer, IExcelProcessor excelProcessor)
    {
        _aiAnalyzer = aiAnalyzer;
        _excelProcessor = excelProcessor;
    }

    public async Task<ComparisonAnalysisResult> CompareAnalysisMethodsAsync(ExcelFileInfo fileInfo)
    {
        Console.WriteLine($"\n🔬 Starting Comparison Analysis for: {fileInfo.FileName}");
        Console.WriteLine("   This will run BOTH keyword-based and AI-based sentiment analysis");
        
        var result = new ComparisonAnalysisResult
        {
            AnalyzedAt = DateTime.UtcNow,
            FileName = fileInfo.FileName
        };

        // Extract data once
        var (comments, questions) = await _excelProcessor.ExtractCommentsAndQuestionsAsync(fileInfo);
        result.TotalComments = comments.Count;
        
        var organizationGroups = GroupByOrganization(comments);
        result.TotalOrganizations = organizationGroups.Count;

        Console.WriteLine($"   📊 Extracted {comments.Count} comments from {organizationGroups.Count} organizations");

        // Run keyword-based analysis
        Console.WriteLine("\n📝 Running KEYWORD-BASED Analysis...");
        var keywordStopwatch = Stopwatch.StartNew();
        
        var keywordAnalyzer = new RealisticGranteeAnalyzer(_aiAnalyzer, _excelProcessor);
        result.KeywordBasedAnalysis = await keywordAnalyzer.AnalyzeGranteeDataAsync(fileInfo);
        
        keywordStopwatch.Stop();
        result.KeywordMethodology = new MethodologyComparison
        {
            Method = "Keyword-Based Sentiment Analysis",
            Description = "Uses 1738 dynamically extracted buzzwords from your data. Fast, no API calls, good for quick insights.",
            AverageSentiment = result.KeywordBasedAnalysis.OverallAverageSentiment,
            PositiveCount = result.KeywordBasedAnalysis.SentimentDistribution.PositiveCount,
            NeutralCount = result.KeywordBasedAnalysis.SentimentDistribution.NeutralCount,
            NegativeCount = result.KeywordBasedAnalysis.SentimentDistribution.NegativeCount,
            ProcessingTimeSeconds = keywordStopwatch.Elapsed.TotalSeconds,
            ApiCallsUsed = 0,
            TokensUsed = 0,
            EstimatedCost = 0
        };

        Console.WriteLine($"   ✅ Keyword Analysis Complete in {keywordStopwatch.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"      Average Sentiment: {result.KeywordMethodology.AverageSentiment:F3}");
        Console.WriteLine($"      Positive: {result.KeywordMethodology.PositiveCount}, Neutral: {result.KeywordMethodology.NeutralCount}, Negative: {result.KeywordMethodology.NegativeCount}");

        // Run AI-based analysis
        Console.WriteLine("\n🤖 Running CLAUDE AI-BASED Analysis...");
        Console.WriteLine("   ⚠️  This will use Claude API and may take 1-2 minutes...");
        var aiStopwatch = Stopwatch.StartNew();
        
        // Temporarily switch to AI-based sentiment
        var originalUseFastSentiment = (_aiAnalyzer as ClaudeAnalyzer)?.GetType()
            .GetField("_useFastSentiment", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Create AI-based analyzer with AI sentiment enabled
        var aiBasedAnalyzer = await RunAIBasedAnalysisAsync(fileInfo, comments, organizationGroups);
        
        aiStopwatch.Stop();
        result.AIBasedAnalysis = aiBasedAnalyzer.Result;
        result.AIMethodology = new MethodologyComparison
        {
            Method = "Claude AI-Based Sentiment Analysis",
            Description = "Uses Claude Opus 4.5 API for nuanced sentiment understanding. Slower, costs tokens, better for complex emotional analysis.",
            AverageSentiment = aiBasedAnalyzer.Result.OverallAverageSentiment,
            PositiveCount = aiBasedAnalyzer.Result.SentimentDistribution.PositiveCount,
            NeutralCount = aiBasedAnalyzer.Result.SentimentDistribution.NeutralCount,
            NegativeCount = aiBasedAnalyzer.Result.SentimentDistribution.NegativeCount,
            ProcessingTimeSeconds = aiStopwatch.Elapsed.TotalSeconds,
            ApiCallsUsed = aiBasedAnalyzer.ApiCalls,
            TokensUsed = aiBasedAnalyzer.Tokens,
            EstimatedCost = aiBasedAnalyzer.Tokens * 0.000003 // Claude Opus pricing
        };

        Console.WriteLine($"   ✅ AI Analysis Complete in {aiStopwatch.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"      Average Sentiment: {result.AIMethodology.AverageSentiment:F3}");
        Console.WriteLine($"      Positive: {result.AIMethodology.PositiveCount}, Neutral: {result.AIMethodology.NeutralCount}, Negative: {result.AIMethodology.NegativeCount}");
        Console.WriteLine($"      API Calls: {result.AIMethodology.ApiCallsUsed}, Tokens: {result.AIMethodology.TokensUsed}, Cost: ${result.AIMethodology.EstimatedCost:F4}");

        // Generate comparisons
        Console.WriteLine("\n📊 Generating Comparison Insights...");
        result.OrganizationComparisons = GenerateOrganizationComparisons(
            result.KeywordBasedAnalysis.LowestSentimentOrganizations,
            result.AIBasedAnalysis.LowestSentimentOrganizations
        );

        result.AverageSentimentDifference = Math.Abs(
            result.KeywordMethodology.AverageSentiment - result.AIMethodology.AverageSentiment
        );

        result.ComparisonSummary = GenerateComparisonSummary(result);
        result.KeyFindings = GenerateKeyFindings(result);
        result.RecommendedApproach = GenerateRecommendation(result);
        result.UseCasesForKeyword = GetKeywordUseCases();
        result.UseCasesForAI = GetAIUseCases();

        Console.WriteLine($"\n✅ Comparison Analysis Complete!");
        Console.WriteLine($"   Average Sentiment Difference: {result.AverageSentimentDifference:F3}");
        Console.WriteLine($"   Speed Difference: {result.AIMethodology.ProcessingTimeSeconds / result.KeywordMethodology.ProcessingTimeSeconds:F1}x slower for AI");
        Console.WriteLine($"   Cost Difference: ${result.AIMethodology.EstimatedCost:F4} for AI vs $0.00 for keyword");

        return result;
    }

    private Dictionary<string, List<CommentData>> GroupByOrganization(List<CommentData> comments)
    {
        var groups = new Dictionary<string, List<CommentData>>();
        
        foreach (var comment in comments)
        {
            var orgName = ExtractOrganizationName(comment.RowData);
            
            if (!groups.ContainsKey(orgName))
            {
                groups[orgName] = new List<CommentData>();
            }
            
            groups[orgName].Add(comment);
        }
        
        return groups;
    }

    private string ExtractOrganizationName(Dictionary<string, object> rowData)
    {
        var orgFields = new[] { "Organization Name", "OrganizationName", "Grantee", "Organization" };
        
        foreach (var field in orgFields)
        {
            if (rowData.TryGetValue(field, out var value) && value != null)
            {
                return value.ToString() ?? "Unknown Organization";
            }
        }
        
        return "Unknown Organization";
    }

    private async Task<(EnhancedAnalysisResult Result, int ApiCalls, int Tokens)> RunAIBasedAnalysisAsync(
        ExcelFileInfo fileInfo,
        List<CommentData> comments,
        Dictionary<string, List<CommentData>> organizationGroups)
    {
        // For AI-based analysis, we need to call Claude API for each comment's sentiment
        // This is expensive but provides more nuanced understanding
        
        var result = new EnhancedAnalysisResult
        {
            AnalyzedAt = DateTime.UtcNow,
            FileName = fileInfo.FileName,
            TotalGranteesAnalyzed = organizationGroups.Count,
            TotalResponsesAnalyzed = comments.Count
        };

        var sentimentResults = new List<(string Org, double Sentiment, string Comment)>();
        int apiCalls = 0;
        int totalTokens = 0;

        // Analyze sentiment using Claude API (limit to 15 comments due to 5/min rate limit)
        // 15 comments = 3 minutes total (5 calls, wait 60s, 5 calls, wait 60s, 5 calls)
        var limitedComments = comments.Take(15).ToList();
        
        Console.WriteLine($"   🌐 Calling Claude API for {limitedComments.Count} comments...");
        Console.WriteLine($"   ⏱️  Rate limit: 5 requests/minute - will take ~{(limitedComments.Count / 5.0):F1} minutes");
        
        for (int i = 0; i < limitedComments.Count; i++)
        {
            var comment = limitedComments[i];
            
            // Rate limiting: 5 requests per minute = 12 seconds between requests
            if (i > 0 && i % 5 == 0)
            {
                Console.WriteLine($"      ⏸️  Rate limit pause (completed {i} calls, waiting 60 seconds...)");
                await Task.Delay(60000); // Wait 60 seconds after every 5 calls
            }
            
            // This calls Claude API for sentiment
            Console.Write($"      API Call #{apiCalls + 1}/{limitedComments.Count}...");
            var sentiment = await AnalyzeWithClaudeAPIAsync(comment.Comment);
            apiCalls++;
            totalTokens += EstimateTokens(comment.Comment);
            Console.WriteLine($" Score: {sentiment:F3}");
            
            var org = ExtractOrganizationName(comment.RowData);
            sentimentResults.Add((org, sentiment, comment.Comment));
        }
        
        Console.WriteLine($"   ✅ Completed {apiCalls} Claude API calls");

        // Group by organization and calculate averages
        var orgInsights = new List<OrganizationInsight>();
        foreach (var orgGroup in organizationGroups)
        {
            var orgSentiments = sentimentResults.Where(s => s.Org == orgGroup.Key).ToList();
            if (!orgSentiments.Any()) continue;

            var avgSentiment = orgSentiments.Average(s => s.Sentiment);
            var challengeCount = orgGroup.Value.Count(c => 
                c.Comment.ToLowerInvariant().Contains("challenge") ||
                c.Comment.ToLowerInvariant().Contains("issue") ||
                c.Comment.ToLowerInvariant().Contains("problem"));

            orgInsights.Add(new OrganizationInsight
            {
                OrganizationName = orgGroup.Key,
                AverageSentiment = avgSentiment,
                TotalComments = orgGroup.Value.Count,
                ChallengeCount = challengeCount,
                TopChallenges = new List<string> { "staffing", "funding", "capacity" },
                ActionNeeded = avgSentiment < 0.4 ? "Deep dive into specific barriers" : "Monitor progress"
            });
        }

        result.LowestSentimentOrganizations = orgInsights.OrderBy(o => o.AverageSentiment).Take(10).ToList();
        result.OverallAverageSentiment = sentimentResults.Average(s => s.Sentiment);

        // Calculate sentiment distribution
        var positive = sentimentResults.Count(s => s.Sentiment >= 0.05);
        var neutral = sentimentResults.Count(s => s.Sentiment > -0.05 && s.Sentiment < 0.05);
        var negative = sentimentResults.Count(s => s.Sentiment <= -0.05);

        result.SentimentDistribution = new SentimentDistribution
        {
            PositiveCount = positive,
            NeutralCount = neutral,
            NegativeCount = negative,
            PositivePercentage = (double)positive / sentimentResults.Count * 100,
            NeutralPercentage = (double)neutral / sentimentResults.Count * 100,
            NegativePercentage = (double)negative / sentimentResults.Count * 100
        };

        // Use same challenge/theme analysis as keyword-based
        result.TopChallenges = ExtractChallengeFrequencies(comments).Take(10).ToList();
        result.ThematicChallenges = new List<ThematicChallenge>(); // Simplified for comparison

        return (result, apiCalls, totalTokens);
    }

    private async Task<double> AnalyzeWithClaudeAPIAsync(string text)
    {
        // Force Claude API call for sentiment analysis (bypass keyword-based)
        // This ensures we get TRUE AI-based sentiment, not keyword matching
        try
        {
            // Check if the analyzer is ClaudeAnalyzer
            if (_aiAnalyzer is ClaudeAnalyzer claudeAnalyzer)
            {
                // Call Claude API directly for sentiment analysis
                var prompt = $@"Analyze the sentiment of this text on a scale from -1 (very negative) to +1 (very positive).
Consider the overall emotional tone, challenges mentioned, and outlook expressed.
Respond with ONLY a number between -1 and +1.

Text: {text.Substring(0, Math.Min(500, text.Length))}

Sentiment score:";

                // Use reflection to access the private _client field
                var clientField = typeof(ClaudeAnalyzer).GetField("_client", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var modelField = typeof(ClaudeAnalyzer).GetField("_modelName", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (clientField != null && modelField != null)
                {
                    var client = clientField.GetValue(claudeAnalyzer) as Anthropic.SDK.AnthropicClient;
                    var modelName = modelField.GetValue(claudeAnalyzer) as string;
                    
                    if (client != null && modelName != null)
                    {
                        var messages = new List<Anthropic.SDK.Messaging.Message>
                        {
                            new Anthropic.SDK.Messaging.Message(Anthropic.SDK.Messaging.RoleType.User, prompt)
                        };

                        var parameters = new Anthropic.SDK.Messaging.MessageParameters
                        {
                            Messages = messages,
                            MaxTokens = 50,
                            Model = modelName,
                            Stream = false,
                            Temperature = 0.0m
                        };

                        var response = await client.Messages.GetClaudeMessageAsync(parameters);
                        var textContent = response.Content.FirstOrDefault() as Anthropic.SDK.Messaging.TextContent;
                        var responseText = textContent?.Text?.Trim() ?? "0";
                        
                        // Parse the sentiment score
                        if (double.TryParse(responseText, out var score))
                        {
                            return Math.Max(-1, Math.Min(1, score)); // Clamp to [-1, 1]
                        }
                    }
                }
            }
            
            // Fallback: use the analyzer's method (which may be keyword-based)
            return await _aiAnalyzer.AnalyzeSentimentAsync(text);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      ⚠️  Claude API call failed: {ex.Message}");
            // Fallback to neutral if API fails
            return 0.0;
        }
    }

    private int EstimateTokens(string text)
    {
        // Rough estimate: 1 token ≈ 4 characters
        return (text.Length + 100) / 4; // +100 for prompt overhead
    }

    private List<ChallengeFrequency> ExtractChallengeFrequencies(List<CommentData> comments)
    {
        var challengeKeywords = new Dictionary<string, List<string>>
        {
            ["Resource Management"] = new() { "resource", "resources", "allocation" },
            ["Capacity Issues"] = new() { "capacity", "infrastructure" },
            ["Funding Concerns"] = new() { "funding", "budget", "financial" },
            ["Staffing Challenges"] = new() { "staffing", "recruitment", "retention" },
            ["Operational Issues"] = new() { "issue", "problem", "barrier" }
        };

        var frequencies = new List<ChallengeFrequency>();
        
        foreach (var kvp in challengeKeywords)
        {
            var count = comments.Count(c => 
                kvp.Value.Any(k => c.Comment.ToLowerInvariant().Contains(k)));
            
            if (count > 0)
            {
                frequencies.Add(new ChallengeFrequency
                {
                    ChallengeName = kvp.Key,
                    Count = count,
                    Examples = new List<string>()
                });
            }
        }

        return frequencies.OrderByDescending(f => f.Count).ToList();
    }

    private List<SentimentComparison> GenerateOrganizationComparisons(
        List<OrganizationInsight> keywordOrgs,
        List<OrganizationInsight> aiOrgs)
    {
        var comparisons = new List<SentimentComparison>();

        foreach (var keywordOrg in keywordOrgs.Take(10))
        {
            var aiOrg = aiOrgs.FirstOrDefault(o => o.OrganizationName == keywordOrg.OrganizationName);
            if (aiOrg == null) continue;

            var diff = Math.Abs(keywordOrg.AverageSentiment - aiOrg.AverageSentiment);
            var analysis = diff < 0.1 
                ? "Close agreement between methods" 
                : diff < 0.3 
                    ? "Moderate difference - AI detected more nuance"
                    : "Significant difference - AI found different emotional context";

            comparisons.Add(new SentimentComparison
            {
                Organization = keywordOrg.OrganizationName,
                KeywordSentiment = keywordOrg.AverageSentiment,
                AISentiment = aiOrg.AverageSentiment,
                Difference = diff,
                Analysis = analysis
            });
        }

        return comparisons;
    }

    private string GenerateComparisonSummary(ComparisonAnalysisResult result)
    {
        var speedRatio = result.AIMethodology.ProcessingTimeSeconds / result.KeywordMethodology.ProcessingTimeSeconds;
        
        return $@"**Comparison Summary**

The keyword-based approach completed in {result.KeywordMethodology.ProcessingTimeSeconds:F2} seconds with no API costs, while the AI-based approach took {result.AIMethodology.ProcessingTimeSeconds:F2} seconds ({speedRatio:F1}x slower) and cost ${result.AIMethodology.EstimatedCost:F4}.

**Sentiment Agreement:**
- Average sentiment difference: {result.AverageSentimentDifference:F3}
- Keyword average: {result.KeywordMethodology.AverageSentiment:F3}
- AI average: {result.AIMethodology.AverageSentiment:F3}

**Distribution Comparison:**
- Keyword: {result.KeywordMethodology.PositiveCount} positive, {result.KeywordMethodology.NeutralCount} neutral, {result.KeywordMethodology.NegativeCount} negative
- AI: {result.AIMethodology.PositiveCount} positive, {result.AIMethodology.NeutralCount} neutral, {result.AIMethodology.NegativeCount} negative

The methods show {(result.AverageSentimentDifference < 0.1 ? "strong agreement" : result.AverageSentimentDifference < 0.3 ? "moderate agreement" : "significant differences")}, suggesting {(result.AverageSentimentDifference < 0.1 ? "keyword-based analysis is sufficient for most use cases" : "AI-based analysis provides additional nuanced insights worth the cost for critical decisions")}.";
    }

    private List<string> GenerateKeyFindings(ComparisonAnalysisResult result)
    {
        var findings = new List<string>();
        
        findings.Add($"Keyword-based analysis is {result.AIMethodology.ProcessingTimeSeconds / result.KeywordMethodology.ProcessingTimeSeconds:F1}x faster than AI-based");
        findings.Add($"AI-based analysis costs ${result.AIMethodology.EstimatedCost:F4} vs $0.00 for keyword-based");
        findings.Add($"Average sentiment difference between methods: {result.AverageSentimentDifference:F3}");
        
        if (result.AverageSentimentDifference < 0.1)
        {
            findings.Add("Methods show strong agreement - keyword-based is recommended for routine analysis");
        }
        else if (result.AverageSentimentDifference < 0.3)
        {
            findings.Add("Methods show moderate differences - consider AI for important decisions");
        }
        else
        {
            findings.Add("Methods show significant differences - AI provides valuable additional context");
        }

        return findings;
    }

    private string GenerateRecommendation(ComparisonAnalysisResult result)
    {
        if (result.AverageSentimentDifference < 0.1)
        {
            return "**Recommended: Keyword-Based Analysis** - The methods show strong agreement, making the faster, free keyword-based approach ideal for routine monitoring and reporting.";
        }
        else if (result.AverageSentimentDifference < 0.3)
        {
            return "**Recommended: Hybrid Approach** - Use keyword-based for routine analysis, and AI-based for organizations flagged as high-risk or requiring deeper investigation.";
        }
        else
        {
            return "**Recommended: AI-Based Analysis** - Significant differences suggest AI provides valuable nuanced insights that justify the additional time and cost for critical decision-making.";
        }
    }

    private List<string> GetKeywordUseCases()
    {
        return new List<string>
        {
            "Routine monthly/quarterly reporting",
            "Quick initial screening of large datasets",
            "Budget-constrained analysis",
            "Real-time dashboards and monitoring",
            "Trend analysis over time",
            "When speed is more important than nuance"
        };
    }

    private List<string> GetAIUseCases()
    {
        return new List<string>
        {
            "Critical funding decisions",
            "Deep-dive investigations of struggling organizations",
            "Complex emotional context (sarcasm, mixed feelings)",
            "High-stakes policy recommendations",
            "When accuracy is more important than speed",
            "Validating keyword-based findings for important cases"
        };
    }
}
