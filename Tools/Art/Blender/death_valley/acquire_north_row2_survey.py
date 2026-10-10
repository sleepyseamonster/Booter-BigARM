"""Bounded official-source audit of all six new northern sections, without Unity writes."""
import argparse
from datetime import datetime, timezone
import json
import math
from pathlib import Path
import re
import urllib.parse
import urllib.request

import numpy as np
from pyproj import Transformer
import rasterio
from rasterio.enums import Resampling
from rasterio.transform import from_origin
from rasterio.warp import reproject
from rasterio.windows import from_bounds, transform as window_transform

from north_row2_contract import CRS, ORIGIN, ROW_BOUNDS, SURVEY, contract, required_products, sha256
from prepare_region import fetch_grid

PROJECT = "CA_FEMAR9Southeast_D24"
SERVICE = "https://imagery.nationalmap.gov/arcgis/rest/services/USGSNAIPImagery/ImageServer"

def download(url):
    req = urllib.request.Request(url, headers={"User-Agent": "BooterBigArm-NorthRow2SourceAudit/1.0"})
    with urllib.request.urlopen(req, timeout=90) as response:
        return response.read()

def source_catalog():
    tr = Transformer.from_crs(26911, 4326, always_xy=True)
    points = [tr.transform(x, y) for x in (ROW_BOUNDS[0], ROW_BOUNDS[2]) for y in (ROW_BOUNDS[1], ROW_BOUNDS[3])]
    bbox = [min(p[0] for p in points), min(p[1] for p in points), max(p[0] for p in points), max(p[1] for p in points)]
    url = "https://tnmaccess.nationalmap.gov/api/v1/products?" + urllib.parse.urlencode({"bbox": ",".join(map(str, bbox)), "datasets": "Digital Elevation Model (DEM) 1 meter", "outputFormat": "JSON", "max": 100})
    raw = download(url)
    catalog = json.loads(raw)
    chosen = {}
    for item in catalog.get("items", []):
        match = re.search(r"USGS_1M_11_(x(?:49|50|51|52)y(?:402|403))_CA_FEMAR9Southeast_D24\.tif$", item.get("downloadURL", ""))
        if match:
            key = match[1]
            if key in chosen:
                raise ValueError("Ambiguous measured source owner: " + key)
            chosen[key] = item
    if set(chosen) != required_products():
        raise ValueError("Missing product quadrants or western sliver: " + str(set(chosen)))
    return url, raw, chosen

def acquire_dem(output):
    if output.exists():
        raise ValueError("Use a fresh survey version; preserve incomplete acquisitions")
    output.mkdir(parents=True)
    url, catalog_raw, chosen = source_catalog()
    (output / "catalog.json").write_bytes(catalog_raw)
    xmin, ymin, xmax, ymax = ROW_BOUNDS
    grid = np.full((4097, 24577), np.nan, dtype="float32")
    records, overlaps, previous = [], [], []
    for key, item in sorted(chosen.items()):
        print(json.dumps({"opening": key}), flush=True)
        with rasterio.Env(GDAL_DISABLE_READDIR_ON_OPEN="EMPTY_DIR", CPL_VSIL_CURL_ALLOWED_EXTENSIONS=".tif", GDAL_HTTP_TIMEOUT="60"):
            with rasterio.open(item["downloadURL"]) as src:
                if src.crs.to_epsg() != 26911 or src.res != (1., 1.) or src.count != 1:
                    raise ValueError("Unexpected native CRS/spacing/bands: " + key)
                bounds = list(src.bounds)
                clip = [max(xmin - 4, src.bounds.left + 1), max(ymin - 4, src.bounds.bottom + 1), min(xmax + 4, src.bounds.right - 1), min(ymax + 4, src.bounds.top - 1)]
                if clip[0] >= clip[2] or clip[1] >= clip[3]:
                    raise ValueError("Catalog owner has no native intersection: " + key)
                win = from_bounds(*clip, src.transform).round_offsets().round_lengths()
                pixels = src.read(1, window=win)
                tr = window_transform(win, src.transform)
                nodata = src.nodata
        window_path = output / (key + "_window.tif")
        with rasterio.open(window_path, "w", driver="GTiff", width=pixels.shape[1], height=pixels.shape[0], count=1, dtype="float32", crs=CRS, transform=tr, nodata=nodata, compress="deflate") as dst:
            dst.write(pixels.astype("float32"), 1)
        metadata_url = f"https://thor-f5.er.usgs.gov/ngtoc/metadata/waf/elevation/1_meter/geotiff/{PROJECT}/USGS_1M_11_{key}_{PROJECT}.xml"
        metadata = download(metadata_url)
        if b"NAVD88" not in metadata or b"bare-earth" not in metadata:
            raise ValueError("Unsupported datum/surface metadata: " + key)
        metadata_path = output / (key + "_metadata.xml")
        metadata_path.write_bytes(metadata)
        # Local target remains a 1 m vertex grid. Interpolation locates integer vertices
        # on the surveyed cell-center raster; strict 2 m decimation happens afterward.
        c0 = max(0, math.ceil(bounds[0] + 1 - xmin))
        c1 = min(xmax - xmin, math.floor(bounds[2] - 1 - xmin)) + 1
        r0 = max(0, math.ceil(ymax - bounds[3] + 1))
        r1 = min(ymax - ymin, math.floor(ymax - bounds[1] - 1)) + 1
        samples = np.full((r1 - r0, c1 - c0), np.nan, dtype="float32")
        target = from_origin(xmin + c0 - .5, ymax - r0 + .5, 1, 1)
        reproject(pixels, samples, src_transform=tr, src_crs=CRS, src_nodata=nodata, dst_transform=target, dst_crs=CRS, dst_nodata=np.nan, resampling=Resampling.bilinear)
        for old_key, old_r0, old_r1, old_c0, old_c1, old in previous:
            ar0, ar1 = max(r0, old_r0), min(r1, old_r1)
            ac0, ac1 = max(c0, old_c0), min(c1, old_c1)
            if ar0 >= ar1 or ac0 >= ac1:
                continue
            a = samples[ar0 - r0:ar1 - r0, ac0 - c0:ac1 - c0]
            b = old[ar0 - old_r0:ar1 - old_r0, ac0 - old_c0:ac1 - old_c0]
            valid = np.isfinite(a) & np.isfinite(b)
            if not valid.any():
                raise ValueError("No finite overlap between adjacent measured sources")
            delta = np.abs(a[valid].astype("float64") - b[valid].astype("float64"))
            overlaps.append({"owners": [old_key, key], "bounds_m": [xmin + ac0, ymax - ar1 + 1, xmin + ac1 - 1, ymax - ar0], "finite_samples": int(valid.sum()), "target_samples": int(valid.size), "max_difference_m": float(delta.max()), "p99_difference_m": float(np.percentile(delta, 99))})
        domain_x, domain_y = map(int, re.fullmatch(r"x(\d+)y(\d+)", key).groups())
        east = xmin + np.arange(c0, c1)
        north = ymax - np.arange(r0, r1)
        owns = ((north >= (domain_y - 1) * 10000) & (north < domain_y * 10000))[:, None] & ((east >= domain_x * 10000) & (east < (domain_x + 1) * 10000))[None, :]
        if not owns.any() or not np.isfinite(samples[owns]).all():
            raise ValueError("Missing finite 1 m samples inside owned domain: " + key)
        part = grid[r0:r1, c0:c1]
        if np.isfinite(part[owns]).any():
            raise ValueError("Competing sample owners")
        part[owns] = samples[owns]
        records.append({"id": key, "url": item["downloadURL"], "source_bounds_m": bounds, "resolution_m": 1, "window_file": window_path.name, "window_sha256": sha256(window_path), "window_shape": list(pixels.shape), "window_transform": list(tr)[:6], "metadata_url": metadata_url, "metadata_file": metadata_path.name, "metadata_sha256": sha256(metadata_path), "vertical_datum": "NAVD88 metres", "surface": "bare-earth", "owned_samples": int(owns.sum()), "owned_min_m": float(samples[owns].min()), "owned_max_m": float(samples[owns].max())})
        previous.append((key, r0, r1, c0, c1, samples))
        print(json.dumps({"acquired": key, "shape": list(pixels.shape), "owned_min_m": records[-1]["owned_min_m"], "owned_max_m": records[-1]["owned_max_m"]}), flush=True)
        del pixels, owns, part
    missing = int((~np.isfinite(grid)).sum())
    if missing:
        raise ValueError(f"Incomplete 1 m strip coverage: {missing} samples")
    sections = []
    for step in range(1, 7):
        c = contract(step)
        offset = c["bounds"][0] - xmin
        native = grid[:, offset:offset + 4097]
        file = output / (c["section"] + "_terrain.npz")
        np.savez_compressed(file, native_1m=native, regular_2m=native[::2, ::2])
        sections.append({"step": step, "id": c["section"], "bounds_m": c["bounds"], "terrain_file": file.name, "terrain_sha256": sha256(file), "native_shape": [4097, 4097], "control_shape": [2049, 2049], "min_height_m": float(native.min()), "max_height_m": float(native.max()), "fits_retained_height_range": bool(native.min() >= -100 and native.max() <= 1700)})
        print(json.dumps({"section_range": sections[-1]}), flush=True)
    fits = all(s["fits_retained_height_range"] for s in sections)
    receipt = {"schema_version": 1, "family": "north_row2", "status": "source_candidate_requires_Blender_and_Unity" if fits else "blocked_elevation_range_requires_author_decision", "acquired_utc": datetime.now(timezone.utc).isoformat(), "working_crs": CRS, "bounds_m": list(ROW_BOUNDS), "unity_origin_m": list(ORIGIN), "catalog_request": url, "catalog_sha256": sha256(output / "catalog.json"), "source_owner_rule": "Half-open 10000 m domains on BOTH axes; x=floor(easting/10000), y=floor(northing/10000)+1; no blending", "sampling": "bilinear native 1 m vertex preparation; strict 2 m decimation", "sources": records, "overlaps": overlaps, "sections": sections, "nodata_count": missing, "min_height_m": float(grid.min()), "max_height_m": float(grid.max()), "fits_retained_height_range": fits, "height_encoding": {"minimum_m": -100, "range_m": 1800}}
    (output / "manifest.json").write_bytes((json.dumps(receipt, indent=2) + "\n").encode("utf-8"))
    print(json.dumps({"dem_complete": str(output), "status": receipt["status"], "min_m": receipt["min_height_m"], "max_m": receipt["max_height_m"]}), flush=True)

def acquire_imagery(output):
    if output.exists():
        raise ValueError("Fresh imagery survey version required")
    output.mkdir(parents=True)
    service = download(SERVICE + "?f=pjson")
    (output / "service.json").write_bytes(service)
    params = {"f": "json", "geometry": json.dumps({"xmin": ROW_BOUNDS[0], "ymin": ROW_BOUNDS[1], "xmax": ROW_BOUNDS[2], "ymax": ROW_BOUNDS[3], "spatialReference": {"wkid": 26911}}), "geometryType": "esriGeometryEnvelope", "inSR": 26911, "spatialRel": "esriSpatialRelIntersects", "outFields": "*", "returnGeometry": "true", "outSR": 26911}
    url = SERVICE + "/query?" + urllib.parse.urlencode(params)
    raw = download(url)
    catalog = json.loads(raw)
    if catalog.get("error") or catalog.get("exceededTransferLimit") or not catalog.get("features"):
        raise ValueError("Incomplete imagery catalog")
    (output / "imagery_catalog.json").write_bytes(raw)
    records = []
    for step in range(1, 7):
        c = contract(step)
        rec = fetch_grid(SERVICE, output / (c["section"] + "_naip_2m.tif"), tuple(c["bounds"]), 2, 26911, vertices=False, elevation=False)
        with rasterio.open(rec["path"]) as src:
            pixels = src.read(masked=True)
            if src.crs.to_epsg() != 26911 or src.width != 2048 or src.height != 2048 or src.count not in (3, 4) or np.ma.getmaskarray(pixels).any():
                raise ValueError("Imagery dimensions/CRS/bands/NoData failed")
        rec.update(section=c["section"], step=step, bounds_m=c["bounds"])
        for key in ("path", "service_response"):
            rec[key] = Path(rec[key]).name
        records.append(rec)
        print(json.dumps({"imagery_acquired": c["section"]}), flush=True)
    receipt = {"schema_version": 1, "family": "north_row2", "bounds_m": list(ROW_BOUNDS), "service_metadata_sha256": sha256(output / "service.json"), "imagery_catalog_request": url, "imagery_catalog_sha256": sha256(output / "imagery_catalog.json"), "sections": records}
    (output / "manifest.json").write_bytes((json.dumps(receipt, indent=2) + "\n").encode("utf-8"))

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--phase", choices=("dem", "imagery"), required=True)
    p.add_argument("--output", type=Path)
    a = p.parse_args()
    output = a.output.resolve() if a.output else SURVEY / "Acquisition01" / ("DEM" if a.phase == "dem" else "Imagery")
    (acquire_dem if a.phase == "dem" else acquire_imagery)(output)

if __name__ == "__main__":
    main()
