"""Regressions for source fidelity and protected terrain boundaries."""
from pathlib import Path
import sys
import unittest
import numpy as np

sys.path.insert(0,str(Path(__file__).resolve().parents[2]/'Tools/Art/Blender/death_valley'))
from prepare_expansion import promote,constrain_border

class ExpansionPreparationTests(unittest.TestCase):
    def test_control_samples_and_asymmetric_orientation_survive_promotion(self):
        source=np.array([[1,5,9],[3,9,15],[7,19,31]],dtype='float32')
        result=promote(source)
        np.testing.assert_array_equal(result[::2,::2],source)
        self.assertEqual(result[1,1],4.5)
        self.assertEqual(result[0,-1],9)
        self.assertEqual(result[-1,0],7)

    def test_missing_or_rectangular_lattices_fail(self):
        for source in [np.zeros((2,3)),np.array([[0,np.nan],[1,2]]),np.ones((1,1))]:
            with self.assertRaises(ValueError):promote(source)

    def test_retained_boundary_overrides_west_candidate_without_mutating_interior(self):
        candidate=np.full((257,257),3,dtype='float32')
        constraints={(520400,4006200+z):np.float32(7+z/1000) for z in range(257)}
        before=dict(constraints)
        delta,count=constrain_border(candidate,520144,4006200,constraints)
        self.assertEqual(count,257)
        self.assertGreater(delta,4)
        np.testing.assert_array_equal(candidate[1:-1,:-1],np.full((255,256),3,dtype='float32'))
        for north,height in constraints.items():self.assertEqual(candidate[256-(north[1]-4006200),-1],height)
        self.assertEqual(constraints,before)

    def test_central_junction_constraints_are_independent_of_section_order(self):
        constraints={(520400,4010296):np.float32(-84.5)}
        positions=[(520144,4010040,0,256),(520400,4010296,256,0),(520144,4010296,256,256)]
        for east,north,row,col in reversed(positions):
            candidate=np.zeros((257,257),dtype='float32')
            _,count=constrain_border(candidate,east,north,constraints)
            self.assertEqual(count,1)
            self.assertEqual(candidate[row,col],-84.5)
            self.assertEqual(candidate[128,128],0)

if __name__=='__main__':unittest.main()
