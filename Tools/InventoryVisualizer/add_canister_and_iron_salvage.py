"""Add ten canister and ten iron finds, preserving existing catalog records.

Artifact Tool authoring, cleared-row reservations and verification are retained in
Logs/CanisterAndIronSalvage20261016. All items are as-found, value-ranked salvage.
"""
import gc
import re
from collections import Counter
from io import BytesIO
from zipfile import ZipFile
from xml.sax.saxutils import escape
from xml.etree import ElementTree as ET
import add_arc_power_armor as batch
from add_ancient_salvage import build as append_items
from category_edits import CELL, parts_for, patch_cells, verify_move
from workbook_edits import digest, parse_sheets


def repair_use_links(snapshot, ids):
    """Repair only the catalog batches affected by recycled master positions."""
    sheets = parse_sheets(snapshot)
    master = {row[0]: (n, row) for n, row in enumerate(sheets['Items'], 1) if row and row[0] in ids}
    edits = {}
    output = BytesIO()
    with ZipFile(BytesIO(snapshot)) as source, ZipFile(output, 'w') as target:
        part = parts_for(source)['Item uses']
        raw = source.read(part).decode()
        prefix = re.search(r'<((?:\w+:)?)worksheet\b', raw).group(1)
        cells = {m.group(1): m.group() for m in re.finditer(CELL, raw, re.S)}
        for number, row in enumerate(sheets['Item uses'], 1):
            if not row or row[0] not in master: continue
            n, item = master[row[0]]
            for col, master_col, value in [('B','B',item[1]),('C','D',item[3]),('E','AA',item[26])]:
                address = f'{col}{number}'; old = cells[address]
                formula = f'Items!{master_col}{n}'
                revised, changed = re.subn(r'(<(?:\w+:)?f\b[^>]*>).*?(</(?:\w+:)?f>)', lambda m: m.group(1)+escape(formula)+m.group(2), old, count=1, flags=re.S)
                assert changed == 1, (address, 'expected direct formula')
                wrapper = f'<{prefix}worksheet>'
                closing = f'</{prefix}worksheet>'
                revised = patch_cells(wrapper+revised+closing, {address:value}).removeprefix(wrapper).removesuffix(closing)
                if revised != old:
                    assert old[:old.index('>')] == revised[:revised.index('>')], (address,'style')
                    raw = raw.replace(old,revised,1)
                    edits[address] = {'formula':'='+formula,'value':value}
        resulting = {m.group(1):m.group() for m in re.finditer(CELL,raw,re.S)}
        assert resulting.keys() == cells.keys()
        assert all(resulting[a] == cell for a, cell in cells.items() if a not in edits)
        ET.fromstring(raw)
        for info in source.infolist(): target.writestr(info,raw.encode() if info.filename == part else source.read(info))
    return output.getvalue(), edits


def prior_canister_ids():
    import json
    spec = batch.append.ROOT / 'Docs/Design/Items/FOUR_CANISTER_SALVAGE_2026-10-16.json'
    return {item['id'] for item in json.loads(spec.read_bytes())['items']}


def build(snapshot, spec):
    assert Counter(item['group'] for item in spec['items']) == {'canister': 10, 'iron': 10}
    gc.collect()
    original = snapshot
    snapshot, repairs = repair_use_links(snapshot, prior_canister_ids())
    candidate, receipt = append_items(snapshot, spec)
    gc.collect()
    groups = {item['id']: item['group'] for item in spec['items']}
    edits = {f'E{n}': f'As-found {groups[row[0]]} salvage' for n, row in enumerate(parse_sheets(candidate)['Craft audit'], 1) if row and row[0] in groups}
    assert len(edits) == 20
    output = BytesIO()
    with ZipFile(BytesIO(candidate)) as source, ZipFile(output, 'w') as target:
        part = parts_for(source)['Craft audit']
        revised = patch_cells(source.read(part).decode(), edits).encode()
        for info in source.infolist():
            target.writestr(info, revised if info.filename == part else source.read(info))
    result = output.getvalue()
    verify_move(candidate, result, {'Craft audit': edits})
    receipt.update(sourceHash=digest(original),resultHash=digest(result), groups={'canister': 10, 'iron': 10}, existingUseRepairs=repairs, access='Twenty as-found salvage records; value and apparent quality do not confer readiness or usable contents.')
    return result, receipt


if __name__ == '__main__':
    batch.OUT = batch.append.ROOT / 'Logs/CanisterAndIronSalvage20261016'
    batch.SPEC = 'Docs/Design/Items/CANISTER_AND_IRON_SALVAGE_2026-10-16.json'
    batch.append.build = build
    batch.guard.OUT = batch.OUT
    batch.guard.prepare = batch.prepare
    batch.guard.apply = batch.apply
    batch.guard.run()
