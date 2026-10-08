"""A package handoff must never silently weaken retained terrain protection."""
import hashlib
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0,str(Path(__file__).resolve().parents[2]/'Tools/Art/Blender/death_valley'))
from reconcile_expansion_protection import reconcile


class ProtectionReconciliationTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name);self.manifest=self.root/'export.json'
        package=self.root/'Packages/manifest.json';package.parent.mkdir();package.write_bytes(b'current\r\n')
        self.terrain=self.root/'Assets/retained.asset';self.terrain.parent.mkdir();self.terrain.write_bytes(b'protected')
        self.data={'protected_files':[{'path':'Packages/manifest.json','sha256':'0'*64},
                    {'path':'Assets/retained.asset','sha256':hashlib.sha256(b'protected').hexdigest()}]}
        self.manifest.write_text(json.dumps(self.data))

    def invoke(self,committed=b'current\n'):
        with patch('reconcile_expansion_protection.subprocess.check_output',side_effect=['a'*40,committed]), \
             patch('reconcile_expansion_protection.subprocess.run'):
            return reconcile(self.manifest,self.root,'owner')

    def test_committed_package_change_records_both_hashes_and_preserves_terrain(self):
        receipt=self.invoke();current=json.loads(self.manifest.read_text())
        self.assertEqual(current['protected_files'][1],self.data['protected_files'][1])
        self.assertEqual(receipt['changes'][0]['baseline_sha256'],'0'*64)
        self.assertEqual(receipt['changes'][0]['owner_commit'],'a'*40)

    def test_changed_terrain_or_uncommitted_package_rejects_without_writing(self):
        before=self.manifest.read_bytes()
        with self.assertRaises(ValueError):self.invoke(b'other package')
        self.assertEqual(self.manifest.read_bytes(),before)
        self.terrain.write_bytes(b'changed terrain')
        with self.assertRaises(ValueError):self.invoke()
        self.assertEqual(self.manifest.read_bytes(),before)


if __name__=='__main__':unittest.main()
