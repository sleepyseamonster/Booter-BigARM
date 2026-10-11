"""Transfer only an independently validated native section under fresh root exclusion."""
import argparse,contextlib,json,re,shutil,xml.etree.ElementTree as ET
from pathlib import Path
from north_row4_contract import ROOT,contract,sha256
from north_row4_transfer_guard import command,live_guard,offline_lock,root_processes,saved_guard
from preserve_expansion_scene import split_documents

def transfer(step,date,isolation,test_proof_version='01',candidate_proof_version='01',playable_proof_version='01'):
    c=contract(step,date);isolation=isolation.resolve()
    if isolation==ROOT or (isolation/'.git').exists():raise ValueError('Physical isolation required')
    bm=saved_guard(step,date);scene=bm['scene']
    report_path=isolation/'Logs'/f'step{step}-production-readback.json'
    report=json.loads(report_path.read_text(encoding='utf-8'))
    expected={'status':'validated_full_grid_and_collider_samples','family':'north_row4','row_step':step,'terrains':c['total'],'retained':c['retained_count'],'edges':c['edges'],'outer_edges':384,'collider_samples':c['collider_samples'],'sectors':(30+step)*16}
    if any(report.get(k)!=v for k,v in expected.items()):raise ValueError('Independent native production report differs')
    if sha256(isolation/scene)!=report['scene_sha256'] or sha256(isolation/c['asset']/'Source/manifest.json')!=report['manifest_sha256']:
        raise ValueError('Isolated saved bytes changed after native validation')
    if sha256(isolation/(scene+'.meta'))!=sha256(ROOT/(scene+'.meta')):raise ValueError('Production scene GUID changed')
    proof=isolation/c['asset']/'Source/unity_production_validation.json'
    if json.loads(proof.read_text(encoding='utf-8'))!=report or not Path(str(proof)+'.meta').is_file():raise ValueError('Native-imported production proof and metadata required')
    test_suffix='' if test_proof_version=='01' else test_proof_version
    candidate_suffix='' if candidate_proof_version=='01' else candidate_proof_version
    xml=isolation/'Logs'/f'step{step}-tests{test_suffix}.xml';tests=ET.parse(xml).getroot()
    if tests.attrib['result']!='Passed' or int(tests.attrib['failed']) or int(tests.attrib['passed'])<34:raise ValueError('Focused row/map test proof failed')
    candidate_report_path=isolation/'Logs'/f'step{step}-candidate-readback{candidate_suffix}.json'
    candidate_report=json.loads(candidate_report_path.read_text(encoding='utf-8'))
    if any(candidate_report.get(k)!=v for k,v in expected.items()) or candidate_report['manifest_sha256']!=report['manifest_sha256']:
        raise ValueError('Selected independent candidate proof differs from production contract')
    if candidate_report['scene']!=c['asset'].replace('Art/Terrain/','Scenes/Reference/')+'Candidate.unity':
        raise ValueError('Candidate proof scene owner differs')
    if sha256(isolation/candidate_report['scene'])!=candidate_report['scene_sha256']:
        raise ValueError('Candidate saved bytes changed after selected readback')
    playable_suffix='' if playable_proof_version=='01' else playable_proof_version
    playable=isolation/'Logs'/f'step{step}-playable{playable_suffix}.log'
    if f'Playable Badwater scene validated: {c["total"]} terrains, Booter, Legger, input, camera, UI, twilight and dust.' not in playable.read_text(encoding='utf-8'):
        raise ValueError('Canonical saved-production setup proof missing')
    _,old=split_documents((ROOT/scene).read_bytes().decode('utf-8'));_,new=split_documents((isolation/scene).read_bytes().decode('utf-8'))
    if set(old)-set(new) or [k for k in old if old[k]!=new[k]]!=['1259644692'] or len(set(new)-set(old))!=1314:
        raise ValueError('Scene preservation differs from the one-section append contract')
    ev=ROOT/'Docs/Evidence/DeathValley'/c['batch']
    if ev.exists():raise ValueError('Fresh section acceptance evidence required')
    offline=not root_processes()
    exclusion=offline_lock() if offline else contextlib.nullcontext(live_guard(step,date))
    with exclusion as guard:
        saved_guard(step,date)
        fingerprint=None if offline else guard['fingerprint']
        # Copy exact files; tolerate only identical partial immutable transfer output.
        source=isolation/c['asset'];destination=ROOT/c['asset']
        for src in [source]+list(source.rglob('*')):
            dst=ROOT/src.relative_to(isolation)
            if src.is_dir():dst.mkdir(parents=True,exist_ok=True)
            elif dst.exists():
                if sha256(src)!=sha256(dst):raise ValueError('Existing native owner bytes differ; preserve and investigate')
            else:shutil.copy2(src,dst)
        candidate='Assets/_Project/Scenes/Reference/'+c['batch']+'Candidate.unity'
        for relative in [c['asset']+'.meta',candidate,candidate+'.meta']:
            src=isolation/relative;dst=ROOT/relative
            if dst.exists():
                if sha256(src)!=sha256(dst):raise ValueError('Native metadata/candidate collision')
            else:shutil.copy2(src,dst)
        saved_guard(step,date)
        if offline:
            if root_processes():raise ValueError('Root Editor appeared; scene transfer stopped before write')
        else:live_guard(step,date)
        shutil.copy2(isolation/scene,ROOT/scene)
        if sha256(ROOT/scene)!=report['scene_sha256']:raise ValueError('Transferred saved scene bytes differ')
        if offline:
            if root_processes():raise ValueError('Root Editor appeared under exclusive transfer lock; inspect before proceeding')
            preservation={'mode':'offline','exclusive_lock_held_through_transfer':True,'no_root_editor_before_or_after':True,'recovery_files_untouched':True}
        else:
            after=command('eval_file',str(ROOT/'Tools/Art/Blender/death_valley/editor_queries/current_scene_fingerprint.cs'),'60000')['result']
            if after!=fingerprint:raise ValueError('Loaded scene fingerprint changed during transfer')
            preservation={'mode':'live','before':fingerprint,'after':after,'identical':True}
    ev.mkdir(parents=True)
    shutil.copy2(isolation/'Logs'/f'step{step}-candidate-readback{candidate_suffix}.json',ev/f'step{step}-candidate-readback.json')
    shutil.copy2(isolation/'Logs'/f'step{step}-production-readback.json',ev/f'step{step}-production-readback.json')
    shutil.copy2(xml,ev/f'step{step}-tests.xml')
    (ev/'root_transfer_guard.json').write_bytes((json.dumps(guard,indent=2)+'\n').encode('utf-8'))
    (ev/'unrelated_editor_state_preservation.json').write_bytes((json.dumps(preservation,indent=2)+'\n').encode('utf-8'))
    receipt={'schema_version':1,'family':'north_row4','step':step,'status':'verified_native_saved_scene_transfer','retained_terrains':c['retained_count'],'new_terrains':256,'total_terrains':c['total'],'changed_original_documents':['1259644692'],'unchanged_original_documents':len(old)-1,'new_scene_documents':1314,'new_sectors':16,'predecessor_scene_sha256':bm['scene_sha256'],'production_scene_sha256':report['scene_sha256'],'manifest_sha256':report['manifest_sha256'],'scene_guid_preserved':True,'native_proof_metadata_sha256':sha256(Path(str(proof)+'.meta')),'canonical_validator_log_sha256':sha256(playable),'test_proof_version':test_proof_version,'candidate_proof_version':candidate_proof_version,'root_editor_mode':preservation['mode'],'root_import_limit':'Native proof imported in isolation, paired metadata copied byte-exact; no fresh root GUI import claim when offline.'}
    receipt['playable_proof_version']=playable_proof_version
    (ev/'scene_preservation_receipt.json').write_bytes((json.dumps(receipt,indent=2)+'\n').encode('utf-8'))
    print(json.dumps(receipt,indent=2))

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--step',type=int,required=True);p.add_argument('--date',required=True);p.add_argument('--isolation',type=Path,required=True)
    p.add_argument('--test-proof-version',default='01');p.add_argument('--candidate-proof-version',default='01')
    p.add_argument('--playable-proof-version',default='01')
    a=p.parse_args();transfer(a.step,a.date,a.isolation,a.test_proof_version,a.candidate_proof_version,a.playable_proof_version)
