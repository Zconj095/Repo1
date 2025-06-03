import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers, callbacks, optimizers
import numpy as np
import pickle
import logging
from typing import Dict
from dataclasses import dataclass
import matplotlib.pyplot as plt
import json
import time

# Configuration
@dataclass
class EvolutionConfig:
    max_text_length: int = 512
    num_classes: int = 10
    vocab_size: int = 10000
    embedding_dim: int = 128
    add_layer_chance: float = 0.3
    delete_layer_chance: float = 0.1
    modify_layer_chance: float = 0.4
    generations: int = 100
    population_size: int = 50
    max_layers: int = 10
    min_layers: int = 3
    batch_size: int = 32
    epochs_per_eval: int = 5
    early_stopping_patience: int = 3

class AdvancedNeuroEvolution:
    def __init__(self, config: EvolutionConfig, x_train, y_train, x_val, y_val, x_test, y_test):
        self.config = config
        self.x_train, self.y_train = x_train, y_train
        self.x_val, self.y_val = x_val, y_val
        self.x_test, self.y_test = x_test, y_test
        
        # Setup logging
        self.setup_logging()
        
        # Evolution tracking
        self.generation_stats = []
        self.best_models = []
        
        # Layer types for evolution
        self.layer_types = [
            'dense', 'dropout', 'batch_norm'
        ]
        
        # Activation functions
        self.activations = ['relu', 'tanh', 'sigmoid', 'swish', 'gelu', 'elu']
        
        # Optimizers
        self.optimizers = ['adam', 'rmsprop', 'sgd', 'adamw']

    def setup_logging(self):
        logging.basicConfig(
            level=logging.INFO,
            format='%(asctime)s - %(levelname)s - %(message)s',
            handlers=[
                logging.FileHandler('evolution.log'),
                logging.StreamHandler()
            ]
        )
        self.logger = logging.getLogger(__name__)

    def create_base_model(self) -> tf.keras.Model:
        """Create base model with embedding layer"""
        inputs = layers.Input(shape=(self.config.max_text_length,))
        
        # Embedding layer
        x = layers.Embedding(
            self.config.vocab_size, 
            self.config.embedding_dim,
            mask_zero=True
        )(inputs)
        
        # Initial layers
        x = layers.LSTM(64, return_sequences=False)(x)
        x = layers.Dropout(0.3)(x)
        x = layers.Dense(32, activation='relu')(x)
        
        outputs = layers.Dense(self.config.num_classes, activation='softmax')(x)
        
        model = tf.keras.Model(inputs=inputs, outputs=outputs)
        return model

    def get_layer_output_shape(self, layer, input_shape):
        """Get the output shape of a layer given input shape"""
        try:
            # Create a temporary model to compute output shape
            temp_input = layers.Input(shape=input_shape[1:])
            temp_output = layer(temp_input)
            temp_model = tf.keras.Model(temp_input, temp_output)
            return temp_model.output_shape
        except:
            return None

    def safe_model_rebuild(self, layer_configs, input_shape):
        """Safely rebuild model with proper shape checking"""
        try:
            inputs = layers.Input(shape=input_shape[1:])
            x = inputs
            
            for config in layer_configs:
                layer_type = config['type']
                params = config['params']
                
                # Get current shape for validation
                current_shape = x.shape
                
                if layer_type == 'embedding':
                    x = layers.Embedding(**params)(x)
                elif layer_type == 'lstm':
                    x = layers.LSTM(**params)(x)
                elif layer_type == 'dense':
                    # Ensure we have 2D input for Dense layers
                    if len(current_shape) > 2:
                        x = layers.Flatten()(x)
                    x = layers.Dense(**params)(x)
                elif layer_type == 'dropout':
                    x = layers.Dropout(**params)(x)
                elif layer_type == 'batch_norm':
                    x = layers.BatchNormalization()(x)
                    
            return tf.keras.Model(inputs=inputs, outputs=x)
        except Exception as e:
            self.logger.error(f"Model rebuild failed: {e}")
            return None

    def model_to_config(self, model):
        """Convert model to configuration that can be safely rebuilt"""
        configs = []
        
        for layer in model.layers:
            if isinstance(layer, layers.InputLayer):
                continue
            elif isinstance(layer, layers.Embedding):
                configs.append({
                    'type': 'embedding',
                    'params': {
                        'input_dim': layer.input_dim,
                        'output_dim': layer.output_dim,
                        'mask_zero': layer.mask_zero
                    }
                })
            elif isinstance(layer, layers.LSTM):
                configs.append({
                    'type': 'lstm',
                    'params': {
                        'units': layer.units,
                        'return_sequences': layer.return_sequences
                    }
                })
            elif isinstance(layer, layers.Dense):
                configs.append({
                    'type': 'dense',
                    'params': {
                        'units': layer.units,
                        'activation': layer.activation.__name__ if hasattr(layer.activation, '__name__') else 'linear'
                    }
                })
            elif isinstance(layer, layers.Dropout):
                configs.append({
                    'type': 'dropout',
                    'params': {
                        'rate': layer.rate
                    }
                })
            elif isinstance(layer, layers.BatchNormalization):
                configs.append({
                    'type': 'batch_norm',
                    'params': {}
                })
                
        return configs

    def add_layer(self, model: tf.keras.Model, layer_type: str = None) -> tf.keras.Model:
        """Add a new layer to the model"""
        if layer_type is None:
            layer_type = np.random.choice(self.layer_types)
        
        try:
            configs = self.model_to_config(model)
            
            # Find insertion point (before output layer)
            if len(configs) < 2:
                return model
                
            insert_idx = len(configs) - 1  # Before output layer
            
            # Create new layer config
            if layer_type == 'dense':
                new_config = {
                    'type': 'dense',
                    'params': {
                        'units': np.random.choice([16, 32, 64, 128]),
                        'activation': np.random.choice(self.activations)
                    }
                }
            elif layer_type == 'dropout':
                new_config = {
                    'type': 'dropout',
                    'params': {
                        'rate': np.random.uniform(0.1, 0.5)
                    }
                }
            elif layer_type == 'batch_norm':
                new_config = {
                    'type': 'batch_norm',
                    'params': {}
                }
            else:
                return model
            
            # Insert new layer
            configs.insert(insert_idx, new_config)
            
            # Rebuild model
            new_model = self.safe_model_rebuild(configs, model.input_shape)
            if new_model is not None:
                return new_model
            else:
                return model
                
        except Exception as e:
            self.logger.warning(f"Failed to add layer {layer_type}: {e}")
            return model

    def remove_layer(self, model: tf.keras.Model) -> tf.keras.Model:
        """Remove a layer from the model"""
        try:
            configs = self.model_to_config(model)
            
            # Need at least: embedding, lstm, dense (output)
            if len(configs) <= self.config.min_layers:
                return model
            
            # Find removable layers (not embedding, lstm, or output)
            removable_indices = []
            for i, config in enumerate(configs):
                if config['type'] not in ['embedding', 'lstm'] and i < len(configs) - 1:
                    removable_indices.append(i)
            
            if not removable_indices:
                return model
            
            # Remove random layer
            remove_idx = np.random.choice(removable_indices)
            configs.pop(remove_idx)
            
            # Rebuild model
            new_model = self.safe_model_rebuild(configs, model.input_shape)
            if new_model is not None:
                return new_model
            else:
                return model
                
        except Exception as e:
            self.logger.warning(f"Failed to remove layer: {e}")
            return model

    def modify_layer(self, model: tf.keras.Model) -> tf.keras.Model:
        """Modify parameters of an existing layer"""
        try:
            configs = self.model_to_config(model)
            
            # Find modifiable layers
            modifiable_indices = []
            for i, config in enumerate(configs):
                if config['type'] in ['dense', 'dropout'] and i < len(configs) - 1:
                    modifiable_indices.append(i)
            
            if not modifiable_indices:
                return model
            
            # Modify random layer
            modify_idx = np.random.choice(modifiable_indices)
            config = configs[modify_idx]
            
            if config['type'] == 'dense':
                config['params']['units'] = np.random.choice([16, 32, 64, 128])
                config['params']['activation'] = np.random.choice(self.activations)
            elif config['type'] == 'dropout':
                config['params']['rate'] = np.random.uniform(0.1, 0.6)
            
            # Rebuild model
            new_model = self.safe_model_rebuild(configs, model.input_shape)
            if new_model is not None:
                return new_model
            else:
                return model
                
        except Exception as e:
            self.logger.warning(f"Failed to modify layer: {e}")
            return model

    def evolve_topology(self, model: tf.keras.Model) -> tf.keras.Model:
        """Apply random mutations to model topology"""
        try:
            if np.random.random() < self.config.add_layer_chance and len(model.layers) < self.config.max_layers:
                model = self.add_layer(model)
                self.logger.info("Added layer to model")
                
            if np.random.random() < self.config.delete_layer_chance:
                model = self.remove_layer(model)
                self.logger.info("Removed layer from model")
                
            if np.random.random() < self.config.modify_layer_chance:
                model = self.modify_layer(model)
                self.logger.info("Modified layer in model")
                
        except Exception as e:
            self.logger.warning(f"Evolution failed: {e}")
            
        return model

    def compile_model(self, model: tf.keras.Model) -> tf.keras.Model:
        """Compile model with random optimizer and learning rate"""
        optimizer_name = np.random.choice(self.optimizers)
        learning_rate = np.random.uniform(0.0001, 0.01)
        
        if optimizer_name == 'adam':
            optimizer = optimizers.Adam(learning_rate=learning_rate)
        elif optimizer_name == 'rmsprop':
            optimizer = optimizers.RMSprop(learning_rate=learning_rate)
        elif optimizer_name == 'sgd':
            optimizer = optimizers.SGD(learning_rate=learning_rate, momentum=0.9)
        elif optimizer_name == 'adamw':
            optimizer = optimizers.AdamW(learning_rate=learning_rate)
            
        model.compile(
            optimizer=optimizer,
            loss='sparse_categorical_crossentropy',
            metrics=['accuracy']
        )
        
        return model

    def evaluate_fitness(self, model: tf.keras.Model) -> Dict:
        """Comprehensive fitness evaluation"""
        try:
            model = self.compile_model(model)
            
            # Callbacks
            early_stopping = callbacks.EarlyStopping(
                patience=self.config.early_stopping_patience,
                restore_best_weights=True
            )
            
            reduce_lr = callbacks.ReduceLROnPlateau(
                factor=0.5, patience=2, min_lr=1e-7
            )
            
            # Training
            start_time = time.time()
            history = model.fit(
                self.x_train, self.y_train,
                validation_data=(self.x_val, self.y_val),
                epochs=self.config.epochs_per_eval,
                batch_size=self.config.batch_size,
                callbacks=[early_stopping, reduce_lr],
                verbose=0
            )
            training_time = time.time() - start_time
            
            # Evaluation
            val_loss, val_accuracy = model.evaluate(
                self.x_val, self.y_val, verbose=0
            )
            
            # Model complexity penalty
            param_count = model.count_params()
            complexity_penalty = param_count / 1000000
            
            # Composite fitness score
            fitness = val_accuracy * 0.7 + (1 - complexity_penalty) * 0.2 + (1 / (training_time + 1)) * 0.1
            
            return {
                'fitness': fitness,
                'accuracy': val_accuracy,
                'loss': val_loss,
                'params': param_count,
                'training_time': training_time,
                'history': history.history
            }
            
        except Exception as e:
            self.logger.error(f"Evaluation failed: {e}")
            return {
                'fitness': 0.0,
                'accuracy': 0.0,
                'loss': float('inf'),
                'params': 0,
                'training_time': float('inf'),
                'history': {}
            }

    def save_model_architecture(self, model: tf.keras.Model, filepath: str):
        """Save model architecture and weights"""
        try:
            model.save(filepath)
            
            # Save architecture as JSON
            arch_path = filepath.replace('.h5', '_architecture.json')
            with open(arch_path, 'w') as f:
                json.dump(model.to_json(), f)
        except Exception as e:
            self.logger.warning(f"Failed to save model: {e}")

    def visualize_evolution(self, save_path: str = 'evolution_progress.png'):
        """Create visualization of evolution progress"""
        if not self.generation_stats:
            return
            
        try:
            generations = range(len(self.generation_stats))
            best_fitness = [stats['best_fitness'] for stats in self.generation_stats]
            avg_fitness = [stats['avg_fitness'] for stats in self.generation_stats]
            
            plt.figure(figsize=(12, 8))
            
            plt.subplot(2, 2, 1)
            plt.plot(generations, best_fitness, 'b-', label='Best Fitness')
            plt.plot(generations, avg_fitness, 'r--', label='Average Fitness')
            plt.xlabel('Generation')
            plt.ylabel('Fitness')
            plt.title('Evolution Progress')
            plt.legend()
            plt.grid(True)
            
            plt.subplot(2, 2, 2)
            best_accuracy = [stats['best_accuracy'] for stats in self.generation_stats]
            plt.plot(generations, best_accuracy, 'g-')
            plt.xlabel('Generation')
            plt.ylabel('Accuracy')
            plt.title('Best Accuracy Over Time')
            plt.grid(True)
            
            plt.subplot(2, 2, 3)
            param_counts = [stats['best_params'] for stats in self.generation_stats]
            plt.plot(generations, param_counts, 'purple')
            plt.xlabel('Generation')
            plt.ylabel('Parameter Count')
            plt.title('Model Complexity Over Time')
            plt.grid(True)
            
            plt.subplot(2, 2, 4)
            diversity = [stats['diversity'] for stats in self.generation_stats]
            plt.plot(generations, diversity, 'orange')
            plt.xlabel('Generation')
            plt.ylabel('Population Diversity')
            plt.title('Population Diversity')
            plt.grid(True)
            
            plt.tight_layout()
            plt.savefig(save_path, dpi=300, bbox_inches='tight')
            plt.close()
        except Exception as e:
            self.logger.warning(f"Failed to create visualization: {e}")

    def train_model(self) -> tf.keras.Model:
        """Advanced evolutionary training with comprehensive tracking"""
        self.logger.info("Starting evolutionary training...")
        
        # Initialize population
        population = []
        for i in range(self.config.population_size):
            model = self.create_base_model()
            # Apply random mutations to create diversity
            for _ in range(np.random.randint(1, 3)):
                model = self.evolve_topology(model)
            population.append(model)
        
        best_overall_fitness = 0
        best_overall_model = None
        
        for generation in range(self.config.generations):
            self.logger.info(f"Generation {generation + 1}/{self.config.generations}")
            
            # Evaluate population
            fitness_scores = []
            evaluation_results = []
            
            for i, model in enumerate(population):
                self.logger.info(f"Evaluating individual {i + 1}/{len(population)}")
                results = self.evaluate_fitness(model)
                fitness_scores.append(results['fitness'])
                evaluation_results.append(results)
                
                # Track best model
                if results['fitness'] > best_overall_fitness:
                    best_overall_fitness = results['fitness']
                    best_overall_model = tf.keras.models.clone_model(model)
                    best_overall_model.set_weights(model.get_weights())
                    
                    # Save best model
                    self.save_model_architecture(
                        best_overall_model, 
                        f'best_model_gen_{generation}.h5'
                    )
            
            # Selection (tournament selection)
            selected_indices = []
            for _ in range(self.config.population_size):
                tournament_size = 3
                tournament_indices = np.random.choice(
                    len(population), tournament_size, replace=False
                )
                tournament_fitness = [fitness_scores[i] for i in tournament_indices]
                winner_idx = tournament_indices[np.argmax(tournament_fitness)]
                selected_indices.append(winner_idx)
            
            # Create next generation
            new_population = []
            for idx in selected_indices:
                # Clone model
                parent = population[idx]
                child = tf.keras.models.clone_model(parent)
                child.set_weights(parent.get_weights())
                
                # Apply mutations
                child = self.evolve_topology(child)
                new_population.append(child)
            
            population = new_population
            
            # Track statistics
            gen_stats = {
                'generation': generation,
                'best_fitness': max(fitness_scores),
                'avg_fitness': np.mean(fitness_scores),
                'std_fitness': np.std(fitness_scores),
                'best_accuracy': max([r['accuracy'] for r in evaluation_results]),
                'best_params': evaluation_results[np.argmax(fitness_scores)]['params'],
                'diversity': np.std(fitness_scores)
            }
            
            self.generation_stats.append(gen_stats)
            
            self.logger.info(f"Generation {generation + 1} - "
                           f"Best Fitness: {gen_stats['best_fitness']:.4f}, "
                           f"Avg Fitness: {gen_stats['avg_fitness']:.4f}")
            
            # Save progress every 10 generations
            if (generation + 1) % 10 == 0:
                self.visualize_evolution(f'evolution_gen_{generation + 1}.png')
                
                # Save statistics
                with open(f'evolution_stats_gen_{generation + 1}.pkl', 'wb') as f:
                    pickle.dump(self.generation_stats, f)
        
        # Final evaluation on test set
        self.logger.info("Evaluating best model on test set...")
        best_overall_model = self.compile_model(best_overall_model)
        test_results = best_overall_model.evaluate(self.x_test, self.y_test, verbose=0)
        
        self.logger.info(f"Final test accuracy: {test_results[1]:.4f}")
        
        # Generate final visualizations
        self.visualize_evolution('final_evolution_progress.png')
        
        return best_overall_model

# Example usage
if __name__ == "__main__":
    # Mock data for demonstration
    np.random.seed(42)
    config = EvolutionConfig()
    
    # Generate sample data
    x_train = np.random.randint(0, config.vocab_size, (1000, config.max_text_length))
    y_train = np.random.randint(0, config.num_classes, 1000)
    x_val = np.random.randint(0, config.vocab_size, (200, config.max_text_length))
    y_val = np.random.randint(0, config.num_classes, 200)
    x_test = np.random.randint(0, config.vocab_size, (200, config.max_text_length))
    y_test = np.random.randint(0, config.num_classes, 200)
    
    # Initialize and run evolution
    evolution = AdvancedNeuroEvolution(config, x_train, y_train, x_val, y_val, x_test, y_test)
    best_model = evolution.train_model()
    
    # Save final model
    best_model.save("ultimate_evolved_nlp_model.h5")
    
    print("Evolution complete! Best model saved.")
