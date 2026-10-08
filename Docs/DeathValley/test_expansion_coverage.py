"""Expanded coverage must never be promoted from proposed/exported rectangles alone."""
import json
from pathlib import Path
import tempfile
import unittest
import numpy as np
from expansion_coverage import append_verified_expansion,owned_file,digest,ASSET_ROOT

class ExpansionCoverageTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(dir='D:/BooterBigArmValidation/Temp')
        self.root=Path(self.temp.name);self.source=self.root/ASSET_ROOT/'Source';self.source.mkdir(parents=True)
        self.scene=self.root/'Assets/_Project/Scenes/Production/GreaterWasteland.unity';self.scene.parent.mkdir(parents=True);self.scene.write_text('saved production scene\n')
        self.manifest={'schema_version':1,'bounds_m':[516304,4006200,524496,4014392],'unity_origin_m':[522448,4008248],'new_tile_count':768}
        (self.source/'manifest.json').write_text(json.dumps(self.manifest))
    def tearDown(self):self.temp.cleanup()

    def test_exported_sections_without_Unity_proof_cannot_become_playable(self):
        with self.assertRaises(FileNotFoundError):append_verified_expansion(self.root,self.scene,'',[])

    def test_candidate_or_stale_scene_proof_cannot_promote_production(self):
        base={'status':'validated_full_grid_and_collider_samples','scene':'Assets/_Project/Scenes/Production/GreaterWasteland.unity',
              'scene_sha256':digest(self.scene),'manifest_sha256':digest(self.source/'manifest.json'),'terrains':1024,'edges':1984,'collider_samples':25600}
        for key,value in [('scene','Assets/_Project/Scenes/Reference/TerrainExpansionCandidate.unity'),('scene_sha256','0'*64),('edges',480),('collider_samples',6400)]:
            proof=dict(base);proof[key]=value;(self.source/'unity_production_validation.json').write_text(json.dumps(proof))
            with self.assertRaisesRegex(ValueError,'missing or stale'):append_verified_expansion(self.root,self.scene,'',[])

    def test_resources_cannot_escape_their_owner(self):
        with self.assertRaises(ValueError):owned_file(self.root,self.source,'../../../../Packages/manifest.json')

    def test_shifted_origin_is_rejected_before_proof_or_asset_lookup(self):
        self.manifest['unity_origin_m'][0]-=2048;(self.source/'manifest.json').write_text(json.dumps(self.manifest))
        with self.assertRaisesRegex(ValueError,'Unsupported'):append_verified_expansion(self.root,self.scene,'',[])

    def full_grid_fixture(self):
        """A complete synthetic grid with source hashes and linked native asset GUIDs."""
        render=self.source/'grid.bytes';np.full((257,257),.5,dtype='<f4').tofile(render)
        height=self.source/'height.bytes';height.write_bytes(b'control source')
        color=self.source/'color.png';color.write_bytes(b'color source')
        new=[];retained=[];coverage=[];guids=[]
        for row in range(32):
            for col in range(32):
                east=516304+col*256;north=4006200+row*256
                key=f'epsg26911/e{east}/n{north}/size256'
                t={'geographic_key':key,'position':[east-522448,-100,north-4008248],
                   'render_file':'grid.bytes','render_sha256':digest(render)}
                if row<16 and col>=16:
                    retained.append(t);coverage.append({'geographic_key':key})
                    continue
                section='west' if row<16 else ('north' if col>=16 else 'northwest')
                ident=f'r{15-row%16:02}_c{col%16:02}'
                relative=ASSET_ROOT+'/TerrainData/'+section+'/'+ident+'.asset'
                asset=self.root/relative;asset.parent.mkdir(parents=True,exist_ok=True);asset.write_bytes(b'native fixture')
                guid=f'{row*32+col+1:032x}';guids.append(guid)
                asset.with_name(asset.name+'.meta').write_text('guid: '+guid+'\n')
                t.update(section=section,local_id=ident,source_version='synthetic',bounds_m=[east,north,east+256,north+256],
                         data_path=relative,height_file='height.bytes',height_sha256=digest(height),
                         color_file='color.png',color_sha256=digest(color))
                new.append(t)
        self.manifest.update(new_tiles=new,retained_tiles=retained)
        manifest=self.source/'manifest.json';manifest.write_text(json.dumps(self.manifest))
        proof={'status':'validated_full_grid_and_collider_samples','scene':'Assets/_Project/Scenes/Production/GreaterWasteland.unity',
               'scene_sha256':digest(self.scene),'manifest_sha256':digest(manifest),'terrains':1024,'edges':1984,'collider_samples':25600}
        (self.source/'unity_production_validation.json').write_text(json.dumps(proof))
        return coverage,'\n'.join(guids)

    def test_complete_integrated_grid_promotes_768_new_geographic_records(self):
        retained,text=self.full_grid_fixture()
        records,assets,proof=append_verified_expansion(self.root,self.scene,text,retained)
        self.assertEqual(len(records),1024);self.assertEqual(len(assets),768)
        self.assertEqual(len({r['geographic_key'] for r in records}),1024)
        self.assertEqual(proof['shared_edges_checked'],1984)
        self.assertEqual(len(retained),256)
        fields=['legacy_id','geographic_key','quarter','source_spacing_m','source_samples','render_samples',
                'height_source','color_source','integrity']
        for record in records[256:]:
            self.assertTrue(all(field in record for field in fields))
            self.assertIn(record['quarter'],('NW','NE','SW','SE'))

    def test_changed_sources_or_missing_scene_reference_reject_complete_grid(self):
        retained,text=self.full_grid_fixture()
        with self.assertRaisesRegex(ValueError,'unlinked'):
            append_verified_expansion(self.root,self.scene,'',retained)
        (self.source/'grid.bytes').write_bytes(b'changed')
        with self.assertRaisesRegex(ValueError,'Render source changed'):
            append_verified_expansion(self.root,self.scene,text,retained)

if __name__=='__main__':unittest.main()
