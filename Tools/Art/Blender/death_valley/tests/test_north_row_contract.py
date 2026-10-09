"""Offline exact-footprint and two-anchor regression checks for the ordered row."""
import sys,unittest
from pathlib import Path
import numpy as np
from PIL import Image
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from north_row_contract import contract,expected_cells,validate_occupancy,feather_color,_feather_color_with_bound,color_path
class NorthRowContractTests(unittest.TestCase):
    def test_all_five_exact_occupied_unions(self):
        for k in range(1,6):
            c=contract(k);old=expected_cells(c);union=expected_cells(c,True)
            self.assertEqual(len(old),c["retained_count"]);self.assertEqual(len(union),c["total"])
            edges=sum((e+256,n) in union for e,n in union)+sum((e,n+256) in union for e,n in union)
            self.assertEqual(edges,c["edges"]);self.assertEqual(4*len(union)-2*edges,288)
            self.assertEqual(len(union-old),256)
            self.assertNotEqual(union-{next(iter(union))},union)
            self.assertEqual(len(c["retained"]),13+k-1)
            if k<5:self.assertNotIn((c["bounds"][2],4014392),union)
            else:self.assertEqual(len(union),96*48)
    def test_negative_occupancy_guards(self):
        c=contract(1)
        tiles=[{"bounds_m":[e,n,e+256,n+256],"geographic_key":f"epsg26911/e{e}/n{n}/size256"} for e,n in sorted(expected_cells(c,True))]
        validate_occupancy(c,tiles,True)
        for bad in (tiles[:-1],tiles+[tiles[0]],tiles[:-1]+[tiles[0]]):
            with self.assertRaises(ValueError):validate_occupancy(c,bad,True)
        bad=tiles[:-1]+[{"bounds_m":[508112,4014392,508368,4014648],"geographic_key":"epsg26911/e508112/n4014392/size256"}]
        with self.assertRaises(ValueError):validate_occupancy(c,bad,True)
        bad=[dict(t) for t in tiles];bad[0]["geographic_key"]="wrong_anchor"
        with self.assertRaises(ValueError):validate_occupancy(c,bad,True)
    def test_only_sealed_steps_and_batch_owners(self):
        for k in (0,6,-1,1.0):
            with self.assertRaises(ValueError):contract(k)
        with self.assertRaises(ValueError):contract(1,"NorthRowE5081122026-10-09")
    def test_color_conflicts_from_actual_retained_south_images(self):
        for k in range(1,6):
            c=contract(k);south=np.asarray(Image.open(color_path(c["south"])))[0].copy()
            if k==1:west=np.asarray(Image.open(color_path("north_ridge")))[:,-1].copy()
            else:
                prior=np.asarray(Image.open(color_path(contract(k-1)["south"])))[0,-1].copy()
                west=np.tile(prior,(2048,1))
            oldsouth=south.copy();oldwest=west.copy()
            result,proof=feather_color(np.full((2048,2048,3),127,dtype='float32'),south,west,k)
            np.testing.assert_array_equal(result[-1],south);np.testing.assert_array_equal(result[:-1,0],west[:-1])
            np.testing.assert_array_equal(south,oldsouth);np.testing.assert_array_equal(west,oldwest)
            self.assertEqual(proof["west_exception_pixels"],int(np.any(oldsouth[0]!=oldwest[-1])))
            if k==4:self.assertEqual(proof["southwest_difference_rgb"],[0,0,0])
            if k==5:self.assertEqual(proof["southwest_difference_rgb"],[16,16,16])
    def test_both_audited_conflict_fixtures(self):
        for step,diff in ((4,[19,19,28]),(5,[16,16,16])):
            south=np.full((2048,3),100,dtype='uint8');west=south.copy();west[-1]=100+np.array(diff)
            result,proof=_feather_color_with_bound(np.full((2048,2048,3),127),south,west,np.array(diff))
            self.assertEqual(proof["southwest_difference_rgb"],diff);self.assertEqual(proof["west_exception_pixels"],1)
            np.testing.assert_array_equal(result[-1],south);np.testing.assert_array_equal(result[:-1,0],west[:-1])
    def test_no_general_color_tolerance(self):
        south=np.zeros((2048,3),dtype='uint8');west=south.copy();west[-1]=[20,0,0]
        with self.assertRaises(ValueError):feather_color(np.zeros((2048,2048,3)),south,west,4)
    def test_feather_bounded_to_new_strips(self):
        base=np.full((2048,2048,3),100,dtype='float32');south=np.full((2048,3),150,dtype='uint8');west=south.copy()
        result,_=feather_color(base,south,west,1)
        np.testing.assert_array_equal(result[:1900,128:],np.full((1900,1920,3),100,dtype='uint8'))
if __name__=="__main__":unittest.main()
