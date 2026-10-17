"""Add five independent Arc power armor pieces with a guarded preserving append."""
import json

import append_catalog_proposals as append
import organize_camp_items as guard
from workbook_edits import atomic_write, digest

OUT=append.ROOT/'Logs/ArcPowerArmor20261015'
SPEC='Docs/Design/Items/ARC_POWER_ARMOR_PROPOSALS_2026-10-15.json'


def prepare():
    append.main('prepare',SPEC,str(OUT.relative_to(append.ROOT)))


def apply():
    live=append.SOURCE
    plan=json.loads((OUT/'plan.json').read_text(encoding='utf-8'))
    append.SOURCE=OUT/'candidate.xlsx'
    append.SOURCE.write_bytes((OUT/'before.xlsx').read_bytes())
    try:append.main('apply',SPEC,str(OUT.relative_to(append.ROOT)))
    finally:append.SOURCE=live
    guard.release_guard()
    atomic_write(live,(OUT/'after.xlsx').read_bytes(),plan['sourceHash'])
    print('Saved',len(plan['spec']['items']),'separate catalog items; no whole-set inventory item.')


if __name__=='__main__':
    guard.OUT=OUT;guard.prepare=prepare;guard.apply=apply;guard.run()
