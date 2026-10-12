"""Full-row completion audit; requires all six accepted closeouts and doctor proof."""
import json,re,subprocess,sys,xml.etree.ElementTree as ET
from pathlib import Path
import argparse
parser=argparse.ArgumentParser(description=__doc__);parser.add_argument("--doctor",type=Path,required=True);parser.add_argument("--output",type=Path,required=True);parser.add_argument("--canonical-log-recovery",type=Path);args=parser.parse_args()
if args.output.exists():raise ValueError("Fresh completion audit output required")
root=Path(__file__).resolve().parents[4];sys.path.insert(0,str(root/'Tools/Art/Blender/death_valley'));sys.path.insert(0,str(root/'Docs/DeathValley'))
from north_row4_contract import contract,sha256,occupied_cells
from preserve_expansion_scene import split_documents
from blender_retirement import load_retired_blender_records
from terrain_tools import locate
scene=root/'Assets/_Project/Scenes/Production/GreaterWasteland.unity'
catalog_path=root/'Docs/DeathValley/coverage_catalog.json';cat=json.loads(catalog_path.read_text(encoding='utf-8'))
if len(cat['unity_tiles'])!=9216:raise ValueError('All six sections and 9216 accepted terrains are required before completion audit')
assert cat['unity_scene']['sha256']==sha256(scene)
assert cat['playable_bounds_m']==[499920,4006200,524496,4030776]
cells={tuple(t['bounds_m'][:2]) for t in cat['unity_tiles']}
assert cells==occupied_cells(6) and len({t['geographic_key'] for t in cat['unity_tiles']})==9216
assert cat['source_audit']['shared_edges_checked']==18240 and cat['source_audit']['hash_mismatches']==0
sections=[];sources=0;source_bytes=0;previous='19c3633ffd884f11b72cd9f6daae13526315ad252f93da49b5191d99ab87c566'
from north_row4_coverage import newest
commits=[]
recovery = json.loads(args.canonical_log_recovery.read_text(encoding='utf-8')) if args.canonical_log_recovery else None
if recovery:
 assert recovery['schema_version']==1 and recovery['status']=='exact_accepted_snapshots_canonically_reverified'
 assert recovery['root_scene_unchanged'] and recovery['original_public_closeouts_unchanged']
 assert len(recovery['sections'])==3 and {r['step'] for r in recovery['sections']}=={1,2,3}
recovered_logs=[]
for step in range(1,7):
 native,m=newest(root,7680+256*step);date=m['execution_date'];c=contract(step,date)
 ev=root/'Docs/Evidence/DeathValley'/c['batch'];close=json.loads((ev/'closeout.json').read_text(encoding='utf-8'))
 author=root/f'Docs/Evidence/DeathValley/NORTH_ROW4_STEP{step:02}_AUTHORING_{date}.json';a=json.loads(author.read_text(encoding='utf-8'))
 native=root/c['asset']/'Source/manifest.json';m=json.loads(native.read_text(encoding='utf-8'))
 assert close['status']=='native_integration_coverage_metadata_and_map_closeout_passed' and close['step']==step
 assert close['height_files']==c['total'] and close['color_files']==c['total'] and close['exact_joins']==c['edges']
 assert close['manifest_sha256']==sha256(native) and m['predecessor_scene_sha256']==previous and m['authoring_checkpoint_sha256']==sha256(author)
 assert m['row_step']==step and m['section']==c['section'] and len(m['new_tiles'])==256 and m['tile_count']==c['total']
 assert {tuple(t['bounds_m'][:2]) for t in m['new_tiles']}=={(e,n) for e in range(c['bounds'][0],c['bounds'][2],256) for n in range(c['bounds'][1],c['bounds'][3],256)}
 bpath=c['data']/'Baseline01/baseline.json';bm=json.loads(bpath.read_text(encoding='utf-8'))
 assert bm['accepted_steps']==step-1 and bm['scene_sha256']==previous and len(bm['tiles'])==c['retained_count']
 assert sha256(root/bm['protected_inventory_path'])==bm['protected_inventory_sha256']
 assert sha256(root/bm['full_native_proof'])==bm['full_native_proof_sha256']
 assert sha256(c['data']/'Prepared01/manifest.json')==a['prepared_manifest_sha256']
 prepared=json.loads((c['data']/'Prepared01/manifest.json').read_text(encoding='utf-8'));calibration=prepared['color_calibration']
 assert sha256(root/calibration['reference_path'])==calibration['reference_sha256']
 assert sha256((root/calibration['reference_path']).with_suffix('.json'))==calibration['metadata_sha256']
 assert a['family']=='north_row4' and a['step']==step and a['status']=='native_Blender_authoring_reopen_and_five_view_gate_passed'
 assert a['bounds_m']==c['bounds'] and a['preparation']['max_retained_adjustment_m']<=a['preparation']['quantization_budget_m'] and a['preparation']['retained_rgb_edge_error']==0
 assert len(a['visual_views'])==5
 for view in a['visual_views']:
  assert view['inspected'] and sha256(root/view['path'])==view['sha256']
 for proof in a['blender_proofs']:
  expected={'pilot':(4 if step==1 else 8,2),c['section']:(256,2),'combined':(c['total'],4)}
  count,stride=expected[proof['section']]
  assert proof['status']=='native_reopened_mesh_verified' and proof['chunks']==count and proof['interior_spacing_m']==stride and proof['perimeter_spacing_m']==1
  blend=Path(proof['blend_file']);assert blend.is_relative_to(c['art']) and sha256(blend)==proof['blend_sha256']
  assert proof['max_mesh_sample_error_m']==0 and proof['max_mesh_edge_difference_m']==0 and proof['minimum_normal_z']>0
  assert sha256(root/proof['proof_path'])==proof['proof_sha256'] and sha256((root/proof['proof_path']).parent/proof['export_file'])==proof['export_sha256']
  sources+=1;source_bytes+=blend.stat().st_size
 assert (c['art']/'README.md').is_file()
 assert a['source_files_local_only'] and len(a['blender_proofs'])==3
 for proof in a['blender_proofs']:
  subprocess.run(['git','check-ignore','--quiet',str(Path(proof['blend_file']).relative_to(root))],cwd=root,check=True)
 r=json.loads((ev/'scene_preservation_receipt.json').read_text(encoding='utf-8'));assert r['predecessor_scene_sha256']==previous and r['production_scene_sha256']==close['production_scene_sha256']
 assert r['changed_original_documents']==['1259644692'] and r['new_scene_documents']==1314 and r['scene_guid_preserved']
 report=json.loads((ev/f'step{step}-production-readback.json').read_text(encoding='utf-8'))
 assert report['scene_sha256']==close['production_scene_sha256'] and report['manifest_sha256']==close['manifest_sha256']
 assert report['terrains']==c['total'] and report['edges']==c['edges'] and report['collider_samples']==c['collider_samples'] and report['outer_edges']==384
 meta=json.loads((ev/'metadata_audit.json').read_text(encoding='utf-8'));assert meta['status']=='owned_metadata_and_global_GUID_uniqueness_verified'
 canonical=close['canonical_playable_validator'];assert canonical['exit_code']==0
 original_log=Path(canonical['local_log'])
 if original_log.exists():
  assert sha256(original_log)==canonical['sha256']
 else:
  assert recovery is not None,'Missing original canonical log requires explicit fresh recovery evidence'
  matches=[r for r in recovery['sections'] if r['step']==step];assert len(matches)==1
  recovered=matches[0]
  assert recovered['closeout_sha256']==sha256(ev/'closeout.json') and recovered['exit_code']==0
  assert recovered['original_missing_log']==canonical['local_log'] and recovered['original_log_sha256']==canonical['sha256']
  assert recovered['production_scene_sha256']==close['production_scene_sha256'] and recovered['manifest_sha256']==close['manifest_sha256']
  assert sha256(root/recovered['exact_snapshot_path'])==close['production_scene_sha256']
  fresh_log=Path(recovered['local_log']);assert sha256(fresh_log)==recovered['sha256']
  assert f'Playable Badwater scene validated: {c["total"]} terrains, Booter, Legger, input, camera, UI, twilight and dust.' in fresh_log.read_text(encoding='utf-8')
  recovered_logs.append(step)
 assert close['current_catalog_tests']['failed']==0 and close['current_catalog_tests']['passed']==34
 catalog_xml=[p for p in ev.glob(f'step{step}-current-catalog-tests*.xml') if sha256(p)==close['current_catalog_tests']['xml_sha256']]
 assert len(catalog_xml)==1
 for xml in [catalog_xml[0],ev/f'step{step}-tests.xml']:
  tests=ET.parse(xml).getroot()
  assert tests.attrib['result']=='Passed' and int(tests.attrib['passed'])==34 and int(tests.attrib['failed'])==0
 assert sha256(ev/'metadata_audit.json')==close['metadata_audit_sha256']
 proof=json.loads((native.parent/'unity_production_validation.json').read_text(encoding='utf-8'))
 assert proof==report and report['family']=='north_row4' and report['row_step']==step
 candidate=json.loads((ev/f'step{step}-candidate-readback.json').read_text(encoding='utf-8'))
 assert candidate['status']=='validated_full_grid_and_collider_samples' and candidate['family']=='north_row4' and candidate['row_step']==step
 assert candidate['collider_samples']==c['collider_samples'] and candidate['outer_edges']==384
 assert candidate['terrains']==c['total'] and candidate['edges']==c['edges'] and candidate['manifest_sha256']==sha256(native) and sha256(root/candidate['scene'])==candidate['scene_sha256']
 acceptance=subprocess.check_output(['git','log','--diff-filter=A','--format=%H','--',str((ev/'closeout.json').relative_to(root))],cwd=root,text=True).splitlines()
 assert len(acceptance)==1, 'Exactly one committed acceptance required'
 commit=acceptance[0];commits.append(commit)
 assert subprocess.check_output(['git','show',commit+':'+str((ev/'closeout.json').relative_to(root))],cwd=root)==(ev/'closeout.json').read_bytes()
 print('Accepted section lineage verified',step,flush=True)
 for view in close['developer_map_views']:assert view['inspected'] and sha256(root/view['path'])==view['sha256']
 previous=report['scene_sha256'];sections.append(dict(step=step,batch=c['batch'],bounds_m=c['bounds'],scene_sha256=previous,manifest_sha256=sha256(native),retained=c['retained_count'],total=c['total'],edges=c['edges'],closeout_sha256=sha256(ev/'closeout.json')))
assert sources==18 and previous==sha256(scene)
assert cat['unity_scene']['terrains']==9216 and cat['unity_scene']['colliders']==9216
assert close['coverage_catalog_sha256']==sha256(catalog_path)
from north_row4_native_references import read_native_reference
from north_row4_contract import validate_geographic_records
records=m['retained_tiles']+m['new_tiles'];validate_geographic_records(6,records)
owners=sorted({t['data_path'].split('/TerrainData/')[0] for t in records})+[m['shared_snapshot_owner']]
cells={}
for index,t in enumerate(records,1):
 assert sha256(root/t['data_path'])==t['data_sha256'] and sha256(root/(t['data_path']+'.meta'))==t['meta_sha256']
 cells[tuple(t['bounds_m'][:2])]=read_native_reference(root,t['native_reference'],owners)
 if index%512==0:print('Current native records verified',index,flush=True)
import numpy as np
join_count=0
for (e,n),grid in cells.items():
 for key,edge,other_edge in [((e+256,n),grid[:,-1],(slice(None),0)),((e,n+256),grid[-1],(0,slice(None)))]:
  if key in cells:
   assert np.array_equal(edge,cells[key][other_edge]),'Native exact join differs'
   join_count+=1
assert join_count==18240
before=root/'SourceData/Terrain/DeathValley/NorthRow4E499920N4026680_2026-10-11/Baseline01/production_before.unity'
_,old=split_documents(before.read_text(encoding='utf-8'));_,new=split_documents(scene.read_text(encoding='utf-8'))
changed_original=[k for k in old if old[k]!=new[k]]
assert not set(old)-set(new) and changed_original==['1259644692'] and len(set(new)-set(old))==7884
pattern=r'(?m)^  m_Children:\n(?:  - \{fileID: \d+\}\n)*'
old_children=re.findall(r'fileID: (\d+)',re.search(pattern,old['1259644692'])[0]);new_children=re.findall(r'fileID: (\d+)',re.search(pattern,new['1259644692'])[0])
assert new_children[:len(old_children)]==old_children and len(new_children)==len(old_children)+6
source_audit=root/'Docs/Evidence/DeathValley/NORTH_ROW4_SOURCE_AUDIT_2026-10-11.json';s=json.loads(source_audit.read_text(encoding='utf-8'))
assert s['status']=='reopened_measured_sources_verified' and s['unique_native_samples']==100691969 and s['min_height_m']>=-100 and s['max_height_m']<=1700
assert sha256(root/s['local_manifest_path'])==s['local_manifest_sha256']
survey=root/'SourceData/Terrain/DeathValley/NorthRow4Survey2026-10-11'
assert sha256(survey/'Acquisition01/DEM/manifest.json')==s['dem_manifest_sha256'] and sha256(survey/'ImageryAcquisition02/manifest.json')==s['imagery_manifest_sha256']
for source in s['sources']:
 assert sha256(survey/'Acquisition01/DEM'/source['window_file'])==source['window_sha256'] and sha256(survey/'Acquisition01/DEM'/source['metadata_file'])==source['metadata_sha256']
 assert source['vertical_datum']=='NAVD88 metres' and source['surface']=='bare-earth'
dm=json.loads((survey/'Acquisition01/DEM/manifest.json').read_text(encoding='utf-8'));assert len(dm['sections'])==6
for t in dm['sections']:assert sha256(survey/'Acquisition01/DEM'/t['terrain_file'])==t['terrain_sha256']
im=json.loads((survey/'ImageryAcquisition02/manifest.json').read_text(encoding='utf-8'));assert len(im['sections'])==6
for t in im['sections']:assert t['width']==2048 and t['height']==2048 and sha256(survey/'ImageryAcquisition02'/t['path'])==t['sha256']
assert len(s['sources'])==8 and len(s['ownership_lines'])==10
retired=load_retired_blender_records(root);assert len(retired)==46 and all(not (root/r['path']).exists() for r in retired)
build=(root/'ProjectSettings/EditorBuildSettings.asset').read_text(encoding='utf-8')
assert re.findall(r'- enabled: 1\n    path: (.+)',build)==['Assets/_Project/Scenes/Production/GreaterWasteland.unity']
assert '4de5dd018ee194314a00fd369e2d3eeb' in (root/(str(scene.relative_to(root))+'.meta')).read_text(encoding='utf-8')
baseline_path=root/'SourceData/Terrain/DeathValley/NorthRow4E499920N4026680_2026-10-11/Baseline01/baseline.json'
baseline=json.loads(baseline_path.read_text(encoding='utf-8'))
assert sha256(before)==baseline['scene_sha256'] and len(baseline['tiles'])==7680
inventory_path=root/baseline['protected_inventory_path']
assert sha256(inventory_path)==baseline['protected_inventory_sha256']
inventory=json.loads(inventory_path.read_text(encoding='utf-8'))
pins={}
for index,(path,expected) in enumerate(inventory.items(),1):
 if path==baseline['scene']:continue
 assert sha256(root/path)==expected,'Original protected file changed: '+path
 if path in ['Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/ProjectVersion.txt']:pins[path]=expected
 if index%10000==0:print('Original protected files verified',index,flush=True)
assert len(pins)==3
assert 'm_EditorVersion: 6000.4.0f1' in (root/'ProjectSettings/ProjectVersion.txt').read_text(encoding='utf-8')
doctor_path=args.doctor.resolve();doctor=json.loads(doctor_path.read_text(encoding='utf-8'));assert doctor['status']=='ok' and not doctor['issues']
assert len(commits)==6
for c in commits:subprocess.run(['git','merge-base','--is-ancestor',c,'HEAD'],cwd=root,check=True)
for earlier,later in zip(commits,commits[1:]):subprocess.run(['git','merge-base','--is-ancestor',earlier,later],cwd=root,check=True)
assert not subprocess.check_output(['git','ls-files','SourceData/Terrain/','*.blend*'],cwd=root,text=True).splitlines() # Historical tracked originals may require separate range audit if present.
points=[(500048+4096*i,4026808) for i in range(6)];assert all(len(locate(cat,e,n)['unity_chunks'])==1 for e,n in points)
out=dict(schema_version=1,family='north_row4',status='full_six_section_completion_audit_passed',scene_sha256=sha256(scene),bounds_m=cat['playable_bounds_m'],sections=sections,terrains=9216,exact_joins=18240,collider_samples=230400,sectors=576,retained_original_scene_documents=len(old)-1,original_parent_appended_children=6,new_scene_documents=7884,native_blender_sources_retained=sources,native_blender_bytes=source_bytes,retired_sources_still_absent=46,source_audit_sha256=sha256(source_audit),measured_height_range_m=[s['min_height_m'],s['max_height_m']],doctor_sha256=sha256(doctor_path),doctor_checked_records=doctor['checked_records'],limits='Saved native full-grid/collider, structural, source and visual proof; hands-on traversal and Player performance remain user-owned.')
out['unchanged_package_editor_pins']=pins
out['acceptance_commits']=commits
out['original_protected_files_verified']=len(inventory)-1
out['changed_original_scene_documents']=changed_original
if recovery:
 out['canonical_log_recovery_sha256']=sha256(args.canonical_log_recovery)
 out['missing_original_logs_freshly_reverified_steps']=recovered_logs
import shutil
capacity={drive:shutil.disk_usage(drive).free for drive in ('C:/','D:/')}
assert all(free>3*1024**3 for free in capacity.values()),'Final 3 GiB reserve violated'
out['remaining_free_bytes']=capacity
target=args.output.resolve();assert not target.exists();target.write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8');print(json.dumps(out,indent=2))
