# Excel Analysis Platform - Setup Guide

## Prerequisites

1. **.NET 9 SDK** - Already installed ✓
2. **Ollama** - Free local AI runtime
3. **Your Excel file** - Located at `C:\Users\KPeterson\Downloads\docanalysis\SAPR2-MAY-2023.xlsx`

## Step 1: Install Ollama

### Download and Install
1. Visit: https://ollama.ai/download
2. Download the Windows installer
3. Run the installer
4. Ollama will start automatically as a Windows service

### Pull the AI Model
Open PowerShell and run:
```powershell
ollama pull llama3.2
```

This downloads a free, local LLM (about 2GB). Wait for it to complete.

### Verify Ollama is Running
```powershell
ollama list
```

You should see `llama3.2` in the list.

## Step 2: Run the Application

### Option A: Using Visual Studio
1. Open `ExcelAnalysisPlatform.sln` in Visual Studio
2. Set `ExcelAnalysis.API` as the startup project
3. Press F5 to run

### Option B: Using Command Line
```powershell
cd C:\Users\KPeterson\CascadeProjects\ExcelAnalysisPlatform\src\ExcelAnalysis.API
dotnet run
```

The API will start at: **https://localhost:7001**

## Step 3: Access the Swagger UI

Open your browser and navigate to:
```
https://localhost:7001
```

You'll see the Swagger UI with all available API endpoints.

## Step 4: Upload and Analyze Your Excel File

### Using Swagger UI:

1. **Upload File**
   - Click on `POST /api/Analysis/upload`
   - Click "Try it out"
   - Click "Choose File" and select your Excel file
   - Click "Execute"
   - Copy the `id` from the response (e.g., `1`)

2. **Analyze File**
   - Click on `POST /api/Analysis/{fileId}/analyze`
   - Click "Try it out"
   - Enter the file ID from step 1
   - Click "Execute"
   - Wait for analysis to complete (may take 30-60 seconds)

3. **View Results**
   - Click on `GET /api/Analysis/{fileId}/results`
   - Click "Try it out"
   - Enter the file ID
   - Click "Execute"
   - See the complete analysis report

### Using PowerShell:

```powershell
# Upload file
$uploadUrl = "https://localhost:7001/api/Analysis/upload"
$filePath = "C:\Users\KPeterson\Downloads\docanalysis\SAPR2-MAY-2023.xlsx"

$form = @{
    file = Get-Item -Path $filePath
}

$response = Invoke-RestMethod -Uri $uploadUrl -Method Post -Form $form -SkipCertificateCheck
$fileId = $response.id
Write-Host "File uploaded with ID: $fileId"

# Analyze file
$analyzeUrl = "https://localhost:7001/api/Analysis/$fileId/analyze"
$analysis = Invoke-RestMethod -Uri $analyzeUrl -Method Post -SkipCertificateCheck
Write-Host "Analysis completed!"

# Get results
$resultsUrl = "https://localhost:7001/api/Analysis/$fileId/results"
$results = Invoke-RestMethod -Uri $resultsUrl -Method Get -SkipCertificateCheck
$results | ConvertTo-Json -Depth 10
```

## What the System Analyzes

### 1. Progress Metrics
- Completion percentage per deliverable
- Yes/No question analysis
- Status classification (Not Started, In Progress, Completed, Blocked)

### 2. Risk Assessment
- Identifies high/medium/low risks from comments
- Classifies risk levels using AI
- Provides mitigation strategies

### 3. Sentiment Analysis
- Overall sentiment score (-1 to 1)
- Positive/negative trend detection
- Team morale indicators

### 4. Issues & Blockers
- Automatically extracts issues from comments
- Identifies blockers
- Generates recommendations

### 5. Executive Summary
- High-level overview
- Key metrics
- Action items

## Sample Output

```json
{
  "completionPercentage": 67.5,
  "totalDeliverables": 10,
  "completedDeliverables": 7,
  "inProgressDeliverables": 2,
  "notStartedDeliverables": 1,
  "highRiskCount": 2,
  "mediumRiskCount": 3,
  "lowRiskCount": 5,
  "overallSentimentScore": 0.3,
  "executiveSummary": "Analysis completed on 2025-12-11...",
  "riskItems": [...],
  "progressMetrics": [...],
  "identifiedIssues": [...],
  "blockers": [...],
  "recommendations": [...]
}
```

## Troubleshooting

### Ollama Not Running
```powershell
# Check if Ollama is running
Get-Process ollama

# If not running, start it
ollama serve
```

### Model Not Found
```powershell
# List available models
ollama list

# Pull the model if missing
ollama pull llama3.2
```

### Database Issues
The SQLite database is created automatically at `src/ExcelAnalysis.API/analysis.db`.
To reset:
```powershell
Remove-Item src/ExcelAnalysis.API/analysis.db
# Restart the API - database will be recreated
```

### Port Already in Use
Edit `src/ExcelAnalysis.API/Properties/launchSettings.json` to change the port.

## Architecture Overview

```
┌─────────────────────────────────────────────┐
│         Excel File Upload                   │
└──────────────┬──────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────┐
│   ExcelProcessor (EPPlus)                   │
│   - Parses .xlsx files                      │
│   - Extracts rows, columns, data            │
│   - Identifies comments & yes/no questions  │
└──────────────┬──────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────┐
│   AIAnalyzer (Ollama + Semantic Kernel)     │
│   - Risk classification                     │
│   - Sentiment analysis                      │
│   - Issue extraction                        │
│   - Progress calculation                    │
└──────────────┬──────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────┐
│   AnalysisRepository (EF Core + SQLite)     │
│   - Stores file metadata                    │
│   - Caches analysis results                 │
│   - Provides query interface                │
└──────────────┬──────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────┐
│   REST API (ASP.NET Core)                   │
│   - Swagger UI                              │
│   - JSON responses                          │
│   - CORS enabled for frontend               │
└─────────────────────────────────────────────┘
```

## Next Steps

### Option 1: Build a Frontend
Create a Blazor Server or React frontend to visualize the data with:
- Interactive dashboards
- Charts and graphs
- Data tables
- Export to PDF/Excel

### Option 2: Extend Analysis
Add more AI capabilities:
- Trend analysis over time
- Predictive analytics
- Custom report templates
- Email notifications

### Option 3: Integration
Integrate with existing systems:
- SharePoint for file storage
- Teams for notifications
- Power BI for visualization
- Azure DevOps for tracking

## Support

For issues or questions:
1. Check the Swagger UI for API documentation
2. Review the logs in the console output
3. Examine the SQLite database with DB Browser for SQLite
4. Test Ollama separately: `ollama run llama3.2 "test"`
