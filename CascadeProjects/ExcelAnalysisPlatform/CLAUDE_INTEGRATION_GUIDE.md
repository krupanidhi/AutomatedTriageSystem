# 🤖 Claude (Anthropic) Integration Guide

## Overview

**Claude by Anthropic** is now integrated into the Excel Analysis Platform. Claude excels at understanding complex contexts, analyzing sentiment, and providing nuanced insights - perfect for analyzing grantee challenges and helping reviewers understand historical patterns.

---

## 🎯 Why Claude for Grantee Analysis?

### Strengths for Your Use Case

1. **Context Understanding**
   - Excellent at understanding grantee challenges in context
   - Identifies patterns across multiple comments
   - Nuanced sentiment analysis

2. **Long Context Window**
   - Can analyze extensive comment histories
   - Maintains context across multiple grantees
   - Better for comprehensive reports

3. **Thoughtful Analysis**
   - Provides detailed mitigation strategies
   - Identifies root causes, not just symptoms
   - Helps reviewers understand "why" not just "what"

4. **Reliable & Consistent**
   - Stable API with good rate limits
   - Consistent quality across analyses
   - Professional-grade outputs

---

## 🚀 Quick Start

### 1. Get Your Claude API Key

Visit: https://console.anthropic.com/

1. Sign up for an Anthropic account
2. Navigate to API Keys section
3. Create a new API key
4. Copy the key (starts with `sk-ant-`)

### 2. Configure the System

Edit `appsettings.json`:

```json
{
  "AI": {
    "Provider": "Claude",
    "UseFastSentiment": true,
    "UseDynamicKeywords": true,
    "Claude": {
      "ApiKey": "sk-ant-your-api-key-here",
      "Model": "claude-3-5-sonnet-20241022",
      "DelayBetweenCallsMs": 0
    }
  }
}
```

### 3. Start the API

```powershell
cd C:\Users\KPeterson\CascadeProjects\ExcelAnalysisPlatform\src\ExcelAnalysis.API
dotnet run --urls "http://localhost:5100"
```

**You should see:**
```
🤖 AI PROVIDER: Claude (Anthropic)
📊 MODEL: claude-3-5-sonnet-20241022
🌐 ENDPOINT: https://api.anthropic.com
💭 SENTIMENT: Keyword-Based (Fast)
🔤 KEYWORDS: Dynamic (extracted from file)
```

### 4. Analyze Grantee Data

```powershell
# Upload your grantee challenges Excel file
$file = Get-Item "grantee_challenges.xlsx"
$form = @{ file = $file }
$upload = Invoke-RestMethod -Uri "http://localhost:5100/api/Upload" -Method Post -Form $form

# Run analysis with Claude
$analysis = Invoke-RestMethod -Uri "http://localhost:5100/api/Analysis/$($upload.id)/analyze" -Method Post

# View results
$analysis | ConvertTo-Json -Depth 10
```

---

## 📊 Available Claude Models

### Recommended: Claude 3.5 Sonnet (Default)
```json
{
  "Model": "claude-3-5-sonnet-20241022"
}
```
- **Best for**: Production analysis, grantee challenges
- **Speed**: Fast
- **Quality**: Excellent
- **Cost**: $3 per million input tokens, $15 per million output tokens
- **Context**: 200K tokens

### Alternative: Claude 3 Opus
```json
{
  "Model": "claude-3-opus-20240229"
}
```
- **Best for**: Most complex analyses
- **Speed**: Slower
- **Quality**: Highest
- **Cost**: $15 per million input tokens, $75 per million output tokens
- **Context**: 200K tokens

### Budget Option: Claude 3 Haiku
```json
{
  "Model": "claude-3-haiku-20240307"
}
```
- **Best for**: High-volume, simple analyses
- **Speed**: Fastest
- **Quality**: Good
- **Cost**: $0.25 per million input tokens, $1.25 per million output tokens
- **Context**: 200K tokens

---

## 💰 Pricing & Rate Limits

### Pricing (Claude 3.5 Sonnet)
- **Input**: $3 per million tokens (~$0.003 per 1,000 tokens)
- **Output**: $15 per million tokens (~$0.015 per 1,000 tokens)

**Example Cost for 50 Comments:**
- Input: 50 comments × 200 tokens = 10,000 tokens = $0.03
- Output: 50 responses × 50 tokens = 2,500 tokens = $0.04
- **Total**: ~$0.07 per analysis

### Rate Limits
- **Free Tier**: 50 requests per minute
- **Paid Tier**: 1,000+ requests per minute
- **No daily limits** (unlike Gemini)

---

## 🎓 Use Cases for Grantee Analysis

### 1. Understanding Historical Challenges

**Scenario:** Reviewers need to understand what challenges grantees faced in past projects.

```powershell
# Analyze historical grantee data
$analysis = Invoke-RestMethod -Uri "http://localhost:5100/api/Analysis/$fileId/analyze" -Method Post

# Claude will identify:
# - Common challenge patterns
# - Risk factors
# - Sentiment trends
# - Mitigation strategies
```

**Output:**
```json
{
  "executiveSummary": "Analysis of 22 grantee comments reveals recurring challenges in staffing (45% of issues) and timeline management (30%). Overall sentiment is cautiously optimistic (0.32) with 3 high-risk items requiring immediate attention.",
  "highRiskCount": 3,
  "sentimentSummary": "Neutral",
  "recommendations": [
    "Address staffing shortages through early recruitment planning",
    "Implement milestone tracking for timeline management",
    "Increase communication frequency with struggling grantees"
  ]
}
```

### 2. Identifying Future Needs

**Scenario:** Use past challenges to inform future grant requirements.

Claude analyzes:
- **Recurring issues** → Future grant criteria
- **Success factors** → Best practices to promote
- **Risk patterns** → Early warning indicators

### 3. Reviewer Decision Support

**Scenario:** Help reviewers make informed decisions about grant applications.

Claude provides:
- **Historical context** for similar projects
- **Risk assessment** based on past patterns
- **Mitigation strategies** proven to work

---

## 🔧 Configuration Options

### Sentiment Analysis Mode

**Keyword-Based (Recommended):**
```json
{
  "UseFastSentiment": true,
  "UseDynamicKeywords": true
}
```
- ✅ **0 API calls** for sentiment
- ✅ Uses extracted buzzwords from your data
- ✅ Instant results
- ✅ Lower cost

**AI-Based:**
```json
{
  "UseFastSentiment": false
}
```
- Uses Claude for sentiment analysis
- More nuanced but slower
- Higher API costs

### Rate Limiting

```json
{
  "DelayBetweenCallsMs": 0  // No delay needed (good rate limits)
}
```

For high-volume processing:
```json
{
  "DelayBetweenCallsMs": 1000  // 1 second delay if needed
}
```

---

## 📈 Comparison with Other Providers

| Feature | Claude | Gemini | OpenAI | Ollama |
|---------|--------|--------|--------|--------|
| **Cost** | $3-15/M tokens | Free (20/day) | $0.15-15/M | Free |
| **Rate Limit** | 50-1000/min | 5/min | 3-500/min | Unlimited |
| **Daily Limit** | None | 20 requests | None | None |
| **Context Window** | 200K tokens | 32K tokens | 128K tokens | 8K tokens |
| **Quality** | Excellent | Good | Excellent | Good |
| **Best For** | Production | Testing | Production | Development |

---

## 🎯 Workflow Example: Grantee Challenge Analysis

### Complete Analysis Pipeline

```powershell
# 1. Upload grantee challenges file
$file = Get-Item "FY2024_Grantee_Challenges.xlsx"
$upload = Invoke-RestMethod -Uri "http://localhost:5100/api/Upload" -Method Post -Form @{ file = $file }
Write-Host "✅ Uploaded: $($upload.fileName)"

# 2. Run Claude analysis
Write-Host "🔍 Analyzing with Claude..."
$analysis = Invoke-RestMethod -Uri "http://localhost:5100/api/Analysis/$($upload.id)/analyze" -Method Post

# 3. Extract key insights
Write-Host "`n📊 Analysis Results:"
Write-Host "   High-Risk Issues: $($analysis.highRiskCount)"
Write-Host "   Medium-Risk Issues: $($analysis.mediumRiskCount)"
Write-Host "   Overall Sentiment: $($analysis.sentimentSummary) ($($analysis.overallSentimentScore))"
Write-Host "   Completion: $($analysis.completionPercentage)%"

# 4. View executive summary
Write-Host "`n📝 Executive Summary:"
Write-Host $analysis.executiveSummary

# 5. Export detailed report
$analysis | ConvertTo-Json -Depth 10 | Out-File "grantee_analysis_report.json"
Write-Host "`n💾 Detailed report saved to: grantee_analysis_report.json"

# 6. Generate recommendations document
$recommendations = @"
# Grantee Challenge Analysis Report
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm")

## Executive Summary
$($analysis.executiveSummary)

## Risk Assessment
- High/Critical Risks: $($analysis.highRiskCount)
- Medium Risks: $($analysis.mediumRiskCount)
- Low Risks: $($analysis.lowRiskCount)

## Sentiment Analysis
- Overall Score: $($analysis.overallSentimentScore)
- Classification: $($analysis.sentimentSummary)

## Recommendations for Future Grants
$($analysis.recommendations)
"@

$recommendations | Out-File "grantee_recommendations.md"
Write-Host "📄 Recommendations saved to: grantee_recommendations.md"
```

---

## 🔍 Advanced Features

### 1. Batch Analysis

Analyze multiple grantee files:

```powershell
$files = Get-ChildItem "*.xlsx"
$results = @()

foreach ($file in $files) {
    Write-Host "Analyzing: $($file.Name)"
    
    $upload = Invoke-RestMethod -Uri "http://localhost:5100/api/Upload" -Method Post -Form @{ file = $file }
    $analysis = Invoke-RestMethod -Uri "http://localhost:5100/api/Analysis/$($upload.id)/analyze" -Method Post
    
    $results += @{
        FileName = $file.Name
        HighRisk = $analysis.highRiskCount
        Sentiment = $analysis.overallSentimentScore
        Summary = $analysis.executiveSummary
    }
}

$results | ConvertTo-Json | Out-File "batch_analysis_results.json"
```

### 2. Trend Analysis

Compare challenges across years:

```powershell
$years = @("FY2022", "FY2023", "FY2024")
$trends = @()

foreach ($year in $years) {
    $file = Get-Item "$year`_Grantee_Challenges.xlsx"
    $upload = Invoke-RestMethod -Uri "http://localhost:5100/api/Upload" -Method Post -Form @{ file = $file }
    $analysis = Invoke-RestMethod -Uri "http://localhost:5100/api/Analysis/$($upload.id)/analyze" -Method Post
    
    $trends += @{
        Year = $year
        HighRisk = $analysis.highRiskCount
        Sentiment = $analysis.overallSentimentScore
    }
}

# Visualize trends
$trends | Format-Table -AutoSize
```

---

## 🛠️ Troubleshooting

### API Key Issues

**Error:** "Claude API key is required"

**Solution:**
```json
{
  "Claude": {
    "ApiKey": "sk-ant-your-actual-key-here"  // Must start with sk-ant-
  }
}
```

### Rate Limit Errors

**Error:** "Rate limit exceeded"

**Solution:** Add delay between calls:
```json
{
  "DelayBetweenCallsMs": 1000  // 1 second delay
}
```

### Model Not Found

**Error:** "Model not found"

**Solution:** Use exact model name:
```json
{
  "Model": "claude-3-5-sonnet-20241022"  // Exact version
}
```

---

## 📚 Best Practices

### 1. Use Dynamic Keywords
```json
{
  "UseFastSentiment": true,
  "UseDynamicKeywords": true
}
```
- Reduces API costs by 50%+
- Faster sentiment analysis
- Domain-specific keywords

### 2. Batch Similar Analyses
- Group grantees by program type
- Analyze similar time periods together
- Build domain-specific knowledge

### 3. Monitor Costs
```powershell
# Check API usage
$analysis = Invoke-RestMethod -Uri "http://localhost:5100/api/Analysis/$fileId/analyze" -Method Post
Write-Host "Estimated Cost: ~$($analysis.estimatedCost)"
```

### 4. Save Results
```powershell
# Always save analysis results
$analysis | ConvertTo-Json -Depth 10 | Out-File "analysis_$(Get-Date -Format 'yyyyMMdd_HHmmss').json"
```

---

## 🎉 Summary

**Claude Integration Benefits:**

✅ **Excellent for grantee analysis**
- Understands complex challenges
- Identifies patterns and trends
- Provides actionable insights

✅ **Reliable & Cost-Effective**
- No daily limits (unlike Gemini)
- Good rate limits (50-1000/min)
- Reasonable pricing (~$0.07 per 50 comments)

✅ **Works with Your Buzzwords**
- Uses dynamic keyword extraction
- 0 API calls for sentiment
- Fast and accurate

✅ **Helps Reviewers**
- Historical challenge analysis
- Future needs identification
- Decision support with context

**Perfect for understanding grantee challenges and informing future grant decisions!** 🎯
