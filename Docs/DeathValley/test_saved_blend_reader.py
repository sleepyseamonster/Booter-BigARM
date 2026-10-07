"""Saved source integrity and safe Blender recovery regressions."""
import hashlib
import io
import json
import struct
import unittest
from pathlib import Path

from PIL import Image
from saved_blend_reader import SavedBlend,decompress
from recover_visualizer_blender import recover,resolve_saved_source

HERE=Path(__file__).resolve().parent


class SavedBlendTests(unittest.TestCase):
    def test_recovery_reproduces_grid_and_exact_images_from_the_saved_source(self):
        folder=HERE/'visualizer_data/blender_recovered';root=next(p for p in HERE.parents if (p/'ProjectSettings/ProjectVersion.txt').exists())
        proof=json.loads((folder/'recovery_proof.json').read_text(encoding='utf-8'))
        source=resolve_saved_source(proof['source_blend'],proof['source_blend_sha256']).read_bytes()
        self.assertEqual(hashlib.sha256(source).hexdigest(),proof['source_blend_sha256'])
        grid,textures,new_proof=recover(source,json.loads((HERE/'coverage_catalog.json').read_text(encoding='utf-8')))
        self.assertEqual(hashlib.sha256(grid.tobytes()).hexdigest(),proof['grid_sha256'])
        self.assertEqual(new_proof['sample_tier_counts'],proof['sample_tier_counts'])
        for actual,expected in zip(textures,proof['textures']):
            self.assertEqual(hashlib.sha256(actual['payload']).hexdigest(),expected['sha256'])

    def test_exact_packed_bytes_and_recovered_grid_have_portable_hashes(self):
        folder=HERE/'visualizer_data/blender_recovered'
        proof=json.loads((folder/'recovery_proof.json').read_text(encoding='utf-8'))
        self.assertEqual(proof['sample_count'],961*1121)
        self.assertEqual(proof['uncovered_samples'],0)
        self.assertEqual(sum(proof['sample_tier_counts'].values()),proof['sample_count'])
        self.assertEqual(sum(proof['mesh_counts'].values()),76)
        self.assertEqual(hashlib.sha256((folder/'blender_overview.f32').read_bytes()).hexdigest(),proof['grid_sha256'])
        for texture in proof['textures']:
            payload=(folder/texture['file']).read_bytes()
            self.assertEqual(hashlib.sha256(payload).hexdigest(),texture['sha256'])
            with Image.open(io.BytesIO(payload)) as image:
                image.verify()
            with Image.open(io.BytesIO(payload)) as image:
                self.assertEqual(image.size,(texture['width'],texture['height']))

    def test_unknown_compression_and_missing_schema_fail_closed(self):
        with self.assertRaises(ValueError):decompress(b'not a Blender file')
        raw=b'BLENDER17-01v0502'+struct.pack('<4siQqq',b'ENDB',0,0,0,0)
        with self.assertRaisesRegex(ValueError,'DNA'):SavedBlend(raw)
        with self.assertRaisesRegex(ValueError,'Truncated'):SavedBlend(b'BLENDER17-01v0502'+b'DATA')

    def test_data_pointers_are_scoped_to_their_blender_id(self):
        reader=SavedBlend.__new__(SavedBlend)
        first={'data':memoryview(b'first mesh')};second={'data':memoryview(b'second mesh')}
        reader.index={42:[first,second]};reader.local={(1,42):first,(2,42):second}
        reader.context=1;self.assertEqual(bytes(reader.pointer(42)),b'first mesh')
        reader.context=2;self.assertEqual(bytes(reader.pointer(42)),b'second mesh')
        reader.context=3
        with self.assertRaisesRegex(ValueError,'context'):reader.pointer(42)
        with self.assertRaisesRegex(ValueError,'Missing'):reader.pointer(999)


if __name__=='__main__':unittest.main()
