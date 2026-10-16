"""Read-only quantitative source/delivery checks plus saved evidence JSON."""
from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib,sys,re
ROOT=Path(sys.argv[1]).resolve();HERE=Path(__file__).resolve().parent
r=json.loads((HERE/'receipt.json').read_text());assert r['complete'] and len(r['maps'])==25 and len(r['renders'])==15
assert hashlib.sha256((HERE/'build_library.py').read_bytes()).hexdigest()==r['script_sha256']
seams=[];normals=[];delivered=[]
for row in r['maps']:
    path=ROOT/row['file'];assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256']
    target=ROOT/'Assets/_Project/Art/Textures/OrganicLibrary20261015'/path.name
    assert target.read_bytes()==path.read_bytes(),target
    a=np.array(Image.open(path));assert a.shape==(2048,2048,4)
    arr=a.astype(float);sx=np.abs(arr[:,0]-arr[:,-1]).mean();sy=np.abs(arr[0]-arr[-1]).mean();dx=np.abs(np.diff(arr,axis=1)).mean();dy=np.abs(np.diff(arr,axis=0)).mean()
    assert sx<=dx*1.6+.1 and sy<=dy*1.6+.1,(row['family'],row['kind'],sx,sy,dx,dy)
    seams.append({'family':row['family'],'kind':row['kind'],'seam_x':float(sx),'seam_y':float(sy),'interior_x':float(dx),'interior_y':float(dy)})
    if row['kind']=='Mask':assert a[:,:,0].max()==0 and a[:,:,2].min()==255 and 0<a[:,:,3].min()<=a[:,:,3].max()<255
    if row['kind']=='Normal':
        n=a[:,:,:3].astype(float)/127.5-1;length=np.sqrt((n*n).sum(2));assert np.abs(length-1).mean()<.015
        normals.append({'family':row['family'],'normal_length_mean':float(length.mean()),'normal_blue_min':int(a[:,:,2].min())})
    delivered.append({'file':str(target.relative_to(ROOT)).replace('\\','/'),'sha256':row['sha256']})
paths=list((ROOT/'Assets/_Project/Art/Textures/OrganicLibrary20261015').glob('*.png'))+list((ROOT/'Assets/_Project/Materials/SurfaceLibrary/OrganicLibrary20261015').glob('*.mat'))
assert len(paths)==40
guids=[]
for p in paths:
    meta=Path(str(p)+'.meta');assert meta.exists();guids.append(re.search(r'guid: ([0-9a-f]{32})',meta.read_text()).group(1))
assert len(guids)==len(set(guids))
# Check collisions against the rest of Assets with one targeted metadata scan.
import subprocess
args=['rg','-l','--glob','*.meta','-e','guid: ('+'|'.join(guids)+')','Assets']
matches=subprocess.check_output(args,cwd=ROOT).decode().splitlines();assert len(matches)==40,len(matches)
(HERE/'seam_analysis.json').write_text(json.dumps(seams,indent=2),encoding='utf-8',newline='\n')
(HERE/'texture_validation.json').write_text(json.dumps({'maps':25,'materials':15,'resolution':[2048,2048],'seam_test':'wrap-step mean <= 1.6 * interior neighbor mean + 0.1 per axis; all maps pass','packed_channels_valid':True,'normal_vectors':normals,'native_asset_meta_pairs':40,'unique_asset_guids':40,'GUID_collisions_elsewhere_in_Assets':0,'delivered_texture_hashes':delivered},indent=2),encoding='utf-8',newline='\n')
print('LIBRARY_VALIDATED: 25 maps, 15 materials, wrap continuity, normals, packing, delivery hashes and 40 unique GUID/meta pairs')
