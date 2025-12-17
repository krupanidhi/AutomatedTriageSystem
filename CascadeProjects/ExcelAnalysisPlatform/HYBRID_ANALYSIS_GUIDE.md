# Hybrid Analysis Guide
## Combining Claude AI + Sentence Transformers Semantic Analysis

This guide explains how to set up and use the hybrid analysis feature that combines **Claude AI** (contextual understanding) with **Sentence Transformers** (semantic clustering) for comprehensive grantee analysis.

---

## 🎯 Overview

### What is Hybrid Analysis?

Hybrid Analysis runs **both** Claude AI and Sentence Transformers semantic analysis **in parallel**, then integrates the results into a unified report that shows:

- **Claude AI Results**: Detailed challenges, remedies, recommendations, sentiment scores, risk levels
- **Semantic Analysis Results**: Automatic theme clustering, semantic similarity, keyword extraction
- **Integrated Insights**: Combined view showing which findings come from which model

### Benefits

1. **Cross-validation**: Both models analyze the same data independently
2. **Complementary strengths**: Claude provides context, Semantic provides clustering
3. **Separate downloads**: Download Claude-only, Semantic-only, or Hybrid reports
4. **Cost-effective**: Semantic analysis is free and runs locally
5. **No fallback confusion**: Each analysis type remains distinct

---

## 📋 Prerequisites

### 1. Python 3.8+ Installation

Ensure Python is installed:
```bash
python --version
```

### 2. Required Python Packages

Navigate to the semantic service directory:
```bash
cd C:\Users\KPeterson\CascadeProjects\ExcelAnalysisPlatform\semantic-service
```

Install dependencies:
```bash
pip install -r requirements.txt
```

**Note**: First run will download the `all-mpnet-base-v2` model (~420MB) from HuggingFace.

### 3. Claude API Key (Optional)

If you want Claude AI analysis, ensure your API key is configured in `ai-settings.json`:
```json
{
  "provider": "claude",
  "claude": {
    "apiKey": "your-api-key-here",
    "model": "claude-opus-4-20250514"
  }
}
```

---

## 🚀 Setup Instructions

### Step 1: Start the Semantic Service

Open a **separate terminal/command prompt**:

```bash
cd C:\Users\KPeterson\CascadeProjects\ExcelAnalysisPlatform\semantic-service
python semantic_analyzer.py
```

You should see:
```
Loading Sentence Transformer model...
Model loaded successfully!
 * Running on http://0.0.0.0:5001
```

**Keep this terminal running** while using hybrid analysis.

### Step 2: Verify Semantic Service

Test the health endpoint:
```bash
curl http://localhost:5001/health
```

Expected response:
```json
{
  "status": "healthy",
  "model": "all-mpnet-base-v2",
  "version": "1.0.0"
}
```

### Step 3: Start the C# Application

In a **different terminal**:

```bash
cd C:\Users\KPeterson\CascadeProjects\ExcelAnalysisPlatform\src\ExcelAnalysis.API
dotnet run --urls "http://localhost:5200"
```

### Step 4: Access the Dashboard

Open your browser:
```
http://localhost:5200
```

---

## 📊 Using Hybrid Analysis

### Via API (Programmatic)

**Endpoint:**
```
POST /api/Analysis/{fileId}/analyze-hybrid
```

**Example:**
```bash
curl -X POST http://localhost:5200/api/Analysis/1/analyze-hybrid
```

**Response Structure:**
```json
{
  "analyzedAt": "2024-12-16T20:00:00Z",
  "fileName": "SAPR2-MAY-2023.xlsx",
  "totalGranteesAnalyzed": 25,
  "totalResponsesAnalyzed": 150,
  
  "claudeResults": {
    "overallAverageSentiment": 0.35,
    "lowestSentimentOrganizations": [...],
    "thematicChallenges": [...]
  },
  
  "semanticResults": {
    "totalComments": 150,
    "themes": [
      {
        "themeId": 0,
        "themeName": "Staffing and Workforce",
        "commentCount": 35,
        "keywords": ["staff", "hiring", "retention"],
        "representativeComment": "..."
      }
    ],
    "organizationInsights": {...}
  },
  
  "integratedOrganizationInsights": [
    {
      "organizationName": "Example Org",
      "claudeSentiment": 0.25,
      "claudeRiskLevel": "Medium",
      "claudeTopChallenges": [...],
      "semanticCoherence": 0.72,
      "semanticKeywords": ["funding", "capacity"],
      "semanticThemeName": "Funding and Resources",
      "integratedRiskAssessment": "Claude: Medium; Low sentiment; Moderate coherence"
    }
  ],
  
  "integratedThemes": [...],
  "executiveSummary": "...",
  "keyFindings": [...]
}
```

### Via Dashboard UI (Coming Soon)

The dashboard will include:
- "Analyze with Hybrid (Claude + Semantic)" button
- Integrated report view showing both results
- Three download options:
  - Download Claude Results Only
  - Download Semantic Results Only  
  - Download Hybrid Report

---

## 📥 Download Options

### 1. Claude-Only Report

**Endpoint:**
```
GET /api/Reports/{fileId}/download-ai-json
```

**Contains:**
- Detailed challenges with remedies
- Specific recommendations
- Sentiment scores
- Risk levels
- Contextual background

### 2. Semantic-Only Report

**Endpoint:**
```
GET /api/Reports/{fileId}/download-semantic-json
```

**Contains:**
- Thematic clusters
- Semantic keywords
- Coherence scores
- Similar comment pairs
- Representative comments

### 3. Hybrid Report

**Endpoint:**
```
GET /api/Reports/{fileId}/download-hybrid-json
```

**Contains:**
- All Claude results
- All Semantic results
- Integrated organization insights
- Cross-model theme mapping
- Combined executive summary

---

## 🔍 Understanding the Results

### Claude AI Analysis

**Strengths:**
- Contextual understanding of challenges
- Specific, actionable recommendations
- Detailed remediation plans with timelines
- Nuanced sentiment analysis
- Risk level classification

**Example Output:**
```json
{
  "organizationName": "Health Center A",
  "averageSentiment": 0.15,
  "riskLevel": "Medium",
  "detailedChallenges": [
    {
      "challenge": "Staff Retention Issues",
      "description": "High turnover in nursing staff",
      "impact": "Reduced patient care quality",
      "suggestedRemedy": "Implement retention bonuses",
      "actionSteps": ["Survey staff", "Design incentive program"],
      "timeline": "3-6 months",
      "responsibleParty": "HR Department"
    }
  ]
}
```

### Semantic Analysis

**Strengths:**
- Automatic theme discovery (no predefined keywords)
- Semantic similarity across organizations
- Coherence measurement (how unified are the issues?)
- Pattern detection
- Clustering without bias

**Example Output:**
```json
{
  "themes": [
    {
      "themeName": "Staffing and Workforce",
      "commentCount": 35,
      "keywords": ["staff", "hiring", "retention", "turnover"],
      "representativeComment": "We struggle with staff retention...",
      "sampleComments": [...]
    }
  ],
  "organizationInsights": {
    "Health Center A": {
      "coherenceScore": 0.68,
      "topKeywords": ["staff", "retention", "hiring"]
    }
  }
}
```

### Integrated Insights

**Combines both models:**
- Claude's detailed understanding + Semantic's pattern detection
- Risk assessment from both perspectives
- Theme mapping (Claude themes ↔ Semantic clusters)
- Confidence through cross-validation

**Example:**
```json
{
  "organizationName": "Health Center A",
  "claudeSentiment": 0.15,
  "claudeRiskLevel": "Medium",
  "claudeTopChallenges": ["Staff Retention", "Funding"],
  "semanticCoherence": 0.68,
  "semanticKeywords": ["staff", "retention", "hiring"],
  "semanticThemeName": "Staffing and Workforce",
  "integratedRiskAssessment": "Claude: Medium; Low sentiment; Moderate coherence"
}
```

---

## 🛠️ Troubleshooting

### Semantic Service Won't Start

**Error:** `ModuleNotFoundError: No module named 'sentence_transformers'`

**Solution:**
```bash
pip install sentence-transformers
```

---

### Semantic Service Times Out

**Error:** `Semantic service not available`

**Check:**
1. Is the Python service running? Look for the terminal window.
2. Is port 5001 available?
   ```bash
   netstat -an | findstr 5001
   ```
3. Check firewall settings

**Solution:**
Restart the semantic service:
```bash
python semantic_analyzer.py
```

---

### Model Download Fails

**Error:** `Connection error downloading model`

**Solution:**
1. Check internet connection
2. Model downloads to: `~/.cache/torch/sentence_transformers/`
3. If behind proxy, set environment variables:
   ```bash
   set HTTP_PROXY=http://proxy:port
   set HTTPS_PROXY=http://proxy:port
   ```

---

### Out of Memory

**Error:** `CUDA out of memory` or Python crashes

**Solution:**
1. Use smaller model in `semantic_analyzer.py`:
   ```python
   model = SentenceTransformer('all-MiniLM-L6-v2')  # Only 80MB
   ```
2. Process fewer comments at once
3. Close other applications

---

### Hybrid Analysis Returns Only One Model

**Scenario:** Hybrid result has `claudeResults: null` or `semanticResults: null`

**Cause:** One service failed while the other succeeded

**Check:**
- Claude API credits (if `claudeResults` is null)
- Semantic service running (if `semanticResults` is null)
- Console logs for specific errors

**Solution:**
- Hybrid analysis continues even if one model fails
- Check which service needs attention
- Results clearly show which model provided data

---

## 📈 Performance Considerations

### Processing Time

| Analysis Type | Typical Time (100 comments) |
|--------------|----------------------------|
| Claude Only | 30-60 seconds |
| Semantic Only | 5-10 seconds |
| Hybrid (parallel) | 30-60 seconds |

**Note:** Hybrid runs both in parallel, so total time ≈ Claude time (slower of the two)

### Cost Comparison

| Analysis Type | Cost per 1000 comments |
|--------------|----------------------|
| Claude AI | $0.50 - $2.00 |
| Semantic | $0.00 (free) |
| Hybrid | Same as Claude |

### Resource Usage

**Semantic Service:**
- RAM: ~1-2 GB (with model loaded)
- CPU: Moderate during analysis
- Disk: ~500 MB (model cache)

**Claude API:**
- Network bandwidth only
- No local resources

---

## 🎓 Best Practices

### 1. When to Use Each Analysis Type

**Use Claude Only:**
- Need detailed, actionable recommendations
- Require specific remediation plans
- Want contextual understanding
- Have API credits available

**Use Semantic Only:**
- Want quick theme discovery
- Need to find similar patterns
- No API credits available
- Exploring data structure

**Use Hybrid:**
- Want comprehensive analysis
- Need cross-validation
- Comparing model outputs
- Maximum insight depth

### 2. Interpreting Coherence Scores

**Semantic Coherence** measures how similar comments are within an organization:

- **High (0.8-1.0)**: Unified, consistent issues
- **Medium (0.5-0.8)**: Some variety in challenges
- **Low (0.0-0.5)**: Diverse, potentially conflicting issues

**Low coherence** doesn't mean "bad" - it may indicate:
- Organization has multiple distinct challenges
- Different departments have different needs
- Requires individualized attention

### 3. Theme Mapping

Hybrid analysis maps Claude themes to Semantic clusters:

```
Claude Theme: "Staffing Challenges"
  ↓ (keyword overlap)
Semantic Cluster: "Staffing and Workforce"
  - Keywords: staff, hiring, retention
  - 35 comments
```

**Use this to:**
- Validate Claude's theme identification
- Discover themes Claude might have missed
- Understand theme prevalence (comment counts)

---

## 🔄 Workflow Example

### Complete Analysis Workflow

1. **Upload File**
   ```bash
   curl -F "file=@data.xlsx" http://localhost:5200/api/Analysis/upload
   ```

2. **Run Hybrid Analysis**
   ```bash
   curl -X POST http://localhost:5200/api/Analysis/1/analyze-hybrid
   ```

3. **Download All Three Reports**
   ```bash
   # Claude only
   curl http://localhost:5200/api/Reports/1/download-ai-json > claude.json
   
   # Semantic only
   curl http://localhost:5200/api/Reports/1/download-semantic-json > semantic.json
   
   # Hybrid
   curl http://localhost:5200/api/Reports/1/download-hybrid-json > hybrid.json
   ```

4. **Compare Results**
   - Review Claude's detailed recommendations
   - Check Semantic's theme clusters
   - Use Hybrid report for integrated view

---

## 🚦 Service Status Checks

### Quick Health Check Script

Save as `check_services.ps1`:

```powershell
# Check Semantic Service
Write-Host "Checking Semantic Service..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5001/health"
    Write-Host "✅ Semantic Service: $($response.status)" -ForegroundColor Green
} catch {
    Write-Host "❌ Semantic Service: Not running" -ForegroundColor Red
}

# Check C# API
Write-Host "Checking C# API..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5200/api/Files"
    Write-Host "✅ C# API: Running" -ForegroundColor Green
} catch {
    Write-Host "❌ C# API: Not running" -ForegroundColor Red
}
```

Run:
```powershell
.\check_services.ps1
```

---

## 📚 API Reference Summary

### Analysis Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/Analysis/{id}/analyze-ai` | POST | Claude only |
| `/api/Analysis/{id}/analyze-semantic` | POST | Semantic only (future) |
| `/api/Analysis/{id}/analyze-hybrid` | POST | Both models |

### Download Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/Reports/{id}/download-ai-json` | GET | Claude results |
| `/api/Reports/{id}/download-semantic-json` | GET | Semantic results |
| `/api/Reports/{id}/download-hybrid-json` | GET | Integrated results |

---

## 🎯 Next Steps

1. **Start both services** (Python + C#)
2. **Upload a test file** via dashboard
3. **Run hybrid analysis** on the file
4. **Download all three reports** to compare
5. **Review integrated insights** for comprehensive understanding

---

## 💡 Tips

- **Keep semantic service running** in background for best performance
- **Use semantic-only** for quick exploratory analysis
- **Use hybrid** for final comprehensive reports
- **Compare results** to understand model strengths
- **Low coherence** organizations may need individualized attention
- **Theme overlap** between models validates findings

---

## 📞 Support

For issues or questions:
1. Check console logs (both Python and C#)
2. Review this guide's troubleshooting section
3. Verify both services are running
4. Check API endpoint responses

---

**Happy Analyzing! 🎉**
