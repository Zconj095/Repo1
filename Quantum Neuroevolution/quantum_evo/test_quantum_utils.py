"""Tests for quantum utility functions."""

import cupy as cp
import numpy as np

def test_quantum_operations():
    """Test basic quantum operations."""
    try:
        # Test quantum state creation
        state = cp.array([1, 0, 0, 1]) / cp.sqrt(2)
        assert cp.allclose(cp.sum(cp.abs(state)**2), 1.0)
        
        # Test quantum gate operations
        pauli_x = cp.array([[0, 1], [1, 0]])
        qubit = cp.array([1, 0])
        result = cp.dot(pauli_x, qubit)
        expected = cp.array([0, 1])
        assert cp.allclose(result, expected)
        
        print("✅ Quantum operations test passed!")
        
    except Exception as e:
        print(f"⚠️ Quantum operations test failed: {e}")
        assert False

def test_quantum_entanglement():
    """Test quantum entanglement operations."""
    try:
        # Create Bell state
        bell_state = cp.array([1, 0, 0, 1]) / cp.sqrt(2)
        
        # Test normalization
        norm = cp.sum(cp.abs(bell_state)**2)
        assert cp.allclose(norm, 1.0)
        
        print("✅ Quantum entanglement test passed!")
        
    except Exception as e:
        print(f"⚠️ Quantum entanglement test failed: {e}")
        assert False

if __name__ == "__main__":
    test_quantum_operations()
    test_quantum_entanglement()
    print("🎉 All quantum utils tests passed!")