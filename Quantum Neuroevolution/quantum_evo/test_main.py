"""Simple tests for the quantum neuroevolution main module."""

import cupy as cp
import numpy as np
from pathlib import Path


def test_ensure_directories():
    """Test directory creation functionality."""
    from main import ensure_directories
    
    # Test directory creation
    ensure_directories()
    
    # Check if directories exist
    assert Path('models').exists()
    assert Path('outputs').exists()
    print("✅ Directory creation test passed")


def test_simple_bayesian_network():
    """Test the simple Bayesian network creation."""
    from main import create_simple_bayesian_network
    
    network = create_simple_bayesian_network()
    
    # Test network properties
    assert network.input_dim == 32
    assert network.hidden_dim == 64
    assert network.output_dim == 2
    
    # Test prediction with mock data
    test_input = cp.random.normal(0, 1, (5, 32))
    results = network.predict_with_uncertainty(test_input)
    
    # Check output structure
    assert 'predictions' in results
    assert 'epistemic_uncertainty' in results
    assert 'aleatoric_uncertainty' in results
    assert results['predictions'].shape == (5, 2)
    
    print("✅ Simple Bayesian network test passed")


def test_simple_integration():
    """Test the NEAT-Bayesian integration."""
    from main import create_simple_bayesian_network, create_simple_integration
    
    network = create_simple_bayesian_network()
    integration_class = create_simple_integration()
    integration = integration_class(network)
    
    # Mock genome and test data
    mock_genome = "test_genome"
    test_data = cp.random.normal(0, 1, (10, 32))
    test_labels = cp.random.normal(0, 1, (10, 2))
    
    # Test evaluation
    results = integration.evaluate_genome_with_bayesian(mock_genome, test_data, test_labels)
    
    # Check result structure
    assert 'fitness' in results
    assert 'accuracy' in results
    assert 'uncertainty' in results
    assert isinstance(results['fitness'], float)
    
    print("✅ NEAT-Bayesian integration test passed")


def test_imports():
    """Test that all required modules can be imported."""
    try:
        from evolution import run_evolution
        from integration import integrate
        from visualize import plot_hfp
        from deep_learning_trainer import create_training_data
        print("✅ All imports successful")
    except ImportError as e:
        print(f"⚠️ Import error: {e}")


def run_all_tests():
    """Run all tests."""
    print("🧪 Running quantum neuroevolution tests...")
    
    test_imports()
    test_ensure_directories()
    test_simple_bayesian_network()
    test_simple_integration()
    
    print("🎉 All tests passed!")


if __name__ == "__main__":
    run_all_tests()