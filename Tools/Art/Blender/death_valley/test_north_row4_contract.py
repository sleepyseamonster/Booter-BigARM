"""Independent topology, ownership and malformed-scope gates for row four."""
import unittest
from north_row4_contract import contract, occupied_cells, product_for, required_products, validate_occupancy

class FourthRowContractTests(unittest.TestCase):
    def test_topology_and_ordered_anchors(self):
        for step in range(1,7):
            c=contract(step);cells=occupied_cells(step)
            joins=sum((e+256,n) in cells for e,n in cells)+sum((e,n+256) in cells for e,n in cells)
            self.assertEqual(len(cells),7680+256*step)
            self.assertEqual(joins,15680+512*(step-1))
            self.assertEqual(4*len(cells)-2*joins,384)
            self.assertEqual(c['south'],f"north_row3_e{c['bounds'][0]}_n4022584")
            self.assertEqual(c['baseline_edges'],15184 if step==1 else contract(step-1)['edges'])
            self.assertEqual(c['west'],None if step==1 else contract(step-1)['section'])
            self.assertEqual(c['collider_samples'],25*len(cells))

    def test_both_axes_half_open_ownership(self):
        self.assertEqual(required_products(),{f'x{x}y{y}' for x in range(49,53) for y in (403,404)})
        for east,x in [(499920,49),(499999,49),(500000,50),(524496,52)]:
            self.assertEqual(product_for(east,4029999),f'x{x}y403')
            self.assertEqual(product_for(east,4030000),f'x{x}y404')
            self.assertEqual(product_for(east,4030776),f'x{x}y404')
        with self.assertRaises(ValueError):product_for(499919,4030000)
        with self.assertRaises(ValueError):product_for(500000,4030777)

    def test_no_holes_duplicates_shift_or_premature_northeast(self):
        cells=sorted(occupied_cells(1))
        for bad in [cells[:-1],cells+[cells[0]],cells[:-1]+[(524240,4030520)],list(occupied_cells(2))]:
            with self.assertRaises(ValueError):validate_occupancy(1,bad)
        self.assertNotIn((524240,4030520),occupied_cells(5))
        self.assertIn((524240,4030520),occupied_cells(6))

    def test_invalid_scope_and_date(self):
        for step in [True,0,7,1.0]:
            with self.assertRaises(ValueError):contract(step)
        with self.assertRaises(ValueError):contract(1,'2026-1-1')

if __name__=='__main__':unittest.main()
