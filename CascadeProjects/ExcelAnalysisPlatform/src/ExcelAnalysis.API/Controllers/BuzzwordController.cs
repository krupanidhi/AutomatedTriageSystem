using Microsoft.AspNetCore.Mvc;
using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Infrastructure.Services;

namespace ExcelAnalysis.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BuzzwordController : ControllerBase
{
    private readonly IExcelProcessor _excelProcessor;
    private readonly ILogger<BuzzwordController> _logger;

    public BuzzwordController(
        IExcelProcessor excelProcessor,
        ILogger<BuzzwordController> logger)
    {
        _excelProcessor = excelProcessor;
        _logger = logger;
    }

    /// <summary>
    /// Extract buzzwords from an uploaded Excel file
    /// </summary>
    [HttpPost("extract/{fileId}")]
    public async Task<ActionResult<BuzzwordAnalysisResult>> ExtractBuzzwords(
        int fileId,
        [FromQuery] int minFrequency = 2)
    {
        try
        {
            _logger.LogInformation("Extracting buzzwords from file {FileId}", fileId);

            // Get file from database (simplified - you'd get this from repository)
            var dbContext = HttpContext.RequestServices.GetRequiredService<ExcelAnalysis.Infrastructure.Data.AnalysisDbContext>();
            var fileInfo = await dbContext.ExcelFiles.FindAsync(fileId);

            if (fileInfo == null)
                return NotFound($"File with ID {fileId} not found");

            var service = new BuzzwordExtractionService(_excelProcessor);
            var result = await service.ExtractBuzzwordsFromFileAsync(fileInfo, minFrequency);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting buzzwords from file {FileId}", fileId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Extract buzzwords and get markdown report
    /// </summary>
    [HttpPost("extract/{fileId}/report")]
    public async Task<ActionResult<string>> ExtractBuzzwordsReport(
        int fileId,
        [FromQuery] int minFrequency = 2)
    {
        try
        {
            _logger.LogInformation("Generating buzzword report for file {FileId}", fileId);

            var dbContext = HttpContext.RequestServices.GetRequiredService<ExcelAnalysis.Infrastructure.Data.AnalysisDbContext>();
            var fileInfo = await dbContext.ExcelFiles.FindAsync(fileId);

            if (fileInfo == null)
                return NotFound($"File with ID {fileId} not found");

            var service = new BuzzwordExtractionService(_excelProcessor);
            var result = await service.ExtractBuzzwordsFromFileAsync(fileInfo, minFrequency);

            return Content(result.Report, "text/markdown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating buzzword report for file {FileId}", fileId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Save extracted buzzwords to file for use in sentiment analysis
    /// </summary>
    [HttpPost("extract/{fileId}/save")]
    public async Task<ActionResult> SaveBuzzwords(
        int fileId,
        [FromQuery] int minFrequency = 2,
        [FromQuery] string outputPath = "extracted_buzzwords.json")
    {
        try
        {
            _logger.LogInformation("Saving buzzwords from file {FileId} to {OutputPath}", fileId, outputPath);

            var dbContext = HttpContext.RequestServices.GetRequiredService<ExcelAnalysis.Infrastructure.Data.AnalysisDbContext>();
            var fileInfo = await dbContext.ExcelFiles.FindAsync(fileId);

            if (fileInfo == null)
                return NotFound($"File with ID {fileId} not found");

            var service = new BuzzwordExtractionService(_excelProcessor);
            var result = await service.ExtractBuzzwordsFromFileAsync(fileInfo, minFrequency);

            // Save to JSON file
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                extractedFrom = fileInfo.FileName,
                extractedAt = DateTime.UtcNow,
                totalComments = result.TotalComments,
                totalWords = result.TotalWords,
                negativeKeywords = result.NegativeKeywords.OrderByDescending(kv => kv.Value).Take(100).ToDictionary(kv => kv.Key, kv => kv.Value),
                positiveKeywords = result.PositiveKeywords.OrderByDescending(kv => kv.Value).Take(100).ToDictionary(kv => kv.Key, kv => kv.Value),
                neutralKeywords = result.NeutralKeywords.OrderByDescending(kv => kv.Value).Take(50).ToDictionary(kv => kv.Key, kv => kv.Value)
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            await System.IO.File.WriteAllTextAsync(outputPath, json);

            _logger.LogInformation("Buzzwords saved to {OutputPath}", outputPath);

            return Ok(new
            {
                message = $"Buzzwords saved to {outputPath}",
                negativeCount = result.NegativeKeywords.Count,
                positiveCount = result.PositiveKeywords.Count,
                neutralCount = result.NeutralKeywords.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving buzzwords from file {FileId}", fileId);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
