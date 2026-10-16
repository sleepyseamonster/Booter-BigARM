"""Give existing ammunition its own category without changing weapon compatibility."""
import json
from collections import Counter
from io import BytesIO

import openpyxl
import organize_camp_items as guard
import split_equipment as migration
from category_edits import DEPENDENCIES, build_move, registry
from workbook_edits import atomic_write, digest, parse_sheets

OUT=migration.ROOT/'Logs/AmmunitionCategory20261015'
AUDIT=migration.ROOT/'Docs/Design/Items/AMMUNITION_CLASSIFICATION.json'
CATEGORY='Ammunition'
DEFINITION='Weapon projectiles, rockets and matched weapon-specific power feeds. Launchers and other weapons remain Weapons. Classification does not establish interchangeability, consumption, recovery, charge or combat behavior.'
REASON='Author direction October 15: weapon ammunition belongs in its own Ammunition category. '+DEFINITION


def prepare():
    OUT.mkdir(parents=True,exist_ok=True)
    original=migration.SOURCE.read_bytes()
    values=openpyxl.load_workbook(BytesIO(original),data_only=True)
    formulas=openpyxl.load_workbook(BytesIO(original),data_only=False)
    rows=list(values['Items'].values)
    selected=[dict(id=r[0],name=r[1],previous=r[3]) for r in rows[5:] if r[0] and r[23]=='Ammunition']
    assert selected and all(i['previous']=='Weapons' for i in selected)
    categories=list(values['Categories'].values);taxonomy=list(values['Taxonomy'].values)
    assert CATEGORY not in registry(categories)
    cr=next(n for n,r in enumerate(categories,1) if r[0]=='Weapons')
    tr=next(n for n,r in enumerate(taxonomy,1) if r[:2]==('Category','Weapons'))
    batched={i['id'] for i in selected[:-1]}
    changes=[]
    def change(sheet,address,value,cached=False,template=None,formula=None):
        changes.append(dict(sheet=sheet,address=address,value=value,cachedOnly=cached,template=template,**({'formula':formula} if formula else {})))
    nc,nt=len(categories)+1,len(taxonomy)+1
    for ci,value in enumerate([CATEGORY,len(batched),DEFINITION,'Author-requested ammunition organization; original item roles retained.','Primary category']):
        change('Categories',f'{migration.col(ci)}{nc}',value,template=f'{migration.col(ci)}{cr}',formula=f'COUNTIF(Items!$D$6:$D$1005,A{nc})' if ci==1 else None)
    for ci,value in enumerate(['Category',CATEGORY,DEFINITION]):
        change('Taxonomy',f'{migration.col(ci)}{nt}',value,template=f'{migration.col(ci)}{tr}')
    change('Categories',f'C{cr}','Weapon equipment and launchers. Projectiles and weapon-specific power feeds are classified separately as Ammunition.')
    for n,row in enumerate(rows[5:],6):
        if row[0] in {i['id'] for i in selected}:change('Items',f'AF{n}',(row[31] or '')+' '+REASON)
        if row[0] in batched:
            change('Items',f'D{n}',CATEGORY)
            search=list(row);search[3]=CATEGORY
            change('Items',f'AE{n}','; '.join(str(search[i] or '') for i in [0,1,3,4,23,24,25,26,27,28,29,21,2,33,34]).lower(),True)
    for sheet,mappings in DEPENDENCIES.items():
        data=list(values[sheet].values);h=data[4]
        for key,category in mappings:
            ki,ci=h.index(key),h.index(category)
            for n,row in enumerate(data[5:],6):
                if row[ki] in batched:
                    address=f'{migration.col(ci)}{n}'
                    change(sheet,address,CATEGORY,formulas[sheet][address].data_type=='f')
    counts=Counter(CATEGORY if r[0] in batched else r[3] for r in rows[5:] if r[0])
    for n,row in enumerate(categories,1):
        if len(row)>4 and row[4]=='Primary category':
            change('Categories',f'B{n}',counts[row[0]],formula=formulas['Categories'][f'B{n}'].value[1:])
    plan=dict(sourceHash=digest(original),target=CATEGORY,items=selected,changes=changes,counts=dict(counts),extendTables=dict(Categories=nc,Taxonomy=nt))
    (OUT/'before.xlsx').write_bytes(original)
    (OUT/'plan.json').write_text(json.dumps(plan,indent=2),encoding='utf-8')
    values.close();formulas.close()


def apply():
    plan=json.loads((OUT/'plan.json').read_text(encoding='utf-8'))
    source=migration.SOURCE
    candidate_path=OUT/'candidate.xlsx';candidate_path.write_bytes((OUT/'before.xlsx').read_bytes())
    migration.SOURCE=candidate_path;migration.OUT=OUT
    try:migration.apply()
    finally:migration.SOURCE=source
    last=plan['items'][-1]
    candidate,_=build_move(candidate_path.read_bytes(),last['id'],CATEGORY,last['previous'])
    sheets=parse_sheets(candidate)
    byid={r[0]:r for r in sheets['Items'][5:] if r[0]}
    assert all(byid[i['id']][3]==CATEGORY for i in plan['items'])
    assert sum(r[3]==CATEGORY for r in byid.values())==len(plan['items'])
    assert CATEGORY in registry(sheets['Categories'])
    assert not AUDIT.exists()
    (OUT/'after.xlsx').write_bytes(candidate)
    guard.release_guard();atomic_write(source,candidate,plan['sourceHash'])
    audit=dict(reason=REASON,items=plan['items'],sourceHashBefore=plan['sourceHash'],sourceHashAfter=digest(candidate),
        scope='Catalog only. IDs, names, tiers, types, properties, recipes, review/art state, world identity, streaming and saved gameplay unchanged.')
    atomic_write(AUDIT,(json.dumps(audit,indent=2)+'\n').encode())
    print('Saved',len(plan['items']),'items in the new Ammunition category.')


if __name__=='__main__':
    guard.OUT=OUT;guard.prepare=prepare;guard.apply=apply;guard.run()
