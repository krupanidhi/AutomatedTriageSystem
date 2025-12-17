using ExcelAnalysis.Core.Interfaces;
using System.Text.Json;

namespace ExcelAnalysis.Infrastructure.Services;

/// <summary>
/// Learns and persists buzzwords across multiple Excel analyses
/// Maintains a growing knowledge base of positive and negative keywords
/// </summary>
public class PersistentBuzzwordLearner
{
    private readonly string _storagePath;
    private BuzzwordKnowledgeBase _knowledgeBase;

    public PersistentBuzzwordLearner(string storagePath = "buzzword_knowledge.json")
    {
        _storagePath = storagePath;
        _knowledgeBase = LoadKnowledgeBase();
    }

    /// <summary>
    /// Learn new buzzwords from an Excel file and merge with existing knowledge
    /// </summary>
    public async Task<BuzzwordLearningResult> LearnFromFileAsync(string filePath, IExcelProcessor processor)
    {
        Console.WriteLine($"\n📚 Learning buzzwords from: {Path.GetFileName(filePath)}");
        
        // Note: This method needs proper implementation with database access
        // For now, return placeholder result
        await Task.CompletedTask;
        
        var result = new BuzzwordLearningResult
        {
            NewNegativeBuzzwords = new Dictionary<string, int>(),
            NewPositiveBuzzwords = new Dictionary<string, int>(),
            TotalNegativeBuzzwords = _knowledgeBase.NegativeKeywords.Count,
            TotalPositiveBuzzwords = _knowledgeBase.PositiveKeywords.Count,
            TotalBuzzwords = _knowledgeBase.NegativeKeywords.Count + _knowledgeBase.PositiveKeywords.Count,
            FilesAnalyzed = _knowledgeBase.TotalFilesAnalyzed
        };
        
        return result;
    }

    /// <summary>
    /// Get the current knowledge base for sentiment analysis
    /// </summary>
    public DynamicSentimentAnalyzer GetSentimentAnalyzer()
    {
        return new DynamicSentimentAnalyzer(
            _knowledgeBase.NegativeKeywords,
            _knowledgeBase.PositiveKeywords
        );
    }

    /// <summary>
    /// Get statistics about the knowledge base
    /// </summary>
    public BuzzwordKnowledgeStats GetStats()
    {
        return new BuzzwordKnowledgeStats
        {
            TotalNegativeKeywords = _knowledgeBase.NegativeKeywords.Count,
            TotalPositiveKeywords = _knowledgeBase.PositiveKeywords.Count,
            TotalKeywords = _knowledgeBase.NegativeKeywords.Count + _knowledgeBase.PositiveKeywords.Count,
            FilesAnalyzed = _knowledgeBase.TotalFilesAnalyzed,
            LastUpdated = _knowledgeBase.LastUpdated,
            TopNegativeKeywords = _knowledgeBase.NegativeKeywords
                .OrderByDescending(kv => kv.Value)
                .Take(10)
                .ToDictionary(kv => kv.Key, kv => kv.Value),
            TopPositiveKeywords = _knowledgeBase.PositiveKeywords
                .OrderByDescending(kv => kv.Value)
                .Take(10)
                .ToDictionary(kv => kv.Key, kv => kv.Value)
        };
    }

    /// <summary>
    /// Reset the knowledge base (for testing or fresh start)
    /// </summary>
    public void Reset()
    {
        _knowledgeBase = new BuzzwordKnowledgeBase();
        SaveKnowledgeBaseAsync().Wait();
        Console.WriteLine("🔄 Knowledge base reset");
    }

    private BuzzwordKnowledgeBase LoadKnowledgeBase()
    {
        if (File.Exists(_storagePath))
        {
            try
            {
                var json = File.ReadAllText(_storagePath);
                var kb = JsonSerializer.Deserialize<BuzzwordKnowledgeBase>(json);
                if (kb != null)
                {
                    Console.WriteLine($"📖 Loaded knowledge base: {kb.NegativeKeywords.Count + kb.PositiveKeywords.Count} buzzwords from {kb.TotalFilesAnalyzed} files");
                    return kb;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to load knowledge base: {ex.Message}");
            }
        }
        
        Console.WriteLine("📝 Creating new knowledge base");
        return new BuzzwordKnowledgeBase();
    }

    private async Task SaveKnowledgeBaseAsync()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(_knowledgeBase, options);
            await File.WriteAllTextAsync(_storagePath, json);
            Console.WriteLine($"💾 Knowledge base saved: {_storagePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to save knowledge base: {ex.Message}");
        }
    }
}

public class BuzzwordKnowledgeBase
{
    public Dictionary<string, int> NegativeKeywords { get; set; } = new();
    public Dictionary<string, int> PositiveKeywords { get; set; } = new();
    public int TotalFilesAnalyzed { get; set; } = 0;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public List<AnalyzedFileInfo> AnalyzedFiles { get; set; } = new();
}

public class AnalyzedFileInfo
{
    public string FileName { get; set; } = "";
    public DateTime AnalyzedAt { get; set; }
    public int NewNegativeCount { get; set; }
    public int NewPositiveCount { get; set; }
}

public class BuzzwordLearningResult
{
    public Dictionary<string, int> NewNegativeBuzzwords { get; set; } = new();
    public Dictionary<string, int> NewPositiveBuzzwords { get; set; } = new();
    public int TotalNegativeBuzzwords { get; set; }
    public int TotalPositiveBuzzwords { get; set; }
    public int TotalBuzzwords { get; set; }
    public int FilesAnalyzed { get; set; }
}

public class BuzzwordKnowledgeStats
{
    public int TotalNegativeKeywords { get; set; }
    public int TotalPositiveKeywords { get; set; }
    public int TotalKeywords { get; set; }
    public int FilesAnalyzed { get; set; }
    public DateTime LastUpdated { get; set; }
    public Dictionary<string, int> TopNegativeKeywords { get; set; } = new();
    public Dictionary<string, int> TopPositiveKeywords { get; set; } = new();
}
