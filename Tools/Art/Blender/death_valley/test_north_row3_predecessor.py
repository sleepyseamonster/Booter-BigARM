"""Stale acceptance evidence must not create a baseline or modeled source."""
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import capture_north_row3_predecessor as capture
from north_row3_contract import contract, sha256

class PredecessorGateTests(unittest.TestCase):
    def test_stale_scene_or_manifest_proof_rejected_before_output(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory);scene=root/capture.SCENE
            scene.parent.mkdir(parents=True);scene.write_text('saved scene')
            source=root/'Source';source.mkdir();manifest=source/'manifest.json'
            manifest.write_text(json.dumps(dict(row_step=6)))
            c=contract(1);c['data']=root/'NewBaseline';c['art']=root/'NewArt'
            valid=dict(family='north_row2',row_step=6,status='validated_full_grid_and_collider_samples',
                       scene=capture.SCENE,scene_sha256=sha256(scene),manifest_sha256=sha256(manifest),
                       terrains=6144,retained=5888,edges=12128,outer_edges=320,collider_samples=153600)
            for field in ['scene_sha256','manifest_sha256']:
                with self.subTest(field=field):
                    bad=dict(valid);bad[field]='0'*64
                    (source/'unity_production_validation.json').write_text(json.dumps(bad))
                    with patch.object(capture,'ROOT',root),patch.object(capture,'contract',return_value=c),\
                         patch.object(capture.subprocess,'check_output',return_value='main\n'),patch.object(capture.subprocess,'run'):
                        with self.assertRaisesRegex(ValueError,'stale immediately preceding'):
                            capture.capture(1,'2026-10-10',manifest)
                    self.assertFalse(c['data'].exists())
                    self.assertFalse(c['art'].exists())

if __name__=='__main__':
    unittest.main()
