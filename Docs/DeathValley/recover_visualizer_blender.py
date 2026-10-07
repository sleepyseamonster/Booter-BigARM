"""Recover the saved Blender overview heights and exact packed aerial images.

Read-only source extraction. Validates coordinates, transforms, coverage and
image sizes before publishing the developer-map descriptor. No Unity terrain edit.
"""
import argparse
import hashlib
import io
import importlib.util
import json
from datetime import datetime,timezone
from pathlib import Path

import numpy as np
from PIL import Image
from saved_blend_reader import SavedBlend

HERE=Path(__file__).resolve().parent
ROOT=next(p for p in HERE.parents if (p/'ProjectSettings/ProjectVersion.txt').exists())


def resolve_saved_source(recorded_path,expected_hash=None):
    module_path=ROOT/'Tools/Repository/repository_paths.py'
    spec=importlib.util.spec_from_file_location('death_valley_repository_paths',module_path)
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    relocations=module.Relocations.from_repository(ROOT)
    return relocations.version_path(ROOT,recorded_path,expected_hash) if expected_hash else relocations.path(ROOT,recorded_path)


def recover(source,catalog):
    blend=SavedBlend(source)
    scenes=[b for b in blend.blocks if b['code']==b'SC\0\0']
    if len(scenes)!=1:raise ValueError('Expected one saved Blender scene')
    properties=blend.properties(scenes[0]['data'],'Scene')
    bounds=json.loads(properties['study_bounds_epsg26911']);origin=json.loads(properties['study_origin_epsg26911'])
    regions={r['id']:r for r in catalog['regions']}
    if bounds!=regions['expanded_region']['bounds_m'] or origin!=[(bounds[0]+bounds[2])/2,(bounds[1]+bounds[3])/2]:
        raise ValueError('Saved Blender coordinates disagree with inventory')
    width,height=int((bounds[2]-bounds[0])/200)+1,int((bounds[3]-bounds[1])/200)+1
    grid=np.full((height,width),np.nan,dtype='<f4');tiers=np.zeros(grid.shape,dtype='uint8');mesh_counts={}
    objects=[b for b in blend.blocks if b['code']==b'OB\0\0']
    for tier,(prefix,region_id) in enumerate((('Overview_','expanded_region'),('BadwaterCorridor40m_','corridor'),('BadwaterPilot20m_','pilot'),('BadwaterCanyonPatch10m_','canyon')),1):
        count=0
        for block in objects:
            obj=block['data'];name=blend.text(blend.field(obj,'Object','id'),'ID','name[258]')[2:]
            if not name.startswith(prefix):continue
            if blend.number(obj,'Object','*parent') or any(struct_value for key in ('loc[3]','dloc[3]','rot[3]','drot[3]') for struct_value in np.frombuffer(blend.field(obj,'Object',key),dtype='<f4')):
                raise ValueError('Terrain object has an unexpected transform or parent')
            if blend.number(obj,'Object','rotmode','<h')!=1 or any(not np.array_equal(np.frombuffer(blend.field(obj,'Object',key),dtype='<f4'),[1,1,1]) for key in ('size[3]','dscale[3]')):
                raise ValueError('Terrain object has unexpected scale')
            props=blend.properties(obj,'Object')
            if json.loads(props['source_bounds_epsg26911'])!=regions[region_id]['bounds_m']:
                raise ValueError('Saved mesh bounds differ from recorded source layer')
            mesh=blend.pointer(blend.number(obj,'Object','*data'))
            positions=np.frombuffer(blend.positions(mesh),dtype='<f4').reshape(-1,3)
            if not np.isfinite(positions).all() or positions[:,2].min()<-1000 or positions[:,2].max()>10000:
                raise ValueError('Invalid saved terrain vertex')
            cols=(positions[:,0].astype('float64')+origin[0]-bounds[0])/200
            rows=(bounds[3]-positions[:,1].astype('float64')-origin[1])/200
            aligned=(np.abs(cols-np.rint(cols))<1e-6)&(np.abs(rows-np.rint(rows))<1e-6)
            c=np.rint(cols[aligned]).astype(int);r=np.rint(rows[aligned]).astype(int);z=positions[aligned,2]
            if (c<0).any() or (c>=width).any() or (r<0).any() or (r>=height).any():raise ValueError('Saved terrain lies outside overview bounds')
            same=tiers[r,c]==tier
            if same.any() and np.max(np.abs(grid[r[same],c[same]]-z[same]))>.001:
                raise ValueError('Same-tier saved terrain vertices disagree')
            grid[r,c]=z;tiers[r,c]=tier;count+=1
        if not count:raise ValueError('Missing saved terrain layer: '+prefix)
        mesh_counts[region_id]=count
    if not np.isfinite(grid).all():raise ValueError('Saved mesh recovery has uncovered coarse samples')
    packed={}
    for block in blend.blocks:
        if block['code']!=b'IM\0\0':continue
        name=blend.text(block['data'],'Image','name[1024]');payload=blend.packed_image(block['data'])
        if payload is not None:packed[name]=payload
    textures=[]
    for ident,marker,band in (('expanded_region','/overview/',0),('corridor','/corridor/',1000),('pilot','/pilot/',300),('canyon','/canyon_patch/',300)):
        matches=[(name,payload) for name,payload in packed.items() if marker in name]
        if len(matches)!=1:raise ValueError('Expected one packed aerial image for '+ident)
        original,payload=matches[0]
        with Image.open(io.BytesIO(payload)) as image:
            image.verify()
        with Image.open(io.BytesIO(payload)) as image:
            if image.mode!='RGB':raise ValueError('Unexpected packed image encoding')
            w,h=image.size
        b=regions[ident]['bounds_m'];x_spacing=(b[2]-b[0])/w;y_spacing=(b[3]-b[1])/h
        if x_spacing!=y_spacing:raise ValueError('Packed aerial image has incompatible aspect')
        textures.append({'id':ident,'file':ident+'.png','sha256':hashlib.sha256(payload).hexdigest(),
                         'bounds_m':b,'width':w,'height':h,'spacing_m':x_spacing,'blend_band_m':band,
                         'original_packed_path':original,'payload':payload})
    return grid,textures,{'saved_bounds_m':bounds,'saved_origin_m':origin,'mesh_counts':mesh_counts,
                           'sample_tier_counts':{str(t):int((tiers==t).sum()) for t in range(1,5)},
                           'sample_count':grid.size,'uncovered_samples':0}


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--blend',type=Path,default=resolve_saved_source('Docs/Agents/Blender/studies/DeathValley/DeathValleyExploreExpanded.blend'))
    args=parser.parse_args()
    folder=HERE/'visualizer_data';output=folder/'blender_recovered';descriptor=folder/'developer_map.json';backup=folder/'usgs_overview_snapshot.json'
    if output.exists() or backup.exists():raise ValueError('Recovered source directory or prior snapshot receipt exists; preserve it before a new version')
    original_descriptor=descriptor.read_bytes();manifest=json.loads(original_descriptor)
    catalog=json.loads((HERE/'coverage_catalog.json').read_text(encoding='utf-8'))
    source=args.blend.read_bytes();source_hash=hashlib.sha256(source).hexdigest()
    grid,textures,proof=recover(source,catalog)
    if args.blend.read_bytes()!=source:raise ValueError('Source Blender file changed during extraction')
    payload=grid.tobytes();output.mkdir(exist_ok=False)
    (output/'blender_overview.f32').write_bytes(payload)
    for texture in textures:(output/texture['file']).write_bytes(texture.pop('payload'))
    proof.update({'source_blend':args.blend.resolve().relative_to(ROOT).as_posix(),'source_blend_sha256':source_hash,
                  'recovered_utc':datetime.now(timezone.utc).isoformat(),'method':'Saved SDNA mesh attributes and exact packed image bytes; no Blender execution or source modification',
                  'grid_sha256':hashlib.sha256(payload).hexdigest(),'textures':textures,
                  'limits':'200 m lattice sampled from saved overview and nested detail meshes, finest available layer wins. Simplified display still uses an 800 m mesh; no full precision GIS archive or gameplay collision claim.'})
    (output/'recovery_proof.json').write_text(json.dumps(proof,indent=2)+'\n',encoding='utf-8',newline='\n')
    backup.write_bytes(original_descriptor)
    manifest.update({'grid_file':'blender_recovered/blender_overview.f32','grid_sha256':proof['grid_sha256'],
                     'min_height_m':float(grid.min()),'max_height_m':float(grid.max()),'retrieved_utc':proof['recovered_utc'],
                     'height_source':'saved_blender_meshes','source_blend_sha256':source_hash,
                     'textures':[{**t,'file':'blender_recovered/'+t['file']} for t in textures],
                     'source_service':'Recovered saved Blender geometry and packed Landsat / NAIP imagery',
                     'limits':proof['limits']})
    # New active source has its own proof; retain the prior service proof in the backup.
    for key in ('request_url','service_response','service_response_sha256','raster_sha256'):manifest.pop(key,None)
    descriptor.write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8',newline='\n')
    print(json.dumps({k:proof[k] for k in ('mesh_counts','sample_tier_counts','sample_count','grid_sha256')}))


if __name__=='__main__':main()
