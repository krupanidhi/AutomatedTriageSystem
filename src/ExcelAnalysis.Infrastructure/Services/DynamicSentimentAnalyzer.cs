using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Core.Models;

namespace ExcelAnalysis.Infrastructure.Services;

/// <summary>
/// Sentiment analyzer that uses dynamically extracted buzzwords from the actual Excel file
/// </summary>
public class DynamicSentimentAnalyzer
{
    private readonly Dictionary<string, int> _negativeKeywords;
    private readonly Dictionary<string, int> _positiveKeywords;
    private readonly Dictionary<string, int> _neutralKeywords;
    private readonly bool _useDynamic;

    public DynamicSentimentAnalyzer(
        Dictionary<string, int>? negativeKeywords = null,
        Dictionary<string, int>? positiveKeywords = null,
        Dictionary<string, int>? neutralKeywords = null)
    {
        _negativeKeywords = negativeKeywords ?? new Dictionary<string, int>();
        _positiveKeywords = positiveKeywords ?? new Dictionary<string, int>();
        _neutralKeywords = neutralKeywords ?? new Dictionary<string, int>();
        _useDynamic = negativeKeywords != null && negativeKeywords.Any();
    }

    public double CalculateSentiment(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        // If we have dynamic keywords, use them with higher priority
        if (_useDynamic)
        {
            return CalculateWithDynamicKeywords(text);
        }

        // Fall back to static keywords
        return SentimentKeywords.CalculateSentimentScore(text);
    }

    private double CalculateWithDynamicKeywords(string text)
    {
        var lowerText = text.ToLowerInvariant();
        var words = lowerText.Split(new[] { ' ', ',', '.', ';', ':', '!', '?', '\n', '\r', '\t' }, 
            StringSplitOptions.RemoveEmptyEntries);

        double negativeScore = 0;
        double positiveScore = 0;
        int totalMatches = 0;

        // Check each word against dynamic keywords
        foreach (var word in words)
        {
            // Check negative keywords (weighted by frequency in original data)
            if (_negativeKeywords.TryGetValue(word, out var negFreq))
            {
                negativeScore += Math.Log(negFreq + 1); // Log scale to prevent domination
                totalMatches++;
            }

            // Check positive keywords
            if (_positiveKeywords.TryGetValue(word, out var posFreq))
            {
                positiveScore += Math.Log(posFreq + 1);
                totalMatches++;
            }
        }

        // Check multi-word phrases
        foreach (var phrase in _negativeKeywords.Keys.Where(k => k.Contains(' ')))
        {
            if (lowerText.Contains(phrase))
            {
                negativeScore += Math.Log(_negativeKeywords[phrase] + 1) * 2; // Phrases get 2x weight
                totalMatches++;
            }
        }

        foreach (var phrase in _positiveKeywords.Keys.Where(k => k.Contains(' ')))
        {
            if (lowerText.Contains(phrase))
            {
                positiveScore += Math.Log(_positiveKeywords[phrase] + 1) * 2;
                totalMatches++;
            }
        }

        // If no matches, fall back to static keywords
        if (totalMatches == 0)
        {
            return SentimentKeywords.CalculateSentimentScore(text);
        }

        // Calculate score: -1 (very negative) to +1 (very positive)
        double totalScore = positiveScore - negativeScore;
        double maxPossible = Math.Max(positiveScore + negativeScore, 1);
        
        return Math.Clamp(totalScore / maxPossible, -1, 1);
    }

    public string GetSentimentLabel(double score)
    {
        return score switch
        {
            >= 0.5 => "Very Positive",
            >= 0.2 => "Positive",
            >= -0.2 => "Neutral",
            >= -0.5 => "Negative",
            _ => "Very Negative"
        };
    }

    public static async Task<DynamicSentimentAnalyzer> CreateFromFileAsync(
        ExcelFileInfo fileInfo, 
        IExcelProcessor excelProcessor,
        int minFrequency = 2)
    {
        Console.WriteLine($"📊 Extracting buzzwords for dynamic sentiment analysis...");
        
        // Extract comments
        var (comments, _) = await excelProcessor.ExtractCommentsAndQuestionsAsync(fileInfo);
        
        // Extract buzzwords
        var analysis = BuzzwordExtractor.ExtractBuzzwords(comments, minFrequency);
        
        Console.WriteLine($"   ✅ Extracted {analysis.AllKeywords.Count} keywords");
        Console.WriteLine($"      🔴 Negative: {analysis.NegativeKeywords.Count}");
        Console.WriteLine($"      🟢 Positive: {analysis.PositiveKeywords.Count}");
        Console.WriteLine($"   💭 Using dynamic keywords for sentiment analysis");
        
        return new DynamicSentimentAnalyzer(
            analysis.NegativeKeywords,
            analysis.PositiveKeywords,
            analysis.NeutralKeywords
        );
    }
}
