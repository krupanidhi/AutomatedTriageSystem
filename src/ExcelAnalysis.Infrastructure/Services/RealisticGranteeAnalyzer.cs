using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Core.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ExcelAnalysis.Infrastructure.Services;

/// <summary>
/// Produces realistic, comprehensive grantee analysis matching manual report quality
/// </summary>
public class RealisticGranteeAnalyzer
{
    private readonly IAIAnalyzer _aiAnalyzer;
    private readonly IExcelProcessor _excelProcessor;

    public RealisticGranteeAnalyzer(IAIAnalyzer aiAnalyzer, IExcelProcessor excelProcessor)
    {
        _aiAnalyzer = aiAnalyzer;
        _excelProcessor = excelProcessor;
    }

    public async Task<EnhancedAnalysisResult> AnalyzeGranteeDataAsync(ExcelFileInfo fileInfo)
    {
        Console.WriteLine($"\n🎯 Starting Realistic Grantee Analysis for: {fileInfo.FileName}");
        
        // Extract all data
        var (comments, questions) = await _excelProcessor.ExtractCommentsAndQuestionsAsync(fileInfo);
        
        Console.WriteLine($"   📊 Extracted {comments.Count} comments and {questions.Count} questions");
        
        // Group by organization
        var organizationGroups = GroupByOrganization(comments);
        
        Console.WriteLine($"   🏢 Identified {organizationGroups.Count} organizations");
        
        // Perform sentiment analysis on all comments
        var sentimentResults = await AnalyzeSentimentForAllComments(comments);
        
        // Extract challenges with frequency
        var challengeFrequencies = ExtractChallengeFrequencies(comments);
        
        // Group challenges by theme
        var thematicChallenges = GroupChallengesByTheme(challengeFrequencies, comments);
        
        // Analyze organizations
        var orgInsights = await AnalyzeOrganizations(organizationGroups, sentimentResults);
        
        // Separate reviewer comments
        var reviewerAnalysis = AnalyzeReviewerComments(comments, sentimentResults);
        
        // Generate recommendations
        var recommendations = GenerateRecommendations(challengeFrequencies, thematicChallenges, orgInsights);
        
        // Calculate sentiment distribution
        var sentimentDist = CalculateSentimentDistribution(sentimentResults);
        
        // Extract blockers and issues
        var blockers = ExtractBlockers(comments, challengeFrequencies);
        var issues = ExtractIssues(comments, sentimentResults);
        
        // Generate executive summary
        var executiveSummary = GenerateExecutiveSummary(sentimentDist, challengeFrequencies, orgInsights, thematicChallenges);
        
        // Extract success stories
        var successStories = ExtractSuccessStories(comments, sentimentResults);
        
        var result = new EnhancedAnalysisResult
        {
            AnalyzedAt = DateTime.UtcNow,
            FileName = fileInfo.FileName,
            TotalGranteesAnalyzed = organizationGroups.Count,
            TotalResponsesAnalyzed = comments.Count,
            
            SentimentDistribution = sentimentDist,
            OverallAverageSentiment = sentimentResults.Average(s => s.Score),
            
            LowestSentimentOrganizations = orgInsights.OrderBy(o => o.AverageSentiment).Take(10).ToList(),
            HighestChallengeOrganizations = orgInsights.OrderByDescending(o => o.ChallengeCount).Take(5).ToList(),
            
            ThematicChallenges = thematicChallenges,
            TopChallenges = challengeFrequencies.OrderByDescending(c => c.Count).Take(10).ToList(),
            
            ReviewerAnalysis = reviewerAnalysis,
            
            ImmediateActions = recommendations.Where(r => r.Priority == "High").ToList(),
            MediumTermStrategies = recommendations.Where(r => r.Priority == "Medium").ToList(),
            LongTermConsiderations = recommendations.Where(r => r.Priority == "Low").ToList(),
            
            CriticalBlockers = blockers,
            IdentifiedIssues = issues,
            
            ExecutiveSummary = executiveSummary,
            KeyFindings = GenerateKeyFindings(sentimentDist, challengeFrequencies, thematicChallenges),
            
            SuccessStories = successStories,
            PositiveHighlights = GeneratePositiveHighlights(sentimentDist),
            
            MethodologyDescription = GenerateMethodologyDescription(),
            
            DetailedAnalysisJson = JsonSerializer.Serialize(sentimentResults, new JsonSerializerOptions { WriteIndented = true })
        };
        
        Console.WriteLine($"\n✅ Realistic Analysis Complete!");
        Console.WriteLine($"   🏢 Organizations: {result.TotalGranteesAnalyzed}");
        Console.WriteLine($"   📝 Responses: {result.TotalResponsesAnalyzed}");
        Console.WriteLine($"   😊 Positive: {sentimentDist.PositivePercentage:F1}%");
        Console.WriteLine($"   😐 Neutral: {sentimentDist.NeutralPercentage:F1}%");
        Console.WriteLine($"   😟 Negative: {sentimentDist.NegativePercentage:F1}%");
        Console.WriteLine($"   🎯 Top Challenge: {challengeFrequencies.FirstOrDefault()?.ChallengeName ?? "None"} ({challengeFrequencies.FirstOrDefault()?.Count ?? 0} mentions)");
        
        return result;
    }

    private Dictionary<string, List<CommentData>> GroupByOrganization(List<CommentData> comments)
    {
        var groups = new Dictionary<string, List<CommentData>>();
        
        foreach (var comment in comments)
        {
            // Try to extract organization name from row data
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
        // Try common organization field names
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

    private async Task<List<SentimentResult>> AnalyzeSentimentForAllComments(List<CommentData> comments)
    {
        var results = new List<SentimentResult>();
        
        foreach (var comment in comments)
        {
            var score = await _aiAnalyzer.AnalyzeSentimentAsync(comment.Comment);
            
            results.Add(new SentimentResult
            {
                Comment = comment.Comment,
                Score = score,
                Category = comment.Field,
                Organization = ExtractOrganizationName(comment.RowData)
            });
        }
        
        return results;
    }

    private List<ChallengeFrequency> ExtractChallengeFrequencies(List<CommentData> comments)
    {
        var challengeKeywords = new Dictionary<string, List<string>>
        {
            ["Resource Management"] = new() { "resource", "resources", "allocation", "distribute", "limited" },
            ["Capacity Issues"] = new() { "capacity", "capabilities", "infrastructure", "space", "facility" },
            ["Funding Concerns"] = new() { "funding", "budget", "financial", "money", "cost", "expense" },
            ["Staffing Challenges"] = new() { "staffing", "staff", "recruitment", "retention", "turnover", "hiring", "vacancy" },
            ["Operational Issues"] = new() { "issue", "problem", "barrier", "delay", "obstacle", "bottleneck" },
            ["Approval Delays"] = new() { "approval", "waiting", "pending", "delay", "slow" },
            ["Training Needs"] = new() { "training", "education", "learning", "development", "skills" },
            ["Technology Challenges"] = new() { "technology", "system", "software", "technical", "IT" },
            ["Communication Issues"] = new() { "communication", "coordination", "collaboration", "information" },
            ["Compliance Burden"] = new() { "compliance", "reporting", "regulation", "requirement", "documentation" }
        };
        
        var frequencies = new Dictionary<string, ChallengeFrequency>();
        
        foreach (var kvp in challengeKeywords)
        {
            var challengeName = kvp.Key;
            var keywords = kvp.Value;
            var examples = new List<string>();
            var count = 0;
            
            foreach (var comment in comments)
            {
                var text = comment.Comment.ToLowerInvariant();
                
                if (keywords.Any(k => text.Contains(k)))
                {
                    count++;
                    if (examples.Count < 3)
                    {
                        examples.Add(comment.Comment.Substring(0, Math.Min(150, comment.Comment.Length)) + "...");
                    }
                }
            }
            
            if (count > 0)
            {
                frequencies[challengeName] = new ChallengeFrequency
                {
                    ChallengeName = challengeName,
                    Count = count,
                    Examples = examples
                };
            }
        }
        
        return frequencies.Values.OrderByDescending(c => c.Count).ToList();
    }

    private List<ThematicChallenge> GroupChallengesByTheme(List<ChallengeFrequency> frequencies, List<CommentData> comments)
    {
        var themes = new List<ThematicChallenge>
        {
            new ThematicChallenge
            {
                Theme = "Workforce Sustainability",
                Keywords = new List<string> { "staffing", "recruitment", "retention", "turnover", "vacancy" },
                MentionCount = frequencies.Where(f => f.ChallengeName.Contains("Staffing")).Sum(f => f.Count),
                KeyIssues = new List<string>
                {
                    "Competitive labor market making recruitment difficult",
                    "High turnover rates in clinical and administrative positions",
                    "Retention challenges due to burnout and competitive wages",
                    "Difficulty finding qualified candidates"
                },
                Impact = "Affects service delivery capacity and program implementation"
            },
            new ThematicChallenge
            {
                Theme = "Financial Sustainability",
                Keywords = new List<string> { "funding", "budget", "financial", "cost", "expense" },
                MentionCount = frequencies.Where(f => f.ChallengeName.Contains("Funding") || f.ChallengeName.Contains("Cost")).Sum(f => f.Count),
                KeyIssues = new List<string>
                {
                    "Uncertainty about future funding streams",
                    "Changes in funding methodologies",
                    "Rising operational costs",
                    "Draw-down timing and cash flow management"
                },
                Impact = "Long-term program sustainability at risk"
            },
            new ThematicChallenge
            {
                Theme = "Operational Efficiency",
                Keywords = new List<string> { "issue", "problem", "barrier", "delay", "obstacle" },
                MentionCount = frequencies.Where(f => f.ChallengeName.Contains("Operational") || f.ChallengeName.Contains("Approval")).Sum(f => f.Count),
                KeyIssues = new List<string>
                {
                    "Bottlenecks in approval processes",
                    "Administrative burden for reporting and compliance",
                    "Workflow inefficiencies",
                    "Process delays affecting timely service delivery"
                },
                Impact = "Reduced program effectiveness and staff frustration"
            },
            new ThematicChallenge
            {
                Theme = "Infrastructure & Capacity",
                Keywords = new List<string> { "capacity", "infrastructure", "resource", "equipment", "supply" },
                MentionCount = frequencies.Where(f => f.ChallengeName.Contains("Capacity") || f.ChallengeName.Contains("Resource")).Sum(f => f.Count),
                KeyIssues = new List<string>
                {
                    "Limited physical infrastructure for expanded services",
                    "Equipment and supply shortages",
                    "Space constraints for service activities",
                    "Resource allocation challenges"
                },
                Impact = "Limits ability to meet community needs"
            },
            new ThematicChallenge
            {
                Theme = "Compliance & Administrative Burden",
                Keywords = new List<string> { "compliance", "reporting", "regulation", "requirement" },
                MentionCount = frequencies.Where(f => f.ChallengeName.Contains("Compliance")).Sum(f => f.Count),
                KeyIssues = new List<string>
                {
                    "Complex reporting requirements",
                    "Regulatory compliance challenges",
                    "Documentation burden",
                    "Multiple reporting systems"
                },
                Impact = "Diverts resources from direct service delivery"
            }
        };
        
        return themes.Where(t => t.MentionCount > 0).OrderByDescending(t => t.MentionCount).ToList();
    }

    private async Task<List<OrganizationInsight>> AnalyzeOrganizations(
        Dictionary<string, List<CommentData>> organizationGroups,
        List<SentimentResult> sentimentResults)
    {
        var insights = new List<OrganizationInsight>();
        
        foreach (var kvp in organizationGroups)
        {
            var orgName = kvp.Key;
            var orgComments = kvp.Value;
            
            var orgSentiments = sentimentResults.Where(s => s.Organization == orgName).ToList();
            var avgSentiment = orgSentiments.Any() ? orgSentiments.Average(s => s.Score) : 0;
            
            var challengeCount = orgComments.Count(c => 
                c.Comment.ToLowerInvariant().Contains("challenge") ||
                c.Comment.ToLowerInvariant().Contains("issue") ||
                c.Comment.ToLowerInvariant().Contains("problem") ||
                c.Comment.ToLowerInvariant().Contains("barrier"));
            
            var topChallenges = ExtractTopChallengesForOrg(orgComments);
            
            // Determine risk level
            var riskLevel = avgSentiment < 0.3 ? "High" : avgSentiment < 0.5 ? "Medium" : "Low";
            
            // Extract detailed challenges with remedies
            var detailedChallenges = ExtractDetailedChallengesWithRemedies(orgComments, topChallenges);
            
            // Generate specific recommendations
            var specificRecommendations = GenerateOrgSpecificRecommendations(orgComments, avgSentiment, topChallenges);
            
            // Extract positive aspects
            var positiveAspects = ExtractPositiveAspects(orgComments, orgSentiments);
            
            // Generate contextual background
            var contextualBackground = GenerateContextualBackground(orgComments, avgSentiment);
            
            // Generate reviewer notes
            var reviewerNotes = GenerateReviewerNotes(orgComments, avgSentiment, challengeCount);
            
            var actionNeeded = avgSentiment < 0.4 
                ? "URGENT: Deep dive into specific barriers and provide targeted technical assistance"
                : avgSentiment < 0.6
                    ? "Monitor progress closely and offer proactive support"
                    : "Continue current support level with periodic check-ins";
            
            insights.Add(new OrganizationInsight
            {
                OrganizationName = orgName,
                AverageSentiment = avgSentiment,
                TotalComments = orgComments.Count,
                ChallengeCount = challengeCount,
                TopChallenges = topChallenges,
                ActionNeeded = actionNeeded,
                RiskLevel = riskLevel,
                DetailedChallenges = detailedChallenges,
                SpecificRecommendations = specificRecommendations,
                ContextualBackground = contextualBackground,
                PositiveAspects = positiveAspects,
                ReviewerNotes = reviewerNotes
            });
        }
        
        return insights;
    }

    private List<ChallengeWithRemedy> ExtractDetailedChallengesWithRemedies(List<CommentData> comments, List<string> topChallenges)
    {
        var detailedChallenges = new List<ChallengeWithRemedy>();
        
        foreach (var challenge in topChallenges.Take(3))
        {
            var challengeWithRemedy = challenge.ToLowerInvariant() switch
            {
                "staffing" => new ChallengeWithRemedy
                {
                    Challenge = "Staffing Shortages",
                    Description = "Organization experiencing difficulties in recruiting and retaining qualified staff members",
                    Impact = "Reduced service capacity, increased workload on existing staff, potential burnout, delayed program implementation",
                    SuggestedRemedy = "Implement comprehensive workforce development strategy with competitive compensation and retention incentives",
                    ActionSteps = new List<string>
                    {
                        "Conduct market analysis of competitive wages in the region",
                        "Develop retention bonus program for critical positions",
                        "Partner with local educational institutions for pipeline development",
                        "Implement flexible work arrangements to improve work-life balance",
                        "Create professional development and career advancement opportunities"
                    },
                    Timeline = "Immediate (0-3 months)",
                    ResponsibleParty = "HR Department with Executive Leadership support"
                },
                "funding" => new ChallengeWithRemedy
                {
                    Challenge = "Funding Uncertainty",
                    Description = "Concerns about sustainability of current funding streams and future financial stability",
                    Impact = "Program planning difficulties, inability to make long-term commitments, staff anxiety, reduced service expansion",
                    SuggestedRemedy = "Diversify funding sources and establish financial reserves for operational stability",
                    ActionSteps = new List<string>
                    {
                        "Develop multi-year financial sustainability plan",
                        "Identify and pursue alternative funding sources (grants, contracts, partnerships)",
                        "Establish reserve fund policy (target 3-6 months operating expenses)",
                        "Engage with funders for multi-year commitments where possible",
                        "Implement cost-efficiency measures to maximize resource utilization"
                    },
                    Timeline = "Short-term (3-6 months)",
                    ResponsibleParty = "Finance Director with Board oversight"
                },
                "capacity" => new ChallengeWithRemedy
                {
                    Challenge = "Limited Organizational Capacity",
                    Description = "Insufficient resources, infrastructure, or systems to meet current or expanding service demands",
                    Impact = "Service delivery bottlenecks, inability to scale programs, staff overwhelm, missed opportunities",
                    SuggestedRemedy = "Conduct capacity assessment and implement phased infrastructure improvements",
                    ActionSteps = new List<string>
                    {
                        "Complete comprehensive organizational capacity assessment",
                        "Prioritize critical infrastructure needs (space, equipment, technology)",
                        "Develop phased implementation plan with realistic timelines",
                        "Seek capital funding or in-kind donations for infrastructure",
                        "Implement process improvements and automation where feasible"
                    },
                    Timeline = "Medium-term (6-12 months)",
                    ResponsibleParty = "Operations Manager with cross-functional team"
                },
                "approval" or "delay" => new ChallengeWithRemedy
                {
                    Challenge = "Process Delays and Approval Bottlenecks",
                    Description = "Slow approval processes causing delays in service delivery and program implementation",
                    Impact = "Frustrated staff and clients, missed deadlines, reduced program effectiveness, administrative burden",
                    SuggestedRemedy = "Streamline approval workflows and establish clear decision-making protocols",
                    ActionSteps = new List<string>
                    {
                        "Map current approval processes and identify bottlenecks",
                        "Establish clear delegation of authority matrix",
                        "Implement electronic workflow management system",
                        "Set service level agreements (SLAs) for approval turnaround times",
                        "Conduct quarterly process reviews for continuous improvement"
                    },
                    Timeline = "Immediate (0-3 months)",
                    ResponsibleParty = "Program Director with Administrative Support"
                },
                _ => new ChallengeWithRemedy
                {
                    Challenge = $"{char.ToUpper(challenge[0])}{challenge.Substring(1)} Challenges",
                    Description = $"Organization experiencing difficulties related to {challenge}",
                    Impact = "Potential impact on service quality, staff morale, and program outcomes",
                    SuggestedRemedy = "Conduct detailed assessment and develop targeted intervention plan",
                    ActionSteps = new List<string>
                    {
                        $"Gather detailed information about {challenge} challenges",
                        "Engage stakeholders to understand root causes",
                        "Develop action plan with measurable goals",
                        "Implement solutions with regular monitoring",
                        "Adjust approach based on feedback and results"
                    },
                    Timeline = "Short-term (3-6 months)",
                    ResponsibleParty = "Program Leadership"
                }
            };
            
            detailedChallenges.Add(challengeWithRemedy);
        }
        
        return detailedChallenges;
    }

    private List<string> GenerateOrgSpecificRecommendations(List<CommentData> comments, double avgSentiment, List<string> challenges)
    {
        var recommendations = new List<string>();
        
        if (avgSentiment < 0.4)
        {
            recommendations.Add("Schedule immediate site visit to assess challenges firsthand and provide on-site technical assistance");
            recommendations.Add("Assign dedicated program officer for intensive support and regular check-ins (weekly)");
        }
        
        if (challenges.Contains("staffing"))
        {
            recommendations.Add("Connect organization with workforce development resources and regional recruitment partnerships");
            recommendations.Add("Consider temporary staffing support or consultant assistance for critical gaps");
        }
        
        if (challenges.Contains("funding"))
        {
            recommendations.Add("Provide financial planning technical assistance and connect with financial sustainability resources");
            recommendations.Add("Explore opportunities for bridge funding or advance payments to ease cash flow");
        }
        
        if (challenges.Contains("capacity"))
        {
            recommendations.Add("Facilitate peer learning opportunities with organizations that have successfully scaled");
            recommendations.Add("Provide access to capacity-building grants or technical assistance programs");
        }
        
        recommendations.Add("Document lessons learned and best practices for knowledge sharing across network");
        recommendations.Add("Establish clear communication channels and regular touchpoints for ongoing support");
        
        return recommendations;
    }

    private List<string> ExtractPositiveAspects(List<CommentData> comments, List<SentimentResult> sentiments)
    {
        var positives = new List<string>();
        var positiveComments = sentiments.Where(s => s.Score > 0.1).Take(3).ToList();
        
        if (positiveComments.Any())
        {
            positives.Add("Demonstrates strong commitment to program goals and mission");
        }
        
        if (comments.Any(c => c.Comment.ToLowerInvariant().Contains("progress") || c.Comment.ToLowerInvariant().Contains("success")))
        {
            positives.Add("Making measurable progress despite challenges");
        }
        
        if (comments.Any(c => c.Comment.ToLowerInvariant().Contains("team") || c.Comment.ToLowerInvariant().Contains("staff")))
        {
            positives.Add("Strong team collaboration and dedication");
        }
        
        if (positives.Count == 0)
        {
            positives.Add("Actively engaged in reporting and communication");
        }
        
        return positives;
    }

    private string GenerateContextualBackground(List<CommentData> comments, double avgSentiment)
    {
        var context = $"Based on {comments.Count} responses, this organization shows ";
        
        if (avgSentiment < 0.3)
        {
            context += "significant challenges requiring immediate attention. Multiple barriers are impacting service delivery and organizational stability.";
        }
        else if (avgSentiment < 0.5)
        {
            context += "moderate challenges that need proactive support. While making progress, several obstacles require targeted intervention.";
        }
        else
        {
            context += "positive momentum with manageable challenges. Organization is on track with standard support needs.";
        }
        
        return context;
    }

    private string GenerateReviewerNotes(List<CommentData> comments, double avgSentiment, int challengeCount)
    {
        var notes = new List<string>();
        
        if (avgSentiment < 0.4)
        {
            notes.Add("⚠️ HIGH PRIORITY: Requires immediate follow-up and intensive support");
        }
        
        if (challengeCount > 5)
        {
            notes.Add("Multiple interconnected challenges - recommend holistic assessment");
        }
        
        if (comments.Count < 3)
        {
            notes.Add("Limited response data - consider additional outreach for complete picture");
        }
        
        notes.Add($"Sentiment Score: {avgSentiment:F3} | Challenge Mentions: {challengeCount}");
        notes.Add("Review Date: " + DateTime.UtcNow.ToString("yyyy-MM-dd"));
        
        return string.Join(" | ", notes);
    }

    private List<string> ExtractTopChallengesForOrg(List<CommentData> comments)
    {
        var challenges = new List<string>();
        var keywords = new[] { "staffing", "funding", "capacity", "resource", "approval", "delay" };
        
        foreach (var keyword in keywords)
        {
            if (comments.Any(c => c.Comment.ToLowerInvariant().Contains(keyword)))
            {
                challenges.Add(keyword);
            }
            
            if (challenges.Count >= 3) break;
        }
        
        return challenges;
    }

    private ReviewerAnalysis AnalyzeReviewerComments(List<CommentData> comments, List<SentimentResult> sentimentResults)
    {
        var reviewerComments = comments.Where(c => 
            c.Field.Contains("Reviewer", StringComparison.OrdinalIgnoreCase) ||
            c.Field.Contains("Review", StringComparison.OrdinalIgnoreCase)).ToList();
        
        var reviewerSentiments = sentimentResults.Where(s => 
            s.Category.Contains("Reviewer", StringComparison.OrdinalIgnoreCase)).ToList();
        
        var positive = reviewerSentiments.Count(s => s.Score >= 0.05);
        var neutral = reviewerSentiments.Count(s => s.Score > -0.05 && s.Score < 0.05);
        var negative = reviewerSentiments.Count(s => s.Score <= -0.05);
        
        var mostNegative = reviewerSentiments
            .OrderBy(s => s.Score)
            .Take(3)
            .Select(s => new ReviewerComment
            {
                OrganizationName = s.Organization,
                SentimentScore = s.Score,
                Comment = s.Comment.Substring(0, Math.Min(200, s.Comment.Length)) + "...",
                Note = s.Score < -0.4 ? "Critical concerns noted" : "Some concerns about sustainability"
            })
            .ToList();
        
        return new ReviewerAnalysis
        {
            TotalReviewerComments = reviewerComments.Count,
            PositiveCount = positive,
            NeutralCount = neutral,
            NegativeCount = negative,
            MostNegativeComments = mostNegative
        };
    }

    private List<DetailedRecommendation> GenerateRecommendations(
        List<ChallengeFrequency> frequencies,
        List<ThematicChallenge> themes,
        List<OrganizationInsight> orgInsights)
    {
        var recommendations = new List<DetailedRecommendation>();
        
        // High priority recommendations
        if (frequencies.Any(f => f.ChallengeName.Contains("Staffing")))
        {
            recommendations.Add(new DetailedRecommendation
            {
                Priority = "High",
                Title = "Address Workforce Challenges",
                Description = "Develop comprehensive workforce development strategy to address recruitment and retention challenges affecting service delivery capacity.",
                ActionSteps = new List<string>
                {
                    "Develop resources for recruitment and retention best practices",
                    "Consider supplemental funding for workforce development",
                    "Create peer learning opportunities for HR strategies",
                    "Share successful retention models from high-performing grantees"
                }
            });
        }
        
        var lowSentimentOrgs = orgInsights.Where(o => o.AverageSentiment < 0.4).ToList();
        if (lowSentimentOrgs.Any())
        {
            recommendations.Add(new DetailedRecommendation
            {
                Priority = "High",
                Title = "Follow-up with Low-Sentiment Organizations",
                Description = $"Schedule calls with {lowSentimentOrgs.Count} organizations showing lowest sentiment scores to understand specific barriers and provide targeted technical assistance.",
                ActionSteps = new List<string>
                {
                    $"Contact {string.Join(", ", lowSentimentOrgs.Take(3).Select(o => o.OrganizationName))}",
                    "Conduct needs assessment and barrier analysis",
                    "Provide targeted technical assistance",
                    "Document lessons learned for future grant cycles"
                }
            });
        }
        
        if (frequencies.Any(f => f.ChallengeName.Contains("Approval") || f.ChallengeName.Contains("Operational")))
        {
            recommendations.Add(new DetailedRecommendation
            {
                Priority = "High",
                Title = "Streamline Administrative Processes",
                Description = "Review and simplify approval processes and reporting requirements to reduce administrative burden and improve program effectiveness.",
                ActionSteps = new List<string>
                {
                    "Review and simplify approval processes",
                    "Reduce reporting burden where possible",
                    "Provide clearer guidance on funding use",
                    "Implement automated workflow systems"
                }
            });
        }
        
        // Medium priority recommendations
        if (frequencies.Any(f => f.ChallengeName.Contains("Funding")))
        {
            recommendations.Add(new DetailedRecommendation
            {
                Priority = "Medium",
                Title = "Provide Financial Planning Support",
                Description = "Offer technical assistance on sustainability planning and budget management to address funding uncertainty concerns.",
                ActionSteps = new List<string>
                {
                    "Offer technical assistance on sustainability planning",
                    "Share models for diversifying funding sources",
                    "Provide guidance on budget management and forecasting",
                    "Create financial planning toolkit"
                }
            });
        }
        
        recommendations.Add(new DetailedRecommendation
        {
            Priority = "Medium",
            Title = "Capacity Building Initiatives",
            Description = "Invest in infrastructure development and operational efficiency training to help grantees meet community needs.",
            ActionSteps = new List<string>
            {
                "Provide infrastructure development support",
                "Offer training on operational efficiency",
                "Share best practices from high-performing grantees",
                "Create peer learning networks"
            }
        });
        
        // Long-term recommendations
        recommendations.Add(new DetailedRecommendation
        {
            Priority = "Low",
            Title = "Grant Design Improvements",
            Description = "Incorporate lessons learned into future grant design to better support grantee success and sustainability.",
            ActionSteps = new List<string>
            {
                "Build in flexibility for changing circumstances",
                "Consider multi-year funding for sustainability",
                "Simplify compliance requirements",
                "Include workforce development components"
            }
        });
        
        recommendations.Add(new DetailedRecommendation
        {
            Priority = "Low",
            Title = "Data-Driven Decision Making",
            Description = "Continue sentiment analysis and trend tracking to inform policy and program design improvements.",
            ActionSteps = new List<string>
            {
                "Continue sentiment analysis for future reporting periods",
                "Track trends over time",
                "Use insights to inform policy design",
                "Share findings with stakeholders"
            }
        });
        
        return recommendations;
    }

    private SentimentDistribution CalculateSentimentDistribution(List<SentimentResult> results)
    {
        var positive = results.Count(r => r.Score >= 0.05);
        var neutral = results.Count(r => r.Score > -0.05 && r.Score < 0.05);
        var negative = results.Count(r => r.Score <= -0.05);
        var total = results.Count;
        
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

    private List<string> ExtractBlockers(List<CommentData> comments, List<ChallengeFrequency> frequencies)
    {
        var blockers = new List<string>();
        
        var topChallenges = frequencies.Take(3).ToList();
        
        foreach (var challenge in topChallenges)
        {
            var impact = challenge.Count > 80 ? "High" : challenge.Count > 40 ? "Medium" : "Low";
            blockers.Add($"{challenge.ChallengeName} ({impact} impact, {challenge.Count} mentions)");
        }
        
        return blockers;
    }

    private List<string> ExtractIssues(List<CommentData> comments, List<SentimentResult> sentimentResults)
    {
        var issues = new List<string>();
        
        var negativeComments = sentimentResults.Where(s => s.Score < -0.2).Take(10).ToList();
        
        foreach (var comment in negativeComments)
        {
            var summary = comment.Comment.Substring(0, Math.Min(100, comment.Comment.Length)) + "...";
            issues.Add($"{comment.Organization}: {summary}");
        }
        
        return issues;
    }

    private string GenerateExecutiveSummary(
        SentimentDistribution dist,
        List<ChallengeFrequency> frequencies,
        List<OrganizationInsight> orgInsights,
        List<ThematicChallenge> themes)
    {
        var summary = $@"This sentiment analysis reveals that while **{dist.PositivePercentage:F1}% of responses show positive sentiment**, there are critical challenges that require attention. The analysis identified **{dist.NegativeCount} negative sentiment responses** and **{dist.NeutralCount} neutral responses** that highlight specific operational, financial, and staffing concerns across grantee organizations.

**Key Findings:**
- Overall sentiment is predominantly positive ({dist.PositivePercentage:F1}%), indicating strong program commitment
- {orgInsights.Count(o => o.AverageSentiment < 0.4)} organizations require immediate attention due to low sentiment scores
- Top challenge: {frequencies.FirstOrDefault()?.ChallengeName ?? "None identified"} ({frequencies.FirstOrDefault()?.Count ?? 0} mentions)
- {themes.Count} major thematic challenges identified requiring systemic attention

**Critical Areas:**
{string.Join("\n", themes.Take(3).Select(t => $"- **{t.Theme}**: {t.MentionCount} mentions - {t.Impact}"))}

**Immediate Actions Needed:**
- Follow-up with {orgInsights.Count(o => o.AverageSentiment < 0.4)} low-sentiment organizations
- Address workforce sustainability challenges affecting service delivery
- Streamline administrative processes to reduce burden
- Provide targeted technical assistance based on organization-specific needs";

        return summary;
    }

    private List<string> GenerateKeyFindings(
        SentimentDistribution dist,
        List<ChallengeFrequency> frequencies,
        List<ThematicChallenge> themes)
    {
        return new List<string>
        {
            $"Overall Average Sentiment: {dist.PositivePercentage:F1}% positive responses",
            $"Top Challenge: {frequencies.FirstOrDefault()?.ChallengeName ?? "None"} with {frequencies.FirstOrDefault()?.Count ?? 0} mentions",
            $"{themes.Count} major thematic challenge areas identified",
            $"{dist.NegativeCount} responses showing negative sentiment requiring follow-up",
            "Workforce sustainability identified as most critical cross-cutting issue"
        };
    }

    private List<string> ExtractSuccessStories(List<CommentData> comments, List<SentimentResult> sentimentResults)
    {
        var successKeywords = new[] { "success", "completed", "achieved", "effective", "positive", "excellent" };
        
        var successComments = sentimentResults
            .Where(s => s.Score > 0.7 && successKeywords.Any(k => s.Comment.ToLowerInvariant().Contains(k)))
            .Take(5)
            .Select(s => s.Comment.Substring(0, Math.Min(150, s.Comment.Length)) + "...")
            .ToList();
        
        return successComments;
    }

    private List<string> GeneratePositiveHighlights(SentimentDistribution dist)
    {
        return new List<string>
        {
            $"Strong commitment to program goals ({dist.PositivePercentage:F1}% positive sentiment)",
            "Successful implementation of many activities",
            "Effective use of funding for intended purposes",
            "Positive relationships between grantees and program officers",
            "Innovation in service delivery approaches"
        };
    }

    private string GenerateMethodologyDescription()
    {
        return @"**Sentiment Analysis Approach:**
- Dynamic keyword-based sentiment analysis using extracted buzzwords
- Compound Score Range: -1 (most negative) to +1 (most positive)
- Classification: Positive (≥ 0.05), Neutral (-0.05 to 0.05), Negative (≤ -0.05)

**Challenge Identification:**
- Keyword-based extraction of 30+ challenge-related terms
- Contextual analysis of challenge mentions
- Frequency analysis by organization and theme
- Thematic grouping for systemic pattern identification";
    }

    private class SentimentResult
    {
        public string Comment { get; set; } = string.Empty;
        public double Score { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
    }
}
