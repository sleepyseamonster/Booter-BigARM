#!/usr/bin/env python3
"""Read-only audit of the imported Badwater height and color source tiles."""

import argparse
import csv
import hashlib
import json
import math
import struct
import sys
from array import array
from pathlib import Path


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def png_size(path: Path) -> tuple[int, int]:
    with path.open("rb") as stream:
        header = stream.read(24)
    if header[:8] != b"\x89PNG\r\n\x1a\n" or header[12:16] != b"IHDR":
        raise ValueError(f"Invalid PNG header: {path}")
    return struct.unpack(">II", header[16:24])


def heights(path: Path, samples: int) -> array:
    data = path.read_bytes()
    if len(data) != samples * samples * 2:
        raise ValueError(f"Wrong height byte count: {path}")
    values = array("H")
    values.frombytes(data)
    if sys.byteorder != "little":
        values.byteswap()
    return values


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    source = args.source.resolve()
    provenance = json.loads((source / "provenance.json").read_text())
    groups = {name: {"area": 0.0, "over38": 0.0, "over48": 0.0, "min": math.inf,
                     "max": -math.inf, "steepest": 0.0} for name in ("NW", "NE", "SW", "SE", "ALL")}
    seen = set()
    focus = []
    color_sizes = set()
    with (source / "tiles.csv").open(newline="") as stream:
        rows = list(csv.DictReader(stream))
    for row in rows:
        tile_id = row["id"]
        tile_row, tile_col = int(row["row"]), int(row["col"])
        resolution, samples = int(row["resolution_m"]), int(row["samples"])
        if tile_id != f"r{tile_row:02d}_c{tile_col:02d}" or not (0 <= tile_row < 16 and 0 <= tile_col < 16):
            raise ValueError(f"Invalid tile ID or address: {tile_id}")
        if tile_id in seen or samples != 256 // resolution + 1 or resolution not in (1, 2):
            raise ValueError(f"Duplicate or invalid tile geometry: {tile_id}")
        seen.add(tile_id)
        raw = source / "Heights" / f"{tile_id}.bytes"
        color = source / "Colors" / f"{tile_id}.png"
        if sha256(raw) != row["raw_sha256"] or sha256(color) != row["color_sha256"]:
            raise ValueError(f"Source hash mismatch: {tile_id}")
        color_sizes.add(png_size(color))
        if resolution == 1:
            focus.append(tile_id)
        values = heights(raw, samples)
        quarter = ("N" if tile_row < 8 else "S") + ("W" if tile_col < 8 else "E")
        for group_name in (quarter, "ALL"):
            group = groups[group_name]
            group["min"] = min(group["min"], min(values))
            group["max"] = max(group["max"], max(values))
        # Centered differences approximate surface slope, not a Unity collider normal.
        # Weight by sample area so the four 1 m tiles do not dominate the summary.
        weight = float(resolution * resolution)
        for y in range(1, samples - 1):
            base = y * samples
            for x in range(1, samples - 1):
                i = base + x
                dx = (values[i + 1] - values[i - 1]) * 1800.0 / 65535.0 / (2 * resolution)
                dz = (values[i + samples] - values[i - samples]) * 1800.0 / 65535.0 / (2 * resolution)
                slope = math.degrees(math.atan(math.hypot(dx, dz)))
                for group_name in (quarter, "ALL"):
                    group = groups[group_name]
                    group["area"] += weight
                    group["over38"] += weight if slope > 38 else 0
                    group["over48"] += weight if slope > 48 else 0
                    group["steepest"] = max(group["steepest"], slope)
    if len(seen) != provenance["tile_count"] or len(focus) != provenance["focus_tile_count"]:
        raise ValueError("Tile count differs from provenance")
    if color_sizes != {(128, 128)}:
        raise ValueError(f"Unexpected color dimensions: {color_sizes}")
    report = {
        "source": str(args.source),
        "bounds_epsg26911_m": provenance["bounds_m"],
        "tile_count": len(seen),
        "focus_tiles": sorted(focus),
        "color_tile_pixels": [128, 128],
        "color_meters_per_pixel": 2,
        "height_encoding_m": provenance["height_encoding"],
        "slope_estimate": {
            name: {"area_fraction_over_38_deg": round(group["over38"] / group["area"], 5),
                   "area_fraction_over_48_deg": round(group["over48"] / group["area"], 5),
                   "steepest_sample_deg": round(group["steepest"], 2),
                   "min_height_m": round(-100 + group["min"] * 1800 / 65535, 2),
                   "max_height_m": round(-100 + group["max"] * 1800 / 65535, 2)}
            for name, group in groups.items()
        },
        "limits": "Slope uses centered RAW-height differences on tile interiors; it does not prove collider normals, routes, or traversal.",
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + "\n")
    print(f"Verified {len(seen)} tile hashes; wrote {args.output}")


if __name__ == "__main__":
    main()
