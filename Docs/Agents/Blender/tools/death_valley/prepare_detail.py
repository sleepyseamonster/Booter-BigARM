"""Clip a selected 1 m 3DEP source to a small, aligned Blender detail patch."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
import rasterio
from rasterio.enums import Resampling
from rasterio.transform import from_bounds
from rasterio.warp import reproject
from PIL import Image

from prepare_region import landform_masks, sha256, terrain_signals


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--bounds", type=float, nargs=4, default=(486650, 4046900, 487150, 4047400))
    args = parser.parse_args()
    xmin, ymin, xmax, ymax = args.bounds
    if xmax <= xmin or ymax <= ymin or any(v != int(v) for v in args.bounds):
        raise ValueError("Detail bounds must be positive integer metre coordinates")
    width, height = int(xmax - xmin) + 1, int(ymax - ymin) + 1
    if width > 1001 or height > 1001:
        raise ValueError("Keep the first 1 m detail patch at or below 1 km per side")
    destination = np.full((height, width), np.nan, dtype="float32")
    target_transform = from_bounds(xmin - 0.5, ymin - 0.5, xmax + 0.5, ymax + 0.5, width, height)
    with rasterio.open(args.source) as source:
        if source.crs.to_epsg() != 26911 or not (source.bounds.left < xmin and source.bounds.right > xmax and source.bounds.bottom < ymin and source.bounds.top > ymax):
            raise RuntimeError("Source 1 m tile does not contain the requested EPSG:26911 patch")
        reproject(
            rasterio.band(source, 1), destination,
            src_transform=source.transform, src_crs=source.crs, src_nodata=source.nodata,
            dst_transform=target_transform, dst_crs=source.crs, dst_nodata=np.nan,
            resampling=Resampling.bilinear,
        )
    slope, aspect, ruggedness, shade = terrain_signals(destination, 1)
    masks = landform_masks(destination, slope, ruggedness, 1)
    args.output.mkdir(parents=True, exist_ok=True)
    npz = args.output / "terrain.npz"
    np.savez_compressed(npz, elevation=destination, slope=slope, aspect=aspect, ruggedness=ruggedness, **masks)
    Image.fromarray(np.uint8(np.clip(shade * 255, 0, 255))).save(args.output / "hillshade_1m.png")
    record = {
        "name": "mosaic-canyon-1m-detail-patch",
        "source": str(args.source.resolve()),
        "source_sha256": sha256(args.source),
        "source_dataset": "USGS 1 Meter 11 x48y405 CA_FEMAR9Southeast_D24",
        "source_crs": "EPSG:26911",
        "bounds_m": list(args.bounds),
        "resolution_m": 1,
        "vertex_shape": [height, width],
        "elevation_range_m": [float(destination.min()), float(destination.max())],
        "prepared_npz": str(npz.resolve()),
        "prepared_sha256": sha256(npz),
        "resampling": "bilinear from source 1 m GeoTIFF to integer metre vertex grid",
        "mask_coverage": {name: float(np.mean(value > 0.5)) for name, value in masks.items()},
    }
    (args.output / "manifest.json").write_text(json.dumps(record, indent=2) + "\n")
    print(json.dumps({k: record[k] for k in ("vertex_shape", "elevation_range_m", "prepared_sha256")}))


if __name__ == "__main__":
    main()
