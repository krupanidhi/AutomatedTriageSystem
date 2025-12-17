# Changelog

## Version 1.1 - Multi-Sheet Support (2025-12-11)

### ✨ New Features
- **Multi-sheet processing**: Now reads ALL sheets in Excel file, not just the first one
- **Sheet tracking**: Each row now includes which sheet it came from
- **Comprehensive column extraction**: Aggregates columns from all sheets

### 🔧 Changes Made

#### 1. Updated `ExcelRow` Model
- Added `SheetName` property to track source sheet
- Added database index on `SheetName` for faster queries

#### 2. Updated `ExcelProcessor`
- Changed from single-sheet to multi-sheet processing
- Processes all worksheets in the Excel file
- Aggregates row counts and column names across all sheets
- Adds `_SheetName` field to each row's JSON data

#### 3. Database Schema
- Added `SheetName` column to `ExcelRows` table
- Added index on `SheetName` for performance
- Database automatically recreated with new schema

### 📊 What This Means

**Before:**
- Only "Search Parameters" sheet was processed
- Other sheets ("2023 Data Set", "Change Request", "Contribution Request") were ignored

**After:**
- ALL 4 sheets are now processed:
  - Search Parameters
  - 2023 Data Set
  - Change Request
  - Contribution Request
- Each row knows which sheet it came from
- Analysis includes data from all sheets

### 🔄 Breaking Changes
- Database schema changed (old database deleted and recreated)
- `ExcelRow` model now includes `SheetName` property
- Row counts now reflect total across all sheets

### 📝 Example Output

When you upload your Excel file now, you'll see:
```json
{
  "sheetNames": [
    "Search Parameters",
    "2023 Data Set", 
    "Change Request",
    "Contribution Request"
  ],
  "totalRows": 1234,  // Total from ALL sheets
  "rows": [
    {
      "sheetName": "2023 Data Set",
      "rowNumber": 5,
      "dataJson": "{\"_SheetName\":\"2023 Data Set\", ...}"
    }
  ]
}
```

### 🚀 How to Use

1. **Upload your Excel file** - All sheets will be processed automatically
2. **Analysis will include data from all sheets** - Comments, questions, and data from every sheet
3. **Filter by sheet** - You can query specific sheets using the `SheetName` property

---

## Version 1.0 - Initial Release (2025-12-11)

### Features
- Excel file upload and processing
- AI-powered risk assessment
- Progress tracking from yes/no questions
- Sentiment analysis
- Issue and blocker detection
- Executive summary generation
- REST API with Swagger UI
- SQLite database storage
- Local AI using Ollama (no cloud costs)
