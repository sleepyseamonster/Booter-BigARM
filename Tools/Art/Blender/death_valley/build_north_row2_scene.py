"""Second-row native authoring adapter with compact combined-review reopen exports."""
import argparse
import json
from pathlib import Path
import sys
import time
import numpy as np
sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_expansion_scene as native
from north_row2_contract import ROOT, contract, occupied_cells, sha256, validate_geographic_records

C = None
ARGS = None
NEW = None

def arguments():
    global C, ARGS, NEW
    if ARGS is not None:
        return ARGS
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--step", type=int, required=True)
    p.add_argument("--date", required=True)
    p.add_argument("--manifest", type=Path, required=True)
    p.add_argument("--mode", choices=("build", "verify", "render"), required=True)
    p.add_argument("--section", required=True)
    p.add_argument("--output", type=Path, required=True)
    p.add_argument("--stride", type=int, choices=(2, 4), default=2)
    p.add_argument("--view", choices=("overhead", "oblique", "junction"), default="oblique")
    ARGS = p.parse_args(sys.argv[sys.argv.index("--") + 1:])
    C = contract(ARGS.step, ARGS.date)
    NEW = C["section"]
    if ARGS.section not in (NEW, "combined", "pilot") or ARGS.stride != (4 if ARGS.section == "combined" else 2):
        p.error("Explicit second-row section and correct 2 m authoring/4 m review spacing required")
    return ARGS

def resource(relative, expected_hash):
    path = (ROOT / relative).resolve()
    if not path.is_relative_to(ROOT / "SourceData/Terrain/DeathValley") or sha256(path) != expected_hash:
        raise ValueError("Changed or escaped measured/model resource")
    return path

def load(manifest):
    m = json.loads(manifest.read_text())
    if m.get("schema_version") != 2 or m.get("family") != "north_row2" or m["row_step"] != C["step"] or m["batch"] != C["batch"] or m["unity_origin_m"] != [522448, 4008248] or m["bounds_m"] != [499920, 4006200, 524496, 4022584]:
        raise ValueError("Wrong second-row authoring contract")
    resource(m["baseline_manifest"], m["baseline_manifest_sha256"])
    resource(m["source_audit_manifest"], m["source_audit_manifest_sha256"])
    if len(m["sections"]) != 1 or m["sections"][0]["id"] != NEW or len(m["retained_sections"]) != 18 + C["step"] - 1:
        raise ValueError("Wrong new/retained section ownership")
    grids, colors, tiles = {}, {}, []
    for s in m["retained_sections"] + m["sections"]:
        if s["id"] in grids or len(s["tiles"]) != 256:
            raise ValueError("Duplicate or incomplete modeled section")
        grids[s["id"]] = np.load(resource(s["mesh_grids_path"], s["mesh_grids_sha256"]))
        colors[s["id"]] = resource(s["color_path"], s["color_sha256"])
        tiles.extend(dict(t, id=s["id"] + "/" + t["local_id"], section=s["id"], section_bounds=s["bounds_m"]) for t in s["tiles"])
    cells = [tuple(t["bounds_m"][:2]) for t in tiles]
    if len(cells) != C["total"] or len(set(cells)) != len(cells) or set(cells) != occupied_cells(C["step"]) or len({t["geographic_key"] for t in tiles}) != C["total"]:
        raise ValueError("Wrong combined occupied cells or geographic identity")
    validate_geographic_records(C["step"], tiles)
    return m, grids, colors, tiles

def selected(tiles, section):
    if section == "combined":
        return tiles
    if section == "pilot":
        if C["step"] == 1:
            ids = {NEW + "/r15_c00", NEW + "/r15_c01", C["south"] + "/r00_c00", C["south"] + "/r00_c01"}
            result = [t for t in tiles if t["id"] in ids]
            if len(result) != 4:
                raise ValueError("Missing one-border, four-mesh pilot")
        else:
            ids = {NEW + "/r15_c00", NEW + "/r14_c00", NEW + "/r15_c08", C["south"] + "/r00_c00", C["south"] + "/r00_c08", C["west"] + "/r15_c15", C["west"] + "/r14_c15"}
            southwest = (C["bounds"][0] - 256, C["bounds"][1] - 256)
            result = [t for t in tiles if t["id"] in ids or tuple(t["bounds_m"][:2]) == southwest]
            if len(result) != 8:
                raise ValueError("Missing two-border, eight-mesh pilot")
        return result
    return [t for t in tiles if t["section"] == section]

original_mesh = native.mesh
original_camera = native.setup_camera

def mesh(*args):
    obj = original_mesh(*args)
    obj["retained_reference"] = args[0]["section"] != NEW
    obj.hide_select = bool(obj["retained_reference"])
    return obj

def setup_camera(view, tiles):
    if view != "junction" or C["step"] == 1:
        return original_camera("oblique" if view == "junction" else view, tiles)
    e, n = C["bounds"][:2]
    corner = [t for t in tiles if t["bounds_m"][0] in (e - 256, e) and t["bounds_m"][1] in (n - 256, n)]
    if len(corner) != 4:
        raise ValueError("Missing geographic southwest junction")
    keys = {t["geographic_key"] for t in corner}
    excluded = [(o, o["geographic_key"]) for o in native.bpy.data.objects if o.type == "MESH" and "geographic_key" in o and o["geographic_key"] not in keys]
    for o, key in excluded:
        del o["geographic_key"]
    try:
        original_camera("oblique", corner)
    finally:
        for o, key in excluded:
            o["geographic_key"] = key

def verify_combined(a):
    start = time.monotonic()
    m, grids, colors, tiles = load(a.manifest.resolve())
    out = a.output.resolve()
    if out.exists() or out.with_suffix(".npz").exists():
        raise ValueError("Fresh combined reopen proof required")
    scene = native.bpy.context.scene
    if scene.get("expansion_manifest_sha256") != sha256(a.manifest) or scene.get("build_section") != "combined" or scene.get("interior_spacing_m") != 4 or scene.get("perimeter_spacing_m") != 1 or scene.unit_settings.system != "METRIC" or scene.unit_settings.scale_length != 1:
        raise ValueError("Saved combined scene units/contract changed")
    objects = {o["geographic_key"]: o for o in native.bpy.data.objects if o.type == "MESH" and "geographic_key" in o}
    if len(objects) != C["total"] or sum(o.type == "MESH" for o in native.bpy.data.objects) != C["total"]:
        raise ValueError("Missing/duplicate combined mesh")
    maximum, edge_error, normal = 0., 0., 1.
    edges, exports = {}, {}
    for index, t in enumerate(tiles):
        obj = objects[t["geographic_key"]]
        if obj.modifiers or any(abs(v - 1) > 1e-7 for v in obj.scale) or any(abs(v) > 1e-7 for v in obj.rotation_euler) or obj["section"] != t["section"] or obj["local_id"] != t["local_id"] or tuple(obj.location) != (t["bounds_m"][0] - 522448, t["bounds_m"][1] - 4008248, 0) or obj["interior_spacing_m"] != 4:
            raise ValueError("Changed combined mesh identity/transform/spacing")
        expected = grids[t["section"]][t["local_id"]]
        samples = {}
        for v in obj.data.vertices:
            world = obj.matrix_world @ v.co
            x = int(round(world.x + 522448 - t["bounds_m"][0]))
            r = 256 - int(round(world.y + 4008248 - t["bounds_m"][1]))
            if not 0 <= x <= 256 or not 0 <= r <= 256 or abs(world.x - (t["bounds_m"][0] + x - 522448)) > 1e-5 or abs(world.y - (t["bounds_m"][3] - r - 4008248)) > 1e-5 or (r, x) in samples:
                raise ValueError("Off-grid or duplicate review vertex")
            maximum = max(maximum, abs(float(world.z) - float(expected[r, x])))
            samples[(r, x)] = np.float32(world.z)
            if r in (0, 256) or x in (0, 256):
                xy = (t["bounds_m"][0] + x, t["bounds_m"][3] - r)
                if xy in edges:
                    edge_error = max(edge_error, abs(float(edges[xy]) - float(world.z)))
                else:
                    edges[xy] = np.float32(world.z)
        prefix = t["id"].replace("/", "__")
        exports[prefix + "__controls"] = np.array([[samples[(r, x)] for x in range(0, 257, 4)] for r in range(0, 257, 4)], dtype="float32")
        exports[prefix + "__borders"] = np.array([[samples[(0, x)] for x in range(257)], [samples[(256, x)] for x in range(257)], [samples[(r, 0)] for r in range(257)], [samples[(r, 256)] for r in range(257)]], dtype="float32")
        normal = min(normal, min(p.normal.z for p in obj.data.polygons))
        if (index + 1) % 512 == 0:
            print("COMBINED_MESHES_REOPEN_VERIFIED", index + 1, flush=True)
    if maximum > .00025 or edge_error != 0 or normal <= 0 or any(i.source == "FILE" and not i.packed_file for i in native.bpy.data.images):
        raise ValueError("Combined source/edge/normal/packed-image gate failed")
    export = out.with_suffix(".npz")
    np.savez_compressed(export, **exports)
    proof = {"schema_version": 2, "family": "north_row2", "section": "combined", "chunks": C["total"], "blender_version": native.bpy.app.version_string, "manifest_sha256": sha256(a.manifest), "blend_file": native.bpy.data.filepath, "blend_sha256": sha256(Path(native.bpy.data.filepath)), "export_file": export.name, "export_sha256": sha256(export), "export_encoding": "4m controls plus all 1m border vertices; no dense 257-square review duplication", "max_mesh_sample_error_m": maximum, "max_mesh_edge_difference_m": edge_error, "minimum_normal_z": normal, "interior_spacing_m": 4, "perimeter_spacing_m": 1, "status": "native_reopened_mesh_verified", "seconds": time.monotonic() - start}
    out.write_bytes((json.dumps(proof, indent=2) + "\n").encode())
    print(json.dumps(proof), flush=True)

if __name__ == "__main__":
    a = arguments()
    native.arguments = arguments
    native.load = load
    native.selected = selected
    native.mesh = mesh
    native.setup_camera = setup_camera
    if a.mode == "verify" and a.section == "combined":
        verify_combined(a)
    else:
        native.main()
