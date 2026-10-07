"""Assemble unchanged geographic pixels into one terrain color/mipmap domain.

Run with the repository's pinned terrain Python environment. --check is read-only.
This does not rebuild TerrainData, geometry, or source imagery.
"""
import argparse
import csv
import hashlib
from pathlib import Path
import re
import uuid

import numpy as np
from PIL import Image


ROOT = Path(__file__).resolve().parents[4]
ART = ROOT / "Assets/_Project/Art/Terrain/BadwaterFourSlices"
ATLAS = ART / "GeographicColor.png"
EXTENT = 4096


def assemble():
    records = list(csv.DictReader((ART / "Source/tiles.csv").open()))
    if len(records) != 256:
        raise ValueError("Expected 256 geographic source records")
    image = Image.new("RGB", (2048, 2048))
    seen = set()
    for record in records:
        row, col = int(record["row"]), int(record["col"])
        name = f"r{row:02}_c{col:02}"
        if not (0 <= row < 16 and 0 <= col < 16) or record["id"] != name or name in seen:
            raise ValueError("Invalid geographic source identity")
        seen.add(name)
        path = ART / "Source/Colors" / (name + ".png")
        if hashlib.sha256(path.read_bytes()).hexdigest() != record["color_sha256"]:
            raise ValueError("Source color hash mismatch: " + name)
        with Image.open(path) as tile:
            if tile.size != (128, 128):
                raise ValueError("Unexpected source dimensions: " + name)
            image.paste(tile.convert("RGB"), (col * 128, row * 128))
    return image


def mapped_layer(text, guid, row, col):
    text, references = re.subn(r"(m_DiffuseTexture: \{fileID: 2800000, guid: )\w+", r"\g<1>" + guid, text)
    text, sizes = re.subn(r"m_TileSize: \{x: \d+, y: \d+\}", f"m_TileSize: {{x: {EXTENT}, y: {EXTENT}}}", text)
    text, offsets = re.subn(r"m_TileOffset: \{x: -?\d+, y: -?\d+\}",
                           f"m_TileOffset: {{x: {col * 256}, y: {(15 - row) * 256}}}", text)
    if (references, sizes, offsets) != (1, 1, 1):
        raise ValueError("Unexpected layer serialization")
    return text


def run(check):
    image = assemble()
    meta = ATLAS.with_suffix(".png.meta")
    if check:
        with Image.open(ATLAS) as saved:
            if saved.mode != "RGB" or saved.size != image.size or saved.tobytes() != image.tobytes():
                raise ValueError("Atlas pixels differ from geographic sources")
    else:
        image.save(ATLAS)
    if not meta.exists():
        if check:
            raise ValueError("Missing atlas importer")
        template = (ART / "Source/Colors/r00_c00.png.meta").read_text()
        meta.write_text(re.sub(r"(?m)^guid: \w+$", "guid: " + uuid.uuid4().hex, template))
    importer = meta.read_text()
    guid = re.search(r"(?m)^guid: (\w+)$", importer)[1]
    if len(guid) != 32 or any(f"    wrap{axis}: 1" not in importer for axis in "UVW") or "enableMipMap: 1" not in importer:
        raise ValueError("Atlas requires Clamp and mipmaps")
    for row in range(16):
        for col in range(16):
            path = ART / "TerrainLayers" / f"r{row:02}_c{col:02}.terrainlayer"
            with path.open(newline="") as stream:
                old = stream.read()
            mapped = mapped_layer(old, guid, row, col)
            if check:
                if old != mapped:
                    raise ValueError("Layer not mapped to its geographic region: " + path.name)
            else:
                with path.open("w", newline="") as stream:
                    stream.write(mapped)
    # Both sides of every edge, and all four contributors to every corner,
    # must produce exactly the same normalized atlas coordinate.
    edges = corners = 0
    for row in range(16):
        for col in range(16):
            origin = np.array([col, 15 - row], dtype=float) / 16
            if col < 15:
                assert np.array_equal(origin + [1 / 16, 0], np.array([col + 1, 15 - row]) / 16)
                edges += 1
            if row < 15:
                assert np.array_equal(origin, np.array([col, 14 - row]) / 16 + [0, 1 / 16])
                edges += 1
            if row < 15 and col < 15:
                uv = origin + [1 / 16, 0]
                assert np.array_equal(uv, np.array([col + 1, 15 - row]) / 16)
                assert np.array_equal(uv, np.array([col, 14 - row]) / 16 + [1 / 16, 1 / 16])
                assert np.array_equal(uv, np.array([col + 1, 14 - row]) / 16 + [0, 1 / 16])
                corners += 1
    print(f"{'Verified' if check else 'Built'} pixel-identical 2048x2048 atlas; 256 layers, {edges} edges, {corners} four-way corners share coordinates at every mip level.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true")
    run(parser.parse_args().check)
