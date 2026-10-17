"""Append four iron plating finds; preserve all existing catalog records.

Artifact Tool input authoring and guarded preserving merge retain local evidence
in Logs/FourIronPlateSalvage20261016. Value tiers do not confer crafting readiness.
"""
import gc
from io import BytesIO
from zipfile import ZipFile

import add_arc_power_armor as batch
from add_ancient_salvage import build as append_items
from category_edits import parts_for, patch_cells, verify_move
from workbook_edits import digest, parse_sheets


def mark_as_found(snapshot, ids):
    """Value-ranked salvage is neither finished gear nor intermediate stock."""
    rows = parse_sheets(snapshot)['Craft audit']
    edits = {f'E{n}': 'As-found iron salvage' for n, row in enumerate(rows, 1) if row and row[0] in ids}
    assert len(edits) == len(ids) == 4
    output = BytesIO()
    with ZipFile(BytesIO(snapshot)) as source, ZipFile(output, 'w') as target:
        part = parts_for(source)['Craft audit']
        revised = patch_cells(source.read(part).decode(), edits).encode()
        for info in source.infolist():
            target.writestr(info, revised if info.filename == part else source.read(info))
    candidate = output.getvalue()
    verify_move(snapshot, candidate, {'Craft audit': edits})
    return candidate


def build(snapshot, spec):
    gc.collect()
    candidate, receipt = append_items(snapshot, spec)
    gc.collect()
    candidate = mark_as_found(candidate, {item['id'] for item in spec['items']})
    receipt.update(resultHash=digest(candidate), access='All four audit records identify as-found iron salvage; value tiers confer no crafting readiness.')
    return candidate, receipt

if __name__ == '__main__':
    batch.OUT = batch.append.ROOT / 'Logs/FourIronPlateSalvage20261016'
    batch.SPEC = 'Docs/Design/Items/FOUR_IRON_PLATE_SALVAGE_2026-10-16.json'
    batch.append.build = build
    batch.guard.OUT = batch.OUT
    batch.guard.prepare = batch.prepare
    batch.guard.apply = batch.apply
    batch.guard.run()
