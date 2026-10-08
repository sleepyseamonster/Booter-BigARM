"""Guarded Blender-derived export used by the canonical Badwater source exporter."""
import json
from pathlib import Path
import numpy as np
from PIL import Image
from prepare_region import sha256
from acquire_expansion import BOUNDS,SECTIONS
from expansion_proof_paths import verify_blender_resources

ROOT=Path(__file__).resolve().parents[4]
ASSET_ROOT='Assets/_Project/Art/Terrain/WestNorthNorthwest2026-10-07'


def export(manifest_path,output):
    manifest_path=manifest_path.resolve();output=output.resolve()
    if output.exists():raise ValueError('Use a fresh expansion export directory')
    m=json.loads(manifest_path.read_text())
    if m['schema_version']!=1 or m['bounds_m']!=BOUNDS or m['unity_origin_m']!=[522448,4008248] or {s['id'] for s in m['sections']}!=set(SECTIONS):
        raise ValueError('Expansion footprint/origin/schema mismatch')
    combined=json.loads((manifest_path.parent/'combined_blender_proof.json').read_text())
    if combined['status']!='native_reopened_mesh_verified' or combined['chunks']!=1024 or combined['manifest_sha256']!=sha256(manifest_path):
        raise ValueError('Combined native Blender proof is missing or stale')
    verify_blender_resources(combined,manifest_path.parent,ROOT)
    base_path=ROOT/m['baseline_manifest'];bm=json.loads(base_path.read_text())
    if sha256(base_path)!=m['baseline_manifest_sha256']:raise ValueError('Retained baseline changed')
    base=np.load(base_path.parent/bm['render_heights_file'])
    constraints={}
    for tile in bm['tiles']:
        h=base[tile['id']];p=tile['position'];east=int(p['x']+522448);north=int(p['z']+4008248)
        if east==520400:
            for z in range(257):constraints[(east,north+z)]=h[z,0]
        if north==4010040:
            for x in range(257):constraints[(east+x,north+256)]=h[-1,x]
    output.mkdir(parents=True);new=[];retained=[];proofs=[];global_edges={};edges_error=0
    for section in m['sections']:
        name=section['id'];proof_path=manifest_path.parent/(name+'_blender_proof.json');proof=json.loads(proof_path.read_text())
        if proof['status']!='native_reopened_mesh_verified' or proof['chunks']!=256 or proof['interior_spacing_m']!=2 or proof['manifest_sha256']!=sha256(manifest_path):
            raise ValueError('Individual native Blender proof missing/stale: '+name)
        _,export_path=verify_blender_resources(proof,manifest_path.parent,ROOT)
        if sha256(manifest_path.parent/section['color_file'])!=section['color_sha256']:raise ValueError('Color resource changed')
        arrays=np.load(export_path);color=Image.open(manifest_path.parent/section['color_file']).convert('RGB')
        if color.size!=(2048,2048):raise ValueError('Unexpected color dimensions')
        proofs.append({'section':name,'proof_sha256':sha256(proof_path),'blend_sha256':proof['blend_sha256'],'export_sha256':proof['export_sha256']})
        for t in section['tiles']:
            ident=t['local_id'];east,north=t['bounds_m'][:2]
            heights=arrays[name+'__'+ident]
            if heights.shape!=(257,257) or not np.isfinite(heights).all() or heights.min()<-100 or heights.max()>1700:
                raise ValueError('Invalid/clipped Blender height field')
            normalized=((heights+np.float32(100))/np.float32(1800)).astype('<f4')
            for nr,x in [(r,c) for r in range(257) for c in (0,256)]+[(r,c) for c in range(1,256) for r in (0,256)]:
                xy=(east+x,north+256-nr)
                if xy in constraints:
                    if abs(float(heights[nr,x])-(float(constraints[xy])*1800-100))>.00025:raise ValueError('Blender retained boundary disagrees')
                    normalized[nr,x]=constraints[xy]
                if xy in global_edges:edges_error=max(edges_error,abs(float(global_edges[xy])-float(normalized[nr,x])))
                else:global_edges[xy]=normalized[nr,x]
            render='Render/'+name+'/'+ident+'.bytes';raw='Heights/'+name+'/'+ident+'.bytes';rgb='Colors/'+name+'/'+ident+'.png'
            for rel in (render,raw,rgb):(output/rel).parent.mkdir(parents=True,exist_ok=True)
            normalized.tofile(output/render)
            np.rint(np.clip(normalized[::2,::2],0,1)*65535).astype('<u2').tofile(output/raw)
            row,col=t['row'],t['col'];color.crop((col*128,row*128,(col+1)*128,(row+1)*128)).save(output/rgb)
            new.append({'section':name,'local_id':ident,'geographic_key':t['geographic_key'],'source_version':t['source_version'],
                        'bounds_m':t['bounds_m'],'position':[east-522448,-100,north-4008248],'size':[256,1800,256],
                        'source_spacing_m':2,'source_samples':129,'render_samples':257,'render_file':render,'render_sha256':sha256(output/render),
                        'height_file':raw,'height_sha256':sha256(output/raw),'color_file':rgb,'color_sha256':sha256(output/rgb),
                        'data_path':ASSET_ROOT+'/TerrainData/'+name+'/'+ident+'.asset',
                        'layer_path':ASSET_ROOT+'/TerrainLayers/'+name+'/'+ident+'.terrainlayer'})
    if len(new)!=768 or len({t['geographic_key'] for t in new})!=768 or edges_error!=0:raise ValueError('New export count/edge mismatch')
    for tile in bm['tiles']:
        file='Baseline/'+tile['id']+'.bytes';(output/file).parent.mkdir(exist_ok=True)
        base[tile['id']][::-1].astype('<f4').tofile(output/file)
        retained.append({'local_id':tile['id'],'geographic_key':tile['geographic_key'],'data_path':tile['data_path'],
                         'data_guid':tile['data_guid'],'data_sha256':tile['data_sha256'],'meta_sha256':tile['meta_sha256'],
                         'render_file':file,'render_sha256':sha256(output/file),
                         'position':[tile['position'][k] for k in ('x','y','z')],'size':[256,1800,256]})
    report={'schema_version':1,'working_crs':'EPSG:26911','bounds_m':BOUNDS,'unity_origin_m':[522448,4008248],
            'scene_guid':bm['scene_guid'],'tile_count':1024,'new_tile_count':768,'asset_root':ASSET_ROOT,
            'minimum_m':-100,'range_m':1800,'render_encoding':'little-endian float32 normalized north-first; retained borders use exact readback',
            'height_encoding':'little-endian uint16 north-first 129x129 control lattice; no inherited focus corrections',
            'prepared_manifest_sha256':sha256(manifest_path),'blender_proofs':proofs,
            'combined_blend_sha256':combined['blend_sha256'],'baseline_manifest_sha256':sha256(base_path),
            'new_tiles':new,'retained_tiles':retained,
            'protected_files':json.loads((base_path.parent/'asset_protection.json').read_text())['protected_files'],
            'status':'verified_Blender_export_requires_Unity_candidate_and_integration'}
    (output/'manifest.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({'exported_new':768,'retained':256,'new_export_edge_error':edges_error,'output':str(output)}))
