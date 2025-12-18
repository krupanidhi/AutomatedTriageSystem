# Excel Analysis Platform with AI (.NET/C#)

AI-powered system for analyzing Excel files containing reviewer comments and yes/no questions. Generates risk assessments, progress reports, and completion metrics.

## Features

- 📊 **Excel Upload & Processing** - Parse large Excel files with comments and questions using EPPlus
- 🤖 **Local AI Analysis** - Uses Semantic Kernel + Ollama (free, local LLM) for text analysis
- 📈 **Automated Reports** - Risk assessment, progress tracking, completion percentages
- 🎨 **Modern Web UI** - Blazor Server or React frontend with interactive dashboard
- 💾 **Data Persistence** - Entity Framework Core with SQL Server/SQLite

## Architecture

### Backend (.NET 9)
- **ASP.NET Core Web API** - REST API server
- **EPPlus** - Excel file processing (.xlsx)
- **Semantic Kernel** - Microsoft's AI orchestration framework
- **Ollama Integration** - Local LLM (no API costs)
- **Entity Framework Core** - Data persistence
- **AutoMapper** - Object mapping

### Frontend Options
- **Blazor Server** - C# full-stack (recommended for .NET shops)
- **React + TypeScript** - Alternative modern SPA
- **TailwindCSS** - Styling
- **Chart.js / Blazor Charts** - Data visualizations

## Setup Instructions

### 1. Install Ollama (Free Local AI)

Download and install from: https://ollama.ai

Then pull a model:
```powershell
ollama pull llama3.2
```

### 2. Restore .NET Dependencies

```powershell
dotnet restore
```

### 3. Update Database

```powershell
cd src/ExcelAnalysis.API
dotnet ef database update
```

## Running the Application

### Start Backend API
```powershell
cd src/ExcelAnalysis.API
dotnet run
```

API will be available at: https://localhost:7001

### Swagger UI
Access API documentation at: https://localhost:7001/swagger

## Usage

1. **Upload Excel File** - Drag and drop your Excel file
2. **AI Analysis** - System automatically analyzes comments and questions
3. **View Reports** - See risk matrix, progress charts, completion metrics
4. **Export Results** - Download analysis reports

## Excel File Format

Expected columns:
- Deliverable/Item name
- Reviewer comments (text)
- Yes/No questions
- Status/Progress indicators
- Due dates (optional)

## AI Analysis Capabilities

- **Risk Classification** - Identifies high/medium/low risks from comments
- **Sentiment Analysis** - Gauges reviewer sentiment
- **Progress Assessment** - Calculates completion percentages
- **Issue Extraction** - Identifies blockers and concerns
- **Trend Analysis** - Tracks progress over time
