# API Keys Setup Guide

## 🔑 Setting Up Your API Keys

This application requires API keys for AI providers. **Never commit your actual API keys to Git.**

---

## **Step 1: Create Local Configuration File**

Copy `appsettings.json` to `appsettings.Development.json`:

```powershell
cd C:\Users\YourUsername\CascadeProjects\ExcelAnalysisPlatform\src\ExcelAnalysis.API
Copy-Item appsettings.json appsettings.Development.json
```

---

## **Step 2: Add Your API Keys**

Edit `appsettings.Development.json` and replace the placeholders:

```json
{
  "AI": {
    "Provider": "Claude",
    "OpenAI": {
      "ApiKey": "sk-proj-YOUR_ACTUAL_OPENAI_KEY_HERE",
      "Model": "gpt-4o-mini"
    },
    "Gemini": {
      "ApiKey": "YOUR_ACTUAL_GEMINI_KEY_HERE",
      "Model": "gemini-2.5-flash"
    },
    "Claude": {
      "ApiKey": "sk-ant-api03-YOUR_ACTUAL_CLAUDE_KEY_HERE",
      "Model": "claude-opus-4-5-20251101"
    }
  }
}
```

---

## **Step 3: Get API Keys**

### **OpenAI**
- Visit: https://platform.openai.com/api-keys
- Create new secret key
- Copy and paste into `appsettings.Development.json`

### **Google Gemini**
- Visit: https://aistudio.google.com/app/apikey
- Create API key
- Copy and paste into `appsettings.Development.json`

### **Anthropic Claude**
- Visit: https://console.anthropic.com/settings/keys
- Create new API key
- Copy and paste into `appsettings.Development.json`

---

## **Important Notes**

✅ **DO:**
- Keep `appsettings.Development.json` on your local machine only
- Add real API keys to `appsettings.Development.json`
- Use environment variables for production deployments

❌ **DON'T:**
- Commit `appsettings.Development.json` to Git (it's in `.gitignore`)
- Share your API keys publicly
- Commit real API keys to `appsettings.json`

---

## **File Structure**

```
appsettings.json              ← Template with placeholders (committed to Git)
appsettings.Development.json  ← Your local file with real keys (NOT committed)
appsettings.Production.json   ← Production config (NOT committed)
```

---

## **Verification**

After adding your keys, run the application:

```powershell
cd C:\Users\YourUsername\CascadeProjects\ExcelAnalysisPlatform\src\ExcelAnalysis.API
dotnet run --urls "http://localhost:5200"
```

Check the console for successful API initialization.
