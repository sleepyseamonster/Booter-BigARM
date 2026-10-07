"""Portable Mac manifest resolution preserves source identities and boundaries."""
import hashlib
import tempfile
import unittest
from pathlib import Path

from terrain_archive import TerrainArchive,MAC_PREFIX


class TerrainArchiveTests(unittest.TestCase):
    def test_rebase_nested_paths_without_mutating_original_metadata(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory);folder=root/'SourceData/Test';folder.mkdir(parents=True)
            path=folder/'terrain.npz';path.write_bytes(b'source')
            receipt={'data_root':'SourceData/Test','records':[{'source_relative':'prepared/terrain.npz','status':'retained_source_snapshot','destination':'SourceData/Test/terrain.npz','sha256':hashlib.sha256(b'source').hexdigest()}]}
            archive=TerrainArchive(root,receipt)
            original={'file':MAC_PREFIX+'prepared/terrain.npz','sources':['https://example.invalid/source',MAC_PREFIX.rstrip('/')]}
            result=archive.rewrite(original)
            self.assertEqual(result['file'],str(path));self.assertEqual(result['sources'][1],str(folder))
            self.assertEqual(original['file'],MAC_PREFIX+'prepared/terrain.npz')
            self.assertEqual(result['sources'][0],original['sources'][0])

    def test_missing_escaped_or_modified_sources_are_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory);folder=root/'SourceData/Test';folder.mkdir(parents=True)
            path=folder/'terrain.npz';path.write_bytes(b'changed')
            record={'source_relative':'terrain.npz','status':'retained_source_snapshot','destination':'SourceData/Test/terrain.npz','sha256':'0'*64}
            receipt={'data_root':'SourceData/Test','records':[record]}
            with self.assertRaisesRegex(ValueError,'changed'):TerrainArchive(root,receipt).resolve(MAC_PREFIX+'terrain.npz')
            with self.assertRaisesRegex(ValueError,'missing'):TerrainArchive(root,receipt).resolve(MAC_PREFIX+'absent.npz')
            record['destination']='../outside.npz'
            with self.assertRaisesRegex(ValueError,'escapes'):TerrainArchive(root,receipt).resolve(MAC_PREFIX+'terrain.npz')


if __name__=='__main__':unittest.main()
