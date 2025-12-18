using ExcelAnalysis.Core.Interfaces;
using ExcelAnalysis.Core.Models;
using ExcelAnalysis.Infrastructure.Data;
using ExcelAnalysis.Infrastructure.Repositories;
using ExcelAnalysis.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Load custom AI settings if they exist (from editable settings UI)
var customSettingsPath = Path.Combine(AppContext.BaseDirectory, "ai-settings.json");
AISettings? customSettings = null;
if (File.Exists(customSettingsPath))
{
    try
    {
        var json = File.ReadAllText(customSettingsPath);
        customSettings = JsonSerializer.Deserialize<AISettings>(json);
        Console.WriteLine("✅ Loaded custom settings from ai-settings.json");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Failed to load ai-settings.json: {ex.Message}");
    }
}

// Add services to the container
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Excel Analysis Platform API", 
        Version = "v1",
        Description = "AI-powered Excel analysis for risk assessment and progress tracking"
    });
});

// Configure CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure Database
builder.Services.AddDbContext<AnalysisDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=analysis.db"));

// Register services
builder.Services.AddScoped<IExcelProcessor, ExcelProcessor>();
builder.Services.AddScoped<IAIAnalyzer>(sp =>
{
    var config = builder.Configuration;
    var logger = sp.GetRequiredService<ILogger<Program>>();
    
    // Use custom settings if available, otherwise fall back to appsettings.json
    var provider = customSettings?.Provider ?? config["AI:Provider"] ?? "Ollama";
    
    if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
    {
        var apiKey = customSettings?.Gemini.ApiKey ?? config["AI:Gemini:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Gemini API key is required when using Gemini provider. Set it in /settings or AI:Gemini:ApiKey in appsettings.json");
        }
        
        var modelName = customSettings?.Gemini.Model ?? config["AI:Gemini:Model"] ?? "gemini-1.5-pro";
        var useFastSentiment = customSettings?.UseFastSentiment ?? config.GetValue<bool>("AI:UseFastSentiment", true);
        var useDynamicKeywords = customSettings?.UseDynamicKeywords ?? config.GetValue<bool>("AI:UseDynamicKeywords", true);
        var delayBetweenCallsMs = customSettings?.Gemini.DelayBetweenCallsMs ?? config.GetValue<int>("AI:Gemini:DelayBetweenCallsMs", 12000);
        
        logger.LogInformation("🤖 AI PROVIDER: Google Gemini");
        logger.LogInformation("📊 MODEL: {ModelName}", modelName);
        logger.LogInformation("🌐 ENDPOINT: https://generativelanguage.googleapis.com");
        logger.LogInformation("💭 SENTIMENT: {SentimentMode}", useFastSentiment ? "Keyword-Based (Fast)" : "AI-Based");
        logger.LogInformation("🔤 KEYWORDS: {KeywordMode}", useDynamicKeywords ? "Dynamic (extracted from file)" : "Static (hardcoded)");
        logger.LogInformation("⏳ RATE LIMIT: {Delay}ms delay between calls", delayBetweenCallsMs);
        logger.LogInformation("🔑 API KEY: {ApiKeyPreview}...", apiKey.Substring(0, Math.Min(20, apiKey.Length)));
        
        return new GeminiAnalyzer(apiKey, modelName, useFastSentiment, useDynamicKeywords, delayBetweenCallsMs);
    }
    else if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
    {
        var apiKey = customSettings?.OpenAI.ApiKey ?? config["AI:OpenAI:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is required when using OpenAI provider. Set it in /settings or AI:OpenAI:ApiKey in appsettings.json");
        }
        
        var modelName = customSettings?.OpenAI.Model ?? config["AI:OpenAI:Model"] ?? "gpt-4o-mini";
        var endpoint = customSettings?.OpenAI.Endpoint ?? config["AI:OpenAI:Endpoint"] ?? "https://api.openai.com/v1";
        var useFastSentiment = customSettings?.UseFastSentiment ?? config.GetValue<bool>("AI:UseFastSentiment", true);
        var useDynamicKeywords = customSettings?.UseDynamicKeywords ?? config.GetValue<bool>("AI:UseDynamicKeywords", true);
        
        logger.LogInformation("🤖 AI PROVIDER: OpenAI");
        logger.LogInformation("📊 MODEL: {ModelName}", modelName);
        logger.LogInformation("🌐 ENDPOINT: {Endpoint}", endpoint);
        logger.LogInformation("💭 SENTIMENT: {SentimentMode}", useFastSentiment ? "Keyword-Based (Fast)" : "AI-Based");
        logger.LogInformation("🔤 KEYWORDS: {KeywordMode}", useDynamicKeywords ? "Dynamic (extracted from file)" : "Static (hardcoded)");
        logger.LogInformation("🔑 API KEY: {ApiKeyPreview}...", apiKey.Substring(0, Math.Min(20, apiKey.Length)));
        
        return new OpenAIAnalyzer(apiKey, modelName, endpoint, useFastSentiment, useDynamicKeywords);
    }
    else if (provider.Equals("Claude", StringComparison.OrdinalIgnoreCase))
    {
        var apiKey = customSettings?.Claude.ApiKey ?? config["AI:Claude:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Claude API key is required when using Claude provider. Set it in /settings or AI:Claude:ApiKey in appsettings.json");
        }
        
        var modelName = customSettings?.Claude.Model ?? config["AI:Claude:Model"] ?? "claude-sonnet-4-5-20250514";
        var useFastSentiment = customSettings?.UseFastSentiment ?? config.GetValue<bool>("AI:UseFastSentiment", true);
        var useDynamicKeywords = customSettings?.UseDynamicKeywords ?? config.GetValue<bool>("AI:UseDynamicKeywords", true);
        var delayBetweenCallsMs = customSettings?.Claude.DelayBetweenCallsMs ?? config.GetValue<int>("AI:Claude:DelayBetweenCallsMs", 0);
        var maxTokensPerRequest = customSettings?.Claude.MaxTokensPerRequest ?? config.GetValue<int>("AI:Claude:MaxTokensPerRequest", 1024);
        var enableBatching = customSettings?.Claude.EnableBatching ?? config.GetValue<bool>("AI:Claude:EnableBatching", true);
        var batchSize = customSettings?.Claude.BatchSize ?? config.GetValue<int>("AI:Claude:BatchSize", 10);
        
        logger.LogInformation("🤖 AI PROVIDER: Claude (Anthropic)");
        logger.LogInformation("📊 MODEL: {ModelName}", modelName);
        logger.LogInformation("🌐 ENDPOINT: https://api.anthropic.com");
        logger.LogInformation("💭 SENTIMENT: {SentimentMode}", useFastSentiment ? "Keyword-Based (Fast)" : "AI-Based");
        logger.LogInformation("🔤 KEYWORDS: {KeywordMode}", useDynamicKeywords ? "Dynamic (extracted from file)" : "Static (hardcoded)");
        logger.LogInformation("⏳ RATE LIMIT: {Delay}ms delay between calls", delayBetweenCallsMs);
        logger.LogInformation("🎯 TOKEN OPTIMIZATION: Max {MaxTokens} tokens/request", maxTokensPerRequest);
        logger.LogInformation("📦 BATCHING: {BatchStatus}", enableBatching ? $"Enabled ({batchSize} items/batch)" : "Disabled");
        logger.LogInformation("🔑 API KEY: {ApiKeyPreview}...", apiKey.Substring(0, Math.Min(20, apiKey.Length)));
        
        return new ClaudeAnalyzer(apiKey, modelName, useFastSentiment, useDynamicKeywords, delayBetweenCallsMs, maxTokensPerRequest, enableBatching, batchSize);
    }
    else
    {
        // Default to Ollama
        var excelProcessor = sp.GetRequiredService<IExcelProcessor>();
        var ollamaEndpoint = customSettings?.Ollama.Endpoint ?? config["AI:Ollama:Endpoint"] ?? "http://localhost:11434";
        var modelName = customSettings?.Ollama.Model ?? config["AI:Ollama:Model"] ?? "llama3.2";
        
        logger.LogInformation("🤖 AI PROVIDER: Ollama (Local)");
        logger.LogInformation("📊 MODEL: {ModelName}", modelName);
        logger.LogInformation("🌐 ENDPOINT: {Endpoint}", ollamaEndpoint);
        
        return new AIAnalyzer(excelProcessor, ollamaEndpoint, modelName);
    }
});

// Register EnhancedAIAnalyzer for comprehensive AI-based analysis
// Uses the configured AI provider (Claude, OpenAI, Gemini, or Ollama) from settings
builder.Services.AddScoped<EnhancedAIAnalyzer>(sp =>
{
    var excelProcessor = sp.GetRequiredService<IExcelProcessor>();
    var aiAnalyzer = sp.GetRequiredService<IAIAnalyzer>();
    
    return new EnhancedAIAnalyzer(excelProcessor, aiAnalyzer);
});

builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();

// Register Historical Tracking Service for trend analysis and remediation tracking
builder.Services.AddScoped<HistoricalTrackingService>();

// Register HTTP client for Semantic Service
builder.Services.AddHttpClient("SemanticService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5001");
    client.Timeout = TimeSpan.FromMinutes(5); // Semantic analysis can take time
});

// Register Semantic Analyzer Service
builder.Services.AddScoped<SemanticAnalyzerService>();

// Register Hybrid Analyzer combining Claude and Semantic analysis
builder.Services.AddScoped<HybridAnalyzer>();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Excel Analysis Platform API v1");
        c.RoutePrefix = "swagger"; // Move Swagger to /swagger instead of root
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Enable serving static files (CSS, JS, images)
app.UseCors("AllowFrontend");
app.UseRouting();
app.UseAuthorization();

// Map MVC routes for web UI
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=WebUI}/{action=Dashboard}/{id?}");

// Map API controllers
app.MapControllers();

app.Run();
