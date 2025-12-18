"""
FastAPI backend for Excel Analysis Platform
"""
from fastapi import FastAPI, File, UploadFile, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
import pandas as pd
import io
from typing import List, Dict, Any
from datetime import datetime
import json

from excel_processor import ExcelProcessor
from ai_analyzer import AIAnalyzer
from database import Database

app = FastAPI(title="Excel Analysis Platform API")

# CORS middleware for React frontend
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:5173", "http://localhost:3000"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Initialize components
excel_processor = ExcelProcessor()
ai_analyzer = AIAnalyzer()
db = Database()


@app.on_event("startup")
async def startup_event():
    """Initialize database on startup"""
    db.init_db()


@app.get("/")
async def root():
    """Health check endpoint"""
    return {
        "status": "online",
        "service": "Excel Analysis Platform",
        "version": "1.0.0",
        "timestamp": datetime.now().isoformat()
    }


@app.post("/api/upload")
async def upload_excel(file: UploadFile = File(...)):
    """
    Upload and process Excel file
    Returns basic file info and preview
    """
    try:
        # Validate file type
        if not file.filename.endswith(('.xlsx', '.xls')):
            raise HTTPException(status_code=400, detail="Only Excel files (.xlsx, .xls) are supported")
        
        # Read file content
        contents = await file.read()
        
        # Process Excel file
        file_info = excel_processor.process_file(contents, file.filename)
        
        # Store in database
        file_id = db.save_file_info(file_info)
        file_info['file_id'] = file_id
        
        return JSONResponse(content=file_info)
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error processing file: {str(e)}")


@app.post("/api/analyze/{file_id}")
async def analyze_file(file_id: int):
    """
    Perform AI analysis on uploaded file
    Returns risk assessment, progress metrics, and insights
    """
    try:
        # Get file data from database
        file_data = db.get_file_data(file_id)
        if not file_data:
            raise HTTPException(status_code=404, detail="File not found")
        
        # Perform AI analysis
        analysis_results = await ai_analyzer.analyze(file_data)
        
        # Save analysis results
        db.save_analysis(file_id, analysis_results)
        
        return JSONResponse(content=analysis_results)
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error analyzing file: {str(e)}")


@app.get("/api/reports/{file_id}")
async def get_reports(file_id: int):
    """
    Get analysis reports for a file
    """
    try:
        analysis = db.get_analysis(file_id)
        if not analysis:
            raise HTTPException(status_code=404, detail="Analysis not found")
        
        return JSONResponse(content=analysis)
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error retrieving reports: {str(e)}")


@app.get("/api/files")
async def list_files():
    """
    List all uploaded files
    """
    try:
        files = db.list_files()
        return JSONResponse(content={"files": files})
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error listing files: {str(e)}")


@app.delete("/api/files/{file_id}")
async def delete_file(file_id: int):
    """
    Delete a file and its analysis
    """
    try:
        db.delete_file(file_id)
        return JSONResponse(content={"status": "deleted", "file_id": file_id})
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error deleting file: {str(e)}")


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
