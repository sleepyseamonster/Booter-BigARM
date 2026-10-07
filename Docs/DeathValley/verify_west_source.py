"""Verify a proposed west batch from bounded remote 3DEP windows.

Writes a float grid and source-window proof under ignored Logs only. Does not
select the expansion, import terrain, change measured heights or edit Unity.
"""
import argparse
import hashlib
import json
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
import rasterio
from rasterio.enums import Resampling
from rasterio.transform import from_origin
from rasterio.warp import reproject
from rasterio.windows import from_bounds, transform as window_transform

from coverage_validation import fresh_output, source_records

HERE=Path(__file__).resolve().parent
ROOT=next(p for p in HERE.parents if (p/'ProjectSettings/ProjectVersion.txt').exists())
OUT=ROOT/'Logs/DeathValleyInventory/west-source-proof'
BOUNDS=[518352,4006200,520400,4008248]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output',type=Path,default=OUT,help='New subdirectory of Logs/DeathValleyInventory; existing directories are refused.')
    args=parser.parse_args()
    records=source_records(json.loads((HERE/'west_raster_headers.json').read_text(encoding='utf-8'))['sources'])
    out=fresh_output(ROOT,args.output)
    xmin,ymin,xmax,ymax=BOUNDS
    spacing=2
    # Reproject at native 1 m spacing, then decimate. Reprojecting directly to
    # 2 m asks GDAL to filter a larger footprint and changes shared samples.
    target=from_origin(xmin-0.5,ymax+0.5,1,1)
    native_side=xmax-xmin+1
    prepared=[];snapshots=[]
    for ident,record in records:
        with rasterio.Env(GDAL_DISABLE_READDIR_ON_OPEN='EMPTY_DIR',CPL_VSIL_CURL_ALLOWED_EXTENSIONS='.tif',GDAL_HTTP_TIMEOUT='40'):
            with rasterio.open(record['url']) as src:
                if src.crs.to_epsg()!=26911 or src.res!=(1.,1.):
                    raise ValueError('Unexpected raster grid')
                clipped=[max(xmin-4,src.bounds.left+1),max(ymin-4,src.bounds.bottom+1),min(xmax+4,src.bounds.right-1),min(ymax+4,src.bounds.top-1)]
                window=from_bounds(*clipped,src.transform).round_offsets().round_lengths()
                pixels=src.read(1,window=window)
                transform=window_transform(window,src.transform)
                nodata=src.nodata
        path=out/(ident+'_window.tif')
        with rasterio.open(path,'w',driver='GTiff',width=pixels.shape[1],height=pixels.shape[0],count=1,dtype='float32',crs='EPSG:26911',transform=transform,nodata=nodata,compress='deflate') as dst:
            dst.write(pixels.astype('float32'),1)
        metadata_url='https://thor-f5.er.usgs.gov/ngtoc/metadata/waf/elevation/1_meter/geotiff/CA_FEMAR9Southeast_D24/USGS_1M_11_'+ident+'_CA_FEMAR9Southeast_D24.xml'
        request=urllib.request.Request(metadata_url,headers={'User-Agent':'BooterBigARM-DeathValleyInventory/1.0'})
        with urllib.request.urlopen(request,timeout=40) as response:
            metadata=response.read()
        metadata_path=out/(ident+'_metadata.xml');metadata_path.write_bytes(metadata)
        decoded=metadata.decode('utf-8')
        if 'NAVD88' not in decoded or 'bare-earth' not in decoded:
            raise ValueError('Vertical datum or surface type not established')
        grid=np.full((native_side,native_side),np.nan,dtype='float32')
        reproject(pixels,grid,src_transform=transform,src_crs='EPSG:26911',src_nodata=nodata,dst_transform=target,dst_crs='EPSG:26911',dst_nodata=np.nan,resampling=Resampling.bilinear)
        prepared.append(grid)
        snapshots.append({'id':ident,'url':record['url'],'window_path':path.relative_to(ROOT).as_posix(),'window_sha256':sha(path),'metadata_url':metadata_url,'metadata_path':metadata_path.relative_to(ROOT).as_posix(),'metadata_sha256':sha(metadata_path),'vertical_reference':'NAVD88 metres','surface':'bare-earth'})
    west,east=prepared
    overlap=np.isfinite(west)&np.isfinite(east)
    if not overlap.any(): raise ValueError('No source overlap')
    xs=xmin+np.arange(native_side)
    native=np.where(xs[None,:]<520000,west,east)
    if not np.isfinite(native).all(): raise ValueError('Candidate has missing elevation samples')
    chosen=native[::spacing,::spacing]
    # Compare the proposed eastern boundary with the retained scene-source exports.
    catalog=json.loads((HERE/'coverage_catalog.json').read_text())
    errors=[]
    for tile in catalog['unity_tiles']:
        if tile['legacy_id'] not in [f'r{r:02d}_c00' for r in range(8,16)]: continue
        samples=tile['source_samples']
        values=np.fromfile(ROOT/tile['height_source'],dtype='<u2').reshape(samples,samples)[:,0]
        heights=-100+values.astype(float)*(1800/65535)
        ytop=tile['bounds_m'][3]
        start=(ymax-ytop)//spacing
        edge=chosen[start:start+129,-1]
        if len(edge)!=129: raise ValueError('Candidate edge mismatch')
        errors.extend(abs(edge-heights).tolist())
    maximum=max(errors)
    # The retained height exports encode a 1800 m range in 16 bits.
    if maximum>1800/65535:
        raise ValueError(f'New source differs from retained border beyond one encoding step: {maximum} m')
    grid_path=out/'west_candidate_2m.npz'
    np.savez_compressed(grid_path,elevation=chosen)
    proof={'acquired_utc':datetime.now(timezone.utc).isoformat(),'status':'source_candidate_verified_not_imported_not_selected','bounds_m':BOUNDS,'working_crs':'EPSG:26911','source_spacing_m':1,'prepared_spacing_m':spacing,'shape':list(chosen.shape),'sample_count':int(chosen.size),'nodata_count':int((~np.isfinite(chosen)).sum()),'min_height_m':float(chosen.min()),'max_height_m':float(chosen.max()),'source_overlap_max_difference_m':float(abs(west[overlap]-east[overlap]).max()),'retained_boundary_max_difference_m':maximum,'boundary_tolerance_m':1800/65535,'grid_path':grid_path.relative_to(ROOT).as_posix(),'grid_sha256':sha(grid_path),'sources':snapshots,'limits':'No Unity import, material, collision, route, runtime or independent survey proof. New source is not assumed byte-identical to the original external snapshot.'}
    (out/'proof.json').write_text(json.dumps(proof,indent=2)+'\n',encoding='utf-8',newline='\n')
    (HERE/'west_source_assessment.json').write_text(json.dumps(proof,indent=2)+'\n',encoding='utf-8',newline='\n')
    print(json.dumps(proof))


if __name__=='__main__':
    main()
