"""
Download the Sentence Transformer model with SSL workaround
Run this once before starting the semantic service
"""

import ssl
import os
import certifi

# Try multiple approaches to fix SSL
print("Setting up SSL workaround...")

# Approach 1: Use certifi certificates
os.environ['REQUESTS_CA_BUNDLE'] = certifi.where()
os.environ['SSL_CERT_FILE'] = certifi.where()

# Approach 2: Disable SSL verification (less secure but works)
ssl._create_default_https_context = ssl._create_unverified_context

print("Downloading Sentence Transformer model...")
print("This may take a few minutes (model is ~420MB)...")

try:
    from sentence_transformers import SentenceTransformer
    
    # Download the model
    model = SentenceTransformer('all-mpnet-base-v2')
    
    print("\n✅ Model downloaded successfully!")
    print(f"Model cached at: {model._model_card_vars.get('model_path', 'default cache location')}")
    print("\nYou can now run: py semantic_analyzer.py")
    
except Exception as e:
    print(f"\n❌ Error downloading model: {e}")
    print("\nTrying alternative approach with smaller model...")
    
    try:
        # Try smaller model as fallback
        model = SentenceTransformer('all-MiniLM-L6-v2')
        print("\n✅ Smaller model (all-MiniLM-L6-v2) downloaded successfully!")
        print("Note: You'll need to update semantic_analyzer.py to use this model")
    except Exception as e2:
        print(f"\n❌ Alternative model also failed: {e2}")
        print("\nPlease check your internet connection and firewall settings.")
