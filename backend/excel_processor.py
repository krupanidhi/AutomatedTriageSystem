"""
Excel file processing module
"""
import pandas as pd
import io
from typing import Dict, Any, List
from datetime import datetime


class ExcelProcessor:
    """Handles Excel file parsing and data extraction"""
    
    def process_file(self, file_contents: bytes, filename: str) -> Dict[str, Any]:
        """
        Process uploaded Excel file and extract structured data
        
        Args:
            file_contents: Raw file bytes
            filename: Original filename
            
        Returns:
            Dictionary with file info and extracted data
        """
        # Read Excel file
        excel_file = pd.ExcelFile(io.BytesIO(file_contents))
        
        # Get sheet names
        sheet_names = excel_file.sheet_names
        
        # Process first sheet (can be extended to handle multiple sheets)
        df = pd.read_excel(excel_file, sheet_name=0)
        
        # Clean data
        df = self._clean_dataframe(df)
        
        # Extract metadata
        file_info = {
            "filename": filename,
            "upload_time": datetime.now().isoformat(),
            "sheet_names": sheet_names,
            "total_rows": len(df),
            "total_columns": len(df.columns),
            "columns": df.columns.tolist(),
            "preview": df.head(10).to_dict(orient='records'),
            "data": df.to_dict(orient='records'),
            "summary": self._generate_summary(df)
        }
        
        return file_info
    
    def _clean_dataframe(self, df: pd.DataFrame) -> pd.DataFrame:
        """Clean and standardize dataframe"""
        # Remove completely empty rows
        df = df.dropna(how='all')
        
        # Remove completely empty columns
        df = df.dropna(axis=1, how='all')
        
        # Fill NaN values with empty strings for text columns
        for col in df.columns:
            if df[col].dtype == 'object':
                df[col] = df[col].fillna('')
        
        # Strip whitespace from string columns
        for col in df.select_dtypes(include=['object']).columns:
            df[col] = df[col].astype(str).str.strip()
        
        return df
    
    def _generate_summary(self, df: pd.DataFrame) -> Dict[str, Any]:
        """Generate summary statistics from dataframe"""
        summary = {
            "total_records": len(df),
            "columns_info": []
        }
        
        for col in df.columns:
            col_info = {
                "name": col,
                "type": str(df[col].dtype),
                "non_null_count": int(df[col].notna().sum()),
                "null_count": int(df[col].isna().sum()),
                "unique_values": int(df[col].nunique())
            }
            
            # Add sample values for text columns
            if df[col].dtype == 'object':
                col_info["sample_values"] = df[col].dropna().head(3).tolist()
            
            # Check for yes/no columns
            unique_vals = df[col].dropna().unique()
            if len(unique_vals) <= 10:
                col_info["unique_list"] = [str(v) for v in unique_vals]
                
                # Detect yes/no questions
                yes_no_values = {'yes', 'no', 'y', 'n', 'true', 'false', '1', '0'}
                if set(str(v).lower() for v in unique_vals).issubset(yes_no_values):
                    col_info["is_yes_no"] = True
                    col_info["yes_count"] = int(df[col].astype(str).str.lower().isin(['yes', 'y', 'true', '1']).sum())
                    col_info["no_count"] = int(df[col].astype(str).str.lower().isin(['no', 'n', 'false', '0']).sum())
            
            summary["columns_info"].append(col_info)
        
        # Identify comment columns (text with longer content)
        comment_columns = []
        for col in df.columns:
            if df[col].dtype == 'object':
                avg_length = df[col].astype(str).str.len().mean()
                if avg_length > 20:  # Likely a comment field
                    comment_columns.append(col)
        
        summary["comment_columns"] = comment_columns
        summary["has_comments"] = len(comment_columns) > 0
        
        return summary
    
    def extract_comments_and_questions(self, data: List[Dict]) -> Dict[str, List]:
        """
        Extract comments and yes/no questions from processed data
        
        Args:
            data: List of row dictionaries
            
        Returns:
            Dictionary with separated comments and questions
        """
        comments = []
        questions = []
        
        for row in data:
            for key, value in row.items():
                # Check if it's a yes/no answer
                if isinstance(value, str):
                    val_lower = value.lower().strip()
                    if val_lower in ['yes', 'no', 'y', 'n', 'true', 'false']:
                        questions.append({
                            "question": key,
                            "answer": value,
                            "row_data": row
                        })
                    # Check if it's a comment (longer text)
                    elif len(value) > 20:
                        comments.append({
                            "field": key,
                            "comment": value,
                            "row_data": row
                        })
        
        return {
            "comments": comments,
            "questions": questions
        }
