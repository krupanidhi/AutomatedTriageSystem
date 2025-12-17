# 🚀 Quick Start - AI Provider Selection

## 📋 Three Options Available

### Option 1: Ollama llama3.2 (Current - Default) ✅
**Already working!** No changes needed.

### Option 2: Better Free Model (Recommended)
**Upgrade to a better model, still 100% free:**

```powershell
# Install better model
ollama pull llama3.1:8b
```

**Edit:** `src\ExcelAnalysis.API\appsettings.json`
```json
"AI": {
  "Provider": "Ollama",
  "Ollama": {
    "Model": "llama3.1:8b"
  }
}
```

### Option 3: OpenAI GPT (Premium)
**Best quality, costs ~$0.02 per analysis:**

1. Get API key: https://platform.openai.com/api-keys
2. **Edit:** `src\ExcelAnalysis.API\appsettings.json`

```json
"AI": {
  "Provider": "OpenAI",
  "OpenAI": {
    "ApiKey": "sk-your-key-here",
    "Model": "gpt-4o-mini"
  }
}
```

---

## 🔄 How to Switch

1. **Edit appsettings.json** (see above)
2. **Restart API:**
   ```powershell
   cd C:\Users\KPeterson\CascadeProjects\ExcelAnalysisPlatform\src\ExcelAnalysis.API
   dotnet run --urls "http://localhost:5100"
   ```
3. **Test with your Excel file**

---

## 📊 Quick Comparison

| Option | Cost | Quality | Speed |
|--------|------|---------|-------|
| llama3.2 | FREE | ⭐⭐⭐ | Fast |
| llama3.1:8b | FREE | ⭐⭐⭐⭐ | Medium |
| gpt-4o-mini | $0.02 | ⭐⭐⭐⭐⭐ | Fast |

---

## 📍 Configuration File Location
```
C:\Users\KPeterson\CascadeProjects\ExcelAnalysisPlatform\src\ExcelAnalysis.API\appsettings.json
```

**Full documentation:** See `AI_PROVIDER_GUIDE.md`
