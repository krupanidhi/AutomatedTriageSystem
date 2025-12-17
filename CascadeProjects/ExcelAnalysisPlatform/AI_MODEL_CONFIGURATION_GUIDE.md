# AI Model Configuration Guide

## Overview

Your Excel Analysis Platform supports **4 AI providers**:
1. **Claude (Anthropic)** - Advanced AI with excellent sentiment analysis
2. **Gemini (Google)** - Free tier available, good performance
3. **OpenAI (GPT)** - Industry standard, reliable
4. **Ollama (Local)** - Run models locally, completely free

---

## Quick Start

### Step 1: Choose Your AI Provider

Open `src/ExcelAnalysis.API/appsettings.json` and set the `Provider`:

```json
{
  "AI": {
    "Provider": "Claude"  // Options: "Claude", "Gemini", "OpenAI", "Ollama"
  }
}
```

### Step 2: Configure Provider Settings

See detailed configurations below for each provider.

### Step 3: Restart Application

```powershell
# Stop current instance (Ctrl+C)
# Then restart:
cd C:\Users\KPeterson\CascadeProjects\ExcelAnalysisPlatform\src\ExcelAnalysis.API
dotnet run --urls "http://localhost:5200"
```

---

## Provider Configurations

### 1️⃣ Claude (Anthropic) - RECOMMENDED

**Best for:** Nuanced sentiment analysis, complex emotional understanding

**Get API Key:** https://console.anthropic.com/

**Configuration:**
```json
{
  "AI": {
    "Provider": "Claude",
    "UseFastSentiment": true,
    "UseDynamicKeywords": true,
    "Claude": {
      "ApiKey": "sk-ant-api03-YOUR_API_KEY_HERE",
      "Model": "claude-opus-4-20250514",
      "DelayBetweenCallsMs": 0,
      "MaxTokensPerRequest": 1024,
      "EnableBatching": true,
      "BatchSize": 10
    }
  }
}
```

**Available Models:**
- `claude-opus-4-20250514` - Most capable, best quality
- `claude-sonnet-4-20250514` - Balanced performance/cost
- `claude-3-5-sonnet-20241022` - Faster, lower cost

**Rate Limits:**
- Free tier: 5 requests/minute
- Paid tier: Higher limits

**Pricing:**
- ~$0.003 per 1K tokens
- Batching reduces costs by 80-90%

**Tips:**
- Keep `EnableBatching: true` to save costs
- Use `UseFastSentiment: true` for keyword-based sentiment (free)
- Set `UseFastSentiment: false` for AI-based sentiment (costs tokens)

---

### 2️⃣ Gemini (Google)

**Best for:** Free tier usage, good performance

**Get API Key:** https://makersuite.google.com/app/apikey

**Configuration:**
```json
{
  "AI": {
    "Provider": "Gemini",
    "UseFastSentiment": true,
    "UseDynamicKeywords": true,
    "Gemini": {
      "ApiKey": "YOUR_GEMINI_API_KEY_HERE",
      "Model": "gemini-1.5-pro",
      "DelayBetweenCallsMs": 12000
    }
  }
}
```

**Available Models:**
- `gemini-1.5-pro` - Most capable
- `gemini-1.5-flash` - Faster, lower cost

**Rate Limits:**
- Free tier: 2 requests/minute (hence 12s delay)
- Paid tier: 1000 requests/minute

**Pricing:**
- Free tier: 15 requests/minute
- Paid: ~$0.00125 per 1K tokens

**Tips:**
- Use `DelayBetweenCallsMs: 12000` for free tier
- Generous free tier makes it great for testing

---

### 3️⃣ OpenAI (GPT)

**Best for:** Industry-standard performance, reliability

**Get API Key:** https://platform.openai.com/api-keys

**Configuration:**
```json
{
  "AI": {
    "Provider": "OpenAI",
    "UseFastSentiment": true,
    "UseDynamicKeywords": true,
    "OpenAI": {
      "ApiKey": "sk-YOUR_OPENAI_API_KEY_HERE",
      "Model": "gpt-4o-mini",
      "Endpoint": "https://api.openai.com/v1"
    }
  }
}
```

**Available Models:**
- `gpt-4o` - Most capable ($5/1M input tokens)
- `gpt-4o-mini` - Cost-effective ($0.15/1M input tokens)
- `gpt-3.5-turbo` - Fast & cheap ($0.50/1M input tokens)

**Rate Limits:**
- Tier 1: 500 requests/minute
- Higher tiers: More generous

**Pricing:**
- Varies by model (see above)
- `gpt-4o-mini` recommended for cost/performance balance

**Tips:**
- Start with `gpt-4o-mini` for cost-effectiveness
- No delay needed due to generous rate limits

---

### 4️⃣ Ollama (Local)

**Best for:** Privacy, no API costs, offline usage

**Setup:**
1. Install Ollama: https://ollama.ai/
2. Pull a model: `ollama pull llama3.2`
3. Start Ollama service (runs automatically after install)

**Configuration:**
```json
{
  "AI": {
    "Provider": "Ollama",
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "Model": "llama3.2"
    }
  }
}
```

**Available Models:**
- `llama3.2` - Latest Llama, very capable
- `mistral` - Fast and capable
- `phi3` - Lightweight, runs on modest hardware
- `gemma2` - Google's open model

**Pull Models:**
```bash
ollama pull llama3.2
ollama pull mistral
ollama pull phi3
```

**Rate Limits:**
- None (runs locally)

**Pricing:**
- Free (requires local hardware)
- Needs: 8GB+ RAM for small models, 16GB+ for larger

**Tips:**
- Great for privacy-sensitive data
- No internet required after model download
- Performance depends on your hardware

---

## Advanced Settings

### UseFastSentiment

Controls how sentiment analysis is performed:

```json
"UseFastSentiment": true  // Keyword-based (fast, free)
"UseFastSentiment": false // AI-based (slower, costs tokens)
```

**Recommendation:** Keep `true` for 99% of use cases. Keyword-based sentiment is accurate and free.

---

### UseDynamicKeywords

Controls keyword extraction:

```json
"UseDynamicKeywords": true  // Extract from YOUR Excel file
"UseDynamicKeywords": false // Use hardcoded keyword list
```

**Recommendation:** Keep `true` to extract buzzwords specific to your data.

---

### DelayBetweenCallsMs

Milliseconds to wait between API calls:

```json
"DelayBetweenCallsMs": 0      // No delay (Claude, OpenAI)
"DelayBetweenCallsMs": 12000  // 12 second delay (Gemini free tier)
```

**When to use:**
- `0` - Claude (batching handles rate limits), OpenAI (generous limits)
- `12000` - Gemini free tier (2 requests/minute)
- `30000` - Very strict rate limits

---

### Batching (Claude Only)

Process multiple comments in one API call:

```json
"EnableBatching": true,
"BatchSize": 10
```

**Benefits:**
- Saves 80-90% on tokens
- Faster processing
- Fewer API calls

**Recommendation:** Always keep enabled for Claude.

---

## Configuration Examples

### Example 1: Free Setup (Gemini)

```json
{
  "AI": {
    "Provider": "Gemini",
    "UseFastSentiment": true,
    "UseDynamicKeywords": true,
    "Gemini": {
      "ApiKey": "YOUR_GEMINI_KEY",
      "Model": "gemini-1.5-flash",
      "DelayBetweenCallsMs": 12000
    }
  }
}
```

**Cost:** Free  
**Speed:** Slow (due to rate limits)  
**Best for:** Testing, low-budget projects

---

### Example 2: Best Performance (Claude)

```json
{
  "AI": {
    "Provider": "Claude",
    "UseFastSentiment": true,
    "UseDynamicKeywords": true,
    "Claude": {
      "ApiKey": "YOUR_CLAUDE_KEY",
      "Model": "claude-opus-4-20250514",
      "DelayBetweenCallsMs": 0,
      "MaxTokensPerRequest": 1024,
      "EnableBatching": true,
      "BatchSize": 10
    }
  }
}
```

**Cost:** ~$0.01-0.05 per analysis  
**Speed:** Fast  
**Best for:** Production use, best quality

---

### Example 3: Cost-Effective (OpenAI)

```json
{
  "AI": {
    "Provider": "OpenAI",
    "UseFastSentiment": true,
    "UseDynamicKeywords": true,
    "OpenAI": {
      "ApiKey": "YOUR_OPENAI_KEY",
      "Model": "gpt-4o-mini",
      "Endpoint": "https://api.openai.com/v1"
    }
  }
}
```

**Cost:** ~$0.005 per analysis  
**Speed:** Fast  
**Best for:** Budget-conscious production

---

### Example 4: Private/Offline (Ollama)

```json
{
  "AI": {
    "Provider": "Ollama",
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "Model": "llama3.2"
    }
  }
}
```

**Cost:** Free  
**Speed:** Depends on hardware  
**Best for:** Privacy, offline, no API costs

---

## Switching Between Providers

You can easily switch between providers by changing the `Provider` value:

```json
{
  "AI": {
    "Provider": "Claude",  // Change this to: "Gemini", "OpenAI", or "Ollama"
    // ... rest of config
  }
}
```

**After changing, restart the application.**

---

## Troubleshooting

### "API key is required" error

**Solution:** Add your API key to the correct provider section in `appsettings.json`

### "Model not found" error

**Solution:** 
- Check model name spelling
- Verify model is available for your API key tier
- Try a different model from the available list

### Rate limit errors

**Solution:**
- Increase `DelayBetweenCallsMs`
- Reduce analysis frequency
- Upgrade to paid tier

### Ollama connection error

**Solution:**
- Verify Ollama is running: `ollama list`
- Check endpoint: `http://localhost:11434`
- Pull the model: `ollama pull llama3.2`

---

## Cost Comparison

For analyzing 280 comments from 52 organizations:

| Provider | Model | Cost | Time |
|----------|-------|------|------|
| **Keyword** | N/A | $0.00 | 0.2s |
| **Claude** | opus-4 | ~$0.05 | 3-5 min |
| **Gemini** | 1.5-pro | Free | 5-10 min |
| **OpenAI** | gpt-4o-mini | ~$0.01 | 2-3 min |
| **Ollama** | llama3.2 | $0.00 | 1-5 min |

---

## Recommendations

### For Production
✅ **Claude Opus 4** - Best quality, worth the cost  
✅ **OpenAI GPT-4o-mini** - Great balance of cost/performance

### For Testing
✅ **Gemini 1.5-flash** - Free tier  
✅ **Ollama llama3.2** - Completely free, private

### For Budget-Conscious
✅ **Keyword-Based Analysis** - Free, fast, accurate  
✅ **OpenAI GPT-4o-mini** - Cheapest AI option

### For Privacy
✅ **Ollama** - Runs locally, no data leaves your machine  
✅ **Keyword-Based** - No external API calls

---

## Security Best Practices

### 1. Never Commit API Keys

Add to `.gitignore`:
```
appsettings.json
appsettings.*.json
```

### 2. Use Environment Variables (Optional)

Instead of hardcoding in `appsettings.json`:

```json
{
  "AI": {
    "Claude": {
      "ApiKey": "${CLAUDE_API_KEY}"
    }
  }
}
```

Set environment variable:
```powershell
$env:CLAUDE_API_KEY = "sk-ant-api03-..."
```

### 3. Use User Secrets (Recommended)

```powershell
dotnet user-secrets init
dotnet user-secrets set "AI:Claude:ApiKey" "sk-ant-api03-..."
```

---

## Getting Help

**Web UI Settings Page:** `http://localhost:5200/settings`

**Documentation:**
- Claude: https://docs.anthropic.com/
- Gemini: https://ai.google.dev/docs
- OpenAI: https://platform.openai.com/docs
- Ollama: https://ollama.ai/

---

**Last Updated:** December 15, 2025
