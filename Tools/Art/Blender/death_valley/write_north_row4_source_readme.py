"""Write text-only native source routing after the inspected authoring checkpoint."""
import argparse,json,re,sys
from pathlib import Path
from north_row4_contract import ROOT
root=ROOT
from north_row4_contract import contract,sha256
p=argparse.ArgumentParser();p.add_argument('--step',type=int,required=True);p.add_argument('--date',required=True);a=p.parse_args();c=contract(a.step,a.date)
checkpoint=root/f'Docs/Evidence/DeathValley/NORTH_ROW4_STEP{a.step:02}_AUTHORING_{a.date}.json';record=json.loads(checkpoint.read_text(encoding='utf-8'))
assert record['status']=='native_Blender_authoring_reopen_and_five_view_gate_passed' and all(v['inspected'] for v in record['visual_views'])
for proof in record['blender_proofs']:assert sha256(Path(proof['blend_file']))==proof['blend_sha256']
text=f"""# Fourth northern row — section {a.step}, {a.date}

Native editable sources in this owner are local-only: `Pilot01.blend`, `{c['section']}_01.blend` and `combined01.blend`. Preserve them and numbered backups; do not stage, commit, upload or retire native binaries through this task.

The EPSG:26911 section bounds are `{c['bounds']}`. South anchor: `{c['south']}`. West anchor: `{c['west']}`. The pilot contains {4 if a.step==1 else 8} meshes, the section contains 256 meshes, and combined review contains {c['total']} meshes. The combined occupied union includes every accepted predecessor and only the first {a.step} new upper-row sections.

Pilot/section interiors use 2 m controls; combined review uses 4 m controls; all borders retain 1 m samples. All three sources saved/reopened with exact samples/borders, positive normals, metre units and packed images. All five required views were inspected. Native source bytes: {record['total_native_source_bytes']}.

Local measured inputs are under `SourceData/Terrain/DeathValley/NorthRow4Survey2026-10-11`; preparation/proofs are under `SourceData/Terrain/DeathValley/{c['batch']}/Prepared01`. The broad palette holds its covered northern edge beyond N4024200; actual DEM and detailed NAIP remain measured.

See the [execution plan](../../../../../Docs/DeathValley/NORTH_ROW4_EXECUTION_PLAN_2026-10-11.md) and [authoring checkpoint](../../../../../Docs/Evidence/DeathValley/{checkpoint.name}). This source checkpoint establishes Blender proof; native Unity and gameplay acceptance have separate gates.
"""
path=c['art']/'README.md';assert not path.exists();path.write_bytes(text.encode())
for target in re.findall(r'\]\(([^)]+)\)',text):assert (path.parent/target).resolve().exists(),target
print('Wrote text-only source README:',path)
