"""Prepare section-qualified Blender candidates with immutable retained border constraints."""
import argparse
import json
from pathlib import Path
import numpy as np
from PIL import Image
import rasterio
from acquire_expansion import BOUNDS, SECTIONS
from prepare_region import sha256

ROOT=Path(__file__).resolve().parents[4]

def promote(coarse):
    if coarse.ndim!=2 or coarse.shape[0]!=coarse.shape[1] or coarse.shape[0]<2 or not np.isfinite(coarse).all():
        raise ValueError('Expected a finite square source lattice')
    n=coarse.shape[0];out=np.empty((2*n-1,2*n-1),dtype='float32')
    out[::2,::2]=coarse
    out[::2,1::2]=(coarse[:,:-1]+coarse[:,1:])*np.float32(.5)
    out[1::2,::2]=(coarse[:-1,:]+coarse[1:,:])*np.float32(.5)
    out[1::2,1::2]=(coarse[:-1,:-1]+coarse[:-1,1:]+coarse[1:,:-1]+coarse[1:,1:])*np.float32(.25)
    return out

def constrain_border(grid,east,north,constraints):
    """Apply immutable retained constraints only to a new tile perimeter."""
    if grid.shape!=(257,257):raise ValueError('Expected a 257 sample render grid')
    delta=0.;count=0
    border=[(n,c) for n in range(257) for c in (0,256)]+[(r,c) for c in range(1,256) for r in (0,256)]
    for nr,x in border:
        xy=(east+x,north+256-nr)
        if xy in constraints:
            delta=max(delta,abs(float(grid[nr,x])-float(constraints[xy])))
            grid[nr,x]=constraints[xy];count+=1
    return delta,count

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--acquisition',type=Path,required=True);p.add_argument('--baseline',type=Path,required=True);p.add_argument('--output',type=Path,required=True)
    a=p.parse_args();acq=a.acquisition.resolve();base=a.baseline.resolve();out=a.output.resolve()
    if out.exists():raise ValueError('Use a fresh prepared directory')
    dm=json.loads((acq/'DEM/manifest.json').read_text());im=json.loads((acq/'Imagery/manifest.json').read_text());bm=json.loads((base/'baseline.json').read_text())
    if dm['bounds_m']!=BOUNDS or dm['working_crs']!='EPSG:26911' or bm['status']!='verified_readback_baseline':raise ValueError('Unexpected source/baseline contract')
    if sha256(base/bm['render_heights_file'])!=bm['render_heights_sha256']:raise ValueError('Baseline arrays changed')
    for source in dm['sources']:
        for file,hashkey in [('window_file','window_sha256'),('metadata_file','metadata_sha256')]:
            if sha256(acq/'DEM'/source[file])!=source[hashkey]:raise ValueError('Source hash changed')
    if sha256(acq/'DEM/catalog.json')!=dm['catalog_sha256']:raise ValueError('Source catalog changed')
    for rec in im['sections']:
        if sha256(acq/'Imagery'/rec['path'])!=rec['sha256']:raise ValueError('Imagery changed')
    retained=np.load(base/bm['render_heights_file'])
    constraints={}
    # World-height floats for Blender; original normalized values remain authoritative in Unity.
    for row in range(16):
        heights=retained[f'r{row:02}_c00']
        north=4010296-256*(row+1)
        for z in range(257):constraints[(520400,north+z)]=np.float32(heights[z,0]*np.float32(1800)-np.float32(100))
    for col in range(16):
        heights=retained[f'r00_c{col:02}'];east=520400+256*col
        for x in range(257):constraints[(east+x,4010296)]=np.float32(heights[-1,x]*np.float32(1800)-np.float32(100))
    original=np.load(ROOT/'SourceData/Terrain/DeathValley/MacSnapshot2026-10-07/four_slices_v1/terrain.npz')['regular_2m']
    source_grids={}
    for section in dm['sections']:
        name=section['id'];file=acq/'DEM'/section['terrain_file']
        if section['bounds_m']!=SECTIONS[name] or sha256(file)!=section['terrain_sha256']:raise ValueError('Section source mismatch')
        arrays=np.load(file);native=arrays['native_1m'];coarse=arrays['regular_2m']
        if native.shape!=(4097,4097) or coarse.shape!=(2049,2049) or not np.isfinite(native).all() or not np.array_equal(native[::2,::2],coarse):raise ValueError('Native decimation/coverage failed')
        source_grids[name]=coarse
    comparisons={'west_existing':float(abs(source_grids['west'][:,-1]-original[:,0]).max()),
                 'north_existing':float(abs(source_grids['north'][-1,:]-original[0,:]).max()),
                 'northwest_west':float(abs(source_grids['northwest'][-1,:]-source_grids['west'][0,:]).max()),
                 'northwest_north':float(abs(source_grids['northwest'][:,-1]-source_grids['north'][:,0]).max())}
    if any(v!=0 for v in comparisons.values()):raise ValueError('Common measured source boundary changed: '+str(comparisons))
    out.mkdir(parents=True)
    sections=[];global_edges={};edge_error=0.;adjustment=0.;keys=set()
    for name,b in SECTIONS.items():
        arrays={};tiles=[];coarse=source_grids[name]
        for row in range(16):
            for col in range(16):
                ident=f'r{row:02}_c{col:02}';east=b[0]+col*256;north=b[3]-(row+1)*256
                key=f'epsg26911/e{east}/n{north}/size256'
                if key in keys:raise ValueError('Duplicate geographic key')
                keys.add(key)
                grid=promote(coarse[row*128:row*128+129,col*128:col*128+129])
                delta,changed=constrain_border(grid,east,north,constraints)
                adjustment=max(adjustment,delta)
                # Reconstruct from the constrained 2m controls so the one-cell
                # transition into the new tile is the same surface exported by Blender.
                grid=promote(grid[::2,::2].copy())
                constrain_border(grid,east,north,constraints)
                for nr,x in [(n,c) for n in range(257) for c in (0,256)]+[(r,c) for c in range(1,256) for r in (0,256)]:
                    xy=(east+x,north+256-nr)
                    if xy in global_edges:edge_error=max(edge_error,abs(float(global_edges[xy])-float(grid[nr,x])))
                    else:global_edges[xy]=grid[nr,x]
                arrays[ident]=grid
                tiles.append({'id':name+'/'+ident,'local_id':ident,'geographic_key':key,'source_version':section_version(dm,name),
                              'row':row,'col':col,'bounds_m':[east,north,east+256,north+256],
                              'source_spacing_m':2,'mesh_interior_spacing_m':2,'mesh_perimeter_spacing_m':1,
                              'render_samples':257,'retained_boundary_samples':changed})
        if edge_error!=0:raise ValueError('Prepared render boundary mismatch')
        file=out/(name+'_mesh_grids.npz');np.savez_compressed(file,**arrays)
        image_record=next(r for r in im['sections'] if r['section']==name)
        with rasterio.open(acq/'Imagery'/image_record['path']) as src:rgb=src.read([1,2,3]).transpose(1,2,0)
        color=out/(name+'_geographic_color.png');Image.fromarray(rgb.astype('uint8')).save(color)
        sections.append({'id':name,'bounds_m':b,'mesh_grids_file':file.name,'mesh_grids_sha256':sha256(file),
                         'color_file':color.name,'color_sha256':sha256(color),'tiles':tiles})
    manifest={'schema_version':1,'working_crs':'EPSG:26911','bounds_m':BOUNDS,'unity_origin_m':[522448,4008248],
              'height_encoding':{'minimum_m':-100,'range_m':1800,'source_dtype':'little-endian uint16','row_order':'north-first'},
              'baseline_manifest':str((base/'baseline.json').relative_to(ROOT)).replace('\\','/'),
              'baseline_manifest_sha256':sha256(base/'baseline.json'),'source_manifest':str((acq/'DEM/manifest.json').relative_to(ROOT)).replace('\\','/'),
              'source_manifest_sha256':sha256(acq/'DEM/manifest.json'),'imagery_manifest_sha256':sha256(acq/'Imagery/manifest.json'),
              'sections':sections,'proof':{'measured_source_joins_max_m':comparisons,'prepared_new_edges_max_m':edge_error,
              'max_retained_boundary_adjustment_m':adjustment,'new_geographic_keys':len(keys)},
              'adjustment_rule':'Only new border controls and shared corner adopt retained readback world heights; bilinear reconstruction affects the adjacent 2m cell; complete 1m borders remain pinned; raw measured rasters remain immutable',
              'status':'prepared_candidate_requires_native_Blender_and_Unity_proof'}
    # Retained uint16 export half-step plus observed Unity Terrain half-step,
    # with bounded float32 arithmetic precision; this is not a transform allowance.
    budget=1.5*1800/65535+4*np.finfo(np.float32).eps*1800
    manifest['proof']['retained_boundary_quantization_budget_m']=float(budget)
    if adjustment>budget:raise ValueError('Retained adjustment exceeds source/Unity quantization budget')
    (out/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(manifest['proof']))

def section_version(manifest,name):
    return next(s['terrain_sha256'] for s in manifest['sections'] if s['id']==name)

if __name__=='__main__':main()
