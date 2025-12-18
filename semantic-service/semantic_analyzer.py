"""
Semantic Analysis Microservice using Sentence Transformers
Provides clustering, similarity, and theme detection for text analysis
"""

from flask import Flask, request, jsonify
from flask_cors import CORS
from sentence_transformers import SentenceTransformer
from sklearn.cluster import KMeans, DBSCAN
from sklearn.metrics.pairwise import cosine_similarity
import numpy as np
from collections import Counter
import logging
import ssl
import os

# Disable SSL verification for model download (if needed in corporate environments)
os.environ['CURL_CA_BUNDLE'] = ''
os.environ['HF_HUB_DISABLE_SSL_VERIFY'] = '1'
os.environ['REQUESTS_CA_BUNDLE'] = ''
os.environ['SSL_CERT_FILE'] = ''
ssl._create_default_https_context = ssl._create_unverified_context

# Also disable SSL for requests library
import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# Initialize Flask app
app = Flask(__name__)
CORS(app)

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Load model (this happens once at startup)
logger.info("Loading Sentence Transformer model...")
try:
    model = SentenceTransformer('all-mpnet-base-v2')
    logger.info("Model loaded successfully!")
except Exception as e:
    logger.error(f"Failed to load model: {e}")
    logger.info("Attempting to load with trust_remote_code=True...")
    model = SentenceTransformer('all-mpnet-base-v2', trust_remote_code=True)
    logger.info("Model loaded successfully with trust_remote_code!")

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
    """
    Main analysis endpoint
    Expects: { "comments": ["text1", "text2", ...], "organizations": ["org1", "org2", ...] }
    Returns: Clustered themes, sentiment patterns, and organization insights
    """
    try:
        data = request.json
        comments = data.get('comments', [])
        organizations = data.get('organizations', [])
        
        if not comments:
            return jsonify({'error': 'No comments provided'}), 400
        
        logger.info(f"Analyzing {len(comments)} comments from {len(set(organizations))} organizations")
        
        # Generate embeddings
        embeddings = model.encode(comments, show_progress_bar=False)
        
        # Perform clustering to identify themes
        n_clusters = min(8, max(3, len(comments) // 10))  # Dynamic cluster count
        kmeans = KMeans(n_clusters=n_clusters, random_state=42, n_init=10)
        cluster_labels = kmeans.fit_predict(embeddings)
        
        # Extract themes from clusters
        themes = extract_themes(comments, cluster_labels, embeddings, kmeans.cluster_centers_)
        
        # Analyze by organization
        org_insights = analyze_by_organization(comments, organizations, embeddings)
        
        # Find similar comment pairs
        similar_pairs = find_similar_comments(comments, embeddings, threshold=0.7)
        
        # Calculate overall sentiment distribution (based on embedding patterns)
        sentiment_distribution = analyze_sentiment_patterns(embeddings)
        
        result = {
            'total_comments': len(comments),
            'total_organizations': len(set(organizations)),
            'themes': themes,
            'organization_insights': org_insights,
            'similar_comment_pairs': similar_pairs[:20],  # Top 20 most similar
            'sentiment_distribution': sentiment_distribution,
            'model_info': {
                'name': 'all-mpnet-base-v2',
                'type': 'Sentence Transformer',
                'embedding_dimension': embeddings.shape[1]
            }
        }
        
        logger.info(f"Analysis complete: {len(themes)} themes identified")
        return jsonify(result)
        
    except Exception as e:
        logger.error(f"Error during analysis: {str(e)}")
        return jsonify({'error': str(e)}), 500

@app.route('/cluster', methods=['POST'])
def cluster_texts():
    """
    Simple clustering endpoint
    Expects: { "texts": ["text1", "text2", ...], "n_clusters": 5 }
    Returns: Cluster assignments and centroids
    """
    try:
        data = request.json
        texts = data.get('texts', [])
        n_clusters = data.get('n_clusters', 5)
        
        if not texts:
            return jsonify({'error': 'No texts provided'}), 400
        
        # Generate embeddings
        embeddings = model.encode(texts, show_progress_bar=False)
        
        # Cluster
        kmeans = KMeans(n_clusters=n_clusters, random_state=42, n_init=10)
        labels = kmeans.fit_predict(embeddings)
        
        return jsonify({
            'cluster_labels': labels.tolist(),
            'n_clusters': n_clusters
        })
        
    except Exception as e:
        logger.error(f"Error during clustering: {str(e)}")
        return jsonify({'error': str(e)}), 500

@app.route('/similarity', methods=['POST'])
def calculate_similarity():
    """
    Calculate similarity between texts
    Expects: { "query": "text", "candidates": ["text1", "text2", ...] }
    Returns: Similarity scores
    """
    try:
        data = request.json
        query = data.get('query', '')
        candidates = data.get('candidates', [])
        
        if not query or not candidates:
            return jsonify({'error': 'Query and candidates required'}), 400
        
        # Generate embeddings
        query_embedding = model.encode([query], show_progress_bar=False)
        candidate_embeddings = model.encode(candidates, show_progress_bar=False)
        
        # Calculate similarities
        similarities = cosine_similarity(query_embedding, candidate_embeddings)[0]
        
        # Sort by similarity
        ranked_results = [
            {'text': candidates[i], 'similarity': float(similarities[i]), 'rank': i+1}
            for i in np.argsort(similarities)[::-1]
        ]
        
        return jsonify({
            'query': query,
            'results': ranked_results
        })
        
    except Exception as e:
        logger.error(f"Error calculating similarity: {str(e)}")
        return jsonify({'error': str(e)}), 500

def extract_themes(comments, cluster_labels, embeddings, centroids):
    """Extract meaningful themes from clusters"""
    themes = []
    
    for cluster_id in range(len(centroids)):
        # Get comments in this cluster
        cluster_comments = [comments[i] for i, label in enumerate(cluster_labels) if label == cluster_id]
        
        if not cluster_comments:
            continue
        
        # Find most representative comment (closest to centroid)
        cluster_embeddings = embeddings[cluster_labels == cluster_id]
        centroid = centroids[cluster_id]
        distances = np.linalg.norm(cluster_embeddings - centroid, axis=1)
        representative_idx = np.argmin(distances)
        representative_comment = cluster_comments[representative_idx]
        
        # Extract keywords (simple approach: most common words)
        keywords = extract_keywords(cluster_comments)
        
        # Generate theme name based on keywords
        theme_name = generate_theme_name(keywords)
        
        themes.append({
            'theme_id': int(cluster_id),
            'theme_name': theme_name,
            'comment_count': len(cluster_comments),
            'keywords': keywords[:10],
            'representative_comment': representative_comment[:200],
            'sample_comments': cluster_comments[:3]
        })
    
    # Sort by comment count
    themes.sort(key=lambda x: x['comment_count'], reverse=True)
    
    return themes

def extract_keywords(comments, top_n=10):
    """Extract top keywords from comments"""
    # Simple word frequency approach
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
    
    # Map common keywords to theme names
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
    
    # Check if any keywords match our mapping
    for keyword in keywords[:3]:
        for key, theme in theme_mapping.items():
            if key in keyword.lower():
                return theme
    
    # Default: use top keywords
    return ' & '.join(keywords[:2]).title()

def analyze_by_organization(comments, organizations, embeddings):
    """Analyze patterns by organization"""
    org_insights = {}
    
    for org in set(organizations):
        org_indices = [i for i, o in enumerate(organizations) if o == org]
        if not org_indices:
            continue
        
        org_embeddings = embeddings[org_indices]
        org_comments = [comments[i] for i in org_indices]
        
        # Calculate average embedding (organization's semantic profile)
        avg_embedding = np.mean(org_embeddings, axis=0)
        
        # Calculate internal coherence (how similar comments are to each other)
        if len(org_embeddings) > 1:
            similarities = cosine_similarity(org_embeddings)
            coherence = np.mean(similarities[np.triu_indices_from(similarities, k=1)])
        else:
            coherence = 1.0
        
        org_insights[org] = {
            'comment_count': len(org_comments),
            'coherence_score': float(coherence),
            'top_keywords': extract_keywords(org_comments, top_n=5)
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
                    'comment1': comments[i][:150],
                    'comment2': comments[j][:150],
                    'similarity': float(similarities[i][j])
                })
    
    # Sort by similarity
    similar_pairs.sort(key=lambda x: x['similarity'], reverse=True)
    
    return similar_pairs

def analyze_sentiment_patterns(embeddings):
    """
    Analyze sentiment patterns based on embedding distributions
    Note: This is a proxy - not true sentiment analysis
    """
    # Use PCA or clustering to identify positive/negative patterns
    # For now, use a simple heuristic based on embedding variance
    
    variances = np.var(embeddings, axis=0)
    mean_variance = np.mean(variances)
    
    # High variance might indicate diverse opinions (mixed sentiment)
    # Low variance might indicate consensus
    
    if mean_variance > 0.1:
        distribution = "Mixed/Diverse"
    elif mean_variance > 0.05:
        distribution = "Moderate Consensus"
    else:
        distribution = "Strong Consensus"
    
    return {
        'pattern': distribution,
        'variance': float(mean_variance),
        'note': 'Based on semantic diversity, not explicit sentiment scoring'
    }

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5001, debug=False)
