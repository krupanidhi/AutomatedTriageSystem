"""
Semantic Analysis Microservice using Sentence Transformers
Modified version that bypasses SSL verification completely
"""

import os
import ssl
import warnings

# Set all SSL bypass environment variables BEFORE any imports
os.environ['CURL_CA_BUNDLE'] = ''
os.environ['HF_HUB_DISABLE_SSL_VERIFY'] = '1'
os.environ['REQUESTS_CA_BUNDLE'] = ''
os.environ['SSL_CERT_FILE'] = ''
os.environ['PYTHONHTTPSVERIFY'] = '0'

# Monkey patch SSL
ssl._create_default_https_context = ssl._create_unverified_context

# Suppress warnings
warnings.filterwarnings('ignore')

# Now import everything else
from flask import Flask, request, jsonify
from flask_cors import CORS
import numpy as np
from collections import Counter
import logging

# Disable urllib3 warnings
import urllib3
urllib3.disable_warnings()

# Monkey patch requests to disable SSL verification
import requests
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry

original_request = requests.Session.request
def patched_request(self, method, url, **kwargs):
    kwargs['verify'] = False
    return original_request(self, method, url, **kwargs)
requests.Session.request = patched_request

# Now import sentence transformers
from sentence_transformers import SentenceTransformer
from sklearn.cluster import KMeans
from sklearn.metrics.pairwise import cosine_similarity

# Initialize Flask app
app = Flask(__name__)
CORS(app)

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Load model (this happens once at startup)
logger.info("Loading Sentence Transformer model with SSL bypass...")
try:
    # Try to load from cache first
    import torch
    cache_folder = os.path.expanduser('~/.cache/huggingface/hub')
    logger.info(f"Checking cache folder: {cache_folder}")
    
    model = SentenceTransformer('all-mpnet-base-v2', cache_folder=cache_folder)
    logger.info("✅ Model loaded successfully!")
except Exception as e:
    logger.error(f"❌ Failed to load model: {e}")
    logger.info("Attempting with smaller model as fallback...")
    try:
        model = SentenceTransformer('all-MiniLM-L6-v2')
        logger.info("✅ Fallback model loaded successfully!")
    except Exception as e2:
        logger.error(f"❌ Fallback also failed: {e2}")
        logger.error("Cannot start service without a model. Please download model manually.")
        exit(1)

@app.route('/health', methods=['GET'])
def health_check():
    """Health check endpoint"""
    return jsonify({
        'status': 'healthy',
        'model': 'all-mpnet-base-v2',
        'version': '1.0.0'
    })

@app.route('/analyze', methods=['POST'])
def analyze_comments():
    """Main analysis endpoint"""
    try:
        data = request.json
        comments = data.get('comments', [])
        organizations = data.get('organizations', [])
        
        if not comments:
            return jsonify({'error': 'No comments provided'}), 400
        
        logger.info(f"Analyzing {len(comments)} comments from {len(set(organizations))} organizations")
        
        # Generate embeddings
        embeddings = model.encode(comments, show_progress_bar=False)
        
        # Perform clustering
        n_clusters = min(8, max(3, len(comments) // 10))
        kmeans = KMeans(n_clusters=n_clusters, random_state=42, n_init=10)
        cluster_labels = kmeans.fit_predict(embeddings)
        
        # Extract themes
        themes = extract_themes(comments, cluster_labels, embeddings, kmeans.cluster_centers_)
        
        # Analyze by organization
        org_insights = analyze_by_organization(comments, organizations, embeddings)
        
        # Find similar comments
        similar_pairs = find_similar_comments(comments, embeddings, threshold=0.7)
        
        # Sentiment distribution
        sentiment_distribution = analyze_sentiment_patterns(embeddings)
        
        result = {
            'TotalComments': len(comments),
            'TotalOrganizations': len(set(organizations)),
            'Themes': themes,
            'OrganizationInsights': org_insights,
            'SimilarCommentPairs': similar_pairs[:20],
            'SentimentDistribution': sentiment_distribution,
            'ModelInfo': {
                'Name': 'all-mpnet-base-v2',
                'Type': 'Sentence Transformer',
                'EmbeddingDimension': embeddings.shape[1]
            }
        }
        
        logger.info(f"Analysis complete: {len(themes)} themes identified")
        return jsonify(result)
        
    except Exception as e:
        logger.error(f"Error during analysis: {str(e)}")
        return jsonify({'error': str(e)}), 500

def extract_themes(comments, cluster_labels, embeddings, centroids):
    """Extract meaningful themes from clusters"""
    themes = []
    
    for cluster_id in range(len(centroids)):
        cluster_comments = [comments[i] for i, label in enumerate(cluster_labels) if label == cluster_id]
        
        if not cluster_comments:
            continue
        
        cluster_embeddings = embeddings[cluster_labels == cluster_id]
        centroid = centroids[cluster_id]
        distances = np.linalg.norm(cluster_embeddings - centroid, axis=1)
        representative_idx = np.argmin(distances)
        representative_comment = cluster_comments[representative_idx]
        
        keywords = extract_keywords(cluster_comments)
        theme_name = generate_theme_name(keywords)
        
        themes.append({
            'ThemeId': int(cluster_id),
            'ThemeName': theme_name,
            'CommentCount': len(cluster_comments),
            'Keywords': keywords[:10],
            'RepresentativeComment': representative_comment[:200],
            'SampleComments': cluster_comments[:3]
        })
    
    themes.sort(key=lambda x: x['CommentCount'], reverse=True)
    return themes

def extract_keywords(comments, top_n=10):
    """Extract top keywords from comments"""
    words = []
    stopwords = {'the', 'a', 'an', 'and', 'or', 'but', 'in', 'on', 'at', 'to', 'for', 
                 'of', 'with', 'by', 'from', 'as', 'is', 'was', 'are', 'were', 'be',
                 'have', 'has', 'had', 'do', 'does', 'did', 'will', 'would', 'could',
                 'should', 'may', 'might', 'must', 'can', 'this', 'that', 'these', 'those'}
    
    for comment in comments:
        words.extend([
            word.lower().strip('.,!?;:') 
            for word in comment.split() 
            if len(word) > 3 and word.lower() not in stopwords
        ])
    
    word_counts = Counter(words)
    return [word for word, count in word_counts.most_common(top_n)]

def generate_theme_name(keywords):
    """Generate a theme name from keywords"""
    if not keywords:
        return "General Comments"
    
    theme_mapping = {
        'staff': 'Staffing and Workforce',
        'fund': 'Funding and Resources',
        'train': 'Training and Development',
        'vaccine': 'Vaccination Programs',
        'covid': 'COVID-19 Response',
        'patient': 'Patient Care',
        'health': 'Health Services',
        'program': 'Program Implementation',
        'community': 'Community Engagement',
        'technology': 'Technology and Systems',
        'capacity': 'Capacity Building',
        'partnership': 'Partnerships and Collaboration'
    }
    
    for keyword in keywords[:3]:
        for key, theme in theme_mapping.items():
            if key in keyword.lower():
                return theme
    
    return ' & '.join(keywords[:2]).title()

def analyze_by_organization(comments, organizations, embeddings):
    """Analyze patterns by organization"""
    org_insights = {}
    
    for org in set(organizations):
        # Skip empty or whitespace-only organization names
        if not org or not org.strip():
            continue
            
        org_indices = [i for i, o in enumerate(organizations) if o == org]
        if not org_indices:
            continue
        
        org_embeddings = embeddings[org_indices]
        org_comments = [comments[i] for i in org_indices]
        
        if len(org_embeddings) > 1:
            similarities = cosine_similarity(org_embeddings)
            coherence = np.mean(similarities[np.triu_indices_from(similarities, k=1)])
        else:
            coherence = 1.0
        
        org_insights[org] = {
            'CommentCount': len(org_comments),
            'CoherenceScore': float(coherence),
            'TopKeywords': extract_keywords(org_comments, top_n=5)
        }
    
    return org_insights

def find_similar_comments(comments, embeddings, threshold=0.7):
    """Find pairs of similar comments"""
    similarities = cosine_similarity(embeddings)
    similar_pairs = []
    
    for i in range(len(comments)):
        for j in range(i + 1, len(comments)):
            if similarities[i][j] >= threshold:
                similar_pairs.append({
                    'Comment1': comments[i][:150],
                    'Comment2': comments[j][:150],
                    'Similarity': float(similarities[i][j])
                })
    
    similar_pairs.sort(key=lambda x: x['Similarity'], reverse=True)
    return similar_pairs

def analyze_sentiment_patterns(embeddings):
    """Analyze sentiment patterns based on embedding distributions"""
    variances = np.var(embeddings, axis=0)
    mean_variance = np.mean(variances)
    
    if mean_variance > 0.1:
        distribution = "Mixed/Diverse"
    elif mean_variance > 0.05:
        distribution = "Moderate Consensus"
    else:
        distribution = "Strong Consensus"
    
    return {
        'Pattern': distribution,
        'Variance': float(mean_variance),
        'Note': 'Based on semantic diversity, not explicit sentiment scoring'
    }

if __name__ == '__main__':
    logger.info("🚀 Starting Semantic Analysis Service on port 5001...")
    app.run(host='0.0.0.0', port=5001, debug=False)
