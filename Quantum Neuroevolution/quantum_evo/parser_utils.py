"""Parse code from Unity codebase text files."""

import re
from pathlib import Path


def extract_tokens(path: str):
    """Return alphanumeric tokens from the given file."""
    text = Path(path).read_text(errors="ignore")
    return re.findall(r"[A-Za-z_]+", text)
