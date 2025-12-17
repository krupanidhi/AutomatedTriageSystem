# Manual Testing Guide

## API is Running at: http://localhost:5100

## Option 1: Using Swagger UI (Easiest)

1. Open your browser and go to: **http://localhost:5100**
2. You'll see the Swagger UI with all API endpoints
3. Follow these steps:

### Step 1: Upload File
- Click on `POST /api/Analysis/upload`
- Click **"Try it out"**
- Click **"Choose File"** and select: `C:\Users\KPeterson\Downloads\docanalysis\SAPR2-MAY-2023.xlsx`
- Click **"Execute"**
- Copy the `id` from the response (e.g., `1`)

### Step 2: Analyze File
- Click on `POST /api/Analysis/{fileId}/analyze`
- Click **"Try it out"**
- Enter the file ID from Step 1
- Click **"Execute"**
- Wait 30-60 seconds for analysis to complete

### Step 3: View Results
- Click on `GET /api/Analysis/{fileId}/results`
- Click **"Try it out"**
- Enter the file ID
- Click **"Execute"**
- See the complete analysis report

---

## Option 2: Using PowerShell Commands

Copy and paste these commands one at a time:

### 1. Upload File
```powershell
$uploadUrl = "http://localhost:5100/api/Analysis/upload"
$filePath = "C:\Users\KPeterson\Downloads\docanalysis\SAPR2-MAY-2023.xlsx"

$form = @{
    file = Get-Item -Path $filePath
}

$response = Invoke-RestMethod -Uri $uploadUrl -Method Post -Form $form
$fileId = $response.id
Write-Host "File uploaded with ID: $fileId" -ForegroundColor Green
$response | ConvertTo-Json
```

### 2. Analyze File
```powershell
$analyzeUrl = "http://localhost:5100/api/Analysis/$fileId/analyze"
Write-Host "Starting analysis (this may take 30-60 seconds)..." -ForegroundColor Yellow
$analysis = Invoke-RestMethod -Uri $analyzeUrl -Method Post
Write-Host "Analysis completed!" -ForegroundColor Green
$analysis | ConvertTo-Json -Depth 5
```

### 3. Get Detailed Results
```powershell
$resultsUrl = "http://localhost:5100/api/Analysis/$fileId/results"
$results = Invoke-RestMethod -Uri $resultsUrl -Method Get
$results | ConvertTo-Json -Depth 10 | Out-File "analysis-results.json"
Write-Host "Results saved to analysis-results.json" -ForegroundColor Green
```

---

## Option 3: Using curl (if you have it)

### 1. Upload File
```bash
curl -X POST "http://localhost:5100/api/Analysis/upload" \
  -F "file=@C:\Users\KPeterson\Downloads\docanalysis\SAPR2-MAY-2023.xlsx"
```

### 2. Analyze File (replace {fileId} with actual ID)
```bash
curl -X POST "http://localhost:5100/api/Analysis/{fileId}/analyze"
```

### 3. Get Results
```bash
curl -X GET "http://localhost:5100/api/Analysis/{fileId}/results"
```

---

## Troubleshooting

### Ollama Not Running
If analysis fails, make sure Ollama is running:
```powershell
ollama serve
```

In another terminal:
```powershell
ollama pull llama3.2
```

### Check if API is Running
```powershell
Invoke-RestMethod -Uri "http://localhost:5100/api/Analysis/files" -Method Get
```

Should return an empty array `[]` if no files uploaded yet.

---

## Expected Output

After analysis, you should see:
- **Completion Percentage**: Overall progress
- **Risk Assessment**: High/Medium/Low risk counts
- **Sentiment Score**: -1 to +1
- **Issues & Blockers**: Extracted from comments
- **Recommendations**: AI-generated action items
- **Executive Summary**: High-level overview
