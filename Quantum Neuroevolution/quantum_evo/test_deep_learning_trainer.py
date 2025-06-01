"""Tests for the deep learning trainer module."""

import cupy as cp
from deep_learning_trainer import create_training_data, DeepBayesianNetwork

def test_create_training_data():
    """Test training data creation."""
    try:
        x_train, y_train = create_training_data(100)
        
        assert x_train.shape == (100, 20)
        assert y_train.shape == (100, 3)
        assert cp.all(x_train >= 0) and cp.all(x_train < 1000)
        
        print("✅ Training data creation test passed!")
        
    except Exception as e:
        print(f"⚠️ Training data creation test failed: {e}")
        assert False

def test_deep_bayesian_network():
    """Test Deep Bayesian Network creation and forward pass."""
    try:
        model_config = {
            'vocab_size': 100,
            'embedding_dim': 32,
            'hidden_dims': [64],
            'output_dim': 3,
            'dropout_rate': 0.2
        }
        
        model = DeepBayesianNetwork(**model_config)
        
        # Test forward pass
        test_input = cp.random.randint(0, 100, (5, 10))
        output = model.forward(test_input, training=False, num_samples=1)
        
        assert output.shape == (5, 3)
        
        print("✅ Deep Bayesian Network test passed!")
        
    except Exception as e:
        print(f"⚠️ Deep Bayesian Network test failed: {e}")
        assert False

if __name__ == "__main__":
    test_create_training_data()
    test_deep_bayesian_network()
    print("🎉 All deep learning trainer tests passed!")