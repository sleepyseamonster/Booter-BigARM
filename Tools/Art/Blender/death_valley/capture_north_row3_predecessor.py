"""Capture a committed accepted predecessor without duplicating full native grids."""
import argparse
import json
from pathlib import Path
import shutil
import subprocess
import numpy as np
from PIL import Image
from north_row3_contract import ROOT, contract, sha256, validate_geographic_records
from north_row3_native_references import read_native_reference

SCENE = 'Assets/_Project/Scenes/Production/GreaterWasteland.unity'

def capture(step, date, predecessor):
    c = contract(step, date)
    if subprocess.check_output(['git','branch','--show-current'],cwd=ROOT,text=True).strip() != 'main':
        raise ValueError('Only the authorized main checkout is supported')
    subprocess.run(['git','diff','--quiet','HEAD','--',SCENE],cwd=ROOT,check=True)
    m = json.loads(predecessor.read_text(encoding='utf-8'))
    proof_path = predecessor.parent/'unity_production_validation.json'
    proof = json.loads(proof_path.read_text(encoding='utf-8'))
    previous_step = 6 if step == 1 else step - 1
    previous_family = 'north_row2' if step == 1 else 'north_row3'
    checks = dict(family=previous_family,row_step=previous_step,status='validated_full_grid_and_collider_samples',
                  scene=SCENE,scene_sha256=sha256(ROOT/SCENE),manifest_sha256=sha256(predecessor),
                  terrains=c['retained_count'],retained=c['retained_count']-256,
                  edges=c['baseline_edges'],outer_edges=320 if step == 1 else 352,collider_samples=c['retained_count']*25)
    if m.get('row_step') != previous_step or any(proof.get(k) != v for k,v in checks.items()):
        raise ValueError('Missing or stale immediately preceding full native proof')
    records = m['retained_tiles']+m['new_tiles']
    validate_geographic_records(step-1, records)
    owners = sorted({t['data_path'].split('/TerrainData/')[0] for t in records})
    for t in records:
        if sha256(ROOT/t['data_path']) != t['data_sha256'] or sha256(ROOT/(t['data_path']+'.meta')) != t['meta_sha256']:
            raise ValueError('Native accepted terrain changed')
        read_native_reference(ROOT,t['native_reference'],owners+[m['shared_snapshot_owner']])
    output = c['data']/'Baseline01'
    if step == 1:
        from north_row2_contract import contract as previous_contract
        prior_contract = previous_contract(6, m['execution_date'])
    else:
        prior_contract = contract(step-1,m['execution_date'])
    model_output = prior_contract['data']/'AcceptedModel01'
    if output.exists() or model_output.exists():
        raise ValueError('Fresh baseline and accepted-model owners required')
    inventory_paths = set()
    for owner in owners+[m['shared_snapshot_owner']]:
        inventory_paths.update(p.relative_to(ROOT).as_posix() for p in (ROOT/owner).rglob('*') if p.is_file())
    inventory_paths.update(['Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/ProjectVersion.txt',
                            'ProjectSettings/EditorBuildSettings.asset',SCENE,SCENE+'.meta'])
    inventory = {p:sha256(ROOT/p) for p in sorted(inventory_paths)}
    for f in m['protected_files']:
        if inventory.get(f['path']) != f['sha256']:
            raise ValueError('Accepted protected source changed: '+f['path'])
    output.mkdir(parents=True)
    inventory_path = output/'protected-files.json'
    inventory_path.write_text(json.dumps(inventory,indent=2)+'\n',encoding='utf-8')
    shutil.copy2(ROOT/SCENE,output/'production_before.unity')
    baseline = dict(schema_version=2,family='north_row3',status='native_baseline_refs_verified',accepted_steps=step-1,
                    scene=SCENE,scene_sha256=proof['scene_sha256'],scene_guid=m['scene_guid'],
                    predecessor_manifest=predecessor.relative_to(ROOT).as_posix(),predecessor_manifest_sha256=sha256(predecessor),
                    tiles=records,edges=c['baseline_edges'],protected_inventory_path=inventory_path.relative_to(ROOT).as_posix(),
                    protected_inventory_sha256=sha256(inventory_path),full_native_proof=proof_path.relative_to(ROOT).as_posix(),
                    full_native_proof_sha256=sha256(proof_path))
    (output/'baseline.json').write_text(json.dumps(baseline,indent=2)+'\n',encoding='utf-8')
    prepared_path = prior_contract['data']/'Prepared01/manifest.json'
    prepared = json.loads(prepared_path.read_text(encoding='utf-8'))
    if sha256(prepared_path) != m['prepared_manifest_sha256']:
        raise ValueError('Accepted preparation changed')
    source = prepared['sections'][0]
    color = ROOT/source['color_path']
    if sha256(color) != source['color_sha256']:
        raise ValueError('Accepted image changed')
    pixels = np.asarray(Image.open(color).convert('RGB'))
    arrays = {}; tiles = []
    for t in m['new_tiles']:
        row,col = int(t['local_id'][1:3]),int(t['local_id'][5:7])
        runtime = np.asarray(Image.open(predecessor.parent/t['color_file']).convert('RGB'))
        if not np.array_equal(runtime,pixels[row*128:(row+1)*128,col*128:(col+1)*128]):
            raise ValueError('Accepted imagery differs from runtime crop')
        h = read_native_reference(ROOT,t['native_reference'],owners)
        arrays[t['local_id']] = h[::-1]*np.float32(1800)-np.float32(100)
        tiles.append(dict(t,id=t['section']+'/'+t['local_id'],row=row,col=col))
    model_output.mkdir(parents=True)
    mesh = model_output/(m['section']+'_native_mesh_grids.npz')
    np.savez_compressed(mesh,**arrays)
    section = dict(id=m['section'],bounds_m=source['bounds_m'],mesh_grids_path=mesh.relative_to(ROOT).as_posix(),
                   mesh_grids_sha256=sha256(mesh),color_path=source['color_path'],color_sha256=source['color_sha256'],tiles=tiles)
    model = dict(schema_version=1,family=previous_family,status='accepted_new_native_model_verified',
                 accepted_step=previous_step,scene_sha256=proof['scene_sha256'],native_proof_sha256=sha256(proof_path),sections=[section])
    (model_output/'manifest.json').write_text(json.dumps(model,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(dict(next_step=step,retained=len(records),protected_files=len(inventory),
                         baseline=str(output/'baseline.json'),accepted_model=str(model_output/'manifest.json')),indent=2))

if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--step',type=int,required=True)
    parser.add_argument('--date',default='2026-10-10')
    parser.add_argument('--predecessor',type=Path,required=True)
    args=parser.parse_args()
    capture(args.step,args.date,args.predecessor.resolve())
