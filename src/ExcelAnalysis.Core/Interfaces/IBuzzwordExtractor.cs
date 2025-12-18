using ExcelAnalysis.Core.Models;

namespace ExcelAnalysis.Core.Interfaces;

public interface IBuzzwordExtractor
{
    Task<BuzzwordAnalysisResult> ExtractBuzzwordsAsync(int fileId);
    Task<string> GenerateBuzzwordReportAsync(int fileId);
}

public class BuzzwordAnalysisResult
{
    public Dictionary<string, int> NegativeKeywords { get; set; } = new();
    public Dictionary<string, int> PositiveKeywords { get; set; } = new();
    public Dictionary<string, int> NeutralKeywords { get; set; } = new();
    public Dictionary<string, int> AllKeywords { get; set; } = new();
    public int TotalComments { get; set; }
    public int TotalWords { get; set; }
    public string Report { get; set; } = string.Empty;
}
