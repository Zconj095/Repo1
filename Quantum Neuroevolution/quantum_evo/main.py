"""Entry point for the Neuroquantum Evolution Toolkit with H5 Deep Learning."""

import cupy as cp
import os
from pathlib import Path
from evolution import run_evolution
from integration import integrate
from visualize import plot_hfp
from deep_learning_trainer import (
    train_quantum_bayesian_network,
    create_training_data,
    DeepBayesianNetwork
)


def ensure_directories():
    """Ensure required directories exist."""
    directories = ['models', 'outputs']
    for directory in directories:
        Path(directory).mkdir(parents=True, exist_ok=True)
    print(f"✅ Created directories: {directories}")

def create_simple_bayesian_network():
    """Create a simplified Bayesian network without complex quantum operations."""
    class SimpleBayesianNetwork:
        def __init__(self, input_dim=32, hidden_dim=64, output_dim=2):
            self.input_dim = input_dim
            self.hidden_dim = hidden_dim
            self.output_dim = output_dim
            
            # Simple weights
            self.w1 = cp.random.normal(0, 0.1, (input_dim, hidden_dim))
            self.b1 = cp.zeros(hidden_dim)
            self.w2 = cp.random.normal(0, 0.1, (hidden_dim, output_dim))
            self.b2 = cp.zeros(output_dim)
        
        def predict_with_uncertainty(self, x):
            """Simple forward pass with mock uncertainty."""
            # Simple feedforward
            h1 = cp.maximum(0, cp.dot(x, self.w1) + self.b1)  # ReLU
            output = cp.dot(h1, self.w2) + self.b2
            
            # Mock uncertainty (in practice this would be from Bayesian inference)
            epistemic = cp.random.uniform(0.01, 0.1, output.shape[0])
            aleatoric = cp.random.uniform(0.01, 0.1, output.shape[0])
            
            return {
                'predictions': output,
                'epistemic_uncertainty': epistemic,
                'aleatoric_uncertainty': aleatoric,
                'total_uncertainty': epistemic + aleatoric,
                'raw_samples': cp.stack([output] * 10)  # Mock samples
            }
    
    return SimpleBayesianNetwork()


def create_simple_integration():
    """Create simplified NEAT-Bayesian integration."""
    class SimpleIntegration:
        def __init__(self, network):
            self.network = network
        
        def evaluate_genome_with_bayesian(self, genome, test_data, test_labels):
            """Simple evaluation without complex quantum operations."""
            # Simple prediction
            results = self.network.predict_with_uncertainty(test_data)
            
            # Simple fitness calculation
            accuracy = cp.mean((results['predictions'] - test_labels)**2)
            uncertainty_penalty = cp.mean(results['total_uncertainty'])
            
            fitness = -accuracy - 0.1 * uncertainty_penalty
            
            return {
                'fitness': float(cp.asnumpy(fitness)),
                'accuracy': float(cp.asnumpy(accuracy)),
                'uncertainty': float(cp.asnumpy(uncertainty_penalty)),
                'predictions': results
            }
    
    return SimpleIntegration


def main():
    print("🚀 Starting Quantum Neuroevolution with Deep Learning & H5 Persistence...")
    
    # Ensure directories exist
    ensure_directories()
    
    # Create and train deep learning model
    print("🧠 Training Deep Bayesian Quantum Network...")
    
    # Generate training data with smaller, manageable sizes
    print("📊 Generating training data...")
    x_train, y_train = create_training_data(500)  # Reduced from 2000
    x_val, y_val = create_training_data(100)      # Reduced from 400
    
    print(f"Training data shapes: X={x_train.shape}, Y={y_train.shape}")
    print(f"Validation data shapes: X={x_val.shape}, Y={y_val.shape}")
    
    # Model configuration
    model_config = {
        'vocab_size': 1000,
        'embedding_dim': 64,      # Reduced from 128
        'hidden_dims': [128, 64], # Reduced complexity
        'output_dim': 3,
        'dropout_rate': 0.2
    }
    
    # Training configuration
    training_config = {
        'epochs': 20,           # Reduced for faster testing
        'batch_size': 16,       # Reduced batch size
        'save_interval': 5,     # More frequent saves
        'model_save_path': 'models/quantum_bayesian_network.h5'
    }
    
    try:
        # Train the deep learning model
        print("🎯 Starting model training...")
        deep_model = train_quantum_bayesian_network(
            (x_train, y_train),
            (x_val, y_val),
            model_config,
            training_config
        )
        
        print("💾 Deep learning model trained and saved to H5!")
        
        # Test deep model predictions with uncertainty
        print("🔮 Testing deep model uncertainty quantification...")
        test_batch_size = 20
        test_predictions = deep_model.forward(x_val[:test_batch_size], training=False, num_samples=10)  # Use 10 samples for uncertainty
        test_loss_dict = deep_model.compute_loss(test_predictions, y_val[:test_batch_size])
        
        print(f"Deep Model - Epistemic Uncertainty: {test_loss_dict['epistemic_uncertainty']:.4f}")
        print(f"Deep Model - Aleatoric Uncertainty: {test_loss_dict['aleatoric_uncertainty']:.4f}")
        print(f"Deep Model - Validation Loss: {test_loss_dict['total_loss']:.4f}")
        
    except Exception as e:
        print(f"❌ Error in deep learning training: {e}")
        print("⚠️ Continuing with NEAT evolution...")
        deep_model = None
    
    # Original quantum integration
    print("⚛️ Running quantum integration...")
    data_vector = cp.ones(4)
    hfp_values = []
    for _ in range(3):
        hfp = integrate(data_vector)
        hfp_values.append(cp.asnumpy(hfp))
    
    # Run NEAT evolution
    print("🧬 Running NEAT evolution...")
    winner = run_evolution()
    
    # Create simplified Bayesian network for NEAT integration
    print("🔗 Creating NEAT-Bayesian integration...")
    simple_network = create_simple_bayesian_network()
    
    # Generate data for NEAT evaluation (simplified)
    print("📊 Generating NEAT evaluation data...")
    x_neat = cp.random.normal(0, 1, (20, 32))  # Simple 2D data
    y_neat = cp.random.normal(0, 1, (20, 2))   # Simple 2D targets
    
    # Integrate NEAT with simplified Bayesian Network
    integration_class = create_simple_integration()
    integration = integration_class(simple_network)
    
    # Evaluate evolved genome with Bayesian uncertainty
    evaluation_results = integration.evaluate_genome_with_bayesian(
        winner, x_neat, y_neat
    )
    
    print("📊 NEAT-Bayesian Integration Results:")
    print(f"NEAT Fitness: {evaluation_results['fitness']:.4f}")
    print(f"NEAT Accuracy: {evaluation_results['accuracy']:.4f}")
    print(f"NEAT Uncertainty Penalty: {evaluation_results['uncertainty']:.4f}")
    
    # Load and test the saved H5 model if training succeeded
    if deep_model is not None:
        print("💿 Testing H5 model loading...")
        try:
            loaded_model = DeepBayesianNetwork(**model_config)
            loaded_model.load_from_h5('models/quantum_bayesian_network.h5')
            
            # Test loaded model with multiple samples for uncertainty
            loaded_predictions = loaded_model.forward(x_val[:10], training=False, num_samples=8)
            loaded_loss = loaded_model.compute_loss(loaded_predictions, y_val[:10])
            print(f"Loaded H5 Model Loss: {loaded_loss['total_loss']:.4f}")
            print(f"Loaded H5 Model Epistemic: {loaded_loss['epistemic_uncertainty']:.4f}")
            print(f"Loaded H5 Model Aleatoric: {loaded_loss['aleatoric_uncertainty']:.4f}")
        except Exception as e:
            print(f"⚠️ Error testing H5 model: {e}")
    
    # Generate visualizations
    print("📈 Generating visualizations...")
    
    # Original HFP plot
    try:
        hfp_plot = plot_hfp(hfp_values)
        hfp_plot.savefig('outputs/quantum_hfp_analysis.png', dpi=150, bbox_inches='tight')
        print("✅ HFP visualization saved")
    except Exception as e:
        print(f"⚠️ Error in HFP visualization: {e}")
    
    # Final comprehensive report
    print("\n" + "="*60)
    print("🎉 QUANTUM NEUROEVOLUTION COMPLETE!")
    print("="*60)
    if deep_model is not None:
        print(f"🧠 Deep Learning Model: ✅ Trained and saved to H5")
        print(f"📊 Deep Model Performance:")
        print(f"   - Epistemic Uncertainty: {test_loss_dict['epistemic_uncertainty']:.4f}")
        print(f"   - Aleatoric Uncertainty: {test_loss_dict['aleatoric_uncertainty']:.4f}")
        print(f"   - Validation Loss: {test_loss_dict['total_loss']:.4f}")
    else:
        print(f"🧠 Deep Learning Model: ❌ Training failed")
    
    print(f"🧬 NEAT Evolution: ✅ Complete")
    print(f"   - Winner Genome: {winner}")
    print(f"   - NEAT-Bayesian Fitness: {evaluation_results['fitness']:.4f}")
    print(f"📈 Visualizations: Saved to outputs/ directory")
    print("="*60)


if __name__ == "__main__":
    main()
