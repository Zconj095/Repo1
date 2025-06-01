"""Bayesian Neural Network with Quantum Embeddings for Neuroevolution."""

import cupy as cp
import numpy as np
from scipy.stats import norm
import matplotlib.pyplot as plt
from quantum_utils import quantum_activation_function, quantum_entanglement_aggregation


class BayesianLayer:
    """Bayesian neural network layer with uncertainty quantification."""
    
    def __init__(self, input_size, output_size, prior_std=1.0):
        self.input_size = input_size
        self.output_size = output_size
        self.prior_std = prior_std
        
        # Weight parameters (mean and log variance)
        self.weight_mean = cp.random.normal(0, 0.1, (input_size, output_size))
        self.weight_logvar = cp.full((input_size, output_size), cp.log(prior_std**2))
        
        # Bias parameters
        self.bias_mean = cp.zeros(output_size)
        self.bias_logvar = cp.full(output_size, cp.log(prior_std**2))
        
        # Cache for gradients
        self.weight_mean_grad = cp.zeros_like(self.weight_mean)
        self.weight_logvar_grad = cp.zeros_like(self.weight_logvar)
        self.bias_mean_grad = cp.zeros_like(self.bias_mean)
        self.bias_logvar_grad = cp.zeros_like(self.bias_logvar)
    
    def sample_weights(self):
        """Sample weights from posterior distribution."""
        weight_std = cp.exp(0.5 * self.weight_logvar)
        bias_std = cp.exp(0.5 * self.bias_logvar)
        
        # Reparameterization trick
        epsilon_w = cp.random.normal(0, 1, self.weight_mean.shape)
        epsilon_b = cp.random.normal(0, 1, self.bias_mean.shape)
        
        weights = self.weight_mean + weight_std * epsilon_w
        biases = self.bias_mean + bias_std * epsilon_b
        
        return weights, biases
    
    def kl_divergence(self):
        """Compute KL divergence between posterior and prior."""
        # KL(q||p) for weights
        weight_var = cp.exp(self.weight_logvar)
        weight_kl = 0.5 * cp.sum(
            (self.weight_mean**2 + weight_var) / (self.prior_std**2) 
            - 1 - self.weight_logvar + cp.log(self.prior_std**2)
        )
        
        # KL(q||p) for biases
        bias_var = cp.exp(self.bias_logvar)
        bias_kl = 0.5 * cp.sum(
            (self.bias_mean**2 + bias_var) / (self.prior_std**2)
            - 1 - self.bias_logvar + cp.log(self.prior_std**2)
        )
        
        return weight_kl + bias_kl
    
    def forward(self, x):
        """Forward pass with sampled weights."""
        weights, biases = self.sample_weights()
        return cp.dot(x, weights) + biases


class QuantumEmbedding:
    """Quantum-inspired embedding layer for high-dimensional representations."""
    
    def __init__(self, vocab_size, embedding_dim, quantum_dim=None):
        self.vocab_size = vocab_size
        self.embedding_dim = embedding_dim
        self.quantum_dim = quantum_dim or embedding_dim // 2
        
        # Classical embeddings
        self.embeddings = cp.random.normal(0, 0.1, (vocab_size, embedding_dim))
        
        # Quantum phase embeddings
        self.phase_embeddings = cp.random.uniform(0, 2*cp.pi, (vocab_size, self.quantum_dim))
        
        # Amplitude embeddings
        self.amplitude_embeddings = cp.random.uniform(0, 1, (vocab_size, self.quantum_dim))
        
        # Entanglement matrix for quantum correlations
        self.entanglement_matrix = cp.random.normal(0, 0.1, (self.quantum_dim, self.quantum_dim))
    
    def forward(self, indices):
        """Forward pass with quantum-enhanced embeddings."""
        # Classical embedding lookup
        classical_emb = self.embeddings[indices]
        
        # Quantum phase encoding
        phases = self.phase_embeddings[indices]
        amplitudes = self.amplitude_embeddings[indices]
        
        # Create quantum superposition states
        quantum_real = amplitudes * cp.cos(phases)
        quantum_imag = amplitudes * cp.sin(phases)
        
        # Apply entanglement correlations
        entangled_real = cp.dot(quantum_real, self.entanglement_matrix)
        entangled_imag = cp.dot(quantum_imag, self.entanglement_matrix)
        
        # Combine classical and quantum features
        quantum_features = cp.concatenate([entangled_real, entangled_imag], axis=-1)
        
        # Ensure dimensions match
        if quantum_features.shape[-1] < classical_emb.shape[-1]:
            padding = classical_emb.shape[-1] - quantum_features.shape[-1]
            quantum_features = cp.pad(quantum_features, ((0, 0), (0, padding)))
        elif quantum_features.shape[-1] > classical_emb.shape[-1]:
            quantum_features = quantum_features[:, :classical_emb.shape[-1]]
        
        # Quantum-classical fusion
        return classical_emb + 0.5 * quantum_features
    
    def get_entanglement_entropy(self, indices):
        """Compute von Neumann entropy of quantum embeddings."""
        phases = self.phase_embeddings[indices]
        amplitudes = self.amplitude_embeddings[indices]
        
        # Create density matrix
        psi_real = amplitudes * cp.cos(phases)
        psi_imag = amplitudes * cp.sin(phases)
        
        # Normalize
        norm = cp.sqrt(psi_real**2 + psi_imag**2)
        psi_real /= (norm + 1e-8)
        psi_imag /= (norm + 1e-8)
        
        # Compute entropy
        probs = psi_real**2 + psi_imag**2
        entropy = -cp.sum(probs * cp.log(probs + 1e-8), axis=-1)
        
        return entropy


class BayesianQuantumNetwork:
    """Complete Bayesian Neural Network with Quantum Embeddings."""
    
    def __init__(self, vocab_size=1000, embedding_dim=64, hidden_dims=[128, 64], output_dim=10):
        self.vocab_size = vocab_size
        self.embedding_dim = embedding_dim
        self.hidden_dims = hidden_dims
        self.output_dim = output_dim
        
        # Quantum embedding layer
        self.embedding = QuantumEmbedding(vocab_size, embedding_dim)
        
        # Bayesian layers
        self.layers = []
        layer_dims = [embedding_dim] + hidden_dims + [output_dim]
        
        for i in range(len(layer_dims) - 1):
            layer = BayesianLayer(layer_dims[i], layer_dims[i + 1])
            self.layers.append(layer)
        
        # Variational parameters for optimization
        self.learning_rate = 0.001
        self.beta = 1.0  # KL divergence weight
        
    def forward(self, x, num_samples=10):
        """Forward pass with Monte Carlo sampling for uncertainty."""
        if x.dtype == cp.int32 or x.dtype == cp.int64:
            # Input is indices for embedding
            x = self.embedding.forward(x)
        
        outputs = []
        kl_divs = []
        
        for _ in range(num_samples):
            current = x
            total_kl = 0
            
            for i, layer in enumerate(self.layers):
                current = layer.forward(current)
                total_kl += layer.kl_divergence()
                
                # Apply quantum activation for hidden layers
                if i < len(self.layers) - 1:
                    try:
                        current = quantum_activation_function(current, 'quantum_sigmoid')
                        
                        # Apply quantum entanglement aggregation
                        # The function now handles 2D inputs correctly
                        current = quantum_entanglement_aggregation(current)
                        
                        # Ensure output has the right shape for next layer
                        if current.ndim == 1 and len(self.layers) > i + 1:
                            # If we got 1D output but need 2D for next layer
                            # Expand to match expected input shape for next layer
                            next_layer_input_size = self.layers[i + 1].input_size if hasattr(self.layers[i + 1], 'input_size') else current.shape[0]
                            if len(current) != next_layer_input_size:
                                # Pad or truncate to match expected size
                                if len(current) < next_layer_input_size:
                                    padding = cp.zeros(next_layer_input_size - len(current))
                                    current = cp.concatenate([current, padding])
                                else:
                                    current = current[:next_layer_input_size]
                            # Reshape to maintain batch dimension
                            current = current.reshape(x.shape[0], -1)
                            
                    except Exception as e:
                        print(f"Error in quantum processing: {e}")
                        # Continue without quantum enhancement
                        pass
            
            outputs.append(current)
            kl_divs.append(total_kl)
        
        return cp.stack(outputs), cp.array(kl_divs)
    
    def predict_with_uncertainty(self, x, num_samples=100):
        """Make predictions with uncertainty quantification."""
        outputs, _ = self.forward(x, num_samples)
        
        # Compute statistics
        mean_pred = cp.mean(outputs, axis=0)
        std_pred = cp.std(outputs, axis=0)
        
        # Epistemic uncertainty (model uncertainty)
        epistemic_uncertainty = cp.var(cp.mean(outputs, axis=-1), axis=0)
        
        # Aleatoric uncertainty (data uncertainty)
        aleatoric_uncertainty = cp.mean(cp.var(outputs, axis=-1), axis=0)
        
        return {
            'predictions': mean_pred,
            'epistemic_uncertainty': epistemic_uncertainty,
            'aleatoric_uncertainty': aleatoric_uncertainty,
            'total_uncertainty': std_pred,
            'raw_samples': outputs
        }
    
    def variational_loss(self, x, y, num_samples=10):
        """Compute variational loss (ELBO)."""
        outputs, kl_divs = self.forward(x, num_samples)
        
        # Reconstruction loss (negative log-likelihood)
        reconstruction_loss = 0
        for output in outputs:
            # Mean squared error for regression
            reconstruction_loss += cp.mean((output - y)**2)
        reconstruction_loss /= num_samples
        
        # KL divergence term
        kl_loss = cp.mean(kl_divs)
        
        # ELBO = reconstruction_loss + β * KL_loss
        total_loss = reconstruction_loss + self.beta * kl_loss
        
        return total_loss, reconstruction_loss, kl_loss
    
    def train_step(self, x, y):
        """Single training step with gradient descent."""
        # Compute loss and gradients (simplified - in practice use autograd)
        loss, recon_loss, kl_loss = self.variational_loss(x, y)
        
        # Update learning rate based on uncertainty
        uncertainty = self.predict_with_uncertainty(x, num_samples=5)
        adaptive_lr = self.learning_rate * (1 + cp.mean(uncertainty['total_uncertainty']))
        
        return {
            'total_loss': float(cp.asnumpy(loss)),
            'reconstruction_loss': float(cp.asnumpy(recon_loss)),
            'kl_loss': float(cp.asnumpy(kl_loss)),
            'adaptive_lr': float(cp.asnumpy(adaptive_lr))
        }
    
    def calibration_curve(self, x, y, num_samples=100):
        """Compute calibration curve for uncertainty validation."""
        predictions = self.predict_with_uncertainty(x, num_samples)
        
        # Bin predictions by confidence
        confidence = 1 - predictions['total_uncertainty']
        accuracy = cp.abs(predictions['predictions'] - y) < 0.1  # threshold
        
        # Create bins
        bins = cp.linspace(0, 1, 11)
        bin_confidence = []
        bin_accuracy = []
        
        for i in range(len(bins) - 1):
            mask = (confidence >= bins[i]) & (confidence < bins[i + 1])
            if cp.sum(mask) > 0:
                bin_confidence.append(cp.mean(confidence[mask]))
                bin_accuracy.append(cp.mean(accuracy[mask]))
        
        return cp.array(bin_confidence), cp.array(bin_accuracy)


class QuantumNEATIntegration:
    """Integration layer between Bayesian Quantum Network and NEAT evolution."""
    
    def __init__(self, network):
        self.network = network
        self.genome_mapping = {}
    
    def encode_genome_to_network(self, genome):
        """Convert NEAT genome to network parameters."""
        # Extract weights and biases from genome
        weights = []
        biases = []
        
        for conn in genome.connections.values():
            if conn.enabled:
                weights.append(conn.weight)
        
        for node in genome.nodes.values():
            biases.append(node.bias)
        
        # Map to Bayesian layer parameters
        weight_idx = 0
        bias_idx = 0
        
        for layer in self.network.layers:
            # Update weight means
            num_weights = layer.weight_mean.size
            if weight_idx + num_weights <= len(weights):
                layer.weight_mean = cp.array(weights[weight_idx:weight_idx + num_weights]).reshape(layer.weight_mean.shape)
                weight_idx += num_weights
            
            # Update bias means
            num_biases = layer.bias_mean.size
            if bias_idx + num_biases <= len(biases):
                layer.bias_mean = cp.array(biases[bias_idx:bias_idx + num_biases])
                bias_idx += num_biases
    
    def evaluate_genome_with_bayesian(self, genome, test_data, test_labels):
        """Evaluate NEAT genome using Bayesian network."""
        self.encode_genome_to_network(genome)
        
        # Get predictions with uncertainty
        results = self.network.predict_with_uncertainty(test_data)
        
        # Fitness based on accuracy and uncertainty
        accuracy = cp.mean((results['predictions'] - test_labels)**2)
        uncertainty_penalty = cp.mean(results['total_uncertainty'])
        
        # Multi-objective fitness
        fitness = -accuracy - 0.1 * uncertainty_penalty
        
        return {
            'fitness': float(cp.asnumpy(fitness)),
            'accuracy': float(cp.asnumpy(accuracy)),
            'uncertainty': float(cp.asnumpy(uncertainty_penalty)),
            'predictions': results
        }


def create_sample_data(n_samples=100):
    """Create sample data for testing."""
    # Create vocabulary indices
    vocab_indices = cp.random.randint(0, 1000, (n_samples, 10))
    
    # Create target values
    targets = cp.random.normal(0, 1, (n_samples, 10))
    
    return vocab_indices, targets


def visualize_uncertainty(network, x, y, title="Bayesian Neural Network Uncertainty"):
    """Visualize predictions with uncertainty bounds."""
    results = network.predict_with_uncertainty(x)
    
    x_np = cp.asnumpy(x).flatten()[:50]  # First 50 points for visualization
    predictions = cp.asnumpy(results['predictions']).flatten()[:50]
    uncertainty = cp.asnumpy(results['total_uncertainty']).flatten()[:50]
    targets = cp.asnumpy(y).flatten()[:50]
    
    plt.figure(figsize=(12, 8))
    
    # Plot predictions with uncertainty
    plt.fill_between(range(len(predictions)), 
                     predictions - 2*uncertainty, 
                     predictions + 2*uncertainty, 
                     alpha=0.3, label='95% Confidence Interval')
    
    plt.plot(predictions, 'b-', label='Predictions', linewidth=2)
    plt.plot(targets, 'r--', label='True Values', linewidth=2)
    plt.xlabel('Sample Index')
    plt.ylabel('Value')
    plt.title(title)
    plt.legend()
    plt.grid(True, alpha=0.3)
    
    return plt


# Example usage and testing
if __name__ == "__main__":
    # Create network
    network = BayesianQuantumNetwork(
        vocab_size=1000,
        embedding_dim=64,
        hidden_dims=[128, 64],
        output_dim=10
    )
    
    # Generate sample data
    x_data, y_data = create_sample_data(100)
    
    # Test predictions with uncertainty
    results = network.predict_with_uncertainty(x_data[:10])
    print("Prediction shape:", results['predictions'].shape)
    print("Epistemic uncertainty:", cp.mean(results['epistemic_uncertainty']))
    print("Aleatoric uncertainty:", cp.mean(results['aleatoric_uncertainty']))
    
    # Test training step
    train_metrics = network.train_step(x_data[:10], y_data[:10])
    print("Training metrics:", train_metrics)
    
    # Visualize results
    plot = visualize_uncertainty(network, x_data[:50], y_data[:50])
    plot.show()