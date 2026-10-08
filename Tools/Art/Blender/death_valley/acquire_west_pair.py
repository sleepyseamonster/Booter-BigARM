"""Acquire the two authored sections west of the accepted 1,024-chunk footprint.

Raw samples and metadata remain immutable. No Blender or Unity acceptance is
implied by acquisition. Existing production inputs are never overwritten.
"""
import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
import urllib.parse

import numpy as np
import rasterio
from rasterio.enums import Resampling
from rasterio.transform import from_origin
from rasterio.warp import reproject
from rasterio.windows import from_bounds, transform as window_transform

from acquire_expansion import download
from prepare_region import sha256, fetch_grid

ROOT = Path(__file__).resolve().parents[4]
BOUNDS = [512208, 4006200, 516304, 4014392]
SECTIONS = {
    "west_outer": [512208, 4006200, 516304, 4010296],
    "northwest_outer": [512208, 4010296, 516304, 4014392],
}


def acquire_dem(output):
    if output.exists():
        raise ValueError("Acquisition requires a fresh directory")
    prior = ROOT / "SourceData/Terrain/DeathValley/WestNorthNorthwest2026-10-07/Acquisition01/DEM"
    manifest = json.loads((prior / "manifest.json").read_text())
    sources = [s for s in manifest["sources"] if s["id"].startswith("x51")]
    if {s["id"] for s in sources} != {"x51y401", "x51y402"}:
        raise ValueError("Expected the two accepted USGS product identities")
    output.mkdir(parents=True)
    grid = np.full((8193, 4097), np.nan, dtype="float32")
    target = from_origin(BOUNDS[0] - .5, BOUNDS[3] + .5, 1, 1)
    norths = BOUNDS[3] - np.arange(8193)
    records = []
    for source in sources:
        with rasterio.Env(GDAL_DISABLE_READDIR_ON_OPEN="EMPTY_DIR", CPL_VSIL_CURL_ALLOWED_EXTENSIONS=".tif", GDAL_HTTP_TIMEOUT="60"):
            with rasterio.open(source["url"]) as src:
                if src.crs.to_epsg() != 26911 or src.res != (1., 1.) or src.count != 1:
                    raise ValueError("Unexpected source CRS, resolution or bands")
                clip = [max(BOUNDS[0]-4, src.bounds.left+1), max(BOUNDS[1]-4, src.bounds.bottom+1),
                        min(BOUNDS[2]+4, src.bounds.right-1), min(BOUNDS[3]+4, src.bounds.top-1)]
                window = from_bounds(*clip, src.transform).round_offsets().round_lengths()
                pixels = src.read(1, window=window)
                transform = window_transform(window, src.transform)
                nodata = src.nodata
                source_bounds = list(src.bounds)
        file = output / (source["id"] + "_window.tif")
        with rasterio.open(file, "w", driver="GTiff", width=pixels.shape[1], height=pixels.shape[0],
                           count=1, dtype="float32", crs="EPSG:26911", transform=transform,
                           nodata=nodata, compress="deflate") as dst:
            dst.write(pixels.astype("float32"), 1)
        metadata = download(source["metadata_url"])
        if b"NAVD88" not in metadata or b"bare-earth" not in metadata:
            raise ValueError("Datum or surface not established")
        meta = output / (source["id"] + "_metadata.xml")
        meta.write_bytes(metadata)
        samples = np.full_like(grid, np.nan)
        reproject(pixels, samples, src_transform=transform, src_crs="EPSG:26911", src_nodata=nodata,
                  dst_transform=target, dst_crs="EPSG:26911", dst_nodata=np.nan, resampling=Resampling.bilinear)
        owns = norths <= 4010000 if source["id"].endswith("401") else norths > 4010000
        if not np.isfinite(samples[owns]).all():
            raise ValueError("NoData in owned source coverage")
        overlap = np.isfinite(grid) & np.isfinite(samples)
        delta = float(np.abs(grid[overlap]-samples[overlap]).max()) if overlap.any() else None
        grid[owns] = samples[owns]
        records.append({"id": source["id"], "url": source["url"], "source_bounds_m": source_bounds,
                        "resolution_m": 1, "window_file": file.name, "window_sha256": sha256(file),
                        "window_transform": list(transform)[:6], "window_shape": list(pixels.shape),
                        "metadata_url": source["metadata_url"], "metadata_file": meta.name,
                        "metadata_sha256": sha256(meta), "vertical_datum": "NAVD88 metres",
                        "surface": "bare-earth", "overlap_max_difference_m": delta})
        print(json.dumps({"acquired": source["id"], "shape": list(pixels.shape)}), flush=True)
    if not np.isfinite(grid).all():
        raise ValueError("Incomplete union coverage")
    sections = []
    for name, bounds in SECTIONS.items():
        row = BOUNDS[3]-bounds[3]
        native = grid[row:row+4097, :]
        file = output / (name + "_terrain.npz")
        np.savez_compressed(file, native_1m=native, regular_2m=native[::2, ::2])
        sections.append({"id": name, "bounds_m": bounds, "terrain_file": file.name,
                         "terrain_sha256": sha256(file), "chunks": 256})
    receipt = {"schema_version": 1, "status": "source_candidate_requires_Blender_and_Unity_validation",
               "acquired_utc": datetime.now(timezone.utc).isoformat(), "working_crs": "EPSG:26911",
               "bounds_m": BOUNDS, "unity_origin_m": [522448, 4008248], "sources": records,
               "source_owner_rule": "n<=4010000 uses x51y401; north uses x51y402",
               "sections": sections, "nodata_count": 0, "min_height_m": float(grid.min()),
               "max_height_m": float(grid.max()), "fits_retained_height_range": bool(grid.min() >= -100 and grid.max() <= 1700)}
    (output / "manifest.json").write_text(json.dumps(receipt, indent=2)+"\n", encoding="utf-8")
    print(json.dumps({k: receipt[k] for k in ("min_height_m", "max_height_m", "fits_retained_height_range")}), flush=True)


def acquire_imagery(output):
    if output.exists():
        raise ValueError("Imagery acquisition requires a fresh directory")
    output.mkdir(parents=True)
    service = "https://imagery.nationalmap.gov/arcgis/rest/services/USGSNAIPImagery/ImageServer"
    metadata = download(service+"?f=pjson")
    (output/"service.json").write_bytes(metadata)
    params = {"f": "json", "geometry": json.dumps({"xmin": BOUNDS[0], "ymin": BOUNDS[1], "xmax": BOUNDS[2], "ymax": BOUNDS[3], "spatialReference": {"wkid": 26911}}),
              "geometryType": "esriGeometryEnvelope", "inSR": 26911, "spatialRel": "esriSpatialRelIntersects",
              "outFields": "*", "returnGeometry": "true", "outSR": 26911}
    url = service+"/query?"+urllib.parse.urlencode(params)
    catalog = download(url)
    if "error" in json.loads(catalog):
        raise ValueError("Imagery catalog request failed")
    (output/"imagery_catalog.json").write_bytes(catalog)
    records = []
    for name, bounds in SECTIONS.items():
        record = fetch_grid(service, output/(name+"_naip_2m.tif"), tuple(bounds), 2, 26911, vertices=False, elevation=False)
        with rasterio.open(record["path"]) as data:
            pixels = data.read(masked=True)
            if np.ma.getmaskarray(pixels).any() or data.count not in (3, 4):
                raise ValueError("Incomplete RGB or RGBN coverage")
        record.update({"section": name, "bounds_m": bounds, "acquired_utc": datetime.now(timezone.utc).isoformat()})
        for key in ("path", "service_response"):
            record[key] = Path(record[key]).name
        records.append(record)
        print(json.dumps({"imagery_acquired": name}), flush=True)
    (output/"manifest.json").write_text(json.dumps({"schema_version": 1, "sections": records,
        "service_metadata_sha256": sha256(output/"service.json"), "imagery_catalog_request": url,
        "imagery_catalog_sha256": sha256(output/"imagery_catalog.json")}, indent=2)+"\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--phase", choices=["dem", "imagery"], default="dem")
    args = parser.parse_args()
    (acquire_dem if args.phase == "dem" else acquire_imagery)(args.output.resolve())


if __name__ == "__main__":
    main()
