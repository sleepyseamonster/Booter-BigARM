"""Twelve-section inventory requires current native proof, not export presence."""
import json
from pathlib import Path
import tempfile
import unittest

import numpy as np
from west_ridge_pair_coverage import ASSET_ROOT, PRIOR_BATCHES, append_verified_west_ridge_pair, digest


class WestRidgePairCoverageTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(dir="D:/BooterBigArmValidation/Temp")
        self.root = Path(self.temp.name)
        self.source = self.root / ASSET_ROOT / "Source"
        self.source.mkdir(parents=True)
        self.scene = self.root / "Assets/_Project/Scenes/Production/GreaterWasteland.unity"
        self.scene.parent.mkdir(parents=True)
        self.scene.write_text("saved production scene\n")
        self.manifest = {"schema_version": 1, "bounds_m": [499920, 4006200, 524496, 4014392],
                         "unity_origin_m": [522448, 4008248], "new_tile_count": 512,
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
                "terrains": 3072, "retained": 2560, "edges": 6016, "outer_edges": 256,
                "collider_samples": 76800}

    def save_proof(self, proof=None):
        (self.source / "unity_production_validation.json").write_text(json.dumps(proof or self.proof()))

    def test_export_only_or_old_production_proof_cannot_promote_coverage(self):
        with self.assertRaises(FileNotFoundError):
            append_verified_west_ridge_pair(self.root, self.scene, "", [])
        for key, value in (("scene_sha256", "0" * 64), ("manifest_sha256", "0" * 64),
                           ("terrains", 2560), ("retained", 2048), ("edges", 5008),
                           ("outer_edges", 224), ("collider_samples", 64000)):
            with self.subTest(key=key):
                proof = self.proof()
                proof[key] = value
                self.save_proof(proof)
                with self.assertRaisesRegex(ValueError, "Missing or stale"):
                    append_verified_west_ridge_pair(self.root, self.scene, "", [])

    def test_shifted_origin_rejected_before_proof(self):
        self.manifest["unity_origin_m"][0] -= 4096
        self.save_manifest()
        with self.assertRaisesRegex(ValueError, "Unsupported"):
            append_verified_west_ridge_pair(self.root, self.scene, "", [])

    def test_protected_prior_source_change_rejected(self):
        protected = self.root / "retained.txt"
        protected.write_text("original")
        self.manifest["protected_files"] = [{"path": "retained.txt", "sha256": digest(protected)}]
        self.save_manifest()
        self.save_proof()
        protected.write_text("changed")
        with self.assertRaisesRegex(ValueError, "Retained source changed"):
            append_verified_west_ridge_pair(self.root, self.scene, "", [])

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
        for row in range(32):
            for col in range(96):
                e, n = 499920 + col * 256, 4006200 + row * 256
                key = f"epsg26911/e{e}/n{n}/size256"
                t = {"geographic_key": key, "bounds_m": [e, n, e + 256, n + 256],
                     "position": [e - 522448, -100, n - 4008248], "render_file": "grid.bytes",
                     "render_sha256": digest(self.source / "grid.bytes")}
                if col >= 80 and row < 16:
                    retained.append(t)
                    coverage.append({"geographic_key": key})
                    continue
                if col < 16:
                    owner, section = ASSET_ROOT, "west_ridge" if row < 16 else "northwest_ridge"
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
        tiles, assets, audit = append_verified_west_ridge_pair(self.root, self.scene, text, retained)
        self.assertEqual((len(tiles), len(assets), len(retained)), (3072, 2816, 256))
        self.assertEqual(audit["shared_edges_checked"], 6016)
        self.assertEqual(len({t["geographic_key"] for t in tiles}), 3072)
        with self.assertRaisesRegex(ValueError, "TerrainData reference"):
            append_verified_west_ridge_pair(self.root, self.scene, "", retained)
        tile = self.manifest["new_tiles"][0]
        grid = np.full((257, 257), .5, dtype="<f4")
        grid[0, -1] = .6
        grid.tofile(self.source / "broken.bytes")
        tile.update(render_file="broken.bytes", render_sha256=digest(self.source / "broken.bytes"))
        self.save_manifest()
        self.save_proof()
        with self.assertRaisesRegex(ValueError, "Normalized seam changed"):
            append_verified_west_ridge_pair(self.root, self.scene, text, retained)
        tile.update(render_file="grid.bytes", render_sha256=digest(self.source / "grid.bytes"))
        self.manifest["new_tiles"][0]["geographic_key"] = self.manifest["retained_tiles"][0]["geographic_key"]
        self.save_manifest()
        self.save_proof()
        with self.assertRaisesRegex(ValueError, "geographic source"):
            append_verified_west_ridge_pair(self.root, self.scene, text, retained)


if __name__ == "__main__":
    unittest.main()
