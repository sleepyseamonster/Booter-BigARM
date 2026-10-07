"""Fill NAIP no-data only with a same-grid natural-color Landsat reference.

The fallback image must be exported over the exact NAIP bbox, CRS and pixel size.
It is for a Blender geographic study, not a shipping game texture.
"""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
import rasterio
from PIL import Image, ImageFilter


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--naip", type=Path, required=True)
    parser.add_argument("--fallback", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    with rasterio.open(args.naip) as source:
        if source.count < 4:
            raise ValueError("Expected four-band NAIP with an alpha coverage band")
        bands = source.read()
        if source.crs.to_epsg() != 26911:
            raise ValueError(f"Expected EPSG:26911 NAIP, got {source.crs}")
        bounds = list(source.bounds)
    rgb = Image.fromarray(np.moveaxis(bands[:3], 0, -1).astype("uint8"), "RGB")
    fallback = Image.open(args.fallback).convert("RGB")
    if fallback.size != rgb.size:
        raise ValueError(f"Fallback size {fallback.size} does not match NAIP {rgb.size}")
    alpha = Image.fromarray(bands[3].astype("uint8"), "L")
    feathered = alpha.filter(ImageFilter.GaussianBlur(2))
    composite = Image.composite(rgb, fallback, feathered)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    composite.save(args.output)
    metadata = {
        "purpose": "Blender regional geographic color reference, not a game texture",
        "bounds_epsg26911": bounds,
        "pixel_size": list(rgb.size),
        "naip_coverage": float(np.mean(bands[3] > 0)),
        "naip_source": str(args.naip),
        "naip_sha256": sha256(args.naip),
        "fallback_source": "https://landsat.arcgis.com/arcgis/rest/services/Landsat/MS/ImageServer",
        "fallback_rendering_rule": "Natural Color with DRA",
        "fallback_sha256": sha256(args.fallback),
        "composite_sha256": sha256(args.output),
    }
    args.output.with_suffix(".json").write_text(json.dumps(metadata, indent=2) + "\n")
    print(json.dumps(metadata))


if __name__ == "__main__":
    main()
