"""Add ten T0/T1 metal finds through the existing guarded catalog appender."""
from io import BytesIO
from zipfile import ZipFile

import add_arc_power_armor as batch
from add_ancient_salvage import build as append_items
from category_edits import parts_for, patch_cells, verify_move
from workbook_edits import digest, parse_sheets


def build(snapshot, spec):
    assert len(spec['items']) == 10
    candidate, receipt = append_items(snapshot, spec)
    ids = {item['id'] for item in spec['items']}
    sheets = parse_sheets(candidate)
    rows = [row for row in sheets['Items'][5:] if row[0] in ids]
    assert sum(row[2] == 'T0' for row in rows) == 6
    assert sum(row[2] == 'T1' for row in rows) == 4
    assert all(row[3] == 'Salvage' and row[27] == 'Metal' for row in rows)
    edits = {f'E{n}': 'As-found metal salvage' for n, row in enumerate(sheets['Craft audit'], 1)
             if row and row[0] in ids}
    assert len(edits) == len(ids)
    output = BytesIO()
    with ZipFile(BytesIO(candidate)) as source, ZipFile(output, 'w') as target:
        part = parts_for(source)['Craft audit']
        revised = patch_cells(source.read(part).decode(), edits).encode()
        for info in source.infolist():
            target.writestr(info, revised if info.filename == part else source.read(info))
    result = output.getvalue()
    verify_move(candidate, result, {'Craft audit': edits})
    receipt.update(resultHash=digest(result), tierCounts={'T0': 6, 'T1': 4},
                   rarityCounts={rarity: sum(row[15] == rarity for row in rows)
                                 for rarity in ['Junk', 'Common', 'Uncommon', 'Rare']},
                   scope='Ten as-found metal salvage definitions; all prior catalog records preserved.')
    return result, receipt


if __name__ == '__main__':
    batch.OUT = batch.append.ROOT / 'Logs/TenMetalSalvage20261016'
    batch.SPEC = 'Docs/Design/Items/TEN_METAL_SALVAGE_2026-10-16.json'
    batch.append.build = build
    batch.guard.OUT = batch.OUT
    batch.guard.prepare = batch.prepare
    batch.guard.apply = batch.apply
    batch.guard.run()
