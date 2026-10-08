"""Migration and immutable source checks for archived Blender proofs."""
import hashlib
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0,str(Path(__file__).resolve().parents[2]/'Tools/Art/Blender/death_valley'))
from expansion_proof_paths import verify_blender_resources


class ExpansionExportPathTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name)
        self.relative='SourceArt/Blender/Studies/DeathValley/WestNorthNorthwest2026-10-07/west01.blend'
        self.blend=self.root/self.relative
        self.blend.parent.mkdir(parents=True)
        self.blend.write_bytes(b'original source')
        self.export=self.root/'proof.npz'
        self.export.write_bytes(b'original exported heights')
        self.proof={'blend_file':'C:\\old checkout\\'+self.relative.replace('/','\\'),
                    'blend_sha256':hashlib.sha256(self.blend.read_bytes()).hexdigest(),
                    'export_file':'proof.npz',
                    'export_sha256':hashlib.sha256(self.export.read_bytes()).hexdigest()}

    def test_old_drive_resolves_to_current_checkout_without_changing_provenance(self):
        before=dict(self.proof)
        self.assertEqual(verify_blender_resources(self.proof,self.root,self.root),
                         (self.blend.resolve(),self.export.resolve()))
        self.assertEqual(self.proof,before)

    def test_missing_current_source_cannot_fall_back_to_archived_drive(self):
        self.blend.unlink()
        with self.assertRaises(FileNotFoundError):
            verify_blender_resources(self.proof,self.root,self.root)

    def test_changed_source_or_export_is_rejected(self):
        for target in (self.blend,self.export):
            original=target.read_bytes()
            target.write_bytes(b'changed')
            with self.assertRaises(ValueError):
                verify_blender_resources(self.proof,self.root,self.root)
            target.write_bytes(original)

    def test_path_escape_and_foreign_archive_are_rejected(self):
        cases=[('blend_file','C:/other/west01.blend'),
               ('blend_file','C:/old/'+self.relative.replace('west01.blend','../west01.blend')),
               ('export_file','../outside.npz'),('export_file',str(self.root.parent/'outside.npz'))]
        for field,value in cases:
            with self.subTest(field=field,value=value):
                proof=dict(self.proof,**{field:value})
                with self.assertRaises(ValueError):
                    verify_blender_resources(proof,self.root,self.root)


if __name__=='__main__':
    unittest.main()
