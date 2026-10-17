"""Add four recovered canister items with explicit as-found audit labels.

Uses cleared master/audit slots if the formula capacity has been reached.
Artifact Tool inputs, snapshots and proof stay in Logs/FourCanisterSalvage20261016.
"""
import gc
from io import BytesIO
from zipfile import ZipFile
import add_arc_power_armor as batch
from add_ancient_salvage import build as append_items
from category_edits import parts_for, patch_cells, verify_move
from workbook_edits import digest, parse_sheets


def build(snapshot, spec):
    gc.collect()
    candidate, receipt = append_items(snapshot, spec)
    gc.collect()
    ids = {item['id'] for item in spec['items']}
    edits = {f'E{n}': 'As-found canister salvage' for n, row in enumerate(parse_sheets(candidate)['Craft audit'], 1) if row and row[0] in ids}
    assert len(edits) == len(ids) == 4
    output = BytesIO()
    with ZipFile(BytesIO(candidate)) as source, ZipFile(output, 'w') as target:
        part = parts_for(source)['Craft audit']
        revised = patch_cells(source.read(part).decode(), edits).encode()
        for info in source.infolist():
            target.writestr(info, revised if info.filename == part else source.read(info))
    result = output.getvalue()
    verify_move(candidate, result, {'Craft audit': edits})
    receipt.update(resultHash=digest(result), access='Four as-found canister records; empty vessels grant no usable contents and quality does not qualify a recovered part.')
    return result, receipt


if __name__ == '__main__':
    batch.OUT = batch.append.ROOT / 'Logs/FourCanisterSalvage20261016'
    batch.SPEC = 'Docs/Design/Items/FOUR_CANISTER_SALVAGE_2026-10-16.json'
    batch.append.build = build
    batch.guard.OUT = batch.OUT
    batch.guard.prepare = batch.prepare
    batch.guard.apply = batch.apply
    batch.guard.run()
