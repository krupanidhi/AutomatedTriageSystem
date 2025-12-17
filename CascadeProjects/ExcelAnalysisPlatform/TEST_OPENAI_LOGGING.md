# 🔍 Testing OpenAI API Logging

## ✅ What Was Added

### Console Logging Features

**1. Startup Logging**
When the API starts, you'll see which provider is active:
```
🤖 AI PROVIDER: OpenAI
📊 MODEL: gpt-4o-mini
🌐 ENDPOINT: https://api.openai.com/v1
🔑 API KEY: sk-proj-jGrUOudWe1Nr...
```

**2. Analysis Start Logging**
When analysis begins:
```
🔍 Starting OpenAI Analysis for file: SAPR2-MAY-2023.xlsx
   Using Model: gpt-4o-mini
   Extracted 53 comments and 120 questions
```

**3. API Call Tracking**
Every OpenAI API call is logged:
```
   🌐 OpenAI API Call #1: Risk Classification
   🌐 OpenAI API Call #2: Risk Classification
   🌐 OpenAI API Call #3: Risk Classification
   ...
   🌐 OpenAI API Call #45: Sentiment Analysis
   🌐 OpenAI API Call #46: Mitigation Generation
   ...
```

**4. Completion Summary**
When analysis finishes:
```
✅ OpenAI Analysis Complete!
   Total OpenAI API Calls: 87
   Risks Found: 12
   Estimated Cost: ~$0.0261
```

---

## 🧪 How to Test

### Step 1: Verify Configuration

Check `appsettings.json`:
```json
{
  "AI": {
    "Provider": "OpenAI",  // ← Should be "OpenAI"
    "OpenAI": {
      "ApiKey": "sk-proj-jGrUOudWe1Nr2AW0L4id...",  // ← Your key
      "Model": "gpt-4o-mini"
    }
  }
}
```

### Step 2: Start API and Watch Console

```powershell
cd C:\Users\KPeterson\CascadeProjects\ExcelAnalysisPlatform\src\ExcelAnalysis.API
dotnet run --urls "http://localhost:5100"
```

**Expected Output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5100
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### Step 3: Run Analysis

In a **NEW PowerShell window**:

```powershell
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

Write-Host "✅ File uploaded: $fileId" -ForegroundColor Green

# Analyze
Write-Host "🤖 Starting OpenAI analysis..." -ForegroundColor Cyan
Write-Host "👀 WATCH THE API CONSOLE for OpenAI logs!" -ForegroundColor Yellow
$analysis = Invoke-RestMethod -Uri "http://localhost:5100/api/Analysis/$fileId/analyze" -Method Post

Write-Host "✅ Analysis complete!" -ForegroundColor Green
```

### Step 4: Watch API Console

**Go back to the first PowerShell window** where the API is running.

**You should see:**

```
🚀 OpenAI Analyzer Initialized
   Model: gpt-4o-mini
   Endpoint: https://api.openai.com/v1
   API Key: sk-proj-jGrUOudWe1Nr...

🔍 Starting OpenAI Analysis for file: SAPR2-MAY-2023.xlsx
   Using Model: gpt-4o-mini
   Extracted 53 comments and 120 questions

   🌐 OpenAI API Call #1: Risk Classification
   🌐 OpenAI API Call #2: Risk Classification
   🌐 OpenAI API Call #3: Risk Classification
   🌐 OpenAI API Call #4: Risk Classification
   🌐 OpenAI API Call #5: Risk Classification
   ...
   🌐 OpenAI API Call #45: Sentiment Analysis
   🌐 OpenAI API Call #46: Sentiment Analysis
   ...
   🌐 OpenAI API Call #78: Mitigation Generation
   🌐 OpenAI API Call #79: Mitigation Generation
   ...
   🌐 OpenAI API Call #87: Executive Summary

✅ OpenAI Analysis Complete!
   Total OpenAI API Calls: 87
   Risks Found: 12
   Estimated Cost: ~$0.0261
```

---

## 🎯 What Each Log Means

### Provider Initialization
```
🤖 AI PROVIDER: OpenAI
```
✅ **Confirms**: You're using OpenAI, not Ollama

### Model Information
```
📊 MODEL: gpt-4o-mini
```
✅ **Confirms**: Which OpenAI model is being used

### API Key Preview
```
🔑 API KEY: sk-proj-jGrUOudWe1Nr...
```
✅ **Confirms**: Your API key is loaded (shows first 20 chars)

### API Call Counter
```
🌐 OpenAI API Call #45: Sentiment Analysis
```
✅ **Confirms**: Each call to OpenAI is tracked and logged

### Final Summary
```
Total OpenAI API Calls: 87
Estimated Cost: ~$0.0261
```
✅ **Confirms**: Total calls made and approximate cost

---

## 🔍 Verify OpenAI Dashboard

**After running analysis:**

1. Go to https://platform.openai.com/usage
2. Login with your account
3. You should see:
   - **Recent requests**: ~87 requests
   - **Tokens used**: ~20,000-30,000 tokens
   - **Cost**: ~$0.02-0.03

**If you see this, OpenAI is definitely being used!**

---

## 🆚 Compare: Ollama vs OpenAI Logs

### Ollama Logs (if you switch back)
```
🤖 AI PROVIDER: Ollama (Local)
📊 MODEL: llama3.2
🌐 ENDPOINT: http://localhost:11434
```

### OpenAI Logs (current)
```
🤖 AI PROVIDER: OpenAI
📊 MODEL: gpt-4o-mini
🌐 ENDPOINT: https://api.openai.com/v1
🔑 API KEY: sk-proj-jGrUOudWe1Nr...
```

**Easy to tell which one is active!**

---

## ❌ Troubleshooting

### "No OpenAI logs appearing"

**Check:**
1. Is `"Provider": "OpenAI"` in appsettings.json?
2. Did you restart the API after changing config?
3. Are you watching the correct console window?

### "Still seeing Ollama logs"

**Solution:**
```json
// Change this in appsettings.json
"AI": {
  "Provider": "OpenAI"  // ← Make sure this says "OpenAI"
}
```

Then restart API.

### "API calls but no cost on OpenAI dashboard"

**Wait a few minutes** - OpenAI dashboard updates every 5-10 minutes.

---

## 📊 Expected Results

### For Your 53-Row Excel File

| Metric | Expected Value |
|--------|---------------|
| **API Calls** | 60-90 calls |
| **Duration** | 3-5 minutes |
| **Cost** | $0.02-0.03 |
| **Risks Found** | 5-15 risks |

### Log Pattern

```
🚀 Initialization (once)
🔍 Analysis Start (once)
🌐 API Calls (60-90 times)
   - Risk Classification: 30-50 calls
   - Sentiment Analysis: 15-20 calls
   - Mitigation Generation: 5-15 calls
   - Executive Summary: 1 call
✅ Completion Summary (once)
```

---

## ✅ Success Checklist

After running your test, verify:

- [ ] Saw "🤖 AI PROVIDER: OpenAI" at startup
- [ ] Saw "🔍 Starting OpenAI Analysis" message
- [ ] Saw multiple "🌐 OpenAI API Call #X" messages
- [ ] Saw "✅ OpenAI Analysis Complete!" with call count
- [ ] OpenAI dashboard shows usage (after 5-10 min)
- [ ] Analysis completed successfully
- [ ] Results look high quality

**If all checked, OpenAI is definitely being used!** ✅

---

## 💡 Tips

### Save Logs to File

```powershell
# Start API with log file
cd C:\Users\KPeterson\CascadeProjects\ExcelAnalysisPlatform\src\ExcelAnalysis.API
dotnet run --urls "http://localhost:5100" 2>&1 | Tee-Object -FilePath "openai-logs.txt"
```

Now all console output is saved to `openai-logs.txt`!

### Count API Calls

```powershell
# After analysis, count calls in log file
(Get-Content "openai-logs.txt" | Select-String "OpenAI API Call").Count
```

### Monitor Cost in Real-Time

Keep https://platform.openai.com/usage open in browser while running analysis.

---

## 🔄 Switch Back to Ollama

If you want to go back to free local AI:

1. Edit `appsettings.json`:
   ```json
   "Provider": "Ollama"
   ```

2. Restart API

3. You'll see:
   ```
   🤖 AI PROVIDER: Ollama (Local)
   ```

---

**You now have complete visibility into which AI provider is being used!** 🎉

*Last Updated: December 12, 2025*
