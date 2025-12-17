using ExcelAnalysis.Core.Models;
using ExcelAnalysis.Core.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ExcelAnalysis.Infrastructure.Services;

/// <summary>
/// Service to communicate with Python Semantic Analysis microservice
/// </summary>
public class SemanticAnalyzerService
{
    private readonly HttpClient _httpClient;
    private readonly string _serviceUrl;
    private readonly ILogger<SemanticAnalyzerService> _logger;

    public SemanticAnalyzerService(IHttpClientFactory httpClientFactory, ILogger<SemanticAnalyzerService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("SemanticService");
        _serviceUrl = "http://localhost:5001";
        _logger = logger;
    }

    /// <summary>
    /// Check if semantic service is available
    /// </summary>
    public async Task<bool> IsServiceAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_serviceUrl}/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Semantic service not available: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Perform semantic analysis on comments
    /// </summary>
    public async Task<SemanticAnalysisResult?> AnalyzeCommentsAsync(List<CommentData> comments)
    {
        try
        {
            // Filter out comments with empty organization names before sending to semantic service
            var validComments = comments.Where(c =>
            {
                var orgKey = c.RowData?.Keys.FirstOrDefault(k =>
                    k.Equals("Organization", StringComparison.OrdinalIgnoreCase) ||
                    k.Equals("Grantee", StringComparison.OrdinalIgnoreCase) ||
                    k.Equals("Grantee Name", StringComparison.OrdinalIgnoreCase) ||
                    k.Equals("Organization Name", StringComparison.OrdinalIgnoreCase));
                var orgName = orgKey != null ? c.RowData[orgKey]?.ToString()?.Trim() : null;
                return !string.IsNullOrWhiteSpace(orgName);
            }).ToList();

            var skippedCount = comments.Count - validComments.Count;
            if (skippedCount > 0)
            {
                _logger.LogInformation($"Filtered out {skippedCount} comments with missing organization names");
            }

            _logger.LogInformation($"Sending {validComments.Count} comments to semantic service");

            var request = new
            {
                comments = validComments.Select(c => c.Comment).ToList(),
                organizations = validComments.Select(c => 
                {
                    var orgKey = c.RowData?.Keys.FirstOrDefault(k =>
                        k.Equals("Organization", StringComparison.OrdinalIgnoreCase) ||
                        k.Equals("Grantee", StringComparison.OrdinalIgnoreCase) ||
                        k.Equals("Grantee Name", StringComparison.OrdinalIgnoreCase) ||
                        k.Equals("Organization Name", StringComparison.OrdinalIgnoreCase));
                    return c.RowData[orgKey]?.ToString()?.Trim() ?? "";
                }).ToList()
            };

            var response = await _httpClient.PostAsJsonAsync($"{_serviceUrl}/analyze", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Semantic service error: {error}");
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<SemanticAnalysisResult>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogInformation($"Semantic analysis complete: {result?.Themes.Count ?? 0} themes identified");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error calling semantic service: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Find similar comments to a query
    /// </summary>
    public async Task<List<(string Comment, double Similarity)>?> FindSimilarCommentsAsync(string query, List<string> candidates)
    {
        try
        {
            var request = new { query, candidates };
            var response = await _httpClient.PostAsJsonAsync($"{_serviceUrl}/similarity", request);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<SimilarityResponse>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Results.Select(r => (r.Text, r.Similarity)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error finding similar comments: {ex.Message}");
            return null;
        }
    }

    private class SimilarityResponse
    {
        public string Query { get; set; } = string.Empty;
        public List<SimilarityResult> Results { get; set; } = new();
    }

    private class SimilarityResult
    {
        public string Text { get; set; } = string.Empty;
        public double Similarity { get; set; }
        public int Rank { get; set; }
    }
}
