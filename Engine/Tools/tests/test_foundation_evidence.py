import struct
import tempfile
import unittest
import zlib
from pathlib import Path
import sys

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS))
from verify_foundation import png, changed_scene


def chunk(kind, payload):
    return struct.pack(">I", len(payload)) + kind + payload + struct.pack(">I", zlib.crc32(kind+payload) & 0xffffffff)


class FoundationEvidenceChecks(unittest.TestCase):
    def test_png_decode_and_corrupt_checksum(self):
        # Two RGB rows, one using PNG's Sub predictor and one using Up.
        header = struct.pack(">IIBBBBB", 2, 2, 8, 2, 0, 0, 0)
        raw = bytes([1, 10, 20, 30, 5, 5, 5, 2, 1, 2, 3, 1, 2, 3])
        data = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", header) + chunk(b"IDAT", zlib.compress(raw)) + chunk(b"IEND", b"")
        cache = TOOLS.parent / ".cache"
        cache.mkdir(exist_ok=True)
        with tempfile.TemporaryDirectory(dir=cache) as directory:
            path = Path(directory) / "image.png"
            path.write_bytes(data)
            w, h, channels, rows = png(path)
            self.assertEqual((w,h,channels), (2,2,3))
            self.assertEqual(list(rows[0]), [10,20,30,15,25,35])
            self.assertEqual(list(rows[1]), [11,22,33,16,27,38])
            path.write_bytes(data[:-1]+bytes([data[-1]^1]))
            with self.assertRaisesRegex(ValueError, "checksum"):
                png(path)

    def test_inspector_only_change_is_not_scene_evidence(self):
        baseline = (100,100,3,[bytearray(300) for _ in range(100)])
        edited = (100,100,3,[bytearray(300) for _ in range(100)])
        for row in edited[3]:
            row[:90] = bytes([255])*90
        self.assertEqual(changed_scene(baseline,edited), (0,0))
        for row in edited[3]:
            for x in range(50,70):
                row[x*3+2]=200
        changed, blue = changed_scene(baseline,edited)
        self.assertGreater(changed,0)
        self.assertEqual(changed,blue)

    def test_dimension_mismatch_rejected(self):
        with self.assertRaisesRegex(ValueError,"different dimensions"):
            changed_scene((2,2,3,[]),(4,2,3,[]))


if __name__ == "__main__":
    unittest.main()
