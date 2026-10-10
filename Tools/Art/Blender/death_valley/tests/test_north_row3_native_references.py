import sys
import tempfile
from pathlib import Path
import unittest
import numpy as np
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from north_row3_native_references import read_native_reference
from north_row3_contract import sha256

class NativeReferenceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.owner = "Assets/_Project/Art/Terrain/Fixture"
        self.file = self.root / self.owner / "Source/tile.bytes"
        self.file.parent.mkdir(parents=True)
        self.south = np.arange(257 * 257, dtype="float32").reshape(257, 257) / np.float32(257 * 257)
        self.south[::-1].astype("<f4").tofile(self.file)
        self.record = {"path": self.file.relative_to(self.root).as_posix(), "sha256": sha256(self.file), "orientation": "north-first", "samples": 257}

    def tearDown(self):
        self.temp.cleanup()

    def test_exact_native_samples_and_row_orientation(self):
        np.testing.assert_array_equal(read_native_reference(self.root, self.record, [self.owner]), self.south)
        changed = dict(self.record, orientation="south-first")
        np.testing.assert_array_equal(read_native_reference(self.root, changed, [self.owner]), self.south[::-1])

    def test_changed_bytes_and_incomplete_reference(self):
        for record in (dict(self.record, sha256="0" * 64), dict(self.record, samples=129), dict(self.record, orientation=None)):
            with self.assertRaises(ValueError):
                read_native_reference(self.root, record, [self.owner])
        self.file.write_bytes(b"short")
        with self.assertRaises(ValueError):
            read_native_reference(self.root, self.record, [self.owner])

    def test_path_escape_and_unprotected_owner(self):
        for path in ("../outside.bytes", "D:/outside.bytes", "Assets\\outside.bytes", "Assets/_Project/Art/Terrain/Fixture/Other/tile.bytes"):
            with self.assertRaises(ValueError):
                read_native_reference(self.root, dict(self.record, path=path), [self.owner])
        with self.assertRaises(ValueError):
            read_native_reference(self.root, self.record, ["Assets/_Project/Art/Terrain/Other"])

if __name__ == "__main__":
    unittest.main()
