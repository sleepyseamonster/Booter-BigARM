import sys
from pathlib import Path
import unittest
import numpy as np
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from north_row2_preparation_policy import feather_color

class NewRowColorTests(unittest.TestCase):
    def setUp(self):
        self.base = np.full((2048, 2048, 3), 100, dtype="float32")
        self.south = np.full((2048, 3), 130, dtype="uint8")

    def test_first_section_only_pins_south(self):
        result, proof = feather_color(1, self.base, self.south)
        np.testing.assert_array_equal(result[-1], self.south)
        np.testing.assert_array_equal(result[:-128], self.base[:-128])
        self.assertFalse(proof["west_anchor_present"])
        self.assertEqual(proof["west_exception_pixels"], 0)
        with self.assertRaises(ValueError):
            feather_color(1, self.base, self.south, self.south)

    def test_fresh_single_pixel_conflict_is_bounded_and_immutable(self):
        west = np.full((2048, 3), 130, dtype="uint8")
        west[-1] = [141, 133, 154]
        before = west.copy()
        result, proof = feather_color(2, self.base, self.south, west, [11, 3, 24])
        np.testing.assert_array_equal(result[-1], self.south)
        np.testing.assert_array_equal(result[:-1, 0], west[:-1])
        np.testing.assert_array_equal(result[:-128, 128:], self.base[:-128, 128:])
        np.testing.assert_array_equal(west, before)
        self.assertEqual(proof["west_exception_pixels"], 1)
        with self.assertRaises(ValueError):
            feather_color(2, self.base, self.south, west, [16, 16, 16])

    def test_missing_or_stale_west_audit_fails(self):
        for west, audit in ((None, None), (self.south, None), (self.south, [1, 0, 0])):
            with self.assertRaises(ValueError):
                feather_color(3, self.base, self.south, west, audit)

if __name__ == "__main__":
    unittest.main()
