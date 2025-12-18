# Semantic Analysis Microservice

Python microservice providing semantic analysis using Sentence Transformers for the Excel Analysis Platform.

## Features

- **Semantic Clustering**: Automatically group similar comments into themes
- **Similarity Search**: Find related comments across organizations
- **Organization Profiling**: Analyze semantic patterns by organization
- **Theme Detection**: Identify common topics without predefined keywords

## Setup

### 1. Install Python Dependencies

```bash
cd semantic-service
pip install -r requirements.txt
```

### 2. Start the Service

```bash
python semantic_analyzer.py
```

The service will start on `http://localhost:5001`

### 3. Verify Service is Running

```bash
curl http://localhost:5001/health
```

Expected response:
```json
{
  "status": "healthy",
  "model": "all-mpnet-base-v2",
  "version": "1.0.0"
}
```

## API Endpoints

### POST /analyze
Main analysis endpoint for comprehensive semantic analysis.

**Request:**
```json
{
  "comments": ["text1", "text2", ...],
  "organizations": ["org1", "org2", ...]
}
```

**Response:**
```json
{
  "total_comments": 100,
  "total_organizations": 10,
  "themes": [
    {
      "theme_id": 0,
      "theme_name": "Staffing and Workforce",
      "comment_count": 25,
      "keywords": ["staff", "hiring", "retention"],
      "representative_comment": "...",
      "sample_comments": ["...", "...", "..."]
    }
  ],
  "organization_insights": {
    "Org Name": {
      "comment_count": 10,
      "coherence_score": 0.85,
      "top_keywords": ["keyword1", "keyword2"]
    }
  },
  "similar_comment_pairs": [...],
  "sentiment_distribution": {...}
}
```

### POST /cluster
Simple clustering endpoint.

**Request:**
```json
{
  "texts": ["text1", "text2", ...],
  "n_clusters": 5
}
```

### POST /similarity
Calculate similarity between query and candidates.

**Request:**
```json
{
  "query": "vaccine hesitancy",
  "candidates": ["text1", "text2", ...]
}
```

## Model Information

- **Model**: all-mpnet-base-v2
- **Size**: ~420MB
- **Embedding Dimension**: 768
- **Performance**: State-of-the-art semantic similarity

## Integration with C# Platform

The C# platform calls this service via HTTP:

```csharp
var response = await httpClient.PostAsJsonAsync(
    "http://localhost:5001/analyze",
    new { comments = allComments, organizations = allOrgs }
);
var result = await response.Content.ReadFromJsonAsync<SemanticAnalysisResult>();
```

## Troubleshooting

**Service won't start:**
- Ensure Python 3.8+ is installed
- Check all dependencies are installed: `pip list`
- Verify port 5001 is not in use

**Model download fails:**
- First run downloads ~420MB model from HuggingFace
- Ensure internet connection is available
- Model is cached in `~/.cache/torch/sentence_transformers/`

**Out of memory:**
- Reduce batch size in code
- Use smaller model: `all-MiniLM-L6-v2` (80MB)
- Process comments in smaller batches
