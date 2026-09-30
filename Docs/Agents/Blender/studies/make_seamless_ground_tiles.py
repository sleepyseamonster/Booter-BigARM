"""Bake repeatable ground tiles from the generated source images.

Run with Blender --background --python. The horizontal and vertical repairs are
done in separate passes so the opposite boundaries stay periodic without any
runtime overlapping image samples in the terrain material.
"""

from pathlib import Path

import bpy
import numpy as np


ROOT = Path(__file__).resolve().parent / "textures"
PAIRS = (
    ("BadlandsRustDirtGenerated.png", "BadlandsRustDirtTile.png"),
    ("BadlandsSparseShaleGenerated.png", "BadlandsSparseShaleTile.png"),
    ("BadlandsDenseShaleGenerated.png", "BadlandsDenseShaleTile.png"),
)


def edge_weight(length):
    # The untouched central 78% retains its full original stone detail.
    distance = np.abs(np.linspace(-1.0, 1.0, length, dtype=np.float32))
    t = np.clip((distance - 0.78) / 0.20, 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def make_periodic(pixels):
    height, width, _ = pixels.shape
    shifted_x = np.roll(pixels, width // 2, axis=1)
    x_weight = edge_weight(width)[None, :, None]
    result = pixels * (1.0 - x_weight) + shifted_x * x_weight
    edge = 0.5 * (result[:, 0, :] + result[:, -1, :])
    result[:, 0, :], result[:, -1, :] = edge, edge

    shifted_y = np.roll(result, height // 2, axis=0)
    y_weight = edge_weight(height)[:, None, None]
    result = result * (1.0 - y_weight) + shifted_y * y_weight
    edge = 0.5 * (result[0, :, :] + result[-1, :, :])
    result[0, :, :], result[-1, :, :] = edge, edge
    return result


def main():
    for source_name, output_name in PAIRS:
        source = bpy.data.images.load(str(ROOT / "sources" / source_name), check_existing=False)
        width, height = source.size
        rgba = np.empty(width * height * 4, dtype=np.float32)
        source.pixels.foreach_get(rgba)
        periodic = make_periodic(rgba.reshape(height, width, 4))
        output = bpy.data.images.new(output_name, width=width, height=height, alpha=True)
        output.pixels.foreach_set(periodic.ravel())
        output.filepath_raw = str(ROOT / output_name)
        output.file_format = "PNG"
        output.save()
        horizontal = float(np.abs(periodic[:, 0, :] - periodic[:, -1, :]).max())
        vertical = float(np.abs(periodic[0, :, :] - periodic[-1, :, :]).max())
        print(output_name, width, height, "edge error", horizontal, vertical)
        bpy.data.images.remove(source)
        bpy.data.images.remove(output)


if __name__ == "__main__":
    main()
