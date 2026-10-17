"""Move the author's currently review-marked Tools to Salvage by stable ID."""
import argparse
import json
from collections import Counter
from io import BytesIO

import openpyxl
import organize_camp_items as guard
import split_equipment as migration
from category_edits import DEPENDENCIES
from workbook_edits import atomic_write, digest

OUT=migration.ROOT/'Logs/ReviewedToolsSalvage20261015'
REVIEW=migration.ROOT/'Docs/Design/Items/ITEM_REVIEW_STATE.json'
AUDIT=migration.ROOT/'Docs/Design/Items/REVIEWED_TOOLS_SALVAGE_CLASSIFICATION.json'
MANIFEST=migration.ROOT/'Docs/Design/Items/EQUIPMENT_CLASSIFICATION.json'
REASON='Author direction October 15: Tools marked for review in the active inventory are reclassified as Salvage. Existing identity, condition, tier, purpose and proposal readiness remain unchanged.'
# Set only for replaying the exact captured selection against the commit baseline.
SELECTED=None
SOURCE_CATEGORY='Tools'
TARGET_CATEGORY='Salvage'


def prepare():
    OUT.mkdir(parents=True,exist_ok=True)
    original=migration.SOURCE.read_bytes()
    review=REVIEW.read_bytes()
    marked=set(json.loads(review)['reviewIds']) if SELECTED is None else set(SELECTED)
    values=openpyxl.load_workbook(BytesIO(original),data_only=True)
    formulas=openpyxl.load_workbook(BytesIO(original),data_only=False)
    rows=list(values['Items'].values)
    selected={r[0] for r in rows[5:] if r[0] in marked and r[3]==SOURCE_CATEGORY}
    assert selected and (SELECTED is None or selected==set(SELECTED))
    changes=[]
    def change(sheet,address,value,cached=False,formula=None):
        changes.append(dict(sheet=sheet,address=address,value=value,cachedOnly=cached,
            template=None,**({'formula':formula} if formula else {})))
    items=[]
    for n,row in enumerate(rows[5:],6):
        if row[0] not in selected:continue
        items.append(dict(id=row[0],name=row[1],previous=row[3]))
        change('Items',f'D{n}',TARGET_CATEGORY)
        change('Items',f'AF{n}',(row[31] or '')+' '+REASON)
        search=list(row);search[3]=TARGET_CATEGORY
        change('Items',f'AE{n}','; '.join(str(search[i] or '') for i in [0,1,3,4,23,24,25,26,27,28,29,21,2,33,34]).lower(),True)
    for sheet,mappings in DEPENDENCIES.items():
        data=list(values[sheet].values);headers=data[4]
        for key,category in mappings:
            ki,ci=headers.index(key),headers.index(category)
            for n,row in enumerate(data[5:],6):
                if row[ki] in selected:
                    address=f'{migration.col(ci)}{n}'
                    change(sheet,address,TARGET_CATEGORY,formulas[sheet][address].data_type=='f')
    counts=Counter(TARGET_CATEGORY if r[0] in selected else r[3] for r in rows[5:] if r[0])
    for n,row in enumerate(values['Categories'].values,1):
        if len(row)>4 and row[4]=='Primary category':
            change('Categories',f'B{n}',counts[row[0]],formula=formulas['Categories'][f'B{n}'].value[1:])
    plan=dict(sourceHash=digest(original),reviewHash=digest(review),target=TARGET_CATEGORY,
        items=items,changes=changes,counts=dict(counts),extendTables={})
    (OUT/'before.xlsx').write_bytes(original)
    (OUT/'review-before.json').write_bytes(review)
    (OUT/'plan.json').write_text(json.dumps(plan,indent=2),encoding='utf-8')
    values.close();formulas.close()
    print('Prepared',len(items),SOURCE_CATEGORY,'entries for',TARGET_CATEGORY)


def apply():
    plan=json.loads((OUT/'plan.json').read_text(encoding='utf-8'))
    assert digest(REVIEW.read_bytes())==plan['reviewHash'],'Review selection changed; recapture before applying'
    source=migration.SOURCE
    candidate_path=OUT/'candidate.xlsx'
    candidate_path.write_bytes((OUT/'before.xlsx').read_bytes())
    migration.SOURCE=candidate_path;migration.OUT=OUT
    try:migration.apply()
    finally:migration.SOURCE=source
    candidate=candidate_path.read_bytes()
    assert not AUDIT.exists()
    assert digest(REVIEW.read_bytes())==plan['reviewHash'],'Review selection changed during preparation'
    guard.release_guard()
    atomic_write(source,candidate,plan['sourceHash'])
    audit=dict(reason=REASON,items=plan['items'],reviewSelectionHash=plan['reviewHash'],
        sourceHashBefore=plan['sourceHash'],sourceHashAfter=digest(candidate),
        scope='Category and linked category/search/count updates only. All review/art state retained. No runtime world identity, streaming, placement or persisted gameplay changes.')
    atomic_write(AUDIT,(json.dumps(audit,indent=2)+'\n').encode())
    print('Moved',len(plan['items']),'entries to',TARGET_CATEGORY,'; review marks retained.')


def update_manifest():
    audit=json.loads(AUDIT.read_text(encoding='utf-8'))
    original=MANIFEST.read_bytes();manifest=json.loads(original)
    selected={item['id'] for item in audit['items']}
    for item in manifest['items']:
        if item['id'] in selected:item.update(category=TARGET_CATEGORY,reason=REASON)
    manifest['counts']=dict(Counter(item['category'] for item in manifest['items']))
    manifest['sourceHashAfter']=audit['sourceHashAfter']
    manifest.setdefault('revisions',[]).append(dict(reason=REASON,movedIds=sorted(selected),
        sourceHashBefore=audit['sourceHashBefore'],sourceHashAfter=audit['sourceHashAfter']))
    atomic_write(MANIFEST,(json.dumps(manifest,indent=2)+'\n').encode(),digest(original))


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('action',choices=['prepare','apply','run'])
    action=parser.parse_args().action
    if action=='run':
        guard.OUT=OUT;guard.prepare=prepare;guard.apply=apply;guard.run()
    else:prepare() if action=='prepare' else apply()
    if action!='prepare':update_manifest()
