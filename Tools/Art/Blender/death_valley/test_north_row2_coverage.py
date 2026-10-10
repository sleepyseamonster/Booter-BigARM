"""Coverage must reject stale saved-scene proof and ambiguous accepted owners."""
import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'Docs/DeathValley'))
from north_row2_coverage import append_verified_north_row2, newest

class CoverageProofTests(unittest.TestCase):
    def fixture(self, root):
        source = root/'Assets/_Project/Art/Terrain/NorthRow2E499920N4018488_2026-10-09/Source'
        source.mkdir(parents=True)
        m = dict(schema_version=2, family='north_row2', row_step=1,
                 bounds_m=[499920,4006200,524496,4022584], unity_origin_m=[522448,4008248],
                 tile_count=4864, new_tile_count=256)
        (source/'manifest.json').write_text(json.dumps(m))
        scene = root/'scene.unity'; scene.write_text('saved scene')
        return source, scene, m

    def test_stale_or_incomplete_proof_cannot_refresh_coverage(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory); source,scene,_=self.fixture(root)
            for proof in ({}, {'status':'validated_full_grid_and_collider_samples', 'scene_sha256':'0'*64}):
                (source/'unity_production_validation.json').write_text(json.dumps(proof))
                with self.assertRaisesRegex(ValueError, 'stale full native'):
                    append_verified_north_row2(root,scene,'',[],4864)

    def test_ambiguous_owner_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory); _,_,m=self.fixture(root)
            other=root/'Assets/_Project/Art/Terrain/NorthRow2E499920N4018488_2026-10-10/Source'
            other.mkdir(parents=True); (other/'manifest.json').write_text(json.dumps(m))
            with self.assertRaisesRegex(ValueError, 'ambiguous'):
                newest(root,4864)

if __name__ == '__main__':
    unittest.main()
