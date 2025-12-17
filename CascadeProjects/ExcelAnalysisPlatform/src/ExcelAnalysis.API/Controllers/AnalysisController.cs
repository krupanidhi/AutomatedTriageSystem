using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Core.Models;
using ExcelAnalysis.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExcelAnalysis.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly IExcelProcessor _excelProcessor;
    private readonly IAIAnalyzer _aiAnalyzer;
    private readonly IAnalysisRepository _repository;
    private readonly ILogger<AnalysisController> _logger;
    private readonly HybridAnalyzer? _hybridAnalyzer;

    public AnalysisController(
        IExcelProcessor excelProcessor,
        IAIAnalyzer aiAnalyzer,
        IAnalysisRepository repository,
        ILogger<AnalysisController> logger,
        HybridAnalyzer? hybridAnalyzer = null)
    {
        _excelProcessor = excelProcessor;
        _aiAnalyzer = aiAnalyzer;
        _repository = repository;
        _logger = logger;
        _hybridAnalyzer = hybridAnalyzer;
    }

    /// <summary>
    /// Upload and process an Excel file
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ExcelFileInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .xlsx files are supported");

        try
        {
            using var stream = file.OpenReadStream();
            var fileInfo = await _excelProcessor.ProcessFileAsync(stream, file.FileName);
            
            var fileId = await _repository.SaveFileInfoAsync(fileInfo);
            fileInfo.Id = fileId;

            _logger.LogInformation("File {FileName} uploaded successfully with ID {FileId}", file.FileName, fileId);
            
            return Ok(fileInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file {FileName}", file.FileName);
            return StatusCode(500, $"Error processing file: {ex.Message}");
        }
    }

    /// <summary>
    /// Analyze an uploaded file
    /// </summary>
    [HttpPost("{fileId}/analyze")]
    [ProducesResponseType(typeof(AnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnalyzeFile(int fileId)
    {
        try
        {
            var fileInfo = await _repository.GetFileInfoAsync(fileId);
            if (fileInfo == null)
                return NotFound($"File with ID {fileId} not found. Please upload the file first.");

            _logger.LogInformation("Starting AI-powered analysis for file ID {FileId}", fileId);

            var result = await _aiAnalyzer.AnalyzeAsync(fileInfo);
            
            // Ensure proper foreign key relationship
            result.ExcelFileInfoId = fileId;
            result.AnalysisType = "basic";
            result.RawResultJson = System.Text.Json.JsonSerializer.Serialize(result);
            result.AnalyzedAt = DateTime.UtcNow;
            
            // Save the result
            await _repository.SaveAnalysisResultAsync(result);
            
            _logger.LogInformation("AI-powered analysis completed and saved for file ID {FileId}", fileId);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing file {FileId}", fileId);
            return StatusCode(500, $"Error analyzing file: {ex.Message}");
        }
    }

    /// <summary>
    /// Get analysis results for a file
    /// </summary>
    [HttpGet("{fileId}/results")]
    [ProducesResponseType(typeof(AnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalysisResults(int fileId)
    {
        var result = await _repository.GetAnalysisResultAsync(fileId);
        if (result == null)
            return NotFound($"No analysis results found for file ID {fileId}");

        return Ok(result);
    }

    /// <summary>
    /// Get all uploaded files
    /// </summary>
    [HttpGet("files")]
    [ProducesResponseType(typeof(List<ExcelFileInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllFiles()
    {
        var files = await _repository.GetAllFilesAsync();
        return Ok(files);
    }

    /// <summary>
    /// Get file details
    /// </summary>
    [HttpGet("files/{fileId}")]
    [ProducesResponseType(typeof(ExcelFileInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(int fileId)
    {
        var file = await _repository.GetFileInfoAsync(fileId);
        if (file == null)
            return NotFound($"File with ID {fileId} not found");

        return Ok(file);
    }

    /// <summary>
    /// Delete a file and its analysis
    /// </summary>
    [HttpDelete("files/{fileId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile(int fileId)
    {
        var file = await _repository.GetFileInfoAsync(fileId);
        if (file == null)
            return NotFound($"File with ID {fileId} not found");

        await _repository.DeleteFileAsync(fileId);
        _logger.LogInformation("File ID {FileId} deleted", fileId);

        return NoContent();
    }

    /// <summary>
    /// Perform realistic grantee analysis with organization insights, thematic analysis, and detailed recommendations
    /// This produces output matching the quality of manual GRANTEE_CHALLENGES_REPORT.md
    /// </summary>
    [HttpPost("{fileId}/analyze-realistic")]
    [ProducesResponseType(typeof(EnhancedAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnalyzeRealistic(int fileId)
    {
        try
        {
            var fileInfo = await _repository.GetFileInfoAsync(fileId);
            if (fileInfo == null)
                return NotFound($"File with ID {fileId} not found");

            _logger.LogInformation("Starting REALISTIC analysis for file ID {FileId}", fileId);

            var realisticAnalyzer = new RealisticGranteeAnalyzer(_aiAnalyzer, _excelProcessor);
            var enhancedResult = await realisticAnalyzer.AnalyzeGranteeDataAsync(fileInfo);
            
            enhancedResult.ExcelFileInfoId = fileId;
            enhancedResult.Id = fileId; // Use same ID for simplicity

            _logger.LogInformation("Realistic analysis completed for file ID {FileId}", fileId);
            _logger.LogInformation("  Organizations: {OrgCount}, Responses: {ResponseCount}", 
                enhancedResult.TotalGranteesAnalyzed, enhancedResult.TotalResponsesAnalyzed);
            _logger.LogInformation("  Sentiment: {Positive}% positive, {Negative}% negative", 
                enhancedResult.SentimentDistribution.PositivePercentage, 
                enhancedResult.SentimentDistribution.NegativePercentage);

            // Save to database for later retrieval
            var analysisResult = new AnalysisResult
            {
                ExcelFileInfoId = fileId,
                AnalyzedAt = DateTime.UtcNow,
                AnalysisType = "realistic",
                RawResultJson = System.Text.Json.JsonSerializer.Serialize(enhancedResult),
                ExecutiveSummary = enhancedResult.ExecutiveSummary,
                OverallSentimentScore = enhancedResult.OverallAverageSentiment,
                CompletionPercentage = 100
            };
            await _repository.SaveAnalysisResultAsync(analysisResult);
            _logger.LogInformation("Analysis results saved to database for file ID {FileId}", fileId);

            // Save historical snapshot for trend tracking
            var historicalService = HttpContext.RequestServices.GetRequiredService<HistoricalTrackingService>();
            await historicalService.SaveSnapshotAsync(enhancedResult, "keyword");
            _logger.LogInformation("Historical snapshot saved for file ID {FileId}", fileId);

            return Ok(enhancedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing realistic analysis for file {FileId}", fileId);
            return StatusCode(500, $"Error performing realistic analysis: {ex.Message}");
        }
    }

    /// <summary>
    /// Perform AI-enhanced grantee analysis using large language model for contextual understanding
    /// Produces comprehensive organization-level insights with AI-generated challenges and remedies
    /// </summary>
    [HttpPost("{fileId}/analyze-ai-enhanced")]
    [ProducesResponseType(typeof(EnhancedAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnalyzeAIEnhanced(int fileId)
    {
        try
        {
            var fileInfo = await _repository.GetFileInfoAsync(fileId);
            if (fileInfo == null)
                return NotFound($"File with ID {fileId} not found");

            _logger.LogInformation("Starting AI-ENHANCED analysis for file ID {FileId}", fileId);

            var enhancedAIAnalyzer = HttpContext.RequestServices.GetRequiredService<EnhancedAIAnalyzer>();
            var enhancedResult = await enhancedAIAnalyzer.AnalyzeGranteeDataWithAIAsync(fileInfo);
            
            enhancedResult.ExcelFileInfoId = fileId;
            enhancedResult.Id = fileId;

            _logger.LogInformation("AI-Enhanced analysis completed for file ID {FileId}", fileId);
            _logger.LogInformation("  Organizations: {OrgCount}, Responses: {ResponseCount}", 
                enhancedResult.TotalGranteesAnalyzed, enhancedResult.TotalResponsesAnalyzed);
            _logger.LogInformation("  Sentiment: {Positive}% positive, {Negative}% negative", 
                enhancedResult.SentimentDistribution.PositivePercentage, 
                enhancedResult.SentimentDistribution.NegativePercentage);

            // Save to database for later retrieval
            var analysisResult = new AnalysisResult
            {
                ExcelFileInfoId = fileId,
                AnalyzedAt = DateTime.UtcNow,
                AnalysisType = "ai-enhanced",
                RawResultJson = System.Text.Json.JsonSerializer.Serialize(enhancedResult),
                ExecutiveSummary = enhancedResult.ExecutiveSummary,
                OverallSentimentScore = enhancedResult.OverallAverageSentiment,
                CompletionPercentage = 100
            };

            await _repository.SaveAnalysisResultAsync(analysisResult);

            // Save historical snapshot for trend tracking
            var historicalService = HttpContext.RequestServices.GetRequiredService<HistoricalTrackingService>();
            await historicalService.SaveSnapshotAsync(enhancedResult, "ai-enhanced");
            _logger.LogInformation("Historical snapshot saved for file ID {FileId}", fileId);

            return Ok(enhancedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing AI-enhanced analysis for file ID {FileId}", fileId);
            return StatusCode(500, $"Error performing AI-enhanced analysis: {ex.Message}");
        }
    }

    /// <summary>
    /// Compare keyword-based vs Claude AI-based sentiment analysis
    /// Shows side-by-side comparison of both methods with speed, cost, and accuracy metrics
    /// </summary>
    [HttpPost("{fileId}/analyze-comparison")]
    [ProducesResponseType(typeof(ComparisonAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnalyzeComparison(int fileId)
    {
        try
        {
            var fileInfo = await _repository.GetFileInfoAsync(fileId);
            if (fileInfo == null)
                return NotFound($"File with ID {fileId} not found");

            _logger.LogInformation("Starting COMPARISON analysis (Keyword vs AI) for file ID {FileId}", fileId);

            var comparisonAnalyzer = new ComparisonAnalyzer(_aiAnalyzer, _excelProcessor);
            var comparisonResult = await comparisonAnalyzer.CompareAnalysisMethodsAsync(fileInfo);

            _logger.LogInformation("Comparison analysis completed for file ID {FileId}", fileId);
            _logger.LogInformation("  Keyword: {KeywordTime:F2}s, AI: {AITime:F2}s", 
                comparisonResult.KeywordMethodology.ProcessingTimeSeconds,
                comparisonResult.AIMethodology.ProcessingTimeSeconds);
            _logger.LogInformation("  Sentiment difference: {Diff:F3}", 
                comparisonResult.AverageSentimentDifference);

            // Save to database for later retrieval
            var analysisResult = new AnalysisResult
            {
                ExcelFileInfoId = fileId,
                AnalyzedAt = DateTime.UtcNow,
                AnalysisType = "comparison",
                RawResultJson = System.Text.Json.JsonSerializer.Serialize(comparisonResult),
                ExecutiveSummary = comparisonResult.ComparisonSummary,
                OverallSentimentScore = comparisonResult.KeywordBasedAnalysis.OverallAverageSentiment,
                CompletionPercentage = 100
            };
            await _repository.SaveAnalysisResultAsync(analysisResult);
            _logger.LogInformation("Comparison results saved to database for file ID {FileId}", fileId);

            return Ok(comparisonResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing comparison analysis for file {FileId}", fileId);
            return StatusCode(500, $"Error analyzing file: {ex.Message}");
        }
    }

    /// <summary>
    /// Perform hybrid analysis combining Claude AI and Sentence Transformers semantic analysis
    /// Returns integrated results from both models with clear source attribution
    /// </summary>
    [HttpPost("{fileId}/analyze-hybrid")]
    [ProducesResponseType(typeof(HybridAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnalyzeHybrid(int fileId)
    {
        try
        {
            if (_hybridAnalyzer == null)
                return StatusCode(503, "Hybrid analyzer not available. Ensure semantic service is running.");

            var fileInfo = await _repository.GetFileInfoAsync(fileId);
            if (fileInfo == null)
                return NotFound($"File with ID {fileId} not found");

            _logger.LogInformation("Starting HYBRID analysis (Claude + Semantic) for file ID {FileId}", fileId);

            var hybridResult = await _hybridAnalyzer.AnalyzeWithBothModelsAsync(fileInfo);

            _logger.LogInformation("Hybrid analysis completed for file ID {FileId}", fileId);
            _logger.LogInformation("  Organizations: {OrgCount}, Themes: {ThemeCount}", 
                hybridResult.TotalGranteesAnalyzed,
                hybridResult.IntegratedThemes.Count);

            // Delete any existing hybrid analysis for this file to avoid concurrency issues
            var existingAnalysis = await _repository.GetAnalysisResultAsync(fileId);
            if (existingAnalysis != null && existingAnalysis.AnalysisType == "hybrid")
            {
                await _repository.DeleteAnalysisResultAsync(existingAnalysis.Id);
                _logger.LogInformation("Deleted existing hybrid analysis for file ID {FileId}", fileId);
            }

            // Save to database (always as new record with Id = 0)
            var analysisResult = new AnalysisResult
            {
                Id = 0, // Force new record
                ExcelFileInfoId = fileId,
                AnalyzedAt = DateTime.UtcNow,
                AnalysisType = "hybrid",
                RawResultJson = System.Text.Json.JsonSerializer.Serialize(hybridResult),
                DetailedAnalysisJson = System.Text.Json.JsonSerializer.Serialize(hybridResult.IntegratedOrganizationInsights),
                ExecutiveSummary = hybridResult.ExecutiveSummary,
                OverallSentimentScore = hybridResult.ClaudeResults?.OverallAverageSentiment ?? 0,
                HighRiskCount = hybridResult.IntegratedOrganizationInsights.Count(o => o.ClaudeRiskLevel == "High"),
                MediumRiskCount = hybridResult.IntegratedOrganizationInsights.Count(o => o.ClaudeRiskLevel == "Medium"),
                LowRiskCount = hybridResult.IntegratedOrganizationInsights.Count(o => o.ClaudeRiskLevel == "Low"),
                CompletionPercentage = 100
            };
            await _repository.SaveAnalysisResultAsync(analysisResult);
            _logger.LogInformation("Hybrid results saved to database for file ID {FileId}", fileId);

            return Ok(hybridResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing hybrid analysis for file ID {FileId}", fileId);
            return StatusCode(500, $"Error performing hybrid analysis: {ex.Message}");
        }
    }
}
