"""One-time editable rarity defaults from live tiers; never retier an item."""
import argparse
import json
import re
from collections import Counter
from io import BytesIO
from pathlib import Path
from zipfile import ZipFile
from xml.sax.saxutils import escape

import openpyxl
from category_edits import CELL,parts_for,patch_cells,verify_move
from workbook_edits import atomic_write,digest,master_items,parse_sheets

ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'Docs/Design/Items/MASTER_ITEM_DATABASE.xlsx'
OUT=ROOT/'Logs/RarityBaseline20261016'
AUDIT=ROOT/'Docs/Design/Items/RARITY_BASELINE_2026-10-16.json'
MAPPING={'T0':'Junk','T1':'Common','T2':'Uncommon','T3':'Rare','T4':'Epic'}


def assignments(snapshot):
    sheets=parse_sheets(snapshot)
    h=sheets['Items'][4]
    rows=master_items(sheets)
    assert len({r[0] for _,r in rows})==len(rows)
    decisions=[]
    changes={}
    for n,row in rows:
        item=dict(zip(h,row))
        tier=item['Tier']
        rarity=MAPPING.get(tier)
        fallback=False
        if rarity is None:
            assert tier=='Not applicable' and item['Origin family']=='Ancient technology' and item['Category']=='Ancient salvage', (item['Item ID'],tier,'Needs an explicit rarity decision')
            rarity='Epic'
            fallback=True
        decisions.append(dict(id=item['Item ID'],name=item['Item name'],tier=tier,
            previousRarity=item['Rarity'],rarity=rarity,
            basis='Canonically extremely rare ancient technology; no numeric tier to map' if fallback else 'Author tier-to-rarity baseline'))
        if item['Rarity']!=rarity:
            changes[f'P{n}']=rarity
    return sheets,decisions,{'Items':changes}


def merge(snapshot,changes):
    stream=BytesIO()
    with ZipFile(BytesIO(snapshot)) as archive,ZipFile(stream,'w') as result:
        part=parts_for(archive)['Items']
        raw=archive.read(part).decode()
        cells={m.group(1):m.group() for m in re.finditer(CELL,raw,re.S)}
        assert all(not re.search(r'<(?:\w+:)?f\b',cells.get(a,'')) for a in changes['Items']), 'Rarity formulas require explicit review.'
        def revised_cell(match):
            address=match.group(1)
            if address not in changes['Items']:
                return match.group()
            old=match.group()
            prefix=re.search(r'<((?:\w+:)?)c\b',old).group(1)
            opening=re.sub(r'\s+t="[^"]*"','',old[:old.index('>')].rstrip('/'))
            return opening+f' t="inlineStr"><{prefix}is><{prefix}t xml:space="preserve">{escape(changes["Items"][address])}</{prefix}t></{prefix}is></{prefix}c>'
        revised=re.sub(CELL,revised_cell,raw,flags=re.S)
        missing={a:v for a,v in changes['Items'].items() if a not in cells}
        replacement=(patch_cells(revised,missing) if missing else revised).encode()
        for info in archive.infolist():
            result.writestr(info,replacement if info.filename==part else archive.read(info.filename))
    candidate=stream.getvalue()
    verify_move(snapshot,candidate,changes,category_contract=False)
    return candidate


def prepare():
    assert not AUDIT.exists(),'Baseline already applied; preserve subsequent manual rarity edits.'
    snapshot=SOURCE.read_bytes()
    sheets,items,changes=assignments(snapshot)
    OUT.mkdir(parents=True,exist_ok=True)
    plan=dict(sourceHash=digest(snapshot),mapping=MAPPING,items=items,changes=changes)
    (OUT/(digest(snapshot)+'.xlsx')).write_bytes(snapshot)
    (OUT/'before.xlsx').write_bytes(snapshot)
    (OUT/'plan.json').write_bytes((json.dumps(plan,indent=2)+'\n').encode())
    # Scoped layout preview of real names, rarity values and source dimensions.
    sample=[]
    for tier in MAPPING:
        sample.extend([i for i in items if i['tier']==tier][:1])
    sample.extend([i for i in items if i['tier']=='Not applicable'][:1])
    book=openpyxl.load_workbook(BytesIO(snapshot),data_only=True)
    positions={r[0]:n for n,r in master_items(sheets)}
    for item in sample:
        n=positions[item['id']]
        item['widths']=[book['Items'].column_dimensions[c].width for c in ['B','C','P']]
        item['height']=book['Items'].row_dimensions[n].height or 20
    book.close()
    (OUT/'preview.json').write_bytes((json.dumps(sample,indent=2)+'\n').encode())
    print('Prepared',len(items),'rarities;',len(changes['Items']),'changed;',Counter(i['rarity'] for i in items))


def apply():
    assert not AUDIT.exists()
    plan=json.loads((OUT/'plan.json').read_bytes())
    original=(OUT/'before.xlsx').read_bytes()
    current=SOURCE.read_bytes()
    # Rebase over unrelated manual edits, checking item identity, current tier and
    # existing rarity rather than overwriting an intervening author adjustment.
    sheets,items,changes=assignments(current)
    planned={i['id']:i for i in plan['items']}
    assert all(i['id'] in planned and i==planned[i['id']] for i in items),'Roster, name, tier or rarity changed; prepare again from the current workbook.'
    authored=openpyxl.load_workbook(OUT/'authored.xlsx',data_only=True)
    for address,value in changes['Items'].items():
        assert authored['Items'][address].value==value,(address,value)
    authored.close()
    candidate=merge(current,changes)
    before=sheets
    after=parse_sheets(candidate)
    assert [r[0] for _,r in master_items(before)]==[r[0] for _,r in master_items(after)]
    assert all(old[:15]+old[16:]==new[:15]+new[16:] for (_,old),(_,new) in zip(master_items(before),master_items(after)))
    expected={i['id']:i['rarity'] for i in items}
    assert all(r[15]==expected[r[0]] for _,r in master_items(after))
    audit=dict(date='2026-10-16',authority='Author requested rarity assignments for all items, using tier mapping as a rule of thumb before manual rebalance.',
        mapping=MAPPING,reviewed=len(items),changed=len(changes['Items']),counts=dict(Counter(i['rarity'] for i in items)),
        nonNumericTierFallback='Ancient technology is canonically extremely rare. Ancient salvage with Not applicable tier receives Epic, retaining its original tier; thirty had Extremely rare labels and two had blank rarity.',
        fallbackCount=sum(i['tier']=='Not applicable' for i in items),items=items,
        sourceHashBefore=digest(current),sourceHashAfter=digest(candidate),
        scope='One-time local Items.Rarity values only, including both salvage sections by current author instruction. These are editable initial defaults, not a formula or automatic ongoing coupling. No tiers, identities, quantities, descriptions, source conditions, recipes, review/art choices or Unity/world/save data changed. Google Sheet unsynchronized.',
        validation='Exact changed-cell allowlist passed; all formulas, styles, other cells and unrelated ZIP members preserved. All IDs and all fields other than Rarity retained.')
    (OUT/'pre-apply.xlsx').write_bytes(current)
    (OUT/'after.xlsx').write_bytes(candidate)
    atomic_write(SOURCE,candidate,digest(current))
    atomic_write(AUDIT,(json.dumps(audit,indent=2)+'\n').encode())
    print('Applied',audit['reviewed'],'rarities:',audit['counts'])


if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('action',choices=['prepare','apply'])
    prepare() if parser.parse_args().action=='prepare' else apply()
