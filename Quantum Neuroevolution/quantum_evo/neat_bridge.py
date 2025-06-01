"""NEAT-based evolution on hyperdimensional embeddings."""

import cupy as cp
import neat
from .neat_utils import create_neat_config


def evolve_embedding(embedding: cp.ndarray):
    """Evolve a tiny network to mimic the embedding."""
    config = create_neat_config()

    def eval_genomes(genomes, cfg):
        for gid, genome in genomes:
            net = neat.nn.FeedForwardNetwork.create(genome, cfg)
            out = net.activate(cp.asnumpy(embedding))
            genome.fitness = float(-cp.linalg.norm(cp.asarray(out) - embedding))

    pop = neat.Population(config)
    pop.add_reporter(neat.StdOutReporter(False))
    pop.run(eval_genomes, n=3)
