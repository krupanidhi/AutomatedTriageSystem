using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Core.Models;
using Microsoft.Extensions.Logging;

namespace ExcelAnalysis.Infrastructure.Services;

/// <summary>
/// Hybrid analyzer combining Claude AI and Sentence Transformers semantic analysis
/// </summary>
public class HybridAnalyzer
{
    private readonly IExcelProcessor _excelProcessor;
    private readonly EnhancedAIAnalyzer _claudeAnalyzer;
    private readonly SemanticAnalyzerService _semanticService;
    private readonly ILogger<HybridAnalyzer> _logger;

    public HybridAnalyzer(
        IExcelProcessor excelProcessor,
        EnhancedAIAnalyzer claudeAnalyzer,
        SemanticAnalyzerService semanticService,
        ILogger<HybridAnalyzer> logger)
    {
        _excelProcessor = excelProcessor;
        _claudeAnalyzer = claudeAnalyzer;
        _semanticService = semanticService;
        _logger = logger;
    }

    public async Task<HybridAnalysisResult> AnalyzeWithBothModelsAsync(ExcelFileInfo fileInfo)
    {
        _logger.LogInformation($"🔄 Starting Hybrid Analysis for: {fileInfo.FileName}");
        
        var result = new HybridAnalysisResult
        {
            AnalyzedAt = DateTime.UtcNow,
            FileName = fileInfo.FileName
        };

        // Extract data once for both analyzers
        var (comments, questions) = await _excelProcessor.ExtractCommentsAndQuestionsAsync(fileInfo);
        result.TotalResponsesAnalyzed = comments.Count;

        _logger.LogInformation($"   📊 Extracted {comments.Count} comments");

        // Run both analyses in parallel
        var claudeTask = RunClaudeAnalysisAsync(fileInfo);
        var semanticTask = RunSemanticAnalysisAsync(comments);

        await Task.WhenAll(claudeTask, semanticTask);

        result.ClaudeResults = await claudeTask;
        result.SemanticResults = await semanticTask;

        // Integrate results
        result.IntegratedOrganizationInsights = IntegrateOrganizationInsights(
            result.ClaudeResults, 
            result.SemanticResults
        );

        result.IntegratedThemes = IntegrateThemes(
            result.ClaudeResults,
            result.SemanticResults
        );

        result.TotalGranteesAnalyzed = result.IntegratedOrganizationInsights.Count;

        // Generate integrated executive summary
        result.ExecutiveSummary = GenerateIntegratedExecutiveSummary(result);
        result.KeyFindings = GenerateIntegratedKeyFindings(result);

        _logger.LogInformation("   ✅ Hybrid analysis complete!");

        return result;
    }

    private async Task<EnhancedAnalysisResult?> RunClaudeAnalysisAsync(ExcelFileInfo fileInfo)
    {
        try
        {
            _logger.LogInformation("   🤖 Running Claude AI analysis...");
            var result = await _claudeAnalyzer.AnalyzeGranteeDataWithAIAsync(fileInfo);
            _logger.LogInformation("   ✅ Claude analysis complete");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"   ⚠️ Claude analysis failed: {ex.Message}");
            return null;
        }
    }

    private async Task<SemanticAnalysisResult?> RunSemanticAnalysisAsync(List<CommentData> comments)
    {
        try
        {
            // Check if semantic service is available
            var isAvailable = await _semanticService.IsServiceAvailableAsync();
            if (!isAvailable)
            {
                _logger.LogWarning("   ⚠️ Semantic service not available");
                return null;
            }

            _logger.LogInformation("   🧠 Running Semantic analysis...");
            var result = await _semanticService.AnalyzeCommentsAsync(comments);
            _logger.LogInformation("   ✅ Semantic analysis complete");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"   ⚠️ Semantic analysis failed: {ex.Message}");
            return null;
        }
    }

    private List<HybridOrganizationInsight> IntegrateOrganizationInsights(
        EnhancedAnalysisResult? claudeResults,
        SemanticAnalysisResult? semanticResults)
    {
        var integrated = new List<HybridOrganizationInsight>();

        // Get all unique organizations from both sources (excluding empty/null names)
        var allOrgs = new HashSet<string>();
        
        if (claudeResults?.LowestSentimentOrganizations != null)
            allOrgs.UnionWith(claudeResults.LowestSentimentOrganizations
                .Select(o => o.OrganizationName)
                .Where(name => !string.IsNullOrWhiteSpace(name)));
        
        if (semanticResults?.OrganizationInsights != null)
            allOrgs.UnionWith(semanticResults.OrganizationInsights.Keys
                .Where(name => !string.IsNullOrWhiteSpace(name)));

        foreach (var orgName in allOrgs)
        {
            var insight = new HybridOrganizationInsight
            {
                OrganizationName = orgName
            };

            // Add Claude data
            var claudeOrg = claudeResults?.LowestSentimentOrganizations?
                .FirstOrDefault(o => o.OrganizationName == orgName);
            
            if (claudeOrg != null)
            {
                insight.ClaudeSentiment = claudeOrg.AverageSentiment;
                insight.ClaudeRiskLevel = claudeOrg.RiskLevel;
                insight.ClaudeTopChallenges = claudeOrg.TopChallenges;
                insight.ClaudeDetailedChallenges = claudeOrg.DetailedChallenges;
                insight.ClaudeRecommendations = claudeOrg.SpecificRecommendations;
                insight.TotalComments = claudeOrg.TotalComments;
                insight.ActionNeeded = claudeOrg.ActionNeeded;
            }

            // Add Semantic data
            if (semanticResults?.OrganizationInsights?.TryGetValue(orgName, out var semanticOrg) == true)
            {
                insight.SemanticCoherence = semanticOrg.CoherenceScore;
                insight.SemanticKeywords = semanticOrg.TopKeywords;
                
                if (insight.TotalComments == 0)
                    insight.TotalComments = semanticOrg.CommentCount;
            }

            // Find which semantic theme this org belongs to
            if (semanticResults?.Themes != null)
            {
                var orgKeywords = insight.SemanticKeywords;
                var matchingTheme = semanticResults.Themes
                    .OrderByDescending(t => t.Keywords.Intersect(orgKeywords).Count())
                    .FirstOrDefault();
                
                if (matchingTheme != null)
                {
                    insight.SemanticThemeId = matchingTheme.ThemeId;
                    insight.SemanticThemeName = matchingTheme.ThemeName;
                }
            }

            // Integrated risk assessment
            insight.IntegratedRiskAssessment = DetermineIntegratedRisk(
                insight.ClaudeRiskLevel,
                insight.ClaudeSentiment,
                insight.SemanticCoherence
            );

            integrated.Add(insight);
        }

        return integrated.OrderBy(i => i.ClaudeSentiment).ToList();
    }

    private List<HybridTheme> IntegrateThemes(
        EnhancedAnalysisResult? claudeResults,
        SemanticAnalysisResult? semanticResults)
    {
        var integrated = new List<HybridTheme>();

        // Start with Claude themes
        if (claudeResults?.ThematicChallenges != null)
        {
            foreach (var claudeTheme in claudeResults.ThematicChallenges)
            {
                var theme = new HybridTheme
                {
                    ThemeName = claudeTheme.Theme,
                    ClaudeKeyIssues = claudeTheme.KeyIssues,
                    ClaudeImpact = claudeTheme.Impact,
                    TotalMentions = claudeTheme.MentionCount
                };

                // Try to find matching semantic theme
                if (semanticResults?.Themes != null)
                {
                    var matchingSemanticTheme = semanticResults.Themes
                        .OrderByDescending(st => st.Keywords.Intersect(claudeTheme.Keywords).Count())
                        .FirstOrDefault();

                    if (matchingSemanticTheme != null)
                    {
                        theme.SemanticCommentCount = matchingSemanticTheme.CommentCount;
                        theme.SemanticKeywords = matchingSemanticTheme.Keywords;
                        theme.SemanticRepresentativeComment = matchingSemanticTheme.RepresentativeComment;
                        theme.TotalMentions = Math.Max(theme.TotalMentions, matchingSemanticTheme.CommentCount);
                    }
                }

                theme.IntegratedDescription = GenerateIntegratedThemeDescription(theme);
                integrated.Add(theme);
            }
        }

        // Add any semantic themes not matched with Claude
        if (semanticResults?.Themes != null)
        {
            foreach (var semanticTheme in semanticResults.Themes)
            {
                if (!integrated.Any(t => t.SemanticKeywords.Intersect(semanticTheme.Keywords).Any()))
                {
                    integrated.Add(new HybridTheme
                    {
                        ThemeName = semanticTheme.ThemeName,
                        SemanticCommentCount = semanticTheme.CommentCount,
                        SemanticKeywords = semanticTheme.Keywords,
                        SemanticRepresentativeComment = semanticTheme.RepresentativeComment,
                        TotalMentions = semanticTheme.CommentCount,
                        IntegratedDescription = $"Semantic clustering identified this theme with {semanticTheme.CommentCount} related comments."
                    });
                }
            }
        }

        return integrated.OrderByDescending(t => t.TotalMentions).ToList();
    }

    private string DetermineIntegratedRisk(string claudeRisk, double sentiment, double coherence)
    {
        var riskFactors = new List<string>();

        // Claude risk assessment
        if (!string.IsNullOrEmpty(claudeRisk))
            riskFactors.Add($"Claude: {claudeRisk}");

        // Sentiment-based risk
        if (sentiment < -0.3)
            riskFactors.Add("Low sentiment");
        else if (sentiment < 0.1)
            riskFactors.Add("Moderate sentiment");

        // Coherence-based risk (low coherence = diverse/conflicting issues)
        if (coherence < 0.5)
            riskFactors.Add("Low coherence (diverse issues)");
        else if (coherence < 0.7)
            riskFactors.Add("Moderate coherence");

        if (riskFactors.Count == 0)
            return "Low risk - stable operations";

        return string.Join("; ", riskFactors);
    }

    private string GenerateIntegratedThemeDescription(HybridTheme theme)
    {
        var parts = new List<string>();

        if (theme.ClaudeKeyIssues.Any())
            parts.Add($"Claude identified: {string.Join(", ", theme.ClaudeKeyIssues.Take(2))}");

        if (theme.SemanticKeywords.Any())
            parts.Add($"Semantic analysis found {theme.SemanticCommentCount} comments with keywords: {string.Join(", ", theme.SemanticKeywords.Take(3))}");

        if (!string.IsNullOrEmpty(theme.ClaudeImpact))
            parts.Add($"Impact: {theme.ClaudeImpact}");

        return string.Join(". ", parts);
    }

    private string GenerateIntegratedExecutiveSummary(HybridAnalysisResult result)
    {
        var summary = new List<string>();

        summary.Add($"## Hybrid Analysis Executive Summary");
        summary.Add($"");
        summary.Add($"Analysis of {result.TotalGranteesAnalyzed} organizations with {result.TotalResponsesAnalyzed} responses using both Claude AI and Sentence Transformers semantic analysis.");
        summary.Add($"");

        // Claude insights
        if (result.ClaudeResults != null)
        {
            var highRisk = result.IntegratedOrganizationInsights.Count(o => o.ClaudeRiskLevel == "High");
            var avgSentiment = result.IntegratedOrganizationInsights.Average(o => o.ClaudeSentiment);
            summary.Add($"**Claude AI Analysis:** Identified {highRisk} high-risk organizations. Average sentiment: {avgSentiment:F2}.");
        }

        // Semantic insights
        if (result.SemanticResults != null)
        {
            summary.Add($"**Semantic Analysis:** Discovered {result.SemanticResults.Themes.Count} distinct thematic clusters. Pattern: {result.SemanticResults.SentimentDistribution.Pattern}.");
        }

        // Integrated findings
        summary.Add($"");
        summary.Add($"**Key Integrated Findings:**");
        summary.Add($"- {result.IntegratedThemes.Count} major themes identified across both analyses");
        summary.Add($"- Organizations with low coherence scores may have diverse or conflicting challenges requiring individualized attention");
        summary.Add($"- Cross-model validation provides higher confidence in identified patterns");

        return string.Join("\n", summary);
    }

    private List<string> GenerateIntegratedKeyFindings(HybridAnalysisResult result)
    {
        var findings = new List<string>();

        // Top themes from both models
        var topThemes = result.IntegratedThemes.Take(3).Select(t => t.ThemeName);
        findings.Add($"Top themes (both models): {string.Join(", ", topThemes)}");

        // High-risk organizations
        var highRiskCount = result.IntegratedOrganizationInsights.Count(o => o.ClaudeRiskLevel == "High");
        findings.Add($"{highRiskCount} organizations identified as high-risk requiring immediate intervention");

        // Semantic patterns
        if (result.SemanticResults != null)
        {
            findings.Add($"Semantic analysis pattern: {result.SemanticResults.SentimentDistribution.Pattern}");
        }

        // Average sentiment
        var avgSentiment = result.IntegratedOrganizationInsights
            .Where(o => o.ClaudeSentiment != 0)
            .Average(o => o.ClaudeSentiment);
        findings.Add($"Average sentiment across network: {avgSentiment:F3}");

        return findings;
    }
}
