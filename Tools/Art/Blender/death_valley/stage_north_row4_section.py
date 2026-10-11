"""Stage exactly a sealed acceptance; never infer unrelated dirty ownership."""
import argparse,json,subprocess
from pathlib import Path
from north_row4_contract import ROOT,contract,sha256

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--step',type=int,required=True);p.add_argument('--date',required=True)
    a=p.parse_args();c=contract(a.step,a.date)
    if subprocess.check_output(['git','branch','--show-current'],cwd=ROOT,text=True).strip()!='main':raise ValueError('Authorized main checkout required')
    if subprocess.check_output(['git','diff','--cached','--name-only'],cwd=ROOT,text=True).strip():raise ValueError('Preserve existing index ownership')
    ev=ROOT/'Docs/Evidence/DeathValley'/c['batch'];receipt=json.loads((ev/'closeout.json').read_text(encoding='utf-8'))
    if receipt['status']!='native_integration_coverage_metadata_and_map_closeout_passed' or receipt['step']!=a.step:raise ValueError('Complete section closeout required')
    if sha256(ROOT/'Assets/_Project/Scenes/Production/GreaterWasteland.unity')!=receipt['production_scene_sha256']:raise ValueError('Accepted saved scene changed')
    paths=['Assets/_Project/Scenes/Production/GreaterWasteland.unity','Docs/DeathValley/NORTH_ROW4_EXECUTION_PLAN_2026-10-11.md','Docs/LOCAL_WORKSPACE.md','Docs/DeathValley/coverage_catalog.json','Docs/DeathValley/coverage_atlas.html','Docs/DeathValley/coverage_badwater.svg','Docs/DeathValley/unity_tiles.csv',c['asset']+'.meta']
    candidate='Assets/_Project/Scenes/Reference/'+c['batch']+'Candidate.unity';paths+=[candidate,candidate+'.meta']
    paths += [p.relative_to(ROOT).as_posix() for p in (ROOT/c['asset']).rglob('*') if p.is_file()]
    paths += [p.relative_to(ROOT).as_posix() for p in ev.iterdir() if p.suffix in ('.json','.xml','.png')]
    if len(paths)!=len(set(paths)) or not all((ROOT/p).is_file() for p in paths):raise ValueError('Acceptance path manifest incomplete or duplicated')
    if any(p.startswith(('SourceData/','SourceArt/','Logs/','Library/')) or p.endswith('.blend') for p in paths):raise ValueError('Local-only source path entered acceptance batch')
    manifest=ROOT/f'Logs/NorthRow4Readiness20261011/stage-step{a.step}-paths.txt'
    manifest.write_bytes(b'\0'.join(p.encode('utf-8') for p in sorted(paths))+b'\0')
    subprocess.run(['git','add','--pathspec-from-file='+str(manifest),'--pathspec-file-nul'],cwd=ROOT,check=True)
    staged=subprocess.check_output(['git','diff','--cached','--name-only','-z'],cwd=ROOT).decode().split('\0')[:-1]
    # Unchanged routing paths are allowed in the manifest but do not produce staged diffs.
    if set(staged)-set(paths) or any(p.startswith('SourceData/') for p in staged):raise ValueError('Unexpected staged ownership')
    print('Staged',len(staged),'acceptance paths from exact',len(paths),'path manifest.')
