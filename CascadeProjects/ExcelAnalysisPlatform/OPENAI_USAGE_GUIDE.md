# 🔍 OpenAI API Usage & Monitoring Guide

**Your API Key**: Configured ✅  
**Current Provider**: Ollama (switch to OpenAI to use your key)

---

## 💰 OpenAI Pricing & Free Tier

### ❌ No Free Tier
**Important**: OpenAI does **NOT** have a free tier. All API calls are paid.

However, new accounts get:
- **$5 free credits** (expires after 3 months)
- Good for ~250-500 analyses with gpt-4o-mini

### Cost Per Analysis (Your Excel File)

| Model | Cost per Analysis | Analyses per $5 |
|-------|------------------|-----------------|
| **gpt-4o-mini** | ~$0.02 | ~250 |
| **gpt-3.5-turbo** | ~$0.05 | ~100 |
| **gpt-4o** | ~$0.30 | ~16 |
| **gpt-4-turbo** | ~$1.20 | ~4 |

**Recommendation**: Use **gpt-4o-mini** - best quality/cost ratio

---

## 📊 How to Monitor API Usage

### 1. OpenAI Dashboard (Recommended)

**View Usage:**
1. Go to https://platform.openai.com/usage
2. Login with your account
3. See real-time usage and costs

**What You'll See:**
- Total tokens used
- Cost per day/month
- Breakdown by model
- Remaining credits

### 2. Check API Call Success in Logs

**View API Logs:**
```powershell
# When running the API, you'll see logs like:
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: ExcelAnalysis.API.Controllers.AnalysisController[0]
      Starting analysis for file ID 1
```

**Successful API Calls:**
- ✅ No errors in console
- ✅ Analysis completes normally
- ✅ Results returned with data

**Failed API Calls:**
- ❌ Error messages in console
- ❌ "OpenAI API key is required" error
- ❌ "Rate limit exceeded" error
- ❌ "Invalid API key" error

### 3. OpenAI API Response Codes

| Code | Meaning | Action |
|------|---------|--------|
| 200 | ✅ Success | API call worked |
| 401 | ❌ Invalid API key | Check your key |
| 429 | ❌ Rate limit | Wait or upgrade plan |
| 500 | ❌ OpenAI error | Retry later |

---

## 🧪 Test OpenAI Connection

### Quick Test Script

```powershell
# Switch to OpenAI provider
# Edit appsettings.json and set:
# "Provider": "OpenAI"

# Restart API
cd C:\Users\KPeterson\CascadeProjects\ExcelAnalysisPlatform\src\ExcelAnalysis.API
dotnet run --urls "http://localhost:5100"

# In another PowerShell window, test:
$filePath = "C:\Users\KPeterson\Downloads\docanalysis\SAPR2-MAY-2023.xlsx"

# Upload
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

Write-Host "✅ File uploaded: $fileId" -ForegroundColor Green

# Analyze with OpenAI
Write-Host "🤖 Analyzing with OpenAI GPT-4o-mini..." -ForegroundColor Cyan
$startTime = Get-Date

try {
    $analysis = Invoke-RestMethod -Uri "http://localhost:5100/api/Analysis/$fileId/analyze" -Method Post
    $duration = ((Get-Date) - $startTime).TotalSeconds
    
    Write-Host "✅ SUCCESS! Analysis completed in $([math]::Round($duration, 1))s" -ForegroundColor Green
    Write-Host "   OpenAI API calls were successful!" -ForegroundColor Green
    
    # Get results
    $results = Invoke-RestMethod -Uri "http://localhost:5100/api/Analysis/$fileId/results" -Method Get
    Write-Host "`n📊 Results:" -ForegroundColor Yellow
    Write-Host "   Risks: $($results.riskItems.Count)" -ForegroundColor White
    Write-Host "   Completion: $([math]::Round($results.completionPercentage, 1))%" -ForegroundColor White
    
} catch {
    Write-Host "❌ FAILED: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Check API logs for details" -ForegroundColor Yellow
}
```

---

## 📈 Expected Token Usage

### For Your 53-Row Excel File

**Estimated Tokens:**
- Input: ~15,000 tokens
- Output: ~5,000 tokens
- **Total**: ~20,000 tokens per analysis

**Cost Breakdown (gpt-4o-mini):**
- Input: 15,000 tokens × $0.15/1M = $0.00225
- Output: 5,000 tokens × $0.60/1M = $0.00300
- **Total**: ~$0.005-0.02 per analysis

### API Calls Made Per Analysis

Your analysis makes approximately:
1. **Risk Classification**: 30-50 calls (one per comment)
2. **Sentiment Analysis**: 20 calls (sampled)
3. **Mitigation Generation**: 5-15 calls (one per risk)
4. **Executive Summary**: 1 call
5. **Total**: ~60-90 API calls per analysis

**With Parallel Processing**: Completes in 3-5 minutes

---

## 🔒 Security Best Practices

### ✅ Do's
- ✅ Keep API key in `appsettings.json` (not in Git)
- ✅ Monitor usage regularly
- ✅ Set spending limits in OpenAI dashboard
- ✅ Use gpt-4o-mini for cost efficiency

### ❌ Don'ts
- ❌ Never commit API key to Git
- ❌ Don't share your API key
- ❌ Don't use gpt-4o unless needed (expensive)
- ❌ Don't forget to check your usage

---

## 🚨 Error Handling

### "Invalid API Key"
**Cause**: Wrong or expired key  
**Solution**: 
1. Check key at https://platform.openai.com/api-keys
2. Update `appsettings.json`
3. Restart API

### "Rate Limit Exceeded"
**Cause**: Too many requests  
**Solution**:
1. Wait a few minutes
2. Upgrade OpenAI plan
3. Switch to Ollama temporarily

### "Insufficient Credits"
**Cause**: Out of credits  
**Solution**:
1. Add payment method at https://platform.openai.com/account/billing
2. Or switch back to Ollama (free)

---

## 💡 Cost Optimization Tips

### 1. Use Hybrid Mode (Future Enhancement)
- Use Ollama for bulk classification (free)
- Use OpenAI for summaries only (minimal cost)
- Best of both worlds!

### 2. Reduce API Calls
- Current: Analyzes 50 comments
- Can reduce to 30 for faster/cheaper analysis

### 3. Use Cheaper Model
- gpt-4o-mini: $0.02 per analysis ✅
- gpt-3.5-turbo: $0.05 per analysis
- gpt-4o: $0.30 per analysis ❌

### 4. Monitor and Set Limits
1. Go to https://platform.openai.com/account/limits
2. Set monthly spending limit (e.g., $10)
3. Get email alerts at 75% usage

---

## 📊 View Your Usage

### OpenAI Dashboard
**URL**: https://platform.openai.com/usage

**What to Check:**
- **Today's Usage**: See immediate costs
- **This Month**: Track monthly spend
- **By Model**: See which model costs most
- **Credits Remaining**: Monitor free credits

### Example Usage Report
```
Date: Dec 12, 2025
Model: gpt-4o-mini
Requests: 85
Tokens: 21,450
Cost: $0.018
```

---

## 🔄 Switch Back to Free (Ollama)

**If costs are too high:**

Edit `appsettings.json`:
```json
{
  "AI": {
    "Provider": "Ollama"
  }
}
```

Restart API - back to 100% free!

---

## ✅ Verification Checklist

After running an analysis with OpenAI:

- [ ] Check console logs - no errors?
- [ ] Analysis completed successfully?
- [ ] Results look good quality?
- [ ] Check OpenAI dashboard - usage recorded?
- [ ] Cost is as expected (~$0.02)?

---

## 📞 Support

**OpenAI Issues:**
- Dashboard: https://platform.openai.com
- Documentation: https://platform.openai.com/docs
- Status: https://status.openai.com

**API Issues:**
- Check console logs
- Review `appsettings.json`
- Verify API key is correct

---

## 🎯 Summary

### Your Setup
- ✅ API Key: Configured
- ✅ Model: gpt-4o-mini (best value)
- ⏸️ Provider: Currently Ollama (switch to "OpenAI" to use)

### Costs
- **Free Credits**: $5 (new accounts)
- **Per Analysis**: ~$0.02
- **Analyses Available**: ~250 with free credits

### Monitoring
- **Dashboard**: https://platform.openai.com/usage
- **Logs**: Console output when API runs
- **Alerts**: Set spending limits

### Next Steps
1. Switch provider to "OpenAI" in `appsettings.json`
2. Restart API
3. Run test analysis
4. Check OpenAI dashboard for usage

---

*Last Updated: December 12, 2025*
