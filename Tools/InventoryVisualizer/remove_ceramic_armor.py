"""Remove precisely the seven highlighted ceramic-faced armor items."""
import json
import re
from collections import Counter
from io import BytesIO

import openpyxl
import organize_camp_items as guard
import split_equipment as migration
from workbook_edits import atomic_write, build_deletion, digest

OUT=migration.ROOT/'Logs/RemoveCeramicArmor20261016'
AUDIT=migration.ROOT/'Docs/Design/Items/CERAMIC_ARMOR_REMOVAL_2026-10-16.json'
IDS={*['proposal.armor.ceramic.'+p for p in ['chest','legs','boots','arms','helmet']],
     'proposal.armor.shoulder.ceramic_deflector','proposal.collection.ceramic_faced_shield'}
REASON='Author direction October 16: remove the seven highlighted ceramic-faced armor pieces, including the pauldron and shield. Other ceramic goods and the unhighlighted specialist ceramic armor insert are outside this removal.'


def prepare():
    OUT.mkdir(parents=True,exist_ok=True)
    original=migration.SOURCE.read_bytes()
    candidate=original;receipts=[]
    for key in sorted(IDS):
        candidate,receipt=build_deletion(candidate,key);receipts.append(receipt)
    values=openpyxl.load_workbook(BytesIO(candidate),data_only=True)
    formulas=openpyxl.load_workbook(BytesIO(candidate),data_only=False)
    rows=[r for r in list(values['Items'].values)[5:] if r[0]]
    assert not any(r[0] in IDS for r in rows)
    counts=Counter(r[3] for r in rows);changes=[]
    def cache(sheet,address,value):
        formula=formulas[sheet][address]
        assert formula.data_type=='f'
        changes.append(dict(sheet=sheet,address=address,value=value,cachedOnly=False,template=None,formula=formula.value[1:]))
    for n,row in enumerate(values['Categories'].values,1):
        if len(row)>4 and row[4]=='Primary category':cache('Categories',f'B{n}',counts[row[0]])
    for n,row in enumerate(formulas['Collection targets'].values,1):
        if len(row)>4 and row[1]=='Armor':
            excluded=set(re.findall(r"'?Items'?!\$A\$6:\$A\$\d+,\"([^\"]+)\"",row[3] or ''))
            count=sum(r[23]=='Armor' and r[29]!='Withheld assumption' and r[0] not in excluded for r in rows)
            cache('Collection targets',f'D{n}',count);cache('Collection targets',f'E{n}',max(0,row[2]-count))
    plan=dict(sourceHash=digest(candidate),originalHash=digest(original),target='Removed',
        items=[dict(id=r['itemId'],name=r['itemName']) for r in receipts],
        changes=changes,counts=dict(counts),extendTables={},deletions=receipts)
    (OUT/'original.xlsx').write_bytes(original)
    (OUT/'before.xlsx').write_bytes(candidate)
    (OUT/'plan.json').write_text(json.dumps(plan,indent=2),encoding='utf-8')
    values.close();formulas.close()


def apply():
    plan=json.loads((OUT/'plan.json').read_text(encoding='utf-8'))
    source=migration.SOURCE
    temporary=OUT/'candidate.xlsx';temporary.write_bytes((OUT/'before.xlsx').read_bytes())
    migration.SOURCE=temporary;migration.OUT=OUT
    try:migration.apply()
    finally:migration.SOURCE=source
    candidate=temporary.read_bytes()
    assert not AUDIT.exists()
    guard.release_guard();atomic_write(source,candidate,plan['originalHash'])
    audit=dict(reason=REASON,removedItems=plan['items'],deletions=plan['deletions'],
        sourceHashBefore=plan['originalHash'],sourceHashAfter=digest(candidate),
        scope='Catalog removals only, using existing dependency-aware deletion. Unrelated items and stable IDs remain unchanged. No Unity assets, runtime world identity, streaming or saved-game changes.')
    atomic_write(AUDIT,(json.dumps(audit,indent=2)+'\n').encode())
    print('Removed seven highlighted ceramic-faced armor items and linked records; recovery snapshot retained.')


def update_manifest():
    path=migration.ROOT/'Docs/Design/Items/EQUIPMENT_CLASSIFICATION.json'
    original=path.read_bytes();manifest=json.loads(original)
    audit=json.loads(AUDIT.read_text())
    manifest['items']=[i for i in manifest['items'] if i['id'] not in IDS]
    manifest['counts']=dict(Counter(i['category'] for i in manifest['items']))
    manifest['sourceHashAfter']=audit['sourceHashAfter']
    manifest.setdefault('revisions',[]).append(dict(reason=REASON,removedIds=sorted(IDS),sourceHashBefore=audit['sourceHashBefore'],sourceHashAfter=audit['sourceHashAfter']))
    atomic_write(path,(json.dumps(manifest,indent=2)+'\n').encode(),digest(original))


if __name__=='__main__':
    guard.OUT=OUT;guard.prepare=prepare;guard.apply=apply;guard.run()
    update_manifest()
