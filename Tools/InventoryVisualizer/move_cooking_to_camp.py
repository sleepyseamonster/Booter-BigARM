"""Follow the author's all-cooking-in-camp correction without changing readiness."""
import argparse
import json
from collections import Counter
from io import BytesIO

import openpyxl
import split_equipment as migration
from category_edits import DEPENDENCIES
from workbook_edits import atomic_write, digest

OUT=migration.ROOT/'Logs/AllCookingInCamp20261015'
AUDIT=migration.ROOT/'Docs/Design/Items/COOKING_CAMP_CLASSIFICATION.json'
MANIFEST=migration.ROOT/'Docs/Design/Items/EQUIPMENT_CLASSIFICATION.json'
GROUPS={'proposal.fire.firebox':'Cooking & ignition',
        'proposal.salvage.expedition_cookpot':'Cooking & ignition',
        'proposal.collection.household_meal_ledger':'Camp records'}
REASON='Author correction: all cooking equipment and related household food-preparation records belong in Camp items, including the cooking station and damaged cooking vessels. Classification does not repair salvage or establish working equipment.'


def prepare():
    OUT.mkdir(parents=True,exist_ok=True)
    original=migration.SOURCE.read_bytes()
    values=openpyxl.load_workbook(BytesIO(original),data_only=True)
    formulas=openpyxl.load_workbook(BytesIO(original),data_only=False)
    rows=list(values['Items'].values);changes=[];items=[]
    assert all(any(r[0]==key for r in rows[5:]) for key in GROUPS)
    def change(sheet,address,value,cached=False,formula=None):
        changes.append(dict(sheet=sheet,address=address,value=value,cachedOnly=cached,template=None,**({'formula':formula} if formula else {})))
    for n,r in enumerate(rows[5:],6):
        if r[0] not in GROUPS:continue
        assert r[3]!='Camp items','Already regrouped'
        group=GROUPS[r[0]]
        items.append(dict(id=r[0],name=r[1],previous=r[3],previousSubtype=r[4],group=group))
        change('Items',f'D{n}','Camp items');change('Items',f'E{n}',group)
        aliases='; '.join(str(v) for v in [r[21],r[4],group] if v)
        change('Items',f'V{n}',aliases)
        change('Items',f'AF{n}',(r[31] or '')+' '+REASON)
        terms=list(r);terms[3]='Camp items';terms[4]=group;terms[21]=aliases
        change('Items',f'AE{n}','; '.join(str(terms[i] or '') for i in [0,1,3,4,23,24,25,26,27,28,29,21,2,33,34]).lower(),True)
    for sheet,mappings in DEPENDENCIES.items():
        data=list(values[sheet].values);h=data[4]
        for key,category in mappings:
            ki,ci=h.index(key),h.index(category)
            for n,r in enumerate(data[5:],6):
                if r[ki] in GROUPS:
                    a=f'{migration.col(ci)}{n}';change(sheet,a,'Camp items',formulas[sheet][a].data_type=='f')
    counts=Counter('Camp items' if r[0] in GROUPS else r[3] for r in rows[5:] if r[0])
    for n,r in enumerate(values['Categories'].values,1):
        if len(r)>4 and r[4]=='Primary category':
            change('Categories',f'B{n}',counts[r[0]],formula=formulas['Categories'][f'B{n}'].value[1:])
        if r[0]=='Camp items':change('Categories',f'C{n}','Camp shelter, rest, all cooking equipment (including the cooking station and damaged cookpots), food service/storage, cleaning, grooming and everyday records. Category does not change condition, readiness or gameplay effects.')
        if r[0]=='Crafting stations':change('Categories',f'C{n}','Placed non-cooking crafting, firing and forging facilities. The dedicated Cooking station is grouped with Camp items by author direction; its station behavior is unchanged.')
    for n,r in enumerate(values['Taxonomy'].values,1):
        if r[:2]==('Category','Camp items'):change('Taxonomy',f'C{n}','Camp shelter, rest, cooking equipment and related everyday goods/records, including the Cooking station and damaged cooking vessels. No change to readiness or gameplay behavior.')
        if r[:2]==('Category','Crafting stations'):change('Taxonomy',f'C{n}','Non-cooking crafting, firing and forging facilities. Cooking station is classified as Camp items by author direction.')
    plan=dict(sourceHash=digest(original),target='Camp items',items=items,changes=changes,counts=dict(counts),extendTables={})
    (OUT/'before.xlsx').write_bytes(original);(OUT/'manifest-before.json').write_bytes(MANIFEST.read_bytes())
    (OUT/'plan.json').write_text(json.dumps(plan,indent=2),encoding='utf-8')
    values.close();formulas.close()
    print('Prepared three cooking-related camp moves.')


def apply():
    plan=json.loads((OUT/'plan.json').read_text(encoding='utf-8'))
    old=(OUT/'manifest-before.json').read_bytes()
    assert MANIFEST.read_bytes()==old and not AUDIT.exists()
    migration.OUT=OUT;migration.apply()
    resultHash=digest(migration.SOURCE.read_bytes())
    manifest=json.loads(old)
    for item in manifest['items']:
        if item['id'] in GROUPS:item.update(category='Camp items',reason=REASON)
    manifest['counts']=dict(Counter(i['category'] for i in manifest['items']));manifest['sourceHashAfter']=resultHash
    manifest.setdefault('revisions',[]).append(dict(date='2026-10-15',reason=REASON,movedIds=list(GROUPS),sourceHashBefore=plan['sourceHash'],sourceHashAfter=resultHash))
    atomic_write(MANIFEST,(json.dumps(manifest,indent=2)+'\n').encode(),digest(old))
    audit=dict(date='2026-10-15',reason=REASON,items=plan['items'],sourceHashBefore=plan['sourceHash'],sourceHashAfter=resultHash,scope='Catalog-only correction. Preserve tiers, types, condition, purposes, recipes, stable IDs, world identity, streaming and saved gameplay deltas.')
    atomic_write(AUDIT,(json.dumps(audit,indent=2)+'\n').encode())
    print('Saved all three moves; camp count:',plan['counts']['Camp items'])


if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('action',choices=['prepare','apply'])
    prepare() if p.parse_args().action=='prepare' else apply()
