"""Bridge hyperdimensional embeddings to quantum circuits."""

import cupy as cp
from qiskit import QuantumCircuit


def embedding_to_circuit(embedding: cp.ndarray):
    """Encode a CuPy vector into a quantum circuit."""
    size = embedding.size
    circuit = QuantumCircuit(size)
    for i in range(size):
        angle = float(embedding[i] % (2 * cp.pi))
        circuit.ry(angle, i)
    return circuit
