"""Fetch bounded USGS study grids, verify alignment, and create Blender-ready arrays.

Usage:
  python prepare_region.py --config region.json --output '/path/to/data'

The source image services are dynamic and may change. Keep their exact response,
request, timestamp and output hash in the manifest. These outputs are source
studies; shipping material textures require a separate art and provenance pass.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import urllib.parse
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
import rasterio
from PIL import Image


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def request_json(url: str, params: dict[str, object]) -> dict:
    query = urllib.parse.urlencode(params)
    request = urllib.request.Request(f"{url}?{query}", headers={"User-Agent": "BooterBigARM-DeathValleyStudy/1.0"})
    with urllib.request.urlopen(request, timeout=90) as response:
        result = json.load(response)
    if "error" in result or "href" not in result:
        raise RuntimeError(f"Image service export failed: {result}")
    return result


def fetch_grid(
    service: str,
    output: Path,
    bounds: tuple[float, float, float, float],
    resolution: float,
    crs_code: int,
    *,
    vertices: bool,
    elevation: bool,
) -> dict:
    xmin, ymin, xmax, ymax = bounds
    nx = round((xmax - xmin) / resolution)
    ny = round((ymax - ymin) / resolution)
    if nx <= 0 or ny <= 0 or not math.isclose(xmin + nx * resolution, xmax) or not math.isclose(ymin + ny * resolution, ymax):
        raise ValueError("Bounds must be positive and aligned to the requested resolution")
    if vertices:
        half = resolution / 2
        request_bounds = (xmin - half, ymin - half, xmax + half, ymax + half)
        width, height = nx + 1, ny + 1
    else:
        request_bounds = bounds
        width, height = nx, ny
    if width > 4000 or height > 4000:
        raise ValueError("One request exceeds the conservative 4000-pixel service limit; divide into aligned tiles")
    params: dict[str, object] = {
        "bbox": ",".join(str(value) for value in request_bounds),
        "bboxSR": crs_code,
        "imageSR": crs_code,
        "size": f"{width},{height}",
        "format": "tiff",
        "f": "json",
    }
    if elevation:
        params["pixelType"] = "F32"
    output.parent.mkdir(parents=True, exist_ok=True)
    response_path = output.with_suffix(".service.json")
    if not output.exists():
        response = request_json(f"{service}/exportImage", params)
        response_path.write_text(json.dumps({"request": params, "response": response}, indent=2) + "\n")
        temp = output.with_suffix(".download")
        try:
            urllib.request.urlretrieve(response["href"], temp)
            temp.replace(output)
        finally:
            temp.unlink(missing_ok=True)
    else:
        if not response_path.exists():
            raise RuntimeError(f"Cached data has no service response: {output}")
        response = json.loads(response_path.read_text())["response"]
    with rasterio.open(output) as dataset:
        if dataset.crs.to_epsg() != crs_code or dataset.width != width or dataset.height != height:
            raise RuntimeError(f"Unexpected CRS or grid dimensions: {output}")
        expected = request_bounds
        actual = (dataset.bounds.left, dataset.bounds.bottom, dataset.bounds.right, dataset.bounds.top)
        if any(abs(a - b) > 0.01 for a, b in zip(actual, expected)):
            raise RuntimeError(f"Unexpected grid bounds: {output}: {actual}")
        if abs(dataset.res[0] - resolution) > 0.01 or abs(dataset.res[1] - resolution) > 0.01:
            raise RuntimeError(f"Unexpected grid spacing: {output}: {dataset.res}")
        band_count = dataset.count
    return {
        "service": service,
        "request": params,
        "service_response": str(response_path),
        "path": str(output),
        "sha256": sha256(output),
        "bands": band_count,
        "grid_role": "vertices" if vertices else "pixels",
        "width": width,
        "height": height,
    }


def terrain_signals(elevation: np.ndarray, resolution: float) -> tuple[np.ndarray, np.ndarray, np.ndarray, np.ndarray]:
    valid = np.isfinite(elevation) & (elevation > -10000) & (elevation < 10000)
    if not valid.all():
        raise RuntimeError("Elevation has missing or implausible cells; repair the source grid before building a mesh")
    north_gradient, east_gradient = np.gradient(elevation.astype(np.float64), resolution, resolution)
    slope = np.degrees(np.arctan(np.hypot(east_gradient, north_gradient))).astype("float32")
    aspect = (np.degrees(np.arctan2(-east_gradient, north_gradient)) + 360) % 360
    padded = np.pad(elevation, 1, mode="edge")
    windows = [padded[dy:dy + elevation.shape[0], dx:dx + elevation.shape[1]] for dy in range(3) for dx in range(3)]
    ruggedness = (np.maximum.reduce(windows) - np.minimum.reduce(windows)).astype("float32")
    nx = -east_gradient
    ny = -north_gradient
    shade = (0.55 + 0.45 * (0.7 * nx + 0.5 * ny + 0.7) / np.sqrt(nx * nx + ny * ny + 1)).clip(0.28, 1).astype("float32")
    return slope, aspect.astype("float32"), ruggedness, shade


def local_mean(data: np.ndarray, radius: int) -> np.ndarray:
    padded = np.pad(data.astype("float64"), radius, mode="edge")
    summed = np.pad(padded, ((1, 0), (1, 0)), mode="constant").cumsum(0).cumsum(1)
    size = 2 * radius + 1
    return (summed[size:, size:] - summed[:-size, size:] - summed[size:, :-size] + summed[:-size, :-size]) / (size * size)


def landform_masks(elevation: np.ndarray, slope: np.ndarray, ruggedness: np.ndarray, resolution: float) -> dict[str, np.ndarray]:
    # These are art placement hints, not hydrological or traversability truth.
    neighborhood = local_mean(elevation, max(2, round(80 / resolution)))
    position = elevation - neighborhood
    wash = np.clip((-position - 0.5) / 4, 0, 1) * np.clip((16 - slope) / 10, 0, 1)
    fan = np.clip((slope - 2) / 6, 0, 1) * np.clip((18 - slope) / 8, 0, 1) * np.clip((8 - abs(position)) / 8, 0, 1)
    cliff = np.clip((slope - 28) / 15, 0, 1) * np.clip(ruggedness / max(resolution * 0.7, 1), 0, 1)
    talus = np.clip((slope - 12) / 10, 0, 1) * np.clip((32 - slope) / 12, 0, 1)
    return {name: value.astype("float32") for name, value in (("wash", wash), ("fan", fan), ("cliff", cliff), ("talus", talus))}


def build_preview(elevation: np.ndarray, imagery: np.ndarray, shade: np.ndarray, output: Path, color_output: Path) -> float:
    rgb = imagery[:3].transpose(1, 2, 0).astype("float32")
    if rgb.shape[:2] != elevation.shape:
        # Elevation samples include an extra shared-edge vertex in each direction.
        elevation = elevation[:-1, :-1]
        shade = shade[:-1, :-1]
    valid = imagery[3] > 0 if imagery.shape[0] >= 4 else np.any(rgb > 0, axis=-1)
    lo, hi = np.percentile(elevation, [2, 98])
    relief = np.clip((elevation - lo) / max(hi - lo, 1), 0, 1)
    neutral = np.stack((70 + relief * 100, 68 + relief * 100, 65 + relief * 100), axis=-1)
    rgb = np.where(valid[:, :, None], rgb, neutral)
    Image.fromarray(np.clip(rgb, 0, 255).astype("uint8")).save(color_output)
    rendered = np.clip(rgb * shade[:, :, None], 0, 255).astype("uint8")
    output.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(rendered).save(output)
    return float(valid.mean())


def run_region(name: str, region: dict, config: dict, output: Path) -> dict:
    bounds = tuple(region["bounds_m"])
    resolution = float(region["resolution_m"])
    imagery_resolution = float(region.get("imagery_resolution_m", resolution))
    crs_code = int(config["working_crs"].split(":")[1])
    raw = output / "raw" / name
    prepared = output / "prepared" / name
    raw.mkdir(parents=True, exist_ok=True)
    prepared.mkdir(parents=True, exist_ok=True)
    elevation_info = fetch_grid(config["services"]["elevation"], raw / "elevation.tif", bounds, resolution, crs_code, vertices=True, elevation=True)
    imagery_info = fetch_grid(config["services"]["imagery"], raw / "naip_reference.tif", bounds, imagery_resolution, crs_code, vertices=False, elevation=False)
    with rasterio.open(elevation_info["path"]) as dataset:
        elevation = dataset.read(1).astype("float32")
    with rasterio.open(imagery_info["path"]) as dataset:
        imagery = dataset.read()
    slope, aspect, ruggedness, shade = terrain_signals(elevation, resolution)
    masks = landform_masks(elevation, slope, ruggedness, resolution)
    npz_path = prepared / "terrain.npz"
    np.savez_compressed(npz_path, elevation=elevation, slope=slope, aspect=aspect, ruggedness=ruggedness, **masks)
    preview_path = prepared / "hillshade_naip_preview.png"
    color_path = prepared / "naip_color_reference.png"
    if imagery_resolution == resolution:
        coverage = build_preview(elevation, imagery, shade, preview_path, color_path)
    else:
        # Hero imagery is kept at higher resolution for material/color inspection.
        factor = round(resolution / imagery_resolution)
        coverage = build_preview(
            elevation[:-1, :-1].repeat(factor, 0).repeat(factor, 1),
            imagery,
            shade[:-1, :-1].repeat(factor, 0).repeat(factor, 1),
            preview_path,
            color_path,
        )
    info = {
        "name": name,
        "bounds_m": bounds,
        "resolution_m": resolution,
        "imagery_resolution_m": imagery_resolution,
        "vertex_shape": list(elevation.shape),
        "elevation_range_m": [float(elevation.min()), float(elevation.max())],
        "slope_range_deg": [float(slope.min()), float(slope.max())],
        "mask_coverage": {name: float(np.mean(value > 0.5)) for name, value in masks.items()},
        "mask_note": "heuristic art placement only; not surveyed watercourse or navigation geometry",
        "imagery_valid_fraction": coverage,
        "elevation": elevation_info,
        "imagery": imagery_info,
        "prepared_npz": str(npz_path),
        "prepared_sha256": sha256(npz_path),
        "preview_png": str(preview_path),
        "color_reference_png": str(color_path),
    }
    (prepared / "manifest.json").write_text(json.dumps(info, indent=2) + "\n")
    return info


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--config", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--region", choices=("overview", "hero", "both"), default="both")
    args = parser.parse_args()
    config = json.loads(args.config.read_text())
    output = args.output.expanduser().resolve()
    output.mkdir(parents=True, exist_ok=True)
    regions = ("overview", "hero") if args.region == "both" else (args.region,)
    results = {name: run_region(name, config[name], config, output) for name in regions}
    manifest = {
        "name": config["name"],
        "generated_utc": datetime.now(timezone.utc).isoformat(),
        "working_crs": config["working_crs"],
        "vertical_units": config["vertical_units"],
        "vertical_datum_note": config["vertical_datum"],
        "source_references": config["source_references"],
        "regions": results,
    }
    (output / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    print(json.dumps({name: {key: value for key, value in info.items() if key in ("vertex_shape", "elevation_range_m", "imagery_valid_fraction", "preview_png")} for name, info in results.items()}, indent=2))


if __name__ == "__main__":
    main()
