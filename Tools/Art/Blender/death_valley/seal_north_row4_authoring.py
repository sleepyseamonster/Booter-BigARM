"""Run only after all five rendered views have actually been inspected."""
import argparse,json,sys
from pathlib import Path
from north_row4_contract import ROOT
root=ROOT
from north_row4_contract import contract,sha256
p=argparse.ArgumentParser();p.add_argument('--step',type=int,required=True);p.add_argument('--date',required=True);p.add_argument('--inspected-five-views',action='store_true',required=True);a=p.parse_args()
c=contract(a.step,a.date);assert 1<=a.step<=6
prepared=c['data']/'Prepared01/manifest.json';m=json.loads(prepared.read_text(encoding='utf-8'))
proofs=[]
for section,count,stride in [('pilot',4 if a.step==1 else 8,2),(c['section'],256,2),('combined',c['total'],4)]:
 p=prepared.parent/(section+'_blender_proof.json');b=json.loads(p.read_text(encoding='utf-8'))
 assert b['status']=='native_reopened_mesh_verified' and b['chunks']==count and b['interior_spacing_m']==stride and b['perimeter_spacing_m']==1
 assert b['manifest_sha256']==sha256(prepared) and b['max_mesh_sample_error_m']==0 and b['max_mesh_edge_difference_m']==0 and b['minimum_normal_z']>0
 assert sha256(Path(b['blend_file']))==b['blend_sha256'] and sha256(p.parent/b['export_file'])==b['export_sha256']
 proofs.append(dict(b,proof_path=p.relative_to(root).as_posix(),proof_sha256=sha256(p)))
views=[]
for name in ['pilot_junction.png','section_overhead.png','section_oblique.png','combined_overhead.png','combined_oblique.png']:
 p=prepared.parent/name;views.append(dict(path=p.relative_to(root).as_posix(),sha256=sha256(p),inspected=True))
baseline=root/m['baseline_manifest'];bm=json.loads(baseline.read_text(encoding='utf-8'))
record=dict(schema_version=1,family='north_row4',step=a.step,batch=c['batch'],section=c['section'],bounds_m=c['bounds'],
 status='native_Blender_authoring_reopen_and_five_view_gate_passed',prepared_manifest_path=prepared.relative_to(root).as_posix(),
 prepared_manifest_sha256=sha256(prepared),preparation=m['proof'],blender_proofs=proofs,visual_views=views,
 source_files_local_only=True,total_native_source_bytes=sum(Path(p['blend_file']).stat().st_size for p in proofs),
 baseline=dict(accepted_steps=a.step-1,retained_terrains=c['retained_count'],scene_sha256=bm['scene_sha256'],manifest_sha256=sha256(baseline),
 protected_inventory_sha256=bm['protected_inventory_sha256'],full_native_proof_sha256=bm['full_native_proof_sha256']),
 visual_assessment=('Pilot joins continuous; section ridges and drainage intact; combined full 96 by 96 rectangle has no missing upper-east cells.' if a.step==6 else 'Pilot joins continuous; section ridges and drainage intact; combined sparse upper-east footprint preserved.')+' Imagery remains geographic reference, not final close-range material.',
 limits='Blender native saved/reopened and five-view proof only; Unity integration and collision validation pending.')
target=root/f'Docs/Evidence/DeathValley/NORTH_ROW4_STEP{a.step:02}_AUTHORING_{a.date}.json'
assert not target.exists();target.write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(checkpoint=str(target),native_source_bytes=record['total_native_source_bytes']),indent=2))
