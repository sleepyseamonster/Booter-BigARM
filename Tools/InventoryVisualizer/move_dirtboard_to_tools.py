"""Reclassify dedicated dirtboard/sail items in one preserving workbook batch."""
import argparse
import json
from collections import Counter
from io import BytesIO

import openpyxl
import split_equipment as migration
from category_edits import DEPENDENCIES
from workbook_edits import atomic_write, digest

IDS = {
    'proposal.wind.sail_panel','proposal.wind.folding_frame','proposal.wind.sail_grip',
    'proposal.wind.wheel','proposal.wind.truck','proposal.legger.board_bracket',
    'proposal.t4.spar_joint','proposal.t4.wheel_hub',
    'proposal.collection.dirtboard_running_gear_kit','proposal.collection.dirtboard_lane_snag_bar',
}
REASON = 'Author correction: dedicated dirtboard and handheld-sail items belong in Tools, including their parts, service kit, carrying bracket and named lane snag bar. Category does not change item type, tier, recipe role or gameplay behavior.'
MANIFEST = migration.ROOT / 'Docs/Design/Items/EQUIPMENT_CLASSIFICATION.json'
AUDIT = migration.ROOT / 'Docs/Design/Items/DIRTBOARD_TOOLS_CLASSIFICATION.json'


def prepare():
    migration.OUT.mkdir(parents=True,exist_ok=True)
    original=migration.SOURCE.read_bytes()
    (migration.OUT/'before.xlsx').write_bytes(original)
    (migration.OUT/'manifest-before.json').write_bytes(MANIFEST.read_bytes())
    values=openpyxl.load_workbook(BytesIO(original),data_only=True)
    formulas=openpyxl.load_workbook(BytesIO(original),data_only=False)
    rows=list(values['Items'].values)
    items=[dict(id=r[0],name=r[1],previous=r[3]) for r in rows[5:] if r[0] in IDS]
    assert {i['id'] for i in items}==IDS and all(i['previous']!='Tools' for i in items)
    changes=[]
    def change(sheet,address,value,cached=False,formula=None):
        changes.append(dict(sheet=sheet,address=address,value=value,cachedOnly=cached,template=None,**({'formula':formula} if formula else {})))
    for n,r in enumerate(rows[5:],6):
        if r[0] not in IDS:continue
        change('Items',f'D{n}','Tools')
        change('Items',f'AF{n}',(r[31] or '')+' '+REASON)
        terms=list(r);terms[3]='Tools'
        change('Items',f'AE{n}','; '.join(str(terms[i] or '') for i in [0,1,3,4,23,24,25,26,27,28,29,21,2,33,34]).lower(),True)
    for sheet,mappings in DEPENDENCIES.items():
        data=list(values[sheet].values);h=data[4]
        for key,category in mappings:
            ki,ci=h.index(key),h.index(category)
            for n,r in enumerate(data[5:],6):
                if r[ki] in IDS:
                    address=f'{migration.col(ci)}{n}'
                    change(sheet,address,'Tools',formulas[sheet][address].data_type=='f')
    counts=Counter('Tools' if r[0] in IDS else r[3] for r in rows[5:] if r[0])
    for n,r in enumerate(values['Categories'].values,1):
        if len(r)>4 and r[4]=='Primary category':
            address=f'B{n}';formula=formulas['Categories'][address]
            assert formula.data_type=='f'
            change('Categories',address,counts[r[0]],formula=formula.value[1:])
    plan=dict(sourceHash=digest(original),target='Tools',items=items,changes=changes,counts=dict(counts),extendTables={})
    (migration.OUT/'plan.json').write_text(json.dumps(plan,indent=2),encoding='utf-8')
    values.close();formulas.close()
    print('Prepared ten dirtboard and sail category moves.')


def apply():
    plan=json.loads((migration.OUT/'plan.json').read_text(encoding='utf-8'))
    old=(migration.OUT/'manifest-before.json').read_bytes()
    assert MANIFEST.read_bytes()==old and not AUDIT.exists()
    migration.apply()
    resultHash=digest(migration.SOURCE.read_bytes())
    manifest=json.loads(old)
    for i in manifest['items']:
        if i['id'] in IDS:i.update(category='Tools',reason=REASON)
    manifest['counts']=dict(Counter(i['category'] for i in manifest['items']))
    manifest['sourceHashAfter']=resultHash
    manifest.setdefault('revisions',[]).append(dict(date='2026-10-15',reason=REASON,movedIds=sorted(IDS),sourceHashBefore=plan['sourceHash'],sourceHashAfter=resultHash))
    atomic_write(MANIFEST,(json.dumps(manifest,indent=2)+'\n').encode(),digest(old))
    audit=dict(date='2026-10-15',reason=REASON,movedItems=plan['items'],alreadyTools=['proposal.wind.hand_sail','proposal.wind.dirtboard'],sharedMaterialsRetained=['proposal.bio.inner_wing','proposal.t3.bearing_blank'],sourceHashBefore=plan['sourceHash'],sourceHashAfter=resultHash,scope='Catalog only. No world identity, streaming, placed objects, persisted gameplay deltas, slots or recipes changed.')
    atomic_write(AUDIT,(json.dumps(audit,indent=2)+'\n').encode())
    print('Ten items moved; board and handheld sail were already Tools.')


if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('action',choices=['prepare','apply'])
    migration.OUT=migration.ROOT/'Logs/DirtboardToolsCategoryMove'
    prepare() if parser.parse_args().action=='prepare' else apply()
