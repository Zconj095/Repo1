"""Data generation utilities using CuPy."""

import cupy as cp


def generate_hexagonal_data(num_samples: int, timesteps: int, data_dim: int):
    """Generate synthetic hexagonal data with CuPy."""
    X = cp.random.random((num_samples, timesteps, data_dim))
    y = cp.random.random((num_samples, data_dim))
    return X, y
