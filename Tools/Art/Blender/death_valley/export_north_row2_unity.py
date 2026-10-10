"""Export reopened new meshes, retaining exact immutable native snapshot references."""
import argparse
import json
from pathlib import Path
import shutil
import numpy as np
from PIL import Image
from north_row2_contract import ROOT, SURVEY, contract, occupied_cells, sha256, validate_geographic_records
from north_row2_native_references import read_native_reference

SHARED_OWNER = "Assets/_Project/Art/Terrain/NorthRow2Baseline2026-10-09"

def verify_proof(file, manifest, c, count, stride, section):
    proof = json.loads(file.read_text(encoding="utf-8"))
    blend = Path(proof["blend_file"]).resolve()
    export = (file.parent / proof["export_file"]).resolve()
    if proof["status"] != "native_reopened_mesh_verified" or proof["chunks"] != count or proof["interior_spacing_m"] != stride or proof["perimeter_spacing_m"] != 1 or proof["section"] != section or proof["manifest_sha256"] != sha256(manifest) or not blend.is_relative_to(c["art"]) or not export.is_relative_to(file.parent) or sha256(blend) != proof["blend_sha256"] or sha256(export) != proof["export_sha256"] or proof["max_mesh_sample_error_m"] > .00025 or proof["max_mesh_edge_difference_m"] != 0 or proof["minimum_normal_z"] <= 0:
        raise ValueError("Changed or invalid native reopen proof")
    if section == "combined" and (proof.get("schema_version") != 2 or proof.get("family") != "north_row2" or proof.get("export_encoding") != "4m controls plus all 1m border vertices; no dense 257-square review duplication"):
        raise ValueError("Compact combined review representation must be explicit")
    return proof

def export(step, date, manifest, output, shared_output, checkpoint):
    c = contract(step, date)
    if output.exists() or (step == 1 and (shared_output is None or shared_output.exists())):
        raise ValueError("Fresh native export owners required")
    m = json.loads(manifest.read_text(encoding="utf-8"))
    if m.get("family") != "north_row2" or m.get("row_step") != step or m.get("batch") != c["batch"]:
        raise ValueError("Wrong prepared row step")
    baseline = ROOT / m["baseline_manifest"]
    bm = json.loads(baseline.read_text(encoding="utf-8"))
    if sha256(baseline) != m["baseline_manifest_sha256"] or bm["accepted_steps"] != step - 1 or sha256(ROOT / bm["scene"]) != bm["scene_sha256"]:
        raise ValueError("Current saved predecessor changed")
    validate_geographic_records(step - 1, [dict(t, bounds_m=[int(t["position"][0]) + 522448, int(t["position"][2]) + 4008248, int(t["position"][0]) + 522704, int(t["position"][2]) + 4008504]) for t in bm["tiles"]])
    evidence = json.loads(checkpoint.read_text(encoding="utf-8"))
    if evidence["status"] != "native_Blender_authoring_reopen_and_five_view_gate_passed" or evidence["step"] != step or evidence["prepared_manifest_sha256"] != sha256(manifest) or len(evidence["visual_views"]) != 5:
        raise ValueError("Five inspected source views must precede export")
    for view in evidence["visual_views"]:
        if not view["inspected"] or sha256(ROOT / view["path"]) != view["sha256"]:
            raise ValueError("Visual proof changed")
    pilot = verify_proof(manifest.parent / "pilot_blender_proof.json", manifest, c, 4 if step == 1 else 8, 2, "pilot")
    section = verify_proof(manifest.parent / (c["section"] + "_blender_proof.json"), manifest, c, 256, 2, c["section"])
    combined = verify_proof(manifest.parent / "combined_blender_proof.json", manifest, c, c["total"], 4, "combined")
    inventory_file = ROOT / bm["protected_inventory_path"]
    if sha256(inventory_file) != bm["protected_inventory_sha256"]:
        raise ValueError("Protected inventory changed")
    inventory = json.loads(inventory_file.read_text(encoding="utf-8"))
    # Scene bytes have a separate predecessor/preservation contract. Metadata stays protected.
    inventory.pop(bm["scene"], None)
    for path, expected in inventory.items():
        if sha256(ROOT / path) != expected:
            raise ValueError("Retained protected file changed: " + path)
    old, cells, constraints = [], {}, {}
    owners = sorted({t["data_path"].split("/TerrainData/")[0] for t in bm["tiles"]})
    if step == 1:
        shared_output.mkdir(parents=True)
    output.mkdir(parents=True)
    for t in bm["tiles"]:
        ref = dict(t["native_reference"])
        if "local_snapshot_file" in ref:
            if step != 1:
                raise ValueError("Later native baselines must reference imported immutable snapshots")
            source = SURVEY / "ReferenceAudit01/NativeSnapshot" / ref["local_snapshot_file"]
            if sha256(source) != ref["sha256"]:
                raise ValueError("Initial full native snapshot changed")
            target = shared_output / "Source/NativeSnapshot" / source.name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source, target)
            h = np.fromfile(source, dtype="<f4").reshape(257, 257)
            ref = {"path": SHARED_OWNER + "/Source/NativeSnapshot/" + source.name, "sha256": ref["sha256"], "orientation": "south-first", "samples": 257}
        else:
            h = read_native_reference(ROOT, ref, owners + [SHARED_OWNER])
        e, n = int(t["position"][0]) + 522448, int(t["position"][2]) + 4008248
        cells[(e, n)] = h[::-1]
        if n + 256 == c["bounds"][1] and c["bounds"][0] <= e < c["bounds"][2]:
            for x in range(257):
                constraints[(e + x, n + 256)] = h[-1, x]
        if step > 1 and e + 256 == c["bounds"][0] and c["bounds"][1] <= n < c["bounds"][3]:
            for z in range(257):
                xy = (e + 256, n + z)
                if xy in constraints and constraints[xy] != h[z, -1]:
                    raise ValueError("Normalized retained corner disagrees")
                constraints[xy] = h[z, -1]
        old.append({k: t[k] for k in ("geographic_key", "data_path", "data_guid", "data_sha256", "meta_sha256", "position", "size")} | {"bounds_m": [e, n, e + 256, n + 256], "native_reference": ref})
    s = m["sections"][0]
    color = ROOT / s["color_path"]
    if sha256(color) != s["color_sha256"]:
        raise ValueError("Prepared imagery changed")
    image = Image.open(color).convert("RGB")
    new = []
    with np.load(manifest.parent / section["export_file"]) as arrays:
        for t in s["tiles"]:
            e, n = map(int, t["bounds_m"][:2])
            local = t["local_id"]
            h = arrays[c["section"] + "__" + local]
            if h.shape != (257, 257) or not np.isfinite(h).all() or h.min() < -100 or h.max() > 1700:
                raise ValueError("Invalid reopened authoring heights")
            normalized = ((h + np.float32(100)) / np.float32(1800)).astype("<f4")
            if n == c["bounds"][1]:
                for x in range(257):
                    target = constraints[(e + x, n)]
                    if abs(float(h[-1, x]) - float(target * np.float32(1800) - np.float32(100))) > .00025:
                        raise ValueError("Reopened south anchor changed")
                    normalized[-1, x] = target
            if step > 1 and e == c["bounds"][0]:
                for r in range(257):
                    target = constraints[(e, n + 256 - r)]
                    if abs(float(h[r, 0]) - float(target * np.float32(1800) - np.float32(100))) > .00025:
                        raise ValueError("Reopened west anchor changed")
                    normalized[r, 0] = target
            cells[(e, n)] = normalized
            render, heights, rgb = [f"{kind}/{c['section']}/{local}.{ext}" for kind, ext in (("Render", "bytes"), ("Heights", "bytes"), ("Colors", "png"))]
            for file in (render, heights, rgb):
                (output / file).parent.mkdir(parents=True, exist_ok=True)
            normalized.tofile(output / render)
            np.rint(normalized[::2, ::2] * 65535).astype("<u2").tofile(output / heights)
            row, col = t["row"], t["col"]
            image.crop((col * 128, row * 128, (col + 1) * 128, (row + 1) * 128)).save(output / rgb)
            new.append({k: t[k] for k in ("section", "local_id", "geographic_key", "source_version", "bounds_m")} | {"position": [e - 522448, -100, n - 4008248], "size": [256, 1800, 256], "render_file": render, "render_sha256": sha256(output / render), "height_file": heights, "height_sha256": sha256(output / heights), "color_file": rgb, "color_sha256": sha256(output / rgb), "source_spacing_m": 2, "source_samples": 129, "render_samples": 257, "data_path": c["asset"] + f"/TerrainData/{c['section']}/{local}.asset", "layer_path": c["asset"] + f"/TerrainLayers/{c['section']}/{local}.terrainlayer"})
    edges = 0
    for (e, n), a in cells.items():
        for cell, edge, index in (((e + 256, n), a[:, -1], (slice(None), 0)), ((e, n + 256), a[0], (-1, slice(None)))):
            if cell in cells:
                if not np.array_equal(edge, cells[cell][index]):
                    raise ValueError("Exact normalized export join failed")
                edges += 1
    if set(cells) != occupied_cells(step) or edges != c["edges"]:
        raise ValueError("Wrong exported union or join count")
    protected = [{"path": p, "sha256": h} for p, h in sorted(inventory.items())]
    result = {"schema_version": 2, "family": "north_row2", "row_step": step, "execution_date": date, "batch": c["batch"], "section": c["section"], "asset_root": c["asset"], "working_crs": "EPSG:26911", "scene_guid": bm["scene_guid"], "bounds_m": [499920, 4006200, 524496, 4022584], "unity_origin_m": [522448, 4008248], "minimum_m": -100, "range_m": 1800, "tile_count": c["total"], "new_tile_count": 256, "new_tiles": new, "retained_tiles": old, "protected_files": protected, "protected_roots": owners, "shared_snapshot_owner": SHARED_OWNER, "predecessor_scene_sha256": bm["scene_sha256"], "predecessor_manifest": bm["predecessor_manifest"], "predecessor_manifest_sha256": bm["predecessor_manifest_sha256"], "prepared_manifest_sha256": sha256(manifest), "authoring_checkpoint_sha256": sha256(checkpoint), "blender_source_sha256": section["blend_sha256"], "pilot_blend_sha256": pilot["blend_sha256"], "combined_blend_sha256": combined["blend_sha256"], "normalized_shared_edges": edges, "exterior_slots": 320, "color_corner": m["proof"]["color_corner"], "status": "verified_Blender_export_pending_native_import"}
    (output / "manifest.json").write_bytes((json.dumps(result, indent=2) + "\n").encode())
    print(json.dumps({"new": len(new), "retained": len(old), "edges": edges, "retained_payloads_duplicated": 0}), flush=True)

if __name__ == "__main__":
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--step", type=int, required=True)
    p.add_argument("--date", required=True)
    p.add_argument("--manifest", type=Path, required=True)
    p.add_argument("--output", type=Path, required=True)
    p.add_argument("--shared-output", type=Path)
    p.add_argument("--checkpoint", type=Path, required=True)
    a = p.parse_args()
    export(a.step, a.date, a.manifest.resolve(), a.output.resolve(), a.shared_output.resolve() if a.shared_output else None, a.checkpoint.resolve())
