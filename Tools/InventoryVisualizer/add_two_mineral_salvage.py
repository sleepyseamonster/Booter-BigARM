"""Append two T1/Uncommon actual minerals with preserved catalog records."""
from io import BytesIO
from zipfile import ZipFile

import add_arc_power_armor as batch
from add_ancient_salvage import build as append_items
from category_edits import parts_for, patch_cells, verify_move
from workbook_edits import build_deletion, digest, parse_sheets


def build(snapshot, spec):
    assert len(spec['items']) == 2
    original_hash = digest(snapshot)
    retired = []
    # The author clarified the two just-created proposals while the append was
    # completing. Replace those task-owned worked objects with actual minerals.
    for key in ['proposal.salvage.scored_soapstone_mold_half', 'proposal.salvage.chipped_agate_burnisher']:
        if any(row[0] == key for row in parse_sheets(snapshot)['Items'][5:]):
            snapshot, removal = build_deletion(snapshot, key)
            assert removal['clearedRows'] == {'Items': 1, 'Item uses': 1, 'Craft audit': 1}
            retired.append(key)
    candidate, receipt = append_items(snapshot, spec)
    ids = {item['id'] for item in spec['items']}
    sheets = parse_sheets(candidate)
    rows = [row for row in sheets['Items'][5:] if row[0] in ids]
    assert len(rows) == 2
    assert all((row[2], row[3], row[15], row[27]) == ('T1', 'Salvage', 'Uncommon', 'Mineral') for row in rows)
    edits = {f'E{n}': 'As-found mineral salvage' for n, row in enumerate(sheets['Craft audit'], 1)
             if row and row[0] in ids}
    assert len(edits) == 2
    output = BytesIO()
    with ZipFile(BytesIO(candidate)) as source, ZipFile(output, 'w') as target:
        part = parts_for(source)['Craft audit']
        revised = patch_cells(source.read(part).decode(), edits).encode()
        for info in source.infolist():
            target.writestr(info, revised if info.filename == part else source.read(info))
    result = output.getvalue()
    verify_move(candidate, result, {'Craft audit': edits})
    receipt.update(sourceHash=original_hash, resultHash=digest(result), replacedTaskProposals=retired,
                   scope='Two T1/Uncommon actual mineral definitions; unrelated records preserved.')
    return result, receipt


if __name__ == '__main__':
    batch.OUT = batch.append.ROOT / 'Logs/TwoMineralSalvage20261016'
    batch.SPEC = 'Docs/Design/Items/TWO_MINERAL_SALVAGE_2026-10-16.json'
    batch.append.build = build
    batch.guard.OUT = batch.OUT
    batch.guard.prepare = batch.prepare
    batch.guard.apply = batch.apply
    batch.guard.run()
