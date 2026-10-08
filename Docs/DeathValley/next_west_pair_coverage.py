"""Publish eight-section coverage only against fresh full production readback.

Prior manifests remain source records. Their older scene proofs cannot validate
the expanded scene; the newest proof and protected-file hashes cover that scene.
"""
import json
from pathlib import Path
import re

import numpy as np
from expansion_coverage import digest, owned_file

ASSET_ROOT = "Assets/_Project/Art/Terrain/NextWestPair2026-10-08"
PRIOR_BATCHES = (
    ("Assets/_Project/Art/Terrain/WestNorthNorthwest2026-10-07", {"west", "north", "northwest"}, 768),
    ("Assets/_Project/Art/Terrain/WestPair2026-10-08", {"west_outer", "northwest_outer"}, 512),
    (ASSET_ROOT, {"west_next", "northwest_next"}, 512),
)


def append_verified_next_west_pair(root, scene, text, retained_tiles):
    root = Path(root).resolve()
    source = root / ASSET_ROOT / "Source"
    manifest = source / "manifest.json"
    m = json.loads(manifest.read_text(encoding="utf-8"))
    if (m["schema_version"] != 1 or m["bounds_m"] != [508112, 4006200, 524496, 4014392]
            or m["unity_origin_m"] != [522448, 4008248] or m["new_tile_count"] != 512):
        raise ValueError("Unsupported eight-section manifest")
    proof_path = source / "unity_production_validation.json"
    proof = json.loads(proof_path.read_text(encoding="utf-8"))
    expected = {"status": "validated_full_grid_and_collider_samples",
                "scene": "Assets/_Project/Scenes/Production/GreaterWasteland.unity",
                "scene_sha256": digest(scene), "manifest_sha256": digest(manifest),
                "terrains": 2048, "retained": 1536, "edges": 4000,
                "outer_edges": 192, "collider_samples": 51200}
    if any(proof.get(key) != value for key, value in expected.items()):
        raise ValueError("Missing or stale eight-section production proof")
    for file in m["protected_files"]:
        if digest(owned_file(root, root, file["path"])) != file["sha256"]:
            raise ValueError("Retained source changed after validation")

    cells = {}
    if len(m["new_tiles"]) != 512 or len(m["retained_tiles"]) != 1536:
        raise ValueError("Incomplete eight-section source records")
    for t in m["new_tiles"] + m["retained_tiles"]:
        render = owned_file(root, source, t["render_file"])
        if digest(render) != t["render_sha256"]:
            raise ValueError("Readback source changed")
        a = np.fromfile(render, dtype="<f4").reshape(257, 257)
        east = t["position"][0] + 522448
        north = t["position"][2] + 4008248
        if (not np.isfinite(a).all() or a.min() < 0 or a.max() > 1
                or east != int(east) or north != int(north)
                or (east, north) in cells
                or t["geographic_key"] != f"epsg26911/e{int(east)}/n{int(north)}/size256"
                or t["bounds_m"] != [east, north, east + 256, north + 256]
                or east not in range(508112, 524496, 256)
                or north not in range(4006200, 4014392, 256)):
            raise ValueError("Invalid or duplicate geographic source")
        cells[(east, north)] = a

    tiles = list(retained_tiles)
    keys = {t["geographic_key"] for t in tiles}
    if len(tiles) != 256 or len(keys) != 256 or not keys.issubset(
            {f"epsg26911/e{int(e)}/n{int(n)}/size256" for e, n in cells}):
        raise ValueError("Invalid original retained coverage")
    paths = []
    guids = set()
    for owner, allowed, count in PRIOR_BATCHES:
        base = root / owner / "Source"
        batch = m if owner == ASSET_ROOT else json.loads((base / "manifest.json").read_text(encoding="utf-8"))
        records = batch["new_tiles"]
        if batch["schema_version"] != 1 or batch["new_tile_count"] != count or len(records) != count:
            raise ValueError("Incomplete prior source manifest")
        section_counts = {section: 0 for section in allowed}
        for t in records:
            if t["section"] not in allowed or t["geographic_key"] in keys:
                raise ValueError("Duplicate or unexpected section")
            e, n = t["bounds_m"][:2]
            if t["geographic_key"] != f"epsg26911/e{e}/n{n}/size256" or (e, n) not in cells:
                raise ValueError("Section absent from current readback")
            keys.add(t["geographic_key"])
            section_counts[t["section"]] += 1
            for file, h in (("height_file", "height_sha256"), ("color_file", "color_sha256"), ("render_file", "render_sha256")):
                if digest(owned_file(root, base, t[file])) != t[h]:
                    raise ValueError("Section source changed")
            asset = owned_file(root, root, t["data_path"])
            if not asset.is_relative_to(root / owner / "TerrainData"):
                raise ValueError("Terrain ownership escaped")
            guid = re.search(r"^guid: ([0-9a-f]{32})$", asset.with_name(asset.name + ".meta").read_text(encoding="utf-8"), re.M)
            if not asset.is_file() or not guid or guid[1] in guids or guid[1] not in text:
                raise ValueError("Missing or duplicated TerrainData reference")
            guids.add(guid[1])
            paths.append(asset)
            ident = t["local_id"]
            if not re.fullmatch(r"r(?:0[0-9]|1[0-5])_c(?:0[0-9]|1[0-5])", ident):
                raise ValueError("Invalid section-local address")
            r, c = int(ident[1:3]), int(ident[5:7])
            tiles.append({"id": t["section"] + "/" + ident, "legacy_id": ident,
                          "geographic_key": t["geographic_key"], "section": t["section"],
                          "quarter": ("N" if r < 8 else "S") + ("W" if c < 8 else "E"),
                          "source_version": t["source_version"], "bounds_m": t["bounds_m"],
                          "source_spacing_m": 2, "source_samples": 129, "render_samples": 257,
                          "height_source": (base / t["height_file"]).relative_to(root).as_posix(),
                          "height_sha256": t["height_sha256"],
                          "color_source": (base / t["color_file"]).relative_to(root).as_posix(),
                          "color_sha256": t["color_sha256"],
                          "integrity": "source_hashes_and_current_Unity_production_readback_verified"})
        if any(value != 256 for value in section_counts.values()):
            raise ValueError("Incomplete terrain section")
    edges = 0
    for (e, n), a in cells.items():
        for cell, edge, index in (((e + 256, n), a[:, -1], (slice(None), 0)),
                                  ((e, n + 256), a[0, :], (-1, slice(None)))):
            if cell in cells:
                if not np.array_equal(edge, cells[cell][index]):
                    raise ValueError("Normalized seam changed")
                edges += 1
    if len(tiles) != 2048 or len(paths) != 1792 or len(cells) != 2048 or edges != 4000:
        raise ValueError("Incomplete eight-section coverage")
    return tiles, paths, {"height_files": 2048, "color_files": 2048,
                          "shared_edges_checked": 4000, "max_encoded_edge_difference": 0,
                          "unity_readback_proof_sha256": digest(proof_path),
                          "limits": "Current full native readback and sampled terrain collision; Player performance and actor traversal remain user-owned"}
