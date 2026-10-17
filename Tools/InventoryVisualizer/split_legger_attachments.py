"""Separate Legger fittings using a staged registry and preserving category moves."""
import argparse
import json
from collections import Counter
from io import BytesIO

import openpyxl
import split_equipment as migration
from category_edits import build_move, DEPENDENCIES
from workbook_edits import atomic_write, digest, parse_sheets

CATEGORY = 'Legger attachments'
IDS = {'proposal.legger.' + suffix for suffix in (
    'sling','cradle','ceramic_backing','pannier_frame','sample_rack','ore_bin',
    'board_bracket','spares_case','seam_guard','impact_cap','fragment_liner','impact_lattice')}
DEFINITION = 'Fitted Legger cargo supports, racks, containers, brackets, service cases and replaceable armor backing, liners and add-ons. Complete protective covers remain Legger armor. Classification grants no fitments, capacity or protection effects.'
BASE = 'General fitted mounts, harnesses, inserts and wearable add-ons. Companion-specific cargo fittings, cases and armor add-ons are Legger attachments; complete protective covers remain Legger armor.'
MANIFEST = migration.ROOT / 'Docs/Design/Items/EQUIPMENT_CLASSIFICATION.json'
AUDIT = migration.ROOT / 'Docs/Design/Items/LEGGER_ATTACHMENT_CLASSIFICATION.json'


def prepare():
    migration.OUT.mkdir(parents=True, exist_ok=True)
    original = migration.SOURCE.read_bytes()
    (migration.OUT / 'before.xlsx').write_bytes(original)
    (migration.OUT / 'manifest-before.json').write_bytes(MANIFEST.read_bytes())
    book = openpyxl.load_workbook(BytesIO(original), data_only=True)
    rows = list(book['Items'].values)
    selected = [dict(id=r[0], name=r[1], previous=r[3]) for r in rows[5:] if r[0] in IDS]
    assert {i['id'] for i in selected} == IDS and all(i['previous']=='Attachments' for i in selected)
    assert {r[0] for r in rows[5:] if r[0] and r[3]=='Attachments' and r[0].startswith('proposal.legger.')} == IDS
    categories, taxonomy = list(book['Categories'].values), list(book['Taxonomy'].values)
    assert not any(r[0]==CATEGORY for r in categories)
    cr=next(n for n,r in enumerate(categories,1) if r[0]=='Attachments')
    tr=next(n for n,r in enumerate(taxonomy,1) if r[:2]==('Category','Attachments'))
    changes=[]
    def change(sheet,address,value,template=None,formula=None,cached=False):
        changes.append(dict(sheet=sheet,address=address,value=value,cachedOnly=cached,template=template,**({'formula':formula} if formula else {})))
    change('Categories',f'C{cr}',BASE)
    change('Taxonomy',f'C{tr}',BASE)
    nc,nt=len(categories)+1,len(taxonomy)+1
    batched={i['id'] for i in selected[:-1]}
    for i,v in enumerate([CATEGORY,len(batched),DEFINITION,'Author-requested Legger attachment category; organization only.','Primary category']):
        change('Categories',f'{migration.col(i)}{nc}',v,f'{migration.col(i)}{cr}',f'COUNTIF(Items!$D$6:$D$1005,A{nc})' if i==1 else None)
    for i,v in enumerate(['Category',CATEGORY,DEFINITION]):change('Taxonomy',f'{migration.col(i)}{nt}',v,f'{migration.col(i)}{tr}')
    for n,r in enumerate(rows[5:],6):
        if r[0] in IDS:change('Items',f'AF{n}',(r[31] or '')+' Author correction, October 15: Legger attachments supersedes Attachments. '+DEFINITION)
        if r[0] in batched:
            change('Items',f'D{n}',CATEGORY)
            terms=list(r)
            terms[3]=CATEGORY
            search='; '.join(str(terms[i] or '') for i in [0,1,3,4,23,24,25,26,27,28,29,21,2,33,34]).lower()
            change('Items',f'AE{n}',search,cached=True)
    formulas=openpyxl.load_workbook(BytesIO(original),data_only=False)
    for sheet,mappings in DEPENDENCIES.items():
        data=list(book[sheet].values)
        header=data[4]
        for key,category in mappings:
            ki,ci=header.index(key),header.index(category)
            for n,r in enumerate(data[5:],6):
                if r[ki] in batched:
                    address=f'{migration.col(ci)}{n}'
                    change(sheet,address,CATEGORY,cached=formulas[sheet][address].data_type=='f')
    change('Categories',f'B{cr}',sum(r[3]=='Attachments' for r in rows[5:] if r[0])-len(batched),formula=f'COUNTIF(Items!$D$6:$D$1005,A{cr})')
    formulas.close()
    plan=dict(sourceHash=digest(original),target=CATEGORY,items=selected,changes=changes,counts={CATEGORY:0},extendTables=dict(Categories=nc,Taxonomy=nt))
    (migration.OUT/'plan.json').write_text(json.dumps(plan,indent=2),encoding='utf-8')
    book.close()
    print('Prepared 12 Legger attachment moves.')


def apply():
    plan=json.loads((migration.OUT/'plan.json').read_text(encoding='utf-8'))
    source=migration.SOURCE
    original=(migration.OUT/'before.xlsx').read_bytes()
    old_manifest=(migration.OUT/'manifest-before.json').read_bytes()
    assert digest(source.read_bytes())==plan['sourceHash'] and MANIFEST.read_bytes()==old_manifest
    assert not AUDIT.exists()
    temporary=migration.OUT/'candidate.xlsx'
    temporary.write_bytes(original)
    migration.SOURCE=temporary
    try:migration.apply()
    finally:migration.SOURCE=source
    candidate=temporary.read_bytes()
    # The last guarded move refreshes every primary count and the dropdown once.
    item=plan['items'][-1]
    candidate,_=build_move(candidate,item['id'],CATEGORY,'Attachments')
    before=parse_sheets(original)['Items'][5:]
    after=parse_sheets(candidate)['Items'][5:]
    assert [r[0] for r in before if r[0]]==[r[0] for r in after if r[0]]
    counts=Counter(r[3] for r in after if r[0])
    assert counts[CATEGORY]==12 and counts['Attachments']==4
    manifest=json.loads(old_manifest)
    for item in manifest['items']:
        if item['id'] in IDS:item.update(category=CATEGORY,reason=DEFINITION)
        elif item['category']=='Attachments':item['reason']=BASE
    manifest['counts']=dict(Counter(i['category'] for i in manifest['items']))
    manifest['sourceHashAfter']=digest(candidate)
    manifest.setdefault('revisions',[]).append(dict(date='2026-10-15',reason='Author separated Legger attachments from general attachments.',movedIds=sorted(IDS),sourceHashBefore=plan['sourceHash'],sourceHashAfter=digest(candidate)))
    atomic_write(source,candidate,plan['sourceHash'])
    atomic_write(MANIFEST,(json.dumps(manifest,indent=2)+'\n').encode(),digest(old_manifest))
    audit=dict(date='2026-10-15',definition=DEFINITION,items=plan['items'],counts={CATEGORY:12,'Attachments':4},sourceHashBefore=plan['sourceHash'],sourceHashAfter=digest(candidate),scope='Catalog organization only. Stable item IDs, art/review choices, fitments, world identity, streaming and persisted gameplay deltas are unchanged.')
    atomic_write(AUDIT,(json.dumps(audit,indent=2)+'\n').encode())
    (migration.OUT/'after.xlsx').write_bytes(candidate)
    (migration.OUT/'receipt.json').write_text(json.dumps(audit,indent=2),encoding='utf-8')
    print('Verified and applied: 12 Legger attachments; 4 general Attachments retained.')


if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('action',choices=['prepare','apply'])
    migration.OUT=migration.ROOT/'Logs/LeggerAttachmentsCategorySplit'
    prepare() if parser.parse_args().action=='prepare' else apply()
