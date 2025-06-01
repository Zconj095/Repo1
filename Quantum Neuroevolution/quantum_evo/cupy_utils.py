"""CuPy-based mathematical utilities."""

import cupy as cp
import numpy as np


def calculate_hyperroot_flux_parameter(R, H, S, n, m, p):
    """Calculate the hyperroot flux parameter (HFP)."""
    H = cp.asarray(H)
    S = cp.asarray(S)
    HFP = 0
    for i in range(n):
        for j in range(m):
            S_prod = cp.prod(S[i, j, :], axis=0)
            HFP = R * cp.exp(H[i, j] * S_prod)
    return HFP


def calculate_digital_flux_ambiance(HFP, lambda_val, I, D, E, a, b, c):
    """Calculate the Digital Flux Ambiance (DFA)."""
    return lambda_val * (HFP * cp.power(I, a) * cp.power(D, b) * cp.power(E, c))
