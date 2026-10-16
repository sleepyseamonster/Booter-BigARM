"""Add reviewed camp stations and expand the existing firebox without a duplicate.

Prepare a small Artifact Tool input workbook, then apply verified inputs through
the preserving appender. The real workbook is replaced once with a hash guard.
"""
import argparse
import json
from collections import Counter
from io import BytesIO
from zipfile import ZipFile

import openpyxl
from add_ancient_salvage import ROOT, SOURCE, build
from category_edits import parts_for, patch_cells, verify_move
from workbook_edits import atomic_write, digest, parse_sheets

DATA = ROOT / 'Docs/Design/Items/CAMP_CRAFTING_STATION_PROPOSALS.json'
OUT = ROOT / 'Logs/CampCraftingStations20261015'


def prepare():
    OUT.mkdir(parents=True, exist_ok=True)
    original = SOURCE.read_bytes()
    spec = json.loads(DATA.read_text(encoding='utf-8'))
    (OUT / 'before.xlsx').write_bytes(original)
    (OUT / 'plan.json').write_text(json.dumps(dict(sourceHash=digest(original), spec=spec), indent=2), encoding='utf-8')
    print('Prepared nine new station inputs and one existing cooking-station revision.')


def apply():
    plan = json.loads((OUT / 'plan.json').read_text(encoding='utf-8'))
    assert digest(SOURCE.read_bytes()) == plan['sourceHash'], 'Workbook changed; prepare again'
    spec = plan['spec']
    assert json.loads(DATA.read_text(encoding='utf-8')) == spec, 'Proposal changed'
    authored = openpyxl.load_workbook(OUT / 'authored.xlsx', read_only=True, data_only=True)
    rows = list(authored['Station inputs'].values)[1:]
    assert len(rows) == len(spec['items'])
    for row, item in zip(rows, spec['items']):
        assert list(row[:6]) == [item[k] for k in ['id','name','purpose','icon','material','use']]
        assert json.loads(row[6]) == item.get('fields', {})
    authored.close()
    candidate, receipt = build((OUT / 'before.xlsx').read_bytes(), spec)
    sheets = parse_sheets(candidate)
    update = spec['existingStationUpdate']
    changes = {}
    for sheet, rows in sheets.items():
        for n, row in enumerate(rows, 1):
            if update['id'] in row:
                for i, value in enumerate(row):
                    if value == update['previousName']:
                        changes.setdefault(sheet, {})[f'{openpyxl.utils.get_column_letter(i+1)}{n}'] = update['name']
    number, current = next((n, r) for n, r in enumerate(sheets['Items'], 1) if r and r[0] == update['id'])
    assert current[1] == update['previousName'] and current[3] == 'Crafting stations'
    header = sheets['Items'][4]
    fields = dict(zip(header, current))
    fields.update({'Item name':update['name'], 'Purpose':update['purpose'],
                   'Search aliases':update['aliases'], 'Limitations':update['limitations'],
                   'Subtype':'Camp cooking station', 'Design status':'Author direction',
                   'Classification basis':'Author requested a full camp cooking station. Expanded existing firebox concept; stable item ID retained, without a duplicate heat station or implemented recipes.'})
    fields['Search terms'] = '; '.join(str(fields.get(k) or '') for k in ['Item ID','Item name','Category','Subtype','Item type','Processing stage','Origin family','Primary use','Material family','Physical form','Design status','Search aliases','Tier','Finished-item input tier','Crafting role']).lower()
    for key in ['Item name','Purpose','Search aliases','Limitations','Subtype','Design status','Classification basis','Search terms']:
        changes.setdefault('Items', {})[f'{openpyxl.utils.get_column_letter(header.index(key)+1)}{number}'] = fields[key]
    planning = next(n for n,r in enumerate(sheets['Categories'],1) if r and r[0]=='Workstations and storage')
    changes.setdefault('Categories', {})[f'D{planning}'] = 'Workbench, Ironstone refinery and Dust refinery now have Crafting stations catalog records. Camp chest remains a storage world object. Catalog registration does not install or duplicate world objects.'
    counts = Counter(r[3] for r in sheets['Items'][5:] if r[0])
    for n,r in enumerate(sheets['Categories'],1):
        if len(r)>4 and r[4]=='Primary category':
            changes['Categories'][f'B{n}'] = counts[r[0]]
    with ZipFile(BytesIO(candidate)) as archive:
        parts = parts_for(archive)
        patches = {parts[s]:patch_cells(archive.read(parts[s]).decode(), edits).encode() for s,edits in changes.items()}
        result = BytesIO()
        with ZipFile(result,'w') as out:
            for info in archive.infolist():out.writestr(info,patches.get(info.filename,archive.read(info.filename)))
    final = result.getvalue()
    verify_move(candidate, final, changes)
    final_rows = parse_sheets(final)['Items'][5:]
    assert sum(r[3]=='Crafting stations' for r in final_rows if r[0]) == 12
    assert sum(r[0]==update['id'] for r in final_rows if r[0]) == 1
    (OUT / 'after.xlsx').write_bytes(final)
    atomic_write(SOURCE, final, plan['sourceHash'])
    receipt.update(resultHash=digest(final), revisedStation=update['id'], stationCount=12)
    (OUT / 'receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8')
    print(json.dumps(receipt,indent=2))


if __name__ == '__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('action',choices=['prepare','apply'])
    args=parser.parse_args()
    prepare() if args.action=='prepare' else apply()
