"""Match new geographic color to retained study macro color without changing source imagery.

Uses the existing broad-color matching convention over one common image canvas.
Retained SE pixels are immutable; corrections near its joins are feathered in new terrain only.
"""
import argparse
import json
from pathlib import Path
import shutil
import numpy as np
from PIL import Image,ImageFilter
import rasterio
from rasterio.transform import from_bounds
from rasterio.warp import reproject
from rasterio.enums import Resampling
from prepare_region import sha256

ROOT=Path(__file__).resolve().parents[4]

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--prepared',type=Path,required=True);p.add_argument('--acquisition',type=Path,required=True);p.add_argument('--output',type=Path,required=True)
    a=p.parse_args();out=a.output.resolve();prepared=a.prepared.resolve();acq=a.acquisition.resolve()
    if out.exists():raise ValueError('Use a fresh color candidate directory')
    m=json.loads((prepared/'manifest.json').read_text())
    source=ROOT/'SourceData/Terrain/DeathValley/MacSnapshot2026-10-07'
    parent=source/'expanded_v2/corridor_40m/prepared/corridor/naip_landsat_composite.png'
    parent_meta=json.loads(parent.with_suffix('.json').read_text())
    parent_bounds=parent_meta['bounds_epsg26911']
    old=np.asarray(Image.open(source/'four_slices_v1/geographic_color.png').convert('RGB'))
    canvas=np.zeros((4096,4096,3),dtype='uint8')
    placements={'northwest':(0,0),'north':(0,2048),'west':(2048,0)}
    for s in m['sections']:
        r,c=placements[s['id']];canvas[r:r+2048,c:c+2048]=np.asarray(Image.open(prepared/s['color_file']).convert('RGB'))
    with rasterio.open(source/'four_slices_v1/naip_2m.tif') as d:canvas[2048:,2048:]=d.read([1,2,3]).transpose(1,2,0)
    parent_pixels=np.asarray(Image.open(parent).convert('RGB'));sampled=np.empty_like(canvas)
    for channel in range(3):
        reproject(parent_pixels[:,:,channel],sampled[:,:,channel],src_transform=from_bounds(*parent_bounds,parent_pixels.shape[1],parent_pixels.shape[0]),
                  src_crs='EPSG:26911',dst_transform=from_bounds(*m['bounds_m'],4096,4096),dst_crs='EPSG:26911',resampling=Resampling.bilinear)
    fine_low=np.asarray(Image.fromarray(canvas).filter(ImageFilter.GaussianBlur(50)),dtype='float32')
    parent_low=np.asarray(Image.fromarray(sampled).filter(ImageFilter.GaussianBlur(50)),dtype='float32')
    matched=np.clip(canvas.astype('float32')+parent_low-fine_low,0,255)
    # The retained composite's actual pixel color, not the NIR band, anchors its joins.
    anchor_delta=old.astype('float32')-matched[2048:,2048:]
    rows,cols=np.indices((4096,4096));nearest_r=np.clip(rows-2048,0,2047);nearest_c=np.clip(cols-2048,0,2047)
    dx=np.maximum(2048-cols,0);dy=np.maximum(2048-rows,0)
    weight=np.clip(1-np.hypot(dx,dy)/128,0,1).astype('float32') # 256m maximum new-only transition.
    matched+=anchor_delta[nearest_r,nearest_c]*weight[:,:,None]
    result=np.clip(np.rint(matched),0,255).astype('uint8')
    result[2048:,2048:]=old
    result[2048:,2047]=old[:,0];result[2047,2048:]=old[0,:];result[2047,2047]=old[0,0]
    out.mkdir(parents=True)
    profiles={}
    for s in m['sections']:
        r,c=placements[s['id']];file=out/s['color_file'];Image.fromarray(result[r:r+2048,c:c+2048]).save(file)
        s['color_sha256']=sha256(file)
        shutil.copy2(prepared/s['mesh_grids_file'],out/s['mesh_grids_file'])
    for name,left,right in [('west_existing',result[2048:,2047],old[:,0]),('north_existing',result[2047,2048:],old[0,:]),
                            ('northwest_west',result[2047,:2048],result[2048,:2048]),('northwest_north',result[:2048,2047],result[:2048,2048])]:
        difference=np.abs(left.astype('int16')-right.astype('int16'))
        profiles[name]={'mean_rgb_step':float(difference.mean()),'max_rgb_step':int(difference.max())}
    report={'schema_version':1,'purpose':'Geographic review color matching; raw NAIP and existing accepted color remain unchanged',
            'parent_file':str(parent.relative_to(ROOT)).replace('\\','/'),'parent_sha256':sha256(parent),'parent_bounds_m':parent_bounds,
            'parent_spacing_m':20,'fine_pixel_spacing_m':2,'macro_blur_m':100,'retained_transition_max_m':256,
            'retained_color_sha256':sha256(source/'four_slices_v1/geographic_color.png'),'profiles':profiles,
            'median_abs_rgb_adjustment':float(np.median(abs(result.astype('int16')-canvas.astype('int16')))),
            'limits':'Color matching is authored review treatment; it does not alter measured elevation or imply new surveyed color detail'}
    m['color_matching_file']='color_matching.json';m['status']='color_matched_candidate_requires_native_Blender_and_Unity_proof'
    (out/'color_matching.json').write_text(json.dumps(report,indent=2)+'\n')
    m['color_matching_sha256']=sha256(out/'color_matching.json')
    (out/'manifest.json').write_text(json.dumps(m,indent=2)+'\n')
    print(json.dumps(report))

if __name__=='__main__':main()
