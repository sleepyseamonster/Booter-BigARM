"""Coverage must reject stale saved-scene proof and ambiguous accepted owners."""
import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'Docs/DeathValley'))
from north_row3_coverage import append_verified_north_row3, newest, occupied_cells

class CoverageProofTests(unittest.TestCase):
    def test_source_and_authoring_contracts_have_identical_all_six_unions(self):
        from north_row3_contract import occupied_cells as authoring_cells
        for step in range(1,7):
            self.assertEqual(occupied_cells(step),authoring_cells(step))

    def fixture(self, root):
        source = root/'Assets/_Project/Art/Terrain/NorthRow3E499920N4022584_2026-10-10/Source'
        source.mkdir(parents=True)
        m = dict(schema_version=2, family='north_row3', row_step=1,
                 bounds_m=[499920,4006200,524496,4026680], unity_origin_m=[522448,4008248],
                 tile_count=6400, new_tile_count=256)
        (source/'manifest.json').write_text(json.dumps(m))
        scene = root/'scene.unity'; scene.write_text('saved scene')
        return source, scene, m

    def test_stale_or_incomplete_proof_cannot_refresh_coverage(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory); source,scene,_=self.fixture(root)
            for proof in ({}, {'status':'validated_full_grid_and_collider_samples', 'scene_sha256':'0'*64}):
                (source/'unity_production_validation.json').write_text(json.dumps(proof))
                with self.assertRaisesRegex(ValueError, 'stale full native'):
                    append_verified_north_row3(root,scene,'',[],6400)

    def test_ambiguous_owner_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory); _,_,m=self.fixture(root)
            other=root/'Assets/_Project/Art/Terrain/NorthRow3E499920N4022584_2026-10-11/Source'
            other.mkdir(parents=True); (other/'manifest.json').write_text(json.dumps(m))
            with self.assertRaisesRegex(ValueError, 'ambiguous'):
                newest(root,6400)

if __name__ == '__main__':
    unittest.main()
