"""Fetch a recorded, aligned Landsat color reference for an overview manifest.

This is geographic review color for Blender, not a shippable game texture.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import urllib.parse
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image


SERVICE = "https://landsat.arcgis.com/arcgis/rest/services/Landsat/MS/ImageServer"
RENDERING_RULE = {"rasterFunction": "Natural Color with DRA"}


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--region", default="overview")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    region = json.loads(args.manifest.read_text())["regions"][args.region]
    xmin, ymin, xmax, ymax = region["bounds_m"]
    resolution = region["imagery_resolution_m"]
    width = round((xmax - xmin) / resolution)
    height = round((ymax - ymin) / resolution)
    terrain_resolution = region["resolution_m"]
    if ((xmax - xmin) / terrain_resolution + 1,
            (ymax - ymin) / terrain_resolution + 1) != tuple(reversed(region["vertex_shape"])):
        raise ValueError("Terrain grid shape does not match bounds and resolution")
    params = {
        "bbox": f"{xmin},{ymin},{xmax},{ymax}",
        "bboxSR": 26911,
        "imageSR": 26911,
        "size": f"{width},{height}",
        "format": "png32",
        "renderingRule": json.dumps(RENDERING_RULE),
        "f": "json",
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    url = f"{SERVICE}/exportImage?{urllib.parse.urlencode(params)}"
    request = urllib.request.Request(url, headers={"User-Agent": "BooterBigARM-DeathValleyStudy/1.0"})
    with urllib.request.urlopen(request, timeout=120) as response:
        result = json.load(response)
    if "error" in result or "href" not in result:
        raise RuntimeError(f"Landsat export failed: {result}")
    temp = args.output.with_suffix(".download")
    try:
        urllib.request.urlretrieve(result["href"], temp)
        with Image.open(temp) as image:
            if image.size != (width, height):
                raise RuntimeError(f"Unexpected Landsat image size: {image.size}")
            alpha = image.convert("RGBA").getchannel("A")
            if alpha.getextrema()[0] == 0:
                raise RuntimeError("Landsat image contains transparent source gaps")
            image.convert("RGB").save(args.output)
    finally:
        temp.unlink(missing_ok=True)
    metadata = {
        "purpose": "Blender geographic review color, not a game texture",
        "acquired_utc": datetime.now(timezone.utc).isoformat(),
        "service": SERVICE,
        "attribution": "Esri, USGS, NASA",
        "request": params,
        "response": result,
        "bounds_epsg26911": [xmin, ymin, xmax, ymax],
        "image_size": [width, height],
        "sha256": sha256(args.output),
    }
    args.output.with_suffix(".json").write_text(json.dumps(metadata, indent=2) + "\n")
    print(json.dumps({"image_size": metadata["image_size"], "sha256": metadata["sha256"]}))


if __name__ == "__main__":
    main()
