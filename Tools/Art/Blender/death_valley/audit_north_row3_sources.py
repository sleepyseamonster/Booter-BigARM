"""Reopen acquired data and distinguish product buffer overlap from actual ownership lines."""
import argparse
import json
from pathlib import Path
import numpy as np
import rasterio
from rasterio.enums import Resampling
from rasterio.transform import from_origin, from_bounds
from rasterio.warp import reproject
from north_row3_contract import ROW_BOUNDS, SURVEY, contract, required_products, sha256

def sample_line(file, shape, transform):
    with rasterio.open(file) as src:
        values = src.read(1)
        result = np.full(shape, np.nan, dtype="float32")
        reproject(values, result, src_transform=src.transform, src_crs=src.crs, src_nodata=src.nodata,
                  dst_transform=transform, dst_crs="EPSG:26911", dst_nodata=np.nan, resampling=Resampling.bilinear)
    if not np.isfinite(result).all():
        raise ValueError("Incomplete same-coordinate product ownership line")
    return result

def audit(acquisition, output, imagery=None):
    if output.exists():
        raise ValueError("Fresh source audit version required")
    dem = acquisition / "DEM"
    imagery = imagery if imagery is not None else acquisition / "Imagery"
    dm = json.loads((dem / "manifest.json").read_text())
    im = json.loads((imagery / "manifest.json").read_text())
    if dm.get("family") != "north_row3" or im.get("family") != "north_row3" or tuple(dm["bounds_m"]) != ROW_BOUNDS or tuple(im["bounds_m"]) != ROW_BOUNDS:
        raise ValueError("Unsupported survey identities")
    if {s["id"] for s in dm["sources"]} != required_products() or sum(s["owned_samples"] for s in dm["sources"]) != 4097 * 24577 or dm["nodata_count"] != 0:
        raise ValueError("Incomplete source ownership")
    if sha256(dem / "catalog.json") != dm["catalog_sha256"]:
        raise ValueError("DEM catalog changed")
    for file, key in (("service.json", "service_metadata_sha256"), ("imagery_catalog.json", "imagery_catalog_sha256")):
        if sha256(imagery / file) != im[key]:
            raise ValueError("Imagery provenance changed")
    sources = {}
    for source in dm["sources"]:
        for file, key in (("window_file", "window_sha256"), ("metadata_file", "metadata_sha256")):
            if sha256(dem / source[file]) != source[key]:
                raise ValueError("Measured source bytes changed")
        sources[source["id"]] = source
    sections = []
    previous = None
    for step in range(1, 7):
        c = contract(step)
        rec = next(s for s in dm["sections"] if s["step"] == step)
        if rec["id"] != c["section"] or rec["bounds_m"] != c["bounds"] or sha256(dem / rec["terrain_file"]) != rec["terrain_sha256"]:
            raise ValueError("Changed section identity or height source")
        with np.load(dem / rec["terrain_file"]) as data:
            native, control = data["native_1m"], data["regular_2m"]
            if native.shape != (4097, 4097) or control.shape != (2049, 2049) or not np.isfinite(native).all() or not np.array_equal(native[::2, ::2], control):
                raise ValueError("Native coverage or strict decimation changed")
            if previous is not None and not np.array_equal(previous, native[:, 0]):
                raise ValueError("Shared measured section edge changed")
            previous = native[:, -1].copy()
            minimum, maximum = float(native.min()), float(native.max())
            if minimum != rec["min_height_m"] or maximum != rec["max_height_m"]:
                raise ValueError("Measured extrema changed")
        image = next(r for r in im["sections"] if r["step"] == step)
        if image["section"] != c["section"] or image["bounds_m"] != c["bounds"] or sha256(imagery / image["path"]) != image["sha256"]:
            raise ValueError("Changed imagery identity or bytes")
        with rasterio.open(imagery / image["path"]) as src:
            if src.crs.to_epsg() != 26911 or src.width != 2048 or src.height != 2048 or src.count not in (3, 4) or src.transform != from_bounds(*c["bounds"], 2048, 2048) or np.ma.getmaskarray(src.read(masked=True)).any():
                raise ValueError("Imagery alignment/coverage changed")
        sections.append({**rec, "imagery_sha256": image["sha256"], "finite_native_samples": 4097 * 4097, "strict_decimation_verified": True, "aligned_imagery_verified": True})
    lines = []
    xmin, ymin, xmax, ymax = ROW_BOUNDS
    for x in (50, 51, 52):
        for y in (403,):
            low, high = max(ymin, (y - 1) * 10000), min(ymax, y * 10000 - 1)
            shape = (high - low + 1, 1)
            transform = from_origin(x * 10000 - .5, high + .5, 1, 1)
            a = sample_line(dem / sources[f"x{x - 1}y{y}"]["window_file"], shape, transform)
            b = sample_line(dem / sources[f"x{x}y{y}"]["window_file"], shape, transform)
            delta = np.abs(a.astype("float64") - b.astype("float64"))
            lines.append({"axis": "easting", "coordinate_m": x * 10000, "extent_m": [low, high], "owners": [f"x{x - 1}y{y}", f"x{x}y{y}"], "selected_owner": f"x{x}y{y}", "samples": int(delta.size), "max_difference_m": float(delta.max()), "p99_difference_m": float(np.percentile(delta, 99))})
    result = {"schema_version": 1, "family": "north_row3", "status": "reopened_measured_sources_verified" if dm["fits_retained_height_range"] else "blocked_elevation_range", "dem_manifest_sha256": sha256(dem / "manifest.json"), "imagery_manifest_sha256": sha256(imagery / "manifest.json"), "bounds_m": list(ROW_BOUNDS), "unique_native_samples": 4097 * 24577, "sources": dm["sources"], "sections": sections, "ownership_lines": lines, "buffer_overlaps": dm["overlaps"], "min_height_m": dm["min_height_m"], "max_height_m": dm["max_height_m"], "source_policy": "Each point takes its half-open product-domain owner; overlap/buffer disagreements are measured, not averaged or smoothed. Exact native Unity joins are a later independent gate.", "proof_limit": "Source audit only, not authored mesh, native TerrainData or Player acceptance"}
    output.mkdir(parents=True)
    (output / "manifest.json").write_bytes((json.dumps(result, indent=2) + "\n").encode("utf-8"))
    print(json.dumps({"status": result["status"], "ownership_lines": lines}, indent=2), flush=True)

if __name__ == "__main__":
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--acquisition", type=Path, default=SURVEY / "Acquisition01")
    p.add_argument("--output", type=Path, default=SURVEY / "SourceAudit01")
    p.add_argument("--imagery", type=Path, help="Explicit separately versioned imagery acquisition")
    a = p.parse_args()
    audit(a.acquisition.resolve(), a.output.resolve(), a.imagery.resolve() if a.imagery else None)
