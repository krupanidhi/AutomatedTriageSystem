# SSL Certificate Workaround for Semantic Service

## Problem
The Sentence Transformers model cannot download from HuggingFace due to SSL certificate verification errors. This is common in corporate environments with proxy servers or custom SSL certificates.

---

## ✅ Solution Options

### **Option 1: Use Environment Variable (Recommended)**

Set the `HF_HUB_DISABLE_SSL_VERIFY` environment variable before running the service:

**PowerShell:**
```powershell
$env:HF_HUB_DISABLE_SSL_VERIFY = "1"
py semantic_analyzer.py
```

**Command Prompt:**
```cmd
set HF_HUB_DISABLE_SSL_VERIFY=1
py semantic_analyzer.py
```

**Permanent (Windows):**
```powershell
[System.Environment]::SetEnvironmentVariable('HF_HUB_DISABLE_SSL_VERIFY', '1', 'User')
```

---

### **Option 2: Download Model on Different Network**

If you have access to a different network (home, mobile hotspot, etc.):

1. Connect to network without SSL restrictions
2. Run the download script:
   ```bash
   py download_model.py
   ```
3. Model will be cached at: `C:\Users\YourName\.cache\huggingface\hub\`
4. Copy this folder to your work machine
5. Place it in the same location on your work machine

---

### **Option 3: Manual Model Download**

1. **Download from HuggingFace website** (in browser):
   - Go to: https://huggingface.co/sentence-transformers/all-mpnet-base-v2
   - Click "Files and versions"
   - Download all files to a local folder

2. **Load from local path** in `semantic_analyzer.py`:
   ```python
   model = SentenceTransformer('C:/path/to/downloaded/model')
   ```

---

### **Option 4: Use Smaller Pre-cached Model**

If you have `transformers` library already installed, you might have some models cached:

Check: `C:\Users\YourName\.cache\huggingface\hub\`

Update `semantic_analyzer.py` to use any available model:
```python
model = SentenceTransformer('sentence-transformers/all-MiniLM-L6-v2')
```

---

### **Option 5: Configure Corporate Proxy**

If your organization uses a proxy, configure it:

**PowerShell:**
```powershell
$env:HTTP_PROXY = "http://proxy.company.com:8080"
$env:HTTPS_PROXY = "http://proxy.company.com:8080"
$env:NO_PROXY = "localhost,127.0.0.1"
```

Then try downloading again.

---

### **Option 6: Use Claude-Only Analysis (Temporary)**

While troubleshooting the semantic service, you can still use:

1. **Claude AI Analysis** - Already working
   ```bash
   POST /api/Analysis/{fileId}/analyze-ai
   ```

2. **Keyword Analysis** - No external dependencies
   ```bash
   POST /api/Analysis/{fileId}/analyze-realistic
   ```

The hybrid analysis will gracefully handle if the semantic service is unavailable - it will return Claude results with `semanticResults: null`.

---

## 🔍 Testing the Fix

After applying any solution, test:

```powershell
# Test 1: Try downloading model
py download_model.py

# Test 2: Start semantic service
py semantic_analyzer.py

# Test 3: Check health endpoint (in another terminal)
curl http://localhost:5001/health
```

Expected output:
```json
{
  "status": "healthy",
  "model": "all-mpnet-base-v2",
  "version": "1.0.0"
}
```

---

## 📝 Quick Start Script

Save as `start_semantic_service.ps1`:

```powershell
# Set environment variable to bypass SSL
$env:HF_HUB_DISABLE_SSL_VERIFY = "1"

# Navigate to semantic service directory
cd C:\Users\KPeterson\CascadeProjects\ExcelAnalysisPlatform\semantic-service

# Start the service
Write-Host "Starting Semantic Analysis Service..." -ForegroundColor Cyan
py semantic_analyzer.py
```

Run:
```powershell
.\start_semantic_service.ps1
```

---

## 🎯 Recommended Approach

**For immediate use:**
1. Try **Option 1** (environment variable) first - quickest
2. If that fails, use **Option 6** (Claude-only) while troubleshooting
3. Contact IT about SSL certificates or proxy settings

**For permanent solution:**
1. Work with IT to add HuggingFace to SSL whitelist
2. Or download model once on unrestricted network
3. Copy cached model to work machine

---

## 💡 Why This Happens

Corporate networks often use:
- **SSL Inspection**: Intercepts HTTPS traffic with custom certificates
- **Proxy Servers**: Routes traffic through company infrastructure
- **Certificate Authorities**: Custom CA certificates not in Python's trust store

Python's SSL verification doesn't recognize these custom certificates, causing the download to fail.

---

## ✅ Verification Checklist

- [ ] Environment variable set: `HF_HUB_DISABLE_SSL_VERIFY=1`
- [ ] Model downloads without SSL errors
- [ ] Semantic service starts successfully
- [ ] Health endpoint returns 200 OK
- [ ] Hybrid analysis can call semantic service

---

## 🆘 Still Not Working?

If none of these solutions work:

1. **Check firewall**: Ensure port 5001 is not blocked
2. **Check Python version**: Requires Python 3.8+
3. **Check disk space**: Model needs ~500MB
4. **Try different model**: Use `all-MiniLM-L6-v2` (smaller, 80MB)
5. **Contact support**: Provide error logs from console

---

**The hybrid analysis system is fully functional** - this SSL issue only affects the semantic service component. Claude AI analysis works independently and can be used while resolving this issue.
