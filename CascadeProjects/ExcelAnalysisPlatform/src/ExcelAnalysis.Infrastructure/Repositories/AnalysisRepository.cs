using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Core.Models;
using ExcelAnalysis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExcelAnalysis.Infrastructure.Repositories;

public class AnalysisRepository : IAnalysisRepository
{
    private readonly AnalysisDbContext _context;

    public AnalysisRepository(AnalysisDbContext context)
    {
        _context = context;
    }

    public async Task<ExcelFileInfo?> GetFileInfoAsync(int id)
    {
        return await _context.ExcelFiles
            .Include(f => f.Rows)
            .Include(f => f.AnalysisResult)
                .ThenInclude(a => a!.RiskItems)
            .Include(f => f.AnalysisResult)
                .ThenInclude(a => a!.ProgressMetrics)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<List<ExcelFileInfo>> GetAllFilesAsync()
    {
        return await _context.ExcelFiles
            .Include(f => f.AnalysisResult)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync();
    }

    public async Task<List<AnalysisResult>> GetAllAnalysisResultsAsync()
    {
        return await _context.AnalysisResults
            .OrderByDescending(a => a.AnalyzedAt)
            .ToListAsync();
    }

    public async Task<int> SaveFileInfoAsync(ExcelFileInfo fileInfo)
    {
        if (fileInfo.Id == 0)
        {
            _context.ExcelFiles.Add(fileInfo);
        }
        else
        {
            _context.ExcelFiles.Update(fileInfo);
        }
        
        await _context.SaveChangesAsync();
        return fileInfo.Id;
    }

    public async Task DeleteFileAsync(int id)
    {
        var file = await _context.ExcelFiles.FindAsync(id);
        if (file != null)
        {
            _context.ExcelFiles.Remove(file);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<AnalysisResult?> GetAnalysisResultAsync(int fileId)
    {
        return await _context.AnalysisResults
            .Include(a => a.RiskItems)
            .Include(a => a.ProgressMetrics)
            .FirstOrDefaultAsync(a => a.ExcelFileInfoId == fileId);
    }

    public async Task<int> SaveAnalysisResultAsync(AnalysisResult analysisResult)
    {
        if (analysisResult.Id == 0)
        {
            _context.AnalysisResults.Add(analysisResult);
        }
        else
        {
            _context.AnalysisResults.Update(analysisResult);
        }
        
        await _context.SaveChangesAsync();
        return analysisResult.Id;
    }

    public async Task DeleteAnalysisResultAsync(int id)
    {
        var analysisResult = await _context.AnalysisResults.FindAsync(id);
        if (analysisResult != null)
        {
            _context.AnalysisResults.Remove(analysisResult);
            await _context.SaveChangesAsync();
        }
    }
}
