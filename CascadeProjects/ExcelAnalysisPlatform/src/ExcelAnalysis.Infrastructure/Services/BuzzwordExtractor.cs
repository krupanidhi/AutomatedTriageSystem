using ExcelAnalysis.Core.Models;
using ExcelAnalysis.Core.Interfaces;
using System.Text.RegularExpressions;

namespace ExcelAnalysis.Infrastructure.Services;

public class BuzzwordExtractor
{
    private static readonly HashSet<string> CommonStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
        "of", "with", "by", "from", "as", "is", "was", "are", "were", "be",
        "been", "being", "have", "has", "had", "do", "does", "did", "will",
        "would", "should", "could", "may", "might", "must", "can", "this",
        "that", "these", "those", "i", "you", "he", "she", "it", "we", "they",
        "what", "which", "who", "when", "where", "why", "how", "all", "each",
        "every", "both", "few", "more", "most", "other", "some", "such", "no",
        "nor", "not", "only", "own", "same", "so", "than", "too", "very"
    };

    public class BuzzwordAnalysis
    {
        public Dictionary<string, int> NegativeKeywords { get; set; } = new();
        public Dictionary<string, int> PositiveKeywords { get; set; } = new();
        public Dictionary<string, int> NeutralKeywords { get; set; } = new();
        public Dictionary<string, int> AllKeywords { get; set; } = new();
        public int TotalComments { get; set; }
        public int TotalWords { get; set; }
    }

    public static BuzzwordAnalysis ExtractBuzzwords(List<CommentData> comments, int minFrequency = 2)
    {
        var analysis = new BuzzwordAnalysis
        {
            TotalComments = comments.Count
        };

        // Extract all words and phrases
        var wordFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var contextMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var comment in comments)
        {
            var text = comment.Comment;
            if (string.IsNullOrWhiteSpace(text)) continue;

            // Extract single words
            var words = ExtractWords(text);
            foreach (var word in words)
            {
                if (word.Length < 3 || CommonStopWords.Contains(word)) continue;

                wordFrequency[word] = wordFrequency.GetValueOrDefault(word) + 1;
                
                if (!contextMap.ContainsKey(word))
                    contextMap[word] = new List<string>();
                contextMap[word].Add(text);
            }

            // Extract 2-word phrases
            for (int i = 0; i < words.Count - 1; i++)
            {
                var phrase = $"{words[i]} {words[i + 1]}";
                if (!CommonStopWords.Contains(words[i]) && !CommonStopWords.Contains(words[i + 1]))
                {
                    wordFrequency[phrase] = wordFrequency.GetValueOrDefault(phrase) + 1;
                    
                    if (!contextMap.ContainsKey(phrase))
                        contextMap[phrase] = new List<string>();
                    contextMap[phrase].Add(text);
                }
            }

            // Extract 3-word phrases
            for (int i = 0; i < words.Count - 2; i++)
            {
                var phrase = $"{words[i]} {words[i + 1]} {words[i + 2]}";
                if (!CommonStopWords.Contains(words[i]) && 
                    !CommonStopWords.Contains(words[i + 1]) && 
                    !CommonStopWords.Contains(words[i + 2]))
                {
                    wordFrequency[phrase] = wordFrequency.GetValueOrDefault(phrase) + 1;
                    
                    if (!contextMap.ContainsKey(phrase))
                        contextMap[phrase] = new List<string>();
                    contextMap[phrase].Add(text);
                }
            }
        }

        analysis.TotalWords = wordFrequency.Sum(kv => kv.Value);

        // Filter by minimum frequency
        var frequentWords = wordFrequency
            .Where(kv => kv.Value >= minFrequency)
            .OrderByDescending(kv => kv.Value)
            .ToList();

        // Classify words based on context and known patterns
        foreach (var (word, frequency) in frequentWords)
        {
            analysis.AllKeywords[word] = frequency;

            var sentiment = ClassifyWordSentiment(word, contextMap[word]);
            
            switch (sentiment)
            {
                case "negative":
                    analysis.NegativeKeywords[word] = frequency;
                    break;
                case "positive":
                    analysis.PositiveKeywords[word] = frequency;
                    break;
                default:
                    analysis.NeutralKeywords[word] = frequency;
                    break;
            }
        }

        return analysis;
    }

    private static List<string> ExtractWords(string text)
    {
        // Remove special characters but keep spaces and hyphens
        var cleaned = Regex.Replace(text, @"[^\w\s-]", " ");
        
        // Split into words
        var words = cleaned
            .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim().ToLowerInvariant())
            .Where(w => w.Length >= 3)
            .ToList();

        return words;
    }

    private static string ClassifyWordSentiment(string word, List<string> contexts)
    {
        var lowerWord = word.ToLowerInvariant();

        // Check against known negative patterns
        if (IsNegativeWord(lowerWord))
            return "negative";

        // Check against known positive patterns
        if (IsPositiveWord(lowerWord))
            return "positive";

        // Analyze context sentiment
        int negativeContexts = 0;
        int positiveContexts = 0;

        foreach (var context in contexts.Take(10)) // Sample contexts
        {
            var lowerContext = context.ToLowerInvariant();
            
            // Count negative indicators in context
            if (HasNegativeIndicators(lowerContext))
                negativeContexts++;
            
            // Count positive indicators in context
            if (HasPositiveIndicators(lowerContext))
                positiveContexts++;
        }

        // Classify based on context
        if (negativeContexts > positiveContexts * 1.5)
            return "negative";
        if (positiveContexts > negativeContexts * 1.5)
            return "positive";

        return "neutral";
    }

    private static bool IsNegativeWord(string word)
    {
        // Check if word contains negative patterns
        var negativePatterns = new[]
        {
            "fail", "error", "issue", "problem", "delay", "late", "behind",
            "block", "stuck", "stop", "cancel", "reject", "deny", "miss",
            "over", "under", "short", "lack", "unable", "cannot", "won't",
            "risk", "threat", "concern", "worry", "critical", "urgent",
            "poor", "bad", "wrong", "incorrect", "invalid", "broken",
            "conflict", "dispute", "disagree", "unhappy", "dissatisfied"
        };

        return negativePatterns.Any(pattern => word.Contains(pattern));
    }

    private static bool IsPositiveWord(string word)
    {
        // Check if word contains positive patterns
        var positivePatterns = new[]
        {
            "success", "complete", "done", "finish", "achieve", "accomplish",
            "good", "great", "excellent", "outstanding", "effective",
            "improve", "enhance", "optimize", "upgrade", "better",
            "progress", "advance", "ahead", "early", "timely",
            "happy", "satisfied", "pleased", "approve", "accept",
            "resolve", "fix", "solve", "work", "functional"
        };

        return positivePatterns.Any(pattern => word.Contains(pattern));
    }

    private static bool HasNegativeIndicators(string text)
    {
        var indicators = new[]
        {
            "not", "no", "never", "nothing", "nobody", "none",
            "failed", "failure", "error", "issue", "problem",
            "delayed", "late", "behind", "blocked", "stuck",
            "critical", "urgent", "risk", "concern", "worry"
        };

        return indicators.Any(indicator => text.Contains(indicator));
    }

    private static bool HasPositiveIndicators(string text)
    {
        var indicators = new[]
        {
            "completed", "finished", "done", "success", "successful",
            "good", "great", "excellent", "well", "effective",
            "improved", "better", "progress", "ahead", "on time",
            "resolved", "fixed", "working", "approved", "accepted"
        };

        return indicators.Any(indicator => text.Contains(indicator));
    }

    public static string GenerateKeywordReport(BuzzwordAnalysis analysis)
    {
        var report = new System.Text.StringBuilder();
        
        report.AppendLine("# 🔤 Extracted Buzzwords Analysis");
        report.AppendLine();
        report.AppendLine($"**Total Comments Analyzed**: {analysis.TotalComments}");
        report.AppendLine($"**Total Words Processed**: {analysis.TotalWords:N0}");
        report.AppendLine($"**Unique Keywords Found**: {analysis.AllKeywords.Count}");
        report.AppendLine();

        // Negative keywords
        report.AppendLine("## 🔴 Negative Keywords");
        report.AppendLine();
        report.AppendLine("| Keyword | Frequency | % of Comments |");
        report.AppendLine("|---------|-----------|---------------|");
        
        foreach (var (word, freq) in analysis.NegativeKeywords.OrderByDescending(kv => kv.Value).Take(30))
        {
            var percentage = (freq * 100.0 / analysis.TotalComments);
            report.AppendLine($"| {word} | {freq} | {percentage:F1}% |");
        }
        report.AppendLine();

        // Positive keywords
        report.AppendLine("## 🟢 Positive Keywords");
        report.AppendLine();
        report.AppendLine("| Keyword | Frequency | % of Comments |");
        report.AppendLine("|---------|-----------|---------------|");
        
        foreach (var (word, freq) in analysis.PositiveKeywords.OrderByDescending(kv => kv.Value).Take(30))
        {
            var percentage = (freq * 100.0 / analysis.TotalComments);
            report.AppendLine($"| {word} | {freq} | {percentage:F1}% |");
        }
        report.AppendLine();

        // Neutral keywords
        report.AppendLine("## ⚪ Neutral Keywords (Top 20)");
        report.AppendLine();
        report.AppendLine("| Keyword | Frequency | % of Comments |");
        report.AppendLine("|---------|-----------|---------------|");
        
        foreach (var (word, freq) in analysis.NeutralKeywords.OrderByDescending(kv => kv.Value).Take(20))
        {
            var percentage = (freq * 100.0 / analysis.TotalComments);
            report.AppendLine($"| {word} | {freq} | {percentage:F1}% |");
        }
        report.AppendLine();

        // Summary statistics
        report.AppendLine("## 📊 Summary");
        report.AppendLine();
        report.AppendLine($"- **Negative Keywords**: {analysis.NegativeKeywords.Count}");
        report.AppendLine($"- **Positive Keywords**: {analysis.PositiveKeywords.Count}");
        report.AppendLine($"- **Neutral Keywords**: {analysis.NeutralKeywords.Count}");
        report.AppendLine();
        
        var totalSentimentWords = analysis.NegativeKeywords.Sum(kv => kv.Value) + 
                                  analysis.PositiveKeywords.Sum(kv => kv.Value);
        var negativeRatio = totalSentimentWords > 0 
            ? (analysis.NegativeKeywords.Sum(kv => kv.Value) * 100.0 / totalSentimentWords) 
            : 0;
        
        report.AppendLine($"- **Negative Sentiment Ratio**: {negativeRatio:F1}%");
        report.AppendLine($"- **Positive Sentiment Ratio**: {(100 - negativeRatio):F1}%");

        return report.ToString();
    }
}
