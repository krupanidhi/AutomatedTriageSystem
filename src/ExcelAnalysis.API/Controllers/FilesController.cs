using Microsoft.AspNetCore.Mvc;
using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Core.Models;

namespace ExcelAnalysis.API.Controllers;

/// <summary>
/// Controller for file upload and management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IAnalysisRepository _repository;
    private readonly IExcelProcessor _excelProcessor;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IAnalysisRepository repository,
        IExcelProcessor excelProcessor,
        ILogger<FilesController> logger)
    {
        _repository = repository;
        _excelProcessor = excelProcessor;
        _logger = logger;
    }

    /// <summary>
    /// Upload an Excel file for analysis
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ExcelFileInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
                return BadRequest("Only Excel files (.xlsx, .xls) are supported");

            if (file.Length > 50 * 1024 * 1024) // 50 MB limit
                return BadRequest("File size exceeds 50 MB limit");

            _logger.LogInformation("Uploading file: {FileName}, Size: {Size} bytes", file.FileName, file.Length);

            // Process the Excel file directly from the uploaded stream
            using (var stream = file.OpenReadStream())
            {
                var fileInfo = await _excelProcessor.ProcessFileAsync(stream, file.FileName);
                
                // Save to database
                var fileId = await _repository.SaveFileInfoAsync(fileInfo);
                fileInfo.Id = fileId;

                _logger.LogInformation("File uploaded successfully: {FileName}, ID: {FileId}", file.FileName, fileId);

                return Ok(fileInfo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
            return StatusCode(500, $"Error uploading file: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all uploaded files with their analysis results
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ExcelFileInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllFiles()
    {
        try
        {
            var files = await _repository.GetAllFilesAsync();
            var allAnalysisResults = await _repository.GetAllAnalysisResultsAsync();
            
            // Group analysis results by file ID and attach to files
            var analysisResultsByFileId = allAnalysisResults
                .GroupBy(a => a.ExcelFileInfoId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.AnalyzedAt).ToList());
            
            // Create response with multiple analysis results per file
            var filesWithAllAnalyses = files.Select(f =>
            {
                var analysisResults = analysisResultsByFileId.ContainsKey(f.Id) 
                    ? analysisResultsByFileId[f.Id].Select(a => new
                    {
                        a.Id,
                        a.AnalysisType,
                        a.AnalyzedAt,
                        a.ExecutiveSummary,
                        a.OverallSentimentScore,
                        a.RawResultJson
                    })
                    : Enumerable.Empty<object>().Select(x => new
                    {
                        Id = 0,
                        AnalysisType = "",
                        AnalyzedAt = DateTime.MinValue,
                        ExecutiveSummary = "",
                        OverallSentimentScore = 0.0,
                        RawResultJson = ""
                    });
                
                return new
                {
                    f.Id,
                    f.FileName,
                    f.UploadedAt,
                    f.FileSizeBytes,
                    f.TotalRows,
                    f.TotalColumns,
                    f.SheetNames,
                    f.ColumnNames,
                    f.FileHash,
                    AnalysisResults = analysisResults.ToList()
                };
            }).ToList();
            
            return Ok(filesWithAllAnalyses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving files");
            return StatusCode(500, $"Error retrieving files: {ex.Message}");
        }
    }

    /// <summary>
    /// Get file by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ExcelFileInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(int id)
    {
        try
        {
            var file = await _repository.GetFileInfoAsync(id);
            if (file == null)
                return NotFound($"File with ID {id} not found");

            return Ok(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file {FileId}", id);
            return StatusCode(500, $"Error retrieving file: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete file by ID
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile(int id)
    {
        try
        {
            var file = await _repository.GetFileInfoAsync(id);
            if (file == null)
                return NotFound($"File with ID {id} not found");

            await _repository.DeleteFileAsync(id);
            
            _logger.LogInformation("File deleted: {FileName}, ID: {FileId}", file.FileName, id);

            return Ok(new { message = "File deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId}", id);
            return StatusCode(500, $"Error deleting file: {ex.Message}");
        }
    }

    /// <summary>
    /// Download file by ID
    /// </summary>
    [HttpGet("{id}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(int id)
    {
        try
        {
            var file = await _repository.GetFileInfoAsync(id);
            if (file == null)
                return NotFound($"File with ID {id} not found");

            // Note: This is a placeholder. In a real implementation, you'd need to store
            // the actual file content or path to retrieve it
            return NotFound("File download not implemented - file content not stored");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId}", id);
            return StatusCode(500, $"Error downloading file: {ex.Message}");
        }
    }
}
