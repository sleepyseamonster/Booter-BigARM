"""Organize existing camp goods, retaining item identity and noncamp ownership."""
import argparse
import ctypes
import json
import subprocess
from collections import Counter
from ctypes import wintypes
from io import BytesIO
from pathlib import Path

import openpyxl
import split_equipment as migration
from category_edits import DEPENDENCIES
from workbook_edits import atomic_write, digest, parse_sheets

OUT=migration.ROOT/'Logs/CampItemsCategory20261015'
AUDIT=migration.ROOT/'Docs/Design/Items/CAMP_ITEM_CLASSIFICATION.json'
MANIFEST=migration.ROOT/'Docs/Design/Items/EQUIPMENT_CLASSIFICATION.json'
DEFINITION='Camp shelter, rest, cooking, food service/storage, cleaning, grooming and everyday records. Dedicated crafting stations, worn clothing, personal accessories, raw feeds and damaged recovered finds retain their own categories. Organization does not assign gameplay capacity, slots or effects.'
GROUPS={
    'Shelter & rest': ['tool.popup_camp','proposal.bio.wing_panel',
        *['proposal.collection.'+s for s in ['folding_vertical_wing_shade','low_wing_roof_panel','segmented_shelter_pole_set','ground_anchor_stake_set','tendon_guyline_bundle','husk_sheet_grit_screen','shell_ground_rest_panel','folding_privacy_partition','canister_shelter_docking_stand','shelter_repair_patch_roll']],
        *['proposal.camp.'+s for s in ['cam_tie_down_buckle','guyline_tensioner','split_ring_hanging_hook']]],
    'Cooking & ignition': [*['proposal.fire.'+s for s in ['cooking_pot','lidded_pan','rack','pot_stand','tongs','striker']],
        'proposal.audit.food_preparation_screen','proposal.camp.divided_warming_pot','proposal.camp.pot_lid_rest'],
    'Food service & storage': ['proposal.culture.bowl','proposal.culture.spoon',
        *['proposal.ceramic.'+s for s in ['bowl','cup','plate','storage_crock']],
        *['proposal.collection.'+s for s in ['long_handled_meal_ladle','ceramic_portion_scoop','shell_serving_tray','metal_eating_tongs']],
        'proposal.camp.hinged_utensil_case','proposal.container.field_chest','proposal.food.drink_flask','proposal.food.bar_case'],
    'Cleaning & personal care': ['proposal.culture.brush','proposal.culture.comb','proposal.camp.folding_grit_pan'],
    'Camp records': ['proposal.culture.tablet','proposal.culture.stylus'],
}
_guard=None


def release_guard():
    global _guard
    if _guard:
        kernel,handle=_guard;kernel.CloseHandle(handle);_guard=None


def run():
    global _guard
    kernel=ctypes.WinDLL('kernel32',use_last_error=True)
    kernel.CreateFileW.argtypes=[wintypes.LPCWSTR,wintypes.DWORD,wintypes.DWORD,wintypes.LPVOID,wintypes.DWORD,wintypes.DWORD,wintypes.HANDLE]
    kernel.CreateFileW.restype=wintypes.HANDLE
    kernel.CloseHandle.argtypes=[wintypes.HANDLE]
    handle=kernel.CreateFileW(str(migration.SOURCE),0x80000000,1,None,3,0x80,None)
    if handle==ctypes.c_void_p(-1).value:raise ctypes.WinError(ctypes.get_last_error())
    _guard=(kernel,handle)
    try:
        prepare()
        node=Path.home()/'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe'
        subprocess.run([str(node),str(OUT/'stage.mjs'),str(OUT)],check=True)
        apply()
    finally:release_guard()


def prepare():
    OUT.mkdir(parents=True,exist_ok=True)
    original=migration.SOURCE.read_bytes()
    values=openpyxl.load_workbook(BytesIO(original),data_only=True)
    formulas=openpyxl.load_workbook(BytesIO(original),data_only=False)
    rows=list(values['Items'].values)
    byid={r[0]:r for r in rows[5:] if r[0]}
    mapping={key:group for group,keys in GROUPS.items() for key in keys}
    assert len(mapping)==sum(map(len,GROUPS.values()))==43
    assert mapping.keys()<=byid.keys(),'A reviewed item is missing; review the live roster'
    everyday={r[0] for r in rows[5:] if r[0] and r[3]=='Everyday objects'}
    assert len(everyday)==20 and everyday<=mapping.keys()
    categories=list(values['Categories'].values);taxonomy=list(values['Taxonomy'].values)
    assert not any(r[0]=='Camp items' for r in categories),'Already registered'
    changes=[];items=[]
    def change(sheet,address,value,cached=False,template=None,formula=None):
        changes.append(dict(sheet=sheet,address=address,value=value,cachedOnly=cached,template=template,**({'formula':formula} if formula else {})))
    for n,r in enumerate(rows[5:],6):
        if r[0] not in mapping:continue
        group=mapping[r[0]]
        items.append(dict(id=r[0],name=r[1],previous=r[3],previousSubtype=r[4],group=group))
        change('Items',f'D{n}','Camp items');change('Items',f'E{n}',group)
        aliases='; '.join(dict.fromkeys(str(v) for v in [r[21],r[4],'camp goods',group] if v))
        change('Items',f'V{n}',aliases)
        change('Items',f'AF{n}',(r[31] or '')+f' Author camp organization: Camp items / {group}; previous category/subtype: {r[3]} / {r[4]}.')
        terms=list(r);terms[3]='Camp items';terms[4]=group;terms[21]=aliases
        change('Items',f'AE{n}','; '.join(str(terms[i] or '') for i in [0,1,3,4,23,24,25,26,27,28,29,21,2,33,34]).lower(),True)
    for sheet,mappings in DEPENDENCIES.items():
        data=list(values[sheet].values);h=data[4]
        for key,category in mappings:
            ki,ci=h.index(key),h.index(category)
            for n,r in enumerate(data[5:],6):
                if r[ki] in mapping:
                    addr=f'{migration.col(ci)}{n}';change(sheet,addr,'Camp items',formulas[sheet][addr].data_type=='f')
    counts=Counter('Camp items' if r[0] in mapping else r[3] for r in rows[5:] if r[0])
    cr=next(n for n,r in enumerate(categories,1) if r[0]=='Everyday objects')
    tr=next(n for n,r in enumerate(taxonomy,1) if r[:2]==('Category','Everyday objects'))
    retired='Retired primary category: all twenty everyday objects are now organized under Camp items. Retained only as a historical label.'
    change('Categories',f'C{cr}',retired);change('Categories',f'E{cr}','Legacy category')
    change('Taxonomy',f'C{tr}',retired)
    for n,r in enumerate(categories,1):
        if len(r)>4 and r[4]=='Primary category':
            formula=formulas['Categories'][f'B{n}'].value
            assert formula.startswith('=')
            change('Categories',f'B{n}',counts[r[0]],formula=formula[1:])
    nc,nt=len(categories)+1,len(taxonomy)+1
    for i,v in enumerate(['Camp items',43,DEFINITION,'Author-requested camp organization; existing behavior retained.','Primary category']):
        change('Categories',f'{migration.col(i)}{nc}',v,template=f'{migration.col(i)}{cr}',formula=f'COUNTIF(Items!$D$6:$D$1005,A{nc})' if i==1 else None)
    for i,v in enumerate(['Category','Camp items',DEFINITION]):change('Taxonomy',f'{migration.col(i)}{nt}',v,template=f'{migration.col(i)}{tr}')
    choices=sorted([r[0] for r in categories if len(r)>4 and r[4]=='Primary category' and r[0]!='Everyday objects']+['Camp items'])
    assert len(choices)==sum(len(r)>4 and r[4]=='Primary category' for r in categories)
    name=formulas.defined_names['InventoryCategoryChoices'].attr_text
    assert name==f"'Categories'!$G$6:$G${len(choices)+5}",name
    for n,choice in enumerate(choices,6):change('Categories',f'G{n}',choice)
    plan=dict(sourceHash=digest(original),target='Camp items',items=items,changes=changes,counts=dict(counts),extendTables=dict(Categories=nc,Taxonomy=nt))
    (OUT/'before.xlsx').write_bytes(original);(OUT/'manifest-before.json').write_bytes(MANIFEST.read_bytes())
    (OUT/'plan.json').write_text(json.dumps(plan,indent=2),encoding='utf-8')
    values.close();formulas.close()
    print('Prepared 43 camp items across five groups; retire Everyday objects from active rail.')


def apply():
    plan=json.loads((OUT/'plan.json').read_text(encoding='utf-8'))
    source=migration.SOURCE;original=(OUT/'before.xlsx').read_bytes()
    old_manifest=(OUT/'manifest-before.json').read_bytes()
    assert digest(source.read_bytes())==plan['sourceHash'] and MANIFEST.read_bytes()==old_manifest and not AUDIT.exists()
    candidate_path=OUT/'candidate.xlsx';candidate_path.write_bytes(original)
    migration.SOURCE=candidate_path;migration.OUT=OUT
    try:migration.apply()
    finally:migration.SOURCE=source
    candidate=candidate_path.read_bytes();sheets=parse_sheets(candidate)
    ids=lambda data:[r[0] for r in data['Items'][5:] if r[0]]
    assert ids(sheets)==ids(parse_sheets(original))
    assert sum(r[3]=='Camp items' for r in sheets['Items'][5:] if r[0])==43
    assert not any(r[3]=='Everyday objects' for r in sheets['Items'][5:] if r[0])
    manifest=json.loads(old_manifest);selected={i['id']:i for i in plan['items']}
    for i in manifest['items']:
        if i['id'] in selected:i.update(category='Camp items',reason=f'Author camp organization: {selected[i["id"]]["group"]}.')
    manifest['counts']=dict(Counter(i['category'] for i in manifest['items']));manifest['sourceHashAfter']=digest(candidate)
    manifest.setdefault('revisions',[]).append(dict(date='2026-10-15',reason='Organize camp items and everyday goods into five camp groups.',movedIds=list(selected),sourceHashBefore=plan['sourceHash'],sourceHashAfter=digest(candidate)))
    release_guard();atomic_write(source,candidate,plan['sourceHash'])
    atomic_write(MANIFEST,(json.dumps(manifest,indent=2)+'\n').encode(),digest(old_manifest))
    audit=dict(date='2026-10-15',definition=DEFINITION,items=plan['items'],groups={k:len(v) for k,v in GROUPS.items()},sourceHashBefore=plan['sourceHash'],sourceHashAfter=digest(candidate),retiredCategory='Everyday objects',scope='Catalog only; preserve IDs, art/review state, tiers, types, purposes, recipes, fitments, deterministic world identity, terrain, streaming and persisted gameplay deltas.')
    atomic_write(AUDIT,(json.dumps(audit,indent=2)+'\n').encode())
    (OUT/'after.xlsx').write_bytes(candidate);(OUT/'receipt.json').write_text(json.dumps(audit,indent=2),encoding='utf-8')
    print('Saved 43 camp items in five groups; original IDs and gameplay fields retained.')


if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('action',choices=['prepare','apply','run'])
    action=p.parse_args().action
    run() if action=='run' else prepare() if action=='prepare' else apply()
