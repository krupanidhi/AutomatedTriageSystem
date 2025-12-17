namespace ExcelAnalysis.Core.Models;

/// <summary>
/// AI configuration settings
/// </summary>
public class AISettings
{
    public string Provider { get; set; } = "Claude"; // Claude, Gemini, OpenAI, Ollama
    public bool UseFastSentiment { get; set; } = true;
    public bool UseDynamicKeywords { get; set; } = true;
    
    public ClaudeSettings Claude { get; set; } = new();
    public GeminiSettings Gemini { get; set; } = new();
    public OpenAISettings OpenAI { get; set; } = new();
    public OllamaSettings Ollama { get; set; } = new();
}

public class ClaudeSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-opus-4-20250514";
    public int DelayBetweenCallsMs { get; set; } = 0;
    public int MaxTokensPerRequest { get; set; } = 1024;
    public bool EnableBatching { get; set; } = true;
    public int BatchSize { get; set; } = 10;
}

public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-1.5-pro";
    public int DelayBetweenCallsMs { get; set; } = 12000;
}

public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public string Endpoint { get; set; } = "https://api.openai.com/v1";
}

public class OllamaSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.2";
}
