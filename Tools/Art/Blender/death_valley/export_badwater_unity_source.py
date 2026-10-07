"""Prepare the measured Badwater height and color tiles for a Unity terrain scene.

This copies the already verified Blender-source RAW tiles; it does not alter
interior DEM samples. The Unity builder makes a narrow edge adjustment only
where a 1 m focus tile touches a 2 m tile, avoiding visible terrain cracks.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
from pathlib import Path

from PIL import Image


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1 << 20), b""):
            digest.update(block)
    return digest.hexdigest()


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--color", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    manifest = json.loads(args.manifest.read_text())
    if manifest["working_crs"] != "EPSG:26911" or manifest["bounds_m"] != [520400, 4006200, 524496, 4010296]:
        raise ValueError("Unexpected geographic source")
    image = Image.open(args.color).convert("RGB")
    if image.size != (2048, 2048):
        raise ValueError("Expected source-aligned 2 m color grid")

    heights = args.output / "Heights"
    colors = args.output / "Colors"
    heights.mkdir(parents=True, exist_ok=True)
    colors.mkdir(parents=True, exist_ok=True)
    lines = ["id,row,col,resolution_m,samples,raw_sha256,color_sha256"]
    for tile in manifest["tiles"]:
        row = tile["row_north_to_south"]
        col = tile["column_west_to_east"]
        focus = "lod1m" in tile
        source = Path(tile["lod1m" if focus else "lod2m"]["path"])
        if sha256(source) != tile["lod1m" if focus else "lod2m"]["sha256"]:
            raise ValueError(f"Source hash changed: {source}")
        name = tile["id"]
        target = heights / f"{name}.bytes"
        shutil.copyfile(source, target)
        color = colors / f"{name}.png"
        image.crop((col * 128, row * 128, (col + 1) * 128, (row + 1) * 128)).save(color)
        lines.append(f"{name},{row},{col},{1 if focus else 2},{257 if focus else 129},{sha256(target)},{sha256(color)}")
    (args.output / "tiles.csv").write_text("\n".join(lines) + "\n")
    provenance = {
        "working_crs": manifest["working_crs"],
        "bounds_m": manifest["bounds_m"],
        "local_center_epsg26911_m": [522448, 4008248],
        "height_encoding": manifest["height_encoding"],
        "terrain_manifest_sha256": sha256(args.manifest),
        "geographic_color_sha256": sha256(args.color),
        "tile_count": len(manifest["tiles"]),
        "focus_tile_count": sum("lod1m" in t for t in manifest["tiles"]),
        "source": "USGS 3DEP 1 m DEM; USGS NAIP color with recorded Landsat fallback",
    }
    (args.output / "provenance.json").write_text(json.dumps(provenance, indent=2) + "\n")
    print(json.dumps({"tiles": len(manifest["tiles"]), "focus_tiles": provenance["focus_tile_count"], "output": str(args.output)}))


if __name__ == "__main__":
    main()
