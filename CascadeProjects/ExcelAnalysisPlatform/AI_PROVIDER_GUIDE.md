# 🤖 AI Provider Configuration Guide

**Version**: 1.4  
**Date**: December 12, 2025  
**Status**: ✅ **READY TO USE**

---

## 🎯 Overview

Your Excel Analysis Platform now supports **THREE AI options**:

1. **Ollama llama3.2** (Current) - Fast, free, local
2. **Ollama Better Models** - Higher quality, still free, local
3. **OpenAI GPT** - Best quality, cloud-based, paid

---

## 📋 Quick Start - Choose Your Provider

### Option 1: Ollama llama3.2 (Current - Default)

**Already configured and working!**

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

**Pros:**
- ✅ Already installed
- ✅ 100% free
- ✅ Fast processing
- ✅ No API keys needed

**Cons:**
- ❌ Good but not best quality
- ❌ Smaller model (2GB)

---

### Option 2: Ollama Better Models (Recommended Upgrade)

**Install a better free model:**

```powershell
# Option A: llama3.1 (8GB) - Better reasoning
ollama pull llama3.1:8b

# Option B: mistral (4GB) - Great for analysis
ollama pull mistral

# Option C: phi3 (2GB) - Faster, still good
ollama pull phi3

# Option D: gemma2 (9GB) - Google's model
ollama pull gemma2:9b
```

**Update appsettings.json:**

```json
{
  "AI": {
    "Provider": "Ollama",
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "Model": "llama3.1:8b"
    }
  }
}
```

**Pros:**
- ✅ Still 100% free
- ✅ Better quality than llama3.2
- ✅ More accurate risk detection
- ✅ Better mitigations

**Cons:**
- ❌ Larger download (4-9GB)
- ❌ Slower processing (bigger model)

---

### Option 3: OpenAI GPT (Premium Quality)

**Get an API key:**
1. Go to https://platform.openai.com/api-keys
2. Create account (requires payment method)
3. Generate API key (starts with `sk-...`)

**Update appsettings.json:**

```json
{
  "AI": {
    "Provider": "OpenAI",
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "Model": "llama3.2"
    },
    "OpenAI": {
      "ApiKey": "sk-your-api-key-here",
      "Model": "gpt-4o-mini",
      "Endpoint": "https://api.openai.com/v1"
    }
  }
}
```

**Pros:**
- ✅ Best quality analysis
- ✅ Most accurate risk detection
- ✅ Superior mitigations
- ✅ Faster than large Ollama models

**Cons:**
- ❌ Costs money (~$0.02-0.30 per analysis)
- ❌ Requires internet
- ❌ Data sent to OpenAI

---

## 💰 Cost Comparison

### For Your 53-Row Excel File

| Provider | Model | Cost per Analysis | Quality | Speed |
|----------|-------|------------------|---------|-------|
| **Ollama** | llama3.2 | **FREE** | ⭐⭐⭐ | ⚡⚡⚡ |
| **Ollama** | llama3.1:8b | **FREE** | ⭐⭐⭐⭐ | ⚡⚡ |
| **Ollama** | mistral | **FREE** | ⭐⭐⭐⭐ | ⚡⚡⚡ |
| **OpenAI** | gpt-3.5-turbo | ~$0.05 | ⭐⭐⭐⭐ | ⚡⚡⚡ |
| **OpenAI** | gpt-4o-mini | ~$0.02 | ⭐⭐⭐⭐⭐ | ⚡⚡⚡ |
| **OpenAI** | gpt-4o | ~$0.30 | ⭐⭐⭐⭐⭐ | ⚡⚡ |

---

## 🔧 Configuration Files

### appsettings.json Location
```
C:\Users\KPeterson\CascadeProjects\ExcelAnalysisPlatform\src\ExcelAnalysis.API\appsettings.json
```

### Full Configuration Template

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=analysis.db"
  },
  "AI": {
    "Provider": "Ollama",
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "Model": "llama3.2"
    },
    "OpenAI": {
      "ApiKey": "",
      "Model": "gpt-4o-mini",
      "Endpoint": "https://api.openai.com/v1"
    }
  }
}
```

---

## 🚀 How to Switch Providers

### Switch to Better Ollama Model

1. **Install the model:**
   ```powershell
   ollama pull llama3.1:8b
   ```

2. **Edit appsettings.json:**
   ```json
   "AI": {
     "Provider": "Ollama",
     "Ollama": {
       "Model": "llama3.1:8b"
     }
   }
   ```

3. **Restart API:**
   ```powershell
   # Stop current API (Ctrl+C)
   cd C:\Users\KPeterson\CascadeProjects\ExcelAnalysisPlatform\src\ExcelAnalysis.API
   dotnet run --urls "http://localhost:5100"
   ```

### Switch to OpenAI

1. **Get API key from OpenAI**

2. **Edit appsettings.json:**
   ```json
   "AI": {
     "Provider": "OpenAI",
     "OpenAI": {
       "ApiKey": "sk-your-actual-key-here",
       "Model": "gpt-4o-mini"
     }
   }
   ```

3. **Restart API**

### Switch Back to Default

1. **Edit appsettings.json:**
   ```json
   "AI": {
     "Provider": "Ollama",
     "Ollama": {
       "Model": "llama3.2"
     }
   }
   ```

2. **Restart API**

---

## 📊 Model Comparison Details

### Ollama Models

#### llama3.2 (Current)
- **Size**: 2GB
- **Speed**: Very Fast
- **Quality**: Good
- **Best For**: Quick demos, testing
- **Install**: `ollama pull llama3.2`

#### llama3.1:8b (Recommended Upgrade)
- **Size**: 8GB
- **Speed**: Medium
- **Quality**: Excellent
- **Best For**: Production use, better accuracy
- **Install**: `ollama pull llama3.1:8b`

#### mistral
- **Size**: 4GB
- **Speed**: Fast
- **Quality**: Excellent
- **Best For**: Code analysis, technical content
- **Install**: `ollama pull mistral`

#### phi3
- **Size**: 2GB
- **Speed**: Very Fast
- **Quality**: Good
- **Best For**: Resource-constrained systems
- **Install**: `ollama pull phi3`

#### gemma2:9b
- **Size**: 9GB
- **Speed**: Medium
- **Quality**: Excellent
- **Best For**: Multilingual content
- **Install**: `ollama pull gemma2:9b`

### OpenAI Models

#### gpt-3.5-turbo
- **Cost**: $0.50/$1.50 per 1M tokens
- **Speed**: Very Fast
- **Quality**: Excellent
- **Best For**: Budget-conscious production

#### gpt-4o-mini (Recommended)
- **Cost**: $0.15/$0.60 per 1M tokens
- **Speed**: Very Fast
- **Quality**: Outstanding
- **Best For**: Best value for quality

#### gpt-4o
- **Cost**: $2.50/$10.00 per 1M tokens
- **Speed**: Fast
- **Quality**: Best Available
- **Best For**: Critical analysis, highest accuracy

---

## 🧪 Testing Different Providers

### Test Script

```powershell
# Function to test a provider
function Test-AIProvider {
    param($providerName)
    
    Write-Host "`n🧪 Testing with: $providerName" -ForegroundColor Cyan
    
    # Upload file
    $filePath = "C:\Users\KPeterson\Downloads\docanalysis\SAPR2-MAY-2023.xlsx"
    Add-Type -AssemblyName System.Net.Http
    $client = New-Object System.Net.Http.HttpClient
    $content = New-Object System.Net.Http.MultipartFormDataContent
    $fileStream = [System.IO.File]::OpenRead($filePath)
    $fileContent = New-Object System.Net.Http.StreamContent($fileStream)
    $content.Add($fileContent, "file", "SAPR2-MAY-2023.xlsx")
    $response = $client.PostAsync("http://localhost:5100/api/Analysis/upload", $content).Result
    $result = $response.Content.ReadAsStringAsync().Result
    $fileStream.Close()
    $fileId = ($result | ConvertFrom-Json).id
    
    # Analyze
    $startTime = Get-Date
    $analysis = Invoke-RestMethod -Uri "http://localhost:5100/api/Analysis/$fileId/analyze" -Method Post
    $duration = ((Get-Date) - $startTime).TotalSeconds
    
    # Results
    $results = Invoke-RestMethod -Uri "http://localhost:5100/api/Analysis/$fileId/results" -Method Get
    
    Write-Host "✅ Completed in $([math]::Round($duration, 1))s" -ForegroundColor Green
    Write-Host "   Risks Found: $($results.riskItems.Count)" -ForegroundColor Yellow
    Write-Host "   Completion: $([math]::Round($results.completionPercentage, 1))%" -ForegroundColor White
    
    # Save results
    $results | ConvertTo-Json -Depth 10 | Out-File "test-$providerName.json"
}

# Test current provider
Test-AIProvider "current"
```

---

## 💡 Recommendations

### For Demo/Testing
**Use**: Ollama llama3.2 (current)
- Already working
- Fast enough
- Free

### For Better Quality (Still Free)
**Use**: Ollama llama3.1:8b or mistral
- Significantly better accuracy
- Still completely free
- Worth the extra 2-3 minutes

### For Production/Critical Analysis
**Use**: OpenAI gpt-4o-mini
- Best quality/cost ratio
- Very accurate
- Only ~$0.02 per analysis

### For Maximum Accuracy
**Use**: OpenAI gpt-4o
- Absolute best quality
- Worth it for important decisions
- ~$0.30 per analysis

---

## 🔒 Security Notes

### Ollama (Local)
- ✅ Data never leaves your machine
- ✅ Complete privacy
- ✅ No API keys to manage
- ✅ Works offline

### OpenAI (Cloud)
- ⚠️ Data sent to OpenAI servers
- ⚠️ Requires API key (keep secret!)
- ⚠️ Subject to OpenAI's terms
- ⚠️ Requires internet connection

**Important**: Never commit API keys to Git!

---

## 🐛 Troubleshooting

### "OpenAI API key is required"
**Solution**: Add your API key to appsettings.json:
```json
"OpenAI": {
  "ApiKey": "sk-your-key-here"
}
```

### "Model not found" (Ollama)
**Solution**: Pull the model first:
```powershell
ollama pull llama3.1:8b
```

### "Connection refused" (Ollama)
**Solution**: Start Ollama:
```powershell
# Check if running
Get-Process | Where-Object { $_.ProcessName -like "*ollama*" }

# If not running, Ollama should auto-start
# Or manually start from Start Menu
```

### OpenAI Rate Limits
**Solution**: 
- Use gpt-4o-mini (higher limits)
- Add delays between requests
- Upgrade OpenAI plan

---

## 📈 Performance Expectations

### Processing Time (53 deliverables)

| Provider | Model | Time | Quality |
|----------|-------|------|---------|
| Ollama | llama3.2 | 4-6 min | Good |
| Ollama | llama3.1:8b | 6-10 min | Excellent |
| Ollama | mistral | 5-8 min | Excellent |
| OpenAI | gpt-4o-mini | 3-5 min | Outstanding |
| OpenAI | gpt-4o | 4-6 min | Best |

---

## ✅ Current Status

**Installed Providers:**
- ✅ Ollama (llama3.2) - Working
- ✅ OpenAI - Ready (needs API key)

**Available Models:**
```powershell
# Check installed Ollama models
ollama list
```

**Active Provider:**
Check `appsettings.json` → `AI:Provider`

---

## 🎯 Next Steps

1. **Choose your provider** (see recommendations above)
2. **Update appsettings.json** if switching
3. **Restart the API**
4. **Test with your Excel file**
5. **Compare results** if trying multiple providers

---

**Need Help?**
- Ollama models: https://ollama.com/library
- OpenAI pricing: https://openai.com/api/pricing
- API documentation: http://localhost:5100 (Swagger)

---

*Excel Analysis Platform v1.4 - Multi-Provider AI Support*  
*Last Updated: December 12, 2025*
