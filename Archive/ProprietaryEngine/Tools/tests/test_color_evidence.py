"""Ensure the pixel oracle rejects plausible but incorrect display pipelines."""
import sys
import unittest
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from verify_foundation import check_color

class ColorEvidenceChecks(unittest.TestCase):
    def captures(self):
        result={}
        for name,values in [('color-linear',[0,10,128,188,255,255,89,225]),
                            ('color-hdr',[0,3,66,99,137,188,44,120]),
                            ('color-restored',[0,3,66,99,137,188,44,120])]:
            rows=[bytearray(800*3) for _ in range(80)]
            for index,value in enumerate(values):
                x=int(800*(index+.5)/8)
                rows[72][x*3:x*3+3]=bytes([value]*3)
            rows[24][240*3:240*3+3]=bytes([41,74,122])
            result[name]=(800,80,3,rows)
        return result
    def test_reference(self):
        self.assertEqual(len(check_color(self.captures())),3)
    def test_missing_scene(self):
        data=self.captures(); data['color-linear'][3][72]=bytearray(2400)
        with self.assertRaises(ValueError):check_color(data)
    def test_double_encoding(self):
        data=self.captures();data['color-linear'][3][72][350*3:350*3+3]=bytes([223]*3)
        with self.assertRaises(ValueError):check_color(data)
    def test_hdr_clipped_before_exposure(self):
        data=self.captures();data['color-hdr'][3][72][550*3:550*3+3]=bytes([137]*3)
        with self.assertRaises(ValueError):check_color(data)
    def test_ui_exposure_leak(self):
        data=self.captures();data['color-hdr'][3][24][240*3]=5
        with self.assertRaises(ValueError):check_color(data)
    def test_resize_target_lost(self):
        data=self.captures();data['color-restored'][3][72]=bytearray(2400)
        with self.assertRaises(ValueError):check_color(data)

if __name__=='__main__':unittest.main()
