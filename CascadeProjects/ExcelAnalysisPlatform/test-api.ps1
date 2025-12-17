# Excel Analysis Platform - Test Script
# This script tests the API with your Excel file

Write-Host "=== Excel Analysis Platform Test ===" -ForegroundColor Cyan
Write-Host ""

# Configuration
$apiBaseUrl = "https://localhost:7001/api/Analysis"
$excelFilePath = "C:\Users\KPeterson\Downloads\docanalysis\SAPR2-MAY-2023.xlsx"

# Check if file exists
if (-not (Test-Path $excelFilePath)) {
    Write-Host "ERROR: Excel file not found at: $excelFilePath" -ForegroundColor Red
    exit 1
}

Write-Host "Excel file found: $excelFilePath" -ForegroundColor Green
Write-Host "File size: $((Get-Item $excelFilePath).Length / 1KB) KB" -ForegroundColor Gray
Write-Host ""

# Step 1: Upload file
Write-Host "Step 1: Uploading Excel file..." -ForegroundColor Yellow
try {
    $form = @{
        file = Get-Item -Path $excelFilePath
    }
    
    $uploadResponse = Invoke-RestMethod -Uri "$apiBaseUrl/upload" -Method Post -Form $form -SkipCertificateCheck
    $fileId = $uploadResponse.id
    
    Write-Host "✓ File uploaded successfully!" -ForegroundColor Green
    Write-Host "  File ID: $fileId" -ForegroundColor Gray
    Write-Host "  Filename: $($uploadResponse.fileName)" -ForegroundColor Gray
    Write-Host "  Total Rows: $($uploadResponse.totalRows)" -ForegroundColor Gray
    Write-Host "  Total Columns: $($uploadResponse.totalColumns)" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host "✗ Upload failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Make sure the API is running at: $apiBaseUrl" -ForegroundColor Yellow
    exit 1
}

# Step 2: Analyze file
Write-Host "Step 2: Analyzing file with AI (this may take 30-60 seconds)..." -ForegroundColor Yellow
try {
    $analysisResponse = Invoke-RestMethod -Uri "$apiBaseUrl/$fileId/analyze" -Method Post -SkipCertificateCheck
    
    Write-Host "✓ Analysis completed!" -ForegroundColor Green
    Write-Host ""
    
    # Display key metrics
    Write-Host "=== ANALYSIS RESULTS ===" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "Progress Overview:" -ForegroundColor White
    Write-Host "  Overall Completion: $($analysisResponse.completionPercentage)%" -ForegroundColor Gray
    Write-Host "  Total Deliverables: $($analysisResponse.totalDeliverables)" -ForegroundColor Gray
    Write-Host "  Completed: $($analysisResponse.completedDeliverables)" -ForegroundColor Green
    Write-Host "  In Progress: $($analysisResponse.inProgressDeliverables)" -ForegroundColor Yellow
    Write-Host "  Not Started: $($analysisResponse.notStartedDeliverables)" -ForegroundColor Red
    Write-Host ""
    
    Write-Host "Risk Assessment:" -ForegroundColor White
    Write-Host "  High/Critical Risks: $($analysisResponse.highRiskCount)" -ForegroundColor Red
    Write-Host "  Medium Risks: $($analysisResponse.mediumRiskCount)" -ForegroundColor Yellow
    Write-Host "  Low Risks: $($analysisResponse.lowRiskCount)" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "Sentiment Analysis:" -ForegroundColor White
    Write-Host "  Overall Score: $($analysisResponse.overallSentimentScore)" -ForegroundColor Gray
    Write-Host "  Summary: $($analysisResponse.sentimentSummary)" -ForegroundColor Gray
    Write-Host ""
    
    if ($analysisResponse.identifiedIssues.Count -gt 0) {
        Write-Host "Identified Issues ($($analysisResponse.identifiedIssues.Count)):" -ForegroundColor White
        $analysisResponse.identifiedIssues | Select-Object -First 5 | ForEach-Object {
            Write-Host "  • $_" -ForegroundColor Gray
        }
        Write-Host ""
    }
    
    if ($analysisResponse.blockers.Count -gt 0) {
        Write-Host "Blockers ($($analysisResponse.blockers.Count)):" -ForegroundColor Red
        $analysisResponse.blockers | ForEach-Object {
            Write-Host "  • $_" -ForegroundColor Gray
        }
        Write-Host ""
    }
    
    if ($analysisResponse.recommendations.Count -gt 0) {
        Write-Host "Recommendations:" -ForegroundColor Cyan
        $analysisResponse.recommendations | ForEach-Object {
            Write-Host "  • $_" -ForegroundColor Gray
        }
        Write-Host ""
    }
    
    Write-Host "Executive Summary:" -ForegroundColor White
    Write-Host $analysisResponse.executiveSummary -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host "✗ Analysis failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Make sure Ollama is running with llama3.2 model" -ForegroundColor Yellow
    Write-Host "Run: ollama pull llama3.2" -ForegroundColor Yellow
    exit 1
}

# Step 3: Get detailed results
Write-Host "Step 3: Retrieving detailed results..." -ForegroundColor Yellow
try {
    $detailedResults = Invoke-RestMethod -Uri "$apiBaseUrl/$fileId/results" -Method Get -SkipCertificateCheck
    
    # Save to JSON file
    $outputFile = "analysis-results-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $detailedResults | ConvertTo-Json -Depth 10 | Out-File $outputFile
    
    Write-Host "✓ Detailed results saved to: $outputFile" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "✗ Failed to retrieve detailed results: $($_.Exception.Message)" -ForegroundColor Red
}

# Summary
Write-Host "=== TEST COMPLETED ===" -ForegroundColor Cyan
Write-Host "File ID: $fileId" -ForegroundColor Gray
Write-Host "You can now:" -ForegroundColor White
Write-Host "  1. View results in Swagger UI: https://localhost:7001" -ForegroundColor Gray
Write-Host "  2. Query the API using file ID: $fileId" -ForegroundColor Gray
Write-Host "  3. Check the JSON output file: $outputFile" -ForegroundColor Gray
Write-Host ""
