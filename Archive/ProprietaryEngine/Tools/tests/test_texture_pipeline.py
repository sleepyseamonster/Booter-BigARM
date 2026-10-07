import copy
import json
import sys
import unittest
from pathlib import Path
from unittest.mock import patch
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from cook_textures import validate
from verify_foundation import check_textures
ROOT=Path(__file__).resolve().parents[2]

class RecipeChecks(unittest.TestCase):
    def recipe(self):return json.loads((ROOT/'Assets/TextureRecipes/library.json').read_text())['recipes'][0]
    def test_accepted_profile(self):validate(self.recipe())
    def test_mask_meanings_remain_distinct(self):
        rows=json.loads((ROOT/'Assets/TextureRecipes/library.json').read_text())['recipes']
        masks=[r for r in rows if r['role']=='mask']
        self.assertEqual({r['channels'] for r in masks},{'crack_halo_deposit_alpha','uninterpreted_linear_mask'})
        for r in masks:validate(r)
    def test_srgb_data_rejected(self):
        r=self.recipe();r['role']='normal'
        with self.assertRaises(ValueError):validate(r)
    def test_unknown_filter_rejected(self):
        r=self.recipe();r['filter']='stock_mips'
        with self.assertRaises(ValueError):validate(r)
    def test_alpha_semantics_required(self):
        r=self.recipe();r['alpha']='premultiplied'
        with self.assertRaises(ValueError):validate(r)
    def test_source_traversal_rejected(self):
        r=self.recipe();r['source']='Assets/../outside.png'
        with self.assertRaises(ValueError):validate(r)

class TexturePixelOracle(unittest.TestCase):
    def captures(self):
        def frame(pixel):return (100,100,3,[bytearray(c for x in range(100) for c in pixel(x,y)) for y in range(100)])
        return {'texture-color':frame(lambda x,y:[(255,0,0),(0,255,0),(0,0,255),(255,255,255)][(y//50)*2+x//50]),
                'texture-mip':frame(lambda x,y:(188,188,188)),
                'texture-normal':frame(lambda x,y:(128,128,255) if x<50 else (255,128,128)),
                'texture-surface':frame(lambda x,y:(128,128,128)),
                'texture-rock':frame(lambda x,y:(127,127,127)),
                'texture-ground':frame(lambda x,y:(127,127,127))}
    def run_oracle(self,data):
        source=(2,2,3,[bytearray([127]*6),bytearray([127]*6)])
        with patch('verify_foundation.png',return_value=source):
            return check_textures(data,ROOT/'Assets/TextureRecipes/library.json')
    def test_reference(self):self.assertEqual(len(self.run_oracle(self.captures())['observations']),10)
    def test_v_flip_rejected(self):
        d=self.captures();d['texture-color'][3].reverse()
        with self.assertRaises(ValueError):self.run_oracle(d)
    def test_wrong_color_mip_rejected(self):
        d=self.captures();d['texture-mip'][3][60][180:183]=bytes([128]*3)
        with self.assertRaises(ValueError):self.run_oracle(d)
    def test_normal_gamma_rejected(self):
        d=self.captures();d['texture-normal'][3][50][120:123]=bytes([188,188,255])
        with self.assertRaises(ValueError):self.run_oracle(d)
    def test_wrong_channel_rejected(self):
        d=self.captures();d['texture-surface'][3][50][210:213]=bytes([192]*3)
        with self.assertRaises(ValueError):self.run_oracle(d)
    def test_wrong_source_rejected(self):
        d=self.captures();d['texture-rock'][3][23][111:114]=bytes([3]*3)
        with self.assertRaises(ValueError):self.run_oracle(d)

if __name__=='__main__':unittest.main()
