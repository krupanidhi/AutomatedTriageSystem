# Provider Attribution Guide
## How to Trace Specific Provider Recommendations & Challenges

This guide explains how to identify which AI provider (Claude AI vs Sentence Transformers) generated each piece of data in your analysis reports.

---

## 🎨 Visual Indicators in Report Grid

### **Provider Column**
The report grid now includes a **Provider** column showing:

- **🤖 Claude** - Blue badge: Data from Claude AI only
- **🧠 Semantic** - Green badge: Data from Sentence Transformers only  
- **🔀 HYBRID** - Purple badge: Data from both providers

### **Color-Coded Sections in Detail View**

When you click "View Details" on an organization:

**Blue sections** (light blue background, blue left border):
- 🤖 **Claude AI** data
- Sentiment scores
- Risk levels
- Detailed challenges with evidence
- Specific recommendations

**Green sections** (light green background, green left border):
- 🧠 **Semantic Analysis** data
- Theme clusters
- Keywords
- Coherence scores

---

## 📊 Field Naming Convention

All hybrid analysis data uses clear prefixes to indicate the source:

### **Claude AI Fields** (prefix: `claude*`)
```json
{
  "claudeSentiment": 0.15,
  "claudeRiskLevel": "Medium",
  "claudeTopChallenges": ["Staff Retention", "Funding"],
  "claudeDetailedChallenges": [
    {
      "challenge": "Staff Retention",
      "evidence": "Multiple comments mention high turnover",
      "howAddressedHistorically": "Hired temporary staff",
      "futureRemedy": "Implement retention bonuses and career development"
    }
  ],
  "claudeRecommendations": [
    "Increase competitive salaries",
    "Improve benefits package"
  ]
}
```

### **Semantic Fields** (prefix: `semantic*`)
```json
{
  "semanticCoherence": 0.68,
  "semanticKeywords": ["staff", "retention", "hiring"],
  "semanticThemeId": 2,
  "semanticThemeName": "Staffing and Workforce"
}
```

### **Integrated Fields** (prefix: `integrated*`)
```json
{
  "integratedRiskAssessment": "Claude: Medium risk; Semantic: Moderate coherence",
  "actionNeeded": "Address staffing challenges immediately"
}
```

---

## 🔍 How to Trace Specific Data

### **Method 1: Visual Inspection in Grid**

1. Navigate to `/report-grid`
2. Look at the **Provider** column
3. Click **View Details** to see color-coded sections

**Example:**
- See blue badge "🤖 Claude" → All data from Claude AI
- See purple badge "🔀 HYBRID" → Data from both providers (check sections)

### **Method 2: Download JSON Report**

Download the hybrid JSON report to see complete attribution:

```json
{
  "organizationName": "Health Center A",
  
  // CLAUDE AI DATA (clearly labeled)
  "claudeSentiment": 0.15,
  "claudeRiskLevel": "Medium",
  "claudeTopChallenges": ["Staff Retention"],
  "claudeDetailedChallenges": [
    {
      "challenge": "Staff Retention",
      "evidence": "Comments mention 40% turnover rate",
      "howAddressedHistorically": "Offered signing bonuses",
      "futureRemedy": "Create comprehensive retention program"
    }
  ],
  
  // SEMANTIC DATA (clearly labeled)
  "semanticCoherence": 0.68,
  "semanticKeywords": ["staff", "retention", "turnover"],
  "semanticThemeName": "Staffing and Workforce",
  
  // INTEGRATED (combined from both)
  "integratedRiskAssessment": "URGENT: Both models indicate critical situation"
}
```

### **Method 3: Check Section Headers**

In the detail modal, each section shows its provider:

- **"⚠️ Detailed Challenges (Claude AI)"** - Blue section
- **"🧠 Semantic Analysis (Sentence Transformers)"** - Green section
- **"💡 Recommendations (Claude AI)"** - Blue section

---

## 📋 What Each Provider Offers

### **🤖 Claude AI Provides:**

**Strengths:**
- Deep contextual understanding
- Evidence-based insights
- Historical context
- Specific, actionable recommendations
- Risk assessment with reasoning

**Data Fields:**
- `claudeSentiment` - Sentiment score (-1.0 to 1.0)
- `claudeRiskLevel` - Risk classification (High/Medium/Low)
- `claudeTopChallenges` - List of main challenges
- `claudeDetailedChallenges` - Challenges with:
  - Evidence from comments
  - How addressed historically
  - Future remedy recommendations
- `claudeRecommendations` - Specific action items

**Example Output:**
```
Challenge: "Staff Retention Crisis"
Evidence: "15 comments mention high turnover; 8 cite burnout"
Historical: "Hired temp staff, but didn't address root cause"
Future Remedy: "Implement retention bonuses, improve work-life balance"
```

---

### **🧠 Sentence Transformers Provides:**

**Strengths:**
- Automatic theme discovery (no predefined keywords)
- Semantic similarity detection
- Coherence scoring
- Unbiased clustering
- 100% free (no API costs)

**Data Fields:**
- `semanticCoherence` - How consistent comments are (0.0 to 1.0)
- `semanticKeywords` - Automatically extracted keywords
- `semanticThemeId` - Cluster ID
- `semanticThemeName` - Auto-generated theme name
- `similarCommentPairs` - Comments with high similarity

**Example Output:**
```
Theme: "Staffing and Workforce"
Keywords: ["staff", "retention", "hiring", "turnover", "burnout"]
Coherence: 0.68 (moderate - some scattered concerns)
Comment Count: 35
```

---

## 🎯 Practical Examples

### **Example 1: Tracing a Recommendation**

**Question:** "Where did the recommendation to 'Increase salaries' come from?"

**Answer:**
1. Open detail view for the organization
2. Scroll to **"💡 Recommendations (Claude AI)"** section (blue background)
3. See the recommendation listed there
4. **Source: Claude AI** ✅

### **Example 2: Tracing a Theme**

**Question:** "How was 'Staffing and Workforce' identified as a theme?"

**Answer:**
1. Open detail view
2. Look at **"🧠 Semantic Analysis"** section (green background)
3. See `Theme: Staffing and Workforce`
4. See `Keywords: staff, retention, hiring...`
5. **Source: Sentence Transformers** ✅

### **Example 3: Cross-Validation**

**Question:** "Do both providers agree this is a critical issue?"

**Answer:**
1. Check Claude section:
   - `claudeRiskLevel: "High"`
   - `claudeSentiment: 0.12` (very low)
   
2. Check Semantic section:
   - `semanticCoherence: 0.45` (low - scattered concerns)
   - `semanticKeywords: ["crisis", "emergency", "urgent"]`

3. Check Integrated section:
   - `integratedRiskAssessment: "URGENT: Both models indicate critical situation"`

**Conclusion:** ✅ Both providers agree → High confidence

---

## 🔄 Download Options

### **Three Separate Reports Available:**

**1. Claude-Only Report**
```
GET /api/Reports/{fileId}/download-ai-json
```
Contains only Claude AI analysis:
- Sentiment scores
- Risk levels
- Detailed challenges with evidence
- Recommendations
- Historical context

**2. Semantic-Only Report**
```
GET /api/Reports/{fileId}/download-semantic-json
```
Contains only Sentence Transformers analysis:
- Theme clusters
- Keywords
- Coherence scores
- Similar comment pairs

**3. Hybrid Report**
```
GET /api/Reports/{fileId}/download-hybrid-json
```
Contains everything from both providers:
- All Claude data (labeled with `claude*` prefix)
- All Semantic data (labeled with `semantic*` prefix)
- Integrated insights (labeled with `integrated*` prefix)

---

## 💡 Best Practices

### **When to Trust Each Provider:**

**Trust Claude AI for:**
- ✅ Specific, actionable recommendations
- ✅ Understanding context and nuance
- ✅ Evidence-based insights
- ✅ Historical patterns
- ✅ Risk assessment with reasoning

**Trust Semantic Analysis for:**
- ✅ Unbiased theme discovery
- ✅ Finding similar comments
- ✅ Keyword extraction
- ✅ Coherence measurement
- ✅ Pattern detection without predefined categories

**Trust Both (Hybrid) for:**
- ✅ Cross-validation of findings
- ✅ Comprehensive understanding
- ✅ Higher confidence in critical issues
- ✅ Discovering insights one model might miss

### **Interpreting Agreement:**

**Both providers agree:**
- 🟢 **High confidence** - Issue is likely real and significant
- Example: Claude says "High risk" + Semantic shows low coherence

**Providers disagree:**
- 🟡 **Investigate further** - May need human review
- Example: Claude says "Low risk" but Semantic shows many urgent keywords

**Only one provider detects:**
- 🔵 **Provider-specific insight** - May be valid but needs validation
- Example: Semantic finds theme Claude didn't explicitly mention

---

## 🆘 Troubleshooting

### **"I don't see provider labels"**
- Ensure you've run **hybrid analysis** (not just Claude or keyword)
- Check that semantic service is running on port 5001
- Re-run analysis if it was done before the UI update

### **"All data shows as Claude"**
- You may have run AI-enhanced analysis (Claude only)
- Run hybrid analysis to get both providers
- Check semantic service status: `curl http://localhost:5001/health`

### **"Semantic results are empty"**
- Restart semantic service with updated code (PascalCase JSON)
- Re-run hybrid analysis
- Check semantic service logs for errors

---

## ✅ Quick Reference

| Visual Indicator | Provider | Data Type |
|-----------------|----------|-----------|
| 🤖 Blue badge | Claude AI | Sentiment, Risk, Challenges, Recommendations |
| 🧠 Green badge | Semantic | Themes, Keywords, Coherence |
| 🔀 Purple badge | Hybrid | Both providers + Integrated insights |
| Blue section background | Claude AI | Detailed challenges, Recommendations |
| Green section background | Semantic | Theme analysis, Keywords |
| `claude*` prefix | Claude AI | All Claude-specific fields |
| `semantic*` prefix | Semantic | All Semantic-specific fields |
| `integrated*` prefix | Combined | Cross-validated insights |

---

**Remember:** Provider attribution is built into every level of the system - from field names to visual indicators. You'll always know exactly where each insight came from! 🎯
