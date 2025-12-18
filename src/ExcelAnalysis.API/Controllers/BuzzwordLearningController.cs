using Microsoft.AspNetCore.Mvc;
using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Infrastructure.Services;

namespace ExcelAnalysis.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BuzzwordLearningController : ControllerBase
{
    private readonly IExcelProcessor _excelProcessor;
    private readonly ILogger<BuzzwordLearningController> _logger;
    private static readonly PersistentBuzzwordLearner _learner = new("buzzword_knowledge.json");

    public BuzzwordLearningController(
        IExcelProcessor excelProcessor,
        ILogger<BuzzwordLearningController> logger)
    {
        _excelProcessor = excelProcessor;
        _logger = logger;
    }

    /// <summary>
    /// Learn buzzwords from an uploaded Excel file and merge with knowledge base
    /// </summary>
    [HttpPost("learn/{fileId}")]
    public async Task<IActionResult> LearnFromFile(int fileId)
    {
        try
        {
            _logger.LogInformation("Learning buzzwords from file ID: {FileId}", fileId);

            // Get file info from database
            var fileInfo = await GetFileInfoAsync(fileId);
            if (fileInfo == null)
            {
                return NotFound($"File with ID {fileId} not found");
            }

            // Learn from file (placeholder - needs proper implementation)
            var result = await _learner.LearnFromFileAsync("", _excelProcessor);

            return Ok(new
            {
                success = true,
                fileName = fileInfo.FileName,
                newNegativeBuzzwords = result.NewNegativeBuzzwords.Count,
                newPositiveBuzzwords = result.NewPositiveBuzzwords.Count,
                totalNegativeBuzzwords = result.TotalNegativeBuzzwords,
                totalPositiveBuzzwords = result.TotalPositiveBuzzwords,
                totalBuzzwords = result.TotalBuzzwords,
                filesAnalyzed = result.FilesAnalyzed,
                newNegativeWords = result.NewNegativeBuzzwords.Keys.Take(20).ToList(),
                newPositiveWords = result.NewPositiveBuzzwords.Keys.Take(20).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error learning buzzwords from file {FileId}", fileId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get statistics about the buzzword knowledge base
    /// </summary>
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        try
        {
            var stats = _learner.GetStats();

            return Ok(new
            {
                totalNegativeKeywords = stats.TotalNegativeKeywords,
                totalPositiveKeywords = stats.TotalPositiveKeywords,
                totalKeywords = stats.TotalKeywords,
                filesAnalyzed = stats.FilesAnalyzed,
                lastUpdated = stats.LastUpdated,
                topNegativeKeywords = stats.TopNegativeKeywords,
                topPositiveKeywords = stats.TopPositiveKeywords
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting buzzword stats");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Reset the knowledge base (for testing or fresh start)
    /// </summary>
    [HttpPost("reset")]
    public IActionResult Reset()
    {
        try
        {
            _learner.Reset();
            return Ok(new { success = true, message = "Knowledge base reset" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting knowledge base");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get the sentiment analyzer with current knowledge base
    /// </summary>
    [HttpGet("analyzer")]
    public IActionResult GetAnalyzer()
    {
        try
        {
            var analyzer = _learner.GetSentimentAnalyzer();
            var stats = _learner.GetStats();

            return Ok(new
            {
                ready = true,
                totalKeywords = stats.TotalKeywords,
                negativeKeywords = stats.TotalNegativeKeywords,
                positiveKeywords = stats.TotalPositiveKeywords,
                filesAnalyzed = stats.FilesAnalyzed
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analyzer");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task<Core.Models.ExcelFileInfo?> GetFileInfoAsync(int fileId)
    {
        // This would normally query the database
        // For now, return a mock implementation
        // You'll need to inject your DbContext and query it properly
        await Task.CompletedTask;
        return null; // TODO: Implement database query
    }
}
