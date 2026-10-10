"""Offline geography and source-ownership guards for the new row."""
import sys
import unittest
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from north_row2_contract import contract, occupied_cells, validate_occupancy, product_for, required_products, validate_geographic_records

class NorthRow2Tests(unittest.TestCase):
    def test_exact_unions_and_topology(self):
        for step in range(1, 7):
            c = contract(step)
            cells = occupied_cells(step)
            edges = sum((e + 256, n) in cells for e, n in cells) + sum((e, n + 256) in cells for e, n in cells)
            self.assertEqual(len(cells), c["total"])
            self.assertEqual(edges, c["edges"])
            self.assertEqual(4 * len(cells) - 2 * edges, 320)
            self.assertEqual(edges - c["baseline_edges"], 496 if step == 1 else 512)
            self.assertEqual(c["collider_samples"], len(cells) * 25)
            self.assertEqual(len(cells - occupied_cells(step - 1)), 256)
            self.assertEqual(c["west"] is None, step == 1)
            if step < 6:
                self.assertNotIn((524240, 4022328), cells)
        self.assertEqual(len(occupied_cells(6)), 96 * 64)

    def test_invalid_union_and_scope(self):
        good = occupied_cells(1)
        for cells in (list(good)[:-1], list(good) + [next(iter(good))], good | {(524240, 4022328)}, (good - {(499920, 4018488)}) | {(499921, 4018488)}):
            with self.assertRaises(ValueError):
                validate_occupancy(1, cells)
        for step in (0, 7, True, 1.0):
            with self.assertRaises(ValueError):
                contract(step)

    def test_both_product_axes_and_western_sliver(self):
        self.assertEqual(required_products(), {f"x{x}y{y}" for x in range(49, 53) for y in (402, 403)})
        self.assertEqual(product_for(499920, 4018488), "x49y402")
        self.assertEqual(product_for(500000, 4019999), "x50y402")
        self.assertEqual(product_for(500000, 4020000), "x50y403")
        self.assertEqual(product_for(524496, 4022584), "x52y403")
        with self.assertRaises(ValueError):
            product_for(524497, 4022584)

    def test_geographic_key_and_physical_bounds_agree(self):
        records = [{"bounds_m": [e, n, e + 256, n + 256], "geographic_key": f"epsg26911/e{e}/n{n}/size256"} for e, n in sorted(occupied_cells(1))]
        validate_geographic_records(1, records)
        for change in ({"geographic_key": "wrong"}, {"bounds_m": [499920, 4006200, 500177, 4006456]}):
            bad = [dict(r) for r in records]
            bad[0].update(change)
            with self.assertRaises(ValueError):
                validate_geographic_records(1, bad)

if __name__ == "__main__":
    unittest.main()
