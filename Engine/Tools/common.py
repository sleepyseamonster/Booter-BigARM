"""Shared paths and JSON output for the Engine preparation tools (Python 3.9+)."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def inside(value):
    path = Path(value)
    path = (ROOT / path if not path.is_absolute() else path).resolve()
    if path != ROOT and ROOT not in path.parents:
        raise ValueError("Path must remain inside Engine: " + str(value))
    return path


def write_json(path, value):
    path = inside(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")
