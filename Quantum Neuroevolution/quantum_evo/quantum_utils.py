"""Quantum utility functions for neural network integration."""

import cupy as cp
from qiskit import QuantumCircuit, transpile
from qiskit_aer import AerSimulator
import numpy as np


def classical_to_quantum_data(classical_data):
    """Convert classical data to quantum state preparation."""
    # Normalize the data
    normalized_data = classical_data / cp.linalg.norm(classical_data)
    
    # Create quantum circuit
    n_qubits = int(cp.ceil(cp.log2(len(classical_data))))
    qc = QuantumCircuit(n_qubits, n_qubits)
    
    # Prepare quantum state (simplified amplitude encoding)
    for i, amplitude in enumerate(cp.asnumpy(normalized_data)):
        if i < 2**n_qubits:
            # Apply rotation gates to encode amplitudes
            angle = 2 * np.arcsin(np.sqrt(abs(amplitude)))
            qc.ry(angle, i % n_qubits)
    
    return qc


def quantum_to_classical_data(quantum_circuit):
    """Extract classical data from quantum measurement."""
    # Add measurement to all qubits
    qc = quantum_circuit.copy()
    qc.measure_all()
    
    # Use AerSimulator instead of deprecated Aer.get_backend()
    simulator = AerSimulator()
    
    # Transpile and run
    transpiled_qc = transpile(qc, simulator)
    job = simulator.run(transpiled_qc, shots=1024)
    result = job.result()
    counts = result.get_counts()
    
    # Convert measurement results to classical data
    classical_data = []
    total_shots = sum(counts.values())
    
    for bitstring, count in counts.items():
        # Remove spaces from bitstring and convert to int
        clean_bitstring = bitstring.replace(' ', '')
        try:
            # Convert bitstring to float and weight by count probability
            if clean_bitstring:  # Check if string is not empty
                value = int(clean_bitstring, 2) / (2**len(clean_bitstring)) * count / total_shots
                classical_data.append(value)
        except ValueError:
            # Handle any remaining parsing issues
            classical_data.append(0.0)
    
    # If no valid data, return a single zero
    if not classical_data:
        classical_data = [0.0]
    
    # Normalize to ensure consistent output size
    max_size = 4  # Adjust based on your needs
    if len(classical_data) < max_size:
        classical_data.extend([0.0] * (max_size - len(classical_data)))
    elif len(classical_data) > max_size:
        classical_data = classical_data[:max_size]
    
    return cp.array(classical_data)


def create_quantum_neural_layer(input_size, output_size):
    """Create a quantum neural network layer."""
    n_qubits = max(input_size, output_size)
    qc = QuantumCircuit(n_qubits, n_qubits)
    
    # Create entangling layer
    for i in range(n_qubits):
        qc.h(i)  # Hadamard gates for superposition
        
    for i in range(n_qubits - 1):
        qc.cx(i, i + 1)  # CNOT gates for entanglement
    
    # Parameterized rotation gates (these would be optimized during training)
    for i in range(n_qubits):
        qc.ry(np.pi/4, i)  # Placeholder parameter
        qc.rz(np.pi/4, i)  # Placeholder parameter
    
    return qc


def quantum_activation_function(x, activation_type='quantum_sigmoid'):
    """Quantum-inspired activation functions."""
    if activation_type == 'quantum_sigmoid':
        # Quantum sigmoid using probability amplitudes
        return 1 / (1 + cp.exp(-x * cp.pi))
    
    elif activation_type == 'phase_shift':
        # Phase-based activation
        return cp.cos(x) + 1j * cp.sin(x)
    
    elif activation_type == 'amplitude_modulation':
        # Amplitude modulation
        return cp.abs(x) * cp.exp(1j * cp.angle(x))
    
    else:
        return cp.tanh(x)  # Fallback to classical


def quantum_entanglement_aggregation(inputs):
    """Aggregate inputs using quantum entanglement principles."""
    try:
        # Ensure we're working with the right shape
        original_shape = inputs.shape
        
        # Handle different input shapes
        if inputs.ndim == 0:
            # Scalar input
            return inputs
            
        elif inputs.ndim == 1:
            # 1D input: direct processing
            n = len(inputs)
            if n == 0:
                return cp.array(0.0)
            
            # Simple quantum-inspired aggregation for 1D
            indices = cp.arange(n)
            quantum_weights = cp.exp(-indices / n)
            quantum_weights = quantum_weights / cp.sum(quantum_weights)
            result = cp.sum(quantum_weights * inputs)
            
        elif inputs.ndim == 2:
            # 2D input: apply aggregation along the last dimension
            batch_size, feature_size = inputs.shape
            results = cp.zeros(batch_size)
            
            # Create quantum weights for features
            if feature_size > 0:
                indices = cp.arange(feature_size)
                quantum_weights = cp.exp(-indices / feature_size)
                quantum_weights = quantum_weights / cp.sum(quantum_weights)
                
                # Apply weights across features for each sample
                for i in range(batch_size):
                    results[i] = cp.sum(quantum_weights * inputs[i])
            
            result = results
            
        else:
            # Higher dimensional: reshape to 2D and process
            if inputs.ndim > 2:
                # Reshape to (batch, features)
                batch_size = inputs.shape[0]
                feature_size = cp.prod(cp.array(inputs.shape[1:]))
                reshaped = inputs.reshape(batch_size, int(feature_size))
                result = quantum_entanglement_aggregation(reshaped)
            else:
                # Fallback to simple aggregation
                result = cp.mean(inputs, axis=-1)
        
        return result
        
    except Exception as e:
        print(f"Error in quantum_entanglement_aggregation: {e}")
        print(f"Input shape: {inputs.shape}")
        # Fallback to simple mean aggregation
        if inputs.ndim == 1:
            return cp.mean(inputs) if len(inputs) > 0 else cp.array(0.0)
        elif inputs.ndim == 2:
            return cp.mean(inputs, axis=1)
        else:
            return cp.mean(inputs, axis=tuple(range(1, inputs.ndim)))


def prepare_quantum_superposition(classical_inputs):
    """Prepare quantum superposition state from classical inputs."""
    try:
        # Handle different input shapes
        if classical_inputs.ndim > 1:
            # Flatten multi-dimensional inputs
            classical_inputs = classical_inputs.flatten()
        
        # Check for empty or zero inputs
        if len(classical_inputs) == 0:
            return cp.array([1.0 + 0j])
        
        # Normalize inputs to probability amplitudes
        norm = cp.linalg.norm(classical_inputs)
        if norm == 0:
            # Handle zero vector
            amplitudes = cp.ones(len(classical_inputs)) / cp.sqrt(len(classical_inputs))
        else:
            amplitudes = classical_inputs / norm
        
        # Create superposition coefficients
        n_qubits = int(cp.ceil(cp.log2(max(len(amplitudes), 1))))
        padded_amplitudes = cp.zeros(2**n_qubits, dtype=complex)
        padded_amplitudes[:len(amplitudes)] = amplitudes
        
        # Normalize to unit vector
        norm = cp.linalg.norm(padded_amplitudes)
        if norm > 0:
            padded_amplitudes = padded_amplitudes / norm
        else:
            # Fallback to equal superposition
            padded_amplitudes[:] = 1.0 / cp.sqrt(len(padded_amplitudes))
        
        return padded_amplitudes
        
    except Exception as e:
        print(f"Error in prepare_quantum_superposition: {e}")
        # Return simple superposition state
        return cp.array([1.0 + 0j])


def measure_quantum_state(quantum_amplitudes):
    """Measure quantum state and return classical probabilities."""
    try:
        # Calculate measurement probabilities
        probabilities = cp.abs(quantum_amplitudes)**2
        
        # Normalize probabilities
        prob_sum = cp.sum(probabilities)
        if prob_sum > 0:
            probabilities = probabilities / prob_sum
        else:
            # Equal probabilities if all are zero
            probabilities = cp.ones_like(probabilities) / len(probabilities)
        
        # Simulate quantum measurement collapse
        # (In real implementation, this would involve actual quantum hardware)
        probabilities_np = cp.asnumpy(probabilities)
        measured_state = cp.random.choice(
            len(probabilities), 
            p=probabilities_np
        )
        
        # Return measurement result as classical data
        result = cp.zeros_like(probabilities, dtype=float)
        result[measured_state] = 1.0
        
        return result
        
    except Exception as e:
        print(f"Error in measure_quantum_state: {e}")
        # Return uniform measurement
        return cp.ones(len(quantum_amplitudes)) / len(quantum_amplitudes)


def quantum_fourier_transform(inputs):
    """Apply quantum Fourier transform to classical inputs."""
    try:
        # Ensure input is 1D
        if inputs.ndim > 1:
            inputs = inputs.flatten()
        
        # Apply discrete Fourier transform (classical approximation of QFT)
        n = len(inputs)
        if n == 0:
            return cp.array([])
        
        # Pad to power of 2 for efficiency
        n_padded = 2**int(cp.ceil(cp.log2(n)))
        padded_inputs = cp.zeros(n_padded, dtype=complex)
        padded_inputs[:n] = inputs
        
        # Apply FFT (quantum Fourier transform approximation)
        qft_result = cp.fft.fft(padded_inputs)
        
        # Normalize
        qft_result = qft_result / cp.sqrt(n_padded)
        
        # Return real part (measurement)
        return cp.real(qft_result[:n])
        
    except Exception as e:
        print(f"Error in quantum_fourier_transform: {e}")
        return inputs


def quantum_phase_estimation(inputs, phase_params=None):
    """Estimate quantum phases from classical inputs."""
    try:
        if phase_params is None:
            # Default phase parameters
            phase_params = cp.linspace(0, 2*cp.pi, len(inputs))
        
        # Create quantum phase encoding
        phases = cp.exp(1j * phase_params * inputs)
        
        # Extract phase information
        estimated_phases = cp.angle(phases)
        
        # Normalize to [0, 1] range
        normalized_phases = (estimated_phases + cp.pi) / (2 * cp.pi)
        
        return normalized_phases
        
    except Exception as e:
        print(f"Error in quantum_phase_estimation: {e}")
        return inputs


def quantum_error_correction(inputs, error_rate=0.01):
    """Apply quantum error correction principles to classical data."""
    try:
        # Simulate quantum error correction through redundancy
        n = len(inputs) if inputs.ndim == 1 else inputs.shape[-1]
        
        # Add redundancy (3-qubit repetition code simulation)
        redundant_data = cp.tile(inputs, 3) if inputs.ndim == 1 else cp.tile(inputs, (1, 3))
        
        # Simulate errors
        error_mask = cp.random.random(redundant_data.shape) < error_rate
        noisy_data = redundant_data.copy()
        noisy_data[error_mask] *= -1  # Bit flip errors
        
        # Error correction through majority voting
        if inputs.ndim == 1:
            corrected = cp.zeros_like(inputs)
            for i in range(n):
                votes = noisy_data[i::n][:3]  # Get 3 redundant copies
                corrected[i] = cp.sign(cp.sum(votes)) * cp.mean(cp.abs(votes))
        else:
            batch_size = inputs.shape[0]
            corrected = cp.zeros_like(inputs)
            for i in range(n):
                for b in range(batch_size):
                    votes = noisy_data[b, i::n][:3]
                    corrected[b, i] = cp.sign(cp.sum(votes)) * cp.mean(cp.abs(votes))
        
        return corrected
        
    except Exception as e:
        print(f"Error in quantum_error_correction: {e}")
        return inputs
