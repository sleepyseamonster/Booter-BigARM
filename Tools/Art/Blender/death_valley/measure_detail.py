"""Measure real fine-grid relief and edge mismatch against a coarser parent DEM."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np


def sample_parent(data: np.ndarray, bounds: list[float], spacing: float,
                  xx: np.ndarray, yy: np.ndarray) -> np.ndarray:
    col = np.clip((xx - bounds[0]) / spacing, 0, data.shape[1] - 1)
    row = np.clip((bounds[3] - yy) / spacing, 0, data.shape[0] - 1)
    c0 = np.minimum(np.floor(col).astype(int), data.shape[1] - 2)
    r0 = np.minimum(np.floor(row).astype(int), data.shape[0] - 2)
    dx, dy = col - c0, row - r0
    return ((1 - dy) * ((1 - dx) * data[r0, c0] + dx * data[r0, c0 + 1])
            + dy * ((1 - dx) * data[r0 + 1, c0] + dx * data[r0 + 1, c0 + 1]))


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--parent-manifest", type=Path, required=True)
    parser.add_argument("--parent-region", required=True)
    parser.add_argument("--child-manifest", type=Path, required=True)
    parser.add_argument("--child-region", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    parent = json.loads(args.parent_manifest.read_text())["regions"][args.parent_region]
    child = json.loads(args.child_manifest.read_text())["regions"][args.child_region]
    pb, cb = parent["bounds_m"], child["bounds_m"]
    if not (pb[0] <= cb[0] < cb[2] <= pb[2] and pb[1] <= cb[1] < cb[3] <= pb[3]):
        raise ValueError("Child bounds do not lie inside parent bounds")
    with np.load(parent["prepared_npz"]) as arrays:
        coarse = arrays["elevation"]
    with np.load(child["prepared_npz"]) as arrays:
        fine = arrays["elevation"]
    spacing = child["resolution_m"]
    xs = cb[0] + np.arange(fine.shape[1]) * spacing
    ys = cb[3] - np.arange(fine.shape[0]) * spacing
    xx, yy = np.meshgrid(xs, ys)
    interpolated = sample_parent(coarse, pb, parent["resolution_m"], xx, yy)
    delta = fine - interpolated
    edge = np.zeros(fine.shape, dtype=bool)
    edge[0, :] = edge[-1, :] = True
    edge[:, 0] = edge[:, -1] = True

    def stats(values: np.ndarray) -> dict:
        absolute = np.abs(values)
        return {"rms_m": float(np.sqrt(np.mean(values * values))),
                "p95_abs_m": float(np.percentile(absolute, 95)),
                "max_abs_m": float(absolute.max())}

    report = {
        "parent_resolution_m": parent["resolution_m"],
        "child_resolution_m": spacing,
        "child_bounds_m": cb,
        "sample_count": int(fine.size),
        "interior_residual": stats(delta[~edge]),
        "raw_boundary_mismatch": stats(delta[edge]),
        "meaning": "Nonzero interior residual shows fine-source relief absent from bilinear parent interpolation; boundary mismatch must be stitched before mesh handoff.",
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report))


if __name__ == "__main__":
    main()
