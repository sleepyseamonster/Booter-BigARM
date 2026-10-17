"""Register the author-directed recovery beacon without implementing runtime death."""
from io import BytesIO
from zipfile import ZipFile

import add_arc_power_armor as batch
from add_ancient_salvage import build as append_items
from category_edits import parts_for, patch_cells, verify_move
from workbook_edits import digest, parse_sheets


def build(snapshot, spec):
    assert len(spec['items']) == 1
    candidate, receipt = append_items(snapshot, spec)
    key = 'author.tool.radio_recovery_beacon'
    sheets = parse_sheets(candidate)
    rows = [row for row in sheets['Items'][5:] if row[0] == key]
    assert len(rows) == 1
    assert (rows[0][1], rows[0][2], rows[0][3], rows[0][15], rows[0][23], rows[0][29]) == (
        'Radio recovery beacon', 'T3', 'Tools', 'Rare', 'Tool', 'Author direction')
    edits = {f'E{n}': 'Author-directed tool; implementation pending' for n, row in enumerate(sheets['Craft audit'], 1)
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
    receipt.update(resultHash=digest(result), scope='One Tools definition. Core recovery behavior recorded; no runtime death or gear-drop system changed.')
    return result, receipt


if __name__ == '__main__':
    batch.OUT = batch.append.ROOT / 'Logs/RadioRecoveryBeacon20261016'
    batch.SPEC = 'Docs/Design/Items/RADIO_RECOVERY_BEACON_2026-10-16.json'
    batch.append.build = build
    batch.guard.OUT = batch.OUT
    batch.guard.prepare = batch.prepare
    batch.guard.apply = batch.apply
    batch.guard.run()
