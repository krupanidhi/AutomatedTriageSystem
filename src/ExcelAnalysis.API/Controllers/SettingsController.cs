using Microsoft.AspNetCore.Mvc;
using ExcelAnalysis.Core.Models;
using System.Text.Json;

namespace ExcelAnalysis.API.Controllers;

/// <summary>
/// Controller for managing AI settings
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SettingsController> _logger;
    private readonly string _settingsFilePath;

    public SettingsController(IConfiguration configuration, ILogger<SettingsController> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _settingsFilePath = Path.Combine(AppContext.BaseDirectory, "ai-settings.json");
    }

    /// <summary>
    /// Get current AI settings
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AISettings), StatusCodes.Status200OK)]
    public IActionResult GetSettings()
    {
        try
        {
            // Try to load from custom settings file first
            if (System.IO.File.Exists(_settingsFilePath))
            {
                var json = System.IO.File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AISettings>(json);
                if (settings != null)
                {
                    // Mask API keys for security
                    settings = MaskApiKeys(settings);
                    return Ok(settings);
                }
            }

            // Fall back to appsettings.json
            var aiSettings = new AISettings
            {
                Provider = _configuration["AI:Provider"] ?? "Claude",
                UseFastSentiment = bool.Parse(_configuration["AI:UseFastSentiment"] ?? "true"),
                UseDynamicKeywords = bool.Parse(_configuration["AI:UseDynamicKeywords"] ?? "true"),
                Claude = new ClaudeSettings
                {
                    ApiKey = MaskKey(_configuration["AI:Claude:ApiKey"] ?? ""),
                    Model = _configuration["AI:Claude:Model"] ?? "claude-opus-4-20250514",
                    DelayBetweenCallsMs = int.Parse(_configuration["AI:Claude:DelayBetweenCallsMs"] ?? "0"),
                    MaxTokensPerRequest = int.Parse(_configuration["AI:Claude:MaxTokensPerRequest"] ?? "1024"),
                    EnableBatching = bool.Parse(_configuration["AI:Claude:EnableBatching"] ?? "true"),
                    BatchSize = int.Parse(_configuration["AI:Claude:BatchSize"] ?? "10")
                },
                Gemini = new GeminiSettings
                {
                    ApiKey = MaskKey(_configuration["AI:Gemini:ApiKey"] ?? ""),
                    Model = _configuration["AI:Gemini:Model"] ?? "gemini-1.5-pro",
                    DelayBetweenCallsMs = int.Parse(_configuration["AI:Gemini:DelayBetweenCallsMs"] ?? "12000")
                },
                OpenAI = new OpenAISettings
                {
                    ApiKey = MaskKey(_configuration["AI:OpenAI:ApiKey"] ?? ""),
                    Model = _configuration["AI:OpenAI:Model"] ?? "gpt-4o-mini",
                    Endpoint = _configuration["AI:OpenAI:Endpoint"] ?? "https://api.openai.com/v1"
                },
                Ollama = new OllamaSettings
                {
                    Endpoint = _configuration["AI:Ollama:Endpoint"] ?? "http://localhost:11434",
                    Model = _configuration["AI:Ollama:Model"] ?? "llama3.2"
                }
            };

            return Ok(aiSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings");
            return StatusCode(500, $"Error loading settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Save AI settings
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveSettings([FromBody] AISettings settings)
    {
        try
        {
            if (settings == null)
                return BadRequest("Settings cannot be null");

            // Validate provider
            var validProviders = new[] { "Claude", "Gemini", "OpenAI", "Ollama" };
            if (!validProviders.Contains(settings.Provider))
                return BadRequest($"Invalid provider. Must be one of: {string.Join(", ", validProviders)}");

            // Save to custom settings file
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            await System.IO.File.WriteAllTextAsync(_settingsFilePath, json);
            
            _logger.LogInformation("AI settings saved successfully. Provider: {Provider}", settings.Provider);

            return Ok(new 
            { 
                message = "Settings saved successfully. Restart the application for changes to take effect.",
                requiresRestart = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings");
            return StatusCode(500, $"Error saving settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Test AI connection with current settings
    /// </summary>
    [HttpPost("test-connection")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult TestConnection([FromBody] AISettings settings)
    {
        try
        {
            // Basic validation
            var errors = new List<string>();

            switch (settings.Provider)
            {
                case "Claude":
                    if (string.IsNullOrWhiteSpace(settings.Claude.ApiKey) || settings.Claude.ApiKey.Contains("*"))
                        errors.Add("Claude API key is required");
                    break;
                case "Gemini":
                    if (string.IsNullOrWhiteSpace(settings.Gemini.ApiKey) || settings.Gemini.ApiKey.Contains("*"))
                        errors.Add("Gemini API key is required");
                    break;
                case "OpenAI":
                    if (string.IsNullOrWhiteSpace(settings.OpenAI.ApiKey) || settings.OpenAI.ApiKey.Contains("*"))
                        errors.Add("OpenAI API key is required");
                    break;
                case "Ollama":
                    if (string.IsNullOrWhiteSpace(settings.Ollama.Endpoint))
                        errors.Add("Ollama endpoint is required");
                    break;
            }

            if (errors.Any())
            {
                return Ok(new 
                { 
                    success = false, 
                    message = string.Join(", ", errors) 
                });
            }

            return Ok(new 
            { 
                success = true, 
                message = $"{settings.Provider} configuration looks valid. Save settings and restart to apply." 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection");
            return Ok(new 
            { 
                success = false, 
                message = $"Error: {ex.Message}" 
            });
        }
    }

    /// <summary>
    /// Reset settings to defaults
    /// </summary>
    [HttpPost("reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ResetSettings()
    {
        try
        {
            if (System.IO.File.Exists(_settingsFilePath))
            {
                System.IO.File.Delete(_settingsFilePath);
                _logger.LogInformation("Settings reset to defaults");
            }

            return Ok(new 
            { 
                message = "Settings reset to defaults. Restart the application for changes to take effect." 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting settings");
            return StatusCode(500, $"Error resetting settings: {ex.Message}");
        }
    }

    private string MaskKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length < 8)
            return string.Empty;
        
        return key.Substring(0, 4) + "****" + key.Substring(key.Length - 4);
    }

    private AISettings MaskApiKeys(AISettings settings)
    {
        settings.Claude.ApiKey = MaskKey(settings.Claude.ApiKey);
        settings.Gemini.ApiKey = MaskKey(settings.Gemini.ApiKey);
        settings.OpenAI.ApiKey = MaskKey(settings.OpenAI.ApiKey);
        return settings;
    }
}
