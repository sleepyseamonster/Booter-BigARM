"""Append five fur armor pieces using the guarded catalog proposal workflow."""
import json

import append_catalog_proposals as append
import organize_camp_items as guard
from workbook_edits import atomic_write

OUT = append.ROOT / 'Logs/FurArmor20261015'
SPEC = 'Docs/Design/Items/FUR_ARMOR_PROPOSALS_2026-10-15.json'


def prepare():
    append.main('prepare', SPEC, str(OUT.relative_to(append.ROOT)))


def apply():
    live = append.SOURCE
    plan = json.loads((OUT / 'plan.json').read_text(encoding='utf-8'))
    append.SOURCE = OUT / 'candidate.xlsx'
    append.SOURCE.write_bytes((OUT / 'before.xlsx').read_bytes())
    try:
        append.main('apply', SPEC, str(OUT.relative_to(append.ROOT)))
    finally:
        append.SOURCE = live
    guard.release_guard()
    atomic_write(live, (OUT / 'after.xlsx').read_bytes(), plan['sourceHash'])
    print('Saved five separate fur armor items; no whole-set inventory item.')


if __name__ == '__main__':
    guard.OUT, guard.prepare, guard.apply = OUT, prepare, apply
    guard.run()
