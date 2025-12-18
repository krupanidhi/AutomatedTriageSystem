using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Core.Models;

namespace ExcelAnalysis.Infrastructure.Services;

public class BuzzwordExtractionService : IBuzzwordExtractor
{
    private readonly IExcelProcessor _excelProcessor;

    public BuzzwordExtractionService(IExcelProcessor excelProcessor)
    {
        _excelProcessor = excelProcessor;
    }

    public async Task<BuzzwordAnalysisResult> ExtractBuzzwordsAsync(int fileId)
    {
        // Get file info (you'll need to implement this based on your repository)
        // For now, assuming we have the file path
        throw new NotImplementedException("Need to integrate with repository to get file by ID");
    }

    public async Task<BuzzwordAnalysisResult> ExtractBuzzwordsFromFileAsync(ExcelFileInfo fileInfo, int minFrequency = 2)
    {
        Console.WriteLine($"\n📊 Extracting buzzwords from: {fileInfo.FileName}");
        
        // Extract comments from Excel
        var (comments, _) = await _excelProcessor.ExtractCommentsAndQuestionsAsync(fileInfo);
        
        Console.WriteLine($"   Found {comments.Count} comments to analyze");
        
        // Extract buzzwords
        var analysis = BuzzwordExtractor.ExtractBuzzwords(comments, minFrequency);
        
        // Generate report
        var report = BuzzwordExtractor.GenerateKeywordReport(analysis);
        
        var result = new BuzzwordAnalysisResult
        {
            NegativeKeywords = analysis.NegativeKeywords,
            PositiveKeywords = analysis.PositiveKeywords,
            NeutralKeywords = analysis.NeutralKeywords,
            AllKeywords = analysis.AllKeywords,
            TotalComments = analysis.TotalComments,
            TotalWords = analysis.TotalWords,
            Report = report
        };
        
        Console.WriteLine($"   ✅ Extracted {analysis.AllKeywords.Count} unique keywords");
        Console.WriteLine($"      🔴 Negative: {analysis.NegativeKeywords.Count}");
        Console.WriteLine($"      🟢 Positive: {analysis.PositiveKeywords.Count}");
        Console.WriteLine($"      ⚪ Neutral: {analysis.NeutralKeywords.Count}");
        
        return result;
    }

    public async Task<string> GenerateBuzzwordReportAsync(int fileId)
    {
        var result = await ExtractBuzzwordsAsync(fileId);
        return result.Report;
    }
}
