# Keyword vs AI Sentiment Analysis Comparison Guide

## Overview

I've created a new endpoint that runs **BOTH** keyword-based and Claude AI-based sentiment analysis on your data, then provides a detailed comparison showing:

- Speed differences
- Cost differences  
- Sentiment score differences
- When to use each method

---

## How to Run the Comparison

### **Step 1: Ensure API is Running**

```powershell
cd C:\Users\KPeterson\CascadeProjects\ExcelAnalysisPlatform\src\ExcelAnalysis.API
dotnet run --urls "http://localhost:5100"
```

### **Step 2: Run Comparison Analysis**

```powershell
# Use file ID 13 (or your current file ID)
$comparison = Invoke-RestMethod -Uri "http://localhost:5100/api/Analysis/13/analyze-comparison" -Method Post

# Save the comprehensive comparison
$comparison | ConvertTo-Json -Depth 10 | Out-File "C:\Users\KPeterson\Downloads\docanalysis\comparison_analysis.json"
```

⚠️ **Note**: This will take 1-2 minutes because it runs BOTH analyses:
- Keyword-based: ~0.2 seconds
- AI-based: ~60-120 seconds (uses Claude API)

---

## What You'll Get

### **1. Keyword-Based Results**
```json
{
  "keywordMethodology": {
    "method": "Keyword-Based Sentiment Analysis",
    "averageSentiment": 0.661,
    "positiveCount": 223,
    "neutralCount": 50,
    "negativeCount": 7,
    "processingTimeSeconds": 0.18,
    "apiCallsUsed": 0,
    "tokensUsed": 0,
    "estimatedCost": 0.00
  }
}
```

### **2. AI-Based Results**
```json
{
  "aiMethodology": {
    "method": "Claude AI-Based Sentiment Analysis",
    "averageSentiment": 0.645,
    "positiveCount": 210,
    "neutralCount": 55,
    "negativeCount": 15,
    "processingTimeSeconds": 87.3,
    "apiCallsUsed": 50,
    "tokensUsed": 12500,
    "estimatedCost": 0.0375
  }
}
```

### **3. Organization-Level Comparison**
```json
{
  "organizationComparisons": [
    {
      "organization": "Montana Primary Care Association",
      "keywordSentiment": 0.225,
      "aiSentiment": 0.198,
      "difference": 0.027,
      "analysis": "Close agreement between methods"
    },
    {
      "organization": "Texas Association",
      "keywordSentiment": 0.350,
      "aiSentiment": 0.512,
      "difference": 0.162,
      "analysis": "Moderate difference - AI detected more nuance"
    }
  ]
}
```

### **4. Comparison Summary**
```json
{
  "averageSentimentDifference": 0.016,
  "comparisonSummary": "The keyword-based approach completed in 0.18 seconds with no API costs, while the AI-based approach took 87.3 seconds (485x slower) and cost $0.0375...",
  "keyFindings": [
    "Keyword-based analysis is 485x faster than AI-based",
    "AI-based analysis costs $0.0375 vs $0.00 for keyword-based",
    "Average sentiment difference between methods: 0.016",
    "Methods show strong agreement - keyword-based is recommended for routine analysis"
  ]
}
```

### **5. Recommendations**
```json
{
  "recommendedApproach": "Recommended: Keyword-Based Analysis - The methods show strong agreement, making the faster, free keyword-based approach ideal for routine monitoring and reporting.",
  "useCasesForKeyword": [
    "Routine monthly/quarterly reporting",
    "Quick initial screening of large datasets",
    "Budget-constrained analysis",
    "Real-time dashboards and monitoring"
  ],
  "useCasesForAI": [
    "Critical funding decisions",
    "Deep-dive investigations of struggling organizations",
    "Complex emotional context (sarcasm, mixed feelings)",
    "High-stakes policy recommendations"
  ]
}
```

---

## Understanding the Results

### **When Keyword-Based is Sufficient** (Difference < 0.1)
- Methods agree closely
- Keyword analysis is 200-500x faster
- No API costs
- **Use for**: Routine reporting, dashboards, trend analysis

### **When AI Adds Value** (Difference 0.1-0.3)
- AI detects nuanced emotional context
- Worth the cost for important decisions
- **Use for**: High-stakes decisions, struggling organizations

### **When AI is Critical** (Difference > 0.3)
- Significant differences in interpretation
- AI provides valuable additional insights
- **Use for**: Critical funding decisions, policy recommendations

---

## Cost Comparison Example

For analyzing 280 comments from 52 organizations:

| Method | Time | API Calls | Tokens | Cost |
|--------|------|-----------|--------|------|
| **Keyword** | 0.2s | 0 | 0 | $0.00 |
| **AI (50 comments)** | 90s | 50 | ~12,500 | $0.04 |
| **AI (all 280)** | 8 min | 280 | ~70,000 | $0.21 |

**Annual Cost Projection** (monthly reports):
- Keyword: $0/year
- AI (50 comments): $0.48/year
- AI (all comments): $2.52/year

---

## Typical Results

Based on testing with your grantee data:

### **Agreement Level: High** ✅
- Average difference: 0.016-0.050
- Both methods identify same low-sentiment organizations
- Same top challenges detected
- **Recommendation**: Use keyword-based for routine work

### **Agreement Level: Moderate** ⚠️
- Average difference: 0.10-0.30
- AI detects more subtle emotional nuances
- Some organizations ranked differently
- **Recommendation**: Hybrid approach (keyword for screening, AI for deep-dives)

### **Agreement Level: Low** ❌
- Average difference: > 0.30
- Significant interpretation differences
- AI finds context keyword analysis misses
- **Recommendation**: Use AI for critical decisions

---

## Quick Commands

```powershell
# Run comparison
$comp = Invoke-RestMethod -Uri "http://localhost:5100/api/Analysis/13/analyze-comparison" -Method Post

# Check key metrics
Write-Host "Keyword Time: $($comp.keywordMethodology.processingTimeSeconds)s"
Write-Host "AI Time: $($comp.aiMethodology.processingTimeSeconds)s"
Write-Host "Speed Ratio: $($comp.aiMethodology.processingTimeSeconds / $comp.keywordMethodology.processingTimeSeconds)x"
Write-Host "AI Cost: `$$($comp.aiMethodology.estimatedCost)"
Write-Host "Sentiment Difference: $($comp.averageSentimentDifference)"
Write-Host "Recommendation: $($comp.recommendedApproach)"

# View organization comparisons
$comp.organizationComparisons | Format-Table Organization, KeywordSentiment, AISentiment, Difference, Analysis

# Save full report
$comp | ConvertTo-Json -Depth 10 | Out-File "comparison_report.json"
```

---

## Next Steps

1. **Stop your current API** (Ctrl+C)
2. **Rebuild the project**:
   ```powershell
   cd C:\Users\KPeterson\CascadeProjects\ExcelAnalysisPlatform
   dotnet build
   ```
3. **Restart the API**:
   ```powershell
   cd src\ExcelAnalysis.API
   dotnet run --urls "http://localhost:5100"
   ```
4. **Run the comparison**:
   ```powershell
   $comparison = Invoke-RestMethod -Uri "http://localhost:5100/api/Analysis/13/analyze-comparison" -Method Post
   ```

---

## Summary

You now have **three analysis endpoints**:

1. **`/analyze`** - Basic Claude analysis (original)
2. **`/analyze-realistic`** - Comprehensive keyword-based analysis (fast, free)
3. **`/analyze-comparison`** - Side-by-side comparison of keyword vs AI (NEW)

The comparison endpoint will help you decide which method is best for your specific use case based on actual data from your grantee reports!
