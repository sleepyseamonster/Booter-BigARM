"""Append one T1/Epic native-metal mineral through the preserving catalog path."""
from io import BytesIO
from zipfile import ZipFile

import add_arc_power_armor as batch
from add_ancient_salvage import build as append_items
from category_edits import parts_for, patch_cells, verify_move
from workbook_edits import digest, parse_sheets


def build(snapshot, spec):
    assert len(spec['items']) == 1
    candidate, receipt = append_items(snapshot, spec)
    key = spec['items'][0]['id']
    sheets = parse_sheets(candidate)
    rows = [row for row in sheets['Items'][5:] if row[0] == key]
    assert len(rows) == 1 and (rows[0][2], rows[0][3], rows[0][15], rows[0][27]) == ('T1', 'Salvage', 'Epic', 'Metal')
    edits = {f'E{n}': 'As-found native-metal mineral salvage' for n, row in enumerate(sheets['Craft audit'], 1)
             if row and row[0] == key}
    assert len(edits) == 1
    output = BytesIO()
    with ZipFile(BytesIO(candidate)) as source, ZipFile(output, 'w') as target:
        part = parts_for(source)['Craft audit']
        revised = patch_cells(source.read(part).decode(), edits).encode()
        for info in source.infolist():
            target.writestr(info, revised if info.filename == part else source.read(info))
    result = output.getvalue()
    verify_move(candidate, result, {'Craft audit': edits})
    receipt.update(resultHash=digest(result), scope='One T1/Epic native bismuth mineral-metal salvage item; prior records preserved.')
    return result, receipt


if __name__ == '__main__':
    batch.OUT = batch.append.ROOT / 'Logs/NativeBismuthSalvage20261016'
    batch.SPEC = 'Docs/Design/Items/NATIVE_BISMUTH_SALVAGE_2026-10-16.json'
    batch.append.build = build
    batch.guard.OUT = batch.OUT
    batch.guard.prepare = batch.prepare
    batch.guard.apply = batch.apply
    batch.guard.run()
