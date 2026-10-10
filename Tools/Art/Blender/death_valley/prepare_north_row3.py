"""Prepare the third row against immutable full native snapshots and shared model resources."""
import argparse
import json
import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter
import rasterio
from rasterio.enums import Resampling
from rasterio.transform import from_bounds
from rasterio.warp import reproject
from north_row3_contract import ROOT, SURVEY, contract, occupied_cells, sha256
from north_row2_preparation_policy import feather_color
from prepare_expansion import promote, constrain_border

def prepare(step, execution_date, baseline, retained_models, output):
    c = contract(step, execution_date)
    if output.exists():
        raise ValueError("Fresh preparation owner required")
    audit = SURVEY / "SourceAudit01/manifest.json"
    a = json.loads(audit.read_text())
    if a["status"] != "reopened_measured_sources_verified" or a["min_height_m"] < -100 or a["max_height_m"] > 1700:
        raise ValueError("Measured source audit does not support unchanged encoding")
    bm = json.loads(baseline.read_text())
    if bm.get("status") != "native_baseline_refs_verified" or bm["accepted_steps"] != step - 1 or len(bm["tiles"]) != c["retained_count"] or sha256(ROOT / bm["scene"]) != bm["scene_sha256"]:
        raise ValueError("Wrong accepted predecessor baseline")
    model_records = []
    for manifest in retained_models:
        record = json.loads(manifest.read_text())
        for s in record["sections"]:
            for key, hash_key in (("mesh_grids_path", "mesh_grids_sha256"), ("color_path", "color_sha256")):
                if sha256(ROOT / s[key]) != s[hash_key]:
                    raise ValueError("Retained model input changed")
            model_records.append(s)
    cells = [tuple(t["bounds_m"][:2]) for r in model_records for t in r["tiles"]]
    if len(cells) != c["retained_count"] or len(set(cells)) != len(cells) or set(cells) != occupied_cells(step - 1):
        raise ValueError("Missing or overlapping retained modeled cells")
    model_by_id = {s["id"]: s for s in model_records}
    if c["south"] not in model_by_id or (c["west"] is not None and c["west"] not in model_by_id):
        raise ValueError("Missing accepted south or west model")
    constraints = {}
    for s in model_records:
        if s["id"] not in (c["south"], c["west"]):
            continue
        with np.load(ROOT / s["mesh_grids_path"]) as grids:
            for t in s["tiles"]:
                e, n = t["bounds_m"][:2]
                h = grids[t["local_id"]]
                points = []
                if s["id"] == c["south"] and n + 256 == c["bounds"][1]:
                    points.extend(((e + x, n + 256), h[0, x]) for x in range(257))
                if s["id"] == c["west"] and e + 256 == c["bounds"][0]:
                    points.extend(((e + 256, n + 256 - r), h[r, -1]) for r in range(257))
                for xy, value in points:
                    if xy in constraints and constraints[xy] != value:
                        raise ValueError("Retained south/west height corner disagrees")
                    constraints[xy] = value
    dem = SURVEY / "Acquisition01/DEM"
    imagery = SURVEY / "ImageryAcquisition02"
    dm = json.loads((dem / "manifest.json").read_text())
    im = json.loads((imagery / "manifest.json").read_text())
    if sha256(dem / "manifest.json") != a["dem_manifest_sha256"] or sha256(imagery / "manifest.json") != a["imagery_manifest_sha256"]:
        raise ValueError("Acquisition identities changed")
    source = next(s for s in dm["sections"] if s["step"] == step)
    if sha256(dem / source["terrain_file"]) != source["terrain_sha256"]:
        raise ValueError("Measured section changed")
    image = next(s for s in im["sections"] if s["step"] == step)
    if sha256(imagery / image["path"]) != image["sha256"]:
        raise ValueError("Measured imagery changed")
    with rasterio.open(imagery / image["path"]) as src:
        rgb = src.read([1, 2, 3]).transpose(1, 2, 0)
    parent = ROOT / "SourceData/Terrain/DeathValley/MacSnapshot2026-10-07/expanded_v2/corridor_40m/prepared/corridor/naip_landsat_composite.png"
    meta = json.loads(parent.with_suffix(".json").read_text())
    old = np.asarray(Image.open(parent).convert("RGB"))
    palette_bounds = list(meta["bounds_epsg26911"])
    palette_spacing = (palette_bounds[3] - palette_bounds[1]) / old.shape[0]
    north_padding = max(0, math.ceil((c["bounds"][3] - palette_bounds[3]) / palette_spacing))
    # The retained 40 m palette ends at N4024200, inside this strip. Extend
    # only its low-frequency colour reference by holding the last covered
    # north row; geometry and detailed imagery still use the measured inputs.
    if north_padding:
        old = np.pad(old, ((north_padding, 0), (0, 0), (0, 0)), mode="edge")
        palette_bounds[3] += north_padding * palette_spacing
    sampled = np.zeros_like(rgb)
    for channel in range(3):
        reproject(old[:, :, channel], sampled[:, :, channel], src_transform=from_bounds(*palette_bounds, old.shape[1], old.shape[0]), src_crs="EPSG:26911", dst_transform=from_bounds(*c["bounds"], 2048, 2048), dst_crs="EPSG:26911", resampling=Resampling.bilinear)
    matched = rgb.astype("float32") + np.asarray(Image.fromarray(sampled).filter(ImageFilter.GaussianBlur(50)), dtype="float32") - np.asarray(Image.fromarray(rgb).filter(ImageFilter.GaussianBlur(50)), dtype="float32")
    south = np.asarray(Image.open(ROOT / model_by_id[c["south"]]["color_path"]).convert("RGB"))[0]
    west = None if c["west"] is None else np.asarray(Image.open(ROOT / model_by_id[c["west"]]["color_path"]).convert("RGB"))[:, -1]
    difference = None if west is None else np.abs(south[0].astype("int16") - west[-1].astype("int16"))
    result, color_proof = feather_color(step, matched, south, west, difference)
    output.mkdir(parents=True)
    grids, tiles, edges = {}, [], {}
    maximum = 0.
    with np.load(dem / source["terrain_file"]) as data:
        coarse = data["regular_2m"]
        if not np.array_equal(data["native_1m"][::2, ::2], coarse):
            raise ValueError("Strict native decimation changed")
        for row in range(16):
            for col in range(16):
                local = f"r{row:02}_c{col:02}"
                e = c["bounds"][0] + col * 256
                n = c["bounds"][3] - (row + 1) * 256
                h = promote(coarse[row * 128:row * 128 + 129, col * 128:col * 128 + 129])
                delta, count = constrain_border(h, e, n, constraints)
                maximum = max(maximum, delta)
                h = promote(h[::2, ::2].copy())
                constrain_border(h, e, n, constraints)
                for r, x in [(r, x) for r in range(257) for x in (0, 256)] + [(r, x) for x in range(1, 256) for r in (0, 256)]:
                    xy = (e + x, n + 256 - r)
                    if xy in edges and edges[xy] != h[r, x]:
                        raise ValueError("Prepared shared edge differs")
                    edges[xy] = h[r, x]
                grids[local] = h
                tiles.append({"id": c["section"] + "/" + local, "section": c["section"], "local_id": local, "geographic_key": f"epsg26911/e{e}/n{n}/size256", "bounds_m": [e, n, e + 256, n + 256], "row": row, "col": col, "source_version": source["terrain_sha256"], "retained_boundary_samples": count})
    budget = float(1.5 * 1800 / 65535 + 4 * np.finfo(np.float32).eps * 1800)
    if maximum > budget:
        raise ValueError(f"New-side retained adjustment exceeds unchanged budget: {maximum} > {budget}")
    mesh = output / (c["section"] + "_mesh_grids.npz")
    np.savez_compressed(mesh, **grids)
    color = output / (c["section"] + "_geographic_color.png")
    Image.fromarray(result).save(color)
    new = {"id": c["section"], "bounds_m": c["bounds"], "mesh_grids_path": mesh.relative_to(ROOT).as_posix(), "mesh_grids_sha256": sha256(mesh), "color_path": color.relative_to(ROOT).as_posix(), "color_sha256": sha256(color), "tiles": tiles}
    record = {"schema_version": 2, "family": "north_row3", "row_step": step, "execution_date": execution_date, "batch": c["batch"], "working_crs": "EPSG:26911", "bounds_m": [499920, 4006200, 524496, 4026680], "unity_origin_m": [522448, 4008248], "baseline_manifest": baseline.relative_to(ROOT).as_posix(), "baseline_manifest_sha256": sha256(baseline), "source_audit_manifest": audit.relative_to(ROOT).as_posix(), "source_audit_manifest_sha256": sha256(audit), "sections": [new], "retained_sections": model_records, "proof": {"retained_chunks": c["retained_count"], "new_chunks": 256, "max_retained_adjustment_m": maximum, "quantization_budget_m": budget, "new_edge_error_m": 0, "retained_rgb_edge_error": 0, "color_corner": color_proof}, "status": "prepared_candidate_requires_native_Blender_and_Unity"}
    record["color_calibration"] = dict(reference_path=parent.relative_to(ROOT).as_posix(), reference_sha256=sha256(parent), metadata_sha256=sha256(parent.with_suffix(".json")), original_bounds_m=meta["bounds_epsg26911"], palette_bounds_m=palette_bounds, north_padding_rows=north_padding, policy="Hold northernmost retained low-frequency palette row beyond its coverage; detailed NAIP and measured terrain remain independently sourced")
    (output / "manifest.json").write_bytes((json.dumps(record, indent=2) + "\n").encode())
    print(json.dumps(record["proof"], indent=2), flush=True)

if __name__ == "__main__":
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--step", type=int)
    p.add_argument("--date", default="2026-10-10")
    p.add_argument("--baseline", type=Path)
    p.add_argument("--retained-model", type=Path, action="append")
    p.add_argument("--output", type=Path)
    a = p.parse_args()
    if a.step is None or a.baseline is None or not a.retained_model:
        p.error("Preparation requires step, baseline and every accepted retained model")
    c = contract(a.step, a.date)
    prepare(a.step, a.date, a.baseline.resolve(), [p.resolve() for p in a.retained_model], a.output.resolve() if a.output else c["data"] / "Prepared01")
