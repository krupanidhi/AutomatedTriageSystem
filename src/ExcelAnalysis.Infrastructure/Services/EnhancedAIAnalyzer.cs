using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;

namespace ExcelAnalysis.Infrastructure.Services;

/// <summary>
/// Enhanced AI analyzer that produces detailed organization-level insights
/// matching the structure of keyword-based analysis but with AI-powered understanding
/// Uses the configured AI provider (Claude, OpenAI, Gemini, or Ollama) from settings
/// </summary>
public class EnhancedAIAnalyzer
{
    private readonly IExcelProcessor _excelProcessor;
    private readonly IAIAnalyzer _aiAnalyzer;

    public EnhancedAIAnalyzer(IExcelProcessor excelProcessor, IAIAnalyzer aiAnalyzer)
    {
        _excelProcessor = excelProcessor;
        _aiAnalyzer = aiAnalyzer;
    }

    public async Task<EnhancedAnalysisResult> AnalyzeGranteeDataWithAIAsync(ExcelFileInfo fileInfo)
    {
        Console.WriteLine($"\n🤖 Starting AI-Enhanced Grantee Analysis for: {fileInfo.FileName}");
        
        // Extract all data
        var (comments, questions) = await _excelProcessor.ExtractCommentsAndQuestionsAsync(fileInfo);
        
        Console.WriteLine($"   📊 Extracted {comments.Count} comments and {questions.Count} questions");
        
        // Group by organization
        var organizationGroups = GroupByOrganization(comments);
        
        Console.WriteLine($"   🏢 Identified {organizationGroups.Count} organizations");
        
        // Analyze each organization with AI
        var orgInsights = new List<OrganizationInsight>();
        int count = 0;
        
        foreach (var kvp in organizationGroups)
        {
            count++;
            Console.WriteLine($"   🤖 AI analyzing organization {count}/{organizationGroups.Count}: {kvp.Key}");
            
            var insight = await AnalyzeOrganizationWithAI(kvp.Key, kvp.Value);
            orgInsights.Add(insight);
        }
        
        // Calculate overall metrics
        var sentimentScores = orgInsights.Select(o => o.AverageSentiment).ToList();
        var sentimentDist = CalculateSentimentDistribution(sentimentScores);
        
        // Generate thematic challenges using AI
        var thematicChallenges = await GenerateThematicChallengesWithAI(comments);
        
        // Generate executive summary with AI
        var executiveSummary = await GenerateAIExecutiveSummary(orgInsights, sentimentDist);
        
        var result = new EnhancedAnalysisResult
        {
            AnalyzedAt = DateTime.UtcNow,
            FileName = fileInfo.FileName,
            TotalGranteesAnalyzed = organizationGroups.Count,
            TotalResponsesAnalyzed = comments.Count,
            
            SentimentDistribution = sentimentDist,
            OverallAverageSentiment = sentimentScores.Average(),
            
            LowestSentimentOrganizations = orgInsights.OrderBy(o => o.AverageSentiment).Take(10).ToList(),
            HighestChallengeOrganizations = orgInsights.OrderByDescending(o => o.ChallengeCount).Take(5).ToList(),
            
            ThematicChallenges = thematicChallenges,
            
            ExecutiveSummary = executiveSummary,
            KeyFindings = await GenerateAIKeyFindings(orgInsights, thematicChallenges),
            
            MethodologyDescription = "AI-powered analysis using large language model for contextual understanding of challenges, sentiment, and recommendations"
        };
        
        Console.WriteLine($"   ✅ AI analysis complete!");
        
        return result;
    }

    private Dictionary<string, List<CommentData>> GroupByOrganization(List<CommentData> comments)
    {
        var groups = new Dictionary<string, List<CommentData>>();
        int skippedCount = 0;
        bool debugLogged = false;
        
        foreach (var comment in comments)
        {
            // Extract organization from RowData - try multiple possible column names
            string? org = null;
            if (comment.RowData != null)
            {
                // Debug: Log available columns from first comment
                if (!debugLogged)
                {
                    Console.WriteLine($"   📋 Available columns in data: {string.Join(", ", comment.RowData.Keys)}");
                    debugLogged = true;
                }
                
                // Try common organization column names (case-insensitive)
                var orgKey = comment.RowData.Keys.FirstOrDefault(k => 
                    k.Equals("Organization", StringComparison.OrdinalIgnoreCase) ||
                    k.Equals("Grantee", StringComparison.OrdinalIgnoreCase) ||
                    k.Equals("Grantee Name", StringComparison.OrdinalIgnoreCase) ||
                    k.Equals("Organization Name", StringComparison.OrdinalIgnoreCase) ||
                    k.Equals("Agency", StringComparison.OrdinalIgnoreCase) ||
                    k.Equals("Agency Name", StringComparison.OrdinalIgnoreCase));
                
                if (orgKey != null)
                {
                    org = comment.RowData[orgKey]?.ToString()?.Trim();
                }
                else
                {
                    // Debug: Log when no matching column is found
                    if (skippedCount == 0)
                    {
                        Console.WriteLine($"   ⚠️ No organization column found. Looking for: Organization, Grantee, Grantee Name, Organization Name, Agency, Agency Name");
                    }
                }
            }
            
            // Skip comments with empty or missing organization names
            if (string.IsNullOrWhiteSpace(org))
            {
                skippedCount++;
                continue;
            }
            
            if (!groups.ContainsKey(org))
                groups[org] = new List<CommentData>();
            groups[org].Add(comment);
        }
        
        if (skippedCount > 0)
        {
            Console.WriteLine($"   ⚠️ Skipped {skippedCount} comments with missing organization names");
        }
        
        return groups;
    }

    private async Task<OrganizationInsight> AnalyzeOrganizationWithAI(string orgName, List<CommentData> comments)
    {
        // Combine all comments for this organization
        var allText = string.Join("\n\n", comments.Select(c => c.Comment).Take(20)); // Limit to avoid token limits
        
        // AI Prompt for comprehensive organization analysis
        var prompt = $@"Analyze the following comments from organization ""{orgName}"" and provide a structured assessment.

COMMENTS:
{allText}

Provide your analysis in the following JSON format:
{{
  ""sentiment_score"": <number between -1 and 1>,
  ""risk_level"": ""<High|Medium|Low>"",
  ""top_challenges"": [""challenge1"", ""challenge2"", ""challenge3""],
  ""contextual_background"": ""<brief description of organization's situation>"",
  ""positive_aspects"": [""strength1"", ""strength2""],
  ""detailed_challenges"": [
    {{
      ""challenge"": ""<challenge name>"",
      ""description"": ""<what the challenge is>"",
      ""impact"": ""<how it affects the organization>"",
      ""suggested_remedy"": ""<how to address it>"",
      ""action_steps"": [""step1"", ""step2"", ""step3"", ""step4"", ""step5""],
      ""timeline"": ""<Immediate|Short-term|Long-term>"",
      ""responsible_party"": ""<who should handle this>"",
      ""evidence_quote"": ""<extract the MOST RELEVANT sentence or phrase from the comments above that directly supports and explains this specific challenge. Include enough context (20-50 words) to make the evidence meaningful. Do NOT use generic phrases like 'This activity is complete' - extract the actual substantive content that reveals the challenge.>""
    }}
  ],
  ""specific_recommendations"": [""rec1"", ""rec2"", ""rec3""],
  ""reviewer_notes"": ""<important notes for reviewers>""
}}

Focus on identifying specific, actionable challenges related to:
- Staffing (recruitment, retention, turnover)
- Funding (budget concerns, sustainability)
- Capacity (resources, infrastructure, equipment)
- Operations (processes, approvals, delays)

Respond with ONLY valid JSON, no additional text.";

        try
        {
            // Use the configured AI analyzer to get structured response
            // Note: This is a workaround since IAIAnalyzer doesn't have a direct prompt method
            // We'll use sentiment analysis as a proxy and parse the response
            var jsonResponse = await CallAIWithPrompt(prompt);
            
            // Debug: Log raw response length and first 500 chars
            Console.WriteLine($"      📝 Received AI response ({jsonResponse.Length} chars)");
            if (jsonResponse.Length > 0 && jsonResponse.Length < 5000)
            {
                Console.WriteLine($"      📄 Full response: {jsonResponse.Substring(0, Math.Min(500, jsonResponse.Length))}...");
            }
            
            // Try to extract JSON if wrapped in markdown code blocks
            var jsonMatch = Regex.Match(jsonResponse, @"```(?:json)?\s*(\{.*?\})\s*```", RegexOptions.Singleline);
            if (jsonMatch.Success)
            {
                Console.WriteLine($"      🔍 Extracted JSON from markdown code block");
                jsonResponse = jsonMatch.Groups[1].Value;
            }
            
            // Parse AI response
            var aiResult = JsonSerializer.Deserialize<AIOrganizationAnalysis>(jsonResponse, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            
            if (aiResult != null)
            {
                Console.WriteLine($"      ✅ Successfully parsed AI response for {orgName}");
                Console.WriteLine($"         Sentiment: {aiResult.SentimentScore:F2}, Risk: {aiResult.RiskLevel}, Challenges: {aiResult.TopChallenges?.Count ?? 0}");
                
                return new OrganizationInsight
                {
                    OrganizationName = orgName,
                    AverageSentiment = aiResult.SentimentScore,
                    TotalComments = comments.Count,
                    ChallengeCount = aiResult.TopChallenges?.Count ?? 0,
                    TopChallenges = aiResult.TopChallenges ?? new List<string>(),
                    RiskLevel = aiResult.RiskLevel ?? "Medium",
                    DetailedChallenges = aiResult.DetailedChallenges?.Select(dc => new ChallengeWithRemedy
                    {
                        Challenge = dc.Challenge ?? "Unknown Challenge",
                        Description = dc.Description ?? "",
                        Impact = dc.Impact ?? "",
                        SuggestedRemedy = dc.SuggestedRemedy ?? "",
                        ActionSteps = dc.ActionSteps ?? new List<string>(),
                        Timeline = dc.Timeline ?? "Short-term",
                        ResponsibleParty = dc.ResponsibleParty ?? "Program Leadership",
                        Evidence = dc.EvidenceQuote ?? ""
                    }).ToList() ?? new List<ChallengeWithRemedy>(),
                    SpecificRecommendations = aiResult.SpecificRecommendations ?? new List<string>(),
                    ContextualBackground = aiResult.ContextualBackground ?? "",
                    PositiveAspects = aiResult.PositiveAspects ?? new List<string>(),
                    ReviewerNotes = aiResult.ReviewerNotes ?? "",
                    ActionNeeded = DetermineActionNeeded(aiResult.SentimentScore, aiResult.RiskLevel)
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      ⚠️ AI analysis failed for {orgName}: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"         Inner exception: {ex.InnerException.Message}");
        }
        
        // Return minimal data if AI fails - don't mix with keyword analysis
        return new OrganizationInsight
        {
            OrganizationName = orgName,
            AverageSentiment = 0,
            TotalComments = comments.Count,
            ChallengeCount = 0,
            TopChallenges = new List<string>(),
            RiskLevel = "Unknown",
            DetailedChallenges = new List<ChallengeWithRemedy>(),
            SpecificRecommendations = new List<string>(),
            ContextualBackground = $"⚠️ AI analysis failed - {comments.Count} responses available. Use keyword analysis instead.",
            PositiveAspects = new List<string>(),
            ReviewerNotes = "AI API call failed - insufficient credits or API error",
            ActionNeeded = "Run keyword-based analysis for insights"
        };
    }

    private OrganizationInsight CreateFallbackAnalysis(string orgName, List<CommentData> comments)
    {
        // Robust keyword-based fallback when AI is unavailable
        var allText = string.Join(" ", comments.Select(c => c.Comment)).ToLowerInvariant();
        
        // Keyword-based sentiment analysis
        var positiveWords = new[] { "success", "excellent", "great", "improved", "effective", "positive", "strong", "well", "good", "better" };
        var negativeWords = new[] { "challenge", "issue", "problem", "difficulty", "concern", "lack", "shortage", "unable", "limited", "struggle", "barrier", "delay" };
        
        var positiveCount = positiveWords.Sum(w => Regex.Matches(allText, $@"\b{w}\w*\b").Count);
        var negativeCount = negativeWords.Sum(w => Regex.Matches(allText, $@"\b{w}\w*\b").Count);
        var totalWords = allText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        
        var sentiment = totalWords > 0 ? (positiveCount - negativeCount) / (double)Math.Max(totalWords / 10, 1) : 0;
        sentiment = Math.Max(-1, Math.Min(1, sentiment)); // Clamp to [-1, 1]
        
        // Identify challenges by keyword frequency
        var challengePatterns = new Dictionary<string, string[]>
        {
            { "Staffing Issues", new[] { "staff", "workforce", "recruit", "retention", "turnover", "hiring", "employee" } },
            { "Funding Constraints", new[] { "fund", "budget", "financial", "cost", "money", "payment", "revenue" } },
            { "Capacity Limitations", new[] { "capacity", "resource", "equipment", "space", "infrastructure", "facility" } },
            { "Training Needs", new[] { "training", "education", "learning", "skill", "development", "workshop" } },
            { "Operational Delays", new[] { "delay", "slow", "waiting", "approval", "process", "bureaucracy" } },
            { "Technology Challenges", new[] { "technology", "system", "software", "IT", "technical", "platform" } },
            { "Vaccine Hesitancy", new[] { "vaccine", "hesitancy", "vaccination", "immunization", "resistant" } },
            { "COVID-19 Impact", new[] { "covid", "pandemic", "coronavirus", "outbreak", "quarantine" } }
        };
        
        var identifiedChallenges = challengePatterns
            .Select(kvp => new
            {
                Challenge = kvp.Key,
                Count = kvp.Value.Sum(keyword => Regex.Matches(allText, $@"\b{keyword}\w*\b").Count)
            })
            .Where(x => x.Count > 0)
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();
        
        var topChallenges = identifiedChallenges.Select(x => x.Challenge).ToList();
        var challengeCount = identifiedChallenges.Sum(x => x.Count);
        
        // Determine risk level
        var riskLevel = sentiment < -0.3 || challengeCount > 15 ? "High" :
                       sentiment < 0.1 || challengeCount > 8 ? "Medium" : "Low";
        
        // Create detailed challenges with remedies
        var detailedChallenges = identifiedChallenges.Select(c => new ChallengeWithRemedy
        {
            Challenge = c.Challenge,
            Description = $"Mentioned {c.Count} times across {comments.Count} responses",
            Impact = GetChallengeImpact(c.Challenge),
            SuggestedRemedy = GetSuggestedRemedy(c.Challenge),
            ActionSteps = GetActionSteps(c.Challenge),
            Timeline = c.Count > 10 ? "Immediate" : c.Count > 5 ? "Short-term" : "Medium-term",
            ResponsibleParty = GetResponsibleParty(c.Challenge)
        }).ToList();
        
        // Generate recommendations
        var recommendations = new List<string>();
        if (topChallenges.Contains("Staffing Issues"))
            recommendations.Add("Develop comprehensive workforce recruitment and retention strategy");
        if (topChallenges.Contains("Funding Constraints"))
            recommendations.Add("Explore alternative payment models and diversified funding sources");
        if (topChallenges.Contains("Training Needs"))
            recommendations.Add("Implement ongoing professional development programs");
        if (topChallenges.Contains("Capacity Limitations"))
            recommendations.Add("Conduct capacity assessment and infrastructure improvement plan");
        if (!recommendations.Any())
            recommendations.Add("Continue monitoring and provide targeted technical assistance as needed");
        
        // Identify positive aspects
        var positiveAspects = new List<string>();
        if (allText.Contains("success")) positiveAspects.Add("Demonstrated successful initiatives");
        if (allText.Contains("improv")) positiveAspects.Add("Showing improvement trends");
        if (allText.Contains("effective")) positiveAspects.Add("Effective program implementation");
        if (allText.Contains("partner")) positiveAspects.Add("Strong partnership engagement");
        if (!positiveAspects.Any()) positiveAspects.Add("Actively engaged in reporting and feedback");
        
        return new OrganizationInsight
        {
            OrganizationName = orgName,
            AverageSentiment = sentiment,
            TotalComments = comments.Count,
            ChallengeCount = challengeCount,
            TopChallenges = topChallenges,
            RiskLevel = riskLevel,
            DetailedChallenges = detailedChallenges,
            SpecificRecommendations = recommendations,
            ContextualBackground = $"Keyword-based analysis of {comments.Count} responses. " +
                                  $"Identified {topChallenges.Count} primary challenge areas. " +
                                  $"Sentiment score: {sentiment:F2} (AI analysis unavailable - using keyword fallback).",
            PositiveAspects = positiveAspects,
            ReviewerNotes = $"⚠️ AI analysis unavailable - results based on keyword analysis. " +
                          $"Risk level: {riskLevel}. Recommend manual review for detailed insights.",
            ActionNeeded = DetermineActionNeeded(sentiment, riskLevel)
        };
    }
    
    private string GetChallengeImpact(string challenge)
    {
        return challenge switch
        {
            "Staffing Issues" => "Affects service delivery capacity, program implementation, and organizational sustainability",
            "Funding Constraints" => "Limits program expansion, staff retention, and service quality improvements",
            "Capacity Limitations" => "Restricts ability to serve patient population and implement new initiatives",
            "Training Needs" => "Impacts staff competency, service quality, and compliance with best practices",
            "Operational Delays" => "Reduces efficiency, increases costs, and affects patient satisfaction",
            "Technology Challenges" => "Hinders data management, reporting capabilities, and operational efficiency",
            "Vaccine Hesitancy" => "Reduces vaccination rates and community health protection",
            "COVID-19 Impact" => "Disrupts normal operations and strains resources",
            _ => "Requires attention to maintain program effectiveness"
        };
    }
    
    private string GetSuggestedRemedy(string challenge)
    {
        return challenge switch
        {
            "Staffing Issues" => "Implement competitive compensation packages, flexible work arrangements, and professional development opportunities",
            "Funding Constraints" => "Diversify funding sources, explore value-based payment models, and optimize operational efficiency",
            "Capacity Limitations" => "Conduct needs assessment, pursue infrastructure grants, and optimize resource utilization",
            "Training Needs" => "Develop comprehensive training programs, leverage virtual learning platforms, and establish mentorship systems",
            "Operational Delays" => "Streamline approval processes, implement project management tools, and improve communication protocols",
            "Technology Challenges" => "Invest in IT infrastructure, provide technical training, and establish IT support systems",
            "Vaccine Hesitancy" => "Implement community education campaigns, train staff in motivational interviewing, and address misinformation",
            "COVID-19 Impact" => "Develop pandemic response protocols, ensure staff safety, and maintain service continuity plans",
            _ => "Conduct detailed assessment and develop targeted intervention strategy"
        };
    }
    
    private List<string> GetActionSteps(string challenge)
    {
        return challenge switch
        {
            "Staffing Issues" => new List<string>
            {
                "Conduct salary benchmarking analysis",
                "Develop recruitment marketing campaign",
                "Implement employee retention program",
                "Create career advancement pathways",
                "Establish work-life balance initiatives"
            },
            "Funding Constraints" => new List<string>
            {
                "Identify alternative funding opportunities",
                "Develop grant proposal strategy",
                "Explore value-based payment participation",
                "Optimize billing and collections",
                "Implement cost reduction initiatives"
            },
            "Training Needs" => new List<string>
            {
                "Assess current training gaps",
                "Develop training curriculum",
                "Schedule regular training sessions",
                "Implement learning management system",
                "Evaluate training effectiveness"
            },
            _ => new List<string>
            {
                "Assess current situation and root causes",
                "Develop action plan with timeline",
                "Allocate resources and assign responsibilities",
                "Implement interventions",
                "Monitor progress and adjust as needed"
            }
        };
    }
    
    private string GetResponsibleParty(string challenge)
    {
        return challenge switch
        {
            "Staffing Issues" => "HR Department / Executive Leadership",
            "Funding Constraints" => "Finance Department / Development Team",
            "Capacity Limitations" => "Operations Management / Facilities Team",
            "Training Needs" => "Training & Development / Clinical Leadership",
            "Operational Delays" => "Operations Management / Process Improvement Team",
            "Technology Challenges" => "IT Department / Technology Leadership",
            _ => "Program Leadership / Management Team"
        };
    }

    private string DetermineActionNeeded(double sentiment, string? riskLevel)
    {
        if (riskLevel == "High" || sentiment < 0.3)
            return "URGENT: Deep dive into specific barriers and provide targeted technical assistance";
        if (riskLevel == "Medium" || sentiment < 0.5)
            return "Monitor progress closely and offer proactive support";
        return "Continue current support level with periodic check-ins";
    }

    private async Task<string> CallAIWithPrompt(string prompt)
    {
        // Use the configured AI analyzer to get structured responses
        try
        {
            // Check if it's ClaudeAnalyzer
            if (_aiAnalyzer is ClaudeAnalyzer claudeAnalyzer)
            {
                // Access the Claude client using reflection
                var clientField = typeof(ClaudeAnalyzer).GetField("_client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var modelField = typeof(ClaudeAnalyzer).GetField("_modelName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (clientField != null && modelField != null)
                {
                    var client = clientField.GetValue(claudeAnalyzer) as AnthropicClient;
                    var modelName = modelField.GetValue(claudeAnalyzer) as string;
                    
                    if (client != null && modelName != null)
                    {
                        var messages = new List<Message>
                        {
                            new Message(RoleType.User, prompt)
                        };

                        var parameters = new MessageParameters
                        {
                            Messages = messages,
                            MaxTokens = 4096,
                            Model = modelName,
                            Stream = false,
                            Temperature = 0.3m
                        };

                        var response = await client.Messages.GetClaudeMessageAsync(parameters);
                        var textContent = response.Content.FirstOrDefault() as TextContent;
                        return textContent?.Text ?? "{}";
                    }
                }
            }
            
            // Fallback for other AI providers - return empty JSON
            Console.WriteLine($"   ⚠️ AI provider {_aiAnalyzer.GetType().Name} not supported for enhanced analysis, using fallback");
            return "{}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ AI call failed: {ex.Message}");
            return "{}";
        }
    }

    private async Task<List<ThematicChallenge>> GenerateThematicChallengesWithAI(List<CommentData> comments)
    {
        var sampleComments = string.Join("\n", comments.Take(50).Select(c => c.Comment));
        
        var prompt = $@"Analyze these grantee comments and identify 3-5 major thematic challenges.

COMMENTS:
{sampleComments}

For each theme, provide:
1. Theme name
2. Key issues within that theme
3. Overall impact

Respond in JSON format:
[
  {{
    ""theme"": ""Theme Name"",
    ""keywords"": [""keyword1"", ""keyword2""],
    ""mention_count"": <estimated number>,
    ""key_issues"": [""issue1"", ""issue2""],
    ""impact"": ""description of impact""
  }}
]

Common themes to look for: Workforce, Funding, Capacity, Operations, Compliance.
Respond with ONLY valid JSON.";

        try
        {
            var jsonResponse = await CallAIWithPrompt(prompt);
            
            var jsonMatch = Regex.Match(jsonResponse, @"```(?:json)?\s*(\[.*?\])\s*```", RegexOptions.Singleline);
            if (jsonMatch.Success)
                jsonResponse = jsonMatch.Groups[1].Value;
            
            var themes = JsonSerializer.Deserialize<List<AIThematicChallenge>>(jsonResponse, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            
            if (themes != null)
            {
                return themes.Select(t => new ThematicChallenge
                {
                    Theme = t.Theme ?? "Unknown",
                    Keywords = t.Keywords ?? new List<string>(),
                    MentionCount = t.MentionCount,
                    KeyIssues = t.KeyIssues ?? new List<string>(),
                    Impact = t.Impact ?? ""
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ Thematic analysis failed: {ex.Message}");
        }
        
        // Fallback themes
        return new List<ThematicChallenge>
        {
            new ThematicChallenge
            {
                Theme = "Workforce Sustainability",
                Keywords = new List<string> { "staffing", "recruitment", "retention" },
                MentionCount = 0,
                KeyIssues = new List<string> { "Recruitment challenges", "Retention issues" },
                Impact = "Affects service delivery capacity"
            }
        };
    }

    private async Task<string> GenerateAIExecutiveSummary(List<OrganizationInsight> insights, SentimentDistribution dist)
    {
        var highRisk = insights.Count(i => i.RiskLevel == "High");
        var avgSentiment = insights.Average(i => i.AverageSentiment);
        
        var prompt = $@"Generate a concise executive summary for a grantee analysis report.

KEY METRICS:
- Total Organizations: {insights.Count}
- High Risk Organizations: {highRisk}
- Average Sentiment: {avgSentiment:F3}
- Positive Comments: {dist.PositiveCount}
- Negative Comments: {dist.NegativeCount}

Write a 2-3 paragraph executive summary highlighting:
1. Overall state of the grantee network
2. Key challenges identified
3. Priority actions needed

Keep it professional and actionable for program reviewers.";

        try
        {
            return await CallAIWithPrompt(prompt);
        }
        catch
        {
            return $"Analysis of {insights.Count} organizations reveals {highRisk} high-risk organizations requiring immediate attention. " +
                   $"Average sentiment score of {avgSentiment:F3} indicates moderate challenges across the network.";
        }
    }

    private async Task<List<string>> GenerateAIKeyFindings(List<OrganizationInsight> insights, List<ThematicChallenge> themes)
    {
        var findings = new List<string>
        {
            $"{insights.Count(i => i.RiskLevel == "High")} organizations identified as high-risk requiring immediate intervention",
            $"Most common challenges: {string.Join(", ", themes.Take(3).Select(t => t.Theme))}",
            $"Average sentiment across network: {insights.Average(i => i.AverageSentiment):F3}"
        };
        
        return findings;
    }

    private SentimentDistribution CalculateSentimentDistribution(List<double> sentimentScores)
    {
        var positive = sentimentScores.Count(s => s >= 0.05);
        var neutral = sentimentScores.Count(s => s > -0.05 && s < 0.05);
        var negative = sentimentScores.Count(s => s <= -0.05);
        var total = sentimentScores.Count;
        
        return new SentimentDistribution
        {
            PositiveCount = positive,
            NeutralCount = neutral,
            NegativeCount = negative,
            PositivePercentage = total > 0 ? (double)positive / total * 100 : 0,
            NeutralPercentage = total > 0 ? (double)neutral / total * 100 : 0,
            NegativePercentage = total > 0 ? (double)negative / total * 100 : 0
        };
    }

    // Helper classes for JSON deserialization
    private class AIOrganizationAnalysis
    {
        [JsonPropertyName("sentiment_score")]
        public double SentimentScore { get; set; }
        
        [JsonPropertyName("risk_level")]
        public string? RiskLevel { get; set; }
        
        [JsonPropertyName("top_challenges")]
        public List<string>? TopChallenges { get; set; }
        
        [JsonPropertyName("contextual_background")]
        public string? ContextualBackground { get; set; }
        
        [JsonPropertyName("positive_aspects")]
        public List<string>? PositiveAspects { get; set; }
        
        [JsonPropertyName("detailed_challenges")]
        public List<AIDetailedChallenge>? DetailedChallenges { get; set; }
        
        [JsonPropertyName("specific_recommendations")]
        public List<string>? SpecificRecommendations { get; set; }
        
        [JsonPropertyName("reviewer_notes")]
        public string? ReviewerNotes { get; set; }
    }

    private class AIDetailedChallenge
    {
        [JsonPropertyName("challenge")]
        public string? Challenge { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("impact")]
        public string? Impact { get; set; }
        
        [JsonPropertyName("suggested_remedy")]
        public string? SuggestedRemedy { get; set; }
        
        [JsonPropertyName("action_steps")]
        public List<string>? ActionSteps { get; set; }
        
        [JsonPropertyName("timeline")]
        public string? Timeline { get; set; }
        
        [JsonPropertyName("responsible_party")]
        public string? ResponsibleParty { get; set; }
        
        [JsonPropertyName("evidence_quote")]
        public string? EvidenceQuote { get; set; }
    }

    private class AIThematicChallenge
    {
        [JsonPropertyName("theme")]
        public string? Theme { get; set; }
        
        [JsonPropertyName("keywords")]
        public List<string>? Keywords { get; set; }
        
        [JsonPropertyName("mention_count")]
        public int MentionCount { get; set; }
        
        [JsonPropertyName("key_issues")]
        public List<string>? KeyIssues { get; set; }
        
        [JsonPropertyName("impact")]
        public string? Impact { get; set; }
    }
}
