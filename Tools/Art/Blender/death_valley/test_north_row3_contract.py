"""Geographic gates for the separately owned third northern row."""
import unittest
from north_row3_contract import contract, occupied_cells, product_for, required_products, validate_occupancy

class ThirdRowContractTests(unittest.TestCase):
    def test_all_native_adjacency_and_exterior_counts(self):
        for step in range(1, 7):
            c = contract(step)
            cells = occupied_cells(step)
            joins = sum((e + 256, n) in cells for e, n in cells) + sum((e, n + 256) in cells for e, n in cells)
            self.assertEqual(len(cells), c["total"])
            self.assertEqual(joins, c["edges"])
            self.assertEqual(4 * len(cells) - 2 * joins, c["exterior_slots"])
            self.assertEqual(c["collider_samples"], len(cells) * 25)
            self.assertEqual(c["south"], f"north_row2_e{c['bounds'][0]}_n4018488")

    def test_first_section_has_only_south_anchor(self):
        self.assertIsNone(contract(1)["west"])
        self.assertEqual(contract(2)["west"], contract(1)["section"])
        self.assertEqual(contract(1)["baseline_edges"], 12128)

    def test_incomplete_duplicate_shifted_and_premature_grids_rejected(self):
        cells = list(occupied_cells(1))
        for changed in (cells[:-1], cells + [cells[0]], cells[:-1] + [(cells[-1][0] + 1, cells[-1][1])], list(occupied_cells(2))):
            with self.assertRaises(ValueError):
                validate_occupancy(1, changed)

    def test_source_ownership_includes_western_sliver(self):
        self.assertEqual(required_products(), {"x49y403", "x50y403", "x51y403", "x52y403"})
        self.assertEqual(product_for(499920, 4022584), "x49y403")
        self.assertEqual(product_for(500000, 4026680), "x50y403")
        self.assertEqual(product_for(524496, 4026680), "x52y403")
        with self.assertRaises(ValueError):
            product_for(499919, 4022584)

    def test_unsupported_steps_and_dates_rejected(self):
        for step in (True, 0, 7, 1.0):
            with self.assertRaises(ValueError):
                contract(step)
        with self.assertRaises(ValueError):
            contract(1, "2026-1-1")

if __name__ == "__main__":
    unittest.main()
