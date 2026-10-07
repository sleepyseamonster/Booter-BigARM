"""Acquire a new coarse USGS relief snapshot for the offline Unity developer map.

Never changes terrain or Blender files. Existing output directories are refused.
Raw service proof stays in Logs; the portable grid and manifest stay beside this tool.
"""
import argparse
import hashlib
import json
import urllib.parse
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
import rasterio

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p/'ProjectSettings/ProjectVersion.txt').exists())
SERVICE = 'https://elevation.nationalmap.gov/arcgis/rest/services/3DEPElevation/ImageServer'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, default=HERE/'visualizer_data')
    args = parser.parse_args()
    output = args.output.resolve()
    if not output.is_relative_to(HERE) or output == HERE or output.exists():
        raise ValueError('Use a new output directory inside Docs/DeathValley; existing snapshots are preserved')
    catalog = json.loads((HERE/'coverage_catalog.json').read_text(encoding='utf-8'))
    region = next(r for r in catalog['regions'] if r['id']=='expanded_region')
    x0,y0,x1,y1 = region['bounds_m']
    spacing = 200
    width,height = int((x1-x0)/spacing)+1,int((y1-y0)/spacing)+1
    requested = [x0-spacing/2,y0-spacing/2,x1+spacing/2,y1+spacing/2]
    params = {'bbox':','.join(map(str,requested)), 'bboxSR':26911,'imageSR':26911,
              'size':f'{width},{height}','format':'tiff','pixelType':'F32','f':'json',
              'interpolation':'+RSP_BilinearInterpolation',
              'renderingRule':json.dumps({'rasterFunction':'None'})}
    request_url = SERVICE+'/exportImage?'+urllib.parse.urlencode(params)
    proof = ROOT/'Logs/DeathValleyVisualizer'/datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%f')
    proof.mkdir(parents=True,exist_ok=False)
    request = urllib.request.Request(request_url,headers={'User-Agent':'BooterBigARM-DeveloperMap/1.0'})
    with urllib.request.urlopen(request,timeout=60) as response:
        raw = response.read()
    (proof/'service_response.json').write_bytes(raw)
    record = json.loads(raw)
    if 'error' in record or 'href' not in record:
        raise ValueError(f'USGS export failed: {record}')
    with urllib.request.urlopen(record['href'],timeout=90) as response:
        (proof/'overview.tif').write_bytes(response.read())
    with rasterio.open(proof/'overview.tif') as src:
        if src.crs.to_epsg()!=26911 or (src.width,src.height)!=(width,height):
            raise ValueError('Unexpected CRS or dimensions')
        if not np.allclose(tuple(src.bounds),requested,rtol=0,atol=.01) or not np.allclose(src.res,[spacing,spacing],rtol=0,atol=.01):
            raise ValueError('Unexpected raster alignment')
        data = src.read(1,masked=True)
        if np.ma.getmaskarray(data).any() or not np.isfinite(data).all() or data.min() < -1000 or data.max()>10000:
            raise ValueError('Missing or implausible elevation samples')
        payload = np.asarray(data,dtype='<f4').tobytes()
    output.mkdir(parents=True,exist_ok=False)
    (output/'overview.f32').write_bytes(payload)
    built = next(r for r in catalog['regions'] if r['id']=='badwater')['bounds_m']
    manifest = {'schema_version':1,'working_crs':'EPSG:26911','bounds_m':region['bounds_m'],
                'spacing_m':spacing,'width':width,'height':height,'grid_file':'overview.f32',
                'grid_sha256':hashlib.sha256(payload).hexdigest(),'encoding':'little-endian float32, north-to-south rows, west-to-east columns',
                'min_height_m':float(data.min()),'max_height_m':float(data.max()),
                'unity_origin_m':[(built[0]+built[2])/2,(built[1]+built[3])/2],
                'scene_guid':catalog['unity_scene']['guid'],'retrieved_utc':datetime.now(timezone.utc).isoformat(),
                'source_service':SERVICE,'request_url':request_url,'service_response':record,
                'service_response_sha256':hashlib.sha256(raw).hexdigest(),
                'raster_sha256':hashlib.sha256((proof/'overview.tif').read_bytes()).hexdigest(),
                'limits':'New coarse visualization snapshot at Blender overview bounds. Not the original Blender grid, detailed game terrain, survey accuracy or gameplay collision. Dynamic service mixes source resolutions; vertical datum requires source-specific review.'}
    (output/'developer_map.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8',newline='\n')
    print(json.dumps({'output':str(output),'shape':[height,width],'height_range_m':[float(data.min()),float(data.max())],'sha256':manifest['grid_sha256']}))


if __name__=='__main__':
    main()
