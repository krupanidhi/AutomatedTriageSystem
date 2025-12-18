using ExcelAnalysis.Core.Models;
using ExcelAnalysis.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExcelAnalysis.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HistoricalAnalysisController : ControllerBase
{
    private readonly HistoricalTrackingService _historicalService;
    private readonly ILogger<HistoricalAnalysisController> _logger;

    public HistoricalAnalysisController(
        HistoricalTrackingService historicalService,
        ILogger<HistoricalAnalysisController> logger)
    {
        _historicalService = historicalService;
        _logger = logger;
    }

    /// <summary>
    /// Get comparative analysis between current and previous analysis
    /// </summary>
    [HttpGet("{fileId}/comparative")]
    [ProducesResponseType(typeof(List<ComparativeAnalysis>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComparativeAnalysis(int fileId)
    {
        try
        {
            var comparison = await _historicalService.GetComparativeAnalysisAsync(fileId);
            
            if (!comparison.Any())
            {
                return Ok(new { message = "Not enough historical data. Run analysis at least twice to see comparisons." });
            }

            return Ok(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comparative analysis for file {FileId}", fileId);
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get trend analysis for a specific challenge
    /// </summary>
    [HttpGet("trends/{challengeName}")]
    [ProducesResponseType(typeof(TrendAnalysis), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChallengeTrend(string challengeName, [FromQuery] int months = 6)
    {
        try
        {
            var trend = await _historicalService.GetChallengeTrendAsync(challengeName, months);
            return Ok(trend);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trend for challenge {ChallengeName}", challengeName);
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get remediation effectiveness report for a challenge
    /// </summary>
    [HttpGet("remediation/{challengeName}")]
    [ProducesResponseType(typeof(RemediationEffectivenessReport), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRemediationEffectiveness(string challengeName)
    {
        try
        {
            var report = await _historicalService.GetRemediationEffectivenessAsync(challengeName);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting remediation effectiveness for {ChallengeName}", challengeName);
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Record a remediation attempt
    /// </summary>
    [HttpPost("remediation")]
    [ProducesResponseType(typeof(RemediationAttempt), StatusCodes.Status201Created)]
    public async Task<IActionResult> RecordRemediationAttempt([FromBody] RecordRemediationRequest request)
    {
        try
        {
            var attempt = await _historicalService.RecordRemediationAttemptAsync(
                request.ChallengeName,
                request.ActionTaken,
                request.ResponsibleParty,
                request.SentimentBefore,
                request.SentimentAfter,
                request.Outcome,
                request.Notes
            );

            return CreatedAtAction(nameof(GetRemediationEffectiveness), 
                new { challengeName = request.ChallengeName }, 
                attempt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording remediation attempt");
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }
}

public class RecordRemediationRequest
{
    public string ChallengeName { get; set; } = string.Empty;
    public string ActionTaken { get; set; } = string.Empty;
    public string ResponsibleParty { get; set; } = string.Empty;
    public double SentimentBefore { get; set; }
    public double SentimentAfter { get; set; }
    public string Outcome { get; set; } = string.Empty; // Successful, Partial, Failed, InProgress
    public string Notes { get; set; } = string.Empty;
}
