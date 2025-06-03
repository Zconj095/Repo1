from ClaudeProject1 import *
import numpy as np
from keras.models import Sequential
from keras.layers import LSTM, Dense
import matplotlib.pyplot as plt

class ContinuityEncoder:
    def __init__(self, sequence_len=5, lstm_units=32, epochs=50):
        self.sequence_len = sequence_len
        self.epochs = epochs
        self.model = Sequential()
        self.model.add(LSTM(lstm_units, input_shape=(sequence_len, 1), return_sequences=False))
        self.model.add(Dense(sequence_len, activation='linear'))
        self.model.compile(optimizer='adam', loss='mean_squared_error', metrics=['mae'])
        self.trained = False
    
    def prepare_sequences(self, sequences):
        """Convert sequences to proper numpy format for training"""
        sequences = np.array(sequences)
        if len(sequences.shape) == 2:
            sequences = sequences.reshape((len(sequences), self.sequence_len, 1))
        return sequences
    
    def train(self, sequences, validation_split=0.2, verbose=0):
        """Train the model with validation"""
        X = self.prepare_sequences(sequences)
        y = X.reshape(len(X), self.sequence_len)
        
        history = self.model.fit(X, y, epochs=self.epochs, 
                               validation_split=validation_split, 
                               verbose=verbose)
        self.trained = True
        return history
        
    def predict(self, seq_start):
        """Make predictions with error handling"""
        if not self.trained:
            raise ValueError("Model must be trained before prediction")
        
        seq_start = np.array(seq_start).reshape((1, len(seq_start), 1))
        return self.model.predict(seq_start, verbose=0)

class NeuronSimulation:
    def __init__(self, normal_potential=-150, altered_potential=-100):
        self.normal_mitochondria = Mitochondria(membrane_potential=normal_potential)
        self.altered_mitochondria = Mitochondria(membrane_potential=altered_potential)
        
        self.normal_neuron = Neuron()
        self.altered_neuron = Neuron()
        
    def run_simulation(self, input_current=5, timesteps=10):
        """Run simulation over multiple timesteps"""
        normal_atp = self.normal_mitochondria.produce_ATP()
        altered_atp = self.altered_mitochondria.produce_ATP()
        
        print(f"Normal ATP production: {normal_atp} units")
        print(f"Altered ATP production: {altered_atp} units")
        
        normal_voltages = []
        altered_voltages = []
        
        for _ in range(timesteps):
            # Apply scaled input based on ATP levels
            self.normal_neuron.update(input_current * normal_atp / 100)
            self.altered_neuron.update(input_current * altered_atp / 100)
            
            normal_voltages.append(self.normal_neuron.voltage)
            altered_voltages.append(self.altered_neuron.voltage)
        
        return normal_voltages, altered_voltages
    
    def analyze_learning(self, normal_voltages, altered_voltages):
        """Analyze learning patterns"""
        sequences = [normal_voltages[:5], altered_voltages[:5]]
        
        # Create and train model
        learning_model = ContinuityEncoder(sequence_len=5, epochs=100)
        history = learning_model.train(sequences, verbose=1)
        
        # Make predictions
        prediction_normal = learning_model.predict(normal_voltages[:5])
        prediction_altered = learning_model.predict(altered_voltages[:5])
        
        return learning_model, prediction_normal, prediction_altered, history
    
    def plot_results(self, normal_voltages, altered_voltages, predictions=None):
        """Visualize simulation results"""
        plt.figure(figsize=(12, 8))
        
        # Plot voltage traces
        plt.subplot(2, 1, 1)
        plt.plot(normal_voltages, label='Normal Neuron', marker='o')
        plt.plot(altered_voltages, label='Altered Neuron', marker='s')
        plt.xlabel('Time Step')
        plt.ylabel('Voltage (mV)')
        plt.title('Neuron Voltage Responses')
        plt.legend()
        plt.grid(True)
        
        # Plot predictions if available
        if predictions:
            plt.subplot(2, 1, 2)
            pred_normal, pred_altered = predictions
            plt.plot(pred_normal[0], label='Normal Prediction', marker='o')
            plt.plot(pred_altered[0], label='Altered Prediction', marker='s')
            plt.xlabel('Sequence Position')
            plt.ylabel('Predicted Voltage')
            plt.title('Learning Model Predictions')
            plt.legend()
            plt.grid(True)
        
        plt.tight_layout()
        plt.show()

def main():
    """Main execution function"""
    # Initialize simulation
    sim = NeuronSimulation()
    
    # Run simulation
    normal_voltages, altered_voltages = sim.run_simulation(timesteps=20)
    
    print(f"\nFinal voltage of normal neuron: {normal_voltages[-1]:.2f} mV")
    print(f"Final voltage of altered neuron: {altered_voltages[-1]:.2f} mV")
    
    # Analyze learning
    model, pred_normal, pred_altered, history = sim.analyze_learning(normal_voltages, altered_voltages)
    
    print(f"\nLearning prediction for normal neuron: {pred_normal[0]}")
    print(f"Learning prediction for altered neuron: {pred_altered[0]}")
    
    # Calculate prediction differences
    diff = np.mean(np.abs(pred_normal - pred_altered))
    print(f"Average prediction difference: {diff:.4f}")
    
    # Visualize results
    sim.plot_results(normal_voltages, altered_voltages, 
                    predictions=(pred_normal, pred_altered))

if __name__ == "__main__":
    main()
