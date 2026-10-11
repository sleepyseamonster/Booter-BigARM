"""Seal completed production, metadata, coverage, current-catalog tests and inspected map views."""
import argparse,json,shutil,sys,xml.etree.ElementTree as ET
from pathlib import Path
from north_row4_contract import ROOT
root=ROOT
from north_row4_contract import sha256,contract
p=argparse.ArgumentParser();p.add_argument('--step',type=int,required=True);p.add_argument('--date',default='2026-10-11');p.add_argument('--catalog-test-version',default='01');p.add_argument('--map-source',type=Path,required=True);p.add_argument('--isolation',type=Path,required=True);p.add_argument('--inspected-map-views',action='store_true',required=True);a=p.parse_args()
c=contract(a.step,a.date);iso=a.isolation.resolve();ev=root/'Docs/Evidence/DeathValley'/c['batch']
suffix='' if a.catalog_test_version=='01' else a.catalog_test_version
xml=iso/f'Logs/step{a.step}-current-catalog-tests{suffix}.xml';x=ET.parse(xml).getroot()
assert x.attrib['result']=='Passed' and x.attrib['failed']=='0' and int(x.attrib['passed'])==34
shutil.copy2(xml,ev/xml.name)
catalog=root/'Docs/DeathValley/coverage_catalog.json';cat=json.loads(catalog.read_text(encoding='utf-8'))
report=json.loads((ev/f'step{a.step}-production-readback.json').read_text(encoding='utf-8'))
assert cat['unity_scene']['sha256']==report['scene_sha256'] and len(cat['unity_tiles'])==c['total'] and cat['source_audit']['shared_edges_checked']==c['edges']
assert sha256(root/report['scene'])==report['scene_sha256']
log=iso/f'Logs/step{a.step}-playable.log'
assert f'Playable Badwater scene validated: {c["total"]} terrains, Booter, Legger, input, camera, UI, twilight and dust.' in log.read_text(encoding='utf-8')
metadata=json.loads((ev/'metadata_audit.json').read_text(encoding='utf-8'))
assert metadata['status']=='owned_metadata_and_global_GUID_uniqueness_verified' and metadata['step']==a.step and metadata['manifest_sha256']==report['manifest_sha256']
views=[]
for name in ['unity-overview.png','unity-chunks.png','unity-chunks-top.png']:
 source=(a.map_source if a.map_source else root/'Logs/DeathValleyVisualizer')/name;target=ev/name
 assert not target.exists();shutil.copy2(source,target);views.append(dict(path=target.relative_to(root).as_posix(),sha256=sha256(target),inspected=True))
receipt=dict(schema_version=1,family='north_row4',step=a.step,status='native_integration_coverage_metadata_and_map_closeout_passed',
 production_scene_sha256=report['scene_sha256'],manifest_sha256=report['manifest_sha256'],
 coverage_catalog_sha256=sha256(catalog),height_files=c['total'],color_files=c['total'],exact_joins=c['edges'],
 metadata_audit_sha256=sha256(ev/'metadata_audit.json'),canonical_playable_validator=dict(exit_code=0,local_log=str(log),sha256=sha256(log)),
 current_catalog_tests=dict(passed=34,failed=0,xml_sha256=sha256(ev/xml.name)),
 developer_map_views=views,limits='Saved native and developer-map proof; no foreground activation or hands-on gameplay smoke test.')
assert not (ev/'closeout.json').exists()
(ev/'closeout.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(json.dumps(receipt,indent=2))
