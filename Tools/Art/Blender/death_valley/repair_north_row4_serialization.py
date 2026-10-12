"""Remove the known incidental D-Pad renderer only in offline terrain isolation."""
import argparse,json,re,subprocess
from pathlib import Path
from north_row4_contract import ROOT,contract,sha256
from preserve_expansion_scene import split_documents
from preserve_north_row4_scene import preserve

def repair(before,current):
    _,old=split_documents(before);_,new=split_documents(current)
    added=set(new)-set(old)
    renderer='668086200';owner='668086197'
    if len(added)!=1315 or renderer not in added or owner not in old:
        raise ValueError('Only the documented isolated D-Pad serialization case is supported')
    if not new[renderer].startswith('--- !u!222 ') or 'm_GameObject: {fileID: '+owner+'}' not in new[renderer]:
        raise ValueError('Unexpected renderer identity')
    reference='  - component: {fileID: '+renderer+'}\n'
    if reference in old[owner] or new[owner].count(reference)!=1:
        raise ValueError('Renderer must be an incidental new reference on the existing indicator')
    repaired=current.replace(new[renderer],'').replace(reference,'')
    return preserve(before,repaired)

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--step',type=int,required=True)
    p.add_argument('--date',required=True);p.add_argument('--isolation',type=Path,required=True)
    a=p.parse_args();iso=a.isolation.resolve();c=contract(a.step,a.date)
    if iso==ROOT or (iso/'.git').exists():raise ValueError('Physical isolation required')
    normalized=iso.as_posix().replace("'","''")
    query=f"@(Get-CimInstance Win32_Process | Where-Object {{$_.Name -eq 'Unity.exe' -and $_.CommandLine.Replace([char]92,[char]47) -like '*{normalized}*'}}).Count"
    if int(subprocess.check_output(['powershell.exe','-NoProfile','-Command',query],text=True).strip()):raise ValueError('Isolation must be offline')
    scene=iso/'Assets/_Project/Scenes/Production/GreaterWasteland.unity'
    before=(c['data']/'Baseline01/production_before.unity').read_bytes().decode('utf-8')
    current=scene.read_bytes().decode('utf-8')
    result=repair(before,current)
    backup=c['data']/'NativeLogs01/integrated_before_preservation01.unity'
    receipt=ROOT/'Docs/Evidence/DeathValley'/c['batch']/'incidental_serialization_repair.json'
    if backup.exists() or receipt.exists():raise ValueError('Fresh repair backup and receipt required')
    backup.write_bytes(current.encode('utf-8'));scene.write_bytes(result.encode('utf-8'))
    receipt.parent.mkdir(parents=True,exist_ok=True)
    receipt.write_text(json.dumps(dict(status='known_incidental_renderer_removed_strict_preservation_passed',
        step=a.step,removed_document='668086200',existing_game_object='668086197',
        backup=backup.relative_to(ROOT).as_posix(),backup_sha256=sha256(backup),
        production_scene_sha256=sha256(scene),root_scene_modified=False,preservation_gate_unchanged=True),indent=2)+'\n',encoding='utf-8')
    print('Bounded isolated renderer repair and strict scene preservation passed',flush=True)
