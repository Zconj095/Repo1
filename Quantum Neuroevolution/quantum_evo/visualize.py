"""Simple visualization utilities."""

import matplotlib.pyplot as plt


def plot_hfp(values):
    plt.figure()
    plt.plot(values)
    plt.title("Hyperroot Flux Parameter Evolution")
    plt.xlabel("Generation")
    plt.ylabel("HFP")
    return plt
