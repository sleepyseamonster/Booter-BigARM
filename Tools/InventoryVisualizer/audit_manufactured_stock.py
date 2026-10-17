"""Apply a complete stock audit without deleting or reclassifying candidates."""
import json
import re
import shutil
import sys
from collections import Counter
from io import BytesIO

import openpyxl
import organize_camp_items as guard
import split_equipment as migration
from category_edits import verify_move
from workbook_edits import atomic_write, digest, parse_sheets

OUT=migration.ROOT/'Logs/ManufacturedStockAudit20261016'
SPEC=migration.ROOT/'Docs/Design/Items/MANUFACTURED_STOCK_AUDIT_INPUTS_2026-10-16.json'
AUDIT=migration.ROOT/'Docs/Design/Items/MANUFACTURED_STOCK_AUDIT_2026-10-16.json'
REVIEW=migration.ROOT/'Docs/Design/Items/ITEM_REVIEW_STATE.json'
PREVIOUS_AUDIT=None
NAME_LINKS={
    'Item uses':[('Item ID','Item name')],
    'Material routes':[('Item ID','Item name')],
    'Tier review':[('Item ID','Item name')],
    'Craft audit':[('Item ID','Item name')],
    'Crafting browser':[('Input item ID','Input name'),('Output item ID','Output name')],
    'Item relationships':[('Parent item ID','Parent item'),('Related item ID','Related item')],
}
SEARCH_FIELDS=['Item ID','Item name','Category','Subtype','Item type','Processing stage',
               'Origin family','Primary use','Material family','Physical form','Design status',
               'Search aliases','Tier','Finished-item input tier','Crafting role']


def prepare():
    assert not AUDIT.exists() or PREVIOUS_AUDIT,'This audit has already been applied.'
    OUT.mkdir(parents=True,exist_ok=True)
    current=migration.SOURCE.read_bytes()
    if PREVIOUS_AUDIT:
        assert digest(current)==json.loads(PREVIOUS_AUDIT)['sourceHashAfter'],'Workbook changed independently; inspect before refining.'
        original=(OUT/'before.xlsx').read_bytes()
    else:original=current
    if (OUT/'before.xlsx').exists():
        assert original==(OUT/'before.xlsx').read_bytes(),'Catalog changed during review; inspect the new state before proceeding.'
    spec=json.loads(SPEC.read_bytes())
    values=openpyxl.load_workbook(BytesIO(original),data_only=True)
    formulas=openpyxl.load_workbook(BytesIO(original),data_only=False)
    sheets={s.title:list(s.values) for s in values}
    h=sheets['Items'][4]
    byid={r[0]:(n,dict(zip(h,r))) for n,r in enumerate(sheets['Items'][5:],6) if r[0]}
    stock={k for k,(_,i) in byid.items() if i['Category']=='Manufactured stock'}
    ids=[i['id'] for i in spec['items']]
    assert len(ids)==len(set(ids)) and set(ids)==stock and len(stock)==100
    retained={i['id']:i for i in spec['items'] if i.get('decision')!='propose removal'}
    proposed={i['id']:i for i in spec['items'] if i.get('decision')=='propose removal'}
    assert len(retained)==94 and len(proposed)==6
    new_names={k:i['name'] for k,i in retained.items()}
    roster=[new_names.get(k,i['Item name']).casefold() for k,(_,i) in byid.items()]
    assert len(roster)==len(set(roster)),'A revised name collides with another live item.'
    edits={}
    def change(sheet,n,field,value):
        headers=sheets[sheet][4];ci=headers.index(field)
        if sheets[sheet][n-1][ci]==value:return
        address=f'{migration.col(ci)}{n}'
        edits[sheet,address]=dict(sheet=sheet,address=address,value=value,
            cachedOnly=formulas[sheet][address].data_type=='f',template=None)
    renamed={}
    for key,item in retained.items():
        n,old=byid[key];updated=dict(old)
        updated.update(item.get('metadata',{}))
        updated.update({'Item name':item['name'],'Purpose':item['description']})
        assert len(item['description'])>=120
        if old['Item name']!=item['name']:
            renamed[old['Item name']]=item['name']
            updated['Search aliases']='; '.join(dict.fromkeys(x for x in [old.get('Search aliases'),old['Item name']] if x))
        updated['Search terms']='; '.join(str(updated.get(k) or '') for k in SEARCH_FIELDS).lower()
        for field in ['Item name','Purpose','Search aliases','Search terms',*item.get('metadata',{})]:
            change('Items',n,field,updated.get(field))
    for sheet,mappings in NAME_LINKS.items():
        headers=sheets[sheet][4]
        for key,field in mappings:
            ki=headers.index(key)
            for n,row in enumerate(sheets[sheet][5:],6):
                if row[ki] in new_names:change(sheet,n,field,new_names[row[ki]])
    # Name-only formula projections include branch overviews and candidate-input
    # lists. Preserve their formulas and rewrite their display caches once.
    pattern=re.compile('|'.join(re.escape(x) for x in sorted(renamed,key=len,reverse=True)))
    for sheet in formulas:
        for row in sheet:
            for cell in row:
                if cell.data_type!='f' or (sheet.title,cell.coordinate) in edits:continue
                cached=values[sheet.title][cell.coordinate].value
                if not isinstance(cached,str):continue
                projected=pattern.sub(lambda m:renamed[m.group()],cached)
                if projected!=cached:
                    edits[sheet.title,cell.coordinate]=dict(sheet=sheet.title,address=cell.coordinate,
                        value=projected,cachedOnly=True,template=None)
    decisions=[]
    for item in spec['items']:
        _,old=byid[item['id']]
        decisions.append(dict(id=item['id'],previousName=old['Item name'],previousDescription=old['Purpose'],
            decision=item.get('decision','retain and refine'),name=item.get('name',old['Item name']),
            description=item.get('description',old['Purpose']),
            reason=item.get('reason','Retain a specific prepared material form for subsequent fitting or fabrication.'),
            metadataCorrections=item.get('metadata',{})))
    plan=dict(sourceHash=digest(original),expectedLiveHash=digest(current),specHash=digest(SPEC.read_bytes()),reviewHash=digest(REVIEW.read_bytes()),
        items=[dict(id=k,name=i['name'],description=i['description']) for k,i in retained.items()],
        decisions=decisions,changes=list(edits.values()),extendTables={},
        counts=dict(reviewed=len(stock),retained=len(retained),renamed=len(renamed),descriptions=len(retained),removalProposals=len(proposed)))
    (OUT/'before.xlsx').write_bytes(original)
    (OUT/'review-before.json').write_bytes(REVIEW.read_bytes())
    (OUT/'plan.json').write_bytes((json.dumps(plan,indent=2)+'\n').encode())
    shutil.copyfile(migration.ROOT/'Tools/InventoryVisualizer/stage_stock_audit.mjs',OUT/'stage.mjs')
    values.close();formulas.close()
    print('Prepared',plan['counts'])


def apply():
    plan=json.loads((OUT/'plan.json').read_bytes())
    assert digest(SPEC.read_bytes())==plan['specHash']
    assert digest(REVIEW.read_bytes())==plan['reviewHash'],'Review choices changed; recapture without overwriting them.'
    original=(OUT/'before.xlsx').read_bytes()
    source=migration.SOURCE;candidate_path=OUT/'candidate.xlsx'
    candidate_path.write_bytes(original)
    migration.SOURCE=candidate_path;migration.OUT=OUT
    try:migration.apply()
    finally:migration.SOURCE=source
    candidate=candidate_path.read_bytes()
    edits={}
    for c in plan['changes']:edits.setdefault(c['sheet'],{})[c['address']]=c['value']
    verify_move(original,candidate,edits,category_contract=False)
    before,after=parse_sheets(original),parse_sheets(candidate)
    old={r[0]:r for r in before['Items'][5:] if r[0]}
    new={r[0]:r for r in after['Items'][5:] if r[0]}
    assert list(old)==list(new),'Item identities or order changed.'
    assert Counter(r[3] for r in old.values())==Counter(r[3] for r in new.values())
    for item in plan['decisions']:
        key=item['id']
        assert old[key][2]==new[key][2],'Tier changed.'
        if item['decision']=='propose removal':assert old[key]==new[key],'A removal candidate was changed.'
        else:assert new[key][1]==item['name'] and new[key][6]==item['description']
    assert digest(REVIEW.read_bytes())==plan['reviewHash']
    spec=json.loads(SPEC.read_bytes())
    # Capture dependency obligations for proposals instead of deleting anything.
    proposals=[]
    for item in plan['decisions']:
        if item['decision']!='propose removal':continue
        key=item['id'];refs={}
        for sheet,rows in before.items():
            if sheet=='Items':continue
            count=sum(any(v==key for v in r) for r in rows[5:])
            if count:refs[sheet]=count
        proposals.append(dict(id=key,name=item['previousName'],reason=item['reason'],exactIdReferenceRows=refs))
    audit=dict(date=spec['date'],authority=spec['authority'],criterion=spec['criterion'],scope=spec['scope'],
        counts=plan['counts'],decisions=plan['decisions'],removalProposals=proposals,
        sourceHashBefore=plan['sourceHash'],sourceHashAfter=digest(candidate),
        validation=dict(allItemIdsAndOrderPreserved=True,allCategoriesAndTiersPreserved=True,
            removalCandidatesUnchanged=True,reviewStateUnchanged=True,
            unrelatedCellsAndZipParts='Byte-identical; formulas and edited-cell styles preserved.',
            changedCellsBySheet={s:len(cells) for s,cells in edits.items()}),
        unityBoundary='Variants.Authored name and authored description remain imported Unity source facts; no source assets or definitions are renamed. Earlier source/proposal documents retain their historical labels. Old catalog names are searchable aliases.')
    assert not AUDIT.exists() or AUDIT.read_bytes()==PREVIOUS_AUDIT
    guard.release_guard();atomic_write(source,candidate,plan['expectedLiveHash'])
    atomic_write(AUDIT,(json.dumps(audit,indent=2)+'\n').encode(),digest(PREVIOUS_AUDIT) if PREVIOUS_AUDIT else None)
    print('Saved and verified',plan['counts'],'; no items removed.')


if __name__=='__main__':
    if '--refine' in sys.argv:PREVIOUS_AUDIT=AUDIT.read_bytes()
    guard.OUT=OUT;guard.prepare=prepare;guard.apply=apply;guard.run()
