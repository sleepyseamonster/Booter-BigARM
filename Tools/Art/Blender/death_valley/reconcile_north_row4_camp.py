"""Bounded step-five reconciliation of committed Camp 1 rock placement a0b69a25a."""
import argparse,hashlib,json,re,subprocess,xml.etree.ElementTree as ET
from pathlib import Path
from north_row4_contract import ROOT,contract,sha256
from north_row4_transfer_guard import root_processes
from preserve_expansion_scene import split_documents

COMMIT='a0b69a25a'
SCENE='Assets/_Project/Scenes/Production/GreaterWasteland.unity'
ROOTS='9223372036854775807'
INSTANCE='522726688'
PREFAB='Assets/_Project/Prefabs/TopDown3D/Environment/Camp1AuthoredRocks.prefab'
RECEIPT=ROOT/'Docs/Evidence/DeathValley/NORTH_ROW4_STEP05_COMMITTED_SCENE_RECONCILIATION_2026-10-12.json'
C=contract(5,'2026-10-11')
LOCAL=C['data']/'Reconciliation01'

def blob(path):
    return subprocess.check_output(['git','show',COMMIT+':'+path],cwd=ROOT)

def merge(captured,committed,terrain):
    prefix,old=split_documents(captured);_,overlay=split_documents(committed);_,new=split_documents(terrain)
    if set(old)-set(overlay) or set(overlay)-set(old)!={INSTANCE} or [k for k in old if old[k]!=overlay[k]]!=[ROOTS]:
        raise ValueError('Only the exact committed prefab/root addition is supported')
    if not overlay[INSTANCE].startswith('--- !u!1001 ') or 'guid: d918f2633c616554182d78c632d13098' not in overlay[INSTANCE]:
        raise ValueError('Unexpected committed prefab identity')
    if overlay[ROOTS]!=old[ROOTS]+'  - {fileID: '+INSTANCE+'}\n':
        raise ValueError('Expected exact root reference append')
    if set(old)-set(new) or set(new)&{INSTANCE} or [k for k in old if old[k]!=new[k]]!=['1259644692'] or len(set(new)-set(old))!=1314:
        raise ValueError('Original isolated one-section preservation must already have passed')
    new[ROOTS]=overlay[ROOTS];new[INSTANCE]=overlay[INSTANCE]
    return prefix+''.join(new.values())

def prepare(iso):
    if LOCAL.exists():raise ValueError('Fresh reconciliation owner required')
    subprocess.run(['git','merge-base','--is-ancestor',COMMIT,'HEAD'],cwd=ROOT,check=True)
    subprocess.run(['git','diff','--quiet','HEAD','--',SCENE],cwd=ROOT,check=True)
    committed=blob(SCENE)
    if (ROOT/SCENE).read_bytes()!=committed:raise ValueError('Root must match the exact committed camp scene')
    captured=(C['data']/'Baseline01/production_before.unity').read_bytes()
    terrain=(iso/SCENE).read_bytes()
    merged=merge(captured.decode(),committed.decode(),terrain.decode()).encode()
    LOCAL.mkdir();(LOCAL/'committed_predecessor.unity').write_bytes(committed)
    (LOCAL/'terrain_before_reconciliation.unity').write_bytes(terrain)
    (LOCAL/'merged-production.unity').write_bytes(merged)
    metas=subprocess.check_output(['git','ls-files','-z','Assets'],cwd=ROOT).decode().split('\0')
    index={}
    for path in metas:
        if not path.endswith('.meta') or path.startswith('Assets/_Project/Art/Terrain/'):continue
        match=re.search(r'(?m)^guid: ([0-9a-f]{32})$',(ROOT/path).read_text(encoding='utf-8'))
        if match:index[match[1]]=path[:-5]
    queue=[PREFAB,'Assets/_Project/Scripts/Editor/TopDown3D/Camp1AuthoredRockAuthoring.cs','Assets/_Project/Tests/Editor/Camp1AuthoredRockTests.cs']
    done=set();files=[];packages=[]
    while queue:
        path=queue.pop()
        if path in done:continue
        done.add(path)
        for rel in [path,path+'.meta']:
            data=blob(rel)
            if (ROOT/rel).read_bytes()!=data:raise ValueError('Committed dependency changed: '+rel)
            dest=LOCAL/'Dependencies'/rel;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(data)
            files.append(dict(path=rel,sha256=hashlib.sha256(data).hexdigest()))
            if rel.endswith(('.prefab','.asset','.mat','.meta','.terrainlayer')):
                for guid in set(re.findall(rb'guid: ([0-9a-f]{32})',data)):
                    guid=guid.decode()
                    if guid in index:queue.append(index[guid])
                    elif guid.startswith('0000000000000000'):continue
                    else:
                        matches=subprocess.check_output(['rg','-l','--fixed-strings','guid: '+guid,'-g','*.meta','Library/PackageCache'],cwd=ROOT,text=True).splitlines()
                        if len(matches)!=1:raise ValueError('Unresolved dependency GUID: '+guid)
                        pkg=Path(matches[0]);asset=Path(str(pkg)[:-5])
                        for part in [pkg,asset]:
                            if sha256(ROOT/part)!=sha256(iso/part):raise ValueError('Package dependency differs: '+str(part))
                        packages.append(dict(guid=guid,path=pkg.as_posix(),sha256=sha256(ROOT/pkg)))
    report=json.loads((iso/'Logs/step5-production-readback.json').read_text(encoding='utf-8'))
    if sha256(iso/SCENE)!=report['scene_sha256']:raise ValueError('Validated draft changed')
    receipt=dict(schema_version=1,status='committed_camp_overlay_prepared_pending_fresh_validation',step=5,
        source_commit=subprocess.check_output(['git','rev-parse',COMMIT],cwd=ROOT,text=True).strip(),
        captured_predecessor_scene_sha256=hashlib.sha256(captured).hexdigest(),
        committed_transfer_predecessor_scene_sha256=hashlib.sha256(committed).hexdigest(),
        original_isolated_scene_sha256=hashlib.sha256(terrain).hexdigest(),production_scene_sha256=hashlib.sha256(merged).hexdigest(),
        manifest_sha256=report['manifest_sha256'],changed_committed_original_documents=[ROOTS],added_committed_documents=[INSTANCE],
        dependencies=files,package_dependencies=packages,source_lineage_unchanged=True)
    (LOCAL/'preparation.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print('Prepared exact committed overlay and',len(files),'committed dependency files',flush=True)

def apply(iso):
    normalized=iso.as_posix().replace("'","''")
    query=f"@(Get-CimInstance Win32_Process | Where-Object {{$_.Name -eq 'Unity.exe' -and $_.CommandLine.Replace([char]92,[char]47) -like '*{normalized}*'}}).Count"
    if int(subprocess.check_output(['powershell.exe','-NoProfile','-Command',query],text=True).strip()):raise ValueError('Isolation must be offline')
    r=json.loads((LOCAL/'preparation.json').read_text(encoding='utf-8'))
    if sha256(iso/SCENE)!=r['original_isolated_scene_sha256'] or sha256(ROOT/SCENE)!=r['committed_transfer_predecessor_scene_sha256']:raise ValueError('Reconciliation checkpoints changed')
    backup=LOCAL/'IsolationBackups'
    if backup.exists():raise ValueError('Fresh isolation backup required')
    backup.mkdir()
    for f in r['dependencies']:
        staged=LOCAL/'Dependencies'/f['path'];target=iso/f['path']
        if sha256(staged)!=f['sha256']:raise ValueError('Staged dependency changed')
        if target.exists():
            b=backup/f['path'];b.parent.mkdir(parents=True,exist_ok=True);b.write_bytes(target.read_bytes())
        target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(staged.read_bytes())
    proof=iso/C['asset']/'Source/unity_production_validation.json'
    for part in [proof,Path(str(proof)+'.meta')]:
        (backup/part.name).write_bytes(part.read_bytes())
    # Retain the draft and its metadata through compilation; refresh only after fresh readback.
    (iso/SCENE).write_bytes((LOCAL/'merged-production.unity').read_bytes())
    assert sha256(iso/SCENE)==r['production_scene_sha256']
    print('Committed overlay applied only in offline isolation; draft proof retained locally for fresh resealing',flush=True)

def seal(iso,production_version='03'):
    if RECEIPT.exists():raise ValueError('Fresh public reconciliation receipt required')
    r=json.loads((LOCAL/'preparation.json').read_text(encoding='utf-8'))
    report_path=iso/f'Logs/step5-production-readback{production_version}.json'
    report=json.loads(report_path.read_text(encoding='utf-8'))
    expected=dict(status='validated_full_grid_and_collider_samples',family='north_row4',row_step=5,terrains=8960,retained=8704,new_terrains=256,edges=17728,outer_edges=384,collider_samples=224000,sectors=560)
    if any(report.get(k)!=v for k,v in expected.items()) or report['scene_sha256']!=r['production_scene_sha256'] or report['manifest_sha256']!=r['manifest_sha256']:
        raise ValueError('Fresh reconciled full native proof required')
    source=iso/C['asset']/'Source/unity_production_validation.json'
    if json.loads(source.read_text(encoding='utf-8'))!=report or sha256(Path(str(source)+'.meta'))!=sha256(LOCAL/'IsolationBackups/unity_production_validation.json.meta'):
        raise ValueError('Fresh imported native proof with preserved metadata required')
    if sha256(iso/SCENE)!=r['production_scene_sha256'] or sha256(ROOT/SCENE)!=r['committed_transfer_predecessor_scene_sha256']:raise ValueError('Saved reconciliation checkpoints changed')
    logs=[]
    for name,marker in [('step5-playable02.log','Playable Badwater scene validated: 8960 terrains, Booter, Legger, input, camera, UI, twilight and dust.')]:
        path=iso/'Logs'/name;text=path.read_text(encoding='utf-8')
        if marker not in text or 'Application will terminate with return code 0' not in text:raise ValueError('Fresh successful canonical/camp log required')
        logs.append(dict(local_log=str(path),sha256=sha256(path),exit_code=0))
    xml=iso/'Logs/step5-testsreconciliation01.xml';tests=ET.parse(xml).getroot()
    if tests.attrib['result']!='Passed' or int(tests.attrib['passed'])!=34 or int(tests.attrib['failed']):raise ValueError('All 34 row/map tests required')
    names=[n.attrib['fullname'] for n in tests.iter('test-case')]
    if len(set(names))!=34 or any(n.attrib['result']!='Passed' for n in tests.iter('test-case')):raise ValueError('Every selected case must pass uniquely')
    for fixture,count in [('BadwaterNorthRow4Tests',18),('DeathValleyMapTests',16)]:
        if sum(name.startswith('BooterBigArm.Tests.'+fixture+'.') for name in names)!=count:raise ValueError('Required fixture coverage changed')
    for f in r['dependencies']:
        if sha256(LOCAL/'Dependencies'/f['path'])!=f['sha256'] or sha256(iso/f['path'])!=f['sha256']:raise ValueError('Frozen committed validation dependency changed')
    public_xml=RECEIPT.with_suffix('.xml');public_xml.write_bytes(xml.read_bytes())
    import_history=[]
    for name,exit_code in [('step5-production-check02.log',1),('step5-importreconciliation01.log',1),('step5-importreconciliation02.log',1),('step5-importreconciliation03.log',1),('step5-importreconciliation04.log',0),('step5-importreconciliation05.log',0)]:
        path=iso/'Logs'/name
        if f'Application will terminate with return code {exit_code}' not in path.read_text(encoding='utf-8'):raise ValueError('Retained import terminal result differs')
        import_history.append(dict(local_log=str(path),sha256=sha256(path),exit_code=exit_code))
    r.update(status='committed_camp_overlay_freshly_validated',fresh_production_readback_sha256=sha256(report_path),fresh_logs=logs,
        production_proof_version=production_version,playable_proof_version='02',test_proof_version='reconciliation01',
        focused_tests=dict(passed=34,failed=0,xml_path=public_xml.relative_to(ROOT).as_posix(),sha256=sha256(public_xml)),
        preserved_native_proof_metadata_sha256=sha256(Path(str(source)+'.meta')),dependency_import_history=import_history,
        proof_metadata_recovery='The unaccepted draft JSON was initially absent during failed compilation. Its original JSON and exact metadata bytes were restored from the retained backup before successful import; the corrected workflow retains both through compilation and refreshes JSON only after fresh native readback.')
    RECEIPT.write_text(json.dumps(r,indent=2)+'\n',encoding='utf-8')
    r['validation_scope']='Terrain integration and exact committed scene preservation in the declared committed validation context. Concurrent non-terrain source changes are preserved in root and excluded from this proof. Per user instruction, no additional camp gameplay or placement audit.'
    print('Exact committed overlay preserved with fresh native, canonical and 34-case terrain/map proof',flush=True)

def extend(iso,paths):
    normalized=iso.as_posix().replace("'","''")
    query=f"@(Get-CimInstance Win32_Process | Where-Object {{$_.Name -eq 'Unity.exe' -and $_.CommandLine.Replace([char]92,[char]47) -like '*{normalized}*'}}).Count"
    if int(subprocess.check_output(['powershell.exe','-NoProfile','-Command',query],text=True).strip()):raise ValueError('Isolation must be offline')
    preparation=LOCAL/'preparation.json';r=json.loads(preparation.read_text(encoding='utf-8'))
    existing={f['path'] for f in r['dependencies']}
    for path in paths:
        if not path.startswith('Assets/_Project/') or path.startswith('Assets/_Project/Art/Terrain/') or not path.endswith(('.cs','.prefab','.mat','.asset')):raise ValueError('Explicit committed non-terrain dependency required')
        for rel in [path,path+'.meta']:
            if rel in existing:raise ValueError('Dependency already captured; do not overwrite its proof')
            data=blob(rel)
            if (ROOT/rel).read_bytes()!=data:raise ValueError('Committed dependency changed: '+rel)
            staged=LOCAL/'Dependencies'/rel;staged.parent.mkdir(parents=True,exist_ok=True);staged.write_bytes(data)
            target=iso/rel;backup=LOCAL/'IsolationBackups'/rel
            if target.exists():
                if backup.exists():raise ValueError('Preserve prior dependency backup')
                backup.parent.mkdir(parents=True,exist_ok=True);backup.write_bytes(target.read_bytes())
            target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
            r['dependencies'].append(dict(path=rel,sha256=hashlib.sha256(data).hexdigest(),reason='explicit_committed_CSharp_or_literal_asset_dependency'))
            existing.add(rel)
    preparation.write_text(json.dumps(r,indent=2)+'\n',encoding='utf-8')
    print('Extended exact committed dependency proof by',len(paths)*2,'files',flush=True)

def refresh_proof(iso,restore=False,production_version='03'):
    normalized=iso.as_posix().replace("'","''")
    query=f"@(Get-CimInstance Win32_Process | Where-Object {{$_.Name -eq 'Unity.exe' -and $_.CommandLine.Replace([char]92,[char]47) -like '*{normalized}*'}}).Count"
    if int(subprocess.check_output(['powershell.exe','-NoProfile','-Command',query],text=True).strip()):raise ValueError('Isolation must be offline')
    proof=iso/C['asset']/'Source/unity_production_validation.json';meta=Path(str(proof)+'.meta')
    old=LOCAL/'IsolationBackups/unity_production_validation.json';old_meta=Path(str(old)+'.meta')
    if meta.exists() and sha256(meta)!=sha256(old_meta):raise ValueError('Preserve unexpected proof metadata')
    if proof.exists() and sha256(proof)!=sha256(old):raise ValueError('Preserve unexpected draft proof')
    if restore:
        proof.write_bytes(old.read_bytes());meta.write_bytes(old_meta.read_bytes())
        print('Original unaccepted draft and exact imported metadata restored from retained backup',flush=True)
        return
    r=json.loads((LOCAL/'preparation.json').read_text(encoding='utf-8'))
    report=iso/f'Logs/step5-production-readback{production_version}.json';data=json.loads(report.read_text(encoding='utf-8'))
    if data['scene_sha256']!=sha256(iso/SCENE) or data['scene_sha256']!=r['production_scene_sha256'] or data['manifest_sha256']!=r['manifest_sha256'] or data['status']!='validated_full_grid_and_collider_samples':raise ValueError('Fresh reconciled readback required')
    proof.write_bytes(report.read_bytes());meta.write_bytes(old_meta.read_bytes())
    print('Fresh validated draft proof replaces backed-up original; exact metadata retained for import',flush=True)

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--stage',choices=['prepare','apply','seal','extend','restore-draft-proof','refresh-proof'],required=True);p.add_argument('--isolation',type=Path,required=True);p.add_argument('--dependency',action='append',default=[]);p.add_argument('--production-proof-version',default='03')
    a=p.parse_args();iso=a.isolation.resolve()
    if iso==ROOT or (iso/'.git').exists():raise ValueError('Physical isolation required')
    if a.stage=='extend':extend(iso,a.dependency)
    elif a.stage in ('restore-draft-proof','refresh-proof'):refresh_proof(iso,a.stage=='restore-draft-proof',a.production_proof_version)
    elif a.stage=='seal':seal(iso,a.production_proof_version)
    else:{'prepare':prepare,'apply':apply}[a.stage](iso)
