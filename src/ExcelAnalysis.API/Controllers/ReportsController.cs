using Microsoft.AspNetCore.Mvc;
using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Core.Models;
using ExcelAnalysis.Infrastructure.Services;
using System.Text.Json;

namespace ExcelAnalysis.API.Controllers;

/// <summary>
/// API Controller for generating and retrieving analysis reports
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IAnalysisRepository _repository;
    private readonly IAIAnalyzer _aiAnalyzer;
    private readonly IExcelProcessor _excelProcessor;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IAnalysisRepository repository,
        IAIAnalyzer aiAnalyzer,
        IExcelProcessor excelProcessor,
        ILogger<ReportsController> logger)
    {
        _repository = repository;
        _aiAnalyzer = aiAnalyzer;
        _excelProcessor = excelProcessor;
        _logger = logger;
    }

    /// <summary>
    /// Generate and retrieve HTML report for comparison analysis
    /// </summary>
    [HttpGet("{fileId}/comparison-html")]
    [Produces("text/html")]
    public async Task<IActionResult> GetComparisonReportHtml(int fileId)
    {
        try
        {
            var fileInfo = await _repository.GetFileInfoAsync(fileId);
            if (fileInfo == null)
                return NotFound($"File with ID {fileId} not found");

            _logger.LogInformation("Generating comparison report HTML for file ID {FileId}", fileId);

            var comparisonAnalyzer = new ComparisonAnalyzer(_aiAnalyzer, _excelProcessor);
            var result = await comparisonAnalyzer.CompareAnalysisMethodsAsync(fileInfo);

            var html = GenerateComparisonHtml(result);
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating comparison report for file {FileId}", fileId);
            return StatusCode(500, $"Error generating report: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate and retrieve HTML report for realistic analysis
    /// </summary>
    [HttpGet("{fileId}/realistic-html")]
    [Produces("text/html")]
    public async Task<IActionResult> GetRealisticReportHtml(int fileId)
    {
        try
        {
            var fileInfo = await _repository.GetFileInfoAsync(fileId);
            if (fileInfo == null)
                return NotFound($"File with ID {fileId} not found");

            _logger.LogInformation("Generating realistic report HTML for file ID {FileId}", fileId);

            var realisticAnalyzer = new RealisticGranteeAnalyzer(_aiAnalyzer, _excelProcessor);
            var result = await realisticAnalyzer.AnalyzeGranteeDataAsync(fileInfo);

            var html = GenerateRealisticHtml(result);
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating realistic report for file {FileId}", fileId);
            return StatusCode(500, $"Error generating report: {ex.Message}");
        }
    }

    /// <summary>
    /// Get comparison analysis data as JSON for dynamic rendering (from database)
    /// </summary>
    [HttpGet("{fileId}/comparison-data")]
    public async Task<IActionResult> GetComparisonData(int fileId)
    {
        try
        {
            // Try to get saved analysis from database
            var savedAnalysis = await _repository.GetAnalysisResultAsync(fileId);
            
            if (savedAnalysis != null && savedAnalysis.AnalysisType == "comparison" && !string.IsNullOrEmpty(savedAnalysis.RawResultJson))
            {
                _logger.LogInformation("Loading comparison analysis from database for file {FileId}", fileId);
                var result = System.Text.Json.JsonSerializer.Deserialize<ComparisonAnalysisResult>(savedAnalysis.RawResultJson);
                return Ok(result);
            }

            // If not found, run new analysis
            _logger.LogInformation("No saved comparison analysis found, running new analysis for file {FileId}", fileId);
            var fileInfo = await _repository.GetFileInfoAsync(fileId);
            if (fileInfo == null)
                return NotFound($"File with ID {fileId} not found");

            var comparisonAnalyzer = new ComparisonAnalyzer(_aiAnalyzer, _excelProcessor);
            var newResult = await comparisonAnalyzer.CompareAnalysisMethodsAsync(fileInfo);

            return Ok(newResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comparison data for file {FileId}", fileId);
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get realistic analysis data for report generation (from database)
    /// </summary>
    [HttpGet("{fileId}/realistic-data")]
    [ProducesResponseType(typeof(EnhancedAnalysisResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRealisticAnalysisData(int fileId)
    {
        try
        {
            // Try to get saved analysis from database
            var savedAnalysis = await _repository.GetAnalysisResultAsync(fileId);
            
            if (savedAnalysis != null && savedAnalysis.AnalysisType == "realistic" && !string.IsNullOrEmpty(savedAnalysis.RawResultJson))
            {
                _logger.LogInformation("Loading realistic analysis from database for file {FileId}", fileId);
                var result = System.Text.Json.JsonSerializer.Deserialize<EnhancedAnalysisResult>(savedAnalysis.RawResultJson);
                return Ok(result);
            }

            // If not found, run new analysis
            _logger.LogInformation("No saved realistic analysis found, running new analysis for file {FileId}", fileId);
            var fileInfo = await _repository.GetFileInfoAsync(fileId);
            if (fileInfo == null)
                return NotFound($"File with ID {fileId} not found");

            var realisticAnalyzer = new RealisticGranteeAnalyzer(_aiAnalyzer, _excelProcessor);
            var newResult = await realisticAnalyzer.AnalyzeGranteeDataAsync(fileInfo);
            
            return Ok(newResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting realistic analysis data for file {FileId}", fileId);
            return StatusCode(500, $"Error getting analysis: {ex.Message}");
        }
    }

    private string GenerateComparisonHtml(ComparisonAnalysisResult result)
    {
        var jsonData = JsonSerializer.Serialize(result, new JsonSerializerOptions 
        { 
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Comparison Analysis Report - {result.FileName}</title>
    <link rel=""stylesheet"" href=""/css/report.css"">
</head>
<body>
    <div id=""app""></div>
    <script>
        window.analysisData = {jsonData};
        window.reportType = 'comparison';
    </script>
    <script src=""/js/report-viewer.js""></script>
</body>
</html>";
    }

    /// <summary>
    /// Download keyword-based analysis results as JSON file
    /// Retrieves cached results if available, otherwise runs new analysis
    /// </summary>
    [HttpGet("{fileId}/download-keyword-json")]
    [Produces("application/json")]
    public async Task<IActionResult> DownloadKeywordAnalysisJson(int fileId)
    {
        try
        {
            var fileInfo = await _repository.GetFileInfoAsync(fileId);
            if (fileInfo == null)
                return NotFound($"File with ID {fileId} not found");

            // Try to get cached results first
            var savedAnalysis = await _repository.GetAnalysisResultAsync(fileId);
            EnhancedAnalysisResult result;

            if (savedAnalysis != null && savedAnalysis.AnalysisType == "realistic" && !string.IsNullOrEmpty(savedAnalysis.RawResultJson))
            {
                _logger.LogInformation("Using cached keyword analysis for file {FileId}", fileId);
                result = JsonSerializer.Deserialize<EnhancedAnalysisResult>(savedAnalysis.RawResultJson);
            }
            else
            {
                _logger.LogInformation("No cached keyword analysis found, running new analysis for file {FileId}", fileId);
                // Run keyword-based analysis
                var realisticAnalyzer = new RealisticGranteeAnalyzer(_aiAnalyzer, _excelProcessor);
                result = await realisticAnalyzer.AnalyzeGranteeDataAsync(fileInfo);
                
                // Save for future use
                savedAnalysis = new AnalysisResult
                {
                    ExcelFileInfoId = fileId,
                    AnalyzedAt = DateTime.UtcNow,
                    AnalysisType = "realistic",
                    RawResultJson = JsonSerializer.Serialize(result),
                    ExecutiveSummary = result.ExecutiveSummary,
                    OverallSentimentScore = result.OverallAverageSentiment,
                    CompletionPercentage = 100
                };
                await _repository.SaveAnalysisResultAsync(savedAnalysis);
            }

            // Use local time for user-friendly file names
            var fileName = $"keyword_analysis_{fileId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading keyword analysis JSON for file {FileId}", fileId);
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Download AI-enhanced analysis results as JSON file
    /// Retrieves cached results if available, otherwise runs new analysis
    /// </summary>
    [HttpGet("{fileId}/download-ai-json")]
    [Produces("application/json")]
    public async Task<IActionResult> DownloadAIAnalysisJson(int fileId)
    {
        try
        {
            var fileInfo = await _repository.GetFileInfoAsync(fileId);
            if (fileInfo == null)
                return NotFound($"File with ID {fileId} not found");

            // Try to get cached results first
            var savedAnalysis = await _repository.GetAnalysisResultAsync(fileId);
            EnhancedAnalysisResult result;

            if (savedAnalysis != null && savedAnalysis.AnalysisType == "ai-enhanced" && !string.IsNullOrEmpty(savedAnalysis.RawResultJson))
            {
                _logger.LogInformation("Using cached AI-enhanced analysis for file {FileId}", fileId);
                result = JsonSerializer.Deserialize<EnhancedAnalysisResult>(savedAnalysis.RawResultJson);
            }
            else
            {
                _logger.LogInformation("No cached AI-enhanced analysis found, running new analysis for file {FileId}", fileId);
                // Run AI-enhanced analysis
                var enhancedAIAnalyzer = HttpContext.RequestServices.GetRequiredService<EnhancedAIAnalyzer>();
                result = await enhancedAIAnalyzer.AnalyzeGranteeDataWithAIAsync(fileInfo);
                
                // Save for future use
                savedAnalysis = new AnalysisResult
                {
                    ExcelFileInfoId = fileId,
                    AnalyzedAt = DateTime.UtcNow,
                    AnalysisType = "ai-enhanced",
                    RawResultJson = JsonSerializer.Serialize(result),
                    ExecutiveSummary = result.ExecutiveSummary,
                    OverallSentimentScore = result.OverallAverageSentiment,
                    CompletionPercentage = 100
                };
                await _repository.SaveAnalysisResultAsync(savedAnalysis);
            }

            // Use local time for user-friendly file names
            var fileName = $"ai_analysis_{fileId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading AI analysis JSON for file {FileId}", fileId);
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Download semantic analysis results as JSON file
    /// </summary>
    [HttpGet("{fileId}/download-semantic-json")]
    [Produces("application/json")]
    public async Task<IActionResult> DownloadSemanticAnalysisJson(int fileId)
    {
        try
        {
            var savedAnalysis = await _repository.GetAnalysisResultAsync(fileId);
            
            if (savedAnalysis == null || savedAnalysis.AnalysisType != "hybrid" || string.IsNullOrEmpty(savedAnalysis.RawResultJson))
            {
                return NotFound($"No semantic analysis found for file ID {fileId}. Run hybrid analysis first.");
            }

            var hybridResult = JsonSerializer.Deserialize<HybridAnalysisResult>(savedAnalysis.RawResultJson);
            if (hybridResult?.SemanticResults == null)
            {
                return NotFound($"Semantic analysis not available in hybrid results for file ID {fileId}");
            }

            // Use local time for user-friendly file names
            var fileName = $"semantic_analysis_{fileId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var json = JsonSerializer.Serialize(hybridResult.SemanticResults, new JsonSerializerOptions { WriteIndented = true });
            return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading semantic analysis JSON for file {FileId}", fileId);
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Download hybrid analysis results as JSON file (includes both Claude and Semantic)
    /// </summary>
    [HttpGet("{fileId}/download-hybrid-json")]
    [Produces("application/json")]
    public async Task<IActionResult> DownloadHybridAnalysisJson(int fileId)
    {
        try
        {
            var savedAnalysis = await _repository.GetAnalysisResultAsync(fileId);
            
            if (savedAnalysis == null || savedAnalysis.AnalysisType != "hybrid" || string.IsNullOrEmpty(savedAnalysis.RawResultJson))
            {
                return NotFound($"No hybrid analysis found for file ID {fileId}. Run hybrid analysis first.");
            }

            // Use local time for user-friendly file names
            var fileName = $"hybrid_analysis_{fileId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var hybridResult = JsonSerializer.Deserialize<HybridAnalysisResult>(savedAnalysis.RawResultJson);
            var json = JsonSerializer.Serialize(hybridResult, new JsonSerializerOptions { WriteIndented = true });
            return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading hybrid analysis JSON for file {FileId}", fileId);
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Download analysis results as JSON file (legacy endpoint)
    /// </summary>
    [HttpGet("{fileId}/download-json")]
    [Produces("application/json")]
    public async Task<IActionResult> DownloadAnalysisJson(int fileId, [FromQuery] string type = "realistic")
    {
        try
        {
            var savedAnalysis = await _repository.GetAnalysisResultAsync(fileId);
            
            if (savedAnalysis == null || string.IsNullOrEmpty(savedAnalysis.RawResultJson))
            {
                return NotFound($"No saved analysis found for file ID {fileId}");
            }

            // Use local time for user-friendly file names
            var fileName = $"analysis_{fileId}_{type}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            return File(System.Text.Encoding.UTF8.GetBytes(savedAnalysis.RawResultJson), "application/json", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading JSON for file {FileId}", fileId);
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    private string GenerateRealisticHtml(EnhancedAnalysisResult result)
    {
        var jsonData = JsonSerializer.Serialize(result, new JsonSerializerOptions 
        { 
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Realistic Analysis Report - {result.FileName}</title>
    <link rel=""stylesheet"" href=""/css/report.css"">
</head>
<body>
    <div id=""app""></div>
    <script>
        window.analysisData = {jsonData};
        window.reportType = 'realistic';
    </script>
    <script src=""/js/report-viewer.js""></script>
</body>
</html>";
    }
}
