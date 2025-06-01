import sys, types

dummy = types.ModuleType('dummy')
for name in [
    'cupy',
    'qiskit',
    'qiskit_aer',
    'neat',
    'numpy',
    'matplotlib',
    'matplotlib.pyplot',
]:
    sys.modules.setdefault(name, dummy)
