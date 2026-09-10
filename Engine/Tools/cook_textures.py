"""Cook immutable semantic recipes into a new portable texture bundle (KTX1 RGBA8)."""
import argparse
import hashlib
import json
import re
import subprocess
import zlib
from pathlib import Path
from common import ROOT, inside, write_json

CHANNELS={'base_color':'srgb_rgb_linear_alpha','normal':'xyz_linear_alpha','packed_surface':'ao_roughness_height_alpha','height':'scalar_linear','mask':('crack_halo_deposit_alpha','uninterpreted_linear_mask')}
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def validate(recipe):
    fields={'id','source','role','transfer','channels','alpha','normal_green','dimensions','filter','edge','format'}
    if set(recipe)!=fields or recipe['role'] not in CHANNELS:raise ValueError('Unsupported recipe fields or role')
    if not re.fullmatch(r'[a-z0-9][a-z0-9/_-]{0,180}',recipe['id']):raise ValueError('Invalid logical texture ID')
    allowed=CHANNELS[recipe['role']]
    if isinstance(allowed,str):allowed=(allowed,)
    if recipe['transfer']!=('srgb' if recipe['role']=='base_color' else 'linear') or recipe['channels'] not in allowed:raise ValueError('Role/channel/transfer mismatch')
    if any(recipe[k]!=v for k,v in {'alpha':'independent_linear','normal_green':'positive_y','dimensions':'preserve','filter':'area_box_v1','edge':'repeat','format':'rgba8_ktx1'}.items()):raise ValueError('Unsupported import policy')
    if not isinstance(recipe['source'],str) or not recipe['source'].startswith('Assets/') or '..' in Path(recipe['source']).parts:raise ValueError('Source must be an Engine asset')

def cook(cooker,recipes,out):
    data=json.loads(recipes.read_text())
    if data.get('schema_version')!=1 or set(data)!={'schema_version','recipes'}:raise ValueError('Unsupported recipe document')
    if out.exists():raise ValueError('Bundle output must be new')
    identities=set()
    for recipe in data['recipes']:
        validate(recipe)
        if recipe['id'] in identities:raise ValueError('Duplicate asset identity')
        identities.add(recipe['id']);inside(recipe['source']).resolve(strict=True)
    out.mkdir(parents=True); (out/'textures').mkdir()
    tool_inputs={'cooker_sha256':sha(cooker),'orchestrator_sha256':sha(Path(__file__)),'probe_lock_sha256':sha(ROOT/'Research/probe-lock.json'),'recipe_version':1}
    records=[]
    for recipe in data['recipes']:
        source=inside(recipe['source']); source_hash=sha(source)
        identity={'recipe':recipe,'source_sha256':source_hash,'tools':tool_inputs}
        key=hashlib.sha256(json.dumps(identity,sort_keys=True,separators=(',',':')).encode()).hexdigest()
        output=out/'textures'/(key+'.ktx')
        info=json.loads(subprocess.check_output([str(cooker),str(source),str(output),recipe['role']],text=True))
        if sha(source)!=source_hash:raise ValueError('Source changed during cook')
        payload=output.read_bytes()
        records.append({'id':recipe['id'],'key':key,'file':output.relative_to(out).as_posix(),'role':recipe['role'],'srgb':recipe['transfer']=='srgb',
                        'width':info['width'],'height':info['height'],'mips':info['mips'],'resident_bytes':info['resident_bytes'],
                        'file_bytes':len(payload),'crc32':zlib.crc32(payload)&0xffffffff,'sha256':hashlib.sha256(payload).hexdigest()})
    write_json(out/'catalog.json',{'kind':'engine.texture-catalog','version':1,'payload':{'textures':records}})
    write_json(out/'cook.json',{'schema_version':1,'tool_inputs':tool_inputs,'recipes_sha256':sha(recipes),'records':records,
                              'source_sha256':{recipe['source']:sha(inside(recipe['source'])) for recipe in data['recipes']},
                              'proof_limit':'Offline semantic cooking and parser readback; GPU residency/material response verified separately.'})
    return {'textures':len(records),'resident_bytes':sum(row['resident_bytes'] for row in records),'catalog':str(out/'catalog.json')}

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--cooker',required=True);parser.add_argument('--recipes',required=True);parser.add_argument('--out',required=True)
    args=parser.parse_args()
    try:print(json.dumps(cook(inside(args.cooker),inside(args.recipes),inside(args.out)),indent=2))
    except (ValueError,OSError,subprocess.CalledProcessError) as error:parser.error(str(error))
if __name__=='__main__':main()
