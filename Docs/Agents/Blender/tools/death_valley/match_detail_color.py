"""Match a fine geographic image's broad color to its parent without blurring detail."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
import rasterio
from PIL import Image, ImageFilter
from rasterio.enums import Resampling
from rasterio.transform import from_bounds
from rasterio.warp import reproject


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def record(manifest: Path, region: str) -> dict:
    return json.loads(manifest.read_text())["regions"][region]


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--parent", type=Path, required=True)
    parser.add_argument("--parent-manifest", type=Path, required=True)
    parser.add_argument("--parent-region", required=True)
    parser.add_argument("--fine", type=Path, required=True)
    parser.add_argument("--fine-manifest", type=Path, required=True)
    parser.add_argument("--fine-region", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--blur-metres", type=float, default=100)
    args = parser.parse_args()
    parent_record = record(args.parent_manifest, args.parent_region)
    fine_record = record(args.fine_manifest, args.fine_region)
    pb, fb = parent_record["bounds_m"], fine_record["bounds_m"]
    if not (pb[0] <= fb[0] < fb[2] <= pb[2] and pb[1] <= fb[1] < fb[3] <= pb[3]):
        raise ValueError("Fine image lies outside parent")
    parent = np.asarray(Image.open(args.parent).convert("RGB"), dtype=np.uint8)
    fine = Image.open(args.fine).convert("RGB")
    fine_pixels = np.asarray(fine, dtype=np.uint8)
    if args.blur_metres <= 0:
        raise ValueError("Blur distance must be positive")
    parent_transform = from_bounds(*pb, parent.shape[1], parent.shape[0])
    fine_transform = from_bounds(*fb, fine.width, fine.height)
    sampled = np.empty_like(fine_pixels)
    for channel in range(3):
        reproject(parent[:, :, channel], sampled[:, :, channel],
                  src_transform=parent_transform, src_crs="EPSG:26911",
                  dst_transform=fine_transform, dst_crs="EPSG:26911",
                  resampling=Resampling.bilinear)
    blur_pixels = args.blur_metres / fine_record["imagery_resolution_m"]
    fine_low = np.asarray(fine.filter(ImageFilter.GaussianBlur(blur_pixels)), dtype=np.int16)
    parent_low = np.asarray(Image.fromarray(sampled).filter(ImageFilter.GaussianBlur(blur_pixels)), dtype=np.int16)
    correction = parent_low - fine_low
    corrected = np.clip(fine_pixels.astype(np.int16) + correction, 0, 255).astype(np.uint8)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(corrected, "RGB").save(args.output)
    report = {
        "purpose": "Blender geographic color reference with broad color matched to parent; not a game texture",
        "parent": str(args.parent), "parent_sha256": sha256(args.parent),
        "fine": str(args.fine), "fine_sha256": sha256(args.fine),
        "bounds_epsg26911": fb, "image_size": list(fine.size),
        "blur_metres": args.blur_metres,
        "median_abs_rgb_correction": float(np.median(np.abs(correction))),
        "sha256": sha256(args.output),
    }
    args.output.with_suffix(".json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report))


if __name__ == "__main__":
    main()
