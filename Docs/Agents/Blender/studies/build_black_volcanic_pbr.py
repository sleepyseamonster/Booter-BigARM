"""Bake aligned, edge-matched study PBR maps from the approved black rock image.

Uses Pillow and NumPy. Height/normal/AO are image-derived estimates for visual
authoring, not measured scans; preserve the source image for future replacement.
"""

from pathlib import Path

import numpy as np
from PIL import Image


ROOT = Path(__file__).with_name("textures")
SOURCE = ROOT / "VolcanicBlackBasaltAlbedo_v1.png"
PREFIX = "VolcanicBlackBasalt"
SIZE = 1024


def gaussian_periodic(array, sigma):
    height, width = array.shape
    fy = np.fft.fftfreq(height)[:, None]
    fx = np.fft.fftfreq(width)[None, :]
    kernel = np.exp(-2 * np.pi**2 * sigma**2 * (fx * fx + fy * fy))
    return np.fft.ifft2(np.fft.fft2(array) * kernel).real


def edge_match(array, band=80):
    """Make opposing pixels identical with a wide, smooth wrapped correction."""
    out = array.astype(np.float32).copy()
    height, width = out.shape[:2]
    delta = out[:, 0].copy() - out[:, -1].copy()
    for x in range(band):
        weight = 0.5 * (1 + np.cos(np.pi * x / band))
        out[:, x] -= 0.5 * weight * delta
        out[:, width - 1 - x] += 0.5 * weight * delta
    delta = out[0].copy() - out[-1].copy()
    for y in range(band):
        weight = 0.5 * (1 + np.cos(np.pi * y / band))
        out[y] -= 0.5 * weight * delta
        out[height - 1 - y] += 0.5 * weight * delta
    return out


def seamless_quilt(image):
    """Blend four wrapped crops so center joins and outer borders both agree."""
    height, width = image.shape[:2]
    shifted_x = np.roll(image, width // 2, axis=1)
    shifted_y = np.roll(image, height // 2, axis=0)
    shifted_xy = np.roll(shifted_x, height // 2, axis=0)
    x = np.abs(np.arange(width) - width / 2) / width
    y = np.abs(np.arange(height) - height / 2) / height
    wx = np.where(x < .25, .5 + .5 * np.cos(np.pi * x / .25), 0)[None, :, None]
    wy = np.where(y < .25, .5 + .5 * np.cos(np.pi * y / .25), 0)[:, None, None]
    quilt = (wx * wy * image + wx * (1 - wy) * shifted_y
             + (1 - wx) * wy * shifted_x + (1 - wx) * (1 - wy) * shifted_xy)
    return edge_match(quilt, band=16)


def rgb_png(path, array):
    pixels = np.uint8(np.clip(array * 255 + 0.5, 0, 255))
    pixels[:, -1] = pixels[:, 0]
    pixels[-1] = pixels[0]
    Image.fromarray(pixels, "RGB").save(path)


def gray_png(path, array):
    pixels = np.uint8(np.clip(array * 255 + 0.5, 0, 255))
    pixels[:, -1] = pixels[:, 0]
    pixels[-1] = pixels[0]
    Image.fromarray(pixels, "L").save(path)


def verify_edges(label, array):
    lr = float(np.max(np.abs(array[:, 0] - array[:, -1])))
    tb = float(np.max(np.abs(array[0] - array[-1])))
    if lr > 1 / 255 or tb > 1 / 255:
        raise RuntimeError(f"{label} is not seamless: {lr:.5f} / {tb:.5f}")
    print(f"{label}: opposing-edge maximum {lr:.5f} / {tb:.5f}")


def main():
    image = Image.open(SOURCE).convert("RGB")
    width, height = image.size
    # Square crop avoids stretching the vesicles. A one-kilopixel tile keeps
    # mipmaps predictable for a future PC-game asset export.
    side = min(width, height, SIZE)
    image = image.crop(((width - side) // 2, (height - side) // 2,
                        (width + side) // 2, (height + side) // 2))
    albedo = seamless_quilt(np.asarray(image, dtype=np.float32) / 255)
    albedo = np.clip(albedo, 0, 1)
    verify_edges("Albedo", albedo)

    lum = albedo[..., 0] * .2126 + albedo[..., 1] * .7152 + albedo[..., 2] * .0722
    local = lum - gaussian_periodic(lum, 23)
    heightmap = np.clip(.51 + 2.15 * local + .22 * (lum - lum.mean()), 0, 1)
    heightmap = edge_match(heightmap)
    heightmap = np.clip(heightmap, 0, 1)
    verify_edges("Height", heightmap)

    dx = (np.roll(heightmap, -1, axis=1) - np.roll(heightmap, 1, axis=1)) * .5
    dy = (np.roll(heightmap, -1, axis=0) - np.roll(heightmap, 1, axis=0)) * .5
    normal = np.stack((-dx * 6.0, -dy * 6.0, np.ones_like(dx)), axis=-1)
    normal /= np.linalg.norm(normal, axis=-1, keepdims=True)
    normal = edge_match((normal + 1) * .5)
    verify_edges("Normal", normal)

    broad = gaussian_periodic(heightmap, 14)
    ao = np.clip(.88 + 1.7 * (heightmap - broad), .58, 1)
    ao = np.clip(edge_match(ao), 0, 1)
    verify_edges("AO", ao)

    normalized = (lum - lum.mean()) / max(float(lum.std()), .01)
    roughness = np.clip(.83 - .055 * normalized, .67, .94)
    roughness = np.clip(edge_match(roughness), 0, 1)
    verify_edges("Roughness", roughness)
    metallic = np.zeros_like(roughness)
    verify_edges("Metallic", metallic)

    rgb_png(ROOT / f"{PREFIX}_Albedo.png", albedo)
    rgb_png(ROOT / f"{PREFIX}_NormalGL.png", normal)
    gray_png(ROOT / f"{PREFIX}_Roughness.png", roughness)
    gray_png(ROOT / f"{PREFIX}_AO.png", ao)
    gray_png(ROOT / f"{PREFIX}_Metallic.png", metallic)
    height_pixels = np.uint16(np.clip(heightmap * 65535 + .5, 0, 65535))
    height_pixels[:, -1] = height_pixels[:, 0]
    height_pixels[-1] = height_pixels[0]
    Image.fromarray(height_pixels, "I;16").save(ROOT / f"{PREFIX}_Height16.png")

    # Four repeats are more revealing than a single tile preview.
    tile = Image.open(ROOT / f"{PREFIX}_Albedo.png")
    preview = Image.new("RGB", (SIZE * 2, SIZE * 2))
    for y in range(2):
        for x in range(2):
            preview.paste(tile, (x * SIZE, y * SIZE))
    preview.thumbnail((1200, 1200))
    preview.save(ROOT / f"{PREFIX}_RepeatPreview.png")
    print("Wrote aligned 1024 px black basalt PBR study maps")


if __name__ == "__main__":
    main()
