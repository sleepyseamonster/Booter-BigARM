"""Append expanded playable coverage only after current production Unity readback proof."""
import hashlib
import json
from pathlib import Path
import re
import numpy as np

ASSET_ROOT='Assets/_Project/Art/Terrain/WestNorthNorthwest2026-10-07'

def digest(path):
    h=hashlib.sha256()
    with path.open('rb') as stream:
        for data in iter(lambda:stream.read(1024*1024),b''):h.update(data)
    return h.hexdigest()

def owned_file(root,base,relative):
    if Path(relative).is_absolute():raise ValueError('Expected relative owned source path')
    p=(base/relative).resolve()
    if not p.is_relative_to(base.resolve()) or not p.is_relative_to(root.resolve()):raise ValueError('Source escaped its owner')
    return p

def append_verified_expansion(root,scene,text,retained_tiles):
    root=Path(root).resolve();source=root/ASSET_ROOT/'Source';manifest=source/'manifest.json'
    m=json.loads(manifest.read_text(encoding='utf-8'))
    if m['schema_version']!=1 or m['bounds_m']!=[516304,4006200,524496,4014392] or m['unity_origin_m']!=[522448,4008248] or m['new_tile_count']!=768:
        raise ValueError('Unsupported expansion manifest')
    proof=json.loads((source/'unity_production_validation.json').read_text(encoding='utf-8'))
    if proof['status']!='validated_full_grid_and_collider_samples' or proof['scene']!='Assets/_Project/Scenes/Production/GreaterWasteland.unity' or proof['scene_sha256']!=digest(scene) or proof['manifest_sha256']!=digest(manifest) or proof['terrains']!=1024 or proof['edges']!=1984 or proof['collider_samples']!=25600:
        raise ValueError('Current integrated Unity proof missing or stale')
    tiles=list(retained_tiles);paths=[];keys={t['geographic_key'] for t in tiles};cells={};seen=set()
    all_tiles=m['new_tiles']+m['retained_tiles']
    for t in all_tiles:
        render=owned_file(root,source,t['render_file'])
        if digest(render)!=t['render_sha256']:raise ValueError('Render source changed')
        a=np.fromfile(render,dtype='<f4').reshape(257,257)
        if not np.isfinite(a).all() or a.min()<0 or a.max()>1:raise ValueError('Invalid normalized source')
        east=int(t['position'][0]+522448);north=int(t['position'][2]+4008248)
        expected=f'epsg26911/e{east}/n{north}/size256'
        if t['geographic_key']!=expected or (east,north) in cells:raise ValueError('Duplicate or incorrect geographic identity')
        cells[(east,north)]=a
        if not t.get('section'):continue
        if expected in keys or t['section'] not in ('west','north','northwest'):raise ValueError('Unexpected new terrain section/key')
        keys.add(expected)
        for f,h in [('height_file','height_sha256'),('color_file','color_sha256')]:
            if digest(owned_file(root,source,t[f]))!=t[h]:raise ValueError('Source hash changed')
        asset=(root/t['data_path']).resolve()
        if not asset.is_relative_to((root/ASSET_ROOT/'TerrainData').resolve()) or not asset.exists():raise ValueError('Owned TerrainData missing')
        meta=asset.with_name(asset.name+'.meta');guid=re.search(r'^guid: ([0-9a-f]{32})$',meta.read_text(encoding='utf-8'),re.M)
        if not guid or guid[1] in seen or guid[1] not in text:raise ValueError('Missing, duplicate or unlinked TerrainData GUID')
        seen.add(guid[1]);paths.append(asset)
        tiles.append({'id':t['section']+'/'+t['local_id'],'legacy_id':t['local_id'],'geographic_key':expected,'section':t['section'],
                      'quarter':('N' if int(t['local_id'][1:3])<8 else 'S')+('W' if int(t['local_id'][5:7])<8 else 'E'),
                      'source_version':t['source_version'],'bounds_m':t['bounds_m'],'source_spacing_m':2,'source_samples':129,'render_samples':257,
                      'height_source':str((source/t['height_file']).relative_to(root)).replace('\\','/'),'height_sha256':t['height_sha256'],
                      'color_source':str((source/t['color_file']).relative_to(root)).replace('\\','/'),'color_sha256':t['color_sha256'],
                      'integrity':'source_hashes_and_current_Unity_production_readback_verified'})
    edges=0
    for (east,north),a in cells.items():
        for cell,edge,index in [((east+256,north),a[:,-1],(slice(None),0)),((east,north+256),a[0,:],(-1,slice(None)))]:
            if cell in cells:
                if not np.array_equal(edge,cells[cell][index]):raise ValueError('Prepared normalized export seam changed')
                edges+=1
    if len(tiles)!=1024 or len(paths)!=768 or edges!=1984:raise ValueError('Incomplete expanded coverage')
    return tiles,paths,{'height_files':1024,'color_files':1024,'shared_edges_checked':1984,'max_encoded_edge_difference':0,
                        'unity_readback_proof_sha256':digest(source/'unity_production_validation.json'),
                        'limits':'Current integrated Unity Terrain/collider readback and source hashes; no Player traversal/performance acceptance'}
