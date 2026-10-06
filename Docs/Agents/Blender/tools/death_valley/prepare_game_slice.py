"""Clip a native USGS 1 m DEM into aligned 256 m game-scale terrain tiles.

The source URL and exact remote window are recorded. The 2 m grid is derived
by decimating the 1 m grid, so shared samples agree exactly. Neither source
height grid is artistically altered; the Blender scene adds transition vertices
to coarse neighbors where 1 m and 2 m tile edges meet.
RAW exports are a documented handoff candidate, not a validated Unity import.
"""

from __future__ import annotations

import argparse
import json
import math
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
import rasterio
from PIL import Image
from rasterio.enums import Resampling
from rasterio.warp import reproject
from rasterio.windows import from_bounds, transform as window_transform
from rasterio.transform import from_origin

from prepare_region import fetch_grid, sha256, terrain_signals, landform_masks


def read_native_grid(config: dict, output: Path) -> tuple[np.ndarray, dict]:
    xmin, ymin, xmax, ymax = config["bounds_m"]
    side = int((xmax - xmin) / config["source_resolution_m"])
    if side != int((ymax - ymin) / config["source_resolution_m"]):
        raise ValueError("The slice must be square")
    target_transform = from_origin(xmin - 0.5, ymax + 0.5, 1, 1)
    source_url = config["source_url"]
    with rasterio.Env(GDAL_DISABLE_READDIR_ON_OPEN="EMPTY_DIR", CPL_VSIL_CURL_ALLOWED_EXTENSIONS=".tif"):
        with rasterio.open(source_url) as source:
            if source.crs.to_epsg() != 26911 or source.res != (1.0, 1.0):
                raise RuntimeError(f"Unexpected 1 m source grid: {source.crs}, {source.res}")
            if not (source.bounds.left < xmin and source.bounds.bottom < ymin and
                    source.bounds.right > xmax and source.bounds.top > ymax):
                raise RuntimeError("Native source does not cover the complete slice")
            window = from_bounds(xmin - 3, ymin - 3, xmax + 3, ymax + 3, source.transform)
            window = window.round_offsets().round_lengths()
            source_clip = source.read(1, window=window)
            clip_transform = window_transform(window, source.transform)
            source_nodata = source.nodata
            source_metadata = {
                "url": source_url,
                "crs": str(source.crs),
                "pixel_size_m": list(source.res),
                "source_bounds_m": list(source.bounds),
                "source_nodata": source_nodata,
                "window": [int(window.col_off), int(window.row_off), int(window.width), int(window.height)],
            }
    raw_path = output / "source_window.tif"
    with rasterio.open(raw_path, "w", driver="GTiff", width=source_clip.shape[1],
                       height=source_clip.shape[0], count=1, dtype="float32",
                       crs="EPSG:26911", transform=clip_transform, nodata=source_nodata,
                       compress="deflate") as target:
        target.write(source_clip.astype("float32"), 1)
    native = np.full((side + 1, side + 1), np.nan, dtype="float32")
    reproject(source_clip, native, src_transform=clip_transform, src_crs="EPSG:26911",
              src_nodata=source_nodata, dst_transform=target_transform,
              dst_crs="EPSG:26911", dst_nodata=np.nan, resampling=Resampling.bilinear)
    if not np.isfinite(native).all() or native.min() < -1000 or native.max() > 6000:
        raise RuntimeError("Native DEM has missing or implausible samples")
    source_metadata["source_window_path"] = str(raw_path)
    source_metadata["source_window_sha256"] = sha256(raw_path)
    metadata_path = output / "usgs_product_metadata.xml"
    if not metadata_path.exists():
        request = urllib.request.Request(config["source_metadata_url"],
                                         headers={"User-Agent": "BooterBigARM-DeathValleyStudy/1.0"})
        with urllib.request.urlopen(request, timeout=90) as response:
            metadata_path.write_bytes(response.read())
    metadata = metadata_path.read_text()
    if "bare-earth" not in metadata or "NAVD88" not in metadata:
        raise RuntimeError("USGS metadata lacks the expected bare-earth or vertical datum description")
    source_metadata["metadata_url"] = config["source_metadata_url"]
    source_metadata["metadata_path"] = str(metadata_path)
    source_metadata["metadata_sha256"] = sha256(metadata_path)
    source_metadata["native_grid_shape"] = list(native.shape)
    source_metadata["native_grid_transform"] = list(target_transform)[:6]
    return native, source_metadata


def save_raw(tile: np.ndarray, low: float, high: float, path: Path) -> dict:
    normalized = np.clip((tile.astype("float64") - low) / (high - low), 0, 1)
    quantized = np.rint(normalized * 65535).astype("<u2")
    quantized.tofile(path)
    return {"path": str(path), "sha256": sha256(path), "shape": list(tile.shape),
            "min_m": float(tile.min()), "max_m": float(tile.max())}


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--config", type=Path, required=True)
    parser.add_argument("--region-config", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    config = json.loads(args.config.read_text())
    region_config = json.loads(args.region_config.read_text())
    if config["working_crs"] != region_config["working_crs"]:
        raise ValueError("Slice and regional CRS disagree")
    xmin, ymin, xmax, ymax = config["bounds_m"]
    size = int(config["chunk_size_m"])
    if xmax - xmin != ymax - ymin or (xmax - xmin) % size:
        raise ValueError("Bounds must be square and divisible by chunk size")
    output = args.output.expanduser().resolve()
    output.mkdir(parents=True, exist_ok=True)
    native, source = read_native_grid(config, output)
    regular = native[::2, ::2].copy()
    if regular.shape != (1025, 1025):
        raise RuntimeError(f"Unexpected 2 m grid: {regular.shape}")
    np.savez_compressed(output / "terrain.npz", native_1m=native, regular_2m=regular)
    naip = fetch_grid(region_config["services"]["imagery"], output / "naip_2m.tif",
                      tuple(config["bounds_m"]), 2, 26911, vertices=False, elevation=False)
    slope, _, ruggedness, _ = terrain_signals(regular, 2)
    rough = landform_masks(regular, slope, ruggedness, 2)
    # These are provisional visual material hints, not geologic or route truth.
    bedrock = np.clip((slope - 18) / 24, 0, 1)
    talus = np.clip(rough["talus"] * 0.8 + np.clip((slope - 8) / 20, 0, 0.5), 0, 1)
    playa = np.clip((5 - slope) / 4, 0, 1) * np.clip((-35 - regular) / 35, 0, 1)
    masks = np.stack((bedrock[:-1, :-1], talus[:-1, :-1], playa[:-1, :-1]), axis=-1)
    mask_path = output / "surface_masks_rgb.png"
    Image.fromarray(np.rint(masks * 255).astype("uint8"), "RGB").save(mask_path)
    np.savez_compressed(output / "surface_signals.npz", slope=slope, ruggedness=ruggedness,
                        bedrock=bedrock, talus=talus, playa=playa, wash=rough["wash"])
    count = (xmax - xmin) // size
    core_rows = set(config["high_detail_rows"])
    core_cols = set(config["high_detail_columns"])
    if any(index < 0 or index >= count for index in core_rows | core_cols):
        raise ValueError("High detail indices outside the chunk grid")
    low = math.floor(float(native.min()) / 100) * 100
    high = math.ceil(float(native.max()) / 100) * 100
    raw_dir = output / "height_raw"
    raw_dir.mkdir(exist_ok=True)
    tiles = []
    for row in range(count):
        for col in range(count):
            r, c = row * 256, col * 256
            coarse = regular[r//2:r//2+129, c//2:c//2+129]
            name = f"r{row:02d}_c{col:02d}"
            record = {
                "id": name,
                "row_north_to_south": row,
                "column_west_to_east": col,
                "bounds_m": [xmin+c, ymax-r-256, xmin+c+256, ymax-r],
                "lod2m": save_raw(coarse, low, high, raw_dir / f"{name}_2m_129.raw"),
            }
            if row in core_rows and col in core_cols:
                fine = native[r:r+257, c:c+257]
                if not np.array_equal(fine[::2, ::2], coarse):
                    raise RuntimeError(f"Shared 1 m and 2 m samples differ in {name}")
                record["lod1m"] = save_raw(fine, low, high, raw_dir / f"{name}_1m_257.raw")
            tiles.append(record)
    manifest = {
        "name": config["name"],
        "generated_utc": datetime.now(timezone.utc).isoformat(),
        "working_crs": config["working_crs"],
        "bounds_m": config["bounds_m"],
        "chunk_size_m": size,
        "grid_shape_1m": list(native.shape),
        "grid_shape_2m": list(regular.shape),
        "source": source,
        "naip": naip,
        "terrain_npz": str(output / "terrain.npz"),
        "terrain_sha256": sha256(output / "terrain.npz"),
        "surface_masks_rgb": str(mask_path),
        "surface_masks_sha256": sha256(mask_path),
        "surface_signals_npz": str(output / "surface_signals.npz"),
        "height_encoding": {"dtype": "little-endian uint16", "row_order": "north to south",
                            "column_order": "west to east", "min_m": low, "max_m": high,
                            "decode": "min_m + raw / 65535 * (max_m - min_m)",
                            "warning": "Unity orientation and collision are unverified; inspect import before use"},
        "tiles": tiles,
        "status": "Blender terrain and material study; Unity import/streaming not implemented",
    }
    path = output / "manifest.json"
    path.write_text(json.dumps(manifest, indent=2) + "\n")
    print(json.dumps({"tile_count": len(tiles), "high_detail_tiles": sum("lod1m" in t for t in tiles),
                      "elevation_range_m": [float(native.min()), float(native.max())],
                      "raw_range_m": [low, high], "naip": naip["path"]}))


if __name__ == "__main__":
    main()
