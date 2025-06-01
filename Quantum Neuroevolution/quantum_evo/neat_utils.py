import neat
from config import CONFIG_PATH


def create_neat_config():
    """Load NEAT configuration."""
    return neat.Config(
        neat.DefaultGenome,
        neat.DefaultReproduction,
        neat.DefaultSpeciesSet,
        neat.DefaultStagnation,
        CONFIG_PATH,
    )
