import cupy as cp
import neat
from neat_utils import create_neat_config
from data_generation import generate_hexagonal_data


def eval_genomes(genomes, config):
    X, y = generate_hexagonal_data(10, 5, 2)
    for genome_id, genome in genomes:
        net = neat.nn.FeedForwardNetwork.create(genome, config)
        # Convert input to numpy for NEAT (it doesn't work with CuPy)
        input_data = cp.asnumpy(X[0].ravel())
        output = net.activate(input_data)
        
        # Convert output list to CuPy array for computation
        output_cp = cp.array(output)
        target_cp = cp.array(y[0]) if isinstance(y[0], list) else y[0]
        
        # Calculate fitness
        genome.fitness = float(-cp.linalg.norm(output_cp - target_cp))


def run_evolution():
    config = create_neat_config()
    pop = neat.Population(config)
    pop.add_reporter(neat.StdOutReporter(True))
    stats = neat.StatisticsReporter()
    pop.add_reporter(stats)
    winner = pop.run(eval_genomes, n=5)
    return winner
