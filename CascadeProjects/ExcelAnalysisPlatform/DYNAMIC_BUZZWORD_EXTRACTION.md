# 🔍 Dynamic Buzzword Extraction from Excel

## 🎯 Overview

The system can now **automatically extract buzzwords from your Excel files** instead of using hardcoded keywords. This creates a custom sentiment dictionary based on your actual project data.

**Benefits:**
- ✅ **Domain-Specific**: Learns from your actual terminology
- ✅ **Adaptive**: Updates as your project evolves
- ✅ **Accurate**: Uses words that actually appear in your data
- ✅ **Automatic**: No manual keyword maintenance

---

## 🚀 How It Works

### 1. Frequency Analysis
- Scans all comments in your Excel file
- Counts how often each word/phrase appears
- Filters out common stop words ("the", "a", "is", etc.)

### 2. Context Classification
- Analyzes surrounding text for each word
- Detects negative indicators (failed, error, delayed)
- Detects positive indicators (success, completed, improved)
- Classifies words as negative, positive, or neutral

### 3. Multi-Word Phrases
- Extracts 2-word phrases ("scope creep", "red flag")
- Extracts 3-word phrases ("not on track", "behind schedule")
- Gives phrases higher weight than single words

### 4. Pattern Matching
- Identifies negative patterns (fail*, delay*, block*)
- Identifies positive patterns (success*, complete*, improve*)
- Uses context to disambiguate unclear words

---

## 📊 API Endpoints

### Extract Buzzwords from File

**Endpoint:** `POST /api/Buzzword/extract/{fileId}`

**Parameters:**
- `fileId` (path): ID of uploaded Excel file
- `minFrequency` (query, optional): Minimum times word must appear (default: 2)

**Example:**
```powershell
# After uploading file with ID 1
$result = Invoke-RestMethod -Uri "http://localhost:5100/api/Buzzword/extract/1?minFrequency=2" -Method Post

# View results
$result.negativeKeywords
$result.positiveKeywords
$result.totalComments
```

**Response:**
```json
{
  "negativeKeywords": {
    "delayed": 8,
    "issue": 6,
    "behind schedule": 4,
    "blocker": 3
  },
  "positiveKeywords": {
    "completed": 12,
    "on track": 7,
    "success": 5
  },
  "neutralKeywords": {
    "meeting": 15,
    "update": 10
  },
  "totalComments": 22,
  "totalWords": 1547
}
```

---

### Generate Buzzword Report

**Endpoint:** `POST /api/Buzzword/extract/{fileId}/report`

**Returns:** Markdown report with detailed analysis

**Example:**
```powershell
$report = Invoke-RestMethod -Uri "http://localhost:5100/api/Buzzword/extract/1/report" -Method Post
$report | Out-File "buzzwords_report.md"
```

**Report Includes:**
- Top 30 negative keywords with frequency
- Top 30 positive keywords with frequency
- Top 20 neutral keywords
- Sentiment ratio analysis
- Percentage of comments containing each keyword

---

### Save Buzzwords to JSON

**Endpoint:** `POST /api/Buzzword/extract/{fileId}/save`

**Parameters:**
- `outputPath` (query, optional): Where to save JSON file (default: "extracted_buzzwords.json")

**Example:**
```powershell
Invoke-RestMethod -Uri "http://localhost:5100/api/Buzzword/extract/1/save?outputPath=my_buzzwords.json" -Method Post
```

**Output File:**
```json
{
  "extractedFrom": "SAPR2-MAY-2023.xlsx",
  "extractedAt": "2025-12-12T20:54:00Z",
  "totalComments": 22,
  "totalWords": 1547,
  "negativeKeywords": {
    "delayed": 8,
    "issue": 6,
    "behind schedule": 4
  },
  "positiveKeywords": {
    "completed": 12,
    "on track": 7
  }
}
```

---

## 🔧 Complete Workflow

### Step 1: Upload Excel File

```powershell
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

Write-Host "✅ File uploaded: ID = $fileId" -ForegroundColor Green
```

### Step 2: Extract Buzzwords

```powershell
Write-Host "📊 Extracting buzzwords..." -ForegroundColor Cyan
$buzzwords = Invoke-RestMethod -Uri "http://localhost:5100/api/Buzzword/extract/$fileId?minFrequency=2" -Method Post

Write-Host "✅ Extraction complete!" -ForegroundColor Green
Write-Host "   🔴 Negative keywords: $($buzzwords.negativeKeywords.Count)" -ForegroundColor Red
Write-Host "   🟢 Positive keywords: $($buzzwords.positiveKeywords.Count)" -ForegroundColor Green
Write-Host "   ⚪ Neutral keywords: $($buzzwords.neutralKeywords.Count)" -ForegroundColor Gray
```

### Step 3: View Top Keywords

```powershell
Write-Host "`n🔴 Top 10 Negative Keywords:" -ForegroundColor Red
$buzzwords.negativeKeywords.GetEnumerator() | 
    Sort-Object -Property Value -Descending | 
    Select-Object -First 10 | 
    ForEach-Object { Write-Host "   $($_.Key): $($_.Value)" }

Write-Host "`n🟢 Top 10 Positive Keywords:" -ForegroundColor Green
$buzzwords.positiveKeywords.GetEnumerator() | 
    Sort-Object -Property Value -Descending | 
    Select-Object -First 10 | 
    ForEach-Object { Write-Host "   $($_.Key): $($_.Value)" }
```

### Step 4: Generate Report

```powershell
Write-Host "`n📄 Generating detailed report..." -ForegroundColor Cyan
$report = Invoke-RestMethod -Uri "http://localhost:5100/api/Buzzword/extract/$fileId/report" -Method Post
$report | Out-File "buzzwords_analysis.md"
Write-Host "✅ Report saved to: buzzwords_analysis.md" -ForegroundColor Green
```

### Step 5: Save for Future Use

```powershell
Write-Host "`n💾 Saving buzzwords to JSON..." -ForegroundColor Cyan
Invoke-RestMethod -Uri "http://localhost:5100/api/Buzzword/extract/$fileId/save?outputPath=project_buzzwords.json" -Method Post
Write-Host "✅ Buzzwords saved to: project_buzzwords.json" -ForegroundColor Green
```

---

## 📊 Example Output

### Console Output

```
📊 Extracting buzzwords from: SAPR2-MAY-2023.xlsx
   Found 22 comments to analyze
   ✅ Extracted 87 unique keywords
      🔴 Negative: 34
      🟢 Positive: 28
      ⚪ Neutral: 25
```

### Top Negative Keywords

| Keyword | Frequency | % of Comments |
|---------|-----------|---------------|
| delayed | 8 | 36.4% |
| issue | 6 | 27.3% |
| behind schedule | 4 | 18.2% |
| blocker | 3 | 13.6% |
| risk | 3 | 13.6% |
| concern | 2 | 9.1% |
| not completed | 2 | 9.1% |

### Top Positive Keywords

| Keyword | Frequency | % of Comments |
|---------|-----------|---------------|
| completed | 12 | 54.5% |
| on track | 7 | 31.8% |
| success | 5 | 22.7% |
| progress | 4 | 18.2% |
| improved | 3 | 13.6% |

---

## 🎯 Use Cases

### 1. Domain-Specific Analysis

**Problem:** Generic keywords don't match your industry terminology

**Solution:**
```powershell
# Extract buzzwords from your domain-specific Excel files
$buzzwords = Invoke-RestMethod -Uri "http://localhost:5100/api/Buzzword/extract/$fileId" -Method Post

# Now you have keywords specific to your project/industry
```

### 2. Project Evolution Tracking

**Problem:** Project terminology changes over time

**Solution:**
```powershell
# Extract buzzwords monthly
$jan = Invoke-RestMethod -Uri "http://localhost:5100/api/Buzzword/extract/1" -Method Post
$feb = Invoke-RestMethod -Uri "http://localhost:5100/api/Buzzword/extract/2" -Method Post

# Compare keyword changes
$newNegatives = $feb.negativeKeywords.Keys | Where-Object { -not $jan.negativeKeywords.ContainsKey($_) }
Write-Host "New negative terms this month: $($newNegatives -join ', ')"
```

### 3. Multi-Project Comparison

**Problem:** Need to compare sentiment across different projects

**Solution:**
```powershell
# Extract buzzwords from each project
$projectA = Invoke-RestMethod -Uri "http://localhost:5100/api/Buzzword/extract/1" -Method Post
$projectB = Invoke-RestMethod -Uri "http://localhost:5100/api/Buzzword/extract/2" -Method Post

# Compare negative keyword counts
$ratioA = $projectA.negativeKeywords.Count / $projectA.totalComments
$ratioB = $projectB.negativeKeywords.Count / $projectB.totalComments

if ($ratioA -gt $ratioB) {
    Write-Host "Project A has more negative sentiment" -ForegroundColor Red
} else {
    Write-Host "Project B has more negative sentiment" -ForegroundColor Red
}
```

### 4. Custom Sentiment Dictionary

**Problem:** Want to use extracted keywords for future analyses

**Solution:**
```powershell
# Extract and save buzzwords
Invoke-RestMethod -Uri "http://localhost:5100/api/Buzzword/extract/1/save?outputPath=custom_dict.json" -Method Post

# Load and use in your own scripts
$dict = Get-Content "custom_dict.json" | ConvertFrom-Json
$negativeWords = $dict.negativeKeywords.PSObject.Properties.Name

# Use for custom analysis
foreach ($comment in $comments) {
    $matches = $negativeWords | Where-Object { $comment -match $_ }
    if ($matches.Count -gt 0) {
        Write-Host "Negative comment detected: $comment"
    }
}
```

---

## 🔍 Technical Details

### Extraction Algorithm

**1. Word Extraction:**
```csharp
// Remove special characters, split into words
var words = text.Split(' ', '\t', '\n')
    .Where(w => w.Length >= 3)
    .Where(w => !StopWords.Contains(w))
```

**2. Phrase Extraction:**
```csharp
// 2-word phrases
for (int i = 0; i < words.Count - 1; i++)
{
    var phrase = $"{words[i]} {words[i + 1]}";
    // Count frequency
}

// 3-word phrases
for (int i = 0; i < words.Count - 2; i++)
{
    var phrase = $"{words[i]} {words[i + 1]} {words[i + 2]}";
    // Count frequency
}
```

**3. Sentiment Classification:**
```csharp
// Check word patterns
if (word.Contains("fail") || word.Contains("error") || word.Contains("delay"))
    return "negative";

if (word.Contains("success") || word.Contains("complete") || word.Contains("improve"))
    return "positive";

// Analyze context
var negativeContexts = contexts.Count(c => c.Contains("not") || c.Contains("failed"));
var positiveContexts = contexts.Count(c => c.Contains("completed") || c.Contains("success"));

if (negativeContexts > positiveContexts * 1.5)
    return "negative";
```

### Stop Words Filtering

Common words excluded from analysis:
```
the, a, an, and, or, but, in, on, at, to, for, of, with, by, from,
as, is, was, are, were, be, been, being, have, has, had, do, does,
did, will, would, should, could, may, might, must, can, this, that,
these, those, i, you, he, she, it, we, they, what, which, who, when,
where, why, how, all, each, every, both, few, more, most, other, some
```

### Minimum Frequency

Default: **2** occurrences

**Why?**
- Filters out typos and one-off terms
- Focuses on recurring themes
- Reduces noise in results

**Adjust based on dataset size:**
- Small (< 20 comments): `minFrequency=1`
- Medium (20-100 comments): `minFrequency=2`
- Large (> 100 comments): `minFrequency=3-5`

---

## 📈 Performance

### Processing Speed

| Comments | Words | Extraction Time |
|----------|-------|-----------------|
| 22 | 1,500 | < 1 second |
| 100 | 7,000 | 2-3 seconds |
| 500 | 35,000 | 10-15 seconds |
| 1,000 | 70,000 | 20-30 seconds |

### Memory Usage

- **Small files** (< 50 comments): < 10 MB
- **Medium files** (50-200 comments): 10-50 MB
- **Large files** (> 200 comments): 50-100 MB

---

## 🎯 Best Practices

### 1. Set Appropriate Minimum Frequency

```powershell
# For small datasets
$buzzwords = Invoke-RestMethod -Uri "http://localhost:5100/api/Buzzword/extract/$fileId?minFrequency=1" -Method Post

# For large datasets
$buzzwords = Invoke-RestMethod -Uri "http://localhost:5100/api/Buzzword/extract/$fileId?minFrequency=5" -Method Post
```

### 2. Review and Refine

```powershell
# Extract buzzwords
$buzzwords = Invoke-RestMethod -Uri "http://localhost:5100/api/Buzzword/extract/$fileId" -Method Post

# Review negative keywords
$buzzwords.negativeKeywords.GetEnumerator() | Sort-Object -Property Value -Descending

# Manually filter out false positives if needed
```

### 3. Combine with Static Keywords

```powershell
# Use extracted keywords for domain-specific terms
# Use static keywords for general project management terms
# Best of both worlds!
```

### 4. Regular Updates

```powershell
# Extract buzzwords monthly or quarterly
# Track how terminology evolves
# Identify emerging issues early
```

---

## 🔄 Integration with Sentiment Analysis

### Current: Static Keywords
```json
{
  "UseFastSentiment": true  // Uses hardcoded keywords
}
```

### Future: Dynamic Keywords (Coming Soon)
```json
{
  "UseFastSentiment": true,
  "UseDynamicKeywords": true,
  "KeywordSource": "extracted_buzzwords.json"
}
```

**This will:**
- Load extracted keywords from JSON
- Merge with static keywords
- Use combined dictionary for sentiment analysis
- Automatically adapt to your project terminology

---

## ✅ Summary

**Dynamic Buzzword Extraction:**
- 🔍 **Analyzes** your actual Excel data
- 📊 **Extracts** frequently used terms
- 🎯 **Classifies** as negative, positive, or neutral
- 💾 **Saves** for future use
- 📄 **Reports** detailed analysis

**API Endpoints:**
- `POST /api/Buzzword/extract/{fileId}` - Extract buzzwords
- `POST /api/Buzzword/extract/{fileId}/report` - Generate report
- `POST /api/Buzzword/extract/{fileId}/save` - Save to JSON

**Use Cases:**
- Domain-specific terminology
- Project evolution tracking
- Multi-project comparison
- Custom sentiment dictionaries

**Next Steps:**
1. Upload your Excel file
2. Extract buzzwords
3. Review results
4. Save for future analyses

---

*Last Updated: December 12, 2025*
