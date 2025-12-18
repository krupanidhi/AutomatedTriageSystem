using Microsoft.AspNetCore.Mvc;
using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Core.Models;

namespace ExcelAnalysis.API.Controllers;

/// <summary>
/// Web UI Controller for interactive analysis dashboard
/// </summary>
public class WebUIController : Controller
{
    private readonly IAnalysisRepository _repository;
    private readonly ILogger<WebUIController> _logger;

    public WebUIController(IAnalysisRepository repository, ILogger<WebUIController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Main dashboard showing all uploaded files
    /// </summary>
    [HttpGet("/")]
    [HttpGet("/dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var files = await _repository.GetAllFilesAsync();
        return View(files);
    }

    /// <summary>
    /// File upload page
    /// </summary>
    [HttpGet("/upload")]
    public IActionResult Upload()
    {
        return View();
    }

    /// <summary>
    /// Analysis selection page for a specific file
    /// </summary>
    [HttpGet("/analyze/{fileId}")]
    public async Task<IActionResult> AnalyzeFile(int fileId)
    {
        var file = await _repository.GetFileInfoAsync(fileId);
        if (file == null)
            return NotFound();

        return View(file);
    }

    /// <summary>
    /// Reports listing page
    /// </summary>
    [HttpGet("/reports")]
    public async Task<IActionResult> Reports()
    {
        var files = await _repository.GetAllFilesAsync();
        return View(files);
    }

    /// <summary>
    /// View a specific analysis report
    /// </summary>
    [HttpGet("/report/{fileId}/{reportType}")]
    public async Task<IActionResult> ViewReport(int fileId, string reportType)
    {
        var file = await _repository.GetFileInfoAsync(fileId);
        if (file == null)
            return NotFound();

        ViewBag.ReportType = reportType;
        ViewBag.FileId = fileId;
        return View(file);
    }

    /// <summary>
    /// Settings page (editable)
    /// </summary>
    [HttpGet("/settings")]
    public IActionResult Settings()
    {
        return View("SettingsEdit");
    }

    /// <summary>
    /// Grid view for analysis reports with filtering
    /// </summary>
    [HttpGet("/report-grid")]
    public IActionResult ReportGrid()
    {
        return View();
    }
}
