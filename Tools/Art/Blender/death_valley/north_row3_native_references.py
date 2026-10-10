"""Exact immutable native snapshots, with explicit orientation and owned-path guards."""
from pathlib import Path, PurePosixPath
from functools import lru_cache
import numpy as np
from north_row3_contract import sha256

@lru_cache(maxsize=32)
def resolved_source_owners(root, owners):
    return tuple((Path(root) / owner / "Source").resolve() for owner in owners)

def read_native_reference(root, record, allowed_owners):
    relative = record.get("path", "")
    parts = PurePosixPath(relative).parts
    if not relative or ":" in relative or "\\" in relative or PurePosixPath(relative).is_absolute() or ".." in parts:
        raise ValueError("Native reference must be an owned relative asset path")
    owners = resolved_source_owners(str(Path(root).resolve()), tuple(allowed_owners))
    path = (Path(root) / relative).resolve()
    if not any(path.is_relative_to(owner) for owner in owners):
        raise ValueError("Native reference escaped all protected owners")
    orientation = record.get("orientation")
    if orientation not in ("north-first", "south-first") or record.get("samples") != 257:
        raise ValueError("Explicit native orientation and 257-square grid required")
    expected = record.get("sha256", "")
    if len(expected) != 64 or not path.is_file() or path.stat().st_size != 257 * 257 * 4 or sha256(path) != expected:
        raise ValueError("Native snapshot missing, changed or incorrectly sized")
    result = np.fromfile(path, dtype="<f4").reshape(257, 257)
    if not np.isfinite(result).all() or result.min() < 0 or result.max() > 1:
        raise ValueError("Invalid normalized native samples")
    return result[::-1].copy() if orientation == "north-first" else result
