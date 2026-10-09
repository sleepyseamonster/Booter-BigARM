"""Northern-row inventory requires current native proof, not export presence."""
import json
from pathlib import Path
import tempfile
import unittest

import numpy as np
from north_row_coverage import (LEGACY_BATCHES, ROW_TOTALS, ROW_EDGES, ROW_EASTINGS,
                                asset_root, occupied_cells, source_batches, append_verified_north_row, digest)

ASSET_ROOT = asset_root(504016)
PRIOR_BATCHES = LEGACY_BATCHES + ((ASSET_ROOT, {"north_row_e504016"}, 256),)


class NorthRowCoverageTests(unittest.TestCase):
    def test_five_steps_have_exact_occupied_cells_seams_and_outer_slots(self):
        previous = occupied_cells(3584) - {(e,n) for e in range(504016,508112,256)
                                         for n in range(4014392,4018488,256)}
        for total, expected_edges, east in zip(ROW_TOTALS, ROW_EDGES, ROW_EASTINGS):
            with self.subTest(total=total):
                cells = occupied_cells(total)
                self.assertEqual(len(cells), total)
                self.assertTrue(previous.issubset(cells))
                self.assertEqual(cells-previous, {(e,n) for e in range(east,east+4096,256)
                                                 for n in range(4014392,4018488,256)})
                edges = sum((e+256,n) in cells for e,n in cells)+sum((e,n+256) in cells for e,n in cells)
                outside = sum((e+de,n+dn) not in cells for e,n in cells
                              for de,dn in ((256,0),(-256,0),(0,256),(0,-256)))
                self.assertEqual((edges,outside), (expected_edges,288))
                self.assertFalse(any(e>=east+4096 and n>=4014392 for e,n in cells))
                batches = source_batches(total)
                self.assertEqual(batches[:len(LEGACY_BATCHES)], LEGACY_BATCHES)
                self.assertEqual([b[0] for b in batches[len(LEGACY_BATCHES):]],
                                 [asset_root(e) for e in ROW_EASTINGS if e<=east])
                self.assertEqual(sum(b[2] for b in batches)+256,total)
                previous = cells
        self.assertEqual(len(previous), 96*48)
        for total in (3328,3585,4864):
            with self.assertRaises(ValueError): occupied_cells(total)

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(dir="D:/BooterBigArmValidation/Temp")
        self.root = Path(self.temp.name)
        self.source = self.root / ASSET_ROOT / "Source"
        self.source.mkdir(parents=True)
        self.scene = self.root / "Assets/_Project/Scenes/Production/GreaterWasteland.unity"
        self.scene.parent.mkdir(parents=True)
        self.scene.write_text("saved production scene\n")
        self.manifest = {"schema_version": 1, "bounds_m": [499920, 4006200, 524496, 4018488],
                         "unity_origin_m": [522448, 4008248], "new_tile_count": 256,
                         "protected_files": []}
        self.save_manifest()

    def tearDown(self):
        self.temp.cleanup()

    def save_manifest(self):
        (self.source / "manifest.json").write_text(json.dumps(self.manifest))

    def proof(self):
        return {"status": "validated_full_grid_and_collider_samples",
                "scene": "Assets/_Project/Scenes/Production/GreaterWasteland.unity",
                "scene_sha256": digest(self.scene), "manifest_sha256": digest(self.source / "manifest.json"),
                "terrains": 3584, "retained": 3328, "edges": 7024, "outer_edges": 288,
                "collider_samples": 89600}

    def save_proof(self, proof=None):
        (self.source / "unity_production_validation.json").write_text(json.dumps(proof or self.proof()))

    def test_export_only_or_old_production_proof_cannot_promote_coverage(self):
        with self.assertRaises(FileNotFoundError):
            append_verified_north_row(self.root, self.scene, "", [], 3584)
        for key, value in (("scene_sha256", "0" * 64), ("manifest_sha256", "0" * 64),
                           ("terrains", 3328), ("retained", 3072), ("edges", 6512),
                           ("outer_edges", 256), ("collider_samples", 83200)):
            with self.subTest(key=key):
                proof = self.proof()
                proof[key] = value
                self.save_proof(proof)
                with self.assertRaisesRegex(ValueError, "Missing or stale"):
                    append_verified_north_row(self.root, self.scene, "", [], 3584)

    def test_shifted_origin_rejected_before_proof(self):
        self.manifest["unity_origin_m"][0] -= 4096
        self.save_manifest()
        with self.assertRaisesRegex(ValueError, "Unsupported"):
            append_verified_north_row(self.root, self.scene, "", [], 3584)

    def test_each_step_requires_its_own_latest_production_proof(self):
        for total, east, edges in zip(ROW_TOTALS, ROW_EASTINGS, ROW_EDGES):
            with self.subTest(total=total):
                source = self.root / asset_root(east) / "Source"
                source.mkdir(parents=True, exist_ok=True)
                manifest = source / "manifest.json"
                manifest.write_text(json.dumps(self.manifest))
                stale = self.proof() | {"terrains": total, "retained": total-256,
                                        "edges": edges-512, "collider_samples": total*25,
                                        "manifest_sha256": digest(manifest)}
                (source / "unity_production_validation.json").write_text(json.dumps(stale))
                with self.assertRaisesRegex(ValueError, "Missing or stale"):
                    append_verified_north_row(self.root, self.scene, "", [], total)

    def test_protected_prior_source_change_rejected(self):
        protected = self.root / "retained.txt"
        protected.write_text("original")
        self.manifest["protected_files"] = [{"path": "retained.txt", "sha256": digest(protected)}]
        self.save_manifest()
        self.save_proof()
        protected.write_text("changed")
        with self.assertRaisesRegex(ValueError, "Retained source changed"):
            append_verified_north_row(self.root, self.scene, "", [], 3584)

    def full_grid_fixture(self):
        batches = {owner: [] for owner, _, _ in PRIOR_BATCHES}
        for owner, _, _ in PRIOR_BATCHES:
            base = self.root / owner / "Source"
            base.mkdir(parents=True, exist_ok=True)
            np.full((257, 257), .5, dtype="<f4").tofile(base / "grid.bytes")
            (base / "height.bytes").write_bytes(b"height")
            (base / "color.png").write_bytes(b"color")
        retained = []
        coverage = []
        guids = []
        for row in range(48):
            for col in range(96):
                if row >= 32 and col >= 32:
                    continue
                e, n = 499920 + col * 256, 4006200 + row * 256
                key = f"epsg26911/e{e}/n{n}/size256"
                t = {"geographic_key": key, "bounds_m": [e, n, e + 256, n + 256],
                     "position": [e - 522448, -100, n - 4008248], "render_file": "grid.bytes",
                     "render_sha256": digest(self.source / "grid.bytes")}
                if col >= 80 and row < 16:
                    retained.append(t)
                    coverage.append({"geographic_key": key})
                    continue
                if row >= 32 and col >= 16:
                    owner, section = ASSET_ROOT, "north_row_e504016"
                elif row >= 32:
                    owner, section = PRIOR_BATCHES[5][0], "north_ridge"
                elif col < 16:
                    owner, section = PRIOR_BATCHES[4][0], "west_ridge" if row < 16 else "northwest_ridge"
                elif col < 32:
                    owner, section = PRIOR_BATCHES[3][0], "west_far" if row < 16 else "northwest_far"
                elif col < 48:
                    owner, section = PRIOR_BATCHES[2][0], "west_next" if row < 16 else "northwest_next"
                elif col < 64:
                    owner, section = PRIOR_BATCHES[1][0], "west_outer" if row < 16 else "northwest_outer"
                else:
                    owner = PRIOR_BATCHES[0][0]
                    section = "west" if row < 16 else "northwest" if col < 80 else "north"
                base = self.root / owner / "Source"
                ident = f"r{15 - row % 16:02}_c{col % 16:02}"
                path = owner + "/TerrainData/" + section + "/" + ident + ".asset"
                asset = self.root / path
                asset.parent.mkdir(parents=True, exist_ok=True)
                asset.write_bytes(b"native fixture")
                guid = f"{row * 96 + col + 1:032x}"
                asset.with_name(asset.name + ".meta").write_text("guid: " + guid + "\n")
                guids.append(guid)
                t.update(section=section, local_id=ident, source_version="synthetic", data_path=path,
                         height_file="height.bytes", height_sha256=digest(base / "height.bytes"),
                         color_file="color.png", color_sha256=digest(base / "color.png"))
                batches[owner].append(t)
                if owner != ASSET_ROOT:
                    retained.append(t)
        for owner, _, count in PRIOR_BATCHES[:-1]:
            # These older records deliberately have no current scene proof.
            (self.root / owner / "Source/manifest.json").write_text(json.dumps(
                {"schema_version": 1, "new_tile_count": count, "new_tiles": batches[owner]}))
        self.manifest.update(new_tiles=batches[ASSET_ROOT], retained_tiles=retained)
        self.save_manifest()
        self.save_proof()
        return coverage, "\n".join(guids)

    def test_complete_current_grid_promotes_all_prior_batches_without_old_scene_proofs(self):
        retained, text = self.full_grid_fixture()
        tiles, assets, audit = append_verified_north_row(self.root, self.scene, text, retained, 3584)
        self.assertEqual((len(tiles), len(assets), len(retained)), (3584, 3328, 256))
        self.assertEqual(audit["shared_edges_checked"], 7024)
        self.assertEqual(len({t["geographic_key"] for t in tiles}), 3584)
        self.assertEqual(len(self.manifest["new_tiles"]), 256)
        self.assertEqual({t["section"] for t in self.manifest["new_tiles"]}, {"north_row_e504016"})
        cap = self.manifest["new_tiles"][0]
        original_cap = dict(cap)
        for de, dn in ((4096, 0), (0, 4096)):
            with self.subTest(shift=(de, dn)):
                e, n = original_cap["bounds_m"][:2]
                cap.update(position=[e+de-522448, -100, n+dn-4008248],
                           bounds_m=[e+de, n+dn, e+de+256, n+dn+256],
                           geographic_key=f"epsg26911/e{e+de}/n{n+dn}/size256")
                self.save_manifest()
                self.save_proof()
                with self.assertRaisesRegex(ValueError, "geographic source"):
                    append_verified_north_row(self.root, self.scene, text, retained, 3584)
                cap.update(original_cap)
        removed = self.manifest["new_tiles"].pop()
        self.save_manifest()
        self.save_proof()
        with self.assertRaisesRegex(ValueError, "Incomplete northern-row source records"):
            append_verified_north_row(self.root, self.scene, text, retained, 3584)
        self.manifest["new_tiles"].append(removed)
        self.save_manifest()
        self.save_proof()
        prior_manifest = self.root / LEGACY_BATCHES[0][0] / "Source/manifest.json"
        prior_backup = prior_manifest.read_bytes()
        prior_manifest.unlink()
        try:
            with self.assertRaises(FileNotFoundError):
                append_verified_north_row(self.root, self.scene, text, retained, 3584)
        finally:
            prior_manifest.write_bytes(prior_backup)
        with self.assertRaisesRegex(ValueError, "TerrainData reference"):
            append_verified_north_row(self.root, self.scene, "", retained, 3584)
        tile = self.manifest["new_tiles"][0]
        grid = np.full((257, 257), .5, dtype="<f4")
        grid[0, -1] = .6
        grid.tofile(self.source / "broken.bytes")
        tile.update(render_file="broken.bytes", render_sha256=digest(self.source / "broken.bytes"))
        self.save_manifest()
        self.save_proof()
        with self.assertRaisesRegex(ValueError, "Normalized seam changed"):
            append_verified_north_row(self.root, self.scene, text, retained, 3584)
        tile.update(render_file="grid.bytes", render_sha256=digest(self.source / "grid.bytes"))
        self.manifest["new_tiles"][0]["geographic_key"] = self.manifest["retained_tiles"][0]["geographic_key"]
        self.save_manifest()
        self.save_proof()
        with self.assertRaisesRegex(ValueError, "geographic source"):
            append_verified_north_row(self.root, self.scene, text, retained, 3584)


if __name__ == "__main__":
    unittest.main()
