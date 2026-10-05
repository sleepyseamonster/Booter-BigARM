"""Generate small deterministic tiling study maps for the five exported rock families.

These are authored procedural starter maps. They are not photogrammetry, baked
high-poly normals, or a finished game texture set.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image

from prepare_region import sha256


FAMILIES = {
    "StratifiedSlab": ((145, 127, 106), 11, 101),
    "FracturedCliffBlock": ((110, 95, 87), 9, 102),
    "AngularDarkBoulder": ((71, 62, 62), 5, 103),
    "TalusShard": ((126, 107, 89), 8, 104),
    "Pebble": ((111, 96, 86), 4, 105),
}


def periodic_noise(size: int, cells: int, rng: np.random.Generator) -> np.ndarray:
    grid = rng.random((cells + 1, cells + 1), dtype=np.float32)
    grid[-1] = grid[0]
    grid[:, -1] = grid[:, 0]
    image = Image.fromarray(np.uint8(grid * 255)).resize((size, size), Image.Resampling.BICUBIC)
    return np.asarray(image, dtype=np.float32) / 255


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--size", type=int, default=512)
    args = parser.parse_args()
    if args.size not in (256, 512, 1024):
        raise ValueError("Study map size must be 256, 512, or 1024")
    args.output.mkdir(parents=True, exist_ok=True)
    inventory = {"size_px": args.size, "families": {}, "version": 1, "note": "Procedural tiling study; normal maps use OpenGL +Y convention"}
    y, x = np.indices((args.size, args.size), dtype=np.float32)
    for name, (base, bands, seed) in FAMILIES.items():
        rng = np.random.default_rng(seed)
        low = periodic_noise(args.size, 8, rng)
        mid = periodic_noise(args.size, 32, rng)
        fine = periodic_noise(args.size, 128, rng)
        strata = np.sin(y / args.size * bands * np.pi * 2 + low * 3.5)
        height = np.clip(0.46 + (low - 0.5) * 0.26 + (mid - 0.5) * 0.22 + (fine - 0.5) * 0.08 + strata * 0.08, 0, 1)
        base_rgb = np.asarray(base, dtype=np.float32)
        brightness = 0.84 + low * 0.24 + strata * 0.045 + (mid - 0.5) * 0.12
        color = np.uint8(np.clip(base_rgb[None, None, :] * brightness[:, :, None], 0, 255))
        roughness = np.uint8(np.clip(205 + (fine - 0.5) * 48 + (mid - 0.5) * 28, 0, 255))
        dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * 2.2
        dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * 2.2
        vector = np.stack((-dx, -dy, np.ones_like(dx)), axis=-1)
        vector /= np.linalg.norm(vector, axis=-1, keepdims=True)
        normal = np.uint8(np.clip((vector * 0.5 + 0.5) * 255, 0, 255))
        paths = {
            "basecolor": args.output / f"{name}_basecolor.png",
            "normal_gl": args.output / f"{name}_normal_gl.png",
            "roughness": args.output / f"{name}_roughness.png",
            "height": args.output / f"{name}_height.png",
        }
        Image.fromarray(color, "RGB").save(paths["basecolor"])
        Image.fromarray(normal, "RGB").save(paths["normal_gl"])
        Image.fromarray(roughness, "L").save(paths["roughness"])
        Image.fromarray(np.uint8(height * 255), "L").save(paths["height"])
        inventory["families"][name] = {key: {"path": str(path.resolve()), "sha256": sha256(path)} for key, path in paths.items()}
    (args.output / "manifest.json").write_text(json.dumps(inventory, indent=2) + "\n")
    print(json.dumps({"families": list(inventory["families"]), "maps": 4 * len(FAMILIES), "size_px": args.size}))


if __name__ == "__main__":
    main()
