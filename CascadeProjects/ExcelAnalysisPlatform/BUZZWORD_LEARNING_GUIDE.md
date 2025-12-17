# 🧠 Persistent Buzzword Learning System

## Overview

The **Persistent Buzzword Learning System** automatically learns and remembers sentiment keywords across multiple Excel file analyses. Each time you analyze a new file, the system:

1. **Extracts** buzzwords from the new file
2. **Detects** new keywords (delta) not seen before
3. **Merges** them with existing knowledge base
4. **Persists** to disk for future analyses
5. **Grows smarter** with each file analyzed

---

## ✅ Works with ALL AI Providers

The buzzword learning system is **provider-agnostic**:

- ✅ **Ollama** (Local, Unlimited)
- ✅ **OpenAI** (Cloud, Paid)
- ✅ **Gemini** (Cloud, Limited Free)

**Sentiment analysis uses buzzwords (0 API calls) regardless of provider!**

---

## 🚀 How It Works

### First Analysis
```
File: project_comments_1.xlsx
📚 Learning buzzwords from: project_comments_1.xlsx
   🆕 New Negative: 649
   🆕 New Positive: 1,089
   📊 Total Negative: 649
   📊 Total Positive: 1,089
   📈 Total Buzzwords: 1,738
   📁 Files Analyzed: 1
💾 Knowledge base saved: buzzword_knowledge.json
```

### Second Analysis (Delta Learning)
```
File: project_comments_2.xlsx
📚 Learning buzzwords from: project_comments_2.xlsx
   🆕 New Negative: 127  ← Only new words!
   🆕 New Positive: 243
   📊 Total Negative: 776  ← Growing!
   📊 Total Positive: 1,332
   📈 Total Buzzwords: 2,108
   📁 Files Analyzed: 2
💾 Knowledge base saved: buzzword_knowledge.json
```

### Third Analysis (Even Smarter)
```
File: project_comments_3.xlsx
📚 Learning buzzwords from: project_comments_3.xlsx
   🆕 New Negative: 45  ← Fewer new words (already learned most)
   🆕 New Positive: 89
   📊 Total Negative: 821
   📊 Total Positive: 1,421
   📈 Total Buzzwords: 2,242
   📁 Files Analyzed: 3
💾 Knowledge base saved: buzzword_knowledge.json
```

---

## 📊 API Endpoints

### 1. Learn from File
Analyze a file and add new buzzwords to knowledge base:

```powershell
# Learn from uploaded file
$result = Invoke-RestMethod -Uri "http://localhost:5100/api/BuzzwordLearning/learn/$fileId" -Method Post

# Response
{
  "success": true,
  "fileName": "project_comments.xlsx",
  "newNegativeBuzzwords": 127,
  "newPositiveBuzzwords": 243,
  "totalNegativeBuzzwords": 776,
  "totalPositiveBuzzwords": 1332,
  "totalBuzzwords": 2108,
  "filesAnalyzed": 2,
  "newNegativeWords": ["blocker", "critical", "urgent", ...],
  "newPositiveWords": ["completed", "success", "approved", ...]
}
```

### 2. Get Knowledge Base Statistics
```powershell
$stats = Invoke-RestMethod -Uri "http://localhost:5100/api/BuzzwordLearning/stats" -Method Get

# Response
{
  "totalNegativeKeywords": 821,
  "totalPositiveKeywords": 1421,
  "totalKeywords": 2242,
  "filesAnalyzed": 3,
  "lastUpdated": "2024-12-12T22:30:00Z",
  "topNegativeKeywords": {
    "delay": 45,
    "issue": 38,
    "blocker": 32,
    ...
  },
  "topPositiveKeywords": {
    "completed": 67,
    "success": 54,
    "approved": 48,
    ...
  }
}
```

### 3. Reset Knowledge Base
```powershell
Invoke-RestMethod -Uri "http://localhost:5100/api/BuzzwordLearning/reset" -Method Post
```

---

## 💾 Storage

**File:** `buzzword_knowledge.json`

**Location:** API project root directory

**Format:**
```json
{
  "NegativeKeywords": {
    "delay": 45,
    "issue": 38,
    "blocker": 32,
    ...
  },
  "PositiveKeywords": {
    "completed": 67,
    "success": 54,
    "approved": 48,
    ...
  },
  "TotalFilesAnalyzed": 3,
  "LastUpdated": "2024-12-12T22:30:00Z",
  "AnalyzedFiles": [
    {
      "FileName": "project_comments_1.xlsx",
      "AnalyzedAt": "2024-12-12T20:00:00Z",
      "NewNegativeCount": 649,
      "NewPositiveCount": 1089
    },
    {
      "FileName": "project_comments_2.xlsx",
      "AnalyzedAt": "2024-12-12T21:00:00Z",
      "NewNegativeCount": 127,
      "NewPositiveCount": 243
    }
  ]
}
```

---

## 🔄 Workflow

### Automatic Learning (Recommended)

The system automatically learns during analysis:

```powershell
# 1. Upload file
$file = Get-Item "project_comments.xlsx"
$form = @{
    file = $file
}
$upload = Invoke-RestMethod -Uri "http://localhost:5100/api/Upload" -Method Post -Form $form
$fileId = $upload.id

# 2. Analyze (automatically learns buzzwords)
$analysis = Invoke-RestMethod -Uri "http://localhost:5100/api/Analysis/$fileId/analyze" -Method Post

# 3. Check what was learned
$stats = Invoke-RestMethod -Uri "http://localhost:5100/api/BuzzwordLearning/stats" -Method Get
Write-Host "Total Buzzwords: $($stats.totalKeywords)"
Write-Host "Files Analyzed: $($stats.filesAnalyzed)"
```

### Manual Learning

Learn without full analysis:

```powershell
# Just extract and learn buzzwords (no AI analysis)
$result = Invoke-RestMethod -Uri "http://localhost:5100/api/BuzzwordLearning/learn/$fileId" -Method Post

Write-Host "New Negative: $($result.newNegativeBuzzwords)"
Write-Host "New Positive: $($result.newPositiveBuzzwords)"
```

---

## 🎯 Benefits

### 1. Growing Intelligence
- **First file**: Learns 1,738 buzzwords
- **Second file**: Adds 370 new buzzwords (2,108 total)
- **Third file**: Adds 134 new buzzwords (2,242 total)
- **Gets smarter** with each analysis

### 2. Cross-Project Learning
- Analyze **Project A** → Learn domain-specific terms
- Analyze **Project B** → Reuse + learn new terms
- Analyze **Project C** → Even smarter analysis

### 3. Zero API Calls
- Sentiment analysis: **0 API calls**
- Works with **any provider**
- **Instant** results

### 4. Persistent Knowledge
- Survives **API restarts**
- Survives **system reboots**
- **Cumulative learning** over time

---

## 📈 Example: Multi-File Analysis

```powershell
# Analyze 5 different project files
$files = @(
    "project_alpha.xlsx",
    "project_beta.xlsx",
    "project_gamma.xlsx",
    "project_delta.xlsx",
    "project_epsilon.xlsx"
)

foreach ($file in $files) {
    # Upload
    $upload = Invoke-RestMethod -Uri "http://localhost:5100/api/Upload" -Method Post -Form @{ file = Get-Item $file }
    
    # Analyze (learns automatically)
    $analysis = Invoke-RestMethod -Uri "http://localhost:5100/api/Analysis/$($upload.id)/analyze" -Method Post
    
    # Show learning progress
    $stats = Invoke-RestMethod -Uri "http://localhost:5100/api/BuzzwordLearning/stats" -Method Get
    Write-Host "$file → Total Buzzwords: $($stats.totalKeywords)"
}

# Final stats
Write-Host "`nFinal Knowledge Base:"
Write-Host "  Negative Keywords: $($stats.totalNegativeKeywords)"
Write-Host "  Positive Keywords: $($stats.totalPositiveKeywords)"
Write-Host "  Files Analyzed: $($stats.filesAnalyzed)"
```

**Output:**
```
project_alpha.xlsx → Total Buzzwords: 1,738
project_beta.xlsx → Total Buzzwords: 2,108
project_gamma.xlsx → Total Buzzwords: 2,242
project_delta.xlsx → Total Buzzwords: 2,315
project_epsilon.xlsx → Total Buzzwords: 2,367

Final Knowledge Base:
  Negative Keywords: 891
  Positive Keywords: 1,476
  Files Analyzed: 5
```

---

## 🔧 Configuration

### Enable/Disable Dynamic Keywords

In `appsettings.json`:

```json
{
  "AI": {
    "UseDynamicKeywords": true  // Enable persistent learning
  }
}
```

### Change Storage Location

```csharp
// In Program.cs or Startup.cs
var learner = new PersistentBuzzwordLearner("custom_path/buzzwords.json");
```

---

## 🎓 Best Practices

### 1. Analyze Similar Projects Together
Group related projects to build domain-specific knowledge:
- **Construction projects** → Learn construction terms
- **Software projects** → Learn tech terms
- **Healthcare projects** → Learn medical terms

### 2. Review Top Keywords Periodically
```powershell
$stats = Invoke-RestMethod -Uri "http://localhost:5100/api/BuzzwordLearning/stats" -Method Get
$stats.topNegativeKeywords | Format-Table
$stats.topPositiveKeywords | Format-Table
```

### 3. Reset When Changing Domains
If switching from construction to software projects:
```powershell
Invoke-RestMethod -Uri "http://localhost:5100/api/BuzzwordLearning/reset" -Method Post
```

### 4. Backup Knowledge Base
```powershell
Copy-Item "buzzword_knowledge.json" "buzzword_knowledge_backup.json"
```

---

## 🆚 Comparison

### Static Keywords (Old)
- ❌ Fixed set of ~100 keywords
- ❌ Same for all projects
- ❌ Never learns
- ❌ Generic terms only

### Dynamic Keywords (Current)
- ✅ Extracts from each file
- ✅ Project-specific
- ✅ Learns from current file
- ✅ Domain-specific terms

### Persistent Learning (New!)
- ✅ Learns from ALL files
- ✅ Remembers across analyses
- ✅ Grows smarter over time
- ✅ Cross-project intelligence
- ✅ Delta detection
- ✅ Cumulative knowledge

---

## 📊 Performance

**Learning Speed:**
- Extract buzzwords: ~1-2 seconds
- Detect delta: <100ms
- Merge knowledge: <50ms
- Save to disk: <100ms
- **Total overhead: ~2 seconds per file**

**Sentiment Analysis:**
- Uses learned buzzwords: **0 API calls**
- Instant results: <10ms per comment
- Works offline

---

## 🔍 Troubleshooting

### Knowledge Base Not Persisting
**Check:** File permissions on `buzzword_knowledge.json`
```powershell
# Verify file exists
Test-Path "buzzword_knowledge.json"

# Check contents
Get-Content "buzzword_knowledge.json" | ConvertFrom-Json
```

### No New Buzzwords Detected
**Reason:** File contains same terms as previous analyses
**Solution:** This is normal! System already learned those terms.

### Want to Start Fresh
```powershell
# Reset knowledge base
Invoke-RestMethod -Uri "http://localhost:5100/api/BuzzwordLearning/reset" -Method Post
```

---

## 🎉 Summary

**The Persistent Buzzword Learning System:**
- ✅ Works with **Ollama, OpenAI, Gemini**
- ✅ **Learns** from every file analyzed
- ✅ **Remembers** across sessions
- ✅ **Detects** new keywords (delta)
- ✅ **Grows** smarter over time
- ✅ **Zero** API calls for sentiment
- ✅ **Instant** sentiment analysis
- ✅ **Cross-project** intelligence

**Your system now has a memory and gets smarter with every analysis!** 🧠
